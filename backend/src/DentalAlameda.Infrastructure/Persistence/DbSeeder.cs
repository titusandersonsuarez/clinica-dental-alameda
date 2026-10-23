using DentalAlameda.Application.Abstractions;
using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentalAlameda.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, IConfiguration config, ILogger logger)
    {
        if (!await db.Servicios.AnyAsync())
        {
            db.Servicios.AddRange(
                new Servicio { Codigo = "valoracion", Nombre = "Valoración y diagnóstico", Descripcion = "Examen clínico, odontograma y plan de tratamiento.", DuracionMinutos = 30, PrecioDesde = 60000 },
                new Servicio { Codigo = "profilaxis", Nombre = "Limpieza (profilaxis)", Descripcion = "Remoción de placa y cálculo, pulido y flúor.", DuracionMinutos = 45, PrecioDesde = 120000 },
                new Servicio { Codigo = "resina", Nombre = "Resina por superficie", Descripcion = "Restauración estética del color del diente.", DuracionMinutos = 45, PrecioDesde = 150000 },
                new Servicio { Codigo = "endodoncia", Nombre = "Endodoncia", Descripcion = "Tratamiento de conducto en uni o multirradicular.", DuracionMinutos = 90, PrecioDesde = 450000 },
                new Servicio { Codigo = "ortodoncia", Nombre = "Control de ortodoncia", Descripcion = "Ajuste mensual de brackets o alineadores.", DuracionMinutos = 30, PrecioDesde = 110000 },
                new Servicio { Codigo = "blanqueamiento", Nombre = "Blanqueamiento", Descripcion = "Aclarado en consultorio con protección gingival.", DuracionMinutos = 60, PrecioDesde = 550000 });
        }

        if (!await db.Odontologos.AnyAsync())
        {
            db.Odontologos.AddRange(
                new Odontologo { Nombre = "Dra. Laura Rojas", Especialidad = "Odontología general y estética", RegistroProfesional = "RD 0000-01" },
                new Odontologo { Nombre = "Dr. Camilo Pineda", Especialidad = "Endodoncia", RegistroProfesional = "RD 0000-02" },
                new Odontologo { Nombre = "Dra. Sofía Mejía", Especialidad = "Ortodoncia", RegistroProfesional = "RD 0000-03" });
        }

        var email = config["Seed:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = config["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && !await db.Usuarios.AnyAsync())
        {
            db.Usuarios.Add(new Usuario { Email = email, Nombre = "Administración", PasswordHash = hasher.Hash(password), Rol = Roles.Admin });
            logger.LogInformation("Usuario administrador creado: {Email}", email);
        }

        await db.SaveChangesAsync();
    }
}
