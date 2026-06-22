using System.Reflection;
using RogueEngine.Engine.Rules;

namespace RogueEngine.Engine.Scripting;

public sealed class ScriptAssembly
{
    private readonly Assembly _assembly;

    public ScriptAssembly(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public IBehavior? CreateBehavior(string typeName) => Create<IBehavior>(typeName);

    public IItemEffect? CreateItemEffect(string typeName) => Create<IItemEffect>(typeName);

    public IInteractionHandler? CreateInteractionHandler(string typeName) => Create<IInteractionHandler>(typeName);

    public IQuestObjectiveChecker? CreateQuestObjectiveChecker(string typeName) => Create<IQuestObjectiveChecker>(typeName);

    private T? Create<T>(string typeName) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        var implementationType = _assembly
            .GetTypes()
            .FirstOrDefault(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                typeof(T).IsAssignableFrom(type));

        if (implementationType is null)
        {
            return null;
        }

        return (T?)Activator.CreateInstance(implementationType);
    }
}
