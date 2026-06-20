namespace RogueEngine.Engine.Data;

public static class GeneratorLoader
{
    public static GeneratorDefinition Load(string path) =>
        ProjectJson.DeserializeFile<GeneratorDefinition>(path);

    public static void Save(string path, GeneratorDefinition generator) =>
        ProjectJson.SerializeFile(path, generator);
}
