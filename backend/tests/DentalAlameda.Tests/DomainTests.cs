using DentalAlameda.Domain;
using DentalAlameda.Domain.Common;
using DentalAlameda.Domain.Entities;

namespace DentalAlameda.Tests;

public class DomainTests
{
    [Theory]
    [InlineData("11", true)][InlineData("48", true)][InlineData("36", true)]
    [InlineData("19", false)][InlineData("51", false)][InlineData("5", false)][InlineData("abc", false)]
    public void PiezaFdi_valida_cuadrante_y_pieza(string pieza, bool esperado) =>
        Assert.Equal(esperado, PiezaDental.EsValida(pieza));

    [Fact]
    public void Registro_con_pieza_invalida_lanza_error()
    {
        var p = new Paciente();
        Assert.Throws<DomainException>(() => p.AgregarRegistro(new DateOnly(2026, 10, 1), "60", "Resina", null, null));
    }

    [Fact]
    public void Cita_sigue_flujo_pendiente_confirmada_atendida()
    {
        var c = new Cita();
        c.CambiarEstado(EstadoCita.Confirmada);
        c.CambiarEstado(EstadoCita.Atendida);
        Assert.Equal(EstadoCita.Atendida, c.Estado);
    }

    [Fact]
    public void Cita_atendida_no_se_puede_cancelar()
    {
        var c = new Cita();
        c.CambiarEstado(EstadoCita.Confirmada);
        c.CambiarEstado(EstadoCita.Atendida);
        Assert.Throws<DomainException>(() => c.CambiarEstado(EstadoCita.Cancelada));
    }

    [Fact]
    public void Horario_domingo_cerrado_y_sabado_solo_manana()
    {
        Assert.Empty(HorarioClinica.InicioPosibles(new DateOnly(2026, 10, 25), 30)); // domingo
        var sabado = HorarioClinica.InicioPosibles(new DateOnly(2026, 10, 24), 30).ToList();
        Assert.Equal(new TimeOnly(8, 0), sabado.First());
        Assert.Equal(new TimeOnly(11, 30), sabado.Last());
    }

    [Fact]
    public void Servicio_de_90_min_no_puede_cruzar_el_almuerzo()
    {
        var lunes = new DateOnly(2026, 10, 26);
        var inicios = HorarioClinica.InicioPosibles(lunes, 90).ToList();
        Assert.Contains(new TimeOnly(10, 30), inicios);
        Assert.DoesNotContain(new TimeOnly(11, 0), inicios);
        Assert.DoesNotContain(new TimeOnly(17, 0), inicios);
    }
}
