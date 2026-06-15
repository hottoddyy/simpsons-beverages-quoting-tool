using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace SimpsonsBeverages.QuotingTool.App;

public partial class WelcomeWindow : Window
{
    // ── DWM navy title bar ───────────────────────────────────────────────────
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, uint attrSize);
    private const uint DwmwaCaption_Color = 35;
    private static int NavyBgr = unchecked((int)0x00612A05);

    // ── Update check ─────────────────────────────────────────────────────────
    private const string CurrentVersion = "1.4.10";
    private const string ReleasesApiUrl =
        "https://api.github.com/repos/hottoddyy/simpsons-beverages-quoting-tool/releases/latest";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders = { { "User-Agent", "SimpsonsBeveragesQuotingTool" } }
    };

    private string? _updateDownloadUrl;
    private string? _updateVersion;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly QuoteStore _quoteStore = new();
    private readonly ObservableCollection<string> _customerItems = [];
    private readonly ObservableCollection<QuoteStoreSummary> _findResults = [];
    private string? _selectedCustomer;
    private string? _selectedQuoteNumber;
    private bool _storeAvailable;
    private bool _closingIsNavigation;
    private readonly bool _startOnCustomer;

    public WelcomeWindow(bool startOnCustomer = false)
    {
        _startOnCustomer = startOnCustomer;
        InitializeComponent();

        CustomerListBox.ItemsSource      = _customerItems;
        FindListBox.ItemsSource          = _findResults;
        ManageCustomerListBox.ItemsSource = _manageCustomerItems;
        GreetingText.Text           = $"Hello, {GetFirstName()}! Welcome.";

        // Navy title bar — must run after handle is created.
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
                DwmSetWindowAttribute(hwnd, DwmwaCaption_Color, ref NavyBgr, sizeof(int));
        };

        Loaded += WindowLoaded;
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        _storeAvailable = _quoteStore.IsAvailable();
        if (_storeAvailable)
        {
            try { _quoteStore.Initialise(); }
            catch { _storeAvailable = false; }
        }

        if (_startOnCustomer) ShowCustomerPanel();

        _ = CheckForUpdatesAsync();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!_closingIsNavigation)
            Application.Current.Shutdown();
    }

    // ── Welcome panel ────────────────────────────────────────────────────────

    private void CreateQuoteClicked(object sender, RoutedEventArgs e) => ShowCustomerPanel();

    private void FindQuoteClicked(object sender, RoutedEventArgs e) => ShowFindPanel();

    // ── Customer panel ───────────────────────────────────────────────────────

    private void ShowCustomerPanel()
    {
        WelcomePanel.Visibility  = Visibility.Collapsed;
        FindPanel.Visibility     = Visibility.Collapsed;
        CustomerPanel.Visibility = Visibility.Visible;

        CustomerSearchBox.Text = string.Empty;
        RefreshCustomerList(string.Empty);
        Dispatcher.BeginInvoke(() => CustomerSearchBox.Focus());
    }

    private void CustomerBackClicked(object sender, RoutedEventArgs e)
    {
        CustomerPanel.Visibility = Visibility.Collapsed;
        WelcomePanel.Visibility  = Visibility.Visible;
    }

    private void CustomerSearchChanged(object sender, TextChangedEventArgs e)
        => RefreshCustomerList(CustomerSearchBox.Text);

    private void CustomerSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ContinueButton.IsEnabled)
        {
            ContinueClicked(sender, e);
        }
        else if (e.Key == Key.Down && _customerItems.Count > 0)
        {
            CustomerListBox.Focus();
            CustomerListBox.SelectedIndex = 0;
        }
    }

    private void RefreshCustomerList(string search)
    {
        _customerItems.Clear();
        _selectedCustomer = null;
        ContinueButton.IsEnabled = false;

        if (_storeAvailable)
        {
            try
            {
                foreach (var name in _quoteStore.GetCustomers(search))
                    _customerItems.Add(name);
            }
            catch { /* network hiccup — show whatever loaded */ }
        }

        // Offer "Add new" when the typed text doesn't match an existing entry exactly.
        var trimmed = search.Trim();
        if (trimmed.Length > 0 &&
            !_customerItems.Any(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            _customerItems.Add($"+ Add new: \"{trimmed}\"");
        }
    }

    private void CustomerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CustomerListBox.SelectedItem is string item)
        {
            _selectedCustomer = item.StartsWith("+ Add new: \"", StringComparison.Ordinal)
                ? CustomerSearchBox.Text.Trim()
                : item;
            ContinueButton.IsEnabled = !string.IsNullOrWhiteSpace(_selectedCustomer);
        }
        else
        {
            _selectedCustomer = null;
            ContinueButton.IsEnabled = false;
        }
    }

    private void CustomerListDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (ContinueButton.IsEnabled) ContinueClicked(sender, e);
    }

    private void ContinueClicked(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedCustomer)) return;
        OpenMainWindow(_selectedCustomer, null);
    }

    // ── Find panel ───────────────────────────────────────────────────────────

    private void ShowFindPanel()
    {
        WelcomePanel.Visibility  = Visibility.Collapsed;
        CustomerPanel.Visibility = Visibility.Collapsed;
        FindPanel.Visibility     = Visibility.Visible;

        FindSearchBox.Text = string.Empty;
        RefreshFindResults(null);
        Dispatcher.BeginInvoke(() => FindSearchBox.Focus());
    }

    private void FindBackClicked(object sender, RoutedEventArgs e)
    {
        FindPanel.Visibility    = Visibility.Collapsed;
        WelcomePanel.Visibility = Visibility.Visible;
    }

    private void FindSearchChanged(object sender, TextChangedEventArgs e)
        => RefreshFindResults(string.IsNullOrWhiteSpace(FindSearchBox.Text) ? null : FindSearchBox.Text);

    private void RefreshFindResults(string? search)
    {
        _findResults.Clear();
        _selectedQuoteNumber         = null;
        OpenQuoteButton.IsEnabled    = false;
        DeleteFindQuoteBtn.IsEnabled = false;

        if (!_storeAvailable) return;

        try
        {
            foreach (var r in _quoteStore.List(search))
                _findResults.Add(r with { ModifiedAt = FormatDate(r.ModifiedAt) });
        }
        catch { /* network hiccup */ }
    }

    private void FindSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FindListBox.SelectedItem is QuoteStoreSummary s)
        {
            _selectedQuoteNumber         = s.QuoteNumber;
            OpenQuoteButton.IsEnabled    = true;
            DeleteFindQuoteBtn.IsEnabled = true;
        }
        else
        {
            _selectedQuoteNumber         = null;
            OpenQuoteButton.IsEnabled    = false;
            DeleteFindQuoteBtn.IsEnabled = false;
        }
    }

    private void FindListDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (OpenQuoteButton.IsEnabled) OpenQuoteClicked(sender, e);
    }

    private void DeleteFindQuoteClicked(object sender, RoutedEventArgs e)
    {
        if (_selectedQuoteNumber is null) return;
        var result = MessageBox.Show(this,
            $"Permanently delete quote {_selectedQuoteNumber}? This cannot be undone.",
            "Delete quote", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;
        try
        {
            _quoteStore.DeleteQuote(_selectedQuoteNumber);
            RefreshFindResults(FindSearchBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete quote: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenQuoteClicked(object sender, RoutedEventArgs e)
    {
        if (_selectedQuoteNumber is null) return;

        try
        {
            var entry = _quoteStore.Load(_selectedQuoteNumber);
            if (entry is null)
            {
                MessageBox.Show(this, $"Quote {_selectedQuoteNumber} could not be loaded.",
                    "Not found", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenMainWindow(null, entry);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to load quote: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Manage customers panel ────────────────────────────────────────────

    private readonly ObservableCollection<string> _manageCustomerItems = [];

    private void ManageCustomersClicked(object sender, RoutedEventArgs e)
    {
        CustomerPanel.Visibility        = Visibility.Collapsed;
        ManageCustomersPanel.Visibility = Visibility.Visible;
        ManageSearchBox.Text            = string.Empty;
        RefreshManageList(string.Empty);
        Dispatcher.BeginInvoke(() => ManageSearchBox.Focus());
    }

    private void ManageCustomersBackClicked(object sender, RoutedEventArgs e)
    {
        ManageCustomersPanel.Visibility = Visibility.Collapsed;
        CustomerPanel.Visibility        = Visibility.Visible;
        RefreshCustomerList(CustomerSearchBox.Text);
    }

    private void ManageSearchChanged(object sender, TextChangedEventArgs e)
        => RefreshManageList(ManageSearchBox.Text);

    private void RefreshManageList(string search)
    {
        _manageCustomerItems.Clear();
        if (!_storeAvailable) return;
        try
        {
            foreach (var name in _quoteStore.GetCustomers(search))
                _manageCustomerItems.Add(name);
        }
        catch { }
    }

    private void ManageCustomerSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var count = ManageCustomerListBox.SelectedItems.Count;
        RenameCustomerBtn.IsEnabled  = count == 1;
        DeleteCustomerBtn.IsEnabled  = count == 1;
        MergeCustomerBtn.IsEnabled   = count == 2;
        ManageSubtext.Text = count == 2
            ? "Click Merge to combine the two selected customers."
            : "Select a customer to rename or delete. Select two to merge.";
    }

    private void RenameCustomerClicked(object sender, RoutedEventArgs e)
    {
        if (ManageCustomerListBox.SelectedItem is not string oldName) return;

        var dialog = new RenameDialog(oldName) { Owner = this };
        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.NewName)) return;
        var newName = dialog.NewName.Trim();
        if (string.Equals(newName, oldName, StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            _quoteStore.RenameCustomer(oldName, newName);
            RefreshManageList(ManageSearchBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not rename: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteCustomerClicked(object sender, RoutedEventArgs e)
    {
        if (ManageCustomerListBox.SelectedItem is not string name) return;

        var quoteCount = 0;
        try { quoteCount = _quoteStore.CountQuotesForCustomer(name); } catch { }

        var msg = quoteCount > 0
            ? $"Delete customer \"{name}\"? They have {quoteCount} quote{(quoteCount == 1 ? "" : "s")} which will remain in the database but no longer be linked to this customer name."
            : $"Delete customer \"{name}\"?";

        var result = MessageBox.Show(this, msg, "Delete customer",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            _quoteStore.DeleteCustomer(name);
            RefreshManageList(ManageSearchBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not delete: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MergeCustomerClicked(object sender, RoutedEventArgs e)
    {
        if (ManageCustomerListBox.SelectedItems.Count != 2) return;
        var a = (string)ManageCustomerListBox.SelectedItems[0]!;
        var b = (string)ManageCustomerListBox.SelectedItems[1]!;

        var dialog = new MergeDialog(a, b) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        try
        {
            _quoteStore.MergeCustomers(dialog.FromName!, dialog.IntoName!);
            RefreshManageList(ManageSearchBox.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not merge: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Import handlers ──────────────────────────────────────────────────────

    private void ImportExcelClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Import legacy costing workbook",
            Filter = "Excel costing workbooks (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var imported = LegacyCostingWorkbookIo.Import(dialog.FileName);
            if (imported.Count == 0)
            {
                MessageBox.Show(this, "No quote lines found in the selected workbook.",
                    "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OpenMainWindow(null, null, imported);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Import failed: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportTabClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Import a specific legacy costing tab",
            Filter = "Excel costing workbooks (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var sheetNames  = LegacyCostingWorkbookIo.GetSheetNames(dialog.FileName);
            var formatNames = LegacyCostingWorkbookIo.GetImportFormatNames();
            var picker      = new LegacyImportSheetWindow(sheetNames, formatNames) { Owner = this };
            if (picker.ShowDialog() != true) return;
            var imported = LegacyCostingWorkbookIo.ImportSheet(
                dialog.FileName, picker.SelectedSheetName!, picker.SelectedFormatName!);
            if (imported.Count == 0)
            {
                MessageBox.Show(this, $"No quote lines found in tab '{picker.SelectedSheetName}'.",
                    "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            OpenMainWindow(null, null, imported);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Import failed: {ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Navigation helpers ───────────────────────────────────────────────────

    private void OpenMainWindow(string? initialCustomer, QuoteStoreEntry? loadedQuote,
                                IReadOnlyList<LegacyCostingQuoteLine>? importedLines = null)
    {
        var main = new MainWindow(initialCustomer, loadedQuote, importedLines);

        main.Closed += (_, _) =>
        {
            var next = new WelcomeWindow(startOnCustomer: main.ReturnToCustomerPanel);
            Application.Current.MainWindow = next;
            next.Show();
        };

        Application.Current.MainWindow = main;
        main.Show();

        _closingIsNavigation = true;
        Close();
    }

    // ── Static helpers ───────────────────────────────────────────────────────

    // ── Update check ─────────────────────────────────────────────────────────

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var json = await Http.GetStringAsync(ReleasesApiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var tagName = root.GetProperty("tag_name").GetString();
            if (tagName is null) return;

            var remoteVersionStr = tagName.TrimStart('v');
            if (!Version.TryParse(remoteVersionStr, out var remote)) return;
            if (!Version.TryParse(CurrentVersion, out var current)) return;
            if (remote <= current) return;

            // Find the .exe installer asset.
            string? assetUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        assetUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }
            }

            if (assetUrl is null) return;

            _updateDownloadUrl = assetUrl;
            _updateVersion     = remoteVersionStr;

            Dispatcher.Invoke(() =>
            {
                if (!IsLoaded) return;   // window already closed (user navigated before check finished)
                UpdatePromptText.Text    = $"Version {remoteVersionStr} is available. You have {CurrentVersion}.";
                UpdateOverlay.Visibility = Visibility.Visible;
            });
        }
        catch
        {
            // Update check is non-critical — silently ignore any network/parse error.
        }
    }

    private void UpdateLaterClicked(object sender, RoutedEventArgs e)
    {
        UpdateOverlay.Visibility = Visibility.Collapsed;
        UpdatePillBtn.Visibility  = Visibility.Visible;
    }

    private void UpdatePillClicked(object sender, RoutedEventArgs e)
    {
        // Re-show the prompt when they click the pill.
        UpdateOverlay.Visibility = Visibility.Visible;
    }

    private async void UpdateNowClicked(object sender, RoutedEventArgs e)
    {
        if (_updateDownloadUrl is null) return;

        // Swap buttons out for the progress bar.
        UpdateButtonRow.Visibility   = Visibility.Collapsed;
        UpdateProgressRow.Visibility = Visibility.Visible;
        UpdatePromptText.Text        = "Downloading update…";

        try
        {
            var fileName = $"SimpsonsQuotingToolSetup-v{_updateVersion}.exe";
            var dest     = Path.Combine(Path.GetTempPath(), fileName);

            if (File.Exists(dest)) File.Delete(dest);

            // Stream download with chunk-by-chunk progress reporting.
            using var response = await Http.GetAsync(_updateDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            var total      = response.Content.Headers.ContentLength ?? -1L;
            var downloaded = 0L;
            var buffer     = new byte[81920]; // 80 KB chunks

            using (var src  = await response.Content.ReadAsStreamAsync())
            using (var dest_ = File.Create(dest))
            {
                int bytesRead;
                while ((bytesRead = await src.ReadAsync(buffer)) > 0)
                {
                    await dest_.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloaded += bytesRead;

                    if (total > 0)
                    {
                        var pct = (int)(downloaded * 100 / total);
                        UpdateProgressBar.Value  = pct;
                        UpdateProgressLabel.Text = $"{pct}%  ({downloaded / 1_048_576.0:F1} / {total / 1_048_576.0:F1} MB)";
                    }
                }
            }

            // Silent install — no wizard, no prompts, no restart.
            UpdateProgressBar.Value  = 100;
            UpdatePromptText.Text    = "Installing in background…";
            UpdateProgressLabel.Text = string.Empty;

            // Retry launch: Defender briefly locks a freshly-written exe while scanning.
            var launched = false;
            for (var attempt = 0; attempt < 5 && !launched; attempt++)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(dest)
                    {
                        UseShellExecute = true,
                        Arguments       = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"
                    });
                    launched = true;
                }
                catch (Exception) when (attempt < 4)
                {
                    await Task.Delay(1000);
                }
            }

            if (!launched)
                throw new Exception("Installer could not be launched. Try running it manually: " + dest);

            _closingIsNavigation = true;
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            UpdatePromptText.Text        = $"Update failed: {ex.Message}";
            UpdateProgressRow.Visibility = Visibility.Collapsed;
            UpdateButtonRow.Visibility   = Visibility.Visible;
            UpdateNowBtn.IsEnabled       = true;
            UpdateLaterBtn.IsEnabled     = true;
        }
    }

    private static string GetFirstName()
    {
        var username = Environment.UserName;                   // e.g. "todd.simpson"
        var first    = username.Split('.', '_', ' ', '-')[0];
        if (first.Length == 0) return username;
        return char.ToUpper(first[0]) + first[1..].ToLower();
    }

    private static string FormatDate(string isoDate)
    {
        return DateTime.TryParse(isoDate, null, DateTimeStyles.RoundtripKind, out var dt)
            ? dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
            : isoDate;
    }
}
