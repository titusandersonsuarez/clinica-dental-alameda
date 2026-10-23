import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, mensajeError } from '../core/api.service';
import { isoFecha } from '../core/format';
import { AccionAuditoria, Auditoria } from '../core/models';

const ACCIONES: Record<AccionAuditoria, string> = {
  ConsultaHistoria: 'Consultó historia',
  RegistroCreado: 'Agregó registro',
  RegistroAnulado: 'Anuló registro',
  AntecedentesModificados: 'Modificó antecedentes'
};

@Component({
  selector: 'app-auditoria',
  imports: [FormsModule, DatePipe, RouterLink],
  template: `
  <p class="muted" style="margin-bottom:1rem;max-width:70ch">
    Registro de quién consultó o modificó cada historia clínica, cuándo y desde qué dirección IP.
    No se puede editar ni borrar (Resolución 1995 de 1999).
  </p>
  <div class="toolbar">
    <select id="a-accion" [(ngModel)]="f.accion" (ngModelChange)="cargar()" aria-label="Acción">
      <option value="">Todas las acciones</option>
      @for (a of acciones; track a[0]) { <option [value]="a[0]">{{ a[1] }}</option> }
    </select>
    <input id="a-desde" type="date" [(ngModel)]="f.desde" (ngModelChange)="cargar()" aria-label="Desde">
    <input id="a-hasta" type="date" [(ngModel)]="f.hasta" (ngModelChange)="cargar()" aria-label="Hasta">
  </div>
  @if (error()) { <p class="note err">{{ error() }}</p> }
  <div class="tbl-wrap">
    <table>
      <thead><tr><th>Fecha</th><th>Usuario</th><th>Acción</th><th>Paciente</th><th>Detalle</th><th>IP</th></tr></thead>
      <tbody>
        @for (a of filas(); track a.id) {
          <tr>
            <td class="num">{{ a.fecha | date:'d MMM y' }}<br><span class="muted">{{ a.fecha | date:'h:mm:ss a' }}</span></td>
            <td>{{ a.usuario }}<br><span class="muted">{{ a.rol }}</span></td>
            <td><span class="aud" [class]="'aud-' + a.accion">{{ nombre(a.accion) }}</span></td>
            <td><a [routerLink]="['/admin/pacientes', a.pacienteId]">{{ a.paciente }}</a></td>
            <td style="max-width:40ch">{{ a.detalle }}</td>
            <td class="num muted">{{ a.ip ?? '—' }}</td>
          </tr>
        } @empty { <tr><td colspan="6"><div class="empty">No hay movimientos en este rango.</div></td></tr> }
      </tbody>
    </table>
  </div>`
})
export class AuditoriaComponent {
  private api = inject(ApiService);
  acciones = Object.entries(ACCIONES);
  filas = signal<Auditoria[]>([]);
  error = signal('');
  f = { accion: '', desde: isoFecha(new Date(Date.now() - 30 * 864e5)), hasta: isoFecha(new Date()) };

  constructor() { this.cargar(); }
  nombre(a: AccionAuditoria) { return ACCIONES[a] ?? a; }
  cargar() {
    this.api.auditoria(this.f).subscribe({ next: f => { this.filas.set(f); this.error.set(''); }, error: e => this.error.set(mensajeError(e)) });
  }
}
