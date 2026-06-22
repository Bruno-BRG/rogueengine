using RogueEngine.Engine.Core;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Rules;

public sealed class GameRulesContext
{
    public GameRulesContext(LoadedProject project, ScriptAssembly? scripts)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        Scripts = scripts;
        ItemEffects = new ItemEffectRegistry(scripts);
        Interactions = new InteractionRegistry(scripts);
        Quests = new QuestService(project, scripts);
    }

    public LoadedProject Project { get; }
    public ScriptAssembly? Scripts { get; }
    public ItemEffectRegistry ItemEffects { get; }
    public InteractionRegistry Interactions { get; }
    public QuestService Quests { get; }

    public void HandleEvent(World world, GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(world);
        Quests.HandleEvent(world, this, gameEvent);
    }
}
