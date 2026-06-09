using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpsonsBeverages.QuotingTool.App;

public sealed class LegacyImportSheetWindow : Window
{
    private readonly ComboBox _sheetBox;
    private readonly ComboBox _formatBox;

    public LegacyImportSheetWindow(IReadOnlyList<string> sheetNames, IReadOnlyList<string> formatNames)
    {
        Title = "Import specific tab";
        Width = 460;
        Height = 250;
        MinWidth = 420;
        MinHeight = 230;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(238, 244, 247));

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(new TextBlock
        {
            Text = "Choose the workbook tab and the costing layout to use.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(29, 48, 56)),
            Margin = new Thickness(0, 0, 0, 14)
        });

        _sheetBox = BuildComboBox(sheetNames);
        AddField(root, "Tab", _sheetBox, 1);

        _formatBox = BuildComboBox(formatNames);
        AddField(root, "Read as format", _formatBox, 2);

        SelectLikelyDefaults(sheetNames, formatNames);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(MakeButton("Cancel", (_, _) => DialogResult = false, isSecondary: true));
        buttons.Children.Add(MakeButton("Import", (_, _) => DialogResult = true));
        Grid.SetRow(buttons, 4);
        root.Children.Add(buttons);

        Content = root;
    }

    public string SelectedSheetName => _sheetBox.SelectedItem?.ToString() ?? string.Empty;

    public string SelectedFormatName => _formatBox.SelectedItem?.ToString() ?? string.Empty;

    private static ComboBox BuildComboBox(IReadOnlyList<string> values)
    {
        return new ComboBox
        {
            ItemsSource = values,
            Height = 32,
            Margin = new Thickness(0, 3, 0, 12),
            IsEditable = false
        };
    }

    private void SelectLikelyDefaults(IReadOnlyList<string> sheetNames, IReadOnlyList<string> formatNames)
    {
        _sheetBox.SelectedItem =
            sheetNames.FirstOrDefault(sheet => sheet.Contains("FLAV REDUCTION", StringComparison.OrdinalIgnoreCase)) ??
            sheetNames.FirstOrDefault(sheet => sheet.Contains("WITH BREAKS", StringComparison.OrdinalIgnoreCase)) ??
            sheetNames.FirstOrDefault();

        var selectedSheet = _sheetBox.SelectedItem?.ToString() ?? string.Empty;
        _formatBox.SelectedItem =
            formatNames.FirstOrDefault(format => string.Equals(format, selectedSheet, StringComparison.OrdinalIgnoreCase)) ??
            formatNames.FirstOrDefault(format => string.Equals(format, "6X1L", StringComparison.OrdinalIgnoreCase)) ??
            formatNames.FirstOrDefault();
    }

    private static void AddField(Grid root, string label, Control control, int row)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock
        {
            Text = label,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(5, 42, 97))
        });
        panel.Children.Add(control);
        Grid.SetRow(panel, row);
        root.Children.Add(panel);
    }

    private static Button MakeButton(string text, RoutedEventHandler handler, bool isSecondary = false)
    {
        var button = new Button
        {
            Content = text,
            Height = 34,
            MinWidth = 92,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 4, 12, 4),
            FontWeight = FontWeights.Bold,
            Background = isSecondary
                ? new SolidColorBrush(Color.FromRgb(219, 229, 234))
                : new SolidColorBrush(Color.FromRgb(5, 42, 97)),
            Foreground = isSecondary
                ? new SolidColorBrush(Color.FromRgb(29, 48, 56))
                : Brushes.White,
            BorderBrush = isSecondary
                ? new SolidColorBrush(Color.FromRgb(199, 213, 220))
                : new SolidColorBrush(Color.FromRgb(5, 42, 97))
        };
        button.Click += handler;
        return button;
    }
}
