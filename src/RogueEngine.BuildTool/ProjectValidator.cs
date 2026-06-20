using RogueEngine.Engine.Data;
using RogueEngine.Engine.Scripting;

namespace RogueEngine.BuildTool;

public sealed class ValidationResult
{
    public bool Success => Errors.Count == 0;
    public LoadedProject? Project { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

public static class ProjectValidator
{
    public static ValidationResult Validate(string reprojPath)
    {
        var errors = new List<string>();

        try
        {
            var project = ProjectLoader.Load(reprojPath);
            ValidateActors(project, errors);
            ValidateScripts(project, errors);

            if (errors.Count > 0)
            {
                return new ValidationResult { Errors = errors };
            }

            return new ValidationResult { Project = project, Errors = errors };
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return new ValidationResult { Errors = errors };
        }
    }

    private static void ValidateActors(LoadedProject project, List<string> errors)
    {
        foreach (var actor in project.Actors.Values)
        {
            if (string.IsNullOrWhiteSpace(actor.Id))
            {
                errors.Add("Actor definition is missing required field 'id'.");
            }

            if (actor.MaxHp <= 0)
            {
                errors.Add($"Actor '{actor.Id}' has invalid maxHp.");
            }

            if (!string.IsNullOrWhiteSpace(actor.Behavior))
            {
                var hasVisualScript = project.VisualScripts.Any(graph =>
                    graph.Id.Equals(actor.Behavior, StringComparison.OrdinalIgnoreCase));
                var hasScriptsDirectory = Directory.Exists(project.ScriptsDirectory);

                if (!hasVisualScript && !hasScriptsDirectory)
                {
                    errors.Add(
                        $"Actor '{actor.Id}' references behavior '{actor.Behavior}', but no Scripts/ or matching visual script was found.");
                }
            }
        }
    }

    private static void ValidateScripts(LoadedProject project, List<string> errors)
    {
        var compileResult = ScriptBuildHelper.CompileProjectScripts(project, errors);
        if (errors.Count > 0)
        {
            return;
        }

        if (!compileResult.Success)
        {
            errors.AddRange(compileResult.Errors);
        }
    }
}
