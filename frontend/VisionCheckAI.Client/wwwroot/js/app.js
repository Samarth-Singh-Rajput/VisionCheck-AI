// Minimal JS interop surface for the Blazor client.
window.visionCheck = (function () {
    "use strict";

    function safeStorage() {
        try {
            return window.localStorage;
        } catch (e) {
            return null;
        }
    }

    return {
        storage: {
            get: function (key) {
                var store = safeStorage();
                return store ? store.getItem(key) : null;
            },
            set: function (key, value) {
                var store = safeStorage();
                if (store) { store.setItem(key, value); }
            },
            remove: function (key) {
                var store = safeStorage();
                if (store) { store.removeItem(key); }
            }
        },

        theme: {
            apply: function (theme) {
                document.documentElement.setAttribute("data-theme", theme === "light" ? "light" : "dark");
            }
        },

        // Used by the drawer so keyboard users land inside it when it opens.
        focusElement: function (element) {
            if (element && typeof element.focus === "function") {
                element.focus();
            }
        }
    };
})();

// Blazor's built-in error banner controls.
(function () {
    var banner = document.getElementById("blazor-error-ui");
    if (!banner) { return; }

    var dismiss = banner.querySelector(".dismiss");
    if (dismiss) {
        dismiss.addEventListener("click", function () {
            banner.style.display = "none";
        });
    }
})();
