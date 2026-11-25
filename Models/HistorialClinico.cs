using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VETCARE_WEB.Models;

/// <summary>
/// Tabla HISTORIAL_CLINICO: expediente clínico electrónico (RF-06).
/// </summary>
[Table("historial_clinico")]
public class HistorialClinico
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>FK → mascotas.id</summary>
    [Column("mascota_id")]
    public int MascotaId { get; set; }

    /// <summary>
    /// FK → citas_medicas.id. Se define UNIQUE: cada cita genera como máximo un
    /// registro clínico (relación 1:1 opcional, ver Docs/ARQUITECTURA.md).
    /// </summary>
    [Column("cita_id")]
    public int CitaId { get; set; }

    [Column("diagnostico")]
    public string? Diagnostico { get; set; }

    [Column("tratamiento")]
    public string? Tratamiento { get; set; }

    [Column("sintomas")]
    public string? Sintomas { get; set; }

    // Columnas agregadas para cubrir el RF-06 (peso actual e indicaciones médicas).
    [Column("peso_kg", TypeName = "decimal(5,2)")]
    public decimal? PesoKg { get; set; }

    [Column("indicaciones")]
    public string? Indicaciones { get; set; }

    [Column("fecha_atencion")]
    public DateTime FechaAtencion { get; set; } = DateTime.Now;

    // ---- Navegación ----
    [ForeignKey(nameof(MascotaId))]
    public Mascota Mascota { get; set; } = null!;

    [ForeignKey(nameof(CitaId))]
    public CitaMedica Cita { get; set; } = null!;
}
