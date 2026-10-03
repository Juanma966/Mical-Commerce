# CLAUDE.md

Guía para Claude Code (claude.ai/code) al trabajar en este repositorio.

## Qué es esto

**Mical** es un e-commerce de regalería/imprenta: una app **ASP.NET Core 8 MVC**,
monolito de **un solo proyecto**, con **PostgreSQL** vía EF Core. Todo el código vive
en `Mical/`.

Nació de la plantilla estática *MiniStore*, pero esa plantilla **ya no existe como
carpeta**: su HTML se partió en vistas Razor y sus assets están en `Mical/wwwroot/`.
No queda nada estático que abrir en el navegador.

Decisiones de arquitectura ya tomadas (no replantearlas sin motivo):

- **Sin JWT, sin API separada, sin Clean Architecture multi-proyecto.** Auth solo con
  cookies de ASP.NET Identity (email + contraseña; no hay login con proveedores externos).
- **Sin sobreingeniería.** Capa de Services + EF Core directo. No hay repositorio genérico.
- El carrito vive en **LocalStorage** (`cart.js`), no en la base. El servidor revalida
  precio y stock en cada paso.

## Estructura

```
Mical/
  Program.cs              Composición completa: DI, middleware, migraciones, seed
  Controllers/            Públicos (Home, Shop, Product, Cart, Checkout, Order,
                          Account, Sitemap, Robots)
  Areas/Admin/            Panel admin: Controllers/, Models/ (VMs), Views/
  Services/               Interfaces/ + Implementations/ — la lógica de negocio
  Entities/               Modelo de dominio + Common/ (IAuditable, ISoftDeletable)
  Data/                   ApplicationDbContext, Configurations/ (Fluent API),
                          Interceptors/ (auditoría), Seed/
  Migrations/             EF Core
  ViewModels/             VMs de vistas públicas
  Validators/             FluentValidation
  Helpers/                Extensiones y constantes (ver abajo)
  Views/                  Razor; Shared/ tiene los partials compartidos
  wwwroot/                css/, js/, images/, uploads/
Mical.Tests/              xUnit + Testcontainers
Mical.sln
Dockerfile                Imagen de producción (multi-stage)
docker-compose.yml        PostgreSQL para desarrollo
```

## Cómo correrlo

```sh
docker compose up -d                      # PostgreSQL 16 (credenciales en .env)
cd Mical && dotnet run                    # https://localhost:xxxx
```

Secretos de desarrollo por **user-secrets** (nunca en el repo):

```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;..."
dotnet user-secrets set "AdminSeed:Password" "..."       # si no está, no crea el admin
dotnet user-secrets set "Resend:ApiToken" "re_..."       # emails de reset de contraseña
```

Las **migraciones se aplican solas al arrancar**, igual que el seed de roles y del admin
(ambos idempotentes). No hace falta `dotnet ef database update` a mano.

## Tests

```sh
dotnet test                 # desde la raíz; 69 tests
```

**Requiere Docker encendido.** `Mical.Tests/PostgresFixture` levanta un
`postgres:16-alpine` descartable por corrida y le aplica las migraciones reales.

**No se usa el provider InMemory de EF** y no hay que introducirlo: el checkout depende
de transacciones con rollback, del SQL crudo `nextval('order_number_seq')` y del token
de concurrencia `xmin`. InMemory **ignora las transacciones en silencio**, así que un
test de rollback contra InMemory daría verde sin probar nada.

Las aserciones **siempre abren un contexto nuevo**, para leer lo que realmente llegó a la
base y no el change tracker.

## Arquitectura

**Flujo:** Controller → Service → EF Core. El controller no arma queries; el service
devuelve `OperationResult` / `OperationResult<T>` y el controller traduce el error a
`ModelState` o a un toast por `TempData` (patrón PRG).

**Servicios** (`Services/Interfaces/`): `ICatalogService` (lectura pública),
`IProductService` · `ICategoryService` · `IPromotionService` (CRUD admin),
`IOrderService` (checkout + estados), `IDashboardService`, `IFileStorageService`,
`ISkuGenerator`, `IEmailService` + `IEmailTemplateRenderer`. Se registran en
`Extensions/ServiceCollectionExtensions`.

**Patrones del dominio:**

- **Soft delete** por query filter global (`IsDeleted`). Para alcanzar un registro borrado
  hace falta `IgnoreQueryFilters()` explícito.
- **Snapshots en `OrderItem`**: nombre y precio se copian al comprar. Nunca resolver el
  precio de un pedido histórico navegando a `Product`.
- **Concurrencia optimista en `Product`** con la columna de sistema `xmin`
  (anti-sobreventa). El checkout descuenta stock en transacción y reintenta ante conflicto.
- **Cancelar un pedido repone stock solo si el estado previo NO era `Entregado`.**
- **Auditoría** por interceptor de `SaveChanges` (`Data/Interceptors`), limitado a
  Product/Category/Order y **solo para acciones de rol Administrador**.
- **SKU** y número de pedido salen de secuencias de PostgreSQL (`product_sku_seq`,
  `order_number_seq`). El SKU no es editable.

**Seguridad ya resuelta** (no reinventar): antiforgery global, rate limiting por IP en
login/registro, lockout de Identity, headers de seguridad, política `AdminOnly` en
`/Admin`, validación de subida de archivos. Detalle en `PRODUCTION.md` §5.

## Frontend

Bootstrap 5 + CSS propio. **`wwwroot/css/style.css`** es el único archivo de estilos a
editar; tiene índice de secciones en el encabezado.

**`Views/Shared/_ProductCard.cshtml` es la ÚNICA ficha de producto** (home, `/shop` y
relacionados). Para cambiar cómo se ve un producto en una grilla se toca ahí y en ningún
otro lado. Sus estilos (`style.css` §4) **no están scopeados a ninguna sección** a
propósito. Ver `Design.md` §8 antes de modificarla.

**JS propio** (`wwwroot/js/`): `cart.js` (carrito en LocalStorage + handlers delegados
`.js-add-to-cart` / `.js-reorder`), `ui.js` (`window.UI.toast` / `UI.loader`),
`search.js` (búsqueda predictiva), `script.js` (popup de búsqueda, steppers de cantidad
y el único Swiper vivo: `.product-swiper`).

**No editar a mano:** `plugins.js`, `bootstrap*`, `jquery*`, `modernizr.js`,
`css/bootstrap.min.css`, `css/vendor.css`. Son vendor.

**Íconos SVG:** símbolos en el partial `_IconSprite`, referenciados con
`<use href="#id">`. Los nuevos van ahí.

## Trampas reales (se pagaron caro)

- **Cultura:** el *display* de precios es es-AR vía `MoneyExtensions.ToMoney()`, pero el
  *parseo* es **invariante** (`UseRequestLocalization` en `Program.cs`), porque los
  `<input type="number">` mandan el decimal con punto. Cambiar una cosa sin la otra hace
  que `15000.50` se guarde como `1500050`.
- **Migraciones:** `dotnet ef migrations add X` **sin `--no-build`** (con `--no-build`
  toma el ensamblado viejo y la migración sale vacía), con la app parada y Postgres
  healthy. Los comandos `dotnet ef` loguean un `Log.Fatal` cosmético
  (`HostAbortedException`): es esperado.
- **La app corriendo bloquea `bin/Debug/net8.0/Mical.exe`** y el build falla con MSB3027.
  Solución: `dotnet test Mical.sln --artifacts-path <carpeta temporal>`. **No matar el
  proceso del usuario**: él corre la app.
- **FluentValidation:** una regla sobre `x.Prop!.Value` registra el error bajo la clave
  `"Value"` y la vista nunca lo muestra. Siempre
  `.OverridePropertyName(nameof(Vm.Prop))`.
- **Imágenes subidas:** van a `Storage:UploadsPath` (ruta **absoluta**; en producción un
  volumen montado), no a `wwwroot`. El filesystem de un contenedor se borra en cada
  deploy. Usar `IFileStorageService.SaveImageAsync(file, subfolder)`, que convierte todo
  a WebP.
- **Verificar en el navegador, no solo con tests.** Varios defectos reales (contenedor que
  no arrancaba, `/sitemap.xml` en 500, botón de carrito invisible en táctil, precio
  desbordando la ficha) pasaron la suite completa y aparecieron recién al correr la imagen
  contra un Postgres real y medir en Chrome headless.

## Convenciones

- **Idioma:** comentarios, textos de UI, mensajes de validación y documentación en
  **español**. Identificadores de código en **inglés**.
- **Commits:** conventional commits, en español, sin atribución de IA. Un commit por paso.
- **`ROADMAP.md` es un tracker vivo:** agregar fila en la bitácora al cerrar algo.
- Un dato nuevo en una ficha de producto se agrega a `ProductCardVm` y se mapea en las
  proyecciones de `CatalogService` (hay 3 de `ProductCardVm` + 1 de `ProductDetailVm`).

## Documentos del proyecto

| Archivo | Para qué |
|---|---|
| `ANALISIS.md` | Arquitectura completa y decisiones fundacionales. |
| `ROADMAP.md` | Estado del desarrollo + bitácora. Qué está hecho y qué no. |
| `Design.md` | Cómo armar vistas, convenciones de frontend y **utilidades reutilizables** (§8). |
| `PRODUCTION.md` | Despliegue, variables de entorno, checklist y Railway (§8). |
