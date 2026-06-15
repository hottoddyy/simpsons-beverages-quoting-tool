using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;

namespace SimpsonsBeverages.QuotingTool.App;

public static class LegacyCostingWorkbookIo
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

    private static readonly IReadOnlyDictionary<string, LegacySheetMap> SheetMaps =
        new Dictionary<string, LegacySheetMap>(StringComparer.OrdinalIgnoreCase)
        {
            ["TOSCA"] = LegacySheetMap.Bulk("TOSCA", "$C$28"),
            ["IBC"] = LegacySheetMap.Bulk("IBC", "$C$27"),
            ["220L DRUM"] = LegacySheetMap.Bulk("220L DRUM", "$C$28"),
            ["25L"] = LegacySheetMap.Bulk("25L", "$C$29"),
            ["10L"] = LegacySheetMap.Packaged("10L", "H", "I", "J", "K", "L", "$C$29", 0.01m),
            ["2X5L"] = LegacySheetMap.Packaged("2X5L", "H", "I", "J", "K", "L", "$C$30", 0.01m),
            ["4X5L"] = LegacySheetMap.Packaged("4X5L", "H", "I", "J", "K", "L", "$C$30", 0.02m),
            ["6X1L"] = LegacySheetMap.Packaged("6X1L", "I", "J", "K", "L", "M", "$C$39", 0.006m),
            ["6X2L"] = LegacySheetMap.Packaged("6X2L", "H", "I", "J", "K", "L", "$C$28", 1m / 83.3333333333m),
            ["770ML POUCH"] = LegacySheetMap.Packaged("770ML POUCH", "H", "I", "J", "K", "L", "$C$29", 0.00308m),
            ["2X2.25L POUCH"] = LegacySheetMap.Packaged("2X2.25L POUCH", "M", "N", "O", "P", "Q", "$I$30", 0.0045m),
            ["2X600ML POUCH"] = LegacySheetMap.Packaged("2X600ML POUCH", "M", "N", "O", "P", "Q", "$I$28", 0.0012m),
            ["12X750ML"] = LegacySheetMap.Packaged("12X750ML", "H", "I", "J", "K", "L", "$C$34", 0.009m),
        };

    public static IReadOnlyList<LegacyCostingQuoteLine> Import(string workbookPath)
    {
        using var archive = OpenWorkbookRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPaths = GetSheetPaths(archive);
        var lines = new List<LegacyCostingQuoteLine>();

        foreach (var map in SheetMaps.Values)
        {
            if (!sheetPaths.TryGetValue(map.SheetName, out var sheetPath))
            {
                continue;
            }

            var sheet = ReadXml(archive, sheetPath);
            var resolvedMap = ResolveSheetMap(sheet, map, sharedStrings);
            foreach (var row in sheet.Descendants(Spreadsheet + "row").Select(row => ReadRow(row, sharedStrings)))
            {
                if (row.RowNumber is < 4 or > 20 || !HasQuoteIdentity(row.Cells))
                {
                    continue;
                }

                var hasQuotedPrice = TryGetDecimal(row.Cells, "D", out var quotedPrice);
                if (!TryGetDecimal(row.Cells, resolvedMap.RecipeCostColumn, out var recipeCost))
                {
                    continue;
                }

                var packCost = ReadPackCost(row.Cells, resolvedMap);
                var gpPercent = hasQuotedPrice
                    ? CalculateGrossProfitPercent(resolvedMap, quotedPrice, recipeCost, packCost, row.Cells)
                    : 50m;

                lines.Add(new LegacyCostingQuoteLine(
                    resolvedMap.SheetName,
                    row.Cells.GetValueOrDefault("A", string.Empty),
                    row.Cells.GetValueOrDefault("B", string.Empty),
                    hasQuotedPrice ? quotedPrice : null,
                    packCost,
                    recipeCost,
                    gpPercent,
                    GuessDilutionParts(row.Cells.GetValueOrDefault("B", string.Empty))));
            }
        }

        return lines;
    }

    public static IReadOnlyList<string> GetSheetNames(string workbookPath)
    {
        using var archive = OpenWorkbookRead(workbookPath);
        return GetSheetPaths(archive).Keys.ToList();
    }

    public static IReadOnlyList<string> GetImportFormatNames()
    {
        return SheetMaps.Keys.ToList();
    }

    public static IReadOnlyList<LegacyCostingQuoteLine> ImportSheet(string workbookPath, string sheetName, string formatName)
    {
        using var archive = OpenWorkbookRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPaths = GetSheetPaths(archive);

        if (!sheetPaths.TryGetValue(sheetName, out var sheetPath))
        {
            throw new InvalidOperationException($"Sheet not found: {sheetName}");
        }

        if (!SheetMaps.TryGetValue(formatName, out var formatMap))
        {
            throw new InvalidOperationException($"Import layout not found: {formatName}");
        }

        var sheet = ReadXml(archive, sheetPath);
        var resolvedMap = ResolveSheetMap(sheet, formatMap, sharedStrings);
        return ReadQuoteLines(sheet, resolvedMap, sharedStrings, formatName);
    }

    private static IReadOnlyList<LegacyCostingQuoteLine> ReadQuoteLines(
        XDocument sheet,
        LegacySheetMap map,
        IReadOnlyList<string> sharedStrings,
        string formatName)
    {
        var lines = new List<LegacyCostingQuoteLine>();
        foreach (var row in sheet.Descendants(Spreadsheet + "row").Select(row => ReadRow(row, sharedStrings)))
        {
            if (row.RowNumber is < 4 or > 20 || !HasQuoteIdentity(row.Cells))
            {
                continue;
            }

            var hasQuotedPrice = TryGetDecimal(row.Cells, "D", out var quotedPrice);
            if (!TryGetDecimal(row.Cells, map.RecipeCostColumn, out var recipeCost))
            {
                continue;
            }

            var packCost = ReadPackCost(row.Cells, map);
            var gpPercent = hasQuotedPrice
                ? CalculateGrossProfitPercent(map, quotedPrice, recipeCost, packCost, row.Cells)
                : 50m;

            lines.Add(new LegacyCostingQuoteLine(
                formatName,
                row.Cells.GetValueOrDefault("A", string.Empty),
                row.Cells.GetValueOrDefault("B", string.Empty),
                hasQuotedPrice ? quotedPrice : null,
                packCost,
                recipeCost,
                gpPercent,
                GuessDilutionParts(row.Cells.GetValueOrDefault("B", string.Empty))));
        }

        return lines;
    }

    private static ZipArchive OpenWorkbookRead(string workbookPath)
    {
        var stream = File.Open(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new ZipArchive(stream, ZipArchiveMode.Read);
    }

    private static LegacySheetMap ResolveSheetMap(XDocument sheet, LegacySheetMap map, IReadOnlyList<string> sharedStrings)
    {
        var header = sheet
            .Descendants(Spreadsheet + "row")
            .Where(row => int.TryParse(row.Attribute("r")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rowNumber) && rowNumber == 3)
            .Select(row => ReadRow(row, sharedStrings))
            .FirstOrDefault();

        if (header.Cells is null || header.Cells.Count == 0)
        {
            return map;
        }

        var recipeCostColumn = header.Cells
            .Where(cell => IsRecipeCostHeader(cell.Value))
            .Select(cell => cell.Key)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(recipeCostColumn) ? map : map with { RecipeCostColumn = recipeCostColumn };
    }

    private static bool IsRecipeCostHeader(string value)
    {
        var normalized = value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized.Contains("1000LCOST", StringComparison.Ordinal) &&
               (normalized.Contains("RECIPE", StringComparison.Ordinal) || normalized.Contains("OGL", StringComparison.Ordinal));
    }

    public static void Export(string templatePath, string outputPath, IReadOnlyList<QuoteLineViewModel> lines)
    {
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException("Costing master template not found.", templatePath);
        }

        if (string.Equals(Path.GetFullPath(templatePath), Path.GetFullPath(outputPath), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Choose a different file name. The export cannot overwrite the costing master template.");
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.Copy(templatePath, outputPath, overwrite: true);

        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Update);
        var sheetPaths = GetSheetPaths(archive);
        var groupedLines = lines
            .GroupBy(line => ResolveExportSheetName(line.FormatName), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Take(17).ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var (sheetName, sheetLines) in groupedLines)
        {
            if (!SheetMaps.TryGetValue(sheetName, out var map) || !sheetPaths.TryGetValue(sheetName, out var sheetPath))
            {
                continue;
            }

            var sheet = ReadXml(archive, sheetPath);
            StripSharedFormulas(sheet);
            ClearQuoteRows(sheet, map);

            for (var index = 0; index < sheetLines.Count; index++)
            {
                WriteLine(sheet, map, rowNumber: 4 + index, sheetLines[index]);
            }

            ReplaceXml(archive, sheetPath, sheet);
        }

        MarkWorkbookForRecalculation(archive);
    }

    private static void StripSharedFormulas(XDocument sheet)
    {
        foreach (var cell in sheet.Descendants(Spreadsheet + "c").ToList())
        {
            var f = cell.Element(Spreadsheet + "f");
            if (f is null) continue;
            var tAttr = f.Attribute("t");
            if (tAttr?.Value != "shared") continue;

            if (!string.IsNullOrEmpty(f.Value))
            {
                // Master shared formula: convert to plain formula
                tAttr.Remove();
                f.Attribute("ref")?.Remove();
                f.Attribute("si")?.Remove();
            }
            else
            {
                // Slave cell: no formula text — clear the cell value to 0
                cell.RemoveNodes();
                cell.SetAttributeValue("t", null);
                cell.Add(new XElement(Spreadsheet + "v", "0"));
            }
        }
    }

    private static void ClearQuoteRows(XDocument sheet, LegacySheetMap map)
    {
        for (var row = 4; row <= 20; row++)
        {
            SetTextCell(sheet, "A", row, string.Empty);
            SetTextCell(sheet, "B", row, string.Empty);
            SetTextCell(sheet, "C", row, string.Empty);
            SetNumberCell(sheet, "D", row, 0m);
            SetNumberCell(sheet, map.GrossProfitColumn, row, 0m);
            SetNumberCell(sheet, map.PackedCostColumn, row, 0m);
            SetNumberCell(sheet, map.LiquidCostColumn, row, 0m);
            if (map.PackCostColumn is not null)
            {
                SetNumberCell(sheet, map.PackCostColumn, row, 0m);
            }
            SetNumberCell(sheet, map.RecipeCostColumn, row, 0m);
        }
    }

    private static void WriteLine(XDocument sheet, LegacySheetMap map, int rowNumber, QuoteLineViewModel line)
    {
        SetTextCell(sheet, "A", rowNumber, line.Code);
        SetTextCell(sheet, "B", rowNumber, line.Description);
        SetTextCell(sheet, "C", rowNumber, line.CustomerUnit);
        SetNumberCell(sheet, "D", rowNumber, line.PricePerUnit);
        SetNumberCell(sheet, map.RecipeCostColumn, rowNumber, line.RecipeCostPer1000L);

        if (map.Kind == LegacySheetKind.Bulk)
        {
            WriteBulkFormulas(sheet, map, rowNumber, line);
        }
        else
        {
            WritePackagedFormulas(sheet, map, rowNumber, line);
        }
    }

    private static void WriteBulkFormulas(XDocument sheet, LegacySheetMap map, int rowNumber, QuoteLineViewModel line)
    {
        var dilution = line.DilutionParts is > 0m ? line.DilutionParts.Value : 1m;
        SetFormulaCell(sheet, "F", rowNumber, $"D{rowNumber}/{dilution.ToString(CultureInfo.InvariantCulture)}", line.RtdPricePerLitre);
        SetFormulaCell(sheet, map.GrossProfitColumn, rowNumber, $"(D{rowNumber}-J{rowNumber})/D{rowNumber}", line.TargetGpPercent / 100m);
        SetFormulaCell(sheet, map.PackedCostColumn, rowNumber, $"K{rowNumber}/1000", line.PackedCost / 1000m);
        SetFormulaCell(sheet, map.LiquidCostColumn, rowNumber, $"M{rowNumber}+L{rowNumber}", line.PackedCost);
        SetFormulaCell(sheet, map.PackCostColumn!, rowNumber, map.PackCostFormulaCell, line.PackCost);
    }

    private static void WritePackagedFormulas(XDocument sheet, LegacySheetMap map, int rowNumber, QuoteLineViewModel line)
    {
        SetFormulaCell(sheet, map.GrossProfitColumn, rowNumber, $"(D{rowNumber}-{map.PackedCostColumn}{rowNumber})/D{rowNumber}", line.TargetGpPercent / 100m);
        SetFormulaCell(sheet, map.LiquidCostColumn, rowNumber, $"{map.RecipeCostColumn}{rowNumber}*{map.RecipeCostMultiplier.ToString(CultureInfo.InvariantCulture)}", line.LiquidCost);

        if (map.PackCostColumn is not null)
        {
            SetFormulaCell(sheet, map.PackCostColumn, rowNumber, map.PackCostFormulaCell, line.PackCost);
            SetFormulaCell(sheet, map.PackedCostColumn, rowNumber, $"{map.LiquidCostColumn}{rowNumber}+{map.PackCostColumn}{rowNumber}", line.PackedCost);
        }
        else
        {
            SetFormulaCell(sheet, map.PackedCostColumn, rowNumber, $"{map.LiquidCostColumn}{rowNumber}+{map.PackCostFormulaCell}", line.PackedCost);
        }
    }

    private static string ResolveExportSheetName(string formatName)
    {
        return string.Equals(formatName, "MANUAL LINE", StringComparison.OrdinalIgnoreCase) ? "IBC" : formatName;
    }

    private static bool HasQuoteIdentity(Dictionary<string, string> cells)
    {
        return
            cells.TryGetValue("A", out var code) && !string.IsNullOrWhiteSpace(code) ||
            cells.TryGetValue("B", out var description) && !string.IsNullOrWhiteSpace(description);
    }

    private static decimal GuessDilutionParts(string description)
    {
        var match = System.Text.RegularExpressions.Regex.Match(description, @"1\s*\+\s*(\d+)");
        return match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parts)
            ? parts + 1m
            : 0m;
    }

    private static decimal CalculateGrossProfitPercent(
        LegacySheetMap map,
        decimal quotedPrice,
        decimal recipeCost,
        decimal packCost,
        Dictionary<string, string> cells)
    {
        if (quotedPrice <= 0m)
        {
            return 0m;
        }

        var cost = map.Kind == LegacySheetKind.Bulk
            ? (recipeCost + packCost) / 1000m
            : ReadPackedCost(map, recipeCost, packCost, cells);

        return ((quotedPrice - cost) / quotedPrice) * 100m;
    }

    private static bool TryGetDecimal(Dictionary<string, string> cells, string column, out decimal value)
    {
        return decimal.TryParse(cells.GetValueOrDefault(column, string.Empty), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static decimal ReadPackCost(Dictionary<string, string> cells, LegacySheetMap map)
    {
        if (map.PackCostColumn is not null && TryGetDecimal(cells, map.PackCostColumn, out var packCost))
        {
            return packCost;
        }

        return TryGetDecimal(cells, map.PackedCostColumn, out var packedCost) &&
               TryGetDecimal(cells, map.LiquidCostColumn, out var liquidCost)
            ? packedCost - liquidCost
            : 0m;
    }

    private static decimal ReadPackedCost(LegacySheetMap map, decimal recipeCost, decimal packCost, Dictionary<string, string> cells)
    {
        if (TryGetDecimal(cells, map.PackedCostColumn, out var packedCost))
        {
            return packedCost;
        }

        return (recipeCost * map.RecipeCostMultiplier) + packCost;
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        var xml = ReadXml(entry);
        return xml
            .Descendants(Spreadsheet + "si")
            .Select(item => string.Concat(item.DescendantNodes().OfType<XText>().Select(text => text.Value)))
            .ToList();
    }

    private static Dictionary<string, string> GetSheetPaths(ZipArchive archive)
    {
        var workbook = ReadXml(archive, "xl/workbook.xml");
        var relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels")
            .Descendants(PackageRelationships + "Relationship")
            .ToDictionary(
                relationship => relationship.Attribute("Id")!.Value,
                relationship => relationship.Attribute("Target")!.Value);

        return workbook
            .Descendants(Spreadsheet + "sheet")
            .ToDictionary(
                sheet => sheet.Attribute("name")!.Value,
                sheet =>
                {
                    var relationshipId = sheet.Attribute(OfficeRelationships + "id")!.Value;
                    var target = relationships[relationshipId].TrimStart('/');
                    return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : $"xl/{target}";
                },
                StringComparer.OrdinalIgnoreCase);
    }

    private static (int RowNumber, Dictionary<string, string> Cells) ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var rowNumber = int.Parse(row.Attribute("r")!.Value, CultureInfo.InvariantCulture);
        var cells = row
            .Elements(Spreadsheet + "c")
            .Where(cell => cell.Attribute("r") is not null)
            .ToDictionary(
                cell => GetColumnName(cell.Attribute("r")!.Value),
                cell => ReadCellValue(cell, sharedStrings));

        return (rowNumber, cells);
    }

    private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var value = cell.Element(Spreadsheet + "v")?.Value;
        var type = cell.Attribute("t")?.Value;

        if (type == "s" && value is not null)
        {
            return sharedStrings[int.Parse(value, CultureInfo.InvariantCulture)];
        }

        if (type == "inlineStr")
        {
            return string.Concat(cell.DescendantNodes().OfType<XText>().Select(text => text.Value));
        }

        return value ?? string.Empty;
    }

    private static void SetTextCell(XDocument sheet, string column, int rowNumber, string value)
    {
        var cell = GetOrCreateCell(sheet, column, rowNumber);
        cell.RemoveNodes();
        cell.SetAttributeValue("t", "inlineStr");
        cell.Add(new XElement(Spreadsheet + "is", new XElement(Spreadsheet + "t", value ?? string.Empty)));
    }

    private static void SetNumberCell(XDocument sheet, string column, int rowNumber, decimal value)
    {
        var cell = GetOrCreateCell(sheet, column, rowNumber);
        cell.RemoveNodes();
        cell.SetAttributeValue("t", null);
        cell.Add(new XElement(Spreadsheet + "v", value.ToString(CultureInfo.InvariantCulture)));
    }

    private static void SetFormulaCell(XDocument sheet, string column, int rowNumber, string formula, decimal cachedValue)
    {
        var cell = GetOrCreateCell(sheet, column, rowNumber);
        cell.RemoveNodes();
        cell.SetAttributeValue("t", null);
        cell.Add(new XElement(Spreadsheet + "f", formula));
        cell.Add(new XElement(Spreadsheet + "v", cachedValue.ToString(CultureInfo.InvariantCulture)));
    }

    private static XElement GetOrCreateCell(XDocument sheet, string column, int rowNumber)
    {
        var sheetData = sheet.Root!.Element(Spreadsheet + "sheetData")!;
        var row = sheetData.Elements(Spreadsheet + "row")
            .SingleOrDefault(item => int.Parse(item.Attribute("r")!.Value, CultureInfo.InvariantCulture) == rowNumber);

        if (row is null)
        {
            row = new XElement(Spreadsheet + "row", new XAttribute("r", rowNumber));
            sheetData.Add(row);
        }

        var cellReference = $"{column}{rowNumber}";
        var cell = row.Elements(Spreadsheet + "c")
            .SingleOrDefault(item => string.Equals(item.Attribute("r")?.Value, cellReference, StringComparison.OrdinalIgnoreCase));

        if (cell is not null)
        {
            return cell;
        }

        cell = new XElement(Spreadsheet + "c", new XAttribute("r", cellReference));
        var insertBefore = row.Elements(Spreadsheet + "c")
            .FirstOrDefault(item => CompareColumns(GetColumnName(item.Attribute("r")!.Value), column) > 0);

        if (insertBefore is null)
        {
            row.Add(cell);
        }
        else
        {
            insertBefore.AddBeforeSelf(cell);
        }

        return cell;
    }

    private static void MarkWorkbookForRecalculation(ZipArchive archive)
    {
        var workbook = ReadXml(archive, "xl/workbook.xml");
        var calcPr = workbook.Root!.Element(Spreadsheet + "calcPr");
        if (calcPr is null)
        {
            calcPr = new XElement(Spreadsheet + "calcPr");
            workbook.Root!.Add(calcPr);
        }

        calcPr.SetAttributeValue("calcMode", "auto");
        calcPr.SetAttributeValue("fullCalcOnLoad", "1");
        calcPr.SetAttributeValue("forceFullCalc", "1");
        ReplaceXml(archive, "xl/workbook.xml", workbook);

        RemoveWorkbookCalcChainRelationship(archive);
        RemoveCalcChainFromContentTypes(archive);
        archive.GetEntry("xl/calcChain.xml")?.Delete();
    }

    private static void RemoveCalcChainFromContentTypes(ZipArchive archive)
    {
        var entry = archive.GetEntry("[Content_Types].xml");
        if (entry is null) return;

        var xml = ReadXml(entry);
        var overrides = xml.Root!
            .Elements(ContentTypes + "Override")
            .Where(e => string.Equals(e.Attribute("PartName")?.Value, "/xl/calcChain.xml", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (overrides.Count == 0) return;

        foreach (var o in overrides) o.Remove();
        ReplaceXml(archive, "[Content_Types].xml", xml);
    }

    private static void RemoveWorkbookCalcChainRelationship(ZipArchive archive)
    {
        const string workbookRelationshipsPath = "xl/_rels/workbook.xml.rels";
        var entry = archive.GetEntry(workbookRelationshipsPath);
        if (entry is null)
        {
            return;
        }

        var relationships = ReadXml(entry);
        var calcChainRelationships = relationships
            .Root!
            .Elements(PackageRelationships + "Relationship")
            .Where(relationship =>
                string.Equals(relationship.Attribute("Target")?.Value, "calcChain.xml", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(relationship.Attribute("Type")?.Value, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/calcChain", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var relationship in calcChainRelationships)
        {
            relationship.Remove();
        }

        if (calcChainRelationships.Count > 0)
        {
            ReplaceXml(archive, workbookRelationshipsPath, relationships);
        }
    }

    private static int CompareColumns(string left, string right)
    {
        return ColumnNumber(left).CompareTo(ColumnNumber(right));
    }

    private static int ColumnNumber(string column)
    {
        var value = 0;
        foreach (var character in column.ToUpperInvariant())
        {
            value = (value * 26) + character - 'A' + 1;
        }

        return value;
    }

    private static string GetColumnName(string cellReference)
    {
        return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path) ?? throw new FileNotFoundException($"Workbook entry not found: {path}");
        return ReadXml(entry);
    }

    private static XDocument ReadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static void ReplaceXml(ZipArchive archive, string path, XDocument xml)
    {
        archive.GetEntry(path)?.Delete();
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        xml.Save(stream);
    }
}

public sealed record LegacyCostingQuoteLine(
    string FormatName,
    string Code,
    string Description,
    decimal? QuotedPrice,
    decimal PackCost,
    decimal RecipeCostPer1000L,
    decimal GrossProfitPercent,
    decimal DilutionParts);

internal sealed record LegacySheetMap(
    string SheetName,
    LegacySheetKind Kind,
    string GrossProfitColumn,
    string PackedCostColumn,
    string LiquidCostColumn,
    string? PackCostColumn,
    string RecipeCostColumn,
    string PackCostFormulaCell,
    decimal RecipeCostMultiplier)
{
    public static LegacySheetMap Bulk(string sheetName, string packCostFormulaCell)
    {
        return new LegacySheetMap(
            sheetName,
            LegacySheetKind.Bulk,
            "I",
            "J",
            "K",
            "L",
            "M",
            packCostFormulaCell,
            1m);
    }

    public static LegacySheetMap Packaged(
        string sheetName,
        string grossProfitColumn,
        string packedCostColumn,
        string liquidCostColumn,
        string? packCostColumn,
        string recipeCostColumn,
        string packCostFormulaCell,
        decimal recipeCostMultiplier)
    {
        return new LegacySheetMap(
            sheetName,
            LegacySheetKind.Packaged,
            grossProfitColumn,
            packedCostColumn,
            liquidCostColumn,
            packCostColumn,
            recipeCostColumn,
            packCostFormulaCell,
            recipeCostMultiplier);
    }
}

internal enum LegacySheetKind
{
    Bulk,
    Packaged
}
