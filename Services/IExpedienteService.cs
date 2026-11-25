using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>
/// Expediente clínico (RF-06), carnet de vacunación (RF-07) y resumen de consulta (RF-10).
/// Permisos: el cliente consulta solo los de sus mascotas; el personal consulta todos;
/// solo el veterinario registra vacunas (el administrador puede eliminar registros erróneos).
/// </summary>
public interface IExpedienteService
{
    /// <summary>Mascota con propietario, consultas, vacunas y citas.</summary>
    Task<Mascota?> ObtenerExpedienteAsync(int mascotaId);

    /// <summary>Una consulta con todos sus datos, para el resumen imprimible.</summary>
    Task<HistorialClinico?> ObtenerConsultaAsync(int historialId);

    /// <summary>Próximas dosis vencidas o que vencen en los próximos días.</summary>
    Task<List<Vacunacion>> ObtenerRecordatoriosAsync(int diasAnticipacion = 30);

    Task<ResultadoOperacion> RegistrarVacunacionAsync(VacunacionFormModel form);

    Task<ResultadoOperacion> EliminarVacunacionAsync(int vacunacionId);
}
