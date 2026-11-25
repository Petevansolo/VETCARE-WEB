using VETCARE_WEB.Models;

namespace VETCARE_WEB.Services;

/// <summary>
/// Horario de atención de la clínica. Las citas se dan en bloques fijos de
/// 30 minutos; así la disponibilidad se calcula de forma sencilla y la BD
/// puede impedir dos citas del mismo veterinario en el mismo bloque.
/// </summary>
public static class HorarioClinica
{
    public static readonly TimeOnly Apertura = new(9, 0);
    public static readonly TimeOnly Cierre = new(18, 0);
    public static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(30);

    /// <summary>Hasta cuántos días hacia adelante se puede agendar.</summary>
    public const int DiasMaximosAnticipacion = 60;

    /// <summary>La clínica atiende de lunes a sábado.</summary>
    public static bool AbreElDia(DateOnly fecha) => fecha.DayOfWeek != DayOfWeek.Sunday;

    /// <summary>Todos los bloques del día: 09:00, 09:30, ..., 17:30.</summary>
    public static IEnumerable<TimeOnly> BloquesDelDia()
    {
        for (var hora = Apertura; hora < Cierre; hora = hora.Add(Intervalo))
            yield return hora;
    }

    /// <summary>Devuelve un mensaje de error si la fecha/hora no es válida, o null si es válida.</summary>
    public static string? Validar(DateOnly fecha, TimeOnly hora, DateTime ahora)
    {
        if (!AbreElDia(fecha))
            return "La clínica no atiende en domingo.";
        if (!BloquesDelDia().Contains(hora))
            return "El horario elegido no es válido.";
        if (fecha.ToDateTime(hora) <= ahora)
            return "No se puede agendar en una fecha u hora que ya pasó.";
        if (fecha > DateOnly.FromDateTime(ahora).AddDays(DiasMaximosAnticipacion))
            return $"Solo se puede agendar con hasta {DiasMaximosAnticipacion} días de anticipación.";
        return null;
    }

    public static string Descripcion =>
        $"Lunes a sábado de {Apertura:HH:mm} a {Cierre:HH:mm} h, citas cada {Intervalo.TotalMinutes:0} minutos.";
}

/// <summary>Reglas del ciclo de vida de una cita (RF-05).</summary>
public static class ReglasCita
{
    /// <summary>
    /// Cambios de estado permitidos desde la agenda.
    /// "Atendida" solo se alcanza registrando la consulta (expediente).
    /// </summary>
    public static bool TransicionPermitida(EstadoCita actual, EstadoCita nuevo) => (actual, nuevo) switch
    {
        (EstadoCita.Programada, EstadoCita.EnAtencion) => true,
        (EstadoCita.Programada, EstadoCita.Cancelada) => true,
        (EstadoCita.EnAtencion, EstadoCita.Programada) => true,   // regresar a sala de espera
        (EstadoCita.EnAtencion, EstadoCita.Cancelada) => true,
        _ => false
    };

    /// <summary>Una cita activa ocupa horario y puede atenderse o cancelarse.</summary>
    public static bool EstaActiva(EstadoCita estado) =>
        estado is EstadoCita.Programada or EstadoCita.EnAtencion;
}
