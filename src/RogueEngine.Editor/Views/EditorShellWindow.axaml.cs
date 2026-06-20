using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using RogueEngine.Editor.Models;
using RogueEngine.Editor.ViewModels;

namespace RogueEngine.Editor.Views;

public partial class EditorShellWindow : Window
{
    public EditorShellWindow()
    {
        InitializeComponent();
    }

    private void OnResourceDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ResourceTreeView.SelectedItem is not EditorResourceNode node)
        {
            return;
        }

        if (DataContext is EditorShellViewModel viewModel)
        {
            viewModel.OpenResourceCommand.Execute(node);
        }
    }
}
