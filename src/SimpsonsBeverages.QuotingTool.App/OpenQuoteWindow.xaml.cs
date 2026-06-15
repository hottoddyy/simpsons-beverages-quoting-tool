using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpsonsBeverages.QuotingTool.App;

public partial class OpenQuoteWindow : Window
{
    private readonly QuoteStore _store;
    public string? SelectedQuoteNumber { get; private set; }

    public OpenQuoteWindow(QuoteStore store)
    {
        _store = store;
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Refresh(null);
            SearchBox.Focus();
        };
        QuoteGrid.SelectionChanged += (_, _) =>
            DeleteBtn.IsEnabled = QuoteGrid.SelectedItem is QuoteRow;
    }

    private void Refresh(string? search)
    {
        try
        {
            var results = _store.List(search);
            QuoteGrid.ItemsSource = results.Select(r => new QuoteRow(r)).ToList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not load quotes:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SearchBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        Refresh(SearchBox.Text);
    }

    private void QuoteGridMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        TryConfirm();
    }

    private void OpenClicked(object sender, RoutedEventArgs e)
    {
        TryConfirm();
    }

    private void CancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void DeleteClicked(object sender, RoutedEventArgs e)
    {
        if (QuoteGrid.SelectedItem is not QuoteRow row) return;
        var result = MessageBox.Show(this,
            $"Permanently delete quote {row.QuoteNumber}? This cannot be undone.",
            "Delete quote", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        try
        {
            _store.DeleteQuote(row.QuoteNumber);
            Refresh(SearchBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete quote: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TryConfirm()
    {
        if (QuoteGrid.SelectedItem is QuoteRow row)
        {
            SelectedQuoteNumber = row.QuoteNumber;
            DialogResult = true;
        }
    }
}

internal sealed class QuoteRow(QuoteStoreSummary summary)
{
    public string QuoteNumber { get; } = summary.QuoteNumber;
    public string Customer { get; } = summary.Customer;

    public string ModifiedDisplay { get; } = DateTime.TryParse(summary.ModifiedAt, out var dt)
        ? dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        : summary.ModifiedAt;
}
