using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>
/// Gestión de mascotas (RF-03) y búsqueda (RF-08).
/// Permisos:
///  - Cliente: ve, registra, edita y da de baja SOLO sus mascotas.
///  - Veterinario: consulta todas (solo lectura).
///  - Administrador: todo, incluido cambiar el propietario.
/// </summary>
public interface IMascotaService
{
    /// <summary>Lista mascotas visibles para el usuario; filtra por nombre, especie, raza, propietario o teléfono.</summary>
    Task<List<Mascota>> ObtenerTodasAsync(string? filtro = null);

    /// <summary>Mascota para editar (null si no existe o no hay permiso).</summary>
    Task<Mascota?> ObtenerPorIdAsync(int id);

    /// <summary>Registra una mascota. Devuelve el Id creado en <see cref="ResultadoOperacion.Id"/>.</summary>
    Task<ResultadoOperacion> CrearAsync(Mascota mascota);

    Task<ResultadoOperacion> ActualizarAsync(Mascota mascota);

    Task<ResultadoOperacion> EliminarAsync(int id);
}
