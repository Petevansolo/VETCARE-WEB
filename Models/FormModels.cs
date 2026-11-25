using System.ComponentModel.DataAnnotations;

namespace VETCARE_WEB.Models;

// Modelos de formulario (no son tablas). Capturan datos en la interfaz y el
// servicio correspondiente los valida y los reparte entre las entidades.

/// <summary>Inicio de sesión (RF-01).</summary>
public class LoginModel
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    public string Password { get; set; } = string.Empty;

    public bool Recordarme { get; set; }
}

/// <summary>Registro público de un cliente nuevo (RF-01 + RF-02).</summary>
public class RegistroModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    public string? Direccion { get; set; }

    [StringLength(100)]
    public string? ContactoEmergencia { get; set; }
}

/// <summary>Datos que el propio usuario puede editar en "Mi perfil" (RF-02).</summary>
public class PerfilModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Solo lectura: el correo es el usuario de acceso.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Indica si se muestran los datos de contacto de propietario.</summary>
    public bool EsCliente { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    [StringLength(100)]
    public string? ContactoEmergencia { get; set; }
}

/// <summary>Cambio de contraseña del usuario actual.</summary>
public class CambioPasswordModel
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string PasswordNueva { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la nueva contraseña.")]
    [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    public string Confirmacion { get; set; } = string.Empty;
}

/// <summary>
/// Formulario de propietario usado por el administrador: junta datos de
/// usuarios (nombre, email, contraseña) y de propietarios (contacto).
/// </summary>
public class PropietarioFormModel
{
    public int Id { get; set; }   // 0 = nuevo

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Obligatoria al crear; si se deja vacía al editar, no se cambia.</summary>
    // La longitud mínima (6) se valida en PropietarioService.
    [StringLength(100)]
    public string? Password { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    public string? Direccion { get; set; }

    [StringLength(100)]
    public string? ContactoEmergencia { get; set; }
}

/// <summary>Alta/edición de personal (administradores y veterinarios).</summary>
public class PersonalFormModel
{
    public int Id { get; set; }   // 0 = nuevo

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Vet;

    public bool Activo { get; set; } = true;

    /// <summary>Obligatoria al crear; vacía al editar = no se cambia.</summary>
    [StringLength(100)]
    public string? Password { get; set; }
}

/// <summary>Solicitud de cita (RF-04).</summary>
public class CitaFormModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una mascota.")]
    public int MascotaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un veterinario.")]
    public int VeterinarioId { get; set; }

    [Required(ErrorMessage = "Indica la fecha.")]
    public DateOnly? Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Elige uno de los horarios disponibles.")]
    public TimeOnly? Hora { get; set; }

    [Required(ErrorMessage = "Describe brevemente el motivo de la consulta.")]
    [StringLength(500)]
    public string? Motivo { get; set; }
}

/// <summary>Registro de la consulta por parte del veterinario (RF-06).</summary>
public class ConsultaFormModel
{
    public int CitaId { get; set; }

    [Range(0.01, 999.99, ErrorMessage = "El peso debe estar entre 0.01 y 999.99 kg.")]
    public decimal? PesoKg { get; set; }

    public string? Sintomas { get; set; }

    [Required(ErrorMessage = "El diagnóstico es obligatorio.")]
    public string? Diagnostico { get; set; }

    public string? Tratamiento { get; set; }

    public string? Indicaciones { get; set; }
}

/// <summary>Registro de una vacuna o desparasitación (RF-07).</summary>
public class VacunacionFormModel
{
    public int MascotaId { get; set; }

    public TipoVacunacion Tipo { get; set; } = TipoVacunacion.Vacuna;

    [Required(ErrorMessage = "Indica el nombre de la vacuna o producto.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Lote { get; set; }

    [Required(ErrorMessage = "Indica la fecha de aplicación.")]
    public DateOnly? FechaAplicacion { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public DateOnly? FechaProximaDosis { get; set; }
}

/// <summary>Métricas del panel de control (RF-09).</summary>
public record MetricasDashboard(
    int CitasDelDia,
    int CitasPendientes,
    int EnConsultorio,
    int MascotasRegistradas,
    int Propietarios,
    int VeterinariosActivos);
