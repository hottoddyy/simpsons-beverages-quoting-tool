using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SimpsonsBeverages.QuotingTool.App;

public sealed class ServeCostSettingsWindow : Window
{
    private readonly QuoteLineViewModel _line;
    private readonly ComboBox _modeBox;
    private readonly TextBox _mlBox;

    public ServeCostSettingsWindow(QuoteLineViewModel line)
    {
        _line = line;
        Title = "Serve cost";
        Width = 330;
        Height = 280;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        FontFamily = new FontFamily("Verdana");

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var modePanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        modePanel.Children.Add(MakeLabel("Mode"));
        _modeBox = new ComboBox
        {
            Height = 32,
            ItemsSource = new[] { "None", "RTD", "Concentrate" },
            SelectedItem = !line.IncludeServeCost ? "None" : line.ServeCostIsRtd ? "RTD" : "Concentrate"
        };
        modePanel.Children.Add(_modeBox);
        root.Children.Add(modePanel);

        var mlPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        Grid.SetRow(mlPanel, 1);
        mlPanel.Children.Add(MakeLabel("ml"));
        _mlBox = new TextBox
        {
            Height = 32,
            Padding = new Thickness(8, 4, 8, 4),
            Text = line.ServeMl.HasValue ? line.ServeMl.Value.ToString("N0", CultureInfo.GetCultureInfo("en-GB")) : string.Empty
        };
        mlPanel.Children.Add(_mlBox);
        root.Children.Add(mlPanel);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0)
        };
        Grid.SetRow(buttons, 3);
        buttons.Children.Add(MakeButton("Cancel", (_, _) => DialogResult = false, false));
        buttons.Children.Add(MakeButton("Apply", ApplyClicked, true));
        root.Children.Add(buttons);

        Content = root;
    }

    private static TextBlock MakeLabel(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(48, 64, 74)),
            Margin = new Thickness(0, 0, 0, 5)
        };
    }

    private static Button MakeButton(string text, RoutedEventHandler handler, bool primary)
    {
        var button = new Button
        {
            Content = text,
            Width = 88,
            MinHeight = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 4, 12, 4),
            Background = primary
                ? new SolidColorBrush(Color.FromRgb(5, 42, 97))
                : new SolidColorBrush(Color.FromRgb(219, 229, 234)),
            Foreground = primary ? Brushes.White : new SolidColorBrush(Color.FromRgb(29, 48, 56)),
            BorderBrush = primary
                ? new SolidColorBrush(Color.FromRgb(5, 42, 97))
                : new SolidColorBrush(Color.FromRgb(199, 213, 220)),
            FontWeight = FontWeights.Bold,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Click += handler;
        return button;
    }

    private void ApplyClicked(object sender, RoutedEventArgs e)
    {
        var mode = _modeBox.SelectedItem?.ToString() ?? "None";
        if (mode == "None")
        {
            _line.IncludeServeCost = false;
            _line.ServeMl = null;
            DialogResult = true;
            return;
        }

        if (!decimal.TryParse(_mlBox.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("en-GB"), out var ml) &&
            !decimal.TryParse(_mlBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out ml))
        {
            MessageBox.Show(this, "Enter ml as a number.", "Serve cost", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _line.IncludeServeCost = true;
        _line.ServeCostIsRtd = mode == "RTD";
        _line.ServeMl = ml;
        DialogResult = true;
    }
}
