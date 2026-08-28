// ============================================================
// SpectralScrollbarSystem.js
// Scrollbar renderer — SpectralX WebGL2 Engine
// Extracted from SpectralEngine.js — Phase 2 migration
// Reads shared GL context via window.SE
// ============================================================

window.SpectralScrollbarSystem = (function () {

    // ============================================================
    // STATE
    // ============================================================
    let _scrollbarProgram = null;
    let _scrollbarLocs = null;
    let _scrollbarVbo = null;
    let _scrollbarMinZ = -60;
    let _scrollbarMaxZ = 10;
    let _scrollbarCurrentZ = 10;
    let _scrollbarGlowPhase = 0;

    // ============================================================
    // SHADERS
    // ============================================================
    const vsSrc = `#version 300 es
        in vec2 aPosition;
        void main() {
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }`;

    const fsSrc = `#version 300 es
        precision mediump float;
        uniform vec4 uColor;
        uniform float uGlow;
        out vec4 fragColor;
        void main() {
            fragColor = vec4(uColor.rgb * (1.0 + uGlow), uColor.a);
        }`;

    // ============================================================
    // HELPERS
    // ============================================================
    function hexToRgb(hex) {
        const r = parseInt(hex.slice(1, 3), 16) / 255;
        const g = parseInt(hex.slice(3, 5), 16) / 255;
        const b = parseInt(hex.slice(5, 7), 16) / 255;
        return { r, g, b };
    }

    function drawRect(x0, y0, x1, y1, r, g, b, a, glow) {
        const gl = SE.gl;
        const verts = new Float32Array([
            x0, y0, x1, y0, x0, y1,
            x0, y1, x1, y0, x1, y1,
        ]);
        gl.bindBuffer(gl.ARRAY_BUFFER, _scrollbarVbo);
        gl.bufferData(gl.ARRAY_BUFFER, verts, gl.DYNAMIC_DRAW);
        gl.enableVertexAttribArray(_scrollbarLocs.pos);
        gl.vertexAttribPointer(_scrollbarLocs.pos, 2, gl.FLOAT, false, 0, 0);
        gl.uniform4f(_scrollbarLocs.color, r, g, b, a);
        gl.uniform1f(_scrollbarLocs.glow, glow);
        gl.drawArrays(gl.TRIANGLES, 0, 6);
    }

    // ============================================================
    // INIT
    // ============================================================
    function init() {
        const gl = SE.gl;

        _scrollbarProgram = SE.buildProgram(vsSrc, fsSrc);
        _scrollbarLocs = {
            pos: gl.getAttribLocation(_scrollbarProgram, 'aPosition'),
            color: gl.getUniformLocation(_scrollbarProgram, 'uColor'),
            glow: gl.getUniformLocation(_scrollbarProgram, 'uGlow'),
        };

        _scrollbarVbo = gl.createBuffer();
        console.log('[SpectralScrollbarSystem] Initialized');
    }

    // ============================================================
    // RENDER
    // ============================================================
    function render(frame) {
        if (!_scrollbarProgram) init();
        if (!frame || frame.cameraMode !== 0) return;
        if (frame.activeScene !== 3) return;

        const gl = SE.gl;

        // Pull theme color from localStorage
        const hex = localStorage.getItem("themeColor") || "#00aaff";
        const tc = hexToRgb(hex);

        _scrollbarGlowPhase += 0.05;
        const glow = (Math.sin(_scrollbarGlowPhase) * 0.5 + 0.5) * 2.0;

        const trackX0 = 0.96;
        const trackX1 = 1.0;
        const trackY0 = -0.98;
        const trackY1 = 0.98;

        const range = _scrollbarMaxZ - _scrollbarMinZ;
        const t = 1.0 - ((_scrollbarCurrentZ - _scrollbarMinZ) / range);
        const thumbH = 0.15;
        const thumbY1 = trackY1 - t * (trackY1 - trackY0 - thumbH);
        const thumbY0 = thumbY1 - thumbH;

        gl.disable(gl.DEPTH_TEST);
        gl.depthMask(false);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.useProgram(_scrollbarProgram);

        // Track — dark tint of theme color
        drawRect(
            trackX0, trackY0, trackX1, trackY1,
            tc.r * 0.2, tc.g * 0.2, tc.b * 0.3, 0.6,
            glow * 0.3
        );

        // Thumb — full theme color with glow
        drawRect(
            trackX0 - 0.01, thumbY0, trackX1 + 0.01, thumbY1,
            tc.r, tc.g, tc.b, 0.95,
            glow
        );

        // Inner highlight — lighter version
        drawRect(
            trackX0 + 0.005, thumbY0 + 0.005, trackX1 - 0.005, thumbY1 - 0.005,
            Math.min(tc.r + 0.3, 1.0), Math.min(tc.g + 0.3, 1.0), Math.min(tc.b + 0.3, 1.0), 0.8,
            glow * 1.5
        );

        gl.enable(gl.DEPTH_TEST);
        gl.depthMask(true);
    }

    // ============================================================
    // SET Z — called from SpectralEngine renderFrame
    // ============================================================
    function setCurrentZ(z) {
        _scrollbarCurrentZ = z;
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return { init, render, setCurrentZ };

})();