using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Runtime;

internal static class ScriptLoader
{
    public static ScriptAssembly? Load(LoadedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!Directory.Exists(project.ScriptsDirectory))
        {
            return null;
        }

        var scriptFiles = Directory.GetFiles(project.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly);
        if (scriptFiles.Length == 0)
        {
            return null;
        }

        var result = ScriptCompiler.Compile(scriptFiles);
        if (!result.Success)
        {
            System.Console.Error.WriteLine("Script compilation failed:");
            foreach (var error in result.Errors)
            {
                System.Console.Error.WriteLine(error);
            }

            Environment.Exit(1);
        }

        return new ScriptAssembly(result.Assembly!);
    }
}
