using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace RogueEngine.Engine.Scripting;

public static class ScriptCompiler
{
    private const string ScriptPreamble =
        """
        using System;
        using System.Linq;
        using RogueEngine.Engine.Core;
        using RogueEngine.Engine.Scripting;

        """;

    public static ScriptCompilationResult Compile(IReadOnlyList<string> scriptFiles)
    {
        ArgumentNullException.ThrowIfNull(scriptFiles);

        if (scriptFiles.Count == 0)
        {
            return new ScriptCompilationResult
            {
                Errors = ["No script files were provided."]
            };
        }

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var scriptFile in scriptFiles)
        {
            if (!File.Exists(scriptFile))
            {
                return new ScriptCompilationResult
                {
                    Errors = [$"Script file not found: {scriptFile}"]
                };
            }

            var source = ScriptPreamble + File.ReadAllText(scriptFile);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(
                SourceText.From(source, Encoding.UTF8),
                path: scriptFile));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: $"RogueEngineScripts_{Guid.NewGuid():N}",
            syntaxTrees: syntaxTrees,
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var stream = new MemoryStream();
        var emitResult = compilation.Emit(stream);
        if (!emitResult.Success)
        {
            var errors = emitResult.Diagnostics
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                .Select(FormatDiagnostic)
                .ToList();

            return new ScriptCompilationResult { Errors = errors };
        }

        stream.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(stream.ToArray());
        return new ScriptCompilationResult { Assembly = assembly };
    }

    private static string FormatDiagnostic(Diagnostic diagnostic)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        var file = string.IsNullOrWhiteSpace(lineSpan.Path)
            ? "<script>"
            : lineSpan.Path;

        var line = lineSpan.StartLinePosition.Line + 1;
        var column = lineSpan.StartLinePosition.Character + 1;
        return $"{file}({line},{column}): {diagnostic.Id}: {diagnostic.GetMessage()}";
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var references = new List<MetadataReference>();

        void AddAssembly(Assembly assembly)
        {
            if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.Location))
            {
                return;
            }

            if (!paths.Add(assembly.Location))
            {
                return;
            }

            references.Add(MetadataReference.CreateFromFile(assembly.Location));

            foreach (var referenceName in assembly.GetReferencedAssemblies())
            {
                try
                {
                    AddAssembly(Assembly.Load(referenceName));
                }
                catch (FileNotFoundException)
                {
                }
            }
        }

        AddAssembly(typeof(IBehavior).Assembly);
        AddAssembly(typeof(object).Assembly);
        AddAssembly(typeof(Math).Assembly);
        AddAssembly(typeof(Enumerable).Assembly);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            AddAssembly(assembly);
        }

        return references;
    }
}
