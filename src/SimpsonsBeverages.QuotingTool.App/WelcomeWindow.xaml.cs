using System.Collections.ObjectModel;
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
    private const string CurrentVersion = "1.4.3";
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

    public WelcomeWindow()
    {
        InitializeComponent();

        CustomerListBox.ItemsSource = _customerItems;
        FindListBox.ItemsSource     = _findResults;
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
        _selectedQuoteNumber     = null;
        OpenQuoteButton.IsEnabled = false;

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
            _selectedQuoteNumber      = s.QuoteNumber;
            OpenQuoteButton.IsEnabled = true;
        }
        else
        {
            _selectedQuoteNumber      = null;
            OpenQuoteButton.IsEnabled = false;
        }
    }

    private void FindListDoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (OpenQuoteButton.IsEnabled) OpenQuoteClicked(sender, e);
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

    // ── Navigation helpers ───────────────────────────────────────────────────

    private void OpenMainWindow(string? initialCustomer, QuoteStoreEntry? loadedQuote)
    {
        var main = new MainWindow(initialCustomer, loadedQuote);

        main.Closed += (_, _) =>
        {
            var next = new WelcomeWindow();
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

        UpdateNowBtn.IsEnabled   = false;
        UpdateLaterBtn.IsEnabled = false;
        UpdatePromptText.Text    = "Downloading update…";

        try
        {
            var fileName = $"SimpsonsQuotingToolSetup-v{_updateVersion}.exe";
            var dest     = Path.Combine(Path.GetTempPath(), fileName);

            // Remove any leftover file from a prior attempt before writing.
            if (File.Exists(dest)) File.Delete(dest);

            using (var response   = await Http.GetStreamAsync(_updateDownloadUrl))
            using (var fileStream = File.Create(dest))
            {
                await response.CopyToAsync(fileStream);
            }

            // Retry up to 5 times: AV scanners (Defender) briefly lock a
            // newly-written exe while they scan it.
            UpdatePromptText.Text = "Launching installer…";
            var launched = false;
            for (var attempt = 0; attempt < 5 && !launched; attempt++)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(dest) { UseShellExecute = true });
                    launched = true;
                }
                catch (Exception) when (attempt < 4)
                {
                    await Task.Delay(1000);
                }
            }

            if (launched)
            {
                _closingIsNavigation = true;
                Application.Current.Shutdown();
            }
            else
            {
                throw new Exception("Installer could not be launched after multiple attempts. Try running it manually from: " + dest);
            }
        }
        catch (Exception ex)
        {
            UpdatePromptText.Text    = $"Download failed: {ex.Message}";
            UpdateNowBtn.IsEnabled   = true;
            UpdateLaterBtn.IsEnabled = true;
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
