using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class PropietarioService(
    IDbContextFactory<VetCareDbContext> factory,
    IPasswordHasher<Usuario> hasher,
    ISesionService sesion) : IPropietarioService
{
    private const int LongitudMinimaPassword = 6;

    public async Task<List<Propietario>> ObtenerTodosAsync(string? filtro = null)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsPersonal) return new();

        await using var db = await factory.CreateDbContextAsync();

        var consulta = db.Propietarios
            .AsNoTracking()
            .Include(p => p.Usuario)
            .Include(p => p.Mascotas)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var f = filtro.Trim();
            consulta = consulta.Where(p =>
                p.Usuario.Nombre.Contains(f) ||
                p.Usuario.Email.Contains(f) ||
                (p.Telefono != null && p.Telefono.Contains(f)) ||
                p.Mascotas.Any(m => m.Nombre.Contains(f)));
        }

        return await consulta.OrderBy(p => p.Usuario.Nombre).ToListAsync();
    }

    public async Task<PropietarioFormModel?> ObtenerFormularioAsync(int id)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return null;

        await using var db = await factory.CreateDbContextAsync();
        return await db.Propietarios
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PropietarioFormModel
            {
                Id = p.Id,
                Nombre = p.Usuario.Nombre,
                Email = p.Usuario.Email,
                Telefono = p.Telefono,
                Direccion = p.Direccion,
                ContactoEmergencia = p.ContactoEmergencia
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<(int Id, string Nombre)>> ObtenerOpcionesAsync()
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsPersonal) return new();

        await using var db = await factory.CreateDbContextAsync();
        var lista = await db.Propietarios
            .AsNoTracking()
            .Where(p => p.Usuario.Activo)
            .OrderBy(p => p.Usuario.Nombre)
            .Select(p => new { p.Id, p.Usuario.Nombre })
            .ToListAsync();

        return lista.Select(x => (x.Id, x.Nombre)).ToList();
    }

    public async Task<ResultadoOperacion> CrearAsync(PropietarioFormModel form)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return ResultadoOperacion.SinPermiso();

        if (string.IsNullOrWhiteSpace(form.Password) || form.Password.Length < LongitudMinimaPassword)
            return ResultadoOperacion.Error($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");

        await using var db = await factory.CreateDbContextAsync();

        var email = CuentaService.Normalizar(form.Email);
        if (await db.Usuarios.AnyAsync(u => u.Email == email))
            return ResultadoOperacion.Error("Ya existe una cuenta con ese correo.");

        var usuario = new Usuario
        {
            Nombre = form.Nombre.Trim(),
            Email = email,
            Rol = RolUsuario.Cliente,
            Activo = true,
            FechaRegistro = DateTime.Now
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, form.Password);
        usuario.Propietario = new Propietario
        {
            Telefono = Limpiar(form.Telefono),
            Direccion = Limpiar(form.Direccion),
            ContactoEmergencia = Limpiar(form.ContactoEmergencia)
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok(usuario.Propietario.Id);
    }

    public async Task<ResultadoOperacion> ActualizarAsync(PropietarioFormModel form)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return ResultadoOperacion.SinPermiso();

        if (!string.IsNullOrEmpty(form.Password) && form.Password.Length < LongitudMinimaPassword)
            return ResultadoOperacion.Error($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");

        await using var db = await factory.CreateDbContextAsync();

        var propietario = await db.Propietarios
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == form.Id);
        if (propietario is null)
            return ResultadoOperacion.Error("El propietario ya no existe.");

        var email = CuentaService.Normalizar(form.Email);
        if (await db.Usuarios.AnyAsync(u => u.Email == email && u.Id != propietario.UsuarioId))
            return ResultadoOperacion.Error("Ya existe otra cuenta con ese correo.");

        propietario.Usuario.Nombre = form.Nombre.Trim();
        propietario.Usuario.Email = email;
        if (!string.IsNullOrEmpty(form.Password))
            propietario.Usuario.PasswordHash = hasher.HashPassword(propietario.Usuario, form.Password);

        propietario.Telefono = Limpiar(form.Telefono);
        propietario.Direccion = Limpiar(form.Direccion);
        propietario.ContactoEmergencia = Limpiar(form.ContactoEmergencia);

        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok(propietario.Id);
    }

    public async Task<ResultadoOperacion> EliminarAsync(int id)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return ResultadoOperacion.SinPermiso();

        await using var db = await factory.CreateDbContextAsync();

        var propietario = await db.Propietarios
            .Include(p => p.Usuario)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (propietario is null)
            return ResultadoOperacion.Error("El propietario ya no existe.");

        // Al borrar el usuario, SQL Server borra en cascada el perfil de
        // propietario, sus mascotas y los registros de esas mascotas.
        db.Usuarios.Remove(propietario.Usuario);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    /// <summary>Convierte textos vacíos en null y quita espacios sobrantes.</summary>
    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
