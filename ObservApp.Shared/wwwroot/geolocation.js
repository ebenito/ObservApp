// Geolocalización del navegador — expuesto como observApp.getCurrentPosition
// para ser invocado desde Blazor vía IJSRuntime.InvokeAsync
window.observApp = window.observApp || {};

window.observApp.getCurrentPosition = function (highAccuracy) {
    return new Promise(function (resolve) {
        if (!navigator.geolocation) {
            resolve({ error: 'La geolocalización no está disponible en este navegador.' });
            return;
        }

        var options = {
            enableHighAccuracy: highAccuracy === true,
            timeout: 15000,
            maximumAge: 30000
        };

        navigator.geolocation.getCurrentPosition(
            function (position) {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    altitude: position.coords.altitude,
                    accuracy: position.coords.accuracy,
                    error: null
                });
            },
            function (err) {
                var msg;
                switch (err.code) {
                    case 1: msg = 'Permiso de ubicación denegado.'; break;
                    case 2: msg = 'Posición no disponible en este momento.'; break;
                    case 3: msg = 'Tiempo de espera agotado al obtener la ubicación.'; break;
                    default: msg = 'Error desconocido al obtener la ubicación.';
                }
                resolve({ latitude: 0, longitude: 0, altitude: null, accuracy: null, error: msg });
            },
            options
        );
    });
};
