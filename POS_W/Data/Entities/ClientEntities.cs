namespace POS_W.Data.Entities;

public sealed class PosCliente
{
    public ulong Id { get; set; }
    public string DocumentoTipo { get; set; } = "DNI";
    public string DocumentoNumero { get; set; } = "";
    public string RazonSocialNombre { get; set; } = "";
    public string? Direccion { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public DateTime? UltimaConsultaIdentidadAt { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}
