namespace RogueEngine.Engine.Data;

public static class ProjectDataWriter
{
    public static void WriteGameProject(string path, GameProject project) =>
        ProjectJson.SerializeFile(path, project);

    public static void WriteSettings(string path, GameSettings settings) =>
        ProjectJson.SerializeFile(path, settings);

    public static void WriteActor(string path, ActorDefinition actor) =>
        ProjectJson.SerializeFile(path, actor);

    public static void WriteGenerator(string path, GeneratorDefinition generator) =>
        GeneratorLoader.Save(path, generator);

    public static void WriteItem(string path, ItemDefinition item) =>
        ProjectJson.SerializeFile(path, item);

    public static void WriteInteraction(string path, InteractionDefinition interaction) =>
        ProjectJson.SerializeFile(path, interaction);

    public static void WriteClass(string path, ClassDefinition classDef) =>
        ProjectJson.SerializeFile(path, classDef);

    public static void WriteQuest(string path, QuestDefinition quest) =>
        ProjectJson.SerializeFile(path, quest);

    public static void WriteOverworld(string path, OverworldDefinition overworld) =>
        ProjectJson.SerializeFile(path, overworld);
}
