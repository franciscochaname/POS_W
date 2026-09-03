namespace POS_W.Data.Entities;

public sealed class PosCaja
{
    public ulong Id { get; set; }
    public ulong EstablecimientoId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Ubicacion { get; set; }
    public string Estado { get; set; } = "activa";
    public DateTime? DeletedAt { get; set; }
}

public sealed class PosMovimientoCaja
{
    public ulong Id { get; set; }
    public ulong CajaId { get; set; }
    public ulong TurnoId { get; set; }
    public ulong UsuarioId { get; set; }
    public ulong? VentaId { get; set; }
    public string Tipo { get; set; } = "";
    public string Concepto { get; set; } = "";
    public string MedioPago { get; set; } = "efectivo";
    public decimal Monto { get; set; }
    public decimal? EfectivoEsperado { get; set; }
    public decimal? EfectivoContado { get; set; }
    public decimal? Diferencia { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
