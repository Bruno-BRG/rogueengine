using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.TurnBased;

public sealed class TurnManager
{
    private readonly List<ITurnActor> _actors = [];

    public IReadOnlyList<ITurnActor> Actors => _actors;

    public void Register(ITurnActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        _actors.Add(actor);
    }

    public void RunEnemyTurns(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (var actor in _actors)
        {
            actor.TakeTurn(world);
        }
    }
}
