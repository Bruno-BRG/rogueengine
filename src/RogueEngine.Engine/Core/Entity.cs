namespace RogueEngine.Engine.Core;

public sealed class Entity
{
    private readonly Dictionary<Type, IComponent> _components = new();

    public Guid Id { get; } = Guid.NewGuid();

    public void AddComponent<T>(T component) where T : class, IComponent
    {
        ArgumentNullException.ThrowIfNull(component);
        _components[typeof(T)] = component;
    }

    public bool HasComponent<T>() where T : class, IComponent =>
        _components.ContainsKey(typeof(T));

    public T GetComponent<T>() where T : class, IComponent
    {
        if (_components.TryGetValue(typeof(T), out var component))
        {
            return (T)component;
        }

        throw new InvalidOperationException($"Entity {Id} does not have component {typeof(T).Name}.");
    }

    public bool TryGetComponent<T>(out T? component) where T : class, IComponent
    {
        if (_components.TryGetValue(typeof(T), out var value))
        {
            component = (T)value;
            return true;
        }

        component = null;
        return false;
    }
}
