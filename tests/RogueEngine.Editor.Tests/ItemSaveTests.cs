using RogueEngine.Editor.Models;
using RogueEngine.Editor.Services;
using RogueEngine.Engine.Data;

namespace RogueEngine.Editor.Tests;

public class ItemSaveTests
{
    [Fact]
    public void Save_RoundTripsItems()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "rogueengine-editor-items", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var templateService = new TemplateService();
            var reprojPath = templateService.CreateProject(tempRoot, "ItemSaveTest");
            var projectService = new ProjectService();
            var editorProject = projectService.Open(reprojPath);

            editorProject.Items.Add(new EditorItem
            {
                Id = "health_potion",
                SourceFileName = "health_potion.json",
                Name = "Health Potion",
                Glyph = '!',
                ColorR = 200,
                ColorG = 50,
                ColorB = 50,
                Kind = "consumable",
                MaxStack = 5,
                HealOnUse = 5
            });

            var errors = projectService.Save(editorProject);
            Assert.Empty(errors);

            var reloaded = projectService.Open(reprojPath);
            var item = Assert.Single(reloaded.Items);
            Assert.Equal("health_potion", item.Id);
            Assert.Equal(5, item.HealOnUse);
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
