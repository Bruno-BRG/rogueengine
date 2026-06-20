using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using RogueEngine.Editor.ViewModels;
using RogueEngine.Editor.Views;

namespace RogueEngine.Editor.Services;

public sealed class EditorNavigationService
{
    private readonly RecentProjectsService _recentProjects = new();

    public void ShowLauncher()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var launcher = new ProjectLauncherWindow
        {
            DataContext = new ProjectLauncherViewModel(this)
        };

        var previous = desktop.MainWindow;
        desktop.MainWindow = launcher;
        launcher.Show();
        launcher.Activate();

        if (previous is not null && !ReferenceEquals(previous, launcher))
        {
            previous.Hide();
            previous.Close();
        }
    }

    public void OpenProjectInEditor(string reprojPath, string projectName)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        _recentProjects.RecordOpened(reprojPath, projectName);

        var shell = new EditorShellWindow
        {
            DataContext = new EditorShellViewModel(this, reprojPath)
        };

        var previous = desktop.MainWindow;
        desktop.MainWindow = shell;
        shell.Show();
        shell.Activate();

        if (previous is not null && !ReferenceEquals(previous, shell))
        {
            previous.Hide();
            previous.Close();
        }
    }

    public static Window? GetActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        return desktop.MainWindow;
    }
}
