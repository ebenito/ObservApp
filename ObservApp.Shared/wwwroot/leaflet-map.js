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
