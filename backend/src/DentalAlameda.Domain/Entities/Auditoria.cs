namespace DentalAlameda.Domain.Entities;

public enum AccionAuditoria
{
    ConsultaHistoria = 0,
    RegistroCreado = 1,
    RegistroAnulado = 2,
    AntecedentesModificados = 3
}

/// <summary>
/// Bitácora de acceso a la historia clínica (Resolución 1995 de 1999): quién la consultó o modificó,
/// cuándo y desde dónde. Solo se insertan filas; nunca se editan ni se borran.
/// </summary>
public class AuditoriaClinica
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public int? UsuarioId { get; set; }
    public string UsuarioNombre { get; set; } = "";
    public string UsuarioRol { get; set; } = "";
    public AccionAuditoria Accion { get; set; }
    public int PacienteId { get; set; }
    public string Detalle { get; set; } = "";
    public string? Ip { get; set; }
}
