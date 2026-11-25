using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>
/// Gestión de cuentas por parte del administrador: personal de la clínica
/// (administradores y veterinarios) y activación/desactivación de cualquier cuenta.
/// Los clientes se administran desde Propietarios.
/// </summary>
public interface IUsuarioService
{
    Task<List<Usuario>> ObtenerPersonalAsync();

    Task<PersonalFormModel?> ObtenerFormularioPersonalAsync(int id);

    /// <summary>Crea (Id = 0) o actualiza una cuenta de administrador o veterinario.</summary>
    Task<ResultadoOperacion> GuardarPersonalAsync(PersonalFormModel form);

    /// <summary>Activa o desactiva cualquier cuenta (excepto la propia).</summary>
    Task<ResultadoOperacion> CambiarActivoAsync(int usuarioId, bool activo);
}
