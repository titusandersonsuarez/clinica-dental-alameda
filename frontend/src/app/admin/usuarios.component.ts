import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService, mensajeError } from '../core/api.service';
import { AuthService } from '../core/auth';
import { Odontologo, Rol, Usuario, UsuarioConPassword } from '../core/models';

const ROLES: { v: Rol; t: string; d: string }[] = [
  { v: 'Recepcion', t: 'Recepción', d: 'Agenda y confirmación de citas. No ve la historia clínica.' },
  { v: 'Odontologo', t: 'Odontólogo', d: 'Agenda e historia clínica de los pacientes.' },
  { v: 'Admin', t: 'Administrador', d: 'Todo, más usuarios y auditoría.' }
];

@Component({
  selector: 'app-usuarios',
  imports: [FormsModule, DatePipe],
  template: `
  @if (temporal(); as t) {
    <div class="panel temporal" role="status">
      <div>
        <div class="eyebrow">Contraseña temporal de {{ t.usuario.nombre }}</div>
        <p class="muted" style="margin-top:.35rem">Entrégasela por un canal privado. Solo se muestra esta vez; al ingresar deberá cambiarla.</p>
      </div>
      <div class="copyrow"><code>{{ t.passwordTemporal }}</code>
        <button class="btn ghost sm" type="button" (click)="copiar(t.passwordTemporal)">{{ copiado() ? 'Copiada' : 'Copiar' }}</button>
        <button class="btn ghost sm" type="button" (click)="temporal.set(null)">Listo</button>
      </div>
    </div>
  }

  <form class="panel nuevo" (ngSubmit)="crear()">
    <h3>Nuevo usuario</h3>
    <div class="grid-nuevo">
      <label class="f">Nombre<input id="u-nombre" name="nombre" [(ngModel)]="nuevo.nombre" required></label>
      <label class="f">Correo<input id="u-email" name="email" type="email" [(ngModel)]="nuevo.email" required></label>
      <label class="f">Rol
        <select id="u-rol" name="rol" [(ngModel)]="nuevo.rol">
          @for (r of roles; track r.v) { <option [value]="r.v">{{ r.t }}</option> }
        </select></label>
      @if (nuevo.rol === 'Odontologo') {
        <label class="f">Profesional de la agenda
          <select id="u-odonto" name="odonto" [(ngModel)]="nuevo.odontologoId">
            <option [ngValue]="null">Elige…</option>
            @for (o of odontologos(); track o.id) { <option [ngValue]="o.id">{{ o.nombre }}</option> }
          </select></label>
      }
    </div>
    <p class="muted" style="font-size:.85rem">{{ descripcionRol() }}</p>
    <div style="display:flex;gap:.75rem;align-items:center">
      <button class="btn sm" type="submit" [disabled]="enviando()">Crear usuario</button>
      @if (error()) { <span class="note err" style="padding:.35rem .6rem">{{ error() }}</span> }
    </div>
  </form>

  <div class="tbl-wrap">
    <table>
      <thead><tr><th>Usuario</th><th>Rol</th><th>Estado</th><th>Último ingreso</th><th>Acciones</th></tr></thead>
      <tbody>
        @for (u of usuarios(); track u.id) {
          <tr [class.inactivo]="!u.activo">
            <td>{{ u.nombre }}<br><span class="muted">{{ u.email }}</span></td>
            <td>{{ nombreRol(u.rol) }}@if (u.odontologo) {<br><span class="muted">{{ u.odontologo }}</span>}</td>
            <td>
              <span class="pill" [class]="u.activo ? 'confirmada' : 'cancelada'">{{ u.activo ? 'Activo' : 'Desactivado' }}</span>
              @if (u.debeCambiarPassword && u.activo) { <div class="wa-state wa-pendiente">Contraseña temporal</div> }
            </td>
            <td class="num">{{ u.ultimoIngresoEn ? (u.ultimoIngresoEn | date:'d MMM y, h:mm a') : 'Nunca' }}</td>
            <td><div class="acts">
              <button class="btn ghost sm" type="button" (click)="restablecer(u)" [disabled]="!u.activo">Nueva contraseña temporal</button>
              @if (u.activo) {
                <button class="btn ghost sm" type="button" (click)="activo(u, false)" [disabled]="u.nombre === auth.usuario()">Desactivar</button>
              } @else {
                <button class="btn ghost sm" type="button" (click)="activo(u, true)">Reactivar</button>
              }
            </div></td>
          </tr>
        }
      </tbody>
    </table>
  </div>`
})
export class UsuariosComponent {
  private api = inject(ApiService);
  auth = inject(AuthService);
  roles = ROLES;
  usuarios = signal<Usuario[]>([]);
  odontologos = signal<Odontologo[]>([]);
  temporal = signal<UsuarioConPassword | null>(null);
  enviando = signal(false);
  copiado = signal(false);
  error = signal('');
  nuevo = { nombre: '', email: '', rol: 'Recepcion' as Rol, odontologoId: null as number | null };

  constructor() {
    this.cargar();
    this.api.odontologos().subscribe(o => this.odontologos.set(o));
  }

  cargar() { this.api.usuarios().subscribe({ next: u => this.usuarios.set(u), error: e => this.error.set(mensajeError(e)) }); }
  nombreRol(r: Rol) { return ROLES.find(x => x.v === r)?.t ?? r; }
  descripcionRol() { return ROLES.find(x => x.v === this.nuevo.rol)?.d ?? ''; }

  crear() {
    this.error.set('');
    if (!this.nuevo.nombre.trim() || !this.nuevo.email.trim()) { this.error.set('Escribe nombre y correo.'); return; }
    this.enviando.set(true);
    this.api.crearUsuario({ ...this.nuevo, odontologoId: this.nuevo.rol === 'Odontologo' ? this.nuevo.odontologoId : null }).subscribe({
      next: r => { this.enviando.set(false); this.temporal.set(r); this.nuevo = { nombre: '', email: '', rol: 'Recepcion', odontologoId: null }; this.cargar(); },
      error: e => { this.enviando.set(false); this.error.set(mensajeError(e)); }
    });
  }

  restablecer(u: Usuario) {
    this.api.restablecerPassword(u.id).subscribe({ next: r => { this.temporal.set(r); this.cargar(); }, error: e => this.error.set(mensajeError(e)) });
  }

  activo(u: Usuario, activo: boolean) {
    this.api.cambiarActivo(u.id, activo).subscribe({ next: () => this.cargar(), error: e => this.error.set(mensajeError(e)) });
  }

  async copiar(texto: string) {
    try { await navigator.clipboard.writeText(texto); this.copiado.set(true); setTimeout(() => this.copiado.set(false), 1800); } catch { /* sigue visible */ }
  }
}
