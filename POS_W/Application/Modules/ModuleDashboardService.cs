using Microsoft.EntityFrameworkCore;
using POS_W.Data;

namespace POS_W.Application.Modules;

public sealed class ModuleDashboardService(IServiceProvider serviceProvider, PosDatabaseSettings databaseSettings)
{
    public async Task<ConfigurationSummary> GetConfigurationSummaryAsync()
    {
        var db = CreateDbContext();
        if (db is null)
        {
            return ConfigurationSummary.NotConfigured;
        }

        try
        {
            return new ConfigurationSummary(
                true,
                null,
                await db.Empresas.CountAsync(x => x.DeletedAt == null),
                await db.Establecimientos.CountAsync(x => x.DeletedAt == null),
                await db.Parametros.CountAsync());
        }
        catch (Exception ex)
        {
            return ConfigurationSummary.Unavailable(ex.Message);
        }
    }

    public async Task<SecuritySummary> GetSecuritySummaryAsync()
    {
        var db = CreateDbContext();
        if (db is null)
        {
            return SecuritySummary.NotConfigured;
        }

        try
        {
            return new SecuritySummary(
                true,
                null,
                await db.Roles.CountAsync(x => x.DeletedAt == null),
                await db.Permisos.CountAsync(x => x.DeletedAt == null),
                await db.Usuarios.CountAsync(x => x.DeletedAt == null),
                await db.Sesiones.CountAsync(x => x.Estado == "activa"),
                await db.Turnos.CountAsync(x => x.DeletedAt == null && x.Estado != "cerrado" && x.Estado != "anulado"));
        }
        catch (Exception ex)
        {
            return SecuritySummary.Unavailable(ex.Message);
        }
    }

    public async Task<CatalogSummary> GetCatalogSummaryAsync()
    {
        var db = CreateDbContext();
        if (db is null)
        {
            return CatalogSummary.NotConfigured;
        }

        try
        {
            return new CatalogSummary(
                true,
                null,
                await db.Categorias.CountAsync(x => x.DeletedAt == null),
                await db.Marcas.CountAsync(x => x.DeletedAt == null),
                await db.UnidadesMedida.CountAsync(x => x.DeletedAt == null),
                await db.Productos.CountAsync(x => x.DeletedAt == null),
                await db.Presentaciones.CountAsync(x => x.DeletedAt == null));
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
