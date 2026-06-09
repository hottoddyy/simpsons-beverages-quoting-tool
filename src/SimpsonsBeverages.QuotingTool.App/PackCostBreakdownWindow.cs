using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace SimpsonsBeverages.QuotingTool.App;

public sealed class PackCostBreakdownWindow : Window
{
    private readonly ObservableCollection<PackCostComponentViewModel> _components;
    private readonly TextBlock _totalText;
    public bool ApplyToAllMatchingFormats { get; private set; }

    public PackCostBreakdownWindow(QuoteLineViewModel line)
    {
        Title = "Pack cost breakdown";
        Width = 620;
        Height = 430;
        MinWidth = 560;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(238, 244, 247));

        _components = new ObservableCollection<PackCostComponentViewModel>(
            line.PackCostBreakdown.Count == 0
                ? [new PackCostComponentViewModel($"{line.FormatName} default pack cost", line.PackCost)]
                : line.PackCostBreakdown.Select(component => new PackCostComponentViewModel(
                    component.Description,
                    component.Cost,
                    component.Options,
                    component.SelectedOption?.Name)));

        foreach (var component in _components)
        {
            component.PropertyChanged += (_, _) => UpdateTotal();
        }

        Components = _components;

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var heading = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        heading.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(line.Description) ? "Pack cost breakdown" : line.Description,
            FontSize = 18,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(5, 42, 97))
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Edit the packaging cost components. The total is applied back to Pack cost.",
            Margin = new Thickness(0, 4, 0, 0),
            Foreground = new SolidColorBrush(Color.FromRgb(89, 103, 117))
        });
        Grid.SetRow(heading, 0);
        root.Children.Add(heading);

        var grid = new DataGrid
        {
            ItemsSource = _components,
            AutoGenerateColumns = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            RowHeaderWidth = 0,
            RowHeight = 32,
            ColumnHeaderHeight = 32,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(207, 216, 223)),
            BorderThickness = new Thickness(1)
        };

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Component",
            Binding = new Binding(nameof(PackCostComponentViewModel.Description)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = 260
        });

        if (_components.Any(component => component.Options.Count > 0))
        {
            grid.Columns.Add(new DataGridTemplateColumn
            {
                Header = "Type",
                CellTemplate = BuildOptionTemplate(isEditing: false),
                CellEditingTemplate = BuildOptionTemplate(isEditing: true),
                Width = 140
            });
        }

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Cost",
            Binding = new Binding(nameof(PackCostComponentViewModel.Cost))
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                StringFormat = "N4"
            },
            Width = 130
        });

        Grid.SetRow(grid, 1);
        root.Children.Add(grid);

        var footer = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftButtons = new StackPanel { Orientation = Orientation.Horizontal };
        leftButtons.Children.Add(MakeButton("Add line", (_, _) => AddLine()));
        leftButtons.Children.Add(MakeButton("Remove selected", (_, _) =>
        {
            if (grid.SelectedItem is PackCostComponentViewModel selected)
            {
                _components.Remove(selected);
                UpdateTotal();
            }
        }, isSecondary: true));
        footer.Children.Add(leftButtons);

        _totalText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 32, 38)),
            Margin = new Thickness(0, 0, 18, 0)
        };
        Grid.SetColumn(_totalText, 1);
        footer.Children.Add(_totalText);

        var rightButtons = new StackPanel { Orientation = Orientation.Horizontal };
        rightButtons.Children.Add(MakeButton("Cancel", (_, _) => DialogResult = false, isSecondary: true));
        rightButtons.Children.Add(MakeButton("Apply all", (_, _) =>
        {
            ApplyToAllMatchingFormats = true;
            DialogResult = true;
        }, isSecondary: true));
        rightButtons.Children.Add(MakeButton("Apply", (_, _) => DialogResult = true));
        Grid.SetColumn(rightButtons, 2);
        footer.Children.Add(rightButtons);

        Grid.SetRow(footer, 2);
        root.Children.Add(footer);

        Content = root;
        UpdateTotal();
    }

    public IReadOnlyList<PackCostComponentViewModel> Components { get; }

    private void AddLine()
    {
        var component = new PackCostComponentViewModel("Packaging component", 0m);
        component.PropertyChanged += (_, _) => UpdateTotal();
        _components.Add(component);
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        _totalText.Text = $"Total pack cost: {_components.Sum(component => component.Cost).ToString("C4", CultureInfo.GetCultureInfo("en-GB"))}";
    }

    private static Button MakeButton(string text, RoutedEventHandler handler, bool isSecondary = false)
    {
        var button = new Button
        {
            Content = text,
            Height = 34,
            MinWidth = isSecondary ? 118 : 92,
            Margin = new Thickness(0, 0, 8, 0),
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

    private static DataTemplate BuildOptionTemplate(bool isEditing)
    {
        var factory = new FrameworkElementFactory(typeof(ComboBox));
        factory.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(PackCostComponentViewModel.Options)));
        factory.SetBinding(Selector.SelectedItemProperty, new Binding(nameof(PackCostComponentViewModel.SelectedOption))
        {
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Mode = BindingMode.TwoWay
        });
        factory.SetValue(ComboBox.DisplayMemberPathProperty, nameof(PackCostOption.Name));
        factory.SetValue(Control.PaddingProperty, new Thickness(5, 2, 5, 2));
        factory.SetValue(UIElement.IsHitTestVisibleProperty, isEditing);
        factory.SetValue(Control.BorderThicknessProperty, isEditing ? new Thickness(1) : new Thickness(0));
        factory.SetValue(Control.BackgroundProperty, Brushes.White);

        return new DataTemplate { VisualTree = factory };
    }
}
