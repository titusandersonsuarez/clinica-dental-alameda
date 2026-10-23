using DentalAlameda.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalAlameda.Infrastructure.Persistence.Configurations;

public class ServicioConfig : IEntityTypeConfiguration<Servicio>
{
    public void Configure(EntityTypeBuilder<Servicio> e)
    {
        e.ToTable("servicios");
        e.Property(x => x.Codigo).HasMaxLength(40).IsRequired();
        e.HasIndex(x => x.Codigo).IsUnique();
        e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        e.Property(x => x.Descripcion).HasMaxLength(400);
        e.Property(x => x.PrecioDesde).HasPrecision(12, 0);
    }
}

public class OdontologoConfig : IEntityTypeConfiguration<Odontologo>
{
    public void Configure(EntityTypeBuilder<Odontologo> e)
    {
        e.ToTable("odontologos");
        e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        e.Property(x => x.Especialidad).HasMaxLength(120);
        e.Property(x => x.RegistroProfesional).HasMaxLength(40);
    }
}

public class PacienteConfig : IEntityTypeConfiguration<Paciente>
{
    public void Configure(EntityTypeBuilder<Paciente> e)
    {
        e.ToTable("pacientes");
        e.Property(x => x.Cedula).HasMaxLength(11).IsRequired();
        e.HasIndex(x => x.Cedula).IsUnique();
        e.Property(x => x.Nombre).HasMaxLength(120).IsRequired();
        e.Property(x => x.Celular).HasMaxLength(15).IsRequired();
        e.Property(x => x.Correo).HasMaxLength(160);
        e.Property(x => x.Antecedentes).HasMaxLength(2000);
        e.HasMany(x => x.Historia).WithOne().HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Citas).WithOne(x => x.Paciente).HasForeignKey(x => x.PacienteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CitaConfig : IEntityTypeConfiguration<Cita>
{
    public void Configure(EntityTypeBuilder<Cita> e)
    {
        e.ToTable("citas");
        e.Ignore(x => x.Activa);
        e.Property(x => x.Estado).HasConversion<int>();
        e.Property(x => x.Motivo).HasMaxLength(500);
        e.HasOne(x => x.Odontologo).WithMany().HasForeignKey(x => x.OdontologoId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.Servicio).WithMany().HasForeignKey(x => x.ServicioId).OnDelete(DeleteBehavior.Restrict);
        e.HasIndex(x => new { x.Fecha, x.OdontologoId });
        // Defensa en base de datos contra reservas dobles: un odontólogo no puede tener dos
        // citas activas (pendiente=0, confirmada=1) que empiecen a la misma hora.
        e.HasIndex(x => new { x.OdontologoId, x.Fecha, x.HoraInicio })
            .IsUnique()
            .HasFilter("\"Estado\" IN (0, 1)")
            .HasDatabaseName("ux_citas_odontologo_inicio_activa");
    }
}

public class RegistroClinicoConfig : IEntityTypeConfiguration<RegistroClinico>
{
    public void Configure(EntityTypeBuilder<RegistroClinico> e)
    {
        e.ToTable("registros_clinicos");
        e.Property(x => x.PiezaFdi).HasMaxLength(2);
        e.Property(x => x.Procedimiento).HasMaxLength(200).IsRequired();
        e.Property(x => x.Observaciones).HasMaxLength(2000);
        e.HasOne(x => x.Odontologo).WithMany().HasForeignKey(x => x.OdontologoId).OnDelete(DeleteBehavior.SetNull);
        e.HasIndex(x => new { x.PacienteId, x.Fecha });
    }
}

public class UsuarioConfig : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> e)
    {
        e.ToTable("usuarios");
        e.Property(x => x.Email).HasMaxLength(160).IsRequired();
        e.HasIndex(x => x.Email).IsUnique();
        e.Property(x => x.Nombre).HasMaxLength(120);
        e.Property(x => x.Rol).HasMaxLength(20);
    }
}

public class NotificacionConfig : IEntityTypeConfiguration<Notificacion>
{
    public void Configure(EntityTypeBuilder<Notificacion> e)
    {
        e.ToTable("notificaciones");
        e.Property(x => x.Tipo).HasConversion<int>();
        e.Property(x => x.Estado).HasConversion<int>();
        e.Property(x => x.Telefono).HasMaxLength(16).IsRequired();
        e.Property(x => x.UltimoError).HasMaxLength(500);
        e.Property(x => x.IdProveedor).HasMaxLength(64);
        e.HasOne(x => x.Cita).WithMany(c => c.Notificaciones).HasForeignKey(x => x.CitaId).OnDelete(DeleteBehavior.Cascade);
        // El worker busca pendientes por fecha de próximo intento
        e.HasIndex(x => new { x.Estado, x.ProximoIntentoEn });
    }
}
