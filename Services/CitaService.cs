using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class CitaService(IDbContextFactory<VetCareDbContext> factory, ISesionService sesion) : ICitaService
{
    public async Task<List<Usuario>> ObtenerVeterinariosAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Rol == RolUsuario.Vet && u.Activo)
            .OrderBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<List<Mascota>> ObtenerMascotasSeleccionablesAsync()
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return new();

        await using var db = await factory.CreateDbContextAsync();
        var consulta = db.Mascotas.AsNoTracking()
            .Include(m => m.Propietario!).ThenInclude(p => p.Usuario)
            .Where(m => m.Propietario!.Usuario.Activo);

        if (s.EsCliente)
            consulta = consulta.Where(m => m.PropietarioId == s.PropietarioId);

        return await consulta.OrderBy(m => m.Nombre).ToListAsync();
    }

    public async Task<List<TimeOnly>> ObtenerHorariosDisponiblesAsync(int veterinarioId, DateOnly fecha)
    {
        var s = await sesion.ObtenerAsync();
        var ahora = DateTime.Now;
        if (!s.Autenticado || !HorarioClinica.AbreElDia(fecha) ||
            fecha > DateOnly.FromDateTime(ahora).AddDays(HorarioClinica.DiasMaximosAnticipacion))
            return new();

        var inicio = fecha.ToDateTime(TimeOnly.MinValue);
        var fin = inicio.AddDays(1);

        await using var db = await factory.CreateDbContextAsync();
        var ocupadas = await db.CitasMedicas.AsNoTracking()
            .Where(c => c.VeterinarioId == veterinarioId &&
                        c.FechaHora >= inicio && c.FechaHora < fin &&
                        c.Estado != EstadoCita.Cancelada)
            .Select(c => c.FechaHora)
            .ToListAsync();

        var horasOcupadas = ocupadas.Select(TimeOnly.FromDateTime).ToHashSet();

        return HorarioClinica.BloquesDelDia()
            .Where(h => fecha.ToDateTime(h) > ahora && !horasOcupadas.Contains(h))
            .ToList();
    }

    public async Task<ResultadoOperacion> SolicitarAsync(CitaFormModel form)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return ResultadoOperacion.SinPermiso();

        if (form.Fecha is null || form.Hora is null)
            return ResultadoOperacion.Error("Elige la fecha y uno de los horarios disponibles.");

        var errorHorario = HorarioClinica.Validar(form.Fecha.Value, form.Hora.Value, DateTime.Now);
        if (errorHorario is not null) return ResultadoOperacion.Error(errorHorario);

        var fechaHora = form.Fecha.Value.ToDateTime(form.Hora.Value);

        await using var db = await factory.CreateDbContextAsync();

        var mascota = await db.Mascotas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == form.MascotaId);
        if (mascota is null)
            return ResultadoOperacion.Error("La mascota seleccionada no existe.");
        if (s.EsCliente && mascota.PropietarioId != s.PropietarioId)
            return ResultadoOperacion.SinPermiso();

        var vetDisponible = await db.Usuarios.AnyAsync(u => u.Id == form.VeterinarioId && u.Rol == RolUsuario.Vet && u.Activo);
        if (!vetDisponible)
            return ResultadoOperacion.Error("El veterinario seleccionado no está disponible.");

        // Validación de disponibilidad (RF-05)
        var vetOcupado = await db.CitasMedicas.AnyAsync(c =>
            c.VeterinarioId == form.VeterinarioId && c.FechaHora == fechaHora && c.Estado != EstadoCita.Cancelada);
        if (vetOcupado)
            return ResultadoOperacion.Error("Ese horario ya está ocupado. Elige otro.");

        var mascotaOcupada = await db.CitasMedicas.AnyAsync(c =>
            c.MascotaId == form.MascotaId && c.FechaHora == fechaHora && c.Estado != EstadoCita.Cancelada);
        if (mascotaOcupada)
            return ResultadoOperacion.Error($"{mascota.Nombre} ya tiene otra cita a esa misma hora.");

        var cita = new CitaMedica
        {
            MascotaId = form.MascotaId,
            VeterinarioId = form.VeterinarioId,
            FechaHora = fechaHora,
            Motivo = string.IsNullOrWhiteSpace(form.Motivo) ? null : form.Motivo.Trim(),
            Estado = EstadoCita.Programada
        };
        db.CitasMedicas.Add(cita);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // El índice único UX_citas_veterinario_horario detectó que otra
            // persona tomó el mismo horario un instante antes.
            return ResultadoOperacion.Error("Ese horario acaba de ser ocupado. Elige otro.");
        }

        return ResultadoOperacion.Ok(cita.Id);
    }

    public async Task<List<CitaMedica>> ObtenerAgendaAsync(DateOnly fecha, int? veterinarioId)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsPersonal) return new();

        var inicio = fecha.ToDateTime(TimeOnly.MinValue);
        var fin = inicio.AddDays(1);

        await using var db = await factory.CreateDbContextAsync();
        var consulta = ConsultaConDetalle(db)
            .Where(c => c.FechaHora >= inicio && c.FechaHora < fin);

        if (veterinarioId is not null)
            consulta = consulta.Where(c => c.VeterinarioId == veterinarioId);

        return await consulta.OrderBy(c => c.FechaHora).ThenBy(c => c.Veterinario.Nombre).ToListAsync();
    }

    public async Task<List<CitaMedica>> ObtenerMisCitasAsync()
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsCliente || s.PropietarioId is null) return new();

        await using var db = await factory.CreateDbContextAsync();
        return await ConsultaConDetalle(db)
            .Where(c => c.Mascota.PropietarioId == s.PropietarioId)
            .OrderByDescending(c => c.FechaHora)
            .ToListAsync();
    }

    public async Task<CitaMedica?> ObtenerParaConsultaAsync(int citaId)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsVet) return null;

        await using var db = await factory.CreateDbContextAsync();
        return await db.CitasMedicas.AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Veterinario)
            .Include(c => c.Historiales)
            .Include(c => c.Mascota).ThenInclude(m => m.Propietario!).ThenInclude(p => p.Usuario)
            .Include(c => c.Mascota).ThenInclude(m => m.Historiales)
            .Include(c => c.Mascota).ThenInclude(m => m.Vacunaciones)
            .FirstOrDefaultAsync(c => c.Id == citaId && c.VeterinarioId == s.Id);
    }

    public async Task<ResultadoOperacion> CambiarEstadoAsync(int citaId, EstadoCita nuevoEstado)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsPersonal) return ResultadoOperacion.SinPermiso();

        await using var db = await factory.CreateDbContextAsync();
        var cita = await db.CitasMedicas.FirstOrDefaultAsync(c => c.Id == citaId);
        if (cita is null)
            return ResultadoOperacion.Error("La cita ya no existe.");

        // Un veterinario solo administra sus propias citas.
        if (s.EsVet && cita.VeterinarioId != s.Id)
            return ResultadoOperacion.SinPermiso();

        if (cita.Estado == nuevoEstado)
            return ResultadoOperacion.Ok();

        if (!ReglasCita.TransicionPermitida(cita.Estado, nuevoEstado))
            return ResultadoOperacion.Error($"No se puede pasar de \"{Textos.Estado(cita.Estado)}\" a \"{Textos.Estado(nuevoEstado)}\".");

        cita.Estado = nuevoEstado;
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> CancelarAsync(int citaId)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return ResultadoOperacion.SinPermiso();

        // El personal cancela con las reglas normales de cambio de estado.
        if (s.EsPersonal)
            return await CambiarEstadoAsync(citaId, EstadoCita.Cancelada);

        await using var db = await factory.CreateDbContextAsync();
        var cita = await db.CitasMedicas
            .Include(c => c.Mascota)
            .FirstOrDefaultAsync(c => c.Id == citaId);

        if (cita is null)
            return ResultadoOperacion.Error("La cita ya no existe.");
        if (cita.Mascota.PropietarioId != s.PropietarioId)
            return ResultadoOperacion.SinPermiso();
        if (cita.Estado != EstadoCita.Programada)
            return ResultadoOperacion.Error("Solo se pueden cancelar citas programadas.");
        if (cita.FechaHora <= DateTime.Now)
            return ResultadoOperacion.Error("La hora de la cita ya pasó; comunícate con la clínica.");

        cita.Estado = EstadoCita.Cancelada;
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> RegistrarConsultaAsync(ConsultaFormModel form)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsVet) return ResultadoOperacion.SinPermiso();

        if (string.IsNullOrWhiteSpace(form.Diagnostico))
            return ResultadoOperacion.Error("El diagnóstico es obligatorio.");

        await using var db = await factory.CreateDbContextAsync();
        var cita = await db.CitasMedicas
            .Include(c => c.Mascota)
            .FirstOrDefaultAsync(c => c.Id == form.CitaId);

        if (cita is null)
            return ResultadoOperacion.Error("La cita ya no existe.");
        if (cita.VeterinarioId != s.Id)
            return ResultadoOperacion.SinPermiso();
        if (!ReglasCita.EstaActiva(cita.Estado))
            return ResultadoOperacion.Error($"La cita está \"{Textos.Estado(cita.Estado)}\" y ya no se puede atender.");
        if (await db.HistorialesClinicos.AnyAsync(h => h.CitaId == cita.Id))
            return ResultadoOperacion.Error("Esta cita ya tiene una consulta registrada.");

        db.HistorialesClinicos.Add(new HistorialClinico
        {
            MascotaId = cita.MascotaId,
            CitaId = cita.Id,
            FechaAtencion = DateTime.Now,
            PesoKg = form.PesoKg,
            Sintomas = Limpiar(form.Sintomas),
            Diagnostico = Limpiar(form.Diagnostico),
            Tratamiento = Limpiar(form.Tratamiento),
            Indicaciones = Limpiar(form.Indicaciones)
        });

        // El peso medido en consulta pasa a ser el peso actual de la mascota.
        if (form.PesoKg is not null)
            cita.Mascota.PesoKg = form.PesoKg;

        cita.Estado = EstadoCita.Atendida;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ResultadoOperacion.Error("No se pudo guardar: es posible que la consulta ya se haya registrado.");
        }

        return ResultadoOperacion.Ok(cita.MascotaId);
    }

    /// <summary>Consulta base de citas con mascota, propietario, veterinario e historial.</summary>
    private static IQueryable<CitaMedica> ConsultaConDetalle(VetCareDbContext db) =>
        db.CitasMedicas.AsNoTracking()
            .Include(c => c.Mascota).ThenInclude(m => m.Propietario!).ThenInclude(p => p.Usuario)
            .Include(c => c.Veterinario)
            .Include(c => c.Historiales);

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
