import { Routes } from '@angular/router';
import { authGuard } from './core/auth';
import { HomeComponent } from './sitio/home.component';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: 'Clínica Dental Alameda' },
  {
    path: 'admin/login', title: 'Ingresar · Clínica Dental Alameda',
    loadComponent: () => import('./admin/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'admin', canActivate: [authGuard], title: 'Administración · Clínica Dental Alameda',
    loadComponent: () => import('./admin/admin-shell.component').then(m => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'citas' },
      { path: 'citas', loadComponent: () => import('./admin/citas.component').then(m => m.CitasComponent) },
      { path: 'pacientes', loadComponent: () => import('./admin/pacientes.component').then(m => m.PacientesComponent) },
      { path: 'pacientes/:id', loadComponent: () => import('./admin/pacientes.component').then(m => m.PacientesComponent) }
    ]
  },
  { path: '**', redirectTo: '' }
];
