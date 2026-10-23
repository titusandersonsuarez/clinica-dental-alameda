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
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Recepcion = "Recepcion";
}
