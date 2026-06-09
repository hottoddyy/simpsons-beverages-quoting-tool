using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace SimpsonsBeverages.QuotingTool.RegressionHarness;

internal sealed record WorkbookQuoteLine(
    int Row,
    string Code,
    string Description,
    string Unit,
    decimal PricePerUnit,
    decimal RtdPricePerLitre,
    decimal GrossProfit,
    decimal PerLitreCost,
    decimal TotalCostPer1000L,
    decimal PackCostPer1000L,
    decimal RecipeCostPer1000L);

internal sealed record PackagedWorkbookQuoteLine(
    int Row,
    string Format,
    string Code,
    string Description,
    string Unit,
    decimal PricePerUnit,
    decimal GrossProfit,
    decimal PackedCost,
    decimal LiquidCost,
    decimal PackCost,
    decimal RecipeCostPer1000L);

internal sealed record BulkWorkbookQuoteLine(
    int Row,
    string Format,
    string Code,
    string Description,
    string Unit,
    decimal PricePerUnit,
    decimal GrossProfit,
    decimal PerLitreCost,
    decimal TotalCostPer1000L,
    decimal PackCostPer1000L,
    decimal RecipeCostPer1000L);

internal static class WorkbookReader
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace PackageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static IReadOnlyList<WorkbookQuoteLine> ReadIbcQuoteLines(string workbookPath)
    {
        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPath = GetSheetPath(archive, "IBC");
        var sheet = ReadXml(archive, sheetPath);

        return sheet
            .Descendants(Spreadsheet + "row")
            .Select(row => ReadRow(row, sharedStrings))
            .Where(row => row.RowNumber is >= 4 and <= 20)
            .Where(row => row.Cells.TryGetValue("A", out var code) && !string.IsNullOrWhiteSpace(code))
            .Select(row => new WorkbookQuoteLine(
                row.RowNumber,
                row.Cells.GetValueOrDefault("A", string.Empty),
                row.Cells.GetValueOrDefault("B", string.Empty),
                row.Cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(row.Cells["D"]),
                ParseDecimal(row.Cells["F"]),
                ParseDecimal(row.Cells["I"]),
                ParseDecimal(row.Cells["J"]),
                ParseDecimal(row.Cells["K"]),
                ParseDecimal(row.Cells["L"]),
                ParseDecimal(row.Cells["M"])))
            .ToList();
    }

    public static IReadOnlyList<PackagedWorkbookQuoteLine> ReadPackagedQuoteLines(string workbookPath, string sheetName)
    {
        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPath = GetSheetPath(archive, sheetName);
        var sheet = ReadXml(archive, sheetPath);

        return sheet
            .Descendants(Spreadsheet + "row")
            .Select(row => ReadRow(row, sharedStrings))
            .Where(row => row.RowNumber is >= 4 and <= 20)
            .Where(HasQuoteIdentity)
            .Where(row => HasPackagedCostColumns(sheetName, row))
            .Select(row => ToPackagedQuoteLine(sheetName, row))
            .ToList();
    }

    public static IReadOnlyList<BulkWorkbookQuoteLine> ReadBulkQuoteLines(string workbookPath, string sheetName)
    {
        using var archive = ZipFile.OpenRead(workbookPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetPath = GetSheetPath(archive, sheetName);
        var sheet = ReadXml(archive, sheetPath);

        return sheet
            .Descendants(Spreadsheet + "row")
            .Select(row => ReadRow(row, sharedStrings))
            .Where(row => row.RowNumber is >= 4 and <= 20)
            .Where(HasQuoteIdentity)
            .Select(row => new BulkWorkbookQuoteLine(
                row.RowNumber,
                sheetName,
                row.Cells.GetValueOrDefault("A", string.Empty),
                row.Cells.GetValueOrDefault("B", string.Empty),
                row.Cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(row.Cells["D"]),
                ParseDecimal(row.Cells["I"]),
                ParseDecimal(row.Cells["J"]),
                ParseDecimal(row.Cells["K"]),
                ParseDecimal(row.Cells["L"]),
                ParseDecimal(row.Cells["M"])))
            .ToList();
    }

    private static PackagedWorkbookQuoteLine ToPackagedQuoteLine(
        string sheetName,
        (int RowNumber, Dictionary<string, string> Cells) row)
    {
        var cells = row.Cells;

        if (sheetName == "770ML POUCH")
        {
            var liquidCost = ParseDecimal(cells["K"]);
            var packedCost = ParseDecimal(cells["J"]);

            return new PackagedWorkbookQuoteLine(
                row.RowNumber,
                sheetName,
                cells.GetValueOrDefault("A", string.Empty),
                cells.GetValueOrDefault("B", string.Empty),
                cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(cells["D"]),
                ParseDecimal(cells["I"]),
                packedCost,
                liquidCost,
                packedCost - liquidCost,
                ParseDecimal(cells["M"]));
        }

        if (sheetName is "2X2.25L POUCH" or "2X600ML POUCH")
        {
            return new PackagedWorkbookQuoteLine(
                row.RowNumber,
                sheetName,
                cells.GetValueOrDefault("A", string.Empty),
                cells.GetValueOrDefault("B", string.Empty),
                cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(cells["D"]),
                ParseDecimal(cells["M"]),
                ParseDecimal(cells["N"]),
                ParseDecimal(cells["O"]),
                ParseDecimal(cells["P"]),
                ParseDecimal(cells["Q"]));
        }

        if (sheetName == "6X1L")
        {
            return new PackagedWorkbookQuoteLine(
                row.RowNumber,
                sheetName,
                cells.GetValueOrDefault("A", string.Empty),
                cells.GetValueOrDefault("B", string.Empty),
                cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(cells["D"]),
                ParseDecimal(cells["I"]),
                ParseDecimal(cells["J"]),
                ParseDecimal(cells["K"]),
                ParseDecimal(cells["L"]),
                ParseDecimal(cells["M"]));
        }

        if (sheetName == "4X5L")
        {
            return new PackagedWorkbookQuoteLine(
                row.RowNumber,
                sheetName,
                cells.GetValueOrDefault("A", string.Empty),
                cells.GetValueOrDefault("B", string.Empty),
                cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(cells["D"]),
                ParseDecimal(cells["I"]),
                ParseDecimal(cells["J"]),
                ParseDecimal(cells["K"]),
                ParseDecimal(cells["L"]),
                ParseDecimal(cells["M"]));
        }

        if (sheetName == "6X2L")
        {
            return new PackagedWorkbookQuoteLine(
                row.RowNumber,
                sheetName,
                cells.GetValueOrDefault("A", string.Empty),
                cells.GetValueOrDefault("B", string.Empty),
                cells.GetValueOrDefault("C", string.Empty),
                ParseDecimal(cells["D"]),
                ParseDecimal(cells["G"]),
                ParseDecimal(cells["H"]),
                ParseDecimal(cells["I"]),
                ParseDecimal(cells["J"]),
                ParseDecimal(cells["K"]));
        }

        return new PackagedWorkbookQuoteLine(
            row.RowNumber,
            sheetName,
            cells.GetValueOrDefault("A", string.Empty),
            cells.GetValueOrDefault("B", string.Empty),
            cells.GetValueOrDefault("C", string.Empty),
            ParseDecimal(cells["D"]),
            ParseDecimal(cells["H"]),
            ParseDecimal(cells["I"]),
            ParseDecimal(cells["J"]),
            ParseDecimal(cells["K"]),
            ParseDecimal(cells["L"]));
    }

    private static bool HasQuoteIdentity((int RowNumber, Dictionary<string, string> Cells) row)
    {
        return
            row.Cells.TryGetValue("D", out var price) &&
            IsDecimal(price) &&
            (
                row.Cells.TryGetValue("A", out var code) && !string.IsNullOrWhiteSpace(code) ||
                row.Cells.TryGetValue("B", out var description) && !string.IsNullOrWhiteSpace(description)
            );
    }

    private static bool HasPackagedCostColumns(string sheetName, (int RowNumber, Dictionary<string, string> Cells) row)
    {
        string[] requiredColumns = sheetName switch
        {
            "6X1L" => ["D", "I", "J", "K", "L", "M"],
            "4X5L" => ["D", "I", "J", "K", "L", "M"],
            "6X2L" => ["D", "G", "H", "I", "J", "K"],
            "770ML POUCH" => ["D", "I", "J", "K", "M"],
            "2X2.25L POUCH" or "2X600ML POUCH" => ["D", "M", "N", "O", "P", "Q"],
            _ => ["D", "H", "I", "J", "K", "L"],
        };

        return requiredColumns.All(column => row.Cells.TryGetValue(column, out var value) && IsDecimal(value));
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

    private static string GetSheetPath(ZipArchive archive, string sheetName)
    {
        var workbook = ReadXml(archive, "xl/workbook.xml");
        var relationships = ReadXml(archive, "xl/_rels/workbook.xml.rels")
            .Descendants(PackageRelationships + "Relationship")
            .ToDictionary(
                relationship => relationship.Attribute("Id")!.Value,
                relationship => relationship.Attribute("Target")!.Value);

        var sheet = workbook
            .Descendants(Spreadsheet + "sheet")
            .Single(sheet => string.Equals(sheet.Attribute("name")?.Value, sheetName, StringComparison.OrdinalIgnoreCase));

        var relationshipId = sheet.Attribute(OfficeRelationships + "id")!.Value;
        var target = relationships[relationshipId].TrimStart('/');
        return target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : $"xl/{target}";
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

    private static string GetColumnName(string cellReference)
    {
        return new string(cellReference.TakeWhile(char.IsLetter).ToArray());
    }

    private static decimal ParseDecimal(string value)
    {
        return decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static bool IsDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
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
}
