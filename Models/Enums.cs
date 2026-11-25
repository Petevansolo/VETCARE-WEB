namespace VETCARE_WEB.Models;

// Enumeraciones del diccionario de datos.
// En la base de datos se guardan como texto (ver VetCareDbContext) para que
// las columnas sean legibles, igual que los ENUM del diseño original.

/// <summary>usuarios.rol → ENUM('admin','vet','cliente')</summary>
public enum RolUsuario
{
    Admin,
    Vet,
    Cliente
}

/// <summary>mascotas.sexo → ENUM('macho','hembra')</summary>
public enum SexoMascota
{
    Macho,
    Hembra
}

/// <summary>
/// citas_medicas.estado → ENUM('programada','atendida','cancelada').
/// Se agrega "EnAtencion" porque el RF-05 y el prototipo HTML lo incluyen.
/// </summary>
public enum EstadoCita
{
    Programada,
    EnAtencion,
    Atendida,
    Cancelada
}

/// <summary>vacunaciones.tipo → ENUM('vacuna','desparasitacion')</summary>
public enum TipoVacunacion
{
    Vacuna,
    Desparasitacion
}
