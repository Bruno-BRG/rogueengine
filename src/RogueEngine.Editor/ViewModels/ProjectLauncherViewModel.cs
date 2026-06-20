using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RogueEngine.Editor.Models;
using RogueEngine.Editor.Services;
using RogueEngine.Editor.Views;

namespace RogueEngine.Editor.ViewModels;

public partial class ProjectLauncherViewModel : ViewModelBase
{
    private readonly EditorNavigationService _navigation;
    private readonly RecentProjectsService _recentProjects = new();
    private readonly TemplateService _templateService = new();
    private readonly ProjectService _projectService = new();

    public ProjectLauncherViewModel(EditorNavigationService navigation)
    {
        _navigation = navigation;
        RecentProjects = new ObservableCollection<RecentProjectEntry>(_recentProjects.Load());
        MostRecentProject = RecentProjects.FirstOrDefault();
    }

    public ObservableCollection<RecentProjectEntry> RecentProjects { get; }

    [ObservableProperty]
    private RecentProjectEntry? _mostRecentProject;

    [ObservableProperty]
    private RecentProjectEntry? _selectedRecentProject;

    [ObservableProperty]
    private string _statusText = "Welcome to RogueEngine";

    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        var window = EditorNavigationService.GetActiveWindow();
        if (window is null)
        {
            return;
        }

        var nameDialog = new NewProjectDialog();
        var nameResult = await nameDialog.ShowDialog<string?>(window);
        if (string.IsNullOrWhiteSpace(nameResult))
        {
            return;
        }

        var folder = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose parent folder for the new project",
            AllowMultiple = false
        });

        if (folder.Count == 0)
        {
            return;
        }

        try
        {
            var parentPath = folder[0].Path.LocalPath;
            var reprojPath = _templateService.CreateProject(parentPath, nameResult.Trim());
            var project = _projectService.Open(reprojPath);
            StatusText = $"Created {project.Name}";
            _navigation.OpenProjectInEditor(reprojPath, project.Name);
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var window = EditorNavigationService.GetActiveWindow();
        if (window is null)
        {
            return;
        }

        var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open game.reproj",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("RogueEngine Project") { Patterns = ["*.reproj"] }
            ]
        });

        if (files.Count == 0)
        {
            return;
        }

        OpenProjectAtPath(files[0].Path.LocalPath);
    }

    [RelayCommand]
    private void OpenRecentProject()
    {
        if (SelectedRecentProject is null)
        {
            return;
        }

        OpenProjectAtPath(SelectedRecentProject.ReprojPath);
    }

    [RelayCommand]
    private void OpenMostRecentProject()
    {
        if (MostRecentProject is null)
        {
            return;
        }

        OpenProjectAtPath(MostRecentProject.ReprojPath);
    }

    private void OpenProjectAtPath(string reprojPath)
    {
        try
        {
            var project = _projectService.Open(reprojPath);
            _navigation.OpenProjectInEditor(reprojPath, project.Name);
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}
