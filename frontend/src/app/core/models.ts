export type EstadoCita = 'Pendiente' | 'Confirmada' | 'Atendida' | 'Cancelada';

export interface Servicio { id: number; codigo: string; nombre: string; descripcion: string; duracionMinutos: number; precioDesde: number; }
export interface Odontologo { id: number; nombre: string; especialidad: string; registroProfesional: string; }
export interface Horario { hora: string; disponible: boolean; }
export interface Disponibilidad { fecha: string; odontologoId: number; servicioId: number; horarios: Horario[]; }

export interface CrearCita {
  servicioId: number; odontologoId: number; fecha: string; hora: string;
  nombre: string; cedula: string; celular: string; correo?: string; motivo?: string; aceptaWhatsApp: boolean; aceptaPoliticaDatos: boolean;
}
export interface CitaCreada { id: number; fecha: string; hora: string; servicio: string; odontologo: string; estado: EstadoCita; }
export interface CitaPaciente { id: number; fecha: string; hora: string; servicio: string; odontologo: string; estado: EstadoCita; }

export interface CitaAdmin {
  id: number; fecha: string; horaInicio: string; horaFin: string; estado: EstadoCita;
  pacienteId: number; pacienteNombre: string; cedula: string; celular: string;
  servicioId: number; servicio: string; odontologoId: number; odontologo: string; motivo: string;
  aceptaWhatsApp: boolean; whatsApp: 'Pendiente' | 'Enviada' | 'Fallida' | null; whatsAppError: string | null;
}
export interface Resumen { citasHoy: number; pendientes: number; confirmadas: number; pacientes: number; }

export interface PacienteLista { id: number; nombre: string; cedula: string; celular: string; totalCitas: number; }
export interface RegistroClinico {
  id: number; fecha: string; piezaFdi: string | null; procedimiento: string; observaciones: string; odontologo: string | null;
  autor: string; creadoEn: string; anulado: boolean; motivoAnulacion: string | null; anuladoPor: string | null; anuladoEn: string | null;
}
export interface PacienteDetalle {
  id: number; nombre: string; cedula: string; celular: string; correo: string | null; antecedentes: string;
  historia: RegistroClinico[]; citas: CitaPaciente[];
}
export interface CrearRegistro { fecha: string; piezaFdi?: string; procedimiento: string; observaciones?: string; odontologoId?: number; }

export type Rol = 'Admin' | 'Recepcion' | 'Odontologo';
export interface LoginResponse { token: string; expiraEn: string; nombre: string; rol: Rol; debeCambiarPassword: boolean; }

export interface Usuario {
  id: number; email: string; nombre: string; rol: Rol; activo: boolean; debeCambiarPassword: boolean;
  odontologoId: number | null; odontologo: string | null; ultimoIngresoEn: string | null;
}
export interface UsuarioConPassword { usuario: Usuario; passwordTemporal: string; }
export interface CrearUsuario { email: string; nombre: string; rol: Rol; odontologoId: number | null; }

export type AccionAuditoria = 'ConsultaHistoria' | 'RegistroCreado' | 'RegistroAnulado' | 'AntecedentesModificados';
export interface Auditoria {
  id: number; fecha: string; usuario: string; rol: string; accion: AccionAuditoria;
  pacienteId: number; paciente: string; detalle: string; ip: string | null;
}

/** Error de la API en formato ProblemDetails. */
export interface Problema { title?: string; status?: number; errors?: Record<string, string[]>; }
