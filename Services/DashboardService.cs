using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class DashboardService(IDbContextFactory<VetCareDbContext> factory, ISesionService sesion) : IDashboardService
{
    public async Task<MetricasDashboard> ObtenerMetricasAsync()
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsPersonal) return new MetricasDashboard(0, 0, 0, 0, 0, 0);

        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);

        await using var db = await factory.CreateDbContextAsync();

        var citasHoy = db.CitasMedicas.Where(c => c.FechaHora >= hoy && c.FechaHora < manana);
        if (s.EsVet)
            citasHoy = citasHoy.Where(c => c.VeterinarioId == s.Id);

        var total = await citasHoy.CountAsync(c => c.Estado != EstadoCita.Cancelada);
        var pendientes = await citasHoy.CountAsync(c => c.Estado == EstadoCita.Programada);
        var enConsultorio = await citasHoy.CountAsync(c => c.Estado == EstadoCita.EnAtencion);
        var mascotas = await db.Mascotas.CountAsync();
        var propietarios = await db.Propietarios.CountAsync();
        var veterinarios = await db.Usuarios.CountAsync(u => u.Rol == RolUsuario.Vet && u.Activo);

        return new MetricasDashboard(total, pendientes, enConsultorio, mascotas, propietarios, veterinarios);
    }
}
