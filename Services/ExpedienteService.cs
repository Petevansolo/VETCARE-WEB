using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class ExpedienteService(IDbContextFactory<VetCareDbContext> factory, ISesionService sesion) : IExpedienteService
{
    public async Task<Mascota?> ObtenerExpedienteAsync(int mascotaId)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return null;

        await using var db = await factory.CreateDbContextAsync();
        var mascota = await db.Mascotas.AsNoTracking()
            .AsSplitQuery()
            .Include(m => m.Propietario!).ThenInclude(p => p.Usuario)
            .Include(m => m.Historiales).ThenInclude(h => h.Cita).ThenInclude(c => c.Veterinario)
            .Include(m => m.Vacunaciones)
            .Include(m => m.Citas).ThenInclude(c => c.Veterinario)
            .FirstOrDefaultAsync(m => m.Id == mascotaId);

        if (mascota is null) return null;
        if (s.EsCliente && mascota.PropietarioId != s.PropietarioId) return null;
        return mascota;
    }

    public async Task<HistorialClinico?> ObtenerConsultaAsync(int historialId)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return null;

        await using var db = await factory.CreateDbContextAsync();
        var consulta = await db.HistorialesClinicos.AsNoTracking()
            .Include(h => h.Mascota).ThenInclude(m => m.Propietario!).ThenInclude(p => p.Usuario)
            .Include(h => h.Cita).ThenInclude(c => c.Veterinario)
            .FirstOrDefaultAsync(h => h.Id == historialId);

        if (consulta is null) return null;
        if (s.EsCliente && consulta.Mascota.PropietarioId != s.PropietarioId) return null;
        return consulta;
    }

    public async Task<List<Vacunacion>> ObtenerRecordatoriosAsync(int diasAnticipacion = 30)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return new();

        var limite = DateOnly.FromDateTime(DateTime.Today).AddDays(diasAnticipacion);

        await using var db = await factory.CreateDbContextAsync();
        var consulta = db.Vacunaciones.AsNoTracking()
            .Include(v => v.Mascota)
            .Where(v => v.FechaProximaDosis != null && v.FechaProximaDosis <= limite)
            // Solo la aplicación más reciente de cada producto: si ya se volvió
            // a aplicar, el recordatorio anterior deja de tener sentido.
            .Where(v => !db.Vacunaciones.Any(o =>
                o.MascotaId == v.MascotaId && o.Nombre == v.Nombre && o.FechaAplicacion > v.FechaAplicacion));

        if (s.EsCliente)
            consulta = consulta.Where(v => v.Mascota.PropietarioId == s.PropietarioId);

        return await consulta.OrderBy(v => v.FechaProximaDosis).ToListAsync();
    }

    public async Task<ResultadoOperacion> RegistrarVacunacionAsync(VacunacionFormModel form)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsVet) return ResultadoOperacion.SinPermiso();

        if (form.FechaAplicacion is null)
            return ResultadoOperacion.Error("Indica la fecha de aplicación.");

        var hoy = DateOnly.FromDateTime(DateTime.Today);
        if (form.FechaAplicacion > hoy)
            return ResultadoOperacion.Error("La fecha de aplicación no puede ser futura.");
        if (form.FechaProximaDosis is not null && form.FechaProximaDosis <= form.FechaAplicacion)
            return ResultadoOperacion.Error("La próxima dosis debe ser posterior a la fecha de aplicación.");

        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Mascotas.AnyAsync(m => m.Id == form.MascotaId))
            return ResultadoOperacion.Error("La mascota ya no existe.");

        var registro = new Vacunacion
        {
            MascotaId = form.MascotaId,
            Tipo = form.Tipo,
            Nombre = form.Nombre.Trim(),
            Lote = string.IsNullOrWhiteSpace(form.Lote) ? null : form.Lote.Trim(),
            FechaAplicacion = form.FechaAplicacion.Value,
            FechaProximaDosis = form.FechaProximaDosis
        };
        db.Vacunaciones.Add(registro);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok(registro.Id);
    }

    public async Task<ResultadoOperacion> EliminarVacunacionAsync(int vacunacionId)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsPersonal) return ResultadoOperacion.SinPermiso();

        await using var db = await factory.CreateDbContextAsync();
        var registro = await db.Vacunaciones.FirstOrDefaultAsync(v => v.Id == vacunacionId);
        if (registro is null) return ResultadoOperacion.Error("El registro ya no existe.");

        db.Vacunaciones.Remove(registro);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }
}
