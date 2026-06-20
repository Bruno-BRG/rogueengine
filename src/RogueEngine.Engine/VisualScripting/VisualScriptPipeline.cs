using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.VisualScripting;

public sealed class VisualScriptCompilationBundle : IDisposable
{
    private readonly List<string> _tempFiles;

    public VisualScriptCompilationBundle(IReadOnlyList<string> scriptFiles, List<string> tempFiles)
    {
        ScriptFiles = scriptFiles;
        _tempFiles = tempFiles;
    }

    public IReadOnlyList<string> ScriptFiles { get; }

    public void Dispose()
    {
        foreach (var tempFile in _tempFiles)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}

public static class VisualScriptPipeline
{
    public static VisualScriptCompilationBundle CollectScriptFiles(LoadedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var scriptFiles = new List<string>();
        var tempFiles = new List<string>();

        if (Directory.Exists(project.ScriptsDirectory))
        {
            scriptFiles.AddRange(Directory.GetFiles(project.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly));
        }

        foreach (var graph in project.VisualScripts)
        {
            var generation = GraphCodeGenerator.Generate(graph);
            if (!generation.Success)
            {
                throw new InvalidOperationException(
                    $"Failed to generate visual script '{graph.Id}': {string.Join("; ", generation.Errors)}");
            }

            var tempFile = Path.Combine(Path.GetTempPath(), $"rogueengine-{graph.Id}-{Guid.NewGuid():N}.generated.cs");
            File.WriteAllText(tempFile, generation.Source);
            tempFiles.Add(tempFile);
            scriptFiles.Add(tempFile);
        }

        return new VisualScriptCompilationBundle(scriptFiles, tempFiles);
    }

    public static ScriptCompilationResult CompileProjectScripts(LoadedProject project)
    {
        using var bundle = CollectScriptFiles(project);
        if (bundle.ScriptFiles.Count == 0)
        {
            return new ScriptCompilationResult();
        }

        return ScriptCompiler.Compile(bundle.ScriptFiles);
    }
}
