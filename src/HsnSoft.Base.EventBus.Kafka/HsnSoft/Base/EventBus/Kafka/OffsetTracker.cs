using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>
/// Which offsets can be committed when messages are handled at the same time and finish in any order. Kafka remembers one offset for a partition: "everything before this is done". So an offset may be committed only when every offset
/// before it, in its partition, is finished too; a message that is still being handled (or that failed) holds the commit back, and it is read again after a restart.
/// </summary>
internal sealed class OffsetTracker
{
    private readonly Lock _lock = new();
    private readonly Dictionary<TopicPartition, SortedDictionary<long, bool>> _pending = [];
    private readonly Dictionary<TopicPartition, long> _ready = [];

    /// <summary>A message was handed to a handler (offsets of a partition arrive in order).</summary>
    public void Register(TopicPartition partition, long offset)
    {
        lock (_lock)
        {
            if (!_pending.TryGetValue(partition, out SortedDictionary<long, bool>? offsets))
            {
                _pending[partition] = offsets = [];
            }

            offsets[offset] = false;
        }
    }

    /// <summary>A message is dealt with (handled, or sent to the dead-letter topic). What it frees is ready to be committed.</summary>
    public void Complete(TopicPartition partition, long offset)
    {
        lock (_lock)
        {
            // the partition may have been taken away meanwhile: nothing is committed for it
            if (!_pending.TryGetValue(partition, out SortedDictionary<long, bool>? offsets) || !offsets.ContainsKey(offset))
            {
                return;
            }

            offsets[offset] = true;
            while (offsets.Count > 0 && offsets.First() is { Value: true } first)
            {
                offsets.Remove(first.Key);
                _ready[partition] = first.Key + 1;
            }
        }
    }

    /// <summary>The offsets to commit now (the next offset to read of each partition that moved), and forgets them.</summary>
    public IReadOnlyList<TopicPartitionOffset> TakeReady()
    {
        lock (_lock)
        {
            List<TopicPartitionOffset> ready = [.. _ready.Select(pair => new TopicPartitionOffset(pair.Key, new Offset(pair.Value)))];
            _ready.Clear();
            return ready;
        }
    }

    /// <summary>The partitions are no longer this consumer's: what is pending for them is dropped.</summary>
    public void Forget(IEnumerable<TopicPartition> partitions)
    {
        lock (_lock)
        {
            foreach (TopicPartition partition in partitions)
            {
                _pending.Remove(partition);
                _ready.Remove(partition);
            }
        }
    }
}
