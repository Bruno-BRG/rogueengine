namespace RogueEngine.Engine.Data;

public static class ClassLoader
{
    public static ClassDefinition Load(string filePath) =>
        ProjectJson.DeserializeFile<ClassDefinition>(filePath);

    public static IReadOnlyDictionary<string, ClassDefinition> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new Dictionary<string, ClassDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        var classes = new Dictionary<string, ClassDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var classDef = Load(file);
            if (string.IsNullOrWhiteSpace(classDef.Id))
            {
                throw new InvalidDataException($"Class file is missing id: {file}");
            }

            if (!classes.TryAdd(classDef.Id, classDef))
            {
                throw new InvalidDataException($"Duplicate class id '{classDef.Id}' in project.");
            }
        }

        return classes;
    }
}
