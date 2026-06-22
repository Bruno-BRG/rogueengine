using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Navigation;

public interface IGridNavigator
{
    bool TryGetNextStep(TileMap map, Position from, Position to, out Position next);
}
