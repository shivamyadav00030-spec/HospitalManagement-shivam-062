document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("appSidebar");
    const toggle = document.querySelector(".sidebar-toggle");

    if (sidebar && toggle) {
        const closeSidebar = () => {
            sidebar.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
        };

        toggle.addEventListener("click", () => {
            const isOpen = sidebar.classList.toggle("is-open");
            toggle.setAttribute("aria-expanded", String(isOpen));
        });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape") closeSidebar();
        });

        document.addEventListener("click", event => {
            if (window.innerWidth < 992 && sidebar.classList.contains("is-open") &&
                !sidebar.contains(event.target) && !toggle.contains(event.target)) {
                closeSidebar();
            }
        });
    }

    document.querySelectorAll("form[data-confirm]").forEach(form => {
        form.addEventListener("submit", event => {
            const message = form.getAttribute("data-confirm");
            if (message && !window.confirm(message)) event.preventDefault();
        });
    });
});
