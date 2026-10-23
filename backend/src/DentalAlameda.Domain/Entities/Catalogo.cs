namespace DentalAlameda.Domain.Entities;

public class Servicio
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int DuracionMinutos { get; set; }
    public decimal PrecioDesde { get; set; }
    public bool Activo { get; set; } = true;
}

public class Odontologo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string RegistroProfesional { get; set; } = "";
    public bool Activo { get; set; } = true;
}

public class Usuario
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Rol { get; set; } = Roles.Admin;
    public bool Activo { get; set; } = true;
    /// <summary>True al crear el usuario o al restablecer su contraseña: debe cambiarla antes de usar el panel.</summary>
    public bool DebeCambiarPassword { get; set; } = true;
    /// <summary>Para usuarios con rol Odontólogo: a qué profesional de la agenda corresponde.</summary>
    public int? OdontologoId { get; set; }
    public Odontologo? Odontologo { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoIngresoEn { get; set; }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Recepcion = "Recepcion";
    public const string Odontologo = "Odontologo";
    public static readonly string[] Todos = [Admin, Recepcion, Odontologo];

    /// <summary>Quién puede leer y escribir historia clínica.</summary>
    public const string Clinicos = Admin + "," + Odontologo;
}

/// <summary>Reglas mínimas de contraseña para el personal.</summary>
public static class PoliticaPassword
{
    public const int LongitudMinima = 10;

    public static string? Validar(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < LongitudMinima)
            return $"La contraseña debe tener al menos {LongitudMinima} caracteres.";
        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return "La contraseña debe combinar letras y números.";
        return null;
    }
}
