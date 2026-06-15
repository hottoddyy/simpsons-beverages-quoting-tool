using System.Windows;

namespace SimpsonsBeverages.QuotingTool.App;

public partial class MergeDialog : Window
{
    // The name that will be DELETED (merged into the other)
    public string? FromName  { get; private set; }
    public string? IntoName  { get; private set; }

    private readonly string _nameA;
    private readonly string _nameB;

    public MergeDialog(string nameA, string nameB)
    {
        InitializeComponent();
        _nameA      = nameA;
        _nameB      = nameB;
        RadioA.Content = nameA;
        RadioB.Content = nameB;
    }

    private void MergeClicked(object sender, RoutedEventArgs e)
    {
        IntoName     = RadioA.IsChecked == true ? _nameA : _nameB;
        FromName     = RadioA.IsChecked == true ? _nameB : _nameA;
        DialogResult = true;
    }

    private void CancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;
}
