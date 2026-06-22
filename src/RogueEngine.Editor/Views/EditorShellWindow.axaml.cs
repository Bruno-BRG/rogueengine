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
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditorShellViewModel viewModel)
        {
            return;
        }

        SceneViewport.PreviewRegenerateRequested += (_, _) =>
            viewModel.RegenerateScenePreviewCommand.Execute(null);
        SceneViewport.SeedCommitted += (_, seed) =>
        {
            viewModel.SceneSeed = seed;
            viewModel.RefreshSceneMapPreview();
        };
        SceneViewport.EntityPlaced += (_, _) => viewModel.OnSceneViewportChanged();
        SceneViewport.EntityRemoved += (_, _) => viewModel.OnSceneViewportChanged();
        SceneViewport.ItemPlaced += (_, _) => viewModel.OnSceneViewportChanged();
        SceneViewport.ItemRemoved += (_, _) => viewModel.OnSceneViewportChanged();
        SceneViewport.PlayerSpawnChanged += (_, _) => viewModel.OnSceneViewportChanged();

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(EditorShellViewModel.Actors) or nameof(EditorShellViewModel.SelectedScene))
            {
                SceneViewport.RefreshPlaceActorCombo();
            }

            if (args.PropertyName is nameof(EditorShellViewModel.Items) or nameof(EditorShellViewModel.SelectedScene))
            {
                SceneViewport.RefreshPlaceItemCombo();
            }
        };
    }

    private void OnResourceDoubleTapped(object? sender, TappedEventArgs e) =>
        OpenSelectedResource();

    private void OnResourceSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ResourceTreeView.SelectedItem is not EditorResourceNode node)
        {
            return;
        }

        if (node.Kind == EditorResourceKind.AddNew)
        {
            OpenSelectedResource();
        }
    }

    private void OpenSelectedResource()
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
