# Puesta en producción

Tiempo estimado: 1 hora la primera vez. Necesitas un servidor, un dominio y acceso SSH.

## 1. Servidor y dominio

- **Servidor:** cualquier VPS con Ubuntu 22.04 o 24.04, 2 GB de RAM y 25 GB de disco (DigitalOcean, Hetzner, Vultr, AWS Lightsail…). Para datos de salud prefiere un proveedor con centro de datos en América.
- **Dominio:** crea un registro DNS **tipo A** que apunte a la IP del servidor, por ejemplo `citas.tuclinica.co`. Espera a que resuelva (`ping citas.tuclinica.co`).
- **Firewall:** abre solo los puertos 22 (SSH), 80 y 443.

## 2. Instalar Docker

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # cierra y vuelve a abrir la sesión SSH
```

## 3. Configurar

```bash
git clone https://github.com/titusandersonsuarez/clinica-dental-alameda.git
cd clinica-dental-alameda
cp .env.production.example .env
openssl rand -base64 48   # úsalo para DB_PASSWORD
openssl rand -base64 48   # úsalo para JWT_KEY
nano .env                 # completa DOMINIO, CORREO_TLS, secretos, ADMIN_EMAIL, ADMIN_PASSWORD_TEMPORAL y datos de la clínica
chmod 600 .env
```

Edita también los datos públicos del sitio en `frontend/src/app/core/clinica.config.ts`: WhatsApp, dirección, **NIT** y **correo para asuntos de datos** (aparecen en la política de privacidad).

El sistema **no arranca** si falta una variable obligatoria o si `JWT_KEY` sigue siendo la de ejemplo.

## 4. Arrancar

```bash
docker compose -f docker-compose.prod.yml up -d --build
docker compose -f docker-compose.prod.yml logs -f api caddy   # Ctrl+C para salir
```

Caddy obtiene el certificado HTTPS automáticamente. Abre `https://citas.tuclinica.co`.

## 5. Primer ingreso

1. Entra a `/admin` con `ADMIN_EMAIL` y `ADMIN_PASSWORD_TEMPORAL`.
2. El sistema te obliga a crear una contraseña propia (mínimo 10 caracteres, letras y números).
3. En **Usuarios**, crea una cuenta por persona:
   - **Recepción:** agenda y confirmación de citas. No ve la historia clínica.
   - **Odontólogo:** agenda e historia clínica. Se vincula con su perfil de la agenda.
   - **Administrador:** todo, más usuarios y auditoría.

   Cada uno recibe una contraseña temporal que se muestra una sola vez y debe cambiarla al entrar. No compartan usuarios: la auditoría registra quién hizo cada cosa.

## 6. Respaldos

El contenedor `backup` hace un respaldo diario a la hora `HORA_BACKUP` y conserva `RETENCION_DIAS` días en la carpeta `backups/` del servidor. Cada archivo se verifica después de crearlo.

```bash
ls -lh backups/                                                            # ver respaldos
docker compose -f docker-compose.prod.yml exec backup /bin/sh /backup.sh ahora   # respaldo inmediato
docker compose -f docker-compose.prod.yml logs backup                      # ver resultados
```

**Copia fuera del servidor (obligatorio):** si el servidor se pierde, los respaldos locales también. Opciones:
- Activar los *snapshots* automáticos del proveedor del VPS.
- Copiar `backups/` a otro lugar a diario con [rclone](https://rclone.org) (Google Drive, S3, Backblaze B2). Ejemplo en `crontab -e`:
  ```
  30 3 * * * rclone copy /home/usuario/clinica-dental-alameda/backups remoto:clinica-backups
  ```

**Restaurar** (prueba esto una vez antes de salir a producción):

```bash
./scripts/restaurar.sh backups/dental_alameda_2026-10-23_0200.dump
```

## 7. Actualizar a una nueva versión

```bash
docker compose -f docker-compose.prod.yml exec backup /bin/sh /backup.sh ahora
git pull
docker compose -f docker-compose.prod.yml up -d --build
```

Las migraciones de base de datos se aplican solas al arrancar la API.

## Lista de verificación antes de abrir al público

- [ ] El sitio abre con candado (HTTPS) y `http://` redirige a `https://`.
- [ ] Cambiaste la contraseña temporal del administrador y creaste usuarios personales.
- [ ] NIT, correo de datos, dirección y WhatsApp reales en `clinica.config.ts`; la política en `/privacidad` la revisó un abogado.
- [ ] Hiciste un respaldo manual, lo restauraste y comprobaste los datos.
- [ ] La copia de respaldos fuera del servidor está programada.
- [ ] `https://tu-dominio/swagger` no abre (Swagger apagado).
- [ ] Hiciste una reserva de prueba de punta a punta y la cancelaste.
- [ ] WhatsApp: en `Consola` hasta tener plantillas aprobadas; luego `Twilio` (ver [WHATSAPP.md](WHATSAPP.md)).

## Qué protege cada pieza

| Riesgo | Protección |
|---|---|
| Tráfico interceptado | HTTPS obligatorio con HSTS (Caddy + Let's Encrypt) |
| Base de datos expuesta | PostgreSQL sin puertos publicados, solo accesible desde la API |
| Secretos de ejemplo en producción | La API se niega a arrancar con `JWT_KEY` o `DB_PASSWORD` de ejemplo |
| Contraseña temporal filtrada | Hasta cambiarla, el token solo permite cambiar la contraseña |
| Fuerza bruta | 10 intentos de ingreso por minuto por IP; 5 cambios de contraseña cada 5 min por usuario |
| Acceso indebido a historia clínica | Recepción no tiene acceso; cada consulta y cambio queda en la auditoría con usuario, fecha e IP |
| Alteración de la historia | Los registros no se editan ni se borran; se anulan con motivo y quedan visibles |
| Pérdida de datos | Respaldo diario verificado con retención y script de restauración |
