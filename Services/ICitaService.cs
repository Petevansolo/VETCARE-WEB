using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>
/// Agendamiento (RF-04), control de estados (RF-05) y atención de la consulta (RF-06).
/// Permisos:
///  - Cliente: solicita citas para SUS mascotas, ve y cancela sus citas.
///  - Veterinario: ve la agenda, cambia el estado de SUS citas y registra la consulta.
///  - Administrador (recepción): agenda para cualquier mascota y cambia el estado de cualquier cita.
/// </summary>
public interface ICitaService
{
    /// <summary>Veterinarios activos que pueden recibir citas.</summary>
    Task<List<Usuario>> ObtenerVeterinariosAsync();

    /// <summary>Mascotas para las que el usuario puede pedir cita.</summary>
    Task<List<Mascota>> ObtenerMascotasSeleccionablesAsync();

    /// <summary>Bloques libres del veterinario en esa fecha.</summary>
    Task<List<TimeOnly>> ObtenerHorariosDisponiblesAsync(int veterinarioId, DateOnly fecha);

    Task<ResultadoOperacion> SolicitarAsync(CitaFormModel form);

    /// <summary>Agenda de un día para el personal; <paramref name="veterinarioId"/> null = todos.</summary>
    Task<List<CitaMedica>> ObtenerAgendaAsync(DateOnly fecha, int? veterinarioId);

    /// <summary>Todas las citas de las mascotas del cliente actual.</summary>
    Task<List<CitaMedica>> ObtenerMisCitasAsync();

    /// <summary>Cita con los datos del paciente para la pantalla de consulta (solo su veterinario).</summary>
    Task<CitaMedica?> ObtenerParaConsultaAsync(int citaId);

    Task<ResultadoOperacion> CambiarEstadoAsync(int citaId, EstadoCita nuevoEstado);

    /// <summary>Cancela una cita (el cliente solo las suyas y antes de la hora).</summary>
    Task<ResultadoOperacion> CancelarAsync(int citaId);

    /// <summary>Guarda el expediente de la consulta y marca la cita como atendida.</summary>
    Task<ResultadoOperacion> RegistrarConsultaAsync(ConsultaFormModel form);
}
