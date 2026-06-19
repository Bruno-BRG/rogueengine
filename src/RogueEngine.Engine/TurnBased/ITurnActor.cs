using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.TurnBased;

public interface ITurnActor
{
    void TakeTurn(World world);
}
