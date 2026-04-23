using NLog;
using WarehouseApp.Data;
using WarehouseApp.Data.Repositories;
using WarehouseApp.Services;

namespace WarehouseApp;

public class AppServices
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    public AppDbContext DbContext { get; }
    public IAuthService AuthService { get; }
    public ICategoryService CategoryService { get; }
    public IProductService ProductService { get; }
    public IShipmentService ShipmentService { get; }
    public ISupplyService SupplyService { get; }
    public IReportService ReportService { get; }
    public ICurrencyService CurrencyService { get; }

    public AppServices(string dbPath = "warehouse.db")
    {
        logger.Debug("Инициализация сервисного контейнера, БД: {Path}", dbPath);

        DbContext = new AppDbContext($"Data Source={dbPath}");
        DbInitializer.Initialize(DbContext);

        var userRepo = new UserRepository(DbContext);
        var categoryRepo = new CategoryRepository(DbContext);
        var productRepo = new ProductRepository(DbContext);
        var shipmentRepo = new ShipmentRepository(DbContext);
        var supplyRepo = new SupplyRepository(DbContext);

        AuthService = new AuthService(userRepo);
        CategoryService = new CategoryService(categoryRepo);
        ProductService = new ProductService(productRepo);
        CurrencyService = new CurrencyService();
        ShipmentService = new ShipmentService(shipmentRepo, productRepo, CurrencyService);
        SupplyService = new SupplyService(supplyRepo, productRepo, CurrencyService);
        ReportService = new ReportService(shipmentRepo, CurrencyService);

        logger.Info("Сервисный контейнер готов");
    }

    /// <summary>Constructor for unit tests with mock services</summary>
    public AppServices(IAuthService auth, ICategoryService cat, IProductService prod, IShipmentService ship,
        ISupplyService? supply = null)
    {
        DbContext = null!;
        AuthService = auth;
        CategoryService = cat;
        ProductService = prod;
        ShipmentService = ship;
        SupplyService = supply!;
        ReportService = null!;
        CurrencyService = new CurrencyService();
    }
}
