using VETCARE_WEB.Models;

namespace VETCARE_WEB.Seguridad;

/// <summary>
/// Nombres de rol que se guardan en la cookie de sesión y se usan en
/// [Authorize(Roles = ...)] y en &lt;AuthorizeView Roles="..."&gt;.
/// Son iguales a los valores de <see cref="RolUsuario"/>.
/// </summary>
public static class RolesApp
{
    public const string Admin = nameof(RolUsuario.Admin);
    public const string Vet = nameof(RolUsuario.Vet);
    public const string Cliente = nameof(RolUsuario.Cliente);

    /// <summary>Personal de la clínica: administradores y veterinarios.</summary>
    public const string Personal = Admin + "," + Vet;

    /// <summary>Quienes pueden registrar y editar mascotas.</summary>
    public const string AdminOCliente = Admin + "," + Cliente;
}

/// <summary>Claims propios de VetCare que se guardan en la cookie.</summary>
public static class ClaimsVetCare
{
    /// <summary>Id del perfil de propietario (solo clientes).</summary>
    public const string PropietarioId = "vetcare:propietario_id";

    /// <summary>Momento (UTC, en ticks) en que se validó la sesión contra la BD.</summary>
    public const string ValidadoUtc = "vetcare:validado_utc";
}
