using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS_W.Data;
using POS_W.Data.Entities;

namespace POS_W.Application.Clientes;

public sealed class ClientService(
    IServiceProvider serviceProvider,
    PosDatabaseSettings databaseSettings,
    IMemoryCache cache)
{
    private const string WorkspaceCacheKey = "clients:workspace";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2),
        SlidingExpiration = TimeSpan.FromSeconds(30)
    };

    public async Task<ClientWorkspace> GetWorkspaceAsync()
    {
        if (cache.TryGetValue(WorkspaceCacheKey, out ClientWorkspace? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return ClientWorkspace.Unavailable("Configura la conexion local a MySQL para gestionar clientes.");
        }

        try
        {
            var clientes = await db.Clientes
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.RazonSocialNombre)
                .Take(250)
                .Select(x => new ClientItem(
                    x.Id,
                    x.DocumentoTipo,
                    x.DocumentoNumero,
                    x.RazonSocialNombre,
                    x.Direccion,
                    x.Email,
                    x.Telefono,
                    x.UltimaConsultaIdentidadAt,
                    x.Estado))
                .ToListAsync();

            var workspace = new ClientWorkspace(true, null, clientes);
            cache.Set(WorkspaceCacheKey, workspace, CacheOptions);
            return workspace;
        }
        catch (Exception ex)
        {
            return ClientWorkspace.Unavailable(ex.Message);
        }
    }

    public async Task<ClientOperationResult> CreateClientAsync(ClientForm model)
    {
        var validation = Validate(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return ClientOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var documentType = model.DocumentoTipo.Trim().ToUpperInvariant();
        var documentNumber = model.DocumentoNumero.Trim();
        var exists = await db.Clientes.AnyAsync(x =>
            x.DocumentoTipo == documentType &&
            x.DocumentoNumero == documentNumber &&
            x.DeletedAt == null);

        if (exists)
        {
            return ClientOperationResult.Fail("Ya existe un cliente activo con ese documento.");
        }

        db.Clientes.Add(new PosCliente
        {
            DocumentoTipo = documentType,
            DocumentoNumero = documentNumber,
            RazonSocialNombre = model.RazonSocialNombre.Trim(),
            Direccion = EmptyToNull(model.Direccion),
            Email = EmptyToNull(model.Email),
            Telefono = EmptyToNull(model.Telefono),
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        InvalidateCache();
        return ClientOperationResult.Ok("Cliente registrado.");
    }

    private static ClientOperationResult Validate(ClientForm model)
    {
        var documentType = model.DocumentoTipo.Trim().ToUpperInvariant();
        var documentNumber = model.DocumentoNumero.Trim();

        if (documentType is not ("DNI" or "RUC" or "CE" or "PASAPORTE"))
        {
            return ClientOperationResult.Fail("Selecciona un tipo de documento valido.");
        }

        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return ClientOperationResult.Fail("El numero de documento es obligatorio.");
        }

        if (documentType == "DNI" && (documentNumber.Length != 8 || !documentNumber.All(char.IsDigit)))
        {
            return ClientOperationResult.Fail("El DNI debe tener 8 digitos.");
        }

        if (documentType == "RUC" && (documentNumber.Length != 11 || !documentNumber.All(char.IsDigit)))
        {
            return ClientOperationResult.Fail("El RUC debe tener 11 digitos.");
        }

        if (string.IsNullOrWhiteSpace(model.RazonSocialNombre))
        {
            return ClientOperationResult.Fail("El nombre o razon social es obligatorio.");
        }

        return ClientOperationResult.Ok();
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

public sealed record ClientWorkspace(bool DatabaseConfigured, string? StatusMessage, IReadOnlyList<ClientItem> Clientes)
{
    public static ClientWorkspace Unavailable(string message) => new(false, message, []);
}

public sealed record ClientItem(
    ulong Id,
    string DocumentoTipo,
    string DocumentoNumero,
    string RazonSocialNombre,
    string? Direccion,
    string? Email,
    string? Telefono,
    DateTime? UltimaConsultaIdentidadAt,
    string Estado);

public sealed class ClientForm
{
    public string DocumentoTipo { get; set; } = "DNI";
    public string DocumentoNumero { get; set; } = "";
    public string RazonSocialNombre { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
}

public sealed record ClientOperationResult(bool Success, string Message)
{
    public static ClientOperationResult Ok(string message = "") => new(true, message);
    public static ClientOperationResult Fail(string message) => new(false, message);
}
