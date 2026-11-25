using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VETCARE_WEB.Models;

/// <summary>
/// Tabla PROPIETARIOS: datos de contacto del cliente.
/// El nombre y el correo viven en la tabla usuarios (relación 1:1).
/// </summary>
[Table("propietarios")]
public class Propietario
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>FK → usuarios.id (UNIQUE para garantizar la relación 1:1).</summary>
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [StringLength(20)]
    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("direccion")]            // TEXT → nvarchar(max)
    public string? Direccion { get; set; }

    [StringLength(100)]
    [Column("contacto_emergencia")]
    public string? ContactoEmergencia { get; set; }

    // ---- Navegación ----
    [ForeignKey(nameof(UsuarioId))]
    public Usuario Usuario { get; set; } = null!;

    /// <summary>Relación 1:N: un propietario registra varias mascotas.</summary>
    public List<Mascota> Mascotas { get; set; } = new();
}
