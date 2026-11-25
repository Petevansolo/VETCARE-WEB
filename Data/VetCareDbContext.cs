using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Models;

namespace VETCARE_WEB.Data;


public class VetCareDbContext : DbContext
{
    public VetCareDbContext(DbContextOptions<VetCareDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Propietario> Propietarios => Set<Propietario>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<CitaMedica> CitasMedicas => Set<CitaMedica>();
    public DbSet<HistorialClinico> HistorialesClinicos => Set<HistorialClinico>();
    public DbSet<Vacunacion> Vacunaciones => Set<Vacunacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- USUARIOS ----------
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();                    // email UNIQUE
            e.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.FechaRegistro).HasDefaultValueSql("GETDATE()");
        });

        // ---------- PROPIETARIOS (1:1 opcional con usuarios) ----------
        modelBuilder.Entity<Propietario>(e =>
        {
            e.HasIndex(p => p.UsuarioId).IsUnique();                // garantiza el 1:1
            e.HasOne(p => p.Usuario)
             .WithOne(u => u.Propietario)
             .HasForeignKey<Propietario>(p => p.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);                     // borrar usuario borra su perfil
        });

        // ---------- MASCOTAS (1:N con propietarios) ----------
        modelBuilder.Entity<Mascota>(e =>
        {
            e.Property(m => m.Sexo).HasConversion<string>().HasMaxLength(10);
            e.HasOne(m => m.Propietario)
             .WithMany(p => p.Mascotas)
             .HasForeignKey(m => m.PropietarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- CITAS_MEDICAS ----------
        modelBuilder.Entity<CitaMedica>(e =>
        {
            e.Property(c => c.Estado).HasConversion<string>().HasMaxLength(20);

            e.HasOne(c => c.Mascota)
             .WithMany(m => m.Citas)
             .HasForeignKey(c => c.MascotaId)
             .OnDelete(DeleteBehavior.Cascade);

            // Restrict: SQL Server no permite dos rutas de borrado en cascada
            // desde usuarios hacia citas (por veterinario y por propietario→mascota).
            // Además, un veterinario con citas no se borra: se desactiva.
            e.HasOne(c => c.Veterinario)
             .WithMany(u => u.CitasAsignadas)
             .HasForeignKey(c => c.VeterinarioId)
             .OnDelete(DeleteBehavior.Restrict);

            // RF-05 "evitar duplicidad de horarios", garantizado por la propia BD:
            // un veterinario no puede tener dos citas activas a la misma hora.
            // Las canceladas no cuentan, para que su horario se pueda volver a usar.
            e.HasIndex(c => new { c.VeterinarioId, c.FechaHora })
             .IsUnique()
             .HasFilter("[estado] <> 'Cancelada'")
             .HasDatabaseName("UX_citas_veterinario_horario");
        });

        // ---------- HISTORIAL_CLINICO ----------
        modelBuilder.Entity<HistorialClinico>(e =>
        {
            e.HasOne(h => h.Mascota)
             .WithMany(m => m.Historiales)
             .HasForeignKey(h => h.MascotaId)
             .OnDelete(DeleteBehavior.Cascade);

            // NoAction: la cascada ya llega por mascotas; una segunda ruta
            // (mascota → cita → historial) provocaría error en SQL Server.
            e.HasOne(h => h.Cita)
             .WithMany(c => c.Historiales)
             .HasForeignKey(h => h.CitaId)
             .OnDelete(DeleteBehavior.NoAction);

            // Una consulta por cita (evita registrar dos veces la misma atención).
            e.HasIndex(h => h.CitaId).IsUnique();
        });

        // ---------- VACUNACIONES ----------
        modelBuilder.Entity<Vacunacion>(e =>
        {
            e.Property(v => v.Tipo).HasConversion<string>().HasMaxLength(20);
            e.HasOne(v => v.Mascota)
             .WithMany(m => m.Vacunaciones)
             .HasForeignKey(v => v.MascotaId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
