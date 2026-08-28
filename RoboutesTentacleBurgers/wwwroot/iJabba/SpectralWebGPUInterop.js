// SpectralWebGPUInterop.js
// WebGPU Hybrid Text Renderer — SpectralX Engine
//
// Renders SDF text meshes on a transparent WebGPU overlay canvas
// positioned exactly above SpectralX-Viewport.
// Falls back silently to SpectralTextRenderSystem if WebGPU is unavailable.
//
// Browser requirement: Chrome 113+, Edge 113+, Firefox Nightly (flag).
// Safari: WebGPU behind feature flag as of 2025.
//
// See integration instructions at the bottom of this file or in
// the accompanying instructions block.

window.SpectralWebGPUInterop = (function () {

    // ─────────────────────────────────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────────────────────────────────
    let _device = null;
    let _gpuCanvas = null;
    let _context = null;
    let _format = null;
    let _pipeline = null;
    let _sampler = null;
    let _bgl = null;        // GPUBindGroupLayout
    let _initialized = false;
    let _initStarted = false;

    // Atlas resources — keyed by fontKey (string)
    const _atlasJson = {};       // { fontKey: { glyphs[], metrics, atlas } }
    const _atlasGpuTex = {};       // { fontKey: GPUTexture }
    const _atlasView = {};       // { fontKey: GPUTextureView }
    const _atlasLoading = {};       // { fontKey: true } while fetch is in-flight

    // Per-mesh GPU vertex buffers — keyed by meshId (string)
    const _vbufs = {};            // position buffers  (float32x3)
    const _ubufs = {};            // texcoord buffers  (float32x2)
    const _vcounts = {};            // vertex draw count
    const _lastTxt = {};            // last rendered string (rebuild sentinel)

    // ─── Uniform struct layout (must match WGSL Uniforms exactly) ───────────
    //  bytes [  0.. 63]  mat4x4<f32>  mvp            (16 floats, col-major)
    //  bytes [ 64.. 79]  vec4<f32>    color           ( 4 floats)
    //  bytes [ 80.. 95]  vec4<f32>    outlineColor    ( 4 floats)
    //  bytes [ 96..111]  vec4<f32>    params          ( 4 floats)
    //                                  x = outlineWidth
    //                                  y = softness
    //                                  z = glowRadius
    //                                  w = glowStrength
    //  Total: 112 bytes  (4-byte aligned, 16-byte struct alignment ✓)
    const UB_BYTES = 112;
    const UB_FLOATS = 28;           // 112 / 4

    // ─────────────────────────────────────────────────────────────────────────
    // WGSL SHADER
    // ─────────────────────────────────────────────────────────────────────────
    const WGSL = /* wgsl */`

struct Uniforms {
    mvp          : mat4x4<f32>,
    color        : vec4<f32>,
    outlineColor : vec4<f32>,
    params       : vec4<f32>,   // x=outlineWidth  y=softness  z=glowRadius  w=glowStrength
};

@group(0) @binding(0) var<uniform> u   : Uniforms;
@group(0) @binding(1) var          atl : texture_2d<f32>;
@group(0) @binding(2) var          smp : sampler;

struct VSIn {
    @location(0) pos : vec3<f32>,
    @location(1) uv  : vec2<f32>,
}
struct VSOut {
    @builtin(position) pos : vec4<f32>,
    @location(0)       uv  : vec2<f32>,
}

fn med3(r : f32, g : f32, b : f32) -> f32 {
    return max(min(r, g), min(max(r, g), b));
}

@vertex
fn vs_main(i : VSIn) -> VSOut {
    var o : VSOut;
    var c = u.mvp * vec4<f32>(i.pos, 1.0);
    // Remap WebGL clip-depth [-w, w] → WebGPU [0, w]
    // C# Mat4.CreatePerspective produces a WebGL-style projection matrix.
    c.z  = c.z * 0.5 + c.w * 0.5;
    o.pos = c;
    o.uv  = i.uv;
    return o;
}

@fragment
fn fs_main(i : VSOut) -> @location(0) vec4<f32> {
    let s    = textureSample(atl, smp, i.uv).rgb;
    let d    = med3(s.r, s.g, s.b);
    let fw   = length(vec2<f32>(dpdx(d), dpdy(d))) * 0.5;  // edge pixel width
    let ow   = u.params.x;
    let sf   = u.params.y;
    let gr   = u.params.z;
    let gs   = u.params.w;

    let alp = smoothstep(0.5 - fw - sf, 0.5 + fw + sf, d);
    if (alp < 0.001) { discard; }

    var rgb : vec3<f32>;
    var a   : f32;

    if (ow > 0.0) {
        // Outline: blend outline ring into text fill
        let oa = smoothstep(0.5 - ow - fw, 0.5 - ow + fw, d);
        let oc = vec4<f32>(u.outlineColor.rgb, u.outlineColor.a * oa);
        let tc = vec4<f32>(u.color.rgb, u.color.a * alp);
        let bl = mix(oc, tc, alp);
        rgb    = bl.rgb;
        a      = bl.a;
    } else {
        rgb = u.color.rgb;
        a   = u.color.a * alp;
    }

    // Soft glow halo just outside the SDF edge
    let ga   = smoothstep(0.5 - gr, 0.5 + gr * 0.5, d);
    let glow = (1.0 - alp) * ga * gs;
    rgb      = mix(rgb, u.color.rgb, glow);
    a        = max(a, glow * u.color.a);

    // Premultiply — required for WebGPU alphaMode 'premultiplied'.
    // The canvas compositor expects (RGB*A, A) from the texture.
    return vec4<f32>(rgb * a, a);
}
`;

    // ─── Private helper — (re)creates the transparent overlay canvas ─────────────
    function _recreateOverlayCanvas() {
        // Remove stale overlay from DOM (if any)
        if (_gpuCanvas && _gpuCanvas.parentElement) {
            _gpuCanvas.parentElement.removeChild(_gpuCanvas);
        }

        const glCanvas = document.getElementById('SpectralX-Viewport');
        if (!glCanvas) {
            console.error('[WebGPU] SpectralX-Viewport not found — cannot create overlay');
            return false;
        }

        _gpuCanvas = document.createElement('canvas');
        _gpuCanvas.id = 'SpectralX-WebGPU-Text';
        _gpuCanvas.width = glCanvas.width;
        _gpuCanvas.height = glCanvas.height;
        Object.assign(_gpuCanvas.style, {
            position: 'absolute', top: '0', left: '0',
            width: '100%', height: '100%',
            pointerEvents: 'none', zIndex: '10',
        });

        const parent = glCanvas.parentElement || document.body;
        if (getComputedStyle(parent).position === 'static') parent.style.position = 'relative';
        parent.appendChild(_gpuCanvas);

        _context = _gpuCanvas.getContext('webgpu');
        _context.configure({ device: _device, format: _format, alphaMode: 'premultiplied' });

        console.log('[WebGPU] Overlay canvas created', _gpuCanvas.width + 'x' + _gpuCanvas.height);
        return true;
    }



    // ─────────────────────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────────────────────
    async function init() {
        if (_initStarted) return;
        _initStarted = true;

        if (!navigator.gpu) {
            console.warn('[WebGPU] navigator.gpu unavailable — text will use WebGL fallback');
            return;
        }

        try {
            // ── Fast path: device + pipeline still valid, just need a new overlay canvas ──
            // Happens on page redirect after SpectralEngine.js calls reset() + init() again.
            if (_device && _pipeline && _bgl && _sampler) {
                if (_recreateOverlayCanvas()) {
                    _initialized = true;
                    console.log('[WebGPU] Text renderer resumed — overlay canvas recreated');
                }
                return;
            }

            // ── Full init (first load or after device lost) ──────────────────────────────
            const adapter = await navigator.gpu.requestAdapter({ powerPreference: 'high-performance' });
            if (!adapter) {
                console.warn('[WebGPU] No GPU adapter returned — text will use WebGL fallback');
                return;
            }

            _device = await adapter.requestDevice({ label: 'SpectralX-TextRenderer' });
            _device.lost.then(info => {
                console.warn('[WebGPU] Device lost:', info.reason, info.message);
                _device = null;
                _initialized = false;
                _initStarted = false;
            });

            _format = navigator.gpu.getPreferredCanvasFormat();

            _sampler = _device.createSampler({
                label: 'SDFAtlasSampler',
                magFilter: 'linear', minFilter: 'linear',
                addressModeU: 'clamp-to-edge', addressModeV: 'clamp-to-edge',
            });

            _bgl = _device.createBindGroupLayout({
                label: 'SpectralTextBGL',
                entries: [
                    { binding: 0, visibility: GPUShaderStage.VERTEX | GPUShaderStage.FRAGMENT, buffer: { type: 'uniform', minBindingSize: UB_BYTES } },
                    { binding: 1, visibility: GPUShaderStage.FRAGMENT, texture: { sampleType: 'float' } },
                    { binding: 2, visibility: GPUShaderStage.FRAGMENT, sampler: { type: 'filtering' } },
                ],
            });

            const module = _device.createShaderModule({ label: 'SpectralTextWGSL', code: WGSL });

            const pipelineDesc = {
                label: 'SpectralTextPipeline',
                layout: _device.createPipelineLayout({ bindGroupLayouts: [_bgl] }),
                vertex: {
                    module, entryPoint: 'vs_main',
                    buffers: [
                        { arrayStride: 12, attributes: [{ shaderLocation: 0, offset: 0, format: 'float32x3' }] },
                        { arrayStride: 8, attributes: [{ shaderLocation: 1, offset: 0, format: 'float32x2' }] },
                    ],
                },
                fragment: {
                    module, entryPoint: 'fs_main',
                    targets: [{
                        format: _format,
                        blend: {
                            color: { srcFactor: 'one', dstFactor: 'one-minus-src-alpha', operation: 'add' },
                            alpha: { srcFactor: 'one', dstFactor: 'one-minus-src-alpha', operation: 'add' },
                        },
                    }],
                },
                primitive: { topology: 'triangle-list', cullMode: 'none' },
            };

            _pipeline = _device.createRenderPipelineAsync
                ? await _device.createRenderPipelineAsync(pipelineDesc)
                : _device.createRenderPipeline(pipelineDesc);

            if (!_recreateOverlayCanvas()) return;   // ← use shared helper

            _initialized = true;
            console.log('[WebGPU] Text renderer ready —', _format, _gpuCanvas.width + 'x' + _gpuCanvas.height);

        } catch (err) {
            console.error('[WebGPU] Init error:', err);
            _initStarted = false;   // allow retry on next init call
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ATLAS LOADING
    // Fetches and uploads the MSDF/SDF font atlas as a GPUTexture.
    // Async — render will skip the font until it's ready.
    // ─────────────────────────────────────────────────────────────────────────
    function _loadAtlas(fontKey, jsonUrl, texUrl) {
        if (_atlasLoading[fontKey]) return;
        _atlasLoading[fontKey] = true;

        Promise.all([
            fetch(jsonUrl).then(r => r.json()),
            fetch(texUrl)
                .then(r => r.blob())
                .then(b => createImageBitmap(b, { premultiplyAlpha: 'none' })),
        ])
            .then(([json, bmp]) => {
                if (!_device) { _atlasLoading[fontKey] = false; return; }  // device lost during load

                _atlasJson[fontKey] = json;

                const gpuTex = _device.createTexture({
                    label: 'SDF:' + fontKey,
                    size: [bmp.width, bmp.height, 1],
                    format: 'rgba8unorm',
                    usage: GPUTextureUsage.TEXTURE_BINDING
                        | GPUTextureUsage.COPY_DST
                        | GPUTextureUsage.RENDER_ATTACHMENT,
                });

                _device.queue.copyExternalImageToTexture(
                    { source: bmp, flipY: false },
                    { texture: gpuTex, premultipliedAlpha: false },
                    [bmp.width, bmp.height],
                );

                _atlasGpuTex[fontKey] = gpuTex;
                _atlasView[fontKey] = gpuTex.createView();
                _atlasLoading[fontKey] = false;
                console.log('[WebGPU] Atlas ready:', fontKey,
                    bmp.width + 'x' + bmp.height, '—', json.glyphs.length, 'glyphs');
            })
            .catch(err => {
                console.error('[WebGPU] Atlas load failed:', fontKey, err);
                _atlasLoading[fontKey] = false;
            });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GEOMETRY BUILDER
    // Mirrors SpectralTextSystem.buildTextGeometry but returns raw Float32Arrays
    // instead of uploading to WebGL buffers. The two systems are independent.
    // ─────────────────────────────────────────────────────────────────────────
    function _buildGeo(text, json, fontSize, spacing, align) {
        const gmap = {};
        for (const g of json.glyphs) gmap[g.unicode] = g;

        const scale = fontSize / json.metrics.emSize;
        const aw = json.atlas.width;
        const ah = json.atlas.height;
        const sp = spacing || 0;

        // First pass: measure total advance for alignment
        let totalW = 0;
        for (const ch of text) {
            const g = gmap[ch.charCodeAt(0)];
            totalW += g ? (g.advance + sp) * scale : fontSize * 0.3;
        }
        let ox = align === 1 ? -totalW * 0.5
            : align === 2 ? -totalW
                : 0;

        const vs = [], uvs = [];

        for (const ch of text) {
            const g = gmap[ch.charCodeAt(0)];
            if (!g?.planeBounds) {
                ox += g ? g.advance * scale : fontSize * 0.3;
                continue;
            }

            const pb = g.planeBounds;
            const ab = g.atlasBounds;

            const x0 = ox + pb.left * scale;
            const x1 = ox + pb.right * scale;
            const y0 = pb.bottom * scale;
            const y1 = pb.top * scale;

            const u0 = ab.left / aw;
            const u1 = ab.right / aw;
            const v0 = 1.0 - ab.top / ah;
            const v1 = 1.0 - ab.bottom / ah;

            // Two CCW triangles forming the glyph quad
            vs.push(x0, y0, 0, x1, y0, 0, x1, y1, 0,
                x0, y0, 0, x1, y1, 0, x0, y1, 0);
            uvs.push(u0, v0, u1, v0, u1, v1,
                u0, v0, u1, v1, u0, v1);

            ox += (g.advance + sp) * scale;
        }

        if (!vs.length) return null;
        return {
            verts: new Float32Array(vs),
            uvs: new Float32Array(uvs),
            count: vs.length / 3,
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GPU BUFFER HELPERS
    // ─────────────────────────────────────────────────────────────────────────
    function _makeVB(data) {
        // Vertex buffers created with mappedAtCreation for one-shot upload
        const size = Math.max(Math.ceil(data.byteLength / 4) * 4, 16);
        const buf = _device.createBuffer({
            label: 'TextVB',
            size,
            usage: GPUBufferUsage.VERTEX,
            mappedAtCreation: true,
        });
        new Float32Array(buf.getMappedRange()).set(data);
        buf.unmap();
        return buf;
    }

    function _makeUB() {
        // Uniform buffers written via queue.writeBuffer each draw call.
        // GPUBufferUsage.COPY_DST is required for writeBuffer.
        return _device.createBuffer({
            label: 'TextUB',
            size: UB_BYTES,
            usage: GPUBufferUsage.UNIFORM | GPUBufferUsage.COPY_DST,
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MESH REBUILD
    // ─────────────────────────────────────────────────────────────────────────
    function _rebuildMesh(tm) {
        if (!_atlasJson[tm.fontKey]) return;

        const geo = _buildGeo(
            tm.text,
            _atlasJson[tm.fontKey],
            tm.fontSize,
            tm.letterSpacing,
            tm.align,
        );

        if (_vbufs[tm.meshId]) _vbufs[tm.meshId].destroy();
        if (_ubufs[tm.meshId]) _ubufs[tm.meshId].destroy();

        if (!geo) { _vcounts[tm.meshId] = 0; return; }

        _vbufs[tm.meshId] = _makeVB(geo.verts);
        _ubufs[tm.meshId] = _makeVB(geo.uvs);
        _vcounts[tm.meshId] = geo.count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SINGLE DRAW-CALL HELPER
    //
    // Creates a transient GPUBuffer + GPUBindGroup per call.
    // This is deliberately simple for a test system — acceptable overhead
    // for ≤20 text meshes at 60fps. For production, replace with a
    // ring-buffer pool (allocate N*UB_BYTES once, use dynamic offsets).
    //
    // dx/dy: optional MVP translation for shadow/glow offset passes.
    //        These are clip-space unit offsets applied to mvp[12]/mvp[13].
    // ─────────────────────────────────────────────────────────────────────────
    function _drawCall(pass, fontKey, vb, uvb, count, mvp, col, oc, params, dx, dy) {
        const data = new Float32Array(UB_FLOATS);

        // mvp → floats [0..15]
        data.set(mvp, 0);
        if (dx) data[12] += dx;
        if (dy) data[13] += dy;

        // color → floats [16..19]
        data[16] = col[0]; data[17] = col[1]; data[18] = col[2]; data[19] = col[3];

        // outlineColor → floats [20..23]
        data[20] = oc[0]; data[21] = oc[1]; data[22] = oc[2]; data[23] = oc[3];

        // params → floats [24..27]
        data[24] = params[0]; data[25] = params[1];
        data[26] = params[2]; data[27] = params[3];

        const ub = _makeUB();
        _device.queue.writeBuffer(ub, 0, data);
        // writeBuffer is a queue operation; it executes before the submit
        // that contains this render pass, so the GPU sees correct data. ✓

        const bg = _device.createBindGroup({
            layout: _bgl,
            entries: [
                { binding: 0, resource: { buffer: ub } },
                { binding: 1, resource: _atlasView[fontKey] },
                { binding: 2, resource: _sampler },
            ],
        });

        pass.setBindGroup(0, bg);
        pass.setVertexBuffer(0, vb);
        pass.setVertexBuffer(1, uvb);
        pass.draw(count);
        // ub is left for GC — acceptable here, use a pool for production
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RENDER  (called once per animation frame from SpectralEngine.js)
    // ─────────────────────────────────────────────────────────────────────────
    function render(frame) {
        if (!_initialized || !_device || !_context || !_pipeline) return;
        // ── Scene-switch / Blazor re-render guard ────────────────────────────────────
        // Blazor's virtual DOM diff can orphan the overlay canvas appended via JS.
        // Re-attach it to the correct parent if it has been detached.
        if (_gpuCanvas && !document.body.contains(_gpuCanvas)) {
            const glCanvas = document.getElementById('SpectralX-Viewport');
            if (glCanvas?.parentElement) {
                glCanvas.parentElement.appendChild(_gpuCanvas);
                console.log('[WebGPU] Overlay canvas re-attached after DOM patch');
            }
        }
        // getCurrentTexture() must be called exactly once per frame
        const swapView = _context.getCurrentTexture().createView();
        const encoder = _device.createCommandEncoder({ label: 'WGPUTextFrame' });

        const pass = encoder.beginRenderPass({
            colorAttachments: [{
                view: swapView,
                clearValue: { r: 0, g: 0, b: 0, a: 0 },  // transparent clear
                loadOp: 'clear',
                storeOp: 'store',
            }],
        });

        const meshes = frame.textMeshes;
        if (meshes?.length) {
            pass.setPipeline(_pipeline);

            for (const tm of meshes) {
                if (!tm.fontKey || !tm.jsonUrl || !tm.texUrl) continue;

                // Trigger async atlas load on first encounter; skip until ready
                if (!_atlasJson[tm.fontKey]) {
                    _loadAtlas(tm.fontKey, tm.jsonUrl, tm.texUrl);
                    continue;
                }
                if (!_atlasView[tm.fontKey]) continue; // texture still uploading

                // Rebuild geometry when C# marks text dirty or string changed
                const needsRebuild = tm.needsRebuild
                    || !_vbufs[tm.meshId]
                    || _lastTxt[tm.meshId] !== tm.text;

                if (needsRebuild) {
                    _rebuildMesh(tm);
                    _lastTxt[tm.meshId] = tm.text;
                }

                if (!_vbufs[tm.meshId] || !_vcounts[tm.meshId]) continue;

                const mvp = (tm.mvp instanceof Float32Array)
                    ? tm.mvp : new Float32Array(tm.mvp);

                const col = [tm.r, tm.g, tm.b, tm.a];
                const oc = [
                    tm.outlineR ?? 0, tm.outlineG ?? 0,
                    tm.outlineB ?? 0, tm.outlineA ?? 0,
                ];
                const params = [
                    tm.outlineWidth ?? 0,
                    0.05,                       // softness
                    tm.glowRadius ?? 0.25,
                    tm.glowStrength ?? 0.8,
                ];

                const vb = _vbufs[tm.meshId];
                const uvb = _ubufs[tm.meshId];
                const cnt = _vcounts[tm.meshId];

                // ── Shadow passes ─────────────────────────────────────────
                const blur = tm.shadowBlur ?? 0;
                if (blur > 0.01) {
                    const sc = [
                        tm.shadowR ?? 0, tm.shadowG ?? 0,
                        tm.shadowB ?? 0, tm.shadowA ?? 0.5,
                    ];
                    // 6-direction spread × 2 distance passes = 12 shadow draws
                    const DIRS = [[1, 0], [-1, 0], [0, 1], [0, -1], [0.71, 0.71], [-0.71, 0.71]];
                    const PASSES = [
                        { s: blur * 0.5, a: 0.5 },
                        { s: blur, a: 0.3 },
                    ];
                    const noGlow = [0, 0.10, 0, 0];  // no glow on shadow layer
                    for (const p of PASSES) {
                        for (const [dx, dy] of DIRS) {
                            _drawCall(pass, tm.fontKey, vb, uvb, cnt, mvp,
                                [sc[0], sc[1], sc[2], sc[3] * p.a],
                                [0, 0, 0, 0], noGlow,
                                dx * p.s, dy * p.s);
                        }
                    }
                }

                // ── Main text draw ────────────────────────────────────────
                _drawCall(pass, tm.fontKey, vb, uvb, cnt, mvp,
                    col, oc, params, 0, 0);
            }
        }

        pass.end();
        _device.queue.submit([encoder.finish()]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RESIZE
    // Must be called whenever the WebGL canvas changes size.
    // ─────────────────────────────────────────────────────────────────────────
    function resize(w, h) {
        if (!_gpuCanvas || !_device || !_context || !_format) return;
        if (w <= 0 || h <= 0) return;
        _gpuCanvas.width = w;
        _gpuCanvas.height = h;
        // Re-configure after resize — required by the WebGPU spec
        _context.configure({
            device: _device,
            format: _format,
            alphaMode: 'premultiplied',
        });
        console.log('[WebGPU] Overlay resized:', w + 'x' + h);
    }
    // Soft reset — keeps atlas + device, destroys only per-mesh vertex buffers.
    // Call on in-page scene switch so geometry is rebuilt without atlas reload delay.
    function resetMeshes() {
        for (const k of Object.keys(_vbufs)) { try { _vbufs[k].destroy(); } catch (_) { } delete _vbufs[k]; }
        for (const k of Object.keys(_ubufs)) { try { _ubufs[k].destroy(); } catch (_) { } delete _ubufs[k]; }
        for (const k of Object.keys(_vcounts)) delete _vcounts[k];
        for (const k of Object.keys(_lastTxt)) delete _lastTxt[k];
        console.log('[WebGPU] Mesh buffers cleared for scene switch');
    }
    // ─────────────────────────────────────────────────────────────────────────
    // RESET
    // Call on scene switch and whenever SpectralEngine.js re-inits the GL
    // context. Destroys all GPU-side resources so they are recreated fresh.
    // ─────────────────────────────────────────────────────────────────────────
    function reset() {
        // Per-mesh vertex/UV buffers
        for (const k of Object.keys(_vbufs)) { try { _vbufs[k].destroy(); } catch (_) { } delete _vbufs[k]; }
        for (const k of Object.keys(_ubufs)) { try { _ubufs[k].destroy(); } catch (_) { } delete _ubufs[k]; }

        // Atlas GPU textures
        for (const k of Object.keys(_atlasGpuTex)) { try { _atlasGpuTex[k].destroy(); } catch (_) { } delete _atlasGpuTex[k]; }
        for (const k of Object.keys(_atlasView)) delete _atlasView[k];
        for (const k of Object.keys(_atlasJson)) delete _atlasJson[k];
        for (const k of Object.keys(_atlasLoading)) delete _atlasLoading[k];
        for (const k of Object.keys(_vcounts)) delete _vcounts[k];
        for (const k of Object.keys(_lastTxt)) delete _lastTxt[k];

        // Remove overlay canvas from DOM and null the context
        if (_gpuCanvas && _gpuCanvas.parentElement) {
            _gpuCanvas.parentElement.removeChild(_gpuCanvas);
        }
        _gpuCanvas = null;
        _context = null;

        // ← KEY FIX: allow init() to re-run after GL context rebuild (page redirect)
        _initStarted = false;
        _initialized = false;

        console.log('[WebGPU] State fully reset — ready for reinit');
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────
    function isAvailable() {
        return _initialized && !!_device;
    }

    return { init, render, resize, reset, resetMeshes, isAvailable };

})();
