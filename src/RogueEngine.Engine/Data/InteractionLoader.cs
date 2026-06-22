namespace RogueEngine.Engine.Data;

public static class InteractionLoader
{
    public static InteractionDefinition Load(string filePath) =>
        ProjectJson.DeserializeFile<InteractionDefinition>(filePath);

    public static IReadOnlyDictionary<string, InteractionDefinition> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new Dictionary<string, InteractionDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        var interactions = new Dictionary<string, InteractionDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var interaction = Load(file);
            if (string.IsNullOrWhiteSpace(interaction.Id))
            {
                throw new InvalidDataException($"Interaction file is missing id: {file}");
            }

            if (!interactions.TryAdd(interaction.Id, interaction))
            {
                throw new InvalidDataException($"Duplicate interaction id '{interaction.Id}' in project.");
            }
        }

        return interactions;
    }
}
