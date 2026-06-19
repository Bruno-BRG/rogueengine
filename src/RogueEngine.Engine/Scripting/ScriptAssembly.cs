using System.Reflection;

namespace RogueEngine.Engine.Scripting;

public sealed class ScriptAssembly
{
    private readonly Assembly _assembly;

    public ScriptAssembly(Assembly assembly)
    {
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
    }

    public IBehavior? CreateBehavior(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);

        var behaviorType = _assembly
            .GetTypes()
            .FirstOrDefault(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase) &&
                typeof(IBehavior).IsAssignableFrom(type));

        if (behaviorType is null)
        {
            return null;
        }

        return (IBehavior?)Activator.CreateInstance(behaviorType);
    }
}
