using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Application.Services;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;
using DentalAlameda.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalAlameda.Tests;

public class AgendaServiceTests
{
    private sealed class RelojFijo(DateTime ahora) : IClock { public DateTime AhoraLocal => ahora; }

    // Viernes 23/10/2026 10:00, hora de Bogotá
    private static readonly DateTime Ahora = new(2026, 10, 23, 10, 0, 0);
    private static readonly DateOnly Lunes = new(2026, 10, 26);

    private static (AppDbContext db, AgendaService svc) Crear()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Servicios.AddRange(
            new Servicio { Id = 1, Codigo = "valoracion", Nombre = "Valoración", DuracionMinutos = 30 },
            new Servicio { Id = 2, Codigo = "endodoncia", Nombre = "Endodoncia", DuracionMinutos = 90 });
        db.Odontologos.Add(new Odontologo { Id = 1, Nombre = "Dra. Rojas" });
        db.SaveChanges();
        return (db, new AgendaService(db, new RelojFijo(Ahora)));
    }

    private static CrearCitaRequest Req(string hora, int servicio = 1, string cedula = "1098765432", DateOnly? fecha = null) => new()
    {
        ServicioId = servicio, OdontologoId = 1, Fecha = fecha ?? Lunes, Hora = hora,
        Nombre = "María Gómez", Cedula = cedula, Celular = "300 111 2233"
    };

    [Fact]
    public async Task Reserva_crea_paciente_y_cita_pendiente()
    {
        var (db, svc) = Crear();
        var r = await svc.ReservarAsync(Req("09:00"));
        Assert.Equal("Pendiente", r.Estado);
        var p = await db.Pacientes.SingleAsync();
        Assert.Equal("3001112233", p.Celular);
    }

    [Fact]
    public async Task No_permite_reserva_doble_en_el_mismo_horario()
    {
        var (_, svc) = Crear();
        await svc.ReservarAsync(Req("09:00"));
        await Assert.ThrowsAsync<DomainException>(() => svc.ReservarAsync(Req("09:00", cedula: "91234567")));
    }

    [Fact]
    public async Task Servicio_largo_bloquea_los_horarios_que_se_cruzan()
    {
        var (_, svc) = Crear();
        await svc.ReservarAsync(Req("08:00", servicio: 2)); // 08:00–09:30
        var d = await svc.DisponibilidadAsync(1, 1, Lunes);
        Assert.False(d.Horarios.Single(h => h.Hora == "09:00").Disponible);
        Assert.True(d.Horarios.Single(h => h.Hora == "09:30").Disponible);
        await Assert.ThrowsAsync<DomainException>(() => svc.ReservarAsync(Req("09:00", cedula: "91234567")));
    }

    [Fact]
    public async Task Cita_cancelada_libera_el_horario()
    {
        var (db, svc) = Crear();
        var r = await svc.ReservarAsync(Req("09:00"));
        var admin = new CitasAdminService(db, new RelojFijo(Ahora));
        await admin.CambiarEstadoAsync(r.Id, EstadoCita.Cancelada);
        var otra = await svc.ReservarAsync(Req("09:00", cedula: "91234567"));
        Assert.Equal("Pendiente", otra.Estado);
    }

    [Fact]
    public async Task No_permite_reservar_en_el_pasado_ni_fuera_de_jornada()
    {
        var (_, svc) = Crear();
        await Assert.ThrowsAsync<DomainException>(() => svc.ReservarAsync(Req("09:00", fecha: new DateOnly(2026, 10, 23))));
        await Assert.ThrowsAsync<DomainException>(() => svc.ReservarAsync(Req("12:30")));
        await Assert.ThrowsAsync<DomainException>(() => svc.ReservarAsync(Req("09:00", fecha: new DateOnly(2026, 10, 25))));
    }

    [Fact]
    public async Task Consulta_de_citas_exige_cedula_y_celular_correctos()
    {
        var (_, svc) = Crear();
        await svc.ReservarAsync(Req("09:00"));
        Assert.Single(await svc.MisCitasAsync("1098765432", "300-111-2233"));
        Assert.Empty(await svc.MisCitasAsync("1098765432", "3009999999"));
    }
}
