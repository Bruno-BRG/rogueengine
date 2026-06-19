using RogueEngine.Engine.Core;

namespace RogueEngine.Engine.Commands;

public interface ICommand
{
    bool Execute(World world);
}
