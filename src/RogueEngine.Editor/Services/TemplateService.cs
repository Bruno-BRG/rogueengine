using RogueEngine.Engine.Data;

namespace RogueEngine.Editor.Services;

public sealed class TemplateService
{
    public string CreateProject(string parentDirectory, string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        var targetDirectory = Path.Combine(parentDirectory, projectName);
        if (Directory.Exists(targetDirectory))
        {
            throw new InvalidOperationException($"Directory already exists: {targetDirectory}");
        }

        var templateRoot = RepositoryPaths.LocateTemplateProjectRoot();
        CopyDirectory(templateRoot, targetDirectory);

        var reprojPath = Path.Combine(targetDirectory, "game.reproj");
        var loaded = ProjectLoader.Load(reprojPath);
        var updated = new GameProject
        {
            Name = projectName,
            Version = loaded.Project.Version,
            DataPath = loaded.Project.DataPath,
            DefaultGenerator = loaded.Project.DefaultGenerator
        };
        ProjectDataWriter.WriteGameProject(reprojPath, updated);

        return reprojPath;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destination, fileName), overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            var directoryName = Path.GetFileName(directory);
            CopyDirectory(directory, Path.Combine(destination, directoryName));
        }
    }
}
