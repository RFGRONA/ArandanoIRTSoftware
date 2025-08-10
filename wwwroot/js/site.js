document.addEventListener('DOMContentLoaded', function () {
    const sidebar = document.querySelector('.sidebar');
    const toggler = document.getElementById('sidebar-toggler');
    const mainContent = document.querySelector('.main-content');

    // Si los elementos esenciales no existen, no hacemos nada.
    if (!sidebar || !toggler || !mainContent) {
        console.error("Faltan elementos del layout (sidebar, toggler, o main-content).");
        return;
    }

    // --- FUNCIÓN PARA ABRIR Y CERRAR EL MENÚ ---
    function toggleSidebar(forceClose = false) {
        if (forceClose) {
            sidebar.classList.remove('is-open');
        } else {
            sidebar.classList.toggle('is-open');
        }
    }

    // --- EVENTOS ---

    // 1. El botón de hamburguesa abre/cierra el menú.
    toggler.addEventListener('click', function (e) {
        e.stopPropagation(); // Evita que el clic llegue al mainContent.
        toggleSidebar();
    });

    // 2. Hacer clic en el contenido principal CIERRA el menú.
    mainContent.addEventListener('click', function () {
        if (sidebar.classList.contains('is-open')) {
            toggleSidebar(true); // El 'true' fuerza el cierre.
        }
    });

    // 3. Presionar la tecla 'Escape' CIERRA el menú.
    document.addEventListener('keydown', function(event) {
        if (event.key === "Escape" && sidebar.classList.contains('is-open')) {
            toggleSidebar(true);
        }
    });
});