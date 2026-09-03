namespace POS_W.Data.Entities;

public sealed class CatCategoria
{
    public ulong Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class CatMarca
{
    public ulong Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class CatUnidadMedida
{
    public ulong Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "unidad";
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class CatProducto
{
    public ulong Id { get; set; }
    public ulong? CategoriaId { get; set; }
    public ulong? MarcaId { get; set; }
    public ulong UnidadBaseId { get; set; }
    public string CodigoInterno { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string TipoProducto { get; set; } = "minimarket";
    public decimal StockMinimo { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal PrecioVenta { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}

public sealed class CatPresentacion
{
    public ulong Id { get; set; }
    public ulong ProductoId { get; set; }
    public ulong UnidadMedidaId { get; set; }
    public string Nombre { get; set; } = "";
    public decimal FactorUnidadBase { get; set; }
    public decimal PrecioVenta { get; set; }
    public string Estado { get; set; } = "activo";
    public DateTime? DeletedAt { get; set; }
}
