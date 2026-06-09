using SimpsonsBeverages.QuotingTool.Calculations;
using SimpsonsBeverages.QuotingTool.App;
using SimpsonsBeverages.QuotingTool.RegressionHarness;
using System.IO.Compression;
using System.Xml.Linq;

const string ridgeviewWorkbook = @"\\adserver2\Company Share\Sales\Quotes\2026\Edmunds\2026-05-13 RTD PROJECT (RIDGEVIEW) SUBMISSION 1.xlsx";
const string daisyVendingWorkbook = @"\\adserver2\Company Share\Sales\Quotes\2026\Daisy Vending - Elite Dairies\2026-06-03 MANGO & PASSIONFRUIT, LEMON MERINGUE.xlsx";
const string bookerRoundTwoWorkbook = @"\\adserver2\Company Share\Sales\Quotes\2026\Booker\2026-04-13 COFFEE SYRUP TENDER ROUND 2.xlsx";
const string masterTemplate = @"\\adserver2\Company Share\Sales\Quotes\COSTING MASTER TEMPLATE V4.1.2.3.xlsm";
const decimal tolerance = 0.000000001m;

RunIbcRegression();
RunBulkRegression(
    "TOSCA",
    @"\\adserver2\Company Share\Sales\Quotes\2026\2026-04-02 BABCO CHARRED PINEAPPLE, BANANA, MANGO & GINGER COMPOUND.xlsx",
    expectedLineCount: 3);
RunPackagedRegression(
    "10L",
    @"\\adserver2\Company Share\Sales\Quotes\2026\2026-02-27 SCHOOL PRODUCTS.xlsx",
    expectedLineCount: 6,
    recipeCostMultiplier: 0.01m);
RunPackagedRegression(
    "2X5L",
    @"\\adserver2\Company Share\Sales\Quotes\2026\18 Gin\2026-05-11 LEMONADE.xlsx",
    expectedLineCount: 2,
    recipeCostMultiplier: 0.01m);
RunPackagedRegression(
    "6X1L",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Booker\2026-03-04 COFFEE SYRUP TENDER.xlsx",
    expectedLineCount: 12,
    recipeCostMultiplier: 0.006m);
RunPackagedRegression(
    "4X5L",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Belux Imports\2026-02-04.xlsx",
    expectedLineCount: 11,
    recipeCostMultiplier: 0.02m);
RunPackagedRegression(
    "6X2L",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Shott Beverages\2026-05- MANGO PUREE CHAIIWALA.xlsx",
    expectedLineCount: 2,
    recipeCostMultiplier: 1m / 83.3333333333m);
RunBulkRegression(
    "25L",
    @"\\adserver2\Company Share\Sales\Quotes\2026\New Juice\2026-06-01 NEW JUICE.xlsx",
    expectedLineCount: 2);
RunBulkRegression(
    "220L DRUM",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Impression beverages\2026-03-05 IMPRESSION BEVERAGES.xlsx",
    expectedLineCount: 3);
RunPackagedRegressionWithSelector(
    "770ML POUCH",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Costa\2026-03-10 COSTA EXPRESS GIVAUDAN FLAVOURS.xlsx",
    expectedLineCount: 11,
    recipeCostMultiplierForLine: line => line.Unit == "2X770ML" ? 0.00154m : 0.00308m);
RunPackagedRegression(
    "2X2.25L POUCH",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Think Drinks\2026-01-08 THINK DRINKS LOUNGERS TENDER.xlsx",
    expectedLineCount: 16,
    recipeCostMultiplier: 0.0045m);
RunPackagedRegression(
    "2X600ML POUCH",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Think Drinks\2026-02-17 THINK DRINKS FUNCTIONAL PERFECTORS ex Lions Mane A and B, collegen, cordyceps.xlsx",
    expectedLineCount: 11,
    recipeCostMultiplier: 0.0012m);
RunPackagedRegression(
    "12X750ML",
    @"\\adserver2\Company Share\Sales\Quotes\2026\Green Key Solutions\2026-03-11 MONSTER MATCHES SLUSH.xlsx",
    expectedLineCount: 3,
    recipeCostMultiplier: 0.009m);
RunRtdPerLitreConversionRegression();
RunManualLitreRegression();
RunLegacyCostingWorkbookIoRegression();
RunDaisyVendingImportRegression();
RunBookerRoundTwoImportRegression();
RunBookerManualTabImportRegression();

Console.WriteLine("All regression checks passed.");

static void RunIbcRegression()
{
    var lines = WorkbookReader.ReadIbcQuoteLines(ridgeviewWorkbook);

    Console.WriteLine("Ridgeview IBC regression");
    Console.WriteLine($"Workbook: {ridgeviewWorkbook}");
    Console.WriteLine($"Lines: {lines.Count}");

    if (lines.Count != 4)
    {
        Fail($"Expected 4 quote lines, found {lines.Count}.");
    }

    foreach (var expected in lines)
    {
        var actual = IbcQuoteCalculator.Calculate(new IbcQuoteInput(
            expected.Code,
            expected.Description,
            expected.RecipeCostPer1000L,
            expected.PackCostPer1000L,
            expected.GrossProfit,
            DilutionParts: 6m));

        AssertClose(expected.TotalCostPer1000L, actual.TotalCostPer1000L, expected, nameof(actual.TotalCostPer1000L));
        AssertClose(expected.PerLitreCost, actual.PerLitreCost, expected, nameof(actual.PerLitreCost));
        AssertClose(expected.PricePerUnit, actual.PricePerUnit, expected, nameof(actual.PricePerUnit));
        AssertClose(expected.RtdPricePerLitre, actual.RtdPricePerLitre, expected, nameof(actual.RtdPricePerLitre));

        Console.WriteLine($"PASS IBC row {expected.Row}: {expected.Code} {expected.Description}");
    }
}

static void RunPackagedRegression(string sheetName, string workbookPath, int expectedLineCount, decimal recipeCostMultiplier)
{
    RunPackagedRegressionWithSelector(sheetName, workbookPath, expectedLineCount, _ => recipeCostMultiplier);
}

static void RunPackagedRegressionWithSelector(
    string sheetName,
    string workbookPath,
    int expectedLineCount,
    Func<PackagedWorkbookQuoteLine, decimal> recipeCostMultiplierForLine)
{
    var lines = WorkbookReader.ReadPackagedQuoteLines(workbookPath, sheetName);

    Console.WriteLine();
    Console.WriteLine($"{sheetName} regression");
    Console.WriteLine($"Workbook: {workbookPath}");
    Console.WriteLine($"Lines: {lines.Count}");

    if (lines.Count != expectedLineCount)
    {
        Fail($"{sheetName}: expected {expectedLineCount} quote lines, found {lines.Count}.");
    }

    foreach (var expected in lines)
    {
        var actual = PackagedQuoteCalculator.Calculate(new PackagedQuoteInput(
            expected.Format,
            expected.Code,
            expected.Description,
            expected.RecipeCostPer1000L,
            expected.PackCost,
            expected.GrossProfit,
            recipeCostMultiplierForLine(expected)));

        AssertPackagedClose(expected.LiquidCost, actual.LiquidCost, expected, nameof(actual.LiquidCost));
        AssertPackagedClose(expected.PackedCost, actual.PackedCost, expected, nameof(actual.PackedCost));
        AssertPackagedClose(expected.PricePerUnit, actual.PricePerUnit, expected, nameof(actual.PricePerUnit));

        Console.WriteLine($"PASS {sheetName} row {expected.Row}: {expected.Code} {expected.Description}");
    }
}

static void RunBulkRegression(string sheetName, string workbookPath, int expectedLineCount)
{
    var lines = WorkbookReader.ReadBulkQuoteLines(workbookPath, sheetName);

    Console.WriteLine();
    Console.WriteLine($"{sheetName} regression");
    Console.WriteLine($"Workbook: {workbookPath}");
    Console.WriteLine($"Lines: {lines.Count}");

    if (lines.Count != expectedLineCount)
    {
        Fail($"{sheetName}: expected {expectedLineCount} quote lines, found {lines.Count}.");
    }

    foreach (var expected in lines)
    {
        var actual = BulkQuoteCalculator.Calculate(new BulkQuoteInput(
            expected.Format,
            expected.Code,
            expected.Description,
            expected.RecipeCostPer1000L,
            expected.PackCostPer1000L,
            expected.GrossProfit));

        AssertBulkClose(expected.TotalCostPer1000L, actual.TotalCostPer1000L, expected, nameof(actual.TotalCostPer1000L));
        AssertBulkClose(expected.PerLitreCost, actual.PerLitreCost, expected, nameof(actual.PerLitreCost));
        AssertBulkClose(expected.PricePerUnit, actual.PricePerUnit, expected, nameof(actual.PricePerUnit));

        Console.WriteLine($"PASS {sheetName} row {expected.Row}: {expected.Code} {expected.Description}");
    }
}

static void RunRtdPerLitreConversionRegression()
{
    Console.WriteLine();
    Console.WriteLine("RTD per litre conversion regression");

    AssertRtdConversion("IBC", pricePerUnit: 2.4087m, sellableUnitLitres: 1m, dilutionParts: 6m, expected: 0.40145m);
    AssertRtdConversion("10L", pricePerUnit: 25.129m, sellableUnitLitres: 10m, dilutionParts: 6m, expected: 0.4188166666666666666666666667m);
    AssertRtdConversion("2X5L", pricePerUnit: 30m, sellableUnitLitres: 10m, dilutionParts: 6m, expected: 0.5m);
    AssertRtdConversion("6X1L", pricePerUnit: 18m, sellableUnitLitres: 6m, dilutionParts: 6m, expected: 0.5m);

    Console.WriteLine("PASS RTD per litre conversion");
}

static void RunManualLitreRegression()
{
    Console.WriteLine();
    Console.WriteLine("Manual line regression");

    var actual = BulkQuoteCalculator.Calculate(new BulkQuoteInput(
        "MANUAL LINE",
        "MANUAL",
        "Manual line",
        RecipeCostPer1000L: 1000m,
        PackCostPer1000L: 250m,
        GrossProfit: 0.5m));

    AssertManualClose(1250m, actual.TotalCostPer1000L, nameof(actual.TotalCostPer1000L));
    AssertManualClose(1.25m, actual.PerLitreCost, nameof(actual.PerLitreCost));
    AssertManualClose(2.5m, actual.PricePerUnit, nameof(actual.PricePerUnit));

    Console.WriteLine("PASS manual line regression");
}

static void RunLegacyCostingWorkbookIoRegression()
{
    Console.WriteLine();
    Console.WriteLine("Legacy costing workbook IO regression");

    var tempPath = Path.Combine(Path.GetTempPath(), $"quoting-tool-legacy-{Guid.NewGuid():N}.xlsm");
    try
    {
        var formats = new[]
        {
            FormatDefinition.FromMaster("IBC", CalculationKind.Bulk, 1m, "LITRE", 1m, 6m, []),
            FormatDefinition.FromMaster("10L", CalculationKind.Packaged, 0.01m, "10L", 10m, 6m, [])
        };

        var ibc = new QuoteLineViewModel(formats)
        {
            FormatName = "IBC",
            Code = "TESTIBC",
            Description = "TEST IBC 1 + 5",
            PackCost = 256.46m,
            RecipeCostPer1000L = 972m,
            TargetGpPercent = 49m,
            DilutionParts = 6m
        };
        ibc.Calculate();

        var tenL = new QuoteLineViewModel(formats)
        {
            FormatName = "10L",
            Code = "TEST10L",
            Description = "TEST 10L 1 + 5",
            PackCost = 3.3816902058823528m,
            RecipeCostPer1000L = 1000m,
            TargetGpPercent = 50m,
            DilutionParts = 6m
        };
        tenL.Calculate();

        LegacyCostingWorkbookIo.Export(masterTemplate, tempPath, [ibc, tenL]);
        AssertLegacyFormula(tempPath, "IBC", "I4", "(D4-J4)/D4");
        AssertLegacyFormula(tempPath, "IBC", "J4", "K4/1000");
        AssertLegacyFormula(tempPath, "IBC", "K4", "M4+L4");
        AssertLegacyFormula(tempPath, "IBC", "L4", "$C$27");
        AssertLegacyFormula(tempPath, "10L", "H4", "(D4-I4)/D4");
        AssertLegacyFormula(tempPath, "10L", "I4", "J4+K4");
        AssertLegacyFormula(tempPath, "10L", "J4", "L4*0.01");
        AssertLegacyFormula(tempPath, "10L", "K4", "$C$29");
        AssertNoWorkbookCalcChainRelationship(tempPath);

        var imported = LegacyCostingWorkbookIo.Import(tempPath);

        if (imported.Count < 2)
        {
            Fail($"Legacy import expected at least 2 lines, found {imported.Count}.");
        }

        var importedIbc = imported.Single(line => line.Code == "TESTIBC");
        AssertLegacyClose(ibc.PricePerUnit, importedIbc.QuotedPrice.GetValueOrDefault(), "IBC quoted price");
        AssertLegacyClose(256.46m, importedIbc.PackCost, "IBC pack cost");
        AssertLegacyClose(972m, importedIbc.RecipeCostPer1000L, "IBC recipe cost");
        AssertLegacyClose(CalculateImportedGp(ibc.PricePerUnit, (972m + 256.46m) / 1000m), importedIbc.GrossProfitPercent, "IBC calculated GP");

        var importedTenL = imported.Single(line => line.Code == "TEST10L");
        AssertLegacyClose(tenL.PricePerUnit, importedTenL.QuotedPrice.GetValueOrDefault(), "10L quoted price");
        AssertLegacyClose(3.3816902058823528m, importedTenL.PackCost, "10L pack cost");
        AssertLegacyClose(1000m, importedTenL.RecipeCostPer1000L, "10L recipe cost");
        AssertLegacyClose(CalculateImportedGp(tenL.PricePerUnit, (1000m * 0.01m) + 3.3816902058823528m), importedTenL.GrossProfitPercent, "10L calculated GP");

        Console.WriteLine("PASS legacy costing workbook IO regression");
    }
    finally
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }
}

static void RunDaisyVendingImportRegression()
{
    Console.WriteLine();
    Console.WriteLine("Daisy Vending legacy import regression");

    var imported = LegacyCostingWorkbookIo.Import(daisyVendingWorkbook);

    if (imported.Count != 4)
    {
        Fail($"Daisy Vending import expected 4 lines, found {imported.Count}.");
    }

    var lemon = imported.Single(line => line.Code == "R110126");
    AssertLegacyText("4X5L", lemon.FormatName, "Daisy lemon format");
    AssertLegacyClose(6.1782448m, lemon.PackCost, "Daisy lemon pack cost");
    AssertLegacyClose(1233.835m, lemon.RecipeCostPer1000L, "Daisy lemon recipe cost");
    if (lemon.QuotedPrice.HasValue)
    {
        Fail($"Daisy lemon blank quoted price: expected blank, actual {lemon.QuotedPrice.Value}.");
    }
    AssertLegacyClose(50m, lemon.GrossProfitPercent, "Daisy lemon default GP");

    var mango = imported.Single(line => line.Code == "R110054");
    AssertLegacyText("4X5L", mango.FormatName, "Daisy mango format");
    AssertLegacyClose(6.1782448m, mango.PackCost, "Daisy mango pack cost");
    AssertLegacyClose(1218.4224m, mango.RecipeCostPer1000L, "Daisy mango recipe cost");

    Console.WriteLine("PASS Daisy Vending legacy import regression");
}

static void RunBookerRoundTwoImportRegression()
{
    Console.WriteLine();
    Console.WriteLine("Booker round two legacy import regression");

    var imported = LegacyCostingWorkbookIo.Import(bookerRoundTwoWorkbook);

    if (imported.Count != 11)
    {
        Fail($"Booker round two import expected 11 lines, found {imported.Count}.");
    }

    var caramel = imported.Single(line => line.Code == @"180132\003");
    AssertLegacyText("6X1L", caramel.FormatName, "Booker caramel format");
    AssertLegacyClose(397.88339999999999m, caramel.RecipeCostPer1000L, "Booker caramel recipe cost");
    AssertLegacyClose(3.8361299999999998m, caramel.PackCost, "Booker caramel pack cost");

    var hazelnut = imported.Single(line => line.Code == @"200183\002");
    AssertLegacyClose(1800.0733m, hazelnut.RecipeCostPer1000L, "Booker hazelnut recipe cost");

    var chai = imported.Single(line => line.Code == @"200144\001");
    AssertLegacyClose(888.11779999999999m, chai.RecipeCostPer1000L, "Booker chai recipe cost");

    Console.WriteLine("PASS Booker round two legacy import regression");
}

static void RunBookerManualTabImportRegression()
{
    Console.WriteLine();
    Console.WriteLine("Booker manual tab import regression");

    var imported = LegacyCostingWorkbookIo.ImportSheet(bookerRoundTwoWorkbook, "WITH BREAKS FLAV REDUCTION", "6X1L");

    if (imported.Count != 11)
    {
        Fail($"Booker manual tab import expected 11 lines, found {imported.Count}.");
    }

    var caramel = imported.Single(line => line.Code == @"200178\002");
    AssertLegacyText("6X1L", caramel.FormatName, "Booker manual tab caramel format");
    AssertLegacyClose(1371.9325999999999m, caramel.RecipeCostPer1000L, "Booker manual tab caramel recipe cost");

    var vanilla = imported.Single(line => line.Code == @"200179\002");
    AssertLegacyClose(1421.3665000000001m, vanilla.RecipeCostPer1000L, "Booker manual tab vanilla recipe cost");

    Console.WriteLine("PASS Booker manual tab import regression");
}

static void AssertRtdConversion(string format, decimal pricePerUnit, decimal sellableUnitLitres, decimal dilutionParts, decimal expected)
{
    var actual = pricePerUnit / sellableUnitLitres / dilutionParts;
    if (Math.Abs(expected - actual) <= tolerance)
    {
        return;
    }

    Fail($"{format} RTD GBP/L: expected {expected}, actual {actual}, difference {actual - expected}.");
}

static decimal CalculateImportedGp(decimal quotedPrice, decimal cost)
{
    return ((quotedPrice - cost) / quotedPrice) * 100m;
}

static void AssertManualClose(decimal expected, decimal actual, string field)
{
    if (Math.Abs(expected - actual) <= tolerance)
    {
        return;
    }

    Fail($"Manual line {field}: expected {expected}, actual {actual}, difference {actual - expected}.");
}

static void AssertLegacyClose(decimal expected, decimal actual, string field)
{
    if (Math.Abs(expected - actual) <= tolerance)
    {
        return;
    }

    Fail($"Legacy workbook {field}: expected {expected}, actual {actual}, difference {actual - expected}.");
}

static void AssertLegacyText(string expected, string actual, string field)
{
    if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    Fail($"Legacy workbook {field}: expected {expected}, actual {actual}.");
}

static void AssertLegacyFormula(string workbookPath, string sheetName, string cellReference, string expectedFormula)
{
    XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    XNamespace packageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    XNamespace officeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    using var archive = ZipFile.OpenRead(workbookPath);
    var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
    var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels")
        .Descendants(packageRelationships + "Relationship")
        .ToDictionary(
            relationship => relationship.Attribute("Id")!.Value,
            relationship => relationship.Attribute("Target")!.Value);

    var sheet = workbook
        .Descendants(spreadsheet + "sheet")
        .Single(sheet => string.Equals(sheet.Attribute("name")?.Value, sheetName, StringComparison.OrdinalIgnoreCase));

    var relationshipId = sheet.Attribute(officeRelationships + "id")!.Value;
    var target = relationships[relationshipId].TrimStart('/');
    var sheetPath = target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? target : $"xl/{target}";
    var worksheet = ReadWorkbookXml(archive, sheetPath);

    var cell = worksheet
        .Descendants(spreadsheet + "c")
        .SingleOrDefault(cell => string.Equals(cell.Attribute("r")?.Value, cellReference, StringComparison.OrdinalIgnoreCase));
    var actualFormula = cell?.Element(spreadsheet + "f")?.Value;

    if (actualFormula == expectedFormula)
    {
        return;
    }

    Fail($"{sheetName}!{cellReference} formula: expected {expectedFormula}, actual {actualFormula ?? "<missing>"}.");
}

static void AssertNoWorkbookCalcChainRelationship(string workbookPath)
{
    XNamespace packageRelationships = "http://schemas.openxmlformats.org/package/2006/relationships";

    using var archive = ZipFile.OpenRead(workbookPath);
    var hasCalcChainPart = archive.GetEntry("xl/calcChain.xml") is not null;
    var hasCalcChainRelationship = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels")
        .Descendants(packageRelationships + "Relationship")
        .Any(relationship =>
            string.Equals(relationship.Attribute("Target")?.Value, "calcChain.xml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(relationship.Attribute("Type")?.Value, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/calcChain", StringComparison.OrdinalIgnoreCase));

    if (!hasCalcChainPart && !hasCalcChainRelationship)
    {
        return;
    }

    Fail("Legacy export should remove both xl/calcChain.xml and the workbook calcChain relationship.");
}

static XDocument ReadWorkbookXml(ZipArchive archive, string path)
{
    var entry = archive.GetEntry(path) ?? throw new FileNotFoundException($"Workbook entry not found: {path}");
    using var stream = entry.Open();
    return XDocument.Load(stream);
}

static void AssertClose(decimal expected, decimal actual, WorkbookQuoteLine line, string field)
{
    if (Math.Abs(expected - actual) <= tolerance)
    {
        return;
    }

    Fail($"Row {line.Row} {field}: expected {expected}, actual {actual}, difference {actual - expected}.");
}

static void AssertPackagedClose(decimal expected, decimal actual, PackagedWorkbookQuoteLine line, string field)
{
    if (Math.Abs(expected - actual) <= tolerance)
    {
        return;
    }

    Fail($"{line.Format} row {line.Row} {field}: expected {expected}, actual {actual}, difference {actual - expected}.");
}

static void AssertBulkClose(decimal expected, decimal actual, BulkWorkbookQuoteLine line, string field)
{
    if (Math.Abs(expected - actual) <= tolerance)
    {
        return;
    }

    Fail($"{line.Format} row {line.Row} {field}: expected {expected}, actual {actual}, difference {actual - expected}.");
}

static void Fail(string message)
{
    Console.Error.WriteLine(message);
    Environment.ExitCode = 1;
    throw new InvalidOperationException(message);
}
