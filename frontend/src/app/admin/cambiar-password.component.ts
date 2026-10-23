import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { mensajeError } from '../core/api.service';
import { AuthService } from '../core/auth';

@Component({
  selector: 'app-cambiar-password',
  imports: [FormsModule],
  template: `
  <div class="wrap login">
    <form class="panel" (ngSubmit)="guardar()">
      <div>
        <div class="eyebrow">{{ auth.debeCambiarPassword() ? 'Primer ingreso' : 'Seguridad' }}</div>
        <h2>Cambia tu contraseña</h2>
        @if (auth.debeCambiarPassword()) {
          <p class="muted" style="margin-top:.5rem">Estás usando una contraseña temporal. Crea una propia para entrar al panel.</p>
        }
      </div>
      <label class="f">Contraseña actual<input id="p-actual" name="actual" type="password" [(ngModel)]="actual" autocomplete="current-password" required></label>
      <label class="f">Nueva contraseña<input id="p-nueva" name="nueva" type="password" [ngModel]="nueva()" (ngModelChange)="nueva.set($event)" autocomplete="new-password" required></label>
      <ul class="reglas">
        <li [class.ok]="largo()">Al menos 10 caracteres</li>
        <li [class.ok]="mezcla()">Letras y números</li>
      </ul>
      <label class="f">Repite la nueva contraseña<input id="p-repite" name="repite" type="password" [(ngModel)]="repite" autocomplete="new-password" required></label>
      <button class="btn" type="submit" [disabled]="enviando()" style="justify-content:center">{{ enviando() ? 'Guardando…' : 'Guardar contraseña' }}</button>
      @if (error()) { <p class="note err" role="alert">{{ error() }}</p> }
    </form>
  </div>`
})
export class CambiarPasswordComponent {
  auth = inject(AuthService);
  private router = inject(Router);
  actual = '';
  nueva = signal('');
  repite = '';
  enviando = signal(false);
  error = signal('');
  largo = computed(() => this.nueva().length >= 10);
  mezcla = computed(() => /[a-zA-Z]/.test(this.nueva()) && /\d/.test(this.nueva()));

  guardar() {
    if (!this.largo() || !this.mezcla()) { this.error.set('La nueva contraseña no cumple los requisitos.'); return; }
    if (this.nueva() !== this.repite) { this.error.set('Las contraseñas nuevas no coinciden.'); return; }
    this.enviando.set(true);
    this.auth.cambiarPassword(this.actual, this.nueva()).subscribe({
      next: () => this.router.navigate(['/admin']),
      error: e => { this.enviando.set(false); this.error.set(mensajeError(e)); }
    });
  }
}
