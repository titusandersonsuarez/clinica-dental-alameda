using DentalAlameda.Domain.Common;

namespace DentalAlameda.Domain.Entities;

public class Paciente
{
    public int Id { get; set; }
    public string Cedula { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Celular { get; set; } = "";
    public string? Correo { get; set; }
    public string Antecedentes { get; set; } = "";
    /// <summary>Autorización expresa para recibir avisos de citas por WhatsApp (Ley 1581 de 2012).</summary>
    public bool AceptaWhatsApp { get; set; }
    /// <summary>Fecha en que aceptó la política de tratamiento de datos (Ley 1581). Obligatoria para reservar.</summary>
    public DateTime? AutorizaDatosEn { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public List<Cita> Citas { get; set; } = [];
    public List<RegistroClinico> Historia { get; set; } = [];

    public void ActualizarContacto(string nombre, string celular, string? correo)
    {
        Nombre = nombre.Trim();
        Celular = celular.Trim();
        if (!string.IsNullOrWhiteSpace(correo)) Correo = correo.Trim();
    }

    public RegistroClinico AgregarRegistro(DateOnly fecha, string? pieza, string procedimiento, string? observaciones, int? odontologoId,
        int? autorId = null, string autorNombre = "")
    {
        if (string.IsNullOrWhiteSpace(procedimiento))
            throw new DomainException("El procedimiento es obligatorio.");
        var registro = new RegistroClinico
        {
            Fecha = fecha,
            PiezaFdi = PiezaDental.Normalizar(pieza),
            Procedimiento = procedimiento.Trim(),
            Observaciones = observaciones?.Trim() ?? "",
            OdontologoId = odontologoId,
            AutorUsuarioId = autorId,
            AutorNombre = autorNombre
        };
        Historia.Add(registro);
        return registro;
    }
}

public class RegistroClinico
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public DateOnly Fecha { get; set; }
    /// <summary>Pieza en notación FDI (11–48). Null si el procedimiento es general.</summary>
    public string? PiezaFdi { get; set; }
    public string Procedimiento { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public int? OdontologoId { get; set; }
    public Odontologo? Odontologo { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    // Quién lo escribió. La historia clínica no se edita: un error se corrige anulando y creando otro registro.
    public int? AutorUsuarioId { get; set; }
    public string AutorNombre { get; set; } = "";
    public bool Anulado { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public string? AnuladoPorNombre { get; private set; }
    public DateTime? AnuladoEn { get; private set; }

    public void Anular(string motivo, string usuario)
    {
        if (Anulado) throw new DomainException("El registro ya está anulado.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5)
            throw new DomainException("Escribe el motivo de la anulación (mínimo 5 caracteres).");
        Anulado = true;
        MotivoAnulacion = motivo.Trim();
        AnuladoPorNombre = usuario;
        AnuladoEn = DateTime.UtcNow;
    }
}

/// <summary>Numeración FDI de dientes permanentes: cuadrante 1–4, pieza 1–8.</summary>
public static class PiezaDental
{
    public static bool EsValida(string pieza) =>
        pieza.Length == 2 && pieza[0] is >= '1' and <= '4' && pieza[1] is >= '1' and <= '8';

    public static string? Normalizar(string? pieza)
    {
        if (string.IsNullOrWhiteSpace(pieza)) return null;
        var p = pieza.Trim();
        if (!EsValida(p))
            throw new DomainException($"'{p}' no es una pieza FDI válida. Usa dos dígitos: cuadrante 1–4 y pieza 1–8 (ej. 36).");
        return p;
    }
}
