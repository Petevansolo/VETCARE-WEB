using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VETCARE_WEB.Models;

namespace VETCARE_WEB.Data;

public static class DbInitializer
{
    // Contraseñas de las cuentas.
    public const string PasswordVeterinariosDemo = "Vet123*";
    public const string PasswordClientesDemo = "Cliente123*";

    public static async Task InicializarAsync(IServiceProvider servicios)
    {
        var logger = servicios.GetRequiredService<ILoggerFactory>().CreateLogger("VetCare.DbInitializer");
        var config = servicios.GetRequiredService<IConfiguration>();
        var hasher = servicios.GetRequiredService<IPasswordHasher<Usuario>>();
        var factory = servicios.GetRequiredService<IDbContextFactory<VetCareDbContext>>();

        await using var db = await factory.CreateDbContextAsync();

        // ---------- 1. Migraciones ----------
        if (!db.Database.GetMigrations().Any())
            throw new InvalidOperationException(
                "El proyecto no tiene migraciones. En la terminal ejecuta: dotnet ef migrations add InitialCreate");

        try
        {
            await db.Database.MigrateAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "El modelo cambió y falta una migración. En la terminal ejecuta: " +
                "dotnet ef migrations add NombreDelCambio  (y vuelve a ejecutar la aplicación).", ex);
        }

        // ---------- 2. Administrador inicial ----------
        if (!await db.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Admin))
        {
            var email = (config["VetCare:Admin:Email"] ?? "admin@vetcare.mx").Trim().ToLowerInvariant();
            var password = config["VetCare:Admin:Password"] ?? "Admin123*";
            var nombre = config["VetCare:Admin:Nombre"] ?? "Administrador VetCare";

            if (await db.Usuarios.AnyAsync(u => u.Email == email))
            {
                logger.LogWarning("No hay administradores y el correo {Email} ya está en uso por otra cuenta.", email);
            }
            else
            {
                var admin = new Usuario { Nombre = nombre, Email = email, Rol = RolUsuario.Admin, Activo = true, FechaRegistro = DateTime.Now };
                admin.PasswordHash = hasher.HashPassword(admin, password);
                db.Usuarios.Add(admin);
                await db.SaveChangesAsync();
                logger.LogInformation("Administrador inicial creado: {Email}", email);
            }
        }

        // ---------- 3. Datos de demostración ----------
        var sembrarDemo = config.GetValue<bool>("VetCare:SembrarDatosDemo", true);
        if (sembrarDemo && !await db.Usuarios.AnyAsync(u => u.Rol == RolUsuario.Vet))
        {
            await SembrarDatosDemoAsync(db, hasher);
            logger.LogInformation("Datos de demostración cargados (veterinarios, clientes, mascotas, citas y vacunas).");
        }
    }

    private static async Task SembrarDatosDemoAsync(VetCareDbContext db, IPasswordHasher<Usuario> hasher)
    {
        var hoy = DateTime.Today;
        var hoyFecha = DateOnly.FromDateTime(hoy);

        // ----- Ayudantes -----
        Usuario NuevoUsuario(string nombre, string email, RolUsuario rol, string password)
        {
            var u = new Usuario { Nombre = nombre, Email = email, Rol = rol, Activo = true, FechaRegistro = hoy.AddDays(-90) };
            u.PasswordHash = hasher.HashPassword(u, password);
            return u;
        }

        Propietario NuevoCliente(string nombre, string email, string telefono, string direccion, string contacto)
        {
            var usuario = NuevoUsuario(nombre, email, RolUsuario.Cliente, PasswordClientesDemo);
            var propietario = new Propietario { Usuario = usuario, Telefono = telefono, Direccion = direccion, ContactoEmergencia = contacto };
            usuario.Propietario = propietario;
            return propietario;
        }

        Mascota NuevaMascota(Propietario dueno, string nombre, string especie, string raza, SexoMascota sexo, DateOnly nacimiento, decimal peso) =>
            new() { Propietario = dueno, Nombre = nombre, Especie = especie, Raza = raza, Sexo = sexo, FechaNacimiento = nacimiento, PesoKg = peso };

        // Mueve al lunes una fecha que caiga en domingo (la clínica no abre).
        static DateTime DiaHabil(DateTime fecha) => fecha.DayOfWeek == DayOfWeek.Sunday ? fecha.AddDays(1) : fecha;

        CitaMedica NuevaCita(Mascota mascota, Usuario vet, DateTime dia, int hora, int minuto, string motivo, EstadoCita estado) =>
            new() { Mascota = mascota, Veterinario = vet, FechaHora = dia.Date.AddHours(hora).AddMinutes(minuto), Motivo = motivo, Estado = estado };

        HistorialClinico NuevaConsulta(CitaMedica cita, decimal peso, string sintomas, string diagnostico, string tratamiento, string indicaciones) =>
            new()
            {
                Cita = cita,
                Mascota = cita.Mascota,
                FechaAtencion = cita.FechaHora.AddMinutes(20),
                PesoKg = peso,
                Sintomas = sintomas,
                Diagnostico = diagnostico,
                Tratamiento = tratamiento,
                Indicaciones = indicaciones
            };

        // ----- Veterinarios (los del prototipo HTML) -----
        var carlos = NuevoUsuario("Dr. Carlos Ruiz", "carlos.ruiz@vetcare.mx", RolUsuario.Vet, PasswordVeterinariosDemo);
        var ana = NuevoUsuario("Dra. Ana Martínez", "ana.martinez@vetcare.mx", RolUsuario.Vet, PasswordVeterinariosDemo);
        var luis = NuevoUsuario("Dr. Luis Gómez", "luis.gomez@vetcare.mx", RolUsuario.Vet, PasswordVeterinariosDemo);

        // ----- Clientes / propietarios -----
        var roberto = NuevoCliente("Roberto Gómez", "roberto.gomez@example.com", "312 100 2001", "Av. Universidad 333, Colima", "Ana Gómez - 312 100 2002");
        var maria = NuevoCliente("María Fernández", "maria.fernandez@example.com", "312 100 3001", "Calle Madero 120, Colima", "José Fernández - 312 100 3002");
        var juan = NuevoCliente("Juan Pérez", "juan.perez@example.com", "312 100 4001", "Av. Constitución 45, Villa de Álvarez", "Lucía Pérez - 312 100 4002");
        var laura = NuevoCliente("Laura Sánchez", "laura.sanchez@example.com", "312 100 5001", "Calle Morelos 8, Colima", "Pedro Sánchez - 312 100 5002");
        var carmen = NuevoCliente("Carmen Torres", "carmen.torres@example.com", "312 100 6001", "Av. Rey Colimán 210, Colima", "Raúl Torres - 312 100 6002");

        // ----- Mascotas -----
        var rocky = NuevaMascota(roberto, "Rocky", "Canino", "Beagle", SexoMascota.Macho, new DateOnly(2021, 3, 15), 12.5m);
        var luna = NuevaMascota(roberto, "Luna", "Felino", "Mestiza", SexoMascota.Hembra, new DateOnly(2023, 6, 2), 3.8m);
        var michi = NuevaMascota(maria, "Michi", "Felino", "Siamés", SexoMascota.Macho, new DateOnly(2020, 11, 20), 4.2m);
        var bruno = NuevaMascota(juan, "Bruno", "Canino", "Labrador", SexoMascota.Macho, new DateOnly(2019, 8, 10), 30.1m);
        var coco = NuevaMascota(laura, "Coco", "Ave", "Perico australiano", SexoMascota.Macho, new DateOnly(2022, 1, 5), 0.04m);
        var pelusa = NuevaMascota(carmen, "Pelusa", "Felino", "Persa", SexoMascota.Hembra, new DateOnly(2018, 5, 30), 5.0m);

        // ----- Citas pasadas (ya atendidas) -----
        var c1 = NuevaCita(rocky, carlos, DiaHabil(hoy.AddDays(-30)), 10, 0, "Vacunación anual", EstadoCita.Atendida);
        var c2 = NuevaCita(bruno, luis, DiaHabil(hoy.AddDays(-14)), 11, 0, "Alergia en piel", EstadoCita.Atendida);
        var c3 = NuevaCita(michi, ana, DiaHabil(hoy.AddDays(-7)), 16, 0, "Revisión general", EstadoCita.Atendida);

        // ----- Citas de hoy (como en la tabla del prototipo) -----
        var c4 = NuevaCita(rocky, carlos, hoy, 9, 0, "Control de peso", EstadoCita.Atendida);
        var c5 = NuevaCita(michi, ana, hoy, 10, 30, "Revisión de vacunas", EstadoCita.EnAtencion);
        var c6 = NuevaCita(bruno, luis, hoy, 11, 30, "Seguimiento de alergia en piel", EstadoCita.Programada);
        var c7 = NuevaCita(coco, carlos, hoy, 13, 0, "Corte de pico y uñas", EstadoCita.Programada);
        var c8 = NuevaCita(pelusa, ana, hoy, 15, 0, "Limpieza dental", EstadoCita.Cancelada);

        // ----- Citas futuras -----
        var c9 = NuevaCita(luna, carlos, DiaHabil(hoy.AddDays(2)), 10, 0, "Primera consulta y plan de esterilización", EstadoCita.Programada);
        var c10 = NuevaCita(rocky, luis, DiaHabil(hoy.AddDays(5)), 12, 0, "Revisión de oído", EstadoCita.Programada);

        // ----- Consultas (historial clínico) de las citas atendidas -----
        var consultas = new[]
        {
            NuevaConsulta(c1, 12.3m, "Ninguno, revisión preventiva.", "Paciente sano.", "Aplicación de vacuna múltiple canina.", "Refuerzo anual. Observar el sitio de aplicación 24 h."),
            NuevaConsulta(c2, 30.4m, "Comezón intensa y enrojecimiento en abdomen.", "Dermatitis alérgica.", "Antihistamínico oral cada 12 h por 7 días y champú medicado.", "Baño con champú medicado 2 veces por semana. Evitar pasto recién cortado."),
            NuevaConsulta(c3, 4.2m, "Ninguno.", "Paciente sano, ligera acumulación de sarro.", "Ninguno.", "Programar limpieza dental en los próximos meses."),
            NuevaConsulta(c4, 12.5m, "Aumento de peso reportado por el dueño.", "Sobrepeso leve.", "Cambio a alimento light.", "Reducir premios y caminar 30 minutos diarios.")
        };

        // ----- Carnet de vacunación -----
        var vacunas = new[]
        {
            new Vacunacion { Mascota = rocky, Tipo = TipoVacunacion.Vacuna, Nombre = "Múltiple canina (DHPPi+L)", Lote = "MC-2611-A", FechaAplicacion = DateOnly.FromDateTime(c1.FechaHora), FechaProximaDosis = hoyFecha.AddDays(335) },
            new Vacunacion { Mascota = rocky, Tipo = TipoVacunacion.Desparasitacion, Nombre = "Desparasitante interno (Prazicuantel)", Lote = "DP-1102", FechaAplicacion = hoyFecha.AddDays(-100), FechaProximaDosis = hoyFecha.AddDays(-10) },
            new Vacunacion { Mascota = bruno, Tipo = TipoVacunacion.Vacuna, Nombre = "Antirrábica", Lote = "AR-5510", FechaAplicacion = hoyFecha.AddDays(-350), FechaProximaDosis = hoyFecha.AddDays(15) },
            new Vacunacion { Mascota = michi, Tipo = TipoVacunacion.Vacuna, Nombre = "Triple felina", Lote = "TF-3301", FechaAplicacion = DateOnly.FromDateTime(c3.FechaHora), FechaProximaDosis = hoyFecha.AddDays(358) },
            new Vacunacion { Mascota = luna, Tipo = TipoVacunacion.Desparasitacion, Nombre = "Pipeta antipulgas", Lote = "PA-0807", FechaAplicacion = hoyFecha.AddDays(-25), FechaProximaDosis = hoyFecha.AddDays(5) }
        };

        db.Usuarios.AddRange(carlos, ana, luis,
            roberto.Usuario, maria.Usuario, juan.Usuario, laura.Usuario, carmen.Usuario);
        db.Mascotas.AddRange(rocky, luna, michi, bruno, coco, pelusa);
        db.CitasMedicas.AddRange(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10);
        db.HistorialesClinicos.AddRange(consultas);
        db.Vacunaciones.AddRange(vacunas);

        await db.SaveChangesAsync();
    }
}
