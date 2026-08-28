// ============================================================
// SpectralSMAA.js
// SMAA Anti-Aliasing System
// Extracted from SpectralEngine.js — SpectralGLInterop
//
// All shared state is read from window.SE:
//   window.SE.gl                — WebGL2 context
//   window.SE.canvas            — HTMLCanvasElement
//   window.SE.fxaaColorTex      — shared offscreen scene color texture (owned by engine)
//   window.SE.fullscreenQuadVbo — shared fullscreen quad VBO
//   window.SE.drawQuad          — shared fullscreen quad draw helper
//
// SpectralEngine.js is responsible for:
//   - Populating window.SE.smaaEdgeTex and window.SE.smaaBlendTex after init()
//   - Updating them in window.SE after resizeAAfbos() rebuilds them
//
// Load order: SpectralSMAA.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralSMAA = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================
    let _smaaProgram1 = null; // Pass 1 — Edge detection
    let _smaaProgram2 = null; // Pass 2 — Blend weights
    let _smaaProgram3 = null; // Pass 3 — Neighbourhood blend
    let _smaaEdgeFbo = null;
    let _smaaBlendFbo = null;
    let _smaaEdgeTex = null;
    let _smaaBlendTex = null;

    // ============================================================
    // SHADERS — Pass 1: Edge Detection
    // ============================================================
    const edgeVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        varying vec2 vOffset[3];
        uniform vec2 uResolution;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            vec2 px = 1.0 / uResolution;
            vOffset[0] = vTexCoord + px * vec2(-1.0,  0.0);
            vOffset[1] = vTexCoord + px * vec2( 0.0, -1.0);
            vOffset[2] = vTexCoord + px * vec2( 1.0,  0.0);
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const edgeFsSrc = `
        precision mediump float;
        uniform sampler2D uColorTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;
        varying vec2 vOffset[3];
        void main() {
            vec3 luma = vec3(0.2126, 0.7152, 0.0722);
            float L     = dot(texture2D(uColorTex, vTexCoord).rgb,  luma);
            float Lleft = dot(texture2D(uColorTex, vOffset[0]).rgb, luma);
            float Ltop  = dot(texture2D(uColorTex, vOffset[1]).rgb, luma);
            vec2 delta = abs(vec2(L - Lleft, L - Ltop));
            vec2 edges = step(0.1, delta);
            if (dot(edges, vec2(1.0)) == 0.0) discard;
            gl_FragColor = vec4(edges, 0.0, 1.0);
        }
    `;

    // ============================================================
    // SHADERS — Pass 2: Blend Weights
    // ============================================================
    const blendVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        uniform vec2 uResolution;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const blendFsSrc = `
        precision mediump float;
        uniform sampler2D uEdgeTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;
        void main() {
            vec2 px = 1.0 / uResolution;
            vec2 edges = texture2D(uEdgeTex, vTexCoord).rg;
            vec4 weights = vec4(0.0);
            if (edges.g > 0.0) {
                // Horizontal edge — search left/right
                float left  = 0.0;
                float right = 0.0;
                for (int i = 1; i <= 8; i++) {
                    if (texture2D(uEdgeTex, vTexCoord + vec2(-float(i), 0.0) * px).g > 0.0) left  += 1.0; else break;
                }
                for (int i = 1; i <= 8; i++) {
                    if (texture2D(uEdgeTex, vTexCoord + vec2( float(i), 0.0) * px).g > 0.0) right += 1.0; else break;
                }
                weights.r = 0.5 / (left + right + 1.0);
            }
            if (edges.r > 0.0) {
                // Vertical edge — search up/down
                float top    = 0.0;
                float bottom = 0.0;
                for (int i = 1; i <= 8; i++) {
                    if (texture2D(uEdgeTex, vTexCoord + vec2(0.0, -float(i)) * px).r > 0.0) top    += 1.0; else break;
                }
                for (int i = 1; i <= 8; i++) {
                    if (texture2D(uEdgeTex, vTexCoord + vec2(0.0,  float(i)) * px).r > 0.0) bottom += 1.0; else break;
                }
                weights.g = 0.5 / (top + bottom + 1.0);
            }
            gl_FragColor = weights;
        }
    `;

    // ============================================================
    // SHADERS — Pass 3: Neighbourhood Blend
    // ============================================================
    const nBlendVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const nBlendFsSrc = `
        precision mediump float;
        uniform sampler2D uColorTex;
        uniform sampler2D uBlendTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;
        void main() {
            vec2 px = 1.0 / uResolution;
            vec4 blendW = texture2D(uBlendTex, vTexCoord);
            vec4 color  = texture2D(uColorTex, vTexCoord);
            if (blendW.r > 0.0)
                color = mix(color, texture2D(uColorTex, vTexCoord + vec2( px.x, 0.0)), blendW.r);
            if (blendW.g > 0.0)
                color = mix(color, texture2D(uColorTex, vTexCoord + vec2(0.0,  px.y)), blendW.g);
            gl_FragColor = color;
        }
    `;

    // ============================================================
    // PRIVATE — shader compile helper
    // ============================================================
    function _compile(type, src) {
        const gl = window.SE.gl;
        const s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
            console.error('[SpectralSMAA] Shader compile error:', gl.getShaderInfoLog(s));
            gl.deleteShader(s);
            return null;
        }
        return s;
    }

    function _buildProgram(vsSrc, fsSrc) {
        const gl = window.SE.gl;
        const vs = _compile(gl.VERTEX_SHADER, vsSrc);
        const fs = _compile(gl.FRAGMENT_SHADER, fsSrc);
        if (!vs || !fs) return null;
        const p = gl.createProgram();
        gl.attachShader(p, vs);
        gl.attachShader(p, fs);
        gl.linkProgram(p);
        if (!gl.getProgramParameter(p, gl.LINK_STATUS)) {
            console.error('[SpectralSMAA] Program link error:', gl.getProgramInfoLog(p));
            return null;
        }
        return p;
    }

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

    // ============================================================
    // PUBLIC — init
    // Called from SpectralEngine.js init() after window.SE is populated.
    // No arguments — reads everything from window.SE.
    // After this call SpectralEngine.js must push smaaEdgeTex and
    // smaaBlendTex into window.SE for resizeAAfbos() to reference.
    // ============================================================
    function init() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;

        _smaaProgram1 = _buildProgram(edgeVsSrc, edgeFsSrc);
        _smaaProgram2 = _buildProgram(blendVsSrc, blendFsSrc);
        _smaaProgram3 = _buildProgram(nBlendVsSrc, nBlendFsSrc);

        if (!_smaaProgram1 || !_smaaProgram2 || !_smaaProgram3) {
            console.error('[SpectralSMAA] One or more programs failed to build');
            return;
        }

        _smaaEdgeTex = _createColorTexture(canvas.width, canvas.height);
        _smaaEdgeFbo = _createFboForTexture(_smaaEdgeTex, canvas.width, canvas.height);

        _smaaBlendTex = _createColorTexture(canvas.width, canvas.height);
        _smaaBlendFbo = _createFboForTexture(_smaaBlendTex, canvas.width, canvas.height);

        // Expose textures to window.SE so resizeAAfbos() can resize them
        window.SE.smaaEdgeTex = _smaaEdgeTex;
        window.SE.smaaBlendTex = _smaaBlendTex;

        // console.log('[SpectralSMAA] Initialized', canvas.width, canvas.height);
    }

    // ============================================================
    // PUBLIC — apply
    // 3-pass SMAA resolve. Reads scene from window.SE.fxaaColorTex,
    // writes final result to the default framebuffer.
    // Called from SpectralEngine.js renderFrame() when aaMode === 3.
    // ============================================================
    function apply() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;
        const sceneTex = window.SE.fxaaColorTex;

        if (!_smaaProgram1 || !sceneTex) return;

        const w = canvas.width;
        const h = canvas.height;

        // ── Pass 1 — Edge detection into _smaaEdgeFbo ────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _smaaEdgeFbo);
        gl.viewport(0, 0, w, h);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.disable(gl.DEPTH_TEST);
        gl.useProgram(_smaaProgram1);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_smaaProgram1, 'uColorTex'), 0);
        gl.uniform2f(gl.getUniformLocation(_smaaProgram1, 'uResolution'), w, h);
        window.SE.drawQuad(_smaaProgram1);

        // ── Pass 2 — Blend weights into _smaaBlendFbo ────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _smaaBlendFbo);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.useProgram(_smaaProgram2);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, _smaaEdgeTex);
        gl.uniform1i(gl.getUniformLocation(_smaaProgram2, 'uEdgeTex'), 0);
        gl.uniform2f(gl.getUniformLocation(_smaaProgram2, 'uResolution'), w, h);
        window.SE.drawQuad(_smaaProgram2);

        // ── Pass 3 — Neighbourhood blend to screen ────────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, w, h);
        gl.useProgram(_smaaProgram3);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_smaaProgram3, 'uColorTex'), 0);
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _smaaBlendTex);
        gl.uniform1i(gl.getUniformLocation(_smaaProgram3, 'uBlendTex'), 1);
        gl.uniform2f(gl.getUniformLocation(_smaaProgram3, 'uResolution'), w, h);
        window.SE.drawQuad(_smaaProgram3);

        gl.enable(gl.DEPTH_TEST);
    }

    // ============================================================
    // PUBLIC — reset
    // Deletes all GPU resources owned by this system.
    // FBOs, textures, and programs all cleaned up here.
    // Called from SpectralEngine.js flush().
    // ============================================================
    function reset() {
        const gl = window.SE.gl;
        if (!gl) return;

        if (_smaaProgram1) { gl.deleteProgram(_smaaProgram1); _smaaProgram1 = null; }
        if (_smaaProgram2) { gl.deleteProgram(_smaaProgram2); _smaaProgram2 = null; }
        if (_smaaProgram3) { gl.deleteProgram(_smaaProgram3); _smaaProgram3 = null; }
        if (_smaaEdgeTex) { gl.deleteTexture(_smaaEdgeTex); _smaaEdgeTex = null; }
        if (_smaaBlendTex) { gl.deleteTexture(_smaaBlendTex); _smaaBlendTex = null; }
        if (_smaaEdgeFbo) { gl.deleteFramebuffer(_smaaEdgeFbo); _smaaEdgeFbo = null; }
        if (_smaaBlendFbo) { gl.deleteFramebuffer(_smaaBlendFbo); _smaaBlendFbo = null; }

        window.SE.smaaEdgeTex = null;
        window.SE.smaaBlendTex = null;

        // console.log('[SpectralSMAA] Reset');
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return {
        init,
        apply,
        reset,
    };

})();