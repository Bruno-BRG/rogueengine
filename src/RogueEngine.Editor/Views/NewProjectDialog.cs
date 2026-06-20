using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace RogueEngine.Editor.Views;

public partial class NewProjectDialog : Window
{
    private readonly TextBox _nameBox;

    public NewProjectDialog()
    {
        Title = "New Project";
        Width = 420;
        Height = 180;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111114");

        var titleLabel = new TextBlock
        {
            Text = "Project Name",
            FontWeight = FontWeight.Bold,
            Foreground = Brush.Parse("#ffffff"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 4)
        };

        _nameBox = new TextBox
        {
            Text = "MyRoguelike",
            Watermark = "Enter project name...",
            Background = Brush.Parse("#232329"),
            BorderBrush = Brush.Parse("#2d2d35"),
            Foreground = Brush.Parse("#e3e3e6"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8),
            SelectionBrush = Brush.Parse("#0dbd85")
        };

        var createButton = new Button
        {
            Content = "Create Project",
            IsDefault = true,
            Background = Brush.Parse("#0dbd85"),
            Foreground = Brush.Parse("#ffffff"),
            BorderBrush = Brush.Parse("#0dbd85"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16, 8),
            FontWeight = FontWeight.SemiBold
        };
        createButton.Click += OnCreateClick;

        var cancelButton = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            Background = Brush.Parse("#232329"),
            Foreground = Brush.Parse("#e3e3e6"),
            BorderBrush = Brush.Parse("#2d2d35"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16, 8),
            FontWeight = FontWeight.SemiBold
        };
        cancelButton.Click += (_, _) => Close(null);

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new StackPanel
                {
                    Spacing = 6,
                    Children = { titleLabel, _nameBox }
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancelButton, createButton }
                }
            }
        };
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e)
    {
        var name = _nameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        Close(name);
    }
}
