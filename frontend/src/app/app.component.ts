import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
  <header class="top">
    <div class="wrap">
      <a class="brand" routerLink="/" style="text-decoration:none">
        <svg viewBox="0 0 32 32" aria-hidden="true"><path fill="var(--accent)" d="M16 4c-3-2-9-2-11 2-2 5 1 10 2 15 1 4 2 8 4 8s2-6 5-6 3 6 5 6 3-4 4-8c1-5 4-10 2-15-2-4-8-4-11-2z"/></svg>
        Clínica Dental Alameda
      </a>
      <nav class="tabs" aria-label="Secciones">
        <a routerLink="/" fragment="servicios">Servicios</a>
        <a routerLink="/" fragment="reservar">Reservar</a>
        <a [routerLink]="auth.autenticado() ? '/admin' : '/admin/login'" routerLinkActive="on">Administración</a>
      </nav>
    </div>
  </header>
  <main><router-outlet /></main>`
})
export class AppComponent {
  auth = inject(AuthService);
}
