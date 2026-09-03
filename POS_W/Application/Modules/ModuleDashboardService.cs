using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS_W.Data;

namespace POS_W.Application.Modules;

public sealed class ModuleDashboardService(
    IServiceProvider serviceProvider,
    PosDatabaseSettings databaseSettings,
    IMemoryCache cache)
{
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45),
        SlidingExpiration = TimeSpan.FromSeconds(15)
    };

    public async Task<ConfigurationSummary> GetConfigurationSummaryAsync()
    {
        if (cache.TryGetValue(nameof(ConfigurationSummary), out ConfigurationSummary? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return ConfigurationSummary.NotConfigured;
        }

        try
        {
            var summary = new ConfigurationSummary(
                true,
                null,
                await db.Empresas.CountAsync(x => x.DeletedAt == null),
                await db.Establecimientos.CountAsync(x => x.DeletedAt == null),
                await db.Parametros.CountAsync());
            cache.Set(nameof(ConfigurationSummary), summary, CacheOptions);
            return summary;
        }
        catch (Exception ex)
        {
            return ConfigurationSummary.Unavailable(ex.Message);
        }
    }

    public async Task<SecuritySummary> GetSecuritySummaryAsync()
    {
        if (cache.TryGetValue(nameof(SecuritySummary), out SecuritySummary? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return SecuritySummary.NotConfigured;
        }

        try
        {
            var summary = new SecuritySummary(
                true,
                null,
                await db.Roles.CountAsync(x => x.DeletedAt == null),
                await db.Permisos.CountAsync(x => x.DeletedAt == null),
                await db.Usuarios.CountAsync(x => x.DeletedAt == null),
                await db.Sesiones.CountAsync(x => x.Estado == "activa"),
                await db.Turnos.CountAsync(x => x.DeletedAt == null && x.Estado != "cerrado" && x.Estado != "anulado"));
            cache.Set(nameof(SecuritySummary), summary, CacheOptions);
            return summary;
        }
        catch (Exception ex)
        {
            return SecuritySummary.Unavailable(ex.Message);
        }
    }

    public async Task<CatalogSummary> GetCatalogSummaryAsync()
    {
        if (cache.TryGetValue(nameof(CatalogSummary), out CatalogSummary? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogSummary.NotConfigured;
        }

        try
        {
            var summary = new CatalogSummary(
                true,
                null,
                await db.Categorias.CountAsync(x => x.DeletedAt == null),
                await db.Marcas.CountAsync(x => x.DeletedAt == null),
                await db.UnidadesMedida.CountAsync(x => x.DeletedAt == null),
                await db.Productos.CountAsync(x => x.DeletedAt == null),
                await db.Presentaciones.CountAsync(x => x.DeletedAt == null));
            cache.Set(nameof(CatalogSummary), summary, CacheOptions);
            return summary;
        }
        catch (Exception ex)
        {
            return CatalogSummary.Unavailable(ex.Message);
        }
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
}

public sealed record ConfigurationSummary(bool DatabaseConfigured, string? StatusMessage, int Empresas, int Establecimientos, int Parametros)
{
    public static ConfigurationSummary NotConfigured => new(false, "Configura POS_CONNECTION_STRING o appsettings.Development.local.json.", 0, 0, 0);
    public static ConfigurationSummary Unavailable(string message) => new(false, message, 0, 0, 0);
}

public sealed record SecuritySummary(bool DatabaseConfigured, string? StatusMessage, int Roles, int Permisos, int Usuarios, int SesionesActivas, int TurnosAbiertos)
{
    public static SecuritySummary NotConfigured => new(false, "Configura POS_CONNECTION_STRING o appsettings.Development.local.json.", 0, 0, 0, 0, 0);
    public static SecuritySummary Unavailable(string message) => new(false, message, 0, 0, 0, 0, 0);
}

public sealed record CatalogSummary(bool DatabaseConfigured, string? StatusMessage, int Categorias, int Marcas, int Unidades, int Productos, int Presentaciones)
{
    public static CatalogSummary NotConfigured => new(false, "Configura POS_CONNECTION_STRING o appsettings.Development.local.json.", 0, 0, 0, 0, 0);
    public static CatalogSummary Unavailable(string message) => new(false, message, 0, 0, 0, 0, 0);
}
