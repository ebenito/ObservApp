// Textos de carga sincronizados con la clave "Loading" de cada App.{lang}.resx.
// Al añadir o modificar un idioma, actualizar también el resx correspondiente.
window.ObservApp = window.ObservApp || {};

window.ObservApp.LOADING_TEXTS = {
    es: "Cargando ObservApp\u2026",
    en: "Loading ObservApp\u2026",
    fr: "Chargement d\u2019ObservApp\u2026",
    de: "ObservApp wird geladen\u2026",
    it: "Caricamento di ObservApp\u2026",
    ar: "\u062c\u0627\u0631\u064d \u062a\u062d\u0645\u064a\u0644 ObservApp\u2026"
};

window.ObservApp.applyLang = function (code, dir) {
    document.documentElement.lang = code;
    document.documentElement.dir  = dir;
    try { localStorage.setItem("app_language", code); } catch (e) { }
    // Sin recarga — solo actualiza atributos del DOM y persiste la preferencia
};

window.ObservApp.setLangAndReload = function (code, dir) {
    document.documentElement.lang = code;
    document.documentElement.dir  = dir;
    try { localStorage.setItem("app_language", code); } catch (e) { }
    location.reload();
};
