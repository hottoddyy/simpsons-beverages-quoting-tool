# Simpsons Beverages Quoting Tool

Internal quoting tool prototype for Simpsons Beverages.

## Current State

This repository currently contains the first calculation proof-of-concept:

- `src/SimpsonsBeverages.QuotingTool.Calculations`
  - reusable costing calculation code
- `src/SimpsonsBeverages.QuotingTool.App`
  - first WPF desktop app prototype
- `tests/SimpsonsBeverages.QuotingTool.RegressionHarness`
  - console harness that reads completed Excel quote workbooks and checks coded calculations against Excel's cached results

Implemented fixtures:

- `IBC`: `\\adserver2\Company Share\Sales\Quotes\2026\Edmunds\2026-05-13 RTD PROJECT (RIDGEVIEW) SUBMISSION 1.xlsx`
- `10L`: `\\adserver2\Company Share\Sales\Quotes\2026\2026-02-27 SCHOOL PRODUCTS.xlsx`
- `2X5L`: `\\adserver2\Company Share\Sales\Quotes\2026\18 Gin\2026-05-11 LEMONADE.xlsx`
- `6X1L`: `\\adserver2\Company Share\Sales\Quotes\2026\Booker\2026-03-04 COFFEE SYRUP TENDER.xlsx`
- `4X5L`: `\\adserver2\Company Share\Sales\Quotes\2026\Belux Imports\2026-02-04.xlsx`
- `6X2L`: `\\adserver2\Company Share\Sales\Quotes\2026\Shott Beverages\2026-05- MANGO PUREE CHAIIWALA.xlsx`
- `25L`: `\\adserver2\Company Share\Sales\Quotes\2026\New Juice\2026-06-01 NEW JUICE.xlsx`
- `220L DRUM`: `\\adserver2\Company Share\Sales\Quotes\2026\Impression beverages\2026-03-05 IMPRESSION BEVERAGES.xlsx`
- `TOSCA`: `\\adserver2\Company Share\Sales\Quotes\2026\2026-04-02 BABCO CHARRED PINEAPPLE, BANANA, MANGO & GINGER COMPOUND.xlsx`
- `770ML POUCH`: `\\adserver2\Company Share\Sales\Quotes\2026\Costa\2026-03-10 COSTA EXPRESS GIVAUDAN FLAVOURS.xlsx`
- `2X2.25L POUCH`: `\\adserver2\Company Share\Sales\Quotes\2026\Think Drinks\2026-01-08 THINK DRINKS LOUNGERS TENDER.xlsx`
- `2X600ML POUCH`: `\\adserver2\Company Share\Sales\Quotes\2026\Think Drinks\2026-02-17 THINK DRINKS FUNCTIONAL PERFECTORS ex Lions Mane A and B, collegen, cordyceps.xlsx`
- `12X750ML`: `\\adserver2\Company Share\Sales\Quotes\2026\Green Key Solutions\2026-03-11 MONSTER MATCHES SLUSH.xlsx`

## Run The Regression Harness

```powershell
dotnet run --project tests\SimpsonsBeverages.QuotingTool.RegressionHarness\SimpsonsBeverages.QuotingTool.RegressionHarness.csproj
```

Expected result:

```text
All regression checks passed.
```

## Build

```powershell
dotnet build SimpsonsBeverages.QuotingTool.sln
```

## Run The Desktop Prototype

Published app:

```text
publish\QuotingToolApp\SimpsonsBeverages.QuotingTool.App.exe
```

The app currently supports:

- pack-format selection
- Excel-like multi-line quote entry
- manual recipe cost per 1000L entry
- editable pack cost
- target GP entry
- dilution-based RTD price display
- red-to-green GP indicator
- customer-facing multi-line PDF export

Publish command:

```powershell
dotnet publish src\SimpsonsBeverages.QuotingTool.App\SimpsonsBeverages.QuotingTool.App.csproj -c Release -r win-x64 --self-contained false -o publish\QuotingToolApp
```

Self-contained publish command:

```powershell
dotnet publish src\SimpsonsBeverages.QuotingTool.App\SimpsonsBeverages.QuotingTool.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish\QuotingToolAppSelfContained
```

Local install script:

```powershell
.\install-quoting-tool.ps1
```

The install script copies the self-contained app to `%LOCALAPPDATA%\Simpsons Beverages\Quoting Tool` and creates a desktop shortcut.
