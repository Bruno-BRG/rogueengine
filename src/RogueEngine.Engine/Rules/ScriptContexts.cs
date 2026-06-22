using RogueEngine.Engine.Components;
using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Rules;

internal sealed class ItemEffectContext : IItemEffectContext
{
    public ItemEffectContext(
        World world,
        Entity entity,
        ItemDefinition item,
        IReadOnlyDictionary<string, ItemDefinition> items)
    {
        World = world;
        Entity = entity;
        Item = item;
        Items = items;
    }

    public World World { get; }
    public Entity Entity { get; }
    public ItemDefinition Item { get; }
    public IReadOnlyDictionary<string, ItemDefinition> Items { get; }

    public void Log(string message) => World.Log.Add(message);
}

internal sealed class InteractionContext : IInteractionContext
{
    public InteractionContext(
        World world,
        Entity entity,
        InteractionDefinition interaction,
        Position position,
        IReadOnlyDictionary<string, ItemDefinition> items)
    {
        World = world;
        Entity = entity;
        Interaction = interaction;
        Position = position;
        Items = items;
    }

    public World World { get; }
    public Entity Entity { get; }
    public InteractionDefinition Interaction { get; }
    public Position Position { get; }
    public IReadOnlyDictionary<string, ItemDefinition> Items { get; }

    public void Log(string message) => World.Log.Add(message);
}

internal sealed class QuestObjectiveContext : IQuestObjectiveContext
{
    public QuestObjectiveContext(
        World world,
        Entity player,
        QuestDefinition quest,
        QuestObjectiveDefinition objective,
        int currentProgress)
    {
        World = world;
        Player = player;
        Quest = quest;
        Objective = objective;
        CurrentProgress = currentProgress;
    }

    public World World { get; }
    public Entity Player { get; }
    public QuestDefinition Quest { get; }
    public QuestObjectiveDefinition Objective { get; }
    public int CurrentProgress { get; }

    public void Log(string message) => World.Log.Add(message);
}
