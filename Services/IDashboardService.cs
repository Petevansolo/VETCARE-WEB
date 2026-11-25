using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>Indicadores del panel de control (RF-09) para el personal.</summary>
public interface IDashboardService
{
    /// <summary>Para un veterinario, las cifras de citas son solo las suyas.</summary>
    Task<MetricasDashboard> ObtenerMetricasAsync();
}
