// ============================================================
// SpectralTextureUploads.js
// Texture Preload, Swap and Cache System
// Extracted from SpectralEngine.js — SpectralGLInterop
//
// All shared state is read from window.SE:
//   window.SE.gl               — WebGL2 context
//   window.SE.textureCache     — master texture lookup written by the render loop
//
// SpectralEngine.js is responsible for:
//   - Calling window.SpectralTextureUploads.reset()              in init() and flush()
//   - Calling window.SpectralTextureUploads.preloadEnemyTextures() per enemy type in renderFrame()
//   - Calling window.SpectralTextureUploads.applyEnemyTexSwaps()  per enemy type in renderFrame()
//   - Calling window.SpectralTextureUploads.retryPendingEnemyTexSwaps() per enemy type in renderFrame()
//   - Calling window.SpectralTextureUploads.preloadBreakTextures()    in renderFrame()
//   - Calling window.SpectralTextureUploads.preloadSplatterTextures() in renderFrame()
//   - Calling window.SpectralTextureUploads.preloadCharTextures()     in renderFrame()
//   - Calling window.SpectralTextureUploads.preloadOverlayTextures()  in renderFrame()
//   - Calling window.SpectralTextureUploads.applyCharTexSwaps(frame)  in renderFrame()
//   - Calling window.SpectralTextureUploads.applyBreakTexSwaps(frame) in renderFrame()
//   - Calling window.SpectralTextureUploads.getSplatterTex(url)       in uploadMesh()
//   - Calling window.SpectralTextureUploads.getOverlayTex(url)        in the overlay render pass
//
// Load order: SpectralTextureUploads.js BEFORE SpectralEngine.js
// ============================================================

window.SpectralTextureUploads = (function () {

    // ============================================================
    // PRIVATE STATE
    // ============================================================

    // Enemy textures — keyed by enemyType, then by url
    let _enemyTexCache = {};

    // Breakable object textures — keyed by url
    let _breakTexCache = {};
    let _pendingBreakTexSwaps = [];

    // Splatter / decal textures — keyed by url
    let _splatterTexCache = {};

    // Player character textures — keyed by meshName, then by url
    let _charTexCache = {};

    // Last known active texture url per character — tracks swaps to avoid redundant writes
    let _lastWarriorTexUrl = null;
    let _lastRogueTexUrl = null;
    let _lastMonkTexUrl = null;
    let _lastMageTexUrl = null;

    // Hit overlay textures — keyed by url
    let _overlayTexCache = {};

    // ============================================================
    // PRIVATE HELPERS
    // ============================================================

    // Allocate a 1x1 grey placeholder texture immediately so the slot
    // is registered before the image loads. The same object is upgraded
    // in place once img.onload fires — no re-registration needed.
    function _createPlaceholderTex() {
        const gl = window.SE.gl;
        const tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
            gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 128, 255]));
        return tex;
    }

    function _uploadImageToTex(tex, img) {
        const gl = window.SE.gl;
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    }

    // ============================================================
    // ENEMY TEXTURE SYSTEM
    // Unified preload + swap pattern shared by all enemy types.
    // enemyType is a string key e.g. 'Skeleton', 'Goatman' etc.
    // ============================================================

    function preloadEnemyTextures(enemyType, urls) {
        if (!urls || urls.length === 0) return;
        if (!_enemyTexCache[enemyType]) _enemyTexCache[enemyType] = {};
        const cache = _enemyTexCache[enemyType];
        for (const url of urls) {
            if (cache[url]) continue;
            const tex = _createPlaceholderTex();
            cache[url] = tex; // register slot immediately
            const img = new Image();
            img.onload = () => _uploadImageToTex(tex, img);
            img.src = url;
        }
    }

    function applyEnemyTexSwaps(enemyType, swaps) {
        if (!swaps || swaps.length === 0) return;
        const cache = _enemyTexCache[enemyType];
        if (!cache) return; // preload hasn't run yet for this type
        for (const swap of swaps) {
            const tex = cache[swap.texUrl];
            if (tex) {
                window.SE.textureCache[swap.meshId] = tex;
            } else {
                console.warn('[' + enemyType + '] Swap miss — texUrl not in cache:', swap.texUrl);
            }
        }
    }

    // No-op — kept so existing call sites in renderFrame() don't need to be touched.
    // applyEnemyTexSwaps re-resolves every frame so there is nothing left to retry.
    function retryPendingEnemyTexSwaps(enemyType) {
        // Intentionally empty.
    }

    // ============================================================
    // BREAKABLE OBJECT TEXTURE SYSTEM
    // Separate cache from enemies — breakables have a pending swap
    // queue because the mesh may arrive before the texture is ready.
    // applyBreakTexSwaps() is called once per frame from renderFrame()
    // and handles both new swaps from the frame and the pending queue.
    // ============================================================

    function preloadBreakTextures(urls) {
        if (!urls || urls.length === 0) return;
        for (const url of urls) {
            if (_breakTexCache[url]) continue;
            const tex = _createPlaceholderTex();
            _breakTexCache[url] = tex;
            const img = new Image();
            img.onload = () => _uploadImageToTex(tex, img);
            img.src = url;
        }
    }

    // Handles frame.breakTexSwaps and drains _pendingBreakTexSwaps.
    // Called once per frame from renderFrame() — replaces the inline block.
    function applyBreakTexSwaps(frame) {
        if (frame.breakTexSwaps && frame.breakTexSwaps.length > 0) {
            for (const swap of frame.breakTexSwaps) {
                if (_breakTexCache[swap.texUrl]) {
                    window.SE.textureCache[swap.meshId] = _breakTexCache[swap.texUrl];
                } else {
                    _pendingBreakTexSwaps.push({ meshId: swap.meshId, texUrl: swap.texUrl });
                }
            }
        }

        // Drain pending queue — retry any swaps that missed on a previous frame
        for (let i = _pendingBreakTexSwaps.length - 1; i >= 0; i--) {
            const swap = _pendingBreakTexSwaps[i];
            if (_breakTexCache[swap.texUrl]) {
                window.SE.textureCache[swap.meshId] = _breakTexCache[swap.texUrl];
                _pendingBreakTexSwaps.splice(i, 1);
            }
        }
    }

    // ============================================================
    // SPLATTER / DECAL TEXTURE SYSTEM
    // Read back by uploadMesh() in SpectralEngine.js via getSplatterTex().
    // ============================================================

    function preloadSplatterTextures(urls) {
        if (!urls || urls.length === 0) return;
        for (const url of urls) {
            if (_splatterTexCache[url]) continue;
            const tex = _createPlaceholderTex();
            _splatterTexCache[url] = tex;
            const img = new Image();
            img.onload = () => _uploadImageToTex(tex, img);
            img.src = url;
        }
    }

    // Called from uploadMesh() in SpectralEngine.js.
    // Returns the cached WebGL texture or null if not yet loaded.
    function getSplatterTex(url) {
        return _splatterTexCache[url] || null;
    }

    // ============================================================
    // PLAYER CHARACTER TEXTURE SYSTEM
    // Preloads a set of animation frame textures per character mesh.
    // applyCharTexSwaps() handles the per-frame active-frame swap
    // and owns the _last*TexUrl tracking internally.
    // ============================================================

    function preloadCharTextures(meshName, urls) {
        if (!urls || urls.length === 0) return;
        if (!_charTexCache[meshName]) _charTexCache[meshName] = {};
        const cache = _charTexCache[meshName];
        for (const url of urls) {
            if (cache[url]) continue;
            const tex = _createPlaceholderTex();
            cache[url] = tex;
            const img = new Image();
            img.onload = () => _uploadImageToTex(tex, img);
            img.src = url;
        }
    }

    // Checks each character's active texture url against its last known value.
    // Writes into window.SE.textureCache when a new frame is needed.
    // Called once per frame from renderFrame() — replaces the inline block.
    function applyCharTexSwaps(frame) {
        if (frame.warriorTexUrl && frame.warriorTexUrl !== _lastWarriorTexUrl) {
            if (_charTexCache['WarriorSquare']?.[frame.warriorTexUrl]) {
                _lastWarriorTexUrl = frame.warriorTexUrl;
                window.SE.textureCache['WarriorSquare'] =
                    _charTexCache['WarriorSquare'][frame.warriorTexUrl];
            }
        }
        if (frame.rogueTexUrl && frame.rogueTexUrl !== _lastRogueTexUrl) {
            if (_charTexCache['RogueSquare']?.[frame.rogueTexUrl]) {
                _lastRogueTexUrl = frame.rogueTexUrl;
                window.SE.textureCache['RogueSquare'] =
                    _charTexCache['RogueSquare'][frame.rogueTexUrl];
            }
        }
        if (frame.monkTexUrl && frame.monkTexUrl !== _lastMonkTexUrl) {
            if (_charTexCache['MonkSquare']?.[frame.monkTexUrl]) {
                _lastMonkTexUrl = frame.monkTexUrl;
                window.SE.textureCache['MonkSquare'] =
                    _charTexCache['MonkSquare'][frame.monkTexUrl];
            }
        }
        if (frame.mageTexUrl && frame.mageTexUrl !== _lastMageTexUrl) {
            if (_charTexCache['MageSquare']?.[frame.mageTexUrl]) {
                _lastMageTexUrl = frame.mageTexUrl;
                window.SE.textureCache['MageSquare'] =
                    _charTexCache['MageSquare'][frame.mageTexUrl];
            }
        }
    }

    // ============================================================
    // HIT OVERLAY TEXTURE SYSTEM
    // Separate cache for flash/hit overlay textures drawn on top of
    // character meshes in a second pass in renderFrame().
    // Read back via getOverlayTex() in the overlay render pass.
    // ============================================================

    function preloadOverlayTextures(urls) {
        if (!urls || urls.length === 0) return;
        for (const url of urls) {
            if (_overlayTexCache[url]) continue;
            const tex = _createPlaceholderTex();
            _overlayTexCache[url] = tex;
            const img = new Image();
            img.onload = () => _uploadImageToTex(tex, img);
            img.src = url;
        }
    }

    // Called from the overlay render pass in renderFrame().
    // Returns the cached WebGL texture or null if not yet loaded.
    function getOverlayTex(url) {
        return _overlayTexCache[url] || null;
    }

    // ============================================================
    // RESET
    // Clears all caches and tracking state.
    // Called from SpectralEngine.js init() and flush().
    // Does NOT delete WebGL textures — the GL context may be gone
    // or about to be recreated. The old textures are simply abandoned.
    // ============================================================
    function reset() {
        _enemyTexCache = {};
        _breakTexCache = {};
        _pendingBreakTexSwaps = [];
        _splatterTexCache = {};
        _charTexCache = {};
        _lastWarriorTexUrl = null;
        _lastRogueTexUrl = null;
        _lastMonkTexUrl = null;
        _lastMageTexUrl = null;
        _overlayTexCache = {};
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return {
        // Enemy
        preloadEnemyTextures,
        applyEnemyTexSwaps,
        retryPendingEnemyTexSwaps,

        // Breakables
        preloadBreakTextures,
        applyBreakTexSwaps,

        // Splatter
        preloadSplatterTextures,
        getSplatterTex,

        // Characters
        preloadCharTextures,
        applyCharTexSwaps,

        // Overlays
        preloadOverlayTextures,
        getOverlayTex,

        // Lifecycle
        reset,
    };

})();