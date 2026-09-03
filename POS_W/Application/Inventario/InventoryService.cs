using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS_W.Data;
using POS_W.Data.Entities;

namespace POS_W.Application.Inventario;

public sealed class InventoryService(
    IServiceProvider serviceProvider,
    PosDatabaseSettings databaseSettings,
    IMemoryCache cache)
{
    private const string WorkspaceCacheKey = "inventory:workspace";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45),
        SlidingExpiration = TimeSpan.FromSeconds(20)
    };

    public async Task<InventoryWorkspace> GetWorkspaceAsync()
    {
        if (cache.TryGetValue(WorkspaceCacheKey, out InventoryWorkspace? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return InventoryWorkspace.Unavailable("Configura la conexion local a MySQL para gestionar inventario.");
        }

        try
        {
            var establecimientos = await db.Establecimientos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Codigo)
                .Select(x => new InventoryEstablishmentItem(x.Id, x.Codigo, x.Nombre))
                .ToListAsync();

            var productos = await db.Productos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Nombre)
                .Select(x => new InventoryProductItem(
                    x.Id,
                    x.CodigoInterno,
                    x.Nombre,
                    x.UnidadBaseId,
                    x.StockMinimo,
                    x.CostoPromedio,
                    x.PrecioVenta,
                    x.TipoProducto))
                .ToListAsync();

            var unidades = await db.UnidadesMedida
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Codigo)
                .Select(x => new InventoryUnitItem(x.Id, x.Codigo, x.Nombre))
                .ToListAsync();

            var usuarios = await db.Usuarios
                .Where(x => x.DeletedAt == null && x.Estado == "activo")
                .OrderBy(x => x.Apellidos)
                .ThenBy(x => x.Nombres)
                .Select(x => new InventoryUserItem(x.Id, x.Nombres, x.Apellidos))
                .ToListAsync();

            var movements = await db.Kardex
                .OrderByDescending(x => x.Id)
                .Take(120)
                .Select(x => new KardexItem(
                    x.Id,
                    x.EstablecimientoId,
                    x.ProductoId,
                    x.UsuarioId,
                    x.TipoMovimiento,
                    x.EntradaBase,
                    x.SalidaBase,
                    x.SaldoBase,
                    x.CostoUnitario,
                    x.Observacion,
                    x.CreatedAt))
                .ToListAsync();

            var lots = await db.LotesVencimientos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.FechaVencimiento == null)
                .ThenBy(x => x.FechaVencimiento)
                .Select(x => new LotItem(
                    x.Id,
                    x.ProductoId,
                    x.Lote,
                    x.FechaVencimiento,
                    x.CantidadInicialBase,
                    x.CantidadActualBase,
                    x.CostoUnitario,
                    x.Ubicacion,
                    x.Estado))
                .ToListAsync();

            var balances = movements
                .GroupBy(x => x.ProductoId)
                .Select(group =>
                {
                    var last = group.OrderByDescending(x => x.Id).First();
                    var product = productos.FirstOrDefault(x => x.Id == group.Key);
                    return new StockBalanceItem(
                        group.Key,
                        product?.CodigoInterno ?? "-",
                        product?.Nombre ?? "Producto no encontrado",
                        product?.UnidadBaseId ?? 0,
                        last.SaldoBase,
                        product?.StockMinimo ?? 0,
                        (product?.StockMinimo ?? 0) > 0 && last.SaldoBase <= (product?.StockMinimo ?? 0));
                })
                .OrderBy(x => x.ProductName)
                .ToList();

            var workspace = new InventoryWorkspace(true, null, establecimientos, productos, unidades, usuarios, movements, lots, balances);
            cache.Set(WorkspaceCacheKey, workspace, CacheOptions);
            return workspace;
        }
        catch (Exception ex)
        {
            return InventoryWorkspace.Unavailable(ex.Message);
        }
    }

    public async Task<InventoryOperationResult> CreateAdjustmentAsync(InventoryAdjustmentForm model)
    {
        var validation = ValidateAdjustment(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return InventoryOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var currentBalance = await db.Kardex
            .Where(x => x.EstablecimientoId == model.EstablecimientoId && x.ProductoId == model.ProductoId)
            .OrderByDescending(x => x.Id)
            .Select(x => x.SaldoBase)
            .FirstOrDefaultAsync();

        var entrada = model.TipoMovimiento == "ajuste_positivo" ? model.CantidadBase : 0m;
        var salida = model.TipoMovimiento == "ajuste_positivo" ? 0m : model.CantidadBase;
        var newBalance = currentBalance + entrada - salida;

        if (newBalance < 0)
        {
            return InventoryOperationResult.Fail("El ajuste dejaria stock negativo. Revisa el saldo actual o registra una entrada primero.");
        }

        db.Kardex.Add(new OpKardex
        {
            EstablecimientoId = model.EstablecimientoId,
            ProductoId = model.ProductoId,
            UsuarioId = model.UsuarioId,
            TipoMovimiento = model.TipoMovimiento,
            DocumentoTipo = "ajuste_manual",
            EntradaBase = entrada,
            SalidaBase = salida,
            SaldoBase = newBalance,
            CostoUnitario = model.CostoUnitario,
            Observacion = EmptyToNull(model.Observacion)
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        InvalidateCache();
        return InventoryOperationResult.Ok("Movimiento de inventario registrado.");
    }

    public async Task<InventoryOperationResult> CreateLotAsync(LotForm model)
    {
        if (model.ProductoId == 0 || string.IsNullOrWhiteSpace(model.Lote))
        {
            return InventoryOperationResult.Fail("Selecciona producto e ingresa codigo de lote.");
        }

        if (model.CantidadInicialBase <= 0 || model.CostoUnitario < 0)
        {
            return InventoryOperationResult.Fail("La cantidad debe ser mayor a cero y el costo no puede ser negativo.");
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return InventoryOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var lotCode = model.Lote.Trim().ToUpperInvariant();
        var exists = await db.LotesVencimientos.AnyAsync(x => x.ProductoId == model.ProductoId && x.Lote == lotCode && x.DeletedAt == null);
        if (exists)
        {
            return InventoryOperationResult.Fail("Ese producto ya tiene un lote activo con el mismo codigo.");
        }

        db.LotesVencimientos.Add(new OpLoteVencimiento
        {
            ProductoId = model.ProductoId,
            Lote = lotCode,
            FechaVencimiento = model.FechaVencimiento is null ? null : DateOnly.FromDateTime(model.FechaVencimiento.Value),
            CantidadInicialBase = model.CantidadInicialBase,
            CantidadActualBase = model.CantidadInicialBase,
            CostoUnitario = model.CostoUnitario,
            Ubicacion = EmptyToNull(model.Ubicacion),
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        InvalidateCache();
        return InventoryOperationResult.Ok("Lote registrado.");
    }

    private static InventoryOperationResult ValidateAdjustment(InventoryAdjustmentForm model)
    {
        if (model.EstablecimientoId == 0 || model.ProductoId == 0 || model.UsuarioId == 0)
        {
            return InventoryOperationResult.Fail("Selecciona establecimiento, producto y usuario responsable.");
        }

        if (model.CantidadBase <= 0)
        {
            return InventoryOperationResult.Fail("La cantidad debe ser mayor a cero.");
        }

        if (model.CostoUnitario < 0)
        {
            return InventoryOperationResult.Fail("El costo unitario no puede ser negativo.");
        }

        return InventoryOperationResult.Ok();
    }

    private PosDbContext? CreateDbContext()
    {
        if (!databaseSettings.IsConfigured)
        {
            return null;
        }

        var factory = serviceProvider.GetService<IDbContextFactory<PosDbContext>>();
        return factory?.CreateDbContext();
    }

    private void InvalidateCache()
    {
        cache.Remove(WorkspaceCacheKey);
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record InventoryWorkspace(
    bool DatabaseConfigured,
    string? StatusMessage,
    IReadOnlyList<InventoryEstablishmentItem> Establecimientos,
    IReadOnlyList<InventoryProductItem> Productos,
    IReadOnlyList<InventoryUnitItem> Unidades,
    IReadOnlyList<InventoryUserItem> Usuarios,
    IReadOnlyList<KardexItem> Movimientos,
    IReadOnlyList<LotItem> Lotes,
    IReadOnlyList<StockBalanceItem> Saldos)
{
    public static InventoryWorkspace Unavailable(string message) => new(false, message, [], [], [], [], [], [], []);
}

public sealed record InventoryEstablishmentItem(ulong Id, string Codigo, string Nombre);
public sealed record InventoryUnitItem(ulong Id, string Codigo, string Nombre);
public sealed record InventoryUserItem(ulong Id, string Nombres, string Apellidos);
public sealed record InventoryProductItem(ulong Id, string CodigoInterno, string Nombre, ulong UnidadBaseId, decimal StockMinimo, decimal CostoPromedio, decimal PrecioVenta, string TipoProducto);

public sealed record KardexItem(
    ulong Id,
    ulong EstablecimientoId,
    ulong ProductoId,
    ulong UsuarioId,
    string TipoMovimiento,
    decimal EntradaBase,
    decimal SalidaBase,
    decimal SaldoBase,
    decimal CostoUnitario,
    string? Observacion,
    DateTime CreatedAt);

public sealed record LotItem(
    ulong Id,
    ulong ProductoId,
    string Lote,
    DateOnly? FechaVencimiento,
    decimal CantidadInicialBase,
    decimal CantidadActualBase,
    decimal CostoUnitario,
    string? Ubicacion,
    string Estado);

public sealed record StockBalanceItem(ulong ProductId, string ProductCode, string ProductName, ulong UnitId, decimal SaldoBase, decimal StockMinimo, bool BajoMinimo);

public sealed class InventoryAdjustmentForm
{
    public ulong EstablecimientoId { get; set; }
    public ulong ProductoId { get; set; }
    public ulong UsuarioId { get; set; }
    public string TipoMovimiento { get; set; } = "ajuste_positivo";
    public decimal CantidadBase { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Observacion { get; set; }
}

public sealed class LotForm
{
    public ulong ProductoId { get; set; }
    public string Lote { get; set; } = "";
    public DateTime? FechaVencimiento { get; set; }
    public decimal CantidadInicialBase { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Ubicacion { get; set; }
}

public sealed record InventoryOperationResult(bool Success, string Message)
{
    public static InventoryOperationResult Ok(string message = "") => new(true, message);
    public static InventoryOperationResult Fail(string message) => new(false, message);
}
