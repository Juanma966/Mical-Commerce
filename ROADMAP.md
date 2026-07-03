# ROADMAP — Mical (E-commerce MVC)

> Seguimiento vivo del desarrollo. Se actualiza al cerrar cada tarea.
> Arquitectura de referencia: [ANALISIS.md](ANALISIS.md).
> **Modo de trabajo: incremental. Una tarea a la vez, validando antes de continuar.**

**Leyenda:** ✅ hecho · 🔄 en progreso · ⬜ pendiente · ⏸️ bloqueado

**Última actualización:** 2026-07-03 — Fase 1.5 completada (autorización por rol en `/Admin` + política `AdminOnly`). Falta 1.4 (Google Login), pospuesta.

---

## Fase 0 — Esqueleto e infraestructura
- [x] **0.1** Crear proyecto MVC `Mical` (namespace `Mical`) + estructura de carpetas. ✅
  - Plantilla `MiniStore-1.0.0/` renombrada a `frontend/`.
  - Proyecto creado en `Mical/`, build OK.
  - Git inicializado + `.gitignore` (estándar .NET + uploads + secrets).
- [x] **0.2** Integrar frontend MiniStore: assets a `wwwroot/`, partir `index.html` en `_Layout.cshtml` + `Home/Index`. ✅
  - Assets copiados a `wwwroot/` (images, css, js de la plantilla).
  - `_Layout.cshtml` reconstruido + partials `_IconSprite`, `_Header`, `_Footer`.
  - `Home/Index.cshtml` con todas las secciones, paths a `~/`, textos al español.
  - Verificado en runtime: home HTTP 200, assets 200, secciones presentes. Build OK.
- [x] **0.3** PostgreSQL con Docker + EF Core + Npgsql + `ApplicationDbContext` vacío. Connection string por env/user-secrets. ✅
  - Docker Desktop instalado; `docker-compose.yml` con `postgres:16-alpine` (volumen `mical-pgdata`, healthcheck). Contenedor `mical-postgres` healthy (PostgreSQL 16.14).
  - Credenciales en `.env` (gitignored) + `.env.example` versionado.
  - Paquetes `Npgsql.EntityFrameworkCore.PostgreSQL` y `EntityFrameworkCore.Design` 8.0.10.
  - `Data/ApplicationDbContext.cs` (vacío, con `ApplyConfigurationsFromAssembly`). Registrado en `Program.cs` con `UseNpgsql`.
  - Connection string en user-secrets. Verificado con `dotnet ef dbcontext info` (provider + base + data source OK). `dotnet-ef` 8.0.10 instalado.
- [x] **0.4** Logging (Serilog) + manejo centralizado de errores + página de error + headers HTTP seguros. ✅
  - Serilog (consola + archivo con rotación diaria en `logs/`, `logs/` gitignored) + request logging.
  - Manejo de errores: developer page en dev, `UseExceptionHandler` en prod, `UseStatusCodePagesWithReExecute` → `/Home/Error` amigable (404/403/500 con título y mensaje).
  - `UseSecurityHeaders` (X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy) + HSTS en prod.
  - Verificado en runtime: home 200 con headers, 404 → página amigable, logs en consola y archivo.

## Fase 1 — Identidad y seguridad base
- [x] **1.1** ASP.NET Identity con cookies + entidad `ApplicationUser`. ✅
  - Paquete `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.10.
  - `Entities/ApplicationUser.cs : IdentityUser` (+ `FullName`, `CreatedAt`).
  - `ApplicationDbContext` ahora hereda de `IdentityDbContext<ApplicationUser>`.
  - `Program.cs`: `AddIdentity` (password: 8+ con dígito/mayús/minús; lockout 5 intentos/15 min; email único) + `ConfigureApplicationCookie` (HttpOnly, Secure=Always, SameSite=Lax, sliding 14d, rutas `/account/login|logout|denied`) + `UseAuthentication()` antes de `UseAuthorization()`.
  - Migración `AddIdentity` creada y aplicada. Verificado: 7 tablas `AspNet*` + columnas `FullName`/`CreatedAt` en `AspNetUsers`. Build OK.
- [x] **1.2** Registro / Login / Logout / perfil / cambio de contraseña. ✅
  - ViewModels en `ViewModels/`: `RegisterVm`, `LoginVm`, `ProfileVm`, `ChangePasswordVm` (DataAnnotations en español).
  - `AccountController`: Register, Login, Logout (POST+antiforgery), Profile (ver/editar `FullName`+`PhoneNumber`; email solo lectura), ChangePassword, Denied. `RedirectToLocal` con `Url.IsLocalUrl` (anti open-redirect); login con `lockoutOnFailure:true` y mensaje genérico; errores de Identity traducidos.
  - Vistas Bootstrap en `Views/Account/` + validación cliente (`_ValidationScriptsPartial`). Header con ícono usuario → Perfil/Login + botón Salir según sesión.
  - Verificado en runtime (curl, https): registro→auto-login→home, `/Profile` protegido (302 a login si anónimo), login ok/fallido, registro duplicado, logout, cambio de contraseña + re-login con clave nueva. Usuario de prueba borrado.
- [x] **1.3** Roles (Administrador, Usuario) + `DbInitializer` (seed de roles y admin inicial). ✅
  - `Helpers/Roles.cs` (constantes `Administrador`/`Usuario` + `All`).
  - `Data/Seed/DbInitializer.cs`: crea roles y admin inicial de forma idempotente. Credenciales del admin fuera del código: `AdminSeed:Email`/`AdminSeed:FullName` en `appsettings.json`, `AdminSeed:Password` en user-secrets (dev). Admin creado con `EmailConfirmed=true`. Invocado en `Program.cs` en un scope antes de `app.Run()`.
  - Registro público asigna rol `Usuario` automáticamente.
  - Verificado en runtime: seeder crea roles + `admin@mical.com` (rol Administrador); registro de `cliente@test.com` → rol Usuario. Test user borrado; admin conservado.
- [ ] **1.4** Google Login. ⬜ *(pospuesta; se hará después de 1.5)*
- [x] **1.5** Autorización por rol en `/Admin` + políticas. ✅
  - `Helpers/Policies.cs` (`AdminOnly`) + `AddAuthorization` con `RequireRole(Administrador)` en `Program.cs`.
  - Área Admin: `AdminBaseController` (`[Area("Admin")]` + `[Authorize(Policy = AdminOnly)]`) del que heredan todos los controladores del panel; `DashboardController` mínimo. Vistas del área (`_ViewImports`/`_ViewStart`/`Shared/_AdminLayout` con sidebar + `Dashboard/Index`).
  - Ruta de áreas registrada antes de la default: `/Admin` → `Dashboard/Index`.
  - Verificado en runtime: anónimo→login, usuario `Usuario`→`/account/denied` (Acceso denegado), admin→200 con el panel.

## Fase 2 — Categorías (CRUD admin)
- [x] **2.1** Entidad + configuración + migración. ✅
  - Interfaces `Entities/Common/IAuditable` (CreatedAt/UpdatedAt) e `ISoftDeletable` (IsDeleted) para reutilizar.
  - `Entities/Category.cs` (Name único, Description, ImagePath, IsActive, timestamps, soft delete).
  - `Data/Configurations/CategoryConfiguration.cs`: max lengths, defaults, índice único de `Name` filtrado por `IsDeleted=false`, query filter global de soft delete. `DbSet<Category>` en el contexto.
  - Migración `AddCategory` aplicada. Verificado en Postgres: tabla + índice único parcial OK.
- [ ] **2.2** Service + validación + Area Admin CRUD + auditoría. ⬜

## Fase 3 — Productos
- [ ] **3.1** Entidad + relación con categoría + soft delete (global filter) + migración. ⬜
- [ ] **3.2** Generador de SKU (`ISkuGenerator`, no editable, formato `PRD-2026-000123`) + subida/validación de imágenes (`IFileStorageService`). ⬜
- [ ] **3.3** CRUD admin completo + auditoría. ⬜

## Fase 4 — Catálogo público
- [ ] **4.1** `/shop` con paginación y filtro por categoría. ⬜
- [ ] **4.2** Búsqueda por nombre (ILIKE + índice trigram `pg_trgm`). ⬜
- [ ] **4.3** Detalle de producto + estado "Sin stock". ⬜

## Fase 5 — Carrito (solo cliente)
- [ ] **5.1** `cart.js` (LocalStorage) + UI de carrito. ⬜
- [ ] **5.2** Endpoint server-side de re-hidratación/validación de precio y stock. ⬜
- [ ] *(Futuro/opcional)* Migración a `ICartService` + tabla `CartItems`. ⬜

## Fase 6 — Checkout y pedidos
- [ ] **6.1** Checkout autenticado + transacción + descuento de stock + concurrencia (anti-sobreventa). ⬜
- [ ] **6.2** Historial y detalle de pedidos (usuario). ⬜
- [ ] **6.3** Gestión de pedidos y cambio de estado (admin) + reposición de stock al cancelar (solo si NO estaba Entregado). ⬜

## Fase 7 — Auditoría y dashboard
- [ ] **7.1** Interceptor de auditoría conectado, **solo acciones de admin** (expandir solo si hace falta). ⬜
- [ ] **7.2** Dashboard admin con métricas (pedidos por estado, productos bajo stock mínimo, ventas recientes). ⬜

## Fase 8 — Endurecimiento para producción
- [ ] **8.1** `appsettings.Production.json`, secretos por entorno, HTTPS/HSTS. ⬜
- [ ] **8.2** Revisión de validaciones, antiforgery (CSRF), rate limiting en login + lockout por intentos fallidos. ⬜
- [ ] **8.3** Checklist de producción + pruebas de humo del flujo completo. ⬜

---

## Bitácora
| Fecha | Tarea | Nota |
|---|---|---|
| 2026-06-30 | Análisis/arquitectura | `ANALISIS.md` aprobado (con correcciones: sin JWT, carrito incremental, auditoría solo admin, SKU no editable, cancelación condicionada). |
| 2026-06-30 | .NET 8 SDK | Instalado (8.0.422) vía winget. |
| 2026-06-30 | Fase 0.1 | Proyecto MVC `Mical` creado, estructura de carpetas, git + `.gitignore`. Build OK. |
| 2026-07-01 | Fase 0.2 | Frontend MiniStore integrado: layout + partials (`_IconSprite`, `_Header`, `_Footer`) + `Home/Index`. Verificado en runtime (HTTP 200). |
| 2026-07-02 | Fase 0.3 | PostgreSQL 16 en Docker (`mical-postgres`) + EF Core/Npgsql + `ApplicationDbContext` + user-secrets. Verificado con `dotnet ef dbcontext info`. |
| 2026-07-02 | Fase 0.4 | Serilog (consola+archivo) + request logging + página de error amigable + headers de seguridad. Verificado en runtime. **Fase 0 cerrada.** |
| 2026-07-03 | Fase 1.1 | ASP.NET Identity (cookies) + `ApplicationUser` + `IdentityDbContext`. Migración `AddIdentity` aplicada; 7 tablas `AspNet*` verificadas en Postgres. |
| 2026-07-03 | Fase 1.2 | `AccountController` + ViewModels + vistas: registro/login/logout/perfil/cambio de contraseña. Flujo completo verificado en runtime con curl. |
| 2026-07-03 | Fase 1.3 | Roles + `DbInitializer` (seed idempotente de roles y admin). Registro asigna rol Usuario. Verificado en runtime + Postgres. |
| 2026-07-03 | Fase 1.5 | Área Admin protegida: política `AdminOnly`, `AdminBaseController`, `DashboardController` + layout. Ruta de áreas. Verificado (anónimo/usuario/admin). 1.4 pospuesta. |
