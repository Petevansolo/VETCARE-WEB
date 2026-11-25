using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Components;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;
using VETCARE_WEB.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Blazor ----------
// Las páginas que necesitan interactividad declaran @rendermode InteractiveServer.
// Login y Registro son estáticas (SSR) porque escriben la cookie de sesión.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ---------- Base de datos (EF Core + SQL Server) ----------
// La cadena "VetCareDb" está en appsettings.json. Para usar SQL Express,
// cambia el nombre por "VetCareDb_SqlExpress".
var cadenaConexion = builder.Configuration.GetConnectionString("VetCareDb")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'VetCareDb' en appsettings.json.");

// Fábrica de DbContext (recomendado para Blazor Server): cada operación
// abre un contexto corto y así se evitan errores de concurrencia.
builder.Services.AddDbContextFactory<VetCareDbContext>(options =>
    options.UseSqlServer(cadenaConexion));

// ---------- Autenticación y autorización ----------
// Cookie propia sobre la tabla "usuarios" del modelo de datos (sin ASP.NET Identity,
// para conservar el esquema del documento). El rol viaja como claim.
builder.Services.AddAuthentication(SesionCookie.Esquema)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/cuenta/login";
        opciones.LogoutPath = "/cuenta/logout";
        opciones.AccessDeniedPath = "/cuenta/acceso-denegado";
        opciones.Cookie.Name = "VetCare.Sesion";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
        opciones.Events.OnValidatePrincipal = SesionCookie.ValidarAsync;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ---------- Servicios de la aplicación ----------
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddScoped<ISesionService, SesionService>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IPropietarioService, PropietarioService>();
builder.Services.AddScoped<IMascotaService, MascotaService>();
builder.Services.AddScoped<ICitaService, CitaService>();
builder.Services.AddScoped<IExpedienteService, ExpedienteService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();

// ---------- Base de datos: migraciones + datos iniciales ----------
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.InicializarAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "No se pudo preparar la base de datos: {Mensaje}", ex.Message);
        throw;
    }
}

// ---------- Pipeline HTTP ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapCuentaEndpoints();          // /cuenta/logout y /cuenta/refrescar-sesion
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
