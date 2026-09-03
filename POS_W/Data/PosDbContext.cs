using Microsoft.EntityFrameworkCore;
using POS_W.Data.Entities;

namespace POS_W.Data;

public sealed class PosDbContext(DbContextOptions<PosDbContext> options) : DbContext(options)
{
    public DbSet<CfgEmpresa> Empresas => Set<CfgEmpresa>();
    public DbSet<CfgEstablecimiento> Establecimientos => Set<CfgEstablecimiento>();
    public DbSet<CfgParametro> Parametros => Set<CfgParametro>();
    public DbSet<SecRol> Roles => Set<SecRol>();
    public DbSet<SecPermiso> Permisos => Set<SecPermiso>();
    public DbSet<SecUsuario> Usuarios => Set<SecUsuario>();
    public DbSet<SecSesion> Sesiones => Set<SecSesion>();
    public DbSet<SecTurno> Turnos => Set<SecTurno>();
    public DbSet<SecAuditoria> Auditorias => Set<SecAuditoria>();
    public DbSet<CatCategoria> Categorias => Set<CatCategoria>();
    public DbSet<CatMarca> Marcas => Set<CatMarca>();
    public DbSet<CatUnidadMedida> UnidadesMedida => Set<CatUnidadMedida>();
    public DbSet<CatProducto> Productos => Set<CatProducto>();
    public DbSet<CatPresentacion> Presentaciones => Set<CatPresentacion>();
    public DbSet<OpKardex> Kardex => Set<OpKardex>();
    public DbSet<OpLoteVencimiento> LotesVencimientos => Set<OpLoteVencimiento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CfgEmpresa>(entity =>
        {
            entity.ToTable("cfg_empresas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Ruc).HasColumnName("ruc").HasMaxLength(11);
            entity.Property(x => x.RazonSocial).HasColumnName("razon_social").HasMaxLength(200);
            entity.Property(x => x.NombreComercial).HasColumnName("nombre_comercial").HasMaxLength(200);
            entity.Property(x => x.DireccionFiscal).HasColumnName("direccion_fiscal").HasMaxLength(300);
            entity.Property(x => x.AmbienteSunat).HasColumnName("ambiente_sunat").HasMaxLength(20);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<CfgEstablecimiento>(entity =>
        {
            entity.ToTable("cfg_establecimientos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EmpresaId).HasColumnName("empresa_id");
            entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20);
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(160);
            entity.Property(x => x.Direccion).HasColumnName("direccion").HasMaxLength(300);
            entity.Property(x => x.SerieFactura).HasColumnName("serie_factura").HasMaxLength(8);
            entity.Property(x => x.SerieBoleta).HasColumnName("serie_boleta").HasMaxLength(8);
            entity.Property(x => x.FormatoImpresion).HasColumnName("formato_impresion").HasMaxLength(20);
            entity.Property(x => x.PermiteStockNegativo).HasColumnName("permite_stock_negativo");
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<CfgParametro>(entity =>
        {
            entity.ToTable("cfg_parametros");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EstablecimientoId).HasColumnName("establecimiento_id");
            entity.Property(x => x.Clave).HasColumnName("clave").HasMaxLength(120);
            entity.Property(x => x.Valor).HasColumnName("valor");
            entity.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
        });

        modelBuilder.Entity<SecRol>(entity =>
        {
            entity.ToTable("sec_roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80);
            entity.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(250);
            entity.Property(x => x.NivelAutorizacion).HasColumnName("nivel_autorizacion");
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<SecPermiso>(entity =>
        {
            entity.ToTable("sec_permisos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Modulo).HasColumnName("modulo").HasMaxLength(60);
            entity.Property(x => x.Pantalla).HasColumnName("pantalla").HasMaxLength(80);
            entity.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(60);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<SecUsuario>(entity =>
        {
            entity.ToTable("sec_usuarios");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.RolId).HasColumnName("rol_id");
            entity.Property(x => x.EstablecimientoBaseId).HasColumnName("establecimiento_base_id");
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(80);
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(x => x.Nombres).HasColumnName("nombres").HasMaxLength(120);
            entity.Property(x => x.Apellidos).HasColumnName("apellidos").HasMaxLength(120);
            entity.Property(x => x.DocumentoTipo).HasColumnName("documento_tipo").HasMaxLength(20);
            entity.Property(x => x.DocumentoNumero).HasColumnName("documento_numero").HasMaxLength(20);
            entity.Property(x => x.Email).HasColumnName("email").HasMaxLength(160);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<SecSesion>(entity =>
        {
            entity.ToTable("sec_sesiones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.InicioAt).HasColumnName("inicio_at");
        });

        modelBuilder.Entity<SecTurno>(entity =>
        {
            entity.ToTable("sec_turnos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            entity.Property(x => x.EstablecimientoId).HasColumnName("establecimiento_id");
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(30);
            entity.Property(x => x.Fecha).HasColumnName("fecha");
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<SecAuditoria>(entity =>
        {
            entity.ToTable("sec_auditoria");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Modulo).HasColumnName("modulo").HasMaxLength(60);
            entity.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(80);
            entity.Property(x => x.TablaAfectada).HasColumnName("tabla_afectada").HasMaxLength(80);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<CatCategoria>(entity =>
        {
            entity.ToTable("cat_categorias");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<CatMarca>(entity =>
        {
            entity.ToTable("cat_marcas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<CatUnidadMedida>(entity =>
        {
            entity.ToTable("cat_unidades_medida");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(20);
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(80);
            entity.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<CatProducto>(entity =>
        {
            entity.ToTable("cat_productos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.CategoriaId).HasColumnName("categoria_id");
            entity.Property(x => x.MarcaId).HasColumnName("marca_id");
            entity.Property(x => x.UnidadBaseId).HasColumnName("unidad_base_id");
            entity.Property(x => x.CodigoInterno).HasColumnName("codigo_interno").HasMaxLength(60);
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(200);
            entity.Property(x => x.TipoProducto).HasColumnName("tipo_producto").HasMaxLength(20);
            entity.Property(x => x.PrecioVenta).HasColumnName("precio_venta").HasPrecision(12, 2);
            entity.Property(x => x.CostoPromedio).HasColumnName("costo_promedio").HasPrecision(12, 4);
            entity.Property(x => x.StockMinimo).HasColumnName("stock_minimo").HasPrecision(10, 4);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<CatPresentacion>(entity =>
        {
            entity.ToTable("cat_presentaciones");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProductoId).HasColumnName("producto_id");
            entity.Property(x => x.UnidadMedidaId).HasColumnName("unidad_medida_id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(120);
            entity.Property(x => x.FactorUnidadBase).HasColumnName("factor_unidad_base").HasPrecision(10, 4);
            entity.Property(x => x.PrecioVenta).HasColumnName("precio_venta").HasPrecision(12, 2);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });

        modelBuilder.Entity<OpKardex>(entity =>
        {
            entity.ToTable("op_kardex");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.EstablecimientoId).HasColumnName("establecimiento_id");
            entity.Property(x => x.ProductoId).HasColumnName("producto_id");
            entity.Property(x => x.PresentacionId).HasColumnName("presentacion_id");
            entity.Property(x => x.LoteId).HasColumnName("lote_id");
            entity.Property(x => x.UsuarioId).HasColumnName("usuario_id");
            entity.Property(x => x.TipoMovimiento).HasColumnName("tipo_movimiento").HasMaxLength(40);
            entity.Property(x => x.DocumentoTipo).HasColumnName("documento_tipo").HasMaxLength(40);
            entity.Property(x => x.DocumentoId).HasColumnName("documento_id");
            entity.Property(x => x.EntradaBase).HasColumnName("entrada_base").HasPrecision(10, 4);
            entity.Property(x => x.SalidaBase).HasColumnName("salida_base").HasPrecision(10, 4);
            entity.Property(x => x.SaldoBase).HasColumnName("saldo_base").HasPrecision(10, 4);
            entity.Property(x => x.CostoUnitario).HasColumnName("costo_unitario").HasPrecision(12, 4);
            entity.Property(x => x.Observacion).HasColumnName("observacion").HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        });

        modelBuilder.Entity<OpLoteVencimiento>(entity =>
        {
            entity.ToTable("op_lotes_vencimientos");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.ProductoId).HasColumnName("producto_id");
            entity.Property(x => x.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(x => x.CompraDetalleId).HasColumnName("compra_detalle_id");
            entity.Property(x => x.Lote).HasColumnName("lote").HasMaxLength(80);
            entity.Property(x => x.FechaFabricacion).HasColumnName("fecha_fabricacion");
            entity.Property(x => x.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(x => x.CantidadInicialBase).HasColumnName("cantidad_inicial_base").HasPrecision(10, 4);
            entity.Property(x => x.CantidadActualBase).HasColumnName("cantidad_actual_base").HasPrecision(10, 4);
            entity.Property(x => x.CostoUnitario).HasColumnName("costo_unitario").HasPrecision(12, 4);
            entity.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(160);
            entity.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20);
            entity.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        });
    }
}
