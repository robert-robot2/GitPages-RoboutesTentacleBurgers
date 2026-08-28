// ============================================================
// SpectralTextSystem.js
// SDF Text renderer — SpectralX WebGL2 Engine
// Extracted from SpectralEngine.js — Phase 7 migration
// Reads shared GL context via window.SE
// ============================================================

window.SpectralTextRenderSystem = (function () {

    // ============================================================
    // STATE
    // ============================================================
    let _textProgram = null;
    let _textLocs = null;

    // ============================================================
    // SHADERS
    // ============================================================
    const textVsSrc = `#version 300 es
in vec3 aPosition;
in vec2 aTexCoord;
uniform mat4 uMVP;
out vec2 vTexCoord;
void main() {
    vTexCoord = aTexCoord;
    gl_Position = uMVP * vec4(aPosition, 1.0);
}`;

    const textFsSrc = `#version 300 es
precision mediump float;
in vec2 vTexCoord;
uniform sampler2D uAtlas;
uniform vec4 uColor;
uniform vec4 uOutlineColor;
uniform float uOutlineWidth;
uniform float uSoftness;
uniform float uGlowRadius;
uniform float uGlowStrength;
out vec4 fragColor;

float median(float r, float g, float b) {
    return max(min(r,g), min(max(r,g),b));
}

void main() {
    vec3 s = texture(uAtlas, vTexCoord).rgb;
    float d = median(s.r, s.g, s.b);
    float w = fwidth(d) * 0.5;
    float alpha = smoothstep(0.5 - w - uSoftness, 0.5 + w + uSoftness, d);
    if (alpha < 0.001) discard;

    float glowAlpha = smoothstep(0.5 - uGlowRadius, 0.5 + uGlowRadius * 0.5, d);
    vec3 glowColor = uColor.rgb * 2.0;
    float glow = (1.0 - alpha) * glowAlpha * uGlowStrength;

    if (uOutlineWidth > 0.0) {
        float outlineA = smoothstep(
            0.5 - uOutlineWidth - w,
            0.5 - uOutlineWidth + w, d);
        vec4 outline = vec4(uOutlineColor.rgb, uOutlineColor.a * outlineA);
        fragColor = mix(outline, vec4(uColor.rgb, uColor.a * alpha), alpha);
    } else {
        fragColor = vec4(uColor.rgb, uColor.a * alpha);
    }

    fragColor.rgb = mix(fragColor.rgb, glowColor, glow);
    fragColor.a = max(fragColor.a, glow);
}`;

    // ============================================================
    // INIT
    // ============================================================
    function init() {
        const gl = SE.gl;
        _textProgram = SE.buildProgram(textVsSrc, textFsSrc);
        _textLocs = {
            mvp: gl.getUniformLocation(_textProgram, 'uMVP'),
            atlas: gl.getUniformLocation(_textProgram, 'uAtlas'),
            color: gl.getUniformLocation(_textProgram, 'uColor'),
            outlineColor: gl.getUniformLocation(_textProgram, 'uOutlineColor'),
            outlineWidth: gl.getUniformLocation(_textProgram, 'uOutlineWidth'),
            softness: gl.getUniformLocation(_textProgram, 'uSoftness'),
            glowRadius: gl.getUniformLocation(_textProgram, 'uGlowRadius'),
            glowStrength: gl.getUniformLocation(_textProgram, 'uGlowStrength'),
            pos: gl.getAttribLocation(_textProgram, 'aPosition'),
            uv: gl.getAttribLocation(_textProgram, 'aTexCoord'),
        };
        console.log('[SpectralTextRenderSystem] Initialized');
    }

    // ============================================================
    // RENDER
    // ============================================================
    function render(frame, meshBuffers) {
        if (!frame.textMeshes || !frame.textMeshes.length) return;
        if (!_textProgram) return;

        const gl = SE.gl;

        gl.disable(gl.DEPTH_TEST);
        gl.depthMask(false);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        gl.disable(gl.CULL_FACE);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.useProgram(_textProgram);

        for (const tm of frame.textMeshes) {
            if (!window.SpectralTextSystem.isAtlasLoaded(tm.fontKey)) {
                window.SpectralTextSystem.loadAtlas(tm.fontKey, tm.jsonUrl, tm.texUrl);
                continue;
            }

            const existingBuf = meshBuffers[tm.meshId];
            const needsBuild = tm.needsRebuild || !existingBuf || existingBuf.vertCount === 0;
            if (needsBuild) {
                window.SpectralTextSystem.buildTextGeometry(
                    tm.meshId, tm.text, tm.fontKey,
                    tm.fontSize, tm.letterSpacing, tm.align);
            }

            const buf = meshBuffers[tm.meshId];
            if (!buf || buf.vertCount === 0) continue;

            const atlasTex = window.SpectralTextSystem.getAtlasTexture(tm.fontKey);
            if (!atlasTex) continue;

            gl.uniform4f(_textLocs.outlineColor,
                tm.outlineR, tm.outlineG, tm.outlineB, tm.outlineA);
            gl.uniform1f(_textLocs.outlineWidth, tm.outlineWidth);
            gl.uniform1f(_textLocs.softness, 0.05);
            gl.uniform1f(_textLocs.glowRadius, tm.glowRadius ?? 0.25);
            gl.uniform1f(_textLocs.glowStrength, tm.glowStrength ?? 0.8);

            gl.activeTexture(gl.TEXTURE0);
            gl.bindTexture(gl.TEXTURE_2D, atlasTex);
            gl.uniform1i(_textLocs.atlas, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
            gl.enableVertexAttribArray(_textLocs.pos);
            gl.vertexAttribPointer(_textLocs.pos, 3, gl.FLOAT, false, 0, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
            gl.enableVertexAttribArray(_textLocs.uv);
            gl.vertexAttribPointer(_textLocs.uv, 2, gl.FLOAT, false, 0, 0);

            // Layer 1 — Shadow blur passes
            if ((tm.shadowBlur ?? 0) > 0.01) {
                const shadowPasses = [
                    { spread: tm.shadowBlur * 0.5, alpha: 0.6 },
                    { spread: tm.shadowBlur * 1.0, alpha: 0.4 },
                    { spread: tm.shadowBlur * 1.5, alpha: 0.2 },
                    { spread: tm.shadowBlur * 2.0, alpha: 0.1 },
                ];
                const shadowDirs = [
                    [1, 0], [-1, 0], [0, 1], [0, -1],
                    [1, 1], [-1, 1], [1, -1], [-1, -1],
                ];
                for (const pass of shadowPasses) {
                    for (const [dx, dy] of shadowDirs) {
                        const m = new Float32Array(tm.mvp);
                        m[12] += dx * pass.spread;
                        m[13] += dy * pass.spread;
                        gl.uniformMatrix4fv(_textLocs.mvp, false, m);
                        gl.uniform4f(_textLocs.color,
                            tm.shadowR ?? 0,
                            tm.shadowG ?? 0,
                            tm.shadowB ?? 0,
                            (tm.shadowA ?? 0) * pass.alpha);
                        gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
                    }
                }
            }

            // Layer 2 — Mirror glow passes
            if ((tm.glowStrength ?? 0.8) > 0.01 && (tm.glowRadius ?? 0.25) > 0.01) {
                const spread = (tm.glowRadius ?? 0.25) * 0.5;
                const glowOffsets = [
                    [spread, 0], [-spread, 0],
                    [0, spread], [0, -spread],
                    [spread, spread], [-spread, spread],
                    [spread, -spread], [-spread, -spread],
                ];
                for (const [ox, oy] of glowOffsets) {
                    const m = new Float32Array(tm.mvp);
                    m[12] += ox;
                    m[13] += oy;
                    gl.uniformMatrix4fv(_textLocs.mvp, false, m);
                    gl.uniform4f(_textLocs.color,
                        tm.glowR ?? tm.r,
                        tm.glowG ?? tm.g,
                        tm.glowB ?? tm.b,
                        (tm.glowA ?? 1.0) * (tm.glowStrength ?? 0.8) * 0.4);
                    gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
                }
            }

            // Main draw on top
            gl.uniformMatrix4fv(_textLocs.mvp, false, tm.mvp);
            gl.uniform4f(_textLocs.color, tm.r, tm.g, tm.b, tm.a);
            gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
        }

        gl.enable(gl.DEPTH_TEST);
        gl.depthMask(true);
        gl.enable(gl.CULL_FACE);
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return { init, render };

})();