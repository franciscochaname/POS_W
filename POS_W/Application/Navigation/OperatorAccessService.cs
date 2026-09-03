using MudBlazor;

namespace POS_W.Application.Navigation;

public sealed class OperatorAccessService
{
    private readonly AppModule[] _modules =
    [
        new("", "Dashboard", Icons.Material.Rounded.Dashboard, "Operacion", "Indicadores del turno y alertas visibles segun rol.", AllRoles, ["pos_ventas", "grf_despachos", "op_kardex"]),
        new("pos", "POS", Icons.Material.Rounded.PointOfSale, "Ventas", "Venta rapida, cliente, carrito, pagos y comprobante.", ["Administrador", "Supervisor", "Cajero"], ["pos_ventas", "pos_venta_detalles", "pos_clientes"]),
        new("combustible", "Combustible", Icons.Material.Rounded.LocalGasStation, "Grifo", "Despachos, lecturas, mangueras, caras, islas y tanques.", ["Administrador", "Supervisor", "Grifero"], ["grf_tanques", "grf_islas", "grf_mangueras", "grf_despachos"]),
        new("caja", "Caja", Icons.Material.Rounded.AccountBalanceWallet, "Caja y arqueo", "Apertura, movimientos, retiros, arqueo y cierre por turno.", ["Administrador", "Supervisor", "Cajero", "Grifero"], ["pos_cajas", "pos_movimientos_caja", "sec_turnos"]),
        new("catalogo", "Catalogo", Icons.Material.Rounded.Category, "Productos", "Productos, unidades, presentaciones, precios y promociones.", ["Administrador", "Supervisor", "Almacenero", "Compras"], ["cat_productos", "cat_presentaciones", "cat_historial_precios"]),
        new("inventario", "Inventario", Icons.Material.Rounded.Inventory2, "Stock", "Kardex, lotes, vencimientos, ajustes y mermas.", ["Administrador", "Supervisor", "Almacenero"], ["op_kardex", "op_lotes_vencimientos"]),
        new("compras", "Compras", Icons.Material.Rounded.ReceiptLong, "Abastecimiento", "Proveedores, facturas, guias, XML y recepcion.", ["Administrador", "Supervisor", "Compras"], ["op_proveedores", "op_compras", "op_compra_detalles"]),
        new("clientes", "Clientes", Icons.Material.Rounded.Groups, "Clientes", "DNI/RUC, direcciones, contacto e historial.", ["Administrador", "Supervisor", "Cajero"], ["pos_clientes"]),
        new("proveedores", "Proveedores", Icons.Material.Rounded.LocalShipping, "Proveedores", "RUC, contactos, condiciones de pago e historial.", ["Administrador", "Supervisor", "Compras"], ["op_proveedores"]),
        new("facturacion", "Facturacion", Icons.Material.Rounded.Description, "SUNAT", "Comprobantes, XML, PDF, CDR y cola de reintentos.", ["Administrador", "Supervisor", "Cajero", "Contabilidad"], ["fe_comprobantes", "fe_envios_sunat"]),
        new("reportes", "Reportes", Icons.Material.Rounded.BarChart, "Analitica", "Ventas, combustible, inventario, compras, caja y exportaciones.", ["Administrador", "Supervisor", "Contabilidad", "Auditor"], ["pos_ventas", "op_kardex", "grf_despachos"]),
        new("seguridad", "Seguridad", Icons.Material.Rounded.AdminPanelSettings, "Accesos", "Roles, permisos, usuarios, sesiones, turnos y auditoria.", ["Administrador", "Supervisor", "Auditor"], ["sec_roles", "sec_permisos", "sec_usuarios", "sec_auditoria"]),
        new("configuracion", "Configuracion", Icons.Material.Rounded.Settings, "Empresa", "Empresa, establecimientos, series, impuestos y reglas.", ["Administrador", "Supervisor"], ["cfg_empresas", "cfg_establecimientos", "cfg_parametros"])
    ];

    public OperatorContext CurrentOperator { get; } = new(
        "admin",
        "Administrador",
        "Administrador",
        "Estacion principal",
        "Turno manana");

    public IReadOnlyList<AppModule> GetVisibleModules()
    {
        return _modules
            .Where(module => CanAccess(module.Route, CurrentOperator.Role))
            .ToArray();
    }

    public AppModule? GetModule(string route)
    {
        return _modules.FirstOrDefault(module => module.Route.Equals(route.Trim('/'), StringComparison.OrdinalIgnoreCase));
    }

    public bool CanAccess(string route, string role)
    {
        var module = GetModule(route);
        return module is not null && module.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<AppModule> GetModulesForRole(string role)
    {
        return _modules
            .Where(module => module.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    private static readonly string[] AllRoles =
    [
        "Administrador",
        "Supervisor",
        "Cajero",
        "Grifero",
        "Almacenero",
        "Compras",
        "Contabilidad",
        "Auditor",
        "Consulta"
    ];
}
