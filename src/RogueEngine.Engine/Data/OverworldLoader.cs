namespace RogueEngine.Engine.Data;

public static class OverworldLoader
{
    public static OverworldDefinition Load(string filePath) =>
        ProjectJson.DeserializeFile<OverworldDefinition>(filePath);
}
