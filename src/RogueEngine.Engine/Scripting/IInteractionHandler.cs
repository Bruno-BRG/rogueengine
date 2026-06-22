namespace RogueEngine.Engine.Scripting;

public interface IInteractionHandler
{
    bool TryInteract(IInteractionContext context);
}
