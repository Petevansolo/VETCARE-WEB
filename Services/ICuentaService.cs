using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>Registro, inicio de sesión y datos de la cuenta propia (RF-01 / RF-02).</summary>
public interface ICuentaService
{
    /// <summary>Devuelve el usuario si el correo y la contraseña son correctos y la cuenta está activa.</summary>
    Task<Usuario?> ValidarCredencialesAsync(string email, string password);

    /// <summary>Crea una cuenta de cliente con su perfil de propietario.</summary>
    Task<(ResultadoOperacion Resultado, Usuario? Usuario)> RegistrarClienteAsync(RegistroModel modelo);

    Task<PerfilModel?> ObtenerMiPerfilAsync();

    Task<ResultadoOperacion> ActualizarMiPerfilAsync(PerfilModel modelo);

    Task<ResultadoOperacion> CambiarPasswordAsync(CambioPasswordModel modelo);
}
