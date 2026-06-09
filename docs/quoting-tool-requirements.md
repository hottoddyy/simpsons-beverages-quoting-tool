# Simpsons Beverages Quoting Tool Requirements

## Objective

Build an installable internal desktop quoting application for Simpsons Beverages sales users. The app should replace the current Excel-led quoting process while matching the costing workbook calculations exactly and producing customer-facing PDF quotes.

## Current Process

1. A sales user goes into OGL / Prof-it Plus.
2. They model from a temporary product.
3. They recreate or review the product recipe in the recipe system.
4. They obtain the 1000L recipe cost.
5. They enter that 1000L cost into the relevant pack-format sheet in the costing workbook.
6. They choose the margin / selling price.
7. The finished quote is sent to the customer.

## Users

Primary users: sales team.

Initial admin/maintainer: Todd.

No role hierarchy is required for the first version, although admin-only maintenance screens may still be useful for editing costing tables.

## Core Requirements

- Desktop application installable on company PCs.
- Initial version can run from Todd's computer / local environment.
- Network access is acceptable and expected.
- GBP only.
- Supports quotes for both new and existing products.
- Customer-facing PDF output.
- PDF must not expose the internal costing breakdown.
- PDF should include a standard expiry statement, currently expected to be approximately: "This quote is valid for 1 week unless otherwise confirmed."
- Quote versioning is not required.
- The app does not need to store historic quote records, because saved PDFs will act as the quote archive.
- The app should show working values during quote preparation, including GP, unit price, litre price, and RTD price where applicable.
- Output should show:
  - price per unit
  - price per litre concentrate, where relevant
  - price per RTD litre, where dilution applies
- Rounding should happen at the final output stage only.

## Costing Accuracy

- The app must match the existing Excel costing template exactly.
- The current workbook should be used as the calculation reference.
- Completed real-world example quotes should be used as regression tests.
- Current pack-format calculations must be preserved exactly.
- GP should be shown using a red-to-green sliding colour scale.
- Low GP does not need to block the quote or show warnings.
- Users can choose margin / selling price.

## Pack Formats

Current formats are complete for the first version but the app should allow additional formats in future.

Current workbook formats:

- TOSCA
- IBC
- 220L DRUM
- 25L
- 2X5L
- 4X5L
- 10L
- 6X1L
- 6X2L
- 770ML POUCH
- 2X2.25L POUCH
- 2X600ML POUCH
- 12X750ML

Pack costs should be configurable by an admin user rather than permanently hard-coded.

Pack/component costs generally do not vary by customer, but users should be able to edit costs with a warning or clear visual indication that they are overriding the standard value.

Cost changes should have approval before becoming live. The first version can treat Todd/admin confirmation as approval unless a more formal workflow is later required.

## Commercial Rules

- Minimum order quantities are not required.
- Seasonal pricing rules are not required.
- Linked/group-company customer pricing is not required.
- Quote conditions should support carriage, ex works, export, and pro forma considerations.
- Current notes from the workbook should be represented in the app as quote prompts/checks:
  - Check other products the customer takes; is it part of a range?
  - Ex works or carriage?
  - Check whether price is above GBP 1 per litre.
  - Minimum GP target is 40%, unless high volume or potential justifies lower.
  - Conditions for low GP may include huge volume, pro forma, ex works, export, non-seasonal, or similar.

## OGL / Prof-it Plus Integration

Integration should be read-only.

The app needs to pull ingredient costs from Prof-it Plus / OGL, ideally on a daily refresh.

If a required ingredient cost is missing or stale, the app should flag it and ask the user for manual input.

Users have individual OGL accounts.

The exact integration method still needs to be confirmed. Public information suggests Prof-it Plus integrations may use direct read access to the live SQL database, an integration runtime, an API, or export/report-based approaches depending on version and vendor setup.

For the first build, the safest path is:

1. Implement the costing engine and manual 1000L recipe cost entry/import first.
2. Validate exact calculation matches against the Excel workbook.
3. Add a daily read-only ingredient-cost sync once OGL connection details are known.

## Data And Audit

- The app should ideally keep an audit trail of the cost inputs used for a quote.
- Since old quotes are stored as PDFs, the app does not need long-term quote history in the first version.
- Ingredient and pack costs should have enough metadata to know when values were last refreshed or edited.

## Suggested MVP

1. Desktop app shell.
2. Pack-format selection.
3. Editable quote input form.
4. Recreated Excel costing calculations for all current pack formats.
5. Admin-maintained pack/component cost table.
6. Manual 1000L recipe cost input.
7. GP red-to-green scale.
8. Customer-facing PDF generation.
9. Regression-test set using the Excel workbook and completed example quotes.

## Implementation Status

As of 2026-06-03, the first calculation proof-of-concept has been created in this workspace.

Implemented:

- .NET 8 solution: `SimpsonsBeverages.QuotingTool.sln`
- Calculation library: `src\SimpsonsBeverages.QuotingTool.Calculations`
- Regression harness: `tests\SimpsonsBeverages.QuotingTool.RegressionHarness`
- WPF desktop prototype: `src\SimpsonsBeverages.QuotingTool.App`
- IBC calculation model covering:
  - recipe cost per 1000L
  - pack cost per 1000L
  - total 1000L cost
  - per litre cost
  - gross profit
  - price per unit
  - RTD price per litre using dilution parts
- Read-only `.xlsx` workbook reader for the Ridgeview IBC fixture
- Packaged-format calculation model covering:
  - 10L
  - 2X5L
  - 6X1L
  - 4X5L
  - 6X2L
  - 770ML POUCH
  - 2X2.25L POUCH
  - 2X600ML POUCH
  - 12X750ML
- Bulk-format calculation model covering:
  - TOSCA
  - 25L
  - 220L DRUM

Verified:

- The Ridgeview IBC workbook passes for all four quote rows.
- The 10L School Products workbook passes for six quote rows.
- The 2X5L 18 Gin Lemonade workbook passes for two quote rows.
- The 6X1L Booker Coffee Syrup Tender workbook passes for twelve quote rows.
- The 4X5L Belux Imports workbook passes for eleven quote rows.
- The 6X2L Shott Beverages Mango Puree workbook passes for two quote rows.
- The 25L New Juice workbook passes for two quote rows.
- The 220L Drum Impression Beverages workbook passes for three quote rows.
- The TOSCA BABCO workbook passes for three quote rows.
- The 770ML Pouch Costa workbook passes for eleven complete quote rows.
- The 2X2.25L Pouch Think Drinks workbook passes for sixteen complete quote rows.
- The 2X600ML Pouch Think Drinks workbook passes for eleven complete quote rows.
- The 12X750ML Green Key Solutions workbook passes for three quote rows.
- The coded calculations match the workbook's cached values within `0.000000001`.
- The first desktop prototype builds and publishes to `publish\QuotingToolApp`.
- The prototype includes Excel-like multi-line manual quote entry, GP colour indication, live calculation, and basic customer-facing multi-line PDF export.
- A self-contained Windows publish is available at `publish\QuotingToolAppSelfContained`.
- A local installer script is available at `install-quoting-tool.ps1`; it copies the self-contained build to `%LOCALAPPDATA%\Simpsons Beverages\Quoting Tool` and creates a desktop shortcut.

## First Regression Example

Completed costing file:

`\\adserver2\Company Share\Sales\Quotes\2026\Edmunds\2026-05-13 RTD PROJECT (RIDGEVIEW) SUBMISSION 1.xlsx`

Active quote sheet: `IBC`

Customer/project context inferred from filename: Edmunds / Ridgeview RTD Project / Submission 1 / 2026-05-13.

Quote lines:

| Code | Description | Unit | Price/unit | RTD price/litre | GP | 1000L total cost | Pack cost | Recipe 1000L cost |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 200349\001 | APERITIF SPRITZ 1 + 5 | LITRE | 2.4050728 | 0.4008454667 | 48.9215503% | 1228.4739 | 256.46 | 972.0139 |
| 200353\001 | RASPBERRY SPRITZ 1 + 5 | LITRE | 2.30922 | 0.38487 | 50.0000000% | 1154.61 | 256.46 | 898.15 |
| 200351\001 | BELLINI SPRITZ 1 + 5 | LITRE | 2.187093 | 0.3645155 | 50.0000000% | 1093.5465 | 256.46 | 837.0865 |
| 200348\001 | ENGLISH GARDEN SPRITZ 1 + 5 | LITRE | 2.096281 | 0.3493801667 | 50.0000000% | 1048.1405 | 256.46 | 791.6805 |

IBC pack/overhead cost inputs used:

| Item | Value |
| --- | ---: |
| IBC | 111.46 |
| Labour | 60.00 |
| Transport | 65.00 |
| Overheads | 20.00 |
| Total pack cost per 1000L | 256.46 |

This example should become an automated calculation test. The app must reproduce the workbook's unrounded internal values and only round at final display/PDF output.

## 2026 Quote Folder Scan

Source folder:

`\\adserver2\Company Share\Sales\Quotes\2026`

Read-only scan date: 2026-06-03.

Files found:

| File type | Count |
| --- | ---: |
| .xlsx | 276 |
| .xlsm | 11 |
| .xlsb | 9 |
| .pdf | 3 |
| .msg | 2 |

The initial XML scan found populated quote rows in 243 `.xlsx` / `.xlsm` workbooks. `.xlsb` files were not included in this first scan because they are binary Excel workbooks and need separate handling.

Detected pack-format coverage:

| Format | Workbooks with populated rows |
| --- | ---: |
| TOSCA | 87 |
| IBC | 36 |
| 220L DRUM | 3 |
| 25L | 3 |
| 2X5L | 19 |
| 4X5L | 12 |
| 10L | 48 |
| 6X1L | 24 |
| 6X2L | 4 |
| 770ML POUCH | 2 |
| 2X2.25L POUCH | 24 |
| 2X600ML POUCH | 5 |
| 12X750ML | 3 |

Candidate regression fixtures by format:

| Format | Candidate file |
| --- | --- |
| TOSCA | `\\adserver2\Company Share\Sales\Quotes\2026\2026-04-02 BABCO CHARRED PINEAPPLE, BANANA, MANGO & GINGER COMPOUND.xlsx` |
| IBC | `\\adserver2\Company Share\Sales\Quotes\2026\Edmunds\2026-05-13 RTD PROJECT (RIDGEVIEW) SUBMISSION 1.xlsx` |
| 220L DRUM | `\\adserver2\Company Share\Sales\Quotes\2026\Impression beverages\2026-03-05 IMPRESSION BEVERAGES.xlsx` |
| 25L | `\\adserver2\Company Share\Sales\Quotes\2026\New Juice\2026-06-01 NEW JUICE.xlsx` |
| 2X5L | `\\adserver2\Company Share\Sales\Quotes\2026\18 Gin\2026-05-11 LEMONADE.xlsx` |
| 4X5L | `\\adserver2\Company Share\Sales\Quotes\2026\Belux Imports\2026-02-04.xlsx` |
| 10L | `\\adserver2\Company Share\Sales\Quotes\2026\2026-02-27 SCHOOL PRODUCTS.xlsx` |
| 6X1L | `\\adserver2\Company Share\Sales\Quotes\2026\Booker\2026-03-04 COFFEE SYRUP TENDER.xlsx` |
| 6X2L | `\\adserver2\Company Share\Sales\Quotes\2026\Shott Beverages\2026-05- MANGO PUREE CHAIIWALA.xlsx` |
| 770ML POUCH | `\\adserver2\Company Share\Sales\Quotes\2026\Costa\2026-03-10 COSTA EXPRESS GIVAUDAN FLAVOURS.xlsx` |
| 2X2.25L POUCH | `\\adserver2\Company Share\Sales\Quotes\2026\Think Drinks\2026-01-08 THINK DRINKS LOUNGERS TENDER.xlsx` |
| 2X600ML POUCH | `\\adserver2\Company Share\Sales\Quotes\2026\Think Drinks\2026-02-17 THINK DRINKS FUNCTIONAL PERFECTORS ex Lions Mane A and B, collegen, cordyceps.xlsx` |
| 12X750ML | `\\adserver2\Company Share\Sales\Quotes\2026\Green Key Solutions\2026-03-11 MONSTER MATCHES SLUSH.xlsx` |

Important caveat: some historic files appear to have manual layout changes or copied sections, so regression fixtures should be reviewed one by one before being treated as authoritative. The first automated fixture should remain the Ridgeview IBC workbook because it is clean and focused.

## Suggested Phase 2

1. Read-only OGL / Prof-it Plus ingredient-cost connector.
2. Daily cost refresh.
3. Missing/stale cost flags.
4. Optional Microsoft 365 / Azure AD authentication.
5. More formal cost-change approval workflow if needed.

## Open Questions

1. What technology should the desktop app use: .NET/WPF, .NET MAUI, Electron, or another stack?
2. Is the app expected to run only on Windows PCs?
3. Where should editable cost data live initially: local SQLite database, shared network database, or a file on the company share?
4. Should generated PDFs follow an existing Simpsons Beverages quote template or should one be designed?
5. What fields must appear on the customer PDF: customer name, product name, pack size, price, RTD price, payment terms, lead time, delivery terms, validity, contact details?
6. What are the exact dilution/RTD rules for products where RTD litre pricing is required?
7. Can we get 5-10 completed example quotes that include the Excel inputs and expected outputs?
8. Can IT or OGL confirm the Prof-it Plus database/API connection method and whether read-only database access is available?
9. Does Prof-it Plus store recipe ingredient costs in a way that can be queried directly, or does the recipe system need a separate connector?
10. Which fields identify an ingredient reliably: stock code, description, supplier code, or another key?
