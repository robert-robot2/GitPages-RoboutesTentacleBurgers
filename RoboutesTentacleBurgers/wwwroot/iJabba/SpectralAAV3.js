// ============================================================
// SpectralAAV3.js
// Spectral AA V3 — Binary Edge Classification + Line Reconstruction
//                  + Triangle Coverage Fill
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
//   - Populating window.SE.spectralV3EdgeTex and window.SE.spectralV3LineTex after init()
//   - Updating them in window.SE after resizeAAfbos() rebuilds them
//
// Load order: SpectralAAV3.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralAAV3 = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================
    let _spectralV3Program1 = null; // Pass 1 — Binary edge classification
    let _spectralV3Program2 = null; // Pass 2 — Line reconstruction
    let _spectralV3Program3 = null; // Pass 3 — Triangle coverage fill
    let _spectralV3EdgeFbo = null;
    let _spectralV3EdgeTex = null;
    let _spectralV3LineFbo = null;
    let _spectralV3LineTex = null;

    // ============================================================
    // SHADERS — Pass 1: Binary Edge Classification
    // ============================================================
    const classifyVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const classifyFsSrc = `
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

            // Sample all 8 neighbours
            float R  = luma(texture2D(uColorTex, vTexCoord + vec2( px.x,  0.0)).rgb);
            float L  = luma(texture2D(uColorTex, vTexCoord + vec2(-px.x,  0.0)).rgb);
            float U  = luma(texture2D(uColorTex, vTexCoord + vec2( 0.0,   px.y)).rgb);
            float D  = luma(texture2D(uColorTex, vTexCoord + vec2( 0.0,  -px.y)).rgb);
            float UR = luma(texture2D(uColorTex, vTexCoord + vec2( px.x,  px.y)).rgb);
            float UL = luma(texture2D(uColorTex, vTexCoord + vec2(-px.x,  px.y)).rgb);
            float DR = luma(texture2D(uColorTex, vTexCoord + vec2( px.x, -px.y)).rgb);
            float DL = luma(texture2D(uColorTex, vTexCoord + vec2(-px.x, -px.y)).rgb);

            // Hard threshold — binary inside/outside classification
            // Higher threshold than V1/V2 to exclude shadow gradients
            float threshold = 0.15;

            float maxDelta = max(
                max(abs(C-R), abs(C-L)),
                max(abs(C-U), abs(C-D))
            );

            // Not an edge pixel at all — discard early
            if (maxDelta < threshold) {
                gl_FragColor = vec4(0.0);
                return;
            }

            // Shadow gradient gate — shadows have gradual falloff
            // True geometry edges have at least one very sharp neighbour transition
            float minDelta = min(
                min(abs(C-R), abs(C-L)),
                min(abs(C-U), abs(C-D))
            );

            // If min and max delta are close together its a gradient not an edge
            if (minDelta > threshold * 0.4) {
                gl_FragColor = vec4(0.0);
                return;
            }

            // Encode neighbour binary pattern into RG channels
            // R = horizontal/vertical pattern
            // G = diagonal pattern
            // B = edge strength
            // A = inside/outside flag
            float inside = step(C - threshold * 0.5, 0.0);

            float hv = (step(threshold, abs(C-R)) +
                       step(threshold, abs(C-L)) +
                       step(threshold, abs(C-U)) +
                       step(threshold, abs(C-D))) / 4.0;

            float diag = (step(threshold, abs(C-UR)) +
                         step(threshold, abs(C-UL)) +
                         step(threshold, abs(C-DR)) +
                         step(threshold, abs(C-DL))) / 4.0;

            gl_FragColor = vec4(hv, diag, maxDelta, inside);
        }
    `;

    // ============================================================
    // SHADERS — Pass 2: Line Reconstruction from Binary Pattern
    // ============================================================
    const lineVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const lineFsSrc = `
        precision mediump float;
        uniform sampler2D uClassifyTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        void main() {
            vec2 px = 1.0 / uResolution;
            vec4 data = texture2D(uClassifyTex, vTexCoord);

            // Not classified as edge — pass through
            if (data.b < 0.15) {
                gl_FragColor = vec4(0.0);
                return;
            }

            float hv   = data.r; // how many orthogonal neighbours differ
            float diag = data.g; // how many diagonal neighbours differ

            // Sample neighbours to get gradient direction
            float R = texture2D(uClassifyTex, vTexCoord + vec2( px.x,  0.0)).b;
            float L = texture2D(uClassifyTex, vTexCoord + vec2(-px.x,  0.0)).b;
            float U = texture2D(uClassifyTex, vTexCoord + vec2( 0.0,   px.y)).b;
            float D = texture2D(uClassifyTex, vTexCoord + vec2( 0.0,  -px.y)).b;

            // Line direction vector from local edge topology
            float dX = R - L;
            float dY = U - D;
            float len = sqrt(dX*dX + dY*dY) + 0.0001;

            // Normalize
            vec2 lineDir = vec2(dX, dY) / len;

            // Coverage — how far across this pixel the line sits
            // Derived from the ratio of orthogonal vs diagonal edge hits
            float coverage = clamp(hv / (hv + diag + 0.0001), 0.1, 0.9);

            // Encode: line direction in RG, coverage in B, valid in A
            gl_FragColor = vec4(lineDir * 0.5 + 0.5, coverage, 1.0);
        }
    `;

    // ============================================================
    // SHADERS — Pass 3: Triangle Coverage Fill
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
        uniform sampler2D uLineTex;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        void main() {
            vec2 px = 1.0 / uResolution;
            vec4 lineData = texture2D(uLineTex, vTexCoord);

            // No line data — pass through completely unchanged
            if (lineData.a < 0.5) {
                gl_FragColor = texture2D(uColorTex, vTexCoord);
                return;
            }

            // Recover line direction
            vec2 lineDir = lineData.rg * 2.0 - 1.0;
            float coverage = lineData.b;

            // The perpendicular to the line direction gives us
            // which side to sample for fill color
            vec2 perpDir = vec2(-lineDir.y, lineDir.x);

            // Sample the two colors on either side of the true geometric line
            vec4 colorInside  = texture2D(uColorTex, vTexCoord + perpDir * px);
            vec4 colorOutside = texture2D(uColorTex, vTexCoord - perpDir * px);
            vec4 colorCenter  = texture2D(uColorTex, vTexCoord);

            // True triangle coverage fill
            // coverage = fraction of pixel area on the inside of the line
            // coverage 0.5 = line passes through center = equal triangles
            // coverage 0.1 = line near edge = small triangle on inside
            // coverage 0.9 = line near other edge = large triangle fill
            vec4 triangleFill = mix(colorOutside, colorInside, coverage);

            // Hard gate — only apply where we have strong confident edge data
            float edgeConfidence = clamp(lineData.b * 2.0, 0.0, 1.0);

            gl_FragColor = mix(colorCenter, triangleFill, edgeConfidence * 0.85);
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
            console.error('[SpectralAAV3] Shader compile error:', gl.getShaderInfoLog(s));
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
            console.error('[SpectralAAV3] Program link error:', gl.getProgramInfoLog(p));
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
    // Exposes spectralV3EdgeTex and spectralV3LineTex to window.SE
    // for resizeAAfbos() to reference.
    // ============================================================
    function init() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;

        _spectralV3Program1 = _buildProgram(classifyVsSrc, classifyFsSrc);
        _spectralV3Program2 = _buildProgram(lineVsSrc, lineFsSrc);
        _spectralV3Program3 = _buildProgram(fillVsSrc, fillFsSrc);

        if (!_spectralV3Program1 || !_spectralV3Program2 || !_spectralV3Program3) {
            console.error('[SpectralAAV3] One or more programs failed to build');
            return;
        }

        _spectralV3EdgeTex = _createColorTexture(canvas.width, canvas.height);
        _spectralV3EdgeFbo = _createFboForTexture(_spectralV3EdgeTex, canvas.width, canvas.height);

        _spectralV3LineTex = _createColorTexture(canvas.width, canvas.height);
        _spectralV3LineFbo = _createFboForTexture(_spectralV3LineTex, canvas.width, canvas.height);

        // Expose textures to window.SE so resizeAAfbos() can resize them
        window.SE.spectralV3EdgeTex = _spectralV3EdgeTex;
        window.SE.spectralV3LineTex = _spectralV3LineTex;

        // console.log('[SpectralAAV3] Initialized', canvas.width, canvas.height);
    }

    // ============================================================
    // PUBLIC — apply
    // 3-pass Spectral AA V3 resolve. Reads scene from window.SE.fxaaColorTex,
    // writes final result to the default framebuffer.
    // Called from SpectralEngine.js renderFrame() when aaMode === 7.
    // ============================================================
    function apply() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;
        const sceneTex = window.SE.fxaaColorTex;

        if (!_spectralV3Program1 || !sceneTex) return;

        const w = canvas.width;
        const h = canvas.height;

        // ── Pass 1 — Binary edge classification into _spectralV3EdgeFbo ──
        gl.bindFramebuffer(gl.FRAMEBUFFER, _spectralV3EdgeFbo);
        gl.viewport(0, 0, w, h);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.disable(gl.DEPTH_TEST);
        gl.useProgram(_spectralV3Program1);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV3Program1, 'uColorTex'), 0);
        gl.uniform2f(gl.getUniformLocation(_spectralV3Program1, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralV3Program1);

        // ── Pass 2 — Line reconstruction into _spectralV3LineFbo ─────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _spectralV3LineFbo);
        gl.clearColor(0, 0, 0, 0);
        gl.clear(gl.COLOR_BUFFER_BIT);
        gl.useProgram(_spectralV3Program2);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, _spectralV3EdgeTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV3Program2, 'uClassifyTex'), 0);
        gl.uniform2f(gl.getUniformLocation(_spectralV3Program2, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralV3Program2);

        // ── Pass 3 — Triangle coverage fill to screen ─────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, w, h);
        gl.useProgram(_spectralV3Program3);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, sceneTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV3Program3, 'uColorTex'), 0);
        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, _spectralV3LineTex);
        gl.uniform1i(gl.getUniformLocation(_spectralV3Program3, 'uLineTex'), 1);
        gl.uniform2f(gl.getUniformLocation(_spectralV3Program3, 'uResolution'), w, h);
        window.SE.drawQuad(_spectralV3Program3);

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

        if (_spectralV3Program1) { gl.deleteProgram(_spectralV3Program1); _spectralV3Program1 = null; }
        if (_spectralV3Program2) { gl.deleteProgram(_spectralV3Program2); _spectralV3Program2 = null; }
        if (_spectralV3Program3) { gl.deleteProgram(_spectralV3Program3); _spectralV3Program3 = null; }
        if (_spectralV3EdgeTex) { gl.deleteTexture(_spectralV3EdgeTex); _spectralV3EdgeTex = null; }
        if (_spectralV3LineTex) { gl.deleteTexture(_spectralV3LineTex); _spectralV3LineTex = null; }
        if (_spectralV3EdgeFbo) { gl.deleteFramebuffer(_spectralV3EdgeFbo); _spectralV3EdgeFbo = null; }
        if (_spectralV3LineFbo) { gl.deleteFramebuffer(_spectralV3LineFbo); _spectralV3LineFbo = null; }

        window.SE.spectralV3EdgeTex = null;
        window.SE.spectralV3LineTex = null;

        // console.log('[SpectralAAV3] Reset');
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