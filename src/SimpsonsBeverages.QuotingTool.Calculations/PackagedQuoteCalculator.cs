namespace SimpsonsBeverages.QuotingTool.Calculations;

public sealed record PackagedQuoteInput(
    string Format,
    string Code,
    string Description,
    decimal RecipeCostPer1000L,
    decimal PackCost,
    decimal GrossProfit,
    decimal RecipeCostMultiplier);

public sealed record PackagedQuoteResult(
    string Format,
    string Code,
    string Description,
    decimal PricePerUnit,
    decimal GrossProfit,
    decimal PackedCost,
    decimal LiquidCost,
    decimal PackCost,
    decimal RecipeCostPer1000L);

public static class PackagedQuoteCalculator
{
    public static PackagedQuoteResult Calculate(PackagedQuoteInput input)
    {
        if (input.GrossProfit >= 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Gross profit must be below 100%.");
        }

        if (input.RecipeCostMultiplier <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "Recipe cost multiplier must be greater than zero.");
        }

        var liquidCost = input.RecipeCostPer1000L * input.RecipeCostMultiplier;
        var packedCost = liquidCost + input.PackCost;
        var pricePerUnit = packedCost / (1m - input.GrossProfit);

        return new PackagedQuoteResult(
            input.Format,
            input.Code,
            input.Description,
            pricePerUnit,
            input.GrossProfit,
            packedCost,
            liquidCost,
            input.PackCost,
            input.RecipeCostPer1000L);
    }
}
