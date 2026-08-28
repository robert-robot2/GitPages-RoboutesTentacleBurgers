// SpectralWebGPUParticle.js
// WebGPU Billboard Particle Renderer — SpectralX Engine
//
// Drop-in performance upgrade for SpectralParticleSystem.js.
// Renders weather particle groups (Rain, Snow, Cloud, Lightning) on a
// transparent WebGPU overlay canvas positioned above SpectralX-Viewport,
// using GPU instancing: one draw call per weather type per frame.
//
// ── Performance gains over the WebGL path ────────────────────────────────────
//  • writeBuffer replaces three bufferSubData calls per group per frame
//  • Bind groups are cached per texKey — no per-frame object creation after warmup
//  • Single command encoder per frame; all groups batched before submit
//  • Async pipeline compilation avoids first-frame stall
//
// ── Trade-off ─────────────────────────────────────────────────────────────────
// The overlay canvas cannot share the WebGL depth buffer, so particles will
// not depth-test against 3D meshes. For atmospheric weather effects (rain,
// snow, clouds, lightning) this is fine — they are naturally drawn on top.
// For in-world particles that need depth occlusion keep SpectralParticleSystem.
//
// ── Fallback ──────────────────────────────────────────────────────────────────
// If WebGPU is unavailable (Firefox, older Safari) isAvailable() returns false
// and SpectralEngine.js automatically falls back to SpectralParticleSystem.
//
// ── See integration snippet at the bottom of this file ───────────────────────

window.SpectralWebGPUParticle = (function () {

    // =========================================================================
    // CONSTANTS
    // =========================================================================

    // GPU buffer layout for one particle instance:
    //   [worldX, worldY, worldZ,  r, g, b, a,  size]
    //    float    float   float   f  f  f  f    float
    //   = 8 floats × 4 bytes = 32 bytes
    const INSTANCE_STRIDE = 32;
    const INSTANCE_FLOATS = 8;   // floats per instance in the staging array

    // Uniform buffer layout (std140-compatible):
    //   offset  0 — mat4x4<f32>  vp       (64 bytes)
    //   offset 64 — vec4<f32>    camRight  (16 bytes, .xyz used)
    //   offset 80 — vec4<f32>    camUp     (16 bytes, .xyz used)
    //   total: 96 bytes
    const UB_BYTES = 96;
    const UB_FLOATS = UB_BYTES / 4;

    // Overlay canvas sits between the GL scene (z implicit) and the
    // text renderer overlay which uses z-index 10.
    const CANVAS_Z = 9;

    // How many instances to pre-allocate per group before growing.
    // Sized above the C# pool maximums: Rain=400, Snow=300, Cloud=296, Lightning=4.
    const PREALLOCATE_PER_GROUP = 512;

    // =========================================================================
    // STATE
    // =========================================================================
    let _device = null;
    let _canvas = null;
    let _ctx = null;
    let _format = null;
    let _pipeline = null;
    let _sampler = null;
    let _bgl = null;      // GPUBindGroupLayout (uniform + texture + sampler)
    let _uniformBuf = null;      // single shared UB, written every frame
    let _quadVB = null;      // 6-vertex interleaved quad, never changes

    let _initialized = false;
    let _initStarted = false;

    // Per particle-type resources, keyed by group.texKey string
    const _textures = {};   // texKey → GPUTexture
    const _texViews = {};   // texKey → GPUTextureView
    const _texLoading = {};   // texKey → true while async load is in flight
    const _instBufs = {};   // texKey → GPUBuffer  (instance data, COPY_DST | VERTEX)
    const _instCaps = {};   // texKey → current capacity in instances
    const _staging = {};   // texKey → Float32Array staging (JS-side write buffer)
    const _bindGroups = {};   // texKey → GPUBindGroup  (cached, rebuilt only on tex load)

    // =========================================================================
    // WGSL SHADER
    // =========================================================================
    const WGSL = /* wgsl */`

struct Uniforms {
    vp       : mat4x4<f32>,   // view-projection (WebGL column-major convention)
    camRight : vec4<f32>,     // world-space camera right vector  (.xyz)
    camUp    : vec4<f32>,     // world-space camera up vector     (.xyz)
};

@group(0) @binding(0) var<uniform> u   : Uniforms;
@group(0) @binding(1) var          tex : texture_2d<f32>;
@group(0) @binding(2) var          smp : sampler;

// ── Per-vertex (6 verts for the billboard quad) ──────────────────────────────
// ── Per-instance (N particles) ───────────────────────────────────────────────
struct VIn {
    // vertex buffer 0 — interleaved quad (stride 16)
    @location(0) quadXY   : vec2<f32>,   // local billboard offset [-0.5, 0.5]
    @location(1) uv       : vec2<f32>,   // texture coordinates

    // vertex buffer 1 — instance data (stride 32, stepMode = instance)
    @location(2) worldPos : vec3<f32>,   // particle world position
    @location(3) color    : vec4<f32>,   // rgba tint (usually 1,1,1,opacity)
    @location(4) size     : f32,         // world-space particle half-extent
}

struct VOut {
    @builtin(position) pos   : vec4<f32>,
    @location(0)       uv    : vec2<f32>,
    @location(1)       color : vec4<f32>,
}

@vertex
fn vs_main(i : VIn) -> VOut {
    // Billboard: offset the quad corners in camera right/up world directions
    let offset   = i.quadXY.x * u.camRight.xyz * i.size
                 + i.quadXY.y * u.camUp.xyz    * i.size;
    var clip     = u.vp * vec4<f32>(i.worldPos + offset, 1.0);

    // Remap WebGL NDC depth [-w, w] → WebGPU NDC depth [0, w].
    // Required because C# Mat4.CreatePerspective uses the OpenGL convention.
    clip.z = clip.z * 0.5 + clip.w * 0.5;

    var o : VOut;
    o.pos   = clip;
    o.uv    = i.uv;
    o.color = i.color;
    return o;
}

@fragment
fn fs_main(i : VOut) -> @location(0) vec4<f32> {
    let t = textureSample(tex, smp, i.uv);
    // Combine texture alpha with per-particle opacity
    let a = t.a * i.color.a;
    if (a < 0.01) { discard; }

    let rgb = t.rgb * i.color.rgb;

    // Premultiplied alpha — required because the overlay canvas is configured
    // with alphaMode = 'premultiplied'. The compositor expects (rgb*a, a).
    return vec4<f32>(rgb * a, a);
}
`;

    // =========================================================================
    // PRIVATE — overlay canvas
    // =========================================================================
    function _createCanvas() {
        if (_canvas && _canvas.parentElement) {
            _canvas.parentElement.removeChild(_canvas);
        }

        const glCanvas = document.getElementById('SpectralX-Viewport');
        if (!glCanvas) {
            console.error('[WebGPU:Particles] SpectralX-Viewport not found');
            return false;
        }

        _canvas = document.createElement('canvas');
        _canvas.id = 'SpectralX-WebGPU-Particles';
        _canvas.width = glCanvas.width;
        _canvas.height = glCanvas.height;

        Object.assign(_canvas.style, {
            position: 'absolute',
            top: '0',
            left: '0',
            width: '100%',
            height: '100%',
            pointerEvents: 'none',
            zIndex: String(CANVAS_Z),
        });

        const parent = glCanvas.parentElement || document.body;
        if (getComputedStyle(parent).position === 'static') {
            parent.style.position = 'relative';
        }
        parent.appendChild(_canvas);

        _ctx = _canvas.getContext('webgpu');
        _ctx.configure({
            device: _device,
            format: _format,
            alphaMode: 'premultiplied',
        });

        console.log('[WebGPU:Particles] Overlay canvas ready',
            _canvas.width + 'x' + _canvas.height);
        return true;
    }

    // =========================================================================
    // PRIVATE — texture loading
    // =========================================================================
    function _loadTexture(texKey) {
        if (_texLoading[texKey]) return;
        _texLoading[texKey] = true;

        // texKey format: "ParticleGeo_/iAssets/RainDrop01.png"
        const url = texKey.startsWith('ParticleGeo_')
            ? texKey.slice('ParticleGeo_'.length)
            : texKey;

        fetch(url)
            .then(r => r.blob())
            .then(b => createImageBitmap(b, { premultiplyAlpha: 'none' }))
            .then(bmp => {
                if (!_device) {
                    // Device was lost during load — mark for retry
                    _texLoading[texKey] = false;
                    return;
                }

                const gpuTex = _device.createTexture({
                    label: 'PTex:' + texKey,
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

                _textures[texKey] = gpuTex;
                _texViews[texKey] = gpuTex.createView();
                _texLoading[texKey] = false;

                // Invalidate any cached bind group so it is rebuilt with the real texture
                delete _bindGroups[texKey];

                console.log('[WebGPU:Particles] Texture ready:', url,
                    bmp.width + 'x' + bmp.height);
            })
            .catch(err => {
                console.warn('[WebGPU:Particles] Texture failed:', url, err);
                _texLoading[texKey] = false;
            });
    }

    // =========================================================================
    // PRIVATE — instance buffer management
    // =========================================================================

    // Ensures _instBufs[texKey] has capacity for at least `count` instances.
    // Grows by doubling if needed (avoids per-frame reallocation at particle count peaks).
    function _ensureInstBuf(texKey, count) {
        if ((_instCaps[texKey] || 0) >= count) return;

        if (_instBufs[texKey]) _instBufs[texKey].destroy();

        const cap = Math.max(count, PREALLOCATE_PER_GROUP);

        _instBufs[texKey] = _device.createBuffer({
            label: 'PInst:' + texKey,
            size: cap * INSTANCE_STRIDE,
            usage: GPUBufferUsage.VERTEX | GPUBufferUsage.COPY_DST,
        });

        _instCaps[texKey] = cap;
        _staging[texKey] = new Float32Array(cap * INSTANCE_FLOATS);
    }

    // =========================================================================
    // PRIVATE — bind group cache
    // =========================================================================

    // Returns a cached GPUBindGroup for the given texKey, or null if the
    // texture isn't ready yet. Cached objects are reused across frames;
    // invalidated when a texture finishes loading (_loadTexture deletes the key).
    function _getBindGroup(texKey) {
        if (_bindGroups[texKey]) return _bindGroups[texKey];
        if (!_texViews[texKey]) return null;

        _bindGroups[texKey] = _device.createBindGroup({
            label: 'PBG:' + texKey,
            layout: _bgl,
            entries: [
                { binding: 0, resource: { buffer: _uniformBuf } },
                { binding: 1, resource: _texViews[texKey] },
                { binding: 2, resource: _sampler },
            ],
        });

        return _bindGroups[texKey];
    }

    // =========================================================================
    // PUBLIC — init
    // =========================================================================
    async function init() {
        if (_initStarted) return;
        _initStarted = true;

        if (!navigator.gpu) {
            console.warn('[WebGPU:Particles] navigator.gpu unavailable — falling back to WebGL');
            return;
        }

        try {
            // ── Fast path: device + pipeline still alive, just recreate the canvas ──
            // Happens after in-page scene switches where SpectralEngine.js re-inits.
            if (_device && _pipeline && _bgl && _sampler && _uniformBuf && _quadVB) {
                if (_createCanvas()) {
                    _initialized = true;
                    console.log('[WebGPU:Particles] Resumed — overlay canvas recreated');
                }
                return;
            }

            // ── Full init ─────────────────────────────────────────────────────────
            const adapter = await navigator.gpu.requestAdapter({
                powerPreference: 'high-performance',
            });
            if (!adapter) {
                console.warn('[WebGPU:Particles] No GPU adapter — falling back to WebGL');
                return;
            }

            _device = await adapter.requestDevice({ label: 'SpectralX-Particles' });
            _device.lost.then(info => {
                console.warn('[WebGPU:Particles] Device lost:', info.reason, info.message);
                _device = null;
                _initialized = false;
                _initStarted = false;
            });

            _format = navigator.gpu.getPreferredCanvasFormat();

            // ── Sampler ──────────────────────────────────────────────────────────
            _sampler = _device.createSampler({
                label: 'PTxSampler',
                magFilter: 'linear',
                minFilter: 'linear',
                addressModeU: 'clamp-to-edge',
                addressModeV: 'clamp-to-edge',
            });

            // ── Bind group layout ────────────────────────────────────────────────
            _bgl = _device.createBindGroupLayout({
                label: 'PTxBGL',
                entries: [
                    {
                        binding: 0,
                        visibility: GPUShaderStage.VERTEX | GPUShaderStage.FRAGMENT,
                        buffer: { type: 'uniform', minBindingSize: UB_BYTES },
                    },
                    {
                        binding: 1,
                        visibility: GPUShaderStage.FRAGMENT,
                        texture: { sampleType: 'float' },
                    },
                    {
                        binding: 2,
                        visibility: GPUShaderStage.FRAGMENT,
                        sampler: { type: 'filtering' },
                    },
                ],
            });

            // ── Pipeline ─────────────────────────────────────────────────────────
            const module = _device.createShaderModule({ label: 'PTxWGSL', code: WGSL });

            const pipeDesc = {
                label: 'PTxPipeline',
                layout: _device.createPipelineLayout({ bindGroupLayouts: [_bgl] }),

                vertex: {
                    module,
                    entryPoint: 'vs_main',
                    buffers: [
                        // Buffer 0 — per-vertex quad (stride 16 bytes)
                        {
                            arrayStride: 16,
                            stepMode: 'vertex',
                            attributes: [
                                { shaderLocation: 0, offset: 0, format: 'float32x2' }, // quadXY
                                { shaderLocation: 1, offset: 8, format: 'float32x2' }, // uv
                            ],
                        },
                        // Buffer 1 — per-instance particle data (stride 32 bytes)
                        {
                            arrayStride: INSTANCE_STRIDE,
                            stepMode: 'instance',
                            attributes: [
                                { shaderLocation: 2, offset: 0, format: 'float32x3' }, // worldPos
                                { shaderLocation: 3, offset: 12, format: 'float32x4' }, // color
                                { shaderLocation: 4, offset: 28, format: 'float32' }, // size
                            ],
                        },
                    ],
                },

                fragment: {
                    module,
                    entryPoint: 'fs_main',
                    targets: [{
                        format: _format,
                        blend: {
                            // Standard premultiplied-alpha compositing
                            color: { srcFactor: 'one', dstFactor: 'one-minus-src-alpha', operation: 'add' },
                            alpha: { srcFactor: 'one', dstFactor: 'one-minus-src-alpha', operation: 'add' },
                        },
                    }],
                },

                // No depth/stencil — overlay canvas has no depth attachment.
                primitive: { topology: 'triangle-list', cullMode: 'none' },
            };

            // createRenderPipelineAsync avoids blocking the main thread on shader compile
            _pipeline = _device.createRenderPipelineAsync
                ? await _device.createRenderPipelineAsync(pipeDesc)
                : _device.createRenderPipeline(pipeDesc);

            // ── Shared quad vertex buffer ─────────────────────────────────────────
            // 6 vertices × 4 floats (quadX, quadY, uvX, uvY) = 96 bytes
            // BL, BR, TR  /  BL, TR, TL  (two CCW triangles)
            //
            //  quadXY       UV
            //  (-0.5,-0.5)  (0,1)   bottom-left
            //  ( 0.5,-0.5)  (1,1)   bottom-right
            //  ( 0.5, 0.5)  (1,0)   top-right
            //  (-0.5,-0.5)  (0,1)
            //  ( 0.5, 0.5)  (1,0)
            //  (-0.5, 0.5)  (0,0)   top-left
            const quadData = new Float32Array([
                -0.5, -0.5, 0.0, 1.0,
                0.5, -0.5, 1.0, 1.0,
                0.5, 0.5, 1.0, 0.0,
                -0.5, -0.5, 0.0, 1.0,
                0.5, 0.5, 1.0, 0.0,
                -0.5, 0.5, 0.0, 0.0,
            ]);

            _quadVB = _device.createBuffer({
                label: 'QuadVB',
                size: quadData.byteLength,
                usage: GPUBufferUsage.VERTEX | GPUBufferUsage.COPY_DST,
                mappedAtCreation: true,
            });
            new Float32Array(_quadVB.getMappedRange()).set(quadData);
            _quadVB.unmap();

            // ── Shared uniform buffer ─────────────────────────────────────────────
            _uniformBuf = _device.createBuffer({
                label: 'PTxUB',
                size: UB_BYTES,
                usage: GPUBufferUsage.UNIFORM | GPUBufferUsage.COPY_DST,
            });

            if (!_createCanvas()) return;

            _initialized = true;
            console.log('[WebGPU:Particles] Renderer ready —', _format,
                _canvas.width + 'x' + _canvas.height);

        } catch (err) {
            console.error('[WebGPU:Particles] Init error:', err);
            _initStarted = false; // allow retry
        }
    }

    // =========================================================================
    // PUBLIC — render
    // Called once per animation frame from SpectralEngine.js renderFrame().
    // =========================================================================
    function render(frame) {
        if (!_initialized || !_device || !_ctx || !_pipeline) return;

        // ── Re-attach canvas if Blazor's DOM diff removed it ─────────────────
        if (_canvas && !document.body.contains(_canvas)) {
            const glCanvas = document.getElementById('SpectralX-Viewport');
            if (glCanvas?.parentElement) {
                glCanvas.parentElement.appendChild(_canvas);
            }
        }

        // getCurrentTexture() must be called exactly once per frame
        const swapView = _ctx.getCurrentTexture().createView();

        const encoder = _device.createCommandEncoder({ label: 'PTxFrame' });

        const pass = encoder.beginRenderPass({
            colorAttachments: [{
                view: swapView,
                clearValue: { r: 0, g: 0, b: 0, a: 0 }, // transparent
                loadOp: 'clear',
                storeOp: 'store',
            }],
        });

        const groups = frame.particleInstances;
        if (!groups || groups.length === 0) {
            // No particles this frame — end pass (canvas already cleared above)
            pass.end();
            _device.queue.submit([encoder.finish()]);
            return;
        }

        // ── Write VP + camera vectors into the shared uniform buffer ──────────
        // Layout: mat4x4 vp (floats 0-15) | vec4 camRight (16-19) | vec4 camUp (20-23)
        const ubData = new Float32Array(UB_FLOATS);
        ubData.set(frame.vp, 0);
        ubData[16] = frame.camRight[0];
        ubData[17] = frame.camRight[1];
        ubData[18] = frame.camRight[2];
        // ubData[19] = 0  — _pad, left as zero from Float32Array init
        ubData[20] = frame.camUp[0];
        ubData[21] = frame.camUp[1];
        ubData[22] = frame.camUp[2];
        // ubData[23] = 0  — _pad
        _device.queue.writeBuffer(_uniformBuf, 0, ubData);

        // ── Draw each particle group ──────────────────────────────────────────
        pass.setPipeline(_pipeline);
        pass.setVertexBuffer(0, _quadVB);

        for (const group of groups) {
            if (!group || group.count <= 0) continue;

            const { texKey, count, offsets, colors, sizes } = group;

            // Kick off async texture load on first encounter; skip draw until ready
            if (!_texViews[texKey]) {
                _loadTexture(texKey);
                continue;
            }

            // Get or build cached bind group for this texture
            const bg = _getBindGroup(texKey);
            if (!bg) continue;

            // Ensure the instance buffer is large enough for this frame's count
            _ensureInstBuf(texKey, count);

            // Pack frame data into the staging Float32Array
            // Layout per instance: [wx, wy, wz,  r, g, b, a,  size]
            const stage = _staging[texKey];
            for (let i = 0; i < count; i++) {
                const dst = i * INSTANCE_FLOATS;
                const o3 = i * 3;
                const c4 = i * 4;
                stage[dst] = offsets[o3];
                stage[dst + 1] = offsets[o3 + 1];
                stage[dst + 2] = offsets[o3 + 2];
                stage[dst + 3] = colors[c4];
                stage[dst + 4] = colors[c4 + 1];
                stage[dst + 5] = colors[c4 + 2];
                stage[dst + 6] = colors[c4 + 3];
                stage[dst + 7] = sizes[i];
            }

            // Upload only the live portion of the staging array
            _device.queue.writeBuffer(
                _instBufs[texKey],
                0,
                stage,
                0,
                count * INSTANCE_FLOATS,
            );

            pass.setBindGroup(0, bg);
            pass.setVertexBuffer(1, _instBufs[texKey]);

            // 6 quad vertices × count instances = count billboards, one draw call
            pass.draw(6, count, 0, 0);
        }

        pass.end();
        _device.queue.submit([encoder.finish()]);
    }

    // =========================================================================
    // PUBLIC — resize
    // Call from SpectralGLInterop.resizeCanvas() alongside the text system.
    // =========================================================================
    function resize(w, h) {
        if (!_canvas || !_device || !_ctx || !_format) return;
        if (w <= 0 || h <= 0) return;

        _canvas.width = w;
        _canvas.height = h;

        // WebGPU requires re-configure after any size change
        _ctx.configure({
            device: _device,
            format: _format,
            alphaMode: 'premultiplied',
        });

        console.log('[WebGPU:Particles] Overlay resized:', w + 'x' + h);
    }

    // =========================================================================
    // PUBLIC — reset
    // Call from SpectralGLInterop.resetParticles() or on scene switch.
    // Destroys all per-group GPU resources; device + pipeline are preserved
    // so the next init() call takes the fast path (canvas-only recreate).
    // =========================================================================
    function reset() {
        // Destroy per-group instance buffers and textures
        for (const k of Object.keys(_instBufs)) {
            try { _instBufs[k].destroy(); } catch (_) { }
            delete _instBufs[k];
        }
        for (const k of Object.keys(_textures)) {
            try { _textures[k].destroy(); } catch (_) { }
            delete _textures[k];
        }

        // Clear all lookup tables
        for (const k of Object.keys(_texViews)) delete _texViews[k];
        for (const k of Object.keys(_texLoading)) delete _texLoading[k];
        for (const k of Object.keys(_instCaps)) delete _instCaps[k];
        for (const k of Object.keys(_staging)) delete _staging[k];
        for (const k of Object.keys(_bindGroups)) delete _bindGroups[k];

        // Remove overlay canvas from DOM
        if (_canvas && _canvas.parentElement) {
            _canvas.parentElement.removeChild(_canvas);
        }
        _canvas = null;
        _ctx = null;

        // Allow init() to re-run; device + pipeline stay alive for fast-path reuse
        _initStarted = false;
        _initialized = false;

        console.log('[WebGPU:Particles] Reset — ready for reinit');
    }

    // =========================================================================
    // PUBLIC — isAvailable
    // =========================================================================
    function isAvailable() {
        return _initialized && !!_device;
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================
    return { init, render, resize, reset, isAvailable };

})();


// =============================================================================
// INTEGRATION SNIPPET — SpectralEngine.js changes (3 locations)
// =============================================================================
//
// ── 1. Add script tag in index.html (after SpectralWebGPUInterop.js) ─────────
//
//   <script src="SpectralWebGPUParticle.js"></script>
//
//
// ── 2. In init() — start particle WebGPU alongside the text WebGPU ───────────
//
//   // existing text WebGPU init:
//   if (window.SpectralWebGPUInterop) {
//       window.SpectralWebGPUInterop.init().catch(e =>
//           console.warn('[SpectralEngine] WebGPU text init failed:', e));
//   }
//
//   // ADD — particle WebGPU init:
//   if (window.SpectralWebGPUParticle) {
//       window.SpectralWebGPUParticle.init().catch(e =>
//           console.warn('[SpectralEngine] WebGPU particle init failed:', e));
//   }
//
//
// ── 3. In renderFrame() — replace the particle render call ───────────────────
//
//   // BEFORE (WebGL only):
//   window.SpectralParticleSystem.render(frame, _activeProgram);
//
//   // AFTER (WebGPU with WebGL fallback):
//   if (window.SpectralWebGPUParticle?.isAvailable()) {
//       window.SpectralWebGPUParticle.render(frame);
//   } else {
//       window.SpectralParticleSystem.render(frame, _activeProgram);
//   }
//
//
// ── 4. In resizeCanvas() — resize alongside the text WebGPU canvas ───────────
//
//   // existing:
//   if (window.SpectralWebGPUInterop?.isAvailable()) {
//       window.SpectralWebGPUInterop.resize(width, height);
//   }
//
//   // ADD:
//   if (window.SpectralWebGPUParticle?.isAvailable()) {
//       window.SpectralWebGPUParticle.resize(width, height);
//   }
//
//
// ── 5. In the public return object — reset alongside particles ────────────────
//
//   // BEFORE:
//   resetParticles: () => window.SpectralParticleSystem.reset,
//
//   // AFTER:
//   resetParticles: () => {
//       window.SpectralParticleSystem.reset();
//       window.SpectralWebGPUParticle?.reset();
//   },
//
// =============================================================================