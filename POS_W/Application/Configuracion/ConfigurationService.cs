using Microsoft.EntityFrameworkCore;
using POS_W.Data;
using POS_W.Data.Entities;

namespace POS_W.Application.Configuracion;

public sealed class ConfigurationService(IServiceProvider serviceProvider, PosDatabaseSettings databaseSettings)
{
    public async Task<ConfigurationWorkspace> GetWorkspaceAsync()
    {
        var db = CreateDbContext();
        if (db is null)
        {
            return ConfigurationWorkspace.Unavailable("Configura la conexion local a MySQL para gestionar empresas y establecimientos.");
        }

        try
        {
            var empresas = await db.Empresas
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.RazonSocial)
                .Select(x => new EmpresaListItem(x.Id, x.Ruc, x.RazonSocial, x.NombreComercial, x.DireccionFiscal, x.Estado))
                .ToListAsync();

            var establecimientos = await db.Establecimientos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Codigo)
                .Select(x => new EstablecimientoListItem(x.Id, x.EmpresaId, x.Codigo, x.Nombre, x.Direccion, x.SerieFactura, x.SerieBoleta, x.FormatoImpresion, x.PermiteStockNegativo, x.Estado))
                .ToListAsync();

            var parametros = await db.Parametros
                .OrderBy(x => x.Clave)
                .Select(x => new ParametroListItem(x.Id, x.EstablecimientoId, x.Clave, x.Valor, x.Tipo, x.Estado))
                .ToListAsync();

            return new ConfigurationWorkspace(true, null, empresas, establecimientos, parametros);
        }
        catch (Exception ex)
        {
            return ConfigurationWorkspace.Unavailable(ex.Message);
        }
    }

    public async Task<OperationResult> CreateEmpresaAsync(EmpresaForm model)
    {
        var validation = ValidateEmpresa(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return OperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var exists = await db.Empresas.AnyAsync(x => x.Ruc == model.Ruc.Trim() && x.DeletedAt == null);
        if (exists)
        {
            return OperationResult.Fail("Ya existe una empresa activa con ese RUC.");
        }

        db.Empresas.Add(new CfgEmpresa
        {
            Ruc = model.Ruc.Trim(),
            RazonSocial = model.RazonSocial.Trim(),
            NombreComercial = EmptyToNull(model.NombreComercial),
            DireccionFiscal = model.DireccionFiscal.Trim(),
            AmbienteSunat = model.AmbienteSunat,
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        return OperationResult.Ok("Empresa registrada.");
    }

    public async Task<OperationResult> CreateEstablecimientoAsync(EstablecimientoForm model)
    {
        var validation = ValidateEstablecimiento(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return OperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var exists = await db.Establecimientos.AnyAsync(x =>
            x.EmpresaId == model.EmpresaId &&
            x.Codigo == model.Codigo.Trim() &&
            x.DeletedAt == null);

        if (exists)
        {
            return OperationResult.Fail("Ya existe un establecimiento activo con ese codigo para la empresa.");
        }

        db.Establecimientos.Add(new CfgEstablecimiento
        {
            EmpresaId = model.EmpresaId,
            Codigo = model.Codigo.Trim(),
            Nombre = model.Nombre.Trim(),
            Direccion = model.Direccion.Trim(),
            SerieFactura = EmptyToNull(model.SerieFactura),
            SerieBoleta = EmptyToNull(model.SerieBoleta),
            FormatoImpresion = model.FormatoImpresion,
            PermiteStockNegativo = model.PermiteStockNegativo,
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        return OperationResult.Ok("Establecimiento registrado.");
    }

    public async Task<OperationResult> CreateParametroAsync(ParametroForm model)
    {
        var validation = ValidateParametro(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return OperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var exists = await db.Parametros.AnyAsync(x =>
            x.EstablecimientoId == model.EstablecimientoId &&
            x.Clave == model.Clave.Trim());

        if (exists)
        {
            return OperationResult.Fail("Ya existe un parametro con esa clave para el establecimiento.");
        }

        db.Parametros.Add(new CfgParametro
        {
            EstablecimientoId = model.EstablecimientoId,
            Clave = model.Clave.Trim(),
            Valor = model.Valor.Trim(),
            Tipo = model.Tipo,
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        return OperationResult.Ok("Parametro registrado.");
    }

    private static OperationResult ValidateEmpresa(EmpresaForm model)
    {
        if (model.Ruc.Trim().Length != 11 || !model.Ruc.All(char.IsDigit))
        {
            return OperationResult.Fail("El RUC debe tener 11 digitos.");
        }

        if (string.IsNullOrWhiteSpace(model.RazonSocial))
        {
            return OperationResult.Fail("La razon social es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(model.DireccionFiscal))
        {
            return OperationResult.Fail("La direccion fiscal es obligatoria.");
        }

        return OperationResult.Ok();
    }

    private static OperationResult ValidateEstablecimiento(EstablecimientoForm model)
    {
        if (model.EmpresaId == 0)
        {
            return OperationResult.Fail("Selecciona una empresa.");
        }

        if (string.IsNullOrWhiteSpace(model.Codigo))
        {
            return OperationResult.Fail("El codigo del establecimiento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(model.Nombre))
        {
            return OperationResult.Fail("El nombre del establecimiento es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(model.Direccion))
        {
            return OperationResult.Fail("La direccion del establecimiento es obligatoria.");
        }

        return OperationResult.Ok();
    }

    private static OperationResult ValidateParametro(ParametroForm model)
    {
        if (model.EstablecimientoId == 0)
        {
            return OperationResult.Fail("Selecciona un establecimiento.");
        }

        if (string.IsNullOrWhiteSpace(model.Clave))
        {
            return OperationResult.Fail("La clave del parametro es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(model.Valor))
        {
            return OperationResult.Fail("El valor del parametro es obligatorio.");
        }

        return OperationResult.Ok();
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

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record ConfigurationWorkspace(
    bool DatabaseConfigured,
    string? StatusMessage,
    IReadOnlyList<EmpresaListItem> Empresas,
    IReadOnlyList<EstablecimientoListItem> Establecimientos,
    IReadOnlyList<ParametroListItem> Parametros)
{
    public static ConfigurationWorkspace Unavailable(string message) => new(false, message, [], [], []);
}

public sealed record EmpresaListItem(ulong Id, string Ruc, string RazonSocial, string? NombreComercial, string DireccionFiscal, string Estado);

public sealed record EstablecimientoListItem(
    ulong Id,
    ulong EmpresaId,
    string Codigo,
    string Nombre,
    string Direccion,
    string? SerieFactura,
    string? SerieBoleta,
    string FormatoImpresion,
    bool PermiteStockNegativo,
    string Estado);

public sealed record ParametroListItem(ulong Id, ulong EstablecimientoId, string Clave, string Valor, string Tipo, string Estado);

public sealed class EmpresaForm
{
    public string Ruc { get; set; } = "";
    public string RazonSocial { get; set; } = "";
    public string? NombreComercial { get; set; }
    public string DireccionFiscal { get; set; } = "";
    public string AmbienteSunat { get; set; } = "beta";
}

public sealed class EstablecimientoForm
{
    public ulong EmpresaId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string? SerieFactura { get; set; }
    public string? SerieBoleta { get; set; }
    public string FormatoImpresion { get; set; } = "ticket_80";
    public bool PermiteStockNegativo { get; set; }
}

public sealed class ParametroForm
{
    public ulong EstablecimientoId { get; set; }
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public string Tipo { get; set; } = "string";
}

public sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message = "") => new(true, message);
    public static OperationResult Fail(string message) => new(false, message);
}
