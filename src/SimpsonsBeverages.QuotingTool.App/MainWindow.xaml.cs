using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using SimpsonsBeverages.QuotingTool.Calculations;

namespace SimpsonsBeverages.QuotingTool.App;

public partial class MainWindow : Window
{
    // ── DWM title bar colouring ──────────────────────────────────────────────
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, uint attr, ref int attrValue, uint attrSize);

    private const uint DwmwaCaption_Color = 35; // DWMWA_CAPTION_COLOR

    // BGR integer for #052A61 (navy)
    private static readonly int NavyBgr = unchecked((int)0x00612A05);
    // BGR integer for #1a1a2e (dark navy) used in dark mode
    private static readonly int DarkNavyBgr = unchecked((int)0x002e1a1a);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        SetTitleBarColour(NavyBgr);
    }

    private void SetTitleBarColour(int colourBgr)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
            DwmSetWindowAttribute(hwnd, DwmwaCaption_Color, ref colourBgr, sizeof(int));
    }

    // ── Dark mode ────────────────────────────────────────────────────────────
    private bool _isDarkMode;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SimpsonsBeverages", "QuotingTool", "settings.json");

    private sealed record AppSettings(bool DarkMode);

    private static AppSettings LoadSettings()
    {
        try
        {
            var json = File.ReadAllText(SettingsPath);
            return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings(false);
        }
        catch { return new AppSettings(false); }
    }

    private static void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, System.Text.Json.JsonSerializer.Serialize(settings));
        }
        catch { /* non-critical — ignore */ }
    }
    private readonly List<FormatDefinition> _formats =
    [
        FormatDefinition.FromMaster("MANUAL LINE", CalculationKind.Bulk, 1m, "LITRE", 1m, 6m, []),
        FormatDefinition.FromMaster("TOSCA", CalculationKind.Bulk, 1m, "LITRE", 1m, 6m,
        [
            new("TOSCA RENTAL", 51.5m),
            new("1000L TOSCA LINER", 11.7229m),
            new("LABOUR", 60m),
            new("TRANSPORT", 65m),
            new("OVERHEADS", 20m)
        ]),
        FormatDefinition.FromMaster("IBC", CalculationKind.Bulk, 1m, "LITRE", 1m, 6m,
        [
            new("IBC", 111.46m),
            new("LABOUR", 60m),
            new("TRANSPORT", 65m),
            new("OVERHEADS", 20m)
        ]),
        FormatDefinition.FromMaster("220L DRUM", CalculationKind.Bulk, 1m, "LITRE", 1m, 6m,
        [
            new("220L DRUM", 149.6385m),
            new("PALLET", 9.9375m),
            new("LABOUR", 60m),
            new("TRANSPORT", 81.25m),
            new("OVERHEADS", 20m)
        ]),
        FormatDefinition.FromMaster("25L", CalculationKind.Bulk, 1m, "LITRE", 1m, 10m,
        [
            new("PALLET", 12.421875m),
            new("25L CLOSURE", 13.1222m),
            new("25L CONTAINER", 182.1816m),
            new("LABOUR", 60m),
            new("TRANSPORT", 93.75m)
        ]),
        FormatDefinition.FromMaster("10L", CalculationKind.Packaged, 0.01m, "10L", 10m, 6m,
        [
            new("PALLET", 0.11691176470588235m),
            new("BAG", 1.27824m,
            [
                new("QCD", 0.85824m),
                new("ENCORE", 0.99824m),
                new("PCSS", 0.87824m),
                new("SCHOLLE", 0.91404m),
                new("PINK/UNQ", 1.27824m),
                new("SILVER", 1.27824m),
                new("CMB", 1.06874m)
            ], "PINK/UNQ"),
            new("BOX", 0.7224255m),
            new("LABOUR", 0.4m),
            new("TRANSPORT", 0.88235294117647056m)
        ]),
        FormatDefinition.FromMaster("2X5L", CalculationKind.Packaged, 0.01m, "2X5L", 10m, 6m,
        [
            new("PALLET", 0.1325m),
            new("5L CLOSURE", 0.1724624m),
            new("5L BOTTLE", 0.95916m),
            new("BOX", 0.3726m),
            new("LABOUR", 0.4m),
            new("TRANSPORT", 1m)
        ]),
        FormatDefinition.FromMaster("4X5L", CalculationKind.Packaged, 0.02m, "4X5L", 20m, 6m,
        [
            new("PALLET", 0.265m),
            new("5L CLOSURE", 0.3449248m),
            new("5L BOTTLE", 1.91832m),
            new("BOX", 0.85m),
            new("LABOUR", 0.8m),
            new("TRANSPORT", 2m)
        ]),
        FormatDefinition.FromMaster("6X1L", CalculationKind.Packaged, 0.006m, "6X1L", 6m, 6m,
        [
            new("PALLET", 0.0975m),
            new("1L CLOSURE", 0.10476m),
            new("1L BOTTLE", 1.17156m),
            new("BOX", 0.39231m),
            new("LABOUR", 0.6m),
            new("TRANSPORT", 0.75m),
            new("LABEL", 0.72m)
        ]),
        FormatDefinition.FromMaster("6X2L", CalculationKind.Packaged, 1m / 83.3333333333m, "6X2L", 12m, 6m,
        [
            new("PALLET", 0.22083333333333333m),
            new("6 X 5L CLOSURE", 0.5173872m),
            new("6 X 2L BOTTLES", 2.467656m),
            new("BOX", 0.39m),
            new("LABOUR", 0.72m),
            new("TRANSPORT", 1.7647058823529411m)
        ]),
        FormatDefinition.FromMaster("770ML POUCH", CalculationKind.Packaged, 0.00308m, "4X770ML", 3.08m, 1m,
        [
            new("POUCHES X 4", 0.72m),
            new("BOX", 0.4m),
            new("LABOUR", 0.246m),
            new("PALLET", 0.1m),
            new("TRANSPORT", 0.8m),
            new("INVESTMENT", 0.28m)
        ]),
        FormatDefinition.FromMaster("2X2.25L POUCH", CalculationKind.Packaged, 0.0045m, "2X2.25L", 4.5m, 1m,
        [
            new("PALLET", 0.0795m),
            new("1X INSERT", 0.17m),
            new("2X2.25L POUCH", 1.74508m),
            new("1X BOX", 0.415309m),
            new("LABOUR", 0.72m),
            new("TRANSPORT", 0.6m),
            new("FAT", 1.56m)
        ]),
        FormatDefinition.FromMaster("2X600ML POUCH", CalculationKind.Packaged, 0.0012m, "2X600ML", 1.2m, 1m,
        [
            new("PALLET", 0.0795m),
            new("2X600ML POUCH", 0.86684m),
            new("BOX", 0.3040395m),
            new("LABOUR", 0.72m),
            new("TRANSPORT", 0.12m)
        ]),
        FormatDefinition.FromMaster("12X750ML", CalculationKind.Packaged, 0.009m, "12X750ML", 9m, 6m,
        [
            new("PALLET", 0.17282608695652174m),
            new("CAPS", 0.24m),
            new("770ML BOTTLE", 2.4m),
            new("BOX", 0.56m),
            new("LABOUR", 0.4m),
            new("TRANSPORT", 1m),
            new("LABEL", 1.44m)
        ]),
    ];
    private const string MasterTemplatePath = @"\\adserver2\Company Share\Sales\Quotes\COSTING MASTER TEMPLATE V4.1.2.3.xlsm";
    private const string BundledMasterTemplatePath = "Templates\\costing-template.xlsx";
    private static string QuoteSaveRoot => $@"\\adserver2\Company Share\Sales\Quotes\{DateTime.Today.Year}";

    private readonly QuoteStore _quoteStore = new();
    private string? _quoteNumber;

    public ObservableCollection<QuoteLineViewModel> Lines { get; } = [];
    public ObservableCollection<QuotePreviewLineViewModel> PreviewLines { get; } = [];
    private bool _isRecalculating;
    private bool _recalculateQueued;
    private bool _isInitializing;
    private bool _hasUnsavedChanges;
    private bool _isFillDragging;
    private QuoteLineViewModel? _fillDragSourceLine;
    private QuoteLineViewModel? _fillDragTargetLine;
    private DataGridColumn? _fillDragSourceColumn;
    private bool _isRestoringUndo;
    private bool _isCellEditUndoCaptured;
    // Lists used as stacks (last element = top). List allows cheap removal
    // from index 0 (oldest entry) without rebuilding the whole collection.
    private readonly List<QuoteStateSnapshot> _undoStack = [];
    private readonly List<QuoteStateSnapshot> _redoStack = [];

    public MainWindow()
    {
        _isInitializing = true;
        InitializeComponent();
        DataContext = this;

        AddLine();
        RecalculateQuote();
        _hasUnsavedChanges = false;
        _isInitializing = false;

        try { _quoteStore.Initialise(); }
        catch { /* network unavailable — quote store disabled */ }

        // Restore dark mode preference from last session
        var settings = LoadSettings();
        if (settings.DarkMode)
        {
            _isDarkMode = true;
            ApplyTheme(true);
            DarkModeButton.Content = "☀";
        }
    }

    private void DarkModeToggleClicked(object sender, RoutedEventArgs e)
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme(_isDarkMode);
        DarkModeButton.Content = _isDarkMode ? "☀" : "☾";
        SaveSettings(new AppSettings(_isDarkMode));
    }

    private void ApplyTheme(bool dark)
    {
        // Replace the brush object entirely — mutating .Color fails when WPF
        // has frozen the brush after first render.
        void Set(string key, string hex) =>
            Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        if (dark)
        {
            // Backgrounds — dark navy family
            Set("AppBg",          "#111927");
            Set("HeaderBg",       "#18222e");
            Set("ToolbarBrush",   "#1c2738");
            Set("PanelBrush",     "#1c2738");
            Set("SoftBrush",      "#18222e");
            Set("LineBrush",      "#2a3a50");
            Set("GpCardBg",       "#18222e");
            Set("RowHoverBrush",  "#22334a");
            Set("RowSelectBrush", "#1a3d5c");
            Set("TextBoxBg",      "#22334a");
            Set("TextBoxBorder",  "#8CBECF");   // accent border on inputs
            // Text — two colours only: near-white for primary, pale blue for secondary
            Set("InkBrush",       "#eaf2f8");   // primary text
            Set("TitleFg",        "#eaf2f8");   // headings — same as body
            Set("MutedBrush",     "#8CBECF");   // secondary text = accent colour
            Set("GpLabelFg",      "#eaf2f8");
            Set("GpSubFg",        "#8CBECF");
            // Accent elements — brand pale blue, navy text on top
            Set("AccentBrush",    "#8CBECF");
            Set("AccentFg",       "#052A61");
            LogoColor.Visibility = Visibility.Collapsed;
            LogoWhite.Visibility = Visibility.Visible;
            SetTitleBarColour(DarkNavyBgr);
        }
        else
        {
            Set("AppBg",          "#eef4f7");
            Set("HeaderBg",       "#ffffff");
            Set("ToolbarBrush",   "#f2f7fa");
            Set("PanelBrush",     "#ffffff");
            Set("SoftBrush",      "#f7fbfc");
            Set("LineBrush",      "#cfd8df");
            Set("InkBrush",       "#172026");
            Set("MutedBrush",     "#64707a");
            Set("RowHoverBrush",  "#edf3f7");
            Set("RowSelectBrush", "#d4e8f2");
            Set("TextBoxBg",      "#ffffff");
            Set("TextBoxBorder",  "#8a9fad");
            Set("TitleFg",        "#052A61");
            Set("GpLabelFg",      "#394b59");
            Set("GpSubFg",        "#596775");
            Set("GpCardBg",       "#d8dee6");
            Set("AccentBrush",    "#052A61");
            Set("AccentFg",       "#ffffff");
            LogoColor.Visibility = Visibility.Visible;
            LogoWhite.Visibility = Visibility.Collapsed;
            SetTitleBarColour(NavyBgr);
        }

        // Re-run to repaint the GP display with correct ink colour for new theme
        UpdateAverageGp();
    }

    private void HeaderInputChanged(object sender, TextChangedEventArgs e)
    {
        MarkDirty();
        RecalculateQuote();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_hasUnsavedChanges)
        {
            base.OnClosing(e);
            return;
        }

        var result = MessageBox.Show(
            this,
            "You have unsaved quote changes. Export the customer PDF before closing?",
            "Save quote before closing",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (result == MessageBoxResult.Yes && !TryExportPdf())
        {
            e.Cancel = true;
            return;
        }

        base.OnClosing(e);
    }

    private void WindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Let a TextBox with its own undo history handle Ctrl+Z / Ctrl+Y itself.
        if (Keyboard.FocusedElement is TextBox { CanUndo: true })
            return;

        if (Keyboard.Modifiers != ModifierKeys.Control)
            return;

        if (e.Key == Key.Z)
        {
            if (TryUndo()) e.Handled = true;
        }
        else if (e.Key == Key.Y)
        {
            if (TryRedo()) e.Handled = true;
        }
    }

    private void AddLineClicked(object sender, RoutedEventArgs e)
    {
        PushUndoSnapshot();
        AddLine();
        MarkDirty();
    }

    private void RemoveSelectedClicked(object sender, RoutedEventArgs e)
    {
        var selected = Lines.Where(line => line.IsMarkedForDelete).ToList();
        if (selected.Count == 0)
        {
            selected = QuoteGrid.SelectedItems.Cast<QuoteLineViewModel>().ToList();
        }

        if (selected.Count > 0)
        {
            PushUndoSnapshot();
        }

        foreach (var line in selected)
        {
            Lines.Remove(line);
        }

        if (Lines.Count == 0)
        {
            AddLine();
        }

        RecalculateQuote();
        MarkDirty();
    }

    private void ResetClicked(object sender, RoutedEventArgs e)
    {
        if (Lines.Any(l => l.HasCalculation) || !string.IsNullOrWhiteSpace(CustomerBox.Text))
        {
            var result = MessageBox.Show(
                this,
                "This will clear all lines and the customer name. Are you sure?",
                "Reset quote",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
        }

        PushUndoSnapshot();
        _quoteNumber = null;
        UpdateQuoteNumberDisplay();
        CustomerBox.Text = string.Empty;
        Lines.Clear();
        AddLine();
        RecalculateQuote();
        MarkDirty();
    }

    private void SaveQuoteClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var state = CaptureQuoteState();
            _quoteNumber = _quoteStore.Save(_quoteNumber, state);
            _hasUnsavedChanges = false;
            UpdateQuoteNumberDisplay();
            SetStatus($"Quote saved: {_quoteNumber}");
        }
        catch (Exception ex)
        {
            SetStatus(NetworkMessage(ex), isError: true);
        }
    }

    private void OpenQuoteClicked(object sender, RoutedEventArgs e)
    {
        if (!_quoteStore.IsAvailable())
        {
            SetStatus(@"Cannot reach the quote store on \\adserver2. Check you are connected to the office network, then try again.", isError: true);
            return;
        }

        var dialog = new OpenQuoteWindow(_quoteStore) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedQuoteNumber is null)
            return;

        try
        {
            var entry = _quoteStore.Load(dialog.SelectedQuoteNumber);
            if (entry is null)
            {
                SetStatus($"Quote {dialog.SelectedQuoteNumber} not found.", isError: true);
                return;
            }

            PushUndoSnapshot();
            _quoteNumber = entry.QuoteNumber;
            RestoreUndoSnapshot(entry.State);
            _hasUnsavedChanges = false;
            UpdateQuoteNumberDisplay();
            SetStatus($"Opened quote: {_quoteNumber}");
        }
        catch (Exception ex)
        {
            SetStatus(NetworkMessage(ex), isError: true);
        }
    }

    private void UpdateQuoteNumberDisplay()
    {
        QuoteNumberDisplay.Text = _quoteNumber ?? "Not saved";
        QuoteNumberDisplay.FontStyle = _quoteNumber is null ? FontStyles.Italic : FontStyles.Normal;
        QuoteNumberDisplay.Foreground = _quoteNumber is null
            ? (Brush)FindResource("MutedBrush")
            : new SolidColorBrush(Color.FromRgb(23, 32, 38));
        Title = _quoteNumber is null
            ? "Simpsons Beverages Quoting Tool"
            : $"Simpsons Beverages Quoting Tool — {_quoteNumber}";
    }

    private void ExportPdfClicked(object sender, RoutedEventArgs e)
    {
        TryExportPdf();
    }

    private bool TryExportPdf()
    {
        RecalculateQuote();
        var quote = BuildPdfModel();

        if (quote.Lines.Count == 0)
        {
            SetStatus("Add at least one valid quote line before exporting.", isError: true);
            return false;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export customer quote",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = BuildPdfFileName(quote)
        };

        if (dialog.ShowDialog(this) != true)
        {
            return false;
        }

        SimplePdfExporter.Export(dialog.FileName, quote);
        SetStatus($"PDF exported: {dialog.FileName}");
        _hasUnsavedChanges = false;
        return true;
    }

    private void ImportLegacyExcelClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import legacy costing workbook",
            Filter = "Excel costing workbooks (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var imported = LegacyCostingWorkbookIo.Import(dialog.FileName);
            if (imported.Count == 0)
            {
                StatusText.Text = "No quote lines found in the selected costing workbook.";
                StatusText.Foreground = Brushes.Firebrick;
                return;
            }

            PushUndoSnapshot();
            LoadImportedLines(imported);
            SetStatus($"Imported {imported.Count} line(s): {dialog.FileName}");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void ImportLegacySheetClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Import a specific legacy costing tab",
            Filter = "Excel costing workbooks (*.xlsx;*.xlsm)|*.xlsx;*.xlsm|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var sheetNames = LegacyCostingWorkbookIo.GetSheetNames(dialog.FileName);
            var formatNames = LegacyCostingWorkbookIo.GetImportFormatNames();
            var picker = new LegacyImportSheetWindow(sheetNames, formatNames)
            {
                Owner = this
            };

            if (picker.ShowDialog() != true)
            {
                return;
            }

            var imported = LegacyCostingWorkbookIo.ImportSheet(dialog.FileName, picker.SelectedSheetName, picker.SelectedFormatName);
            if (imported.Count == 0)
            {
                SetStatus($"No quote lines found in tab '{picker.SelectedSheetName}'.", isError: true);
                return;
            }

            PushUndoSnapshot();
            LoadImportedLines(imported);
            SetStatus($"Imported {imported.Count} line(s) from '{picker.SelectedSheetName}' as {picker.SelectedFormatName}.");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void ExportLegacyExcelClicked(object sender, RoutedEventArgs e)
    {
        RecalculateQuote();
        var validLines = Lines.Where(line => line.HasCalculation).ToList();
        if (validLines.Count == 0)
        {
            SetStatus("Add at least one valid quote line before exporting Excel.", isError: true);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export legacy costing workbook",
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            DefaultExt = ".xlsx",
            AddExtension = true,
            InitialDirectory = ResolveLegacyExportInitialDirectory(),
            FileName = BuildLegacyWorkbookFileName()
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            LegacyCostingWorkbookIo.Export(ResolveLegacyExportTemplatePath(), dialog.FileName, validLines);
            SetStatus($"Excel exported: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void ServeCostSettingsClicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not QuoteLineViewModel line)
        {
            return;
        }

        var dialog = new ServeCostSettingsWindow(line)
        {
            Owner = this
        };

        var undoSnapshot = CaptureQuoteState();
        if (dialog.ShowDialog() == true)
        {
            PushUndoSnapshot(undoSnapshot);
            RecalculateQuote();
            MarkDirty();
        }
    }

    private void QuoteGridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        _isCellEditUndoCaptured = false;
        QueueRecalculate();
    }

    private void QuoteGridCellPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGridCell cell ||
            cell.IsReadOnly)
        {
            return;
        }

        if (IsFillHandleHit(cell, e.GetPosition(cell)) &&
            cell.DataContext is QuoteLineViewModel fillSourceLine &&
            CanFillColumn(cell.Column))
        {
            _isFillDragging = true;
            _fillDragSourceLine = fillSourceLine;
            _fillDragTargetLine = fillSourceLine;
            _fillDragSourceColumn = cell.Column;
            cell.CaptureMouse();
            e.Handled = true;
            return;
        }

        if (cell.Column == PackCostColumn)
        {
            if (e.ClickCount == 2 &&
                cell.DataContext is QuoteLineViewModel line &&
                line.HasPackCostBreakdown)
            {
                e.Handled = true;
                OpenPackCostBreakdown(line);
            }

            return;
        }

        var isTextCell = cell.Column is DataGridTextColumn;
        if (!isTextCell)
        {
            return;
        }

        if (e.ClickCount > 1)
        {
            QuoteGrid.CurrentCell = new DataGridCellInfo(cell);
            QuoteGrid.BeginEdit();
            Dispatcher.BeginInvoke(() =>
            {
                if (FindVisualChild<TextBox>(cell) is { } textBox)
                {
                    textBox.Focus();
                    textBox.SelectionLength = 0;
                    var point = Mouse.GetPosition(textBox);
                    var characterIndex = textBox.GetCharacterIndexFromPoint(point, snapToText: true);
                    if (characterIndex >= 0)
                    {
                        var bounds = textBox.GetRectFromCharacterIndex(characterIndex);
                        if (!bounds.IsEmpty && point.X > bounds.X + bounds.Width / 2d)
                        {
                            characterIndex++;
                        }
                    }

                    textBox.CaretIndex = Math.Clamp(characterIndex < 0 ? textBox.Text.Length : characterIndex, 0, textBox.Text.Length);
                }
            });
            return;
        }

        e.Handled = true;
        if (!cell.IsFocused)
        {
            cell.Focus();
        }

        QuoteGrid.CurrentCell = new DataGridCellInfo(cell);
        QuoteGrid.BeginEdit();
        Dispatcher.BeginInvoke(() =>
        {
            if (FindVisualChild<TextBox>(cell) is { } textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        });
    }

    private void QuoteGridCellPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not DataGridCell cell)
        {
            return;
        }

        if (_isFillDragging)
        {
            _fillDragTargetLine = GetLineUnderMouse() ?? _fillDragTargetLine;
            e.Handled = true;
            return;
        }

        cell.Cursor = CanFillColumn(cell.Column) && IsFillHandleHit(cell, e.GetPosition(cell))
            ? Cursors.Cross
            : null;
    }

    private void QuoteGridCellPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isFillDragging)
        {
            return;
        }

        if (sender is DataGridCell cell && cell.IsMouseCaptured)
        {
            cell.ReleaseMouseCapture();
        }

        try
        {
            ApplyFillDrag();
        }
        finally
        {
            _isFillDragging = false;
            _fillDragSourceLine = null;
            _fillDragTargetLine = null;
            _fillDragSourceColumn = null;
            e.Handled = true;
        }
    }

    private void MoveLineUpClicked(object sender, RoutedEventArgs e)
    {
        if (QuoteGrid.SelectedItem is not QuoteLineViewModel line) return;
        var idx = Lines.IndexOf(line);
        if (idx <= 0) return;
        PushUndoSnapshot();
        Lines.Move(idx, idx - 1);
        QuoteGrid.SelectedItem = line;
        QuoteGrid.ScrollIntoView(line);
        RenumberLines();
        MarkDirty();
    }

    private void MoveLineDownClicked(object sender, RoutedEventArgs e)
    {
        if (QuoteGrid.SelectedItem is not QuoteLineViewModel line) return;
        var idx = Lines.IndexOf(line);
        if (idx < 0 || idx >= Lines.Count - 1) return;
        PushUndoSnapshot();
        Lines.Move(idx, idx + 1);
        QuoteGrid.SelectedItem = line;
        QuoteGrid.ScrollIntoView(line);
        RenumberLines();
        MarkDirty();
    }

    private void RenumberLines()
    {
        for (var i = 0; i < Lines.Count; i++)
            Lines[i].LineNumber = i + 1;
    }

    private void FormatComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox comboBox)
            return;

        comboBox.ItemsSource = _formats.Select(format => format.Name).ToList();
        comboBox.Focus();
        Dispatcher.BeginInvoke(() => comboBox.IsDropDownOpen = true);
    }

    private void QuoteGridBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        if (!_isCellEditUndoCaptured && e.Column != PackCostColumn)
        {
            PushUndoSnapshot();
            _isCellEditUndoCaptured = true;
        }

        if (e.Column != PackCostColumn || e.Row.Item is not QuoteLineViewModel line)
        {
            return;
        }

        if (line.HasPackCostBreakdown)
        {
            e.Cancel = true;
        }
    }

    private void QuoteGridCurrentCellChanged(object sender, EventArgs e)
    {
        QueueRecalculate();
    }

    private void QuoteGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is not TextBox ||
            e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down) ||
            QuoteGrid.CurrentCell.Item is not QuoteLineViewModel currentLine ||
            QuoteGrid.CurrentCell.Column is not { } currentColumn)
        {
            return;
        }

        var currentRow = Lines.IndexOf(currentLine);
        var currentColumnIndex = QuoteGrid.Columns.IndexOf(currentColumn);
        if (currentRow < 0 || currentColumnIndex < 0)
        {
            return;
        }

        var targetRow = currentRow;
        var targetColumnIndex = currentColumnIndex;
        switch (e.Key)
        {
            case Key.Left:
                targetColumnIndex--;
                break;
            case Key.Right:
                targetColumnIndex++;
                break;
            case Key.Up:
                targetRow--;
                break;
            case Key.Down:
                targetRow++;
                break;
        }

        if (targetRow < 0 || targetRow >= Lines.Count)
        {
            return;
        }

        var targetColumn = FindEditableNavigationColumn(targetColumnIndex, e.Key is Key.Left ? -1 : 1);
        if (targetColumn is null)
        {
            return;
        }

        e.Handled = true;
        QuoteGrid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
        QuoteGrid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true);

        var targetLine = Lines[targetRow];
        QuoteGrid.CurrentCell = new DataGridCellInfo(targetLine, targetColumn);
        QuoteGrid.SelectedItem = targetLine;
        QuoteGrid.ScrollIntoView(targetLine, targetColumn);
        Dispatcher.BeginInvoke(() =>
        {
            QuoteGrid.BeginEdit();
            if (TryFindCell(targetLine, targetColumn) is { } cell &&
                FindVisualChild<TextBox>(cell) is { } textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        });
    }

    private void OpenPackCostBreakdown(QuoteLineViewModel line)
    {
        var dialog = new PackCostBreakdownWindow(line)
        {
            Owner = this
        };

        var undoSnapshot = CaptureQuoteState();
        if (dialog.ShowDialog() == true)
        {
            PushUndoSnapshot(undoSnapshot);
            line.ApplyPackCostBreakdown(dialog.Components);
            if (dialog.ApplyToAllMatchingFormats)
            {
                foreach (var matchingLine in Lines.Where(item => !ReferenceEquals(item, line) && item.FormatName == line.FormatName && item.HasPackCostBreakdown))
                {
                    matchingLine.ApplyPackCostBreakdown(dialog.Components);
                }
            }

            RecalculateQuote();
            MarkDirty();
        }
    }

    private void LoadImportedLines(IReadOnlyList<LegacyCostingQuoteLine> imported)
    {
        Lines.Clear();
        foreach (var importedLine in imported)
        {
            var line = new QuoteLineViewModel(_formats);
            line.LoadFromLegacy(importedLine);
            AttachLine(line);
            Lines.Add(line);
        }

        RecalculateQuote();
        MarkDirty();
    }

    private void AddLine()
    {
        var line = new QuoteLineViewModel(_formats);
        if (Lines.LastOrDefault() is { } previousLine)
        {
            line.FormatName = previousLine.FormatName;
        }

        AttachLine(line);
        Lines.Add(line);

        // Scroll the grid so the new (bottom) row is visible.
        Dispatcher.BeginInvoke(() => QuoteGrid.ScrollIntoView(line));
    }

    // Select all text whenever a DataGrid editing TextBox gains focus so that
    // typing immediately overwrites the current value rather than inserting into it.
    private void QuoteGridTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            Dispatcher.BeginInvoke(() => tb.SelectAll());
    }

    private void AttachLine(QuoteLineViewModel line)
    {
        line.PropertyChanged += (_, args) =>
        {
            if (QuoteLineViewModel.CalculatedPropertyNames.Contains(args.PropertyName))
            {
                return;
            }

            MarkDirty();
            QueueRecalculate();
        };
    }

    private void MarkDirty()
    {
        if (!_isInitializing && !_isRestoringUndo)
        {
            _hasUnsavedChanges = true;
        }
    }

    private QuoteStateSnapshot CaptureQuoteState()
    {
        return new QuoteStateSnapshot(
            CustomerBox.Text,
            Lines.Select(QuoteLineSnapshot.FromLine).ToList());
    }

    private void PushUndoSnapshot()
    {
        PushUndoSnapshot(CaptureQuoteState());
    }

    private void PushUndoSnapshot(QuoteStateSnapshot snapshot)
    {
        _redoStack.Clear();
        _undoStack.Add(snapshot);
        if (_undoStack.Count > 50)
            _undoStack.RemoveAt(0); // drop oldest entry
    }

    private bool TryUndo()
    {
        if (_undoStack.Count == 0) return false;
        _redoStack.Add(CaptureQuoteState());
        RestoreUndoSnapshot(_undoStack[^1]);
        _undoStack.RemoveAt(_undoStack.Count - 1);
        return true;
    }

    private bool TryRedo()
    {
        if (_redoStack.Count == 0) return false;
        _undoStack.Add(CaptureQuoteState());
        RestoreUndoSnapshot(_redoStack[^1]);
        _redoStack.RemoveAt(_redoStack.Count - 1);
        return true;
    }

    private void RestoreUndoSnapshot(QuoteStateSnapshot snapshot)
    {
        _isRestoringUndo = true;
        try
        {
            CustomerBox.Text = snapshot.Customer;
            Lines.Clear();
            foreach (var lineSnapshot in snapshot.Lines)
            {
                var line = new QuoteLineViewModel(_formats);
                lineSnapshot.ApplyTo(line);
                AttachLine(line);
                Lines.Add(line);
            }

            if (Lines.Count == 0)
            {
                AddLine();
            }

            _isCellEditUndoCaptured = false;
            RecalculateQuote();
            _hasUnsavedChanges = true;
        }
        finally
        {
            _isRestoringUndo = false;
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static T? FindVisualParent<T>(DependencyObject? child)
        where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
            {
                return match;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private static bool IsFillHandleHit(DataGridCell cell, Point point)
    {
        const double handleSize = 14d;
        return point.X >= cell.ActualWidth - handleSize &&
               point.Y >= cell.ActualHeight - handleSize;
    }

    private bool CanFillColumn(DataGridColumn column)
    {
        return column == FormatColumn ||
               column == CodeColumn ||
               column == DescriptionColumn ||
               column == DilutionColumn ||
               column == TargetGpColumn ||
               column == PackCostColumn ||
               column == PricePerUnitColumn ||
               column == RecipeCostColumn ||
               column == ServeCostColumn;
    }

    private DataGridColumn? FindEditableNavigationColumn(int startIndex, int direction)
    {
        for (var index = startIndex; index >= 0 && index < QuoteGrid.Columns.Count; index += direction)
        {
            var column = QuoteGrid.Columns[index];
            if (!column.IsReadOnly && CanFillColumn(column) && column != ServeCostColumn)
            {
                return column;
            }
        }

        return null;
    }

    private DataGridCell? TryFindCell(QuoteLineViewModel line, DataGridColumn column)
    {
        if (QuoteGrid.ItemContainerGenerator.ContainerFromItem(line) is not DataGridRow row)
        {
            return null;
        }

        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
        return presenter?.ItemContainerGenerator.ContainerFromIndex(QuoteGrid.Columns.IndexOf(column)) as DataGridCell;
    }

    private QuoteLineViewModel? GetLineUnderMouse()
    {
        var hit = VisualTreeHelper.HitTest(QuoteGrid, Mouse.GetPosition(QuoteGrid));
        return FindVisualParent<DataGridRow>(hit?.VisualHit)?.Item as QuoteLineViewModel;
    }

    private void ApplyFillDrag()
    {
        if (_fillDragSourceLine is null || _fillDragSourceColumn is null || _fillDragTargetLine is null)
        {
            return;
        }

        var sourceIndex = Lines.IndexOf(_fillDragSourceLine);
        var targetIndex = Lines.IndexOf(_fillDragTargetLine);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex)
        {
            return;
        }

        PushUndoSnapshot();
        var start = Math.Min(sourceIndex, targetIndex);
        var end = Math.Max(sourceIndex, targetIndex);
        for (var index = start; index <= end; index++)
        {
            var line = Lines[index];
            if (ReferenceEquals(line, _fillDragSourceLine))
            {
                continue;
            }

            CopyColumnValue(_fillDragSourceColumn, _fillDragSourceLine, line);
        }

        RecalculateQuote();
        MarkDirty();
    }

    private void CopyColumnValue(DataGridColumn column, QuoteLineViewModel source, QuoteLineViewModel target)
    {
        if (column == FormatColumn)
        {
            target.FormatName = source.FormatName;
        }
        else if (column == CodeColumn)
        {
            target.Code = source.Code;
        }
        else if (column == DescriptionColumn)
        {
            target.Description = source.Description;
        }
        else if (column == DilutionColumn)
        {
            target.DilutionParts = source.DilutionParts;
        }
        else if (column == TargetGpColumn)
        {
            target.TargetGpPercent = source.TargetGpPercent;
        }
        else if (column == PackCostColumn)
        {
            target.PackCost = source.PackCost;
            target.CopyPackCostBreakdownFrom(source);
        }
        else if (column == PricePerUnitColumn)
        {
            target.PricePerUnitText = source.PricePerUnitText;
        }
        else if (column == RecipeCostColumn)
        {
            target.RecipeCostPer1000L = source.RecipeCostPer1000L;
        }
        else if (column == ServeCostColumn)
        {
            target.CopyServeCostFrom(source);
        }
    }

    private void QueueRecalculate()
    {
        if (_isRecalculating || _recalculateQueued)
        {
            return;
        }

        _recalculateQueued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _recalculateQueued = false;
            RecalculateQuote();
        });
    }

    private void RecalculateQuote()
    {
        if (_isRecalculating)
        {
            return;
        }

        try
        {
            _isRecalculating = true;
            foreach (var line in Lines)
            {
                line.Calculate();
            }

            UpdateUsagePriceColumn();
            UpdateAverageGp();
            UpdatePreview();
            RenumberLines();
            SetStatus("Calculated");
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
        finally
        {
            _isRecalculating = false;
        }
    }

    private void SetStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        var colour = isError
            ? Color.FromRgb(176, 60, 60)
            : Color.FromRgb(89, 103, 117);
        StatusText.Foreground = new SolidColorBrush(colour);
        StatusDot.Fill = new SolidColorBrush(isError
            ? Color.FromRgb(176, 60, 60)
            : Color.FromRgb(89, 103, 117));
    }

    // Produces a friendly message for network/IO failures so users
    // see "check your connection" rather than a raw exception string.
    private static string NetworkMessage(Exception ex)
    {
        if (ex is IOException || ex.InnerException is IOException)
            return $@"Cannot reach the quote store on \\adserver2. Check you are connected to the office network, then try again.";
        return $"Operation failed — {ex.Message}";
    }

    private void UpdateAverageGp()
    {
        var calculatedLines = Lines.Where(line => line.HasCalculation).ToList();
        if (calculatedLines.Count == 0)
        {
            AverageGpDisplay.Text = "—";
            AverageGpDisplay.Foreground = (Brush)FindResource("MutedBrush");
            GpSubtitle.Text = "Add lines to calculate";
            GpIndicator.Background = (Brush)FindResource("GpCardBg");
            return;
        }

        var averageGp = calculatedLines.Average(line => (double)line.TargetGpPercent) / 100d;
        // Use a compact format so the badge doesn't overflow at high values
        AverageGpDisplay.Text = averageGp >= 1d
            ? "100%"
            : averageGp.ToString("P1", CultureInfo.GetCultureInfo("en-GB"));
        AverageGpDisplay.Foreground = (Brush)FindResource("InkBrush");
        GpSubtitle.Text = $"Across {calculatedLines.Count} line{(calculatedLines.Count == 1 ? "" : "s")}";

        var normalized = Math.Clamp(averageGp, 0d, 0.7d) / 0.7d;
        var red = (byte)(210 - (120 * normalized));
        var green = (byte)(70 + (100 * normalized));
        var blue = (byte)(65 + (25 * normalized));
        GpIndicator.Background = new SolidColorBrush(Color.FromRgb(red, green, blue));
    }

    private void UpdateUsagePriceColumn()
    {
        var calculatedLines = Lines.Where(line => line.HasCalculation).ToList();
        UsagePriceColumn.Header = calculatedLines.Count > 0 &&
                                  calculatedLines.All(line => string.Equals(line.FormatName, "6X1L", StringComparison.OrdinalIgnoreCase))
            ? "£/L BOTTLE"
            : "Cost in use (£/RTD L)";
    }

    private void UpdatePreview()
    {
        var quote = BuildPdfModel();
        PreviewCustomerText.Text = string.IsNullOrWhiteSpace(quote.Customer) ? "-" : quote.Customer;
        PreviewDateText.Text = DateTime.Today.ToString("dd/MM/yyyy");
        PreviewUsagePriceColumn.Header = quote.UsagePriceHeader;
        PreviewServeCostColumn.Visibility = quote.HasServeCostColumn ? Visibility.Visible : Visibility.Collapsed;
        PreviewServeCostColumn.Header = quote.ServeCostHeader;

        PreviewLines.Clear();
        foreach (var line in quote.Lines)
        {
            PreviewLines.Add(new QuotePreviewLineViewModel(
                CleanPreviewCode(line.Code),
                line.Description,
                line.Unit,
                line.PricePerUnit,
                line.RtdPricePerLitre,
                line.ServeCost));
        }
    }

    private QuotePdfModel BuildPdfModel()
    {
        var lines = Lines.Where(line => line.HasCalculation).ToList();
        return new QuotePdfModel(
            CustomerBox.Text.Trim(),
            _quoteNumber,
            BuildUsagePriceHeader(lines),
            lines.Any(line => line.IncludeServeCost && !string.IsNullOrWhiteSpace(line.ServeCostDisplay)),
            BuildServeCostHeader(lines),
            lines
                .Select(line => new QuotePdfLine(
                    line.Code,
                    line.Description,
                    line.CustomerUnit,
                    QuoteMoney(line.CustomerPricePerUnit),
                    BuildCostInUseDisplay(line),
                    line.IncludeServeCost ? line.ServeCostDisplay : string.Empty,
                    Percent(line.TargetGpPercent / 100m)))
                .ToList());
    }

    private static string BuildCostInUseDisplay(QuoteLineViewModel line)
    {
        if (string.Equals(line.FormatName, "6X1L", StringComparison.OrdinalIgnoreCase))
        {
            return Money(line.CustomerPricePerUnit / 6m);
        }

        return QuoteLineViewModel.ShowsCostInUse(line.FormatName) && line.RtdPricePerLitre > 0m
            ? Money(line.RtdPricePerLitre)
            : string.Empty;
    }

    private static string BuildUsagePriceHeader(IReadOnlyList<QuoteLineViewModel> lines)
    {
        return lines.Count > 0 && lines.All(line => string.Equals(line.FormatName, "6X1L", StringComparison.OrdinalIgnoreCase))
            ? "\u00A3/L BOTTLE"
            : "\u00A3/RTD L";
    }

    private static string BuildServeCostHeader(IReadOnlyList<QuoteLineViewModel> lines)
    {
        var enabledLines = lines
            .Where(line => line.IncludeServeCost && !string.IsNullOrWhiteSpace(line.ServeCostDisplay) && line.ServeMl is > 0m)
            .ToList();

        if (enabledLines.Count == 0)
        {
            return "\u00A3/SERVE";
        }

        var first = enabledLines[0];
        var sameModeAndMl = enabledLines.All(line =>
            line.ServeCostIsRtd == first.ServeCostIsRtd &&
            line.ServeMl == first.ServeMl);

        if (!sameModeAndMl)
        {
            return "\u00A3/SERVE";
        }

        var mode = first.ServeCostIsRtd ? "RTD" : "CONC";
        return $"\u00A3/{mode} {first.ServeMl!.Value:N0}ml";
    }

    private static string QuoteMoney(decimal value)
    {
        return value.ToString("C2", CultureInfo.GetCultureInfo("en-GB"));
    }

    private static string CleanPreviewCode(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Trim().Replace("\\", "/", StringComparison.Ordinal);
    }

    private static string Money(decimal value)
    {
        return value.ToString("C4", CultureInfo.GetCultureInfo("en-GB"));
    }

    private static string Percent(decimal value)
    {
        return value.ToString("P2", CultureInfo.GetCultureInfo("en-GB"));
    }

    private static string BuildPdfFileName(QuotePdfModel quote)
    {
        var customer = CleanFileName(string.IsNullOrWhiteSpace(quote.Customer) ? "Customer" : quote.Customer);
        return $"{DateTime.Today:yyyy-MM-dd} {customer} Quote.pdf";
    }

    private string BuildLegacyWorkbookFileName()
    {
        var customer = CleanFileName(string.IsNullOrWhiteSpace(CustomerBox.Text) ? "Customer" : CustomerBox.Text);
        return $"{DateTime.Today:yyyy-MM-dd} {customer} Costing.xlsx";
    }

    private static string ResolveLegacyExportTemplatePath()
    {
        var bundledPath = Path.Combine(AppContext.BaseDirectory, BundledMasterTemplatePath);
        if (File.Exists(bundledPath))
        {
            return bundledPath;
        }

        if (File.Exists(MasterTemplatePath))
        {
            return MasterTemplatePath;
        }

        throw new FileNotFoundException("Costing template not found in the installed app or on the network.", bundledPath);
    }

    private string? ResolveLegacyExportInitialDirectory()
    {
        if (!Directory.Exists(QuoteSaveRoot))
        {
            return null;
        }

        var customer = CleanFileName(CustomerBox.Text);
        if (string.IsNullOrWhiteSpace(customer))
        {
            return QuoteSaveRoot;
        }

        var customerDirectory = Path.Combine(QuoteSaveRoot, customer);
        try
        {
            Directory.CreateDirectory(customerDirectory);
            return customerDirectory;
        }
        catch
        {
            return QuoteSaveRoot;
        }
    }

    private static string CleanFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(character => invalidChars.Contains(character) ? ' ' : character).ToArray());
        return string.Join(" ", cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)).Trim();
    }
}

public sealed class QuoteLineViewModel : INotifyPropertyChanged
{
    private static readonly IReadOnlySet<string> CostInUseFormatNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "TOSCA",
        "IBC",
        "220L DRUM",
        "25L",
        "2X5L",
        "4X5L",
        "10L"
    };

    public static readonly IReadOnlySet<string?> CalculatedPropertyNames = new HashSet<string?>
    {
        nameof(RtdPricePerLitre),
        nameof(LiquidCost),
        nameof(PackedCost),
        nameof(CustomerUnit),
        nameof(CustomerPricePerUnit),
        nameof(PricePerUnitText),
        nameof(UsagePriceDisplay),
        nameof(ServeCostDisplay),
        nameof(ServeCostSummary),
        nameof(HasCalculation),
        nameof(LineNumber)   // set by MainWindow.RenumberLines, not user input
    };

    public static bool ShowsCostInUse(string formatName)
    {
        return CostInUseFormatNames.Contains(formatName);
    }

    private readonly IReadOnlyList<FormatDefinition> _formats;
    private string _formatName = string.Empty;
    private string _code = string.Empty;
    private string _description = string.Empty;
    private bool _isMarkedForDelete;
    private decimal _recipeCostPer1000L;
    private decimal _packCost;
    private decimal _targetGpPercent = 50m;
    private decimal? _dilutionParts;
    private decimal _pricePerUnit;
    private bool _isPriceBlank;
    private decimal _rtdPricePerLitre;
    private decimal _liquidCost;
    private decimal _packedCost;
    private bool _includeServeCost;
    private bool _serveCostIsRtd = true;
    private decimal? _serveMl;
    private decimal _serveCost;
    private QuotePriceDriver _priceDriver = QuotePriceDriver.GrossProfit;
    private bool _isCalculating;
    private int _lineNumber;

    public QuoteLineViewModel(IReadOnlyList<FormatDefinition> formats)
    {
        _formats = formats;
        FormatName = "IBC";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int LineNumber
    {
        get => _lineNumber;
        set => SetField(ref _lineNumber, value);
    }

    public bool IsMarkedForDelete
    {
        get => _isMarkedForDelete;
        set => SetField(ref _isMarkedForDelete, value);
    }

    public string FormatName
    {
        get => _formatName;
        set
        {
            if (SetField(ref _formatName, value))
            {
                ApplyFormatDefaults();
                OnPropertyChanged(nameof(CustomerUnit));
            }
        }
    }

    public string Code
    {
        get => _code;
        set => SetField(ref _code, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public decimal RecipeCostPer1000L
    {
        get => _recipeCostPer1000L;
        set => SetField(ref _recipeCostPer1000L, value);
    }

    public decimal PackCost
    {
        get => _packCost;
        set => SetField(ref _packCost, value);
    }

    public decimal TargetGpPercent
    {
        get => _targetGpPercent;
        set
        {
            if (SetField(ref _targetGpPercent, value) && !_isCalculating)
            {
                _priceDriver = QuotePriceDriver.GrossProfit;
            }
        }
    }

    public decimal? DilutionParts
    {
        get => _dilutionParts;
        set => SetField(ref _dilutionParts, value);
    }

    public decimal PricePerUnit
    {
        get => _pricePerUnit;
        set
        {
            if (SetField(ref _pricePerUnit, value))
            {
                _isPriceBlank = false;
                OnPropertyChanged(nameof(PricePerUnitText));
                OnPropertyChanged(nameof(CustomerPricePerUnit));
                OnPropertyChanged(nameof(HasCalculation));

                if (!_isCalculating)
                {
                    _priceDriver = QuotePriceDriver.UnitPrice;
                }
            }
        }
    }

    public string PricePerUnitText
    {
        get => _isPriceBlank ? string.Empty : PricePerUnit.ToString("N4", CultureInfo.GetCultureInfo("en-GB"));
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _isPriceBlank = true;
                if (SetField(ref _pricePerUnit, 0m, nameof(PricePerUnit)))
                {
                    OnPropertyChanged(nameof(UsagePriceDisplay));
                }

                _priceDriver = QuotePriceDriver.UnitPrice;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomerPricePerUnit));
                OnPropertyChanged(nameof(HasCalculation));
                return;
            }

            if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.GetCultureInfo("en-GB"), out var parsed) ||
                decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out parsed))
            {
                PricePerUnit = parsed;
                _isPriceBlank = false;
                _priceDriver = QuotePriceDriver.UnitPrice;
                OnPropertyChanged();
            }
        }
    }

    public decimal RtdPricePerLitre
    {
        get => _rtdPricePerLitre;
        private set => SetField(ref _rtdPricePerLitre, value);
    }

    public bool IncludeServeCost
    {
        get => _includeServeCost;
        set
        {
            if (SetField(ref _includeServeCost, value))
            {
                OnPropertyChanged(nameof(ServeCostDisplay));
                OnPropertyChanged(nameof(ServeCostSummary));
            }
        }
    }

    public bool ServeCostIsRtd
    {
        get => _serveCostIsRtd;
        set
        {
            if (SetField(ref _serveCostIsRtd, value))
            {
                OnPropertyChanged(nameof(ServeCostDisplay));
                OnPropertyChanged(nameof(ServeCostSummary));
            }
        }
    }

    public decimal? ServeMl
    {
        get => _serveMl;
        set
        {
            if (SetField(ref _serveMl, value))
            {
                OnPropertyChanged(nameof(ServeMlText));
                OnPropertyChanged(nameof(ServeCostDisplay));
                OnPropertyChanged(nameof(ServeCostSummary));
            }
        }
    }

    public string ServeMlText
    {
        get => ServeMl.HasValue ? ServeMl.Value.ToString("N0", CultureInfo.GetCultureInfo("en-GB")) : string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ServeMl = null;
                return;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("en-GB"), out var parsed) ||
                decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                ServeMl = parsed;
            }
        }
    }

    public decimal ServeCost
    {
        get => _serveCost;
        private set => SetField(ref _serveCost, value);
    }

    public string ServeCostDisplay => IncludeServeCost && ServeMl is > 0m && ServeCost > 0m
        ? ServeCost.ToString("C4", CultureInfo.GetCultureInfo("en-GB"))
        : string.Empty;

    public string ServeCostSummary
    {
        get
        {
            if (!IncludeServeCost)
            {
                return "None";
            }

            var mode = ServeCostIsRtd ? "RTD" : "Conc";
            var ml = ServeMl is > 0m ? $"{ServeMl.Value:N0}ml" : "ml?";
            return string.IsNullOrWhiteSpace(ServeCostDisplay)
                ? $"{mode} {ml}"
                : $"{mode} {ml} {ServeCostDisplay}";
        }
    }

    public decimal LiquidCost
    {
        get => _liquidCost;
        private set => SetField(ref _liquidCost, value);
    }

    public decimal PackedCost
    {
        get => _packedCost;
        private set => SetField(ref _packedCost, value);
    }

    public string CustomerUnit
    {
        get
        {
            var format = _formats.SingleOrDefault(item => item.Name == FormatName);
            return format?.Unit ?? FormatName;
        }
    }

    public decimal CustomerPricePerUnit => PricePerUnit;

    public bool IsUnitPriceDriven => _priceDriver == QuotePriceDriver.UnitPrice;

    public void RestorePriceDriver(bool isUnitPriceDriven)
    {
        _priceDriver = isUnitPriceDriven ? QuotePriceDriver.UnitPrice : QuotePriceDriver.GrossProfit;
    }

    public string UsagePriceDisplay
    {
        get
        {
            if (!HasCalculation)
            {
                return string.Empty;
            }

            if (string.Equals(FormatName, "6X1L", StringComparison.OrdinalIgnoreCase))
            {
                return FormatMoney(PricePerUnit / 6m);
            }

            return ShowsCostInUse(FormatName) && RtdPricePerLitre > 0m
                ? FormatMoney(RtdPricePerLitre)
                : string.Empty;
        }
    }

    private static string FormatMoney(decimal value)
    {
        return value.ToString("C4", CultureInfo.GetCultureInfo("en-GB"));
    }

    public IReadOnlyList<PackCostComponentViewModel> PackCostBreakdown { get; private set; } = [];

    public bool HasPackCostBreakdown => PackCostBreakdown.Count > 0;

    public bool HasCalculation => PricePerUnit > 0m && !string.IsNullOrWhiteSpace(Description);

    public void Calculate()
    {
        var format = _formats.Single(item => item.Name == FormatName);
        _isCalculating = true;
        try
        {
            if (_priceDriver == QuotePriceDriver.UnitPrice)
            {
                CalculateFromUnitPrice(format);
            }
            else
            {
                CalculateFromGrossProfit(format);
            }

            RtdPricePerLitre = DilutionParts is > 0m && format.SellableUnitLitres > 0m
                ? PricePerUnit / format.SellableUnitLitres / DilutionParts.Value
                : 0m;
            ServeCost = CalculateServeCost(format);
            OnPropertyChanged(nameof(PricePerUnitText));
            OnPropertyChanged(nameof(UsagePriceDisplay));
            OnPropertyChanged(nameof(ServeCostDisplay));
            OnPropertyChanged(nameof(ServeCostSummary));
            OnPropertyChanged(nameof(CustomerPricePerUnit));
            OnPropertyChanged(nameof(HasCalculation));
        }
        finally
        {
            _isCalculating = false;
        }
    }

    private void CalculateFromGrossProfit(FormatDefinition format)
    {
        // Cap at 99.99 % — a GP of exactly 100 % causes division by zero
        // (price = cost / (1 − GP)) which produces ∞ or an exception.
        var grossProfit = Math.Min(TargetGpPercent, 99.99m) / 100m;

        if (format.Kind == CalculationKind.Bulk)
        {
            var result = BulkQuoteCalculator.Calculate(new BulkQuoteInput(
                format.Name,
                Code,
                Description,
                RecipeCostPer1000L,
                PackCost,
                grossProfit));

            PricePerUnit = result.PricePerUnit;
            LiquidCost = result.PerLitreCost;
            PackedCost = result.TotalCostPer1000L;
        }
        else
        {
            var result = PackagedQuoteCalculator.Calculate(new PackagedQuoteInput(
                format.Name,
                Code,
                Description,
                RecipeCostPer1000L,
                PackCost,
                grossProfit,
                format.RecipeCostMultiplier));

            PricePerUnit = result.PricePerUnit;
            LiquidCost = result.LiquidCost;
            PackedCost = result.PackedCost;
        }
    }

    private void CalculateFromUnitPrice(FormatDefinition format)
    {
        if (format.Kind == CalculationKind.Bulk)
        {
            PackedCost = RecipeCostPer1000L + PackCost;
            LiquidCost = PackedCost / 1000m;
        }
        else
        {
            LiquidCost = RecipeCostPer1000L * format.RecipeCostMultiplier;
            PackedCost = LiquidCost + PackCost;
        }

        TargetGpPercent = PricePerUnit > 0m
            ? ((PricePerUnit - (format.Kind == CalculationKind.Bulk ? LiquidCost : PackedCost)) / PricePerUnit) * 100m
            : 0m;
    }

    private decimal CalculateServeCost(FormatDefinition format)
    {
        if (!IncludeServeCost || ServeMl is not > 0m || PricePerUnit <= 0m)
        {
            return 0m;
        }

        var packLitres = format.SellableUnitLitres > 0m ? format.SellableUnitLitres : 1m;
        var concentrateCostPerLitre = PricePerUnit / packLitres;
        var costPerLitre = ServeCostIsRtd && DilutionParts is > 0m
            ? concentrateCostPerLitre / DilutionParts.Value
            : concentrateCostPerLitre;

        return costPerLitre * (ServeMl.Value / 1000m);
    }

    private void ApplyFormatDefaults()
    {
        var format = _formats.SingleOrDefault(item => item.Name == FormatName);
        if (format is null)
        {
            return;
        }

        PackCost = format.DefaultPackCost;
        PackCostBreakdown = format.PackCostComponents
            .Select(component => new PackCostComponentViewModel(
                component.Description,
                component.Cost,
                component.Options,
                component.SelectedOptionName))
            .ToList();
        OnPropertyChanged(nameof(HasPackCostBreakdown));
    }

    public void ApplyPackCostBreakdown(IEnumerable<PackCostComponentViewModel> components)
    {
        PackCostBreakdown = components
            .Where(component => !string.IsNullOrWhiteSpace(component.Description) || component.Cost != 0m)
            .Select(component => new PackCostComponentViewModel(
                component.Description.Trim(),
                component.Cost,
                component.Options,
                component.SelectedOption?.Name))
            .ToList();

        PackCost = PackCostBreakdown.Sum(component => component.Cost);
        OnPropertyChanged(nameof(HasPackCostBreakdown));
    }

    public void CopyPackCostBreakdownFrom(QuoteLineViewModel source)
    {
        PackCostBreakdown = source.PackCostBreakdown
            .Select(component => new PackCostComponentViewModel(
                component.Description,
                component.Cost,
                component.Options,
                component.SelectedOption?.Name))
            .ToList();
        OnPropertyChanged(nameof(HasPackCostBreakdown));
    }

    public void CopyServeCostFrom(QuoteLineViewModel source)
    {
        IncludeServeCost = source.IncludeServeCost;
        ServeCostIsRtd = source.ServeCostIsRtd;
        ServeMl = source.ServeMl;
    }

    public void LoadFromLegacy(LegacyCostingQuoteLine imported)
    {
        FormatName = _formats.Any(format => format.Name == imported.FormatName) ? imported.FormatName : "MANUAL LINE";
        Code = imported.Code;
        Description = imported.Description;
        RecipeCostPer1000L = imported.RecipeCostPer1000L;
        TargetGpPercent = imported.GrossProfitPercent;
        DilutionParts = imported.DilutionParts;

        if (Math.Abs(PackCost - imported.PackCost) > 0.0000001m)
        {
            PackCostBreakdown =
            [
                new PackCostComponentViewModel("Imported pack cost", imported.PackCost)
            ];
            OnPropertyChanged(nameof(HasPackCostBreakdown));
        }

        PackCost = imported.PackCost;
        if (imported.QuotedPrice.HasValue)
        {
            PricePerUnit = imported.QuotedPrice.Value;
            _priceDriver = QuotePriceDriver.UnitPrice;
        }
        else
        {
            _isPriceBlank = true;
            _pricePerUnit = 0m;
            _priceDriver = QuotePriceDriver.UnitPrice;
            OnPropertyChanged(nameof(PricePerUnit));
            OnPropertyChanged(nameof(PricePerUnitText));
            OnPropertyChanged(nameof(CustomerPricePerUnit));
            OnPropertyChanged(nameof(HasCalculation));
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed record QuotePreviewLineViewModel(
    string Code,
    string Description,
    string Unit,
    string PricePerUnit,
    string CostInUse,
    string ServeCost);

public sealed record QuoteStateSnapshot(
    string Customer,
    IReadOnlyList<QuoteLineSnapshot> Lines);

public sealed record QuoteLineSnapshot(
    string FormatName,
    string Code,
    string Description,
    bool IsMarkedForDelete,
    decimal RecipeCostPer1000L,
    decimal PackCost,
    decimal TargetGpPercent,
    decimal? DilutionParts,
    string PricePerUnitText,
    bool IsUnitPriceDriven,
    bool IncludeServeCost,
    bool ServeCostIsRtd,
    decimal? ServeMl,
    IReadOnlyList<PackCostComponentSnapshot> PackCostBreakdown)
{
    public static QuoteLineSnapshot FromLine(QuoteLineViewModel line)
    {
        return new QuoteLineSnapshot(
            line.FormatName,
            line.Code,
            line.Description,
            line.IsMarkedForDelete,
            line.RecipeCostPer1000L,
            line.PackCost,
            line.TargetGpPercent,
            line.DilutionParts,
            line.PricePerUnitText,
            line.IsUnitPriceDriven,
            line.IncludeServeCost,
            line.ServeCostIsRtd,
            line.ServeMl,
            line.PackCostBreakdown.Select(PackCostComponentSnapshot.FromComponent).ToList());
    }

    public void ApplyTo(QuoteLineViewModel line)
    {
        line.FormatName = FormatName;
        line.Code = Code;
        line.Description = Description;
        line.IsMarkedForDelete = IsMarkedForDelete;
        line.RecipeCostPer1000L = RecipeCostPer1000L;
        line.TargetGpPercent = TargetGpPercent;
        line.DilutionParts = DilutionParts;
        line.ApplyPackCostBreakdown(PackCostBreakdown.Select(component => component.ToComponent()));
        line.PackCost = PackCost;
        line.PricePerUnitText = PricePerUnitText;
        line.IncludeServeCost = IncludeServeCost;
        line.ServeCostIsRtd = ServeCostIsRtd;
        line.ServeMl = ServeMl;
        line.RestorePriceDriver(IsUnitPriceDriven);
    }
}

public sealed record PackCostComponentSnapshot(
    string Description,
    decimal Cost,
    IReadOnlyList<PackCostOption> Options,
    string? SelectedOptionName)
{
    public static PackCostComponentSnapshot FromComponent(PackCostComponentViewModel component)
    {
        return new PackCostComponentSnapshot(
            component.Description,
            component.Cost,
            component.Options,
            component.SelectedOption?.Name);
    }

    public PackCostComponentViewModel ToComponent()
    {
        return new PackCostComponentViewModel(Description, Cost, Options, SelectedOptionName);
    }
}

public sealed class PackCostComponentViewModel : INotifyPropertyChanged
{
    private string _description;
    private decimal _cost;
    private PackCostOption? _selectedOption;

    public PackCostComponentViewModel(
        string description,
        decimal cost,
        IReadOnlyList<PackCostOption>? options = null,
        string? selectedOptionName = null)
    {
        _description = description;
        _cost = cost;
        Options = options ?? [];
        _selectedOption = Options.FirstOrDefault(option => option.Name == selectedOptionName);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public decimal Cost
    {
        get => _cost;
        set => SetField(ref _cost, value);
    }

    public IReadOnlyList<PackCostOption> Options { get; }

    public PackCostOption? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (SetField(ref _selectedOption, value) && value is not null)
            {
                Cost = value.Cost;
            }
        }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public enum CalculationKind
{
    Bulk,
    Packaged
}

public enum QuotePriceDriver
{
    GrossProfit,
    UnitPrice
}

public sealed record FormatDefinition(
    string Name,
    CalculationKind Kind,
    decimal DefaultPackCost,
    decimal RecipeCostMultiplier,
    string Unit,
    decimal SellableUnitLitres,
    decimal DefaultDilutionParts,
    IReadOnlyList<PackCostComponentTemplate> PackCostComponents)
{
    public static FormatDefinition FromMaster(
        string name,
        CalculationKind kind,
        decimal recipeCostMultiplier,
        string unit,
        decimal sellableUnitLitres,
        decimal defaultDilutionParts,
        IReadOnlyList<PackCostComponentTemplate> packCostComponents)
    {
        return new FormatDefinition(
            name,
            kind,
            packCostComponents.Sum(component => component.Cost),
            recipeCostMultiplier,
            unit,
            sellableUnitLitres,
            defaultDilutionParts,
            packCostComponents);
    }
}

public sealed record PackCostComponentTemplate(
    string Description,
    decimal Cost,
    IReadOnlyList<PackCostOption>? Options = null,
    string? SelectedOptionName = null);

public sealed record PackCostOption(string Name, decimal Cost);
