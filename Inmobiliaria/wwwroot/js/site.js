// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.querySelectorAll("[data-remote-lookup]").forEach((lookup) => {
    const search = lookup.querySelector("[data-remote-lookup-search]");
    const value = lookup.querySelector("[data-remote-lookup-value]");
    const results = lookup.querySelector("[data-remote-lookup-results]");
    const status = lookup.querySelector("[data-remote-lookup-status]");
    if (!search || !value || !results || !status) return;

    let timer;
    let selectedText = search.value;
    const hideResults = () => {
        results.replaceChildren();
        results.style.display = "none";
    };

    search.addEventListener("input", () => {
        clearTimeout(timer);
        if (search.value !== selectedText) value.value = "";
        const query = search.value.trim();
        if (query.length < 2) {
            hideResults();
            status.textContent = query.length === 0 ? "" : "Escriba al menos 2 caracteres.";
            return;
        }

        status.textContent = "Buscando...";
        timer = setTimeout(async () => {
            try {
                const url = new URL(lookup.dataset.url, window.location.origin);
                url.searchParams.set("q", query);
                document.querySelectorAll("[data-lookup-inicio], [data-lookup-fin]").forEach((dateInput) => {
                    if (dateInput.value) url.searchParams.set(dateInput.dataset.lookupInicio !== undefined ? "inicio" : "fin", dateInput.value);
                });
                const response = await fetch(url);
                if (!response.ok) throw new Error(`HTTP ${response.status}`);
                const items = await response.json();
                if (search.value.trim() !== query) return;
                hideResults();
                if (items.length === 0) {
                    status.textContent = "No se encontraron resultados.";
                    return;
                }
                status.textContent = `${items.length} resultado(s).`;
                items.forEach((item) => {
                    const button = document.createElement("button");
                    button.type = "button";
                    button.className = "list-group-item list-group-item-action";
                    button.textContent = item.text;
                    button.addEventListener("click", () => {
                        value.value = item.id;
                        selectedText = item.text;
                        search.value = item.text;
                        status.textContent = "";
                        hideResults();
                    });
                    results.append(button);
                });
                results.style.display = "block";
            } catch (error) {
                hideResults();
                status.textContent = "No se pudo completar la búsqueda. Intente nuevamente.";
                console.error("Error en búsqueda remota:", error);
            }
        }, 250);
    });

    document.addEventListener("click", (event) => {
        if (!lookup.contains(event.target)) hideResults();
    });
});
