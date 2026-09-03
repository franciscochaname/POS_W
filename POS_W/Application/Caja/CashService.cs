using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using POS_W.Data;
using POS_W.Data.Entities;

namespace POS_W.Application.Caja;

public sealed class CashService(
    IServiceProvider serviceProvider,
    PosDatabaseSettings databaseSettings,
    IMemoryCache cache)
{
    private const string WorkspaceCacheKey = "cash:workspace";

    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
        SlidingExpiration = TimeSpan.FromSeconds(10)
    };

    public async Task<CashWorkspace> GetWorkspaceAsync()
    {
        if (cache.TryGetValue(WorkspaceCacheKey, out CashWorkspace? cached) && cached is not null)
        {
            return cached;
        }

        var db = CreateDbContext();
        if (db is null)
        {
            return CashWorkspace.Unavailable("Configura la conexion local a MySQL para gestionar caja.");
        }

        try
        {
            var cajas = await db.Cajas
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Codigo)
                .Select(x => new CashRegisterItem(x.Id, x.EstablecimientoId, x.Codigo, x.Nombre, x.Ubicacion, x.Estado))
                .ToListAsync();

            var turnos = await db.Turnos
                .Where(x => x.DeletedAt == null && x.Estado != "cerrado" && x.Estado != "anulado")
                .OrderByDescending(x => x.InicioAt ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .Select(x => new CashShiftItem(x.Id, x.UsuarioId, x.EstablecimientoId, x.CajaId, x.Fecha, x.Estado, x.SaldoInicial, x.SaldoFinal, x.DiferenciaCaja))
                .ToListAsync();

            var usuarios = await db.Usuarios
                .Where(x => x.DeletedAt == null)
                .OrderBy(x => x.Apellidos)
                .ThenBy(x => x.Nombres)
                .Select(x => new CashUserItem(x.Id, x.Nombres, x.Apellidos))
                .ToListAsync();

            var movimientos = await db.MovimientosCaja
                .Where(x => x.DeletedAt == null)
                .OrderByDescending(x => x.Id)
                .Take(100)
                .Select(x => new CashMovementItem(
                    x.Id,
                    x.CajaId,
                    x.TurnoId,
                    x.UsuarioId,
                    x.Tipo,
                    x.Concepto,
                    x.MedioPago,
                    x.Monto,
                    x.EfectivoEsperado,
                    x.EfectivoContado,
                    x.Diferencia,
                    x.CreatedAt))
                .ToListAsync();

            var activeTurnId = turnos.FirstOrDefault()?.Id;
            var activeMovements = activeTurnId is null ? [] : movimientos.Where(x => x.TurnoId == activeTurnId).ToList();
            var activeShift = activeTurnId is null ? null : turnos.FirstOrDefault(x => x.Id == activeTurnId);

            var expectedCash = (activeShift?.SaldoInicial ?? 0m) + activeMovements.Sum(CalculateCashImpact);
            var lastCount = activeMovements
                .Where(x => x.EfectivoContado is not null)
                .OrderByDescending(x => x.Id)
                .Select(x => x.EfectivoContado)
                .FirstOrDefault();
            var difference = lastCount is null ? null : lastCount - expectedCash;

            var workspace = new CashWorkspace(
                true,
                null,
                cajas,
                turnos,
                usuarios,
                movimientos,
                activeShift?.SaldoInicial ?? 0m,
                expectedCash,
                difference);

            cache.Set(WorkspaceCacheKey, workspace, CacheOptions);
            return workspace;
        }
        catch (Exception ex)
        {
            return CashWorkspace.Unavailable(ex.Message);
        }
    }

    private static decimal CalculateCashImpact(CashMovementItem movement)
    {
        if (!movement.MedioPago.Equals("efectivo", StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        return movement.Tipo.ToLowerInvariant() switch
        {
            "apertura" => 0m,
            "ingreso" or "venta" => movement.Monto,
            "retiro" or "egreso" or "devolucion" => -movement.Monto,
            _ => 0m
        };
    }

    private PosDbContext? CreateDbContext()
    {
        if (!databaseSettings.IsConfigured)
        {
            return null;
        }

        var factory = serviceProvider.GetService<IDbContextFactory<PosDbContext>>();
        return factory?.CreateDbContext();
    }
}

public sealed record CashWorkspace(
    bool DatabaseConfigured,
    string? StatusMessage,
    IReadOnlyList<CashRegisterItem> Cajas,
    IReadOnlyList<CashShiftItem> Turnos,
    IReadOnlyList<CashUserItem> Usuarios,
    IReadOnlyList<CashMovementItem> Movimientos,
    decimal SaldoInicial,
    decimal EfectivoEsperado,
    decimal? Diferencia)
{
    public static CashWorkspace Unavailable(string message) => new(false, message, [], [], [], [], 0m, 0m, null);
}

public sealed record CashRegisterItem(ulong Id, ulong EstablecimientoId, string Codigo, string Nombre, string? Ubicacion, string Estado);
public sealed record CashShiftItem(ulong Id, ulong UsuarioId, ulong EstablecimientoId, ulong? CajaId, DateOnly Fecha, string Estado, decimal SaldoInicial, decimal? SaldoFinal, decimal? DiferenciaCaja);
public sealed record CashUserItem(ulong Id, string Nombres, string Apellidos);
public sealed record CashMovementItem(
    ulong Id,
    ulong CajaId,
    ulong TurnoId,
    ulong UsuarioId,
    string Tipo,
    string Concepto,
    string MedioPago,
    decimal Monto,
    decimal? EfectivoEsperado,
    decimal? EfectivoContado,
    decimal? Diferencia,
    DateTime CreatedAt);
