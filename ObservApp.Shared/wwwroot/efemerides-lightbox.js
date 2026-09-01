window.showDsoLightbox = function (url, alt) {
    if (!url) return;

    // Evitar duplicados
    if (document.getElementById('obs-dso-lightbox')) return;

    const overlay = document.createElement('div');
    overlay.id = 'obs-dso-lightbox';
    overlay.className = 'eph-dso-lightbox';

    overlay.innerHTML = `
        <div class="eph-dso-lightbox-panel" role="dialog" aria-modal="true">
            <button type="button" class="eph-dso-lightbox-close" aria-label="Cerrar imagen">×</button>
            <img class="eph-dso-lightbox-image" src="${url}" alt="${alt || ''}" loading="eager" />
        </div>`;

    // Cerrar al clicar fuera
    overlay.addEventListener('click', function () {
        window.closeDsoLightbox();
    });

    // Evitar cierre al clicar en panel
    overlay.querySelector('.eph-dso-lightbox-panel').addEventListener('click', function (e) {
        e.stopPropagation();
    });

    // Botón cerrar
    overlay.querySelector('.eph-dso-lightbox-close').addEventListener('click', function (e) {
        e.stopPropagation();
        window.closeDsoLightbox();
    });

    // Cerrar con ESC
    function onKey(e) {
        if (e.key === 'Escape') window.closeDsoLightbox();
    }

    overlay._onKey = onKey;
    document.addEventListener('keydown', onKey);

    document.body.appendChild(overlay);
};

window.closeDsoLightbox = function () {
    const overlay = document.getElementById('obs-dso-lightbox');
    if (!overlay) return;
    if (overlay._onKey) document.removeEventListener('keydown', overlay._onKey);
    overlay.remove();
};
