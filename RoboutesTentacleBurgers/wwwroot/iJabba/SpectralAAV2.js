// ============================================================
// SpectralAAV2.js
// Spectral AA V2 — Staircase Detection + Triangle Fill Composite
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
//   - Populating window.SE.spectralV2EdgeTex after init()
//   - Updating it in window.SE after resizeAAfbos() rebuilds it
//
// Load order: SpectralAAV2.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralAAV2 = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================
    let _spectralV2Program1 = null; // Pass 1 — Staircase topology detection
    let _spectralV2Program2 = null; // Pass 2 — Triangle fill composite
    let _spectralV2EdgeFbo = null;
    let _spectralV2EdgeTex = null;

    // ============================================================
    // SHADERS — Pass 1: Staircase Topology Detection
    // ============================================================
    const detectVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const detectFsSrc = `
        precision mediump float;
        uniform sampler2D uColorTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        float luma(vec3 c) {
            return dot(c, vec3(0.2126, 0.7152, 0.0722));
        }

        void main() {
            vec2 px = 1.0 / uResolution;

            float C  = luma(texture2D(uColorTex, vTexCoord).rgb);
            float R  = luma(texture2D(uColorTex, vTexCoord + vec2( px.x,  0.0)).rgb);
            float L  = luma(texture2D(uColorTex, vTexCoord + vec2(-px.x,  0.0)).rgb);
            float U  = luma(texture2D(uColorTex, vTexCoord + vec2( 0.0,   px.y)).rgb);
            float D  = luma(texture2D(uColorTex, vTexCoord + vec2( 0.0,  -px.y)).rgb);
            float DR = luma(texture2D(uColorTex, vTexCoord + vec2( px.x, -px.y)).rgb);
            float DL = luma(texture2D(uColorTex, vTexCoord + vec2(-px.x, -px.y)).rgb);
            float UR = luma(texture2D(uColorTex, vTexCoord + vec2( px.x,  px.y)).rgb);
            float UL = luma(texture2D(uColorTex, vTexCoord + vec2(-px.x,  px.y)).rgb);

            float threshold = 0.08;

            // Check all 4 staircase corner patterns:
            // Pattern: differs from orthogonal neighbours, matches diagonal

            // Bottom-right staircase corner
            float dR  = abs(C - R);
            float dD  = abs(C - D);
            float dDR = abs(C - DR);
            if (dR > threshold && dD > threshold && dDR < threshold * 0.5) {
                float coverage = dD / (dR + dD + 0.0001);
                gl_FragColor = vec4(1.0, coverage, dR, dD);
                return;
            }

            // Bottom-left staircase corner
            float dL  = abs(C - L);
            float dDL = abs(C - DL);
            if (dL > threshold && dD > threshold && dDL < threshold * 0.5) {
                float coverage = dD / (dL + dD + 0.0001);
                gl_FragColor = vec4(2.0 / 4.0, coverage, dL, dD);
                return;
            }

            // Top-right staircase corner
            float dU  = abs(C - U);
            float dUR = abs(C - UR);
            if (dR > threshold && dU > threshold && dUR < threshold * 0.5) {
                float coverage = dU / (dR + dU + 0.0001);
                gl_FragColor = vec4(3.0 / 4.0, coverage, dR, dU);
                return;
            }

            // Top-left staircase corner
            float dUL = abs(C - UL);
            if (dL > threshold && dU > threshold && dUL < threshold * 0.5) {
                float coverage = dU / (dL + dU + 0.0001);
                gl_FragColor = vec4(0.25, coverage, dL, dU);
                return;
            }

            // Not a staircase pixel
            gl_FragColor = vec4(0.0);
        }
    `;

    // ============================================================
    // SHADERS — Pass 2: Triangle Fill Composite
    // ============================================================
    const fillVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const fillFsSrc = `
        precision mediump float;
        uniform sampler2D uColorTex;
        uniform sampler2D uStaircaseTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        void main() {
            vec2 px = 1.0 / uResolution;
            vec4 staircase = texture2D(uStaircaseTex, vTexCoord);
            vec4 colorC = texture2D(uColorTex, vTexCoord);

            // No staircase detected — pass through completely untouched
            if (staircase.a < 0.01) {
                gl_FragColor = colorC;
                return;
            }

            float cornerType = staircase.r;
            float coverage   = staircase.g;

            // Clamp coverage to reasonable triangle fill range
            coverage = clamp(coverage, 0.1, 0.9);

            // Fetch the fill colour from the diagonal neighbour
            // (the colour that should fill the triangular gap)
            vec4 fillColor;

            if (cornerType > 0.9) {
                // Bottom-right corner — fill from bottom-right diagonal
                fillColor = texture2D(uColorTex, vTexCoord + vec2( px.x, -px.y));
            } else if (cornerType > 0.68) {
                // Top-right corner — fill from top-right diagonal
                fillColor = texture2D(uColorTex, vTexCoord + vec2( px.x,  px.y));
            } else if (cornerType > 0.37) {
                // Bottom-left corner — fill from bottom-left diagonal
                fillColor = texture2D(uColorTex, vTexCoord + vec2(-px.x, -px.y));
            } else {
                // Top-left corner — fill from top-left diagonal
                fillColor = texture2D(uColorTex, vTexCoord + vec2(-px.x,  px.y));
            }

            // Triangle fill — replace exactly the coverage fraction
            // with the diagonal colour. No blur — pure geometric replacement.
            gl_FragColor = mix(colorC, fillColor, coverage * 0.5);
        }
    `;

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================
    function _compile(type, src) {
        const gl = window.SE.gl;
        const s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
            console.error('[SpectralAAV2] Shader compile error:', gl.getShaderInfoLog(s));
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
            console.error('[SpectralAAV2] Program link error:', gl.getProgramInfoLog(p));
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
    // Exposes spectralV2EdgeTex to window.SE for resizeAAfbos() to reference.
    // ============================================================
    function init() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;

        _spectralV2Program1 = _buildProgram(detectVsSrc, detectFsSrc);
        _spectralV2Program2 = _buildProgram(fillVsSrc, fillFsSrc);

        if (!_spectralV2Program1 || !_spectralV2Program2) {
            console.error('[SpectralAAV2] One or more programs failed to build');
            return;
        }

        _spectralV2EdgeTex = _createColorTexture(canvas.width, canvas.height);
        _spectralV2EdgeFbo = _createFboForTexture(_spectralV2EdgeTex, canvas.width, canvas.height);

        // Expose texture to window.SE so resizeAAfbos() can resize it
        window.SE.spectralV2EdgeTex = _spectralV2EdgeTex;

        // console.log('[SpectralAAV2] Initialized', canvas.width, canvas.height);
    }

    // ============================================================
    // PUBLIC — apply
    // 2-pass Spectral AA V2 resolve. Reads scene from window.SE.fxaaColorTex,
    // writes final result to the default framebuffer.
    // Called from SpectralEngine.js renderFrame() when aaMode === 6.
    // ============================================================
    function apply() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;
        const sceneTex = window.SE.fxaaColorTex;

        if (!_spectralV2Program1 || !sceneTex) return;

        const w = canvas.width;
        const h = canvas.height;

        // ── Pass 1 — Staircase detection into _spectralV2EdgeFbo ─────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _spectralV2EdgeFbo);
        gl.viewport(0, 0, w, h);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.disable(gl.DEPTH_TEST);
        gl.useProgram(_spectralV2Program1);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV2Program1, 'uColorTex'), 0);
        gl.uniform2f(gl.getUniformLocation(_spectralV2Program1, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralV2Program1);

        // ── Pass 2 — Triangle fill to screen ─────────────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, w, h);
        gl.useProgram(_spectralV2Program2);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV2Program2, 'uColorTex'), 0);
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _spectralV2EdgeTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV2Program2, 'uStaircaseTex'), 1);
        gl.uniform2f(gl.getUniformLocation(_spectralV2Program2, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralV2Program2);

        gl.enable(gl.DEPTH_TEST);
    }

    // ============================================================
    // PUBLIC — reset
    // Deletes all GPU resources owned by this system.
    // Called from SpectralEngine.js flush().
    // ============================================================
    function reset() {
        const gl = window.SE.gl;
        if (!gl) return;

        if (_spectralV2Program1) { gl.deleteProgram(_spectralV2Program1); _spectralV2Program1 = null; }
        if (_spectralV2Program2) { gl.deleteProgram(_spectralV2Program2); _spectralV2Program2 = null; }
        if (_spectralV2EdgeTex) { gl.deleteTexture(_spectralV2EdgeTex); _spectralV2EdgeTex = null; }
        if (_spectralV2EdgeFbo) { gl.deleteFramebuffer(_spectralV2EdgeFbo); _spectralV2EdgeFbo = null; }

        window.SE.spectralV2EdgeTex = null;

        // console.log('[SpectralAAV2] Reset');
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