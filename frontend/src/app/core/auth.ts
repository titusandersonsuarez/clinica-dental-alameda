import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, tap, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { LoginResponse } from './models';

const KEY = 'alameda.sesion';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private router = inject(Router);
  private sesion = signal<LoginResponse | null>(this.leer());

  readonly usuario = computed(() => this.sesion()?.nombre ?? null);
  readonly autenticado = computed(() => {
    const s = this.sesion();
    return !!s && new Date(s.expiraEn).getTime() > Date.now();
  });
  get token() { return this.autenticado() ? this.sesion()!.token : null; }

  login(email: string, password: string) {
    return this.api.login(email, password).pipe(tap(r => {
      this.sesion.set(r);
      try { sessionStorage.setItem(KEY, JSON.stringify(r)); } catch { /* almacenamiento no disponible */ }
    }));
  }

  logout() {
    this.sesion.set(null);
    try { sessionStorage.removeItem(KEY); } catch { /* ignorar */ }
    this.router.navigate(['/admin/login']);
  }

  private leer(): LoginResponse | null {
    try { return JSON.parse(sessionStorage.getItem(KEY) ?? 'null'); } catch { return null; }
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const r = token && req.url.includes('/admin/') ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(r).pipe(catchError((e: HttpErrorResponse) => {
    if (e.status === 401 && req.url.includes('/admin/')) auth.logout();
    return throwError(() => e);
  }));
};

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.autenticado() ? true : inject(Router).createUrlTree(['/admin/login']);
};
