namespace POS_W.Data.Entities;

public sealed class CfgEmpresa
{
    public ulong Id { get; set; }
    public string Ruc { get; set; } = "";
    public string RazonSocial { get; set; } = "";
    public string? NombreComercial { get; set; }
    public string DireccionFiscal { get; set; } = "";
    public string? AmbienteSunat { get; set; } = "beta";
    public string Estado { get; set; } = "activo";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
