// ============================================================
// SpectralParticleSystem.js
// Particle instanced renderer — SpectralX WebGL2 Engine
// Extracted from SpectralEngine.js — Phase 1 migration
// Reads shared GL context via window.SE
// ============================================================

window.SpectralParticleSystem = (function () {

    // ============================================================
    // STATE
    // ============================================================
    let _particleProgram = null;
    let _particleQuadVbo = null;
    let _particleQuadUbo = null;
    let _particleInstanceBuffers = {};
    let _pLocs = null;

    // ============================================================
    // SHADERS
    // ============================================================
    const particleVsSrc = `#version 300 es
        in vec3 aPosition;
        in vec2 aTexCoord;
        in vec3 iOffset;      // per-instance world position
        in vec4 iColor;       // per-instance color
        in float iSize;       // per-instance scale
        uniform mat4 uVP;     // view-projection only, no model
        uniform vec3 uCamRight;
        uniform vec3 uCamUp;
        out vec2 vTexCoord;
        out vec4 vColor;
        void main() {
            vec3 worldPos = iOffset
                + uCamRight * aPosition.x * iSize
                + uCamUp    * aPosition.y * iSize;
            gl_Position = uVP * vec4(worldPos, 1.0);
            vTexCoord = aTexCoord;
            vColor = iColor;
        }`;

    const particleFsSrc = `#version 300 es
        precision mediump float;
        in vec2 vTexCoord;
        in vec4 vColor;
        uniform sampler2D uTexture;
        uniform bool uHasTexture;
        out vec4 fragColor;
        void main() {
            vec4 base = uHasTexture ? texture(uTexture, vTexCoord) : vColor;
            if (base.a < 0.1) discard;
            fragColor = base * vColor;
        }`;

    // ============================================================
    // INIT
    // ============================================================
    function init() {
        const gl = SE.gl;

        _particleInstanceBuffers = {};
        _particleQuadVbo = null;
        _particleQuadUbo = null;
        _pLocs = null;

        _particleProgram = SE.buildProgram(particleVsSrc, particleFsSrc);

        // Billboard quad — two triangles, centered at origin
        const quadVerts = new Float32Array([
            -0.5, -0.5, 0,
            0.5, -0.5, 0,
            -0.5, 0.5, 0,
            -0.5, 0.5, 0,
            0.5, -0.5, 0,
            0.5, 0.5, 0,
        ]);
        _particleQuadVbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, _particleQuadVbo);
        gl.bufferData(gl.ARRAY_BUFFER, quadVerts, gl.STATIC_DRAW);

        const quadUVs = new Float32Array([
            0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 1
        ]);
        _particleQuadUbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, _particleQuadUbo);
        gl.bufferData(gl.ARRAY_BUFFER, quadUVs, gl.STATIC_DRAW);

        // Pre-allocate max-size instance buffers — reused every frame, no GPU reallocation
        const MAX_PARTICLES = 5000;
        _particleInstanceBuffers['__preallocated__'] = {
            offsetBuf: gl.createBuffer(),
            colorBuf: gl.createBuffer(),
            sizeBuf: gl.createBuffer(),
        };
        gl.bindBuffer(gl.ARRAY_BUFFER, _particleInstanceBuffers['__preallocated__'].offsetBuf);
        gl.bufferData(gl.ARRAY_BUFFER, MAX_PARTICLES * 3 * 4, gl.DYNAMIC_DRAW);
        gl.bindBuffer(gl.ARRAY_BUFFER, _particleInstanceBuffers['__preallocated__'].colorBuf);
        gl.bufferData(gl.ARRAY_BUFFER, MAX_PARTICLES * 4 * 4, gl.DYNAMIC_DRAW);
        gl.bindBuffer(gl.ARRAY_BUFFER, _particleInstanceBuffers['__preallocated__'].sizeBuf);
        gl.bufferData(gl.ARRAY_BUFFER, MAX_PARTICLES * 1 * 4, gl.DYNAMIC_DRAW);

        // Cache all particle locations once — never call getUniformLocation in render again
        _pLocs = {
            vp: gl.getUniformLocation(_particleProgram, 'uVP'),
            camR: gl.getUniformLocation(_particleProgram, 'uCamRight'),
            camU: gl.getUniformLocation(_particleProgram, 'uCamUp'),
            tex: gl.getUniformLocation(_particleProgram, 'uTexture'),
            hasTex: gl.getUniformLocation(_particleProgram, 'uHasTexture'),
            pos: gl.getAttribLocation(_particleProgram, 'aPosition'),
            uv: gl.getAttribLocation(_particleProgram, 'aTexCoord'),
            off: gl.getAttribLocation(_particleProgram, 'iOffset'),
            col: gl.getAttribLocation(_particleProgram, 'iColor'),
            size: gl.getAttribLocation(_particleProgram, 'iSize'),
        };

        console.log('[SpectralParticleSystem] Initialized');
    }

    // ============================================================
    // RENDER
    // ============================================================
    function render(frame, activeProgram) {
        if (!_particleProgram || !frame.particleInstances || !frame.particleInstances.length) return;

        const gl = SE.gl;

        gl.useProgram(_particleProgram);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
        gl.depthMask(false);

        const { vp: vpLoc, camR: camRLoc, camU: camULoc, tex: texLoc,
            hasTex: hasTexLoc, pos: posLoc, uv: uvLoc,
            off: offLoc, col: colLoc, size: sizeLoc } = _pLocs;

        gl.uniformMatrix4fv(vpLoc, false, frame.vp);
        gl.uniform3f(camRLoc, frame.camRight[0], frame.camRight[1], frame.camRight[2]);
        gl.uniform3f(camULoc, frame.camUp[0], frame.camUp[1], frame.camUp[2]);

        // Bind shared quad geometry
        gl.bindBuffer(gl.ARRAY_BUFFER, _particleQuadVbo);
        gl.enableVertexAttribArray(posLoc);
        gl.vertexAttribPointer(posLoc, 3, gl.FLOAT, false, 0, 0);

        gl.bindBuffer(gl.ARRAY_BUFFER, _particleQuadUbo);
        gl.enableVertexAttribArray(uvLoc);
        gl.vertexAttribPointer(uvLoc, 2, gl.FLOAT, false, 0, 0);

        for (const group of frame.particleInstances) {
            if (!group || group.count <= 0) continue;
            if (!group.offsets || group.offsets.length < group.count * 3) {
                console.warn('[Particles] bad offsets', group.type, group.count, group.offsets?.length);
                continue;
            }
            if (!group.colors || group.colors.length < group.count * 4) {
                console.warn('[Particles] bad colors', group.type, group.count, group.colors?.length);
                continue;
            }
            if (!group.sizes || group.sizes.length < group.count) {
                console.warn('[Particles] bad sizes', group.type, group.count, group.sizes?.length);
                continue;
            }

            if (!_particleInstanceBuffers[group.type]) {
                _particleInstanceBuffers[group.type] = {
                    offsetBuf: gl.createBuffer(),
                    colorBuf: gl.createBuffer(),
                    sizeBuf: gl.createBuffer(),
                    maxCount: 0,
                };
            }
            const bufs = _particleInstanceBuffers[group.type];

            const offsets = group.offsets instanceof Float32Array ? group.offsets : new Float32Array(group.offsets);
            const colors = group.colors instanceof Float32Array ? group.colors : new Float32Array(group.colors);
            const sizes = group.sizes instanceof Float32Array ? group.sizes : new Float32Array(group.sizes);

            if (group.count > bufs.maxCount) {
                bufs.maxCount = group.count;
                gl.bindBuffer(gl.ARRAY_BUFFER, bufs.offsetBuf);
                gl.bufferData(gl.ARRAY_BUFFER, offsets, gl.DYNAMIC_DRAW);
                gl.bindBuffer(gl.ARRAY_BUFFER, bufs.colorBuf);
                gl.bufferData(gl.ARRAY_BUFFER, colors, gl.DYNAMIC_DRAW);
                gl.bindBuffer(gl.ARRAY_BUFFER, bufs.sizeBuf);
                gl.bufferData(gl.ARRAY_BUFFER, sizes, gl.DYNAMIC_DRAW);
            } else {
                gl.bindBuffer(gl.ARRAY_BUFFER, bufs.offsetBuf);
                gl.bufferSubData(gl.ARRAY_BUFFER, 0, offsets);
                gl.bindBuffer(gl.ARRAY_BUFFER, bufs.colorBuf);
                gl.bufferSubData(gl.ARRAY_BUFFER, 0, colors);
                gl.bindBuffer(gl.ARRAY_BUFFER, bufs.sizeBuf);
                gl.bufferSubData(gl.ARRAY_BUFFER, 0, sizes);
            }

            // Offsets — xyz per instance
            gl.bindBuffer(gl.ARRAY_BUFFER, bufs.offsetBuf);
            gl.enableVertexAttribArray(offLoc);
            gl.vertexAttribPointer(offLoc, 3, gl.FLOAT, false, 12, 0);
            gl.vertexAttribDivisor(offLoc, 1);

            // Colors — rgba per instance
            gl.bindBuffer(gl.ARRAY_BUFFER, bufs.colorBuf);
            gl.enableVertexAttribArray(colLoc);
            gl.vertexAttribPointer(colLoc, 4, gl.FLOAT, false, 16, 0);
            gl.vertexAttribDivisor(colLoc, 1);

            // Sizes — 1 float per instance
            gl.bindBuffer(gl.ARRAY_BUFFER, bufs.sizeBuf);
            gl.enableVertexAttribArray(sizeLoc);
            gl.vertexAttribPointer(sizeLoc, 1, gl.FLOAT, false, 4, 0);
            gl.vertexAttribDivisor(sizeLoc, 1);

            // Texture
            const tex = SE.textureCache[group.texKey];
            if (tex) {
                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.uniform1i(texLoc, 0);
                gl.uniform1i(hasTexLoc, 1);
            } else {
                gl.uniform1i(hasTexLoc, 0);
            }

            gl.drawArraysInstanced(gl.TRIANGLES, 0, 6, group.count);

            gl.vertexAttribDivisor(offLoc, 0);
            gl.vertexAttribDivisor(colLoc, 0);
            gl.vertexAttribDivisor(sizeLoc, 0);
        }

        // Restore active program and depth state
        if (activeProgram) gl.useProgram(activeProgram);
        gl.depthMask(true);
    }

    // ============================================================
    // RESET
    // ============================================================
    function reset() {
        _particleInstanceBuffers = {};
        const particleKeys = [
            'ParticleGeo_/iAssets/RainDrop01.png',
            'ParticleGeo_/iAssets/SnowFlake06.png',
            'ParticleGeo_/iAssets/GOkuCloud001.png',
            'ParticleGeo_/iAssets/LBolt002.png',
        ];
        particleKeys.forEach(key => {
            delete SE.textureCache[key];
            delete SE.meshBuffers[key];
            delete SE.textureReady[key];
        });

        // Reset per-type instance buffers
        for (const type in _particleInstanceBuffers) {
            if (type !== '__preallocated__')
                delete _particleInstanceBuffers[type];
        }

        console.log('[SpectralParticleSystem] Textures and instance buffers reset');
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return { init, render, reset };

})();