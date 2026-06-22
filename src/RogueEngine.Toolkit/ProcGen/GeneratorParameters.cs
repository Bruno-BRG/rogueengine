using System.Text.Json;

namespace RogueEngine.Toolkit.ProcGen;

internal static class GeneratorParameters
{
    public static int GetInt(IReadOnlyDictionary<string, object> parameters, string key, int defaultValue)
    {
        if (!parameters.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            float f => (int)f,
            JsonElement element when element.ValueKind == JsonValueKind.Number => element.GetInt32(),
            string text when int.TryParse(text, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    public static double GetDouble(IReadOnlyDictionary<string, object> parameters, string key, double defaultValue)
    {
        if (!parameters.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            JsonElement element when element.ValueKind == JsonValueKind.Number => element.GetDouble(),
            string text when double.TryParse(text, out var parsed) => parsed,
            _ => defaultValue
        };
    }
}
