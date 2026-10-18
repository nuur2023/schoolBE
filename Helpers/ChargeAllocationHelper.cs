using schoolBE.Models.DTOs;

namespace schoolBE.Helpers;

/// <summary>
/// Splits gross fee lines into net amounts after subtracting a per-student discount (proportionally).
/// </summary>
public static class ChargeAllocationHelper
{
    public static List<decimal> AllocateAfterDiscount(decimal discount, IReadOnlyList<FeeCategoryCharge> fees)
    {
        if (fees == null || fees.Count == 0)
            return new List<decimal>();

        var gross = fees.Sum(f => f.Amount);
        if (gross <= 0)
            return fees.Select(_ => 0m).ToList();

        var net = Math.Max(0m, gross - discount);
        if (net <= 0m)
            return fees.Select(_ => 0m).ToList();

        var result = new List<decimal>(fees.Count);
        decimal allocated = 0m;
        for (var i = 0; i < fees.Count; i++)
        {
            if (i == fees.Count - 1)
            {
                result.Add(Math.Round(net - allocated, 2, MidpointRounding.AwayFromZero));
            }
            else
            {
                var portion = Math.Round(fees[i].Amount / gross * net, 2, MidpointRounding.AwayFromZero);
                result.Add(portion);
                allocated += portion;
            }
        }

        return result;
    }
}
