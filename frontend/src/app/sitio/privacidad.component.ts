import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CLINICA } from '../core/clinica.config';

/**
 * Plantilla de política de tratamiento de datos (Ley 1581 de 2012 y Decreto 1377 de 2013).
 * Los campos resaltados se completan en clinica.config.ts. Debe revisarla un abogado antes de publicar.
 */
@Component({
  selector: 'app-privacidad',
  imports: [RouterLink],
  template: `
  <article class="wrap legal">
    <div class="eyebrow">Ley 1581 de 2012</div>
    <h1>Política de tratamiento de datos personales</h1>
    <p class="muted">Vigente desde {{ c.politicaVigenteDesde }}.</p>

    <h2>1. Responsable</h2>
    <p>
      {{ c.razonSocial }}, identificada con NIT <span [class.pendiente]="!c.nit">{{ c.nit || 'NIT por definir' }}</span>,
      con domicilio en {{ c.direccion }}, {{ c.ciudad }}. Correo para asuntos de datos:
      <span [class.pendiente]="!c.correoDatos">{{ c.correoDatos || 'correo por definir' }}</span>. Teléfono: {{ c.whatsappVisible }}.
    </p>

    <h2>2. Datos que tratamos</h2>
    <ul>
      <li>Identificación y contacto: nombre, cédula, celular y correo.</li>
      <li>Datos de salud oral: motivo de consulta, antecedentes, alergias, procedimientos y evolución registrados en la historia clínica.
        Estos son <b>datos sensibles</b>: no está obligado a suministrarlos, pero sin ellos no es posible prestar el servicio odontológico.</li>
      <li>Datos de agenda: citas, estados y mensajes de aviso enviados.</li>
    </ul>

    <h2>3. Finalidades</h2>
    <ul>
      <li>Agendar, confirmar, reprogramar y cancelar citas.</li>
      <li>Prestar la atención odontológica y llevar la historia clínica exigida por la Resolución 1995 de 1999.</li>
      <li>Enviar avisos sobre sus citas por WhatsApp, solo si lo autorizó de forma separada.</li>
      <li>Cumplir obligaciones legales ante autoridades de salud.</li>
    </ul>
    <p>No vendemos ni cedemos sus datos. Los proveedores tecnológicos que los procesan por cuenta nuestra (alojamiento y mensajería) están obligados a protegerlos.</p>

    <h2>4. Sus derechos</h2>
    <ul>
      <li>Conocer, actualizar y rectificar sus datos.</li>
      <li>Solicitar prueba de la autorización que nos dio.</li>
      <li>Ser informado sobre el uso que les damos.</li>
      <li>Revocar la autorización o pedir la supresión cuando no exista un deber legal de conservarlos.</li>
      <li>Presentar quejas ante la Superintendencia de Industria y Comercio.</li>
    </ul>

    <h2>5. Cómo ejercerlos</h2>
    <p>
      Escriba a <span [class.pendiente]="!c.correoDatos">{{ c.correoDatos || 'correo por definir' }}</span> indicando su nombre, cédula y la solicitud.
      Las consultas se responden en máximo 10 días hábiles y los reclamos en máximo 15 días hábiles.
    </p>

    <h2>6. Conservación</h2>
    <p>
      La historia clínica se conserva por el tiempo que exige la normativa de salud (Resolución 839 de 2017), por lo que no puede
      suprimirse antes de ese plazo. Los demás datos se conservan mientras exista relación con el paciente.
    </p>

    <h2>7. Seguridad</h2>
    <p>
      El acceso a la historia clínica está limitado al personal autorizado, con usuario y contraseña personal, y cada consulta o cambio
      queda registrado con fecha, usuario y origen.
    </p>

    <p style="margin-top:2rem"><a routerLink="/" fragment="reservar" class="btn ghost">Volver a reservar</a></p>
  </article>`
})
export class PrivacidadComponent {
  c = CLINICA;
}
