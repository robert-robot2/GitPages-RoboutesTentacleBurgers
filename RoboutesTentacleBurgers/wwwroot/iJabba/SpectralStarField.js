window.StarFieldSystem = (function () {

    let _program = null;
    let _locs = null;
    let _vbo = null;
    let _starCount = 0;
    let _time = 0;

    const STAR_COUNT = 1200;
    const CUBE_SIZE = 850.0;

    const vsSrc = `#version 300 es
        in vec3 aPosition;
        in float aSeed;
        in float aSize;
        uniform mat4 uMVP;
        uniform float uTime;
        uniform float uNightFade;
        out float vAlpha;
        out float vColor;
        void main() {
            float twinkle = 0.6 + 0.4 * sin(uTime * (1.5 + aSeed * 3.0) + aSeed * 6.28);
            vAlpha = twinkle * uNightFade;
            vColor = aSeed;
            gl_Position = uMVP * vec4(aPosition, 1.0);
            gl_PointSize = aSize * (0.8 + 0.4 * twinkle);
        }
    `;

    const fsSrc = `#version 300 es
        precision mediump float;
        in float vAlpha;
        in float vColor;
        out vec4 fragColor;
        void main() {
            vec2 coord = gl_PointCoord - vec2(0.5);
            float dist = length(coord);
            if (dist > 0.5) discard;
            float soft = 1.0 - smoothstep(0.15, 0.5, dist);
            vec3 col = mix(
                vec3(1.0, 0.97, 0.88),
                vec3(0.85, 0.92, 1.0),
                vColor
            );
            fragColor = vec4(col, soft * vAlpha);
        }
    `;

    function randomOnCubeFace() {
        // Distribute stars across all 6 faces of the cube
        const face = Math.floor(Math.random() * 6);
        const a = (Math.random() * 2 - 1) * CUBE_SIZE;
        const b = (Math.random() * 2 - 1) * CUBE_SIZE;
        switch (face) {
            case 0: return [CUBE_SIZE, a, b];
            case 1: return [-CUBE_SIZE, a, b];
            case 2: return [a, CUBE_SIZE, b];
            case 3: return [a, -CUBE_SIZE, b];
            case 4: return [a, b, CUBE_SIZE];
            case 5: return [a, b, -CUBE_SIZE];
        }
    }

    function init() {
        const gl = SE.gl;
        _program = SE.buildProgram(vsSrc, fsSrc);
        _locs = {
            pos: gl.getAttribLocation(_program, 'aPosition'),
            seed: gl.getAttribLocation(_program, 'aSeed'),
            size: gl.getAttribLocation(_program, 'aSize'),
            mvp: gl.getUniformLocation(_program, 'uMVP'),
            time: gl.getUniformLocation(_program, 'uTime'),
            night: gl.getUniformLocation(_program, 'uNightFade'),
        };

        // 5 floats per star: x, y, z, seed, size
        const data = new Float32Array(STAR_COUNT * 5);
        for (let i = 0; i < STAR_COUNT; i++) {
            const pos = randomOnCubeFace();
            const base = i * 5;
            data[base] = pos[0];
            data[base + 1] = pos[1];
            data[base + 2] = pos[2];
            data[base + 3] = Math.random();       // seed
            data[base + 4] = 1.5 + Math.random() * 2.5; // size
        }

        _vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, _vbo);
        gl.bufferData(gl.ARRAY_BUFFER, data, gl.STATIC_DRAW);
        _starCount = STAR_COUNT;
        console.log('[StarField] Initialized —', STAR_COUNT, 'stars on cube faces');
    }

    function render(frame) {
        if (!_program || !_vbo) { console.warn('[StarField] not ready'); return; }

        const skyBlend = frame.skyBlend ?? 0.0;
        if (skyBlend < 0.05) return;

        const nightFade = Math.min(skyBlend * 2.0, 1.0);
        const gl = SE.gl;
        _time += 0.016;

        gl.depthMask(false);
        gl.disable(gl.DEPTH_TEST);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE);

        gl.useProgram(_program);
        // TEMP TEST — bypass skyMesh.mvp, use raw camera-relative transform
        gl.uniformMatrix4fv(_locs.mvp, false, frame.vp); // no model matrix at all
        gl.uniform1f(_locs.time, _time);
        gl.uniform1f(_locs.night, nightFade);

        gl.bindBuffer(gl.ARRAY_BUFFER, _vbo);

        const stride = 5 * 4;
        gl.enableVertexAttribArray(_locs.pos);
        gl.vertexAttribPointer(_locs.pos, 3, gl.FLOAT, false, stride, 0);

        gl.enableVertexAttribArray(_locs.seed);
        gl.vertexAttribPointer(_locs.seed, 1, gl.FLOAT, false, stride, 12);

        gl.enableVertexAttribArray(_locs.size);
        gl.vertexAttribPointer(_locs.size, 1, gl.FLOAT, false, stride, 16);

        gl.drawArrays(gl.POINTS, 0, _starCount);

        gl.depthMask(true);
        gl.enable(gl.DEPTH_TEST);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
    }

    function reset() {
        _time = 0;
        console.log('[StarField] Reset');
    }

    return { init, render, reset };
})();