using Confluent.Kafka;

namespace HsnSoft.Base.EventBus.Kafka.Tests;

public class OffsetTrackerTests
{
    private static readonly TopicPartition s_first = new("events", 0);
    private static readonly TopicPartition s_second = new("events", 1);

    private static long[] Ready(OffsetTracker tracker, TopicPartition partition)
        => [.. tracker.TakeReady().Where(offset => offset.TopicPartition == partition).Select(offset => offset.Offset.Value)];

    [Fact]
    public void MessagesThatFinishInOrder_AreCommittedUpToTheLastOne()
    {
        OffsetTracker tracker = new();
        tracker.Register(s_first, 10);
        tracker.Register(s_first, 11);
        tracker.Complete(s_first, 10);
        tracker.Complete(s_first, 11);

        Assert.Equal([12L], Ready(tracker, s_first));
    }

    [Fact]
    public void AMessageThatIsStillRunning_HoldsBackTheOnesAfterIt()
    {
        OffsetTracker tracker = new();
        foreach (long offset in new long[] { 5, 6, 7 })
        {
            tracker.Register(s_first, offset);
        }

        tracker.Complete(s_first, 6);
        tracker.Complete(s_first, 7);

        // 5 is not finished: nothing can be committed, or a crash would lose it
        Assert.Empty(tracker.TakeReady());

        tracker.Complete(s_first, 5);

        Assert.Equal([8L], Ready(tracker, s_first));
    }

    [Fact]
    public void WhatWasTaken_IsNotTakenAgain_AndTheNextOnesCommitFromWhereItStopped()
    {
        OffsetTracker tracker = new();
        tracker.Register(s_first, 0);
        tracker.Register(s_first, 1);
        tracker.Complete(s_first, 0);

        Assert.Equal([1L], Ready(tracker, s_first));
        Assert.Empty(tracker.TakeReady());

        tracker.Complete(s_first, 1);

        Assert.Equal([2L], Ready(tracker, s_first));
    }

    [Fact]
    public void EveryPartitionHasItsOwnOrder()
    {
        OffsetTracker tracker = new();
        tracker.Register(s_first, 3);
        tracker.Register(s_second, 40);
        tracker.Register(s_second, 41);
        tracker.Complete(s_second, 40);
        tracker.Complete(s_second, 41);

        IReadOnlyList<TopicPartitionOffset> ready = tracker.TakeReady();

        Assert.Equal(s_second, Assert.Single(ready).TopicPartition);
        Assert.Equal(42, ready[0].Offset.Value);
    }

    [Fact]
    public void ApartitionThatWasTakenAway_CommitsNothing_AndALateCompletionIsIgnored()
    {
        OffsetTracker tracker = new();
        tracker.Register(s_first, 1);
        tracker.Register(s_second, 9);

        tracker.Forget([s_first]);
        tracker.Complete(s_first, 1);
        tracker.Complete(s_first, 77);
        tracker.Complete(s_second, 9);

        Assert.Equal([10L], Ready(tracker, s_second));
        Assert.Empty(tracker.TakeReady());
    }

    [Fact]
    public void ForgettingAPartition_AlsoDropsWhatWasReadyButNotTaken()
    {
        OffsetTracker tracker = new();
        tracker.Register(s_first, 1);
        tracker.Complete(s_first, 1);

        tracker.Forget([s_first]);

        Assert.Empty(tracker.TakeReady());
    }
}
