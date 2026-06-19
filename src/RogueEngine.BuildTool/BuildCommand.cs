using System.Diagnostics;
using RogueEngine.Engine.Data;

namespace RogueEngine.BuildTool;

public static class BuildCommand
{
    public static BuildResult Execute(string reprojPath, string? outputDirectory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reprojPath);

        BuildLogger.Info($"Loading project: {reprojPath}");

        var validation = ProjectValidator.Validate(reprojPath);
        if (!validation.Success || validation.Project is null)
        {
            foreach (var error in validation.Errors)
            {
                BuildLogger.Error(error);
            }

            return new BuildResult { Errors = validation.Errors };
        }

        var project = validation.Project;
        var actorCount = project.Actors.Count;
        var scriptCount = Directory.Exists(project.ScriptsDirectory)
            ? Directory.GetFiles(project.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly).Length
            : 0;

        BuildLogger.Success($"Validation passed ({actorCount} actors, {scriptCount} script(s))");

        var buildDirectory = outputDirectory ?? Path.Combine(project.ProjectRoot, "Build");
        if (Directory.Exists(buildDirectory))
        {
            Directory.Delete(buildDirectory, recursive: true);
        }

        Directory.CreateDirectory(buildDirectory);

        try
        {
            PublishRuntime(buildDirectory, project.Project.Name);
            BuildLogger.Success("Runtime published");

            AssetCopier.CopyProjectAssets(project.ProjectRoot, buildDirectory, project.Project.DataPath);
            BuildLogger.Success("Assets copied");

            var zipPath = Path.Combine(project.ProjectRoot, $"{SanitizeFileName(project.Project.Name)}.zip");
            ZipPacker.CreateZip(buildDirectory, zipPath);
            BuildLogger.Success($"ZIP created: {zipPath}");

            BuildLogger.Success($"Build complete -> {buildDirectory}");
            return new BuildResult
            {
                Success = true,
                OutputDirectory = buildDirectory,
                ZipPath = zipPath
            };
        }
        catch (Exception ex)
        {
            BuildLogger.Error(ex.Message);
            return new BuildResult { Errors = [ex.Message] };
        }
    }

    private static void PublishRuntime(string outputDirectory, string projectName)
    {
        var runtimeCsproj = RuntimeProjectLocator.LocateRuntimeCsproj();
        BuildLogger.Info("Publishing runtime...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"publish \"{runtimeCsproj}\" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o \"{outputDirectory}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start dotnet publish.");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException($"dotnet publish failed with exit code {process.ExitCode}.{Environment.NewLine}{details}");
        }

        RenameRuntimeExecutable(outputDirectory, projectName);
    }

    private static void RenameRuntimeExecutable(string outputDirectory, string projectName)
    {
        var runtimeExe = Path.Combine(outputDirectory, "RogueEngine.Runtime.exe");
        if (!File.Exists(runtimeExe))
        {
            return;
        }

        var targetExe = Path.Combine(outputDirectory, $"{SanitizeFileName(projectName)}.exe");
        if (File.Exists(targetExe))
        {
            File.Delete(targetExe);
        }

        File.Move(runtimeExe, targetExe);
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "Game" : name;
    }
}
