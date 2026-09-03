using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS_W.Application.Modules;
using POS_W.Data;
using POS_W.Data.Entities;

namespace POS_W.Application.Catalogo;

public sealed class CatalogService(
    IServiceProvider serviceProvider,
    PosDatabaseSettings databaseSettings,
    IMemoryCache cache)
{
    private const string WorkspaceCacheKey = "catalog:workspace";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
        SlidingExpiration = TimeSpan.FromSeconds(30)
    };

    public async Task<CatalogWorkspace> GetWorkspaceAsync()
    {
        if (cache.TryGetValue(WorkspaceCacheKey, out CatalogWorkspace? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogWorkspace.Unavailable("Configura la conexion local a MySQL para gestionar el catalogo.");
        }

        try
        {
            var categorias = await db.Categorias
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Nombre)
                .Select(x => new CatalogCategoryItem(x.Id, x.Nombre, x.Estado))
                .ToListAsync();

            var marcas = await db.Marcas
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Nombre)
                .Select(x => new CatalogBrandItem(x.Id, x.Nombre, x.Estado))
                .ToListAsync();

            var unidades = await db.UnidadesMedida
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Tipo)
                .ThenBy(x => x.Codigo)
                .Select(x => new CatalogUnitItem(x.Id, x.Codigo, x.Nombre, x.Tipo, x.Estado))
                .ToListAsync();

            var productos = await db.Productos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Nombre)
                .Select(x => new CatalogProductItem(
                    x.Id,
                    x.CodigoInterno,
                    x.Nombre,
                    x.TipoProducto,
                    x.CategoriaId,
                    x.MarcaId,
                    x.UnidadBaseId,
                    x.StockMinimo,
                    x.CostoPromedio,
                    x.PrecioVenta,
                    x.Estado))
                .ToListAsync();

            var presentaciones = await db.Presentaciones
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.ProductoId)
                .ThenBy(x => x.FactorUnidadBase)
                .Select(x => new CatalogPresentationItem(
                    x.Id,
                    x.ProductoId,
                    x.UnidadMedidaId,
                    x.Nombre,
                    x.FactorUnidadBase,
                    x.PrecioVenta,
                    x.Estado))
                .ToListAsync();

            var workspace = new CatalogWorkspace(true, null, categorias, marcas, unidades, productos, presentaciones);
            cache.Set(WorkspaceCacheKey, workspace, CacheOptions);
            return workspace;
        }
        catch (Exception ex)
        {
            return CatalogWorkspace.Unavailable(ex.Message);
        }
    }

    public async Task<CatalogOperationResult> CreateCategoryAsync(SimpleCatalogForm model)
    {
        return await CreateSimpleAsync(model, db => db.Categorias, name => new CatCategoria { Nombre = name });
    }

    public async Task<CatalogOperationResult> CreateBrandAsync(SimpleCatalogForm model)
    {
        return await CreateSimpleAsync(model, db => db.Marcas, name => new CatMarca { Nombre = name });
    }

    public async Task<CatalogOperationResult> CreateUnitAsync(UnitForm model)
    {
        if (string.IsNullOrWhiteSpace(model.Codigo) || string.IsNullOrWhiteSpace(model.Nombre))
        {
            return CatalogOperationResult.Fail("Codigo y nombre de unidad son obligatorios.");
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var codigo = model.Codigo.Trim().ToUpperInvariant();
        var exists = await db.UnidadesMedida.AnyAsync(x => x.Codigo == codigo && x.DeletedAt == null);
        if (exists)
        {
            return CatalogOperationResult.Fail("Ya existe una unidad activa con ese codigo.");
        }

        db.UnidadesMedida.Add(new CatUnidadMedida
        {
            Codigo = codigo,
            Nombre = model.Nombre.Trim(),
            Tipo = model.Tipo,
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        InvalidateCache();
        return CatalogOperationResult.Ok("Unidad registrada.");
    }

    public async Task<CatalogOperationResult> CreateProductAsync(ProductForm model)
    {
        var validation = ValidateProduct(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var codigo = model.CodigoInterno.Trim().ToUpperInvariant();
        var exists = await db.Productos.AnyAsync(x => x.CodigoInterno == codigo && x.DeletedAt == null);
        if (exists)
        {
            return CatalogOperationResult.Fail("Ya existe un producto activo con ese codigo.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var product = new CatProducto
        {
            CategoriaId = model.CategoriaId is null or 0 ? null : model.CategoriaId,
            MarcaId = model.MarcaId is null or 0 ? null : model.MarcaId,
            UnidadBaseId = model.UnidadBaseId,
            CodigoInterno = codigo,
            Nombre = model.Nombre.Trim(),
            TipoProducto = model.TipoProducto,
            StockMinimo = model.StockMinimo,
            CostoPromedio = model.CostoPromedio,
            PrecioVenta = model.PrecioVenta,
            Estado = "activo"
        };

        db.Productos.Add(product);
        await db.SaveChangesAsync();

        db.Presentaciones.Add(new CatPresentacion
        {
            ProductoId = product.Id,
            UnidadMedidaId = product.UnidadBaseId,
            Nombre = "Unidad base",
            FactorUnidadBase = 1.0000m,
            PrecioVenta = product.PrecioVenta,
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        InvalidateCache();
        return CatalogOperationResult.Ok("Producto registrado con presentacion base.");
    }

    public async Task<CatalogOperationResult> CreatePresentationAsync(PresentationForm model)
    {
        if (model.ProductoId == 0 || model.UnidadMedidaId == 0)
        {
            return CatalogOperationResult.Fail("Selecciona producto y unidad de medida.");
        }

        if (string.IsNullOrWhiteSpace(model.Nombre) || model.FactorUnidadBase <= 0 || model.PrecioVenta < 0)
        {
            return CatalogOperationResult.Fail("La presentacion requiere nombre, factor mayor a cero y precio valido.");
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var name = model.Nombre.Trim();
        var exists = await db.Presentaciones.AnyAsync(x =>
            x.ProductoId == model.ProductoId &&
            x.Nombre == name &&
            x.DeletedAt == null);

        if (exists)
        {
            return CatalogOperationResult.Fail("Ese producto ya tiene una presentacion activa con el mismo nombre.");
        }

        db.Presentaciones.Add(new CatPresentacion
        {
            ProductoId = model.ProductoId,
            UnidadMedidaId = model.UnidadMedidaId,
            Nombre = name,
            FactorUnidadBase = model.FactorUnidadBase,
            PrecioVenta = model.PrecioVenta,
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        InvalidateCache();
        return CatalogOperationResult.Ok("Presentacion registrada.");
    }

    private async Task<CatalogOperationResult> CreateSimpleAsync<TEntity>(
        SimpleCatalogForm model,
        Func<PosDbContext, DbSet<TEntity>> setSelector,
        Func<string, TEntity> entityFactory)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(model.Nombre))
        {
            return CatalogOperationResult.Fail("El nombre es obligatorio.");
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var name = model.Nombre.Trim();
        var exists = await setSelector(db)
            .AnyAsync(x => EF.Property<string>(x, nameof(CatCategoria.Nombre)) == name &&
                           EF.Property<DateTime?>(x, nameof(CatCategoria.DeletedAt)) == null);

        if (exists)
        {
            return CatalogOperationResult.Fail("Ya existe un registro activo con ese nombre.");
        }

        setSelector(db).Add(entityFactory(name));
        await db.SaveChangesAsync();
        InvalidateCache();
        return CatalogOperationResult.Ok("Registro guardado.");
    }

    private static CatalogOperationResult ValidateProduct(ProductForm model)
    {
        if (model.UnidadBaseId == 0)
        {
            return CatalogOperationResult.Fail("Selecciona la unidad base obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(model.CodigoInterno) || string.IsNullOrWhiteSpace(model.Nombre))
        {
            return CatalogOperationResult.Fail("Codigo interno y nombre son obligatorios.");
        }

        if (model.StockMinimo < 0 || model.CostoPromedio < 0 || model.PrecioVenta < 0)
        {
            return CatalogOperationResult.Fail("Stock, costo y precio no pueden ser negativos.");
        }

        return CatalogOperationResult.Ok();
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
        cache.Remove(nameof(CatalogSummary));
    }
}

public sealed record CatalogWorkspace(
    bool DatabaseConfigured,
    string? StatusMessage,
    IReadOnlyList<CatalogCategoryItem> Categorias,
    IReadOnlyList<CatalogBrandItem> Marcas,
    IReadOnlyList<CatalogUnitItem> Unidades,
    IReadOnlyList<CatalogProductItem> Productos,
    IReadOnlyList<CatalogPresentationItem> Presentaciones)
{
    public static CatalogWorkspace Unavailable(string message) => new(false, message, [], [], [], [], []);
}

public sealed record CatalogCategoryItem(ulong Id, string Nombre, string Estado);
public sealed record CatalogBrandItem(ulong Id, string Nombre, string Estado);
public sealed record CatalogUnitItem(ulong Id, string Codigo, string Nombre, string Tipo, string Estado);

public sealed record CatalogProductItem(
    ulong Id,
    string CodigoInterno,
    string Nombre,
    string TipoProducto,
    ulong? CategoriaId,
    ulong? MarcaId,
    ulong UnidadBaseId,
    decimal StockMinimo,
    decimal CostoPromedio,
    decimal PrecioVenta,
    string Estado);

public sealed record CatalogPresentationItem(
    ulong Id,
    ulong ProductoId,
    ulong UnidadMedidaId,
    string Nombre,
    decimal FactorUnidadBase,
    decimal PrecioVenta,
    string Estado);

public sealed class SimpleCatalogForm
{
    public string Nombre { get; set; } = "";
}

public sealed class UnitForm
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "unidad";
}

public sealed class ProductForm
{
    public ulong? CategoriaId { get; set; }
    public ulong? MarcaId { get; set; }
    public ulong UnidadBaseId { get; set; }
    public string CodigoInterno { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string TipoProducto { get; set; } = "minimarket";
    public decimal StockMinimo { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal PrecioVenta { get; set; }
}

public sealed class PresentationForm
{
    public ulong ProductoId { get; set; }
    public ulong UnidadMedidaId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal FactorUnidadBase { get; set; } = 1.0000m;
    public decimal PrecioVenta { get; set; }
}

public sealed record CatalogOperationResult(bool Success, string Message)
{
    public static CatalogOperationResult Ok(string message = "") => new(true, message);
    public static CatalogOperationResult Fail(string message) => new(false, message);
}
