using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Runtime;

internal static class RuntimeBootstrap
{
    public static LoadedProject Project { get; set; } = null!;
    public static ScriptAssembly? Scripts { get; set; }
}
