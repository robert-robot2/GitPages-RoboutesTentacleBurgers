// ============================================================
// SpectralSkySystem.js
// Sky rendering system — day/night textures, sun, moon, clouds
// Reads shared GL context via window.SE
// ============================================================
window.SkySystem = (function () {

    let _skyProgram = null;
    let _skyLocs = null;

    // 2D texture sky
    let _skyDayTex = null;
    let _skyNightTex = null;
    let _skyTexsLoaded = { day: false, night: false };
    let _lastSkyDayUrl = null;
    let _lastSkyNightUrl = null;
    let _lastGL = null;

    // Cube map sky
    let _skyCubeLoaded = { day: false, night: false };
    let _skyCubeDayTex = null;
    let _skyCubeNightTex = null;

    // ============================================================
    // SHADERS
    // ============================================================
    const skyVsSrc = `#version 300 es
        in vec3 aPosition;
        in vec2 aTexCoord;
        uniform mat4 uMVP;
        out vec2 vTexCoord;
        out vec3 vLocalPos;
        void main() {
            vTexCoord = aTexCoord;
          vLocalPos = vec3(
    aPosition.x,
    aPosition.z,
    aPosition.y
);
            gl_Position = uMVP * vec4(aPosition, 1.0);
        }
    `;

    const skyFsSrc = `#version 300 es
precision mediump float;
in vec2 vTexCoord;
in vec3 vLocalPos;

uniform samplerCube uDayTex;
uniform samplerCube uNightTex;
uniform float uSkyBlend;
uniform vec3  uZenithColor;
uniform vec3  uHorizonColor;
uniform vec3  uSunDir;
uniform float uTimeOfDay;
uniform float uCloudOffset;
uniform float uStarOffset;
uniform float uCloudScale;
uniform float uStarScale;
uniform vec3  uMoonDir;
uniform vec3  uMoonColor;
uniform float uMoonGlow;
uniform float uCloudOpacity;
uniform float uRainbowIntensity;

out vec4 fragColor;

void main() {
    vec3 dir = normalize(vLocalPos);

    float vertFactor = clamp(dir.y * 0.5 + 0.5, 0.0, 1.0);
    float gradientFactor = pow(vertFactor, 0.6);
    vec3 gradientColor = mix(uHorizonColor, uZenithColor, gradientFactor);

    vec3 color = gradientColor;

    // ── Stars — drawn first, beneath sun/moon and clouds ────────────────────
    float cs = cos(uStarOffset * uStarScale);
    float ss = sin(uStarOffset * uStarScale);
    vec3 starDir  = vec3(dir.x * cs - dir.y * ss, dir.x * ss + dir.y * cs, dir.z);

    vec4 nightTex = texture(uNightTex, starDir);
    float nightAlpha = max(nightTex.r, max(nightTex.g, nightTex.b));
    nightAlpha *= uSkyBlend * 2.5;
    nightAlpha  = clamp(nightAlpha, 0.0, 1.0);

    color = mix(color, nightTex.rgb, nightAlpha);
    // ── End Stars ─────────────────────────────────────────────────────────

    // ── Sun ───────────────────────────────────────────────────────────────
    vec3  sunPos      = -uSunDir;
    float sunElevZ    = sunPos.y;
    float sunAngle    = acos(clamp(dot(dir, sunPos), -1.0, 1.0));
    vec3  sunDiskColor = mix(vec3(1.0, 0.40, 0.05), vec3(1.0, 0.98, 0.85),
                            clamp(sunElevZ * 1.5, 0.0, 1.0));
    float sunCore     = 1.0 - smoothstep(0.04, 0.07, sunAngle);
    float sunGlow     = 1.0 - smoothstep(0.07, 0.22, sunAngle);
    float sunScatter  = (1.0 - smoothstep(0.15, 0.55, sunAngle)) *
                        clamp(1.0 - sunElevZ * 2.5, 0.0, 1.0);
    float sunVisible  = clamp(1.0 - uSkyBlend * 2.5, 0.0, 1.0) *
                        clamp(sunElevZ + 0.12, 0.0, 1.0);
    vec3  scatterColor = mix(vec3(1.0, 0.55, 0.15),
                            vec3(1.0, 0.85, 0.6), clamp(sunElevZ, 0.0, 1.0));
    color = mix(color, scatterColor,   sunScatter * sunVisible * 0.18);
    color = mix(color, sunDiskColor,   sunGlow    * sunVisible * 0.85);
    color = mix(color, vec3(1.0, 0.99, 0.95), sunCore * sunVisible * 1.0);

    // ── Moon ──────────────────────────────────────────────────────────────
    vec3  moonPos      = -uMoonDir;
    float moonElevZ    = moonPos.y;
    float moonAngle    = acos(clamp(dot(dir, moonPos), -1.0, 1.0));
    float moonVisible  = clamp(uMoonGlow * 1.3, 0.0, 1.0) *
                         clamp(moonElevZ + 0.08, 0.0, 1.0);
    float moonCore     = 1.0 - smoothstep(0.03, 0.06, moonAngle);
    float moonGlowRing = 1.0 - smoothstep(0.06, 0.12, moonAngle);
    float moonHotspot  = 1.0 - smoothstep(0.0, 0.045, moonAngle);
    color = mix(color, uMoonColor * 0.7, moonGlowRing * moonVisible * 0.4);
    color = mix(color, uMoonColor * 1.4, moonCore     * moonVisible * 1.0);
    color += uMoonColor * moonHotspot * moonVisible * 0.4;
    // ── End Sun/Moon ──────────────────────────────────────────────────────

    // ── Clouds — drawn last, over everything above ───────────────────────────
    float cc = cos(uCloudOffset * uCloudScale);
    float sc = sin(uCloudOffset * uCloudScale);
    vec3 cloudDir = vec3(dir.x * cc - dir.y * sc, dir.x * sc + dir.y * cc, dir.z);

    vec4 dayTex = texture(uDayTex, cloudDir);

    float horizonFade = smoothstep(0.0, 0.18, abs(dir.y));
    float dayAlpha = max(dayTex.r, max(dayTex.g, dayTex.b));
    dayAlpha *= horizonFade * uCloudOpacity;

    color = mix(color, dayTex.rgb, dayAlpha);
    // ── End Clouds ────────────────────────────────────────────────────────

    float horizonBand = 1.0 - smoothstep(0.0, 0.22, abs(dir.y));
    float horizonStrength = horizonBand * 0.12 * (1.0 - uSkyBlend * 0.5);
    color = mix(color, uHorizonColor, horizonStrength);

    // ── Rainbow ────────────────────────────────────────────────────────────
    float rainbowVisible = uRainbowIntensity
        * clamp(sunElevZ + 0.15, 0.0, 1.0)
        * clamp(1.0 - uSkyBlend * 2.0, 0.0, 1.0);       
    if (rainbowVisible > 0.001) {
        vec3 antiSolar = normalize(-uSunDir);
        float angleToAntiSolar = acos(clamp(dot(dir, antiSolar), -1.0, 1.0));

        float arcCenter = 0.733;
        float arcWidth = 0.10;

        float arcFade = clamp(1.0 - dir.y * 4.0, 0.0, 1.0);

        vec3 up = vec3(0.0, 1.0, 0.0);
        vec3 antiSolarPerp = normalize(cross(antiSolar, up));
        float sideAngle = abs(dot(dir, antiSolarPerp));
        float endpointFade = smoothstep(0.95, 0.65, sideAngle);

        float totalFade = arcFade * endpointFade * rainbowVisible;

        if (totalFade > 0.001) {
            vec3 rainbow = vec3(0.0);

            float bandR   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter + 0.048)));
            float bandO   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter + 0.032)));
            float bandY   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter + 0.016)));
            float bandG   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter + 0.000)));
            float bandB   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter - 0.016)));
            float bandI   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter - 0.032)));
            float bandV   = smoothstep(0.05, 0.0, abs(angleToAntiSolar - (arcCenter - 0.048)));

            rainbow += vec3(0.95, 0.15, 0.10) * bandR;
            rainbow += vec3(1.00, 0.50, 0.05) * bandO;
            rainbow += vec3(1.00, 0.95, 0.10) * bandY;
            rainbow += vec3(0.10, 0.90, 0.20) * bandG;
            rainbow += vec3(0.10, 0.40, 1.00) * bandB;
            rainbow += vec3(0.35, 0.10, 0.90) * bandI;
            rainbow += vec3(0.60, 0.05, 0.80) * bandV;

            rainbow = clamp(rainbow, 0.0, 1.0);

            float rainbowStrength = clamp(length(rainbow), 0.0, 1.0) * totalFade * 0.75;
            color = mix(color, color + rainbow * 0.6, rainbowStrength);
        }
    }
    // ── End Rainbow ───────────────────────────────────────────────────────

    fragColor = vec4(color, 1.0);
}
`;


    // ============================================================
    // FUNCTIONS
    // ============================================================

    function init() {
        const gl = SE.gl;
        _skyProgram = SE.buildProgram(skyVsSrc, skyFsSrc);
        _skyLocs = {
            mvp: gl.getUniformLocation(_skyProgram, 'uMVP'),
            skyBlend: gl.getUniformLocation(_skyProgram, 'uSkyBlend'),
            zenith: gl.getUniformLocation(_skyProgram, 'uZenithColor'),
            horizon: gl.getUniformLocation(_skyProgram, 'uHorizonColor'),
            sunDir: gl.getUniformLocation(_skyProgram, 'uSunDir'),
            timeOfDay: gl.getUniformLocation(_skyProgram, 'uTimeOfDay'),
            cloudOffset: gl.getUniformLocation(_skyProgram, 'uCloudOffset'),
            starOffset: gl.getUniformLocation(_skyProgram, 'uStarOffset'),
            moonDir: gl.getUniformLocation(_skyProgram, 'uMoonDir'),
            moonColor: gl.getUniformLocation(_skyProgram, 'uMoonColor'),
            moonGlow: gl.getUniformLocation(_skyProgram, 'uMoonGlow'),
            dayTex: gl.getUniformLocation(_skyProgram, 'uDayTex'),
            nightTex: gl.getUniformLocation(_skyProgram, 'uNightTex'),
            pos: gl.getAttribLocation(_skyProgram, 'aPosition'),
            uv: gl.getAttribLocation(_skyProgram, 'aTexCoord'),
            cloudScale: gl.getUniformLocation(_skyProgram, 'uCloudScale'),
            starScale: gl.getUniformLocation(_skyProgram, 'uStarScale'),
            cloudOpacity: gl.getUniformLocation(_skyProgram, 'uCloudOpacity'),
            rainbowIntensity: gl.getUniformLocation(_skyProgram, 'uRainbowIntensity'),
        };
        console.log('[SkySystem] Shader initialized');
    }

    function ensureTextures(dayUrl, nightUrl) {
        const gl = SE.gl;

        // Reset if URLs changed or GL context changed
        if (dayUrl !== _lastSkyDayUrl || nightUrl !== _lastSkyNightUrl) {
            reset();
            _lastSkyDayUrl = dayUrl;
            _lastSkyNightUrl = nightUrl;
        }
        if (_lastGL !== SE.gl) {
            reset();
            _lastGL = SE.gl;
        }

        // Day texture
        if (!_skyTexsLoaded.day && dayUrl) {
            _skyDayTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, _skyDayTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([255, 0, 255, 255]));

            const img = new Image();
            img.onload = () => {
                gl.bindTexture(gl.TEXTURE_2D, _skyDayTex);
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                _skyTexsLoaded.day = true;
                console.log('[SkySystem] Day texture loaded:', img.width, img.height);
            };
            img.onerror = () => console.error('[SkySystem] Day texture FAILED:', dayUrl);
            img.src = dayUrl;
        }

        // Night texture
        if (!_skyTexsLoaded.night && nightUrl) {
            _skyNightTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, _skyNightTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([0, 0, 0, 255]));

            const img = new Image();
            img.onload = () => {
                gl.bindTexture(gl.TEXTURE_2D, _skyNightTex);
                gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                _skyTexsLoaded.night = true;
                console.log('[SkySystem] Night texture loaded:', img.width, img.height);
            };
            img.onerror = () => console.error('[SkySystem] Night texture FAILED:', nightUrl);
            img.src = nightUrl;
        }
    }

    // Single Image Per Face Loader for 2K Sky Texture
    /*
    function ensureSkyTexturesCube(dayUrl, nightUrl) {
        const gl = SE.gl;

        function loadSeamlessCube(url, callback) {
            const img = new Image();
            img.onload = () => {
                const tex = gl.createTexture();
                gl.bindTexture(gl.TEXTURE_CUBE_MAP, tex);

                // Stamp the same seamless texture onto all 6 cube faces
                const faces = [
                    gl.TEXTURE_CUBE_MAP_POSITIVE_X,
                    gl.TEXTURE_CUBE_MAP_NEGATIVE_X,
                    gl.TEXTURE_CUBE_MAP_POSITIVE_Y,
                    gl.TEXTURE_CUBE_MAP_NEGATIVE_Y,
                    gl.TEXTURE_CUBE_MAP_POSITIVE_Z,
                    gl.TEXTURE_CUBE_MAP_NEGATIVE_Z,
                ];

                for (const face of faces) {
                    gl.texImage2D(face, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                }

                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);

                callback(tex);
                console.log('[SpectralGL] Seamless cubemap loaded:', url, img.width, img.height);
            };
            img.onerror = () => console.error('[SpectralGL] Cubemap FAILED:', url);
            img.src = url;
        }

        if (!_skyCubeLoaded.day && dayUrl) {
            _skyCubeLoaded.day = true;
            loadSeamlessCube(dayUrl, (tex) => {
                _skyCubeDayTex = tex;
            });
        }

        if (!_skyCubeLoaded.night && nightUrl) {
            _skyCubeLoaded.night = true;
            loadSeamlessCube(nightUrl, (tex) => {
                _skyCubeNightTex = tex;
            });
        }
    }
    */

    // Cubemap Textures

    function ensureSkyTexturesCube(dayUrl, nightUrl) {
        const gl = SE.gl;

        // ── Guard: reset if GL context changed (page navigation recreates context) ──
        if (_lastGL !== null && _lastGL !== gl) {
            reset();
        }
        _lastGL = gl;


        function loadCubemap(url, callback) {
            const img = new Image();
            img.crossOrigin = "anonymous";
            img.onload = () => {
                const tex = gl.createTexture();
                gl.bindTexture(gl.TEXTURE_CUBE_MAP, tex);

                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                const w = img.width, h = img.height;
                const ratio = w / h;

                let faceSize, faceCoords;

                if (Math.abs(ratio - 4 / 3) < 0.02) {
                    // Horizontal cross  +X(2,1) -X(0,1) +Y(1,0) -Y(1,2) +Z(1,1) -Z(3,1)
                    faceSize = Math.round(w / 4);
                    faceCoords = [
                        { face: gl.TEXTURE_CUBE_MAP_POSITIVE_X, sx: 2, sy: 1 },
                        { face: gl.TEXTURE_CUBE_MAP_NEGATIVE_X, sx: 0, sy: 1 },
                        { face: gl.TEXTURE_CUBE_MAP_POSITIVE_Y, sx: 1, sy: 0 },
                        { face: gl.TEXTURE_CUBE_MAP_NEGATIVE_Y, sx: 1, sy: 2 },
                        { face: gl.TEXTURE_CUBE_MAP_POSITIVE_Z, sx: 1, sy: 1 },
                        { face: gl.TEXTURE_CUBE_MAP_NEGATIVE_Z, sx: 3, sy: 1 },
                    ];
                } else if (Math.abs(ratio - 3 / 4) < 0.02) {
                    // Vertical cross  +X(2,1) -X(0,1) +Y(1,0) -Y(1,2) +Z(1,1) -Z(1,3)
                    faceSize = Math.round(w / 3);
                    faceCoords = [
                        { face: gl.TEXTURE_CUBE_MAP_POSITIVE_X, sx: 2, sy: 1 },
                        { face: gl.TEXTURE_CUBE_MAP_NEGATIVE_X, sx: 0, sy: 1 },
                        { face: gl.TEXTURE_CUBE_MAP_POSITIVE_Y, sx: 1, sy: 0 },
                        { face: gl.TEXTURE_CUBE_MAP_NEGATIVE_Y, sx: 1, sy: 2 },
                        { face: gl.TEXTURE_CUBE_MAP_POSITIVE_Z, sx: 1, sy: 1 },
                        { face: gl.TEXTURE_CUBE_MAP_NEGATIVE_Z, sx: 1, sy: 3, rot: 180 },
                    ];
                } else if (Math.abs(ratio - 6) < 0.1) {
                    // Horizontal strip  +X -X +Y -Y +Z -Z left to right
                    faceSize = Math.round(w / 6);
                    const order = [
                        gl.TEXTURE_CUBE_MAP_POSITIVE_X, gl.TEXTURE_CUBE_MAP_NEGATIVE_X,
                        gl.TEXTURE_CUBE_MAP_POSITIVE_Y, gl.TEXTURE_CUBE_MAP_NEGATIVE_Y,
                        gl.TEXTURE_CUBE_MAP_POSITIVE_Z, gl.TEXTURE_CUBE_MAP_NEGATIVE_Z,
                    ];
                    faceCoords = order.map((face, i) => ({ face, sx: i, sy: 0 }));
                } else {
                    // Fallback: seamless stamp (works for solid-color or noise textures)
                    const faces = [
                        gl.TEXTURE_CUBE_MAP_POSITIVE_X, gl.TEXTURE_CUBE_MAP_NEGATIVE_X,
                        gl.TEXTURE_CUBE_MAP_POSITIVE_Y, gl.TEXTURE_CUBE_MAP_NEGATIVE_Y,
                        gl.TEXTURE_CUBE_MAP_POSITIVE_Z, gl.TEXTURE_CUBE_MAP_NEGATIVE_Z,
                    ];
                    for (const face of faces)
                        gl.texImage2D(face, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                    gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);
                    callback(tex);
                    console.log('[SkySystem] Cubemap seamless fallback:', url, w, h);
                    return;
                }

                canvas.width = canvas.height = faceSize;

                for (const { face, sx, sy, rot } of faceCoords) {
                    ctx.save();
                    ctx.clearRect(0, 0, faceSize, faceSize);
                    if (rot) {
                        ctx.translate(faceSize / 2, faceSize / 2);
                        ctx.rotate(rot * Math.PI / 180);
                        ctx.drawImage(img, sx * faceSize, sy * faceSize, faceSize, faceSize,
                            -faceSize / 2, -faceSize / 2, faceSize, faceSize);
                    } else {
                        ctx.drawImage(img, sx * faceSize, sy * faceSize, faceSize, faceSize,
                            0, 0, faceSize, faceSize);
                    }
                    ctx.restore();
                    gl.texImage2D(face, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, canvas);
                }

                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_CUBE_MAP, gl.TEXTURE_WRAP_R, gl.CLAMP_TO_EDGE);
                gl.generateMipmap(gl.TEXTURE_CUBE_MAP);

                callback(tex);
                console.log('[SkySystem] Cubemap sliced:', url, 'faceSize:', faceSize, 'ratio:', ratio.toFixed(2));
            };
            img.onerror = () => console.error('[SkySystem] Cubemap FAILED:', url);
            img.src = url;
        }

        if (!_skyCubeLoaded.day && dayUrl) {
            _skyCubeLoaded.day = true;
            loadCubemap(dayUrl, (tex) => { _skyCubeDayTex = tex; });
        }
        if (!_skyCubeLoaded.night && nightUrl) {
            _skyCubeLoaded.night = true;
            loadCubemap(nightUrl, (tex) => { _skyCubeNightTex = tex; });
        }
    }


    function render(frame, skyBuf) {
        if (!_skyProgram || !skyBuf) return;
        const gl = SE.gl;

        gl.depthMask(false);
        gl.disable(gl.DEPTH_TEST);
        gl.disable(gl.CULL_FACE);
        gl.useProgram(_skyProgram);

        gl.uniformMatrix4fv(_skyLocs.mvp, false, frame.skyMesh.mvp);
        gl.uniform1f(_skyLocs.skyBlend, frame.skyBlend ?? 0.0);
        gl.uniform3f(_skyLocs.zenith, frame.skyZenithR ?? 0.1,
            frame.skyZenithG ?? 0.45,
            frame.skyZenithB ?? 0.9);
        gl.uniform3f(_skyLocs.horizon, frame.skyHorizonR ?? 0.65,
            frame.skyHorizonG ?? 0.8,
            frame.skyHorizonB ?? 1.0);
        gl.uniform3f(_skyLocs.sunDir, frame.sunDirSkyX,
            frame.sunDirSkyY,
            frame.sunDirSkyZ);
        gl.uniform1f(_skyLocs.timeOfDay, frame.timeOfDay ?? 0.5);
        gl.uniform1f(_skyLocs.cloudOffset, frame.cloudOffset ?? 0.0);
        gl.uniform1f(_skyLocs.starOffset, frame.starOffset ?? 0.0);
        gl.uniform1f(_skyLocs.cloudScale, frame.cloudScale ?? 2.0);
        gl.uniform1f(_skyLocs.starScale, frame.starScale ?? 3.0);
        gl.uniform3f(_skyLocs.moonDir, frame.moonDirSkyX,
            frame.moonDirSkyY,
            frame.moonDirSkyZ);
        gl.uniform3f(_skyLocs.moonColor, frame.moonColorR ?? 0.7,
            frame.moonColorG ?? 0.8,
            frame.moonColorB ?? 1.0);
        gl.uniform1f(_skyLocs.moonGlow, frame.moonGlow ?? 0.0);
        gl.uniform1f(_skyLocs.cloudOpacity, frame.cloudOpacity ?? 1.0);
        gl.uniform1f(_skyLocs.rainbowIntensity, frame.rainbowIntensity ?? 0.0);

        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_CUBE_MAP, _skyCubeDayTex ?? null);
        gl.uniform1i(_skyLocs.dayTex, 0);

        gl.activeTexture(gl.TEXTURE1);
        gl.bindTexture(gl.TEXTURE_CUBE_MAP, _skyCubeNightTex ?? null);
        gl.uniform1i(_skyLocs.nightTex, 1);

        gl.bindBuffer(gl.ARRAY_BUFFER, skyBuf.vbo);
        gl.enableVertexAttribArray(_skyLocs.pos);
        gl.vertexAttribPointer(_skyLocs.pos, 3, gl.FLOAT, false, 0, 0);

        gl.bindBuffer(gl.ARRAY_BUFFER, skyBuf.ubo);
        gl.enableVertexAttribArray(_skyLocs.uv);
        gl.vertexAttribPointer(_skyLocs.uv, 2, gl.FLOAT, false, 0, 0);

     

        if (skyBuf.ibo && skyBuf.isUVSphere) {
            gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, skyBuf.ibo);
            gl.drawElements(gl.TRIANGLES, skyBuf.indexCount, gl.UNSIGNED_INT, 0);
        } else {
            gl.drawArrays(gl.TRIANGLES, 0, skyBuf.vertCount);
        }
       

        gl.depthMask(true);
        gl.enable(gl.DEPTH_TEST);
    }

    function reset() {
        _skyTexsLoaded.day = false; _skyTexsLoaded.night = false;
        _skyCubeLoaded.day = false; _skyCubeLoaded.night = false;
        _skyDayTex = null; _skyNightTex = null;
        _skyCubeDayTex = null; _skyCubeNightTex = null;
        _lastSkyDayUrl = null;      // ← ADD THIS
        _lastSkyNightUrl = null;    // ← ADD THIS
        _lastGL = null;             // ← ADD THIS
        console.log('[SkySystem] Textures reset');
    }

    return { init, render, ensureTextures, ensureSkyTexturesCube, reset };

})();