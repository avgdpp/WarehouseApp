using Microsoft.EntityFrameworkCore;
using NLog;
using WarehouseApp.Models;

namespace WarehouseApp.Data;

public static class DbInitializer
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public static void Initialize(AppDbContext context)
    {
        logger.Info("Инициализация БД");
        try
        {
            context.Database.EnsureCreated();
            MigrateSchema(context);

            if (!context.Users.Any(u => u.Role == UserRole.Administrator))
            {
                context.Users.Add(new User
                {
                    Login = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                    Role = UserRole.Administrator
                });
                context.SaveChanges();
                logger.Info("Создан администратор по умолчанию (login=admin)");
            }

            logger.Info("БД готова к работе");
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Не удалось инициализировать БД");
            throw;
        }
    }

    private static void MigrateSchema(AppDbContext context)
    {
        var conn = context.Database.GetDbConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();

        // Supplies table
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Supplies'";
        if (cmd.ExecuteScalar() == null)
        {
            logger.Info("Миграция схемы: создаются таблицы Supplies, SupplyItems, ProductBatches");
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Supplies (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL DEFAULT '',
                    Supplier TEXT NOT NULL DEFAULT '',
                    TotalCost TEXT NOT NULL DEFAULT '0',
                    SuppliedAt TEXT NOT NULL DEFAULT '',
                    CreatedByUserId INTEGER NOT NULL,
                    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id) ON DELETE RESTRICT
                );
                CREATE TABLE IF NOT EXISTS SupplyItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SupplyId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    PurchasePrice TEXT NOT NULL DEFAULT '0',
                    Quantity INTEGER NOT NULL DEFAULT 0,
                    ExpiryDate TEXT,
                    FOREIGN KEY (SupplyId) REFERENCES Supplies(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
                );
                CREATE TABLE IF NOT EXISTS ProductBatches (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL DEFAULT 0,
                    ExpiryDate TEXT,
                    PurchasePrice TEXT NOT NULL DEFAULT '0',
                    ReceivedAt TEXT NOT NULL DEFAULT '',
                    SupplyId INTEGER,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
                    FOREIGN KEY (SupplyId) REFERENCES Supplies(Id) ON DELETE SET NULL
                );
            ");
        }

        // WriteOffs table
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='WriteOffs'";
        if (cmd.ExecuteScalar() == null)
        {
            logger.Info("Миграция схемы: создаётся таблица WriteOffs");
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS WriteOffs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    BatchId INTEGER NOT NULL DEFAULT 0,
                    Quantity INTEGER NOT NULL DEFAULT 0,
                    PurchasePrice TEXT NOT NULL DEFAULT '0',
                    WrittenOffAt TEXT NOT NULL DEFAULT '',
                    Reason TEXT NOT NULL DEFAULT '',
                    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE RESTRICT
                );
            ");
        }

        // PurchaseCost column on ShipmentItems
        cmd.CommandText = "PRAGMA table_info(ShipmentItems)";
        bool hasPurchaseCost = false;
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                if (reader.GetString(1) == "PurchaseCost")
                    hasPurchaseCost = true;
            }
        }
        if (!hasPurchaseCost)
        {
            logger.Info("Миграция схемы: добавляется колонка PurchaseCost в ShipmentItems");
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE ShipmentItems ADD COLUMN PurchaseCost TEXT NOT NULL DEFAULT '0'");
        }

        // Исторические курсы валют на Supplies и Shipments
        AddColumnIfMissing(cmd, context, "Supplies", "UsdRate", "TEXT NOT NULL DEFAULT '0'");
        AddColumnIfMissing(cmd, context, "Supplies", "EurRate", "TEXT NOT NULL DEFAULT '0'");
        AddColumnIfMissing(cmd, context, "Supplies", "UsdtRate", "TEXT NOT NULL DEFAULT '0'");
        AddColumnIfMissing(cmd, context, "Shipments", "UsdRate", "TEXT NOT NULL DEFAULT '0'");
        AddColumnIfMissing(cmd, context, "Shipments", "EurRate", "TEXT NOT NULL DEFAULT '0'");
        AddColumnIfMissing(cmd, context, "Shipments", "UsdtRate", "TEXT NOT NULL DEFAULT '0'");

        conn.Close();
    }

    /// <summary>Добавляет колонку в таблицу, если её ещё нет (идемпотентная миграция).</summary>
    private static void AddColumnIfMissing(System.Data.Common.DbCommand cmd, AppDbContext context,
        string table, string column, string columnDef)
    {
        cmd.CommandText = $"PRAGMA table_info({table})";
        bool exists = false;
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                if (reader.GetString(1) == column)
                {
                    exists = true;
                    break;
                }
            }
        }
        if (!exists)
        {
            logger.Info("Миграция схемы: добавляется колонка {Column} в {Table}", column, table);
            context.Database.ExecuteSqlRaw($"ALTER TABLE {table} ADD COLUMN {column} {columnDef}");
        }
    }
}
