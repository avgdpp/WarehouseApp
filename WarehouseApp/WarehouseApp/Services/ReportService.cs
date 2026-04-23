using System.Text;
using NLog;
using WarehouseApp.Data.Repositories;
using WarehouseApp.Models;

namespace WarehouseApp.Services;

public interface IReportService
{
    List<Shipment> GetShipmentsByPeriod(DateTime from, DateTime to);
    string ExportToCsv(List<Shipment> shipments);
}

public class ReportService : IReportService
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly IShipmentRepository _shipRepo;
    private readonly ICurrencyService? _currencyService;

    public ReportService(IShipmentRepository shipRepo, ICurrencyService? currencyService = null)
    {
        _shipRepo = shipRepo;
        _currencyService = currencyService;
    }

    public List<Shipment> GetShipmentsByPeriod(DateTime from, DateTime to)
    {
        logger.Debug("Построение отчёта по отгрузкам за период {From:d} – {To:d}", from, to);

        var result = _shipRepo.GetAll()
            .Where(s => s.ShippedAt.Date >= from.Date && s.ShippedAt.Date <= to.Date)
            .OrderByDescending(s => s.ShippedAt)
            .ToList();

        logger.Info("Отчёт за период {From:d} – {To:d}: найдено {Count} отгрузок",
            from, to, result.Count);
        return result;
    }

    public string ExportToCsv(List<Shipment> shipments)
    {
        logger.Info("Экспорт отчёта в CSV ({Count} отгрузок)", shipments.Count);

        // Экспортируем в текущей валюте настроек, но суммы каждой отгрузки
        // пересчитываем по её собственному историческому курсу —
        // это соответствует принципу неизменности исторических данных.
        var settings = _currencyService?.Settings;
        string currency = settings?.Currency ?? "RUB";
        string currencyLabel = currency == "RUB" ? "₽" : currency;

        var sb = new StringBuilder();
        sb.AppendLine($"Дата;Покупатель;Адрес;Сумма отгрузки ({currencyLabel});Себестоимость ({currencyLabel});Прибыль ({currencyLabel})");
        foreach (var s in shipments)
        {
            decimal sum = ConvertRubToCurrent(s.TotalCost, s, settings);
            decimal cost = ConvertRubToCurrent(s.TotalPurchaseCost, s, settings);
            decimal profit = ConvertRubToCurrent(s.Profit, s, settings);

            sb.AppendLine($"{s.ShippedAt:dd.MM.yyyy};" +
                          $"{Escape(s.Recipient)};" +
                          $"{Escape(s.Address)};" +
                          $"{sum:N2};" +
                          $"{cost:N2};" +
                          $"{profit:N2}");
        }
        return sb.ToString();
    }

    /// <summary>Конвертирует сумму в рублях в текущую валюту настроек,
    /// используя курс, зафиксированный в момент совершения отгрузки.
    /// Fallback на актуальный курс, если исторический не сохранён.</summary>
    private static decimal ConvertRubToCurrent(decimal rub, Shipment s, AppSettings? settings)
    {
        if (settings == null || settings.Currency == "RUB") return rub;
        decimal rate = s.GetStoredRate(settings.Currency) ?? settings.GetRate(settings.Currency);
        if (rate <= 0) return rub;
        return Math.Round(rub / rate, 2);
    }

    private static string Escape(string val)
    {
        if (val.Contains(';') || val.Contains('"'))
            return $"\"{val.Replace("\"", "\"\"")}\"";
        return val;
    }
}
