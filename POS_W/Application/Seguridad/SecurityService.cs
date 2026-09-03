using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS_W.Application.Modules;
using POS_W.Data;
using POS_W.Data.Entities;

namespace POS_W.Application.Seguridad;

public sealed class SecurityService(
    IServiceProvider serviceProvider,
    PosDatabaseSettings databaseSettings,
    IMemoryCache cache)
{
    private const string WorkspaceCacheKey = "security:workspace";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(45),
        SlidingExpiration = TimeSpan.FromSeconds(15)
    };

    public async Task<SecurityWorkspace> GetWorkspaceAsync()
    {
        if (cache.TryGetValue(WorkspaceCacheKey, out SecurityWorkspace? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return SecurityWorkspace.Unavailable("Configura la conexion local a MySQL para gestionar seguridad.");
        }

        try
        {
            var roles = await db.Roles
                .Where(x => x.DeletedAt == null)
                .OrderByDescending(x => x.NivelAutorizacion)
                .ThenBy(x => x.Nombre)
                .Select(x => new RolListItem(x.Id, x.Nombre, x.Descripcion, x.NivelAutorizacion, x.Estado))
                .ToListAsync();

            var permisos = await db.Permisos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Modulo)
                .ThenBy(x => x.Pantalla)
                .ThenBy(x => x.Accion)
                .Select(x => new PermisoListItem(x.Id, x.Modulo, x.Pantalla, x.Accion, x.Estado))
                .ToListAsync();

            var usuarios = await db.Usuarios
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Apellidos)
                .ThenBy(x => x.Nombres)
                .Select(x => new UsuarioListItem(
                    x.Id,
                    x.Username,
                    x.Nombres,
                    x.Apellidos,
                    x.DocumentoTipo,
                    x.DocumentoNumero,
                    x.Email,
                    x.RolId,
                    x.EstablecimientoBaseId,
                    x.Estado))
                .ToListAsync();

            var establecimientos = await db.Establecimientos
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Codigo)
                .Select(x => new SeguridadEstablecimientoItem(x.Id, x.Codigo, x.Nombre))
                .ToListAsync();

            var sesionesActivas = await db.Sesiones.CountAsync(x => x.Estado == "activa");
            var turnosAbiertos = await db.Turnos.CountAsync(x => x.DeletedAt == null && x.Estado != "cerrado" && x.Estado != "anulado");

            var workspace = new SecurityWorkspace(true, null, roles, permisos, usuarios, establecimientos, sesionesActivas, turnosAbiertos);
            cache.Set(WorkspaceCacheKey, workspace, CacheOptions);
            return workspace;
        }
        catch (Exception ex)
        {
            return SecurityWorkspace.Unavailable(ex.Message);
        }
    }

    public async Task<SecurityOperationResult> CreateRoleAsync(RolForm model)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre))
        {
            return SecurityOperationResult.Fail("El nombre del rol es obligatorio.");
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return SecurityOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var roleName = model.Nombre.Trim();
        var exists = await db.Roles.AnyAsync(x => x.Nombre == roleName && x.DeletedAt == null);
        if (exists)
        {
            return SecurityOperationResult.Fail("Ya existe un rol activo con ese nombre.");
        }

        db.Roles.Add(new SecRol
        {
            Nombre = roleName,
            Descripcion = EmptyToNull(model.Descripcion),
            NivelAutorizacion = (byte)Math.Clamp(model.NivelAutorizacion, 0, 100),
            Estado = "activo"
        });

        await db.SaveChangesAsync();
        InvalidateCache();
        return SecurityOperationResult.Ok("Rol registrado.");
    }

    public async Task<SecurityOperationResult> CreateUserAsync(UsuarioForm model)
    {
        var validation = ValidateUser(model);
        if (!validation.Success)
        {
            return validation;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return SecurityOperationResult.Fail("La conexion a MySQL no esta configurada.");
        }

        var username = model.Username.Trim();
        var exists = await db.Usuarios.AnyAsync(x => x.Username == username && x.DeletedAt == null);
        if (exists)
        {
            return SecurityOperationResult.Fail("Ya existe un usuario activo con ese nombre de acceso.");
        }

        var user = new SecUsuario
        {
            RolId = model.RolId,
            EstablecimientoBaseId = model.EstablecimientoBaseId is null or 0 ? null : model.EstablecimientoBaseId,
            Username = username,
            Nombres = model.Nombres.Trim(),
            Apellidos = model.Apellidos.Trim(),
            DocumentoTipo = model.DocumentoTipo,
            DocumentoNumero = EmptyToNull(model.DocumentoNumero),
            Email = EmptyToNull(model.Email),
            Estado = "activo"
        };

        var hasher = new PasswordHasher<SecUsuario>();
        user.PasswordHash = hasher.HashPassword(user, model.Password);

        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        InvalidateCache();
        return SecurityOperationResult.Ok("Usuario registrado.");
    }

    private static SecurityOperationResult ValidateUser(UsuarioForm model)
    {
        if (model.RolId == 0)
        {
            return SecurityOperationResult.Fail("Selecciona un rol.");
        }

        if (string.IsNullOrWhiteSpace(model.Username))
        {
            return SecurityOperationResult.Fail("El usuario de acceso es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(model.Nombres) || string.IsNullOrWhiteSpace(model.Apellidos))
        {
            return SecurityOperationResult.Fail("Nombres y apellidos son obligatorios.");
        }

        if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
        {
            return SecurityOperationResult.Fail("La clave temporal debe tener al menos 8 caracteres.");
        }

        return SecurityOperationResult.Ok();
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
        cache.Remove(nameof(SecuritySummary));
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed record SecurityWorkspace(
    bool DatabaseConfigured,
    string? StatusMessage,
    IReadOnlyList<RolListItem> Roles,
    IReadOnlyList<PermisoListItem> Permisos,
    IReadOnlyList<UsuarioListItem> Usuarios,
    IReadOnlyList<SeguridadEstablecimientoItem> Establecimientos,
    int SesionesActivas,
    int TurnosAbiertos)
{
    public static SecurityWorkspace Unavailable(string message) => new(false, message, [], [], [], [], 0, 0);
}

public sealed record RolListItem(ulong Id, string Nombre, string? Descripcion, byte NivelAutorizacion, string Estado);

public sealed record PermisoListItem(ulong Id, string Modulo, string Pantalla, string Accion, string Estado);

public sealed record UsuarioListItem(
    ulong Id,
    string Username,
    string Nombres,
    string Apellidos,
    string DocumentoTipo,
    string? DocumentoNumero,
    string? Email,
    ulong RolId,
    ulong? EstablecimientoBaseId,
    string Estado);

public sealed record SeguridadEstablecimientoItem(ulong Id, string Codigo, string Nombre);

public sealed class RolForm
{
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public int NivelAutorizacion { get; set; } = 10;
}

public sealed class UsuarioForm
{
    public ulong RolId { get; set; }
    public ulong? EstablecimientoBaseId { get; set; }
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string DocumentoTipo { get; set; } = "DNI";
    public string? DocumentoNumero { get; set; }
    public string? Email { get; set; }
}

public sealed record SecurityOperationResult(bool Success, string Message)
{
    public static SecurityOperationResult Ok(string message = "") => new(true, message);
    public static SecurityOperationResult Fail(string message) => new(false, message);
}
