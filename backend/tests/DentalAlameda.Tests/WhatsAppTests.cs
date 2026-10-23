using System.Net;
using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Services;
using DentalAlameda.Domain.Entities;
using DentalAlameda.Infrastructure.Persistence;
using DentalAlameda.Infrastructure.WhatsApp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DentalAlameda.Tests;

public class WhatsAppTests
{
    private sealed class RelojFijo : IClock { public DateTime AhoraLocal => new(2026, 10, 23, 10, 0, 0); }

    private sealed class NotificadorFalso(ResultadoEnvio resultado) : INotificadorWhatsApp
    {
        public List<MensajeWhatsApp> Enviados { get; } = [];
        public Task<ResultadoEnvio> EnviarAsync(MensajeWhatsApp m, CancellationToken ct = default)
        {
            Enviados.Add(m);
            return Task.FromResult(resultado);
        }
    }

    private static (AppDbContext db, int citaId) ConCita(bool acepta = true, string celular = "3001112233")
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var cita = new Cita
        {
            Paciente = new Paciente { Nombre = "María Fernanda Gómez", Cedula = "1098765432", Celular = celular, AceptaWhatsApp = acepta },
            Odontologo = new Odontologo { Nombre = "Dra. Laura Rojas" },
            Servicio = new Servicio { Codigo = "valoracion", Nombre = "Valoración", DuracionMinutos = 30 },
            Fecha = new DateOnly(2026, 10, 26), HoraInicio = new TimeOnly(14, 30), HoraFin = new TimeOnly(15, 0)
        };
        db.Citas.Add(cita);
        db.SaveChanges();
        return (db, cita.Id);
    }

    private static NotificacionesService Procesador(AppDbContext db, INotificadorWhatsApp n) =>
        new(db, n, Options.Create(new ClinicaOptions { Nombre = "Clínica Dental Alameda", Direccion = "Calle 1" }), NullLogger<NotificacionesService>.Instance);

    [Theory]
    [InlineData("300 111 2233", "+573001112233")]
    [InlineData("+57 300-111-2233", "+573001112233")]
    [InlineData("6071234567", null)]   // fijo, no es celular
    [InlineData("300111", null)]
    public void Celular_colombiano_a_E164(string entrada, string? esperado) =>
        Assert.Equal(esperado, TelefonoWhatsApp.AE164(entrada));

    [Fact]
    public void Fallo_temporal_reprograma_y_tras_5_intentos_queda_fallida()
    {
        var n = new Notificacion();
        var t0 = new DateTime(2026, 10, 23, 15, 0, 0, DateTimeKind.Utc);
        n.MarcarFallo("timeout", true, t0);
        Assert.Equal(EstadoNotificacion.Pendiente, n.Estado);
        Assert.Equal(t0.AddMinutes(1), n.ProximoIntentoEn);
        for (var i = 0; i < 4; i++) n.MarcarFallo("timeout", true, t0);
        Assert.Equal(EstadoNotificacion.Fallida, n.Estado);
        Assert.Equal(5, n.Intentos);
    }

    [Fact]
    public void Error_definitivo_no_se_reintenta()
    {
        var n = new Notificacion();
        n.MarcarFallo("[63003] número inválido", false, DateTime.UtcNow);
        Assert.Equal(EstadoNotificacion.Fallida, n.Estado);
    }

    [Fact]
    public async Task Confirmar_encola_WhatsApp_solo_si_el_paciente_autorizo()
    {
        var (db, id) = ConCita(acepta: true);
        await new CitasAdminService(db, new RelojFijo()).CambiarEstadoAsync(id, EstadoCita.Confirmada);
        var n = await db.Notificaciones.SingleAsync();
        Assert.Equal(TipoNotificacion.ConfirmacionCita, n.Tipo);
        Assert.Equal("+573001112233", n.Telefono);

        var (db2, id2) = ConCita(acepta: false);
        await new CitasAdminService(db2, new RelojFijo()).CambiarEstadoAsync(id2, EstadoCita.Confirmada);
        Assert.Empty(db2.Notificaciones);
    }

    [Fact]
    public async Task Procesador_envia_mensaje_con_datos_de_la_cita()
    {
        var (db, id) = ConCita();
        await new CitasAdminService(db, new RelojFijo()).CambiarEstadoAsync(id, EstadoCita.Confirmada);
        var falso = new NotificadorFalso(ResultadoEnvio.Exito("SM123"));

        await Procesador(db, falso).ProcesarPendientesAsync();

        var m = Assert.Single(falso.Enviados);
        Assert.Contains("María", m.Texto);
        Assert.Contains("lunes 26 de octubre", m.Texto);
        Assert.Contains("2:30 p.m.", m.Texto);
        Assert.Equal("Dra. Laura Rojas", m.Variables["5"]);
        var n = await db.Notificaciones.SingleAsync();
        Assert.Equal(EstadoNotificacion.Enviada, n.Estado);
        Assert.Equal("SM123", n.IdProveedor);
    }

    [Fact]
    public async Task Aviso_de_confirmacion_se_descarta_si_la_cita_se_cancelo_antes_de_enviarlo()
    {
        var (db, id) = ConCita();
        var admin = new CitasAdminService(db, new RelojFijo());
        await admin.CambiarEstadoAsync(id, EstadoCita.Confirmada);
        await admin.CambiarEstadoAsync(id, EstadoCita.Cancelada);
        var falso = new NotificadorFalso(ResultadoEnvio.Exito("SM1"));

        await Procesador(db, falso).ProcesarPendientesAsync();

        var m = Assert.Single(falso.Enviados);
        Assert.Equal(TipoNotificacion.CancelacionCita, m.Tipo);
        Assert.Contains(db.Notificaciones, n => n.Tipo == TipoNotificacion.ConfirmacionCita && n.Estado == EstadoNotificacion.Fallida);
    }

    // ---------- Proveedor Twilio con HTTP simulado ----------
    private sealed class HandlerFalso(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public HttpRequestMessage? Peticion { get; private set; }
        public string? Cuerpo { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken ct)
        {
            Peticion = r;
            Cuerpo = await r.Content!.ReadAsStringAsync(ct);
            return new HttpResponseMessage(status) { Content = new StringContent(json) };
        }
    }

    private static (TwilioWhatsAppNotificador, HandlerFalso) Twilio(HttpStatusCode status, string json, string plantilla = "")
    {
        var h = new HandlerFalso(status, json);
        var http = new HttpClient(h) { BaseAddress = new Uri("https://api.twilio.com/") };
        var o = Options.Create(new TwilioOptions { AccountSid = "AC123", AuthToken = "tok", From = "+14155238886", PlantillaConfirmacion = plantilla });
        return (new TwilioWhatsAppNotificador(http, o), h);
    }

    private static readonly MensajeWhatsApp Msg = new("+573001112233", TipoNotificacion.ConfirmacionCita,
        new Dictionary<string, string> { ["1"] = "María" }, "Hola María");

    [Fact]
    public async Task Twilio_usa_plantilla_cuando_esta_configurada()
    {
        var (t, h) = Twilio(HttpStatusCode.Created, """{"sid":"SM999","status":"queued"}""", plantilla: "HXabc");
        var r = await t.EnviarAsync(Msg);
        Assert.True(r.Ok);
        Assert.Equal("SM999", r.IdProveedor);
        Assert.EndsWith("/2010-04-01/Accounts/AC123/Messages.json", h.Peticion!.RequestUri!.AbsolutePath);
        Assert.Contains("ContentSid=HXabc", h.Cuerpo);
        Assert.Contains("To=whatsapp%3A%2B573001112233", h.Cuerpo);
        Assert.DoesNotContain("Body=", h.Cuerpo);
    }

    [Fact]
    public async Task Twilio_sin_plantilla_envia_texto_libre()
    {
        var (t, h) = Twilio(HttpStatusCode.Created, """{"sid":"SM1"}""");
        await t.EnviarAsync(Msg);
        Assert.Contains("Body=Hola+Mar", h.Cuerpo);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    public async Task Twilio_clasifica_errores(HttpStatusCode status, bool reintentable)
    {
        var (t, _) = Twilio(status, """{"code":63003,"message":"Invalid destination"}""");
        var r = await t.EnviarAsync(Msg);
        Assert.False(r.Ok);
        Assert.Equal(reintentable, r.Reintentable);
        if (status == HttpStatusCode.BadRequest) Assert.Contains("63003", r.Error);
    }
}
