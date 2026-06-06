window.observApp = window.observApp || {};

window.observApp.playBeep = function (src) {
    try {
        var audio = new Audio(src);
        audio.volume = 0.8;
        return audio.play().catch(function () { });
    } catch (e) { }
};