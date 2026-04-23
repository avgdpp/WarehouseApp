using System.Text.Json;

namespace WarehouseApp.Models;

/// <summary>Настройки приложения (валюта и т.д.)</summary>
public class AppSettings
{
    public string Currency { get; set; } = "RUB";
    public decimal UsdRate { get; set; } = 90m;
    public decimal EurRate { get; set; } = 98m;
    public decimal UsdtRate { get; set; } = 90m;
    public DateTime RatesUpdatedAt { get; set; } = DateTime.MinValue;

    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    internal decimal GetRate(string currency) => currency switch
    {
        "USD" => UsdRate,
        "EUR" => EurRate,
        "USDT" => UsdtRate,
        _ => 1m
    };

    public string CurrencySymbol => Currency switch
    {
        "USD" => "$",
        "EUR" => "€",
        "USDT" => "₮",
        _ => "р."
    };

    internal decimal ConvertFromRub(decimal rubAmount)
    {
        if (Currency == "RUB") return rubAmount;
        var rate = GetRate(Currency);
        return rate > 0 ? Math.Round(rubAmount / rate, 2) : rubAmount;
    }

    /// <summary>Переводит сумму из текущей валюты в рубли для хранения в БД</summary>
    internal decimal ConvertToRub(decimal amount)
    {
        if (Currency == "RUB") return amount;
        var rate = GetRate(Currency);
        return Math.Round(amount * rate, 2);
    }

    internal string FormatPrice(decimal rubAmount)
    {
        if (Currency == "RUB") return $"{rubAmount:N0} р.";
        var converted = ConvertFromRub(rubAmount);
        return $"{converted:N2} {CurrencySymbol}";
    }

    /// <summary>Форматирует сумму в рублях, используя исторический курс
    /// (на момент совершения операции). Если исторический курс не передан
    /// или равен 0 — используется актуальный курс из настроек.</summary>
    internal string FormatPriceAt(decimal rubAmount, decimal? historicalRate)
    {
        if (Currency == "RUB") return $"{rubAmount:N0} р.";
        var rate = (historicalRate.HasValue && historicalRate.Value > 0)
            ? historicalRate.Value
            : GetRate(Currency);
        if (rate <= 0) return $"{rubAmount:N0} р.";
        var converted = Math.Round(rubAmount / rate, 2);
        return $"{converted:N2} {CurrencySymbol}";
    }

    internal static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    internal void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch { }
    }
}
