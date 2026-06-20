namespace RogueEngine.Engine.Data;

public static class SceneLoader
{
    public static SceneDefinition Load(string filePath) => ProjectJson.DeserializeFile<SceneDefinition>(filePath);

    public static void Save(SceneDefinition scene, string filePath) => ProjectJson.SerializeFile(filePath, scene);

    public static IReadOnlyList<SceneDefinition> LoadAllFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(directory, "*.scene.json", SearchOption.TopDirectoryOnly)
            .Select(Load)
            .OrderBy(scene => scene.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
