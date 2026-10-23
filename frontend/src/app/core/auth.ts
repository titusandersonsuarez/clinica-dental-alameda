import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { LoginResponse, Rol } from './models';

const KEY = 'alameda.sesion';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);
  private sesion = signal<LoginResponse | null>(this.leer());

  readonly usuario = computed(() => this.sesion()?.nombre ?? null);
  readonly rol = computed<Rol | null>(() => this.sesion()?.rol ?? null);
  readonly debeCambiarPassword = computed(() => this.sesion()?.debeCambiarPassword ?? false);
  readonly autenticado = computed(() => {
    const s = this.sesion();
    return !!s && new Date(s.expiraEn).getTime() > Date.now();
  });
  readonly esAdmin = computed(() => this.rol() === 'Admin');
  /** Admin y Odontólogo ven la historia clínica; Recepción solo la agenda. */
  readonly veHistoria = computed(() => this.rol() === 'Admin' || this.rol() === 'Odontologo');

  get token() { return this.autenticado() ? this.sesion()!.token : null; }

  login(email: string, password: string) {
    return this.api.login(email, password).pipe(tap(r => this.guardar(r)));
  }

  cambiarPassword(actual: string, nueva: string) {
    return this.api.cambiarPassword(actual, nueva).pipe(tap(r => this.guardar(r)));
  }

  tiene(...roles: Rol[]) { const r = this.rol(); return !!r && roles.includes(r); }

  logout() {
    this.sesion.set(null);
    try { sessionStorage.removeItem(KEY); } catch { /* ignorar */ }
    this.router.navigate(['/admin/login']);
  }

  private guardar(r: LoginResponse) {
    this.sesion.set(r);
    try { sessionStorage.setItem(KEY, JSON.stringify(r)); } catch { /* almacenamiento no disponible */ }
  }

  private leer(): LoginResponse | null {
    try { return JSON.parse(sessionStorage.getItem(KEY) ?? 'null'); } catch { return null; }
  }
}

const necesitaToken = (url: string) => url.includes('/admin/') || url.includes('/auth/cambiar-password');

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const r = token && necesitaToken(req.url) ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(r).pipe(catchError((e: HttpErrorResponse) => {
    if (e.status === 401 && necesitaToken(req.url)) auth.logout();
    return throwError(() => e);
  }));
};

/** Exige sesión; si la contraseña es temporal, solo deja entrar a la pantalla de cambio. */
export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.autenticado()) return router.createUrlTree(['/admin/login']);
  if (auth.debeCambiarPassword() && !state.url.startsWith('/admin/cambiar-password'))
    return router.createUrlTree(['/admin/cambiar-password']);
  return true;
};

export const rolGuard = (...roles: Rol[]): CanActivateFn => () => {
  const auth = inject(AuthService);
  return auth.tiene(...roles) ? true : inject(Router).createUrlTree(['/admin/citas']);
};
