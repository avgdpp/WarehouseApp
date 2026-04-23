using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApp.Models;

/// <summary>Запись о списании просроченного товара</summary>
public class WriteOff
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int BatchId { get; set; }

    /// <summary>Количество списанных единиц</summary>
    public int Quantity { get; set; }

    /// <summary>Закупочная цена (убыток за единицу)</summary>
    public decimal PurchasePrice { get; set; }

    /// <summary>Общий убыток</summary>
    [NotMapped]
    public decimal TotalLoss => PurchasePrice * Quantity;

    /// <summary>Дата списания</summary>
    public DateTime WrittenOffAt { get; set; } = DateTime.Now;

    /// <summary>Причина</summary>
    [MaxLength(500)]
    public string Reason { get; set; } = "Истёк срок реализации";
}
