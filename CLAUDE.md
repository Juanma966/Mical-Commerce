# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A static, single-page e-commerce storefront template ("MiniStore" / "Ministore by Moksha"). All source lives under `MiniStore-1.0.0/`. There is **no build step, no package manager, no bundler, and no backend** — `index.html` is opened directly or served as static files. Content (products, prices, blog posts, testimonials) is hard-coded in `index.html`; there is no data layer or API.

## Running it

No build/test/lint tooling exists. To preview, open `MiniStore-1.0.0/index.html` in a browser, or serve the folder statically:

```sh
cd MiniStore-1.0.0
python -m http.server 8000   # then visit http://localhost:8000
```

Note: Swiper is loaded from a CDN (`cdn.jsdelivr.net`) and Google Fonts from `fonts.googleapis.com`, so an internet connection is needed for sliders and fonts to render correctly.

## Architecture

Everything is one page, `MiniStore-1.0.0/index.html`, divided into `<section id="...">` blocks that are the units to edit. Key sections in document order: `billboard` (hero swiper), `company-services`, `mobile-products`, `smart-watches` (product carousels), `yearly-sale`, `latest-blog`, `testimonials`, `subscribe`, `instagram`, and `footer`.

Load order at the bottom of `index.html` matters — scripts depend on it: `jquery-1.11.0.min.js` → Swiper (CDN) → `bootstrap.bundle.min.js` → `plugins.js` → `script.js`.

- **`js/script.js`** — the only hand-written behavior, wrapped in an IIFE `(function($){ ... })(jQuery)`. It wires up the search popup, the product quantity +/- steppers (`.quantity-right-plus` / `.quantity-left-minus`), and initializes every Swiper carousel inside `$(document).ready`. Each carousel is bound by CSS class (`.main-swiper`, `.product-swiper`, `.product-watch-swiper`, `.testimonial-swiper`); to add a new slider, add the markup with the matching class and a `new Swiper(...)` call here.
- **`js/plugins.js`** — concatenated third-party plugin bundle (anime.js and others). Treat as vendor; do not hand-edit.
- **Vendored libraries** (`bootstrap*`, `jquery*`, `modernizr.js`) — do not edit.

Styling is Bootstrap 5 utility classes plus custom rules:

- **`css/bootstrap.min.css`** — vendored framework, do not edit.
- **`style.css`** (repo-root of the template folder, not in `css/`) — all custom theme styles. This is the file to edit for design changes; it has a table-of-contents header documenting its structure.
- **`css/vendor.css`** — supporting vendor styles.

SVG icons are defined once as `<symbol id="...">` in a hidden `<svg>` at the top of `<body>` and referenced elsewhere via `<use href="#id">` (e.g. `search`, `cart`, `user`, `star-fill`, social icons). Add new icons as symbols there.

## Conventions

- Image assets go in `MiniStore-1.0.0/images/` and are referenced with relative paths (`images/...`).
- Since product/content data is inline HTML, changing the catalog means editing the repeated card markup within the relevant `<section>`, not a config or JSON file.
