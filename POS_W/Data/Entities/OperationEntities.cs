namespace POS_W.Data.Entities;

public sealed class OpKardex
{
    public ulong Id { get; set; }
    public ulong EstablecimientoId { get; set; }
    public ulong ProductoId { get; set; }
    public ulong? PresentacionId { get; set; }
    public ulong? LoteId { get; set; }
    public ulong UsuarioId { get; set; }
    public string TipoMovimiento { get; set; } = "ajuste_positivo";
    public string? DocumentoTipo { get; set; }
    public ulong? DocumentoId { get; set; }
    public decimal EntradaBase { get; set; }
    public decimal SalidaBase { get; set; }
    public decimal SaldoBase { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class OpLoteVencimiento
{
    public ulong Id { get; set; }
    public ulong ProductoId { get; set; }
    public ulong? ProveedorId { get; set; }
    public ulong? CompraDetalleId { get; set; }
    public string Lote { get; set; } = "";
    public DateOnly? FechaFabricacion { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public decimal CantidadInicialBase { get; set; }
    public decimal CantidadActualBase { get; set; }
    public decimal CostoUnitario { get; set; }
    public string? Ubicacion { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}
