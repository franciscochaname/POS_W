using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using POS_W.Application.Caja;
using POS_W.Application.Catalogo;
using POS_W.Application.Clientes;
using POS_W.Application.Configuracion;
using POS_W.Application.Inventario;
using POS_W.Application.Modules;
using POS_W.Application.Navigation;
using POS_W.Application.Seguridad;
using POS_W.Components;
using POS_W.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.local.json",
    optional: true,
    reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys")));

var posConnectionString = builder.Configuration.GetConnectionString("PosDatabase")
    ?? Environment.GetEnvironmentVariable("POS_CONNECTION_STRING");

builder.Services.AddSingleton(new PosDatabaseSettings(posConnectionString));

if (!string.IsNullOrWhiteSpace(posConnectionString))
{
    builder.Services.AddDbContextFactory<PosDbContext>(options =>
        options.UseMySql(posConnectionString, new MySqlServerVersion(new Version(8, 0, 0))));
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CashService>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<ClientService>();
builder.Services.AddScoped<ConfigurationService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ModuleDashboardService>();
builder.Services.AddScoped<SecurityService>();
builder.Services.AddSingleton<OperatorAccessService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
