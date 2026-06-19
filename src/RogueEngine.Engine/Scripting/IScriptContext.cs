using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Scripting;

public interface IScriptContext
{
    Position Position { get; }
    int CurrentHp { get; }
    Position? FindPlayer();
    bool MoveToward(Position target);
    bool AttackAt(Position target);
    void Log(string message);
}
