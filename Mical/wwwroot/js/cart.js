/*
 * Carrito de Mical — estado en LocalStorage (solo {id, qty}, nunca precios).
 * El precio y el stock siempre se resuelven en el servidor (/cart/rehydrate).
 */
(function () {
    "use strict";
    var KEY = "mical_cart";

    function read() {
        try { return JSON.parse(localStorage.getItem(KEY)) || []; }
        catch (e) { return []; }
    }

    function write(items) {
        localStorage.setItem(KEY, JSON.stringify(items));
        updateBadge();
        document.dispatchEvent(new CustomEvent("cart:changed"));
    }

    function items() { return read(); }

    function count() {
        return read().reduce(function (sum, i) { return sum + i.qty; }, 0);
    }

    function add(id, qty) {
        id = parseInt(id, 10);
        qty = parseInt(qty, 10) || 1;
        if (!id || qty < 1) { return; }
        var list = read();
        var found = list.find(function (x) { return x.id === id; });
        if (found) { found.qty += qty; } else { list.push({ id: id, qty: qty }); }
        write(list);
    }

    function setQty(id, qty) {
        id = parseInt(id, 10);
        qty = parseInt(qty, 10);
        var list = read();
        if (!qty || qty < 1) {
            list = list.filter(function (x) { return x.id !== id; });
        } else {
            var found = list.find(function (x) { return x.id === id; });
            if (found) { found.qty = qty; } else { list.push({ id: id, qty: qty }); }
        }
        write(list);
    }

    function remove(id) {
        id = parseInt(id, 10);
        write(read().filter(function (x) { return x.id !== id; }));
    }

    function clear() { write([]); }

    function updateBadge() {
        var badges = document.querySelectorAll(".js-cart-count");
        var c = count();
        badges.forEach(function (b) {
            b.textContent = c;
            b.style.display = c > 0 ? "" : "none";
        });
    }

    window.Cart = {
        items: items, count: count, add: add,
        setQty: setQty, remove: remove, clear: clear, updateBadge: updateBadge
    };

    document.addEventListener("DOMContentLoaded", function () {
        updateBadge();

        // Botón "Agregar al carrito" (delegado): lee data-product-id y una cantidad opcional.
        document.addEventListener("click", function (e) {
            var btn = e.target.closest(".js-add-to-cart");
            if (!btn) { return; }
            e.preventDefault();

            var id = btn.getAttribute("data-product-id");
            var qty = 1;
            var target = btn.getAttribute("data-qty-target");
            if (target) {
                var input = document.querySelector(target);
                if (input) { qty = parseInt(input.value, 10) || 1; }
            }
            add(id, qty);

            var original = btn.getAttribute("data-label") || btn.textContent;
            btn.setAttribute("data-label", original);
            btn.textContent = "Agregado ✓";
            setTimeout(function () { btn.textContent = original; }, 1200);
        });
    });
})();
