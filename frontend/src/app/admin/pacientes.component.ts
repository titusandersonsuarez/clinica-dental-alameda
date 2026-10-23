import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, mensajeError } from '../core/api.service';
import { FechaCortaPipe, isoFecha } from '../core/format';
import { Odontologo, PacienteDetalle, PacienteLista } from '../core/models';
import { OdontogramaComponent } from '../shared/odontograma.component';

@Component({
  selector: 'app-pacientes',
  imports: [FormsModule, FechaCortaPipe, OdontogramaComponent, DatePipe],
  template: `
  <div class="pac">
    <div>
      <div class="toolbar"><input id="p-q" [(ngModel)]="q" (ngModelChange)="buscar()" placeholder="Buscar paciente o cédula" aria-label="Buscar paciente" style="flex:1"></div>
      <div class="plist">
        @for (p of lista(); track p.id) {
          <button type="button" [attr.aria-current]="p.id === seleccion()" (click)="abrir(p.id)">
            {{ p.nombre }}<small>CC {{ p.cedula }} · {{ p.totalCitas }} cita{{ p.totalCitas === 1 ? '' : 's' }}</small>
          </button>
        } @empty { <div class="empty" style="border:0">Aún no hay pacientes. Se crean automáticamente con cada reserva.</div> }
      </div>
    </div>

    <div class="panel">
      @if (detalle(); as p) {
        <div style="display:flex;justify-content:space-between;gap:1rem;flex-wrap:wrap">
          <div><h3>{{ p.nombre }}</h3><p class="muted num">CC {{ p.cedula }} · {{ p.celular }}{{ p.correo ? ' · ' + p.correo : '' }}</p></div>
          <p class="muted" style="font-size:.85rem">{{ p.citas.length }} cita{{ p.citas.length === 1 ? '' : 's' }} registradas</p>
        </div>

        <label class="f" style="margin-top:1rem">Antecedentes y alergias
          <textarea id="p-ant" [(ngModel)]="antecedentes"></textarea></label>
        <div style="margin-top:.5rem;display:flex;gap:.75rem;align-items:center">
          <button class="btn ghost sm" type="button" (click)="guardarAntecedentes()">Guardar antecedentes</button>
          <span class="muted" style="font-size:.85rem">{{ msgAnt() }}</span>
        </div>

        <app-odontograma [piezas]="piezas()" />

        <form class="grid2" style="grid-template-columns:140px 110px 1fr;align-items:end" (ngSubmit)="agregar()">
          <label class="f">Fecha<input id="h-fecha" name="fecha" type="date" [(ngModel)]="nuevo.fecha" required></label>
          <label class="f">Pieza (FDI)<input id="h-pieza" name="pieza" [(ngModel)]="nuevo.piezaFdi" placeholder="Ej. 36" inputmode="numeric" maxlength="2"></label>
          <label class="f">Procedimiento<input id="h-proc" name="proc" [(ngModel)]="nuevo.procedimiento" placeholder="Ej. Resina oclusal" required></label>
          <label class="f">Odontólogo
            <select id="h-odonto" name="odonto" [(ngModel)]="nuevo.odontologoId">
              <option [ngValue]="null">—</option>
              @for (o of odontologos(); track o.id) { <option [ngValue]="o.id">{{ o.nombre }}</option> }
            </select></label>
          <label class="f" style="grid-column:2/-1">Observaciones<input id="h-obs" name="obs" [(ngModel)]="nuevo.observaciones"></label>
          <div style="grid-column:1/-1;display:flex;gap:.75rem;align-items:center">
            <button class="btn sm" type="submit">Agregar a la historia</button>
            <span class="muted" style="font-size:.85rem">{{ msgHist() }}</span>
          </div>
        </form>

        <div class="timeline" style="margin-top:1.25rem">
          @for (h of p.historia; track h.id) {
            <div class="tl" [class.anulado]="h.anulado">
              <span class="num">{{ h.fecha }}</span>
              @if (h.piezaFdi) { <span class="tooth">{{ h.piezaFdi }}</span> } @else { <span></span> }
              <div>
                <b class="proc">{{ h.procedimiento }}</b>
                @if (h.observaciones) { <br><span class="muted">{{ h.observaciones }}</span> }
                <div class="meta-reg">
                  {{ h.odontologo ?? 'Sin odontólogo' }} · escrito por {{ h.autor || 'sistema' }} el {{ h.creadoEn | date:'d MMM y, h:mm a' }}
                </div>
                @if (h.anulado) {
                  <div class="anulacion">Anulado por {{ h.anuladoPor }} el {{ h.anuladoEn | date:'d MMM y, h:mm a' }}. Motivo: {{ h.motivoAnulacion }}</div>
                } @else if (anulando() === h.id) {
                  <form class="anular-form" (ngSubmit)="anular(h.id)">
                    <input id="h-motivo" name="motivo" [(ngModel)]="motivo" placeholder="Motivo de la anulación (ej. se registró en el paciente equivocado)" aria-label="Motivo de la anulación">
                    <button class="btn sm" style="background:var(--bad)" type="submit">Anular registro</button>
                    <button class="btn ghost sm" type="button" (click)="anulando.set(null)">Cancelar</button>
                  </form>
                } @else {
                  <button class="link-btn" type="button" (click)="anulando.set(h.id); motivo = ''">Anular</button>
                }
              </div>
            </div>
          } @empty { <div class="empty">Sin registros todavía. Agrega el primer procedimiento arriba.</div> }
        </div>
        <p class="muted" style="font-size:.8rem;margin-top:.75rem">Los registros de la historia clínica no se editan ni se borran: si hay un error, anúlalo con el motivo y agrega uno nuevo.</p>

        @if (p.citas.length) {
          <h3 style="margin-top:1.5rem">Citas</h3>
          @for (c of p.citas; track c.id) {
            <div class="mine-item"><span>{{ c.fecha | fechaCorta }} · {{ c.hora }} · {{ c.servicio }}<br><span class="muted">{{ c.odontologo }}</span></span>
              <span class="pill" [class]="c.estado.toLowerCase()">{{ c.estado }}</span></div>
          }
        }
      } @else {
        <div class="empty" style="border:0">Selecciona un paciente para ver su historia clínica.</div>
      }
    </div>
  </div>`
})
export class PacientesComponent {
  private api = inject(ApiService);
  private router = inject(Router);
  /** Id desde la ruta /admin/pacientes/:id (withComponentInputBinding). */
  id = input<string>();

  q = '';
  lista = signal<PacienteLista[]>([]);
  seleccion = signal<number | null>(null);
  detalle = signal<PacienteDetalle | null>(null);
  odontologos = signal<Odontologo[]>([]);
  antecedentes = '';
  msgAnt = signal('');
  msgHist = signal('');
  nuevo = this.vacio();
  piezas = computed(() => this.detalle()?.historia.filter(h => !h.anulado).map(h => h.piezaFdi) ?? []);
  anulando = signal<number | null>(null);
  motivo = '';
  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.api.odontologos().subscribe(o => this.odontologos.set(o));
    this.cargarLista();
    effect(() => {
      const id = Number(this.id());
      if (id) this.cargarDetalle(id);
    });
  }

  private vacio() { return { fecha: isoFecha(new Date()), piezaFdi: '', procedimiento: '', observaciones: '', odontologoId: null as number | null }; }

  cargarLista() {
    this.api.pacientes(this.q.trim()).subscribe(l => {
      this.lista.set(l);
      if (!this.id() && !this.seleccion() && l.length) this.abrir(l[0].id);
    });
  }
  buscar() { clearTimeout(this.timer); this.timer = setTimeout(() => this.cargarLista(), 300); }

  abrir(id: number) { this.router.navigate(['/admin/pacientes', id]); }

  cargarDetalle(id: number) {
    this.seleccion.set(id);
    this.msgAnt.set(''); this.msgHist.set('');
    this.api.paciente(id).subscribe({
      next: p => { this.detalle.set(p); this.antecedentes = p.antecedentes; },
      error: e => { this.detalle.set(null); this.msgHist.set(mensajeError(e)); }
    });
  }

  anular(registroId: number) {
    const p = this.detalle(); if (!p) return;
    if (this.motivo.trim().length < 5) { this.msgHist.set('Escribe el motivo de la anulación (mínimo 5 caracteres).'); return; }
    this.api.anularRegistro(p.id, registroId, this.motivo.trim()).subscribe({
      next: r => { this.anulando.set(null); this.detalle.update(d => d && { ...d, historia: d.historia.map(h => h.id === r.id ? r : h) }); this.msgHist.set('Registro anulado.'); },
      error: e => this.msgHist.set(mensajeError(e))
    });
  }

  guardarAntecedentes() {
    const p = this.detalle(); if (!p) return;
    this.api.guardarAntecedentes(p.id, this.antecedentes).subscribe({
      next: () => this.msgAnt.set('Antecedentes guardados.'),
      error: e => this.msgAnt.set(mensajeError(e))
    });
  }

  agregar() {
    const p = this.detalle(); if (!p) return;
    const n = this.nuevo;
    if (n.piezaFdi && !/^[1-4][1-8]$/.test(n.piezaFdi)) { this.msgHist.set('Usa numeración FDI: cuadrante 1–4 y pieza 1–8 (ej. 36).'); return; }
    if (!n.procedimiento.trim()) { this.msgHist.set('Escribe el procedimiento.'); return; }
    this.api.agregarRegistro(p.id, {
      fecha: n.fecha, piezaFdi: n.piezaFdi || undefined, procedimiento: n.procedimiento.trim(),
      observaciones: n.observaciones.trim() || undefined, odontologoId: n.odontologoId ?? undefined
    }).subscribe({
      next: r => {
        this.detalle.update(d => d && { ...d, historia: [r, ...d.historia].sort((a, b) => b.fecha.localeCompare(a.fecha)) });
        this.nuevo = this.vacio();
        this.msgHist.set('Registro agregado.');
      },
      error: e => this.msgHist.set(mensajeError(e))
    });
  }
}
