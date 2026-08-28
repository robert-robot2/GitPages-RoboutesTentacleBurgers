// ============================================================
// SpectralFXAA.js
// FXAA Anti-Aliasing System
// Extracted from SpectralEngine.js — SpectralGLInterop
//
// All shared state is read from window.SE:
//   window.SE.gl              — WebGL2 context
//   window.SE.canvas          — HTMLCanvasElement
//   window.SE.fxaaColorTex    — shared offscreen color texture (owned by engine)
//   window.SE.fxaaFbo         — shared offscreen framebuffer (owned by engine)
//   window.SE.fullscreenQuadVbo — shared fullscreen quad VBO
//
// SpectralEngine.js is responsible for:
//   - Populating window.SE.fxaaFbo and window.SE.fxaaColorTex after initSharedFbo()
//   - Updating them in window.SE after resizeAAfbos() rebuilds them
//
// Load order: SpectralFXAA.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralFXAA = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================
    let _fxaaProgram = null;
    let _fxaaPosLoc = null;
    let _fxaaTexLoc = null;
    let _fxaaResLoc = null;

    // ============================================================
    // SHADERS
    // ============================================================
    const fxaaVsSrc = `
        attribute vec2 aPosition;
        varying vec2 vTexCoord;
        void main() {
            vTexCoord = aPosition * 0.5 + 0.5;
            gl_Position = vec4(aPosition, 0.0, 1.0);
        }
    `;

    const fxaaFsSrc = `
        precision mediump float;
        uniform sampler2D uTexture;
        uniform vec2 uResolution;
        varying vec2 vTexCoord;

        void main() {
            vec2 texel = 1.0 / uResolution;

            vec3 rgbNW = texture2D(uTexture, vTexCoord + vec2(-1.0, -1.0) * texel).rgb;
            vec3 rgbNE = texture2D(uTexture, vTexCoord + vec2( 1.0, -1.0) * texel).rgb;
            vec3 rgbSW = texture2D(uTexture, vTexCoord + vec2(-1.0,  1.0) * texel).rgb;
            vec3 rgbSE = texture2D(uTexture, vTexCoord + vec2( 1.0,  1.0) * texel).rgb;
            vec3 rgbM  = texture2D(uTexture, vTexCoord).rgb;

            vec3 luma = vec3(0.299, 0.587, 0.114);
            float lumaNW = dot(rgbNW, luma);
            float lumaNE = dot(rgbNE, luma);
            float lumaSW = dot(rgbSW, luma);
            float lumaSE = dot(rgbSE, luma);
            float lumaM  = dot(rgbM,  luma);

            float lumaMin = min(lumaM, min(min(lumaNW, lumaNE), min(lumaSW, lumaSE)));
            float lumaMax = max(lumaM, max(max(lumaNW, lumaNE), max(lumaSW, lumaSE)));
            float lumaRange = lumaMax - lumaMin;

            // Skip non-edges
            if (lumaRange < max(0.0312, lumaMax * 0.125)) {
                gl_FragColor = vec4(rgbM, 1.0);
                return;
            }

            vec2 dir = vec2(
                -((lumaNW + lumaNE) - (lumaSW + lumaSE)),
                 ((lumaNW + lumaSW) - (lumaNE + lumaSE))
            );

            float dirReduce = max((lumaNW + lumaNE + lumaSW + lumaSE) * 0.03125, 0.0078125);
            float rcpDirMin = 1.0 / (min(abs(dir.x), abs(dir.y)) + dirReduce);
            dir = clamp(dir * rcpDirMin, vec2(-8.0), vec2(8.0)) * texel;

            vec3 rgbA = 0.5 * (
                texture2D(uTexture, vTexCoord + dir * (1.0/3.0 - 0.5)).rgb +
                texture2D(uTexture, vTexCoord + dir * (2.0/3.0 - 0.5)).rgb
            );
            vec3 rgbB = rgbA * 0.5 + 0.25 * (
                texture2D(uTexture, vTexCoord + dir * -0.5).rgb +
                texture2D(uTexture, vTexCoord + dir *  0.5).rgb
            );

            float lumaB = dot(rgbB, luma);
            if (lumaB < lumaMin || lumaB > lumaMax)
                gl_FragColor = vec4(rgbA, 1.0);
            else
                gl_FragColor = vec4(rgbB, 1.0);
        }
    `;

    // ============================================================
    // PUBLIC — init
    // Called from SpectralEngine.js init() after GL context is ready,
    // initSharedFbo() has run, and window.SE has been fully populated.
    // No arguments — reads everything from window.SE.
    // ============================================================
    function init() {
        const gl = window.SE.gl;

        const vs = _compile(gl.VERTEX_SHADER, fxaaVsSrc);
        const fs = _compile(gl.FRAGMENT_SHADER, fxaaFsSrc);
        if (!vs || !fs) return;

        _fxaaProgram = gl.createProgram();
        gl.attachShader(_fxaaProgram, vs);
        gl.attachShader(_fxaaProgram, fs);
        gl.linkProgram(_fxaaProgram);

        if (!gl.getProgramParameter(_fxaaProgram, gl.LINK_STATUS)) {
            console.error('[SpectralFXAA] Program link error:', gl.getProgramInfoLog(_fxaaProgram));
            _fxaaProgram = null;
            return;
        }

        _fxaaPosLoc = gl.getAttribLocation(_fxaaProgram, 'aPosition');
        _fxaaTexLoc = gl.getUniformLocation(_fxaaProgram, 'uTexture');
        _fxaaResLoc = gl.getUniformLocation(_fxaaProgram, 'uResolution');

        // console.log('[SpectralFXAA] Initialized', window.SE.canvas.width, window.SE.canvas.height);
    }

    // ============================================================
    // PUBLIC — apply
    // Reads _fxaaColorTex from window.SE (scene rendered into shared FBO),
    // writes FXAA result to the default framebuffer.
    // Called from SpectralEngine.js renderFrame() when aaMode === 2.
    // ============================================================
    function apply() {
        const gl = window.SE.gl;
        const canvas = window.SE.canvas;
        const fxaaColorTex = window.SE.fxaaColorTex;

        if (!_fxaaProgram || !fxaaColorTex) return;

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, canvas.width, canvas.height);
        gl.disable(gl.DEPTH_TEST);
        gl.useProgram(_fxaaProgram);

        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, fxaaColorTex);
        gl.uniform1i(_fxaaTexLoc, 0);
        gl.uniform2f(_fxaaResLoc, canvas.width, canvas.height);

        gl.bindBuffer(gl.ARRAY_BUFFER, window.SE.fullscreenQuadVbo);
        gl.enableVertexAttribArray(_fxaaPosLoc);
        gl.vertexAttribPointer(_fxaaPosLoc, 2, gl.FLOAT, false, 0, 0);
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_2D, null);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, null);

        gl.enable(gl.DEPTH_TEST);
    }

    // ============================================================
    // PUBLIC — reset
    // Clears GPU program. window.SE texture/FBO references are owned
    // by SpectralEngine.js and cleaned up there.
    // Called from SpectralEngine.js flush().
    // ============================================================
    function reset() {
        const gl = window.SE.gl;
        if (gl && _fxaaProgram) {
            gl.deleteProgram(_fxaaProgram);
        }
        _fxaaProgram = null;
        _fxaaPosLoc = null;
        _fxaaTexLoc = null;
        _fxaaResLoc = null;
        // console.log('[SpectralFXAA] Reset');
    }

    // ============================================================
    // PRIVATE — shader compile helper
    // ============================================================
    function _compile(type, src) {
        const gl = window.SE.gl;
        const s = gl.createShader(type);
        gl.shaderSource(s, src);
        gl.compileShader(s);
        if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) {
            console.error('[SpectralFXAA] Shader compile error:', gl.getShaderInfoLog(s));
            gl.deleteShader(s);
            return null;
        }
        return s;
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