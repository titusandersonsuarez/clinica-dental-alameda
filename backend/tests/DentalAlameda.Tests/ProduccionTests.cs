using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Application.Services;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using DentalAlameda.Infrastructure.Persistence;
using DentalAlameda.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Tests;

/// <summary>Usuarios y roles, historia clínica inmutable, auditoría y autorización de datos.</summary>
public class ProduccionTests
{
    private sealed class Usuario(int id, string nombre, string rol) : ICurrentUser
    {
        public int? Id => id; public string Nombre => nombre; public string Rol => rol; public string? Ip => "10.0.0.5";
    }
    private sealed class Reloj : IClock { public DateTime AhoraLocal => new(2026, 10, 23, 10, 0, 0); }
    private sealed class HasherRapido : IPasswordHasher
    {
        public string Hash(string p) => "h:" + p;
        public bool Verify(string p, string h) => h == "h:" + p;
    }
    private sealed class TokenFalso : ITokenService
    {
        public (string, DateTime) Generar(Domain.Entities.Usuario u) => ($"tok-{u.Id}-{u.DebeCambiarPassword}", DateTime.UtcNow.AddHours(1));
    }

    private static AppDbContext Db()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Odontologos.Add(new Odontologo { Id = 1, Nombre = "Dra. Laura Rojas" });
        db.Pacientes.Add(new Paciente { Id = 1, Nombre = "María Gómez", Cedula = "1098765432", Celular = "3001112233" });
        db.Usuarios.Add(new Domain.Entities.Usuario { Id = 1, Email = "admin@x.co", Nombre = "Admin", Rol = Roles.Admin, PasswordHash = "h:Temporal1", DebeCambiarPassword = true });
        db.SaveChanges();
        return db;
    }

    // ---------- Contraseñas ----------
    [Theory]
    [InlineData("corta1", false)]
    [InlineData("soloLetrasLargas", false)]
    [InlineData("1234567890", false)]
    [InlineData("Consultorio2026", true)]
    public void Politica_de_password(string pass, bool valida) => Assert.Equal(valida, PoliticaPassword.Validar(pass) is null);

    [Fact]
    public async Task Cambiar_password_quita_la_marca_de_cambio_obligatorio()
    {
        var db = Db();
        var auth = new AuthService(db, new HasherRapido(), new TokenFalso());
        await Assert.ThrowsAsync<DomainException>(() => auth.CambiarPasswordAsync(1, new("Incorrecta1", "Consultorio2026")));
        await Assert.ThrowsAsync<DomainException>(() => auth.CambiarPasswordAsync(1, new("Temporal1", "corta")));
        var r = await auth.CambiarPasswordAsync(1, new("Temporal1", "Consultorio2026"));
        Assert.False(r.DebeCambiarPassword);
        Assert.False((await db.Usuarios.SingleAsync()).DebeCambiarPassword);
    }

    [Fact]
    public async Task Usuario_desactivado_no_puede_ingresar()
    {
        var db = Db();
        db.Usuarios.Single().Activo = false; db.SaveChanges();
        var auth = new AuthService(db, new HasherRapido(), new TokenFalso());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync(new("admin@x.co", "Temporal1")));
    }

    [Fact]
    public void Token_lleva_marca_de_cambio_obligatorio_solo_si_aplica()
    {
        var svc = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions { Key = new string('k', 40) }));
        string Payload(bool debe) => svc.Generar(new Domain.Entities.Usuario { Id = 1, Email = "a@b.co", Nombre = "A", Rol = Roles.Admin, DebeCambiarPassword = debe }).Token;
        var con = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(Payload(true));
        var sin = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(Payload(false));
        Assert.Contains(con.Claims, c => c.Type == ClaimsPersonal.DebeCambiarPassword);
        Assert.DoesNotContain(sin.Claims, c => c.Type == ClaimsPersonal.DebeCambiarPassword);
    }

    // ---------- Usuarios ----------
    [Fact]
    public async Task Crear_usuario_genera_temporal_y_exige_cambio()
    {
        var db = Db();
        var svc = new UsuariosService(db, new HasherRapido(), new Usuario(1, "Admin", Roles.Admin));
        var r = await svc.CrearAsync(new("recepcion@x.co", "Paola Recepción", Roles.Recepcion, null));
        Assert.Equal(12, r.PasswordTemporal.Length);
        Assert.Null(PoliticaPassword.Validar(r.PasswordTemporal));
        Assert.True(r.Usuario.DebeCambiarPassword);
        await Assert.ThrowsAsync<DomainException>(() => svc.CrearAsync(new("recepcion@x.co", "Otra", Roles.Recepcion, null)));
        await Assert.ThrowsAsync<DomainException>(() => svc.CrearAsync(new("doc@x.co", "Doctor", Roles.Odontologo, null)));
    }

    [Fact]
    public async Task No_se_puede_desactivar_al_ultimo_admin_ni_a_si_mismo()
    {
        var db = Db();
        var svc = new UsuariosService(db, new HasherRapido(), new Usuario(99, "Otro", Roles.Admin));
        await Assert.ThrowsAsync<DomainException>(() => svc.CambiarActivoAsync(1, false));
        var propio = new UsuariosService(db, new HasherRapido(), new Usuario(1, "Admin", Roles.Admin));
        await Assert.ThrowsAsync<DomainException>(() => propio.CambiarActivoAsync(1, false));
    }

    // ---------- Historia clínica y auditoría ----------
    [Fact]
    public async Task Consultar_historia_queda_auditado_con_usuario_e_ip()
    {
        var db = Db();
        await new PacientesService(db, new Usuario(7, "Dra. Laura Rojas", Roles.Odontologo)).DetalleAsync(1);
        var a = await db.Auditoria.SingleAsync();
        Assert.Equal(AccionAuditoria.ConsultaHistoria, a.Accion);
        Assert.Equal("Dra. Laura Rojas", a.UsuarioNombre);
        Assert.Equal("10.0.0.5", a.Ip);
    }

    [Fact]
    public async Task Registro_guarda_autor_y_se_anula_con_motivo_sin_borrarse()
    {
        var db = Db();
        var svc = new PacientesService(db, new Usuario(7, "Dra. Laura Rojas", Roles.Odontologo));
        var r = await svc.AgregarRegistroAsync(1, new CrearRegistroRequest { Fecha = new DateOnly(2026, 10, 20), PiezaFdi = "36", Procedimiento = "Resina oclusal" });
        Assert.Equal("Dra. Laura Rojas", r.Autor);

        await Assert.ThrowsAsync<DomainException>(() => svc.AnularRegistroAsync(1, r.Id, "x"));
        var anulado = await svc.AnularRegistroAsync(1, r.Id, "Se registró en el paciente equivocado");
        Assert.True(anulado.Anulado);
        Assert.Single(db.RegistrosClinicos);   // sigue existiendo
        await Assert.ThrowsAsync<DomainException>(() => svc.AnularRegistroAsync(1, r.Id, "Otra vez por error"));

        var acciones = db.Auditoria.Select(x => x.Accion).ToList();
        Assert.Contains(AccionAuditoria.RegistroCreado, acciones);
        Assert.Contains(AccionAuditoria.RegistroAnulado, acciones);
    }

    [Fact]
    public async Task Cambio_de_antecedentes_guarda_valor_anterior()
    {
        var db = Db();
        var svc = new PacientesService(db, new Usuario(1, "Admin", Roles.Admin));
        await svc.ActualizarAntecedentesAsync(1, "Alergia a la penicilina");
        var a = await db.Auditoria.SingleAsync();
        Assert.Equal(AccionAuditoria.AntecedentesModificados, a.Accion);
        Assert.Contains("penicilina", a.Detalle);
    }

    // ---------- Ley 1581 ----------
    [Fact]
    public async Task Reserva_exige_aceptar_politica_de_datos_y_guarda_la_fecha()
    {
        var db = Db();
        db.Servicios.Add(new Servicio { Id = 1, Codigo = "v", Nombre = "Valoración", DuracionMinutos = 30 });
        db.SaveChanges();
        var agenda = new AgendaService(db, new Reloj());
        var req = new CrearCitaRequest
        {
            ServicioId = 1, OdontologoId = 1, Fecha = new DateOnly(2026, 10, 26), Hora = "09:00",
            Nombre = "Pedro Pérez", Cedula = "80123456", Celular = "3101234567"
        };
        var ex = await Assert.ThrowsAsync<DomainException>(() => agenda.ReservarAsync(req));
        Assert.Contains("política", ex.Message);

        await agenda.ReservarAsync(req with { AceptaPoliticaDatos = true });
        Assert.NotNull((await db.Pacientes.SingleAsync(p => p.Cedula == "80123456")).AutorizaDatosEn);
    }
}
