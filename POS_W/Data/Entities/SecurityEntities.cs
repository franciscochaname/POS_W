namespace POS_W.Data.Entities;

public sealed class SecRol
{
    public ulong Id { get; set; }
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public byte NivelAutorizacion { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class SecPermiso
{
    public ulong Id { get; set; }
    public string Modulo { get; set; } = "";
    public string Pantalla { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class SecUsuario
{
    public ulong Id { get; set; }
    public ulong RolId { get; set; }
    public ulong? EstablecimientoBaseId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Nombres { get; set; } = "";
    public string Apellidos { get; set; } = "";
    public string DocumentoTipo { get; set; } = "DNI";
    public string? DocumentoNumero { get; set; }
    public string? Email { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class SecSesion
{
    public ulong Id { get; set; }
    public ulong UsuarioId { get; set; }
    public string Estado { get; set; } = "activa";
    public DateTime InicioAt { get; set; }
}

public sealed class SecTurno
{
    public ulong Id { get; set; }
    public ulong UsuarioId { get; set; }
    public ulong EstablecimientoId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Estado { get; set; } = "programado";
    public DateTime? DeletedAt { get; set; }
}

public sealed class SecAuditoria
{
    public ulong Id { get; set; }
    public string Modulo { get; set; } = "";
    public string Accion { get; set; } = "";
    public string? TablaAfectada { get; set; }
    public DateTime CreatedAt { get; set; }
}
