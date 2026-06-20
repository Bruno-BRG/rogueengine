using RogueEngine.Editor.Services;

namespace RogueEngine.Editor.Tests;

public class TemplateServiceTests
{
    [Fact]
    public void CreateProject_CopiesTemplateAndRenamesProject()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "rogueengine-editor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var service = new TemplateService();
            var reprojPath = service.CreateProject(tempRoot, "TestGame");

            Assert.True(File.Exists(reprojPath));
            var projectService = new ProjectService();
            var editorProject = projectService.Open(reprojPath);
            Assert.Equal("TestGame", editorProject.Name);
            Assert.NotEmpty(editorProject.Scenes);
            Assert.Equal("main", editorProject.Scenes[0].Id);
            Assert.NotEmpty(editorProject.ScriptFiles);
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
