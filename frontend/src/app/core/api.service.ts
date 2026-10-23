import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import {
  CitaAdmin, CitaCreada, CitaPaciente, CrearCita, CrearRegistro, Disponibilidad, EstadoCita,
  Auditoria, CrearUsuario, LoginResponse, Odontologo, PacienteDetalle, PacienteLista, Problema, RegistroClinico, Resumen, Servicio, Usuario, UsuarioConPassword
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private http = inject(HttpClient);
  private base = environment.apiUrl;

  // Público
  servicios() { return this.http.get<Servicio[]>(`${this.base}/servicios`); }
  odontologos() { return this.http.get<Odontologo[]>(`${this.base}/odontologos`); }
  disponibilidad(odontologoId: number, servicioId: number, fecha: string) {
    const params = new HttpParams().set('odontologoId', odontologoId).set('servicioId', servicioId).set('fecha', fecha);
    return this.http.get<Disponibilidad>(`${this.base}/disponibilidad`, { params });
  }
  reservar(body: CrearCita) { return this.http.post<CitaCreada>(`${this.base}/citas`, body); }
  misCitas(cedula: string, celular: string) {
    return this.http.get<CitaPaciente[]>(`${this.base}/citas/consulta`, { params: { cedula, celular } });
  }
  login(email: string, password: string) { return this.http.post<LoginResponse>(`${this.base}/auth/login`, { email, password }); }
  cambiarPassword(actual: string, nueva: string) { return this.http.post<LoginResponse>(`${this.base}/auth/cambiar-password`, { actual, nueva }); }

  // Administración
  citas(f: { estado?: string; odontologoId?: number | null; desde?: string; hasta?: string; q?: string }) {
    let params = new HttpParams();
    Object.entries(f).forEach(([k, v]) => { if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v)); });
    return this.http.get<CitaAdmin[]>(`${this.base}/admin/citas`, { params });
  }
  resumen() { return this.http.get<Resumen>(`${this.base}/admin/citas/resumen`); }
  cambiarEstado(id: number, estado: EstadoCita) { return this.http.patch<void>(`${this.base}/admin/citas/${id}/estado`, { estado }); }
  reenviarWhatsApp(id: number) { return this.http.post<void>(`${this.base}/admin/citas/${id}/whatsapp`, {}); }
  pacientes(q = '') { return this.http.get<PacienteLista[]>(`${this.base}/admin/pacientes`, { params: q ? { q } : {} }); }
  paciente(id: number) { return this.http.get<PacienteDetalle>(`${this.base}/admin/pacientes/${id}`); }
  guardarAntecedentes(id: number, antecedentes: string) {
    return this.http.put<void>(`${this.base}/admin/pacientes/${id}/antecedentes`, { antecedentes });
  }
  agregarRegistro(id: number, body: CrearRegistro) {
    return this.http.post<RegistroClinico>(`${this.base}/admin/pacientes/${id}/historia`, body);
  }
  anularRegistro(pacienteId: number, registroId: number, motivo: string) {
    return this.http.post<RegistroClinico>(`${this.base}/admin/pacientes/${pacienteId}/historia/${registroId}/anular`, { motivo });
  }

  // Usuarios y auditoría (solo Admin)
  usuarios() { return this.http.get<Usuario[]>(`${this.base}/admin/usuarios`); }
  crearUsuario(body: CrearUsuario) { return this.http.post<UsuarioConPassword>(`${this.base}/admin/usuarios`, body); }
  restablecerPassword(id: number) { return this.http.post<UsuarioConPassword>(`${this.base}/admin/usuarios/${id}/restablecer-password`, {}); }
  cambiarActivo(id: number, activo: boolean) { return this.http.patch<void>(`${this.base}/admin/usuarios/${id}/activo`, { activo }); }
  auditoria(f: { pacienteId?: number | null; desde?: string; hasta?: string; accion?: string }) {
    let params = new HttpParams();
    Object.entries(f).forEach(([k, v]) => { if (v !== undefined && v !== null && v !== '') params = params.set(k, String(v)); });
    return this.http.get<Auditoria[]>(`${this.base}/admin/auditoria`, { params });
  }
}

/** Convierte un error HTTP en un mensaje legible para el usuario. */
export function mensajeError(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'No hay conexión con el servidor. Revisa tu internet e intenta de nuevo.';
    if (err.status === 403) return (err.error as Problema | null)?.title ?? 'Tu usuario no tiene permiso para esta sección.';
    if (err.status === 429) return 'Demasiados intentos seguidos. Espera un minuto e intenta de nuevo.';
    const p = err.error as Problema | null;
    if (p?.errors) return Object.values(p.errors).flat()[0] ?? 'Revisa los datos del formulario.';
    if (p?.title) return p.title;
  }
  return 'Ocurrió un error inesperado. Intenta de nuevo.';
}
