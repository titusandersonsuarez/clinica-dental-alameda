import { Component, inject, signal } from '@angular/core';
import { ApiService } from '../core/api.service';
import { CLINICA } from '../core/clinica.config';
import { CopPipe } from '../core/format';
import { Odontologo, Servicio } from '../core/models';
import { ArcadaComponent } from '../shared/odontograma.component';
import { ReservaComponent } from './reserva.component';

@Component({
  selector: 'app-home',
  imports: [ReservaComponent, ArcadaComponent, CopPipe],
  template: `
  <section class="hero">
    <div class="wrap">
      <div>
        <div class="eyebrow">Odontología general y especializada</div>
        <h1>Agenda tu cita en un minuto y llega <em>sin esperar</em>.</h1>
        <p class="lead">Valoración, limpieza, ortodoncia, endodoncia y estética dental con tres especialistas. Reserva tu horario aquí y te confirmamos por WhatsApp.</p>
        <div class="cta">
          <a class="btn" href="#reservar">Reservar cita</a>
          <a class="btn ghost" href="#servicios">Ver servicios y precios</a>
        </div>
        <div class="facts">
          <div><b>{{ odontologos().length || 3 }}</b><span>especialistas</span></div>
          <div><b>32</b><span>piezas, un plan claro</span></div>
          <div><b>L–S</b><span>atención presencial</span></div>
        </div>
      </div>
      <figure class="chart" style="margin:0">
        <app-arcada />
        <figcaption>Arcada superior con numeración FDI (11–28), el sistema que usamos en tu historia clínica.</figcaption>
      </figure>
    </div>
  </section>

  <section class="blk" id="servicios">
    <div class="wrap">
      <div class="sec-head">
        <div><div class="eyebrow">Servicios</div><h2>Tratamientos y tarifas de referencia</h2></div>
        <p class="muted">Precios en pesos colombianos. El valor final depende de la valoración clínica.</p>
      </div>
      @if (error()) { <p class="note err">{{ error() }}</p> }
      <div class="services">
        @for (s of servicios(); track s.id) {
          <div class="svc">
            <h3>{{ s.nombre }}</h3>
            <p class="muted">{{ s.descripcion }}</p>
            <div class="meta"><span>{{ s.duracionMinutos }} min</span><b>desde {{ s.precioDesde | cop }}</b></div>
          </div>
        }
      </div>
    </div>
  </section>

  <section class="blk band">
    <div class="wrap">
      <div class="sec-head"><div><div class="eyebrow">Equipo</div><h2>Quién te atiende</h2></div></div>
      <div class="team">
        @for (o of odontologos(); track o.id) {
          <div class="doc">
            <div class="avatar">{{ iniciales(o.nombre) }}</div>
            <div><h3>{{ o.nombre }}</h3><p class="muted">{{ o.especialidad }}</p><small>{{ o.registroProfesional }}</small></div>
          </div>
        }
      </div>
    </div>
  </section>

  <section class="blk" id="reservar">
    <div class="wrap">
      <div class="sec-head">
        <div><div class="eyebrow">Citas en línea</div><h2>Reserva tu cita</h2></div>
        <p class="muted">Los horarios tachados ya están ocupados. Recibes la confirmación por WhatsApp.</p>
      </div>
      <app-reserva [servicios]="servicios()" [odontologos]="odontologos()" />
    </div>
  </section>

  <section class="blk" id="contacto" style="border-top:1px solid var(--line)">
    <div class="wrap contact">
      <div>
        <h3>Horario</h3>
        <div class="hours">
          @for (h of clinica.horario; track h.dias) { <div><span>{{ h.dias }}</span><span>{{ h.horas }}</span></div> }
        </div>
      </div>
      <div>
        <h3>WhatsApp</h3>
        <p class="muted">Escríbenos para dudas o urgencias.</p>
        <div class="copyrow"><code>{{ clinica.whatsappVisible }}</code>
          <button class="btn ghost sm" type="button" (click)="copiar()">{{ copiado() ? 'Copiado' : 'Copiar' }}</button></div>
      </div>
      <div><h3>Dirección</h3><p class="muted">{{ clinica.direccion }}<br>{{ clinica.ciudad }}</p></div>
    </div>
  </section>
  <footer><div class="wrap">{{ clinica.nombre }} · © {{ anio }}</div></footer>

  <a class="wa" [href]="'https://wa.me/' + clinica.whatsapp" target="_blank" rel="noopener" aria-label="Escribir por WhatsApp">
    <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M12 2a10 10 0 0 0-8.6 15.1L2 22l5-1.3A10 10 0 1 0 12 2zm0 18.2c-1.5 0-3-.4-4.3-1.2l-.3-.2-3 .8.8-2.9-.2-.3A8.2 8.2 0 1 1 12 20.2zm4.5-6.1c-.2-.1-1.5-.7-1.7-.8s-.4-.1-.6.1-.7.8-.8 1-.3.2-.5.1a6.7 6.7 0 0 1-3.3-2.9c-.3-.4.3-.4.7-1.3.1-.2 0-.3 0-.4l-.8-1.8c-.2-.5-.4-.4-.6-.4h-.5a1 1 0 0 0-.7.3 3 3 0 0 0-.9 2.2 5.2 5.2 0 0 0 1.1 2.7 11.8 11.8 0 0 0 4.5 4c1.7.7 2.3.8 3.2.6.5-.1 1.5-.6 1.7-1.2s.2-1.1.2-1.2-.2-.2-.4-.3z"/></svg>
  </a>`
})
export class HomeComponent {
  private api = inject(ApiService);
  clinica = CLINICA;
  anio = new Date().getFullYear();
  servicios = signal<Servicio[]>([]);
  odontologos = signal<Odontologo[]>([]);
  error = signal('');
  copiado = signal(false);

  constructor() {
    const fallo = () => this.error.set('No pudimos cargar los servicios. Recarga la página o escríbenos por WhatsApp.');
    this.api.servicios().subscribe({ next: s => this.servicios.set(s), error: fallo });
    this.api.odontologos().subscribe({ next: o => this.odontologos.set(o), error: fallo });
  }

  iniciales(n: string) {
    return n.replace(/^(Dra?\.)\s*/, '').split(' ').slice(0, 2).map(p => p[0]).join('');
  }

  async copiar() {
    try { await navigator.clipboard.writeText(this.clinica.whatsappVisible); this.copiado.set(true); setTimeout(() => this.copiado.set(false), 1800); }
    catch { /* el texto sigue visible y seleccionable */ }
  }
}
