// ============================================================
// SpectralCubeCitySystem.js
// Cube City instanced renderer — SpectralX WebGL2 Engine
// Extracted from SpectralEngine.js — Phase 4 migration
// Reads shared GL context via window.SE
// ============================================================

window.SpectralCubeCitySystem = (function () {

    // ============================================================
    // STATE
    // ============================================================
    let _cubeCityProgram = null;
    let _cubeCityBuffers = {};
    let _ccLocs = null;

    // ============================================================
    // SHADERS
    // ============================================================
    const ccVsSrc = `#version 300 es
    in vec3 aPosition;
    in vec3 aNormal;
    in vec2 aTexCoord;
    in vec3 iInstancePos;
    in float iInstanceScale;
    in float iInstanceRot;
    in vec3 iInstanceColor;
    uniform mat4 uVP;
    uniform mat4 uLightVP;
    out vec3 vNormal;
    out vec3 vWorldPos;
    out vec2 vTexCoord;
    out vec4 vShadowCoord;
    out vec3 vColor;
    void main() {
        float c = cos(iInstanceRot);
        float s = sin(iInstanceRot);
        // Y-axis rotation
        vec3 rotated = vec3(
            aPosition.x * c - aPosition.y * s,
            aPosition.x * s + aPosition.y * c,
            aPosition.z
        );
        vec3 worldPos = rotated * iInstanceScale + iInstancePos;
        vWorldPos    = worldPos;
        vNormal      = vec3(-s * aNormal.x + c * aNormal.y,
                             c * aNormal.x + s * aNormal.y,
                             aNormal.z);
        vTexCoord    = aTexCoord;
        vShadowCoord = uLightVP * vec4(worldPos, 1.0);
        vColor       = iInstanceColor;
        gl_Position  = uVP * vec4(worldPos, 1.0);
    }`;

    const ccFsSrc = `#version 300 es
    precision mediump float;
    in vec3 vNormal;
    in vec3 vWorldPos;
    in vec2 vTexCoord;
    in vec4 vShadowCoord;
    in vec3 vColor;
    uniform int uLightCount;
    uniform vec3 uLightPos[32];
    uniform vec3 uLightColor[32];
    uniform float uLightIntensity[32];
    uniform float uLightRange[32];
    uniform int uLightType[32];
    uniform sampler2D uShadowMap;
    out vec4 fragColor;

    float shadowFactor() {
        vec3 proj = vShadowCoord.xyz / vShadowCoord.w;
        proj = proj * 0.5 + 0.5;
        if (proj.x < 0.0 || proj.x > 1.0 ||
            proj.y < 0.0 || proj.y > 1.0 ||
            proj.z > 1.0) return 1.0;
        float bias = 0.004;
        float depth = texture(uShadowMap, proj.xy).r;
        return proj.z - bias > depth ? 0.35 : 1.0;
    }

    void main() {
        vec3 norm    = normalize(vNormal);
        vec3 ambient = vec3(0.25);
        vec3 light   = ambient;
        for (int i = 0; i < 32; i++) {
            if (i >= uLightCount) break;
            if (uLightType[i] == 1) {
                vec3 dir  = normalize(-uLightPos[i]);
                float diff = max(dot(norm, dir), 0.0);
                light += uLightColor[i] * uLightIntensity[i] * diff * shadowFactor();
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
        light = clamp(light, 0.0, 2.5);
        fragColor = vec4(vColor * light, 1.0);
    }`;

    // ============================================================
    // INIT
    // ============================================================
    function init() {
        const gl = SE.gl;
        const MAX_LIGHTS = 32;

        _cubeCityProgram = SE.buildProgram(ccVsSrc, ccFsSrc);

        _ccLocs = {
            vp: gl.getUniformLocation(_cubeCityProgram, 'uVP'),
            lightVP: gl.getUniformLocation(_cubeCityProgram, 'uLightVP'),
            lightCount: gl.getUniformLocation(_cubeCityProgram, 'uLightCount'),
            shadowMap: gl.getUniformLocation(_cubeCityProgram, 'uShadowMap'),
            pos: gl.getAttribLocation(_cubeCityProgram, 'aPosition'),
            norm: gl.getAttribLocation(_cubeCityProgram, 'aNormal'),
            uv: gl.getAttribLocation(_cubeCityProgram, 'aTexCoord'),
            iPos: gl.getAttribLocation(_cubeCityProgram, 'iInstancePos'),
            iScale: gl.getAttribLocation(_cubeCityProgram, 'iInstanceScale'),
            iRot: gl.getAttribLocation(_cubeCityProgram, 'iInstanceRot'),
            iColor: gl.getAttribLocation(_cubeCityProgram, 'iInstanceColor'),
            lightPos: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_cubeCityProgram, `uLightPos[${i}]`)),
            lightColor: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_cubeCityProgram, `uLightColor[${i}]`)),
            lightIntensity: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_cubeCityProgram, `uLightIntensity[${i}]`)),
            lightRange: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_cubeCityProgram, `uLightRange[${i}]`)),
            lightType: Array.from({ length: MAX_LIGHTS }, (_, i) =>
                gl.getUniformLocation(_cubeCityProgram, `uLightType[${i}]`)),
        };

        // Pre-allocate GPU instance buffers — one set, reused every frame
        _cubeCityBuffers = {
            posBuf: gl.createBuffer(),
            scaleBuf: gl.createBuffer(),
            rotBuf: gl.createBuffer(),
            colorBuf: gl.createBuffer(),
            maxCount: 0,
        };

       // console.log('[SpectralCubeCitySystem] Initialized');
    }

    // ============================================================
    // RENDER
    // ============================================================
    function render(frame) {
        if (!frame.cubeCityInstances || frame.cubeCityInstances.count <= 0) return;

        const gl = SE.gl;
        const grp = frame.cubeCityInstances;

        if (!_cubeCityProgram) init();
        if (!_cubeCityProgram) return;

        const buf = SE.meshBuffers[grp.meshId];
        if (!buf || buf.vertCount < 3) return;

        gl.useProgram(_cubeCityProgram);
        gl.enable(gl.DEPTH_TEST);
        gl.depthMask(true);
        gl.disable(gl.BLEND);
        gl.enable(gl.CULL_FACE);
        gl.cullFace(gl.BACK);

        gl.uniformMatrix4fv(_ccLocs.vp, false, frame.vp);
        gl.uniform1i(_ccLocs.lightCount, frame.lightCount);

        for (let li = 0; li < frame.lightCount; li++) {
            gl.uniform3f(_ccLocs.lightPos[li],
                frame.lightPositions[li * 3],
                frame.lightPositions[li * 3 + 1],
                frame.lightPositions[li * 3 + 2]);
            gl.uniform3f(_ccLocs.lightColor[li],
                frame.lightColors[li * 3],
                frame.lightColors[li * 3 + 1],
                frame.lightColors[li * 3 + 2]);
            gl.uniform1f(_ccLocs.lightIntensity[li], frame.lightIntensities[li]);
            gl.uniform1f(_ccLocs.lightRange[li], frame.lightRanges[li]);
            gl.uniform1i(_ccLocs.lightType[li], frame.lightTypes[li]);
        }

        // Shadow map — directional light slot
        let sunIdx = -1;
        if (frame.lightTypes) {
            for (let i = 0; i < frame.lightCount; i++) {
                if (frame.lightTypes[i] === 1) { sunIdx = i; break; }
            }
        }
        if (sunIdx >= 0 && SE.shadowDepthTexs[sunIdx]) {
            gl.activeTexture(gl.TEXTURE1);
            gl.bindTexture(gl.TEXTURE_2D, SE.shadowDepthTexs[sunIdx]);
            gl.uniform1i(_ccLocs.shadowMap, 1);
            if (frame.lightVPs && frame.lightVPs[sunIdx])
                gl.uniformMatrix4fv(_ccLocs.lightVP, false,
                    new Float32Array(frame.lightVPs[sunIdx]));
        }

        // Instance data — bufferSubData when fits, bufferData when growing
        const positions = grp.positions instanceof Float32Array ? grp.positions : new Float32Array(grp.positions);
        const scales = grp.scales instanceof Float32Array ? grp.scales : new Float32Array(grp.scales);
        const rotations = grp.rotations instanceof Float32Array ? grp.rotations : new Float32Array(grp.rotations);
        const colors = grp.colors instanceof Float32Array ? grp.colors : new Float32Array(grp.colors);

        const cb = _cubeCityBuffers;
        if (grp.count > cb.maxCount) {
            cb.maxCount = grp.count;
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.posBuf); gl.bufferData(gl.ARRAY_BUFFER, positions, gl.DYNAMIC_DRAW);
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.scaleBuf); gl.bufferData(gl.ARRAY_BUFFER, scales, gl.DYNAMIC_DRAW);
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.rotBuf); gl.bufferData(gl.ARRAY_BUFFER, rotations, gl.DYNAMIC_DRAW);
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.colorBuf); gl.bufferData(gl.ARRAY_BUFFER, colors, gl.DYNAMIC_DRAW);
        } else {
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.posBuf); gl.bufferSubData(gl.ARRAY_BUFFER, 0, positions);
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.scaleBuf); gl.bufferSubData(gl.ARRAY_BUFFER, 0, scales);
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.rotBuf); gl.bufferSubData(gl.ARRAY_BUFFER, 0, rotations);
            gl.bindBuffer(gl.ARRAY_BUFFER, cb.colorBuf); gl.bufferSubData(gl.ARRAY_BUFFER, 0, colors);
        }

        // Geometry buffers
        gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
        gl.enableVertexAttribArray(_ccLocs.pos);
        gl.vertexAttribPointer(_ccLocs.pos, 3, gl.FLOAT, false, 0, 0);

        gl.bindBuffer(gl.ARRAY_BUFFER, buf.nbo);
        gl.enableVertexAttribArray(_ccLocs.norm);
        gl.vertexAttribPointer(_ccLocs.norm, 3, gl.FLOAT, false, 0, 0);

        gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
        gl.enableVertexAttribArray(_ccLocs.uv);
        gl.vertexAttribPointer(_ccLocs.uv, 2, gl.FLOAT, false, 0, 0);

        // Instance position (xyz)
        gl.bindBuffer(gl.ARRAY_BUFFER, cb.posBuf);
        gl.enableVertexAttribArray(_ccLocs.iPos);
        gl.vertexAttribPointer(_ccLocs.iPos, 3, gl.FLOAT, false, 12, 0);
        gl.vertexAttribDivisor(_ccLocs.iPos, 1);

        // Instance scale (1 float)
        gl.bindBuffer(gl.ARRAY_BUFFER, cb.scaleBuf);
        gl.enableVertexAttribArray(_ccLocs.iScale);
        gl.vertexAttribPointer(_ccLocs.iScale, 1, gl.FLOAT, false, 4, 0);
        gl.vertexAttribDivisor(_ccLocs.iScale, 1);

        // Instance rotation (1 float, Y axis)
        gl.bindBuffer(gl.ARRAY_BUFFER, cb.rotBuf);
        gl.enableVertexAttribArray(_ccLocs.iRot);
        gl.vertexAttribPointer(_ccLocs.iRot, 1, gl.FLOAT, false, 4, 0);
        gl.vertexAttribDivisor(_ccLocs.iRot, 1);

        // Instance color (rgb)
        gl.bindBuffer(gl.ARRAY_BUFFER, cb.colorBuf);
        gl.enableVertexAttribArray(_ccLocs.iColor);
        gl.vertexAttribPointer(_ccLocs.iColor, 3, gl.FLOAT, false, 12, 0);
        gl.vertexAttribDivisor(_ccLocs.iColor, 1);

        gl.drawArraysInstanced(gl.TRIANGLES, 0, buf.vertCount, grp.count);

        // Reset divisors
        gl.vertexAttribDivisor(_ccLocs.iPos, 0);
        gl.vertexAttribDivisor(_ccLocs.iScale, 0);
        gl.vertexAttribDivisor(_ccLocs.iRot, 0);
        gl.vertexAttribDivisor(_ccLocs.iColor, 0);

        gl.disable(gl.CULL_FACE);
        gl.enable(gl.BLEND);
    }

    // ============================================================
    // RESET
    // ============================================================
    function reset() {
        _cubeCityProgram = null;
        _ccLocs = null;
        _cubeCityBuffers = {};
       // console.log('[SpectralCubeCitySystem] Reset');
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return { init, render, reset };

})();