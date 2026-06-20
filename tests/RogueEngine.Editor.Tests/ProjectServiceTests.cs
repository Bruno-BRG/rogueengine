using RogueEngine.Editor.Models;
using RogueEngine.Editor.Services;
using RogueEngine.Engine.Data;

namespace RogueEngine.Editor.Tests;

public class ProjectServiceTests
{
    [Fact]
    public void Save_RoundTripsSettingsActorsAndGenerator()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "rogueengine-editor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var templateService = new TemplateService();
            var reprojPath = templateService.CreateProject(tempRoot, "SaveTest");
            var projectService = new ProjectService();
            var editorProject = projectService.Open(reprojPath);

            editorProject.Settings.MapWidth = 64;
            editorProject.Settings.MaxEnemies = 7;
            editorProject.Actors.First(actor => actor.Id == "goblin").MaxHp = 9;
            editorProject.Generator.Width = 64;
            editorProject.Generator.Height = 18;

            var errors = projectService.Save(editorProject);
            Assert.Empty(errors);

            var reloaded = projectService.Open(reprojPath);
            Assert.Equal(64, reloaded.Settings.MapWidth);
            Assert.Equal(7, reloaded.Settings.MaxEnemies);
            Assert.Equal(9, reloaded.Actors.First(actor => actor.Id == "goblin").MaxHp);
            Assert.Equal(64, reloaded.Generator.Width);
            Assert.Equal(18, reloaded.Generator.Height);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
