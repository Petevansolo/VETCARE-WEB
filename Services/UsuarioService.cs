using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class UsuarioService(
    IDbContextFactory<VetCareDbContext> factory,
    IPasswordHasher<Usuario> hasher,
    ISesionService sesion) : IUsuarioService
{
    private const int LongitudMinimaPassword = 6;

    public async Task<List<Usuario>> ObtenerPersonalAsync()
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return new();

        await using var db = await factory.CreateDbContextAsync();
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Rol != RolUsuario.Cliente)
            .OrderBy(u => u.Rol).ThenBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task<PersonalFormModel?> ObtenerFormularioPersonalAsync(int id)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return null;

        await using var db = await factory.CreateDbContextAsync();
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.Id == id && u.Rol != RolUsuario.Cliente)
            .Select(u => new PersonalFormModel { Id = u.Id, Nombre = u.Nombre, Email = u.Email, Rol = u.Rol, Activo = u.Activo })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacion> GuardarPersonalAsync(PersonalFormModel form)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return ResultadoOperacion.SinPermiso();

        if (form.Rol == RolUsuario.Cliente)
            return ResultadoOperacion.Error("Las cuentas de cliente se registran desde Propietarios.");

        var correo = CuentaService.Normalizar(form.Email);
        await using var db = await factory.CreateDbContextAsync();

        if (await db.Usuarios.AnyAsync(u => u.Email == correo && u.Id != form.Id))
            return ResultadoOperacion.Error("Ya existe otra cuenta con ese correo.");

        // ----- Alta -----
        if (form.Id == 0)
        {
            if (string.IsNullOrWhiteSpace(form.Password) || form.Password.Length < LongitudMinimaPassword)
                return ResultadoOperacion.Error($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");

            var nuevo = new Usuario
            {
                Nombre = form.Nombre.Trim(),
                Email = correo,
                Rol = form.Rol,
                Activo = true,
                FechaRegistro = DateTime.Now
            };
            nuevo.PasswordHash = hasher.HashPassword(nuevo, form.Password);
            db.Usuarios.Add(nuevo);
            await db.SaveChangesAsync();
            return ResultadoOperacion.Ok(nuevo.Id);
        }

        // ----- Edición -----
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == form.Id);
        if (usuario is null || usuario.Rol == RolUsuario.Cliente)
            return ResultadoOperacion.Error("La cuenta ya no existe.");

        if (usuario.Id == s.Id && (form.Rol != RolUsuario.Admin || !form.Activo))
            return ResultadoOperacion.Error("No puedes quitarte el rol de administrador ni desactivar tu propia cuenta.");

        // Si deja de ser administrador activo, debe quedar al menos otro.
        var dejaDeSerAdmin = usuario.Rol == RolUsuario.Admin && usuario.Activo && (form.Rol != RolUsuario.Admin || !form.Activo);
        if (dejaDeSerAdmin && !await HayOtroAdminActivoAsync(db, usuario.Id))
            return ResultadoOperacion.Error("Debe quedar al menos un administrador activo.");

        // Si deja de atender como veterinario, no debe tener citas pendientes.
        var dejaDeSerVet = usuario.Rol == RolUsuario.Vet && (form.Rol != RolUsuario.Vet || !form.Activo);
        if (dejaDeSerVet && await TieneCitasPendientesAsync(db, usuario.Id))
            return ResultadoOperacion.Error("Este veterinario tiene citas programadas. Cancélalas o espera a atenderlas antes de cambiar su rol o desactivarlo.");

        if (!string.IsNullOrEmpty(form.Password))
        {
            if (form.Password.Length < LongitudMinimaPassword)
                return ResultadoOperacion.Error($"La contraseña debe tener al menos {LongitudMinimaPassword} caracteres.");
            usuario.PasswordHash = hasher.HashPassword(usuario, form.Password);
        }

        usuario.Nombre = form.Nombre.Trim();
        usuario.Email = correo;
        usuario.Rol = form.Rol;
        usuario.Activo = form.Activo;

        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok(usuario.Id);
    }

    public async Task<ResultadoOperacion> CambiarActivoAsync(int usuarioId, bool activo)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.EsAdmin) return ResultadoOperacion.SinPermiso();
        if (usuarioId == s.Id) return ResultadoOperacion.Error("No puedes desactivar tu propia cuenta.");

        await using var db = await factory.CreateDbContextAsync();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null) return ResultadoOperacion.Error("La cuenta ya no existe.");

        if (!activo)
        {
            if (usuario.Rol == RolUsuario.Admin && !await HayOtroAdminActivoAsync(db, usuario.Id))
                return ResultadoOperacion.Error("Debe quedar al menos un administrador activo.");
            if (usuario.Rol == RolUsuario.Vet && await TieneCitasPendientesAsync(db, usuario.Id))
                return ResultadoOperacion.Error("Este veterinario tiene citas programadas. Cancélalas antes de desactivarlo.");
        }

        usuario.Activo = activo;
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    private static Task<bool> HayOtroAdminActivoAsync(VetCareDbContext db, int excluirId) =>
        db.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Admin && u.Activo && u.Id != excluirId);

    private static Task<bool> TieneCitasPendientesAsync(VetCareDbContext db, int veterinarioId)
    {
        var ahora = DateTime.Now;
        return db.CitasMedicas.AnyAsync(c =>
            c.VeterinarioId == veterinarioId &&
            (c.Estado == EstadoCita.Programada || c.Estado == EstadoCita.EnAtencion) &&
            c.FechaHora >= ahora.Date);
    }
}
