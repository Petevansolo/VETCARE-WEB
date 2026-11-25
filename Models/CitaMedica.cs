using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VETCARE_WEB.Models;


[Table("citas_medicas")]
public class CitaMedica
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>FK → mascotas.id</summary>
    [Column("mascota_id")]
    public int MascotaId { get; set; }

    /// <summary>FK → usuarios.id (usuario con rol veterinario)</summary>
    [Column("veterinario_id")]
    public int VeterinarioId { get; set; }

    [Column("fecha_hora")]
    public DateTime FechaHora { get; set; }

    [Column("motivo")]               // TEXT → nvarchar(max)
    public string? Motivo { get; set; }

    [Column("estado")]
    public EstadoCita Estado { get; set; } = EstadoCita.Programada;

    // ---- Navegación ----
    [ForeignKey(nameof(MascotaId))]
    public Mascota Mascota { get; set; } = null!;

    [ForeignKey(nameof(VeterinarioId))]
    public Usuario Veterinario { get; set; } = null!;

    public List<HistorialClinico> Historiales { get; set; } = new();
}
