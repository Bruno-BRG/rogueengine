namespace RogueEngine.Engine.Data;

public static class ItemLoader
{
    public static ItemDefinition Load(string filePath) =>
        ProjectJson.DeserializeFile<ItemDefinition>(filePath);

    public static IReadOnlyDictionary<string, ItemDefinition> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        var items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var item = Load(file);
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new InvalidDataException($"Item file is missing id: {file}");
            }

            if (!items.TryAdd(item.Id, item))
            {
                throw new InvalidDataException($"Duplicate item id '{item.Id}' in project.");
            }
        }

        return items;
    }
}
