namespace DentalAlameda.Domain;

/// <summary>
/// Horario de atención: L–V 8:00–12:00 y 14:00–18:00; sábado 8:00–12:00; domingo cerrado.
/// Las citas arrancan cada 30 minutos y deben terminar dentro del mismo bloque.
/// </summary>
public static class HorarioClinica
{
    public const int IntervaloMinutos = 30;

    public static IReadOnlyList<(TimeOnly Inicio, TimeOnly Fin)> Bloques(DateOnly fecha) => fecha.DayOfWeek switch
    {
        DayOfWeek.Sunday => [],
        DayOfWeek.Saturday => [(new(8, 0), new(12, 0))],
        _ => [(new(8, 0), new(12, 0)), (new(14, 0), new(18, 0))]
    };

    /// <summary>Horas de inicio posibles para un servicio de la duración dada.</summary>
    public static IEnumerable<TimeOnly> InicioPosibles(DateOnly fecha, int duracionMinutos)
    {
        foreach (var (inicio, fin) in Bloques(fecha))
            for (var t = inicio; t.AddMinutes(duracionMinutos) <= fin && t < fin; t = t.AddMinutes(IntervaloMinutos))
                yield return t;
    }

    public static bool EsHorarioValido(DateOnly fecha, TimeOnly inicio, int duracionMinutos) =>
        InicioPosibles(fecha, duracionMinutos).Contains(inicio);
}
