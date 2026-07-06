/*
 * Búsqueda predictiva del header. Consulta /shop/suggest mientras se escribe
 * (con debounce) y muestra un dropdown de productos. Degrada al submit normal
 * del formulario (Enter) si no hay JS o no hay sugerencias.
 */
(function () {
    "use strict";

    var input = document.querySelector(".js-search-input");
    var box = document.getElementById("search-suggestions");
    if (!input || !box) { return; }

    var form = input.closest("form");
    var MIN = 2;
    var debounceTimer = null;
    var activeIndex = -1;
    var currentItems = [];
    var lastQuery = "";

    function esc(s) {
        var d = document.createElement("div");
        d.textContent = s == null ? "" : s;
        return d.innerHTML;
    }

    function close() {
        box.hidden = true;
        box.innerHTML = "";
        input.setAttribute("aria-expanded", "false");
        activeIndex = -1;
        currentItems = [];
    }

    function open() {
        box.hidden = false;
        input.setAttribute("aria-expanded", "true");
    }

    function render(items, query) {
        currentItems = items;
        activeIndex = -1;

        if (items.length === 0) {
            box.innerHTML = '<div class="search-suggestions__empty">Sin resultados para “' + esc(query) + '”.</div>';
            open();
            return;
        }

        var html = items.map(function (it, idx) {
            var img = it.imagePath
                ? '<img src="/' + esc(it.imagePath) + '" alt="" width="40" height="40" loading="lazy">'
                : '<span class="search-suggestions__noimg"></span>';
            return '' +
                '<a class="search-suggestions__item" role="option" id="sugg-' + idx + '" href="' + esc(it.url) + '">' +
                img +
                '<span class="search-suggestions__name">' + esc(it.name) + '</span>' +
                '<span class="search-suggestions__price">' + esc(it.price) + '</span>' +
                '</a>';
        }).join("");

        // Enlace para ver todos los resultados en la tienda.
        html += '<a class="search-suggestions__all" href="/shop?q=' + encodeURIComponent(query) + '">' +
                'Ver todos los resultados de “' + esc(query) + '” →</a>';

        box.innerHTML = html;
        open();
    }

    function fetchSuggestions(query) {
        fetch("/shop/suggest?q=" + encodeURIComponent(query))
            .then(function (res) { return res.ok ? res.json() : []; })
            .then(function (items) {
                // Evita pintar respuestas viejas si el usuario siguió tecleando.
                if (input.value.trim() !== query) { return; }
                render(items, query);
            })
            .catch(function () { close(); });
    }

    input.addEventListener("input", function () {
        var query = input.value.trim();
        lastQuery = query;
        clearTimeout(debounceTimer);

        if (query.length < MIN) { close(); return; }

        debounceTimer = setTimeout(function () { fetchSuggestions(query); }, 200);
    });

    function highlight(idx) {
        var options = box.querySelectorAll(".search-suggestions__item");
        options.forEach(function (o) { o.classList.remove("is-active"); });
        if (idx >= 0 && idx < options.length) {
            options[idx].classList.add("is-active");
            input.setAttribute("aria-activedescendant", options[idx].id);
        } else {
            input.removeAttribute("aria-activedescendant");
        }
    }

    input.addEventListener("keydown", function (e) {
        if (box.hidden) { return; }
        var options = box.querySelectorAll(".search-suggestions__item");

        if (e.key === "ArrowDown") {
            e.preventDefault();
            activeIndex = Math.min(activeIndex + 1, options.length - 1);
            highlight(activeIndex);
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            activeIndex = Math.max(activeIndex - 1, -1);
            highlight(activeIndex);
        } else if (e.key === "Enter") {
            // Si hay una sugerencia resaltada, navega a ella; si no, deja el submit normal.
            if (activeIndex >= 0 && options[activeIndex]) {
                e.preventDefault();
                window.location.href = options[activeIndex].getAttribute("href");
            }
        } else if (e.key === "Escape") {
            close();
        }
    });

    // Cerrar al hacer click afuera.
    document.addEventListener("click", function (e) {
        if (!form.contains(e.target)) { close(); }
    });
})();
