using Microsoft.AspNetCore.Components.Authorization;

namespace VETCARE_WEB.Seguridad;

/// <summary>Da acceso al usuario actual desde cualquier servicio o componente.</summary>
public interface ISesionService
{
    Task<UsuarioSesion> ObtenerAsync();
}

/// <summary>
/// Lee el usuario del AuthenticationStateProvider de Blazor. Funciona tanto en
/// páginas estáticas (SSR) como interactivas (circuito de Blazor Server).
/// </summary>
public class SesionService(AuthenticationStateProvider proveedor) : ISesionService
{
    public async Task<UsuarioSesion> ObtenerAsync()
    {
        var estado = await proveedor.GetAuthenticationStateAsync();
        return UsuarioSesion.Desde(estado.User);
    }
}
