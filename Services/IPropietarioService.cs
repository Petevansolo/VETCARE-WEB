using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>
/// Gestión de propietarios (RF-02) y búsqueda (RF-08).
/// Permisos: consultar → administrador y veterinario; crear/editar/eliminar → administrador.
/// (Cada cliente edita sus propios datos en "Mi perfil", vía ICuentaService.)
/// </summary>
public interface IPropietarioService
{
    /// <summary>Lista propietarios con su usuario y mascotas; filtra por nombre, correo o teléfono.</summary>
    Task<List<Propietario>> ObtenerTodosAsync(string? filtro = null);

    /// <summary>Datos de un propietario listos para el formulario de edición.</summary>
    Task<PropietarioFormModel?> ObtenerFormularioAsync(int id);

    /// <summary>Lista corta (Id, Nombre) para llenar un &lt;select&gt;.</summary>
    Task<List<(int Id, string Nombre)>> ObtenerOpcionesAsync();

    /// <summary>Crea el usuario (rol cliente) y su perfil de propietario.</summary>
    Task<ResultadoOperacion> CrearAsync(PropietarioFormModel form);

    Task<ResultadoOperacion> ActualizarAsync(PropietarioFormModel form);

    /// <summary>Elimina el propietario, su cuenta y, en cascada, sus mascotas y expedientes.</summary>
    Task<ResultadoOperacion> EliminarAsync(int id);
}
