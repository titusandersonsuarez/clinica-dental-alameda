import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, mensajeError } from '../core/api.service';
import { CopPipe, FechaCortaPipe, fechaCorta, proximosDias } from '../core/format';
import { CitaPaciente, Horario, Odontologo, Servicio } from '../core/models';

type Aviso = { texto: string; tipo: '' | 'ok' | 'err' } | null;

@Component({
  selector: 'app-reserva',
  imports: [FormsModule, CopPipe, FechaCortaPipe],
  template: `
  <div class="booking">
    <form class="panel" (ngSubmit)="reservar()" novalidate id="form-reserva">
      <div class="step">
        <div class="step-title"><span>01</span>Servicio</div>
        <div class="chips">
          @for (s of servicios(); track s.id) {
            <button type="button" class="chip" [attr.aria-pressed]="servicioId() === s.id" (click)="servicioId.set(s.id)">
              {{ s.nombre }}<small>{{ s.duracionMinutos }} min · {{ s.precioDesde | cop }}</small>
            </button>
          }
        </div>
      </div>
      <div class="step">
        <div class="step-title"><span>02</span>Odontólogo</div>
        <div class="chips">
          @for (o of odontologos(); track o.id) {
            <button type="button" class="chip" [attr.aria-pressed]="odontologoId() === o.id" (click)="odontologoId.set(o.id)">
              {{ o.nombre }}<small>{{ o.especialidad }}</small>
            </button>
          }
        </div>
      </div>
      <div class="step">
        <div class="step-title"><span>03</span>Fecha</div>
        <div class="chips">
          @for (d of dias; track d) {
            <button type="button" class="chip" [attr.aria-pressed]="fecha() === d" (click)="fecha.set(d)">{{ d | fechaCorta }}</button>
          }
        </div>
      </div>
      <div class="step">
        <div class="step-title"><span>04</span>Hora</div>
        @if (cargandoHorarios()) { <p class="loading">Consultando horarios…</p> }
        @else if (horarios().length === 0) { <p class="muted">No hay atención este día. Elige otra fecha.</p> }
        @else if (!hayLibres()) { <p class="muted">No quedan horarios libres este día con este odontólogo.</p> }
        <div class="chips">
          @for (h of horarios(); track h.hora) {
            <button type="button" class="chip" [disabled]="!h.disponible" [attr.aria-pressed]="hora() === h.hora" (click)="hora.set(h.hora)">{{ h.hora }}</button>
          }
        </div>
      </div>
      <div class="step">
        <div class="step-title"><span>05</span>Tus datos</div>
        <div class="grid2">
          <label class="f">Nombre completo<input id="r-nombre" name="nombre" [(ngModel)]="datos.nombre" autocomplete="name" required></label>
          <label class="f">Cédula<input id="r-cedula" name="cedula" [(ngModel)]="datos.cedula" inputmode="numeric" required></label>
          <label class="f">Celular (WhatsApp)<input id="r-celular" name="celular" [(ngModel)]="datos.celular" inputmode="tel" autocomplete="tel" required></label>
          <label class="f">Correo (opcional)<input id="r-correo" name="correo" [(ngModel)]="datos.correo" type="email" autocomplete="email"></label>
        </div>
        <label class="f">Motivo de consulta
          <textarea id="r-motivo" name="motivo" [(ngModel)]="datos.motivo" placeholder="Ej.: sensibilidad en muela inferior derecha"></textarea>
        </label>
        <label class="check">
          <input id="r-wa" name="aceptaWhatsApp" type="checkbox" [(ngModel)]="datos.aceptaWhatsApp">
          <span>Autorizo a la clínica a enviarme la confirmación y los cambios de mi cita por WhatsApp a este celular.</span>
        </label>
      </div>
    </form>

    <aside class="panel summary">
      <h3>Resumen</h3>
      <dl>
        <dt>Servicio</dt><dd>{{ servicio()?.nombre ?? '—' }}</dd>
        <dt>Odontólogo</dt><dd>{{ odontologo()?.nombre ?? '—' }}</dd>
        <dt>Fecha</dt><dd>{{ fecha() | fechaCorta }}</dd>
        <dt>Hora</dt><dd>{{ hora() ?? '—' }}</dd>
        @if (servicio(); as s) {
          <dt>Duración</dt><dd>{{ s.duracionMinutos }} min</dd>
          <dt>Valor desde</dt><dd>{{ s.precioDesde | cop }}</dd>
        }
      </dl>
      <button class="btn" style="width:100%;justify-content:center" type="submit" form="form-reserva" [disabled]="enviando()">
        {{ enviando() ? 'Reservando…' : 'Confirmar reserva' }}
      </button>
      @if (aviso(); as a) { <p class="note" [class.ok]="a.tipo === 'ok'" [class.err]="a.tipo === 'err'" style="margin-top:.75rem" role="status">{{ a.texto }}</p> }

      <form class="consulta" (ngSubmit)="consultar()">
        <div class="eyebrow">Consulta tus citas</div>
        <div class="grid2">
          <label class="f">Cédula<input id="c-cedula" name="cCedula" [(ngModel)]="consulta.cedula" inputmode="numeric"></label>
          <label class="f">Celular<input id="c-celular" name="cCelular" [(ngModel)]="consulta.celular" inputmode="tel"></label>
        </div>
        <button class="btn ghost sm" type="submit">Ver mis citas</button>
        @if (misCitas(); as lista) {
          @for (c of lista; track c.id) {
            <div class="mine-item">
              <span>{{ c.fecha | fechaCorta }} · {{ c.hora }}<br><span class="muted">{{ c.servicio }}</span></span>
              <span class="pill" [class]="c.estado.toLowerCase()">{{ c.estado }}</span>
            </div>
          } @empty { <p class="muted" style="font-size:.85rem">No encontramos citas próximas con esos datos.</p> }
        }
      </form>
    </aside>
  </div>`
})
export class ReservaComponent {
  private api = inject(ApiService);
  servicios = input<Servicio[]>([]);
  odontologos = input<Odontologo[]>([]);

  dias = proximosDias(12);
  servicioId = signal<number | null>(null);
  odontologoId = signal<number | null>(null);
  fecha = signal(this.dias[0]);
  hora = signal<string | null>(null);
  horarios = signal<Horario[]>([]);
  cargandoHorarios = signal(false);
  enviando = signal(false);
  aviso = signal<Aviso>(null);
  misCitas = signal<CitaPaciente[] | null>(null);

  datos = { nombre: '', cedula: '', celular: '', correo: '', motivo: '', aceptaWhatsApp: false };
  consulta = { cedula: '', celular: '' };

  servicio = computed(() => this.servicios().find(s => s.id === this.servicioId()));
  odontologo = computed(() => this.odontologos().find(o => o.id === this.odontologoId()));
  hayLibres = computed(() => this.horarios().some(h => h.disponible));

  constructor() {
    // Selección inicial cuando llega el catálogo
    effect(() => {
      if (this.servicioId() === null && this.servicios().length) this.servicioId.set(this.servicios()[0].id);
      if (this.odontologoId() === null && this.odontologos().length) this.odontologoId.set(this.odontologos()[0].id);
    });
    // Recarga horarios al cambiar servicio, odontólogo o fecha
    effect(() => {
      const s = this.servicioId(), o = this.odontologoId(), f = this.fecha();
      if (s === null || o === null) return;
      this.cargarHorarios(o, s, f);
    });
  }

  private cargarHorarios(o: number, s: number, f: string) {
    this.hora.set(null);
    this.cargandoHorarios.set(true);
    this.api.disponibilidad(o, s, f).subscribe({
      next: d => { this.horarios.set(d.horarios); this.cargandoHorarios.set(false); },
      error: e => { this.horarios.set([]); this.cargandoHorarios.set(false); this.aviso.set({ texto: mensajeError(e), tipo: 'err' }); }
    });
  }

  reservar() {
    const d = this.datos;
    if (!this.hora()) return this.aviso.set({ texto: 'Elige una hora disponible.', tipo: 'err' });
    if (!d.nombre.trim() || !d.cedula.trim() || !d.celular.trim())
      return this.aviso.set({ texto: 'Completa nombre, cédula y celular para reservar.', tipo: 'err' });

    this.enviando.set(true);
    this.aviso.set(null);
    this.api.reservar({
      servicioId: this.servicioId()!, odontologoId: this.odontologoId()!, fecha: this.fecha(), hora: this.hora()!,
      nombre: d.nombre.trim(), cedula: d.cedula.trim(), celular: d.celular.trim(),
      correo: d.correo.trim() || undefined, motivo: d.motivo.trim() || undefined, aceptaWhatsApp: d.aceptaWhatsApp
    }).subscribe({
      next: c => {
        this.enviando.set(false);
        this.aviso.set({ texto: `Listo: ${fechaCorta(c.fecha)} a las ${c.hora} con ${c.odontologo}. ` + (d.aceptaWhatsApp ? 'Te avisamos por WhatsApp cuando la confirmemos.' : 'Consulta el estado aquí con tu cédula y celular.'), tipo: 'ok' });
        this.datos.motivo = '';
        this.consulta = { cedula: d.cedula, celular: d.celular };
        this.cargarHorarios(this.odontologoId()!, this.servicioId()!, this.fecha());
      },
      error: e => {
        this.enviando.set(false);
        this.aviso.set({ texto: mensajeError(e), tipo: 'err' });
        this.cargarHorarios(this.odontologoId()!, this.servicioId()!, this.fecha());
      }
    });
  }

  consultar() {
    const { cedula, celular } = this.consulta;
    if (!cedula.trim() || !celular.trim()) { this.misCitas.set([]); return; }
    this.api.misCitas(cedula.trim(), celular.trim()).subscribe({
      next: l => this.misCitas.set(l),
      error: e => this.aviso.set({ texto: mensajeError(e), tipo: 'err' })
    });
  }
}
