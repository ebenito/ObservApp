// Geolocalización del navegador — expuesto como observApp.getCurrentPosition
// para ser invocado desde Blazor vía IJSRuntime.InvokeAsync.
// Devuelve códigos de error (no textos): los traduce WebGeolocationService.
window.observApp = window.observApp || {};

window.observApp.getCurrentPosition = function (highAccuracy) {
    return new Promise(function (resolve) {
        if (!navigator.geolocation) {
            resolve({ latitude: 0, longitude: 0, altitude: null, accuracy: null, error: 'unsupported' });
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
                var code;
                switch (err.code) {
                    case 1: code = 'permission_denied'; break;
                    case 2: code = 'position_unavailable'; break;
                    case 3: code = 'timeout'; break;
                    default: code = 'unknown';
                }
                resolve({ latitude: 0, longitude: 0, altitude: null, accuracy: null, error: code });
            },
            options
        );
    });
};
