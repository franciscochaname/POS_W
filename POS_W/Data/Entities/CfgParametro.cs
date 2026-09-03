namespace POS_W.Data.Entities;

public sealed class CfgParametro
{
    public ulong Id { get; set; }
    public ulong EstablecimientoId { get; set; }
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public string Tipo { get; set; } = "string";
    public string Estado { get; set; } = "activo";
}
