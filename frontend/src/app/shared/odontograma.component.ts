import { Component, computed, input } from '@angular/core';

const SUPERIOR = [18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28];
const INFERIOR = [48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38];

/** Odontograma FDI de 32 piezas; resalta las piezas con registros en la historia clínica. */
@Component({
  selector: 'app-odontograma',
  template: `
    <div class="odonto">
      <svg viewBox="0 0 560 120" role="img" aria-label="Odontograma FDI">
        @for (fila of filas; track $index) {
          @for (t of fila.dientes; track t.n) {
            <rect [attr.x]="t.x" [attr.y]="fila.y" width="28" height="28" rx="6"
                  [attr.fill]="marcadas().has(t.n) ? 'var(--accent)' : 'var(--surface)'"
                  [attr.stroke]="marcadas().has(t.n) ? 'var(--accent)' : 'var(--line)'" />
            <text [attr.x]="t.x + 14" [attr.y]="fila.y + 18" text-anchor="middle"
                  font-family="JetBrains Mono, monospace" font-size="10"
                  [attr.fill]="marcadas().has(t.n) ? 'var(--accent-ink)' : 'var(--muted)'">{{ t.n }}</text>
          }
        }
        <line x1="280" y1="4" x2="280" y2="100" stroke="var(--line)" stroke-dasharray="3 3" />
        <text x="280" y="116" text-anchor="middle" font-size="10" fill="var(--muted)">Piezas resaltadas: con registro en la historia</text>
      </svg>
    </div>`
})
export class OdontogramaComponent {
  piezas = input<(string | null)[]>([]);
  marcadas = computed(() => new Set(this.piezas().filter(Boolean).map(Number)));
  filas = [SUPERIOR, INFERIOR].map((f, ri) => ({
    y: ri ? 66 : 10,
    dientes: f.map((n, i) => ({ n, x: 14 + i * 33 + (i > 7 ? 6 : 0) }))
  }));
}

/** Arcada superior decorativa para la portada. */
@Component({
  selector: 'app-arcada',
  template: `
    <svg viewBox="0 0 320 210" role="img" aria-label="Arcada superior con numeración FDI">
      @for (t of dientes; track t.n) {
        <rect [attr.x]="t.x - t.w / 2" [attr.y]="t.y - 12" [attr.width]="t.w" [attr.height]="t.molar ? 22 : 20"
              [attr.rx]="t.molar ? 5 : 7" [attr.fill]="t.hi ? 'var(--accent)' : 'var(--accent-soft)'"
              stroke="var(--accent)" stroke-width="1" />
        <text [attr.x]="t.x" [attr.y]="t.y + 24" text-anchor="middle" font-family="JetBrains Mono, monospace"
              font-size="8.5" fill="var(--muted)">{{ t.n }}</text>
      }
      <text x="160" y="200" text-anchor="middle" font-size="10" fill="var(--muted)">derecha del paciente ← → izquierda</text>
    </svg>`
})
export class ArcadaComponent {
  dientes = SUPERIOR.map((n, i) => {
    const t = (i - 7.5) / 7.5;
    return {
      n, x: 160 + t * 138, y: 150 - Math.cos(t * 1.25) * 95, w: i % 8 < 3 ? 18 : 15,
      molar: [18, 17, 16, 26, 27, 28].includes(n), hi: n === 16 || n === 21
    };
  });
}
