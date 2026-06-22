using CommunityToolkit.Mvvm.ComponentModel;

namespace RogueEngine.Editor.Models;

public partial class EditorDocumentTab : ObservableObject
{
    public required string TabKey { get; init; }
    public required EditorDocumentKind Kind { get; init; }
    public string? ResourceId { get; init; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _icon = "—";

    [ObservableProperty]
    private bool _isModified;
}
