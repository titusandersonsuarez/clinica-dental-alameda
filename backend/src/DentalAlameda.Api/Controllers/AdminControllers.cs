using DentalAlameda.Application.Dtos;
using DentalAlameda.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalAlameda.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/citas")]
public class CitasAdminController(CitasAdminService citas) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CitaAdminDto>> Listar([FromQuery] string? estado, [FromQuery] int? odontologoId,
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] string? q, CancellationToken ct)
        => citas.ListarAsync(new CitasFiltro(estado, odontologoId, desde, hasta, q), ct);

    [HttpGet("resumen")]
    public Task<ResumenDto> Resumen(CancellationToken ct) => citas.ResumenAsync(ct);

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoRequest req, CancellationToken ct)
    {
        await citas.CambiarEstadoAsync(id, req.Estado, ct);
        return NoContent();
    }

    /// <summary>Vuelve a encolar el WhatsApp del estado actual (confirmación o cancelación).</summary>
    [HttpPost("{id:int}/whatsapp")]
    public async Task<IActionResult> ReenviarWhatsApp(int id, CancellationToken ct)
    {
        await citas.ReenviarWhatsAppAsync(id, ct);
        return Accepted();
    }
}

[ApiController]
[Authorize]
[Route("api/admin/pacientes")]
public class PacientesController(PacientesService pacientes) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<PacienteListaDto>> Listar([FromQuery] string? q, CancellationToken ct) => pacientes.ListarAsync(q, ct);

    [HttpGet("{id:int}")]
    public Task<PacienteDetalleDto> Detalle(int id, CancellationToken ct) => pacientes.DetalleAsync(id, ct);

    [HttpPut("{id:int}/antecedentes")]
    public async Task<IActionResult> Antecedentes(int id, AntecedentesRequest req, CancellationToken ct)
    {
        await pacientes.ActualizarAntecedentesAsync(id, req.Antecedentes, ct);
        return NoContent();
    }

    /// <summary>Agrega un procedimiento a la historia clínica (pieza en notación FDI opcional).</summary>
    [HttpPost("{id:int}/historia")]
    public async Task<ActionResult<RegistroClinicoDto>> AgregarRegistro(int id, CrearRegistroRequest req, CancellationToken ct)
        => Created($"/api/admin/pacientes/{id}", await pacientes.AgregarRegistroAsync(id, req, ct));
}
