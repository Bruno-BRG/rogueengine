using RogueEngine.BuildTool;

namespace RogueEngine.Editor.Services;

public sealed class BuildService
{
    public BuildResult Build(string reprojPath, IBuildLogSink? logSink = null) =>
        BuildCommand.Execute(reprojPath, logSink: logSink);
}
