using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RogueEngine.Editor.Models;

public partial class EditorResourceNode : ObservableObject
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public required string Icon { get; init; }
    public required EditorResourceKind Kind { get; init; }
    public string? Payload { get; init; }
    public bool IsSelectable { get; init; } = true;
    public bool IsPlaceholder { get; init; }

    public ObservableCollection<EditorResourceNode> Children { get; } = [];
}
