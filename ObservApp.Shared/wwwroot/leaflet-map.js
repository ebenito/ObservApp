// Interoperabilidad de JavaScript para Leaflet en ObservApp
window.observApp = window.observApp || {};
window.observApp.initMap = function (containerId, lat, lon, zoom, dotNetRef) {
    var container = document.getElementById(containerId);
    if (!container) return false;
    // Destruir mapa previo en este contenedor si existe
    if (container._leaflet_map) {
        container._leaflet_map.remove();
        container._leaflet_map = null;
        container._leaflet_marker = null;
    }
    // Inicializar mapa
    var map = L.map(containerId).setView([lat, lon], zoom);
    // La política de uso de las teselas de OSM exige que las peticiones lleven Referer
    // (sin él responden 403 "Access blocked"). Azure SWA sirve la página con
    // Referrer-Policy: same-origin, que lo suprime en peticiones a otros dominios.
    // Los subdominios a/b/c están obsoletos: OSM pide usar tile.openstreetmap.org.
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        referrerPolicy: 'strict-origin-when-cross-origin'
    }).addTo(map);
    // Crear marcador arrastrable
    var marker = L.marker([lat, lon], { draggable: true }).addTo(map);
    // Escuchar evento de arrastre de marcador
    marker.on('dragend', function (event) {
        var markerPos = event.target.getLatLng();
        // Llamar al método C# de Blazor pasándole latitud y longitud
        dotNetRef.invokeMethodAsync('OnMarkerDragged', markerPos.lat, markerPos.lng);
    });
    // Guardar referencias en el propio elemento del DOM
    container._leaflet_map = map;
    container._leaflet_marker = marker;
    // Forzar redimensionamiento para evitar visualización incorrecta en modales o pestañas
    setTimeout(function () {
        map.invalidateSize();
    }, 200);
    return true;
};
window.observApp.updateMapPosition = function (containerId, lat, lon, zoom) {
    var container = document.getElementById(containerId);
    if (container && container._leaflet_map && container._leaflet_marker) {
        var newPos = [lat, lon];
        container._leaflet_map.setView(newPos, zoom);
        container._leaflet_marker.setLatLng(newPos);
    }
};
window.observApp.isMapInitialized = function (containerId) {
    var container = document.getElementById(containerId);
    return !!(container && container._leaflet_map && container._leaflet_marker);
};

// ─────────────────────────────────────────────────────────────────────────────
// Mapa con varias chinchetas y selector de capas (componente ObsMap / página /mapa).
// Ninguna capa necesita API key. Las de Google son las teselas públicas de
// mt*.google.com: no están documentadas y Google puede retirarlas sin aviso, por eso
// OpenStreetMap queda como capa por defecto.
// ─────────────────────────────────────────────────────────────────────────────
window.observApp.multiMap = (function () {
    var STORAGE_KEY = 'observapp.map.baseLayer';
    var COLORS = { favorite: '#f4c430', entry: '#2ec4b6' };

    function googleLayer(lyrs, lang) {
        return L.tileLayer('https://mt{s}.google.com/vt/lyrs=' + lyrs + '&hl=' + lang + '&x={x}&y={y}&z={z}', {
            subdomains: ['0', '1', '2', '3'],
            maxZoom: 20,
            attribution: '&copy; Google'
        });
    }

    function buildBaseLayers(lang) {
        return {
            osm: L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
                // Ver initMap: OSM exige Referer.
                referrerPolicy: 'strict-origin-when-cross-origin'
            }),
            googleRoad: googleLayer('m', lang),
            googleSatellite: googleLayer('s', lang),
            googleHybrid: googleLayer('y', lang),
            googleTerrain: googleLayer('p', lang),
            topo: L.tileLayer('https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
                maxZoom: 17,
                attribution: 'Map data: &copy; OpenStreetMap contributors, SRTM | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (CC-BY-SA)'
            })
            // CARTO (dark_all) descartada: sus teselas muestran un aviso pidiendo API key.
            // Si alguien la tenía guardada en localStorage, create() vuelve a 'osm'.
        };
    }

    function escapeHtml(text) {
        return String(text == null ? '' : text)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function popupHtml(m, texts) {
        var html = '<div class="obs-map-popup"><strong>' + escapeHtml(m.title) + '</strong>';
        (m.subtitle || []).forEach(function (line) {
            html += '<div class="obs-map-popup-line">' + escapeHtml(line) + '</div>';
        });
        html += '<div class="obs-map-popup-links">' +
            '<a href="#" data-obs-ext="' + escapeHtml(m.googleUrl) + '">' + escapeHtml(texts.openGoogle) + '</a>' +
            '<a href="#" data-obs-ext="' + escapeHtml(m.osmUrl) + '">' + escapeHtml(texts.openOsm) + '</a>' +
            '</div></div>';
        return html;
    }

    function get(containerId) {
        var el = document.getElementById(containerId);
        return el && el._obsMulti ? el._obsMulti : null;
    }

    // options: { lang, labels: { osm, googleRoad, ..., favorites, entries }, texts: { openGoogle, openOsm } }
    function create(containerId, options, dotNetRef) {
        var el = document.getElementById(containerId);
        if (!el || typeof L === 'undefined') return false;
        dispose(containerId);

        var labels = options.labels || {};
        var map = L.map(el, { worldCopyJump: true }).setView([40.4168, -3.7038], 5);
        var bases = buildBaseLayers(options.lang || 'es');

        var saved = null;
        try { saved = localStorage.getItem(STORAGE_KEY); } catch (e) { }
        var current = bases[saved] ? saved : 'osm';
        bases[current].addTo(map);

        var groups = {
            favorite: L.featureGroup().addTo(map),
            entry: L.featureGroup().addTo(map)
        };

        var baseByLabel = {};
        var labelToKey = {};
        Object.keys(bases).forEach(function (key) {
            var label = escapeHtml(labels[key] || key);
            baseByLabel[label] = bases[key];
            labelToKey[label] = key;
        });
        var overlays = {};
        overlays['<span style="color:' + COLORS.favorite + '">&#9679;</span> ' + escapeHtml(labels.favorites)] = groups.favorite;
        overlays['<span style="color:' + COLORS.entry + '">&#9679;</span> ' + escapeHtml(labels.entries)] = groups.entry;
        L.control.layers(baseByLabel, overlays, { collapsed: true }).addTo(map);
        L.control.scale({ imperial: false }).addTo(map);

        map.on('baselayerchange', function (e) {
            var key = labelToKey[e.name];
            if (key) { try { localStorage.setItem(STORAGE_KEY, key); } catch (err) { } }
        });

        // Los enlaces externos se abren desde .NET (IExternalLinkService): en el WebView de
        // Android window.open y target=_blank no funcionan.
        var onClick = function (e) {
            var a = e.target && e.target.closest ? e.target.closest('a[data-obs-ext]') : null;
            if (!a) return;
            e.preventDefault();
            var url = a.getAttribute('data-obs-ext');
            if (url && dotNetRef) dotNetRef.invokeMethodAsync('OnExternalLinkRequested', url);
        };
        el.addEventListener('click', onClick);

        el._obsMulti = { map: map, groups: groups, markers: {}, texts: options.texts || {}, onClick: onClick };
        setTimeout(function () { map.invalidateSize(); }, 200);
        return true;
    }

    // markers: [{ id, kind: 'favorite'|'entry', lat, lon, title, subtitle: [], googleUrl, osmUrl }]
    function setMarkers(containerId, markers, fit) {
        var state = get(containerId);
        if (!state) return;
        state.groups.favorite.clearLayers();
        state.groups.entry.clearLayers();
        state.markers = {};

        (markers || []).forEach(function (m) {
            var group = state.groups[m.kind] || state.groups.entry;
            var marker = L.circleMarker([m.lat, m.lon], {
                radius: m.kind === 'favorite' ? 9 : 7,
                color: '#0d1b2a',
                weight: 2,
                fillColor: COLORS[m.kind] || COLORS.entry,
                fillOpacity: 0.95
            }).bindPopup(popupHtml(m, state.texts)).bindTooltip(escapeHtml(m.title));
            marker.addTo(group);
            state.markers[m.kind + ':' + m.id] = marker;
        });

        if (fit) fitAll(containerId);
    }

    function fitAll(containerId) {
        var state = get(containerId);
        if (!state) return;
        var bounds = L.latLngBounds([]);
        [state.groups.favorite, state.groups.entry].forEach(function (g) {
            if (state.map.hasLayer(g) && g.getLayers().length) bounds.extend(g.getBounds());
        });
        if (bounds.isValid()) {
            state.map.fitBounds(bounds, { padding: [30, 30], maxZoom: 13 });
        }
    }

    function focus(containerId, kind, id, zoom) {
        var state = get(containerId);
        if (!state) return;
        var marker = state.markers[kind + ':' + id];
        if (!marker) return;
        var group = state.groups[kind];
        if (group && !state.map.hasLayer(group)) group.addTo(state.map);
        state.map.setView(marker.getLatLng(), zoom || 14);
        marker.openPopup();
        var el = document.getElementById(containerId);
        if (el && el.scrollIntoView) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    function dispose(containerId) {
        var el = document.getElementById(containerId);
        if (!el || !el._obsMulti) return;
        el.removeEventListener('click', el._obsMulti.onClick);
        el._obsMulti.map.remove();
        el._obsMulti = null;
    }

    return { create: create, setMarkers: setMarkers, fitAll: fitAll, focus: focus, dispose: dispose };
})();
