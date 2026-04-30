// game.js — Sound effects (Web Audio API, no external assets) + helpers
window.gameSound = (() => {
    let _ctx = null;

    function getCtx() {
        if (!_ctx) {
            try { _ctx = new (window.AudioContext || window.webkitAudioContext)(); }
            catch { return null; }
        }
        if (_ctx.state === 'suspended') _ctx.resume();
        return _ctx;
    }

    function tone(ctx, freq, type, startTime, duration, vol) {
        const osc  = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);
        osc.type      = type;
        osc.frequency.setValueAtTime(freq, startTime);
        gain.gain.setValueAtTime(vol, startTime);
        gain.gain.exponentialRampToValueAtTime(0.0001, startTime + duration);
        osc.start(startTime);
        osc.stop(startTime + duration + 0.01);
    }

    function playClick() {
        const ctx = getCtx(); if (!ctx) return;
        const t = ctx.currentTime;
        tone(ctx, 880, 'sine',    t,       0.05, 0.25);
        tone(ctx, 440, 'sine',    t + 0.02, 0.06, 0.15);
    }

    function playWin() {
        const ctx = getCtx(); if (!ctx) return;
        const t = ctx.currentTime;
        const melody = [523.25, 659.25, 783.99, 1046.50];
        melody.forEach((f, i) => tone(ctx, f, 'sine', t + i * 0.14, 0.25, 0.28));
        // sparkle layer
        [1318, 1567, 2093].forEach((f, i) =>
            tone(ctx, f, 'triangle', t + 0.5 + i * 0.1, 0.12, 0.12));
    }

    function playLose() {
        const ctx = getCtx(); if (!ctx) return;
        const t = ctx.currentTime;
        [392, 311.13, 261.63].forEach((f, i) =>
            tone(ctx, f, 'sawtooth', t + i * 0.18, 0.22, 0.18));
    }

    function playDraw() {
        const ctx = getCtx(); if (!ctx) return;
        const t = ctx.currentTime;
        tone(ctx, 440, 'sine', t,       0.1,  0.2);
        tone(ctx, 440, 'sine', t + 0.15, 0.1, 0.15);
        tone(ctx, 330, 'sine', t + 0.35, 0.25, 0.18);
    }

    function playChat() {
        const ctx = getCtx(); if (!ctx) return;
        tone(ctx, 1200, 'sine', ctx.currentTime, 0.06, 0.1);
    }

    return {
        play(type) {
            try {
                switch (type) {
                    case 'click': playClick(); break;
                    case 'win':   playWin();   break;
                    case 'lose':  playLose();  break;
                    case 'draw':  playDraw();  break;
                    case 'chat':  playChat();  break;
                }
            } catch { /* audio blocked — silent fail */ }
        },
        scrollChat(id) {
            const el = document.getElementById(id);
            if (el) el.scrollTop = el.scrollHeight;
        }
    };
})();
