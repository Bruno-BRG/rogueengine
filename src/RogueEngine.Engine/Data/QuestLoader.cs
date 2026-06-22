namespace RogueEngine.Engine.Data;

public static class QuestLoader
{
    public static QuestDefinition Load(string filePath) =>
        ProjectJson.DeserializeFile<QuestDefinition>(filePath);

    public static IReadOnlyDictionary<string, QuestDefinition> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        var quests = new Dictionary<string, QuestDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(directory, "*.json"))
        {
            var quest = Load(file);
            if (string.IsNullOrWhiteSpace(quest.Id))
            {
                throw new InvalidDataException($"Quest file is missing id: {file}");
            }

            if (!quests.TryAdd(quest.Id, quest))
            {
                throw new InvalidDataException($"Duplicate quest id '{quest.Id}' in project.");
            }
        }

        return quests;
    }
}
