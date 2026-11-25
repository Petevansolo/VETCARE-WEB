using System.Security.Claims;
using VETCARE_WEB.Models;

namespace VETCARE_WEB.Seguridad;

/// <summary>
/// Datos del usuario que inició sesión, leídos de la cookie.
/// Los servicios lo usan para decidir qué puede ver y hacer cada rol.
/// </summary>
public sealed record UsuarioSesion(int Id, string Nombre, string Email, RolUsuario? Rol, int? PropietarioId)
{
    public static UsuarioSesion Anonimo { get; } = new(0, string.Empty, string.Empty, null, null);

    public bool Autenticado => Id > 0 && Rol is not null;
    public bool EsAdmin => Rol == RolUsuario.Admin;
    public bool EsVet => Rol == RolUsuario.Vet;
    public bool EsCliente => Rol == RolUsuario.Cliente;

    /// <summary>Administrador o veterinario.</summary>
    public bool EsPersonal => EsAdmin || EsVet;

    /// <summary>Construye la sesión a partir de los claims de la cookie.</summary>
    public static UsuarioSesion Desde(ClaimsPrincipal? usuario)
    {
        if (usuario?.Identity?.IsAuthenticated != true)
            return Anonimo;

        if (!int.TryParse(usuario.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Anonimo;

        RolUsuario? rol = Enum.TryParse<RolUsuario>(usuario.FindFirstValue(ClaimTypes.Role), out var r) ? r : null;
        int? propietarioId = int.TryParse(usuario.FindFirstValue(ClaimsVetCare.PropietarioId), out var p) ? p : null;

        return new UsuarioSesion(
            id,
            usuario.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            usuario.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            rol,
            propietarioId);
    }
}
