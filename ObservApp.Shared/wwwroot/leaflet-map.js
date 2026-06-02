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
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
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
