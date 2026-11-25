using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;

namespace VETCARE_WEB.Seguridad;

/// <summary>
/// Todo lo relacionado con la cookie de sesión: crearla, renovarla y
/// validarla contra la base de datos.
/// </summary>
public static class SesionCookie
{
    public const string Esquema = CookieAuthenticationDefaults.AuthenticationScheme;

    /// <summary>Cada cuánto se vuelve a comprobar en la BD que la cuenta siga activa.</summary>
    private static readonly TimeSpan IntervaloValidacion = TimeSpan.FromMinutes(1);

    /// <summary>Crea la identidad (claims) que se guarda en la cookie.</summary>
    public static ClaimsPrincipal CrearPrincipal(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new(ClaimsVetCare.ValidadoUtc, DateTime.UtcNow.Ticks.ToString())
        };

        if (usuario.Propietario is not null)
            claims.Add(new Claim(ClaimsVetCare.PropietarioId, usuario.Propietario.Id.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, Esquema));
    }

    /// <summary>Inicia sesión (escribe la cookie). Solo funciona en páginas estáticas o endpoints.</summary>
    public static Task IniciarAsync(HttpContext http, Usuario usuario, bool recordar) =>
        http.SignInAsync(Esquema, CrearPrincipal(usuario), new AuthenticationProperties { IsPersistent = recordar });

    /// <summary>Carga un usuario activo con su perfil de propietario (si tiene).</summary>
    public static async Task<Usuario?> CargarUsuarioActivoAsync(IServiceProvider servicios, int usuarioId)
    {
        var factory = servicios.GetRequiredService<IDbContextFactory<VetCareDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        return await db.Usuarios
            .AsNoTracking()
            .Include(u => u.Propietario)
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Activo);
    }

    /// <summary>
    /// Se ejecuta en cada petición con cookie. Cada <see cref="IntervaloValidacion"/>
    /// revisa en la BD que el usuario exista y esté activo, y actualiza nombre/rol.
    /// Así, si el administrador desactiva una cuenta, esa sesión se cierra sola.
    /// </summary>
    public static async Task ValidarAsync(CookieValidatePrincipalContext contexto)
    {
        var principal = contexto.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
            return;

        if (long.TryParse(principal.FindFirstValue(ClaimsVetCare.ValidadoUtc), out var ticks) &&
            DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) < IntervaloValidacion)
            return;   // validada hace poco

        Usuario? usuario = null;
        if (int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            usuario = await CargarUsuarioActivoAsync(contexto.HttpContext.RequestServices, id);

        if (usuario is null)
        {
            contexto.RejectPrincipal();
            await contexto.HttpContext.SignOutAsync(Esquema);
            return;
        }

        contexto.ReplacePrincipal(CrearPrincipal(usuario));
        contexto.ShouldRenew = true;
    }

    /// <summary>Evita redirecciones a otros sitios (open redirect).</summary>
    public static bool EsUrlLocal(string? url) =>
        !string.IsNullOrWhiteSpace(url) &&
        url.StartsWith('/') &&
        !url.StartsWith("//") &&
        !url.StartsWith("/\\");
}
