using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VETCARE_WEB.Models;

/// <summary>
/// Tabla USUARIOS: cuentas de acceso y rol (admin, veterinario o cliente).
/// </summary>
[Table("usuarios")]
public class Usuario
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;   // UNIQUE (configurado en el DbContext)

    [Required]
    [StringLength(255)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("rol")]
    public RolUsuario Rol { get; set; } = RolUsuario.Cliente;

    [Column("fecha_registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    /// <summary>
    /// Columna agregada al modelo original: permite desactivar una cuenta sin
    /// borrarla (las citas y expedientes conservan su referencia).
    /// Una cuenta inactiva no puede iniciar sesión.
    /// </summary>
    [Column("activo")]
    public bool Activo { get; set; } = true;

    // ---- Navegación ----

    /// <summary>Relación 1:1 opcional: un usuario puede tener un perfil de propietario.</summary>
    public Propietario? Propietario { get; set; }

    /// <summary>Relación 1:N: citas asignadas cuando el usuario es veterinario.</summary>
    public List<CitaMedica> CitasAsignadas { get; set; } = new();
}
