namespace SimpsonsBeverages.QuotingTool.Calculations;

public sealed record BulkQuoteInput(
    string Format,
    string Code,
    string Description,
    decimal RecipeCostPer1000L,
    decimal PackCostPer1000L,
    decimal GrossProfit);

public sealed record BulkQuoteResult(
    string Format,
    string Code,
    string Description,
    decimal PricePerUnit,
    decimal GrossProfit,
    decimal PerLitreCost,
    decimal TotalCostPer1000L,
    decimal PackCostPer1000L,
    decimal RecipeCostPer1000L);

public static class BulkQuoteCalculator
{
    public static BulkQuoteResult Calculate(BulkQuoteInput input)
    {
        if (input.GrossProfit >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Gross profit must be below 100%.");
        }

        var totalCostPer1000L = input.RecipeCostPer1000L + input.PackCostPer1000L;
        var perLitreCost = totalCostPer1000L / 1000m;
        var pricePerUnit = perLitreCost / (1m - input.GrossProfit);

        return new BulkQuoteResult(
            input.Format,
            input.Code,
            input.Description,
            pricePerUnit,
            input.GrossProfit,
            perLitreCost,
            totalCostPer1000L,
            input.PackCostPer1000L,
            input.RecipeCostPer1000L);
    }
}
