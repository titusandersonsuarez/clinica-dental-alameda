using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalAlameda.Infrastructure.WhatsApp;

public class TwilioOptions
{
    public string AccountSid { get; set; } = "";
    public string AuthToken { get; set; } = "";
    /// <summary>Remitente: número del sandbox (+14155238886) o tu número de WhatsApp Business, en E.164.</summary>
    public string From { get; set; } = "";
    /// <summary>Content SID (HX…) de la plantilla aprobada por Meta. Vacío = texto libre (solo sandbox o ventana de 24 h).</summary>
    public string PlantillaConfirmacion { get; set; } = "";
    public string PlantillaCancelacion { get; set; } = "";
}

/// <summary>Envía por la API de Mensajería de Twilio (POST /Messages.json) sin depender del SDK.</summary>
public class TwilioWhatsAppNotificador(HttpClient http, IOptions<TwilioOptions> options) : INotificadorWhatsApp
{
    public async Task<ResultadoEnvio> EnviarAsync(MensajeWhatsApp m, CancellationToken ct = default)
    {
        var o = options.Value;
        var form = new Dictionary<string, string>
        {
            ["From"] = "whatsapp:" + o.From,
            ["To"] = "whatsapp:" + m.Telefono
        };
        var plantilla = m.Tipo == TipoNotificacion.ConfirmacionCita ? o.PlantillaConfirmacion : o.PlantillaCancelacion;
        if (!string.IsNullOrWhiteSpace(plantilla))
        {
            form["ContentSid"] = plantilla;
            form["ContentVariables"] = JsonSerializer.Serialize(m.Variables);
        }
        else
        {
            form["Body"] = m.Texto;
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, $"2010-04-01/Accounts/{o.AccountSid}/Messages.json")
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{o.AccountSid}:{o.AuthToken}")));

        HttpResponseMessage res;
        try { res = await http.SendAsync(req, ct); }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            return ResultadoEnvio.Fallo("Sin conexión con Twilio: " + ex.Message, reintentable: true);
        }

        using (res)
        {
            var json = await res.Content.ReadAsStringAsync(ct);
            if (res.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(json);
                return ResultadoEnvio.Exito(doc.RootElement.TryGetProperty("sid", out var sid) ? sid.GetString() : null);
            }

            var detalle = LeerError(json) ?? res.ReasonPhrase ?? "Error de Twilio";
            var reintentable = res.StatusCode == HttpStatusCode.TooManyRequests || (int)res.StatusCode >= 500;
            return ResultadoEnvio.Fallo($"Twilio {(int)res.StatusCode}: {detalle}", reintentable);
        }
    }

    private static string? LeerError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            var code = r.TryGetProperty("code", out var c) ? c.ToString() : null;
            var msg = r.TryGetProperty("message", out var m) ? m.GetString() : null;
            return code is null ? msg : $"[{code}] {msg}";
        }
        catch (JsonException) { return null; }
    }
}

/// <summary>Proveedor de desarrollo: escribe el mensaje en el log en lugar de enviarlo.</summary>
public class ConsolaNotificador(ILogger<ConsolaNotificador> log) : INotificadorWhatsApp
{
    public Task<ResultadoEnvio> EnviarAsync(MensajeWhatsApp m, CancellationToken ct = default)
    {
        log.LogInformation("[WhatsApp simulado] Para {Telefono}: {Texto}", m.Telefono, m.Texto);
        return Task.FromResult(ResultadoEnvio.Exito("consola-" + Guid.NewGuid().ToString("N")[..12]));
    }
}
