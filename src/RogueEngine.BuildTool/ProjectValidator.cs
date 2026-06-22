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
            ValidateGameRules(project, errors);
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

    private static void ValidateGameRules(LoadedProject project, List<string> errors)
    {
        foreach (var quest in project.Quests.Values)
        {
            foreach (var objective in quest.Objectives)
            {
                if (string.Equals(objective.Type, "kill", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(objective.ActorId) &&
                    !project.Actors.ContainsKey(objective.ActorId))
                {
                    errors.Add($"Quest '{quest.Id}' references unknown actor '{objective.ActorId}'.");
                }

                if (string.Equals(objective.Type, "collect", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(objective.ItemId) &&
                    !project.Items.ContainsKey(objective.ItemId))
                {
                    errors.Add($"Quest '{quest.Id}' references unknown item '{objective.ItemId}'.");
                }
            }

            foreach (var reward in quest.Rewards)
            {
                if (!project.Items.ContainsKey(reward.ItemId))
                {
                    errors.Add($"Quest '{quest.Id}' reward references unknown item '{reward.ItemId}'.");
                }
            }
        }

        foreach (var scene in SceneLoader.LoadAllFromDirectory(project.ScenesDirectory))
        {
            foreach (var placement in scene.Interactions)
            {
                if (!project.Interactions.ContainsKey(placement.InteractionId))
                {
                    errors.Add($"Scene '{scene.Id}' references unknown interaction '{placement.InteractionId}'.");
                }
            }
        }

        foreach (var classDef in project.Classes.Values)
        {
            foreach (var startItem in classDef.StartItems)
            {
                if (!project.Items.ContainsKey(startItem.ItemId))
                {
                    errors.Add($"Class '{classDef.Id}' start item references unknown item '{startItem.ItemId}'.");
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
