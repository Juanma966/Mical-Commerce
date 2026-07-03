# Guía de despliegue a producción — Mical

Checklist para poner Mical en un entorno de producción. El código ya trae el
endurecimiento base (Fase 8); esta guía cubre la configuración del entorno.

## 1. Secretos y configuración (NUNCA en el repo)

En producción, estos valores se pasan por **variables de entorno** (no van en
`appsettings.Production.json`). ASP.NET Core los lee automáticamente:

| Variable de entorno | Descripción |
|---|---|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a PostgreSQL. |
| `AdminSeed__Password` | Contraseña del admin inicial (el seeder lo crea al arrancar). |
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
- Aplicar migraciones: `dotnet ef database update` (o generar script con
  `dotnet ef migrations script --idempotent` y correrlo en el pipeline de deploy).

## 3. HTTPS / proxy inverso

- La app fuerza HTTPS (`UseHttpsRedirection`) y envía **HSTS** en producción.
- Detrás de un proxy inverso (nginx, Caddy, etc.) ya está `UseForwardedHeaders`
  (X-Forwarded-For/Proto). **Restringir `KnownProxies`/`KnownNetworks`** a la IP
  del proxy (en `Program.cs`) para que no se puedan falsificar las cabeceras.
- Terminar TLS en el proxy o en Kestrel con un certificado válido (no el de dev).

## 4. Seguridad ya incluida en el código

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

## 5. Antes de publicar

- [ ] `ASPNETCORE_ENVIRONMENT=Production` y secretos por variables de entorno.
- [ ] `AllowedHosts` = dominio real.
- [ ] Migraciones aplicadas.
- [ ] Cambiar la contraseña del admin inicial tras el primer login.
- [ ] Certificado TLS válido; HTTP redirige a HTTPS.
- [ ] `KnownProxies` del proxy inverso configurados.
- [ ] Backups de la base y de `wwwroot/uploads/products`.
- [ ] Revisar logs (`logs/`) y rotación.

## 6. Pruebas de humo (flujo completo)

1. Home y `/shop` cargan; búsqueda y filtro por categoría funcionan.
2. Registro de un usuario nuevo → login.
3. Agregar al carrito → `/cart` refleja precio y stock del servidor.
4. Checkout autenticado → se crea el pedido, descuenta stock, muestra confirmación.
5. `/order` (Mis pedidos) muestra el pedido; el detalle es solo del dueño.
6. Admin: crear categoría y producto (con imagen), cambiar estado de un pedido,
   cancelar y verificar reposición de stock (salvo Entregado).
7. Dashboard muestra métricas; `AuditLogs` registra las acciones de admin.
8. Rate limiting: muchos intentos de login seguidos → 429.
