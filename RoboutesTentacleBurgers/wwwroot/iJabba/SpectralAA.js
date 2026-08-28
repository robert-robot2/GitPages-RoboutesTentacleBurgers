// ============================================================
// SpectralAA.js
// Spectral AA V1 — Gradient Edge Detection + Geometric Composite
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
//   - Populating window.SE.spectralEdgeTex after init()
//   - Updating it in window.SE after resizeAAfbos() rebuilds it
//
// Load order: SpectralAA.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralAA = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================
    let _spectralProgram1 = null; // Pass 1 — Edge detection with angle encoding
    let _spectralProgram2 = null; // Pass 2 — Geometric subpixel composite
    let _spectralEdgeFbo = null;
    let _spectralEdgeTex = null;

    // ============================================================
    // SHADERS — Pass 1: Edge Detection with Angle Encoding
    // ============================================================
    const edgeVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const edgeFsSrc = `
        precision mediump float;
        uniform sampler2D uColorTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        void main() {
            vec2 px = 1.0 / uResolution;
            vec3 luma = vec3(0.2126, 0.7152, 0.0722);

            float c  = dot(texture2D(uColorTex, vTexCoord).rgb, luma);
            float cR = dot(texture2D(uColorTex, vTexCoord + vec2( px.x, 0.0)).rgb, luma);
            float cL = dot(texture2D(uColorTex, vTexCoord + vec2(-px.x, 0.0)).rgb, luma);
            float cU = dot(texture2D(uColorTex, vTexCoord + vec2(0.0,  px.y)).rgb, luma);
            float cD = dot(texture2D(uColorTex, vTexCoord + vec2(0.0, -px.y)).rgb, luma);

            float dX = cR - cL;
            float dY = cU - cD;
            float edgeStrength = sqrt(dX * dX + dY * dY);

            if (edgeStrength < 0.08) {
                gl_FragColor = vec4(0.0);
                return;
            }

            // Encode edge angle into RG, strength into B
            // Normalize gradient direction to 0..1 range for storage
            float angleX = dX * 0.5 + 0.5;
            float angleY = dY * 0.5 + 0.5;
            gl_FragColor = vec4(angleX, angleY, edgeStrength, 1.0);
        }
    `;

    // ============================================================
    // SHADERS — Pass 2: Geometric Subpixel Composite
    // ============================================================
    const compositeVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const compositeFsSrc = `
        precision mediump float;
        uniform sampler2D uColorTex;
        uniform sampler2D uEdgeTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        void main() {
            vec2 px = 1.0 / uResolution;
            vec4 edgeData = texture2D(uEdgeTex, vTexCoord);
            float edgeStrength = edgeData.b;

            // Not an edge — pass through unchanged
            if (edgeStrength < 0.08) {
                gl_FragColor = texture2D(uColorTex, vTexCoord);
                return;
            }

            // Recover gradient direction from storage
            float dX = edgeData.r * 2.0 - 1.0;
            float dY = edgeData.g * 2.0 - 1.0;

            // Normalize to get perpendicular edge direction
            float len = sqrt(dX * dX + dY * dY) + 0.0001;
            vec2 gradDir = vec2(dX, dY) / len;

            // Sample the two colours on either side of the edge
            vec4 colorA = texture2D(uColorTex, vTexCoord + gradDir * px);
            vec4 colorB = texture2D(uColorTex, vTexCoord - gradDir * px);
            vec4 colorC = texture2D(uColorTex, vTexCoord);

            // Calculate sub-pixel coverage using the gradient angle
            // This is the micro-triangle fill — what fraction of this pixel
            // belongs to each side of the edge
            float angle = atan(abs(dY), abs(dX));
            float coverage = angle / 1.5708; // normalize to 0..1 (pi/2)

            // Geometric composite — blend the two sides at the coverage ratio
            // This is sharper than FXAA because we use the actual edge angle
            // rather than a luma gradient blur
            vec4 edgeComposite = mix(colorA, colorB, coverage);

            // Scale blend by edge strength — weak edges get less correction
            float blendFactor = clamp(edgeStrength * 3.0, 0.0, 0.8);
            gl_FragColor = mix(colorC, edgeComposite, blendFactor);
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
            console.error('[SpectralAA] Shader compile error:', gl.getShaderInfoLog(s));
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
            console.error('[SpectralAA] Program link error:', gl.getProgramInfoLog(p));
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
    // Exposes spectralEdgeTex to window.SE for resizeAAfbos() to reference.
    // ============================================================
    function init() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;

        _spectralProgram1 = _buildProgram(edgeVsSrc, edgeFsSrc);
        _spectralProgram2 = _buildProgram(compositeVsSrc, compositeFsSrc);

        if (!_spectralProgram1 || !_spectralProgram2) {
            console.error('[SpectralAA] One or more programs failed to build');
            return;
        }

        _spectralEdgeTex = _createColorTexture(canvas.width, canvas.height);
        _spectralEdgeFbo = _createFboForTexture(_spectralEdgeTex, canvas.width, canvas.height);

        // Expose texture to window.SE so resizeAAfbos() can resize it
        window.SE.spectralEdgeTex = _spectralEdgeTex;

        // console.log('[SpectralAA] Initialized', canvas.width, canvas.height);
    }

    // ============================================================
    // PUBLIC — apply
    // 2-pass Spectral AA V1 resolve. Reads scene from window.SE.fxaaColorTex,
    // writes final result to the default framebuffer.
    // Called from SpectralEngine.js renderFrame() when aaMode === 5.
    // ============================================================
    function apply() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;
        const sceneTex = window.SE.fxaaColorTex;

        if (!_spectralProgram1 || !sceneTex) return;

        const w = canvas.width;
        const h = canvas.height;

        // ── Pass 1 — Edge detection + angle into _spectralEdgeFbo ────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _spectralEdgeFbo);
        gl.viewport(0, 0, w, h);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.disable(gl.DEPTH_TEST);
        gl.useProgram(_spectralProgram1);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_spectralProgram1, 'uColorTex'), 0);
        gl.uniform2f(gl.getUniformLocation(_spectralProgram1, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralProgram1);

        // ── Pass 2 — Geometric composite to screen ────────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, w, h);
        gl.useProgram(_spectralProgram2);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_spectralProgram2, 'uColorTex'), 0);
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _spectralEdgeTex);
        gl.uniform1i(gl.getUniformLocation(_spectralProgram2, 'uEdgeTex'), 1);
        gl.uniform2f(gl.getUniformLocation(_spectralProgram2, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralProgram2);

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

        if (_spectralProgram1) { gl.deleteProgram(_spectralProgram1); _spectralProgram1 = null; }
        if (_spectralProgram2) { gl.deleteProgram(_spectralProgram2); _spectralProgram2 = null; }
        if (_spectralEdgeTex) { gl.deleteTexture(_spectralEdgeTex); _spectralEdgeTex = null; }
        if (_spectralEdgeFbo) { gl.deleteFramebuffer(_spectralEdgeFbo); _spectralEdgeFbo = null; }

        window.SE.spectralEdgeTex = null;

        // console.log('[SpectralAA] Reset');
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