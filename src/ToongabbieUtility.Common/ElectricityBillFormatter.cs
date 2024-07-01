using System.Globalization;
using System.Text;
using ToongabbieUtility.Domain;

namespace ToongabbieUtility.Common;

public static class ElectricityBillFormatter
{
    public static decimal GetTotalKwh(List<DailyHeaterUsage> dailyUsages)
    {
        return dailyUsages.Sum(d => d.TotalWattMinutes / 60000m);  // Divide by 60 minutes and 1000w
    }
    public static decimal GetTotalAmount(List<DailyHeaterUsage> dailyUsages,
        decimal unitPrice)
    {
        var totalKwh = GetTotalKwh(dailyUsages);
        return totalKwh * unitPrice;
    }
    public static string GenerateReportForTenant(List<DailyHeaterUsage> dailyUsages, decimal unitPrice)
    {
        CultureInfo culture = CultureInfo.CurrentCulture.Clone() as CultureInfo;
        culture.NumberFormat.CurrencySymbol = "$";
        CultureInfo.CurrentCulture = culture;
        
        var sb = new StringBuilder();
        foreach (var dailyUsage in dailyUsages)
        {
            var subtotalMinutes = dailyUsage.TotalMinutes;
            var subtotalTimeSpan = TimeSpan.FromMinutes(subtotalMinutes);
            var subtotalWattMinutes = dailyUsage.TotalWattMinutes;
            var subtotalKwh = subtotalWattMinutes / 60000m;
            var subtotalDollarAmount = subtotalKwh * unitPrice;
                    
            sb.AppendLine(
                $"{dailyUsage.Date:dddd, dd MMMM}: {subtotalTimeSpan:h'h 'mm'm'}, {subtotalKwh:0.0}Kwh, {subtotalDollarAmount:C}");
        }

        var totalTimeSpan = GetTotalTimeSpan(dailyUsages);
        var totalKwh = GetTotalKwh(dailyUsages);
        var totalAmount = GetTotalAmount(dailyUsages, unitPrice);

        sb.Append($"Total: {totalTimeSpan.TotalHours:0.0}hrs, {totalKwh:0.0}Kwh, {totalAmount:C}");
        
        return sb.ToString();
    }

    public static TimeSpan GetTotalTimeSpan(List<DailyHeaterUsage> dailyUsages)
    {
        var totalMinutes = dailyUsages.Sum(d => d.TotalMinutes);
        var totalTimeSpan = TimeSpan.FromMinutes(totalMinutes);
        return totalTimeSpan;
    }
}