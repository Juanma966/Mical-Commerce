# Guía de despliegue a producción — Mical

Checklist para poner Mical en un entorno de producción. El código ya trae el
endurecimiento base (Fase 8); esta guía cubre la configuración del entorno.

## 1. Secretos y configuración (NUNCA en el repo)

En producción, estos valores se pasan por **variables de entorno** (no van en
`appsettings.Production.json`). ASP.NET Core los lee automáticamente:

| Variable de entorno | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a PostgreSQL (formato Npgsql). |
| `DATABASE_URL` | Alternativa: URL `postgres://user:pass@host:port/db` como la publican Railway/Render/Heroku. Se convierte sola a formato Npgsql. La cadena explícita de arriba tiene prioridad. |
| `AdminSeed__Password` | Contraseña del admin inicial (el seeder lo crea al arrancar). |
| `RESEND_API_KEY` | API key de Resend para el envío de emails (recuperación de contraseña). |
| `Storage__UploadsPath` | **Ruta absoluta** del volumen persistente donde se guardan las imágenes (ej. `/data/uploads`). Sin esto las imágenes van a `wwwroot/uploads`, que en un contenedor se borra en cada deploy. Una ruta relativa hace fallar el arranque a propósito. |
| `AllowedHosts` | Dominio real, separado por `;` si hay más de uno. Dejarlo en `*` acepta cualquier Host header. |
| `PORT` | La asigna la plataforma; la app la detecta y escucha ahí. No hace falta setearla a mano. |
| `ASPNETCORE_ENVIRONMENT` | `Production`. |

> Autenticación **solo por ASP.NET Identity** (email + contraseña). No hay login
> con proveedores externos.

> El doble guion bajo `__` es el separador de secciones en variables de entorno.
> Si `AdminSeed__Password` no está seteada, el seeder omite crear el admin (loguea un warning).

`appsettings.Production.json` (versionado) solo tiene config **no sensible**:
niveles de log y `AdminSeed:Email`/`FullName`. Ajustá `AllowedHosts` al dominio real.

## 2. Base de datos

- Levantar PostgreSQL (el `docker-compose.yml` sirve de referencia; en prod usar
  credenciales fuertes y volumen persistente respaldado).
- Las migraciones **se aplican solas al arrancar** (`Database.MigrateAsync()` en
  `Program.cs`), porque en un PaaS no hay una shell donde correrlas a mano. Es
  idempotente. Si algún día se escala a varias instancias, conviene moverlo a un
  paso previo del deploy (`dotnet ef migrations script --idempotent`) para que no
  migren dos instancias a la vez.

## 3. HTTPS / proxy inverso

- La app fuerza HTTPS (`UseHttpsRedirection`) y envía **HSTS** en producción.
- Detrás de un proxy inverso ya está `UseForwardedHeaders` (X-Forwarded-For/Proto),
  y va **antes** de `UseHttpsRedirection`. Ese orden importa: si el redirect corriera
  primero, un edge que termina TLS y reenvía HTTP provocaría un bucle de redirecciones.
- **En un PaaS** (Railway, Render, Fly) el edge tiene IPs dinámicas y no se pueden
  enumerar, así que `KnownProxies`/`KnownNetworks` quedan vacíos con `ForwardLimit = 1`:
  solo se honra el último hop, el que agrega el propio edge.
- **Sobre un proxy propio con IP fija** (nginx, Caddy en tu VM), restringir
  `KnownProxies` a esa IP en `Program.cs`. Es más estricto y conviene hacerlo.
- Terminar TLS en el proxy o en Kestrel con un certificado válido (no el de dev).

## 4. Email (Resend)

El envío de emails (recuperación de contraseña) usa **Resend**, detrás de la
abstracción `IEmailService` (se puede cambiar de proveedor sin tocar el resto).

1. **API key**: en producción, por variable de entorno `RESEND_API_KEY` (en dev,
   user-secrets `Resend:ApiToken`). Nunca en el repo. Conviene una key propia de prod.
2. **Dominio verificado**: en el panel de Resend → *Domains*, agregá tu dominio y
   cargá los registros DNS (SPF/DKIM/DMARC). Hasta verificarlo, con el remitente de
   prueba `onboarding@resend.dev` **solo se entrega al email de la cuenta de Resend**.
3. **Remitente**: una vez verificado el dominio, cambiá `Resend:From` en
   `appsettings.json` a una dirección **de ese dominio**:
   ```json
   "Resend": { "From": "Mical <no-responder@tudominio.com>" }
   ```
4. **URL del enlace de reset**: se arma sola con el host real del request
   (`Request.Scheme`/`Host`), así que respeta el dominio de producción siempre que
   el proxy inverso pase bien los forwarded headers (ver §3).

> El número de WhatsApp del botón flotante y de la confirmación de pedidos está en
> `Business:WhatsAppNumber` (`appsettings.json`, no sensible). Ajustalo al real.

## 5. Seguridad ya incluida en el código

- **Autenticación**: cookies `HttpOnly` + `Secure` + `SameSite=Lax`.
- **Autorización**: `/Admin` exige rol Administrador (política `AdminOnly`).
- **CSRF**: antiforgery global en todo POST/PUT/DELETE (`AutoValidateAntiforgeryToken`).
- **Fuerza bruta**: lockout de Identity (5 intentos / 15 min) + **rate limiting**
  por IP en login/registro (10/min).
- **Contraseñas**: hash PBKDF2 (Identity).
- **XSS**: Razor codifica por defecto; cookie de auth no accesible desde JS.
- **SQL Injection**: EF Core parametriza todo (incluida la búsqueda `ILIKE`).
- **Subida de archivos**: valida extensión/tipo/tamaño y regenera el nombre (GUID).
- **Headers**: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy.
- **Errores**: página amigable `/Home/Error` (sin stack traces al usuario).
- **Auditoría**: interceptor de `SaveChanges` registra acciones de admin en `AuditLogs`.

## 6. Antes de publicar

- [ ] `ASPNETCORE_ENVIRONMENT=Production` y secretos por variables de entorno.
- [ ] `AllowedHosts` = dominio real.
- [ ] `Storage__UploadsPath` apuntando al volumen persistente montado.
- [x] Migraciones: se aplican solas al arrancar (`Database.MigrateAsync()` en `Program.cs`).
- [ ] Cambiar la contraseña del admin inicial tras el primer login.
- [ ] Certificado TLS válido; HTTP redirige a HTTPS.
- [ ] `KnownProxies` restringidos **si el proxy tiene IP fija** (en un PaaS no aplica, ver §3).
- [ ] `RESEND_API_KEY` seteada, dominio verificado en Resend y `Resend:From` con una dirección del dominio.
- [ ] `Business:WhatsAppNumber` con el número real.
- [ ] Backups de la base y del volumen de imágenes (`Storage__UploadsPath`, products y promotions).
- [ ] Revisar logs (`logs/`) y rotación.
- [ ] Verificar `/sitemap.xml` y `/robots.txt` responden con el dominio real.

## 7. Pruebas de humo (flujo completo)

1. Home y `/shop` cargan; búsqueda predictiva y filtro por categoría funcionan.
2. Registro de un usuario nuevo → login.
3. Agregar al carrito → `/cart` refleja precio y stock del servidor.
4. Checkout autenticado → se crea el pedido, descuenta stock, muestra confirmación
   (y el botón de confirmación por WhatsApp).
5. `/order` (Mis pedidos) muestra el pedido; el detalle es solo del dueño; "Volver a pedir" funciona.
6. Admin: crear categoría y producto (con imagen → se guarda como WebP), destacar un
   producto, crear una promoción, cambiar estado de un pedido, cancelar y verificar
   reposición de stock (salvo Entregado).
7. Dashboard muestra métricas y el gráfico; `AuditLogs` registra las acciones de admin.
8. Rate limiting: muchos intentos de login seguidos → 429.
9. Recuperación de contraseña: *olvidé mi contraseña* → llega el email → el enlace
   abre el reset → nueva contraseña → login con la nueva.

---

## 8. Despliegue en Railway

La app se empaqueta con el `Dockerfile` de la raíz (multi-stage: SDK 8 para
compilar, runtime ASP.NET 8 para correr). Se usa un Dockerfile explícito en vez de
la autodetección de la plataforma para que el build no intente publicar el proyecto
de tests y la versión del runtime no cambie sola.

### Pasos

1. **Crear el proyecto** en Railway desde el repo de GitHub. Detecta el `Dockerfile`
   solo.
2. **Agregar PostgreSQL** (`+ New` → `Database` → `PostgreSQL`). Railway expone la
   variable `DATABASE_URL`; la app la convierte a formato Npgsql automáticamente.
   Referenciala desde el servicio web con `${{Postgres.DATABASE_URL}}`.
3. **Crear el volumen** (`+ Volume` en el servicio web) con punto de montaje
   `/data`. **Sin esto se pierden todas las imágenes en cada deploy.**
4. **Cargar las variables** del servicio web:

   | Variable | Valor |
   |---|---|
   | `DATABASE_URL` | `${{Postgres.DATABASE_URL}}` |
   | `Storage__UploadsPath` | `/data/uploads` |
   | `AdminSeed__Password` | una contraseña fuerte |
   | `RESEND_API_KEY` | la API key de Resend |
   | `AllowedHosts` | el dominio real |
   | `ASPNETCORE_ENVIRONMENT` | `Production` |

   `PORT` la inyecta Railway sola y la app la detecta; no hay que definirla.

5. **Deploy.** Al arrancar se aplican las migraciones y se siembra el admin.
6. **Dominio**: `Settings` → `Networking` → generar dominio o conectar uno propio.
   Actualizar `AllowedHosts` y el remitente de Resend al dominio final.

### Verificado localmente

Antes de este documento se corrió la imagen contra un PostgreSQL real reproduciendo
las condiciones de Railway (`PORT`, `DATABASE_URL` en formato URL, volumen montado):
home, `/shop`, `/sitemap.xml`, `/robots.txt`, `/account/login` y `/cart` responden 200,
`/Admin` anónimo redirige, las 9 migraciones se aplican solas y el admin queda sembrado.
Se destruyó y recreó el contenedor: **el archivo del volumen sobrevivió y el de
`wwwroot/uploads` desapareció**, que es justamente el motivo del volumen.

### Notas

- El contenedor corre como root para poder escribir en el volumen montado. El
  alcance está acotado al contenedor; si se endurece, hay que alinear el usuario
  con los permisos del punto de montaje.
- El volumen se ata a una sola instancia, así que el servicio no escala en
  horizontal tal como está. Para eso habría que mover las imágenes a object
  storage (S3/R2) detrás de `IFileStorageService`.
