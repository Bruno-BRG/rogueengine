using RogueEngine.Engine.VisualScripting;

namespace RogueEngine.Engine.Data;

public static class ProjectLoader
{
    public static LoadedProject Load(string reprojPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reprojPath);

        var fullReprojPath = Path.GetFullPath(reprojPath);
        if (!File.Exists(fullReprojPath))
        {
            throw new FileNotFoundException($"Project file not found: {fullReprojPath}");
        }

        var projectRoot = Path.GetDirectoryName(fullReprojPath)
            ?? throw new InvalidOperationException($"Could not resolve project root for {fullReprojPath}");

        var project = ProjectJson.DeserializeFile<GameProject>(fullReprojPath);
        var dataDirectory = Path.Combine(projectRoot, project.DataPath);
        if (!Directory.Exists(dataDirectory))
        {
            throw new DirectoryNotFoundException($"Data directory not found: {dataDirectory}");
        }

        var settingsPath = Path.Combine(dataDirectory, "settings.json");
        var settings = ProjectJson.DeserializeFile<GameSettings>(settingsPath);

        var actorsDirectory = Path.Combine(dataDirectory, "actors");
        if (!Directory.Exists(actorsDirectory))
        {
            throw new DirectoryNotFoundException($"Actors directory not found: {actorsDirectory}");
        }

        var actors = new Dictionary<string, ActorDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var actorFile in Directory.EnumerateFiles(actorsDirectory, "*.json"))
        {
            var actor = ProjectJson.DeserializeFile<ActorDefinition>(actorFile);
            if (string.IsNullOrWhiteSpace(actor.Id))
            {
                throw new InvalidDataException($"Actor file is missing id: {actorFile}");
            }

            if (!actors.TryAdd(actor.Id, actor))
            {
                throw new InvalidDataException($"Duplicate actor id '{actor.Id}' in project.");
            }
        }

        if (!actors.Values.Any(actor => actor.IsPlayer))
        {
            throw new InvalidDataException("Project must define at least one player actor.");
        }

        GeneratorDefinition? generator = null;
        if (!string.IsNullOrWhiteSpace(project.DefaultGenerator))
        {
            var generatorPath = Path.Combine(dataDirectory, project.DefaultGenerator);
            generator = GeneratorLoader.Load(generatorPath);
        }

        var visualScriptsDirectory = Path.Combine(projectRoot, "VisualScripts");
        var visualScripts = VisualGraphLoader.LoadAllFromDirectory(visualScriptsDirectory);

        return new LoadedProject
        {
            ProjectRoot = projectRoot,
            ReprojPath = fullReprojPath,
            Project = project,
            Settings = settings,
            Actors = actors,
            Generator = generator,
            VisualScripts = visualScripts
        };
    }
}
