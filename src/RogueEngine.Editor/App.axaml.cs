using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RogueEngine.Editor.Services;
using RogueEngine.Editor.ViewModels;
using RogueEngine.Editor.Views;

namespace RogueEngine.Editor;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnLastWindowClose;

            var navigation = new EditorNavigationService();
            desktop.MainWindow = new ProjectLauncherWindow
            {
                DataContext = new ProjectLauncherViewModel(navigation)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
