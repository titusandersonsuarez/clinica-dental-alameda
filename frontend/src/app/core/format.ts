import { Pipe, PipeTransform } from '@angular/core';

const DIAS = ['dom', 'lun', 'mar', 'mié', 'jue', 'vie', 'sáb'];
const MESES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];

const pad = (n: number) => String(n).padStart(2, '0');
export const isoFecha = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

/** "2026-10-26" → "lun 26 oct" */
export function fechaCorta(iso: string): string {
  const d = new Date(iso + 'T12:00:00');
  return `${DIAS[d.getDay()]} ${d.getDate()} ${MESES[d.getMonth()]}`;
}

/** Próximos días hábiles de atención (lunes a sábado). */
export function proximosDias(cantidad: number, desde = new Date()): string[] {
  const d = new Date(desde); d.setHours(12, 0, 0, 0);
  const out: string[] = [];
  while (out.length < cantidad) {
    if (d.getDay() !== 0) out.push(isoFecha(d));
    d.setDate(d.getDate() + 1);
  }
  return out;
}

@Pipe({ name: 'fechaCorta' })
export class FechaCortaPipe implements PipeTransform { transform(v: string) { return fechaCorta(v); } }

@Pipe({ name: 'cop' })
export class CopPipe implements PipeTransform {
  transform(v: number) { return '$ ' + v.toLocaleString('es-CO'); }
}
