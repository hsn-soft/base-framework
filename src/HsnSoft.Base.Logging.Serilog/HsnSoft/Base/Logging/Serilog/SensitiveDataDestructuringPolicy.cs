using System.Linq;
using HsnSoft.Base.Logging.Masking;
using Serilog.Core;
using Serilog.Events;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Destructures an object that is logged with <c>{@Object}</c> the way <see cref="SensitiveDataTree"/> says (what is secret already masked), as Serilog values: the tree is the same for every logging library, only
/// its conversion is Serilog's.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = Convert(SensitiveDataTree.Build(value), propertyValueFactory);
        return true;
    }

    private static LogEventPropertyValue Convert(MaskedNode node, ILogEventPropertyValueFactory factory)
        => node switch
        {
            MaskedScalar { Value: null } => new ScalarValue(null),

            // a plain value is a value of Serilog's own, as Serilog makes it
            MaskedScalar { Value: { } plain } => factory.CreatePropertyValue(plain, destructureObjects: false),
            MaskedStructure structure => new StructureValue(structure.Properties.Select(property => new LogEventProperty(property.Key, Convert(property.Value, factory))), structure.TypeName),
            MaskedDictionary dictionary => new DictionaryValue(dictionary.Entries.Select(entry => new System.Collections.Generic.KeyValuePair<ScalarValue, LogEventPropertyValue>(new ScalarValue(entry.Key), Convert(entry.Value, factory)))),
            _ => new SequenceValue(((MaskedSequence)node).Items.Select(item => Convert(item, factory))),
        };
}
