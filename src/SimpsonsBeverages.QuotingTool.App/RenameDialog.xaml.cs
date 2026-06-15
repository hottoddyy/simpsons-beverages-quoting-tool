using System.Windows;
using System.Windows.Input;

namespace SimpsonsBeverages.QuotingTool.App;

public partial class RenameDialog : Window
{
    public string? NewName { get; private set; }

    public RenameDialog(string currentName)
    {
        InitializeComponent();
        PromptText.Text = $"Rename \"{currentName}\" to:";
        NameBox.Text    = currentName;
        Loaded += (_, _) => { NameBox.Focus(); NameBox.SelectAll(); };
    }

    private void OkClicked(object sender, RoutedEventArgs e) => Accept();
    private void CancelClicked(object sender, RoutedEventArgs e) => DialogResult = false;

    private void NameBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) Accept();
        if (e.Key == Key.Escape) DialogResult = false;
    }

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text)) return;
        NewName      = NameBox.Text.Trim();
        DialogResult = true;
    }
}
