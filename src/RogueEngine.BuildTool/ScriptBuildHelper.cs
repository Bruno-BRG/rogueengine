using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;
using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.BuildTool;

public static class ScriptBuildHelper
{
    public static ScriptCompilationResult CompileProjectScripts(LoadedProject project, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var graph in project.VisualScripts)
        {
            var generation = GraphCodeGenerator.Generate(graph);
            if (!generation.Success)
            {
                foreach (var error in generation.Errors)
                {
                    errors.Add($"Visual script '{graph.Id}': {error}");
                }
            }
        }

        if (errors.Count > 0)
        {
            return new ScriptCompilationResult { Errors = errors };
        }

        var hasManualScripts = Directory.Exists(project.ScriptsDirectory) &&
            Directory.GetFiles(project.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly).Length > 0;
        var hasVisualScripts = project.VisualScripts.Count > 0;

        if (!hasManualScripts && !hasVisualScripts)
        {
            return new ScriptCompilationResult();
        }

        return VisualScriptPipeline.CompileProjectScripts(project);
    }
}
