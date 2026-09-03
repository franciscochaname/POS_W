namespace POS_W.Data.Entities;

public sealed class CfgEstablecimiento
{
    public ulong Id { get; set; }
    public ulong EmpresaId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Estado { get; set; } = "activo";
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
