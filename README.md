# POS_W

Aplicacion web para POS de grifo + minimarket + facturacion electronica, construida con C# y Blazor.

## Estado inicial

- Blazor Web App en `.NET 10`.
- MudBlazor integrado como libreria visual.
- Sidebar principal con modulos operativos.
- Dashboard inicial.
- Paginas base por modulo.
- Reglas de acceso por rol para modulos operativos.

## Stack principal

- Blazor Web App
- MudBlazor
- MySQL `POS`
- Pomelo.EntityFrameworkCore.MySql
- FluentValidation
- Serilog
- ClosedXML
- CsvHelper
- QuestPDF
- MailKit
- Blazor-ApexCharts

## Ejecutar

```powershell
dotnet run --project POS_W/POS_W.csproj
```

URL local por defecto:

`http://localhost:5017`

## Conexion local a MySQL

La aplicacion lee la conexion desde `POS_CONNECTION_STRING` o desde un archivo local ignorado por Git:

`POS_W/appsettings.Development.local.json`

Ejemplo:

```json
{
  "ConnectionStrings": {
    "PosDatabase": "Server=localhost;Port=3306;Database=POS;User=TU_USUARIO;Password=TU_CLAVE;TreatTinyAsBoolean=true;SslMode=None;"
  }
}
```

## Accesos por rol

- Administrador y Supervisor: acceso amplio a gestion y supervision.
- Cajero: POS, caja, arqueo, clientes y comprobantes relacionados a venta.
- Grifero: combustible, lecturas, despachos y caja asignada.
- Perfiles administrativos especializados: catalogo, compras, inventario, reportes o auditoria segun rol.

## Repositorio remoto objetivo

`https://github.com/franciscochaname/POS_W`
