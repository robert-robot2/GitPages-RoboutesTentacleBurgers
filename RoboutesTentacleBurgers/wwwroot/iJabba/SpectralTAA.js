// ============================================================
// SpectralTAA.js
// Temporal Anti-Aliasing System
// Extracted from SpectralEngine.js — SpectralGLInterop
//
// All shared state is read from window.SE:
//   window.SE.gl                — WebGL2 context
//   window.SE.canvas            — HTMLCanvasElement
//   window.SE.fxaaColorTex      — shared offscreen scene color texture (owned by engine)
//   window.SE.fullscreenQuadVbo — shared fullscreen quad VBO
//   window.SE.drawQuad          — shared fullscreen quad draw helper
//   window.SE.buildProgram      — shared program build helper
//
// SpectralEngine.js is responsible for:
//   - Calling window.SpectralTAA.init()       in init()
//   - Calling window.SpectralTAA.apply()      in renderFrame() when aaMode === 4
//   - Calling window.SpectralTAA.resize(w, h) in resizeAAfbos()
//   - Calling window.SpectralTAA.reset()      in flush()
//
// Load order: SpectralTAA.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralTAA = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================
    let _taaProgram = null;
    let _taaCurrentFbo = null;
    let _taaHistoryFbo = null;
    let _taaCurrentTex = null;
    let _taaHistoryTex = null;
    let _taaTexWidth = 0;
    let _taaTexHeight = 0;

    // ============================================================
    // SHADERS
    // ============================================================
    const _taaVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const _taaFsSrc = `
        precision mediump float;
        uniform sampler2D uCurrentTex;
        uniform sampler2D uHistoryTex;
        uniform vec2 uResolution;
        uniform float uBlend;
        varying vec2 vTexCoord;

        vec3 clipToAABB(vec3 color, vec3 minimum, vec3 maximum) {
            vec3 center  = 0.5 * (maximum + minimum);
            vec3 extents = 0.5 * (maximum - minimum);
            vec3 offset  = color - center;
            vec3 ts      = abs(extents / (offset + 0.0001));
            float t      = clamp(min(min(ts.x, ts.y), ts.z), 0.0, 1.0);
            return center + offset * t;
        }

        void main() {
            vec2 px = 1.0 / uResolution;

            vec3 current = texture2D(uCurrentTex, vTexCoord).rgb;

            // Build neighbourhood AABB — 3x3 around current pixel
            vec3 minC = current;
            vec3 maxC = current;
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    vec3 s = texture2D(uCurrentTex,
                        vTexCoord + vec2(float(x), float(y)) * px).rgb;
                    minC = min(minC, s);
                    maxC = max(maxC, s);
                }
            }

            // Shrink AABB slightly — clips stale history more aggressively
            vec3 center  = 0.5 * (minC + maxC);
            vec3 extents = 0.5 * (maxC - minC);
            minC = center - extents * 0.75;
            maxC = center + extents * 0.75;

            vec3 history = texture2D(uHistoryTex, vTexCoord).rgb;

            // Clip history into neighbourhood AABB to reduce ghosting
            history = clipToAABB(history, minC, maxC);

            vec3 blended = mix(current, history, uBlend);

            // Sharpening pass — counteracts TAA blur and kills jitter perception
            vec3 sharp = current - blended;
            blended    = blended + sharp * 0.3;
            blended    = clamp(blended, 0.0, 1.0);

            gl_FragColor = vec4(blended, 1.0);
        }
    `;

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================
    function _createColorTexture(w, h) {
        const gl = window.SE.gl;
        const tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        return tex;
    }

    function _createFboForTexture(tex, w, h) {
        const gl = window.SE.gl;
        const rb = gl.createRenderbuffer();
        gl.bindRenderbuffer(gl.RENDERBUFFER, rb);
        gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT16, w, h);
        const fbo = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, fbo);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
        gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT, gl.RENDERBUFFER, rb);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        return fbo;
    }

    function _destroyBuffers() {
        const gl = window.SE.gl;
        if (!gl) return;
        if (_taaCurrentFbo) { gl.deleteFramebuffer(_taaCurrentFbo); _taaCurrentFbo = null; }
        if (_taaCurrentTex) { gl.deleteTexture(_taaCurrentTex); _taaCurrentTex = null; }
        if (_taaHistoryFbo) { gl.deleteFramebuffer(_taaHistoryFbo); _taaHistoryFbo = null; }
        if (_taaHistoryTex) { gl.deleteTexture(_taaHistoryTex); _taaHistoryTex = null; }
    }

    function _buildBuffers(w, h) {
        _taaCurrentTex = _createColorTexture(w, h);
        _taaCurrentFbo = _createFboForTexture(_taaCurrentTex, w, h);
        _taaHistoryTex = _createColorTexture(w, h);
        _taaHistoryFbo = _createFboForTexture(_taaHistoryTex, w, h);
        _taaTexWidth = w;
        _taaTexHeight = h;
    }

    // ============================================================
    // PUBLIC — init
    // Called from SpectralEngine.js init() after window.SE is populated.
    // ============================================================
    function init() {
        const canvas = window.SE.canvas;

        _taaProgram = window.SE.buildProgram(_taaVsSrc, _taaFsSrc);
        if (!_taaProgram) {
            console.error('[SpectralTAA] Program failed to build');
            return;
        }

        _buildBuffers(canvas.width, canvas.height);

        // console.log('[SpectralTAA] Initialized', canvas.width, canvas.height);
    }

    // ============================================================
    // PUBLIC — apply
    // 3-step TAA resolve:
    //   1. Blend current frame (_fxaaColorTex) with history → _taaCurrentFbo
    //   2. Blit _taaCurrentTex → screen
    //   3. Copy _taaCurrentTex → _taaHistoryFbo for next frame
    //
    // Reads scene from window.SE.fxaaColorTex.
    // Called from SpectralEngine.js renderFrame() when aaMode === 4.
    // ============================================================
    function apply() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;
        const sceneTex = window.SE.fxaaColorTex;

        if (!_taaProgram || !sceneTex) return;

        // Guard — rebuild if canvas was resized before resize() was called
        if (_taaTexWidth !== canvas.width || _taaTexHeight !== canvas.height) {
            resize(canvas.width, canvas.height);
        }

        const w = canvas.width;
        const h = canvas.height;

        gl.disable(gl.DEPTH_TEST);
        gl.useProgram(_taaProgram);

        const uCurrent = gl.getUniformLocation(_taaProgram, 'uCurrentTex');
        const uHistory = gl.getUniformLocation(_taaProgram, 'uHistoryTex');
        const uResolution = gl.getUniformLocation(_taaProgram, 'uResolution');
        const uBlend = gl.getUniformLocation(_taaProgram, 'uBlend');

        // ── Step 1 — Blend current + history into _taaCurrentFbo ─────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _taaCurrentFbo);
        gl.viewport(0, 0, w, h);

        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);       // current frame
        gl.uniform1i(uCurrent, 0);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _taaHistoryTex); // previous frame
        gl.uniform1i(uHistory, 1);

        gl.uniform2f(uResolution, w, h);
        gl.uniform1f(uBlend, 0.97);

        window.SE.drawQuad(_taaProgram);

        // ── Step 2 — Blit _taaCurrentTex to screen (blend=0 = passthrough) ─
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, w, h);

        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, _taaCurrentTex);
        gl.uniform1i(uCurrent, 0);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _taaCurrentTex); // blend with itself = passthrough
        gl.uniform1i(uHistory, 1);

        gl.uniform2f(uResolution, w, h);
        gl.uniform1f(uBlend, 0.0); // 0% history = show current only

        window.SE.drawQuad(_taaProgram);

        // ── Step 3 — Copy current → history for next frame ───────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _taaHistoryFbo);

        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, _taaCurrentTex);
        gl.uniform1i(uCurrent, 0);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _taaCurrentTex);
        gl.uniform1i(uHistory, 1);

        gl.uniform2f(uResolution, w, h);
        gl.uniform1f(uBlend, 0.0);

        window.SE.drawQuad(_taaProgram);

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.enable(gl.DEPTH_TEST);
    }

    // ============================================================
    // PUBLIC — resize
    // Rebuilds ping-pong textures + FBOs at the new canvas size.
    // Called from SpectralEngine.js resizeAAfbos().
    // ============================================================
    function resize(w, h) {
        if (w <= 0 || h <= 0) return;
        _destroyBuffers();
        _buildBuffers(w, h);
        // console.log('[SpectralTAA] Resized', w, h);
    }

    // ============================================================
    // PUBLIC — reset
    // Deletes all GPU resources owned by this system.
    // Called from SpectralEngine.js flush().
    // ============================================================
    function reset() {
        _destroyBuffers();
        const gl = window.SE.gl;
        if (gl && _taaProgram) { gl.deleteProgram(_taaProgram); }
        _taaProgram = null;
        _taaTexWidth = 0;
        _taaTexHeight = 0;
        // console.log('[SpectralTAA] Reset');
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return {
        init,
        apply,
        resize,
        reset,
    };

})();