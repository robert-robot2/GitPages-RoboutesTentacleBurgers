window.ShootingStarSystem = (function () {

    let _program = null;
    let _locs = null;
    let _vbo = null;
    let _stars = [];
    let _spawnTimer = 0;
    let _spawnInterval = 4.0;

    const MAX_STARS = 8;
    const CUBE_SIZE = 850.0; // just inside sky cube

    const vsSrc = `#version 300 es
        in vec3 aPosition;
        in float aAlpha;
        uniform mat4 uMVP;
        out float vAlpha;
        void main() {
            vAlpha = aAlpha;
            gl_Position = uMVP * vec4(aPosition, 1.0);
        }
    `;

    const fsSrc = `#version 300 es
        precision mediump float;
        in float vAlpha;
        out vec4 fragColor;
        void main() {
            fragColor = vec4(1.0, 0.97, 0.90, vAlpha);
        }
    `;

    function randomCubePoint() {
        // Pick a random face of the cube, spawn on upper 4 faces only
        // Faces: +X, -X, +Z (top), +Y, -Y
        // We want upper hemisphere so bias toward top faces
        const face = Math.floor(Math.random() * 5);
        const a = (Math.random() * 2 - 1) * CUBE_SIZE;
        const b = (Math.random() * 2 - 1) * CUBE_SIZE;
        switch (face) {
            case 0: return [CUBE_SIZE, Math.abs(b), a];   // +X upper
            case 1: return [-CUBE_SIZE, Math.abs(b), a];   // -X upper
            case 2: return [a, CUBE_SIZE, b];               // +Y top face
            case 3: return [a, Math.abs(b), CUBE_SIZE];    // +Z upper
            case 4: return [a, Math.abs(b), -CUBE_SIZE];   // -Z upper
        }
    }

    function spawnStar() {
        const pos = randomCubePoint();

        // Velocity — drift across the cube face
        const speed = 2.5 + Math.random() * 3.5;
        const vx = (Math.random() - 0.5) * speed;
        const vy = -(speed * 0.4 + Math.random() * speed * 0.3);
        const vz = (Math.random() - 0.5) * speed;

        _stars.push({
            x: pos[0], y: pos[1], z: pos[2],
            vx, vy, vz,
            alpha: 1.0,
            decay: 0.008 + Math.random() * 0.012,
            trailLength: 12 + Math.random() * 20,
        });
        console.log('[ShootingStars] Spawned at:', pos[0].toFixed(0), pos[1].toFixed(0), pos[2].toFixed(0));
    }

    function render(frame) {
        if (!_program) { console.warn('[ShootingStars] no program'); return; }

        const skyBlend = frame.skyBlend ?? 0.0;
        if (skyBlend < 0.3) return;

        const nightFade = Math.min((skyBlend - 0.3) / 0.3, 1.0);
        const gl = SE.gl;

        _spawnTimer += 0.016;
        if (_spawnTimer >= _spawnInterval && _stars.length < MAX_STARS) {
            spawnStar();
            _spawnTimer = 0;
            _spawnInterval = 3.0 + Math.random() * 5.0;
        }

        for (let i = _stars.length - 1; i >= 0; i--) {
            const s = _stars[i];
            s.x += s.vx;
            s.y += s.vy;
            s.z += s.vz;
            s.alpha -= s.decay;
            if (s.alpha <= 0) _stars.splice(i, 1);
        }

        if (_stars.length === 0) return;

        const data = new Float32Array(_stars.length * 8);
        let idx = 0;
        for (const s of _stars) {
            // Head
            data[idx++] = s.x;
            data[idx++] = s.y;
            data[idx++] = s.z;
            data[idx++] = s.alpha * nightFade;
            // Tail
            data[idx++] = s.x - s.vx * s.trailLength;
            data[idx++] = s.y - s.vy * s.trailLength;
            data[idx++] = s.z - s.vz * s.trailLength;
            data[idx++] = 0.0;
        }

        gl.depthMask(false);
        gl.disable(gl.DEPTH_TEST);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE);

        gl.useProgram(_program);
        gl.uniformMatrix4fv(_locs.mvp, false, frame.vp); // no model matrix at all

        gl.bindBuffer(gl.ARRAY_BUFFER, _vbo);
        gl.bufferData(gl.ARRAY_BUFFER, data, gl.DYNAMIC_DRAW);

        gl.enableVertexAttribArray(_locs.pos);
        gl.vertexAttribPointer(_locs.pos, 3, gl.FLOAT, false, 16, 0);

        gl.enableVertexAttribArray(_locs.alpha);
        gl.vertexAttribPointer(_locs.alpha, 1, gl.FLOAT, false, 16, 12);

        gl.drawArrays(gl.LINES, 0, _stars.length * 2);

        gl.depthMask(true);
        gl.enable(gl.DEPTH_TEST);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
    }

    function init() {
        const gl = SE.gl;
        _program = SE.buildProgram(vsSrc, fsSrc);
        _locs = {
            pos: gl.getAttribLocation(_program, 'aPosition'),
            alpha: gl.getAttribLocation(_program, 'aAlpha'),
            mvp: gl.getUniformLocation(_program, 'uMVP'),
        };
        _vbo = gl.createBuffer();
        console.log('[ShootingStars] Initialized');
    }

    function reset() {
        _stars = [];
        _spawnTimer = 0;
        console.log('[ShootingStars] Reset');
    }

    return { init, render, reset };
})();