# ADR-001: Arquitectura del sistema de citas de Clínica Dental Alameda

**Estado:** Aceptada
**Fecha:** 2026-10-23
**Decisores:** Equipo de desarrollo

## Contexto

La clínica necesita un sitio público donde los pacientes reserven sin crear cuenta, y un panel interno para gestionar citas e historia clínica. Volumen esperado bajo (cientos de citas al mes, 3 odontólogos), pero dos requisitos no negociables: **nunca reservar dos pacientes en el mismo horario** y **proteger los datos clínicos**. El equipo domina C#/.NET, Angular y PostgreSQL.

## Decisión

Monolito modular en ASP.NET Core 8 con Clean Architecture (Domain, Application, Infrastructure, Api), PostgreSQL con EF Core, SPA en Angular servida por nginx, todo en Docker Compose. La reserva doble se previene en dos niveles: transacción `SERIALIZABLE` con chequeo de cruce de horarios y un índice único parcial en la base de datos.

## Opciones consideradas

### Opción A: Monolito modular con Clean Architecture (elegida)
| Dimensión | Evaluación |
|---|---|
| Complejidad | Media |
| Costo | Bajo: un VPS o un plan básico de contenedores |
| Escalabilidad | Suficiente para una o varias sedes |
| Familiaridad | Alta |

**Pros:** reglas de negocio aisladas y probables sin base de datos; un solo despliegue; transacciones locales simples.
**Contras:** más proyectos y archivos que una API mínima.

### Opción B: API mínima en un solo proyecto
| Dimensión | Evaluación |
|---|---|
| Complejidad | Baja |
| Costo | Bajo |
| Escalabilidad | Suficiente |
| Familiaridad | Alta |

**Pros:** arranque rápido.
**Contras:** la lógica de agenda queda mezclada con HTTP y EF; difícil de probar y extender (multi-sede, notificaciones).

### Opción C: Microservicios (agenda, pacientes, auth)
| Dimensión | Evaluación |
|---|---|
| Complejidad | Alta |
| Costo | Alto |
| Escalabilidad | Muy alta, innecesaria aquí |
| Familiaridad | Media |

**Pros:** despliegue independiente.
**Contras:** transacciones distribuidas para algo tan simple como "crear paciente + cita"; sobrecosto operativo.

## Análisis de trade-offs

- **Concurrencia:** `SERIALIZABLE` detecta el cruce entre reservas que se solapan aunque empiecen a distinta hora (p. ej. endodoncia 8:00–9:30 vs. valoración 9:00). El índice único parcial es la red de seguridad si alguien escribe por fuera del servicio. Un conflicto se responde como 409 "Ese horario se acaba de ocupar", no como 500. Se validó con 8 peticiones simultáneas: 1 creada, 7 rechazadas.
- **Fecha y hora como `date` + `time`:** la clínica opera en una sola zona (`America/Bogota`), así que se evita la conversión UTC en cada consulta de agenda. Si se abre una sede en otra zona, migrar a `timestamptz`.
- **Reserva sin cuenta:** menor fricción para el paciente; la consulta de citas exige cédula + celular y tiene rate limiting.

## Consecuencias

- Más fácil: probar reglas (18 pruebas sin base de datos), agregar notificaciones o nuevos estados.
- Más difícil: el paciente no tiene portal completo (historial propio) sin agregar autenticación de pacientes.
- A revisar: festivos, bloqueos de agenda, auditoría de accesos a historia clínica, roles diferenciados.

## Acciones

1. [x] Dominio, casos de uso, API y migración inicial.
2. [x] Frontend Angular: sitio, reserva, panel y odontograma.
3. [x] Notificación por WhatsApp al confirmar y cancelar (Twilio, patrón outbox). Ver WHATSAPP.md.
4. [ ] Calendario de festivos y bloqueos por odontólogo.
5. [ ] Registro de auditoría de lecturas y cambios en historia clínica.
