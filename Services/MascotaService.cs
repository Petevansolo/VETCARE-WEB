using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class MascotaService(IDbContextFactory<VetCareDbContext> factory, ISesionService sesion) : IMascotaService
{
    public async Task<List<Mascota>> ObtenerTodasAsync(string? filtro = null)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return new();

        await using var db = await factory.CreateDbContextAsync();

        var consulta = db.Mascotas
            .AsNoTracking()
            .Include(m => m.Propietario!).ThenInclude(p => p.Usuario)
            .AsQueryable();

        if (s.EsCliente)
            consulta = consulta.Where(m => m.PropietarioId == s.PropietarioId);

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var f = filtro.Trim();
            consulta = consulta.Where(m =>
                m.Nombre.Contains(f) ||
                m.Especie.Contains(f) ||
                (m.Raza != null && m.Raza.Contains(f)) ||
                m.Propietario!.Usuario.Nombre.Contains(f) ||
                (m.Propietario!.Telefono != null && m.Propietario!.Telefono.Contains(f)));
        }

        return await consulta.OrderBy(m => m.Nombre).ToListAsync();
    }

    public async Task<Mascota?> ObtenerPorIdAsync(int id)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return null;

        await using var db = await factory.CreateDbContextAsync();
        var mascota = await db.Mascotas.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);

        if (mascota is null) return null;
        if (s.EsCliente && mascota.PropietarioId != s.PropietarioId) return null;
        return mascota;
    }

    public async Task<ResultadoOperacion> CrearAsync(Mascota mascota)
    {
        var s = await sesion.ObtenerAsync();
        if (!(s.EsAdmin || s.EsCliente)) return ResultadoOperacion.SinPermiso();

        if (s.EsCliente)
        {
            if (s.PropietarioId is null)
                return ResultadoOperacion.Error("Tu cuenta no tiene perfil de propietario. Contacta a la clínica.");
            mascota.PropietarioId = s.PropietarioId.Value;   // un cliente solo registra mascotas propias
        }

        var error = Validar(mascota);
        if (error is not null) return ResultadoOperacion.Error(error);

        await using var db = await factory.CreateDbContextAsync();

        if (!await db.Propietarios.AnyAsync(p => p.Id == mascota.PropietarioId))
            return ResultadoOperacion.Error("El propietario seleccionado no existe.");

        var nueva = new Mascota
        {
            PropietarioId = mascota.PropietarioId,
            Nombre = mascota.Nombre.Trim(),
            Especie = mascota.Especie,
            Raza = Limpiar(mascota.Raza),
            FechaNacimiento = mascota.FechaNacimiento,
            Sexo = mascota.Sexo,
            PesoKg = mascota.PesoKg
        };
        db.Mascotas.Add(nueva);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok(nueva.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(Mascota mascota)
    {
        var s = await sesion.ObtenerAsync();
        if (!(s.EsAdmin || s.EsCliente)) return ResultadoOperacion.SinPermiso();

        var error = Validar(mascota);
        if (error is not null) return ResultadoOperacion.Error(error);

        await using var db = await factory.CreateDbContextAsync();

        var existente = await db.Mascotas.FirstOrDefaultAsync(m => m.Id == mascota.Id);
        if (existente is null)
            return ResultadoOperacion.Error("La mascota ya no existe.");

        if (s.EsCliente && existente.PropietarioId != s.PropietarioId)
            return ResultadoOperacion.SinPermiso();

        // Solo el administrador puede transferir la mascota a otro propietario.
        if (s.EsAdmin && existente.PropietarioId != mascota.PropietarioId)
        {
            if (!await db.Propietarios.AnyAsync(p => p.Id == mascota.PropietarioId))
                return ResultadoOperacion.Error("El propietario seleccionado no existe.");
            existente.PropietarioId = mascota.PropietarioId;
        }

        existente.Nombre = mascota.Nombre.Trim();
        existente.Especie = mascota.Especie;
        existente.Raza = Limpiar(mascota.Raza);
        existente.FechaNacimiento = mascota.FechaNacimiento;
        existente.Sexo = mascota.Sexo;
        existente.PesoKg = mascota.PesoKg;

        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok(existente.Id);
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id)
    {
        var s = await sesion.ObtenerAsync();
        if (!(s.EsAdmin || s.EsCliente)) return ResultadoOperacion.SinPermiso();

        await using var db = await factory.CreateDbContextAsync();

        var mascota = await db.Mascotas.FirstOrDefaultAsync(m => m.Id == id);
        if (mascota is null)
            return ResultadoOperacion.Error("La mascota ya no existe.");

        if (s.EsCliente)
        {
            if (mascota.PropietarioId != s.PropietarioId)
                return ResultadoOperacion.SinPermiso();

            // El expediente clínico es un registro médico: un cliente no puede borrarlo.
            var tieneExpediente =
                await db.HistorialesClinicos.AnyAsync(h => h.MascotaId == id) ||
                await db.Vacunaciones.AnyAsync(v => v.MascotaId == id);
            if (tieneExpediente)
                return ResultadoOperacion.Error($"{mascota.Nombre} ya tiene expediente clínico en la clínica, así que no puede eliminarse desde aquí. Solicita la baja en recepción.");
        }

        // Las citas, historiales y vacunaciones se borran en cascada.
        db.Mascotas.Remove(mascota);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    private static string? Validar(Mascota mascota)
    {
        if (string.IsNullOrWhiteSpace(mascota.Nombre)) return "El nombre es obligatorio.";
        if (string.IsNullOrWhiteSpace(mascota.Especie)) return "La especie es obligatoria.";
        if (mascota.FechaNacimiento > DateOnly.FromDateTime(DateTime.Today))
            return "La fecha de nacimiento no puede ser futura.";
        if (mascota.PesoKg is <= 0m)
            return "El peso debe ser mayor a 0.";
        return null;
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
