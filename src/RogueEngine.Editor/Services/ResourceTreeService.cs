using RogueEngine.Editor.Models;

namespace RogueEngine.Editor.Services;

public sealed class ResourceTreeService
{
    public IReadOnlyList<EditorResourceNode> BuildTree(EditorProject project)
    {
        var root = new EditorResourceNode
        {
            Id = "root",
            Title = project.Name,
            Icon = "📁",
            Kind = EditorResourceKind.Folder,
            IsSelectable = false
        };

        root.Children.Add(new EditorResourceNode
        {
            Id = "settings",
            Title = "Project Settings",
            Icon = "⚙",
            Kind = EditorResourceKind.ProjectSettings
        });

        var scenesFolder = CreateFolder("scenes", "Scenes", "🎬");
        foreach (var scene in project.Scenes.OrderBy(scene => scene.Name, StringComparer.OrdinalIgnoreCase))
        {
            scenesFolder.Children.Add(new EditorResourceNode
            {
                Id = $"scene:{scene.Id}",
                Title = scene.Name,
                Icon = "🗺",
                Kind = EditorResourceKind.Scene,
                Payload = scene.Id
            });
        }

        scenesFolder.Children.Add(new EditorResourceNode
        {
            Id = "add-scene",
            Title = "Add Scene…",
            Icon = "➕",
            Kind = EditorResourceKind.AddNew,
            IsPlaceholder = true
        });
        root.Children.Add(scenesFolder);

        var actorsFolder = CreateFolder("actors", "Actors", "👾");
        foreach (var actor in project.Actors.OrderBy(actor => actor.Id, StringComparer.OrdinalIgnoreCase))
        {
            actorsFolder.Children.Add(new EditorResourceNode
            {
                Id = $"actor:{actor.Id}",
                Title = actor.Id,
                Icon = actor.Glyph.ToString(),
                Kind = EditorResourceKind.Actor,
                Payload = actor.Id
            });
        }

        actorsFolder.Children.Add(new EditorResourceNode
        {
            Id = "add-actor",
            Title = "Add Actor…",
            Icon = "➕",
            Kind = EditorResourceKind.AddNew,
            IsPlaceholder = true
        });
        root.Children.Add(actorsFolder);

        root.Children.Add(CreatePlaceholderFolder("items", "Items", "📦", "Phase 8"));
        root.Children.Add(CreatePlaceholderFolder("sprites", "Sprites", "🎨", "Phase E3"));
        root.Children.Add(CreatePlaceholderFolder("tilesets", "Tilesets", "🧱", "Phase 9"));

        var scriptsFolder = CreateFolder("scripts", "Scripts", "📜");
        foreach (var script in project.ScriptFiles.OrderBy(script => script.FileName, StringComparer.OrdinalIgnoreCase))
        {
            scriptsFolder.Children.Add(new EditorResourceNode
            {
                Id = $"script:{script.FileName}",
                Title = script.FileName,
                Icon = "C#",
                Kind = EditorResourceKind.CodeScript,
                Payload = script.FullPath
            });
        }

        root.Children.Add(scriptsFolder);

        var visualFolder = CreateFolder("visual", "Visual Scripts", "🧠");
        foreach (var graph in project.VisualGraphs.OrderBy(graph => graph.Id, StringComparer.OrdinalIgnoreCase))
        {
            visualFolder.Children.Add(new EditorResourceNode
            {
                Id = $"visual:{graph.Id}",
                Title = graph.Id,
                Icon = "◇",
                Kind = EditorResourceKind.VisualScript,
                Payload = graph.Id
            });
        }

        visualFolder.Children.Add(new EditorResourceNode
        {
            Id = "add-visual",
            Title = "Add Visual Script…",
            Icon = "➕",
            Kind = EditorResourceKind.AddNew,
            IsPlaceholder = true
        });
        root.Children.Add(visualFolder);

        root.Children.Add(new EditorResourceNode
        {
            Id = "generator",
            Title = project.Generator.Id,
            Icon = "🗺",
            Kind = EditorResourceKind.Generator,
            Payload = project.Generator.Id
        });

        return [root];
    }

    private static EditorResourceNode CreateFolder(string id, string title, string icon) => new()
    {
        Id = id,
        Title = title,
        Icon = icon,
        Kind = EditorResourceKind.Folder,
        IsSelectable = false
    };

    private static EditorResourceNode CreatePlaceholderFolder(string id, string title, string icon, string phase) => new()
    {
        Id = id,
        Title = title,
        Icon = icon,
        Kind = EditorResourceKind.Folder,
        IsSelectable = false,
        Children =
        {
            new EditorResourceNode
            {
                Id = $"{id}-soon",
                Title = $"Coming in {phase}",
                Icon = "⏳",
                Kind = EditorResourceKind.Folder,
                IsSelectable = false,
                IsPlaceholder = true
            }
        }
    };
}
