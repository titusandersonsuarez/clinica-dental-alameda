import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService, mensajeError } from '../core/api.service';
import { FechaCortaPipe, isoFecha } from '../core/format';
import { CitaAdmin, EstadoCita, Odontologo } from '../core/models';

@Component({
  selector: 'app-citas',
  imports: [FormsModule, FechaCortaPipe],
  template: `
  <div class="toolbar">
    <select id="f-estado" [(ngModel)]="f.estado" (ngModelChange)="cargar()" aria-label="Filtrar por estado">
      <option value="">Todos los estados</option>
      @for (e of estados; track e) { <option [value]="e">{{ e }}</option> }
    </select>
    <select id="f-odonto" [(ngModel)]="f.odontologoId" (ngModelChange)="cargar()" aria-label="Filtrar por odontólogo">
      <option [ngValue]="null">Todos los odontólogos</option>
      @for (o of odontologos(); track o.id) { <option [ngValue]="o.id">{{ o.nombre }}</option> }
    </select>
    <input id="f-desde" type="date" [(ngModel)]="f.desde" (ngModelChange)="cargar()" aria-label="Desde">
    <input id="f-q" [(ngModel)]="f.q" (ngModelChange)="buscar()" placeholder="Buscar paciente o cédula" aria-label="Buscar">
  </div>
  @if (error()) { <p class="note err" style="margin-bottom:1rem">{{ error() }}</p> }
  <div class="tbl-wrap">
    <table>
      <thead><tr><th>Fecha y hora</th><th>Paciente</th><th>Servicio</th><th>Odontólogo</th><th>Estado</th><th>Acciones</th></tr></thead>
      <tbody>
        @for (c of citas(); track c.id) {
          <tr>
            <td class="num">{{ c.fecha | fechaCorta }}<br><span class="muted">{{ c.horaInicio }}–{{ c.horaFin }}</span></td>
            <td>{{ c.pacienteNombre }}<br><span class="muted num">CC {{ c.cedula }} · {{ c.celular }}</span>
              @if (c.motivo) { <br><span class="muted" style="font-size:.82rem">{{ c.motivo }}</span> }</td>
            <td>{{ c.servicio }}</td>
            <td>{{ c.odontologo }}</td>
            <td><span class="pill" [class]="c.estado.toLowerCase()">{{ c.estado }}</span></td>
            <td><div class="acts">
              @if (c.estado === 'Pendiente') { <button class="btn sm" (click)="cambiar(c, 'Confirmada')" [disabled]="ocupado() === c.id">Confirmar</button> }
              @if (c.estado === 'Confirmada') { <button class="btn sm" (click)="cambiar(c, 'Atendida')" [disabled]="ocupado() === c.id">Marcar atendida</button> }
              @if (c.estado === 'Pendiente' || c.estado === 'Confirmada') {
                @if (confirmarCancelar() === c.id) {
                  <button class="btn sm" style="background:var(--bad)" (click)="cambiar(c, 'Cancelada')">Sí, cancelar</button>
                  <button class="btn ghost sm" (click)="confirmarCancelar.set(null)">No</button>
                } @else {
                  <button class="btn ghost sm" (click)="confirmarCancelar.set(c.id)">Cancelar</button>
                }
              }
              <button class="btn ghost sm" (click)="historia(c)">Historia</button>
            </div></td>
          </tr>
        } @empty {
          <tr><td colspan="6"><div class="empty">{{ cargando() ? 'Cargando citas…' : 'No hay citas con estos filtros.' }}</div></td></tr>
        }
      </tbody>
    </table>
  </div>`
})
export class CitasComponent {
  private api = inject(ApiService);
  private router = inject(Router);
  estados: EstadoCita[] = ['Pendiente', 'Confirmada', 'Atendida', 'Cancelada'];
  f = { estado: '', odontologoId: null as number | null, desde: isoFecha(new Date()), q: '' };
  citas = signal<CitaAdmin[]>([]);
  odontologos = signal<Odontologo[]>([]);
  cargando = signal(true);
  ocupado = signal<number | null>(null);
  confirmarCancelar = signal<number | null>(null);
  error = signal('');
  private timer?: ReturnType<typeof setTimeout>;

  constructor() {
    this.api.odontologos().subscribe(o => this.odontologos.set(o));
    this.cargar();
  }

  cargar() {
    this.cargando.set(true);
    this.api.citas({ ...this.f }).subscribe({
      next: c => { this.citas.set(c); this.cargando.set(false); this.error.set(''); },
      error: e => { this.cargando.set(false); this.error.set(mensajeError(e)); }
    });
  }

  buscar() { clearTimeout(this.timer); this.timer = setTimeout(() => this.cargar(), 300); }

  cambiar(c: CitaAdmin, estado: EstadoCita) {
    this.ocupado.set(c.id);
    this.confirmarCancelar.set(null);
    this.api.cambiarEstado(c.id, estado).subscribe({
      next: () => { this.ocupado.set(null); this.citas.update(l => l.map(x => x.id === c.id ? { ...x, estado } : x)); },
      error: e => { this.ocupado.set(null); this.error.set(mensajeError(e)); }
    });
  }

  historia(c: CitaAdmin) { this.router.navigate(['/admin/pacientes', c.pacienteId]); }
}
