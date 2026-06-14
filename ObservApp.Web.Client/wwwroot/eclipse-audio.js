window.observApp = window.observApp || {};

// TTS — anuncia texto con Web Speech API
window.observApp.speakText = function (text) {
    try {
        if (!window.speechSynthesis || !text) return;
        // Cancelar cualquier síntesis en curso
        window.speechSynthesis.cancel();

        var utterance = new SpeechSynthesisUtterance(text);

        // Detectar idioma de la página
        utterance.lang = document.documentElement.lang || 'es-ES';
        utterance.rate = 1.0;
        utterance.pitch = 1.0;
        utterance.volume = 1.0;

        window.speechSynthesis.speak(utterance);
    } catch (e) {
        console.warn('[ObservApp] speakText error:', e);
    }
};

// Beep sintético mediante Web Audio API (sin necesidad de archivo .wav)
window.observApp.playBeepTone = function () {
    try {
        var ctx = new (window.AudioContext || window.webkitAudioContext)();
        var oscillator = ctx.createOscillator();
        var gain = ctx.createGain();

        oscillator.connect(gain);
        gain.connect(ctx.destination);

        oscillator.type = 'sine';
        oscillator.frequency.value = 880; // La5
        gain.gain.setValueAtTime(0.4, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.6);

        oscillator.start(ctx.currentTime);
        oscillator.stop(ctx.currentTime + 0.6);

        oscillator.onended = function () { ctx.close(); };
    } catch (e) {
        console.warn('[ObservApp] playBeepTone error:', e);
    }
};

// Mantener compatibilidad con llamadas antiguas a playBeep (por si acaso)
window.observApp.playBeep = window.observApp.playBeepTone;