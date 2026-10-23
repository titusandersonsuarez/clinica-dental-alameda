import { Routes } from '@angular/router';
import { authGuard, rolGuard } from './core/auth';
import { HomeComponent } from './sitio/home.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: 'Clínica Dental Alameda' },
  {
    path: 'admin/login', title: 'Ingresar · Clínica Dental Alameda',
    loadComponent: () => import('./admin/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'privacidad', title: 'Política de tratamiento de datos · Clínica Dental Alameda',
    loadComponent: () => import('./sitio/privacidad.component').then(m => m.PrivacidadComponent)
  },
  {
    path: 'admin/cambiar-password', canActivate: [authGuard], title: 'Cambiar contraseña · Clínica Dental Alameda',
    loadComponent: () => import('./admin/cambiar-password.component').then(m => m.CambiarPasswordComponent)
  },
  {
    path: 'admin', canActivate: [authGuard], title: 'Administración · Clínica Dental Alameda',
    loadComponent: () => import('./admin/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'citas' },
      { path: 'citas', loadComponent: () => import('./admin/citas.component').then(m => m.CitasComponent) },
      { path: 'pacientes', canActivate: [rolGuard('Admin', 'Odontologo')], loadComponent: () => import('./admin/pacientes.component').then(m => m.PacientesComponent) },
      { path: 'pacientes/:id', canActivate: [rolGuard('Admin', 'Odontologo')], loadComponent: () => import('./admin/pacientes.component').then(m => m.PacientesComponent) },
      { path: 'usuarios', canActivate: [rolGuard('Admin')], loadComponent: () => import('./admin/usuarios.component').then(m => m.UsuariosComponent) },
      { path: 'auditoria', canActivate: [rolGuard('Admin')], loadComponent: () => import('./admin/auditoria.component').then(m => m.AuditoriaComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
