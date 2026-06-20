using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueEngine.Engine.Data;

internal static class ProjectJson
{
    internal static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    internal static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    internal static T DeserializeFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, ReadOptions)
            ?? throw new InvalidDataException($"Failed to deserialize {path}");
    }

    internal static void SerializeFile<T>(string path, T value)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(value, WriteOptions);
        File.WriteAllText(path, json);
    }
}
