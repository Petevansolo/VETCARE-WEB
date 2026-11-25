using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VETCARE_WEB.Models;

/// <summary>
/// Tabla VACUNACIONES (entidad complementaria propuesta para el RF-07):
/// vacunas y desparasitaciones del carnet digital.
/// </summary>
[Table("vacunaciones")]
public class Vacunacion
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>FK → mascotas.id</summary>
    [Column("mascota_id")]
    public int MascotaId { get; set; }

    [Column("tipo")]
    public TipoVacunacion Tipo { get; set; } = TipoVacunacion.Vacuna;

    [Required]
    [StringLength(100)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(50)]
    [Column("lote")]
    public string? Lote { get; set; }

    [Column("fecha_aplicacion")]
    public DateOnly FechaAplicacion { get; set; }

    [Column("fecha_proxima_dosis")]
    public DateOnly? FechaProximaDosis { get; set; }

    // ---- Navegación ----
    [ForeignKey(nameof(MascotaId))]
    public Mascota Mascota { get; set; } = null!;
}
