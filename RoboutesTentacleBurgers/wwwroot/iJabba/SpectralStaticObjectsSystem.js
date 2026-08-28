// ============================================================
// STATIC OBJECTS TEXTURE SYSTEM — Scene4 (BWP)
// Mirrors the Splatter Puddle pattern: preload once into a shared
// cache keyed by TYPE (e.g. "Tree", "Rock"), not by mesh instance.
// This means every Static_Tree_N mesh reuses the SAME resolved
// WebGLTexture object immediately — no per-instance placeholder,
// no pink flash, and re-entering frustum culling is instant because
// the texture was never evicted or re-requested.
// ============================================================

window.SpectralStaticObjectsSystem = (function () {

    // type -> WebGLTexture (fully loaded, shared by every instance of that type)
    let _staticTexCache = {};
    // type -> bool, true once the image has finished decoding
    let _staticTexReady = {};

    // Must match SpectralXBWPStaticObjects.PixelSizeMap keys in C#
    const STATIC_TYPES = [
        "Tree", "Rock", "FenceBroken", "TorchNew01", "Chest",
        "GStone", "GStoneCross", "Mushroom", "Skelcorpse001",
        "Grass01", "SkullONStick", "Urn", "Rose01", "Barrel01", "Bush01"
    ];

    function texUrlForType(type) {
        return `/iAssets/${type}.png`;
    }

    // Call once per Scene4 load (mirrors preloadSplatterTextures / preloadBreakTextures)
    function preloadAll(gl) {
        for (const type of STATIC_TYPES) {
            preloadOne(gl, type);
        }
    }

    function preloadOne(gl, type) {
        if (_staticTexCache[type]) return; // already loading/loaded

        const url = texUrlForType(type);

        // 1x1 neutral grey placeholder — NOT magenta/pink, so if this is ever
        // seen for a frame it reads as "loading" rather than "broken/missing".
        const tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, 1, 1, 0,
            gl.RGBA, gl.UNSIGNED_BYTE, new Uint8Array([128, 128, 128, 255]));
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);

        // Register the slot immediately — same tex object gets upgraded
        // in place on load, same pattern as preloadEnemyTextures.
        _staticTexCache[type] = tex;
        _staticTexReady[type] = false;

        const img = new Image();
        img.onload = () => {
            gl.bindTexture(gl.TEXTURE_2D, tex);
            gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, false);
            gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);

            const isPOT = (img.width & (img.width - 1)) === 0 &&
                (img.height & (img.height - 1)) === 0;
            if (isPOT) {
                gl.generateMipmap(gl.TEXTURE_2D);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR_MIPMAP_LINEAR);
            } else {
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
                gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
            }
            gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);

            _staticTexReady[type] = true;
            console.log('[StaticObjects] Preloaded:', type, url);
        };
        img.onerror = (e) => {
            console.warn('[StaticObjects] Failed to load:', type, url, e);
        };
        img.src = url;
    }

    // Given a mesh upload whose meshId is "Static_<Type>_<index>", resolve
    // the type and point _textureCache[meshId] straight at the shared,
    // already-resolving texture object. This is the hook to call from
    // uploadMesh() BEFORE it falls through to the generic placeholder path.
    //
    // Returns true if handled (caller should skip the generic texture upload
    // branch for this mesh), false if this isn't a recognized static mesh.
    function tryResolveStaticTexture(upload, textureCache) {
        if (!upload.meshId || !upload.meshId.startsWith('Static_')) return false;

        // meshId format: Static_<Type>_<globalIndex>  e.g. Static_Tree_12
        // Type names themselves never contain underscores in the C# map,
        // so splitting is safe: ["Static", "<Type>", "<index>"]
        const parts = upload.meshId.split('_');
        if (parts.length < 3) return false;
        const type = parts[1];

        if (!_staticTexCache[type]) return false; // not preloaded — let caller fall back

        textureCache[upload.meshId] = _staticTexCache[type];
        return true;
    }

    function isReady(type) {
        return !!_staticTexReady[type];
    }

    function reset() {
        _staticTexCache = {};
        _staticTexReady = {};
    }

    return {
        preloadAll,
        preloadOne,
        tryResolveStaticTexture,
        isReady,
        reset,
        STATIC_TYPES,
    };

})();