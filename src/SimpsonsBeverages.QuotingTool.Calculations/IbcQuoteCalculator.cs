namespace SimpsonsBeverages.QuotingTool.Calculations;

public sealed record IbcQuoteInput(
    string Code,
    string Description,
    decimal RecipeCostPer1000L,
    decimal PackCostPer1000L,
    decimal GrossProfit,
    decimal DilutionParts);

public sealed record IbcQuoteResult(
    string Code,
    string Description,
    decimal PricePerUnit,
    decimal RtdPricePerLitre,
    decimal GrossProfit,
    decimal PerLitreCost,
    decimal TotalCostPer1000L,
    decimal PackCostPer1000L,
    decimal RecipeCostPer1000L);

public static class IbcQuoteCalculator
{
    public static IbcQuoteResult Calculate(IbcQuoteInput input)
    {
        if (input.GrossProfit >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Gross profit must be below 100%.");
        }

        if (input.DilutionParts <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Dilution parts must be greater than zero.");
        }

        var totalCostPer1000L = input.RecipeCostPer1000L + input.PackCostPer1000L;
        var perLitreCost = totalCostPer1000L / 1000m;
        var pricePerUnit = perLitreCost / (1m - input.GrossProfit);
        var rtdPricePerLitre = pricePerUnit / input.DilutionParts;

        return new IbcQuoteResult(
            input.Code,
            input.Description,
            pricePerUnit,
            rtdPricePerLitre,
            input.GrossProfit,
            perLitreCost,
            totalCostPer1000L,
            input.PackCostPer1000L,
            input.RecipeCostPer1000L);
    }
}
