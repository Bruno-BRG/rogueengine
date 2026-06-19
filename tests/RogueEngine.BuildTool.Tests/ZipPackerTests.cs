using System.IO.Compression;
using RogueEngine.BuildTool;

namespace RogueEngine.BuildTool.Tests;

public class ZipPackerTests
{
    [Fact]
    public void CreateZip_IncludesCopiedFiles()
    {
        var sourceDirectory = Path.Combine(Path.GetTempPath(), $"rogueengine-zip-src-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(Path.GetTempPath(), $"rogueengine-zip-{Guid.NewGuid():N}.zip");

        try
        {
            Directory.CreateDirectory(sourceDirectory);
            File.WriteAllText(Path.Combine(sourceDirectory, "game.reproj"), "{}");
            File.WriteAllText(Path.Combine(sourceDirectory, "readme.txt"), "test");

            ZipPacker.CreateZip(sourceDirectory, zipPath);

            Assert.True(File.Exists(zipPath));

            using var archive = ZipFile.OpenRead(zipPath);
            Assert.Contains(archive.Entries, entry => entry.FullName == "game.reproj");
            Assert.Contains(archive.Entries, entry => entry.FullName == "readme.txt");
        }
        finally
        {
            if (Directory.Exists(sourceDirectory))
            {
                Directory.Delete(sourceDirectory, recursive: true);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
        }
    }
}
