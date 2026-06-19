namespace RogueEngine.BuildTool;

public static class AssetCopier
{
    public static void CopyProjectAssets(string projectRoot, string outputDirectory, string dataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataPath);

        Directory.CreateDirectory(outputDirectory);

        var reprojPath = Directory.GetFiles(projectRoot, "*.reproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
        if (reprojPath is not null)
        {
            CopyFile(reprojPath, Path.Combine(outputDirectory, Path.GetFileName(reprojPath)));
        }

        var dataDirectory = Path.Combine(projectRoot, dataPath);
        if (Directory.Exists(dataDirectory))
        {
            CopyDirectory(dataDirectory, Path.Combine(outputDirectory, dataPath));
        }

        var scriptsDirectory = Path.Combine(projectRoot, "Scripts");
        if (Directory.Exists(scriptsDirectory))
        {
            CopyDirectory(scriptsDirectory, Path.Combine(outputDirectory, "Scripts"));
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var destinationFile = file.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal);
            CopyFile(file, destinationFile);
        }
    }

    private static void CopyFile(string sourceFile, string destinationFile)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationFile);
        if (!string.IsNullOrEmpty(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        File.Copy(sourceFile, destinationFile, overwrite: true);
    }
}
