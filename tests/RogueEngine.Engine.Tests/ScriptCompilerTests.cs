using RogueEngine.Engine.Scripting;

namespace RogueEngine.Engine.Tests;

public class ScriptCompilerTests
{
    [Fact]
    public void Compile_Succeeds_ForValidBehaviorScript()
    {
        var scriptPath = WriteTempScript(
            """
            using RogueEngine.Engine.Core;
            using RogueEngine.Engine.Scripting;

            public sealed class TestChaseBehavior : IBehavior
            {
                public void OnTurn(IScriptContext context)
                {
                    var player = context.FindPlayer();
                    if (player is null)
                    {
                        return;
                    }

                    context.MoveToward(player.Value);
                }
            }
            """);

        try
        {
            var result = ScriptCompiler.Compile([scriptPath]);

            Assert.True(result.Success);
            Assert.NotNull(result.Assembly);
            Assert.Empty(result.Errors);

            var scripts = new ScriptAssembly(result.Assembly!);
            var behavior = scripts.CreateBehavior("TestChaseBehavior");
            Assert.NotNull(behavior);
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    [Fact]
    public void Compile_ReturnsFriendlyErrors_ForInvalidScript()
    {
        var scriptPath = WriteTempScript(
            """
            public class BrokenBehavior
            {
                public void OnTurn()
                {
                    this is not valid c#
                }
            }
            """);

        try
        {
            var result = ScriptCompiler.Compile([scriptPath]);

            Assert.False(result.Success);
            Assert.Null(result.Assembly);
            Assert.NotEmpty(result.Errors);
            Assert.Contains(result.Errors, error => error.Contains("BrokenBehavior") || error.Contains(scriptPath));
        }
        finally
        {
            File.Delete(scriptPath);
        }
    }

    private static string WriteTempScript(string source)
    {
        var path = Path.Combine(Path.GetTempPath(), $"rogueengine-script-{Guid.NewGuid():N}.cs");
        File.WriteAllText(path, source);
        return path;
    }
}
