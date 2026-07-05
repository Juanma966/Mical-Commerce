# Design.md — Guía de frontend de Mical

Guía práctica para **editar** vistas existentes y **crear** nuevas, manteniendo la
consistencia del sitio. Mical es **ASP.NET Core MVC**: no hay un único HTML, el
frontend son **vistas Razor (`.cshtml`)** + estilos en `wwwroot/css/style.css`.

> Regla de oro: todo el frontend vive en **`Mical/Views/`** (HTML) y
> **`Mical/wwwroot/`** (css/js/imágenes). No hay carpeta `frontend/` (se eliminó).

---

## 1. Cómo se arma una página

Cada request llega a un **Controller → Action**, que devuelve una **View** (`.cshtml`).
La vista se envuelve automáticamente en el layout:

```
_ViewStart.cshtml        → dice: Layout = "_Layout"
└─ _Layout.cshtml        → <head> + CSS/JS + estructura de la página
   ├─ _IconSprite        → íconos SVG (ocultos, se referencian con <use>)
   ├─ _Header            → navbar, logo, buscador, íconos usuario/carrito
   ├─ @RenderBody()      → ACÁ se inyecta el contenido de la vista concreta
   └─ _Footer            → pie del sitio
```

Archivos clave (en `Mical/Views/Shared/`):
- **`_Layout.cshtml`** — esqueleto de toda página. Acá van los `<link>` de CSS y los `<script>`.
- **`_Header.cshtml`** — encabezado (logo `mical-logo.webp`, menú, buscador → `/shop`).
- **`_Footer.cshtml`** — pie.
- **`_IconSprite.cshtml`** — íconos SVG como `<symbol id="...">`.
- **`_ViewImports.cshtml`** (en `Views/`) — `@using` globales y tag helpers. **Ya incluye
  `Mical.Helpers`, `Mical.ViewModels`, etc.**, así que no hace falta importarlos en cada vista.

El **área Admin** tiene su propio layout: `Areas/Admin/Views/Shared/_AdminLayout.cshtml`
(sidebar oscuro). Las vistas de `Areas/Admin/Views/**` lo usan automáticamente.

---

## 2. Mapa de vistas

**Públicas** (`Mical/Views/`):
| Ruta | Vista | Controller/Action |
|---|---|---|
| `/` | `Home/Index.cshtml` | `HomeController.Index` |
| `/shop` | `Shop/Index.cshtml` | `ShopController.Index` |
| `/product/{id}` | `Product/Details.cshtml` | `ProductController.Details` |
| `/cart` | `Cart/Index.cshtml` | `CartController.Index` |
| `/checkout` | `Checkout/Index.cshtml` | `CheckoutController.Index` |
| `/order` · `/order/details/{id}` | `Order/Index.cshtml` · `Order/Details.cshtml` | `OrderController` |
| `/account/login` · `register` · `profile`… | `Account/*.cshtml` | `AccountController` |

**Home** (`Home/Index.cshtml`) por secciones — cada una es un `<section id="...">`:
`#billboard` (hero) · `#company-services` · `#mobile-products` · `#yearly-sale` ·
`#latest-blog` · `#testimonials` · `#subscribe` · `#instagram`.
Se pueden **editar o borrar** secciones enteras sin romper el resto.

**Admin** (`Areas/Admin/Views/`): `Dashboard/`, `Categories/`, `Products/`, `Orders/`.

---

## 3. Convenciones (seguirlas al crear/editar)

### Estructura de una vista pública
El header es **fijo (position-fixed)**, así que el contenido arranca con un margen
superior para no quedar tapado. Patrón estándar:

```cshtml
@model TuViewModel
@{
    ViewData["Title"] = "Título de la pestaña";
}

<section class="py-5" style="margin-top:90px;">
    <div class="container">
        <!-- contenido -->
    </div>
</section>
```

### Estilos
- **Bootstrap 5** (utility classes) para el 90% del layout: `container`, `row`, `col-*`,
  `d-flex`, `mb-3`, `text-end`, `btn btn-dark`, `card`, `badge`, etc.
- Reglas propias del tema → **`wwwroot/css/style.css`** (es el archivo a tocar para diseño).
  Tiene una tabla de contenidos arriba. **No** editar `bootstrap.min.css` ni `vendor.css`.

### Precios → SIEMPRE `.ToMoney()`
Formatea en es-AR (`$1.234,56`). Nunca uses `.ToString("N2")`.
```cshtml
<strong>@Model.Price.ToMoney()</strong>       @* decimal *@
<span>@producto.SalePrice.ToMoney()</span>    @* decimal? también sirve *@
```
En **JavaScript**, para formatear precios del lado cliente:
```js
"$" + Number(n).toLocaleString("es-AR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })
```

### Estado de pedido → `.Badge()`
```cshtml
@{ var badge = pedido.Status.Badge(); }
<span class="badge @badge.Css">@badge.Text</span>
```

### Links y forms → tag helpers (no URLs hardcodeadas)
```cshtml
<a asp-controller="Shop" asp-action="Index" asp-route-categoria="@id">Ver</a>
<form asp-controller="Checkout" asp-action="Index" method="post"> ... </form>
```
Los `<form method="post">` incluyen el **token antiforgery automáticamente** (hay validación
CSRF global). Para inputs ligados al modelo: `asp-for`, `asp-validation-for`, `asp-validation-summary`.

### Imágenes
- Referenciar con `~/` (resuelve a `wwwroot/`): `<img src="~/images/mical-logo.webp">`.
- Imágenes de productos subidas por admin: `~/uploads/products/...` (las guarda el sistema).
- Assets disponibles: `wwwroot/images/` (logo `mical-logo.webp`, banner `mical-hero.png`,
  y las imágenes de muestra de la plantilla `product-item*.jpg`, `post-*.jpg`, etc.).

### Íconos SVG
Definidos una vez en `_IconSprite.cshtml` y usados así:
```cshtml
<svg class="cart"><use xlink:href="#cart"></use></svg>
```
Para un ícono nuevo, agregá un `<symbol id="miicono">…</symbol>` en `_IconSprite`.

### Scripts por vista
Si una vista necesita JS propio, va en una sección `Scripts` (se renderiza al final del layout):
```cshtml
@section Scripts {
    <script> /* ... */ </script>
}
```

---

## 4. Crear una vista NUEVA (paso a paso)

Ejemplo: una página "Nosotros" en `/nosotros`.

1. **Action** en un controller (podés reusar `HomeController` o crear `PagesController`):
   ```csharp
   public IActionResult Nosotros() => View();
   ```
2. **Vista** en `Mical/Views/Home/Nosotros.cshtml` (carpeta = nombre del controller):
   ```cshtml
   @{ ViewData["Title"] = "Nosotros"; }
   <section class="py-5" style="margin-top:90px;">
       <div class="container">
           <h1 class="h3 border-bottom pb-3 mb-4">Nosotros</h1>
           <p>Contenido…</p>
       </div>
   </section>
   ```
3. **Link** en el header/footer con tag helpers:
   `<a asp-controller="Home" asp-action="Nosotros">Nosotros</a>`
4. **Reiniciar** `dotnet run` (ver §5) y probar.

> Para una página con datos, creá un ViewModel en `Mical/ViewModels/`, pasalo con
> `return View(vm);` y declaralo con `@model TuVm` arriba de la vista.

Reutilizar una vista existente como base: copiá el `.cshtml` más parecido (p. ej.
`Shop/Index.cshtml` para grillas, `Account/Login.cshtml` para formularios centrados) y adaptá.

---

## 5. Ver los cambios

- **CSS / JS / imágenes** (`wwwroot/`): se sirven directo → **refrescás el navegador** y listo
  (el `<link>` de `style.css` usa `asp-append-version` para romper caché).
- **Vistas `.cshtml`**: se compilan al build → hay que **reiniciar `dotnet run`** para verlas.
  - *(Opción cómoda para maquetar: activar **Razor Runtime Compilation** — con eso alcanza
    con refrescar el navegador para ver cambios de `.cshtml`. Pedilo y lo dejo configurado.)*

---

## 6. Qué NO tocar

- `wwwroot/css/bootstrap.min.css`, `wwwroot/css/vendor.css`, `wwwroot/js/jquery*`,
  `wwwroot/js/bootstrap*`, `wwwroot/js/plugins.js`, `wwwroot/js/modernizr.js` → **vendor**.
- El **orden de carga de scripts** en `_Layout` (jQuery → Swiper → Bootstrap → plugins → script/cart).
- `_ViewImports.cshtml` salvo para sumar un `@using` nuevo.

---

## 7. Paleta de marca (referencia)

Del logo de Mical:
- **Violeta** (wordmark): aprox. `#8B6DB0` / `#7E5FA4`.
- **Azul pizarra** (banner): aprox. `#4A5568` / `#3E4A5B`.
- **Crema** (texto del banner): aprox. `#F5F0E1`.

> Si querés que el sitio adopte estos colores (botones, links, acentos) en vez del
> negro/gris de la plantilla, se centraliza en `style.css`. Pedilo y lo aplico.
