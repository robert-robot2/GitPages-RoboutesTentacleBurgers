window.SpectralLightningSystem = (function () {

    'use strict';

    let _gl = null;
    let _programChannel = null;
    let _programFlash = null;
    let _programSpark = null;
    let _locsChannel = null;
    let _locsFlash = null;
    let _locsSpark = null;
    let _vboChannel = null;
    let _vboFlash = null;
    let _vboSpark = null;

    let _flashes = [];
    let _sparks = [];
    let _ccFlashes = [];
    let _spawnTimer = 0;
    let _spawnInterval = 1.8;
    let _ccSpawnTimer = 0;
    let _ccSpawnInterval = 4.0;
    let _skyFlashLevel = 0.0;
    let _thunderQueue = [];
    let _frameIndex = 0;
    let _rngState = 88172645463325252;
    let _lastCamPos = [0, 0, 0];
    let _lastVP = null;
    let _stormCells = [];
    let _scorchDecals = [];
    let _camShakeState = { x: 0, y: 0, z: 0, energy: 0 };
    let _beadFlashes = [];
    let _anvilFlashes = [];
    let _anvilCrawlerTimer = 0;

    const MAX_FLASHES = 10;
    const MAX_CC_FLASHES = 4;
    const MAX_SPARKS = 400;
    const MAX_THUNDER_QUEUE = 16;

    const BOLT_RADIUS_MIN = 40.0;
    const BOLT_RADIUS_MAX = 380.0;
    const BOLT_Z_MIN = 300.0;
    const BOLT_Z_MAX = 620.0;
    const BOLT_Z_TARGET_MIN = 40.0;
    const BOLT_Z_TARGET_MAX = 130.0;

    const STROKE_RISE_FRAMES = 2;
    const STROKE_HOLD_FRAMES = 3;
    const STROKE_DECAY_FRAMES = 20;
    const RESTRIKE_DELAY_MIN = 5;
    const RESTRIKE_DELAY_MAX = 14;
    const MAX_STROKES = 4;
    const DART_LEADER_CHANCE = 0.55;
    const DART_LEADER_JITTER_SCALE = 0.12;

    const MAX_DEPTH = 5;
    const BRANCH_PROB_BASE = 0.42;
    const BRANCH_PROB_DEPTH_FALLOFF = 0.08;
    const MIN_SEG_LENGTH = 10.0;
    const MAX_BRANCH_ANGLE = Math.PI * 0.62;

    const SOUND_SPEED = 343.0;
    const THUNDER_RUMBLE_TAIL = 2.4;

    // ---- Electric field / dielectric breakdown constants ----
    const AIR_BREAKDOWN_FIELD_STP = 3.0e6;
    const BREAKDOWN_HUMIDITY_COEFF = 0.18;
    const BREAKDOWN_TEMP_COEFF = 0.004;
    const FIELD_SAMPLE_RADIUS = 26.0;
    const FIELD_BIAS_STRENGTH = 0.38;
    const SPACE_CHARGE_FALLOFF = 1.6;

    // ---- NEW: heading-based leader-growth constants ----
    // Replaces the old jitterMagnitudeForDepth/stepDirectionBias
    // approach. Instead of perturbing each new POINT sideways by an
    // independent random offset (which lets a leader zigzag past its
    // own target and fold back on itself, reading as a fuzzy scribble),
    // the leader now walks in HEADING: each step turns by a small
    // bounded angle from the previous heading, that turn allowance is
    // damped toward zero as the tip nears its target so it straightens
    // in rather than overshooting, and branches only spawn within a
    // forward-biased cone off the current heading so they can never
    // fold back across their own parent channel. Step length is roughly
    // constant, giving sharp evenly-spaced kinks instead of a smooth
    // noisy curve.
    const LEADER_STEP_LEN = 18.0;
    const LEADER_HEADING_JITTER = 0.5;
    const LEADER_STRAIGHTEN = 0.55;
    const LEADER_BRANCH_CONE_MIN = 0.35;
    const LEADER_BRANCH_CONE_MAX = 1.05;

    // ---- Spark particle constants ----
    const SPARK_LIFETIME_MIN = 0.12;
    const SPARK_LIFETIME_MAX = 0.42;
    const SPARK_SPEED_MIN = 60.0;
    const SPARK_SPEED_MAX = 240.0;
    const SPARK_GRAVITY = 180.0;
    const SPARK_DRAG = 2.2;
    const SPARK_SPAWN_PER_TERMINATION_MIN = 2;
    const SPARK_SPAWN_PER_TERMINATION_MAX = 6;
    const SPARK_SIZE_MIN = 0.6;
    const SPARK_SIZE_MAX = 2.2;

    // ---- Cloud-to-cloud discharge constants ----
    const CC_HORIZONTAL_MIN = 220.0;
    const CC_HORIZONTAL_MAX = 620.0;
    const CC_Z_JITTER = 60.0;
    const CC_BRIGHTNESS_SCALE = 0.7;
    const CC_WIDTH_SCALE = 0.8;

    // ---- Storm cell constants ----
    const MAX_STORM_CELLS = 5;
    const STORM_CELL_RADIUS_MIN = 900.0;
    const STORM_CELL_RADIUS_MAX = 2600.0;
    const STORM_CELL_DRIFT_MIN = 4.0;
    const STORM_CELL_DRIFT_MAX = 22.0;
    const STORM_CELL_LIFETIME_MIN = 90.0;
    const STORM_CELL_LIFETIME_MAX = 340.0;
    const STORM_CELL_INTENSITY_RAMP = 0.12;

    // ---- Turbulence / coherent noise constants ----
    const NOISE_GRID_SIZE = 17;
    const NOISE_WORLD_SCALE = 0.018;
    const TURBULENCE_STRENGTH = 0.34;
    const TURBULENCE_OCTAVES = 3;
    const TURBULENCE_LACUNARITY = 2.05;
    const TURBULENCE_GAIN = 0.55;

    // ---- Bead (staccato) lightning constants ----
    const BEAD_SEGMENT_COUNT_MIN = 5;
    const BEAD_SEGMENT_COUNT_MAX = 11;
    const BEAD_HOLD_FRAMES = 6;
    const BEAD_GAP_FRAMES = 2;
    const BEAD_GLOW_RADIUS = 3.2;

    // ---- Anvil crawler constants ----
    const ANVIL_SEGMENT_COUNT_MIN = 6;
    const ANVIL_SEGMENT_COUNT_MAX = 14;
    const ANVIL_HORIZONTAL_SPAN_MIN = 500.0;
    const ANVIL_HORIZONTAL_SPAN_MAX = 1400.0;
    const ANVIL_CRAWL_DURATION_MIN = 0.6;
    const ANVIL_CRAWL_DURATION_MAX = 1.6;
    const ANVIL_BRIGHTNESS_SCALE = 0.55;

    // ---- Scorch decal constants ----
    const SCORCH_LIFETIME = 14.0;
    const SCORCH_FADE_START = 9.0;
    const SCORCH_MAX_DECALS = 24;
    const SCORCH_RADIUS_SCALE = 1.4;

    // ---- Camera shake constants ----
    const SHAKE_IMPULSE_PER_INTENSITY = 3.2;
    const SHAKE_DECAY_RATE = 5.5;
    const SHAKE_FREQUENCY = 26.0;
    const SHAKE_MAX_OFFSET = 2.4;
    const SHAKE_DISTANCE_FALLOFF = 700.0;

    const CHANNEL_VS = `#version 300 es
        precision highp float;
        in vec3 aPosition;
        in float aEdgeDist;
        in float aAlpha;
        in float aSegT;
        uniform mat4 uVP;
        out float vEdgeDist;
        out float vAlpha;
        out float vSegT;
        void main() {
            vEdgeDist = aEdgeDist;
            vAlpha = aAlpha;
            vSegT = aSegT;
            gl_Position = uVP * vec4(aPosition, 1.0);
        }
    `;

    const CHANNEL_FS = `#version 300 es
        precision mediump float;
        in float vEdgeDist;
        in float vAlpha;
        in float vSegT;
        uniform vec3 uCoreColor;
        uniform vec3 uHaloColor;
        uniform float uPassType;
        uniform float uFlicker;
        out vec4 fragColor;
        void main() {
            float d = abs(vEdgeDist);
            float intensity;
            vec3 col;
            float endTaper = smoothstep(0.0, 0.05, vSegT) * smoothstep(1.0, 0.95, vSegT);
            endTaper = mix(1.0, endTaper, 0.35);
            if (uPassType < 0.5) {
                intensity = exp(-d * d * 2.0) * 0.42;
                col = uHaloColor;
            } else if (uPassType < 1.5) {
                intensity = exp(-d * d * 6.0) * 0.80;
                col = mix(uHaloColor, uCoreColor, 0.55);
            } else if (uPassType < 2.5) {
                float core = 1.0 - smoothstep(0.0, 0.22, d);
                float bloom = exp(-d * d * 16.0);
                intensity = core * 1.0 + bloom * 0.55;
                col = uCoreColor;
            } else {
                intensity = exp(-d * d * 1.2) * 0.30;
                col = uHaloColor;
            }
            intensity *= endTaper * uFlicker;
            float a = clamp(intensity * vAlpha, 0.0, 1.0);
            if (a < 0.003) discard;
            fragColor = vec4(col, a);
        }
    `;

    const FLASH_VS = `#version 300 es
        precision highp float;
        in vec2 aClip;
        void main() {
            gl_Position = vec4(aClip, 0.0, 1.0);
        }
    `;

    const FLASH_FS = `#version 300 es
        precision mediump float;
        uniform vec3 uFlashColor;
        uniform float uFlashAlpha;
        out vec4 fragColor;
        void main() {
            fragColor = vec4(uFlashColor, uFlashAlpha);
        }
    `;

    const SPARK_VS = `#version 300 es
        precision highp float;
        in vec3 aPosition;
        in vec2 aCorner;
        in float aSize;
        in float aAlpha;
        uniform mat4 uVP;
        uniform vec3 uRight;
        uniform vec3 uUp;
        out vec2 vCorner;
        out float vAlpha;
        void main() {
            vCorner = aCorner;
            vAlpha = aAlpha;
            vec3 worldPos = aPosition + (uRight * aCorner.x + uUp * aCorner.y) * aSize;
            gl_Position = uVP * vec4(worldPos, 1.0);
        }
    `;

    const SPARK_FS = `#version 300 es
        precision mediump float;
        in vec2 vCorner;
        in float vAlpha;
        uniform vec3 uSparkColor;
        out vec4 fragColor;
        void main() {
            float d = length(vCorner);
            float intensity = exp(-d * d * 5.5);
            float a = clamp(intensity * vAlpha, 0.0, 1.0);
            if (a < 0.004) discard;
            fragColor = vec4(uSparkColor, a);
        }
    `;

    function seedRandom(seed) {
        _rngState = seed >>> 0;
    }

    function rand() {
        _rngState ^= _rngState << 13;
        _rngState ^= _rngState >>> 17;
        _rngState ^= _rngState << 5;
        _rngState >>>= 0;
        return _rngState / 4294967296;
    }

    function randRange(a, b) {
        return a + rand() * (b - a);
    }

    function lerp(a, b, t) {
        return a + (b - a) * t;
    }

    function clamp01(x) {
        return x < 0 ? 0 : x > 1 ? 1 : x;
    }

    function len3(x, y, z) {
        return Math.sqrt(x * x + y * y + z * z) || 1e-6;
    }

    function norm3(x, y, z) {
        const l = len3(x, y, z);
        return [x / l, y / l, z / l];
    }

    function dot3(ax, ay, az, bx, by, bz) {
        return ax * bx + ay * by + az * bz;
    }

    function cross3(ax, ay, az, bx, by, bz) {
        return [
            ay * bz - az * by,
            az * bx - ax * bz,
            ax * by - ay * bx
        ];
    }

    function perpPair(bx, by, bz) {
        let ux = 0, uy = 1, uz = 0;
        if (Math.abs(by) > 0.9) {
            ux = 1; uy = 0; uz = 0;
        }
        let [p1x, p1y, p1z] = cross3(bx, by, bz, ux, uy, uz);
        const p1l = len3(p1x, p1y, p1z);
        p1x /= p1l; p1y /= p1l; p1z /= p1l;
        const [p2x, p2y, p2z] = cross3(bx, by, bz, p1x, p1y, p1z);
        return { p1x, p1y, p1z, p2x, p2y, p2z };
    }

    function camVecsFromVP(vp) {
        const rx = vp[0], ry = vp[1], rz = vp[2];
        const ux = vp[4], uy = vp[5], uz = vp[6];
        const fx = vp[8], fy = vp[9], fz = vp[10];
        const rl = len3(rx, ry, rz);
        const ul = len3(ux, uy, uz);
        const fl = len3(fx, fy, fz);
        return {
            rx: rx / rl, ry: ry / rl, rz: rz / rl,
            ux: ux / ul, uy: uy / ul, uz: uz / ul,
            fx: fx / fl, fy: fy / fl, fz: fz / fl
        };
    }

    function smoothstepScalar(edge0, edge1, x) {
        const t = clamp01((x - edge0) / (edge1 - edge0 || 1e-6));
        return t * t * (3 - 2 * t);
    }

    function easeOutExpo(t) {
        return t >= 1 ? 1 : 1 - Math.pow(2, -10 * t);
    }

    function easeInQuad(t) {
        return t * t;
    }

    function distanceToCamera(x, y, z, camPos) {
        const dx = x - camPos[0];
        const dy = y - camPos[1];
        const dz = z - camPos[2];
        return len3(dx, dy, dz);
    }

    function invertVP4(vp) {
        return vp;
    }

    function cameraPositionFromVP(vp, cam) {
        if (!vp || vp.length < 16) return [0, 0, 0];
        const tx = vp[12], ty = vp[13], tz = vp[14], tw = vp[15] || 1.0;
        const c = cam || camVecsFromVP(vp);
        const px = -(c.rx * tx + c.ux * ty + c.fx * tz) / tw;
        const py = -(c.ry * tx + c.uy * ty + c.fy * tz) / tw;
        const pz = -(c.rz * tx + c.uz * ty + c.fz * tz) / tw;
        if (!isFinite(px) || !isFinite(py) || !isFinite(pz)) return [0, 0, 0];
        return [px, py, pz];
    }

    function tuneWidthByAltitude(z, baseWidth) {
        const altFactor = clamp01((z - BOLT_Z_TARGET_MIN) / (BOLT_Z_MAX - BOLT_Z_TARGET_MIN));
        return baseWidth * (0.75 + altFactor * 0.5);
    }

    function ionizationDecayCurve(t) {
        return Math.exp(-t * 4.5) * (1.0 - 0.15 * Math.sin(t * 22.0));
    }

    function returnStrokeCurrentProfile(t) {
        if (t < 0.08) {
            return t / 0.08;
        }
        if (t < 0.2) {
            return 1.0;
        }
        const decayT = (t - 0.2) / 0.8;
        return Math.exp(-decayT * 5.2);
    }

    function branchProbabilityForDepth(depth) {
        return Math.max(0.02, BRANCH_PROB_BASE - depth * BRANCH_PROB_DEPTH_FALLOFF);
    }

    function widthFalloffForDepth(depth, parentWidth) {
        return parentWidth * (0.74 - depth * 0.045);
    }

    function computeSegmentRoughness(depth) {
        return 1.0 - depth * 0.12;
    }

    function selectBranchCount(depth) {
        if (depth === 0) return 2 + Math.floor(rand() * 3);
        if (depth === 1) return 2 + Math.floor(rand() * 2);
        return 1 + Math.floor(rand() * 2);
    }

    function shouldTerminateBranch(segLen, depth) {
        return segLen < MIN_SEG_LENGTH || depth > MAX_DEPTH;
    }

    // ---- Coherent value-noise turbulence field ----
    let _noiseLattice = null;

    function buildNoiseLattice() {
        const n = NOISE_GRID_SIZE;
        const size = n * n * n;
        const lattice = new Float32Array(size);
        for (let i = 0; i < size; i++) {
            lattice[i] = rand() * 2.0 - 1.0;
        }
        _noiseLattice = lattice;
        return lattice;
    }

    function latticeIndex(ix, iy, iz) {
        const n = NOISE_GRID_SIZE;
        const wx = ((ix % n) + n) % n;
        const wy = ((iy % n) + n) % n;
        const wz = ((iz % n) + n) % n;
        return (wz * n * n) + (wy * n) + wx;
    }

    function valueNoise3(x, y, z) {
        if (!_noiseLattice) buildNoiseLattice();
        const fx = x, fy = y, fz = z;
        const ix0 = Math.floor(fx), iy0 = Math.floor(fy), iz0 = Math.floor(fz);
        const ix1 = ix0 + 1, iy1 = iy0 + 1, iz1 = iz0 + 1;
        const tx = fx - ix0, ty = fy - iy0, tz = fz - iz0;
        const sx = tx * tx * (3 - 2 * tx);
        const sy = ty * ty * (3 - 2 * ty);
        const sz = tz * tz * (3 - 2 * tz);

        const c000 = _noiseLattice[latticeIndex(ix0, iy0, iz0)];
        const c100 = _noiseLattice[latticeIndex(ix1, iy0, iz0)];
        const c010 = _noiseLattice[latticeIndex(ix0, iy1, iz0)];
        const c110 = _noiseLattice[latticeIndex(ix1, iy1, iz0)];
        const c001 = _noiseLattice[latticeIndex(ix0, iy0, iz1)];
        const c101 = _noiseLattice[latticeIndex(ix1, iy0, iz1)];
        const c011 = _noiseLattice[latticeIndex(ix0, iy1, iz1)];
        const c111 = _noiseLattice[latticeIndex(ix1, iy1, iz1)];

        const x00 = lerp(c000, c100, sx);
        const x10 = lerp(c010, c110, sx);
        const x01 = lerp(c001, c101, sx);
        const x11 = lerp(c011, c111, sx);
        const y0 = lerp(x00, x10, sy);
        const y1 = lerp(x01, x11, sy);
        return lerp(y0, y1, sz);
    }

    function fractalTurbulence3(x, y, z) {
        let amplitude = 1.0;
        let frequency = 1.0;
        let sum = 0.0;
        let maxAmp = 0.0;
        for (let o = 0; o < TURBULENCE_OCTAVES; o++) {
            sum += valueNoise3(x * frequency, y * frequency, z * frequency) * amplitude;
            maxAmp += amplitude;
            amplitude *= TURBULENCE_GAIN;
            frequency *= TURBULENCE_LACUNARITY;
        }
        return maxAmp > 0 ? sum / maxAmp : 0.0;
    }

    function turbulenceVector(x, y, z) {
        const sx = x * NOISE_WORLD_SCALE;
        const sy = y * NOISE_WORLD_SCALE;
        const sz = z * NOISE_WORLD_SCALE;
        const nx = fractalTurbulence3(sx + 71.3, sy, sz);
        const ny = fractalTurbulence3(sx, sy + 133.7, sz);
        const nz = fractalTurbulence3(sx, sy, sz + 29.9);
        return [nx, ny, nz];
    }

    // ---- Dielectric breakdown field helpers ----
    function effectiveBreakdownField(humidity, temperatureC) {
        const humidityFactor = 1.0 - clamp01(humidity) * BREAKDOWN_HUMIDITY_COEFF;
        const tempDelta = temperatureC - 20.0;
        const tempFactor = 1.0 - tempDelta * BREAKDOWN_TEMP_COEFF;
        return AIR_BREAKDOWN_FIELD_STP * Math.max(0.35, humidityFactor) * Math.max(0.35, tempFactor);
    }

    function tipFieldEnhancement(distFromTip) {
        const d = Math.max(distFromTip, 1.0);
        return 1.0 + (FIELD_SAMPLE_RADIUS / d) * 0.6;
    }

    function spaceChargeShielding(x, y, z, existingSegments) {
        if (!existingSegments || existingSegments.length === 0) return 1.0;
        let minDistSq = Infinity;
        const sampleStride = Math.max(1, Math.floor(existingSegments.length / 24));
        for (let i = 0; i < existingSegments.length; i += sampleStride) {
            const s = existingSegments[i];
            const mx = (s.x0 + s.x1) * 0.5;
            const my = (s.y0 + s.y1) * 0.5;
            const mz = (s.z0 + s.z1) * 0.5;
            const dx = x - mx, dy = y - my, dz = z - mz;
            const dSq = dx * dx + dy * dy + dz * dz;
            if (dSq < minDistSq) minDistSq = dSq;
        }
        const dist = Math.sqrt(minDistSq);
        if (dist < 1e-3) return 0.15;
        return clamp01(1.0 - Math.pow(FIELD_SAMPLE_RADIUS / (dist + FIELD_SAMPLE_RADIUS), SPACE_CHARGE_FALLOFF));
    }

    function fieldBiasDirection(fromX, fromY, fromZ, targetX, targetY, targetZ) {
        const [dx, dy, dz] = norm3(targetX - fromX, targetY - fromY, targetZ - fromZ);
        return { dx, dy, dz };
    }

    // =====================================================================
    // ---- steppedLeader: heading-based leader growth (REPLACED) ----
    // =====================================================================
    // Same call signature and same output segment shape as before:
    //   steppedLeader(x0,y0,z0, x1,y1,z1, depth, parentWidth, segments, roughnessOverride)
    //   pushes {x0,y0,z0,x1,y1,z1,depth,width,alpha,roughness,terminal} into segments
    //
    // The old version jittered each new POINT sideways by independent
    // random offsets on two perpendicular axes, then added a turbulence
    // offset, then a field-bias offset — several uncorrelated position
    // displacements stacked per step, large relative to segment length.
    // That let a leader zigzag past its own target and fold back on
    // itself, which is what reads visually as a fuzzy scribble instead
    // of a lightning bolt.
    //
    // This version walks in HEADING instead: each step turns the
    // current direction by a small bounded angle, that allowed turn is
    // damped toward zero as the tip approaches its target (so it
    // straightens in rather than overshooting/backtracking), and
    // branches only spawn within a forward-biased cone measured off the
    // CURRENT heading so a branch can never fold back across its own
    // parent channel. Step length is roughly constant, which produces
    // sharp, evenly spaced kinks like real leader-step photography
    // instead of a smooth noisy curve. Coherent turbulence still steers
    // the turn direction (not raw position) so branch curvature stays
    // spatially correlated instead of independently jittering.
    // =====================================================================
    function steppedLeader(x0, y0, z0, x1, y1, z1, depth, parentWidth, segments, roughnessOverride) {
        const totalLen = len3(x1 - x0, y1 - y0, z1 - z0);

        if (totalLen < MIN_SEG_LENGTH || depth > MAX_DEPTH) {
            segments.push({
                x0, y0, z0, x1, y1, z1,
                depth,
                width: parentWidth,
                alpha: 1.0,
                roughness: roughnessOverride != null ? roughnessOverride : computeSegmentRoughness(depth),
                terminal: true
            });
            return;
        }

        let [hx, hy, hz] = norm3(x1 - x0, y1 - y0, z1 - z0);
        let x = x0, y = y0, z = z0;
        let remaining = totalLen;

        const stepLen = Math.max(6.0, LEADER_STEP_LEN * (1.0 - depth * 0.10));

        const breakdownField = effectiveBreakdownField(CONFIG.humidity, CONFIG.temperatureC);
        const breakdownFactor = clamp01(AIR_BREAKDOWN_FIELD_STP / breakdownField - 0.6);
        const branchProb = branchProbabilityForDepth(depth);
        const localBranchProb = clamp01(branchProb * (1.0 + breakdownFactor * 0.25));

        while (remaining > stepLen * 0.6) {
            const [tdx, tdy, tdz] = norm3(x1 - x, y1 - y, z1 - z);

            // damp allowed turning as the tip nears the target so it
            // straightens in instead of overshooting / backtracking
            const approachT = Math.min(1.0, remaining / (totalLen * 0.3 + 1e-6));
            const turnLimit = LEADER_HEADING_JITTER * approachT;

            // blend heading toward target direction, then apply one
            // small bounded rotation about the perpendicular plane
            hx = lerp(hx, tdx, LEADER_STRAIGHTEN);
            hy = lerp(hy, tdy, LEADER_STRAIGHTEN);
            hz = lerp(hz, tdz, LEADER_STRAIGHTEN);
            [hx, hy, hz] = norm3(hx, hy, hz);

            const pp = perpPair(hx, hy, hz);

            if (CONFIG.enableTurbulence) {
                const turb = turbulenceVector(x, y, z);
                const turnAngle = (turb[0] * 0.5 + (rand() - 0.5)) * turnLimit;
                const rollAngle = (turb[1] * 0.5 + (rand() - 0.5)) * turnLimit * 0.6;
                const cosA = Math.cos(turnAngle), sinA = Math.sin(turnAngle);
                const cosB = Math.cos(rollAngle), sinB = Math.sin(rollAngle);
                const nhx = hx * cosA + pp.p1x * sinA;
                const nhy = hy * cosA + pp.p1y * sinA;
                const nhz = hz * cosA + pp.p1z * sinA;
                hx = nhx * cosB + pp.p2x * sinB;
                hy = nhy * cosB + pp.p2y * sinB;
                hz = nhz * cosB + pp.p2z * sinB;
            } else {
                const turnAngle = (rand() - 0.5) * 2.0 * turnLimit;
                const cosA = Math.cos(turnAngle), sinA = Math.sin(turnAngle);
                hx = hx * cosA + pp.p1x * sinA;
                hy = hy * cosA + pp.p1y * sinA;
                hz = hz * cosA + pp.p1z * sinA;
            }
            [hx, hy, hz] = norm3(hx, hy, hz);

            const thisStep = Math.min(stepLen, remaining);
            const nx = x + hx * thisStep;
            const ny = y + hy * thisStep;
            const nz = z + hz * thisStep;

            segments.push({
                x0: x, y0: y, z0: z,
                x1: nx, y1: ny, z1: nz,
                depth,
                width: parentWidth,
                alpha: 1.0,
                roughness: roughnessOverride != null ? roughnessOverride : computeSegmentRoughness(depth),
                terminal: false
            });

            // forward-biased branch: cone measured off the CURRENT
            // heading, so a branch can never fold back across its own
            // parent channel.
            if (depth < 3 && remaining > 60.0 && rand() < localBranchProb) {
                const bpp = perpPair(hx, hy, hz);
                const coneAngle = randRange(LEADER_BRANCH_CONE_MIN, LEADER_BRANCH_CONE_MAX) * (rand() < 0.5 ? 1 : -1);
                const rollAngle2 = rand() * Math.PI * 2.0;
                const cosC = Math.cos(coneAngle), sinC = Math.sin(coneAngle);
                const cosR = Math.cos(rollAngle2), sinR = Math.sin(rollAngle2);
                const axisX = bpp.p1x * cosR + bpp.p2x * sinR;
                const axisY = bpp.p1y * cosR + bpp.p2y * sinR;
                const axisZ = bpp.p1z * cosR + bpp.p2z * sinR;
                let [bdx, bdy, bdz] = norm3(
                    hx * cosC + axisX * sinC,
                    hy * cosC + axisY * sinC,
                    hz * cosC + axisZ * sinC
                );

                const branchLen = remaining * randRange(0.25, 0.55);
                const bx1 = nx + bdx * branchLen;
                const by1 = ny + bdy * branchLen;
                const bz1 = nz + bdz * branchLen;
                const childWidth = widthFalloffForDepth(depth, parentWidth);

                steppedLeader(nx, ny, nz, bx1, by1, bz1, depth + 1, childWidth * 0.6, segments);

                if (depth < 1 && rand() < branchProb * 0.4) {
                    const cpp = perpPair(hx, hy, hz);
                    const coneAngle2 = randRange(LEADER_BRANCH_CONE_MIN * 0.7, LEADER_BRANCH_CONE_MAX * 0.7) * (rand() < 0.5 ? 1 : -1);
                    const rollAngle3 = rand() * Math.PI * 2.0;
                    const cosC2 = Math.cos(coneAngle2), sinC2 = Math.sin(coneAngle2);
                    const cosR2 = Math.cos(rollAngle3), sinR2 = Math.sin(rollAngle3);
                    const axisX2 = cpp.p1x * cosR2 + cpp.p2x * sinR2;
                    const axisY2 = cpp.p1y * cosR2 + cpp.p2y * sinR2;
                    const axisZ2 = cpp.p1z * cosR2 + cpp.p2z * sinR2;
                    let [cdx, cdy, cdz] = norm3(
                        hx * cosC2 + axisX2 * sinC2,
                        hy * cosC2 + axisY2 * sinC2,
                        hz * cosC2 + axisZ2 * sinC2
                    );
                    const bLen3 = remaining * randRange(0.14, 0.36);
                    const cx1 = nx + cdx * bLen3;
                    const cy1 = ny + cdy * bLen3;
                    const cz1 = nz + cdz * bLen3;
                    steppedLeader(nx, ny, nz, cx1, cy1, cz1, depth + 2, childWidth * 0.35, segments);
                }
            }

            x = nx; y = ny; z = nz;
            remaining = len3(x1 - x, y1 - y, z1 - z);
        }

        // final straight snap to exact target so branch tips (and
        // downstream terminal-point / spark logic) land precisely
        segments.push({
            x0: x, y0: y, z0: z,
            x1, y1, z1,
            depth,
            width: parentWidth,
            alpha: 1.0,
            roughness: roughnessOverride != null ? roughnessOverride : computeSegmentRoughness(depth),
            terminal: true
        });
    }

    function dartLeaderPerturb(segments, scale) {
        const out = new Array(segments.length);
        for (let i = 0; i < segments.length; i++) {
            const s = segments[i];
            const dx = s.x1 - s.x0;
            const dy = s.y1 - s.y0;
            const dz = s.z1 - s.z0;
            const segLen = len3(dx, dy, dz);
            const [nbx, nby, nbz] = norm3(dx, dy, dz);
            const pp = perpPair(nbx, nby, nbz);
            const jScale = segLen * scale;
            const j1 = (rand() - 0.5) * jScale;
            const j2 = (rand() - 0.5) * jScale * 0.6;
            out[i] = {
                x0: s.x0,
                y0: s.y0,
                z0: s.z0,
                x1: s.x1 + pp.p1x * j1 + pp.p2x * j2,
                y1: s.y1 + pp.p1y * j1 + pp.p2y * j2,
                z1: s.z1 + pp.p1z * j1 + pp.p2z * j2,
                depth: s.depth,
                width: s.width,
                alpha: s.alpha,
                roughness: s.roughness,
                terminal: s.terminal
            };
        }
        return out;
    }

    function computeSegmentTValues(segments) {
        let totalLen = 0;
        const lens = new Array(segments.length);
        for (let i = 0; i < segments.length; i++) {
            const s = segments[i];
            const l = len3(s.x1 - s.x0, s.y1 - s.y0, s.z1 - s.z0);
            lens[i] = l;
            totalLen += l;
        }
        let acc = 0;
        const ts = new Array(segments.length);
        for (let i = 0; i < segments.length; i++) {
            const t0 = acc / (totalLen || 1);
            acc += lens[i];
            const t1 = acc / (totalLen || 1);
            ts[i] = [t0, t1];
        }
        return ts;
    }

    function buildChannelGeo(segments, segTs, halfWBase, strokeBrightness, cam) {
        const floatsPerVert = 6;
        const out = new Float32Array(segments.length * 6 * floatsPerVert);
        let wi = 0;

        for (let si = 0; si < segments.length; si++) {
            const seg = segments[si];
            const x0 = seg.x0, y0 = seg.y0, z0 = seg.z0;
            const x1 = seg.x1, y1 = seg.y1, z1 = seg.z1;
            const width = seg.width;
            const alpha = seg.alpha;
            const roughness = seg.roughness != null ? seg.roughness : 1.0;
            const hw = halfWBase * width * strokeBrightness * roughness;
            const segA = alpha * strokeBrightness;
            const t0 = segTs[si][0];
            const t1 = segTs[si][1];

            const sdx = x1 - x0, sdy = y1 - y0, sdz = z1 - z0;
            const sl = len3(sdx, sdy, sdz);
            const sx = sdx / sl, sy = sdy / sl, sz = sdz / sl;

            let px = sy * cam.uz - sz * cam.uy;
            let py = sz * cam.ux - sx * cam.uz;
            let pz = sx * cam.uy - sy * cam.ux;
            const pl = len3(px, py, pz);
            px = px / pl * hw; py = py / pl * hw; pz = pz / pl * hw;

            const tlx = x0 + px, tly = y0 + py, tlz = z0 + pz;
            const trx = x0 - px, try_ = y0 - py, trz = z0 - pz;
            const blx = x1 + px, bly = y1 + py, blz = z1 + pz;
            const brx = x1 - px, bry = y1 - py, brz = z1 - pz;

            out[wi++] = tlx; out[wi++] = tly; out[wi++] = tlz; out[wi++] = 1; out[wi++] = segA; out[wi++] = t0;
            out[wi++] = trx; out[wi++] = try_; out[wi++] = trz; out[wi++] = -1; out[wi++] = segA; out[wi++] = t0;
            out[wi++] = blx; out[wi++] = bly; out[wi++] = blz; out[wi++] = 1; out[wi++] = segA; out[wi++] = t1;

            out[wi++] = trx; out[wi++] = try_; out[wi++] = trz; out[wi++] = -1; out[wi++] = segA; out[wi++] = t0;
            out[wi++] = brx; out[wi++] = bry; out[wi++] = brz; out[wi++] = -1; out[wi++] = segA; out[wi++] = t1;
            out[wi++] = blx; out[wi++] = bly; out[wi++] = blz; out[wi++] = 1; out[wi++] = segA; out[wi++] = t1;
        }
        return out;
    }

    function pickColorVariant() {
        const hueVariant = rand();
        if (hueVariant < 0.48) {
            return { core: [1.0, 1.0, 1.0], halo: [0.55, 0.75, 1.0] };
        } else if (hueVariant < 0.76) {
            return { core: [0.90, 0.95, 1.0], halo: [0.50, 0.55, 1.0] };
        } else if (hueVariant < 0.92) {
            return { core: [1.0, 0.98, 0.90], halo: [0.70, 0.80, 1.0] };
        } else {
            return { core: [0.96, 0.90, 1.0], halo: [0.62, 0.48, 1.0] };
        }
    }

    const STORM_PRESETS = {
        temperate: {
            colorBias: 'auto',
            humidity: 0.5,
            temperatureC: 15.0,
            stormIntensity: 0.5,
            windSpeed: 3.0
        },
        tropical: {
            colorBias: 'warm',
            humidity: 0.85,
            temperatureC: 28.0,
            stormIntensity: 0.8,
            windSpeed: 6.0
        },
        arctic: {
            colorBias: 'cold',
            humidity: 0.25,
            temperatureC: -8.0,
            stormIntensity: 0.35,
            windSpeed: 8.5
        },
        desert: {
            colorBias: 'dry',
            humidity: 0.08,
            temperatureC: 34.0,
            stormIntensity: 0.6,
            windSpeed: 4.5
        }
    };

    function pickColorVariantForPreset(biasName) {
        const base = pickColorVariant();
        if (!biasName || biasName === 'auto') return base;
        if (biasName === 'warm') {
            return {
                core: [Math.min(1, base.core[0] + 0.03), base.core[1], Math.max(0, base.core[2] - 0.05)],
                halo: [Math.min(1, base.halo[0] + 0.08), base.halo[1] * 0.95, Math.max(0, base.halo[2] - 0.08)]
            };
        }
        if (biasName === 'cold') {
            return {
                core: [base.core[0], base.core[1], Math.min(1, base.core[2] + 0.04)],
                halo: [Math.max(0, base.halo[0] - 0.06), base.halo[1], Math.min(1, base.halo[2] + 0.1)]
            };
        }
        if (biasName === 'dry') {
            return {
                core: [Math.min(1, base.core[0] + 0.02), Math.min(1, base.core[1] + 0.01), base.core[2]],
                halo: [Math.min(1, base.halo[0] + 0.05), base.halo[1], Math.max(0, base.halo[2] - 0.03)]
            };
        }
        return base;
    }

    function applyStormPreset(name) {
        const preset = STORM_PRESETS[name];
        if (!preset) return getConfig();
        return configure(preset);
    }

    function listStormPresets() {
        return Object.keys(STORM_PRESETS);
    }

    function spawnStormCell(originX, originY) {
        const cell = {
            x: originX != null ? originX : randRange(-1600, 1600),
            y: originY != null ? originY : randRange(-1600, 1600),
            radius: randRange(STORM_CELL_RADIUS_MIN, STORM_CELL_RADIUS_MAX),
            driftX: randRange(-1, 1) * randRange(STORM_CELL_DRIFT_MIN, STORM_CELL_DRIFT_MAX),
            driftY: randRange(-1, 1) * randRange(STORM_CELL_DRIFT_MIN, STORM_CELL_DRIFT_MAX),
            age: 0,
            lifetime: randRange(STORM_CELL_LIFETIME_MIN, STORM_CELL_LIFETIME_MAX),
            intensity: 0.0,
            targetIntensity: randRange(0.4, 1.0),
            dead: false
        };
        _stormCells.push(cell);
        return cell;
    }

    function updateStormCells(dt) {
        for (let i = _stormCells.length - 1; i >= 0; i--) {
            const c = _stormCells[i];
            c.age += dt;
            c.x += c.driftX * dt;
            c.y += c.driftY * dt;
            const lifeT = clamp01(c.age / c.lifetime);
            const rampT = clamp01(c.age / (c.lifetime * STORM_CELL_INTENSITY_RAMP));
            const fadeT = lifeT > 0.75 ? clamp01((lifeT - 0.75) / 0.25) : 0.0;
            c.intensity = c.targetIntensity * rampT * (1.0 - fadeT);
            if (c.age >= c.lifetime) {
                c.dead = true;
                _stormCells.splice(i, 1);
            }
        }
        if (CONFIG.enableStormCells) {
            while (_stormCells.length < Math.min(MAX_STORM_CELLS, CONFIG.desiredStormCellCount)) {
                spawnStormCell();
            }
        }
    }

    function pickWeightedStormCell() {
        if (_stormCells.length === 0) return null;
        let totalWeight = 0;
        for (const c of _stormCells) totalWeight += Math.max(0.01, c.intensity);
        let roll = rand() * totalWeight;
        for (const c of _stormCells) {
            const w = Math.max(0.01, c.intensity);
            if (roll < w) return c;
            roll -= w;
        }
        return _stormCells[_stormCells.length - 1];
    }

    function getStormCellOrigin() {
        if (!CONFIG.enableStormCells || _stormCells.length === 0) {
            return [0, 0];
        }
        const cell = pickWeightedStormCell();
        if (!cell) return [0, 0];
        return [cell.x, cell.y];
    }

    function getStormCells() {
        return _stormCells.map(c => ({
            x: c.x, y: c.y, radius: c.radius, intensity: c.intensity, age: c.age, lifetime: c.lifetime
        }));
    }

    function clearStormCells() {
        _stormCells = [];
    }

    function computeGroundStrikePoint(segments) {
        let lowest = null;
        for (const s of segments) {
            if (!lowest || s.z1 < lowest.z1) lowest = s;
        }
        return lowest ? [lowest.x1, lowest.y1, lowest.z1] : [0, 0, 0];
    }

    function estimateThunderDelay(originDistance) {
        return originDistance / SOUND_SPEED;
    }

    function enqueueThunder(distance, intensity) {
        if (_thunderQueue.length >= MAX_THUNDER_QUEUE) {
            _thunderQueue.shift();
        }
        _thunderQueue.push({
            delay: estimateThunderDelay(distance),
            elapsed: 0,
            intensity: intensity,
            rumbleTail: THUNDER_RUMBLE_TAIL + rand() * 1.5,
            fired: false
        });
    }

    function updateThunderQueue(dt) {
        for (let i = _thunderQueue.length - 1; i >= 0; i--) {
            const th = _thunderQueue[i];
            th.elapsed += dt;
            if (!th.fired && th.elapsed >= th.delay) {
                th.fired = true;
                if (typeof window.dispatchEvent === 'function') {
                    try {
                        window.dispatchEvent(new CustomEvent('spectral-thunder', {
                            detail: { intensity: th.intensity, rumbleTail: th.rumbleTail }
                        }));
                    } catch (e) { }
                }
                emit('thunder', { intensity: th.intensity, rumbleTail: th.rumbleTail });
            }
            if (th.fired && th.elapsed >= th.delay + th.rumbleTail) {
                _thunderQueue.splice(i, 1);
            }
        }
    }

    function findTerminalPoints(segments) {
        const out = [];
        for (const s of segments) {
            if (s.terminal) {
                out.push([s.x1, s.y1, s.z1, s.depth]);
            }
        }
        return out;
    }

    function spawnSparksForFlash(flash, brightness) {
        if (!CONFIG.enableSparks) return;
        if (_sparks.length >= MAX_SPARKS) return;
        const terminals = findTerminalPoints(flash.segments);
        if (terminals.length === 0) return;
        const maxNewBudget = MAX_SPARKS - _sparks.length;
        let spawnedThisCall = 0;
        for (const term of terminals) {
            if (spawnedThisCall >= maxNewBudget) break;
            const depthFactor = clamp01(1.0 - term[3] / (MAX_DEPTH + 1));
            const count = Math.round(lerp(SPARK_SPAWN_PER_TERMINATION_MIN, SPARK_SPAWN_PER_TERMINATION_MAX, depthFactor) * brightness);
            for (let i = 0; i < count && spawnedThisCall < maxNewBudget; i++) {
                const theta = rand() * Math.PI * 2.0;
                const phi = rand() * Math.PI;
                const speed = randRange(SPARK_SPEED_MIN, SPARK_SPEED_MAX) * (0.4 + depthFactor * 0.6);
                const vx = Math.sin(phi) * Math.cos(theta) * speed;
                const vy = Math.sin(phi) * Math.sin(theta) * speed;
                const vz = Math.abs(Math.cos(phi)) * speed * 0.6;
                _sparks.push({
                    x: term[0], y: term[1], z: term[2],
                    vx, vy, vz,
                    age: 0,
                    lifetime: randRange(SPARK_LIFETIME_MIN, SPARK_LIFETIME_MAX),
                    size: randRange(SPARK_SIZE_MIN, SPARK_SIZE_MAX),
                    color: flash.coreColor
                });
                spawnedThisCall++;
            }
        }
    }

    function updateSparks(dt) {
        for (let i = _sparks.length - 1; i >= 0; i--) {
            const sp = _sparks[i];
            sp.age += dt;
            if (sp.age >= sp.lifetime) {
                _sparks.splice(i, 1);
                continue;
            }
            const dragFactor = Math.max(0, 1.0 - SPARK_DRAG * dt);
            sp.vx *= dragFactor;
            sp.vy *= dragFactor;
            sp.vz *= dragFactor;
            sp.vz -= SPARK_GRAVITY * dt;
            sp.x += sp.vx * dt;
            sp.y += sp.vy * dt;
            sp.z += sp.vz * dt;
        }
    }

    function buildSparkGeo(cam) {
        if (_sparks.length === 0) return null;
        const floatsPerVert = 6;
        const out = new Float32Array(_sparks.length * 6 * floatsPerVert);
        let wi = 0;
        for (const sp of _sparks) {
            const lifeT = clamp01(sp.age / sp.lifetime);
            const alpha = (1.0 - lifeT) * (1.0 - lifeT);
            const corners = [
                [-1, -1], [1, -1], [-1, 1],
                [1, -1], [1, 1], [-1, 1]
            ];
            for (const c of corners) {
                out[wi++] = sp.x; out[wi++] = sp.y; out[wi++] = sp.z;
                out[wi++] = c[0]; out[wi++] = c[1]; out[wi++] = alpha;
            }
        }
        return out;
    }

    function renderSparks(gl, cam) {
        if (!CONFIG.enableSparks) return;
        if (_sparks.length === 0 || !_programSpark) return;
        const geo = buildSparkGeo(cam);
        if (!geo) return;

        gl.useProgram(_programSpark);
        gl.bindBuffer(gl.ARRAY_BUFFER, _vboSpark);
        gl.bufferData(gl.ARRAY_BUFFER, geo, gl.DYNAMIC_DRAW);

        const stride = 24;
        gl.enableVertexAttribArray(_locsSpark.pos);
        gl.vertexAttribPointer(_locsSpark.pos, 3, gl.FLOAT, false, stride, 0);
        gl.enableVertexAttribArray(_locsSpark.corner);
        gl.vertexAttribPointer(_locsSpark.corner, 2, gl.FLOAT, false, stride, 12);
        gl.enableVertexAttribArray(_locsSpark.alpha);
        gl.vertexAttribPointer(_locsSpark.alpha, 1, gl.FLOAT, false, stride, 20);

        gl.uniformMatrix4fv(_locsSpark.vp, false, _lastVP);
        gl.uniform3f(_locsSpark.right, cam.rx, cam.ry, cam.rz);
        gl.uniform3f(_locsSpark.up, cam.ux, cam.uy, cam.uz);
        gl.uniform3f(_locsSpark.sparkColor, 0.85, 0.9, 1.0);

        gl.drawArrays(gl.TRIANGLES, 0, geo.length / 6);
    }

    function getSparkCount() {
        return _sparks.length;
    }

    function clearSparks() {
        _sparks = [];
    }

    function spawnCloudToCloudFlash(cam, camPos) {
        const angle = rand() * Math.PI * 2.0;
        const radius = BOLT_RADIUS_MIN + rand() * (BOLT_RADIUS_MAX - BOLT_RADIUS_MIN);
        const baseZ = lerp(CONFIG.cloudBaseZ, CONFIG.cloudTopZ, 0.3 + rand() * 0.4);

        const ox = Math.cos(angle) * radius;
        const oy = Math.sin(angle) * radius;
        const oz = baseZ + randRange(-CC_Z_JITTER, CC_Z_JITTER);

        const horizDist = randRange(CC_HORIZONTAL_MIN, CC_HORIZONTAL_MAX);
        const targetAngle = angle + randRange(-0.6, 0.6);
        const tx = ox + Math.cos(targetAngle) * horizDist;
        const ty = oy + Math.sin(targetAngle) * horizDist;
        const tz = baseZ + randRange(-CC_Z_JITTER, CC_Z_JITTER);

        const segments = [];
        steppedLeader(ox, oy, oz, tx, ty, tz, 0, 1.0, segments);
        const segTs = computeSegmentTValues(segments);
        const colors = pickColorVariantForPreset(CONFIG.colorBias);
        const baseHalfW = (3.0 + rand() * 6.0) * CC_WIDTH_SCALE;

        const numStrokes = 1 + Math.floor(rand() * 2);
        const strokes = [];
        let nextStrike = 0;
        for (let s = 0; s < numStrokes; s++) {
            strokes.push({
                startFrame: nextStrike,
                phase: 'rise',
                phaseFrame: 0,
                brightness: 0.0,
                isDart: s > 0,
                segCache: s > 0 ? dartLeaderPerturb(segments, DART_LEADER_JITTER_SCALE) : segments,
                segTsCache: segTs
            });
            nextStrike += RESTRIKE_DELAY_MIN + Math.floor(rand() * (RESTRIKE_DELAY_MAX - RESTRIKE_DELAY_MIN));
        }

        const midPoint = [(ox + tx) * 0.5, (oy + ty) * 0.5, (oz + tz) * 0.5];
        const distToCam = distanceToCamera(midPoint[0], midPoint[1], midPoint[2], camPos);

        const flash = {
            segments,
            segTs,
            coreColor: colors.core,
            haloColor: colors.halo,
            baseHalfW,
            strokes,
            currentStroke: 0,
            globalFrame: 0,
            dead: false,
            groundPoint: midPoint,
            distToCam,
            isCloudToCloud: true,
            brightnessScale: CC_BRIGHTNESS_SCALE
        };

        _ccFlashes.push(flash);
        emit('strike', { type: 'cloudToCloud', origin: [ox, oy, oz], target: [tx, ty, tz] });
        return flash;
    }

    function spawnFlash(cam, camPos) {
        const [cellOx, cellOy] = getStormCellOrigin();
        const angle = rand() * Math.PI * 2.0;
        const radius = BOLT_RADIUS_MIN + rand() * (BOLT_RADIUS_MAX - BOLT_RADIUS_MIN);
        const ox = cellOx + Math.cos(angle) * radius;
        const oy = cellOy + Math.sin(angle) * radius;
        const oz = BOLT_Z_MIN + rand() * (BOLT_Z_MAX - BOLT_Z_MIN);

        const hDrift = 60.0 + rand() * 160.0;
        const tAngle = angle + (rand() - 0.5) * 0.9;
        const tx = ox + Math.cos(tAngle) * hDrift;
        const ty = oy + Math.sin(tAngle) * hDrift;
        const tz = BOLT_Z_TARGET_MIN + rand() * (BOLT_Z_TARGET_MAX - BOLT_Z_TARGET_MIN);

        const segments = [];
        const baseWidth = 1.0;
        steppedLeader(ox, oy, oz, tx, ty, tz, 0, baseWidth, segments);
        const segTs = computeSegmentTValues(segments);

        const colors = pickColorVariantForPreset(CONFIG.colorBias);
        const baseHalfW = 4.5 + rand() * 9.0;

        const numStrokes = 1 + Math.floor(rand() * MAX_STROKES);
        const strokes = [];
        let nextStrike = 0;
        for (let s = 0; s < numStrokes; s++) {
            const isDart = s > 0 && rand() < DART_LEADER_CHANCE;
            strokes.push({
                startFrame: nextStrike,
                phase: 'rise',
                phaseFrame: 0,
                brightness: 0.0,
                isDart: isDart,
                segCache: isDart ? dartLeaderPerturb(segments, DART_LEADER_JITTER_SCALE) : segments,
                segTsCache: segTs
            });
            nextStrike += RESTRIKE_DELAY_MIN + Math.floor(rand() * (RESTRIKE_DELAY_MAX - RESTRIKE_DELAY_MIN));
        }

        const groundPoint = computeGroundStrikePoint(segments);
        const distToCam = distanceToCamera(groundPoint[0], groundPoint[1], groundPoint[2], camPos);
        const strikeIntensity = clamp01(1.0 - distToCam / 900.0) * 0.6 + 0.4;
        enqueueThunder(distToCam, strikeIntensity);
        spawnScorchDecal(groundPoint, strikeIntensity);
        applyCameraShakeImpulse(strikeIntensity, distToCam);

        const flash = {
            segments,
            segTs,
            coreColor: colors.core,
            haloColor: colors.halo,
            baseHalfW,
            strokes,
            currentStroke: 0,
            globalFrame: 0,
            dead: false,
            groundPoint,
            distToCam,
            sparksSpawned: false
        };

        _flashes.push(flash);
        emit('strike', { type: 'groundStrike', groundPoint, distToCam });
        return flash;
    }

    function updateFlashBrightness(flash) {
        if (flash.dead) return 0.0;

        flash.globalFrame++;
        let anyAlive = false;
        let totalBrightness = 0.0;
        let skyContribution = 0.0;
        let justPeaked = false;

        for (let s = 0; s < flash.strokes.length; s++) {
            const stroke = flash.strokes[s];
            if (stroke.phase === 'done') continue;
            anyAlive = true;

            if (flash.globalFrame < stroke.startFrame) continue;

            stroke.phaseFrame++;

            if (stroke.phase === 'rise') {
                stroke.brightness = easeOutExpo(stroke.phaseFrame / STROKE_RISE_FRAMES);
                if (stroke.phaseFrame >= STROKE_RISE_FRAMES) {
                    stroke.phase = 'hold';
                    stroke.phaseFrame = 0;
                    stroke.brightness = 1.0;
                    justPeaked = true;
                }
            } else if (stroke.phase === 'hold') {
                stroke.brightness = 1.0;
                if (stroke.phaseFrame >= STROKE_HOLD_FRAMES) {
                    stroke.phase = 'decay';
                    stroke.phaseFrame = 0;
                }
            } else if (stroke.phase === 'decay') {
                const t = stroke.phaseFrame / STROKE_DECAY_FRAMES;
                stroke.brightness = ionizationDecayCurve(t);
                if (stroke.phaseFrame >= STROKE_DECAY_FRAMES) {
                    stroke.phase = 'done';
                    stroke.brightness = 0.0;
                }
            }

            totalBrightness = Math.max(totalBrightness, stroke.brightness);
            if (stroke.phase === 'rise' || stroke.phase === 'hold') {
                skyContribution = Math.max(skyContribution, stroke.brightness);
            }
        }

        if (justPeaked && CONFIG.enableSparks && !flash.isCloudToCloud) {
            spawnSparksForFlash(flash, totalBrightness);
        }

        if (!anyAlive && !flash.dead && !flash.isCloudToCloud && shouldBecomeBeadLightning()) {
            spawnBeadFlashFromSegments(flash.segments, flash.coreColor, flash.haloColor, flash.baseHalfW);
        }

        if (!anyAlive) flash.dead = true;
        flash.skyContribution = skyContribution;
        return totalBrightness;
    }

    function updateSkyFlashLevel(dt) {
        let target = 0.0;
        for (const f of _flashes) {
            if (f.skyContribution) {
                const proximityBoost = clamp01(1.0 - (f.distToCam || 500) / 800.0);
                target = Math.max(target, f.skyContribution * (0.35 + proximityBoost * 0.65));
            }
        }
        for (const f of _ccFlashes) {
            if (f.skyContribution) {
                const proximityBoost = clamp01(1.0 - (f.distToCam || 500) / 800.0);
                target = Math.max(target, f.skyContribution * (0.25 + proximityBoost * 0.5) * CC_BRIGHTNESS_SCALE);
            }
        }
        if (target > _skyFlashLevel) {
            _skyFlashLevel = lerp(_skyFlashLevel, target, Math.min(1.0, dt * 40.0));
        } else {
            _skyFlashLevel = lerp(_skyFlashLevel, target, Math.min(1.0, dt * 6.0));
        }
    }

    function buildFlashQuadVerts() {
        return new Float32Array([
            -1, -1,
            1, -1,
            -1, 1,
            1, -1,
            1, 1,
            -1, 1
        ]);
    }

    function init() {
        _gl = SE.gl;
        _programChannel = SE.buildProgram(CHANNEL_VS, CHANNEL_FS);
        _locsChannel = {
            pos: _gl.getAttribLocation(_programChannel, 'aPosition'),
            edgeDist: _gl.getAttribLocation(_programChannel, 'aEdgeDist'),
            alpha: _gl.getAttribLocation(_programChannel, 'aAlpha'),
            segT: _gl.getAttribLocation(_programChannel, 'aSegT'),
            vp: _gl.getUniformLocation(_programChannel, 'uVP'),
            coreColor: _gl.getUniformLocation(_programChannel, 'uCoreColor'),
            haloColor: _gl.getUniformLocation(_programChannel, 'uHaloColor'),
            passType: _gl.getUniformLocation(_programChannel, 'uPassType'),
            flicker: _gl.getUniformLocation(_programChannel, 'uFlicker')
        };
        _vboChannel = _gl.createBuffer();

        _programFlash = SE.buildProgram(FLASH_VS, FLASH_FS);
        _locsFlash = {
            clip: _gl.getAttribLocation(_programFlash, 'aClip'),
            flashColor: _gl.getUniformLocation(_programFlash, 'uFlashColor'),
            flashAlpha: _gl.getUniformLocation(_programFlash, 'uFlashAlpha')
        };
        _vboFlash = _gl.createBuffer();
        _gl.bindBuffer(_gl.ARRAY_BUFFER, _vboFlash);
        _gl.bufferData(_gl.ARRAY_BUFFER, buildFlashQuadVerts(), _gl.STATIC_DRAW);

        _programSpark = SE.buildProgram(SPARK_VS, SPARK_FS);
        _locsSpark = {
            pos: _gl.getAttribLocation(_programSpark, 'aPosition'),
            corner: _gl.getAttribLocation(_programSpark, 'aCorner'),
            alpha: _gl.getAttribLocation(_programSpark, 'aAlpha'),
            vp: _gl.getUniformLocation(_programSpark, 'uVP'),
            right: _gl.getUniformLocation(_programSpark, 'uRight'),
            up: _gl.getUniformLocation(_programSpark, 'uUp'),
            sparkColor: _gl.getUniformLocation(_programSpark, 'uSparkColor')
        };
        _vboSpark = _gl.createBuffer();

        seedRandom(Date.now() >>> 0);
        console.log('[SpectralLightning] Initialized');
    }

    function renderSkyFlashOverlay(gl) {
        if (_skyFlashLevel < 0.01) return;
        gl.depthMask(false);
        gl.disable(gl.DEPTH_TEST);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

        gl.useProgram(_programFlash);
        gl.bindBuffer(gl.ARRAY_BUFFER, _vboFlash);
        gl.enableVertexAttribArray(_locsFlash.clip);
        gl.vertexAttribPointer(_locsFlash.clip, 2, gl.FLOAT, false, 8, 0);
        gl.uniform3f(_locsFlash.flashColor, 0.85, 0.90, 1.0);
        gl.uniform1f(_locsFlash.flashAlpha, _skyFlashLevel * 0.35);
        gl.drawArrays(gl.TRIANGLES, 0, 6);

        gl.enable(gl.DEPTH_TEST);
        gl.depthMask(true);
    }

    function renderFlashList(gl, list, cam, nightFade, brightnessMul) {
        for (const flash of list) {
            let brightness = 0.0;
            let activeSegs = flash.segments;
            let activeTs = flash.segTs;
            for (const s of flash.strokes) {
                if (s.brightness > brightness) {
                    brightness = s.brightness;
                    activeSegs = s.segCache;
                    activeTs = s.segTsCache;
                }
            }
            brightness *= nightFade * (brightnessMul != null ? brightnessMul : 1.0);
            if (brightness < 0.008) continue;

            const flicker = 0.92 + rand() * 0.16;

            gl.uniform3f(_locsChannel.coreColor, flash.coreColor[0], flash.coreColor[1], flash.coreColor[2]);
            gl.uniform3f(_locsChannel.haloColor, flash.haloColor[0], flash.haloColor[1], flash.haloColor[2]);
            gl.uniform1f(_locsChannel.flicker, flicker);

            const coronaGeo = buildChannelGeo(activeSegs, activeTs, flash.baseHalfW * 5.5, brightness * 0.5, cam);
            gl.uniform1f(_locsChannel.passType, 0.0);
            _uploadAndDraw(gl, coronaGeo, 24);

            const innerGeo = buildChannelGeo(activeSegs, activeTs, flash.baseHalfW * 2.2, brightness * 0.82, cam);
            gl.uniform1f(_locsChannel.passType, 1.0);
            _uploadAndDraw(gl, innerGeo, 24);

            const coreGeo = buildChannelGeo(activeSegs, activeTs, flash.baseHalfW * 0.7, brightness * 1.0, cam);
            gl.uniform1f(_locsChannel.passType, 2.0);
            _uploadAndDraw(gl, coreGeo, 24);

            let peakBrightness = 0.0;
            for (const s of flash.strokes) {
                if (s.phase === 'rise' || s.phase === 'hold') {
                    peakBrightness = Math.max(peakBrightness, s.brightness);
                }
            }
            if (peakBrightness > 0.08) {
                const bloomGeo = buildChannelGeo(activeSegs, activeTs, flash.baseHalfW * 12.0, peakBrightness * nightFade * 0.25 * (brightnessMul != null ? brightnessMul : 1.0), cam);
                gl.uniform1f(_locsChannel.passType, 3.0);
                _uploadAndDraw(gl, bloomGeo, 24);
            }

            if (CONFIG.enableGroundGlow && !flash.isCloudToCloud) {
                renderGroundGlow(gl, flash, brightness, cam);
            }
        }
    }

    function render(frame) {
        if (!_programChannel) return;

        const skyBlend = frame.skyBlend ?? 0.0;
        if (skyBlend < 0.3) return;
        const nightFade = Math.min((skyBlend - 0.3) / 0.3, 1.0);

        const gl = _gl;
        const cam = camVecsFromVP(frame.vp);
        const camPos = frame.camPos || cameraPositionFromVP(frame.vp, cam);
        _lastCamPos = camPos;
        _lastVP = frame.vp;
        const dt = frame.dt || 0.016;
        _frameIndex++;

        _spawnTimer += dt;
        const spawnIntervalEffective = effectiveSpawnInterval();
        if (_spawnTimer >= spawnIntervalEffective && _flashes.length < CONFIG.maxFlashes) {
            spawnFlash(cam, camPos);
            _spawnTimer = 0;
            _spawnInterval = 0.4 + rand() * 1.4;
        }

        if (CONFIG.enableCloudToCloud) {
            _ccSpawnTimer += dt;
            if (_ccSpawnTimer >= _ccSpawnInterval && _ccFlashes.length < MAX_CC_FLASHES) {
                spawnCloudToCloudFlash(cam, camPos);
                _ccSpawnTimer = 0;
                _ccSpawnInterval = 2.5 + rand() * 5.0;
            }
        }

        for (let i = _flashes.length - 1; i >= 0; i--) {
            const f = _flashes[i];
            updateFlashBrightness(f);
            if (f.dead) _flashes.splice(i, 1);
        }
        for (let i = _ccFlashes.length - 1; i >= 0; i--) {
            const f = _ccFlashes[i];
            updateFlashBrightness(f);
            if (f.dead) _ccFlashes.splice(i, 1);
        }

        updateSkyFlashLevel(dt);
        updateThunderQueue(dt);
        updateSparks(dt);
        updateStormCells(dt);
        updateScorchDecals(dt);
        updateCameraShake(dt, (frame.elapsedTime != null ? frame.elapsedTime : _frameIndex * dt));

        if (CONFIG.enableAnvilCrawlers) {
            _anvilCrawlerTimer = (_anvilCrawlerTimer || 0) + dt;
            const anvilInterval = 6.0 + rand() * 10.0;
            if (_anvilCrawlerTimer >= anvilInterval && _anvilFlashes.length < 2) {
                spawnAnvilCrawler(cam, camPos);
                _anvilCrawlerTimer = 0;
            }
        }

        if (_flashes.length === 0 && _ccFlashes.length === 0 && _sparks.length === 0
            && _beadFlashes.length === 0 && _anvilFlashes.length === 0 && _scorchDecals.length === 0) {
            renderSkyFlashOverlay(gl);
            return;
        }

        gl.depthMask(false);
        gl.disable(gl.DEPTH_TEST);
        gl.enable(gl.BLEND);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE);

        gl.useProgram(_programChannel);
        gl.uniformMatrix4fv(_locsChannel.vp, false, frame.vp);
        gl.bindBuffer(gl.ARRAY_BUFFER, _vboChannel);

        renderFlashList(gl, _flashes, cam, nightFade, 1.0);
        renderFlashList(gl, _ccFlashes, cam, nightFade, CC_BRIGHTNESS_SCALE);
        renderAnvilFlashes(gl, cam, nightFade);
        renderBeadFlashes(gl);
        renderScorchDecals(gl, cam);
        renderSparks(gl, cam);

        gl.depthMask(true);
        gl.enable(gl.DEPTH_TEST);
        gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);

        renderSkyFlashOverlay(gl);
    }

    function _uploadAndDraw(gl, geo, stride) {
        if (!geo || geo.length === 0) return;
        gl.bufferData(gl.ARRAY_BUFFER, geo, gl.DYNAMIC_DRAW);
        gl.enableVertexAttribArray(_locsChannel.pos);
        gl.vertexAttribPointer(_locsChannel.pos, 3, gl.FLOAT, false, stride, 0);
        gl.enableVertexAttribArray(_locsChannel.edgeDist);
        gl.vertexAttribPointer(_locsChannel.edgeDist, 1, gl.FLOAT, false, stride, 12);
        gl.enableVertexAttribArray(_locsChannel.alpha);
        gl.vertexAttribPointer(_locsChannel.alpha, 1, gl.FLOAT, false, stride, 16);
        gl.enableVertexAttribArray(_locsChannel.segT);
        gl.vertexAttribPointer(_locsChannel.segT, 1, gl.FLOAT, false, stride, 20);
        gl.drawArrays(gl.TRIANGLES, 0, geo.length / 6);
    }

    function setSpawnInterval(minSeconds, maxSeconds) {
        _spawnInterval = minSeconds + rand() * Math.max(0, maxSeconds - minSeconds);
    }

    function setCloudToCloudSpawnInterval(minSeconds, maxSeconds) {
        _ccSpawnInterval = minSeconds + rand() * Math.max(0, maxSeconds - minSeconds);
    }

    function forceStrike() {
        const cam = { rx: 1, ry: 0, rz: 0, ux: 0, uy: 0, uz: 1, fx: 0, fy: 1, fz: 0 };
        return spawnFlash(cam, [0, 0, 0]);
    }

    function forceCloudToCloudStrike() {
        const cam = { rx: 1, ry: 0, rz: 0, ux: 0, uy: 0, uz: 1, fx: 0, fy: 1, fz: 0 };
        return spawnCloudToCloudFlash(cam, [0, 0, 0]);
    }

    function getActiveFlashCount() {
        return _flashes.length;
    }

    function getActiveCloudToCloudCount() {
        return _ccFlashes.length;
    }

    function shouldBecomeBeadLightning() {
        return CONFIG.enableBeadLightning && rand() < 0.06;
    }

    function spawnBeadFlashFromSegments(segments, coreColor, haloColor, baseHalfW) {
        const beadCount = Math.floor(randRange(BEAD_SEGMENT_COUNT_MIN, BEAD_SEGMENT_COUNT_MAX));
        const stride = Math.max(1, Math.floor(segments.length / beadCount));
        const beads = [];
        for (let i = 0; i < segments.length; i += stride) {
            const s = segments[i];
            beads.push({
                x: (s.x0 + s.x1) * 0.5,
                y: (s.y0 + s.y1) * 0.5,
                z: (s.z0 + s.z1) * 0.5,
                phase: 'hold',
                phaseFrame: -Math.floor(rand() * BEAD_GAP_FRAMES * 2),
                brightness: 0.0
            });
        }
        const beadFlash = {
            beads,
            coreColor,
            haloColor,
            baseHalfW: baseHalfW * 0.6,
            globalFrame: 0,
            dead: false
        };
        _beadFlashes.push(beadFlash);
        return beadFlash;
    }

    function updateBeadFlash(bf) {
        bf.globalFrame++;
        let anyAlive = false;
        for (const b of bf.beads) {
            b.phaseFrame++;
            if (b.phase === 'hold') {
                anyAlive = true;
                const t = clamp01(b.phaseFrame / BEAD_HOLD_FRAMES);
                b.brightness = Math.max(0, Math.sin(t * Math.PI));
                if (b.phaseFrame >= BEAD_HOLD_FRAMES) {
                    b.phase = 'gap';
                    b.phaseFrame = 0;
                }
            } else if (b.phase === 'gap') {
                b.brightness = 0.0;
                if (b.phaseFrame >= BEAD_GAP_FRAMES) {
                    b.phase = 'done';
                }
            }
        }
        if (!anyAlive && bf.globalFrame > BEAD_HOLD_FRAMES + BEAD_GAP_FRAMES) {
            bf.dead = true;
        }
    }

    function buildBeadGeo(bf) {
        const alive = bf.beads.filter(b => b.brightness > 0.01);
        if (alive.length === 0) return null;
        const rings = 1;
        const segsPerRing = 8;
        const floatsPerVert = 6;
        const out = new Float32Array(alive.length * segsPerRing * 6 * floatsPerVert);
        let wi = 0;
        for (const b of alive) {
            const radius = BEAD_GLOW_RADIUS * bf.baseHalfW * 0.1 * (0.6 + b.brightness * 0.4);
            for (let i = 0; i < segsPerRing; i++) {
                const a0 = (i / segsPerRing) * Math.PI * 2;
                const a1 = ((i + 1) / segsPerRing) * Math.PI * 2;
                const x0 = b.x + Math.cos(a0) * radius;
                const y0 = b.y + Math.sin(a0) * radius;
                const x1 = b.x + Math.cos(a1) * radius;
                const y1 = b.y + Math.sin(a1) * radius;
                const alpha = b.brightness;
                out[wi++] = b.x; out[wi++] = b.y; out[wi++] = b.z; out[wi++] = 0; out[wi++] = alpha; out[wi++] = 0.5;
                out[wi++] = x0; out[wi++] = y0; out[wi++] = b.z; out[wi++] = 1; out[wi++] = alpha * 0.15; out[wi++] = 0.5;
                out[wi++] = x1; out[wi++] = y1; out[wi++] = b.z; out[wi++] = 1; out[wi++] = alpha * 0.15; out[wi++] = 0.5;
            }
        }
        return out;
    }

    function renderBeadFlashes(gl) {
        for (let i = _beadFlashes.length - 1; i >= 0; i--) {
            const bf = _beadFlashes[i];
            updateBeadFlash(bf);
            if (bf.dead) {
                _beadFlashes.splice(i, 1);
                continue;
            }
            const geo = buildBeadGeo(bf);
            if (!geo) continue;
            gl.uniform3f(_locsChannel.coreColor, bf.coreColor[0], bf.coreColor[1], bf.coreColor[2]);
            gl.uniform3f(_locsChannel.haloColor, bf.haloColor[0], bf.haloColor[1], bf.haloColor[2]);
            gl.uniform1f(_locsChannel.passType, 2.0);
            gl.uniform1f(_locsChannel.flicker, 1.0);
            _uploadAndDraw(gl, geo, 24);
        }
    }

    function getActiveBeadFlashCount() {
        return _beadFlashes.length;
    }

    function spawnAnvilCrawler(cam, camPos) {
        const [cellOx, cellOy] = getStormCellOrigin();
        const startAngle = rand() * Math.PI * 2.0;
        const span = randRange(ANVIL_HORIZONTAL_SPAN_MIN, ANVIL_HORIZONTAL_SPAN_MAX);
        const baseZ = lerp(CONFIG.cloudBaseZ, CONFIG.cloudTopZ, 0.55 + rand() * 0.3);

        const nodeCount = Math.floor(randRange(ANVIL_SEGMENT_COUNT_MIN, ANVIL_SEGMENT_COUNT_MAX));
        const nodes = [];
        let px = cellOx + Math.cos(startAngle) * randRange(100, 400);
        let py = cellOy + Math.sin(startAngle) * randRange(100, 400);
        const dirAngle = startAngle + randRange(-0.3, 0.3);
        const dirX = Math.cos(dirAngle);
        const dirY = Math.sin(dirAngle);
        for (let i = 0; i < nodeCount; i++) {
            const t = i / (nodeCount - 1 || 1);
            const nx = px + dirX * span * t + randRange(-40, 40);
            const ny = py + dirY * span * t + randRange(-40, 40);
            const nz = baseZ + randRange(-30, 30);
            nodes.push([nx, ny, nz]);
        }

        const chainSegments = [];
        for (let i = 0; i < nodes.length - 1; i++) {
            const sub = [];
            steppedLeader(nodes[i][0], nodes[i][1], nodes[i][2], nodes[i + 1][0], nodes[i + 1][1], nodes[i + 1][2], 2, 0.6, sub);
            chainSegments.push(sub);
        }

        const colors = pickColorVariantForPreset(CONFIG.colorBias);
        const crawlDuration = randRange(ANVIL_CRAWL_DURATION_MIN, ANVIL_CRAWL_DURATION_MAX);
        const flash = {
            chainSegments,
            chainTs: chainSegments.map(computeSegmentTValues),
            coreColor: colors.core,
            haloColor: colors.halo,
            baseHalfW: (3.0 + rand() * 4.0) * ANVIL_BRIGHTNESS_SCALE,
            elapsed: 0,
            crawlDuration,
            dead: false
        };
        _anvilFlashes.push(flash);
        emit('strike', { type: 'anvilCrawler', nodeCount });
        return flash;
    }

    function updateAnvilFlash(af, dt) {
        af.elapsed += dt;
        if (af.elapsed >= af.crawlDuration + 0.4) {
            af.dead = true;
        }
    }

    function renderAnvilFlashes(gl, cam, nightFade) {
        for (let i = _anvilFlashes.length - 1; i >= 0; i--) {
            const af = _anvilFlashes[i];
            updateAnvilFlash(af, 0.016);
            if (af.dead) {
                _anvilFlashes.splice(i, 1);
                continue;
            }
            const progress = clamp01(af.elapsed / af.crawlDuration);
            const litChains = Math.max(1, Math.round(progress * af.chainSegments.length));

            gl.uniform3f(_locsChannel.coreColor, af.coreColor[0], af.coreColor[1], af.coreColor[2]);
            gl.uniform3f(_locsChannel.haloColor, af.haloColor[0], af.haloColor[1], af.haloColor[2]);
            gl.uniform1f(_locsChannel.flicker, 1.0);

            for (let c = 0; c < litChains; c++) {
                const segs = af.chainSegments[c];
                const ts = af.chainTs[c];
                if (!segs || segs.length === 0) continue;
                const chainAge = progress * af.chainSegments.length - c;
                const chainBrightness = clamp01(1.2 - chainAge) * nightFade * ANVIL_BRIGHTNESS_SCALE;
                if (chainBrightness < 0.01) continue;

                const geo = buildChannelGeo(segs, ts, af.baseHalfW * 2.0, chainBrightness, cam);
                gl.uniform1f(_locsChannel.passType, 1.0);
                _uploadAndDraw(gl, geo, 24);
            }
        }
    }

    function getActiveAnvilCrawlerCount() {
        return _anvilFlashes.length;
    }

    function spawnScorchDecal(groundPoint, intensity) {
        if (!CONFIG.enableScorchDecals) return;
        if (_scorchDecals.length >= SCORCH_MAX_DECALS) {
            _scorchDecals.shift();
        }
        _scorchDecals.push({
            point: groundPoint.slice(),
            radius: groundImpactRadius(intensity) * SCORCH_RADIUS_SCALE,
            intensity,
            age: 0
        });
    }

    function updateScorchDecals(dt) {
        for (let i = _scorchDecals.length - 1; i >= 0; i--) {
            const d = _scorchDecals[i];
            d.age += dt;
            if (d.age >= SCORCH_LIFETIME) {
                _scorchDecals.splice(i, 1);
            }
        }
    }

    function scorchDecalAlpha(decal) {
        if (decal.age < SCORCH_FADE_START) return decal.intensity * 0.35;
        const fadeT = clamp01((decal.age - SCORCH_FADE_START) / (SCORCH_LIFETIME - SCORCH_FADE_START));
        return decal.intensity * 0.35 * (1.0 - fadeT);
    }

    function renderScorchDecals(gl, cam) {
        if (!CONFIG.enableScorchDecals) return;
        for (const d of _scorchDecals) {
            const alpha = scorchDecalAlpha(d);
            if (alpha < 0.01) continue;
            const geo = buildGroundGlowGeo(d.point, d.radius, alpha, cam);
            gl.uniform3f(_locsChannel.coreColor, 0.35, 0.32, 0.30);
            gl.uniform3f(_locsChannel.haloColor, 0.2, 0.18, 0.16);
            gl.uniform1f(_locsChannel.passType, 0.0);
            gl.uniform1f(_locsChannel.flicker, 1.0);
            _uploadAndDraw(gl, geo, 24);
        }
    }

    function getScorchDecalCount() {
        return _scorchDecals.length;
    }

    function clearScorchDecals() {
        _scorchDecals = [];
    }

    function applyCameraShakeImpulse(intensity, distToCam) {
        if (!CONFIG.enableCameraShake) return;
        const falloff = clamp01(1.0 - distToCam / SHAKE_DISTANCE_FALLOFF);
        if (falloff <= 0) return;
        _camShakeState.energy += intensity * falloff * SHAKE_IMPULSE_PER_INTENSITY;
        _camShakeState.energy = Math.min(_camShakeState.energy, SHAKE_IMPULSE_PER_INTENSITY * 3.0);
    }

    function updateCameraShake(dt, elapsedTime) {
        if (_camShakeState.energy <= 0.0001) {
            _camShakeState.x = 0; _camShakeState.y = 0; _camShakeState.z = 0;
            return;
        }
        _camShakeState.energy = Math.max(0, _camShakeState.energy - SHAKE_DECAY_RATE * dt);
        const amp = Math.min(SHAKE_MAX_OFFSET, _camShakeState.energy);
        _camShakeState.x = Math.sin(elapsedTime * SHAKE_FREQUENCY * 1.0) * amp;
        _camShakeState.y = Math.sin(elapsedTime * SHAKE_FREQUENCY * 1.3 + 1.7) * amp * 0.7;
        _camShakeState.z = Math.sin(elapsedTime * SHAKE_FREQUENCY * 0.8 + 0.6) * amp * 0.5;
    }

    function getCameraShakeOffset() {
        return [_camShakeState.x, _camShakeState.y, _camShakeState.z];
    }

    function resetCameraShake() {
        _camShakeState = { x: 0, y: 0, z: 0, energy: 0 };
    }

    function getSkyFlashLevel() {
        return _skyFlashLevel;
    }

    const CONFIG = {
        maxFlashes: MAX_FLASHES,
        strikeRateScale: 1.0,
        atmosphericDensity: 1.0,
        humidity: 0.5,
        temperatureC: 15.0,
        windSpeed: 3.0,
        windDirection: 0.0,
        stormIntensity: 0.5,
        enableThunder: true,
        enableGroundGlow: true,
        enableDartLeaders: true,
        enableChannelCooling: true,
        enableSparks: true,
        enableCloudToCloud: true,
        enableTurbulence: true,
        enableStormCells: true,
        desiredStormCellCount: 3,
        enableBeadLightning: true,
        enableAnvilCrawlers: true,
        enableScorchDecals: true,
        enableCameraShake: true,
        cloudBaseZ: BOLT_Z_MIN,
        cloudTopZ: BOLT_Z_MAX,
        colorBias: 'auto',
        maxBranchDepth: MAX_DEPTH,
        segmentSubdivisionLOD: true,
        lodNearDistance: 150.0,
        lodFarDistance: 1200.0,
        lodMinSegments: 6,
        lodMaxSegments: 48
    };

    const EVENT_LISTENERS = {
        strike: [],
        thunder: [],
        groundImpact: []
    };

    function on(eventName, callback) {
        if (!EVENT_LISTENERS[eventName]) {
            EVENT_LISTENERS[eventName] = [];
        }
        EVENT_LISTENERS[eventName].push(callback);
        return function unsubscribe() {
            const idx = EVENT_LISTENERS[eventName].indexOf(callback);
            if (idx >= 0) EVENT_LISTENERS[eventName].splice(idx, 1);
        };
    }

    function emit(eventName, payload) {
        const list = EVENT_LISTENERS[eventName];
        if (!list || list.length === 0) return;
        for (let i = 0; i < list.length; i++) {
            try {
                list[i](payload);
            } catch (e) {
                console.error('[SpectralLightning] listener error for', eventName, e);
            }
        }
    }

    function configure(partial) {
        if (!partial || typeof partial !== 'object') return CONFIG;
        for (const key in partial) {
            if (!Object.prototype.hasOwnProperty.call(CONFIG, key)) continue;
            const value = partial[key];
            switch (key) {
                case 'maxFlashes':
                    CONFIG.maxFlashes = Math.max(1, Math.min(64, Math.floor(value)));
                    break;
                case 'strikeRateScale':
                    CONFIG.strikeRateScale = Math.max(0.05, Math.min(20.0, value));
                    break;
                case 'atmosphericDensity':
                    CONFIG.atmosphericDensity = Math.max(0.2, Math.min(3.0, value));
                    break;
                case 'humidity':
                    CONFIG.humidity = clamp01(value);
                    break;
                case 'temperatureC':
                    CONFIG.temperatureC = value;
                    break;
                case 'windSpeed':
                    CONFIG.windSpeed = Math.max(0, value);
                    break;
                case 'windDirection':
                    CONFIG.windDirection = value % (Math.PI * 2);
                    break;
                case 'stormIntensity':
                    CONFIG.stormIntensity = clamp01(value);
                    break;
                case 'cloudBaseZ':
                    CONFIG.cloudBaseZ = value;
                    break;
                case 'cloudTopZ':
                    CONFIG.cloudTopZ = value;
                    break;
                case 'maxBranchDepth':
                    CONFIG.maxBranchDepth = Math.max(1, Math.min(8, Math.floor(value)));
                    break;
                case 'lodNearDistance':
                    CONFIG.lodNearDistance = Math.max(1, value);
                    break;
                case 'lodFarDistance':
                    CONFIG.lodFarDistance = Math.max(CONFIG.lodNearDistance + 1, value);
                    break;
                case 'lodMinSegments':
                    CONFIG.lodMinSegments = Math.max(2, Math.floor(value));
                    break;
                case 'lodMaxSegments':
                    CONFIG.lodMaxSegments = Math.max(CONFIG.lodMinSegments, Math.floor(value));
                    break;
                case 'enableSparks':
                    CONFIG.enableSparks = !!value;
                    break;
                case 'enableCloudToCloud':
                    CONFIG.enableCloudToCloud = !!value;
                    break;
                case 'enableTurbulence':
                    CONFIG.enableTurbulence = !!value;
                    break;
                case 'enableStormCells':
                    CONFIG.enableStormCells = !!value;
                    break;
                case 'desiredStormCellCount':
                    CONFIG.desiredStormCellCount = Math.max(0, Math.min(MAX_STORM_CELLS, Math.floor(value)));
                    break;
                case 'enableBeadLightning':
                    CONFIG.enableBeadLightning = !!value;
                    break;
                case 'enableAnvilCrawlers':
                    CONFIG.enableAnvilCrawlers = !!value;
                    break;
                case 'enableScorchDecals':
                    CONFIG.enableScorchDecals = !!value;
                    break;
                case 'enableCameraShake':
                    CONFIG.enableCameraShake = !!value;
                    break;
                case 'colorBias':
                    CONFIG.colorBias = value;
                    break;
                default:
                    CONFIG[key] = value;
                    break;
            }
        }
        return CONFIG;
    }

    function getConfig() {
        return Object.assign({}, CONFIG);
    }

    function windDisplacement(z, dt) {
        const heightFactor = clamp01((z - CONFIG.cloudBaseZ * 0.3) / (CONFIG.cloudTopZ || 1));
        const magnitude = CONFIG.windSpeed * dt * (0.4 + heightFactor * 0.6);
        return [
            Math.cos(CONFIG.windDirection) * magnitude,
            Math.sin(CONFIG.windDirection) * magnitude
        ];
    }

    function applyWindToSegments(segments, dt) {
        if (CONFIG.windSpeed <= 0) return segments;
        const out = new Array(segments.length);
        for (let i = 0; i < segments.length; i++) {
            const s = segments[i];
            const [wx0, wy0] = windDisplacement(s.z0, dt);
            const [wx1, wy1] = windDisplacement(s.z1, dt);
            out[i] = {
                x0: s.x0 + wx0,
                y0: s.y0 + wy0,
                z0: s.z0,
                x1: s.x1 + wx1,
                y1: s.y1 + wy1,
                z1: s.z1,
                depth: s.depth,
                width: s.width,
                alpha: s.alpha,
                roughness: s.roughness,
                terminal: s.terminal
            };
        }
        return out;
    }

    function atmosphericAttenuation(distance) {
        const density = CONFIG.atmosphericDensity;
        const humidityFactor = 1.0 + CONFIG.humidity * 0.6;
        const scatterCoefficient = 0.0009 * density * humidityFactor;
        return Math.exp(-distance * scatterCoefficient);
    }

    function temperatureColorShift(baseColor, tempC) {
        const coldBias = clamp01((10.0 - tempC) / 40.0);
        return [
            clamp01(baseColor[0] + coldBias * 0.03),
            clamp01(baseColor[1] + coldBias * 0.02),
            clamp01(baseColor[2] + coldBias * 0.06)
        ];
    }

    function lodSegmentCountForDistance(distance) {
        const t = clamp01((distance - CONFIG.lodNearDistance) / (CONFIG.lodFarDistance - CONFIG.lodNearDistance));
        return Math.round(lerp(CONFIG.lodMaxSegments, CONFIG.lodMinSegments, t));
    }

    function decimateSegmentsForLOD(segments, targetCount) {
        if (!CONFIG.segmentSubdivisionLOD) return segments;
        if (segments.length <= targetCount || targetCount <= 0) return segments;
        const stride = segments.length / targetCount;
        const out = [];
        for (let i = 0; i < targetCount; i++) {
            const idx = Math.min(segments.length - 1, Math.floor(i * stride));
            out.push(segments[idx]);
        }
        return out;
    }

    const LEADER_GROWTH_STAGES = 6;

    function buildLeaderGrowthStages(fullSegments) {
        const stages = [];
        const stageCount = LEADER_GROWTH_STAGES;
        const sortedByDepth = fullSegments.slice().sort((a, b) => a.depth - b.depth);
        const totalCount = sortedByDepth.length;
        for (let stage = 1; stage <= stageCount; stage++) {
            const frac = stage / stageCount;
            const visibleCount = Math.max(1, Math.round(totalCount * frac));
            stages.push(sortedByDepth.slice(0, visibleCount));
        }
        return stages;
    }

    function channelCoolingFactor(elapsedFrames, totalFrames) {
        if (!CONFIG.enableChannelCooling) return 1.0;
        const t = clamp01(elapsedFrames / Math.max(1, totalFrames));
        const coolCurve = 1.0 - Math.pow(t, 0.4) * 0.35;
        return Math.max(0.5, coolCurve);
    }

    function groundImpactRadius(strikeIntensity) {
        return 6.0 + strikeIntensity * 22.0;
    }

    function buildGroundGlowGeo(groundPoint, radius, brightness, cam) {
        const rings = 3;
        const segsPerRing = 10;
        const floatsPerVert = 6;
        const out = new Float32Array(rings * segsPerRing * 6 * floatsPerVert);
        let wi = 0;
        for (let ring = 0; ring < rings; ring++) {
            const ringT = (ring + 1) / rings;
            const ringRadius = radius * ringT;
            const ringAlpha = brightness * (1.0 - ringT * 0.6);
            for (let i = 0; i < segsPerRing; i++) {
                const a0 = (i / segsPerRing) * Math.PI * 2;
                const a1 = ((i + 1) / segsPerRing) * Math.PI * 2;
                const x0 = groundPoint[0] + Math.cos(a0) * ringRadius;
                const y0 = groundPoint[1] + Math.sin(a0) * ringRadius;
                const x1 = groundPoint[0] + Math.cos(a1) * ringRadius;
                const y1 = groundPoint[1] + Math.sin(a1) * ringRadius;
                const z = groundPoint[2];
                const cx = groundPoint[0];
                const cy = groundPoint[1];
                out[wi++] = cx; out[wi++] = cy; out[wi++] = z; out[wi++] = 0; out[wi++] = ringAlpha; out[wi++] = 0.5;
                out[wi++] = x0; out[wi++] = y0; out[wi++] = z; out[wi++] = 1; out[wi++] = ringAlpha * 0.2; out[wi++] = 0.5;
                out[wi++] = x1; out[wi++] = y1; out[wi++] = z; out[wi++] = 1; out[wi++] = ringAlpha * 0.2; out[wi++] = 0.5;
                out[wi++] = cx; out[wi++] = cy; out[wi++] = z; out[wi++] = 0; out[wi++] = ringAlpha; out[wi++] = 0.5;
                out[wi++] = x1; out[wi++] = y1; out[wi++] = z; out[wi++] = 1; out[wi++] = ringAlpha * 0.2; out[wi++] = 0.5;
                out[wi++] = cx; out[wi++] = cy; out[wi++] = z + 0.01; out[wi++] = 0; out[wi++] = ringAlpha * 0.9; out[wi++] = 0.5;
            }
        }
        return out;
    }

    function renderGroundGlow(gl, flash, brightness, cam) {
        if (!CONFIG.enableGroundGlow) return;
        if (brightness < 0.05) return;
        const radius = groundImpactRadius(brightness);
        const geo = buildGroundGlowGeo(flash.groundPoint, radius, brightness, cam);
        gl.uniform3f(_locsChannel.coreColor, flash.coreColor[0], flash.coreColor[1], flash.coreColor[2]);
        gl.uniform3f(_locsChannel.haloColor, flash.haloColor[0], flash.haloColor[1], flash.haloColor[2]);
        gl.uniform1f(_locsChannel.passType, 0.0);
        gl.uniform1f(_locsChannel.flicker, 1.0);
        _uploadAndDraw(gl, geo, 24);
        emit('groundImpact', { point: flash.groundPoint.slice(), radius, brightness });
    }

    function stormActivityMultiplier() {
        return 0.3 + CONFIG.stormIntensity * 1.7;
    }

    function effectiveSpawnInterval() {
        return _spawnInterval / (CONFIG.strikeRateScale * stormActivityMultiplier());
    }

    function humidityChannelWidthScale() {
        return 0.85 + CONFIG.humidity * 0.3;
    }

    function computeStrikeStatistics() {
        let totalSegments = 0;
        let totalBranches = 0;
        let maxDepthSeen = 0;
        for (const f of _flashes) {
            totalSegments += f.segments.length;
            for (const s of f.segments) {
                if (s.depth > 0) totalBranches++;
                if (s.depth > maxDepthSeen) maxDepthSeen = s.depth;
            }
        }
        let ccSegments = 0;
        for (const f of _ccFlashes) {
            ccSegments += f.segments.length;
        }
        return {
            activeFlashes: _flashes.length,
            activeCloudToCloudFlashes: _ccFlashes.length,
            totalSegments,
            ccSegments,
            totalBranches,
            maxDepthSeen,
            pendingThunder: _thunderQueue.length,
            skyFlashLevel: _skyFlashLevel,
            activeSparks: _sparks.length
        };
    }

    function debugDumpFlash(index) {
        const f = _flashes[index];
        if (!f) return null;
        return {
            segmentCount: f.segments.length,
            strokeCount: f.strokes.length,
            groundPoint: f.groundPoint.slice(),
            distToCam: f.distToCam,
            coreColor: f.coreColor.slice(),
            haloColor: f.haloColor.slice(),
            globalFrame: f.globalFrame
        };
    }

    function serializeFlashForReplay(flash) {
        return JSON.stringify({
            segments: flash.segments.map(s => ({
                x0: s.x0, y0: s.y0, z0: s.z0,
                x1: s.x1, y1: s.y1, z1: s.z1,
                depth: s.depth, width: s.width, alpha: s.alpha, roughness: s.roughness,
                terminal: !!s.terminal
            })),
            coreColor: flash.coreColor,
            haloColor: flash.haloColor,
            baseHalfW: flash.baseHalfW,
            groundPoint: flash.groundPoint
        });
    }

    function deserializeFlashFromReplay(json) {
        const data = JSON.parse(json);
        const segTs = computeSegmentTValues(data.segments);
        return {
            segments: data.segments,
            segTs,
            coreColor: data.coreColor,
            haloColor: data.haloColor,
            baseHalfW: data.baseHalfW,
            strokes: [{
                startFrame: 0,
                phase: 'rise',
                phaseFrame: 0,
                brightness: 0.0,
                isDart: false,
                segCache: data.segments,
                segTsCache: segTs
            }],
            currentStroke: 0,
            globalFrame: 0,
            dead: false,
            groundPoint: data.groundPoint,
            distToCam: 300
        };
    }

    function replayFlash(json) {
        const flash = deserializeFlashFromReplay(json);
        _flashes.push(flash);
        return flash;
    }

    function pruneOldestFlash() {
        if (_flashes.length === 0) return;
        _flashes.shift();
    }

    function setMaxFlashes(n) {
        CONFIG.maxFlashes = Math.max(1, Math.min(64, Math.floor(n)));
    }

    function computeVisualComplexityScore(flash) {
        let score = flash.segments.length * 1.0;
        score += flash.strokes.length * 4.0;
        for (const s of flash.segments) {
            score += s.depth * 0.5;
        }
        return score;
    }

    function totalSceneComplexity() {
        let total = 0;
        for (const f of _flashes) {
            total += computeVisualComplexityScore(f);
        }
        for (const f of _ccFlashes) {
            total += computeVisualComplexityScore(f) * 0.6;
        }
        total += _sparks.length * 0.3;
        return total;
    }

    function adaptiveQualityThrottle(targetFrameBudget) {
        const complexity = totalSceneComplexity();
        if (complexity > targetFrameBudget) {
            CONFIG.segmentSubdivisionLOD = true;
            CONFIG.lodMaxSegments = Math.max(CONFIG.lodMinSegments, CONFIG.lodMaxSegments - 2);
        } else {
            CONFIG.lodMaxSegments = Math.min(96, CONFIG.lodMaxSegments + 1);
        }
        return CONFIG.lodMaxSegments;
    }

    function reset() {
        _flashes = [];
        _ccFlashes = [];
        _sparks = [];
        _beadFlashes = [];
        _anvilFlashes = [];
        _stormCells = [];
        _scorchDecals = [];
        _spawnTimer = 0;
        _ccSpawnTimer = 0;
        _anvilCrawlerTimer = 0;
        _skyFlashLevel = 0.0;
        _thunderQueue = [];
        _frameIndex = 0;
        resetCameraShake();
        console.log('[SpectralLightning] Reset');
    }

    return {
        init,
        render,
        reset,
        setSpawnInterval,
        setCloudToCloudSpawnInterval,
        forceStrike,
        forceCloudToCloudStrike,
        getActiveFlashCount,
        getActiveCloudToCloudCount,
        getSkyFlashLevel,
        getSparkCount,
        clearSparks,
        configure,
        getConfig,
        applyStormPreset,
        listStormPresets,
        cameraPositionFromVP,
        on,
        computeStrikeStatistics,
        debugDumpFlash,
        serializeFlashForReplay,
        replayFlash,
        pruneOldestFlash,
        setMaxFlashes,
        totalSceneComplexity,
        adaptiveQualityThrottle,
        spawnStormCell,
        getStormCells,
        clearStormCells,
        forceAnvilCrawler: function () {
            const cam = { rx: 1, ry: 0, rz: 0, ux: 0, uy: 0, uz: 1, fx: 0, fy: 1, fz: 0 };
            return spawnAnvilCrawler(cam, [0, 0, 0]);
        },
        getActiveAnvilCrawlerCount,
        getActiveBeadFlashCount,
        getScorchDecalCount,
        clearScorchDecals,
        getCameraShakeOffset,
        resetCameraShake,
        applyCameraShakeImpulse
    };

})();