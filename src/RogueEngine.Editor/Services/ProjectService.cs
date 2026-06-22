using RogueEngine.BuildTool;
using RogueEngine.Editor.Models;
using RogueEngine.Engine.Data;
using RogueEngine.Engine.VisualScripting;
using RogueEngine.Toolkit.ProcGen;

namespace RogueEngine.Editor.Services;

public sealed class ProjectService
{
    private readonly VisualScriptService _visualScriptService = new();
    public EditorProject Open(string reprojPath)
    {
        var loaded = ProjectLoader.Load(reprojPath);
        var editor = EditorProject.FromLoaded(loaded);
        editor.VisualGraphs = _visualScriptService.LoadAll(editor.ProjectRoot).ToList();
        return editor;
    }

    public IReadOnlyList<string> Save(EditorProject project)
    {
        var errors = ValidateEditorProject(project);
        if (errors.Count > 0)
        {
            return errors;
        }

        ProjectDataWriter.WriteGameProject(project.ReprojPath, project.ToGameProject());
        ProjectDataWriter.WriteSettings(
            Path.Combine(project.DataDirectory, "settings.json"),
            project.ToGameSettings());

        Directory.CreateDirectory(project.ActorsDirectory);
        var actorFiles = Directory.Exists(project.ActorsDirectory)
            ? Directory.GetFiles(project.ActorsDirectory, "*.json").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        foreach (var actor in project.Actors)
        {
            var fileName = string.IsNullOrWhiteSpace(actor.SourceFileName)
                ? $"{actor.Id}.json"
                : actor.SourceFileName;
            var actorPath = Path.Combine(project.ActorsDirectory, fileName);
            ProjectDataWriter.WriteActor(actorPath, actor.ToEngine());
            actor.SourceFileName = fileName;
            actorFiles.Remove(actorPath);
        }

        foreach (var staleFile in actorFiles)
        {
            File.Delete(staleFile);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(project.GeneratorFilePath)!);
        ProjectDataWriter.WriteGenerator(project.GeneratorFilePath, project.ToGeneratorDefinition());

        Directory.CreateDirectory(project.VisualScriptsDirectory);
        var graphFiles = Directory.Exists(project.VisualScriptsDirectory)
            ? Directory.GetFiles(project.VisualScriptsDirectory, "*.graph.json").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        foreach (var graph in project.VisualGraphs)
        {
            _visualScriptService.Save(graph, project.ProjectRoot);
            graphFiles.Remove(Path.Combine(project.VisualScriptsDirectory, graph.SourceFileName));
        }

        foreach (var staleFile in graphFiles)
        {
            File.Delete(staleFile);
        }

        Directory.CreateDirectory(project.ScenesDirectory);
        var sceneFiles = Directory.Exists(project.ScenesDirectory)
            ? Directory.GetFiles(project.ScenesDirectory, "*.scene.json").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        foreach (var scene in project.Scenes)
        {
            var fileName = string.IsNullOrWhiteSpace(scene.SourceFileName)
                ? $"{scene.Id}.scene.json"
                : scene.SourceFileName;
            var scenePath = Path.Combine(project.ScenesDirectory, fileName);
            SceneLoader.Save(scene.ToEngine(), scenePath);
            scene.SourceFileName = fileName;
            sceneFiles.Remove(scenePath);
        }

        foreach (var staleFile in sceneFiles)
        {
            File.Delete(staleFile);
        }

        Directory.CreateDirectory(project.ItemsDirectory);
        var itemFiles = Directory.Exists(project.ItemsDirectory)
            ? Directory.GetFiles(project.ItemsDirectory, "*.json").ToHashSet(StringComparer.OrdinalIgnoreCase)
            : [];

        foreach (var item in project.Items)
        {
            var fileName = string.IsNullOrWhiteSpace(item.SourceFileName)
                ? $"{item.Id}.json"
                : item.SourceFileName;
            var itemPath = Path.Combine(project.ItemsDirectory, fileName);
            ProjectDataWriter.WriteItem(itemPath, item.ToEngine());
            item.SourceFileName = fileName;
            itemFiles.Remove(itemPath);
        }

        foreach (var staleFile in itemFiles)
        {
            File.Delete(staleFile);
        }

        if (project.Overworld is not null)
        {
            Directory.CreateDirectory(project.OverworldDirectory);
            var overworldFileName = string.IsNullOrWhiteSpace(project.Overworld.SourceFileName)
                ? "world.json"
                : project.Overworld.SourceFileName;
            var overworldPath = Path.Combine(project.OverworldDirectory, overworldFileName);
            ProjectDataWriter.WriteOverworld(overworldPath, project.Overworld.ToEngine());
            project.Overworld.SourceFileName = overworldFileName;
            project.DefaultOverworldPath = $"overworld/{overworldFileName}";
        }

        project.IsDirty = false;
        return [];
    }

    public ValidationResult Validate(string reprojPath) => ProjectValidator.Validate(reprojPath);

    public IReadOnlyList<string> ValidateEditorProject(EditorProject project)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(project.Name))
        {
            errors.Add("Project name is required.");
        }

        if (project.Settings.MapWidth <= 0 || project.Settings.MapHeight <= 0)
        {
            errors.Add("Map width and height must be greater than zero.");
        }

        if (project.Settings.MinEnemies > project.Settings.MaxEnemies)
        {
            errors.Add("Min enemies cannot be greater than max enemies.");
        }

        if (!project.Actors.Any(actor => actor.IsPlayer))
        {
            errors.Add("At least one actor must be marked as player.");
        }

        foreach (var actor in project.Actors)
        {
            if (string.IsNullOrWhiteSpace(actor.Id))
            {
                errors.Add("All actors must have an id.");
            }

            if (actor.MaxHp <= 0)
            {
                errors.Add($"Actor '{actor.Id}' must have maxHp greater than zero.");
            }
        }

        if (project.Generator.Width <= 0 || project.Generator.Height <= 0)
        {
            errors.Add("Generator width and height must be greater than zero.");
        }

        if (!GeneratorRegistry.TryGet(project.Generator.Algorithm, out _))
        {
            errors.Add($"Generator algorithm '{project.Generator.Algorithm}' is not supported.");
        }

        return errors;
    }
}
