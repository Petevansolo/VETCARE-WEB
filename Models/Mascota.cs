using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VETCARE_WEB.Models;

/// <summary>
/// Tabla MASCOTAS: pacientes animales (RF-03).
/// </summary>
[Table("mascotas")]
public class Mascota
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>FK → propietarios.id</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un propietario.")]
    [Column("propietario_id")]
    public int PropietarioId { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50)]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La especie es obligatoria.")]
    [StringLength(30)]
    [Column("especie")]
    public string Especie { get; set; } = string.Empty;

    [StringLength(50)]
    [Column("raza")]
    public string? Raza { get; set; }

    [Column("fecha_nacimiento")]     // DATE
    public DateOnly? FechaNacimiento { get; set; }

    [Column("sexo")]
    public SexoMascota Sexo { get; set; } = SexoMascota.Macho;

    [Range(0, 999.99, ErrorMessage = "El peso debe estar entre 0 y 999.99 kg.")]
    [Column("peso_kg", TypeName = "decimal(5,2)")]
    public decimal? PesoKg { get; set; }

    // ---- Navegación ----
    [ForeignKey(nameof(PropietarioId))]
    public Propietario? Propietario { get; set; }

    public List<CitaMedica> Citas { get; set; } = new();
    public List<HistorialClinico> Historiales { get; set; } = new();
    public List<Vacunacion> Vacunaciones { get; set; } = new();
}
