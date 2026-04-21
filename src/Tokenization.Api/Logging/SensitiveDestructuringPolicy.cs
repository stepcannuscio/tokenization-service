using Serilog.Core;
using Serilog.Events;
using System.Reflection;

namespace Tokenization.Api.Logging;

internal sealed class SensitiveDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object? value, ILogEventPropertyValueFactory factory, out LogEventPropertyValue result)
    {
        result = null!;
        
        if (value == null)
        {
            return false;
        }
        
        var type = value.GetType();

        // Fast-path scalars
        if (type.IsPrimitive || value is string or Guid or DateTimeOffset or decimal)
            return false; // let Serilog handle

        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var structure = new List<LogEventProperty>();
        foreach (var property in props)
        {
            if (!property.CanRead) continue;
            var raw = property.GetValue(value);
            var sensitiveAttribute = property.GetCustomAttribute<SensitiveAttribute>();
            var propertyValue = sensitiveAttribute is null
                ? factory.CreatePropertyValue(raw, destructureObjects: true)
                : Mask(raw, sensitiveAttribute.Kind);

            structure.Add(new LogEventProperty(property.Name, propertyValue));
        }

        result = new StructureValue(structure, type.Name);
        return true;
    }

    private static ScalarValue Mask(object? v, Sensitivity kind) =>
        new (kind switch
        {
            Sensitivity.Payment => MaskPayment(v as string),
            Sensitivity.Credential => "********",
            _ => "***redacted***"
        });

    private static string MaskPayment(string? s) =>
        !string.IsNullOrEmpty(s) && s.Length >= 4 ? $"**** **** **** {s[^4..]}" : "***redacted***";
}