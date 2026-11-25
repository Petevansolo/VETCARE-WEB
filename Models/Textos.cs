namespace VETCARE_WEB.Models;

/// <summary>Textos amigables para mostrar enumeraciones y datos en pantalla.</summary>
public static class Textos
{
    public static string Estado(EstadoCita estado) => estado switch
    {
        EstadoCita.EnAtencion => "En atención",
        _ => estado.ToString()
    };

    public static string Rol(RolUsuario rol) => rol switch
    {
        RolUsuario.Admin => "Administrador",
        RolUsuario.Vet => "Veterinario",
        _ => "Cliente"
    };

    public static string Tipo(TipoVacunacion tipo) =>
        tipo == TipoVacunacion.Desparasitacion ? "Desparasitación" : "Vacuna";

    /// <summary>Edad aproximada a partir de la fecha de nacimiento.</summary>
    public static string Edad(DateOnly? nacimiento)
    {
        if (nacimiento is null) return "-";
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var meses = (hoy.Year - nacimiento.Value.Year) * 12 + hoy.Month - nacimiento.Value.Month;
        if (hoy.Day < nacimiento.Value.Day) meses--;
        if (meses < 0) return "-";
        if (meses < 12) return meses == 1 ? "1 mes" : $"{meses} meses";
        var anios = meses / 12;
        return anios == 1 ? "1 año" : $"{anios} años";
    }

    public static string Peso(decimal? kg) => kg is null ? "-" : $"{kg.Value:0.##} kg";

    public static string EspecieYRaza(Mascota m) =>
        string.IsNullOrWhiteSpace(m.Raza) ? m.Especie : $"{m.Especie} - {m.Raza}";

    /// <summary>Fecha corta en español, p. ej. "sáb 21/11/2026".</summary>
    public static string Fecha(DateTime fecha) =>
        fecha.ToString("ddd dd/MM/yyyy", Cultura);

    public static string Fecha(DateOnly fecha) =>
        fecha.ToString("ddd dd/MM/yyyy", Cultura);

    public static string FechaLarga(DateOnly fecha) =>
        fecha.ToString("dddd d 'de' MMMM 'de' yyyy", Cultura);

    private static readonly System.Globalization.CultureInfo Cultura = new("es-MX");
}
