using System.Reflection;

namespace RogueEngine.Engine.Scripting;

public sealed class ScriptCompilationResult
{
    public Assembly? Assembly { get; init; }
    public bool Success => Assembly is not null;
    public IReadOnlyList<string> Errors { get; init; } = [];
}
