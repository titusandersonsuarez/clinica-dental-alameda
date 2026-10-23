using DentalAlameda.Application.Dtos;
using DentalAlameda.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DentalAlameda.Api.Controllers;

[ApiController]
[Route("api")]
public class CatalogoController(CatalogoService catalogo) : ControllerBase
{
    /// <summary>Servicios activos con duración y precio de referencia.</summary>
    [HttpGet("servicios")]
    public Task<IReadOnlyList<ServicioDto>> Servicios(CancellationToken ct) => catalogo.ServiciosAsync(ct);

    [HttpGet("odontologos")]
    public Task<IReadOnlyList<OdontologoDto>> Odontologos(CancellationToken ct) => catalogo.OdontologosAsync(ct);
}

[ApiController]
[Route("api")]
public class AgendaController(AgendaService agenda) : ControllerBase
{
    /// <summary>Horarios del día para un odontólogo y servicio, marcando los ocupados.</summary>
    [HttpGet("disponibilidad")]
    public Task<DisponibilidadDto> Disponibilidad([FromQuery] int odontologoId, [FromQuery] int servicioId, [FromQuery] DateOnly fecha, CancellationToken ct)
        => agenda.DisponibilidadAsync(odontologoId, servicioId, fecha, ct);

    /// <summary>Reserva pública. Crea o actualiza el paciente por cédula.</summary>
    [HttpPost("citas")]
    [EnableRateLimiting("reservas")]
    public async Task<ActionResult<CitaCreadaDto>> Reservar(CrearCitaRequest req, CancellationToken ct)
    {
        var cita = await agenda.ReservarAsync(req, ct);
        return Created($"/api/citas/{cita.Id}", cita);
    }

    /// <summary>El paciente consulta sus próximas citas con cédula y celular.</summary>
    [HttpGet("citas/consulta")]
    [EnableRateLimiting("reservas")]
    public Task<IReadOnlyList<CitaPacienteDto>> MisCitas([FromQuery] string cedula, [FromQuery] string celular, CancellationToken ct)
        => agenda.MisCitasAsync(cedula, celular, ct);
}

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public Task<LoginResponse> Login(LoginRequest req, CancellationToken ct) => auth.LoginAsync(req, ct);
}
