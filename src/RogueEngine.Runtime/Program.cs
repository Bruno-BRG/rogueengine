using RogueEngine.Engine.Data;
using RogueEngine.Runtime;
using SadConsole.Configuration;

var reprojPath = ProjectPathResolver.Resolve(args);
RuntimeBootstrap.Project = ProjectLoader.Load(reprojPath);
RuntimeBootstrap.Scripts = ScriptLoader.Load(RuntimeBootstrap.Project);
var settings = RuntimeBootstrap.Project.Settings;

Settings.WindowTitle = RuntimeBootstrap.Project.Project.Name;

new Builder()
    .SetWindowSizeInCells(settings.WindowWidth, settings.WindowHeight)
    .SetStartingScreen<GameScreen>()
    .IsStartingScreenFocused(true)
    .ConfigureFonts(true)
    .Run();
