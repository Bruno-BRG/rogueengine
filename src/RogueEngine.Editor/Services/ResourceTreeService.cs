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
            Icon = string.Empty,
            Kind = EditorResourceKind.Folder,
            IsSelectable = false
        };

        root.Children.Add(new EditorResourceNode
        {
            Id = "settings",
            Title = "Project Settings",
            Icon = "[.]",
            Kind = EditorResourceKind.ProjectSettings
        });

        var scenesFolder = CreateFolder("scenes", "Scenes");
        foreach (var scene in project.Scenes.OrderBy(scene => scene.Name, StringComparer.OrdinalIgnoreCase))
        {
            scenesFolder.Children.Add(new EditorResourceNode
            {
                Id = $"scene:{scene.Id}",
                Title = scene.Name,
                Icon = "[S]",
                Kind = EditorResourceKind.Scene,
                Payload = scene.Id
            });
        }

        scenesFolder.Children.Add(new EditorResourceNode
        {
            Id = "add-scene",
            Title = "Add scene…",
            Icon = "+",
            Kind = EditorResourceKind.AddNew
        });
        root.Children.Add(scenesFolder);

        var actorsFolder = CreateFolder("actors", "Actors");
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
            Title = "Add actor…",
            Icon = "+",
            Kind = EditorResourceKind.AddNew
        });
        root.Children.Add(actorsFolder);

        root.Children.Add(CreatePlaceholderFolder("sprites", "Sprites", "E3"));
        root.Children.Add(CreatePlaceholderFolder("tilesets", "Tilesets", "E3"));

        var itemsFolder = CreateFolder("items", "Items");
        foreach (var item in project.Items.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
        {
            itemsFolder.Children.Add(new EditorResourceNode
            {
                Id = $"item:{item.Id}",
                Title = item.Name,
                Icon = item.Glyph.ToString(),
                Kind = EditorResourceKind.Item,
                Payload = item.Id
            });
        }

        itemsFolder.Children.Add(new EditorResourceNode
        {
            Id = "add-item",
            Title = "Add item…",
            Icon = "+",
            Kind = EditorResourceKind.AddNew
        });
        root.Children.Add(itemsFolder);

        if (project.Overworld is not null)
        {
            root.Children.Add(new EditorResourceNode
            {
                Id = "overworld",
                Title = project.Overworld.Id,
                Icon = "[W]",
                Kind = EditorResourceKind.Overworld,
                Payload = project.Overworld.Id
            });
        }

        var scriptsFolder = CreateFolder("scripts", "Scripts");
        foreach (var script in project.ScriptFiles.OrderBy(script => script.FileName, StringComparer.OrdinalIgnoreCase))
        {
            scriptsFolder.Children.Add(new EditorResourceNode
            {
                Id = $"script:{script.FileName}",
                Title = script.FileName,
                Icon = "[C#]",
                Kind = EditorResourceKind.CodeScript,
                Payload = script.FullPath
            });
        }

        root.Children.Add(scriptsFolder);

        var visualFolder = CreateFolder("visual", "Visual Scripts");
        foreach (var graph in project.VisualGraphs.OrderBy(graph => graph.Id, StringComparer.OrdinalIgnoreCase))
        {
            visualFolder.Children.Add(new EditorResourceNode
            {
                Id = $"visual:{graph.Id}",
                Title = graph.Id,
                Icon = "[V]",
                Kind = EditorResourceKind.VisualScript,
                Payload = graph.Id
            });
        }

        visualFolder.Children.Add(new EditorResourceNode
        {
            Id = "add-visual",
            Title = "Add visual script…",
            Icon = "+",
            Kind = EditorResourceKind.AddNew
        });
        root.Children.Add(visualFolder);

        root.Children.Add(new EditorResourceNode
        {
            Id = "generator",
            Title = project.Generator.Id,
            Icon = "[G]",
            Kind = EditorResourceKind.Generator,
            Payload = project.Generator.Id
        });

        return [root];
    }

    private static EditorResourceNode CreateFolder(string id, string title) => new()
    {
        Id = id,
        Title = title,
        Icon = string.Empty,
        Kind = EditorResourceKind.Folder,
        IsSelectable = false
    };

    private static EditorResourceNode CreatePlaceholderFolder(string id, string title, string phase) => new()
    {
        Id = id,
        Title = title,
        Icon = string.Empty,
        Kind = EditorResourceKind.Folder,
        IsSelectable = false,
        Children =
        {
            new EditorResourceNode
            {
                Id = $"{id}-soon",
                Title = $"Planned ({phase})",
                Icon = "—",
                Kind = EditorResourceKind.Folder,
                IsSelectable = false,
                IsPlaceholder = true
            }
        }
    };
}
