/*
 * UI de Mical — Toasts y Loader global reutilizables.
 * Expone window.UI = { toast, loader }.
 * Sin dependencias (jQuery no es necesario).
 */
(function () {
    "use strict";

    /* ---------------- Toasts ---------------- */
    function toastContainer() {
        var el = document.getElementById("mical-toasts");
        if (!el) {
            el = document.createElement("div");
            el.id = "mical-toasts";
            el.className = "mical-toasts";
            el.setAttribute("aria-live", "polite");
            el.setAttribute("aria-atomic", "true");
            document.body.appendChild(el);
        }
        return el;
    }

    var ICONS = {
        success: "✓",
        error: "✕",
        info: "i",
        warning: "!"
    };

    // toast(message, type = "success", timeout = 3200 ms)
    function toast(message, type, timeout) {
        type = type || "success";
        timeout = typeof timeout === "number" ? timeout : 3200;

        var t = document.createElement("div");
        t.className = "mical-toast mical-toast--" + type;
        t.setAttribute("role", type === "error" ? "alert" : "status");

        var icon = document.createElement("span");
        icon.className = "mical-toast__icon";
        icon.textContent = ICONS[type] || ICONS.info;

        var body = document.createElement("span");
        body.className = "mical-toast__msg";
        body.textContent = message;

        var close = document.createElement("button");
        close.type = "button";
        close.className = "mical-toast__close";
        close.setAttribute("aria-label", "Cerrar");
        close.innerHTML = "&times;";
        close.addEventListener("click", function () { dismiss(t); });

        t.appendChild(icon);
        t.appendChild(body);
        t.appendChild(close);
        toastContainer().appendChild(t);

        // fuerza el reflow para animar la entrada
        void t.offsetWidth;
        t.classList.add("is-visible");

        if (timeout > 0) {
            setTimeout(function () { dismiss(t); }, timeout);
        }
        return t;
    }

    function dismiss(t) {
        if (!t || t.classList.contains("is-leaving")) { return; }
        t.classList.add("is-leaving");
        t.classList.remove("is-visible");
        setTimeout(function () {
            if (t.parentNode) { t.parentNode.removeChild(t); }
        }, 250);
    }

    /* ---------------- Loader global ---------------- */
    var loaderCount = 0;

    function loaderEl() {
        var el = document.getElementById("mical-loader");
        if (!el) {
            el = document.createElement("div");
            el.id = "mical-loader";
            el.className = "mical-loader";
            el.setAttribute("aria-hidden", "true");
            el.innerHTML = '<div class="mical-loader__spinner" role="status" aria-label="Cargando"></div>';
            document.body.appendChild(el);
        }
        return el;
    }

    // Soporta llamadas anidadas: se oculta cuando todas las tareas terminaron.
    function loaderShow() {
        loaderCount++;
        var el = loaderEl();
        el.classList.add("is-active");
        el.setAttribute("aria-hidden", "false");
    }

    function loaderHide(force) {
        loaderCount = force ? 0 : Math.max(0, loaderCount - 1);
        if (loaderCount === 0) {
            var el = loaderEl();
            el.classList.remove("is-active");
            el.setAttribute("aria-hidden", "true");
        }
    }

    window.UI = {
        toast: toast,
        dismissToast: dismiss,
        loader: {
            show: loaderShow,
            hide: loaderHide,
            reset: function () { loaderHide(true); }
        }
    };
})();
