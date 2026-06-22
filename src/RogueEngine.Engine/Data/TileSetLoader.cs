namespace RogueEngine.Engine.Data;

public static class TileSetLoader
{
    public static TileSetDefinition Load(string filePath) =>
        ProjectJson.DeserializeFile<TileSetDefinition>(filePath);
}
