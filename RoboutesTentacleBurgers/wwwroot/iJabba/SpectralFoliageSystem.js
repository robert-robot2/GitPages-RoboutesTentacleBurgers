// ============================================================
// SpectralFoliageSystem.js
// Foliage instanced renderer — SpectralX WebGL2 Engine
// Reads shared GL context via window.SE
// ============================================================

/*

window.SpectralFoliageSystem = (function () {

    // ============================================================
    // STATE
    // ============================================================
    let _foliageProgram = null;
    let _foliageBuffers = {};
    let _fLocs = null;
    let _foliageDirty = false;

    // ============================================================
    // SHADERS
    // ============================================================
    const foliageVsSrc = `#version 300 es
        in vec3 aPosition;
        in vec3 aNormal;
        in vec2 aTexCoord;
        in vec3 iInstancePos;
        in float iInstanceScale;
        in float iInstanceRot;
        uniform mat4 uVP;
        uniform mat4 uLightVP;
        out vec3 vNormal;
        out vec3 vWorldPos;
        out vec2 vTexCoord;
        out vec4 vShadowCoord;
        void main() {
            float c = cos(iInstanceRot);
            float s = sin(iInstanceRot);
            vec3 rotated = vec3(
                aPosition.x * c - aPosition.y * s,
                aPosition.x * s + aPosition.y * c,
                aPosition.z
            );
            vec3 worldPos = rotated * iInstanceScale + iInstancePos;
            vWorldPos   = worldPos;
            vNormal     = vec3(-s * aNormal.x + c * aNormal.y,
                                c * aNormal.x + s * aNormal.y,
                                aNormal.z);
            vTexCoord   = aTexCoord;
            vShadowCoord = uLightVP * vec4(worldPos, 1.0);
            gl_Position = uVP * vec4(worldPos, 1.0);
        }`;

    const foliageFsSrc = `#version 300 es
        precision mediump float;
        in vec3 vNormal;
        in vec3 vWorldPos;
        in vec2 vTexCoord;
        in vec4 vShadowCoord;
        uniform sampler2D uTexture;
        uniform bool uHasTexture;
        uniform vec3 uCamPos;
        uniform int uLightCount;
        uniform vec3 uLightPos[32];
        uniform vec3 uLightColor[32];
        uniform float uLightIntensity[32];
        uniform float uLightRange[32];
        uniform int uLightType[32];
        uniform sampler2D uShadowMap;
        uniform mat4 uLightVP;
        out vec4 fragColor;
        uniform sampler2D uShadowMap0;
uniform sampler2D uShadowMap1;
uniform sampler2D uShadowMap2;
uniform sampler2D uShadowMap3;
uniform sampler2D uShadowMap4;
uniform sampler2D uShadowMap5;
uniform sampler2D uShadowMap6;
uniform sampler2D uShadowMap7;
uniform mat4 uLightVP0;
uniform mat4 uLightVP1;
uniform mat4 uLightVP2;
uniform mat4 uLightVP3;
uniform mat4 uLightVP4;
uniform mat4 uLightVP5;
uniform mat4 uLightVP6;
uniform mat4 uLightVP7;
uniform int uSunShadowSlot;
    float sampleShadowMap(int index, vec2 uv) {
    if (index == 0) return texture(uShadowMap0, uv).r;
    if (index == 1) return texture(uShadowMap1, uv).r;
    if (index == 2) return texture(uShadowMap2, uv).r;
    if (index == 3) return texture(uShadowMap3, uv).r;
    if (index == 4) return texture(uShadowMap4, uv).r;
    if (index == 5) return texture(uShadowMap5, uv).r;
    if (index == 6) return texture(uShadowMap6, uv).r;
    if (index == 7) return texture(uShadowMap7, uv).r;
    return 1.0;
}

vec4 getShadowCoord(int index, vec4 worldPos) {
    if (index == 0) return uLightVP0 * worldPos;
    if (index == 1) return uLightVP1 * worldPos;
    if (index == 2) return uLightVP2 * worldPos;
    if (index == 3) return uLightVP3 * worldPos;
    if (index == 4) return uLightVP4 * worldPos;
    if (index == 5) return uLightVP5 * worldPos;
    if (index == 6) return uLightVP6 * worldPos;
    if (index == 7) return uLightVP7 * worldPos;
    return vec4(0.0);
}

float shadowFactor() {
    vec4 shadowCoord = getShadowCoord(uSunShadowSlot, vec4(vWorldPos, 1.0));
    vec3 proj = shadowCoord.xyz / shadowCoord.w;
    proj = proj * 0.5 + 0.5;
    if (proj.x < 0.0 || proj.x > 1.0 ||
        proj.y < 0.0 || proj.y > 1.0 ||
        proj.z > 1.0) return 1.0;
    float bias = 0.005;
    float depth = sampleShadowMap(uSunShadowSlot, proj.xy);
    return proj.z - bias > depth ? 0.7 : 1.0;
}

        void main() {
            vec4 base = uHasTexture
                ? texture(uTexture, vTexCoord)
                : vec4(0.6, 0.8, 0.4, 1.0);
            if (base.a < 0.1) discard;

            vec3 norm    = normalize(vNormal);
            vec3 ambient = vec3(0.3);
            vec3 light   = ambient;

            for (int i = 0; i < 32; i++) {
                if (i >= uLightCount) break;
                if (uLightType[i] == 1) {
                    vec3 dir  = normalize(-uLightPos[i]);
                    float diff = max(dot(norm, dir), 0.0);
                    light += uLightColor[i] * uLightIntensity[i]
                             * diff * shadowFactor();
                } else {
                    vec3 toLight = uLightPos[i] - vWorldPos;
                    float dist   = length(toLight);
                    float att    = 1.0 / (1.0 + (dist * dist) /
                        (uLightRange[i] * uLightRange[i]));
                    att = att * att * att;
                    float diff   = max(dot(norm, normalize(toLight)), 0.0);
                    light += uLightColor[i] * uLightIntensity[i] * diff * att;
                }
            }

            light = clamp(light, 0.0, 2.0);
            fragColor = vec4(base.rgb * light, base.a);
        }`;

    // ============================================================
    // INIT
    // ============================================================
    function init() {
        const gl = SE.gl;
        const MAX_LIGHTS = 32;

        _foliageProgram = SE.buildProgram(foliageVsSrc, foliageFsSrc);

        _fLocs = {
            vp: gl.getUniformLocation(_foliageProgram, 'uVP'),
            lightVP: gl.getUniformLocation(_foliageProgram, 'uLightVP'),
            camPos: gl.getUniformLocation(_foliageProgram, 'uCamPos'),
            lightCount: gl.getUniformLocation(_foliageProgram, 'uLightCount'),
            tex: gl.getUniformLocation(_foliageProgram, 'uTexture'),
            hasTex: gl.getUniformLocation(_foliageProgram, 'uHasTexture'),
            shadowMap: gl.getUniformLocation(_foliageProgram, 'uShadowMap'),
            pos: gl.getAttribLocation(_foliageProgram, 'aPosition'),
            norm: gl.getAttribLocation(_foliageProgram, 'aNormal'),
            uv: gl.getAttribLocation(_foliageProgram, 'aTexCoord'),
            iPos: gl.getAttribLocation(_foliageProgram, 'iInstancePos'),
            iScale: gl.getAttribLocation(_foliageProgram, 'iInstanceScale'),
            iRot: gl.getAttribLocation(_foliageProgram, 'iInstanceRot'),
            lightPos: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_foliageProgram, `uLightPos[${i}]`)),
            lightColor: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_foliageProgram, `uLightColor[${i}]`)),
            lightIntensity: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_foliageProgram, `uLightIntensity[${i}]`)),
            lightRange: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_foliageProgram, `uLightRange[${i}]`)),
            lightType: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_foliageProgram, `uLightType[${i}]`)),
            sunShadowSlot: gl.getUniformLocation(_foliageProgram, 'uSunShadowSlot'),
            shadowMap0: gl.getUniformLocation(_foliageProgram, 'uShadowMap0'),
            shadowMap1: gl.getUniformLocation(_foliageProgram, 'uShadowMap1'),
            shadowMap2: gl.getUniformLocation(_foliageProgram, 'uShadowMap2'),
            shadowMap3: gl.getUniformLocation(_foliageProgram, 'uShadowMap3'),
            shadowMap4: gl.getUniformLocation(_foliageProgram, 'uShadowMap4'),
            shadowMap5: gl.getUniformLocation(_foliageProgram, 'uShadowMap5'),
            shadowMap6: gl.getUniformLocation(_foliageProgram, 'uShadowMap6'),
            shadowMap7: gl.getUniformLocation(_foliageProgram, 'uShadowMap7'),
            lightVP0: gl.getUniformLocation(_foliageProgram, 'uLightVP0'),
            lightVP1: gl.getUniformLocation(_foliageProgram, 'uLightVP1'),
            lightVP2: gl.getUniformLocation(_foliageProgram, 'uLightVP2'),
            lightVP3: gl.getUniformLocation(_foliageProgram, 'uLightVP3'),
            lightVP4: gl.getUniformLocation(_foliageProgram, 'uLightVP4'),
            lightVP5: gl.getUniformLocation(_foliageProgram, 'uLightVP5'),
            lightVP6: gl.getUniformLocation(_foliageProgram, 'uLightVP6'),
            lightVP7: gl.getUniformLocation(_foliageProgram, 'uLightVP7'),
        };

        console.log('[SpectralFoliageSystem] Initialized');
    }

    // ============================================================
    // RENDER
    // ============================================================
    function render(frame) {
        if (!frame.foliageInstances || !frame.foliageInstances.length) return;

        const gl = SE.gl;
        // ── DEBUG BLOCK — remove after fix confirmed ──────────────
        const fbo = gl.getParameter(gl.FRAMEBUFFER_BINDING);
        const depthTest = gl.getParameter(gl.DEPTH_TEST);
        const depthMask = gl.getParameter(gl.DEPTH_WRITEMASK);
        const activeTex = gl.getParameter(gl.ACTIVE_TEXTURE);
      
        if (!_foliageProgram) init();
        if (!_foliageProgram) return;

        gl.useProgram(_foliageProgram);
        // Reset texture units left dirty by AA passes
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, null);
        gl.enable(gl.DEPTH_TEST);
        gl.depthMask(true);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        gl.disable(gl.CULL_FACE); // billboards are double-sided

        gl.uniformMatrix4fv(_fLocs.vp, false, frame.vp);
        gl.uniform3f(_fLocs.camPos, frame.camX, frame.camY, frame.camZ);
        gl.uniform1i(_fLocs.lightCount, frame.lightCount);

        for (let li = 0; li < frame.lightCount; li++) {
            gl.uniform3f(_fLocs.lightPos[li],
                frame.lightPositions[li * 3],
                frame.lightPositions[li * 3 + 1],
                frame.lightPositions[li * 3 + 2]);
            gl.uniform3f(_fLocs.lightColor[li],
                frame.lightColors[li * 3],
                frame.lightColors[li * 3 + 1],
                frame.lightColors[li * 3 + 2]);
            gl.uniform1f(_fLocs.lightIntensity[li], frame.lightIntensities[li]);
            gl.uniform1f(_fLocs.lightRange[li], frame.lightRanges[li]);
            gl.uniform1i(_fLocs.lightType[li], frame.lightTypes[li]);
        }

    

        let sunIdx = -1;
        if (frame.lightCastsShadows) {
            for (let i = 0; i < Math.min(frame.lightCount, 16); i++) {
                if (frame.lightCastsShadows[i] && SE.shadowDepthTexs[i]) {
                    sunIdx = i;
                    break;
                }
            }
        }
    

        gl.uniform1i(_fLocs.sunShadowSlot, sunIdx);

        for (let i = 0; i < 8; i++) {
            if (SE.shadowDepthTexs[i]) {
                gl.activeTexture(gl.TEXTURE2 + i);
                gl.bindTexture(gl.TEXTURE_2D, SE.shadowDepthTexs[i]);
                gl.uniform1i(_fLocs['shadowMap' + i], 2 + i);
            }
            if (frame.lightVPs && frame.lightVPs[i]) {
                gl.uniformMatrix4fv(_fLocs['lightVP' + i], false,
                    new Float32Array(frame.lightVPs[i]));
            }
        }

        for (const group of frame.foliageInstances) {
            if (!group || group.count <= 0) continue;

            const buf = SE.meshBuffers[group.meshId];
            if (!buf || buf.vertCount < 3) {
                delete _foliageBuffers[group.meshId]; // force retry later
                continue;
            }

            const positions = group.positions instanceof Float32Array ? group.positions : new Float32Array(group.positions);
            const scales = group.scales instanceof Float32Array ? group.scales : new Float32Array(group.scales);
            const rotations = group.rotations instanceof Float32Array ? group.rotations : new Float32Array(group.rotations);

            // Upload instance buffers — re-upload if count changed or dirty flag set
            if (
                !_foliageBuffers[group.meshId] ||
                _foliageBuffers[group.meshId].maxCount !== group.count ||
                _foliageDirty
            ) {
                if (!_foliageBuffers[group.meshId]) {
                    _foliageBuffers[group.meshId] = {
                        posBuf: gl.createBuffer(),
                        scaleBuf: gl.createBuffer(),
                        rotBuf: gl.createBuffer(),
                        maxCount: 0,
                    };
                }
                const fb = _foliageBuffers[group.meshId];
                fb.maxCount = group.count;
                gl.bindBuffer(gl.ARRAY_BUFFER, fb.posBuf);
                gl.bufferData(gl.ARRAY_BUFFER, positions, gl.STATIC_DRAW);
                gl.bindBuffer(gl.ARRAY_BUFFER, fb.scaleBuf);
                gl.bufferData(gl.ARRAY_BUFFER, scales, gl.STATIC_DRAW);
                gl.bindBuffer(gl.ARRAY_BUFFER, fb.rotBuf);
                gl.bufferData(gl.ARRAY_BUFFER, rotations, gl.STATIC_DRAW);
            }

            const fb = _foliageBuffers[group.meshId];
            if (!fb) continue;

            // Geometry buffers
            gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
            gl.enableVertexAttribArray(_fLocs.pos);
            gl.vertexAttribPointer(_fLocs.pos, 3, gl.FLOAT, false, 0, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.nbo);
            gl.enableVertexAttribArray(_fLocs.norm);
            gl.vertexAttribPointer(_fLocs.norm, 3, gl.FLOAT, false, 0, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
            gl.enableVertexAttribArray(_fLocs.uv);
            gl.vertexAttribPointer(_fLocs.uv, 2, gl.FLOAT, false, 0, 0);

            // Instance attributes
            gl.bindBuffer(gl.ARRAY_BUFFER, fb.posBuf);
            gl.enableVertexAttribArray(_fLocs.iPos);
            gl.vertexAttribPointer(_fLocs.iPos, 3, gl.FLOAT, false, 12, 0);
            gl.vertexAttribDivisor(_fLocs.iPos, 1);

            gl.bindBuffer(gl.ARRAY_BUFFER, fb.scaleBuf);
            gl.enableVertexAttribArray(_fLocs.iScale);
            gl.vertexAttribPointer(_fLocs.iScale, 1, gl.FLOAT, false, 4, 0);
            gl.vertexAttribDivisor(_fLocs.iScale, 1);

            gl.bindBuffer(gl.ARRAY_BUFFER, fb.rotBuf);
            gl.enableVertexAttribArray(_fLocs.iRot);
            gl.vertexAttribPointer(_fLocs.iRot, 1, gl.FLOAT, false, 4, 0);
            gl.vertexAttribDivisor(_fLocs.iRot, 1);

            // Multi-material instanced draw — one call per material segment
            if (buf.matBreaks && buf.matBreaks.length > 1) {
                let vertOffset = 0;
                for (let m = 0; m < buf.matBreaks.length; m++) {
                    const segVerts = buf.matBreaks[m];
                    const matIdx = buf.matIndices[m];
                    const matTex = buf.materialTextures && buf.materialTextures[matIdx];
                    const loaded = buf.matTexLoaded && buf.matTexLoaded[matIdx];

                    const segTex = SE.textureCache[group.meshId] ||
                        (buf.materialTextures && buf.materialTextures[matIdx]);
                    const segValid = segTex && gl.isTexture(segTex);
                    if (segValid) {
                        gl.activeTexture(gl.TEXTURE0);
                        gl.bindTexture(gl.TEXTURE_2D, segTex);
                        gl.uniform1i(_fLocs.tex, 0);
                        gl.uniform1i(_fLocs.hasTex, 1);
                    } else {
                        gl.activeTexture(gl.TEXTURE0);
                        gl.bindTexture(gl.TEXTURE_2D, null);
                        gl.uniform1i(_fLocs.hasTex, 0);
                    }

                    gl.drawArraysInstanced(gl.TRIANGLES, vertOffset, segVerts, group.count);
                    vertOffset += segVerts;
                }
            } else {
                // Single material
              
                const cachedTex = SE.textureCache[group.meshId] ||
                    (buf.materialTextures && buf.materialTextures[0]);
                const texValid = cachedTex && gl.isTexture(cachedTex);
                if (texValid) {
                    gl.activeTexture(gl.TEXTURE0);
                    gl.bindTexture(gl.TEXTURE_2D, cachedTex);
                    gl.uniform1i(_fLocs.tex, 0);
                    gl.uniform1i(_fLocs.hasTex, 1);
                } else {
                    gl.activeTexture(gl.TEXTURE0);
                    gl.bindTexture(gl.TEXTURE_2D, null);
                    gl.uniform1i(_fLocs.hasTex, 0);
                }
           

                gl.drawArraysInstanced(gl.TRIANGLES, 0, buf.vertCount, group.count);
            }

            gl.vertexAttribDivisor(_fLocs.iPos, 0);
            gl.vertexAttribDivisor(_fLocs.iScale, 0);
            gl.vertexAttribDivisor(_fLocs.iRot, 0);
        }

        _foliageDirty = false;
        gl.enable(gl.CULL_FACE); // restore after foliage
    }

    // ============================================================
    // RESET
    // ============================================================
    function reset() {
        const gl = SE.gl;

        for (const key in _foliageBuffers) {
            const fb = _foliageBuffers[key];
            if (!fb) continue;
            if (fb.posBuf) gl.deleteBuffer(fb.posBuf);
            if (fb.scaleBuf) gl.deleteBuffer(fb.scaleBuf);
            if (fb.rotBuf) gl.deleteBuffer(fb.rotBuf);
        }

        _foliageBuffers = {};
        _foliageDirty = true; // force re-upload next frame regardless of count
        _foliageProgram = null;
        _fLocs = null;

        console.log('[SpectralFoliageSystem] Reset');
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    function getBuffers() {
        return _foliageBuffers;
    }

    return { init, render, reset, getBuffers };

})();
*/