import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth';
import { Resumen } from '../core/models';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
  <div class="admin">
    <div class="wrap">
      <div class="admin-head">
        <div><div class="eyebrow">Panel interno</div><h2>Administración</h2></div>
        <div style="display:flex;gap:.75rem;align-items:center">
          <span class="muted" style="font-size:.9rem">{{ auth.usuario() }}</span>
          <button class="btn ghost sm" type="button" (click)="auth.logout()">Cerrar sesión</button>
        </div>
      </div>
      @if (resumen(); as r) {
        <div class="kpis">
          <div class="kpi"><b>{{ r.citasHoy }}</b><span>Citas hoy</span></div>
          <div class="kpi"><b>{{ r.pendientes }}</b><span>Por confirmar</span></div>
          <div class="kpi"><b>{{ r.confirmadas }}</b><span>Confirmadas próximas</span></div>
          <div class="kpi"><b>{{ r.pacientes }}</b><span>Pacientes</span></div>
        </div>
      }
      <nav class="subtabs">
        <a routerLink="citas" routerLinkActive="on">Citas</a>
        <a routerLink="pacientes" routerLinkActive="on">Pacientes e historia clínica</a>
      </nav>
      <router-outlet (activate)="cargarResumen()" />
    </div>
  </div>`
})
export class AdminShellComponent {
  private api = inject(ApiService);
  auth = inject(AuthService);
  resumen = signal<Resumen | null>(null);

  cargarResumen() { this.api.resumen().subscribe({ next: r => this.resumen.set(r), error: () => {} }); }
}
