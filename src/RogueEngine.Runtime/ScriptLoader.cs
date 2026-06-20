using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Runtime;

internal static class ScriptLoader
{
    public static ScriptAssembly? Load(LoadedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var hasManualScripts = Directory.Exists(project.ScriptsDirectory) &&
            Directory.GetFiles(project.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly).Length > 0;
        var hasVisualScripts = project.VisualScripts.Count > 0;

        if (!hasManualScripts && !hasVisualScripts)
        {
            return null;
        }

        using var bundle = VisualScriptPipeline.CollectScriptFiles(project);
        if (bundle.ScriptFiles.Count == 0)
        {
            return null;
        }

        var result = ScriptCompiler.Compile(bundle.ScriptFiles);
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
