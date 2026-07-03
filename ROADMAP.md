# ROADMAP — Mical (E-commerce MVC)

> Seguimiento vivo del desarrollo. Se actualiza al cerrar cada tarea.
> Arquitectura de referencia: [ANALISIS.md](ANALISIS.md).
> **Modo de trabajo: incremental. Una tarea a la vez, validando antes de continuar.**

**Leyenda:** ✅ hecho · 🔄 en progreso · ⬜ pendiente · ⏸️ bloqueado

**Última actualización:** 2026-07-03 — Fase 5 completada (Carrito: cart.js LocalStorage + UI /cart + endpoint de re-validación). Falta 1.4 (Google Login), pospuesta.

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
- [x] **2.2** Service + validación + Area Admin CRUD + auditoría. ✅
  - `Models/OperationResult.cs` (resultado de negocio) + VMs admin (`CategoryFormVm`, `AdminCategoryListItemVm`).
  - `Services/Interfaces/ICategoryService` + `Implementations/CategoryService`: CRUD, mapeo VM↔entidad, chequeo de unicidad **case-insensitive**, soft delete, timestamps (IAuditable) y logging de auditoría `[AUDIT]` con el usuario actor (vía `IHttpContextAccessor`). Registrado en `Extensions/ServiceCollectionExtensions.AddApplicationServices` + `AddHttpContextAccessor`.
  - `Areas/Admin/Controllers/CategoriesController` (hereda `AdminBaseController`) + vistas Index/Create/Edit/Delete (antiforgery, validación cliente, PRG con TempData). Sidebar admin habilita "Categorías".
  - Validación: DataAnnotations (cliente + básico) + unicidad en el service; el índice único parcial de PostgreSQL es la garantía final. *(FluentValidation se difiere a la Fase 3 para reglas ricas de producto: SKU/stock/imágenes.)*
  - Auditoría: por ahora vía log `[AUDIT]` (Serilog). La tabla `AuditLogs` + interceptor son la Fase 7.1.
  - Verificado en runtime como admin: crear/listar/editar/soft-delete OK, duplicado (distinto case) rechazado, query filter oculta la borrada, logs `[AUDIT]` con el actor.

## Fase 3 — Productos
- [x] **3.1** Entidad + relación con categoría + soft delete (global filter) + migración. ✅
  - `Entities/Product` (SKU único, precios `numeric(12,2)`, FK Categoría `Restrict`, stock/minStock, soft delete, props calculadas `EffectivePrice`/`IsOnSale`/`IsOutOfStock` ignoradas) + `ProductConfiguration` (índices `Sku` único, `CategoryId`, `(IsDeleted,IsActive)`, query filter). Migración `AddProduct` + secuencia `product_sku_seq`. Verificado en Postgres.
- [x] **3.2** Generador de SKU + subida/validación de imágenes. ✅
  - `ISkuGenerator`/`SkuGenerator`: `nextval('product_sku_seq')` → `PRD-2026-000001` (concurrencia segura, huecos aceptados). `IFileStorageService`/`FileStorageService`: valida extensión (jpg/png/webp), content-type y tamaño (≤2 MB), regenera nombre con GUID, guarda en `wwwroot/uploads/products`, borra la anterior al reemplazar.
- [x] **3.3** CRUD admin completo + auditoría. ✅
  - `ProductsController` + vistas Index/Create/Edit/Delete (multipart, thumbnails, SKU solo lectura en edición). FluentValidation (`ProductFormVmValidator`) auto + adaptadores cliente. Auditoría por log `[AUDIT]` con actor y SKU. Sidebar habilita Productos.
  - **Fixes durante validación**: (1) cultura invariante en `Program.cs` (`UseRequestLocalization`) para que los `<input type=number>` parseen decimales con punto — antes `15000.50` se guardaba como `1500050`; (2) `OverridePropertyName("SalePrice")` en el validador para que el error oferta<precio se muestre en el campo.
  - Verificado en runtime: alta con imagen + SKU correlativo, decimales correctos, validaciones (precio>0, oferta<precio, tipo de imagen), edición con swap de imagen (borra la vieja) y SKU inmutable, soft delete + query filter, logs de auditoría.

## Fase 4 — Catálogo público
- [x] **4.1** `/shop` con paginación y filtro por categoría. ✅
  - `Models/PagedResult<T>`; VMs `ProductCardVm`, `ShopIndexVm`+`CategoryFilterVm`; `ICatalogService`/`CatalogService` (solo productos activos de categorías activas). `ShopController` (`/shop?categoria=&q=&page=`, pageSize 12). Vista `Views/Shop/Index` (filtro por categoría, grilla de cards, paginación). Header: buscador → `/shop`, nav "Tienda".
- [x] **4.2** Búsqueda por nombre (ILIKE + índice trigram `pg_trgm`). ✅
  - Extensión `pg_trgm` + índice GIN `gin_trgm_ops` sobre `Products.Name` (migración `AddProductSearchIndex`). Búsqueda con `EF.Functions.ILike(Name, %q%)`.
- [x] **4.3** Detalle de producto + estado "Sin stock". ✅
  - `ProductController` (`/product/{id:int}`) + `ProductDetailVm` + vista `Views/Product/Details` (galería, precio/oferta, breadcrumb, botón "Agregar al carrito" deshabilitado hasta Fase 5; "Sin stock" si Stock≤0). Productos ocultos (inactivos o de categoría inactiva) → 404.
  - Verificado en runtime: listado (14 visibles, ocultos excluidos), paginación (2 págs), filtro por categoría, búsqueda trigram, detalle con oferta/sin-stock, 404 en ocultos/inexistentes.

## Fase 5 — Carrito (solo cliente)
- [x] **5.1** `cart.js` (LocalStorage) + UI de carrito. ✅
  - `wwwroot/js/cart.js`: API `window.Cart` (add/setQty/remove/clear/count/items) sobre LocalStorage (`mical_cart`, solo `{id,qty}`), badge `.js-cart-count` en el header, botón delegado `.js-add-to-cart` (con `data-product-id`/`data-qty-target`). Incluido en `_Layout`.
  - Detalle de producto: botón "Agregar al carrito" habilitado con selector de cantidad (deshabilitado si sin stock). Ícono del header enlaza a `/cart` con badge.
  - `Views/Cart/Index` + `CartController.Index`: la página se rellena por JS (fetch a `/cart/rehydrate`), muestra líneas con stepper de cantidad, quita ítems, avisa ajustes/no-disponibles, total, y botón de checkout deshabilitado (Fase 6).
- [x] **5.2** Endpoint server-side de re-hidratación/validación de precio y stock. ✅
  - `POST /cart/rehydrate` (`CartController`, `[IgnoreAntiforgeryToken]` porque es solo lectura sin datos privados) → `ICatalogService.RehydrateCartAsync`: resuelve precio efectivo del servidor (ignora cualquier precio del cliente), recorta cantidad al stock, marca no disponibles (inactivos/borrados/categoría inactiva), calcula total. `CartVm`/`CartLineVm`/`CartItemInput`.
  - Verificado en runtime: casos normal/ajuste-por-stock/sin-stock/inexistente, total correcto, resistencia a inyección de precio (DTO solo id+qty).
  - **Verificado en navegador real (Chrome headless vía CDP)**: agregar desde el detalle → badge sube, `/cart` renderiza por fetch, reconcilia (recorta al stock con aviso), cambio de cantidad recalcula total, quitar ítem. Todo OK.
- [ ] *(Futuro/opcional)* Migración a `ICartService` + tabla `CartItems`. ⬜

## Fase 6 — Checkout y pedidos
- [x] **6.1** Checkout autenticado + transacción + descuento de stock + concurrencia (anti-sobreventa). ✅
  - Entidades `Order`/`OrderItem` (snapshots de nombre y precio) + enum `OrderStatus` + configs (FK a AspNetUsers Restrict, OrderItems→Orders Cascade, →Products Restrict; OrderNumber único; índices). Secuencia `order_number_seq`. Token de concurrencia optimista `xmin` en Product (anti-sobreventa). Migración `AddOrders`.
  - `IOrderService.CheckoutAsync`: parsea el carrito del cliente, re-valida en el servidor (existencia/activo/stock), calcula precios/total autoritativos, **descuenta stock dentro de una transacción** con reintentos ante `DbUpdateConcurrencyException`. `GetForUserAsync` (detalle solo del dueño).
  - `CheckoutController` (`[Authorize]`, GET prellenado desde el perfil + POST) y `OrderController.Details` (dueño; `placed=true` → confirmación + limpia el carrito). Vistas `Checkout/Index` (form + resumen por JS desde LocalStorage, reconcilia y postea `CartJson`) y `Order/Details` (confirmación + badge de estado). Botón "Finalizar compra" del carrito → `/checkout`.
  - Verificado en runtime: anónimo→login, compra OK (pedido `ORD-2026-000001` Pendiente, items con snapshot, stock descontado 50→48 / 3→0), stock insuficiente→rechazo con rollback (stock intacto), carrito vacío→rechazo, aislamiento por usuario (404 al ajeno). *(La carrera de concurrencia simultánea no se orquestó por HTTP; xmin+retry implementados.)*
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
| 2026-07-03 | Fase 2.1 | Entidad `Category` + `CategoryConfiguration` (índice único parcial + query filter) + migración `AddCategory`. Verificado en Postgres. |
| 2026-07-03 | Fase 2.2 | `CategoryService` (CRUD, unicidad case-insensitive, soft delete, audit log) + CRUD admin de categorías. Verificado en runtime. |
| 2026-07-03 | Fase 3 | Productos: entidad+config+migración (3.1), `ISkuGenerator`+`IFileStorageService` (3.2), CRUD admin + FluentValidation (3.3). Fixes: cultura invariante (decimales) y OverridePropertyName. Verificado en runtime. |
| 2026-07-03 | Fase 4 | Catálogo público: `ICatalogService`, `/shop` (filtro+paginación+búsqueda `pg_trgm`), `/product/{id}` (detalle + "Sin stock"). Header enlaza a la tienda. Verificado en runtime. |
| 2026-07-03 | Fase 5 | Carrito: `cart.js` (LocalStorage) + `/cart` (UI por JS) + `POST /cart/rehydrate` (re-valida precio/stock server-side). Verificado endpoint + markup; JS cableado + browser real. |
| 2026-07-03 | Fase 6.1 | Checkout: entidades Order/OrderItem + `OrderService.CheckoutAsync` (transacción, descuento de stock, xmin) + `/checkout` + confirmación. Verificado en runtime. |
