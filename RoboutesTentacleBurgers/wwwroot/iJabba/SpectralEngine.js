

// working scene 2 version
// but scene 3 has the ortho box shadow cast on tile map

// @ts-check
// SpectralEngine.js
// SpectralEngine.js
// SpectralEngine.js

// ============================================================
// SPECTRAL ENGINE — Shared Context
// Shell populated in init() after GL is ready.
// System files access shared state via window.SE
// ============================================================
window.SpectralEngine = {
    gl: null,
    canvas: null,
    textureCache: null,
    meshBuffers: null,
    textureReady: null,
    fullscreenQuadVbo: null,
    quadPosLocs: null,
    activeProgram: null,
    shadowDepthTexs: null,
    fxaaFbo: null,
    fxaaColorTex: null,
    smaaEdgeTex: null,    
    smaaBlendTex: null,   
    compileShader: null,
    buildProgram: null,
    drawQuad: null,
    spectralEdgeTex: null,   
    spectralV2EdgeTex: null, 
    spectralV3EdgeTex: null,  
    spectralV3LineTex: null, 
};

window.SE = window.SpectralEngine;


function isBWPScene(sceneId) {
    return sceneId >= 4 && sceneId <= 14;
}



window.SpectralGLInterop = (function () {

    // ============================================================
    // CORE ENGINE STATE
    // ============================================================
    let _canvas = null;
    let _gl = null;
    let _dotnetRef = null;
    let _animationHandle = null;
    let _initialized = false;

    // Shader programs
    let _programs = [null, null, null, null, null];
    let _activeProgram = null;
    let _activeLocs = null;
    let _programLocations = [];

    // Shared GPU resources — read by every system
    let _textureCache = {};
    let _meshBuffers = {};
    let _textureReady = {};
    let _pendingUploads = [];
    let _parsedMeshCache = {};

    // Fullscreen quad — shared by all AA + post passes
    let _fullscreenQuadVbo = null;
    let _quadPosLocs = {};

    // ISO CAMERA INPUT
    let _isoMouseX = -1;
    let _isoMouseY = -1;


    // Lights and Shadow Max
    const MAX_LIGHTS = 32;
    const MAX_SHADOW_LIGHTS = 8;

    // ============================================================
    // SHADOW SYSTEM
    // ============================================================
    let _shadowProgram = null;
    let _shadowFbos = [];
    let _shadowDepthTexs = [];
    let _shadowLightMVPLoc = null;
    let _shadowModelLoc = null;
    let _shadowPosLoc = null;
    let _shadowInstancedLoc = null;
    let _shadowInstPosLoc = null;
    const SHADOW_SIZE_MAX = 4096;
    let SHADOW_SIZE = 4096;
    let _shadowTexCoordLoc = null;
    let _shadowHeightLoc = null;
    let _shadowHasTextureLoc = null;
    let _shadowTextureLoc = null;
    let _shadowAlphaThresholdLoc = null;
       
    function initShadowMaps(count) {
        const gl = _gl;
        const sv = compileShader(gl.VERTEX_SHADER, shadowVsSource);
        const sf = compileShader(gl.FRAGMENT_SHADER, shadowFsSource);

        _shadowProgram = gl.createProgram();
        gl.attachShader(_shadowProgram, sv);
        gl.attachShader(_shadowProgram, sf);
        gl.linkProgram(_shadowProgram);
        _shadowLightMVPLoc = gl.getUniformLocation(_shadowProgram, "uLightVP");
        _shadowModelLoc = gl.getUniformLocation(_shadowProgram, "uModel");
        _shadowPosLoc = gl.getAttribLocation(_shadowProgram, "aPosition");
        _shadowInstancedLoc = gl.getUniformLocation(_shadowProgram, "uIsInstanced");
        _shadowInstPosLoc = gl.getAttribLocation(_shadowProgram, "aInstancePos");
        _shadowTexCoordLoc = gl.getAttribLocation(_shadowProgram, "aTexCoord");
        _shadowHasTextureLoc = gl.getUniformLocation(_shadowProgram, "uShadowHasTexture");
        _shadowTextureLoc = gl.getUniformLocation(_shadowProgram, "uShadowTexture");
        _shadowAlphaThresholdLoc = gl.getUniformLocation(_shadowProgram, "uShadowAlphaThreshold");
        _shadowHeightLoc = gl.getAttribLocation(_shadowProgram, "aHeight");
        _shadowFbos = [];
        _shadowDepthTexs = [];

        for (let i = 0; i < count; i++) {
            const depthTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, depthTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.DEPTH_COMPONENT24,
                SHADOW_SIZE, SHADOW_SIZE, 0, gl.DEPTH_COMPONENT, gl.UNSIGNED_INT, null);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            _shadowDepthTexs.push(depthTex);

            const colorTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, colorTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA,
                SHADOW_SIZE, SHADOW_SIZE, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);

            const fbo = gl.createFramebuffer();
            gl.bindFramebuffer(gl.FRAMEBUFFER, fbo);
            gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT,
                gl.TEXTURE_2D, depthTex, 0);
            gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0,
                gl.TEXTURE_2D, colorTex, 0);
            _shadowFbos.push(fbo);
        }

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
    }

    // ============================================================
    // AA SHARED STATE — used by all AA passes
    // ============================================================
    let _fxaaFbo = null;        // shared offscreen FBO for all AA input
    let _fxaaColorTex = null;   // shared scene color texture input

    function initSharedFbo() {
        const gl = _gl;

        _fxaaColorTex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, _fxaaColorTex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA,
            _canvas.width, _canvas.height, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);

        const depthRb = gl.createRenderbuffer();
        gl.bindRenderbuffer(gl.RENDERBUFFER, depthRb);
        gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT16,
            _canvas.width, _canvas.height);

        _fxaaFbo = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, _fxaaFbo);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0,
            gl.TEXTURE_2D, _fxaaColorTex, 0);
        gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT,
            gl.RENDERBUFFER, depthRb);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);


    }

    function resizeAAfbos() {
        const gl = _gl;
        if (!gl || !_canvas) return;
        const w = _canvas.width;
        const h = _canvas.height;

        if (_fxaaFbo) {
            gl.deleteFramebuffer(_fxaaFbo);
            _fxaaFbo = null;
        }
        if (_fxaaColorTex) {
            gl.deleteTexture(_fxaaColorTex);
            _fxaaColorTex = null;
        }
 
        initSharedFbo();
        window.SE.fxaaFbo = _fxaaFbo;          
        window.SE.fxaaColorTex = _fxaaColorTex;

        if (window.SE.smaaEdgeTex) {                 
            gl.bindTexture(gl.TEXTURE_2D, window.SE.smaaEdgeTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, null);
        }
        if (window.SE.smaaBlendTex) {               
            gl.bindTexture(gl.TEXTURE_2D, window.SE.smaaBlendTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, null);
        }

        window.SpectralTAA.resize(w, h);

        if (window.SE.spectralEdgeTex) {
            gl.bindTexture(gl.TEXTURE_2D, window.SE.spectralEdgeTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, null);
        }
        if (window.SE.spectralV2EdgeTex) {
            gl.bindTexture(gl.TEXTURE_2D, window.SE.spectralV2EdgeTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, null);
        }
        if (window.SE.spectralV3EdgeTex) {
            gl.bindTexture(gl.TEXTURE_2D, window.SE.spectralV3EdgeTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, null);
        }
        if (window.SE.spectralV3LineTex) {
            gl.bindTexture(gl.TEXTURE_2D, window.SE.spectralV3LineTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, null);
        }

        gl.bindTexture(gl.TEXTURE_2D, null);  
    }

    function drawQuad(program) {
        if (_quadPosLocs[program] === undefined) {
            _quadPosLocs[program] = _gl.getAttribLocation(program, "aPosition");
        }
        const pos = _quadPosLocs[program];
        _gl.bindBuffer(_gl.ARRAY_BUFFER, _fullscreenQuadVbo);
        _gl.enableVertexAttribArray(pos);
        _gl.vertexAttribPointer(pos, 2, _gl.FLOAT, false, 0, 0);
        _gl.drawArrays(_gl.TRIANGLES, 0, 6);
    }


    function createColorTexture(w, h) {
        const gl = _gl;
        const tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, w, h, 0, gl.RGBA, gl.UNSIGNED_BYTE, null);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        return tex;
    }
    function createFboForTexture(tex, w, h) {
        const gl = _gl;
        const rb = gl.createRenderbuffer();
        gl.bindRenderbuffer(gl.RENDERBUFFER, rb);
        gl.renderbufferStorage(gl.RENDERBUFFER, gl.DEPTH_COMPONENT16, w, h);
        const fbo = gl.createFramebuffer();
        gl.bindFramebuffer(gl.FRAMEBUFFER, fbo);
        gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0, gl.TEXTURE_2D, tex, 0);
        gl.framebufferRenderbuffer(gl.FRAMEBUFFER, gl.DEPTH_ATTACHMENT, gl.RENDERBUFFER, rb);
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        return fbo;
    }



    // ============================================================
    // TILEMAP SYSTEM
    // ============================================================
    let _tileProgram = null;
    let _tileGridVAO = null;
    let _tileGridVBO = null;     // static XY positions
    let _tileHeightVBO = null;   // dynamic Z heights
    let _tileNormalVBO = null;   // dynamic vertex normals
    let _tileIBO = null;         // static index buffer
    let _tileMatTex = null;      // dynamic material texture
    // GPU splat map system — one RGBA8 texture + FBO per 4 materials
    // splatTextures[0] = R:Dirt G:Rock B:Grass A:Snow
    // splatTextures[1] = R:Water G:Ice B:future A:future
    let _splatTextures = [];     // WebGL textures, one per 4-material group
    let _splatFbos = [];         // FBOs, one per splat texture
    let _splatScratch = [];      // ping-pong read copies — avoids feedback loop
    let _paintProgram = null;    // circular brush stamp shader
    let _splatUniforms = null;   // cached paint shader uniform locations
    let _splatInitialized = false;
    const SPLAT_MATS_PER_TEX = 4; // materials packed per RGBA texture
    let _tileTextures = {};
    let _tileTexturesReady = false;
    let _tileMapTexturesUploaded = false; // mirrors C# flag — prevents double upload
    let _tileMapGeneration = 0;
    // ── PBR Texture Dictionaries — one entry per material slot (0-5) ──────
    // Each dict maps slot index → WebGL texture object
    // Null slots fall back to default flat textures at draw time
    let _tileNormalTextures = {};       // tangent-space normal maps
    let _tileRoughnessTextures = {};    // greyscale roughness
    let _tileMetallicTextures = {};     // greyscale metallic
    let _tileAOTextures = {};           // greyscale ambient occlusion
    let _tileSpecularTextures = {};     // greyscale/RGB specular
    let _tileEmissiveTextures = {};     // RGB emissive glow
    let _tileDisplacementTextures = {}; // greyscale height for parallax

    // ── Combined PBR upload flag ───────────────────────────────────────────
    // Set true when all PBR texture sets finish async loading
    // Mirrors _tileMapTexturesUploaded pattern
    let _tilePBRReady = false;
    let _tilePBRSetsTotal = 0;     // how many non-null sets were requested
    let _tilePBRSetsLoaded = 0;    // how many have finished loading

    // ── Fallback textures — created once at initTileMap, reused forever ───
    // Bound to slots where no PBR texture is assigned
    // Flat normal = RGB(128,128,255) = pointing straight up in tangent space
    let _tileDefaultNormalTex = null;
    // White = roughness 1.0 / AO 1.0 / specular 1.0 scalar passthrough
    let _tileDefaultWhiteTex = null;
    // Black = metallic 0.0 / emissive 0.0 scalar passthrough
    let _tileDefaultBlackTex = null;

    // ── GPU texture unit cap — queried at initTileMap ─────────────────────
    let _maxTextureUnits = 32; // safe minimum, updated from GPU at init

    let _tileIdxCount = 0;
    let _tileUniforms = null;
    let _tileViewMatrixF32 = null;
    let _tileProjMatrixF32 = null;

    let _pendingFullSeedData = null;

    // Tile Grid Configuration — GRID_SIZE is now dynamic, set from frame data
    // Default 512 matches Scene2 — Scene4 will send 128 on first full upload
    const TILE_SIZE = 1.0;
    let GRID_SIZE = 512;
    let GRID_VERTS = (GRID_SIZE + 1) * (GRID_SIZE + 1);
    let GRID_ORIGIN_X = -(GRID_SIZE * TILE_SIZE) / 2.0;
    let GRID_ORIGIN_Y = -(GRID_SIZE * TILE_SIZE) / 2.0;

    // Track last known grid size — triggers full rebuild when it changes
    let _lastKnownGridSize = 0;

    function updateGridConstants(newSize) {
        if (newSize === GRID_SIZE && _lastKnownGridSize === newSize) return false;
        GRID_SIZE = newSize;
        GRID_VERTS = (GRID_SIZE + 1) * (GRID_SIZE + 1);
        GRID_ORIGIN_X = -(GRID_SIZE * TILE_SIZE) / 2.0;
        GRID_ORIGIN_Y = -(GRID_SIZE * TILE_SIZE) / 2.0;
        CHUNKS_PER_SIDE = GRID_SIZE / CHUNK_SIZE;
        _lastKnownGridSize = newSize;
        return true;
    }

    function teardownTileMap() {
        const gl = _gl;
        if (!gl) return;

        if (_tileGridVAO) { gl.deleteVertexArray(_tileGridVAO); _tileGridVAO = null; }
        if (_tileGridVBO) { gl.deleteBuffer(_tileGridVBO); _tileGridVBO = null; }
        if (_tileHeightVBO) { gl.deleteBuffer(_tileHeightVBO); _tileHeightVBO = null; }
        if (_tileNormalVBO) { gl.deleteBuffer(_tileNormalVBO); _tileNormalVBO = null; }
        if (_tileIBO) { gl.deleteBuffer(_tileIBO); _tileIBO = null; }
        if (_tileMatTex) { gl.deleteTexture(_tileMatTex); _tileMatTex = null; }

        // Splat map cleanup
        for (const tex of _splatTextures) { if (tex) gl.deleteTexture(tex); }
        for (const tex of _splatScratch) { if (tex) gl.deleteTexture(tex); }
        for (const fbo of _splatFbos) { if (fbo) gl.deleteFramebuffer(fbo); }
        _splatTextures = [];
        _splatScratch = [];
        _splatFbos = [];
        _paintProgram = null;
        _splatUniforms = null;
        _splatInitialized = false;
        _tileIdxCount = 0;
        _tileTexturesReady = false;
        _tileMapTexturesUploaded = false;
        // ── Reset PBR state ───────────────────────────────────────────────
        _tileNormalTextures = {};
        _tileRoughnessTextures = {};
        _tileMetallicTextures = {};
        _tileAOTextures = {};
        _tileSpecularTextures = {};
        _tileEmissiveTextures = {};
        _tileDisplacementTextures = {};
        _tilePBRReady = false;
        _tilePBRSetsTotal = 0;
        _tilePBRSetsLoaded = 0;
        // Note: fallback textures (_tileDefaultNormalTex etc.) are NOT deleted
        // They are recreated once per GL context init and reused across scenes

        _chunkIndexOffsets = null;
        _chunkIndexCounts = null;
        _chunkBoundsMinZ = null;
        _chunkBoundsMaxZ = null;
        _pendingFullSeedData = null;
        window._tileGridReady = false;

      //  console.log('[TileMap] GPU resources torn down — ready for rebuild');
    }

    // Functions owned by this section

    // ============================================================
    // TILE CULLING SYSTEM
    // ============================================================
  
    const CHUNK_SIZE = 32; // tiles per chunk side — fixed, independent of grid size
    let CHUNKS_PER_SIDE = GRID_SIZE / CHUNK_SIZE; // recalculated when GRID_SIZE changes

    // Frustum plane extraction from VP matrix
    function extractFrustumPlanes(vp) {
        // Each plane is [nx, ny, nz, d]
        return [
            // Left
            [vp[3] + vp[0], vp[7] + vp[4], vp[11] + vp[8], vp[15] + vp[12]],
            // Right
            [vp[3] - vp[0], vp[7] - vp[4], vp[11] - vp[8], vp[15] - vp[12]],
            // Bottom
            [vp[3] + vp[1], vp[7] + vp[5], vp[11] + vp[9], vp[15] + vp[13]],
            // Top
            [vp[3] - vp[1], vp[7] - vp[5], vp[11] - vp[9], vp[15] - vp[13]],
            // Near
            [vp[3] + vp[2], vp[7] + vp[6], vp[11] + vp[10], vp[15] + vp[14]],
            // Far
            [vp[3] - vp[2], vp[7] - vp[6], vp[11] - vp[10], vp[15] - vp[14]],
        ];
    }

    // AABB vs frustum — returns false if fully outside any plane
    function aabbInFrustum(planes, minX, minY, minZ, maxX, maxY, maxZ) {
        for (const [nx, ny, nz, d] of planes) {
            // Pick positive vertex (furthest in plane normal direction)
            const px = nx >= 0 ? maxX : minX;
            const py = ny >= 0 ? maxY : minY;
            const pz = nz >= 0 ? maxZ : minZ;
            if (nx * px + ny * py + nz * pz + d < 0) return false; // outside
        }
        return true;
    }

    // Build chunk index ranges once after IBO is built — call this at end of buildChunk()
    let _chunkIndexOffsets = null; // byte offset into IBO per chunk
    let _chunkIndexCounts = null; // index count per chunk
    let _chunkBoundsMinZ = null; // min Z per chunk (updated when heights change)
    let _chunkBoundsMaxZ = null;

    function buildChunkIndex() {
        const C = CHUNKS_PER_SIDE;
        _chunkIndexOffsets = new Int32Array(C * C);
        _chunkIndexCounts = new Int32Array(C * C);
        _chunkBoundsMinZ = new Float32Array(C * C);
        _chunkBoundsMaxZ = new Float32Array(C * C);

        const GS = GRID_SIZE;
        const GSP1 = GS + 1;
        const CS = CHUNK_SIZE;
        const totalIndices = GS * GS * 6;
        const chunkedIndices = new Uint32Array(totalIndices);
        let ii = 0;

        for (let cy = 0; cy < C; cy++) {
            for (let cx = 0; cx < C; cx++) {
                const chunkId = cy * C + cx;
                _chunkIndexOffsets[chunkId] = ii * 4; // byte offset

                for (let ty = cy * CS; ty < (cy + 1) * CS; ty++) {
                    for (let tx = cx * CS; tx < (cx + 1) * CS; tx++) {
                        const bl = ty * GSP1 + tx;
                        const br = bl + 1;
                        const tl = bl + GSP1;
                        const tr = tl + 1;
                        if ((tx + ty) % 2 === 0) {
                            chunkedIndices[ii++] = bl; chunkedIndices[ii++] = br; chunkedIndices[ii++] = tl;
                            chunkedIndices[ii++] = br; chunkedIndices[ii++] = tr; chunkedIndices[ii++] = tl;
                        } else {
                            chunkedIndices[ii++] = bl; chunkedIndices[ii++] = br; chunkedIndices[ii++] = tr;
                            chunkedIndices[ii++] = bl; chunkedIndices[ii++] = tr; chunkedIndices[ii++] = tl;
                        }
                    }
                }

                _chunkIndexCounts[chunkId] = ii - (_chunkIndexOffsets[chunkId] / 4);
                // Z bounds start flat — updated by updateChunkZBounds()
                _chunkBoundsMinZ[chunkId] = -5;
                _chunkBoundsMaxZ[chunkId] = 10;
            }
        }

        // Replace the IBO with the chunked version
        const gl = _gl;
        gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, _tileIBO);
        gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, chunkedIndices, gl.STATIC_DRAW);
        _tileIdxCount = totalIndices;
      //  console.log('[TileMap] Chunked IBO built —', C * C, 'chunks');
    }

    // Call after height uploads to keep Z bounds tight
    function updateChunkZBounds(heights) {
        if (!_chunkBoundsMinZ) return;
        const C = CHUNKS_PER_SIDE;
        const CS = CHUNK_SIZE;
        const GSP1 = GRID_SIZE + 1;
        for (let cy = 0; cy < C; cy++) {
            for (let cx = 0; cx < C; cx++) {
                let minZ = 999, maxZ = -999;
                for (let ty = cy * CS; ty <= (cy + 1) * CS; ty++) {
                    for (let tx = cx * CS; tx <= (cx + 1) * CS; tx++) {
                        if (ty > GRID_SIZE || tx > GRID_SIZE) continue;
                        const h = heights[ty * GSP1 + tx];
                        if (h < minZ) minZ = h;
                        if (h > maxZ) maxZ = h;
                    }
                }
                const id = cy * C + cx;
                _chunkBoundsMinZ[id] = minZ - 1; // small pad
                _chunkBoundsMaxZ[id] = maxZ + 1;
            }
        }
    }

    function initSplatMap(gl, GS) {
        // How many RGBA textures we need for all materials
        const matCount = 6; // Dirt Rock Grass Snow Water Ice
        const texCount = Math.ceil(matCount / SPLAT_MATS_PER_TEX); // = 2

        _splatTextures = [];
        _splatScratch = [];
        _splatFbos = [];

        for (let t = 0; t < texCount; t++) {
            // Default fill — tex0: Grass is channel 2 (B) = 255, all others 0
            //                tex1: all zero (Water/Ice start unpainted)
            const pixels = new Uint8Array(GS * GS * 4);
            if (t === 0) {
                for (let i = 0; i < GS * GS; i++) {
                    pixels[i * 4 + 0] = 0;   // Dirt
                    pixels[i * 4 + 1] = 0;   // Rock
                    pixels[i * 4 + 2] = 255; // Grass — default
                    pixels[i * 4 + 3] = 0;   // Snow
                }
            }
            // tex1 stays zeroed — Water/Ice start at 0

            const tex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, tex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8,
                GS, GS, 0, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            _splatTextures.push(tex);

            const fbo = gl.createFramebuffer();
            gl.bindFramebuffer(gl.FRAMEBUFFER, fbo);
            gl.framebufferTexture2D(gl.FRAMEBUFFER, gl.COLOR_ATTACHMENT0,
                gl.TEXTURE_2D, tex, 0);
            _splatFbos.push(fbo);

            // Scratch copy — same size/format, used as read source during paint pass
            const scratch = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, scratch);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA8,
                GS, GS, 0, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            _splatScratch.push(scratch);
        }

        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.bindTexture(gl.TEXTURE_2D, null);

        // Compile paint program once
        if (!_paintProgram) {
            _paintProgram = buildProgram(paintVsSrc, paintFsSrc);
            _splatUniforms = {
                gridOrigin: gl.getUniformLocation(_paintProgram, 'uGridOrigin'),
                gridSize: gl.getUniformLocation(_paintProgram, 'uGridSize'),
                tileSize: gl.getUniformLocation(_paintProgram, 'uTileSize'),
                brushWorldPos: gl.getUniformLocation(_paintProgram, 'uBrushWorldPos'),
                brushRadius: gl.getUniformLocation(_paintProgram, 'uBrushRadius'),
                paintStrength: gl.getUniformLocation(_paintProgram, 'uPaintStrength'),
                paintChannel: gl.getUniformLocation(_paintProgram, 'uPaintChannel'),
                currentSplat: gl.getUniformLocation(_paintProgram, 'uCurrentSplat'),
            };
        }

        _splatInitialized = true;
       // console.log('[SplatMap] Initialized —', texCount, 'textures at', GS, 'x', GS);
    }

    function applyPaintBrush(frame) {
        const gl = _gl;
        if (!_splatInitialized || !_paintProgram) return;
        if (!frame.landscapeActive || !frame.isMousePainting) return;

        // Which splat texture + channel does this material write to?
        const matIdx = frame.activePaintMaterial ?? 2; // default Grass
        const texIdx = Math.floor(matIdx / SPLAT_MATS_PER_TEX);
        const chanIdx = matIdx % SPLAT_MATS_PER_TEX;

        if (texIdx >= _splatTextures.length || texIdx >= _splatFbos.length) {
            console.warn('[SplatMap] Material index out of splat texture range:', matIdx);
            return;
        }

        const GS = GRID_SIZE;

        // ── Save GL state ───────────────────────────────────────────────────
        gl.disable(gl.DEPTH_TEST);
        gl.disable(gl.BLEND);
        gl.disable(gl.CULL_FACE);

        // ── Bind target splat FBO ───────────────────────────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _splatFbos[texIdx]);
        gl.viewport(0, 0, GS, GS);

        // ── Bind paint program ──────────────────────────────────────────────
        gl.useProgram(_paintProgram);

        // ── Bind current splat as read source (sample existing weights) ─────
        // ── Copy live splat into scratch before binding FBO ─────────────────
        // Prevents feedback loop — read from scratch, write to live texture
        gl.copyTexSubImage2D = gl.copyTexSubImage2D; // ensure available
        gl.bindFramebuffer(gl.FRAMEBUFFER, _splatFbos[texIdx]);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, _splatScratch[texIdx]);
        gl.copyTexSubImage2D(gl.TEXTURE_2D, 0, 0, 0, 0, 0, GRID_SIZE, GRID_SIZE);

        // ── Bind target FBO and read from scratch ───────────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, _splatFbos[texIdx]);
        gl.activeTexture(gl.TEXTURE0);
        gl.bindTexture(gl.TEXTURE_2D, _splatScratch[texIdx]);
        gl.uniform1i(_splatUniforms.currentSplat, 0);

        // ── Brush uniforms ──────────────────────────────────────────────────
        gl.uniform2f(_splatUniforms.gridOrigin, GRID_ORIGIN_X, GRID_ORIGIN_Y);
        gl.uniform1f(_splatUniforms.gridSize, GS);
        gl.uniform1f(_splatUniforms.tileSize, TILE_SIZE);
        gl.uniform2f(_splatUniforms.brushWorldPos, frame.brushWorldX, frame.brushWorldY);
        gl.uniform1f(_splatUniforms.brushRadius, Math.max(frame.brushRadius ?? 1.0, 0.5));
        gl.uniform1f(_splatUniforms.paintStrength, frame.paintStrength ?? 0.4);
        gl.uniform1i(_splatUniforms.paintChannel, chanIdx);

        // ── Draw fullscreen quad — stamps circle into splat FBO ─────────────
        drawQuad(_paintProgram);

        // ── Restore GL state ────────────────────────────────────────────────
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.bindTexture(gl.TEXTURE_2D, null);
        gl.viewport(0, 0, _canvas.width, _canvas.height);
        gl.enable(gl.DEPTH_TEST);
    }


    function initTileMap(gl) {
        // Always use current GRID_SIZE — may have been updated by updateGridConstants()
        const GS = GRID_SIZE;
        const GSP1 = GS + 1;

        // ── Capture generation at the moment this initTileMap was called ──
        // Any subsequent call to initTileMap (via scene switch) increments
        // _tileMapGeneration, causing in-flight buildChunk callbacks to bail.
        const myGeneration = ++_tileMapGeneration;
      //  console.log('[TileMap] initTileMap starting — GS:', GS, 'GSP1:', GSP1);

        // ── Query GPU texture unit cap ─────────────────────────────────────
        _maxTextureUnits = gl.getParameter(gl.MAX_TEXTURE_IMAGE_UNITS);
        /*
        console.log('[TileMap] GPU max texture units:', _maxTextureUnits);
        if (_maxTextureUnits < 36) {
            console.warn('[TileMap] GPU has fewer than 36 texture units —',
                'some PBR channels will be skipped. Units available:', _maxTextureUnits);
        }
        */
        // ── Create fallback textures once ─────────────────────────────────
        if (!_tileDefaultNormalTex) {
            // Flat normal = RGB(128, 128, 255) — points straight up in tangent space
            _tileDefaultNormalTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, _tileDefaultNormalTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 255, 255]));
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        }
        if (!_tileDefaultWhiteTex) {
            // White = scalar 1.0 passthrough for roughness, AO, specular
            _tileDefaultWhiteTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, _tileDefaultWhiteTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([255, 255, 255, 255]));
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        }
        if (!_tileDefaultBlackTex) {
            // Black = scalar 0.0 passthrough for metallic, emissive
            _tileDefaultBlackTex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, _tileDefaultBlackTex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([0, 0, 0, 255]));
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        }
        gl.bindTexture(gl.TEXTURE_2D, null);


        // Only rebuild shader program if not already compiled
        // Shader source is grid-size independent — safe to reuse
        if (!_tileProgram) {
            _tileProgram = buildProgram(tileVsSrc, tileFsSrc);
            if (!_tileProgram) { console.error('[TileMap] shader failed'); return; }
        }

        // Static XY grid positions — built synchronously, small enough
        const xyData = new Float32Array(GSP1 * GSP1 * 2);
        for (let y = 0; y <= GS; y++) {
            for (let x = 0; x <= GS; x++) {
                const i = (y * GSP1 + x) * 2;
                xyData[i] = GRID_ORIGIN_X + x * TILE_SIZE;
                xyData[i + 1] = GRID_ORIGIN_Y + y * TILE_SIZE;
            }
        }
        _tileGridVBO = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, _tileGridVBO);
        gl.bufferData(gl.ARRAY_BUFFER, xyData, gl.STATIC_DRAW);

        // Dynamic VBOs and texture — allocate now, no data yet
        _tileHeightVBO = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, _tileHeightVBO);
        gl.bufferData(gl.ARRAY_BUFFER, GRID_VERTS * 4, gl.DYNAMIC_DRAW);

        _tileNormalVBO = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, _tileNormalVBO);
        gl.bufferData(gl.ARRAY_BUFFER, GRID_VERTS * 3 * 4, gl.DYNAMIC_DRAW);

        _tileMatTex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, _tileMatTex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA32F,
            GS, GS, 0, gl.RGBA, gl.FLOAT, null);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);

        // Cache uniforms immediately — safe to do before IBO ready
        _tileUniforms = {
            view: gl.getUniformLocation(_tileProgram, 'uView'),
            projection: gl.getUniformLocation(_tileProgram, 'uProjection'),
            tex0: gl.getUniformLocation(_tileProgram, 'uTex0'),
            tex1: gl.getUniformLocation(_tileProgram, 'uTex1'),
            tex2: gl.getUniformLocation(_tileProgram, 'uTex2'),
            tex3: gl.getUniformLocation(_tileProgram, 'uTex3'),
            tex4: gl.getUniformLocation(_tileProgram, 'uTex4'),
            tex5: gl.getUniformLocation(_tileProgram, 'uTex5'),
            tileData: gl.getUniformLocation(_tileProgram, 'uTileData'),
            gridOrigin: gl.getUniformLocation(_tileProgram, 'uGridOrigin'),
            gridSize: gl.getUniformLocation(_tileProgram, 'uGridSize'),
            tileSize: gl.getUniformLocation(_tileProgram, 'uTileSize'),
            sunDir: gl.getUniformLocation(_tileProgram, 'uSunDir'),
            sunColor: gl.getUniformLocation(_tileProgram, 'uSunColor'),
            sunIntensity: gl.getUniformLocation(_tileProgram, 'uSunIntensity'),
            ambient: gl.getUniformLocation(_tileProgram, 'uAmbient'),
            brushPos: gl.getUniformLocation(_tileProgram, 'uBrushPos'),
            brushRadius: gl.getUniformLocation(_tileProgram, 'uBrushRadius'),
            brushActive: gl.getUniformLocation(_tileProgram, 'uBrushActive'),
            shadowMap: Array.from({ length: 8 }, (_, i) => gl.getUniformLocation(_tileProgram, `uShadowMap${i}`)),
            lightVP: Array.from({ length: 8 }, (_, i) => gl.getUniformLocation(_tileProgram, `uLightVP${i}`)),
            shadowBias: gl.getUniformLocation(_tileProgram, 'uShadowBias'),
            tileLightCount: gl.getUniformLocation(_tileProgram, 'uTileLightCount'),
            shadowMapSize: gl.getUniformLocation(_tileProgram, 'uShadowMapSize'),
            tileLightShadowSlot: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightShadowSlot[${i}]`)),
            // Splat map samplers — one per splat texture slot
            splatMap0: gl.getUniformLocation(_tileProgram, 'uSplatMap0'),
            splatMap1: gl.getUniformLocation(_tileProgram, 'uSplatMap1'),
            // ── Camera position — needed for PBR specular and parallax ──────
            camPos: gl.getUniformLocation(_tileProgram, 'uCamPos'),

            // ── Normal map samplers — units 17-22 ────────────────────────────
            norm0: gl.getUniformLocation(_tileProgram, 'uNorm0'),
            norm1: gl.getUniformLocation(_tileProgram, 'uNorm1'),
            norm2: gl.getUniformLocation(_tileProgram, 'uNorm2'),
            norm3: gl.getUniformLocation(_tileProgram, 'uNorm3'),
            norm4: gl.getUniformLocation(_tileProgram, 'uNorm4'),
            norm5: gl.getUniformLocation(_tileProgram, 'uNorm5'),

            // ── Roughness map samplers — units 23-28 ─────────────────────────
            rough0: gl.getUniformLocation(_tileProgram, 'uRough0'),
            rough1: gl.getUniformLocation(_tileProgram, 'uRough1'),
            rough2: gl.getUniformLocation(_tileProgram, 'uRough2'),
            rough3: gl.getUniformLocation(_tileProgram, 'uRough3'),
            rough4: gl.getUniformLocation(_tileProgram, 'uRough4'),
            rough5: gl.getUniformLocation(_tileProgram, 'uRough5'),

            // ── Metallic map samplers — units 29-34 ──────────────────────────
            metal0: gl.getUniformLocation(_tileProgram, 'uMetal0'),
            metal1: gl.getUniformLocation(_tileProgram, 'uMetal1'),
            metal2: gl.getUniformLocation(_tileProgram, 'uMetal2'),
            metal3: gl.getUniformLocation(_tileProgram, 'uMetal3'),
            metal4: gl.getUniformLocation(_tileProgram, 'uMetal4'),
            metal5: gl.getUniformLocation(_tileProgram, 'uMetal5'),

            // ── AO map samplers — units 35-40 ────────────────────────────────
            ao0: gl.getUniformLocation(_tileProgram, 'uAO0'),
            ao1: gl.getUniformLocation(_tileProgram, 'uAO1'),
            ao2: gl.getUniformLocation(_tileProgram, 'uAO2'),
            ao3: gl.getUniformLocation(_tileProgram, 'uAO3'),
            ao4: gl.getUniformLocation(_tileProgram, 'uAO4'),
            ao5: gl.getUniformLocation(_tileProgram, 'uAO5'),

            // ── Specular map samplers — units 41-46 ──────────────────────────
            spec0: gl.getUniformLocation(_tileProgram, 'uSpec0'),
            spec1: gl.getUniformLocation(_tileProgram, 'uSpec1'),
            spec2: gl.getUniformLocation(_tileProgram, 'uSpec2'),
            spec3: gl.getUniformLocation(_tileProgram, 'uSpec3'),
            spec4: gl.getUniformLocation(_tileProgram, 'uSpec4'),
            spec5: gl.getUniformLocation(_tileProgram, 'uSpec5'),

            // ── Emissive map samplers — units 47-52 ──────────────────────────
            emissive0: gl.getUniformLocation(_tileProgram, 'uEmissive0'),
            emissive1: gl.getUniformLocation(_tileProgram, 'uEmissive1'),
            emissive2: gl.getUniformLocation(_tileProgram, 'uEmissive2'),
            emissive3: gl.getUniformLocation(_tileProgram, 'uEmissive3'),
            emissive4: gl.getUniformLocation(_tileProgram, 'uEmissive4'),
            emissive5: gl.getUniformLocation(_tileProgram, 'uEmissive5'),

            // ── Displacement map samplers — units 53-58 ───────────────────────
            displace0: gl.getUniformLocation(_tileProgram, 'uDisplace0'),
            displace1: gl.getUniformLocation(_tileProgram, 'uDisplace1'),
            displace2: gl.getUniformLocation(_tileProgram, 'uDisplace2'),
            displace3: gl.getUniformLocation(_tileProgram, 'uDisplace3'),
            displace4: gl.getUniformLocation(_tileProgram, 'uDisplace4'),
            displace5: gl.getUniformLocation(_tileProgram, 'uDisplace5'),

            // ── PBR Scalar uniform arrays ─────────────────────────────────────
            roughnessVal: gl.getUniformLocation(_tileProgram, 'uRoughnessVal'),
            metallicVal: gl.getUniformLocation(_tileProgram, 'uMetallicVal'),
            aoVal: gl.getUniformLocation(_tileProgram, 'uAOVal'),
            specularVal: gl.getUniformLocation(_tileProgram, 'uSpecularVal'),
            emissiveIntensityVal: gl.getUniformLocation(_tileProgram, 'uEmissiveIntensityVal'),
            displacementStrength: gl.getUniformLocation(_tileProgram, 'uDisplacementStrength'),
            parallaxScale: gl.getUniformLocation(_tileProgram, 'uParallaxScale'),


            tileLightPos: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightPos[${i}]`)),
            tileLightColor: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightColor[${i}]`)),
            tileLightIntensity: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightIntensity[${i}]`)),
            tileLightRange: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightRange[${i}]`)),
            tileLightType: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightType[${i}]`)),
            tileLightDir: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightDir[${i}]`)),
            tileLightSpotAngle: Array.from({ length: 32 }, (_, i) => gl.getUniformLocation(_tileProgram, `uTileLightSpotAngle[${i}]`)),
        };

        // Chunked async index buffer build — yields to browser every CHUNK rows
        // Keeps main thread responsive during heavy index generation
        const totalIndices = GS * GS * 6;
        const indices = new Uint32Array(totalIndices);
        const CHUNK = 512; // rows per chunk — tune if needed
        let row = 0;
        let ii = 0;

        function buildChunk() {
            // ── Stale context guard — bail if a newer init has started ──
            if (myGeneration !== _tileMapGeneration) {
                //   console.log('[TileMap] buildChunk cancelled — generation mismatch');
                return;
            }

            const endRow = Math.min(row + CHUNK, GS);
            for (let y = row; y < endRow; y++) {
                for (let x = 0; x < GS; x++) {
                    const bl = y * GSP1 + x;
                    const br = bl + 1;
                    const tl = bl + GSP1;
                    const tr = tl + 1;
                    if ((x + y) % 2 === 0) {
                        indices[ii++] = bl; indices[ii++] = br; indices[ii++] = tl;
                        indices[ii++] = br; indices[ii++] = tr; indices[ii++] = tl;
                    } else {
                        indices[ii++] = bl; indices[ii++] = br; indices[ii++] = tr;
                        indices[ii++] = bl; indices[ii++] = tr; indices[ii++] = tl;
                    }
                }
            }
            row = endRow;

            if (row < GS) {
                setTimeout(buildChunk, 0);
            } else {
                // ── Final guard before touching GPU ──
                if (myGeneration !== _tileMapGeneration) {
                    //   console.log('[TileMap] buildChunk final stage cancelled — generation mismatch');
                    return;
                }

                _tileIBO = gl.createBuffer();
                gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, _tileIBO);
                gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, indices, gl.STATIC_DRAW);
                _tileIdxCount = indices.length;

                _tileGridVAO = gl.createVertexArray();
                gl.bindVertexArray(_tileGridVAO);
                gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, _tileIBO);

                gl.bindBuffer(gl.ARRAY_BUFFER, _tileGridVBO);
                gl.enableVertexAttribArray(0);
                gl.vertexAttribPointer(0, 2, gl.FLOAT, false, 0, 0);

                gl.bindBuffer(gl.ARRAY_BUFFER, _tileHeightVBO);
                gl.enableVertexAttribArray(1);
                gl.vertexAttribPointer(1, 1, gl.FLOAT, false, 0, 0);

                gl.bindBuffer(gl.ARRAY_BUFFER, _tileNormalVBO);
                gl.enableVertexAttribArray(2);
                gl.vertexAttribPointer(2, 3, gl.FLOAT, false, 0, 0);

                gl.bindVertexArray(null);

                SpectralGLLoader.onSpecialComplete('tilemap');
                buildChunkIndex();
                initSplatMap(gl, GRID_SIZE);

                if (_pendingFullSeedData) {
                    applySeedData(gl, _pendingFullSeedData);
                    _pendingFullSeedData = null;
                }
                window._tileGridReady = true;
            }
        }

        setTimeout(buildChunk, 0);
    }

    function applySeedData(gl, seedData) {
        if (!_splatInitialized || _splatTextures.length === 0) return;

        const GS = seedData.gridSize;
        const texCount = _splatTextures.length;

        // Build pixel buffers for both live and scratch textures
        const splatPixels = [];
        for (let t = 0; t < texCount; t++) {
            splatPixels.push(new Uint8Array(GS * GS * 4));
        }

        for (let i = 0; i < GS * GS; i++) {
            const matIdx = seedData.materials[i] ?? 2;
            const blendMat = seedData.blendMaterials[i] ?? matIdx;
            const blendW = seedData.blendWeights[i] ?? 0;

            const texA = Math.floor(matIdx / SPLAT_MATS_PER_TEX);
            const chanA = matIdx % SPLAT_MATS_PER_TEX;
            const texB = Math.floor(blendMat / SPLAT_MATS_PER_TEX);
            const chanB = blendMat % SPLAT_MATS_PER_TEX;

            if (texA < texCount) {
                splatPixels[texA][i * 4 + chanA] =
                    Math.round((1.0 - blendW) * 255);
            }
            if (texB < texCount && blendW > 0.001) {
                splatPixels[texB][i * 4 + chanB] =
                    Math.round(blendW * 255);
            }
        }

        // Upload to BOTH live texture and scratch — keeps them in sync
        for (let t = 0; t < texCount; t++) {
            // Live texture
            gl.bindTexture(gl.TEXTURE_2D, _splatTextures[t]);
            gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0,
                GS, GS, gl.RGBA, gl.UNSIGNED_BYTE, splatPixels[t]);

            // Scratch copy — must match live or first paint stroke overwrites with stale data
            gl.bindTexture(gl.TEXTURE_2D, _splatScratch[t]);
            gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0,
                GS, GS, gl.RGBA, gl.UNSIGNED_BYTE, splatPixels[t]);
        }

        gl.bindTexture(gl.TEXTURE_2D, null);
       // console.log('[SplatMap] Seeded live + scratch from C# data —', texCount, 'textures');
    }

    function drawTileMap(gl, frame) {
        if (!_tileProgram || !_tileGridVAO || !_tileTexturesReady) return;
        if (!_tileMatTex || !_tileUniforms || _tileIdxCount === 0) return;

        // ── GPU paint pass — stamp brush circle into splat before terrain draw ──
        if (frame.isMousePainting && frame.landscapeActive) {
            applyPaintBrush(frame);
        }

        gl.useProgram(_tileProgram);
        gl.bindVertexArray(_tileGridVAO);

        if (!_tileViewMatrixF32) _tileViewMatrixF32 = new Float32Array(16);
        if (!_tileProjMatrixF32) _tileProjMatrixF32 = new Float32Array(16);
        _tileViewMatrixF32.set(frame.viewMatrix);
        _tileProjMatrixF32.set(frame.projMatrix);

        gl.uniformMatrix4fv(_tileUniforms.view, false, _tileViewMatrixF32);
        gl.uniformMatrix4fv(_tileUniforms.projection, false, _tileProjMatrixF32);

        // Material textures on units 0-5
        for (let i = 0; i < 6; i++) {
            gl.activeTexture(gl.TEXTURE0 + i);
            gl.bindTexture(gl.TEXTURE_2D, _tileTextures[i] || null);
            gl.uniform1i(_tileUniforms['tex' + i], i);
        }

        // Tile data texture on unit 6
        gl.activeTexture(gl.TEXTURE6);
        gl.bindTexture(gl.TEXTURE_2D, _tileMatTex);
        gl.uniform1i(_tileUniforms.tileData, 6);

        // Splat map textures — units 8 and 9
        // Unit 7 is reserved for shadow map
        // Splat map textures — units 7 and 8 (moved down to free 9-16 for shadow maps)
        if (_splatInitialized) {
            if (_splatTextures[0]) {
                gl.activeTexture(gl.TEXTURE7);
                gl.bindTexture(gl.TEXTURE_2D, _splatTextures[0]);
                gl.uniform1i(_tileUniforms.splatMap0, 7);
            }
            if (_splatTextures[1]) {
                gl.activeTexture(gl.TEXTURE8);
                gl.bindTexture(gl.TEXTURE_2D, _splatTextures[1]);
                gl.uniform1i(_tileUniforms.splatMap1, 8);
            }
        }

        // ── PBR Scalar Uniforms — sent every frame ─────────────────────────
        if (frame.tileRoughnessValues)
            gl.uniform1fv(_tileUniforms.roughnessVal, frame.tileRoughnessValues);
        if (frame.tileMetallicValues)
            gl.uniform1fv(_tileUniforms.metallicVal, frame.tileMetallicValues);
        if (frame.tileAOValues)
            gl.uniform1fv(_tileUniforms.aoVal, frame.tileAOValues);
        if (frame.tileSpecularValues)
            gl.uniform1fv(_tileUniforms.specularVal, frame.tileSpecularValues);
        if (frame.tileEmissiveIntensityValues)
            gl.uniform1fv(_tileUniforms.emissiveIntensityVal, frame.tileEmissiveIntensityValues);
        if (frame.tileDisplacementStrengthValues)
            gl.uniform1fv(_tileUniforms.displacementStrength, frame.tileDisplacementStrengthValues);
        if (frame.tileParallaxScaleValues)
            gl.uniform1fv(_tileUniforms.parallaxScale, frame.tileParallaxScaleValues);

        // ── Camera position for PBR specular + parallax ────────────────────
        if (_tileUniforms.camPos)
            gl.uniform3f(_tileUniforms.camPos,
                frame.camX ?? 0, frame.camY ?? 0, frame.camZ ?? 0);

        // ── PBR Texture Bindings — units 17-58 ────────────────────────────
        // Each channel binds 6 slots. Null slots get fallback texture.
        // GPU unit cap guard — skip channels that exceed hardware limit.
        const unitBase = 17;

        // Helper: bind one PBR texture slot with fallback
        function bindPBRSlot(dict, slotIdx, unit, uniformLoc, fallbackTex) {
            if (unit >= _maxTextureUnits) return; // GPU cap guard
            gl.activeTexture(gl.TEXTURE0 + unit);
            const tex = dict[slotIdx] || fallbackTex;
            gl.bindTexture(gl.TEXTURE_2D, tex);
            if (uniformLoc) gl.uniform1i(uniformLoc, unit);
        }

        // Normal maps — units 17-22 — fallback: flat normal (128,128,255)
        bindPBRSlot(_tileNormalTextures, 0, unitBase + 0, _tileUniforms.norm0, _tileDefaultNormalTex);
        bindPBRSlot(_tileNormalTextures, 1, unitBase + 1, _tileUniforms.norm1, _tileDefaultNormalTex);
        bindPBRSlot(_tileNormalTextures, 2, unitBase + 2, _tileUniforms.norm2, _tileDefaultNormalTex);
        bindPBRSlot(_tileNormalTextures, 3, unitBase + 3, _tileUniforms.norm3, _tileDefaultNormalTex);
        bindPBRSlot(_tileNormalTextures, 4, unitBase + 4, _tileUniforms.norm4, _tileDefaultNormalTex);
        bindPBRSlot(_tileNormalTextures, 5, unitBase + 5, _tileUniforms.norm5, _tileDefaultNormalTex);

        // Roughness maps — units 23-28 — fallback: white (1.0 = use scalar)
        bindPBRSlot(_tileRoughnessTextures, 0, unitBase + 6, _tileUniforms.rough0, _tileDefaultWhiteTex);
        bindPBRSlot(_tileRoughnessTextures, 1, unitBase + 7, _tileUniforms.rough1, _tileDefaultWhiteTex);
        bindPBRSlot(_tileRoughnessTextures, 2, unitBase + 8, _tileUniforms.rough2, _tileDefaultWhiteTex);
        bindPBRSlot(_tileRoughnessTextures, 3, unitBase + 9, _tileUniforms.rough3, _tileDefaultWhiteTex);
        bindPBRSlot(_tileRoughnessTextures, 4, unitBase + 10, _tileUniforms.rough4, _tileDefaultWhiteTex);
        bindPBRSlot(_tileRoughnessTextures, 5, unitBase + 11, _tileUniforms.rough5, _tileDefaultWhiteTex);

        // Metallic maps — units 29-34 — fallback: black (0.0 = use scalar)
        bindPBRSlot(_tileMetallicTextures, 0, unitBase + 12, _tileUniforms.metal0, _tileDefaultBlackTex);
        bindPBRSlot(_tileMetallicTextures, 1, unitBase + 13, _tileUniforms.metal1, _tileDefaultBlackTex);
        bindPBRSlot(_tileMetallicTextures, 2, unitBase + 14, _tileUniforms.metal2, _tileDefaultBlackTex);
        bindPBRSlot(_tileMetallicTextures, 3, unitBase + 15, _tileUniforms.metal3, _tileDefaultBlackTex);
        bindPBRSlot(_tileMetallicTextures, 4, unitBase + 16, _tileUniforms.metal4, _tileDefaultBlackTex);
        bindPBRSlot(_tileMetallicTextures, 5, unitBase + 17, _tileUniforms.metal5, _tileDefaultBlackTex);

        // AO maps — units 35-40 — fallback: white (1.0 = fully lit)
        bindPBRSlot(_tileAOTextures, 0, unitBase + 18, _tileUniforms.ao0, _tileDefaultWhiteTex);
        bindPBRSlot(_tileAOTextures, 1, unitBase + 19, _tileUniforms.ao1, _tileDefaultWhiteTex);
        bindPBRSlot(_tileAOTextures, 2, unitBase + 20, _tileUniforms.ao2, _tileDefaultWhiteTex);
        bindPBRSlot(_tileAOTextures, 3, unitBase + 21, _tileUniforms.ao3, _tileDefaultWhiteTex);
        bindPBRSlot(_tileAOTextures, 4, unitBase + 22, _tileUniforms.ao4, _tileDefaultWhiteTex);
        bindPBRSlot(_tileAOTextures, 5, unitBase + 23, _tileUniforms.ao5, _tileDefaultWhiteTex);

        // Specular maps — units 41-46 — fallback: white (use scalar)
        bindPBRSlot(_tileSpecularTextures, 0, unitBase + 24, _tileUniforms.spec0, _tileDefaultWhiteTex);
        bindPBRSlot(_tileSpecularTextures, 1, unitBase + 25, _tileUniforms.spec1, _tileDefaultWhiteTex);
        bindPBRSlot(_tileSpecularTextures, 2, unitBase + 26, _tileUniforms.spec2, _tileDefaultWhiteTex);
        bindPBRSlot(_tileSpecularTextures, 3, unitBase + 27, _tileUniforms.spec3, _tileDefaultWhiteTex);
        bindPBRSlot(_tileSpecularTextures, 4, unitBase + 28, _tileUniforms.spec4, _tileDefaultWhiteTex);
        bindPBRSlot(_tileSpecularTextures, 5, unitBase + 29, _tileUniforms.spec5, _tileDefaultWhiteTex);

        // Emissive maps — units 47-52 — fallback: black (no glow)
        bindPBRSlot(_tileEmissiveTextures, 0, unitBase + 30, _tileUniforms.emissive0, _tileDefaultBlackTex);
        bindPBRSlot(_tileEmissiveTextures, 1, unitBase + 31, _tileUniforms.emissive1, _tileDefaultBlackTex);
        bindPBRSlot(_tileEmissiveTextures, 2, unitBase + 32, _tileUniforms.emissive2, _tileDefaultBlackTex);
        bindPBRSlot(_tileEmissiveTextures, 3, unitBase + 33, _tileUniforms.emissive3, _tileDefaultBlackTex);
        bindPBRSlot(_tileEmissiveTextures, 4, unitBase + 34, _tileUniforms.emissive4, _tileDefaultBlackTex);
        bindPBRSlot(_tileEmissiveTextures, 5, unitBase + 35, _tileUniforms.emissive5, _tileDefaultBlackTex);

        // Displacement maps — units 53-58 — fallback: black (no parallax)
        bindPBRSlot(_tileDisplacementTextures, 0, unitBase + 36, _tileUniforms.displace0, _tileDefaultBlackTex);
        bindPBRSlot(_tileDisplacementTextures, 1, unitBase + 37, _tileUniforms.displace1, _tileDefaultBlackTex);
        bindPBRSlot(_tileDisplacementTextures, 2, unitBase + 38, _tileUniforms.displace2, _tileDefaultBlackTex);
        bindPBRSlot(_tileDisplacementTextures, 3, unitBase + 39, _tileUniforms.displace3, _tileDefaultBlackTex);
        bindPBRSlot(_tileDisplacementTextures, 4, unitBase + 40, _tileUniforms.displace4, _tileDefaultBlackTex);
        bindPBRSlot(_tileDisplacementTextures, 5, unitBase + 41, _tileUniforms.displace5, _tileDefaultBlackTex);


        gl.uniform2f(_tileUniforms.gridOrigin, GRID_ORIGIN_X, GRID_ORIGIN_Y);
        gl.uniform1f(_tileUniforms.gridSize, GRID_SIZE);
        gl.uniform1f(_tileUniforms.tileSize, TILE_SIZE);
        gl.uniform3f(_tileUniforms.sunDir,
            frame.sunDirX,
            frame.sunDirY,
            frame.sunDirZ);
        gl.uniform3f(_tileUniforms.sunColor,
            frame.sunColorR ?? 1.0,
            frame.sunColorG ?? 0.95,
            frame.sunColorB ?? 0.8);
        gl.uniform1f(_tileUniforms.sunIntensity, frame.sunIntensity ?? 1.0);

        if (_tileUniforms.ambient) {
            gl.uniform3f(_tileUniforms.ambient,
                frame.ambientR ?? 0.3,
                frame.ambientG ?? 0.3,
                frame.ambientB ?? 0.3);
        }

        const tm = frame.tileMap;
        gl.uniform2f(_tileUniforms.brushPos, frame.brushWorldX ?? 0.0, frame.brushWorldY ?? 0.0);
        gl.uniform1f(_tileUniforms.brushRadius, Math.max(frame.brushRadius ?? 0.5, 0.5));
        gl.uniform1f(_tileUniforms.brushActive, frame.landscapeActive ? 1.0 : 0.0);

        // ============================================================
        // AFTER
        // ============================================================

        // uTileLightShadowSlot[i]: shadow slot index (0-7) if this light casts
        // shadows, else -1. A light's own array index IS its shadow slot when < 8
        // (shadow casters are sorted to the front of the light list in C#).
        gl.uniform1i(_tileUniforms.tileLightCount, frame.lightCount);
        for (let i = 0; i < frame.lightCount; i++) {
            gl.uniform3f(_tileUniforms.tileLightPos[i],
                frame.lightPositions[i * 3],
                frame.lightPositions[i * 3 + 1],
                frame.lightPositions[i * 3 + 2]);
            gl.uniform3f(_tileUniforms.tileLightColor[i],
                frame.lightColors[i * 3],
                frame.lightColors[i * 3 + 1],
                frame.lightColors[i * 3 + 2]);
            gl.uniform1f(_tileUniforms.tileLightIntensity[i], frame.lightIntensities[i]);
            gl.uniform1f(_tileUniforms.tileLightRange[i], frame.lightRanges[i]);
            gl.uniform1i(_tileUniforms.tileLightType[i], frame.lightTypes[i]);
            gl.uniform3f(_tileUniforms.tileLightDir[i],
                frame.lightDirections[i * 3],
                frame.lightDirections[i * 3 + 1],
                frame.lightDirections[i * 3 + 2]);
            gl.uniform1f(_tileUniforms.tileLightSpotAngle[i], frame.lightSpotAngles[i]);

            const shadowSlot = (frame.lightCastsShadows && frame.lightCastsShadows[i] && i < 8)
                ? i : -1;
            gl.uniform1i(_tileUniforms.tileLightShadowSlot[i], shadowSlot);
        }

        // Bind all active shadow-casting lights' depth maps + VPs to units 9-16.
        if (frame.lightCastsShadows) {
            for (let slot = 0; slot < 8; slot++) {
                if (slot >= frame.lightCount || !frame.lightCastsShadows[slot]) continue;
                if (!_shadowDepthTexs[slot]) continue;

                const unit = 9 + slot; // units 9-16
                gl.activeTexture(gl.TEXTURE0 + unit);
                gl.bindTexture(gl.TEXTURE_2D, _shadowDepthTexs[slot]);
                gl.uniform1i(_tileUniforms.shadowMap[slot], unit);

                if (frame.lightVPs && frame.lightVPs[slot]) {
                    gl.uniformMatrix4fv(_tileUniforms.lightVP[slot], false,
                        new Float32Array(frame.lightVPs[slot]));
                }
            }
            gl.uniform1f(_tileUniforms.shadowBias, 0.003);
        }

        if (!_chunkIndexOffsets) {
          
            gl.drawElements(gl.TRIANGLES, _tileIdxCount, gl.UNSIGNED_INT, 0);
        } else {
            const vp = frame.vp; 
            const planes = extractFrustumPlanes(vp);

            const C = CHUNKS_PER_SIDE;
            const CS = CHUNK_SIZE;
            let drawnChunks = 0;

            for (let cy = 0; cy < C; cy++) {
                for (let cx = 0; cx < C; cx++) {
                    const id = cy * C + cx;
                    const minX = GRID_ORIGIN_X + cx * CS * TILE_SIZE;
                    const minY = GRID_ORIGIN_Y + cy * CS * TILE_SIZE;
                    const maxX = minX + CS * TILE_SIZE;
                    const maxY = minY + CS * TILE_SIZE;
                    const minZ = _chunkBoundsMinZ[id];
                    const maxZ = _chunkBoundsMaxZ[id];

                    if (!aabbInFrustum(planes, minX, minY, minZ, maxX, maxY, maxZ)) continue;

                    gl.drawElements(gl.TRIANGLES,
                        _chunkIndexCounts[id],
                        gl.UNSIGNED_INT,
                        _chunkIndexOffsets[id]);
                    drawnChunks++;
                }
            }
        }

        gl.bindVertexArray(null);
    }

   
    function updateTileHeights(gl, tileData) {

  
        if (tileData.tileGridSize && tileData.tileGridSize !== GRID_SIZE) {
            teardownTileMap();
            updateGridConstants(tileData.tileGridSize);
            initTileMap(gl);
            if (_dotnetRef) {
                _dotnetRef.invokeMethodAsync('OnTileGridRebuilding')
                    .catch(() => { }); 
            }
            return;
        }

        const GS = GRID_SIZE;
        const GSP1 = GS + 1;

        if (tileData.isFullUpload) {
          
            const heights = new Float32Array(tileData.heights);
            gl.bindBuffer(gl.ARRAY_BUFFER, _tileHeightVBO);
            gl.bufferSubData(gl.ARRAY_BUFFER, 0, heights);
            updateChunkZBounds(heights);
      
            const normals = computeTileNormalsFromHeights(heights, GRID_SIZE);

            gl.bindBuffer(gl.ARRAY_BUFFER, _tileNormalVBO);
            gl.bufferSubData(gl.ARRAY_BUFFER, 0, normals);

            const matData = new Float32Array(GS * GS * 4);
            for (let i = 0; i < GS * GS; i++) {
                matData[i * 4] = tileData.materials[i];
                matData[i * 4 + 1] = tileData.blendWeights[i];
                matData[i * 4 + 2] = tileData.blendMaterials[i];
                matData[i * 4 + 3] = 0;
            }
            gl.bindTexture(gl.TEXTURE_2D, _tileMatTex);
            gl.texSubImage2D(gl.TEXTURE_2D, 0, 0, 0,
                GS, GS, gl.RGBA, gl.FLOAT, matData);

            _pendingFullSeedData = {
                materials: tileData.materials,
                blendMaterials: tileData.blendMaterials,
                blendWeights: tileData.blendWeights,
                gridSize: GS
            };

            if (_splatInitialized && _splatTextures.length > 0) {
                applySeedData(gl, _pendingFullSeedData);
                _pendingFullSeedData = null;
            }
     

        } else {
         
            const x0 = tileData.dirtyX, y0 = tileData.dirtyY;
            const w = tileData.dirtyW, h = tileData.dirtyH;

          
            if (w <= 0 || h <= 0) {
                console.warn('[TileMap] Partial upload skipped — invalid dirty rect:',
                    x0, y0, w, h, 'GridSize:', GRID_SIZE);
                return;
            }
            if (x0 < 0 || y0 < 0 || x0 + w > GRID_SIZE || y0 + h > GRID_SIZE) {
                console.warn('[TileMap] Partial upload skipped — dirty rect out of bounds:',
                    x0, y0, w, h, 'GridSize:', GRID_SIZE);
                return;
            }
            const vW = w + 1, vH = h + 1;

         
            const heights = new Float32Array(tileData.heights);
            for (let row = 0; row < vH; row++) {
                const globalY = y0 + row;
                if (globalY > GS) break;
                const byteOffset = (globalY * GSP1 + x0) * 4;
                const rowSlice = heights.subarray(row * vW, row * vW + vW);
                gl.bindBuffer(gl.ARRAY_BUFFER, _tileHeightVBO);
                gl.bufferSubData(gl.ARRAY_BUFFER, byteOffset, rowSlice);
            }
          
            const normals = computeTileNormalsFromHeights(heights, vW - 1);
            for (let row = 0; row <= h; row++) {
                const gy = y0 + row;
                if (gy > GS) break;
                const byteOffset = (gy * GSP1 + x0) * 3 * 4;
                const rowSlice = normals.subarray(row * vW * 3, (row + 1) * vW * 3);
                gl.bindBuffer(gl.ARRAY_BUFFER, _tileNormalVBO);
                gl.bufferSubData(gl.ARRAY_BUFFER, byteOffset, rowSlice);
            }

            const matPatch = new Float32Array(w * h * 4);
            for (let i = 0; i < w * h; i++) {
                matPatch[i * 4] = tileData.materials[i];
                matPatch[i * 4 + 1] = tileData.blendWeights[i];
                matPatch[i * 4 + 2] = tileData.blendMaterials[i];
                matPatch[i * 4 + 3] = 0;
            }
            gl.bindTexture(gl.TEXTURE_2D, _tileMatTex);
            gl.texSubImage2D(gl.TEXTURE_2D, 0, x0, y0,
                w, h, gl.RGBA, gl.FLOAT, matPatch);

        

        }
    }

    function uploadTilePBRTextureSet(gl, paths, targetDict, onAllDone) {
        if (!paths || paths.length === 0) {
            if (onAllDone) onAllDone();
            return;
        }

        let needed = 0;
        let loaded = 0;
        for (let i = 0; i < paths.length; i++) {
            if (paths[i]) needed++;
        }

        if (needed === 0) {
            if (onAllDone) onAllDone();
            return;
        }

        for (let i = 0; i < paths.length; i++) {
            const path = paths[i];
            if (!path) continue; 
            const slotIndex = i;
            SpectralGLLoader.onAssetRequested();
            const tex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, tex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 255, 255]));
            targetDict[slotIndex] = tex;

            const img = new Image();
            img.onload = () => {
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                gl.generateMipmap(gl.TEXTURE_2D);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.REPEAT);
                gl.bindTexture(gl.TEXTURE_2D, null);
                SpectralGLLoader.onAssetComplete();
                loaded++;
                if (loaded >= needed && onAllDone) onAllDone();
            };
            img.onerror = () => {
                console.warn('[TilePBR] Failed to load slot', slotIndex, ':', path);
                SpectralGLLoader.onAssetComplete();
                loaded++;
                if (loaded >= needed && onAllDone) onAllDone();
            };
            img.src = path;
        }
    }

    function uploadAllTilePBRTextures(gl, frame) {
        const sets = [
            { paths: frame.tileMapNormalTextures, dict: _tileNormalTextures, name: 'Normal' },
            { paths: frame.tileMapRoughnessTextures, dict: _tileRoughnessTextures, name: 'Roughness' },
            { paths: frame.tileMapMetallicTextures, dict: _tileMetallicTextures, name: 'Metallic' },
            { paths: frame.tileMapAOTextures, dict: _tileAOTextures, name: 'AO' },
            { paths: frame.tileMapSpecularTextures, dict: _tileSpecularTextures, name: 'Specular' },
            { paths: frame.tileMapEmissiveTextures, dict: _tileEmissiveTextures, name: 'Emissive' },
            { paths: frame.tileMapDisplacementTextures, dict: _tileDisplacementTextures, name: 'Displacement' },
        ];

        _tilePBRSetsTotal = 0;
        _tilePBRSetsLoaded = 0;

        for (const set of sets) {
            const hasAny = set.paths && set.paths.some(p => p != null);
            if (hasAny) _tilePBRSetsTotal++;
        }

        if (_tilePBRSetsTotal === 0) {
            _tilePBRReady = true;
            if (_dotnetRef)
                _dotnetRef.invokeMethodAsync('OnTilePBRTexturesUploaded').catch(() => { });
            return;
        }

        function onSetDone(name) {
            _tilePBRSetsLoaded++;
            if (_tilePBRSetsLoaded >= _tilePBRSetsTotal) {
                _tilePBRReady = true;
                if (_dotnetRef)
                    _dotnetRef.invokeMethodAsync('OnTilePBRTexturesUploaded').catch(() => { });
            }
        }

        for (const set of sets) {
            const hasAny = set.paths && set.paths.some(p => p != null);
            if (!hasAny) continue;
            const capturedName = set.name;
            uploadTilePBRTextureSet(gl, set.paths, set.dict,
                () => onSetDone(capturedName));
        }

    }

   
    function uploadTileTextures(gl, texturePaths) {
        let loaded = 0;
        const total = texturePaths.length;

        texturePaths.forEach((path, i) => {
            const img = new Image();
            SpectralGLLoader.onAssetRequested();
            img.onload = () => {
                const tex = gl.createTexture();
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA,
                    gl.UNSIGNED_BYTE, img);
                gl.generateMipmap(gl.TEXTURE_2D);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER,
                    gl.NEAREST_MIPMAP_LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER,
                    gl.NEAREST);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S,
                    gl.REPEAT);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T,
                    gl.REPEAT);

                _tileTextures[i] = tex;
                loaded++;
                SpectralGLLoader.onAssetComplete();

                if (loaded === total) {
                    _tileTexturesReady = true;
                    _tileMapTexturesUploaded = true;
                    if (_dotnetRef)
                        _dotnetRef.invokeMethodAsync('OnTileTexturesUploaded');
                }
            };
            img.onerror = () => console.warn('[TileMap] Failed to load: ' + path);
            img.src = path;
        });
    }


    function computeTileNormalsFromHeights(heights, gridSize) {
        const GSP1 = gridSize + 1;
        const normals = new Float32Array(GSP1 * GSP1 * 3);

        for (let y = 0; y <= gridSize; y++) {
            for (let x = 0; x <= gridSize; x++) {
                const hC = heights[y * GSP1 + x];
                const hL = x > 0 ? heights[y * GSP1 + (x - 1)] : hC;
                const hR = x < gridSize ? heights[y * GSP1 + (x + 1)] : hC;
                const hD = y > 0 ? heights[(y - 1) * GSP1 + x] : hC;
                const hU = y < gridSize ? heights[(y + 1) * GSP1 + x] : hC;

                const dX = (hR - hL) * 0.5;
                const dY = (hU - hD) * 0.5;
                const nx = -dX, ny = -dY, nz = 1.0;
                const len = Math.sqrt(nx * nx + ny * ny + nz * nz);

                const idx = (y * GSP1 + x) * 3;
                normals[idx] = nx / len;
                normals[idx + 1] = ny / len;
                normals[idx + 2] = nz / len;
            }
        }
        return normals;
    }


    // ── Splat Paint Brush Shaders ────────────────────────────────────────────
    const paintVsSrc = `#version 300 es
precision mediump float;
in vec2 aPosition;
out vec2 vUV;
void main() {
    vUV = aPosition * 0.5 + 0.5;
    gl_Position = vec4(aPosition, 0.0, 1.0);
}
`;

    const paintFsSrc = `#version 300 es
precision mediump float;

in vec2 vUV;
out vec4 fragColor;

uniform vec2  uGridOrigin;
uniform float uGridSize;
uniform float uTileSize;
uniform vec2  uBrushWorldPos;
uniform float uBrushRadius;
uniform float uPaintStrength;
uniform int   uPaintChannel;   // 0=R 1=G 2=B 3=A
uniform sampler2D uCurrentSplat;

void main() {
    // Pixel world position from UV
    vec2 worldPos = uGridOrigin + vUV * (uGridSize * uTileSize);

    // True circular radial falloff
    float dist    = length(worldPos - uBrushWorldPos);
    float falloff = 1.0 - smoothstep(0.0, uBrushRadius, dist);
    falloff       = falloff * falloff * (3.0 - 2.0 * falloff); // smoothstep curve
    float delta   = falloff * uPaintStrength;

    if (delta < 0.001) discard;

    // Read existing splat weights
    vec4 splat = texture(uCurrentSplat, vUV);

    // Add to target channel, subtract proportionally from others
    float newVal = min(splat[uPaintChannel] + delta, 1.0);
    float added  = newVal - splat[uPaintChannel];

    // Scale others down to keep sum normalised
    float otherSum = 0.0;
    for (int i = 0; i < 4; i++) {
        if (i != uPaintChannel) otherSum += splat[i];
    }

    vec4 result = splat;
    result[uPaintChannel] = newVal;
    if (otherSum > 0.0) {
        float scale = max(0.0, 1.0 - newVal) / otherSum;
        for (int i = 0; i < 4; i++) {
            if (i != uPaintChannel) result[i] = splat[i] * scale;
        }
    } else {
        for (int i = 0; i < 4; i++) {
            if (i != uPaintChannel) result[i] = 0.0;
        }
    }

    fragColor = clamp(result, 0.0, 1.0);
}
`;

    // ?? Tile Map Vertex Shader ????????????????????????????????????????????????
    const tileVsSrc = `#version 300 es
    precision mediump float;

    layout(location = 0) in vec2 aGridPos;
    layout(location = 1) in float aHeight;
    layout(location = 2) in vec3 aNormal;

    uniform mat4 uView;
    uniform mat4 uProjection;

    out vec3 vNormal;
    out vec3 vWorldPos;

    void main() {
        vec3 worldPos = vec3(aGridPos.x, aGridPos.y, aHeight);
        vWorldPos   = worldPos;
        vNormal     = aNormal;
        gl_Position = uProjection * uView * vec4(worldPos, 1.0);
    }
    `;

    // ?? Tile Map Fragment Shader ??????????????????????????????????????????????

    const tileFsSrc = `#version 300 es
precision mediump float;

in vec3 vNormal;
in vec3 vWorldPos;

uniform sampler2D uTex0;
uniform sampler2D uTex1;
uniform sampler2D uTex2;
uniform sampler2D uTex3;
uniform sampler2D uTex4;
uniform sampler2D uTex5;
uniform sampler2D uTileData;   // reserved — height pipeline, not read for material
uniform sampler2D uSplatMap0;  // R:Dirt  G:Rock  B:Grass A:Snow
uniform sampler2D uSplatMap1;  // R:Water G:Ice   B:---   A:---


uniform vec2  uGridOrigin;
uniform float uGridSize;
uniform float uTileSize;

uniform vec3  uSunDir;
uniform vec3  uSunColor;
uniform float uSunIntensity;
uniform vec3  uAmbient;
uniform vec2  uBrushPos;

const int TILE_MAX_LIGHTS = 32;
uniform int   uTileLightCount;
uniform vec3  uTileLightPos[TILE_MAX_LIGHTS];
uniform vec3  uTileLightColor[TILE_MAX_LIGHTS];
uniform float uTileLightIntensity[TILE_MAX_LIGHTS];
uniform float uTileLightRange[TILE_MAX_LIGHTS];
uniform int   uTileLightType[TILE_MAX_LIGHTS];
uniform vec3  uTileLightDir[TILE_MAX_LIGHTS];
uniform float uTileLightSpotAngle[TILE_MAX_LIGHTS];
uniform int   uTileLightShadowSlot[TILE_MAX_LIGHTS];

uniform float uBrushRadius;
uniform float uBrushActive;

const int TILE_MAX_SHADOW_LIGHTS = 8;
uniform sampler2D uShadowMap0;
uniform sampler2D uShadowMap1;
uniform sampler2D uShadowMap2;
uniform sampler2D uShadowMap3;
uniform sampler2D uShadowMap4;
uniform sampler2D uShadowMap5;
uniform sampler2D uShadowMap6;
uniform sampler2D uShadowMap7;
uniform mat4      uLightVP0;
uniform mat4      uLightVP1;
uniform mat4      uLightVP2;
uniform mat4      uLightVP3;
uniform mat4      uLightVP4;
uniform mat4      uLightVP5;
uniform mat4      uLightVP6;
uniform mat4      uLightVP7;
uniform float     uShadowBias;
uniform float     uShadowMapSize;


out vec4 fragColor;

vec4 sampleMaterial(int idx, vec2 uv) {
    if (idx == 0) return texture(uTex0, uv);
    if (idx == 1) return texture(uTex1, uv);
    if (idx == 2) return texture(uTex2, uv);
    if (idx == 3) return texture(uTex3, uv);
    if (idx == 4) return texture(uTex4, uv);
    if (idx == 5) return texture(uTex5, uv);
    return texture(uTex2, uv);
}

vec4 triplanar(int idx, vec3 worldPos, vec3 norm) {
    vec2 uvXY = worldPos.xy * 0.5;
    vec2 uvXZ = worldPos.xz * 0.5;
    vec2 uvYZ = worldPos.yz * 0.5;

    vec4 colXY = sampleMaterial(idx, uvXY);
    vec4 colXZ = sampleMaterial(idx, uvXZ);
    vec4 colYZ = sampleMaterial(idx, uvYZ);

    vec3 blend = abs(norm);
    blend = max(blend - 0.2, 0.0);
    blend /= (blend.x + blend.y + blend.z);

    return colXY * blend.z + colXZ * blend.y + colYZ * blend.x;
}

float sampleTileShadowMap(int index, vec2 uv) {
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

vec4 getTileLightVPPos(int index, vec4 worldPos) {
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

float tileShadowFactor(int lightIndex, vec3 worldPos, vec3 norm, vec3 lightDir) {
    vec4 shadowCoord = getTileLightVPPos(lightIndex, vec4(worldPos, 1.0));
    vec3 proj = shadowCoord.xyz / shadowCoord.w;
    proj = proj * 0.5 + 0.5;

    if (proj.x < 0.0 || proj.x > 1.0 ||
        proj.y < 0.0 || proj.y > 1.0 ||
        proj.z > 1.0) return 1.0;

    float currentDepth = proj.z;
    float cosTheta     = clamp(dot(norm, lightDir), 0.0, 1.0);
    float bias         = mix(0.005, 0.001, cosTheta) + uShadowBias;

    float shadow   = 0.0;
    vec2 texelSize = vec2(1.0 / 1024.0);
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            float pcfDepth = sampleTileShadowMap(lightIndex,
                proj.xy + vec2(float(x), float(y)) * texelSize);
            shadow += currentDepth - bias > pcfDepth ? 0.0 : 1.0;
        }
    }
    return shadow / 9.0;
}

// Sample a tile's colour at a given tileUV position
vec4 sampleTileColor(vec2 splatUV, vec3 norm) {
    // Sample both splat textures at continuous world-space UV
    // This is what makes brushes truly circular — no tile snapping
    vec4 splat0 = texture(uSplatMap0, splatUV); // Dirt Rock Grass Snow
    vec4 splat1 = texture(uSplatMap1, splatUV); // Water Ice

    // Slope auto-rock — overrides paint on steep faces
    float slopeAngle = 1.0 - abs(dot(norm, vec3(0.0, 0.0, 1.0)));
    float autoRock   = smoothstep(0.55, 0.75, slopeAngle);
    float triBlend   = smoothstep(0.35, 0.65, slopeAngle);
    vec2  flatUV     = vWorldPos.xy * 0.5;

    // Sample all 6 material textures
    vec4 colDirt  = mix(sampleMaterial(0, flatUV), triplanar(0, vWorldPos, norm), triBlend);
    vec4 colRock  = mix(sampleMaterial(1, flatUV), triplanar(1, vWorldPos, norm), triBlend);
    vec4 colGrass = mix(sampleMaterial(2, flatUV), triplanar(2, vWorldPos, norm), triBlend);
    vec4 colSnow  = mix(sampleMaterial(3, flatUV), triplanar(3, vWorldPos, norm), triBlend);
    vec4 colWater = mix(sampleMaterial(4, flatUV), triplanar(4, vWorldPos, norm), triBlend);
    vec4 colIce   = mix(sampleMaterial(5, flatUV), triplanar(5, vWorldPos, norm), triBlend);

    // Weighted blend across all 6 channels
    vec4 painted =
        splat0.r * colDirt  +
        splat0.g * colRock  +
        splat0.b * colGrass +
        splat0.a * colSnow  +
        splat1.r * colWater +
        splat1.g * colIce;

    // Auto-rock on slopes overrides painted material
    return mix(painted, colRock, autoRock);
}

void main() {
    // ── Continuous world-space splat UV — the fix for square brushes ────────
    // Single sample at pixel world position, no tile snapping, no 4-corner loop
    vec3 norm    = normalize(vNormal);
    vec2 splatUV = (vWorldPos.xy - uGridOrigin) / (uGridSize * uTileSize);
    splatUV      = clamp(splatUV, 0.0, 1.0);

    vec4 albedo  = sampleTileColor(splatUV, norm);



    // Lighting — unchanged from your original
  // Sun — always assumed to occupy shadow slot 0 when it casts shadows.
    // (Matches the C# nearest-light sort: shadow casters first, sun is
    // typically first since it's added before point lights in Scene.Lights.)
    vec3 lightDir = normalize(-uSunDir);
    float diff    = max(dot(norm, lightDir), 0.0);
    float shadow  = tileShadowFactor(0, vWorldPos, norm, lightDir);
    vec3 lighting = uAmbient + uSunColor * uSunIntensity * diff * shadow;

    for (int i = 0; i < TILE_MAX_LIGHTS; i++) {
        if (i >= uTileLightCount) break;
        if (uTileLightType[i] == 1) continue;

        vec3 toLightDir;
        float attenuation;

        if (uTileLightType[i] == 2) {
            vec3 toLight   = uTileLightPos[i] - vWorldPos;
            float distance = length(toLight);
            toLightDir     = normalize(toLight);
            attenuation    = 1.0 / (1.0 + (distance * distance) /
                (uTileLightRange[i] * uTileLightRange[i]));
            attenuation    = attenuation * attenuation * attenuation;
            float cosAngle = cos(radians(uTileLightSpotAngle[i]));
            float cosOuter = cos(radians(uTileLightSpotAngle[i] * 1.3));
            float spotDot  = dot(-toLightDir, normalize(uTileLightDir[i]));
            attenuation   *= smoothstep(cosOuter, cosAngle, spotDot);
        } else if (uTileLightType[i] == 3) {
            vec3 upRef     = abs(uTileLightDir[i].z) < 0.9 ? vec3(0.0, 0.0, 1.0) : vec3(0.0, 1.0, 0.0);
            vec3 areaRight = normalize(cross(uTileLightDir[i], upRef));
            vec3 areaUp    = normalize(cross(areaRight, uTileLightDir[i]));
            float hw       = uTileLightSpotAngle[i] * 0.1;
            float hh       = uTileLightRange[i] * 0.05;
            vec3 c0 = uTileLightPos[i] + areaRight * hw + areaUp * hh;
            vec3 c1 = uTileLightPos[i] - areaRight * hw + areaUp * hh;
            vec3 c2 = uTileLightPos[i] + areaRight * hw - areaUp * hh;
            vec3 c3 = uTileLightPos[i] - areaRight * hw - areaUp * hh;
            toLightDir  = normalize(
                normalize(c0 - vWorldPos) + normalize(c1 - vWorldPos) +
                normalize(c2 - vWorldPos) + normalize(c3 - vWorldPos));
            float dist  = length(uTileLightPos[i] - vWorldPos);
            attenuation = 1.0 / (1.0 + (dist * dist) /
                (uTileLightRange[i] * uTileLightRange[i]));
            attenuation = attenuation * attenuation;
        } else {
            vec3 toLight   = uTileLightPos[i] - vWorldPos;
            float distance = length(toLight);
            toLightDir     = normalize(toLight);
            attenuation    = 1.0 / (1.0 + (distance * distance) /
                (uTileLightRange[i] * uTileLightRange[i]));
            attenuation    = attenuation * attenuation * attenuation;
        }

 float tileDiff = max(dot(norm, toLightDir), 0.0);

        // Point/spot/area lights — apply shadow if this light landed in a
        // shadow slot (1-7; slot 0 is reserved for the sun above).
        float pointShadow = 1.0;
        if (uTileLightShadowSlot[i] >= 0) {
            pointShadow = tileShadowFactor(uTileLightShadowSlot[i], vWorldPos, norm, toLightDir);
        }

        lighting += uTileLightColor[i] * uTileLightIntensity[i] * tileDiff * attenuation * pointShadow;
    }

    lighting = clamp(lighting, 0.0, 2.0);

    float brushDist = length(vWorldPos.xy - uBrushPos);
    float ringInner = uBrushRadius - 0.15;
    float ringOuter = uBrushRadius + 0.15;
    float ring      = smoothstep(ringInner - 0.1, ringInner, brushDist) *
                      (1.0 - smoothstep(ringOuter, ringOuter + 0.1, brushDist));
    vec3 ringColor  = vec3(1.0, 1.0, 0.3);
    vec3 litAlbedo  = albedo.rgb * lighting;
    fragColor = vec4(mix(litAlbedo, ringColor, ring * uBrushActive * 0.85), albedo.a);
}
`;


    // ============================================================
    // FOLIAGE SYSTEM — instanced static props (bushes, trees, rocks, grass)
    // ============================================================
    let _foliageColorProgram = null;
    let _foliageShadowProgram = null;
    let _foliageColorLocs = null;
    let _foliageShadowLocs = null;

    // Per-group instance VBO cache, keyed by MeshId ("Bush001", "Tree002", etc.)
    // Rebuilt every frame since C# sends only frustum-culled instances per frame.
    let _foliageInstanceVBOs = {};      // meshId -> WebGLBuffer
    let _foliageInstanceCapacity = {};  // meshId -> allocated instance count (for bufferData sizing)


    // Packs one FoliageInstanceGroup into a Float32Array laid out as
    // [x, y, z, scale, rotZ] per instance, and uploads it to a cached
    // per-meshId VBO — reallocating only when the instance count grows
    // beyond the buffer's current capacity.


    // Packs one FoliageInstanceGroup into a Float32Array laid out as
    // [x, y, z, scale, rotZ] per instance, and uploads it to a cached
    // per-meshId VBO — reallocating only when the instance count grows
    // beyond the buffer's current capacity.
    function uploadFoliageInstances(gl, group) {
        const count = group.count;
        if (count === 0) return null;

        const FLOATS_PER_INSTANCE = 5;
        const data = new Float32Array(count * FLOATS_PER_INSTANCE);

        for (let i = 0; i < count; i++) {
            const base = i * FLOATS_PER_INSTANCE;
            data[base] = group.positions[i * 3];
            data[base + 1] = group.positions[i * 3 + 1];
            data[base + 2] = group.positions[i * 3 + 2];
            data[base + 3] = group.scales[i];
            data[base + 4] = group.rotations[i];
        }

        let vbo = _foliageInstanceVBOs[group.meshId];
        const capacity = _foliageInstanceCapacity[group.meshId] || 0;

        if (!vbo) {
            vbo = gl.createBuffer();
            _foliageInstanceVBOs[group.meshId] = vbo;
        }

        gl.bindBuffer(gl.ARRAY_BUFFER, vbo);

        if (count > capacity) {
            const newCapacity = Math.ceil(count * 1.25);
            gl.bufferData(gl.ARRAY_BUFFER, newCapacity * FLOATS_PER_INSTANCE * 4, gl.DYNAMIC_DRAW);
            _foliageInstanceCapacity[group.meshId] = newCapacity;
            gl.bufferSubData(gl.ARRAY_BUFFER, 0, data);
        } else {
            gl.bufferSubData(gl.ARRAY_BUFFER, 0, data);
        }

        return vbo;
    }




    function uploadAllFoliageInstances(gl, frame) {
        if (!frame.foliageInstances) return;
        for (const group of frame.foliageInstances) {
            if (group.count === 0) continue;
            uploadFoliageInstances(gl, group);
        }
    }

    function initFoliageSystem() {
        const gl = _gl;

        // ── Color program ──────────────────────────────────────────────────
        _foliageColorProgram = buildProgram(foliageVsSrc, foliageFsSrc);

        _foliageColorLocs = {
            // Per-vertex attributes
            pos: gl.getAttribLocation(_foliageColorProgram, "aPosition"),
            norm: gl.getAttribLocation(_foliageColorProgram, "aNormal"),
            texCoord: gl.getAttribLocation(_foliageColorProgram, "aTexCoord"),
            // Per-instance attributes
            instOffset: gl.getAttribLocation(_foliageColorProgram, "aInstOffset"),
            instScale: gl.getAttribLocation(_foliageColorProgram, "aInstScale"),
            instRotZ: gl.getAttribLocation(_foliageColorProgram, "aInstRotZ"),

            // Uniforms
            view: gl.getUniformLocation(_foliageColorProgram, "uView"),
            projection: gl.getUniformLocation(_foliageColorProgram, "uProjection"),
            color: gl.getUniformLocation(_foliageColorProgram, "uColor"),
            camPos: gl.getUniformLocation(_foliageColorProgram, "uCamPos"),
            tex: gl.getUniformLocation(_foliageColorProgram, "uTexture"),
            hasTex: gl.getUniformLocation(_foliageColorProgram, "uHasTexture"),

            lightCount: gl.getUniformLocation(_foliageColorProgram, "uLightCount"),
            lightPos: [],
            lightColor: [],
            lightDir: [],
            lightIntensity: [],
            lightRange: [],
            lightType: [],
            lightSpotAngle: [],

            shadowMap0: gl.getUniformLocation(_foliageColorProgram, "uShadowMap0"),
            lightVP0: gl.getUniformLocation(_foliageColorProgram, "uLightVP0"),
        };

        for (let i = 0; i < 32; i++) {
            _foliageColorLocs.lightPos[i] = gl.getUniformLocation(_foliageColorProgram, `uLightPos[${i}]`);
            _foliageColorLocs.lightColor[i] = gl.getUniformLocation(_foliageColorProgram, `uLightColor[${i}]`);
            _foliageColorLocs.lightDir[i] = gl.getUniformLocation(_foliageColorProgram, `uLightDir[${i}]`);
            _foliageColorLocs.lightIntensity[i] = gl.getUniformLocation(_foliageColorProgram, `uLightIntensity[${i}]`);
            _foliageColorLocs.lightRange[i] = gl.getUniformLocation(_foliageColorProgram, `uLightRange[${i}]`);
            _foliageColorLocs.lightType[i] = gl.getUniformLocation(_foliageColorProgram, `uLightType[${i}]`);
            _foliageColorLocs.lightSpotAngle[i] = gl.getUniformLocation(_foliageColorProgram, `uLightSpotAngle[${i}]`);
        }

        // ── Shadow program ──────────────────────────────────────────────────
        _foliageShadowProgram = buildProgram(foliageShadowVsSrc, foliageShadowFsSrc);

        _foliageShadowLocs = {
            pos: gl.getAttribLocation(_foliageShadowProgram, "aPosition"),
            texCoord: gl.getAttribLocation(_foliageShadowProgram, "aTexCoord"),
            instOffset: gl.getAttribLocation(_foliageShadowProgram, "aInstOffset"),
            instScale: gl.getAttribLocation(_foliageShadowProgram, "aInstScale"),
            instRotZ: gl.getAttribLocation(_foliageShadowProgram, "aInstRotZ"),

            lightVP: gl.getUniformLocation(_foliageShadowProgram, "uLightVP"),
            shadowTexture: gl.getUniformLocation(_foliageShadowProgram, "uShadowTexture"),
            shadowHasTexture: gl.getUniformLocation(_foliageShadowProgram, "uShadowHasTexture"),
            shadowAlphaThreshold: gl.getUniformLocation(_foliageShadowProgram, "uShadowAlphaThreshold"),
        };

        console.log('[Foliage] Shaders compiled — color:', !!_foliageColorProgram,
            'shadow:', !!_foliageShadowProgram);
    }


    function renderFoliageColor(gl, frame) {
        if (!_foliageColorProgram || !frame.foliageInstances || frame.foliageInstances.length === 0) return;

        gl.useProgram(_foliageColorProgram);
        const locs = _foliageColorLocs;

        gl.uniformMatrix4fv(locs.view, false, frame.viewMatrix);
        gl.uniformMatrix4fv(locs.projection, false, frame.projMatrix);
        gl.uniform3f(locs.camPos, frame.camX, frame.camY, frame.camZ);

        gl.uniform1i(locs.lightCount, frame.lightCount);
        for (let i = 0; i < frame.lightCount; i++) {
            gl.uniform3f(locs.lightPos[i],
                frame.lightPositions[i * 3], frame.lightPositions[i * 3 + 1], frame.lightPositions[i * 3 + 2]);
            gl.uniform3f(locs.lightColor[i],
                frame.lightColors[i * 3], frame.lightColors[i * 3 + 1], frame.lightColors[i * 3 + 2]);
            gl.uniform3f(locs.lightDir[i],
                frame.lightDirections[i * 3], frame.lightDirections[i * 3 + 1], frame.lightDirections[i * 3 + 2]);
            gl.uniform1f(locs.lightIntensity[i], frame.lightIntensities[i]);
            gl.uniform1f(locs.lightRange[i], frame.lightRanges[i]);
            gl.uniform1i(locs.lightType[i], frame.lightTypes[i]);
            gl.uniform1f(locs.lightSpotAngle[i], frame.lightSpotAngles[i]);
        }

        if (_shadowDepthTexs[0]) {
            gl.activeTexture(gl.TEXTURE1);
            gl.bindTexture(gl.TEXTURE_2D, _shadowDepthTexs[0]);
            gl.uniform1i(locs.shadowMap0, 1);
            if (frame.lightVPs && frame.lightVPs[0]) {
                gl.uniformMatrix4fv(locs.lightVP0, false, new Float32Array(frame.lightVPs[0]));
            }
        }

        gl.enable(gl.DEPTH_TEST);
        gl.depthMask(true);
        gl.disable(gl.BLEND);

        for (const group of frame.foliageInstances) {
            if (group.count === 0) continue;

            const buf = _meshBuffers[group.meshId];
            if (!buf) {
                console.warn('[Foliage] No mesh buffer for:', group.meshId);
                continue;
            }

            const instVbo = _foliageInstanceVBOs[group.meshId];
            if (!instVbo) continue;

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
            gl.enableVertexAttribArray(locs.pos);
            gl.vertexAttribPointer(locs.pos, 3, gl.FLOAT, false, 0, 0);
            gl.vertexAttribDivisor(locs.pos, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.nbo);
            gl.enableVertexAttribArray(locs.norm);
            gl.vertexAttribPointer(locs.norm, 3, gl.FLOAT, false, 0, 0);
            gl.vertexAttribDivisor(locs.norm, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
            gl.enableVertexAttribArray(locs.texCoord);
            gl.vertexAttribPointer(locs.texCoord, 2, gl.FLOAT, false, 0, 0);
            gl.vertexAttribDivisor(locs.texCoord, 0);

            const stride = 5 * 4;
            gl.bindBuffer(gl.ARRAY_BUFFER, instVbo);

            gl.enableVertexAttribArray(locs.instOffset);
            gl.vertexAttribPointer(locs.instOffset, 3, gl.FLOAT, false, stride, 0);
            gl.vertexAttribDivisor(locs.instOffset, 1);

            gl.enableVertexAttribArray(locs.instScale);
            gl.vertexAttribPointer(locs.instScale, 1, gl.FLOAT, false, stride, 12);
            gl.vertexAttribDivisor(locs.instScale, 1);

            gl.enableVertexAttribArray(locs.instRotZ);
            gl.vertexAttribPointer(locs.instRotZ, 1, gl.FLOAT, false, stride, 16);
            gl.vertexAttribDivisor(locs.instRotZ, 1);

            if (buf.matBreaks && buf.matBreaks.length >= 1) {
                let offsets = [], running = 0;
                for (let i = 0; i < buf.matBreaks.length; i++) {
                    offsets[i] = running;
                    running += buf.matBreaks[i];
                }
                for (let m = 0; m < buf.matBreaks.length; m++) {
                    const matIdx = buf.matIndices[m];
                    const matTex = buf.materialTextures[matIdx];
                    const matLoaded = buf.matTexLoaded[matIdx];

                    if (matTex && matLoaded) {
                        gl.activeTexture(gl.TEXTURE0);
                        gl.bindTexture(gl.TEXTURE_2D, matTex);
                        gl.uniform1i(locs.tex, 0);
                        gl.uniform1i(locs.hasTex, 1);
                        gl.uniform4f(locs.color, 1.0, 1.0, 1.0, 1.0);
                    } else if (buf.materialColors && buf.materialColors[matIdx]) {
                        const parts = buf.materialColors[matIdx].split(',');
                        gl.uniform4f(locs.color,
                            parseFloat(parts[0]), parseFloat(parts[1]), parseFloat(parts[2]), 1.0);
                        gl.uniform1i(locs.hasTex, 0);
                    } else if (buf.hasTexture && _textureCache[buf.texCacheKey]) {
                        gl.activeTexture(gl.TEXTURE0);
                        gl.bindTexture(gl.TEXTURE_2D, _textureCache[buf.texCacheKey]);
                        gl.uniform1i(locs.tex, 0);
                        gl.uniform1i(locs.hasTex, 1);
                        gl.uniform4f(locs.color, 1.0, 1.0, 1.0, 1.0);
                    } else {
                        gl.uniform4f(locs.color, 1.0, 1.0, 1.0, 1.0);
                        gl.uniform1i(locs.hasTex, 0);
                    }

                    gl.drawArraysInstanced(gl.TRIANGLES, offsets[m], buf.matBreaks[m], group.count);
                }
            } else {
                // single-material path (your current fixed code)
                if (buf.materialTextures && buf.materialTextures[0] && buf.matTexLoaded[0]) {
                    gl.activeTexture(gl.TEXTURE0);
                    gl.bindTexture(gl.TEXTURE_2D, buf.materialTextures[0]);
                    gl.uniform1i(locs.tex, 0);
                    gl.uniform1i(locs.hasTex, 1);
                    gl.uniform4f(locs.color, 1.0, 1.0, 1.0, 1.0);
                } else if (buf.hasTexture && _textureCache[buf.texCacheKey]) {
                    gl.activeTexture(gl.TEXTURE0);
                    gl.bindTexture(gl.TEXTURE_2D, _textureCache[buf.texCacheKey]);
                    gl.uniform1i(locs.tex, 0);
                    gl.uniform1i(locs.hasTex, 1);
                    gl.uniform4f(locs.color, 1.0, 1.0, 1.0, 1.0);
                } else if (buf.materialColors && buf.materialColors[0]) {
                    const parts = buf.materialColors[0].split(',');
                    gl.uniform4f(locs.color,
                        parseFloat(parts[0]), parseFloat(parts[1]), parseFloat(parts[2]), 1.0);
                    gl.uniform1i(locs.hasTex, 0);
                } else {
                    gl.uniform4f(locs.color, 1.0, 1.0, 1.0, 1.0);
                    gl.uniform1i(locs.hasTex, 0);
                }

                gl.drawArraysInstanced(gl.TRIANGLES, 0, buf.vertCount, group.count);
            }
         

            gl.drawArraysInstanced(gl.TRIANGLES, 0, buf.vertCount, group.count);

            gl.vertexAttribDivisor(locs.instOffset, 0);
            gl.vertexAttribDivisor(locs.instScale, 0);
            gl.vertexAttribDivisor(locs.instRotZ, 0);
        }
    }


    function renderFoliageShadow(gl, frame, lightVP) {
        if (!_foliageShadowProgram || !frame.foliageInstances || frame.foliageInstances.length === 0) return;

        gl.useProgram(_foliageShadowProgram);
        const locs = _foliageShadowLocs;

        gl.uniformMatrix4fv(locs.lightVP, false, lightVP);

        for (const group of frame.foliageInstances) {
            if (group.count === 0) continue;

            const buf = _meshBuffers[group.meshId];
            if (!buf) continue;

            // Reuse the same instance VBO the color pass already uploaded this frame —
            // renderFoliageColor must run before renderFoliageShadow each frame.
            const instVbo = _foliageInstanceVBOs[group.meshId];
            if (!instVbo) continue;

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
            gl.enableVertexAttribArray(locs.pos);
            gl.vertexAttribPointer(locs.pos, 3, gl.FLOAT, false, 0, 0);
            gl.vertexAttribDivisor(locs.pos, 0);

            gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
            gl.enableVertexAttribArray(locs.texCoord);
            gl.vertexAttribPointer(locs.texCoord, 2, gl.FLOAT, false, 0, 0);
            gl.vertexAttribDivisor(locs.texCoord, 0);

            const stride = 5 * 4;
            gl.bindBuffer(gl.ARRAY_BUFFER, instVbo);

            gl.enableVertexAttribArray(locs.instOffset);
            gl.vertexAttribPointer(locs.instOffset, 3, gl.FLOAT, false, stride, 0);
            gl.vertexAttribDivisor(locs.instOffset, 1);

            gl.enableVertexAttribArray(locs.instScale);
            gl.vertexAttribPointer(locs.instScale, 1, gl.FLOAT, false, stride, 12);
            gl.vertexAttribDivisor(locs.instScale, 1);

            gl.enableVertexAttribArray(locs.instRotZ);
            gl.vertexAttribPointer(locs.instRotZ, 1, gl.FLOAT, false, stride, 16);
            gl.vertexAttribDivisor(locs.instRotZ, 1);

            // Alpha-cutout support — primary texture, option (b)
            const hasTex = buf.hasTexture && buf.materialTextures && buf.materialTextures[0] && buf.matTexLoaded[0];
            gl.uniform1i(locs.shadowHasTexture, hasTex ? 1 : 0);
            if (hasTex) {
                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, buf.materialTextures[0]);
                gl.uniform1i(locs.shadowTexture, 0);
                gl.uniform1f(locs.shadowAlphaThreshold, 0.1);
            }

            gl.drawArraysInstanced(gl.TRIANGLES, 0, buf.vertCount, group.count);

            gl.vertexAttribDivisor(locs.instOffset, 0);
            gl.vertexAttribDivisor(locs.instScale, 0);
            gl.vertexAttribDivisor(locs.instRotZ, 0);
        }
    }
    function resetFoliage() {
        const gl = _gl;
        if (!gl) return;

        for (const meshId in _foliageInstanceVBOs) {
            const vbo = _foliageInstanceVBOs[meshId];
            if (vbo) gl.deleteBuffer(vbo);
        }
        _foliageInstanceVBOs = {};
        _foliageInstanceCapacity = {};

        console.log('[Foliage] Reset — instance buffers cleared');
    }

    // ── Foliage Instanced Color Shaders ──────────────────────────────────────
    const foliageVsSrc = `#version 300 es
precision mediump float;

in vec3 aPosition;
in vec3 aNormal;
in vec2 aTexCoord;

// Per-instance attributes (divisor = 1)
in vec3 aInstOffset;   // world-space x,y,z
in float aInstScale;   // uniform scale
in float aInstRotZ;    // rotation around Z (radians)

uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vNormal;
out vec3 vWorldPos;
out vec2 vTexCoord;

void main() {
    float c = cos(aInstRotZ);
    float s = sin(aInstRotZ);

    // Rotate around Z, then scale, then translate — matches C# rotZ baked at scatter time
    vec3 scaled = aPosition * aInstScale;
    vec3 rotated = vec3(
        scaled.x * c - scaled.y * s,
        scaled.x * s + scaled.y * c,
        scaled.z
    );
    vec3 worldPos = rotated + aInstOffset;

    vec3 rotatedNormal = vec3(
        aNormal.x * c - aNormal.y * s,
        aNormal.x * s + aNormal.y * c,
        aNormal.z
    );

    vWorldPos = worldPos;
    vNormal = normalize(rotatedNormal);
    vTexCoord = aTexCoord;

    gl_Position = uProjection * uView * vec4(worldPos, 1.0);
}
`;

    const foliageFsSrc = `#version 300 es
precision mediump float;

in vec3 vNormal;
in vec3 vWorldPos;
in vec2 vTexCoord;

const int MAX_LIGHTS = 32;
const int MAX_SHADOW_LIGHTS = 8;

uniform int uLightCount;
uniform vec3 uLightPos[MAX_LIGHTS];
uniform vec3 uLightColor[MAX_LIGHTS];
uniform vec3 uLightDir[MAX_LIGHTS];
uniform float uLightIntensity[MAX_LIGHTS];
uniform float uLightRange[MAX_LIGHTS];
uniform int uLightType[MAX_LIGHTS];
uniform float uLightSpotAngle[MAX_LIGHTS];

uniform vec4 uColor;
uniform vec3 uCamPos;
uniform sampler2D uTexture;
uniform bool uHasTexture;

uniform sampler2D uShadowMap0;
uniform mat4 uLightVP0;
// Foliage only casts/receives shadow from the sun (slot 0) — see engine notes

out vec4 fragColor;

float shadowFactor(vec3 normal, vec3 lightDir) {
    vec4 shadowCoord = uLightVP0 * vec4(vWorldPos, 1.0);
    vec3 proj = shadowCoord.xyz / shadowCoord.w;
    proj = proj * 0.5 + 0.5;
    if (proj.x < 0.0 || proj.x > 1.0 ||
        proj.y < 0.0 || proj.y > 1.0 ||
        proj.z > 1.0) return 1.0;

    float currentDepth = proj.z;
    float cosTheta = clamp(dot(normal, lightDir), 0.0, 1.0);
    float bias = mix(0.005, 0.001, cosTheta);

    float shadow = 0.0;
    vec2 texelSize = vec2(1.0 / 1024.0);
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            float pcfDepth = texture(uShadowMap0,
                proj.xy + vec2(float(x), float(y)) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 0.0 : 1.0;
        }
    }
    return shadow / 9.0;
}

void main() {
    vec4 baseColor = uHasTexture ? texture(uTexture, vTexCoord) * uColor : uColor;
    if (baseColor.a < 0.1) discard;

    vec3 normal = normalize(gl_FrontFacing ? vNormal : -vNormal);
    vec3 totalDiffuse = vec3(0.0);

    for (int i = 0; i < MAX_LIGHTS; i++) {
        if (i >= uLightCount) break;

        vec3 lightDir;
        float attenuation;

        if (uLightType[i] == 1) {
            lightDir = normalize(-uLightDir[i]);
            attenuation = 1.0;
        } else {
            vec3 toLight = uLightPos[i] - vWorldPos;
            float distance = length(toLight);
            lightDir = normalize(toLight);
            attenuation = 1.0 / (1.0 + (distance * distance) /
                (uLightRange[i] * uLightRange[i]));
            attenuation = attenuation * attenuation * attenuation;
        }

        float diff = max(dot(normal, lightDir), 0.0);
        float shadow = (uLightType[i] == 1) ? shadowFactor(normal, lightDir) : 1.0;

        totalDiffuse += shadow * diff * uLightColor[i] * uLightIntensity[i] * attenuation;
    }

    fragColor = vec4(totalDiffuse * baseColor.rgb, baseColor.a);
}
`;


    // ── Foliage Instanced Shadow Shaders ─────────────────────────────────────
    const foliageShadowVsSrc = `#version 300 es
precision mediump float;

in vec3 aPosition;
in vec2 aTexCoord;

// Per-instance attributes (divisor = 1) — same layout as color pass
in vec3 aInstOffset;
in float aInstScale;
in float aInstRotZ;

uniform mat4 uLightVP;

out vec2 vTexCoord;

void main() {
    float c = cos(aInstRotZ);
    float s = sin(aInstRotZ);

    vec3 scaled = aPosition * aInstScale;
    vec3 rotated = vec3(
        scaled.x * c - scaled.y * s,
        scaled.x * s + scaled.y * c,
        scaled.z
    );
    vec3 worldPos = rotated + aInstOffset;

    vTexCoord = aTexCoord;
    gl_Position = uLightVP * vec4(worldPos, 1.0);
}
`;

    const foliageShadowFsSrc = `#version 300 es
precision mediump float;

in vec2 vTexCoord;

uniform sampler2D uShadowTexture;
uniform bool uShadowHasTexture;
uniform float uShadowAlphaThreshold;

out vec4 fragColor;

void main() {
    if (uShadowHasTexture) {
        float a = texture(uShadowTexture, vTexCoord).a;
        if (a < uShadowAlphaThreshold) discard;
    }
    fragColor = vec4(1.0);
}
`;


    // ============================================================
    // Shaders
    // ============================================================


    const shadowVsSource = `#version 300 es
in vec3 aPosition;
in float aHeight;
in vec2 aTexCoord;
in vec3 aInstancePos;

uniform mat4 uLightVP;
uniform mat4 uModel;
uniform int uIsInstanced;

out vec2 vTexCoord;

void main() {
    vec3 worldPos = aPosition;

    if (uIsInstanced == 1) {
        float c = 1.0;
        float s = 0.0;
        worldPos = vec3(
            aPosition.x * c - aPosition.y * s + aInstancePos.x,
            aPosition.x * s + aPosition.y * c + aInstancePos.y,
            aPosition.z + aInstancePos.z
        );
        gl_Position = uLightVP * vec4(worldPos, 1.0);
    } else if (uIsInstanced == 2) {
        // Tile map path — XY from aPosition, Z from aHeight
        worldPos = vec3(aPosition.x, aPosition.y, aHeight);
        gl_Position = uLightVP * vec4(worldPos, 1.0);
    } else {
        gl_Position = uLightVP * uModel * vec4(aPosition, 1.0);
        gl_Position.z -= 0.01;
    }

    vTexCoord = aTexCoord;
}
`;

    const shadowFsSource = `#version 300 es
precision mediump float;

in vec2 vTexCoord;

uniform sampler2D uShadowTexture;
uniform bool uShadowHasTexture;
uniform float uShadowAlphaThreshold;

out vec4 fragColor;

void main() {
    if (uShadowHasTexture) {
        float a = texture(uShadowTexture, vTexCoord).a;
        if (a < uShadowAlphaThreshold) discard;
    }
    fragColor = vec4(1.0);
}
`;





    function compileShader(type, src) {
        const s = _gl.createShader(type);
        _gl.shaderSource(s, src);
        _gl.compileShader(s);
        if (!_gl.getShaderParameter(s, _gl.COMPILE_STATUS)) {
            console.error("Shader error:", _gl.getShaderInfoLog(s));
            return null;
        }
        return s;
    }


    function init(canvasRef, dotnetRef) {
        _canvas = canvasRef instanceof HTMLCanvasElement ? canvasRef : null;
        if (!_canvas) { console.error("SpectralGL: no canvas"); return; }
        console.log("[SpectralGL] Canvas size at init:", _canvas.width, _canvas.height);
        if (window.SpectralWebGPUInterop) window.SpectralWebGPUInterop.reset();
        _textureReady = {};
        _textureCache = {};
        _meshBuffers = {};   
        _tileProgram = null;
        _tileGridVAO = null;
        _tileGridVBO = null;
        _tileHeightVBO = null;
        _tileNormalVBO = null;
        _tileIBO = null;
        _tileMatTex = null;
        _tileTextures = {};
        _tileTexturesReady = false;
        _tileMapTexturesUploaded = false;
        _tileUniforms = null;
        _tileIdxCount = 0;
        _chunkIndexOffsets = null;
        _chunkIndexCounts = null;
        _chunkBoundsMinZ = null;
        _chunkBoundsMaxZ = null;
        _lastKnownGridSize = 0;
        window.SpectralTextureUploads.reset();
        window._staticObjectsPreloaded = false;
        window.SpectralStaticObjectsSystem.reset();
        window.SpectralTextSystem.reset();
        window._tileGridReady = false;
        _initialized = false;
        console.log('[SpectralGL] TileMap state cleared on init — fresh context');

        for (const [meshName, cached] of Object.entries(_parsedMeshCache)) {       
            if (!_meshBuffers[meshName]) {  
                _pendingUploads.push({
                    meshName, data: cached.data,
                    textures: cached.textures, materialColors: cached.materialColors
                });
            }
        }
     
    
        _gl = _canvas.getContext("webgl2", { antialias: true });
        if (!_gl) {
           console.warn("SpectralGL: WebGL2 not supported, falling back to WebGL1");
            _gl = _canvas.getContext("webgl", { antialias: true });
        }
        if (!_gl) { console.error("SpectralGL: WebGL not supported"); return; }

        _dotnetRef = dotnetRef;

        const fsSources = [
            window._SpectralShaders.fsSourcePCF,
            window._SpectralShaders.fsSourcePCSS,
            window._SpectralShaders.fsSourceSpectralXSV1,
            window._SpectralShaders.fsSourceSpectralXSV2,
            window._SpectralShaders.fsSourceSpectralXSV3
        ];

        const vs = compileShader(_gl.VERTEX_SHADER, window._SpectralShaders.vsSourceMain);
        for (let i = 0; i < 5; i++) {
            const fs = compileShader(_gl.FRAGMENT_SHADER, fsSources[i]);
            _programs[i] = _gl.createProgram();
            _gl.attachShader(_programs[i], vs);
            _gl.attachShader(_programs[i], fs);
            _gl.linkProgram(_programs[i]);
            if (!_gl.getProgramParameter(_programs[i], _gl.LINK_STATUS)) {
                console.error("Program error [" + i + "]:", _gl.getProgramInfoLog(_programs[i]));
            }
        }

        _programLocations = [];
        for (let pi = 0; pi < 5; pi++) {
            const p = _programs[pi];
            const locs = {
                pos: _gl.getAttribLocation(p, "aPosition"),
                norm: _gl.getAttribLocation(p, "aNormal"),
                texCoord: _gl.getAttribLocation(p, "aTexCoord"),
                mvp: _gl.getUniformLocation(p, "uMVP"),
                color: _gl.getUniformLocation(p, "uColor"),
                tex: _gl.getUniformLocation(p, "uTexture"),
                hasTex: _gl.getUniformLocation(p, "uHasTexture"),
                model: _gl.getUniformLocation(p, "uModel"),
                camPos: _gl.getUniformLocation(p, "uCamPos"),
                lightCount: _gl.getUniformLocation(p, "uLightCount"),
                emissive: _gl.getUniformLocation(p, "uIsEmissive"),
                emissiveIntensity: _gl.getUniformLocation(p, "uEmissiveIntensity"),
                jitter: _gl.getUniformLocation(p, "uJitter"),
                shadowSoftnessBias: _gl.getUniformLocation(p, "uShadowSoftnessBias"),
                shadowBlockerSearchRadius: _gl.getUniformLocation(p, "uShadowBlockerSearchRadius"),
                shadowKernelSize: _gl.getUniformLocation(p, "uShadowKernelSize"),
                shadowContactSharpness: _gl.getUniformLocation(p, "uShadowContactSharpness"),
                shadowDepthBias: _gl.getUniformLocation(p, "uShadowDepthBias"),
                shadowTintR: _gl.getUniformLocation(p, "uShadowTintR"),
                shadowTintG: _gl.getUniformLocation(p, "uShadowTintG"),
                shadowTintB: _gl.getUniformLocation(p, "uShadowTintB"),
                shadowTintStrength: _gl.getUniformLocation(p, "uShadowTintStrength"),
                shadowPenumbraTintStrength: _gl.getUniformLocation(p, "uShadowPenumbraTintStrength"),
                uvOffset: _gl.getUniformLocation(p, "uUVOffset"),
                uvScale: _gl.getUniformLocation(p, "uUVScale"),
                ambient: _gl.getUniformLocation(p, "uAmbient"),
                lightPos: [],
                lightColor: [],
                lightDir: [],
                lightIntensity: [],
                lightRange: [],
                lightType: [],
                lightSpotAngle: [],
                shadowMap: [],
                lightVP: [],
            };
            for (let i = 0; i < MAX_LIGHTS; i++) {
                locs.lightPos[i] = _gl.getUniformLocation(p, `uLightPos[${i}]`);
                locs.lightColor[i] = _gl.getUniformLocation(p, `uLightColor[${i}]`);
                locs.lightDir[i] = _gl.getUniformLocation(p, `uLightDir[${i}]`);
                locs.lightIntensity[i] = _gl.getUniformLocation(p, `uLightIntensity[${i}]`);
                locs.lightRange[i] = _gl.getUniformLocation(p, `uLightRange[${i}]`);
                locs.lightType[i] = _gl.getUniformLocation(p, `uLightType[${i}]`);
                locs.lightSpotAngle[i] = _gl.getUniformLocation(p, `uLightSpotAngle[${i}]`);
                locs.shadowMap[i] = _gl.getUniformLocation(p, `uShadowMap${i}`);
                locs.lightVP[i] = _gl.getUniformLocation(p, `uLightVP${i}`);
            }
            _programLocations.push(locs);
        }

        // ?? Fullscreen quad VBO — shared by all AA + shadow post passes ???????????
        _fullscreenQuadVbo = _gl.createBuffer();
        _gl.bindBuffer(_gl.ARRAY_BUFFER, _fullscreenQuadVbo);
        _gl.bufferData(_gl.ARRAY_BUFFER, new Float32Array([
            -1, -1, 1, -1, -1, 1,
            -1, 1, 1, -1, 1, 1
        ]), _gl.STATIC_DRAW);


        // ── Populate shared context — systems read from window.SE ──
        window.SE.gl = _gl;
        window.SE.canvas = _canvas;
        window.SE.textureCache = _textureCache;
        window.SE.meshBuffers = _meshBuffers;
        window.SE.textureReady = _textureReady;
        window.SE.fullscreenQuadVbo = _fullscreenQuadVbo;
        window.SE.quadPosLocs = _quadPosLocs;
        window.SE.activeProgram = null;
        window.SE.shadowDepthTexs = _shadowDepthTexs;
        window.SE.fxaaFbo = _fxaaFbo;
        window.SE.fxaaColorTex = _fxaaColorTex;
        window.SE.compileShader = compileShader;
        window.SE.buildProgram = buildProgram;
        window.SE.drawQuad = drawQuad;



        initSharedFbo();
        window.SpectralFXAA.init();
        window.SpectralSMAA.init(); 
        window.SpectralTAA.init();
        window.SpectralAA.init();
        window.SpectralAAV2.init();
        window.SpectralAAV3.init();   
   
        const gpuMax = _gl.getParameter(_gl.MAX_TEXTURE_SIZE);
        SHADOW_SIZE = Math.min(gpuMax, SHADOW_SIZE_MAX);
        initShadowMaps(MAX_SHADOW_LIGHTS);
        initTileMap(_gl);
        initFoliageSystem();
        window.SpectralPrimLoader.upload();

        _isoMouseX = -1;
        _isoMouseY = -1;
        _canvas.addEventListener('mousemove', function (e) {
            const rect = _canvas.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;
            if (x <= 0 && y <= 0) return;
            _isoMouseX = x;
            _isoMouseY = y;
        });
        _canvas.addEventListener('mouseleave', function () {
            _isoMouseX = -1;
            _isoMouseY = -1;       
        });
    

       

        if (window.SpectralWebGPUInterop) {
                    window.SpectralWebGPUInterop.init().catch(e =>
                       console.warn('[SpectralEngine] WebGPU text init failed:', e));
                }
          if (window.SpectralWebGPUParticle) {
              window.SpectralWebGPUParticle.init().catch(e =>
                  console.warn('[SpectralEngine] WebGPU particle init failed:', e));
          }
        /*
        if (window.SpectralTextRenderSystem && typeof window.SpectralTextRenderSystem.init === 'function') {
            try { window.SpectralTextRenderSystem.init(); }
            catch (e) { console.error('[SpectralEngine] SpectralTextRenderSystem.init() failed:', e); }
        }
        */
       /*
        if (window.SpectralParticleSystem && typeof window.SpectralParticleSystem.init === 'function') {
            try { window.SpectralParticleSystem.init(); }
            catch (e) { console.error('[SpectralEngine] SpectralParticleSystem.init() failed:', e); }
        }
        */
        if (window.SkySystem) {
            if (typeof window.SkySystem.reset === 'function') {
                try { window.SkySystem.reset(); }
                catch (e) { console.error('[SpectralEngine] SkySystem.reset() failed:', e); }
            }
            if (typeof window.SkySystem.init === 'function') {
                try { window.SkySystem.init(); }
                catch (e) { console.error('[SpectralEngine] SkySystem.init() failed:', e); }
            }
        }
        if (window.ShootingStarSystem && typeof window.ShootingStarSystem.init === 'function') {
            try { window.ShootingStarSystem.init(); }
            catch (e) { console.error('[SpectralEngine] ShootingStarSystem.init() failed:', e); }
        }
        if (window.SpectralLightningSystem && typeof window.SpectralLightningSystem.init === 'function') {
            try { window.SpectralLightningSystem.init(); }
            catch (e) { console.error('[SpectralEngine] SpectralLightningSystem.init() failed:', e); }
        }
        if (window.StarFieldSystem && typeof window.StarFieldSystem.init === 'function') {
            try { window.StarFieldSystem.init(); }
            catch (e) { console.error('[SpectralEngine] StarFieldSystem.init() failed:', e); }
        }

        if (window.SpectralScrollbarSystem && typeof window.SpectralScrollbarSystem.init === 'function') {
            try { window.SpectralScrollbarSystem.init(); }
            catch (e) { console.error('[SpectralEngine] SpectralScrollbarSystem.init() failed:', e); }
        }

    

        if (window.SpectralCubeCitySystem && typeof window.SpectralCubeCitySystem.init === 'function') {
            try { window.SpectralCubeCitySystem.init(); }
            catch (e) { console.error('[SpectralEngine] SpectralCubeCitySystem.init() failed:', e); }
        }

        console.log("[SpectralGL] WebGL ready");
      
    }

    function uploadMesh(upload) {
        const gl = _gl;
        // ============================================================
        // UV SPHERE INTERCEPT
        // ============================================================
        if (upload.meshId === 'SkySphere') {
            if (_meshBuffers['SkySphere'] && _meshBuffers['SkySphere'].isUVSphere) return;

            const stacks = 48;
            const slices = 96;
            const radius = 900.0;

            const verts = [];
            const normals = [];
            const uvs = [];
            const indices = [];

            for (let stack = 0; stack <= stacks; stack++) {
                const phi = (stack / stacks) * Math.PI;
                const sinPhi = Math.sin(phi);
                const cosPhi = Math.cos(phi);
                const v = stack / stacks;

                for (let slice = 0; slice <= slices; slice++) {
                    const theta = (slice / slices) * Math.PI * 2;
                    const sinTheta = Math.sin(theta);
                    const cosTheta = Math.cos(theta);
                    const u = slice / slices;
                    const x = radius * sinPhi * cosTheta;
                    const y = radius * cosPhi;
                    const z = radius * sinPhi * sinTheta;

                    verts.push(x, y, z);
                    normals.push(-sinPhi * cosTheta, -cosPhi, -sinPhi * sinTheta);
                    uvs.push(u, v);
                }
            }

            for (let stack = 0; stack < stacks; stack++) {
                for (let slice = 0; slice < slices; slice++) {
                    const a = stack * (slices + 1) + slice;
                    const b = a + (slices + 1);
                    indices.push(a, b, a + 1);
                    indices.push(a + 1, b, b + 1);
                }
            }

            const vbo = gl.createBuffer();
            gl.bindBuffer(gl.ARRAY_BUFFER, vbo);
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(verts), gl.STATIC_DRAW);

            const nbo = gl.createBuffer();
            gl.bindBuffer(gl.ARRAY_BUFFER, nbo);
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(normals), gl.STATIC_DRAW);

            const ubo = gl.createBuffer();
            gl.bindBuffer(gl.ARRAY_BUFFER, ubo);
            gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(uvs), gl.STATIC_DRAW);

            const ibo = gl.createBuffer();
            gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, ibo);
            gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint32Array(indices), gl.STATIC_DRAW);

            _meshBuffers['SkySphere'] = {
                vbo, nbo, ubo, ibo,
                indexCount: indices.length,
                vertCount: verts.length / 3,
                hasTexture: false,
                texCacheKey: 'SkySphere',
                materialTextures: [],
                matTexLoaded: [],
                matBreaks: [],
                matIndices: [],
                materialColors: [],
                isUVSphere: true
            };
            return;
        }


        // ============================================================
        // END UV SPHERE INTERCEPT
        // ============================================================

        const baseName = upload.meshId.replace('_Clone', '');
        const existing = _meshBuffers[upload.meshId] || _meshBuffers[baseName];
        if (existing && existing.vertCount > 0 && !upload.textureDirty) {

            if (upload.hasTexture && upload.textureDataUrl &&
                !_textureReady[upload.meshId]) {
                _textureReady[upload.meshId] = true;
                SpectralGLLoader.onAssetRequested();
                const tex = gl.createTexture();
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA,
                    gl.UNSIGNED_BYTE, new Uint8Array([255, 0, 255, 255]));
                const img = new Image();
                img.onload = () => {
                    gl.bindTexture(gl.TEXTURE_2D, tex);
                    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                    _textureCache[upload.meshId] = tex;
                    SpectralGLLoader.onAssetComplete();
                };
                img.src = upload.textureDataUrl;
            }
            if (upload.textureDirty && upload.hasTexture && upload.textureDataUrl) {
                const tex = _gl.createTexture();
                _gl.bindTexture(_gl.TEXTURE_2D, tex);
                _gl.texImage2D(_gl.TEXTURE_2D, 0, _gl.RGBA, 1, 1, 0, _gl.RGBA,
                    _gl.UNSIGNED_BYTE, new Uint8Array([255, 0, 255, 255]));
                const img = new Image();
                img.onload = () => {
                    _gl.bindTexture(_gl.TEXTURE_2D, tex);
                    _gl.texImage2D(_gl.TEXTURE_2D, 0, _gl.RGBA, _gl.RGBA, _gl.UNSIGNED_BYTE, img);
                    _gl.texParameteri(_gl.TEXTURE_2D, _gl.TEXTURE_MIN_FILTER, _gl.LINEAR);
                    _gl.texParameteri(_gl.TEXTURE_2D, _gl.TEXTURE_MAG_FILTER, _gl.LINEAR);
                    _gl.texParameteri(_gl.TEXTURE_2D, _gl.TEXTURE_WRAP_S, _gl.CLAMP_TO_EDGE);
                    _gl.texParameteri(_gl.TEXTURE_2D, _gl.TEXTURE_WRAP_T, _gl.CLAMP_TO_EDGE);
                    _textureCache[upload.meshId] = tex;
                };
                img.src = upload.textureDataUrl;
                _textureCache[upload.meshId] = tex;
            }

            return; 
        }
        if (upload.hasTexture && upload.meshId.startsWith('Static_')) {
            if (window.SpectralStaticObjectsSystem.tryResolveStaticTexture(upload, _textureCache)) {
                _textureReady[upload.meshId] = true;
               
            }
        }

        const vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, vbo);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(upload.vertices), gl.STATIC_DRAW);

        const nbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, nbo);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(upload.normals), gl.STATIC_DRAW);

        const ubo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, ubo);
        gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(upload.uvs || []), gl.STATIC_DRAW);

        const splatterTex = window.SpectralTextureUploads.getSplatterTex(upload.textureDataUrl);
        if (upload.hasTexture && upload.textureDataUrl && splatterTex) {
            _textureCache[upload.meshId] = splatterTex;
            _textureReady[upload.meshId] = true;
        }

        // Main texture upload
        else if (upload.hasTexture && upload.textureDataUrl && !_textureReady[upload.meshId]) {
            _textureReady[upload.meshId] = true;
            SpectralGLLoader.onAssetRequested();

            const tex = gl.createTexture();
            gl.bindTexture(gl.TEXTURE_2D, tex);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA,
                gl.UNSIGNED_BYTE, new Uint8Array([255, 0, 255, 255]));

            if (upload.textureIsRawRGBA) {
                const pixels = Uint8Array.from(atob(upload.textureDataUrl), c => c.charCodeAt(0));
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA,
                    upload.textureWidth, upload.textureHeight,
                    0, gl.RGBA, gl.UNSIGNED_BYTE, pixels);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                SpectralGLLoader.onAssetComplete();
            } else {
                const img = new Image();
                img.onload = () => {
                    gl.bindTexture(gl.TEXTURE_2D, tex);
                    gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, false);
                    gl.pixelStorei(gl.UNPACK_FLIP_Y_WEBGL, false);
                    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                    const isPOT = (img.width & (img.width - 1)) === 0 && (img.height & (img.height - 1)) === 0;
                    if (isPOT) {
                        gl.generateMipmap(gl.TEXTURE_2D);
                        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
                    } else {
                        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                    }
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                    SpectralGLLoader.onAssetComplete();
                };
                img.onerror = (e) => console.error('[SpectralGL] Texture FAILED:', upload.meshId, e);
                img.src = upload.textureDataUrl;
            }

            _textureCache[upload.meshId] = tex;
        }

        const matTexCache = new Array(upload.materialTextures ? upload.materialTextures.length : 0).fill(null);
        const matTexLoaded = new Array(matTexCache.length).fill(false);

        if ((!upload.vertices || upload.vertices.length === 0)
            && upload.meshId.startsWith('ParticlePool_')) {
            const sharedKey = upload.textureDataUrl;
            if (sharedKey && _meshBuffers[sharedKey]) {
                _meshBuffers[upload.meshId] = {
                    ..._meshBuffers[sharedKey],
                    texCacheKey: sharedKey
                };
            } else {
                console.warn('[SpectralGL] Alias not ready, will retry:', upload.meshId);
                delete _uploadedMeshBuffers;
            }
            return;
        }

        _meshBuffers[upload.meshId] = {
            vbo, nbo, ubo,
            vertCount: upload.vertices.length / 3,
            hasTexture: upload.hasTexture,
            texCacheKey: upload.meshId,
            materialTextures: matTexCache,
            matTexLoaded: matTexLoaded,
            matBreaks: upload.matBreaks || [],
            matIndices: upload.matIndices || [],
            materialColors: upload.materialColors || []
        };

      
        const uploadBaseName = upload.meshId.replace('_Clone', '');
        if (uploadBaseName !== upload.meshId) {
            _meshBuffers[uploadBaseName] = _meshBuffers[upload.meshId];
        }
        // Material textures
        if (upload.materialTextures && upload.materialTextures.length > 0) {
            for (let i = 0; i < upload.materialTextures.length; i++) {
                const dataUrl = upload.materialTextures[i];
                if (!dataUrl || dataUrl === '') continue;
                const capturedIdx = i;
                const capturedMeshId = upload.meshId;
                const tex = gl.createTexture();
                matTexCache[i] = tex;
                _meshBuffers[capturedMeshId].materialTextures[i] = tex;
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0, gl.RGBA,
                    gl.UNSIGNED_BYTE, new Uint8Array([255, 0, 255, 255]));
                SpectralGLLoader.onAssetRequested();
                const img = new Image();
                img.onload = () => {
                    gl.bindTexture(gl.TEXTURE_2D, tex);
                    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                    _meshBuffers[capturedMeshId].matTexLoaded[capturedIdx] = true;
                    SpectralGLLoader.onAssetComplete();
                };
                img.src = dataUrl;
            }
        }

        console.log('[SpectralGL] Mesh uploaded:', upload.meshId, 'verts:', upload.vertices.length / 3);
    }

    function uploadParsedMesh(meshName, data, textures, materialColors) {
        const gl = _gl;
        if (!gl) {
            _pendingUploads.push({ meshName, data, textures, materialColors });
            return;
        }
        _doUploadParsedMesh(meshName, data, textures, materialColors);
    }
   

    function _doUploadParsedMesh(meshName, data, textures, materialColors) {
        _parsedMeshCache[meshName] = { data, textures: textures || [], materialColors: materialColors || [] };
   
        const gl = _gl;
        if (_meshBuffers[meshName] &&
            _meshBuffers[meshName].vertCount > 0 &&
            meshName.startsWith('Text_')) {
            return;
        }
        const vbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, vbo);
        gl.bufferData(gl.ARRAY_BUFFER, data.vertices, gl.STATIC_DRAW);

        const nbo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, nbo);
        gl.bufferData(gl.ARRAY_BUFFER, data.normals, gl.STATIC_DRAW);

        const ubo = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, ubo);
        gl.bufferData(gl.ARRAY_BUFFER, data.uvs, gl.STATIC_DRAW);

        const matTextures = [];
        const matTexLoaded = [];

        if (textures && textures.length > 0) {
            for (let i = 0; i < textures.length; i++) {
                const dataUrl = textures[i];
                matTextures.push(null);
                matTexLoaded.push(false);
                if (!dataUrl) continue;

                const capturedIdx = i;
                SpectralGLLoader.onAssetRequested();
                const tex = gl.createTexture();
                gl.bindTexture(gl.TEXTURE_2D, tex);
                gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
                    gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([255, 0, 255, 255]));

                const img = new Image();
                img.onload = () => {
                    gl.bindTexture(gl.TEXTURE_2D, tex);
                    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
                    matTextures[capturedIdx] = tex;
                    matTexLoaded[capturedIdx] = true;
                    console.log('[SpectralGL] Texture loaded for:', meshName, 'slot:', capturedIdx);
                    SpectralGLLoader.onAssetComplete();
                };
                img.onerror = () => {
                    console.warn('[SpectralGL] Texture failed for:', meshName, 'slot:', capturedIdx);
                    SpectralGLLoader.onAssetComplete();
                };
                img.src = dataUrl;
                matTextures[capturedIdx] = tex;
            }
        }

        const matBreaks = data.matBreaks || [];
        const matIndices = data.matIndices || [];

        const bufEntry = {
            vbo, nbo, ubo,
            vertCount: data.vertices.length / 3,
            hasTexture: matTextures.length > 0,
            texCacheKey: meshName,
            materialTextures: matTextures,
            matTexLoaded: matTexLoaded,
            matBreaks: matBreaks,
            matIndices: matIndices,
            materialColors: materialColors || [] 
        };

        _meshBuffers[meshName] = bufEntry;
        _meshBuffers[meshName + '_Clone'] = bufEntry;


    }


 


    async function renderFrame() {
        if (!_gl || !_dotnetRef) return;

        try {

            if (_pendingUploads.length > 0) {   
                for (const p of _pendingUploads)
                    _doUploadParsedMesh(p.meshName, p.data, p.textures || [], p.materialColors || []);
                _pendingUploads = [];
            }

            if (_isoMouseX > 0 && _isoMouseY > 0 && _dotnetRef) {
                try {
                    await _dotnetRef.invokeMethodAsync("SetIsoCameraMousePos", _isoMouseX, _isoMouseY);
                } catch (ex) {
                }
            }

            const frame = await _dotnetRef.invokeMethodAsync("TickAndGetFrame");
            if (!frame || !frame.meshes) return;

            const gl = _gl;
       
            if (_canvas.width !== frame.width || _canvas.height !== frame.height) {
                _canvas.width = frame.width;
                _canvas.height = frame.height;
                gl.viewport(0, 0, frame.width, frame.height);
            }
            uploadAllFoliageInstances(gl, frame);
            
            if (_shadowProgram && frame.lightVPs) {

                // ── Clear ALL shadow FBO slots first ─────────────────────────────────
                for (let li = 0; li < MAX_SHADOW_LIGHTS; li++) {
                    const hasLight = li < frame.lightCount && frame.lightCastsShadows[li];
                    if (!hasLight && _shadowFbos[li]) {
                        gl.bindFramebuffer(gl.FRAMEBUFFER, _shadowFbos[li]);
                        gl.viewport(0, 0, SHADOW_SIZE, SHADOW_SIZE);
                        gl.enable(gl.DEPTH_TEST);
                        gl.depthFunc(gl.LESS);
                        gl.enable(gl.CULL_FACE);
                        gl.cullFace(gl.BACK);
                        gl.clear(gl.DEPTH_BUFFER_BIT | gl.COLOR_BUFFER_BIT);
                    }
                }

                // ── Shadow render pass — active casting lights only ───────────────────
                for (let li = 0; li < frame.lightCount; li++) {
                    if (!frame.lightCastsShadows[li]) continue;
                    const lv = frame.lightVPs[li];
                    /*
                    if (frame.activeScene === 3 && li === 0) {
                        console.log('[Shadow] li:0 lightType:', frame.lightTypes[0],
                            'VP[0]:', lv ? Array.from(lv).slice(0, 4) : 'null',
                            'castsShadow:', frame.lightCastsShadows[0]);
                    }
                    */
                    if (!lv) continue;

                    gl.depthMask(true);
                    gl.enable(gl.DEPTH_TEST);
                    gl.disable(gl.BLEND);
                    gl.bindFramebuffer(gl.FRAMEBUFFER, _shadowFbos[li]);
                    gl.viewport(0, 0, SHADOW_SIZE, SHADOW_SIZE);

                    gl.enable(gl.DEPTH_TEST);
                    gl.depthFunc(gl.LESS);
                    gl.enable(gl.CULL_FACE);
                    gl.cullFace(gl.BACK);
                    gl.clear(gl.DEPTH_BUFFER_BIT | gl.COLOR_BUFFER_BIT);

                    gl.disable(gl.CULL_FACE);
                    gl.useProgram(_shadowProgram);
                    gl.uniform1i(_shadowInstancedLoc, 0);

                    // Normal mesh shadow pass - TRANSLUCENT OBJECTS NOW CAST SHADOWS
                    for (const mesh of frame.meshes) {
                        const buf = _meshBuffers[mesh.meshId] || _meshBuffers[mesh.meshId.replace('_Clone', '')];
                        if (!buf) continue;

                        if (mesh.castsShadow === false) continue;
                        if (mesh.isEmissive) continue;
                        if (mesh.meshId.startsWith('Particle')) continue;
                        if (mesh.meshId.startsWith('Text_')) continue;
                        if (
                            mesh.meshId.includes('S1_LightGizmo_L1') ||
                            mesh.meshId.includes('S1_LightCore_L1') ||
                            mesh.meshId.includes('S1_LightAuraInner_L1') ||
                            mesh.meshId.includes('S1_LightAuraOuter_L1') ||
                            mesh.meshId.includes('S1_LightGizmo_L2') ||
                            mesh.meshId.includes('S1_LightAura_L2') ||
                            mesh.meshId.includes('S1_LightGizmo_L3') ||
                            mesh.meshId.includes('S1_LightAura_L3') ||

                            mesh.meshId.includes('S2_LightGizmo_L1') ||
                            mesh.meshId.includes('S2_LightCore_L1') ||
                            mesh.meshId.includes('S2_LightAuraInner_L1') ||
                            mesh.meshId.includes('S2_LightAuraOuter_L1') ||
                            mesh.meshId.includes('S2_LightGizmo_L2') ||
                            mesh.meshId.includes('S2_LightAura_L2') ||
                            mesh.meshId.includes('S2_LightGizmo_L3') ||
                            mesh.meshId.includes('S2_LightAura_L3') ||
                            mesh.meshId.includes('S2_SpotGizmo_L1') ||
                            mesh.meshId.includes('S2_SpotAura_L1') ||
                            mesh.meshId.includes('S2_AreaGizmo_L1') ||
                            mesh.meshId.includes('S2_AreaAura_L1') ||
                            mesh.meshId.includes('S2_RedSpotGizmo_L1') ||
                            mesh.meshId.includes('S2_RedSpotAura_L1') ||
                            mesh.meshId.includes('S2_GreenPointGizmo_L1') ||
                            mesh.meshId.includes('S2_GreenPointAura_L1') ||
                            mesh.meshId.includes('S2_PurplePointGizmo_L1') ||
                            mesh.meshId.includes('S2_PurplePointAura_L1') ||
                            mesh.meshId.includes('S2_OrangePointGizmo_L1') ||
                            mesh.meshId.includes('S2_OrangePointAura_L1') ||
                            mesh.meshId.includes('S2_PurpleAreaGizmo_L1') ||
                            mesh.meshId.includes('S2_PurpleAreaAura_L1') ||
                            mesh.meshId.includes('S2_CyanPointGizmo_L1') ||
                            mesh.meshId.includes('S2_CyanPointAura_L1') ||
                            mesh.meshId.includes('S2_DeepBluePointGizmo_L1') ||
                            mesh.meshId.includes('S2_DeepBluePointAura_L1') ||
                            mesh.meshId.includes('S2_WarmYellowPointGizmo_L1') ||
                            mesh.meshId.includes('S2_WarmYellowPointAura_L1') ||
                            mesh.meshId.includes('S2_ColdWhitePointGizmo_L1') ||
                            mesh.meshId.includes('S2_ColdWhitePointAura_L1') ||
                            mesh.meshId.includes('S2_SicklyGreenPointGizmo_L1') ||
                            mesh.meshId.includes('S2_SicklyGreenPointAura_L1') ||
                            mesh.meshId.includes('S2_DeepRedPointGizmo_L1') ||
                            mesh.meshId.includes('S2_DeepRedPointAura_L1') ||
                            mesh.meshId.includes('S2_PinkPointGizmo_L1') ||
                            mesh.meshId.includes('S2_PinkPointAura_L1') ||

                            mesh.meshId.includes('S3_LightGizmo_L1') ||
                            mesh.meshId.includes('S3_LightCore_L1') ||
                            mesh.meshId.includes('S3_LightAuraInner_L1') ||
                            mesh.meshId.includes('S3_LightAuraOuter_L1') ||
                            mesh.meshId.includes('S3_LightGizmo_L2') ||
                            mesh.meshId.includes('S3_LightAura_L2') ||
                            mesh.meshId.includes('S3_LightGizmo_L3') ||
                            mesh.meshId.includes('S3_LightAura_L3') ||

                            mesh.meshId.includes('S4_LightGizmo_L1') ||
                            mesh.meshId.includes('S4_LightCore_L1') ||
                            mesh.meshId.includes('S4_LightAuraInner_L1') ||
                            mesh.meshId.includes('S4_LightAuraOuter_L1') ||
                            mesh.meshId.includes('S4_LightGizmo_L2') ||
                            mesh.meshId.includes('S4_LightAura_L2') ||
                            mesh.meshId.includes('S4_LightGizmo_L3') ||
                            mesh.meshId.includes('S4_LightAura_L3') ||

                            mesh.meshId.includes('S5_LightGizmo_L1') ||
                            mesh.meshId.includes('S5_LightCore_L1') ||
                            mesh.meshId.includes('S5_LightAuraInner_L1') ||
                            mesh.meshId.includes('S5_LightAuraOuter_L1') ||
                            mesh.meshId.includes('S5_LightGizmo_L2') ||
                            mesh.meshId.includes('S5_LightAura_L2') ||
                            mesh.meshId.includes('S5_LightGizmo_L3') ||
                            mesh.meshId.includes('S5_LightAura_L3')
                        ) continue;

                        const model = mesh.model || buf.lastModel;
                        if (!model) continue;

                        // Skip light source meshes
                        const lx = frame.lightPositions[li * 3];
                        const ly = frame.lightPositions[li * 3 + 1];
                        const lz = frame.lightPositions[li * 3 + 2];
                        const mx = model[12];
                        const my = model[13];
                        const mz = model[14];
                        const dx = mx - lx, dy = my - ly, dz = mz - lz;
                        if (dx * dx + dy * dy + dz * dz < 1.5) continue;

                        gl.uniformMatrix4fv(_shadowLightMVPLoc, false, lv);
                        gl.uniformMatrix4fv(_shadowModelLoc, false, model);

                        const hasTex = buf.hasTexture && _textureCache[buf.texCacheKey];
                        if (_shadowHasTextureLoc !== null) {
                            gl.uniform1i(_shadowHasTextureLoc, hasTex ? 1 : 0);
                        }
                        if (hasTex) {
                            gl.activeTexture(gl.TEXTURE0);
                            gl.bindTexture(gl.TEXTURE_2D, _textureCache[buf.texCacheKey]);
                            gl.uniform1i(_shadowTextureLoc, 0);
                            gl.uniform1f(_shadowAlphaThresholdLoc, 0.1);
                        }

                        if (_shadowTexCoordLoc >= 0) {
                            gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
                            gl.enableVertexAttribArray(_shadowTexCoordLoc);
                            gl.vertexAttribPointer(_shadowTexCoordLoc, 2, gl.FLOAT, false, 0, 0);
                        }

                        gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
                        gl.enableVertexAttribArray(_shadowPosLoc);
                        gl.vertexAttribPointer(_shadowPosLoc, 3, gl.FLOAT, false, 0, 0);
                        gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
                    }

                    if (_tileGridVAO && _tileIdxCount > 0 &&
                        (frame.activeScene === 2 || frame.activeScene === 3)) {
                        gl.uniformMatrix4fv(_shadowLightMVPLoc, false, lv);
                        gl.uniformMatrix4fv(_shadowModelLoc, false, new Float32Array([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]));
                        gl.uniform1i(_shadowInstancedLoc, 2); // tile map path

                        gl.bindVertexArray(_tileGridVAO);

                        // XY positions
                        gl.bindBuffer(gl.ARRAY_BUFFER, _tileGridVBO);
                        gl.enableVertexAttribArray(_shadowPosLoc);
                        gl.vertexAttribPointer(_shadowPosLoc, 2, gl.FLOAT, false, 0, 0);

                        // Z heights
                        if (_shadowHeightLoc >= 0) {
                            gl.bindBuffer(gl.ARRAY_BUFFER, _tileHeightVBO);
                            gl.enableVertexAttribArray(_shadowHeightLoc);
                            gl.vertexAttribPointer(_shadowHeightLoc, 1, gl.FLOAT, false, 0, 0);
                        }

                        gl.drawElements(gl.TRIANGLES, _tileIdxCount, gl.UNSIGNED_INT, 0);

                        // Cleanup
                        if (_shadowHeightLoc >= 0) gl.disableVertexAttribArray(_shadowHeightLoc);
                        gl.uniform1i(_shadowInstancedLoc, 0); // reset
                        gl.bindVertexArray(null);
                    }

                            /*
                      if (_tileGridVAO && _tileIdxCount > 0 &&
                          (frame.activeScene === 2 || frame.activeScene === 3)) {
                          gl.uniformMatrix4fv(_shadowLightMVPLoc, false, lv);
                          gl.uniformMatrix4fv(_shadowModelLoc, false, new Float32Array([1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1]));
                          gl.bindVertexArray(_tileGridVAO);
                          gl.bindBuffer(gl.ARRAY_BUFFER, _tileGridVBO);
                          gl.enableVertexAttribArray(_shadowPosLoc);
                          gl.vertexAttribPointer(_shadowPosLoc, 2, gl.FLOAT, false, 0, 0);
                          gl.drawElements(gl.TRIANGLES, _tileIdxCount, gl.UNSIGNED_INT, 0);
                          gl.bindVertexArray(null);
                      }
                      */

                    // Foliage shadow — sun slot only (li === 0 assumption matches tile map)
                    if (li === 0 && (frame.activeScene === 2 || frame.activeScene === 3)) {
                        if (frame.activeScene === 3 && !window._loggedFoliageShadowOnce) {
                            window._loggedFoliageShadowOnce = true;
                            console.log('[DEBUG] lightVP0:', Array.from(lv));
                            console.log('[DEBUG] lightType0:', frame.lightTypes[0]);
                            console.log('[DEBUG] lightCastsShadows0:', frame.lightCastsShadows[0]);
                            console.log('[DEBUG] foliageInstances count:', frame.foliageInstances?.length);
                            if (frame.foliageInstances?.[0]) {
                                console.log('[DEBUG] first group meshId/count:', frame.foliageInstances[0].meshId, frame.foliageInstances[0].count);
                                console.log('[DEBUG] first 5 positions:', Array.from(frame.foliageInstances[0].positions.slice(0, 15)));
                            }
                        }

                        renderFoliageShadow(gl, frame, lv);
                    }
                    


                }

                gl.bindFramebuffer(gl.FRAMEBUFFER, null);
                gl.viewport(0, 0, _canvas.width, _canvas.height);
            }
            
         
            const aaMode = frame.aaMode || 0;
            if ((aaMode === 2 || aaMode === 3 || aaMode === 4 ||
                aaMode === 5 || aaMode === 6 || aaMode === 7) && _fxaaFbo) {
                gl.bindFramebuffer(gl.FRAMEBUFFER, _fxaaFbo);
                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, null);
                gl.activeTexture(gl.TEXTURE1);
                gl.bindTexture(gl.TEXTURE_2D, null);
                gl.activeTexture(gl.TEXTURE2);
                gl.bindTexture(gl.TEXTURE_2D, null);
            } else {
                gl.bindFramebuffer(gl.FRAMEBUFFER, null);
            }

            gl.clearColor(0, 0, 0, 1);
            gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
            gl.enable(gl.DEPTH_TEST);
            gl.enable(gl.BLEND);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
            gl.disable(gl.CULL_FACE);
            gl.cullFace(gl.BACK);
            gl.depthFunc(gl.LESS);

            const skyMesh = frame.meshes.find(m => m.meshId === 'SkySphere');
            if (skyMesh && window.SkySystem) {
                if (skyMesh.upload) uploadMesh(skyMesh.upload);
                if (frame.skyDayTexUrl)
                    window.SkySystem.ensureSkyTexturesCube(frame.skyDayTexUrl, frame.skyNightTexUrl);
                const skyBuf = SE.meshBuffers['UVSphere']
                    ?? SE.meshBuffers['SmoothSphere']
                    ?? SE.meshBuffers['FBXCube']
                    ?? SE.meshBuffers['SkySphere'];
                frame.skyMesh = skyMesh;
                window.SkySystem.render(frame, skyBuf);
            }

    
            if (window.ShootingStarSystem && frame.skyMesh) {
                window.ShootingStarSystem.render(frame);
            }
            if (window.SpectralLightningSystem && frame.skyMesh &&
                (frame.activeScene === 2 || frame.activeScene === 3)) {
                window.SpectralLightningSystem.render(frame);
            }
            if (window.StarFieldSystem && frame.skyMesh) {
                window.StarFieldSystem.render(frame);
            }

            const TX = window.SpectralTextureUploads;            
            if (frame.warriorTexUrls?.length > 0 || frame.rogueTexUrls?.length > 0)
                TX.preloadOverlayTextures(['/iAssets/WarriorGothit01.png']);           
            TX.preloadCharTextures('WarriorSquare', frame.warriorTexUrls);
            TX.preloadCharTextures('RogueSquare', frame.rogueTexUrls);
            TX.preloadCharTextures('MonkSquare', frame.monkTexUrls);
            TX.preloadCharTextures('MageSquare', frame.mageTexUrls);           
            if (isBWPScene(frame.activeScene)) {
                TX.preloadBreakTextures(frame.breakTexUrls);
                TX.preloadSplatterTextures(frame.splatterTexUrls);
            }            
            TX.preloadEnemyTextures('Skeleton', frame.skeletonTexUrls);
            TX.preloadEnemyTextures('PsychoSkeleton', frame.psychoSkeletonTexUrls);
            TX.preloadEnemyTextures('ZombiePsycho', frame.zombiePsychoTexUrls);
            TX.preloadEnemyTextures('SkeletonWar', frame.skeletonWarTexUrls);
            TX.preloadEnemyTextures('Goatman', frame.goatmanTexUrls);
            TX.preloadEnemyTextures('ScavBoss', frame.scavBossTexUrls);
            TX.preloadEnemyTextures('SkeletonBoss', frame.skeletonBossTexUrls);
            TX.preloadEnemyTextures('Cow', frame.cowTexUrls);
            TX.preloadEnemyTextures('Cat', frame.catTexUrls);
            TX.preloadEnemyTextures('TownSlut', frame.townSlutTexUrls);
            TX.applyEnemyTexSwaps('Skeleton', frame.skeletonTexSwaps);
            TX.applyEnemyTexSwaps('PsychoSkeleton', frame.psychoSkeletonTexSwaps);
            TX.applyEnemyTexSwaps('ZombiePsycho', frame.zombiePsychoTexSwaps);
            TX.applyEnemyTexSwaps('SkeletonWar', frame.skeletonWarTexSwaps);
            TX.applyEnemyTexSwaps('Goatman', frame.goatmanTexSwaps);
            TX.applyEnemyTexSwaps('ScavBoss', frame.scavBossTexSwaps);
            TX.applyEnemyTexSwaps('SkeletonBoss', frame.skeletonBossTexSwaps);
            TX.applyEnemyTexSwaps('Cow', frame.cowTexSwaps);
            TX.applyEnemyTexSwaps('Cat', frame.catTexSwaps);
            TX.applyEnemyTexSwaps('TownSlut', frame.townSlutTexSwaps);
            TX.applyBreakTexSwaps(frame);
            TX.applyCharTexSwaps(frame);




            if (isBWPScene(frame.activeScene) && !window._staticObjectsPreloaded) {
                window.SpectralStaticObjectsSystem.preloadAll(gl);
                window._staticObjectsPreloaded = true;
            }

       
            if (frame.tileMapTextures && frame.tileMapTextures.length > 0) {
                uploadTileTextures(gl, frame.tileMapTextures);
            }

     
            if (!_tilePBRReady && (
                frame.tileMapNormalTextures ||
                frame.tileMapRoughnessTextures ||
                frame.tileMapMetallicTextures ||
                frame.tileMapAOTextures ||
                frame.tileMapSpecularTextures ||
                frame.tileMapEmissiveTextures ||
                frame.tileMapDisplacementTextures)) {
                uploadAllTilePBRTextures(gl, frame);
            }
           
            if (frame.tileMap && frame.tileMap.isDirty) {
                updateTileHeights(gl, frame.tileMap);
            }
     
            if (window._tileGridReady && !_tileTexturesReady && _dotnetRef) {
                if (!_tileMapTexturesUploaded) {
                    console.log('[TileMap] Grid ready but textures missing — requesting resend');
                    _tileMapTexturesUploaded = true;
                    _dotnetRef.invokeMethodAsync('OnTileGridRebuilding').catch(() => { });
                }
            }
          
            if (frame.activeScene === 2 || frame.activeScene === 3 || isBWPScene(frame.activeScene)) {
                drawTileMap(gl, frame);
            }

         


            const shadowMode = frame.shadowMode || 0;
            window.SE.activeProgram = _activeProgram;
            _activeProgram = _programs[shadowMode] || _programs[0];
            _activeLocs = _programLocations[shadowMode] || _programLocations[0];
            gl.useProgram(_activeProgram);
            gl.uniform3f(_activeLocs.camPos, frame.camX, frame.camY, frame.camZ);
            gl.uniform1i(_activeLocs.lightCount, frame.lightCount);

            for (let li = 0; li < frame.lightCount; li++) {
                gl.uniform3f(_activeLocs.lightPos[li],
                    frame.lightPositions[li * 3], frame.lightPositions[li * 3 + 1], frame.lightPositions[li * 3 + 2]);
                gl.uniform3f(_activeLocs.lightColor[li],
                    frame.lightColors[li * 3], frame.lightColors[li * 3 + 1], frame.lightColors[li * 3 + 2]);
                gl.uniform3f(_activeLocs.lightDir[li],
                    frame.lightDirections[li * 3], frame.lightDirections[li * 3 + 1], frame.lightDirections[li * 3 + 2]);
                gl.uniform1f(_activeLocs.lightIntensity[li], frame.lightIntensities[li]);
                gl.uniform1f(_activeLocs.lightRange[li], frame.lightRanges[li]);
                gl.uniform1i(_activeLocs.lightType[li], frame.lightTypes[li]);
                gl.uniform1f(_activeLocs.lightSpotAngle[li], frame.lightSpotAngles[li]);
            }

            for (let li = 0; li < frame.lightCount; li++) {
                if (_shadowDepthTexs[li]) {
                    gl.activeTexture(gl.TEXTURE1 + li);
                    gl.bindTexture(gl.TEXTURE_2D, _shadowDepthTexs[li]);
                    gl.uniform1i(_activeLocs.shadowMap[li], 1 + li);
                    if (frame.lightVPs[li]) {
                        gl.uniformMatrix4fv(_activeLocs.lightVP[li], false, frame.lightVPs[li]);
                    }
                }
            }

            if (aaMode === 4 && _activeLocs.jitter) {
                gl.uniform2f(_activeLocs.jitter, frame.jitterX, frame.jitterY);
            }


            if (shadowMode === 2 || shadowMode === 3 || shadowMode === 4) {
                gl.uniform1f(_activeLocs.shadowSoftnessBias, frame.shadowSoftnessBias);
                gl.uniform1f(_activeLocs.shadowBlockerSearchRadius, frame.shadowBlockerSearchRadius);
                gl.uniform1f(_activeLocs.shadowKernelSize, frame.shadowKernelSize);
                gl.uniform1f(_activeLocs.shadowContactSharpness, frame.shadowContactSharpness);
                gl.uniform1f(_activeLocs.shadowDepthBias, frame.shadowDepthBias);
                gl.uniform1f(_activeLocs.shadowTintR, frame.shadowTintR);
                gl.uniform1f(_activeLocs.shadowTintG, frame.shadowTintG);
                gl.uniform1f(_activeLocs.shadowTintB, frame.shadowTintB);
                gl.uniform1f(_activeLocs.shadowTintStrength, frame.shadowTintStrength);
                gl.uniform1f(_activeLocs.shadowPenumbraTintStrength, frame.shadowPenumbraTintStrength);
            }

            const sortedMeshes = [...frame.meshes].sort((a, b) => {
                const aT = a.a < 0.99 ? 1 : 0;
                const bT = b.a < 0.99 ? 1 : 0;
                if (aT !== bT) return aT - bT; 
                if (aT === 1) {
                  
                    const ax = (a.model?.[12] ?? 0) - frame.camX;
                    const ay = (a.model?.[13] ?? 0) - frame.camY;
                    const az = (a.model?.[14] ?? 0) - frame.camZ;
                    const bx = (b.model?.[12] ?? 0) - frame.camX;
                    const by = (b.model?.[13] ?? 0) - frame.camY;
                    const bz = (b.model?.[14] ?? 0) - frame.camZ;
                    return (bx * bx + by * by + bz * bz) - (ax * ax + ay * ay + az * az);
                }
                return 0;
            });

            for (const mesh of sortedMeshes) {

                if (mesh.upload) uploadMesh(mesh.upload);

                if (mesh.meshId.startsWith('ParticlePool_') ||
                    mesh.meshId.startsWith('ParticleGeo_') ||
                    mesh.meshId.startsWith('ParticlePrewarm_') ||
                    mesh.meshId.startsWith('Text_') ||
                    mesh.meshId === 'SkySphere') continue;

                const buf = _meshBuffers[mesh.meshId];
                if (!buf) continue;

                gl.uniformMatrix4fv(_activeLocs.mvp, false, mesh.mvp);
                if (mesh.model) {
                    gl.uniformMatrix4fv(_activeLocs.model, false, mesh.model);
                    buf.lastModel = mesh.model;
                } else if (buf.lastModel) {
                    gl.uniformMatrix4fv(_activeLocs.model, false, buf.lastModel);
                }
                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                gl.uniform1i(_activeLocs.emissive, mesh.isEmissive ? 1 : 0);
                gl.uniform1f(_activeLocs.emissiveIntensity, mesh.emissiveIntensity ?? 1.0);

                gl.uniform2f(_activeLocs.uvOffset, mesh.uvOffsetX || 0.0, mesh.uvOffsetY || 0.0);
                gl.uniform2f(_activeLocs.uvScale, mesh.uvScaleX || 1.0, mesh.uvScaleY || 1.0);

                if (mesh.a < 0.99) {
                    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
                    gl.depthMask(false);
                } else {
                    gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
                    gl.depthMask(mesh.a >= 0.99);
                }
                if (mesh.a < 0.99) {
                    gl.depthMask(false);
                } else {
                    gl.depthMask(true);
                    gl.enable(gl.DEPTH_TEST);
                }

                // Vertex buffers
                gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
                gl.enableVertexAttribArray(_activeLocs.pos);
                gl.vertexAttribPointer(_activeLocs.pos, 3, gl.FLOAT, false, 0, 0);

                gl.bindBuffer(gl.ARRAY_BUFFER, buf.nbo);
                gl.enableVertexAttribArray(_activeLocs.norm);
                gl.vertexAttribPointer(_activeLocs.norm, 3, gl.FLOAT, false, 0, 0);

                gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
                gl.enableVertexAttribArray(_activeLocs.texCoord);
                gl.vertexAttribPointer(_activeLocs.texCoord, 2, gl.FLOAT, false, 0, 0);

                // Find texture — use texCacheKey from buffer (handles shared particle geo)
                const resolvedTexKey = buf.texCacheKey || mesh.meshId;

                if (buf.hasTexture && _textureCache[resolvedTexKey]) {
                    gl.activeTexture(gl.TEXTURE0);
                    gl.bindTexture(gl.TEXTURE_2D, _textureCache[resolvedTexKey]);
                    gl.uniform1i(_activeLocs.tex, 0);
                    gl.uniform1i(_activeLocs.hasTex, 1);
                } else {
                    gl.uniform1i(_activeLocs.hasTex, 0);
                }

                // ?? Transparent mesh — double-sided two-pass render ???????????????
                if (mesh.a < 0.99) {
                    gl.depthMask(false);
                    gl.enable(gl.CULL_FACE);

                    // Pass 1 — back faces
                    gl.cullFace(gl.FRONT);
                    if (buf.matBreaks && buf.matBreaks.length >= 1) {
                        let offsets = [], running = 0;
                        for (let i = 0; i < buf.matBreaks.length; i++) {
                            offsets[i] = running;
                            running += buf.matBreaks[i];
                        }
                        for (let m = 0; m < buf.matBreaks.length; m++) {
                            const matIdx = buf.matIndices[m];
                            const matTex = buf.materialTextures[matIdx];
                            const matLoaded = buf.matTexLoaded[matIdx];
                            if (matTex && matLoaded) {
                                gl.activeTexture(gl.TEXTURE0);
                                gl.bindTexture(gl.TEXTURE_2D, matTex);
                                gl.uniform1i(_activeLocs.tex, 0);
                                gl.uniform1i(_activeLocs.hasTex, 1);
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                            } else if (buf.materialColors && buf.materialColors[matIdx]) {
                                const hasCSharpColor = !(mesh.r >= 0.99 && mesh.g >= 0.99 && mesh.b >= 0.99);
                                if (hasCSharpColor) {
                                    gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                                } else {
                                    const parts = buf.materialColors[matIdx].split(',');
                                    gl.uniform4f(_activeLocs.color,
                                        parseFloat(parts[0]),
                                        parseFloat(parts[1]),
                                        parseFloat(parts[2]),
                                        mesh.a);
                                }
                                gl.uniform1i(_activeLocs.hasTex, 0);

                            } else if (buf.hasTexture && _textureCache[buf.texCacheKey]) {
                                gl.activeTexture(gl.TEXTURE0);
                                gl.bindTexture(gl.TEXTURE_2D, _textureCache[buf.texCacheKey]);
                                gl.uniform1i(_activeLocs.tex, 0);
                                gl.uniform1i(_activeLocs.hasTex, 1);
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                            } else {
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                                gl.uniform1i(_activeLocs.hasTex, 0);
                            }



                            gl.drawArrays(gl.TRIANGLES, offsets[m], buf.matBreaks[m]);
                        }
                    } else {

                        gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
                    }

                    // Pass 2 — front faces
                    gl.cullFace(gl.BACK);
                    if (buf.matBreaks && buf.matBreaks.length >= 1) {
                        let offsets = [], running = 0;
                        for (let i = 0; i < buf.matBreaks.length; i++) {
                            offsets[i] = running;
                            running += buf.matBreaks[i];
                        }
                        for (let m = 0; m < buf.matBreaks.length; m++) {
                            const matIdx = buf.matIndices[m];
                            const matTex = buf.materialTextures[matIdx];
                            const matLoaded = buf.matTexLoaded[matIdx];
                            if (matTex && matLoaded) {
                                gl.activeTexture(gl.TEXTURE0);
                                gl.bindTexture(gl.TEXTURE_2D, matTex);
                                gl.uniform1i(_activeLocs.tex, 0);
                                gl.uniform1i(_activeLocs.hasTex, 1);
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                            } else if (buf.materialColors && buf.materialColors[matIdx]) {
                                const hasCSharpColor = !(mesh.r >= 0.99 && mesh.g >= 0.99 && mesh.b >= 0.99);
                                if (hasCSharpColor) {
                                    gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                                } else {
                                    const parts = buf.materialColors[matIdx].split(',');
                                    gl.uniform4f(_activeLocs.color,
                                        parseFloat(parts[0]),
                                        parseFloat(parts[1]),
                                        parseFloat(parts[2]),
                                        mesh.a);
                                }
                                gl.uniform1i(_activeLocs.hasTex, 0);

                            } else if (buf.hasTexture && _textureCache[buf.texCacheKey]) {
                                gl.activeTexture(gl.TEXTURE0);
                                gl.bindTexture(gl.TEXTURE_2D, _textureCache[buf.texCacheKey]);
                                gl.uniform1i(_activeLocs.tex, 0);
                                gl.uniform1i(_activeLocs.hasTex, 1);
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                            } else {
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                                gl.uniform1i(_activeLocs.hasTex, 0);
                            }
                            gl.drawArrays(gl.TRIANGLES, offsets[m], buf.matBreaks[m]);
                        }
                    } else {
                        gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
                    }

                    gl.disable(gl.CULL_FACE);
                    gl.cullFace(gl.BACK);
                    gl.depthMask(true);

                    // ?? Opaque mesh — standard single-pass render ?????????????????????
                } else {
                    gl.depthMask(true);
                    gl.enable(gl.DEPTH_TEST);

                    if (buf.matBreaks && buf.matBreaks.length >= 1) {
                        let offsets = [], running = 0;
                        for (let i = 0; i < buf.matBreaks.length; i++) {
                            offsets[i] = running;
                            running += buf.matBreaks[i];
                        }
                        for (let m = 0; m < buf.matBreaks.length; m++) {
                            const matIdx = buf.matIndices[m];
                            const matTex = buf.materialTextures[matIdx];
                            const matLoaded = buf.matTexLoaded[matIdx];
                            if (matTex && matLoaded) {
                                gl.activeTexture(gl.TEXTURE0);
                                gl.bindTexture(gl.TEXTURE_2D, matTex);
                                gl.uniform1i(_activeLocs.tex, 0);
                                gl.uniform1i(_activeLocs.hasTex, 1);
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                            } else if (buf.hasTexture && _textureCache[buf.texCacheKey]) {
                                // Fallback — mesh-level texture (used by primitives with sprite sheets)
                                gl.activeTexture(gl.TEXTURE0);
                                gl.bindTexture(gl.TEXTURE_2D, _textureCache[buf.texCacheKey]);
                                gl.uniform1i(_activeLocs.tex, 0);
                                gl.uniform1i(_activeLocs.hasTex, 1);
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                            } else if (buf.materialColors && buf.materialColors[matIdx]) {
                                const hasCSharpColor = !(mesh.r >= 0.99 && mesh.g >= 0.99 && mesh.b >= 0.99);
                                if (hasCSharpColor) {
                                    gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                                } else {
                                    const parts = buf.materialColors[matIdx].split(',');
                                    gl.uniform4f(_activeLocs.color,
                                        parseFloat(parts[0]),
                                        parseFloat(parts[1]),
                                        parseFloat(parts[2]),
                                        mesh.a);
                                }
                                gl.uniform1i(_activeLocs.hasTex, 0);
                            } else {
                                gl.uniform4f(_activeLocs.color, mesh.r, mesh.g, mesh.b, mesh.a);
                                gl.uniform1i(_activeLocs.hasTex, 0);
                            }

                            gl.drawArrays(gl.TRIANGLES, offsets[m], buf.matBreaks[m]);
                        }
                    } else {
                        gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
                    }
                }

                gl.depthMask(true);
                gl.enable(gl.DEPTH_TEST);
            }
            // Foliage — opaque instanced pass, after regular meshes, before overlays
            if (frame.activeScene === 2 || frame.activeScene === 3) {
                renderFoliageColor(gl, frame);
            }

            // Overlay pass — draw hit overlays on top of character meshes
            gl.enable(gl.BLEND);
            gl.blendFunc(gl.SRC_ALPHA, gl.ONE_MINUS_SRC_ALPHA);
            gl.depthMask(false);

            for (const mesh of frame.meshes) {
                if (!mesh.overlayTextureDataUrl || mesh.overlayAlpha <= 0) continue;
                const buf = _meshBuffers[mesh.meshId];
                if (!buf) continue;

                const overlayTex = TX.getOverlayTex(mesh.overlayTextureDataUrl);
                if (!overlayTex) continue;

                gl.useProgram(_activeProgram);
                gl.uniformMatrix4fv(_activeLocs.mvp, false, mesh.mvp);
                if (mesh.model)
                    gl.uniformMatrix4fv(_activeLocs.model, false, mesh.model);
                gl.uniform4f(_activeLocs.color, 1.0, 1.0, 1.0, mesh.overlayAlpha);
                gl.uniform1i(_activeLocs.emissive, 0);
                gl.uniform2f(_activeLocs.uvOffset, 0.0, 0.0);
                gl.uniform2f(_activeLocs.uvScale, 1.0, 1.0);

                gl.activeTexture(gl.TEXTURE0);
                gl.bindTexture(gl.TEXTURE_2D, overlayTex);
                gl.uniform1i(_activeLocs.tex, 0);
                gl.uniform1i(_activeLocs.hasTex, 1);

                gl.bindBuffer(gl.ARRAY_BUFFER, buf.vbo);
                gl.enableVertexAttribArray(_activeLocs.pos);
                gl.vertexAttribPointer(_activeLocs.pos, 3, gl.FLOAT, false, 0, 0);

                gl.bindBuffer(gl.ARRAY_BUFFER, buf.nbo);
                gl.enableVertexAttribArray(_activeLocs.norm);
                gl.vertexAttribPointer(_activeLocs.norm, 3, gl.FLOAT, false, 0, 0);

                gl.bindBuffer(gl.ARRAY_BUFFER, buf.ubo);
                gl.enableVertexAttribArray(_activeLocs.texCoord);
                gl.vertexAttribPointer(_activeLocs.texCoord, 2, gl.FLOAT, false, 0, 0);

                gl.drawArrays(gl.TRIANGLES, 0, buf.vertCount);
            }

            gl.depthMask(true);  

            if (aaMode === 1) applyMSAA();
            else if (aaMode === 2) window.SpectralFXAA.apply();
            else if (aaMode === 3) window.SpectralSMAA.apply();
            else if (aaMode === 4) window.SpectralTAA.apply();
            else if (aaMode === 5) window.SpectralAA.apply();
            else if (aaMode === 6) window.SpectralAAV2.apply();
            else if (aaMode === 7) window.SpectralAAV3.apply();
            gl.bindFramebuffer(gl.FRAMEBUFFER, null);
      
      
            //  Disabled Webgl systems
            //  window.SpectralTextRenderSystem.render(frame, _meshBuffers);
            //  window.SpectralParticleSystem.render(frame, _activeProgram);
            if (window.SpectralWebGPUInterop?.isAvailable()) {
                window.SpectralWebGPUInterop.render(frame);
            } else {
                window.SpectralTextRenderSystem.render(frame, _meshBuffers);
            }

            if (window.SpectralWebGPUParticle?.isAvailable()) {
                window.SpectralWebGPUParticle.render(frame);
            } else {
                window.SpectralParticleSystem.render(frame, _activeProgram);
            }
         

            // Disabled Overall old system
            //  window.SpectralCubeCitySystem.render(frame);           
            window.SpectralScrollbarSystem.setCurrentZ(frame.scrollbarZ ?? 10);
            window.SpectralScrollbarSystem.render(frame);
            if (SpectralGLLoader._visible) {
                if (SpectralGLLoader.isDone()) {
                    SpectralGLLoader.hide();
                } else {
                    console.log('[Loader] not done —',
                        'requested:', SpectralGLLoader._requested,
                        'completed:', SpectralGLLoader._completed,             
                    );
                }
            }
        } catch (ex) {
            console.error("[SpectralGL] renderFrame error:", ex);
        }
    }

    function applyMSAA() {
        // Hardware MSAA — enabled at context creation via { antialias: true }
        // No per-frame work needed
    }

    function buildProgram(vsSrc, fsSrc) {
        const gl = _gl;
        const p = gl.createProgram();
        gl.attachShader(p, compileShader(gl.VERTEX_SHADER, vsSrc));
        gl.attachShader(p, compileShader(gl.FRAGMENT_SHADER, fsSrc));
        gl.linkProgram(p);
        return p;
    }

    function clearAllShadowMaps() {
        const gl = _gl;
        if (!gl) return;

        for (let i = 0; i < MAX_SHADOW_LIGHTS; i++) {
            if (!_shadowFbos[i]) continue;

            gl.bindFramebuffer(gl.FRAMEBUFFER, _shadowFbos[i]);
            gl.viewport(0, 0, SHADOW_SIZE, SHADOW_SIZE);
            gl.enable(gl.DEPTH_TEST);
            gl.depthMask(true);
            gl.clear(gl.DEPTH_BUFFER_BIT | gl.COLOR_BUFFER_BIT);

            // Unbind shadow depth texture from all units
            if (_shadowDepthTexs[i]) {
                gl.activeTexture(gl.TEXTURE0 + (9 + i));
                gl.bindTexture(gl.TEXTURE_2D, null);
            }
        }

        // Flush GPU to process clears immediately
        gl.bindFramebuffer(gl.FRAMEBUFFER, null);
        gl.viewport(0, 0, _canvas.width, _canvas.height);
        gl.flush();

        console.log('[SpectralGL] clearAllShadowMaps — all shadow slots cleared and flushed');
    }


    let _pendingFrame = false;
    function startRenderLoop(canvasRef, dotnetRef) {
   
        if (_animationHandle !== null) {
            cancelAnimationFrame(_animationHandle);
            _animationHandle = null;
        }
   
        _pendingFrame = false; 
       window.SpectralEngineLoader.ready();
        
        function waitForSize() {
            const w = canvasRef.clientWidth || canvasRef.width;
            const h = canvasRef.clientHeight || canvasRef.height;
            if (w > 300 && h > 150) {
                canvasRef.width = w;
                canvasRef.height = h;
                init(canvasRef, dotnetRef);
              
                function loop() {
                    _animationHandle = requestAnimationFrame(loop);
                    if (_pendingFrame) return;
                    _pendingFrame = true;
                    renderFrame().finally(() => { _pendingFrame = false; });
                }
                _animationHandle = requestAnimationFrame(loop);
            } else {
                requestAnimationFrame(waitForSize);
            }
        }
        requestAnimationFrame(waitForSize);
    }
    function stopRenderLoop() {
        if (_animationHandle !== null) {
            cancelAnimationFrame(_animationHandle);
            _animationHandle = null;
        }
        _dotnetRef = null;
    }  
    function resizeCanvas(width, height) {
        if (!_canvas || !_gl) return;       
        if (width <= 0 || height <= 0) return;
        _canvas.width = width;
        _canvas.height = height;
        _gl.viewport(0, 0, width, height);
        resizeAAfbos();
               if (window.SpectralWebGPUInterop?.isAvailable()) {
                    window.SpectralWebGPUInterop.resize(width, height);
                }
               if (window.SpectralWebGPUParticle?.isAvailable()) {
               window.SpectralWebGPUParticle.resize(width, height);
                }
        console.log('[SpectralGL] resizeCanvas ->', width, 'x', height);
    }
    function initShaders(bundle) {
        for (const key of Object.keys(bundle)) {
            if (bundle[key] != null) {
                window._SpectralShaders[key] = bundle[key];
                console.log('[SpectralGL] Shader overridden from C#:', key);
            }
        }
    }
  

    return {
        startRenderLoop,
        stopRenderLoop,
        resizeCanvas,
        resizeAAfbos,
        resetSkyTextures: () => {
            window.SkySystem.reset();
            if (window.ShootingStarSystem) window.ShootingStarSystem.reset();
            if (window.StarFieldSystem) window.StarFieldSystem.reset();
            if (window.SpectralLightningSystem) window.SpectralLightningSystem.reset();
        },
        uploadParsedMesh,
        resetCubeCity: () => window.SpectralCubeCitySystem.reset(),
        initShaders,
        resetWebGPUMeshes: () => window.SpectralWebGPUInterop?.resetMeshes?.(),
        resetTileMap: () => {
            teardownTileMap();          
            initTileMap(_gl);
            console.log('[SpectralGL] TileMap reset + rebuild started for scene switch');
        },
        resetParticles: () => {
             //  window.SpectralParticleSystem.reset();
               window.SpectralWebGPUParticle?.reset();
        },
        resetFoliage,
        clearAllShadowMaps,
        flush: function () {       
            if (_animationHandle !== null) {
                cancelAnimationFrame(_animationHandle);
                _animationHandle = null;
            }
            _dotnetRef = null;
            _initialized = false;
            _pendingFrame = false;
            window._tileGridReady = false;
            window._staticObjectsPreloaded = false;
            window.SpectralStaticObjectsSystem?.reset?.();
            window.SpectralTextSystem?.reset?.();
            window.SkySystem?.reset?.();
            window.ShootingStarSystem?.reset?.();
            window.SpectralLightningSystem?.reset?.();
            window.StarFieldSystem?.reset?.();
            //  window.SpectralParticleSystem?.reset?.();
            window.SpectralParticleSystem.reset();
            window.SpectralCubeCitySystem?.reset?.();
            window.SpectralScrollbarSystem?.reset?.();
            window.SpectralWebGPUInterop?.resetMeshes?.();
            window.SpectralTextureUploads.reset();
            window.SpectralFXAA.reset();
            window.SpectralSMAA.reset();
            window.SpectralTAA.reset();
            window.SpectralAA.reset();
            window.SpectralAAV2.reset();
            window.SpectralAAV3.reset();
            resetFoliage();
            console.log('[SpectralGL] flush() — SPA clean exit complete');
        },
    };
})();
