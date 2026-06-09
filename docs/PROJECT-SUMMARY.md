# Simpsons Beverages Quoting Tool - Project Summary

## Location

Workspace:

```text
C:\Users\todd.simpson\OneDrive - Simpsons Beverages\Documents\Quoting tool
```

Installed app:

```text
C:\Users\todd.simpson\AppData\Local\Simpsons Beverages\Quoting Tool
```

Desktop shortcut:

```text
C:\Users\todd.simpson\OneDrive - Simpsons Beverages\Desktop\Simpsons Beverages Quoting Tool.lnk
```

Current publish output:

```text
publish\QuotingToolAppSelfContainedCurrent49
```

## Purpose

Internal Simpsons Beverages desktop quoting tool for sales users. It replaces the Excel-led quote workflow while preserving the costing logic from the legacy costing master workbook and producing customer-facing PDF quotes that do not expose internal cost breakdowns.

## Main Projects

- `SimpsonsBeverages.QuotingTool.sln`
- `src\SimpsonsBeverages.QuotingTool.App`
  WPF desktop quoting application.
- `src\SimpsonsBeverages.QuotingTool.Calculations`
  Reusable costing calculation library.
- `tests\SimpsonsBeverages.QuotingTool.RegressionHarness`
  Console regression harness comparing coded calculations/imports/exports against real Excel workbooks.

## Key App Features

- Installable Windows desktop app.
- Excel-like multi-line quote grid.
- Supports current pack formats:
  - Manual line
  - TOSCA
  - IBC
  - 220L DRUM
  - 25L
  - 10L
  - 2X5L
  - 4X5L
  - 6X1L
  - 6X2L
  - 770ML POUCH
  - 2X2.25L POUCH
  - 2X600ML POUCH
  - 12X750ML
- Editable recipe GBP/1000L, pack cost, target GP, and quote GBP/unit.
- Bidirectional GP/unit-price behaviour: changing GP recalculates unit price; changing unit price recalculates GP.
- Pack cost breakdown popup with apply-all support.
- Optional serve-cost calculation with mode and ml settings:
  - RTD: `£/unit ÷ pack litres ÷ dilution × ml/1000`
  - Concentrate: `£/unit ÷ pack litres × ml/1000`
- Customer-facing PDF export with Simpsons header/branding.
- Customer preview inside the app.
- Legacy Excel import from costing master-style workbooks.
- Manual tab import for non-standard active quote tabs.
- Legacy Excel export back to the costing master template.
- App prompts to save/export PDF before closing if there are unsaved changes.
- Opens maximised.
- Desktop shortcut uses the Simpsons icon.

## Current Grid Behaviour

- Single-click in text cells selects the full value for replacement.
- Double-click enters edit mode and places the caret near the click position.
- Arrow keys while editing commit the current box and move into editing the adjacent editable box.
- Fill-handle style drag-copy is available from the bottom-right corner of editable cells.
- `Ctrl+Z` restores the last committed quote-grid change, including edits, add/remove/reset, imports, pack-cost changes, serve-cost changes, and drag-fill.

## Customer Quote Output

Customer PDFs show:

- Code
- Description
- Unit
- GBP/unit
- Cost in use / RTD litre where applicable
- Optional serve-cost column

For 6X1L quotes, the usage column is shown as `£/L BOTTLE` and calculated as:

```text
GBP/unit ÷ 6
```

Serve-cost PDF header changes based on selected settings, for example:

```text
£/RTD 250ml
£/CONC 250ml
```

If serve-cost settings are mixed across lines, the header falls back to:

```text
£/SERVE
```

## Important Files

- `src\SimpsonsBeverages.QuotingTool.App\MainWindow.xaml`
  Main WPF layout and grid columns.
- `src\SimpsonsBeverages.QuotingTool.App\MainWindow.xaml.cs`
  Main app logic, quote line view model, grid behaviour, PDF model creation, undo, fill-drag, import/export handlers.
- `src\SimpsonsBeverages.QuotingTool.App\LegacyCostingWorkbookIo.cs`
  Legacy costing workbook import/export logic.
- `src\SimpsonsBeverages.QuotingTool.App\SimplePdfExporter.cs`
  PDF generation.
- `src\SimpsonsBeverages.QuotingTool.App\PackCostBreakdownWindow.cs`
  Pack-cost breakdown editor.
- `src\SimpsonsBeverages.QuotingTool.App\ServeCostSettingsWindow.cs`
  Serve-cost editor.
- `src\SimpsonsBeverages.QuotingTool.App\Templates\costing-template.xlsx`
  Bundled legacy costing template used when the network master is unavailable.
- `src\SimpsonsBeverages.QuotingTool.App\Assets`
  Logo/icon assets.
- `install-quoting-tool.ps1`
  Installs the latest self-contained publish output locally and creates the desktop shortcut.

## Build, Test, Publish

Build:

```powershell
dotnet build SimpsonsBeverages.QuotingTool.sln
```

Run regression harness:

```powershell
dotnet run --project tests\SimpsonsBeverages.QuotingTool.RegressionHarness\SimpsonsBeverages.QuotingTool.RegressionHarness.csproj
```

Publish current app pattern:

```powershell
dotnet publish src\SimpsonsBeverages.QuotingTool.App\SimpsonsBeverages.QuotingTool.App.csproj -c Release -r win-x64 --self-contained true -o publish\QuotingToolAppSelfContainedCurrentXX
```

Install:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\install-quoting-tool.ps1
```

## Verification Status

As of 2026-06-09:

- Build passes.
- Full regression harness passes.
- Legacy Excel export includes a regression check to prevent broken calc-chain relationships, which previously caused Excel repair warnings.
- Current installed version is the optimized version after the latest grid, undo, fill-drag, Excel export, and serve-cost changes.

## Known Future Work

- OGL / Prof-it Plus read-only integration still needs technical discovery.
- Ingredient-cost refresh is not implemented yet.
- Historic quote storage/audit trail is not implemented; saved PDFs are currently the quote archive.
- Admin-maintained pack/component-cost screens are not yet separated from the main app.
- Fill handle currently uses cursor feedback rather than a visible Excel-style square.
