using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace VETCARE_WEB.Seguridad;

/// <summary>
/// Endpoints HTTP de la cuenta. Cerrar sesión y refrescar la cookie deben
/// hacerse con una petición HTTP normal (no desde el circuito de Blazor).
/// </summary>
public static class CuentaEndpoints
{
    public static IEndpointRouteBuilder MapCuentaEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /cuenta/logout  (el formulario incluye el token antiforgery)
        app.MapPost("/cuenta/logout", async (HttpContext http, [FromForm] string? returnUrl) =>
        {
            await http.SignOutAsync(SesionCookie.Esquema);
            return Results.LocalRedirect(SesionCookie.EsUrlLocal(returnUrl) ? returnUrl! : "~/");
        });

        // GET /cuenta/refrescar-sesion?returnUrl=/mi-perfil
        // Vuelve a escribir la cookie con los datos actuales (p. ej. tras cambiar el nombre).
        app.MapGet("/cuenta/refrescar-sesion", async (HttpContext http, string? returnUrl) =>
        {
            if (int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                var usuario = await SesionCookie.CargarUsuarioActivoAsync(http.RequestServices, id);
                if (usuario is null)
                {
                    await http.SignOutAsync(SesionCookie.Esquema);
                }
                else
                {
                    var actual = await http.AuthenticateAsync(SesionCookie.Esquema);
                    var propiedades = new AuthenticationProperties { IsPersistent = actual.Properties?.IsPersistent ?? false };
                    await http.SignInAsync(SesionCookie.Esquema, SesionCookie.CrearPrincipal(usuario), propiedades);
                }
            }
            return Results.LocalRedirect(SesionCookie.EsUrlLocal(returnUrl) ? returnUrl! : "~/");
        }).RequireAuthorization();

        return app;
    }
}
