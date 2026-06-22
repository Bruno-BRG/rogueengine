using System.Reflection;

namespace RogueEngine.Engine.Scripting;

public sealed class ScriptCompilationResult
{
    public Assembly? Assembly { get; init; }
    public bool Success => Errors.Count == 0;
    public IReadOnlyList<string> Errors { get; init; } = [];
}
