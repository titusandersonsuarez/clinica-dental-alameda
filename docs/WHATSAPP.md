# Avisos por WhatsApp

Cuando el panel **confirma** o **cancela** una cita, el sistema envía un WhatsApp al paciente, siempre que este lo haya autorizado al reservar.

## Cómo funciona

```
Panel: Confirmar ──► PATCH /api/admin/citas/{id}/estado
                       │  misma transacción
                       ├─ cita.Estado = Confirmada
                       └─ INSERT notificaciones (Pendiente)      ← patrón outbox
NotificacionesWorker (cada 15 s)
   └─ toma pendientes ─► Twilio /Messages.json ─► Enviada | reintento | Fallida
```

- **Nunca bloquea el panel.** Si Twilio está caído, la cita queda confirmada y el aviso se reintenta a los 1, 5, 15 y 60 minutos (5 intentos en total).
- **Errores definitivos** (número inválido, plantilla rechazada, credenciales malas) no se reintentan: la cita muestra "WhatsApp falló" con el motivo y un botón **Reenviar WhatsApp**.
- **Avisos desactualizados se descartan:** si una cita se confirma y se cancela antes de que salga el primer mensaje, solo se envía el de cancelación.
- **Autorización:** el formulario de reserva trae una casilla, desmarcada por defecto, para cumplir la autorización previa y expresa de la Ley 1581 de 2012. Sin ella no se envía nada.
- Solo se aceptan celulares colombianos (10 dígitos que empiezan por 3); se convierten a `+57…`.

## Proveedores

| `WhatsApp__Proveedor` | Qué hace |
|---|---|
| `Consola` (por defecto) | No envía; escribe el mensaje en el log de la API. Ideal para desarrollo. |
| `Twilio` | Envía por la API de Mensajería de Twilio. |

## Paso 1: probar con el sandbox de Twilio (gratis, sin aprobación)

1. Crea una cuenta en twilio.com y copia **Account SID** y **Auth Token** de la consola.
2. En la consola: *Messaging → Try it out → Send a WhatsApp message*. Desde tu celular envía `join <tu-código>` al **+1 415 523 8886**.
3. En `.env`:
   ```
   WHATSAPP_PROVEEDOR=Twilio
   TWILIO_ACCOUNT_SID=ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   TWILIO_AUTH_TOKEN=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   TWILIO_FROM=+14155238886
   ```
4. `docker compose up --build`, reserva con tu celular marcando la autorización y confirma la cita desde el panel.

Límites del sandbox: solo llega a números que hicieron `join`, la inscripción vence a los 3 días y hay tope de mensajes diarios.

## Paso 2: producción

WhatsApp solo deja que una empresa escriba primero con **plantillas aprobadas por Meta**. El texto libre solo sirve en el sandbox o dentro de las 24 horas siguientes a un mensaje del paciente.

1. Registra un número como remitente de WhatsApp Business en Twilio (*Messaging → Senders → WhatsApp senders*) y vincúlalo a tu cuenta de Meta Business.
2. Crea las dos plantillas en *Messaging → Content Template Builder*, tipo **Text**, idioma **Spanish (es)**, categoría **Utility**:

   **cita_confirmada**
   ```
   Hola {{1}}, tu cita en {{2}} quedó confirmada para el {{3}} a las {{4}} con {{5}}. Dirección: {{6}}. Si no puedes asistir, responde a este mensaje.
   ```

   **cita_cancelada**
   ```
   Hola {{1}}, tu cita en {{2}} del {{3}} a las {{4}} fue cancelada. Para reprogramar, responde a este mensaje o reserva de nuevo en nuestro sitio.
   ```

   Variables que envía el sistema: `{{1}}` nombre, `{{2}}` clínica, `{{3}}` fecha ("lunes 26 de octubre"), `{{4}}` hora ("9:30 a.m."), `{{5}}` odontólogo, `{{6}}` dirección. La plantilla de cancelación solo usa de la 1 a la 4.
3. Cuando Meta las apruebe, copia sus Content SID (`HX…`) al `.env`:
   ```
   TWILIO_FROM=+57XXXXXXXXXX
   TWILIO_PLANTILLA_CONFIRMACION=HXxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   TWILIO_PLANTILLA_CANCELACION=HXxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
   ```
   Con plantilla configurada se envía `ContentSid` + `ContentVariables`; sin ella, texto libre.

## Errores frecuentes de Twilio

| Código | Significado | Qué hacer |
|---|---|---|
| 63003 | El número no tiene WhatsApp o está mal escrito | Verificar el celular del paciente |
| 63016 | Texto libre fuera de la ventana de 24 h | Configurar las plantillas |
| 63018 | Límite de velocidad del remitente | Se reintenta solo |
| 63020 | Meta Business no aceptó la invitación de Twilio | Aceptarla en Meta Business Manager |
| 20003 | Credenciales inválidas | Revisar Account SID y Auth Token |

## Dónde está el código

- Dominio: `Domain/Entities/Notificacion.cs` (estados, reintentos, formato E.164) y `Cita.EncolarAvisoWhatsApp()`
- Casos de uso: `Application/Services/NotificacionesService.cs` (arma el mensaje y procesa la cola)
- Proveedores: `Infrastructure/WhatsApp/Notificadores.cs` (Twilio por HTTP y Consola)
- Proceso en segundo plano: `Api/Workers/NotificacionesWorker.cs`
- Pruebas: `tests/DentalAlameda.Tests/WhatsAppTests.cs`
