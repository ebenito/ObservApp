window.observApp = window.observApp || {};

/**
 * Intenta TTS. Si no hay voz para el idioma activo, usa fallback de audio.
 * @param {string} text - Texto a leer
 * @param {string} audioFallbackBase - Ruta base sin extensión (se prueba .mp3 luego .wav)
 */
window.observApp.speakTextWithFallback = function (text, audioFallbackBase) {
    try {
        if (!window.speechSynthesis || !text) {
            window.observApp._playAudioFallback(audioFallbackBase);
            return;
        }

        var lang = document.documentElement.lang || 'es-ES';
        var langBase = lang.split('-')[0].toLowerCase();

        var attemptSpeak = function () {
            var voices = window.speechSynthesis.getVoices();
            var hasVoice = voices.some(function (v) {
                return v.lang.toLowerCase().startsWith(langBase);
            });

            if (!hasVoice) {
                console.info('[ObservApp] Sin voz TTS para "' + lang + '" → audio fallback.');
                window.observApp._playAudioFallback(audioFallbackBase);
                return;
            }

            window.speechSynthesis.cancel();

            var utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = lang;
            utterance.rate = 1.0;
            utterance.pitch = 1.0;
            utterance.volume = 1.0;

            var matchedVoice = voices.find(function (v) {
                return v.lang.toLowerCase().startsWith(langBase);
            });
            if (matchedVoice) utterance.voice = matchedVoice;

            var fallbackTimer = setTimeout(function () {
                console.warn('[ObservApp] TTS timeout → audio fallback.');
                window.observApp._playAudioFallback(audioFallbackBase);
            }, 3000);

            utterance.onerror = function (e) {
                clearTimeout(fallbackTimer);
                console.warn('[ObservApp] TTS error:', e.error, '→ audio fallback.');
                window.observApp._playAudioFallback(audioFallbackBase);
            };

            utterance.onstart = function () {
                clearTimeout(fallbackTimer);
            };

            window.speechSynthesis.speak(utterance);
        };

        if (window.speechSynthesis.getVoices().length > 0) {
            attemptSpeak();
        } else {
            window.speechSynthesis.onvoiceschanged = function () {
                window.speechSynthesis.onvoiceschanged = null;
                attemptSpeak();
            };
            // Timeout de seguridad si onvoiceschanged nunca dispara
            setTimeout(function () {
                if (window.speechSynthesis.getVoices().length === 0) {
                    window.observApp._playAudioFallback(audioFallbackBase);
                }
            }, 1500);
        }

    } catch (e) {
        console.warn('[ObservApp] speakTextWithFallback error:', e);
        window.observApp._playAudioFallback(audioFallbackBase);
    }
};

/**
 * Reproduce archivo de audio: intenta .mp3 primero, luego .wav, luego beep sintético.
 */
window.observApp._playAudioFallback = function (base) {
    try {
        if (!base) { window.observApp.playBeepTone(); return; }

        var tryPlay = function (src, onFail) {
            var audio = new Audio(src);
            audio.volume = 0.8;
            var p = audio.play();
            if (p !== undefined) {
                p.catch(function () { if (onFail) onFail(); });
            }
        };

        tryPlay(base + '.mp3', function () {
            tryPlay(base + '.wav', function () {
                window.observApp.playBeepTone();
            });
        });

    } catch (e) {
        window.observApp.playBeepTone();
    }
};

/**
 * Beep sintético — último recurso, sin archivos externos.
 */
window.observApp.playBeepTone = function () {
    try {
        var ctx = new (window.AudioContext || window.webkitAudioContext)();
        var osc = ctx.createOscillator();
        var gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.type = 'sine';
        osc.frequency.value = 880;
        gain.gain.setValueAtTime(0.4, ctx.currentTime);
        gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.6);
        osc.start(ctx.currentTime);
        osc.stop(ctx.currentTime + 0.6);
        osc.onended = function () { ctx.close(); };
    } catch (e) {
        console.warn('[ObservApp] playBeepTone error:', e);
    }
};

// Alias para compatibilidad con llamadas existentes
window.observApp.speakText = function (text) {
    window.observApp.speakTextWithFallback(
        text,
        '_content/ObservApp.Shared/sounds/eclipse-beep'
    );
};
window.observApp.playBeep = window.observApp.playBeepTone;


/* ============================================================
TTS GENÉRICO POR IDIOMA — usado por "Señales" para leer
artículos en el idioma del propio artículo (no en el idioma
activo de la UI). Sin fallback a beep: si no hay voz para el
idioma solicitado, simplemente no se reproduce nada.
============================================================ */

/**
 * Lee un texto con una voz que coincida con el código de idioma dado.
 * @param {string} text - Texto a leer
 * @param {string} languageCode - Código ISO de 2 letras (es, en, fr...)
 * @param {object} dotNetRef - DotNetObjectReference con método JSInvokable "OnSpeechEnded"
 * @returns {boolean} true si se encontró voz y se inició la lectura; false si no hay voz disponible
 */
window.observApp.speakWithLanguage = function (text, languageCode, dotNetRef) {
    try {
        if (!window.speechSynthesis || !text || !languageCode) return false;

        var langBase = languageCode.toLowerCase();

        var voices = window.speechSynthesis.getVoices();
        var matchedVoice = voices.find(function (v) {
            return v.lang.toLowerCase().startsWith(langBase);
        });

        if (!matchedVoice) {
            console.info('[ObservApp] Sin voz TTS para idioma "' + languageCode + '".');
            return false;
        }

        window.speechSynthesis.cancel();

        var utterance = new SpeechSynthesisUtterance(text);
        utterance.voice = matchedVoice;
        utterance.lang = matchedVoice.lang;
        utterance.rate = 1.0;
        utterance.pitch = 1.0;
        utterance.volume = 1.0;

        var notifyEnded = function () {
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync('OnSpeechEnded').catch(function () { });
            }
        };

        utterance.onend = notifyEnded;
        utterance.onerror = notifyEnded;

        window.speechSynthesis.speak(utterance);
        return true;

    } catch (e) {
        console.warn('[ObservApp] speakWithLanguage error:', e);
        return false;
    }
};

/**
 * Detiene cualquier lectura TTS en curso (de speakWithLanguage).
 */
window.observApp.stopSpeaking = function () {
    try {
        if (window.speechSynthesis) {
            window.speechSynthesis.cancel();
        }
    } catch (e) {
        console.warn('[ObservApp] stopSpeaking error:', e);
    }
};