#!/bin/sh
# Restaura un respaldo sobre la base de datos de producción.
# Uso (desde la carpeta del proyecto en el servidor):
#   ./scripts/restaurar.sh backups/dental_alameda_2026-10-23_0200.dump
# ATENCIÓN: reemplaza los datos actuales. Antes toma un respaldo con:
#   docker compose -f docker-compose.prod.yml exec backup /bin/sh /backup.sh ahora
set -eu

ARCHIVO=${1:?Indica el archivo .dump a restaurar}
COMPOSE="docker compose -f docker-compose.prod.yml"

[ -f "$ARCHIVO" ] || { echo "No existe $ARCHIVO" >&2; exit 1; }

printf "Esto reemplaza TODOS los datos actuales con %s. Escribe RESTAURAR para continuar: " "$ARCHIVO"
read -r confirmacion
[ "$confirmacion" = "RESTAURAR" ] || { echo "Cancelado."; exit 1; }

echo "Deteniendo la API para que nadie escriba durante la restauración…"
$COMPOSE stop api

echo "Restaurando…"
$COMPOSE exec -T db pg_restore -U dental -d dental_alameda --clean --if-exists --no-owner < "$ARCHIVO"

echo "Arrancando la API…"
$COMPOSE start api
echo "Listo. Verifica en el panel que los datos sean los esperados."
