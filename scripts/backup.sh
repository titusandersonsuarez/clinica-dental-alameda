#!/bin/sh
# Respaldo diario de PostgreSQL con retención. Corre dentro del contenedor "backup".
# Variables: PGHOST PGUSER PGPASSWORD PGDATABASE HORA_BACKUP (00-23) RETENCION_DIAS
# Uso manual (un respaldo inmediato):  docker compose -f docker-compose.prod.yml exec backup /bin/sh /backup.sh ahora
set -eu

DESTINO=${DESTINO:-/backups}
HORA_BACKUP=${HORA_BACKUP:-02}
RETENCION_DIAS=${RETENCION_DIAS:-30}

respaldar() {
  mkdir -p "$DESTINO"
  archivo="$DESTINO/dental_alameda_$(date +%Y-%m-%d_%H%M).dump"
  tmp="$archivo.parcial"
  # Formato custom (-Fc): comprimido y restaurable con pg_restore
  if pg_dump -Fc --no-owner -f "$tmp" "$PGDATABASE"; then
    mv "$tmp" "$archivo"
    # Verifica que el archivo se pueda leer antes de darlo por bueno
    pg_restore --list "$archivo" > /dev/null
    echo "$(date '+%F %T') Respaldo OK: $archivo ($(du -h "$archivo" | cut -f1))"
    find "$DESTINO" -name 'dental_alameda_*.dump' -mtime +"$RETENCION_DIAS" -print -delete | sed 's/^/Eliminado por retención: /'
  else
    rm -f "$tmp"
    echo "$(date '+%F %T') ERROR: falló el respaldo" >&2
    return 1
  fi
}

if [ "${1:-}" = "ahora" ]; then
  respaldar
  exit $?
fi

echo "Respaldos diarios a las ${HORA_BACKUP}:00 (America/Bogota), retención ${RETENCION_DIAS} días, en $DESTINO"
ultimo=""
while true; do
  hoy=$(date +%F)
  if [ "$(date +%H)" = "$HORA_BACKUP" ] && [ "$ultimo" != "$hoy" ]; then
    respaldar || true
    ultimo=$hoy
  fi
  sleep 300
done
