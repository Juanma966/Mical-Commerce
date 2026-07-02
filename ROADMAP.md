# ROADMAP — Mical (E-commerce MVC)

> Seguimiento vivo del desarrollo. Se actualiza al cerrar cada tarea.
> Arquitectura de referencia: [ANALISIS.md](ANALISIS.md).
> **Modo de trabajo: incremental. Una tarea a la vez, validando antes de continuar.**

**Leyenda:** ✅ hecho · 🔄 en progreso · ⬜ pendiente · ⏸️ bloqueado

**Última actualización:** 2026-07-02 — Fase 0.4 completada. **Fase 0 cerrada.**

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
- [ ] **1.1** ASP.NET Identity con cookies + entidad `ApplicationUser`. ⬜
- [ ] **1.2** Registro / Login / Logout / perfil / cambio de contraseña. ⬜
- [ ] **1.3** Roles (Administrador, Usuario) + `DbInitializer` (seed de roles y admin inicial). ⬜
- [ ] **1.4** Google Login. ⬜
- [ ] **1.5** Autorización por rol en `/Admin` + políticas. ⬜

## Fase 2 — Categorías (CRUD admin)
- [ ] **2.1** Entidad + configuración + migración. ⬜
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
