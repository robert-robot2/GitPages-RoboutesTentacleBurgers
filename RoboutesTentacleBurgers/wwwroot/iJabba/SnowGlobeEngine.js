/**
 * SnowGlobeEngine.js  —  Hyper-realistic Canvas 2D Snow Globe
 * Features:
 *   • Full physics snow simulation  (gravity, drag, turbulence, buoyancy, collision)
 *   • Glass sphere: refraction mask, Fresnel rim, specular highlight, caustic shimmer
 *   • Subsurface scattering on snow accumulation
 *   • Multi-layer parallax depth (6 depth bands)
 *   • Procedural scene generation for Village / Forest / Castle / Arctic
 *   • Dynamic lighting from time-of-day (sky gradient + lamp glow)
 *   • Globe base: mahogany wood grain, gold inlay filigree, text relief
 *   • Water fluid simulation (standing wave on shake, settling ripple)
 *   • Snowflake shapes: plate / dendrite / needle / column (random per flake)
 *   • Ambient dust motes & light shafts
 *   • Mouse-drag rotation / scene parallax
 *   • Shake impulse with acceleration model
 *   • Frost edge crystals around glass rim
 *   • Reflection of scene in base-plate chrome ring
 *   • Bokeh blur for distant particles
 */

window.SnowGlobeEngine = (() => {
    'use strict';

    // ─── Constants ────────────────────────────────────────────────────────────
    const TWO_PI = Math.PI * 2;
    const HALF_PI = Math.PI / 2;
    const RAD = Math.PI / 180;

    // ─── State ────────────────────────────────────────────────────────────────
    let canvas, ctx, dotNet, cfg;
    let W, H, CX, CY, R;           // canvas dims + globe centre + radius
    let animId;
    let lastTime = 0;
    let flakes = [];
    let motes = [];              // ambient dust motes
    let accum = [];              // snow accumulation heights per x-column
    let accumRes = 400;             // resolution of accumulation array

    // Shake / fluid state
    let shakeX = 0, shakeY = 0;
    let shakeVX = 0, shakeVY = 0;
    let shakeDecay = 0.88;
    let fluidWave = [];           // ripple heights for water surface
    let fluidVel = [];
    let fluidDamp = 0.985;
    let shakeEnergy = 0;

    // Mouse / drag
    let mouseDown = false;
    let lastMX = 0, lastMY = 0;
    let parallaxX = 0, parallaxY = 0;  // accumulated parallax offset
    let parallaxTargX = 0, parallaxTargY = 0;

    // Lighting
    let skyTop, skyBot, ambientL, sunAngle, sunColor;
    let lampGlow = [];            // point lights inside scene

    // Scene geometry (rebuilt on scene change)
    let sceneObjs = [];            // { draw(ctx, t, px, py) }

    // Frost crystals on rim
    let frostPoints = [];

    // Offscreen buffers
    let offGlobe, offScene, offBase;  // ImageBitmap caches

    // Timers
    let t = 0;                    // global animation time (seconds)

    // ─── Init ─────────────────────────────────────────────────────────────────
    function init(canvasId, dotNetRef, options) {
        canvas = document.getElementById(canvasId);
        ctx = canvas.getContext('2d', { alpha: true, willReadFrequently: false });
        dotNet = dotNetRef;
        cfg = Object.assign({ snowCount: 250, wind: 0.3, timeOfDay: 20, scene: 'Village' }, options);

        W = canvas.width;
        H = canvas.height;
        CX = W / 2;
        CY = H * 0.46;
        R = W * 0.40;

        setupFluidGrid();
        buildFrost();
        buildScene(cfg.scene);
        buildLighting(cfg.timeOfDay);
        spawnFlakes(cfg.snowCount);
        spawnMotes(80);
        initAccum();

        canvas.addEventListener('mousedown', onMouseDown);
        canvas.addEventListener('mousemove', onMouseMove);
        canvas.addEventListener('mouseup', onMouseUp);
        canvas.addEventListener('mouseleave', onMouseUp);
        canvas.addEventListener('touchstart', onTouchStart, { passive: true });
        canvas.addEventListener('touchmove', onTouchMove, { passive: false });
        canvas.addEventListener('touchend', onMouseUp);

        lastTime = performance.now();
        animId = requestAnimationFrame(loop);
    }

    // ─── Main Loop ────────────────────────────────────────────────────────────
    function loop(now) {
        const dt = Math.min((now - lastTime) / 1000, 0.05);
        lastTime = now;
        t += dt;

        update(dt);
        render();

        animId = requestAnimationFrame(loop);
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    function update(dt) {
        // Shake decay
        shakeX += shakeVX * dt;
        shakeY += shakeVY * dt;
        shakeVX *= shakeDecay;
        shakeVY *= shakeDecay;
        shakeX *= 0.90;
        shakeY *= 0.90;
        shakeEnergy = Math.sqrt(shakeVX * shakeVX + shakeVY * shakeVY) * 0.01;

        // Fluid wave propagation
        updateFluid(dt);

        // Parallax smoothing
        parallaxX += (parallaxTargX - parallaxX) * 0.06;
        parallaxY += (parallaxTargY - parallaxY) * 0.06;

        // Snow physics
        const windNoise = cfg.wind + Math.sin(t * 0.7) * 0.4 + Math.cos(t * 1.3) * 0.2;
        updateFlakes(dt, windNoise);

        // Motes
        updateMotes(dt);
    }

    // ─── Fluid Simulation ─────────────────────────────────────────────────────
    function setupFluidGrid() {
        const n = 180;
        fluidWave = new Float32Array(n);
        fluidVel = new Float32Array(n);
    }

    function updateFluid(dt) {
        const n = fluidWave.length;
        const c = 80; // wave speed
        const dx = 1;
        for (let i = 1; i < n - 1; i++) {
            const acc = c * c * (fluidWave[i - 1] - 2 * fluidWave[i] + fluidWave[i + 1]) / (dx * dx);
            fluidVel[i] += acc * dt;
            fluidVel[i] *= fluidDamp;
        }
        for (let i = 1; i < n - 1; i++) {
            fluidWave[i] += fluidVel[i] * dt;
        }
        // Boundary: free end
        fluidWave[0] = fluidWave[1];
        fluidWave[n - 1] = fluidWave[n - 2];
    }

    function disturbFluid(x, strength) {
        // x in [0,1] -> index
        const i = Math.floor(x * (fluidWave.length - 2)) + 1;
        fluidVel[i] += strength;
        fluidVel[Math.max(1, i - 1)] += strength * 0.5;
        fluidVel[Math.min(fluidWave.length - 2, i + 1)] += strength * 0.5;
    }

    // ─── Frost Rim ────────────────────────────────────────────────────────────
    function buildFrost() {
        frostPoints = [];
        const count = 220;
        for (let i = 0; i < count; i++) {
            const angle = (i / count) * TWO_PI;
            const jitter = rnd(0.008, 0.022);
            frostPoints.push({
                angle,
                r: R + jitter * R,
                arms: Math.floor(rnd(3, 7)),
                armLen: rnd(4, 18),
                alpha: rnd(0.15, 0.55)
            });
        }
    }

    // ─── Scene Builder ────────────────────────────────────────────────────────
    function buildScene(name) {
        sceneObjs = [];
        lampGlow = [];
        switch (name) {
            case 'Village': buildVillage(); break;
            case 'Forest': buildForest(); break;
            case 'Castle': buildCastle(); break;
            case 'Arctic': buildArctic(); break;
        }
    }

    // --- Village ---------------------------------------------------------------
    function buildVillage() {
        // Ground
        sceneObjs.push({ z: 0, draw: drawGround });

        // Background houses (far, small)
        for (let i = 0; i < 5; i++) {
            const hx = lerp(-R * 0.65, R * 0.65, (i + 0.5) / 5) + rnd(-10, 10);
            const hw = rnd(38, 60), hh = rnd(44, 68);
            const hy = groundY(hx) - hh * 0.55;
            const col = hsl(rnd(10, 30), rnd(25, 50), rnd(35, 55));
            const roofCol = hsl(rnd(0, 10), rnd(30, 50), rnd(20, 35));
            sceneObjs.push({
                z: 1,
                draw: (ctx, t, px, py) => drawHouse(ctx, hx + px * 0.3, hy + py * 0.3, hw, hh, col, roofCol, t, false)
            });
        }

        // Lamp posts
        const lampXs = [-R * 0.28, R * 0.15, R * 0.42];
        lampXs.forEach(lx => {
            const ly = groundY(lx);
            lampGlow.push({ x: lx, y: ly - 90, r: 60, color: [255, 220, 120], intensity: 0.6 });
            sceneObjs.push({
                z: 2,
                draw: (ctx, t, px, py) => drawLampPost(ctx, lx + px * 0.55, ly + py * 0.55, t)
            });
        });

        // Foreground houses
        const fgHouses = [
            { x: -R * 0.52, w: 70, h: 90 },
            { x: R * 0.45, w: 65, h: 85 },
        ];
        fgHouses.forEach(h => {
            const hy = groundY(h.x) - h.h * 0.55;
            const col = hsl(rnd(10, 35), rnd(30, 55), rnd(30, 50));
            const roofCol = hsl(rnd(0, 15), rnd(35, 55), rnd(18, 30));
            sceneObjs.push({
                z: 3,
                draw: (ctx, t, px, py) => drawHouse(ctx, h.x + px * 0.75, hy + py * 0.75, h.w, h.h, col, roofCol, t, true)
            });
        });

        // Church steeple
        sceneObjs.push({
            z: 2,
            draw: (ctx, t, px, py) => drawChurch(ctx, R * 0.02 + px * 0.55, groundY(0) + py * 0.55, t)
        });

        // Trees
        for (let i = 0; i < 6; i++) {
            const tx = lerp(-R * 0.9, R * 0.9, i / 5) + rnd(-15, 15);
            const ty = groundY(tx);
            const tz = rnd(0.5, 3);
            sceneObjs.push({
                z: tz,
                draw: (ctx, t, px, py) => drawPineTree(ctx, tx + px * tz * 0.25, ty + py * tz * 0.25, rnd(25, 50), tz, t)
            });
        }

        // Snowman
        sceneObjs.push({
            z: 2.5,
            draw: (ctx, t, px, py) => drawSnowman(ctx, -R * 0.1 + px * 0.6, groundY(-R * 0.1) + py * 0.6, t)
        });
    }

    // --- Forest ----------------------------------------------------------------
    function buildForest() {
        sceneObjs.push({ z: 0, draw: drawGround });
        // Lots of pine trees in depth layers
        for (let i = 0; i < 22; i++) {
            const tx = lerp(-R * 0.95, R * 0.95, i / 21) + rnd(-20, 20);
            const ty = groundY(tx) + rnd(-5, 5);
            const tz = rnd(0.3, 3.5);
            const sz = rnd(30, 90);
            sceneObjs.push({
                z: tz,
                draw: (ctx, t, px, py) => drawPineTree(ctx, tx + px * tz * 0.25, ty + py * tz * 0.25, sz, tz, t)
            });
        }
        // Deer
        sceneObjs.push({
            z: 2,
            draw: (ctx, t, px, py) => drawDeer(ctx, R * 0.22 + px * 0.6, groundY(R * 0.22) + py * 0.6, t)
        });
        // Cabin
        sceneObjs.push({
            z: 1.5,
            draw: (ctx, t, px, py) => drawCabin(ctx, -R * 0.3 + px * 0.45, groundY(-R * 0.3) + py * 0.45, t)
        });
        lampGlow.push({ x: -R * 0.3, y: groundY(-R * 0.3) - 40, r: 50, color: [255, 200, 100], intensity: 0.7 });
        // Moon/lantern hanging
        sceneObjs.push({
            z: 0.5,
            draw: (ctx, t, px, py) => drawHangingLantern(ctx, R * 0.05 + px * 0.15, -R * 0.2 + py * 0.15, t)
        });
        lampGlow.push({ x: R * 0.05, y: -R * 0.2, r: 45, color: [255, 180, 80], intensity: 0.5 });
    }

    // --- Castle ----------------------------------------------------------------
    function buildCastle() {
        sceneObjs.push({ z: 0, draw: drawGround });
        sceneObjs.push({
            z: 0.5,
            draw: (ctx, t, px, py) => drawMountains(ctx, t, px * 0.2, py * 0.2)
        });
        sceneObjs.push({
            z: 1,
            draw: (ctx, t, px, py) => drawCastle(ctx, t, px * 0.6, py * 0.6)
        });
        for (let i = 0; i < 8; i++) {
            const tx = lerp(-R * 0.9, R * 0.9, i / 7) + rnd(-15, 15);
            const ty = groundY(tx);
            const tz = rnd(0.4, 2);
            sceneObjs.push({
                z: tz,
                draw: (ctx, t, px, py) => drawPineTree(ctx, tx + px * tz * 0.25, ty + py * tz * 0.25, rnd(20, 55), tz, t)
            });
        }
        // Torches on castle walls
        [[-R * 0.18, -R * 0.08], [R * 0.18, -R * 0.08]].forEach(([lx, ly]) => {
            lampGlow.push({ x: lx, y: ly, r: 50, color: [255, 140, 40], intensity: 0.8 });
            sceneObjs.push({
                z: 1.5,
                draw: (ctx, t, px, py) => drawTorch(ctx, lx + px * 0.6, ly + py * 0.6, t)
            });
        });
        sceneObjs.push({
            z: 2.5,
            draw: (ctx, t, px, py) => drawSnowman(ctx, R * 0.3 + px * 0.65, groundY(R * 0.3) + py * 0.65, t)
        });
    }

    // --- Arctic ----------------------------------------------------------------
    function buildArctic() {
        sceneObjs.push({ z: 0, draw: drawArcticGround });
        sceneObjs.push({
            z: 0.5,
            draw: (ctx, t, px, py) => drawAurora(ctx, t, px * 0.2, py * 0.2)
        });
        for (let i = 0; i < 5; i++) {
            const ix = lerp(-R * 0.8, R * 0.8, i / 4) + rnd(-20, 20);
            const iy = groundY(ix) + rnd(-10, 5);
            sceneObjs.push({
                z: rnd(0.8, 2.5),
                draw: (ctx, t, px, py) => drawIceberg(ctx, ix + px * 0.5, iy + py * 0.5, rnd(30, 70), t)
            });
        }
        // Polar bear
        sceneObjs.push({
            z: 2,
            draw: (ctx, t, px, py) => drawPolarBear(ctx, -R * 0.15 + px * 0.6, groundY(-R * 0.15) + py * 0.6, t)
        });
        // Igloo
        sceneObjs.push({
            z: 1.5,
            draw: (ctx, t, px, py) => drawIgloo(ctx, R * 0.32 + px * 0.55, groundY(R * 0.32) + py * 0.55, t)
        });
        lampGlow.push({ x: R * 0.32, y: groundY(R * 0.32) - 10, r: 35, color: [180, 220, 255], intensity: 0.4 });
    }

    // ─── Drawing Primitives ───────────────────────────────────────────────────

    function drawGround(ctx, t, px, py) {
        // Snowy ground with blue-tinted shadows
        const gx = CX + px;
        const gy = CY + py;
        const groundTop = gy + R * 0.38;
        const groundBot = gy + R + 60;

        // Multi-layer ground
        const g1 = ctx.createLinearGradient(0, groundTop, 0, groundBot);
        g1.addColorStop(0, 'rgba(220,235,255,0.95)');
        g1.addColorStop(0.3, 'rgba(190,215,245,0.95)');
        g1.addColorStop(1, 'rgba(140,180,230,0.9)');
        ctx.fillStyle = g1;
        ctx.beginPath();
        ctx.ellipse(gx, groundTop + 15, R * 0.90, R * 0.22, 0, 0, TWO_PI);
        ctx.fill();

        // Subsurface scatter — blue glow under snow
        const g2 = ctx.createRadialGradient(gx, groundTop, 0, gx, groundTop, R * 0.8);
        g2.addColorStop(0, 'rgba(120,180,240,0.25)');
        g2.addColorStop(1, 'rgba(120,180,240,0)');
        ctx.fillStyle = g2;
        ctx.beginPath();
        ctx.ellipse(gx, groundTop + 15, R * 0.90, R * 0.22, 0, 0, TWO_PI);
        ctx.fill();

        // Snow surface bumps
        ctx.save();
        ctx.strokeStyle = 'rgba(255,255,255,0.6)';
        ctx.lineWidth = 2;
        for (let i = 0; i < 8; i++) {
            const bx = gx - R * 0.75 + (i / 7) * R * 1.5;
            const by = groundTop + Math.sin(bx * 0.05 + t * 0.3) * 4 + rndSeeded(i, 12345) * 8;
            ctx.beginPath();
            ctx.arc(bx, by, rndSeeded(i + 1, 54321) * 15 + 5, Math.PI, TWO_PI);
            ctx.stroke();
        }
        ctx.restore();
    }

    function drawArcticGround(ctx, t, px, py) {
        drawGround(ctx, t, px, py);
        // Ice cracks
        const gx = CX + px, gy = CY + py;
        const groundTop = gy + R * 0.38;
        ctx.save();
        ctx.strokeStyle = 'rgba(140,200,255,0.4)';
        ctx.lineWidth = 1;
        for (let i = 0; i < 12; i++) {
            const sx = gx + rndSeeded(i, 111) * (R * 1.6) - R * 0.8;
            const sy = groundTop + rndSeeded(i, 222) * 20;
            ctx.beginPath();
            ctx.moveTo(sx, sy);
            for (let j = 0; j < 4; j++) {
                ctx.lineTo(sx + rndSeeded(i * 10 + j, 333) * 40 - 20,
                    sy + rndSeeded(i * 10 + j, 444) * 15);
            }
            ctx.stroke();
        }
        ctx.restore();
    }

    function drawMountains(ctx, t, px, py) {
        const gx = CX + px, gy = CY + py;
        ctx.save();
        ctx.globalAlpha = 0.7;
        // Far mountains
        ctx.fillStyle = hsl(210, 20, 30);
        ctx.beginPath();
        ctx.moveTo(gx - R, gy + R * 0.35);
        peakAt(ctx, gx - R * 0.5, gy - R * 0.1, 80);
        peakAt(ctx, gx - R * 0.2, gy - R * 0.18, 60);
        peakAt(ctx, gx + R * 0.2, gy - R * 0.14, 70);
        peakAt(ctx, gx + R * 0.6, gy - R * 0.08, 55);
        ctx.lineTo(gx + R, gy + R * 0.35);
        ctx.closePath();
        ctx.fill();

        // Snow caps
        ctx.fillStyle = 'rgba(220,235,255,0.85)';
        drawSnowCap(ctx, gx - R * 0.5, gy - R * 0.1, 80);
        drawSnowCap(ctx, gx - R * 0.2, gy - R * 0.18, 60);
        drawSnowCap(ctx, gx + R * 0.2, gy - R * 0.14, 70);
        drawSnowCap(ctx, gx + R * 0.6, gy - R * 0.08, 55);
        ctx.restore();
    }

    function peakAt(ctx, x, y, h) {
        ctx.lineTo(x - h * 0.7, y + h); ctx.lineTo(x, y); ctx.lineTo(x + h * 0.7, y + h);
    }

    function drawSnowCap(ctx, x, y, h) {
        ctx.beginPath();
        ctx.moveTo(x, y);
        ctx.lineTo(x - h * 0.3, y + h * 0.35);
        ctx.lineTo(x + h * 0.3, y + h * 0.35);
        ctx.closePath();
        ctx.fill();
    }

    function drawHouse(ctx, x, y, w, h, wallCol, roofCol, t, hasLight) {
        const gx = CX + x, gy = CY + y;
        const bx = gx - w / 2, by = gy;

        // Shadow
        ctx.save();
        ctx.fillStyle = 'rgba(0,20,60,0.18)';
        ctx.beginPath();
        ctx.ellipse(gx, gy + 4, w * 0.55, 6, 0, 0, TWO_PI);
        ctx.fill();

        // Wall
        ctx.fillStyle = wallCol;
        ctx.fillRect(bx, by - h, w, h);

        // Brick texture
        ctx.strokeStyle = 'rgba(0,0,0,0.08)';
        ctx.lineWidth = 0.5;
        for (let row = 0; row < 6; row++) {
            const ry = by - h + row * (h / 6);
            const offset = (row % 2) * (w / 8);
            for (let col = 0; col < 5; col++) {
                ctx.strokeRect(bx + offset + col * (w / 4), ry, w / 4, h / 6);
            }
        }

        // Roof
        ctx.fillStyle = roofCol;
        ctx.beginPath();
        ctx.moveTo(bx - 6, by - h);
        ctx.lineTo(gx, by - h - h * 0.55);
        ctx.lineTo(gx + w / 2 + 6, by - h);
        ctx.closePath();
        ctx.fill();

        // Snow on roof
        ctx.fillStyle = 'rgba(230,240,255,0.9)';
        ctx.beginPath();
        ctx.moveTo(bx - 6, by - h);
        ctx.lineTo(gx, by - h - h * 0.55);
        ctx.lineTo(gx + w / 2 + 6, by - h);
        ctx.lineTo(gx + w / 2 + 6, by - h + 8);
        snowyEdge(ctx, gx + w / 2 + 6, bx - 6, by - h + 8, 5);
        ctx.closePath();
        ctx.fill();

        // Windows
        const winW = w * 0.22, winH = h * 0.22;
        [[-w * 0.25, -h * 0.35], [w * 0.25, -h * 0.35]].forEach(([ox, oy]) => {
            const wx = gx + ox - winW / 2, wy = gy + oy - winH / 2;
            // Window glow
            if (hasLight) {
                const wg = ctx.createRadialGradient(wx + winW / 2, wy + winH / 2, 0, wx + winW / 2, wy + winH / 2, winW * 1.5);
                wg.addColorStop(0, 'rgba(255,220,100,0.6)');
                wg.addColorStop(1, 'rgba(255,220,100,0)');
                ctx.fillStyle = wg;
                ctx.fillRect(wx - winW, wy - winH, winW * 3, winH * 3);
            }
            // Window pane
            ctx.fillStyle = hasLight ? `rgba(255,210,100,${0.7 + 0.2 * Math.sin(t * 2)})` : 'rgba(160,195,240,0.5)';
            ctx.fillRect(wx, wy, winW, winH);
            // Pane divider
            ctx.strokeStyle = 'rgba(0,0,0,0.3)';
            ctx.lineWidth = 1;
            ctx.beginPath();
            ctx.moveTo(wx + winW / 2, wy); ctx.lineTo(wx + winW / 2, wy + winH);
            ctx.moveTo(wx, wy + winH / 2); ctx.lineTo(wx + winW, wy + winH / 2);
            ctx.stroke();
        });

        // Door
        ctx.fillStyle = hsl(25, 60, 25);
        const dw = w * 0.25, dh = h * 0.32;
        ctx.beginPath();
        ctx.roundRect(gx - dw / 2, gy - dh, dw, dh, [3, 3, 0, 0]);
        ctx.fill();
        // Door knob
        ctx.fillStyle = '#c8a96e';
        ctx.beginPath();
        ctx.arc(gx + dw * 0.2, gy - dh * 0.4, 2, 0, TWO_PI);
        ctx.fill();

        // Chimney
        const chx = gx + w * 0.2;
        ctx.fillStyle = hsl(5, 40, 30);
        ctx.fillRect(chx, by - h - h * 0.52, 10, h * 0.38);
        // Smoke puffs
        if (hasLight) {
            for (let s = 0; s < 4; s++) {
                const st = (t * 0.4 + s * 0.3) % 1.0;
                const sa = Math.max(0, 0.5 - st * 0.8);
                ctx.save();
                ctx.globalAlpha = sa;
                ctx.fillStyle = 'rgba(180,180,200,1)';
                ctx.beginPath();
                ctx.arc(chx + 5 + Math.sin(st * 4) * 8, by - h - h * 0.52 - st * 40, 5 + st * 10, 0, TWO_PI);
                ctx.fill();
                ctx.restore();
            }
        }

        ctx.restore();
    }

    function drawChurch(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        const w = 80, h = 100;
        ctx.save();

        // Body
        ctx.fillStyle = hsl(25, 15, 80);
        ctx.fillRect(gx - w / 2, gy - h, w, h);

        // Steeple
        ctx.fillStyle = hsl(210, 15, 55);
        ctx.beginPath();
        ctx.moveTo(gx - 18, gy - h);
        ctx.lineTo(gx, gy - h - 75);
        ctx.lineTo(gx + 18, gy - h);
        ctx.closePath();
        ctx.fill();

        // Cross
        ctx.strokeStyle = '#c8a96e';
        ctx.lineWidth = 3;
        ctx.lineCap = 'round';
        ctx.beginPath();
        ctx.moveTo(gx, gy - h - 75);
        ctx.lineTo(gx, gy - h - 55);
        ctx.moveTo(gx - 8, gy - h - 68);
        ctx.lineTo(gx + 8, gy - h - 68);
        ctx.stroke();

        // Bell window
        ctx.fillStyle = 'rgba(255,200,100,0.6)';
        ctx.beginPath();
        ctx.arc(gx, gy - h - 15, 12, Math.PI, TWO_PI);
        ctx.rect(gx - 12, gy - h - 15, 24, 12);
        ctx.fill();

        // Snow on roof
        ctx.fillStyle = 'rgba(230,240,255,0.9)';
        ctx.beginPath();
        ctx.moveTo(gx - w / 2 - 5, gy - h);
        ctx.lineTo(gx, gy - h - 75);
        ctx.lineTo(gx + w / 2 + 5, gy - h);
        ctx.closePath();
        ctx.fill();

        ctx.restore();
    }

    function drawLampPost(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        ctx.save();
        // Post
        ctx.strokeStyle = '#2a2a3a';
        ctx.lineWidth = 4;
        ctx.beginPath();
        ctx.moveTo(gx, gy);
        ctx.lineTo(gx, gy - 90);
        ctx.lineTo(gx + 14, gy - 96);
        ctx.stroke();
        // Lamp housing
        ctx.fillStyle = '#1a1a2a';
        ctx.beginPath();
        ctx.rect(gx + 6, gy - 110, 16, 14);
        ctx.fill();
        // Light glow
        const flicker = 0.85 + 0.15 * Math.sin(t * 7 + x);
        const lg = ctx.createRadialGradient(gx + 14, gy - 103, 0, gx + 14, gy - 103, 55);
        lg.addColorStop(0, `rgba(255,220,120,${0.9 * flicker})`);
        lg.addColorStop(0.3, `rgba(255,200,80,${0.4 * flicker})`);
        lg.addColorStop(1, 'rgba(255,200,80,0)');
        ctx.fillStyle = lg;
        ctx.beginPath();
        ctx.arc(gx + 14, gy - 103, 55, 0, TWO_PI);
        ctx.fill();
        // Bulb
        ctx.fillStyle = `rgba(255,240,180,${flicker})`;
        ctx.beginPath();
        ctx.arc(gx + 14, gy - 103, 5, 0, TWO_PI);
        ctx.fill();
        ctx.restore();
    }

    function drawPineTree(ctx, x, y, size, depth, t) {
        const gx = CX + x, gy = CY + y;
        const layers = 4;
        ctx.save();
        ctx.globalAlpha = Math.min(1, 0.5 + depth * 0.15);

        // Trunk
        const trunkH = size * 0.35;
        const trunkG = ctx.createLinearGradient(gx, gy, gx + 8, gy);
        trunkG.addColorStop(0, hsl(25, 50, 22));
        trunkG.addColorStop(1, hsl(25, 50, 15));
        ctx.fillStyle = trunkG;
        ctx.fillRect(gx - 4, gy - trunkH, 8, trunkH);

        // Sway
        const sway = Math.sin(t * 0.8 + x * 0.1) * size * 0.015;

        for (let l = 0; l < layers; l++) {
            const frac = l / layers;
            const layerW = size * (1 - frac * 0.55);
            const layerH = size * 0.38;
            const layerY = gy - trunkH - l * layerH * 0.62;

            // Green gradient
            const tg = ctx.createLinearGradient(gx - layerW, layerY, gx, layerY - layerH);
            tg.addColorStop(0, hsl(130, 50, 12 + l * 3));
            tg.addColorStop(1, hsl(145, 55, 20 + l * 4));
            ctx.fillStyle = tg;

            ctx.save();
            ctx.translate(sway * (layers - l) / layers, 0);
            ctx.beginPath();
            ctx.moveTo(gx - layerW / 2 - 5, layerY);
            ctx.lineTo(gx + sway * 0.5, layerY - layerH);
            ctx.lineTo(gx + layerW / 2 + 5, layerY);
            ctx.closePath();
            ctx.fill();

            // Snow on layer
            ctx.fillStyle = `rgba(215,235,255,${0.7 - frac * 0.2})`;
            ctx.beginPath();
            ctx.moveTo(gx - layerW * 0.35, layerY);
            ctx.lineTo(gx + sway * 0.5, layerY - layerH);
            ctx.lineTo(gx + layerW * 0.35, layerY);
            snowyEdge(ctx, gx + layerW * 0.35, gx - layerW * 0.35, layerY, 3);
            ctx.closePath();
            ctx.fill();
            ctx.restore();
        }

        ctx.restore();
    }

    function drawSnowman(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        ctx.save();
        const bob = Math.sin(t * 1.2) * 0.5;

        // Shadow
        ctx.fillStyle = 'rgba(0,30,80,0.15)';
        ctx.beginPath();
        ctx.ellipse(gx, gy + 2, 22, 5, 0, 0, TWO_PI);
        ctx.fill();

        // Bottom ball
        const b1 = drawSnowBall(ctx, gx, gy - 16, 18);
        // Middle ball
        drawSnowBall(ctx, gx, gy - 48 + bob, 14);
        // Head
        drawSnowBall(ctx, gx, gy - 76 + bob, 11);

        // Eyes
        ctx.fillStyle = '#1a1a1a';
        [-4, 4].forEach(ox => {
            ctx.beginPath();
            ctx.arc(gx + ox, gy - 80 + bob, 1.8, 0, TWO_PI);
            ctx.fill();
        });

        // Carrot nose
        ctx.fillStyle = '#e05010';
        ctx.beginPath();
        ctx.moveTo(gx, gy - 76 + bob);
        ctx.lineTo(gx + 9, gy - 76.5 + bob);
        ctx.lineTo(gx, gy - 75 + bob);
        ctx.fill();

        // Buttons
        ctx.fillStyle = '#1a1a1a';
        [0, -8, -16].forEach(oy => {
            ctx.beginPath();
            ctx.arc(gx, gy - 48 + oy + bob, 1.5, 0, TWO_PI);
            ctx.fill();
        });

        // Hat
        ctx.fillStyle = '#0a0a0a';
        ctx.fillRect(gx - 12, gy - 96 + bob, 24, 5);
        ctx.fillRect(gx - 9, gy - 115 + bob, 18, 20);
        // Hat band
        ctx.fillStyle = '#c00020';
        ctx.fillRect(gx - 9, gy - 98 + bob, 18, 3);

        // Stick arms
        ctx.strokeStyle = hsl(25, 50, 20);
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';
        ctx.beginPath();
        ctx.moveTo(gx - 14, gy - 50 + bob);
        ctx.lineTo(gx - 30, gy - 62 + bob);
        ctx.lineTo(gx - 38, gy - 58 + bob);
        ctx.moveTo(gx - 30, gy - 62 + bob);
        ctx.lineTo(gx - 34, gy - 55 + bob);
        ctx.moveTo(gx + 14, gy - 50 + bob);
        ctx.lineTo(gx + 30, gy - 62 + bob);
        ctx.lineTo(gx + 38, gy - 58 + bob);
        ctx.moveTo(gx + 30, gy - 62 + bob);
        ctx.lineTo(gx + 34, gy - 55 + bob);
        ctx.stroke();

        ctx.restore();
    }

    function drawSnowBall(ctx, x, y, r) {
        const sg = ctx.createRadialGradient(x - r * 0.3, y - r * 0.3, r * 0.1, x, y, r);
        sg.addColorStop(0, 'rgba(255,255,255,0.98)');
        sg.addColorStop(0.6, 'rgba(220,235,255,0.95)');
        sg.addColorStop(1, 'rgba(180,210,245,0.9)');
        ctx.fillStyle = sg;
        ctx.beginPath();
        ctx.arc(x, y, r, 0, TWO_PI);
        ctx.fill();
        // Specular
        ctx.fillStyle = 'rgba(255,255,255,0.6)';
        ctx.beginPath();
        ctx.arc(x - r * 0.28, y - r * 0.28, r * 0.2, 0, TWO_PI);
        ctx.fill();
    }

    function drawCabin(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        const w = 70, h = 55;
        ctx.save();

        // Logs
        const logG = ctx.createLinearGradient(gx - w / 2, 0, gx + w / 2, 0);
        logG.addColorStop(0, hsl(25, 50, 28));
        logG.addColorStop(1, hsl(20, 45, 22));
        ctx.fillStyle = logG;
        ctx.fillRect(gx - w / 2, gy - h, w, h);

        // Log lines
        ctx.strokeStyle = 'rgba(0,0,0,0.2)';
        ctx.lineWidth = 1;
        for (let i = 1; i < 6; i++) {
            ctx.beginPath();
            ctx.moveTo(gx - w / 2, gy - h + i * (h / 6));
            ctx.lineTo(gx + w / 2, gy - h + i * (h / 6));
            ctx.stroke();
        }

        // Roof
        ctx.fillStyle = hsl(20, 35, 18);
        ctx.beginPath();
        ctx.moveTo(gx - w / 2 - 8, gy - h);
        ctx.lineTo(gx, gy - h - 45);
        ctx.lineTo(gx + w / 2 + 8, gy - h);
        ctx.fill();

        // Snow roof
        ctx.fillStyle = 'rgba(225,238,255,0.9)';
        ctx.beginPath();
        ctx.moveTo(gx - w / 2 - 8, gy - h);
        ctx.lineTo(gx, gy - h - 45);
        ctx.lineTo(gx + w / 2 + 8, gy - h);
        ctx.lineTo(gx + w / 2 + 8, gy - h + 10);
        snowyEdge(ctx, gx + w / 2 + 8, gx - w / 2 - 8, gy - h + 10, 4);
        ctx.closePath();
        ctx.fill();

        // Glowing window
        const wg = ctx.createRadialGradient(gx, gy - h / 2, 0, gx, gy - h / 2, 35);
        wg.addColorStop(0, 'rgba(255,200,80,0.5)');
        wg.addColorStop(1, 'rgba(255,200,80,0)');
        ctx.fillStyle = wg;
        ctx.fillRect(gx - 35, gy - h / 2 - 35, 70, 70);

        ctx.fillStyle = `rgba(255,200,80,${0.6 + 0.2 * Math.sin(t * 1.7)})`;
        ctx.beginPath();
        ctx.rect(gx - 12, gy - h / 2 - 10, 24, 18);
        ctx.fill();

        ctx.restore();
    }

    function drawDeer(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        const walk = Math.sin(t * 2.5) * 4;
        ctx.save();
        ctx.fillStyle = hsl(25, 55, 38);

        // Body
        ctx.beginPath();
        ctx.ellipse(gx, gy - 20, 20, 12, -0.1, 0, TWO_PI);
        ctx.fill();

        // Neck
        ctx.beginPath();
        ctx.moveTo(gx + 12, gy - 28);
        ctx.lineTo(gx + 20, gy - 44);
        ctx.lineTo(gx + 15, gy - 20);
        ctx.fill();

        // Head
        ctx.beginPath();
        ctx.ellipse(gx + 22, gy - 46, 10, 7, 0.3, 0, TWO_PI);
        ctx.fill();

        // Snout
        ctx.fillStyle = hsl(25, 45, 50);
        ctx.beginPath();
        ctx.ellipse(gx + 30, gy - 44, 5, 4, 0.1, 0, TWO_PI);
        ctx.fill();

        // Eye
        ctx.fillStyle = '#1a1a1a';
        ctx.beginPath();
        ctx.arc(gx + 24, gy - 49, 2, 0, TWO_PI);
        ctx.fill();

        // Antlers
        ctx.strokeStyle = hsl(25, 50, 28);
        ctx.lineWidth = 2;
        ctx.lineCap = 'round';
        ctx.beginPath();
        ctx.moveTo(gx + 20, gy - 52);
        ctx.lineTo(gx + 18, gy - 65);
        ctx.lineTo(gx + 12, gy - 72);
        ctx.moveTo(gx + 18, gy - 65);
        ctx.lineTo(gx + 24, gy - 70);
        ctx.moveTo(gx + 21, gy - 52);
        ctx.lineTo(gx + 24, gy - 65);
        ctx.lineTo(gx + 28, gy - 70);
        ctx.moveTo(gx + 24, gy - 65);
        ctx.lineTo(gx + 20, gy - 68);
        ctx.stroke();

        // Legs
        ctx.fillStyle = hsl(25, 55, 32);
        const legPairs = [[-12, -5], [-5, -5], [5, -2], [12, -2]];
        legPairs.forEach(([ox, oz], i) => {
            const legWalk = (i % 2 === 0) ? walk : -walk;
            ctx.beginPath();
            ctx.moveTo(gx + ox, gy - 8);
            ctx.lineTo(gx + ox - 2 + legWalk, gy + 4);
            ctx.lineTo(gx + ox + 5, gy);
            ctx.fill();
        });

        // White tail
        ctx.fillStyle = 'rgba(245,240,235,0.9)';
        ctx.beginPath();
        ctx.ellipse(gx - 18, gy - 20, 6, 4, 0.2, 0, TWO_PI);
        ctx.fill();

        ctx.restore();
    }

    function drawCastle(ctx, t, px, py) {
        const gx = CX + px, gy = CY + py;
        ctx.save();

        // Main keep
        const keepW = 110, keepH = 140;
        const wallG = ctx.createLinearGradient(gx - keepW / 2, 0, gx + keepW / 2, 0);
        wallG.addColorStop(0, hsl(210, 8, 45));
        wallG.addColorStop(0.5, hsl(210, 6, 55));
        wallG.addColorStop(1, hsl(210, 8, 38));
        ctx.fillStyle = wallG;
        ctx.fillRect(gx - keepW / 2, gy - keepH, keepW, keepH);

        // Stone texture
        ctx.strokeStyle = 'rgba(0,0,0,0.12)';
        ctx.lineWidth = 0.8;
        for (let row = 0; row < 10; row++) {
            const ry = gy - keepH + row * (keepH / 10);
            const off = (row % 2) * (keepW / 8);
            for (let col = 0; col < 6; col++) {
                ctx.strokeRect(gx - keepW / 2 + off + col * (keepW / 5), ry, keepW / 5, keepH / 10);
            }
        }

        // Battlements
        ctx.fillStyle = hsl(210, 8, 50);
        for (let m = 0; m < 7; m++) {
            if (m % 2 === 0) {
                ctx.fillRect(gx - keepW / 2 + m * (keepW / 6), gy - keepH - 14, keepW / 6, 14);
            }
        }

        // Towers
        [-keepW / 2 - 20, keepW / 2 - 20].forEach(ox => {
            const tw = 44, th = 100;
            const tg = ctx.createLinearGradient(gx + ox, 0, gx + ox + tw, 0);
            tg.addColorStop(0, hsl(210, 8, 38));
            tg.addColorStop(1, hsl(210, 8, 52));
            ctx.fillStyle = tg;
            ctx.fillRect(gx + ox, gy - th, tw, th);
            // Tower battlements
            ctx.fillStyle = hsl(210, 8, 48);
            for (let m = 0; m < 4; m++) {
                if (m % 2 === 0) ctx.fillRect(gx + ox + m * (tw / 3), gy - th - 10, tw / 3, 10);
            }
            // Conical roof
            ctx.fillStyle = hsl(0, 35, 28);
            ctx.beginPath();
            ctx.moveTo(gx + ox - 4, gy - th);
            ctx.lineTo(gx + ox + tw / 2, gy - th - 55);
            ctx.lineTo(gx + ox + tw + 4, gy - th);
            ctx.fill();
            // Snow on cone
            ctx.fillStyle = 'rgba(225,238,255,0.88)';
            ctx.beginPath();
            ctx.moveTo(gx + ox + tw / 2, gy - th - 55);
            ctx.lineTo(gx + ox + tw * 0.2, gy - th - 18);
            ctx.lineTo(gx + ox + tw * 0.8, gy - th - 18);
            ctx.closePath();
            ctx.fill();
        });

        // Gate arch
        ctx.fillStyle = hsl(210, 8, 20);
        ctx.beginPath();
        ctx.arc(gx, gy - 25, 22, Math.PI, TWO_PI);
        ctx.rect(gx - 22, gy - 25, 44, 25);
        ctx.fill();
        // Portcullis bars
        ctx.strokeStyle = hsl(210, 5, 35);
        ctx.lineWidth = 2;
        for (let b = 0; b < 4; b++) {
            ctx.beginPath();
            ctx.moveTo(gx - 18 + b * 12, gy - 45);
            ctx.lineTo(gx - 18 + b * 12, gy);
            ctx.stroke();
        }

        // Snow on castle top
        ctx.fillStyle = 'rgba(225,238,255,0.88)';
        const snowDepth = 9;
        ctx.beginPath();
        ctx.moveTo(gx - keepW / 2, gy - keepH);
        for (let sx = gx - keepW / 2; sx <= gx + keepW / 2; sx += 8) {
            ctx.lineTo(sx, gy - keepH - snowDepth + Math.sin(sx * 0.3 + t * 0.5) * 2);
        }
        ctx.lineTo(gx + keepW / 2, gy - keepH);
        ctx.closePath();
        ctx.fill();

        // Flag
        const flagX = gx, flagY = gy - keepH - 30;
        const flagWave = Math.sin(t * 2.2) * 8;
        ctx.strokeStyle = hsl(210, 5, 60);
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        ctx.moveTo(flagX, flagY);
        ctx.lineTo(flagX, flagY - 30);
        ctx.stroke();
        ctx.fillStyle = '#c00020';
        ctx.beginPath();
        ctx.moveTo(flagX, flagY - 30);
        ctx.quadraticCurveTo(flagX + 20 + flagWave, flagY - 24, flagX + 22 + flagWave, flagY - 18);
        ctx.quadraticCurveTo(flagX + 15 + flagWave, flagY - 18, flagX, flagY - 18);
        ctx.fill();

        ctx.restore();
    }

    function drawTorch(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        ctx.save();
        // Handle
        ctx.fillStyle = hsl(25, 50, 25);
        ctx.fillRect(gx - 2, gy, 4, 20);
        // Flame
        const flicker = 0.7 + 0.3 * Math.sin(t * 9 + x);
        const fg = ctx.createRadialGradient(gx, gy - 8, 0, gx, gy - 8, 22 * flicker);
        fg.addColorStop(0, `rgba(255,240,100,${0.9 * flicker})`);
        fg.addColorStop(0.4, `rgba(255,120,20,${0.7 * flicker})`);
        fg.addColorStop(1, 'rgba(255,60,0,0)');
        ctx.fillStyle = fg;
        ctx.beginPath();
        ctx.arc(gx, gy - 8, 22 * flicker, 0, TWO_PI);
        ctx.fill();
        ctx.restore();
    }

    function drawHangingLantern(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        const sway = Math.sin(t * 1.4) * 6;
        ctx.save();
        ctx.translate(sway * 0.5, 0);
        // Chain
        ctx.strokeStyle = 'rgba(80,80,80,0.6)';
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.moveTo(gx, gy - 30);
        ctx.lineTo(gx + sway, gy);
        ctx.stroke();
        // Glow
        const lg = ctx.createRadialGradient(gx + sway, gy + 15, 0, gx + sway, gy + 15, 40);
        lg.addColorStop(0, 'rgba(255,200,80,0.6)');
        lg.addColorStop(1, 'rgba(255,200,80,0)');
        ctx.fillStyle = lg;
        ctx.beginPath();
        ctx.arc(gx + sway, gy + 15, 40, 0, TWO_PI);
        ctx.fill();
        // Lantern body
        ctx.fillStyle = '#1a1a1a';
        ctx.fillRect(gx + sway - 8, gy, 16, 22);
        ctx.fillStyle = `rgba(255,200,80,${0.7 + 0.2 * Math.sin(t * 5)})`;
        ctx.fillRect(gx + sway - 5, gy + 3, 10, 16);
        ctx.restore();
    }

    function drawIceberg(ctx, x, y, size, t) {
        const gx = CX + x, gy = CY + y;
        ctx.save();
        const ibG = ctx.createLinearGradient(gx, gy - size, gx + size * 0.3, gy);
        ibG.addColorStop(0, 'rgba(180,220,255,0.9)');
        ibG.addColorStop(1, 'rgba(120,180,240,0.85)');
        ctx.fillStyle = ibG;
        ctx.beginPath();
        ctx.moveTo(gx - size * 0.5, gy);
        ctx.lineTo(gx - size * 0.35, gy - size * 0.6);
        ctx.lineTo(gx - size * 0.1, gy - size);
        ctx.lineTo(gx + size * 0.2, gy - size * 0.75);
        ctx.lineTo(gx + size * 0.5, gy - size * 0.3);
        ctx.lineTo(gx + size * 0.55, gy);
        ctx.closePath();
        ctx.fill();
        // Snow top
        ctx.fillStyle = 'rgba(230,245,255,0.9)';
        ctx.beginPath();
        ctx.moveTo(gx - size * 0.25, gy - size * 0.6);
        ctx.lineTo(gx - size * 0.1, gy - size);
        ctx.lineTo(gx + size * 0.2, gy - size * 0.75);
        ctx.lineTo(gx + size * 0.1, gy - size * 0.58);
        ctx.closePath();
        ctx.fill();
        // Internal blue glow (subsurface)
        const ssg = ctx.createRadialGradient(gx, gy - size * 0.4, 0, gx, gy - size * 0.4, size * 0.5);
        ssg.addColorStop(0, 'rgba(80,160,240,0.25)');
        ssg.addColorStop(1, 'rgba(80,160,240,0)');
        ctx.fillStyle = ssg;
        ctx.beginPath();
        ctx.arc(gx, gy - size * 0.4, size * 0.5, 0, TWO_PI);
        ctx.fill();
        ctx.restore();
    }

    function drawPolarBear(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        const walk = Math.sin(t * 1.5) * 3;
        ctx.save();
        ctx.fillStyle = 'rgba(240,245,255,0.95)';
        // Body
        ctx.beginPath();
        ctx.ellipse(gx, gy - 15, 22, 14, 0, 0, TWO_PI);
        ctx.fill();
        // Head
        ctx.beginPath();
        ctx.ellipse(gx + 20, gy - 22, 14, 11, 0.2, 0, TWO_PI);
        ctx.fill();
        // Snout
        ctx.fillStyle = 'rgba(200,210,225,0.95)';
        ctx.beginPath();
        ctx.ellipse(gx + 32, gy - 20, 7, 5, 0.1, 0, TWO_PI);
        ctx.fill();
        // Nose
        ctx.fillStyle = '#2a2a2a';
        ctx.beginPath();
        ctx.arc(gx + 37, gy - 21, 2.5, 0, TWO_PI);
        ctx.fill();
        // Eyes
        ctx.beginPath();
        ctx.arc(gx + 26, gy - 25, 2, 0, TWO_PI);
        ctx.fill();
        // Legs
        ctx.fillStyle = 'rgba(230,238,250,0.95)';
        [[-12, walk], [-4, -walk], [4, walk], [14, -walk]].forEach(([ox, lw]) => {
            ctx.beginPath();
            ctx.ellipse(gx + ox, gy - 2 + lw, 5, 10, 0.1, 0, TWO_PI);
            ctx.fill();
        });
        // Ears
        ctx.fillStyle = 'rgba(240,245,255,0.95)';
        ctx.beginPath();
        ctx.arc(gx + 14, gy - 30, 5, 0, TWO_PI);
        ctx.fill();
        ctx.beginPath();
        ctx.arc(gx + 22, gy - 31, 5, 0, TWO_PI);
        ctx.fill();
        ctx.restore();
    }

    function drawIgloo(ctx, x, y, t) {
        const gx = CX + x, gy = CY + y;
        ctx.save();
        // Dome
        const ig = ctx.createLinearGradient(gx, gy - 45, gx + 40, gy);
        ig.addColorStop(0, 'rgba(230,245,255,0.95)');
        ig.addColorStop(1, 'rgba(190,220,250,0.9)');
        ctx.fillStyle = ig;
        ctx.beginPath();
        ctx.arc(gx, gy, 40, Math.PI, TWO_PI);
        ctx.fill();
        // Block lines
        ctx.strokeStyle = 'rgba(140,190,240,0.4)';
        ctx.lineWidth = 1;
        for (let row = 0; row < 4; row++) {
            ctx.beginPath();
            ctx.arc(gx, gy, 40 - row * 10, Math.PI, TWO_PI);
            ctx.stroke();
        }
        // Entrance
        ctx.fillStyle = 'rgba(0,20,60,0.6)';
        ctx.beginPath();
        ctx.arc(gx, gy, 14, Math.PI, TWO_PI);
        ctx.rect(gx - 14, gy - 14, 28, 14);
        ctx.fill();
        // Warm glow from inside
        const wg = ctx.createRadialGradient(gx, gy - 14, 0, gx, gy - 14, 20);
        wg.addColorStop(0, `rgba(255,200,80,${0.4 + 0.1 * Math.sin(t * 3)})`);
        wg.addColorStop(1, 'rgba(255,200,80,0)');
        ctx.fillStyle = wg;
        ctx.beginPath();
        ctx.arc(gx, gy - 14, 20, 0, TWO_PI);
        ctx.fill();
        ctx.restore();
    }

    function drawAurora(ctx, t, px, py) {
        const gx = CX + px, gy = CY + py;
        ctx.save();
        ctx.globalCompositeOperation = 'screen';
        for (let band = 0; band < 3; band++) {
            const phase = band * 1.2 + t * 0.4;
            const yBase = gy - R * 0.55 + band * 30;
            const grad = ctx.createLinearGradient(gx - R, yBase, gx + R, yBase);
            const hue = [160, 200, 140][band];
            grad.addColorStop(0, `hsla(${hue},80%,55%,0)`);
            grad.addColorStop(0.3, `hsla(${hue},80%,55%,${0.12 + 0.05 * Math.sin(phase)})`);
            grad.addColorStop(0.7, `hsla(${hue},80%,55%,${0.08 + 0.04 * Math.sin(phase + 1)})`);
            grad.addColorStop(1, `hsla(${hue},80%,55%,0)`);
            ctx.fillStyle = grad;
            ctx.beginPath();
            ctx.moveTo(gx - R, yBase);
            for (let xi = gx - R; xi <= gx + R; xi += 8) {
                const yi = yBase + Math.sin((xi - gx) * 0.015 + phase) * 20
                    + Math.sin((xi - gx) * 0.03 + phase * 1.3) * 10;
                ctx.lineTo(xi, yi);
            }
            ctx.lineTo(gx + R, yBase - 35);
            ctx.lineTo(gx - R, yBase - 35);
            ctx.closePath();
            ctx.fill();
        }
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    // ─── Snow Flake Spawner ───────────────────────────────────────────────────
    function spawnFlakes(count) {
        flakes = [];
        for (let i = 0; i < count; i++) {
            flakes.push(mkFlake());
        }
    }

    function mkFlake(born = false) {
        const depth = Math.random();          // 0=far, 1=close
        const angle = Math.random() * TWO_PI;
        const dist = Math.sqrt(Math.random()) * R * 0.92;
        const rx = CX + Math.cos(angle) * dist;
        const ry = born ? (CY - R * 0.9 - Math.random() * 20)
            : (CY - R * (0.1 + Math.random() * 0.85));

        return {
            x: rx, y: ry,
            vx: 0, vy: 0,
            size: lerp(0.5, 4.5, depth),
            depth,
            alpha: lerp(0.3, 0.92, depth),
            type: Math.floor(Math.random() * 4), // 0=dot, 1=plate, 2=dendrite, 3=needle
            rotation: Math.random() * TWO_PI,
            rotSpeed: (Math.random() - 0.5) * 2,
            settled: false,
            meltTimer: 0,
        };
    }

    function updateFlakes(dt, windX) {
        const gBase = 28;  // px/s^2 gravity
        const drag = 2.8;

        for (let i = 0; i < flakes.length; i++) {
            const f = flakes[i];
            if (f.settled) {
                f.meltTimer -= dt;
                if (f.meltTimer < 0) {
                    f.settled = false;
                    f.y = CY - R * 0.85;
                    f.x = CX + (Math.random() - 0.5) * R * 1.8;
                }
                continue;
            }

            // Turbulence
            const turb = (simplex2(f.x * 0.012, f.y * 0.012 + t * 0.4) - 0.5) * 12;
            const wx = windX * lerp(0.2, 1.0, f.depth) + turb;
            const gy = gBase * lerp(0.15, 1.0, f.depth);

            // Shake impulse on flake
            f.vx += (shakeVX * 0.006 + wx) * dt;
            f.vy += (shakeVY * 0.006 + gy) * dt;
            f.vx -= f.vx * drag * dt;
            f.vy -= (f.vy - gy / drag) * drag * dt;

            f.x += f.vx * dt + shakeX * 0.04 * f.depth;
            f.y += f.vy * dt + shakeY * 0.02 * f.depth;
            f.rotation += f.rotSpeed * dt;

            // Confine inside sphere (transformed to globe coords)
            const dx = f.x - CX, dy = f.y - CY;
            const dd = Math.sqrt(dx * dx + dy * dy);
            if (dd > R * 0.93) {
                const nx = dx / dd, ny = dy / dd;
                f.x = CX + nx * R * 0.93;
                f.y = CY + ny * R * 0.93;
                f.vx -= nx * Math.abs(f.vx) * 0.5;
                f.vy -= ny * Math.abs(f.vy) * 0.5;
            }

            // Ground accumulation
            const groundTop = groundY(f.x - CX) + CY;
            const accumH = getAccum(f.x);
            if (f.y >= groundTop - accumH) {
                f.y = groundTop - accumH;
                addAccum(f.x, f.size * 0.18);
                disturbFluid((f.x - CX + R) / (R * 2), f.vy * 0.03);
                f.settled = true;
                f.meltTimer = 8 + Math.random() * 20;
            }

            // Respawn if out of globe
            if (f.y > CY + R || f.y < CY - R * 1.1) {
                flakes[i] = mkFlake(true);
            }
        }
    }

    // ─── Accumulation ─────────────────────────────────────────────────────────
    function initAccum() {
        accum = new Float32Array(accumRes).fill(0);
    }

    function getAccum(wx) {
        const col = Math.floor(((wx - (CX - R)) / (R * 2)) * accumRes);
        return accum[Math.max(0, Math.min(accumRes - 1, col))] || 0;
    }

    function addAccum(wx, amt) {
        const col = Math.floor(((wx - (CX - R)) / (R * 2)) * accumRes);
        if (col >= 0 && col < accumRes) {
            accum[col] = Math.min(accum[col] + amt, 35);
            // Spread to neighbors
            if (col > 0) accum[col - 1] = Math.min(accum[col - 1] + amt * 0.4, 35);
            if (col < accumRes - 1) accum[col + 1] = Math.min(accum[col + 1] + amt * 0.4, 35);
        }
    }

    // ─── Motes ────────────────────────────────────────────────────────────────
    function spawnMotes(count) {
        motes = [];
        for (let i = 0; i < count; i++) {
            motes.push(mkMote());
        }
    }

    function mkMote() {
        const angle = Math.random() * TWO_PI;
        const dist = Math.sqrt(Math.random()) * R * 0.88;
        return {
            x: CX + Math.cos(angle) * dist,
            y: CY + Math.sin(angle) * dist,
            r: Math.random() * 1.2 + 0.3,
            vx: (Math.random() - 0.5) * 4,
            vy: (Math.random() - 0.5) * 2 - 0.5,
            alpha: Math.random() * 0.25 + 0.05,
            life: Math.random(),
        };
    }

    function updateMotes(dt) {
        for (let i = 0; i < motes.length; i++) {
            const m = motes[i];
            m.x += m.vx * dt + shakeX * 0.01;
            m.y += m.vy * dt + shakeY * 0.01;
            m.life -= dt * 0.12;
            if (m.life < 0 || !insideGlobe(m.x, m.y, 0.95)) {
                motes[i] = mkMote();
            }
        }
    }

    // ─── Lighting ─────────────────────────────────────────────────────────────
    function buildLighting(hour) {
        const h = Math.min(23, Math.max(0, hour));
        // Sky gradient based on time
        if (h >= 6 && h < 8) {         // Dawn
            skyTop = '#1a1a5a'; skyBot = '#ff7043';
            ambientL = 0.45; sunAngle = (h - 6) / 2 * 60 * RAD;
            sunColor = [255, 180, 100];
        } else if (h >= 8 && h < 17) {  // Day
            const noon = 1 - Math.abs(h - 12.5) / 4.5;
            skyTop = `hsl(210,${55 + noon * 15}%,${40 + noon * 20}%)`;
            skyBot = `hsl(200,${60 + noon * 10}%,${55 + noon * 15}%)`;
            ambientL = 0.85 + noon * 0.15;
            sunAngle = (h - 6) / 12 * Math.PI;
            sunColor = [255, 240, 200];
        } else if (h >= 17 && h < 20) { // Dusk
            const frac = (h - 17) / 3;
            skyTop = `hsl(${220 - frac * 210},${50 - frac * 30}%,${35 - frac * 20}%)`;
            skyBot = `hsl(${25 - frac * 10},${70 - frac * 30}%,${50 - frac * 20}%)`;
            ambientL = 0.65 - frac * 0.35;
            sunAngle = (h - 6) / 12 * Math.PI;
            sunColor = [255, 140, 60];
        } else {                         // Night
            skyTop = '#020515'; skyBot = '#0a0e2a';
            ambientL = 0.18;
            sunAngle = 0;
            sunColor = [60, 80, 140];
        }
    }

    // ─── Render ───────────────────────────────────────────────────────────────
    function render() {
        ctx.clearRect(0, 0, W, H);

        // Save state, clip to globe
        ctx.save();
        const globePath = new Path2D();
        globePath.arc(CX + shakeX * 0.5, CY + shakeY * 0.5, R, 0, TWO_PI);
        ctx.clip(globePath);

        // Sky background
        drawSky();

        // Sort and draw scene objects
        sceneObjs.sort((a, b) => a.z - b.z);
        for (const obj of sceneObjs) {
            ctx.save();
            obj.draw(ctx, t, parallaxX, parallaxY);
            ctx.restore();
        }

        // Point light contributions
        drawLampGlow();

        // Snow accumulation profile
        drawAccumProfile();

        // Motes (ambient dust)
        drawMotes();

        // Snow flakes
        drawFlakes();

        // Water surface with fluid simulation
        drawWaterSurface();

        // Light shafts
        if (ambientL > 0.5) drawLightShafts();

        ctx.restore(); // end globe clip

        // Globe glass overlay (refraction, reflection, Fresnel, caustics)
        drawGlassOverlay();

        // Frost rim crystals
        drawFrostRim();

        // Globe base
        drawBase();

        // Stars (outside clip, on sky canvas behind) — drawn outside clip for night
        if (ambientL < 0.4) drawStars();
    }

    function drawSky() {
        const gx = CX + shakeX * 0.5, gy = CY + shakeY * 0.5;
        const sg = ctx.createRadialGradient(gx, gy - R * 0.2, R * 0.1, gx, gy, R);
        sg.addColorStop(0, skyTop || '#0a0e2a');
        sg.addColorStop(0.6, skyBot || '#0a0e2a');
        sg.addColorStop(1, 'rgba(5,10,30,0.9)');
        ctx.fillStyle = sg;
        ctx.beginPath();
        ctx.arc(gx, gy, R, 0, TWO_PI);
        ctx.fill();

        // Stars for night / dusk
        if (ambientL < 0.55) {
            ctx.save();
            ctx.globalAlpha = Math.max(0, 1 - ambientL * 2.5);
            for (let s = 0; s < 80; s++) {
                const sx = gx - R * 0.85 + rndSeeded(s, 11111) * R * 1.7;
                const sy = gy - R * 0.9 + rndSeeded(s, 22222) * R * 0.85;
                const sr = rndSeeded(s, 33333) * 1.5 + 0.3;
                const twinkle = 0.5 + 0.5 * Math.sin(t * (1 + rndSeeded(s, 44444) * 3) + s);
                ctx.globalAlpha = Math.max(0, (1 - ambientL * 2.5)) * twinkle * 0.8;
                ctx.fillStyle = `hsl(${200 + rndSeeded(s, 55555) * 60},60%,${80 + rndSeeded(s, 66666) * 20}%)`;
                ctx.beginPath();
                ctx.arc(sx, sy, sr, 0, TWO_PI);
                ctx.fill();
            }
            ctx.restore();
        }

        // Sun / Moon disc
        if (ambientL > 0.3 || (ambientL < 0.25)) {
            const gx2 = CX + shakeX * 0.5, gy2 = CY + shakeY * 0.5;
            const sx = gx2 + Math.cos(sunAngle - HALF_PI) * R * 0.65;
            const sy = gy2 + Math.sin(sunAngle - HALF_PI) * R * 0.55;
            if (ambientL > 0.3) {
                // Sun
                const sunG = ctx.createRadialGradient(sx, sy, 0, sx, sy, 55);
                sunG.addColorStop(0, `rgba(${sunColor.join(',')},0.95)`);
                sunG.addColorStop(0.4, `rgba(${sunColor.join(',')},0.4)`);
                sunG.addColorStop(1, `rgba(${sunColor.join(',')},0)`);
                ctx.fillStyle = sunG;
                ctx.beginPath();
                ctx.arc(sx, sy, 55, 0, TWO_PI);
                ctx.fill();
                ctx.fillStyle = `rgba(255,250,220,0.95)`;
                ctx.beginPath();
                ctx.arc(sx, sy, 14, 0, TWO_PI);
                ctx.fill();
            } else {
                // Moon
                ctx.save();
                const mg = ctx.createRadialGradient(sx, sy, 0, sx, sy, 18);
                mg.addColorStop(0, 'rgba(230,230,200,0.95)');
                mg.addColorStop(1, 'rgba(200,210,220,0.8)');
                ctx.fillStyle = mg;
                ctx.beginPath();
                ctx.arc(sx, sy, 18, 0, TWO_PI);
                ctx.fill();
                // Crater shadows
                ctx.fillStyle = 'rgba(0,0,0,0.08)';
                [[4, 3, 4], [-5, -2, 3], [2, -5, 2]].forEach(([cx, cy, cr]) => {
                    ctx.beginPath();
                    ctx.arc(sx + cx, sy + cy, cr, 0, TWO_PI);
                    ctx.fill();
                });
                ctx.restore();
            }
        }
    }

    function drawLampGlow() {
        ctx.save();
        ctx.globalCompositeOperation = 'screen';
        for (const lamp of lampGlow) {
            const lx = CX + lamp.x + parallaxX * 0.6 + shakeX * 0.5;
            const ly = CY + lamp.y + parallaxY * 0.6 + shakeY * 0.5;
            const flicker = 0.75 + 0.25 * Math.sin(t * 5.3 + lamp.x);
            const lg = ctx.createRadialGradient(lx, ly, 0, lx, ly, lamp.r * flicker);
            const [r, g, b] = lamp.color;
            lg.addColorStop(0, `rgba(${r},${g},${b},${lamp.intensity * flicker})`);
            lg.addColorStop(0.5, `rgba(${r},${g},${b},${lamp.intensity * flicker * 0.3})`);
            lg.addColorStop(1, `rgba(${r},${g},${b},0)`);
            ctx.fillStyle = lg;
            ctx.beginPath();
            ctx.arc(lx, ly, lamp.r * flicker * 1.4, 0, TWO_PI);
            ctx.fill();
        }
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    function drawAccumProfile() {
        if (!accum.length) return;
        ctx.save();
        ctx.fillStyle = 'rgba(230,242,255,0.92)';
        ctx.beginPath();
        const step = (R * 2) / accumRes;
        const startX = CX - R;
        ctx.moveTo(startX, CY + R * 0.42);
        for (let i = 0; i < accumRes; i++) {
            const ax = startX + i * step + shakeX * 0.2;
            const groundH = groundY((ax - CX - shakeX * 0.5)) + CY;
            const ay = groundH - accum[i] + shakeY * 0.1;
            const bump = Math.sin(i * 0.18 + t * 0.4) * 0.8;
            if (i === 0) ctx.moveTo(ax, ay + bump);
            else ctx.lineTo(ax, ay + bump);
        }
        ctx.lineTo(startX + accumRes * step, CY + R * 0.42);
        ctx.closePath();
        ctx.fill();

        // SSS glow on accum
        ctx.globalCompositeOperation = 'screen';
        ctx.fillStyle = 'rgba(100,160,230,0.10)';
        ctx.fill();
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    function drawFlakes() {
        ctx.save();
        for (const f of flakes) {
            if (f.settled) continue;
            ctx.save();
            ctx.globalAlpha = f.alpha * (0.7 + shakeEnergy * 0.3);
            ctx.translate(f.x + shakeX * f.depth * 0.08,
                f.y + shakeY * f.depth * 0.08);
            ctx.rotate(f.rotation);

            switch (f.type) {
                case 0: drawDotFlake(ctx, f.size); break;
                case 1: drawPlateFlake(ctx, f.size); break;
                case 2: drawDendriteFlake(ctx, f.size); break;
                case 3: drawNeedleFlake(ctx, f.size); break;
            }
            ctx.restore();
        }
        ctx.restore();
    }

    function drawDotFlake(ctx, s) {
        // Simple circular flake with specular
        const fg = ctx.createRadialGradient(-s * 0.2, -s * 0.2, 0, 0, 0, s);
        fg.addColorStop(0, 'rgba(255,255,255,0.98)');
        fg.addColorStop(0.6, 'rgba(210,230,255,0.9)');
        fg.addColorStop(1, 'rgba(180,210,250,0.5)');
        ctx.fillStyle = fg;
        ctx.beginPath();
        ctx.arc(0, 0, s, 0, TWO_PI);
        ctx.fill();
    }

    function drawPlateFlake(ctx, s) {
        // Hexagonal plate
        ctx.strokeStyle = 'rgba(220,235,255,0.9)';
        ctx.lineWidth = Math.max(0.5, s * 0.3);
        ctx.beginPath();
        for (let arm = 0; arm < 6; arm++) {
            const a = arm * Math.PI / 3;
            ctx.moveTo(0, 0);
            ctx.lineTo(Math.cos(a) * s * 1.5, Math.sin(a) * s * 1.5);
        }
        ctx.stroke();
        // Hex outline
        ctx.strokeStyle = 'rgba(200,220,255,0.7)';
        ctx.lineWidth = Math.max(0.3, s * 0.2);
        ctx.beginPath();
        for (let v = 0; v < 7; v++) {
            const a = v * Math.PI / 3;
            v === 0 ? ctx.moveTo(Math.cos(a) * s, Math.sin(a) * s)
                : ctx.lineTo(Math.cos(a) * s, Math.sin(a) * s);
        }
        ctx.stroke();
    }

    function drawDendriteFlake(ctx, s) {
        // Branching dendrite
        ctx.strokeStyle = 'rgba(230,245,255,0.88)';
        ctx.lineWidth = Math.max(0.4, s * 0.25);
        ctx.lineCap = 'round';
        for (let arm = 0; arm < 6; arm++) {
            const a = arm * Math.PI / 3;
            ctx.save();
            ctx.rotate(a);
            ctx.beginPath();
            ctx.moveTo(0, 0);
            ctx.lineTo(s * 1.8, 0);
            // Sub-branches
            for (let b = 1; b <= 3; b++) {
                const bx = (b / 4) * s * 1.8;
                const bl = s * 0.6 * (1 - b * 0.2);
                ctx.moveTo(bx, 0); ctx.lineTo(bx + bl * 0.6, -bl * 0.7);
                ctx.moveTo(bx, 0); ctx.lineTo(bx + bl * 0.6, bl * 0.7);
            }
            ctx.stroke();
            ctx.restore();
        }
    }

    function drawNeedleFlake(ctx, s) {
        // Elongated needle / column
        ctx.strokeStyle = 'rgba(210,230,255,0.85)';
        ctx.lineWidth = Math.max(0.4, s * 0.35);
        ctx.lineCap = 'round';
        ctx.beginPath();
        ctx.moveTo(-s * 2.2, 0);
        ctx.lineTo(s * 2.2, 0);
        ctx.stroke();
        // End caps
        ctx.strokeStyle = 'rgba(240,248,255,0.7)';
        ctx.lineWidth = s * 0.2;
        [-s * 2.2, s * 2.2].forEach(ex => {
            ctx.beginPath();
            ctx.moveTo(ex, -s * 0.5);
            ctx.lineTo(ex, s * 0.5);
            ctx.stroke();
        });
    }

    function drawMotes() {
        ctx.save();
        ctx.globalCompositeOperation = 'screen';
        for (const m of motes) {
            ctx.globalAlpha = m.alpha * (0.5 + shakeEnergy);
            ctx.fillStyle = 'rgba(200,225,255,1)';
            ctx.beginPath();
            ctx.arc(m.x + shakeX * 0.02, m.y + shakeY * 0.02, m.r, 0, TWO_PI);
            ctx.fill();
        }
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    function drawWaterSurface() {
        const gx = CX + shakeX * 0.5;
        const gy = CY + shakeY * 0.5;
        const waterY = gy + R * 0.42;
        const n = fluidWave.length;

        // Water body
        ctx.save();
        ctx.globalAlpha = 0.18;
        const wbg = ctx.createLinearGradient(0, waterY, 0, waterY + 60);
        wbg.addColorStop(0, 'rgba(100,160,220,1)');
        wbg.addColorStop(1, 'rgba(60,120,200,1)');
        ctx.fillStyle = wbg;
        ctx.beginPath();
        ctx.moveTo(gx - R, waterY);
        for (let i = 0; i < n; i++) {
            const wx = gx - R + (i / (n - 1)) * R * 2;
            ctx.lineTo(wx, waterY + fluidWave[i] * 5);
        }
        ctx.lineTo(gx + R, waterY + 60);
        ctx.lineTo(gx - R, waterY + 60);
        ctx.fill();
        ctx.restore();

        // Surface shimmer / caustics
        ctx.save();
        ctx.globalAlpha = 0.15 + shakeEnergy * 0.3;
        ctx.strokeStyle = 'rgba(180,220,255,0.8)';
        ctx.lineWidth = 1;
        ctx.beginPath();
        for (let i = 0; i < n; i++) {
            const wx = gx - R + (i / (n - 1)) * R * 2;
            const wy = waterY + fluidWave[i] * 5;
            i === 0 ? ctx.moveTo(wx, wy) : ctx.lineTo(wx, wy);
        }
        ctx.stroke();

        // Caustic blobs on ground
        ctx.globalCompositeOperation = 'screen';
        ctx.globalAlpha = 0.08 + shakeEnergy * 0.12;
        for (let c = 0; c < 12; c++) {
            const cangle = c / 12 * TWO_PI + t * 0.5;
            const cr = R * (0.15 + Math.sin(t * 0.7 + c) * 0.1);
            const cx2 = gx + Math.cos(cangle) * cr;
            const cy2 = gy + R * 0.35 + Math.sin(cangle * 3) * 8;
            const cg = ctx.createRadialGradient(cx2, cy2, 0, cx2, cy2, 22);
            cg.addColorStop(0, 'rgba(120,200,255,0.9)');
            cg.addColorStop(1, 'rgba(120,200,255,0)');
            ctx.fillStyle = cg;
            ctx.beginPath();
            ctx.arc(cx2, cy2, 22, 0, TWO_PI);
            ctx.fill();
        }
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    function drawLightShafts() {
        ctx.save();
        ctx.globalCompositeOperation = 'screen';
        ctx.globalAlpha = 0.04 * ambientL;
        const ox = CX + Math.cos(sunAngle - HALF_PI) * R * 0.5 + shakeX * 0.5;
        const oy = CY + Math.sin(sunAngle - HALF_PI) * R * 0.5 + shakeY * 0.5;
        for (let ray = 0; ray < 5; ray++) {
            const ra = sunAngle + (ray - 2) * 0.08;
            const sg = ctx.createLinearGradient(ox, oy, ox + Math.cos(ra) * R * 1.5, oy + Math.sin(ra) * R * 1.5);
            sg.addColorStop(0, `rgba(${sunColor.join(',')},0.8)`);
            sg.addColorStop(1, `rgba(${sunColor.join(',')},0)`);
            ctx.fillStyle = sg;
            ctx.beginPath();
            ctx.moveTo(ox, oy);
            ctx.lineTo(ox + Math.cos(ra - 0.03) * R * 1.5, oy + Math.sin(ra - 0.03) * R * 1.5);
            ctx.lineTo(ox + Math.cos(ra + 0.03) * R * 1.5, oy + Math.sin(ra + 0.03) * R * 1.5);
            ctx.fill();
        }
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    function drawStars() {
        // Extra stars drawn over the base canvas for night
    }

    // ─── Glass Overlay ────────────────────────────────────────────────────────
    function drawGlassOverlay() {
        const gx = CX + shakeX * 0.5, gy = CY + shakeY * 0.5;

        // 1. Refraction distortion haze (rim edge)
        const rim = ctx.createRadialGradient(gx, gy, R * 0.82, gx, gy, R);
        rim.addColorStop(0, 'rgba(140,190,240,0)');
        rim.addColorStop(0.7, 'rgba(160,210,250,0.12)');
        rim.addColorStop(1, 'rgba(100,150,220,0.35)');
        ctx.fillStyle = rim;
        ctx.beginPath();
        ctx.arc(gx, gy, R, 0, TWO_PI);
        ctx.fill();

        // 2. Fresnel (edge darkening)
        const fresnel = ctx.createRadialGradient(gx, gy, R * 0.78, gx, gy, R);
        fresnel.addColorStop(0, 'rgba(0,0,0,0)');
        fresnel.addColorStop(0.85, 'rgba(0,10,30,0.08)');
        fresnel.addColorStop(1, 'rgba(0,10,40,0.55)');
        ctx.fillStyle = fresnel;
        ctx.beginPath();
        ctx.arc(gx, gy, R, 0, TWO_PI);
        ctx.fill();

        // 3. Main specular highlight
        const hlx = gx - R * 0.32, hly = gy - R * 0.38;
        const hl = ctx.createRadialGradient(hlx - 10, hly - 10, 0, hlx, hly, R * 0.55);
        hl.addColorStop(0, 'rgba(255,255,255,0.65)');
        hl.addColorStop(0.2, 'rgba(255,255,255,0.20)');
        hl.addColorStop(0.6, 'rgba(200,225,255,0.04)');
        hl.addColorStop(1, 'rgba(200,225,255,0)');
        ctx.fillStyle = hl;
        ctx.beginPath();
        ctx.arc(gx, gy, R, 0, TWO_PI);
        ctx.fill();

        // 4. Secondary small specular
        ctx.save();
        ctx.globalAlpha = 0.35;
        ctx.fillStyle = 'rgba(255,255,255,0.9)';
        ctx.beginPath();
        ctx.ellipse(gx - R * 0.28, gy - R * 0.35, R * 0.07, R * 0.035, -0.6, 0, TWO_PI);
        ctx.fill();
        ctx.restore();

        // 5. Bottom internal reflection
        const br = ctx.createRadialGradient(gx + R * 0.2, gy + R * 0.55, 0, gx + R * 0.2, gy + R * 0.55, R * 0.4);
        br.addColorStop(0, 'rgba(180,215,255,0.12)');
        br.addColorStop(1, 'rgba(180,215,255,0)');
        ctx.fillStyle = br;
        ctx.beginPath();
        ctx.arc(gx, gy, R, 0, TWO_PI);
        ctx.fill();

        // 6. Caustic shimmer on inner glass wall
        ctx.save();
        ctx.globalCompositeOperation = 'screen';
        ctx.globalAlpha = 0.07 + Math.sin(t * 1.8) * 0.03;
        for (let c = 0; c < 8; c++) {
            const ca = c / 8 * TWO_PI + t * 0.25;
            const cr = R * (0.88 + Math.sin(t * 0.9 + c) * 0.06);
            const csg = ctx.createRadialGradient(
                gx + Math.cos(ca) * cr, gy + Math.sin(ca) * cr, 0,
                gx + Math.cos(ca) * cr, gy + Math.sin(ca) * cr, 18
            );
            csg.addColorStop(0, 'rgba(200,230,255,0.9)');
            csg.addColorStop(1, 'rgba(200,230,255,0)');
            ctx.fillStyle = csg;
            ctx.beginPath();
            ctx.arc(gx + Math.cos(ca) * cr, gy + Math.sin(ca) * cr, 18, 0, TWO_PI);
            ctx.fill();
        }
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();

        // 7. Globe outline stroke
        ctx.save();
        ctx.strokeStyle = 'rgba(160,210,250,0.45)';
        ctx.lineWidth = 2.5;
        ctx.beginPath();
        ctx.arc(gx, gy, R, 0, TWO_PI);
        ctx.stroke();
        ctx.restore();
    }

    // ─── Frost Rim ────────────────────────────────────────────────────────────
    function drawFrostRim() {
        const gx = CX + shakeX * 0.3, gy = CY + shakeY * 0.3;
        ctx.save();
        ctx.globalAlpha = 0.35 + Math.sin(t * 0.5) * 0.05;
        for (const fp of frostPoints) {
            const fx = gx + Math.cos(fp.angle) * R;
            const fy = gy + Math.sin(fp.angle) * R;
            // Crystal arms
            ctx.strokeStyle = `rgba(200,230,255,${fp.alpha})`;
            ctx.lineWidth = 0.8;
            ctx.lineCap = 'round';
            for (let arm = 0; arm < fp.arms; arm++) {
                const aa = fp.angle + arm * (TWO_PI / fp.arms) + t * 0.01;
                const inward = fp.angle + Math.PI;  // pointing inward
                ctx.beginPath();
                ctx.moveTo(fx, fy);
                ctx.lineTo(fx + Math.cos(inward + (arm - fp.arms / 2) * 0.35) * fp.armLen,
                    fy + Math.sin(inward + (arm - fp.arms / 2) * 0.35) * fp.armLen);
                ctx.stroke();
            }
            // Nub
            ctx.fillStyle = `rgba(240,248,255,${fp.alpha * 0.6})`;
            ctx.beginPath();
            ctx.arc(fx, fy, 1.2, 0, TWO_PI);
            ctx.fill();
        }
        ctx.restore();
    }

    // ─── Globe Base ───────────────────────────────────────────────────────────
    function drawBase() {
        const gx = CX + shakeX * 0.15, gy = CY + shakeY * 0.15;
        const baseTop = gy + R - 4;
        const baseBot = baseTop + 110;
        const baseW = R * 1.32;

        // Drop shadow
        ctx.save();
        ctx.shadowColor = 'rgba(0,0,0,0.6)';
        ctx.shadowBlur = 40;
        ctx.shadowOffsetY = 20;
        ctx.fillStyle = 'rgba(0,0,0,0.01)';
        ctx.beginPath();
        ctx.ellipse(gx, baseBot, baseW * 0.85, 22, 0, 0, TWO_PI);
        ctx.fill();
        ctx.restore();

        // Main base body — mahogany gradient
        const bg = ctx.createLinearGradient(gx - baseW, baseTop, gx + baseW, baseBot);
        bg.addColorStop(0, '#4a1a08');
        bg.addColorStop(0.15, '#7a3010');
        bg.addColorStop(0.35, '#9a4018');
        bg.addColorStop(0.5, '#6a2808');
        bg.addColorStop(0.7, '#8a3814');
        bg.addColorStop(0.85, '#5a2008');
        bg.addColorStop(1, '#3a1005');
        ctx.fillStyle = bg;
        ctx.beginPath();
        ctx.moveTo(gx - baseW, baseTop + 10);
        ctx.quadraticCurveTo(gx - baseW, baseTop, gx - baseW * 0.88, baseTop);
        ctx.lineTo(gx + baseW * 0.88, baseTop);
        ctx.quadraticCurveTo(gx + baseW, baseTop, gx + baseW, baseTop + 10);
        ctx.lineTo(gx + baseW, baseBot - 14);
        ctx.quadraticCurveTo(gx + baseW, baseBot, gx + baseW * 0.92, baseBot);
        ctx.lineTo(gx - baseW * 0.92, baseBot);
        ctx.quadraticCurveTo(gx - baseW, baseBot, gx - baseW, baseBot - 14);
        ctx.closePath();
        ctx.fill();

        // Wood grain lines
        ctx.save();
        ctx.beginPath();
        ctx.moveTo(gx - baseW, baseTop + 10);
        ctx.quadraticCurveTo(gx - baseW, baseTop, gx - baseW * 0.88, baseTop);
        ctx.lineTo(gx + baseW * 0.88, baseTop);
        ctx.quadraticCurveTo(gx + baseW, baseTop, gx + baseW, baseTop + 10);
        ctx.lineTo(gx + baseW, baseBot - 14);
        ctx.quadraticCurveTo(gx + baseW, baseBot, gx + baseW * 0.92, baseBot);
        ctx.lineTo(gx - baseW * 0.92, baseBot);
        ctx.quadraticCurveTo(gx - baseW, baseBot, gx - baseW, baseBot - 14);
        ctx.closePath();
        ctx.clip();
        ctx.strokeStyle = 'rgba(0,0,0,0.07)';
        ctx.lineWidth = 1.5;
        for (let g2 = 0; g2 < 18; g2++) {
            const gy2 = baseTop + 4 + g2 * (baseBot - baseTop) / 18;
            ctx.beginPath();
            ctx.moveTo(gx - baseW, gy2);
            ctx.bezierCurveTo(gx - baseW * 0.5, gy2 + Math.sin(g2 * 0.7) * 3,
                gx + baseW * 0.5, gy2 - Math.sin(g2 * 0.9) * 3,
                gx + baseW, gy2);
            ctx.stroke();
        }
        ctx.restore();

        // Gold top ring (collar where globe sits)
        const ringY = baseTop;
        const goldenG = ctx.createLinearGradient(gx - baseW * 0.6, ringY, gx + baseW * 0.6, ringY + 20);
        goldenG.addColorStop(0, '#a07830');
        goldenG.addColorStop(0.3, '#e8c870');
        goldenG.addColorStop(0.5, '#f8e088');
        goldenG.addColorStop(0.7, '#c8a840');
        goldenG.addColorStop(1, '#a07830');
        ctx.fillStyle = goldenG;
        ctx.beginPath();
        ctx.ellipse(gx, ringY, baseW * 0.88, 18, 0, 0, TWO_PI);
        ctx.fill();

        // Chrome reflection ring on collar
        ctx.save();
        ctx.globalCompositeOperation = 'screen';
        ctx.globalAlpha = 0.25;
        const chromeG = ctx.createLinearGradient(gx - baseW * 0.5, ringY - 8, gx + baseW * 0.5, ringY + 8);
        chromeG.addColorStop(0, 'rgba(255,255,255,0)');
        chromeG.addColorStop(0.4, 'rgba(255,255,255,0.8)');
        chromeG.addColorStop(0.6, 'rgba(255,255,255,0.8)');
        chromeG.addColorStop(1, 'rgba(255,255,255,0)');
        ctx.fillStyle = chromeG;
        ctx.beginPath();
        ctx.ellipse(gx, ringY, baseW * 0.88, 18, 0, 0, TWO_PI);
        ctx.fill();
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();

        // Gold filigree line on body
        const filigreeY = baseTop + 32;
        ctx.strokeStyle = '#c8a840';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(gx - baseW * 0.78, filigreeY);
        for (let fx = 0; fx <= 1; fx += 0.02) {
            const fix = gx - baseW * 0.78 + fx * baseW * 1.56;
            const fiy = filigreeY + Math.sin(fx * Math.PI * 10) * 4;
            ctx.lineTo(fix, fiy);
        }
        ctx.stroke();

        // Text on base
        ctx.save();
        ctx.font = `bold 12px 'Georgia', serif`;
        ctx.letterSpacing = '3px';
        ctx.textAlign = 'center';
        ctx.fillStyle = '#c8a840';
        ctx.shadowColor = 'rgba(0,0,0,0.5)';
        ctx.shadowBlur = 4;
        ctx.fillText('❄  SNOW GLOBE  ❄', gx, filigreeY + 20);
        ctx.restore();

        // Bottom foot plate
        const footG = ctx.createLinearGradient(gx - baseW * 0.5, baseBot, gx + baseW * 0.5, baseBot + 10);
        footG.addColorStop(0, '#a07830');
        footG.addColorStop(0.5, '#d8b850');
        footG.addColorStop(1, '#a07830');
        ctx.fillStyle = footG;
        ctx.beginPath();
        ctx.ellipse(gx, baseBot, baseW * 0.92, 12, 0, 0, TWO_PI);
        ctx.fill();

        // Scene-reflected colors in the chrome ring (faked)
        ctx.save();
        ctx.globalCompositeOperation = 'multiply';
        ctx.globalAlpha = 0.12;
        ctx.fillStyle = skyTop || '#0a0e2a';
        ctx.beginPath();
        ctx.ellipse(gx, ringY, baseW * 0.88, 18, 0, 0, TWO_PI);
        ctx.fill();
        ctx.globalCompositeOperation = 'source-over';
        ctx.restore();
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────
    function groundY(relX) {
        // Globe-relative Y of the ground surface at given relative X
        return R * 0.40 + Math.pow(relX / R, 2) * R * 0.04;
    }

    function insideGlobe(wx, wy, frac = 1.0) {
        const dx = wx - CX, dy = wy - CY;
        return dx * dx + dy * dy < (R * frac) * (R * frac);
    }

    function lerp(a, b, t) { return a + (b - a) * t; }

    function rnd(lo, hi) { return lo + Math.random() * (hi - lo); }

    function rndSeeded(seed, salt) {
        // Deterministic pseudo-random based on seed
        const x = Math.sin(seed * 127.1 + salt * 311.7) * 43758.5453;
        return x - Math.floor(x);
    }

    function hsl(h, s, l) { return `hsl(${h},${s}%,${l}%)`; }

    function snowyEdge(ctx, fromX, toX, y, amp) {
        const step = 6;
        const dir = fromX < toX ? 1 : -1;
        for (let ex = fromX; dir * (toX - ex) > 0; ex += dir * step) {
            ctx.lineTo(ex, y - amp * (Math.sin(ex * 0.4) * 0.5 + 0.5) - amp * 0.3);
        }
    }

    // Simplex-inspired 2D noise (lightweight)
    function simplex2(x, y) {
        const F2 = 0.5 * (Math.sqrt(3) - 1);
        const G2 = (3 - Math.sqrt(3)) / 6;
        const s = (x + y) * F2;
        const i = Math.floor(x + s), j = Math.floor(y + s);
        const t2 = (i + j) * G2;
        const X0 = i - t2, Y0 = j - t2;
        const x0 = x - X0, y0 = y - Y0;
        const i1 = x0 > y0 ? 1 : 0, j1 = x0 > y0 ? 0 : 1;
        const x1 = x0 - i1 + G2, y1 = y0 - j1 + G2;
        const x2 = x0 - 1 + 2 * G2, y2 = y0 - 1 + 2 * G2;
        let n0 = 0, n1 = 0, n2 = 0;
        const t0 = 0.5 - x0 * x0 - y0 * y0;
        if (t0 >= 0) { const g = grad2(i, j); n0 = t0 * t0 * t0 * t0 * (g[0] * x0 + g[1] * y0); }
        const t1 = 0.5 - x1 * x1 - y1 * y1;
        if (t1 >= 0) { const g = grad2(i + i1, j + j1); n1 = t1 * t1 * t1 * t1 * (g[0] * x1 + g[1] * y1); }
        const t2b = 0.5 - x2 * x2 - y2 * y2;
        if (t2b >= 0) { const g = grad2(i + 1, j + 1); n2 = t2b * t2b * t2b * t2b * (g[0] * x2 + g[1] * y2); }
        return 70 * (n0 + n1 + n2) * 0.5 + 0.5;
    }

    const GRADS2 = [[1, 1], [-1, 1], [1, -1], [-1, -1], [1, 0], [-1, 0], [0, 1], [0, -1]];
    function grad2(ix, iy) {
        const h = (ix * 1619 + iy * 31337 + 1013904223) & 0x7fffffff;
        return GRADS2[h % GRADS2.length];
    }

    // ─── Input ────────────────────────────────────────────────────────────────
    function onMouseDown(e) {
        mouseDown = true;
        lastMX = e.clientX; lastMY = e.clientY;
        e.preventDefault();
    }

    function onMouseMove(e) {
        if (!mouseDown) return;
        const dx = e.clientX - lastMX, dy = e.clientY - lastMY;
        parallaxTargX = Math.max(-40, Math.min(40, parallaxTargX + dx * 0.4));
        parallaxTargY = Math.max(-25, Math.min(25, parallaxTargY + dy * 0.4));
        // Sloshing effect
        shakeVX += dx * 0.15; shakeVY += dy * 0.15;
        disturbFluid(0.5 + dx * 0.003, Math.abs(dx) * 0.5);
        lastMX = e.clientX; lastMY = e.clientY;
        e.preventDefault();
    }

    function onMouseUp() { mouseDown = false; }

    function onTouchStart(e) {
        lastMX = e.touches[0].clientX;
        lastMY = e.touches[0].clientY;
        mouseDown = true;
    }

    function onTouchMove(e) {
        e.preventDefault();
        onMouseMove({ clientX: e.touches[0].clientX, clientY: e.touches[0].clientY, preventDefault: () => { } });
    }

    // ─── Public API ───────────────────────────────────────────────────────────
    function shake() {
        const intensity = 280 + Math.random() * 180;
        shakeVX = (Math.random() - 0.5) * intensity;
        shakeVY = -(Math.random() * intensity * 0.7 + 100);
        shakeEnergy = 1.0;
        // Disturb fluid at multiple points
        for (let i = 0; i < 8; i++) {
            disturbFluid(Math.random(), (Math.random() - 0.5) * 60);
        }
        // Fling all flakes
        for (const f of flakes) {
            f.settled = false;
            f.vx += (Math.random() - 0.5) * 180;
            f.vy += -Math.random() * 250;
        }
    }

    function setSnowCount(n) {
        cfg.snowCount = n;
        spawnFlakes(n);
        initAccum();
    }

    function setTimeOfDay(h) {
        cfg.timeOfDay = h;
        buildLighting(h);
    }

    function setScene(name) {
        cfg.scene = name;
        buildScene(name);
        initAccum();
        parallaxTargX = parallaxTargY = 0;
    }

    function destroy() {
        if (animId) cancelAnimationFrame(animId);
        if (canvas) {
            canvas.removeEventListener('mousedown', onMouseDown);
            canvas.removeEventListener('mousemove', onMouseMove);
            canvas.removeEventListener('mouseup', onMouseUp);
            canvas.removeEventListener('mouseleave', onMouseUp);
            canvas.removeEventListener('touchstart', onTouchStart);
            canvas.removeEventListener('touchmove', onTouchMove);
            canvas.removeEventListener('touchend', onMouseUp);
        }
    }

    return { init, shake, setSnowCount, setTimeOfDay, setScene, destroy };
})();