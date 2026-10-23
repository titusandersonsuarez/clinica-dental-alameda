namespace DentalAlameda.Domain.Entities;

public enum TipoNotificacion { ConfirmacionCita = 0, CancelacionCita = 1 }
public enum EstadoNotificacion { Pendiente = 0, Enviada = 1, Fallida = 2 }

/// <summary>
/// Mensaje de WhatsApp en cola (patrón outbox): se guarda en la misma transacción que el cambio
/// de la cita y un proceso en segundo plano lo envía con reintentos.
/// </summary>
public class Notificacion
{
    public const int MaxIntentos = 5;
    // Espera antes de cada reintento: 1, 5, 15 y 60 minutos
    private static readonly int[] EsperaMinutos = [1, 5, 15, 60];

    public int Id { get; set; }
    public int CitaId { get; set; }
    public Cita Cita { get; set; } = null!;
    public TipoNotificacion Tipo { get; set; }
    /// <summary>Número en formato E.164, p. ej. +573001112233.</summary>
    public string Telefono { get; set; } = "";
    public EstadoNotificacion Estado { get; private set; } = EstadoNotificacion.Pendiente;
    public int Intentos { get; private set; }
    public string? UltimoError { get; private set; }
    /// <summary>Id del mensaje en el proveedor (SID de Twilio).</summary>
    public string? IdProveedor { get; private set; }
    public DateTime CreadaEn { get; set; } = DateTime.UtcNow;
    public DateTime ProximoIntentoEn { get; set; } = DateTime.UtcNow;
    public DateTime? EnviadaEn { get; private set; }

    public void MarcarEnviada(string? idProveedor, DateTime ahoraUtc)
    {
        Intentos++;
        Estado = EstadoNotificacion.Enviada;
        IdProveedor = idProveedor;
        UltimoError = null;
        EnviadaEn = ahoraUtc;
    }

    /// <summary>Registra un fallo. Si es reintentable y quedan intentos, reprograma; si no, queda Fallida.</summary>
    public void MarcarFallo(string error, bool reintentable, DateTime ahoraUtc)
    {
        Intentos++;
        UltimoError = error.Length > 500 ? error[..500] : error;
        if (!reintentable || Intentos >= MaxIntentos)
        {
            Estado = EstadoNotificacion.Fallida;
            return;
        }
        ProximoIntentoEn = ahoraUtc.AddMinutes(EsperaMinutos[Math.Min(Intentos - 1, EsperaMinutos.Length - 1)]);
    }
}

public static class TelefonoWhatsApp
{
    /// <summary>
    /// Convierte un celular colombiano a E.164: "300 111 2233" → "+573001112233".
    /// Acepta también números que ya traen el 57. Devuelve null si no es un celular válido.
    /// </summary>
    public static string? AE164(string celular)
    {
        var d = new string(celular.Where(char.IsDigit).ToArray());
        if (d.Length == 10 && d[0] == '3') return "+57" + d;
        if (d.Length == 12 && d.StartsWith("573")) return "+" + d;
        return null;
    }
}
