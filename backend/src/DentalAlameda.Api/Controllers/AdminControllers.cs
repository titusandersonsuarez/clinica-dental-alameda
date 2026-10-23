using DentalAlameda.Application.Abstractions;
using DentalAlameda.Application.Dtos;
using DentalAlameda.Application.Services;
using DentalAlameda.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalAlameda.Api.Controllers;

/// <summary>Agenda: la gestionan todos los roles del personal.</summary>
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

/// <summary>Historia clínica: solo Admin y Odontólogo. Cada lectura y cambio queda auditado.</summary>
[ApiController]
[Authorize(Roles = Roles.Clinicos)]
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

    /// <summary>Anula un registro con motivo. Los registros nunca se editan ni se borran.</summary>
    [HttpPost("{id:int}/historia/{registroId:int}/anular")]
    public Task<RegistroClinicoDto> Anular(int id, int registroId, AnularRegistroRequest req, CancellationToken ct)
        => pacientes.AnularRegistroAsync(id, registroId, req.Motivo, ct);
}

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/auditoria")]
public class AuditoriaController(AuditoriaService auditoria) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<AuditoriaDto>> Listar([FromQuery] int? pacienteId, [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta, [FromQuery] string? accion, CancellationToken ct)
        => auditoria.ListarAsync(pacienteId, desde, hasta, accion, ct);
}

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/usuarios")]
public class UsuariosController(UsuariosService usuarios) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<UsuarioDto>> Listar(CancellationToken ct) => usuarios.ListarAsync(ct);

    /// <summary>Crea el usuario con una contraseña temporal que se muestra una sola vez.</summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioConPasswordDto>> Crear(CrearUsuarioRequest req, CancellationToken ct)
        => Created("/api/admin/usuarios", await usuarios.CrearAsync(req, ct));

    [HttpPost("{id:int}/restablecer-password")]
    public Task<UsuarioConPasswordDto> Restablecer(int id, CancellationToken ct) => usuarios.RestablecerPasswordAsync(id, ct);

    [HttpPatch("{id:int}/activo")]
    public async Task<IActionResult> Activo(int id, ActivoRequest req, CancellationToken ct)
    {
        await usuarios.CambiarActivoAsync(id, req.Activo, ct);
        return NoContent();
    }
}
