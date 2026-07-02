# MiCal — Análisis y Arquitectura (E-commerce MVC)

> Documento de análisis previo a la implementación. **No contiene código.**
> Stack: ASP.NET Core 8 MVC · EF Core · PostgreSQL · ASP.NET Identity (cookies) · Google Login · FluentValidation.
> **Autenticación exclusivamente por cookies de Identity. NO se usa JWT en ninguna parte del sistema.**
> Principio rector: **simplicidad, legibilidad, rapidez y mantenibilidad. Sin sobreingeniería.**

---

## 0. Decisiones de arquitectura (y por qué)

| Decisión | Elección | Justificación |
|---|---|---|
| Tipo de app | **Monolito MVC, un solo proyecto** | Server-rendered. Un solo despliegue, un solo modelo mental. Lo pedido. |
| Autenticación | **Cookies de Identity (JWT eliminado por completo del stack)** | En MVC las vistas viajan desde el servidor; la cookie `HttpOnly+Secure` es nativa, no expone tokens a JS (mitiga XSS) y no requiere lógica de tokens. No hay API ni SPA desacoplada, por lo que JWT no aporta nada: **se descarta totalmente** (sin refresh tokens, sin `Bearer`, sin `JwtBearer` middleware). Google Login se integra como esquema OAuth externo sobre la misma cookie. |
| Acceso a datos | **Capa de Services + EF Core directo** (repositorio genérico **opcional y mínimo**) | EF Core's `DbContext` **ya es** Unit of Work y `DbSet` ya es repositorio. Envolverlo en Repository<T>+UnitOfWork completos sería sobreingeniería. Los `Services` concentran la lógica de negocio y son testeables. Si más adelante un agregado lo amerita, se agrega un repositorio puntual. |
| Soft Delete | **Global Query Filter** en EF Core | Un filtro `IsDeleted == false` por entidad evita repetir `Where` en cada consulta y es imposible de olvidar. |
| Auditoría | **Empezar SOLO con acciones de ADMIN** (Interceptor de `SaveChanges`) | Arranca registrando únicamente las operaciones de escritura del panel admin (productos, categorías, cambios de estado de pedidos). Se expande a más entidades/acciones **solo si hace falta**. Centralizado, sin ensuciar cada Service. |
| Carrito | **Fase 1: solo `cart.js` (LocalStorage). Fase 2 (si se decide): `ICartService` + tabla `CartItems`** | Arranca 100% en cliente, sin backend de carrito. El precio/stock se valida siempre en servidor al confirmar. La migración a DB es un agregado posterior que no toca el checkout. |
| Validación | **DataAnnotations (simple) + FluentValidation (reglas ricas)** | DataAnnotations para lo trivial y validación cliente automática; FluentValidation para reglas de negocio (SKU, stock, imágenes). |
| Búsqueda | **PostgreSQL `ILIKE` + índice GIN `pg_trgm`** | Búsqueda parcial case-insensitive eficiente para ~400 productos y escalable a miles. |

---

## 1. Estructura de carpetas recomendada

Un único proyecto llamado **`Mical`** (namespace raíz `Mical`). Mantengo tu propuesta y la afino (agrupo por responsabilidad, no por tipo cuando ayuda):

```
Mical/                          # proyecto MVC (namespace Mical)
├── Areas/
│   └── Admin/
│       ├── Controllers/      # DashboardController, ProductsController, CategoriesController, OrdersController, UsersController
│       ├── Models/           # ViewModels propios del panel admin
│       └── Views/
│           ├── Dashboard/
│           ├── Products/
│           ├── Categories/
│           ├── Orders/
│           ├── Users/
│           └── _ViewImports / _ViewStart / Shared/_AdminLayout.cshtml
├── Controllers/              # Público: Home, Shop, Product, Cart, Checkout, Account, Order(historial)
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/       # IEntityTypeConfiguration<T> por entidad (Fluent API)
│   ├── Interceptors/         # AuditSaveChangesInterceptor
│   └── Seed/                 # DbInitializer (roles, admin inicial, categorías demo)
├── Entities/                 # Modelos de dominio/persistencia (Product, Category, Order, ...)
├── Models/                   # Tipos transversales: PagedResult<T>, OperationResult, enums sueltos
├── ViewModels/               # ViewModels del sitio público (no del admin)
├── Services/
│   ├── Interfaces/           # IProductService, ICategoryService, IOrderService, ICartService, IFileStorageService, ISkuGenerator, IAuditService
│   └── Implementations/
├── Repositories/             # (Opcional) IRepository<T> genérico + GenericRepository<T>. Vacío al inicio.
├── Validators/               # FluentValidation validators
├── Helpers/                  # SlugHelper, ImageValidationHelper, constantes
├── Extensions/               # ServiceCollectionExtensions (DI), QueryableExtensions (paginación)
├── Mappings/                 # Perfiles de AutoMapper (si se decide usar; ver nota)
├── Views/                    # Vistas públicas + Shared/_Layout.cshtml (adaptado de MiniStore)
├── wwwroot/
│   ├── css/  js/  images/    # assets actuales de la plantilla
│   └── uploads/products/     # imágenes de productos subidas
├── appsettings.json
├── appsettings.Production.json
└── Program.cs
```

**Justificación de cambios sobre tu lista:**
- **`ViewModels/` separado de `Models/`**: `Models/` queda para tipos transversales (paginación, resultados); `ViewModels/` para lo que consume cada vista. Evita un cajón de sastre.
- **`Data/Configurations/`**: mover el mapeo Fluent API fuera del `DbContext` mantiene el contexto legible y cada entidad con su configuración aislada.
- **`Validators/` y `Mappings/`** explícitos: descubribilidad inmediata.
- **AutoMapper opcional**: con pocos ViewModels, el mapeo manual es más legible y depurable. Recomiendo **empezar sin AutoMapper** y agregarlo solo si la repetición duele. (Lo dejo como decisión reversible.)
- **`Repositories/` casi vacío**: presente por si se necesita, pero no se fuerza su uso. Evitamos la sobreingeniería.

---

## 2. Modelo de base de datos

Tablas propias del dominio + tablas estándar de Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, etc.). Solo creo lo necesario hoy.

### Tablas

**`Categories`**
| Columna | Tipo | Notas |
|---|---|---|
| Id | int PK identity | |
| Name | varchar(100) | requerido, **único** |
| Description | varchar(500) | nullable |
| ImagePath | varchar(255) | nullable (solo si la plantilla lo usa) |
| IsActive | bool | default true |
| CreatedAt | timestamptz | |
| UpdatedAt | timestamptz | nullable |
| IsDeleted | bool | default false (soft delete) |

**`Products`**
| Columna | Tipo | Notas |
|---|---|---|
| Id | int PK identity | |
| Sku | varchar(30) | **único**, autogenerado, **no editable** (ver §3.1) |
| Name | varchar(150) | requerido |
| Description | text | nullable |
| Price | numeric(12,2) | > 0 |
| SalePrice | numeric(12,2) | nullable, < Price |
| CategoryId | int FK → Categories.Id | requerido |
| ImagePath | varchar(255) | ruta relativa en uploads/products |
| Stock | int | >= 0 |
| MinStock | int | >= 0, default 0 |
| IsActive | bool | default true |
| CreatedAt | timestamptz | |
| UpdatedAt | timestamptz | nullable |
| IsDeleted | bool | default false |

**`Orders`**
| Columna | Tipo | Notas |
|---|---|---|
| Id | int PK identity | |
| OrderNumber | varchar(20) | **único**, legible (ej. `ORD-2026-000123`) |
| UserId | varchar FK → AspNetUsers.Id | requerido |
| Status | int (enum) | Pendiente…Cancelado |
| Total | numeric(12,2) | calculado del servidor |
| ContactName | varchar(150) | snapshot al momento de compra |
| ContactPhone | varchar(30) | |
| ShippingAddress | varchar(300) | datos de envío embebidos (sin tabla aparte por ahora) |
| CreatedAt | timestamptz | |
| UpdatedAt | timestamptz | nullable |

**`OrderItems`**
| Columna | Tipo | Notas |
|---|---|---|
| Id | int PK identity | |
| OrderId | int FK → Orders.Id | cascade delete |
| ProductId | int FK → Products.Id | restrict (no borrar producto con historial) |
| ProductName | varchar(150) | **snapshot** (el nombre puede cambiar luego) |
| UnitPrice | numeric(12,2) | **snapshot** del precio cobrado |
| Quantity | int | > 0 |
| LineTotal | numeric(12,2) | UnitPrice × Quantity |

**`AuditLogs`**
| Columna | Tipo | Notas |
|---|---|---|
| Id | bigint PK identity | |
| UserId | varchar | nullable (acciones del sistema) |
| UserName | varchar(256) | snapshot legible |
| Action | varchar(20) | Created / Updated / Deleted |
| EntityName | varchar(100) | ej. "Product" |
| EntityId | varchar(50) | clave de la entidad afectada |
| Timestamp | timestamptz | |
| Details | text | nullable (resumen del cambio) |

### Relaciones, claves e índices

```
AspNetUsers 1───∞ Orders            (UserId)
Orders      1───∞ OrderItems        (OrderId, cascade)
Products    1───∞ OrderItems        (ProductId, restrict)
Categories  1───∞ Products          (CategoryId, restrict)
```

- **Únicos**: `Products.Sku`, `Categories.Name`, `Orders.OrderNumber`.
- **Índices**: `Products.CategoryId`, `Products(IsDeleted, IsActive)` (filtro de catálogo), **GIN `pg_trgm` sobre `Products.Name`** (búsqueda ILIKE), `Orders.UserId`, `Orders.Status`, `OrderItems.OrderId`, `AuditLogs.Timestamp`.
- **Borrado**: `OrderItems` → `Orders` en cascada; `Products`/`Categories` con `Restrict` para no romper historial. El soft delete cubre el "borrado" funcional de productos/categorías.

---

## 3. Entidades (modelo de dominio)

Conceptual, sin código:

- **`ApplicationUser : IdentityUser`** — agrega `FullName`, `CreatedAt`. Resto lo aporta Identity (email, hash de password, lockout, logins externos).
- **`Category`** — raíz simple; expone colección `Products`.
- **`Product`** — núcleo del catálogo; pertenece a una `Category`; propiedad calculada `IsOutOfStock => Stock <= 0` y `EffectivePrice` (SalePrice ?? Price) resueltas en la vista/VM, no en BD.
- **`Order`** — agregado de compra; contiene `OrderItems`; el `Status` se gobierna por la máquina de estados (ver §7).
- **`OrderItem`** — línea con **snapshot** de nombre y precio (inmutable ante cambios futuros del producto).
- **`AuditLog`** — registro append-only escrito por el interceptor (al inicio, solo acciones de admin).
- **Enum `OrderStatus`** = Pendiente, Pagado, Preparando, Enviado, Entregado, Cancelado.

### 3.1 Regla del SKU
- **Único** y **no editable**: se genera al crear el producto y nunca cambia (ni siquiera el admin lo edita; en el formulario se muestra solo lectura).
- **No depende del Id** (puede incluirlo, pero no se deriva de él): el Id es un detalle interno de persistencia; acoplar el SKU al Id rompería si se migran datos o se reordenan claves.
- **Formato propuesto**:
  - `PRD-2026-000123` → prefijo + año + secuencia correlativa rellenada a 6 dígitos (recomendado para productos).
  - `CAT-000123` → variante corta sin año (útil si se quisiera codificar por categoría).
- **Generación**: un `ISkuGenerator` produce el correlativo de forma segura ante concurrencia (secuencia de PostgreSQL o tabla de contador transaccional), de modo que dos altas simultáneas no colisionen. El SKU resultante se persiste como columna propia (no se calcula al vuelo).

Interfaces transversales sugeridas (para soft delete y auditoría automática): `ISoftDeletable` (IsDeleted) y `IAuditable` (CreatedAt/UpdatedAt). El `DbContext`/interceptor las usa para aplicar comportamiento sin repetir lógica.

---

## 4. ViewModels necesarios

**Público:**
- `ProductCardVm` (listado/grilla): Id, Name, ImagePath, EffectivePrice, Price, IsOnSale, IsOutOfStock.
- `ProductDetailVm`: lo anterior + Description, Sku, Stock, CategoryName.
- `ShopIndexVm`: lista paginada (`PagedResult<ProductCardVm>`) + filtros (categoría, término de búsqueda) + datos de paginación.
- `CartLineVm` / `CartVm`: reflejo del carrito de LocalStorage para la vista de checkout (re-validado en servidor).
- `CheckoutVm`: datos de contacto/envío + resumen de items.
- `OrderHistoryVm` / `OrderDetailVm`: para "Mis pedidos".
- `RegisterVm`, `LoginVm`, `ProfileVm`, `ChangePasswordVm`.

**Admin (`Areas/Admin/Models`):**
- `AdminProductListVm`, `ProductFormVm` (con `IFormFile Image`), `AdminCategoryListVm`, `CategoryFormVm`.
- `AdminOrderListVm`, `AdminOrderDetailVm`, `UpdateOrderStatusVm`.
- `AdminUserListVm`, `AdminUserDetailVm`.
- `DashboardVm`: contadores (pedidos por estado, productos bajo stock mínimo, ventas recientes).

Los **FormVm de admin nunca exponen la entidad directamente** (evita over-posting). El mapeo VM↔entidad va en el Service.

---

## 5. Navegación completa del sitio (site map)

**Sitio público:**
```
/                       Home (hero + destacados, adaptado de MiniStore)
/shop                   Catálogo con filtros, búsqueda y paginación
/shop?categoria=&q=     Búsqueda/filtro
/product/{id}           Detalle de producto
/cart                   Carrito (render desde LocalStorage)
/checkout               Confirmar compra (requiere login)
/account/login          Login (form + botón Google)
/account/register       Registro
/account/profile        Editar perfil / cambiar contraseña
/orders                 Historial de pedidos (mis pedidos)
/orders/{id}            Detalle de pedido
/error                  Página de error amigable
```

**Panel admin (`/Admin`, requiere rol Administrador):**
```
/Admin                          Dashboard
/Admin/Products                 Listado + buscador
/Admin/Products/Create
/Admin/Products/Edit/{id}
/Admin/Products/Delete/{id}     Soft delete (confirmación)
/Admin/Categories               CRUD categorías
/Admin/Orders                   Pedidos (todos)
/Admin/Orders/{id}              Detalle + cambiar estado
/Admin/Users                    Clientes/usuarios
/Admin/Users/{id}               Detalle de usuario
```

---

## 6. Flujo de compra

1. Usuario navega `/shop`, agrega productos → `cart.js` guarda `{productId, qty}` en **LocalStorage** (no precios; el precio se resuelve siempre en servidor).
2. Va a `/cart` → JS pide al servidor los datos actuales de esos productos (precio, stock, disponibilidad) y arma el resumen.
3. Click "Finalizar compra" → si **no está autenticado**, redirige a `/account/login?returnUrl=/checkout`.
4. En `/checkout` (GET), el servidor **re-valida**: existencia, `IsActive`, stock suficiente, y **recalcula precios** (ignora cualquier precio del cliente).
5. POST checkout dentro de **una transacción**:
   - Verifica stock fila por fila (optimistic concurrency con columna de versión para evitar sobreventa).
   - Crea `Order` (estado **Pendiente**) + `OrderItems` con snapshots.
   - Como el pago inicial es Transferencia/Contado, el pedido queda **Pendiente** hasta que el admin lo marque **Pagado**.
6. Confirmado → limpia LocalStorage, muestra `/orders/{id}` con número de pedido.
7. **Descuento de stock**: se aplica al **confirmar el pedido** (creación de la Order), atómicamente en la misma transacción. Si un producto queda en 0, sigue visible con etiqueta **"Sin stock"** y deshabilita "Agregar al carrito".

> Nota de diseño del carrito: como nunca se confía en datos del cliente para precio/stock, migrar a tabla `CartItems` luego es transparente para el checkout (solo cambia el origen de los datos, no las reglas).

---

## 7. Flujo del administrador

- **Login** → si tiene rol Administrador, accede a `/Admin`.
- **Dashboard**: pedidos por estado, productos bajo `MinStock`, últimas ventas.
- **Productos**: crear (SKU autogenerado, subir imagen validada), editar, soft-delete. Toda acción genera `AuditLog`.
- **Categorías**: CRUD; al desactivar una categoría, sus productos dejan de listarse en el catálogo público.
- **Pedidos**: ver todos, abrir detalle, **cambiar estado** según máquina de estados:
  ```
  Pendiente → Pagado → Preparando → Enviado → Entregado
        ↘ Cancelado (desde cualquier estado)
           · repone stock SOLO si el estado previo NO era Entregado
  ```
  Transiciones inválidas se rechazan en el Service. **Cancelar repone el stock solo si el pedido NO estaba `Entregado`** (un pedido entregado ya consumió el stock físicamente; cancelarlo es un caso administrativo que no debe reinflar inventario).
- **Usuarios/Clientes**: listar, ver detalle, (futuro) activar/desactivar.
- Cada operación de escritura queda auditada: *"Juan modificó el precio del producto Mate Imperial."*

---

## 8. Roadmap de implementación (incremental, una tarea a la vez)

> Trabajaremos **fase por fase, validando cada etapa antes de continuar**. No se implementan módulos en paralelo.

**Fase 0 — Esqueleto e infraestructura**
- 0.1 Crear proyecto MVC `Mical` (namespace `Mical`) + estructura de carpetas.
- 0.2 Mover assets de MiniStore a `wwwroot/`, partir `index.html` en `_Layout` + Home.
- 0.3 **PostgreSQL con Docker**: instalar Docker Desktop (prerequisito, aún no instalado), crear `docker-compose.yml` con un servicio `postgres` (volumen persistente, puerto, credenciales). Conectar EF Core + Npgsql + `ApplicationDbContext` vacío. Connection string vía variables de entorno / user-secrets (fuera del repo).
- 0.4 Logging (Serilog) + manejo centralizado de errores + página `/error`. Headers HTTP seguros.

**Fase 1 — Identidad y seguridad base**
- 1.1 ASP.NET Identity con cookies + entidad `ApplicationUser`.
- 1.2 Registro / Login / Logout / perfil / cambio de contraseña.
- 1.3 Roles (Administrador, Usuario) + `DbInitializer` (seed de roles y admin inicial).
- 1.4 Google Login.
- 1.5 Autorización por rol en `/Admin` + políticas.

**Fase 2 — Categorías (CRUD admin)**
- 2.1 Entidad + configuración + migración.
- 2.2 Service + validación + Area Admin CRUD + auditoría.

**Fase 3 — Productos**
- 3.1 Entidad + relación con categoría + soft delete (global filter) + migración.
- 3.2 Generador de SKU + subida/validación de imágenes (`IFileStorageService`).
- 3.3 CRUD admin completo + auditoría.

**Fase 4 — Catálogo público**
- 4.1 `/shop` con paginación y filtro por categoría.
- 4.2 Búsqueda por nombre (ILIKE + índice trigram).
- 4.3 Detalle de producto + estado "Sin stock".

**Fase 5 — Carrito (solo cliente)**
- 5.1 `cart.js` (LocalStorage) + UI de carrito.
- 5.2 Endpoint server-side de re-hidratación/validación de precio y stock (sin tabla de carrito todavía).
- *(Futuro, opcional)* Migración a `ICartService` + tabla `CartItems` solo si se decide persistir el carrito.

**Fase 6 — Checkout y pedidos**
- 6.1 Checkout autenticado + transacción + descuento de stock + concurrencia.
- 6.2 Historial y detalle de pedidos (usuario).
- 6.3 Gestión de pedidos y cambio de estado (admin) + reposición de stock al cancelar.

**Fase 7 — Auditoría y dashboard**
- 7.1 Interceptor de auditoría conectado, registrando **solo acciones de admin** (productos, categorías, estados de pedido). Expandir después solo si hace falta.
- 7.2 Dashboard admin con métricas.

**Fase 8 — Endurecimiento para producción**
- 8.1 `appsettings.Production.json`, secretos por entorno, HTTPS/HSTS.
- 8.2 Revisión de validaciones, antiforgery (CSRF), rate limiting básico en login.
- 8.3 Checklist de producción y pruebas de humo del flujo completo.

---

## Anexo — Cómo se cubre cada requisito de seguridad

| Requisito | Mecanismo |
|---|---|
| SQL Injection | EF Core parametriza todo; `ILIKE` vía LINQ/`EF.Functions`, nunca SQL concatenado. |
| XSS | Razor codifica por defecto; nunca `Html.Raw` con input de usuario; cookie auth `HttpOnly`. |
| CSRF | Antiforgery token en todos los POST (automático en forms Razor). |
| Validación cliente/servidor | DataAnnotations + jQuery Validation (cliente) y FluentValidation (servidor, autoridad final). |
| Password hashing | ASP.NET Identity (PBKDF2). |
| Archivos subidos | `IFormFile` validado: extensión (jpg/png/webp), content-type, tamaño máx., nombre regenerado (GUID), guardado solo en `uploads/products`. |
| Roles/Autorización | `[Authorize(Roles="Administrador")]` en Area Admin + políticas. |
| Errores | Middleware centralizado + página `/error`; sin stack traces al usuario. |
| Logging/Auditoría | Serilog + interceptor de `SaveChanges`. |
| Secretos | Connection string y claves de Google por variables de entorno / user-secrets en dev. |
| Producción | `appsettings.Production.json`, HSTS/HTTPS, rate limiting en login, bloqueo por intentos fallidos (Identity lockout). |

---

### Decisiones reversibles (las dejo señaladas para revisarlas si hace falta)
- **AutoMapper**: empezar sin él; incorporar solo si el mapeo manual se vuelve repetitivo.
- **Repositorio genérico**: presente pero sin uso forzado; se adopta puntualmente si un agregado lo justifica.
- **Datos de envío embebidos en `Order`**: si más adelante se requieren múltiples direcciones por usuario, se extrae a tabla `Addresses` sin afectar pedidos existentes.
```
