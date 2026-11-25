using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Data;
using VETCARE_WEB.Models;
using VETCARE_WEB.Seguridad;

namespace VETCARE_WEB.Services;

public class CuentaService(
    IDbContextFactory<VetCareDbContext> factory,
    IPasswordHasher<Usuario> hasher,
    ISesionService sesion) : ICuentaService
{
    public async Task<Usuario?> ValidarCredencialesAsync(string email, string password)
    {
        var correo = Normalizar(email);
        await using var db = await factory.CreateDbContextAsync();

        var usuario = await db.Usuarios
            .Include(u => u.Propietario)
            .FirstOrDefaultAsync(u => u.Email == correo);

        if (usuario is null || !usuario.Activo)
            return null;

        PasswordVerificationResult resultado;
        try
        {
            resultado = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        }
        catch (FormatException)
        {
            // Hash con formato inválido (por ejemplo, un dato cargado a mano).
            return null;
        }

        if (resultado == PasswordVerificationResult.Failed)
            return null;

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = hasher.HashPassword(usuario, password);
            await db.SaveChangesAsync();
        }

        return usuario;
    }

    public async Task<(ResultadoOperacion Resultado, Usuario? Usuario)> RegistrarClienteAsync(RegistroModel modelo)
    {
        var correo = Normalizar(modelo.Email);
        await using var db = await factory.CreateDbContextAsync();

        if (await db.Usuarios.AnyAsync(u => u.Email == correo))
            return (ResultadoOperacion.Error("Ya existe una cuenta con ese correo. Inicia sesión o usa otro correo."), null);

        var usuario = new Usuario
        {
            Nombre = modelo.Nombre.Trim(),
            Email = correo,
            Rol = RolUsuario.Cliente,     // el registro público solo crea clientes
            Activo = true,
            FechaRegistro = DateTime.Now
        };
        usuario.PasswordHash = hasher.HashPassword(usuario, modelo.Password);
        usuario.Propietario = new Propietario
        {
            Telefono = Limpiar(modelo.Telefono),
            Direccion = Limpiar(modelo.Direccion),
            ContactoEmergencia = Limpiar(modelo.ContactoEmergencia)
        };

        db.Usuarios.Add(usuario);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return (ResultadoOperacion.Error("No se pudo crear la cuenta. Es posible que el correo ya esté registrado."), null);
        }

        return (ResultadoOperacion.Ok(usuario.Id), usuario);
    }

    public async Task<PerfilModel?> ObtenerMiPerfilAsync()
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return null;

        await using var db = await factory.CreateDbContextAsync();
        var usuario = await db.Usuarios.AsNoTracking()
            .Include(u => u.Propietario)
            .FirstOrDefaultAsync(u => u.Id == s.Id);
        if (usuario is null) return null;

        return new PerfilModel
        {
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            EsCliente = usuario.Propietario is not null,
            Telefono = usuario.Propietario?.Telefono,
            Direccion = usuario.Propietario?.Direccion,
            ContactoEmergencia = usuario.Propietario?.ContactoEmergencia
        };
    }

    public async Task<ResultadoOperacion> ActualizarMiPerfilAsync(PerfilModel modelo)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return ResultadoOperacion.SinPermiso();

        await using var db = await factory.CreateDbContextAsync();
        var usuario = await db.Usuarios
            .Include(u => u.Propietario)
            .FirstOrDefaultAsync(u => u.Id == s.Id);
        if (usuario is null) return ResultadoOperacion.Error("Tu cuenta ya no existe.");

        usuario.Nombre = modelo.Nombre.Trim();
        if (usuario.Propietario is not null)
        {
            if (string.IsNullOrWhiteSpace(modelo.Telefono))
                return ResultadoOperacion.Error("El teléfono es obligatorio para poder contactarte.");

            usuario.Propietario.Telefono = Limpiar(modelo.Telefono);
            usuario.Propietario.Direccion = Limpiar(modelo.Direccion);
            usuario.Propietario.ContactoEmergencia = Limpiar(modelo.ContactoEmergencia);
        }

        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    public async Task<ResultadoOperacion> CambiarPasswordAsync(CambioPasswordModel modelo)
    {
        var s = await sesion.ObtenerAsync();
        if (!s.Autenticado) return ResultadoOperacion.SinPermiso();

        await using var db = await factory.CreateDbContextAsync();
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == s.Id);
        if (usuario is null) return ResultadoOperacion.Error("Tu cuenta ya no existe.");

        var verificacion = hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, modelo.PasswordActual);
        if (verificacion == PasswordVerificationResult.Failed)
            return ResultadoOperacion.Error("La contraseña actual no es correcta.");

        usuario.PasswordHash = hasher.HashPassword(usuario, modelo.PasswordNueva);
        await db.SaveChangesAsync();
        return ResultadoOperacion.Ok();
    }

    public static string Normalizar(string email) => email.Trim().ToLowerInvariant();

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
