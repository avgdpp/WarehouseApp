using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Models;

/// <summary>Партия товара с индивидуальным сроком реализации</summary>
public class ProductBatch
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Количество единиц в этой партии</summary>
    public int Quantity { get; set; }

    /// <summary>Дата окончания срока реализации (null = бессрочный).
    /// В БД хранится в старой колонке ExpiryDate — миграция не требуется.</summary>
    [Column("ExpiryDate")]
    public DateTime? SaleDeadline { get; set; }

    /// <summary>Закупочная цена партии</summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>Дата поступления (начало срока реализации)</summary>
    public DateTime ReceivedAt { get; set; } = DateTime.Now;

    /// <summary>ID поставки, из которой поступила партия (null = начальный остаток)</summary>
    public int? SupplyId { get; set; }
    public Supply? Supply { get; set; }

    // ---- Вычисляемые свойства ----

    /// <summary>Срок реализации вышел — партия подлежит списанию</summary>
    [NotMapped]
    public bool IsOverdue => SaleDeadline.HasValue && SaleDeadline.Value.Date < DateTime.Today;

    /// <summary>Доля пройденного срока реализации: 0 — только поступил, 1 — полностью истёк</summary>
    [NotMapped]
    public double PeriodProgress
    {
        get
        {
            if (!SaleDeadline.HasValue) return 0;
            var total = (SaleDeadline.Value.Date - ReceivedAt.Date).TotalDays;
            if (total <= 0) return DateTime.Today >= SaleDeadline.Value.Date ? 1.0 : 0.0;
            var elapsed = (DateTime.Today - ReceivedAt.Date).TotalDays;
            return Math.Max(0, Math.Min(1, elapsed / total));
        }
    }

    /// <summary>Процент скидки по партии.
    /// До 2/3 срока — 0%; от 2/3 до конца — линейно растёт от 30% до 70%.
    /// После истечения срока скидка не имеет смысла (партия списывается).</summary>
    [NotMapped]
    public int DiscountPercent
    {
        get
        {
            if (!SaleDeadline.HasValue || IsOverdue) return 0;
            double p = PeriodProgress;
            if (p < 2.0 / 3.0) return 0;
            // p ∈ [2/3, 1) → скидка ∈ [30, 70)
            double pct = 30 + (p - 2.0 / 3.0) * 120.0;
            return (int)Math.Round(Math.Min(70, Math.Max(30, pct)));
        }
    }

    /// <summary>Партия уже в зоне скидок (пройдено больше 2/3 срока)</summary>
    [NotMapped]
    public bool IsDiscounted => !IsOverdue && DiscountPercent > 0;

    [NotMapped]
    public string DeadlineDisplay => SaleDeadline.HasValue
        ? SaleDeadline.Value.ToString("dd.MM.yyyy")
        : "Бессрочный";

    [NotMapped]
    public string StatusDisplay
    {
        get
        {
            if (!SaleDeadline.HasValue) return "Бессрочный";
            if (IsOverdue) return "Просрочен";
            if (IsDiscounted) return $"Скидка {DiscountPercent}% (до {SaleDeadline.Value:dd.MM.yyyy})";
            return $"До {SaleDeadline.Value:dd.MM.yyyy}";
        }
    }
}
