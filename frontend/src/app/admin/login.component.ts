import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { mensajeError } from '../core/api.service';
import { AuthService } from '../core/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
  <div class="wrap login">
    <form class="panel" (ngSubmit)="entrar()">
      <div><div class="eyebrow">Panel interno</div><h2>Iniciar sesión</h2></div>
      <label class="f">Correo<input id="l-email" name="email" type="email" [(ngModel)]="email" autocomplete="username" required></label>
      <label class="f">Contraseña<input id="l-pass" name="password" type="password" [(ngModel)]="password" autocomplete="current-password" required></label>
      <button class="btn" type="submit" [disabled]="enviando()" style="justify-content:center">{{ enviando() ? 'Entrando…' : 'Entrar' }}</button>
      @if (error()) { <p class="note err" role="alert">{{ error() }}</p> }
    </form>
  </div>`
})
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);
  email = '';
  password = '';
  enviando = signal(false);
  error = signal('');

  entrar() {
    if (!this.email || !this.password) { this.error.set('Escribe tu correo y contraseña.'); return; }
    this.enviando.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: r => this.router.navigate([r.debeCambiarPassword ? '/admin/cambiar-password' : '/admin']),
      error: e => { this.enviando.set(false); this.error.set(mensajeError(e)); }
    });
  }
}
