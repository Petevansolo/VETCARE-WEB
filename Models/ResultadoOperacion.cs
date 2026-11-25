namespace VETCARE_WEB.Models;

/// <summary>
/// Resultado de una operación de negocio: éxito o mensaje de error para mostrar
/// al usuario. <see cref="Id"/> devuelve el id del registro creado cuando aplica.
/// </summary>
public record ResultadoOperacion(bool Exito, string? Mensaje = null, int? Id = null)
{
    public static ResultadoOperacion Ok() => new(true);
    public static ResultadoOperacion Ok(int id) => new(true, null, id);
    public static ResultadoOperacion Error(string mensaje) => new(false, mensaje);
    public static ResultadoOperacion SinPermiso() => new(false, "No tienes permiso para realizar esta acción.");
}
