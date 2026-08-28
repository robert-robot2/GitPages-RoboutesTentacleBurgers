


// SpectralUI.js
// SpectralUI.js
// SpectralUI.js
// ADD at the very top of SpectralUI.js, before all the window.* assignments:
window._spectralFullscreenTransition = false;

window.toggleFullscreen = function (elementId) {
    const elem = document.getElementById(elementId);
    if (!elem) return;
    if (!document.fullscreenElement) {
        if (elem.requestFullscreen) elem.requestFullscreen();
        else if (elem.webkitRequestFullscreen) elem.webkitRequestFullscreen();
    } else {
        if (document.exitFullscreen) document.exitFullscreen();
    }
};
// ADD at very top of SpectralUI.js
window._spectralFullscreenTransition = false;

// REPLACE getViewportSize:
window.getViewportSize = function (id, isExiting) {
    // Entering fullscreen — screen dimensions are always correct here
    if (!isExiting && document.fullscreenElement) {
        return { width: window.screen.width, height: window.screen.height };
    }

    // Exiting OR normal read — go straight to container
    // Never read canvas rect here — canvas buffer hasn't resized yet on exit
    const el = document.getElementById(id);
    if (el) {
        const rect = el.getBoundingClientRect();
        if (rect.width > 100 && rect.height > 100) {
            return { width: Math.floor(rect.width), height: Math.floor(rect.height) };
        }
    }

    // Hard fallback
    return { width: 1024, height: 768 };
};

// REPLACE watchCanvasSize:
window.watchCanvasSize = function (containerId, dotnetRef) {
    const canvas = document.getElementById('SpectralX-Viewport');
    if (!canvas) return;

    let _resizeTimer = null;

    const ro = new ResizeObserver(entries => {
        for (const entry of entries) {
            const w = Math.floor(entry.contentRect.width);
            const h = Math.floor(entry.contentRect.height);
            if (w < 100 || h < 100) return;

            // Debounce — ignore rapid intermediate sizes during
            // fullscreen enter/exit CSS transitions
            clearTimeout(_resizeTimer);
            _resizeTimer = setTimeout(() => {
                // Stand down if fullscreen toggle is in progress —
                // ToggleViewport() and registerFullscreenListener own
                // the resize in that case
                if (window._spectralFullscreenTransition) return;

                if (window.SpectralGLInterop?.resizeCanvas)
                    window.SpectralGLInterop.resizeCanvas(w, h);

                dotnetRef.invokeMethodAsync('OnCanvasResized', w, h);
                console.log('[ResizeObserver] settled at', w, 'x', h);
            }, 300);
        }
    });
    ro.observe(canvas);
};

// REPLACE registerFullscreenListener:
window.registerFullscreenListener = function (dotnetRef, elementId) {
    document.addEventListener("fullscreenchange", () => {

        // Block ResizeObserver from firing during transition
        window._spectralFullscreenTransition = true;

        if (!document.fullscreenElement) {
            // Exiting fullscreen — Escape key or button
            setTimeout(() => {
                // isExiting = true — reads container CSS, never canvas
                const size = window.getViewportSize(elementId, true);
                const w = size?.width || 1024;
                const h = size?.height || 768;

                if (window.SpectralGLInterop?.resizeCanvas)
                    window.SpectralGLInterop.resizeCanvas(w, h);

                dotnetRef.invokeMethodAsync('OnCanvasResized', w, h);
                console.log('[FullscreenListener] exit restore:', w, 'x', h);

                setTimeout(() => {
                    window._spectralFullscreenTransition = false;
                }, 300);

            }, 200);

        } else {
            // Entering fullscreen
            setTimeout(() => {
                const w = window.screen.width;
                const h = window.screen.height;

                if (window.SpectralGLInterop?.resizeCanvas)
                    window.SpectralGLInterop.resizeCanvas(w, h);

                dotnetRef.invokeMethodAsync('OnCanvasResized', w, h);
                console.log('[FullscreenListener] enter:', w, 'x', h);

                setTimeout(() => {
                    window._spectralFullscreenTransition = false;
                }, 300);

            }, 150);
        }
    });
};

window.GamepadAPI = {
    dotNetRef: null, isPolling: false, rafId: null, deadZone: 0.15,

    init: function (ref) {
        this.dotNetRef = ref;
        // Only start polling if gamepad actually connected
        window.addEventListener('gamepadconnected', () => this.startPolling());
    },


    startPolling: function () {
        if (this.isPolling) return;
        this.isPolling = true;
        const poll = () => { if (!this.isPolling) return; this.update(); this.rafId = requestAnimationFrame(poll); };
        poll();
    },
    stopPolling: function () {
        this.isPolling = false;
        if (this.rafId) { cancelAnimationFrame(this.rafId); this.rafId = null; }
    },
    update: function () {
        const gamepads = navigator.getGamepads();
        let gp = null;
        for (let i = 0; i < gamepads.length; i++) { if (gamepads[i]?.mapping === 'standard') { gp = gamepads[i]; break; } }
        if (!gp) for (let i = 0; i < gamepads.length; i++) { if (gamepads[i]) { gp = gamepads[i]; break; } }
        if (!gp) return;
        const dz = (v) => Math.abs(v) < this.deadZone ? 0 : v;
        const state = {
            leftStickX: -dz(gp.axes[0] || 0), leftStickY: -dz(gp.axes[1] || 0),
            rightStickX: -dz(gp.axes[2] || 0), rightStickY: -dz(gp.axes[3] || 0),
            buttonA: gp.buttons[0]?.pressed || false, buttonB: gp.buttons[1]?.pressed || false,
            buttonX: gp.buttons[2]?.pressed || false, buttonY: gp.buttons[3]?.pressed || false,
            leftBumper: gp.buttons[4]?.pressed || false, rightBumper: gp.buttons[5]?.pressed || false,
            leftTrigger: gp.buttons[6]?.value || 0, rightTrigger: gp.buttons[7]?.value || 0,
            buttonBack: gp.buttons[8]?.pressed || false, buttonStart: gp.buttons[9]?.pressed || false,
            leftStickButton: gp.buttons[10]?.pressed || false, rightStickButton: gp.buttons[11]?.pressed || false,
            dpadUp: gp.buttons[12]?.pressed || false, dpadDown: gp.buttons[13]?.pressed || false,
            dpadLeft: gp.buttons[14]?.pressed || false, dpadRight: gp.buttons[15]?.pressed || false
        };
        if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('UpdateGamepadState', state);
    },
    dispose: function () { this.stopPolling(); this.dotNetRef = null; }
};

window.addEventListener('gamepadconnected', (e) => { console.log('[GamepadAPI] connected:', e.gamepad.id); });
window.addEventListener('gamepaddisconnected', (e) => { console.log('[GamepadAPI] disconnected:', e.gamepad.id); });



// ═══ SPECTRAL ENGINE INIT LOADER ═══
window.SpectralEngineLoader = {
    _requested: 0,
    _completed: 0,
    _visible: false,
  
    show: function () {
        const el = document.getElementById('spectral-engine-init');
        console.log('[Loader] show() — el found:', !!el);
        if (el) { el.style.display = 'flex'; this._visible = true; }
    },

    hide: function () {
        const el = document.getElementById('spectral-engine-init');
        if (el) { el.style.display = 'none'; this._visible = false; }
    },
    setStatus: function (msg) {
        const el = document.getElementById('sei-status');
        if (el) el.textContent = msg;
    },

    ready: function () {
        if (this._dismissed) return;
        this._dismissed = true;

        clearTimeout(this._fallbackTimer);

        const el = document.getElementById('spectral-engine-init');
        if (!el) return;

        this.setStatus('WebGL2 Ready ✓');

        setTimeout(() => {
            el.classList.add('sei-hidden');
            setTimeout(() => {
                el.remove();
            }, 650);
        }, 600);
    },

    reset: function () {
        this._requested = 0;
        this._completed = 0;
     
        this.updateDisplay(0);
        this.show();

       
    },
    forceHide: function () {
        clearTimeout(this._killTimer);
        this.hide();   // Requires a hide() method
    }
};

window.SpectralEngineLoader._killTimer = setTimeout(() => {
    if (window.SpectralEngineLoader._visible) {
        console.warn('[SpectralGLLoader] force-hiding after 12 s');
        window.SpectralEngineLoader.forceHide();
    }
}, 1200);

// this loader is incorrectly built it does work but on bwpscene 2 gets stuck on loading tile map for spectralloader

/*

window.SpectralEngineLoader = {
    _dismissed: false,

    // Call this on every Blazor page navigation to reset state
    reset: function () {
        this._dismissed = false;
        const el = document.getElementById('spectral-engine-init');
        if (el) {
            el.classList.remove('sei-hidden');
            el.style.display = 'flex';
        }
        this.setStatus('Loading...');
    },

    setStatus: function (msg) {
        const el = document.getElementById('sei-status');
        if (el) el.textContent = msg;
    },

    ready: function () {
        if (this._dismissed) return;
        this._dismissed = true;
        const el = document.getElementById('spectral-engine-init');
        if (!el) return;
        this.setStatus('Ready ✓');
        setTimeout(() => {
            el.classList.add('sei-hidden');
            setTimeout(() => el.style.display = 'none', 650);
        }, 400);
    },
};


*/




window.SpectralGLLoader = {
    _requested: 0,
    _completed: 0,
    _visible: false,
    _specialFlags: { tilemap: false, cubemap: false },
    _needsSpecial: { tilemap: false, cubemap: false },

    show: function () {
        const el = document.getElementById('SpectralX-Loader');
        console.log('[Loader] show() — el found:', !!el);
        if (el) { el.style.display = 'flex'; this._visible = true; }
    },

    hide: function () {
        const el = document.getElementById('SpectralX-Loader');
        if (el) { el.style.display = 'none'; this._visible = false; }
    },

    reset: function (needsTilemap, needsCubemap) {
        this._requested = 0;
        this._completed = 0;
        this._specialFlags.tilemap = false;
        this._specialFlags.cubemap = false;
        this._needsSpecial.tilemap = needsTilemap || false;
        this._needsSpecial.cubemap = needsCubemap || false;
        this.updateDisplay(0);
        this.show();

        if (window.SpectralGLInterop?.resetParticles)
            window.SpectralGLInterop.resetParticles();

        // If tilemap grid is already built from a previous scene load,
        // signal complete immediately — initTileMap won't run again
        if (needsTilemap && window._tileGridReady) {
            setTimeout(() => this.onSpecialComplete('tilemap'), 0);
        }
    },

    onAssetRequested: function () {
        this._requested++;
    },

    onAssetComplete: function () {
        this._completed++;
        const pct = this.getPercentage();
        this.updateDisplay(pct);
        if (pct >= 100) this.hide();
    },
    onSpecialComplete: function (flag) {
        this._specialFlags[flag] = true;
        this.updateDisplay(this.getPercentage());

    },

    getPercentage: function () {
        if (this._requested === 0) return 100;
        let basePercent = (this._completed / this._requested) * 100;

        // Special flags each reserve a slice of the last 10%
        let specialCount = 0;
        let specialDone = 0;
        if (this._needsSpecial.tilemap) { specialCount++; if (this._specialFlags.tilemap) specialDone++; }
        if (this._needsSpecial.cubemap) { specialCount++; if (this._specialFlags.cubemap) specialDone++; }

        if (specialCount > 0) {
            basePercent = Math.min(basePercent, 90);
            basePercent += (specialDone / specialCount) * 10;
        }

        return Math.min(Math.floor(basePercent), 100);
    },

    isDone: function () {
        if (!this._visible) return false;
        if (this._completed < this._requested) return false;
        // remove these two lines:
        // if (this._needsSpecial.tilemap && !this._specialFlags.tilemap) return false;
        // if (this._needsSpecial.cubemap && !this._specialFlags.cubemap) return false;
        return true;
    },

    updateDisplay: function (pct) {
        const text = document.getElementById('SpectralX-Loader');
        if (!text) return;
        const textEl = text.querySelector('.sx-loading-text');
        const circle = text.querySelector('.sx-loading-progress circle:last-child');
        if (textEl) textEl.textContent = pct + '%';
        if (circle) {
            const circumference = 251.2;
            const fill = (pct / 100) * circumference;
            circle.style.strokeDasharray = fill + ',' + circumference;
        }
    }
};

