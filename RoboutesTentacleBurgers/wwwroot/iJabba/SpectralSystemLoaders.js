

// SpectralSystemLoaders.js


window.SpectralLandscape = {
    _saveTimer: null,

    // ── Legacy localStorage (used by C# SaveLandscape / LoadLandscape) ────
    save: function (json, key) {
        const storageKey = key || 'spectralx_landscape';
        if (this._saveTimer) clearTimeout(this._saveTimer);
        this._saveTimer = setTimeout(() => {
            try {
                localStorage.setItem(storageKey, json);
                console.log('[Landscape] Saved —', storageKey, json.length, 'bytes');
            } catch (e) {
                console.warn('[Landscape] Save failed:', e);
            }
            this._saveTimer = null;
        }, 500);
    },

    load: function (key) {
        const storageKey = key || 'spectralx_landscape';
        try {
            const data = localStorage.getItem(storageKey);
            if (!data) {
                console.log('[Landscape] No saved data found for:', storageKey);
                return null;
            }
            console.log('[Landscape] Loaded —', storageKey, data.length, 'bytes');
            return data;
        } catch (e) {
            console.warn('[Landscape] Load failed:', e);
            return null;
        }
    },

    clear: function (key) {
        const storageKey = key || 'spectralx_landscape';
        localStorage.removeItem(storageKey);
        console.log('[Landscape] Cleared:', storageKey);
    },

    // ── Export R16 Height Map ─────────────────────────────────────────────
    exportR16: function (base64, filename) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++)
            bytes[i] = binary.charCodeAt(i);

        const blob = new Blob([bytes], { type: 'application/octet-stream' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename || 'heightmap.r16';
        a.click();
        URL.revokeObjectURL(url);
        console.log('[Landscape] R16 exported:', filename, bytes.length, 'bytes');
    },

    // ── Import R16 Height Map ─────────────────────────────────────────────
    importR16: function () {
        return new Promise((resolve) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = '.r16,.raw';
            input.onchange = () => {
                const file = input.files[0];
                if (!file) { resolve(null); return; }
                const reader = new FileReader();
                reader.onload = () => {
                    const bytes = new Uint8Array(reader.result);
                    let binary = '';
                    for (let i = 0; i < bytes.length; i++)
                        binary += String.fromCharCode(bytes[i]);
                    resolve(btoa(binary));
                };
                reader.readAsArrayBuffer(file);
            };
            input.click();
        });
    },

    // ── Export Paint Map PNG ──────────────────────────────────────────────
    exportPng: function (pixelBase64, width, height, filename) {
        const binary = atob(pixelBase64);
        const rgba = new Uint8ClampedArray(binary.length);
        for (let i = 0; i < binary.length; i++)
            rgba[i] = binary.charCodeAt(i);

        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        canvas.getContext('2d').putImageData(new ImageData(rgba, width, height), 0, 0);

        canvas.toBlob(blob => {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = filename || 'paintmap.png';
            a.click();
            URL.revokeObjectURL(url);
            console.log('[Landscape] PNG exported:', filename, width, 'x', height);
        }, 'image/png');
    },

    // ── Import Paint Map PNG ──────────────────────────────────────────────
    importPng: function () {
        return new Promise((resolve) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = '.png,.bmp';
            input.onchange = () => {
                const file = input.files[0];
                if (!file) { resolve(null); return; }
                const objectUrl = URL.createObjectURL(file);
                const img = new Image();
                img.onload = () => {
                    const canvas = document.createElement('canvas');
                    canvas.width = img.width;
                    canvas.height = img.height;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0);
                    const data = ctx.getImageData(0, 0, img.width, img.height).data;
                    let binary = '';
                    for (let i = 0; i < data.length; i++)
                        binary += String.fromCharCode(data[i]);
                    URL.revokeObjectURL(objectUrl);
                    resolve({ pixels: btoa(binary), width: img.width, height: img.height });
                };
                img.src = objectUrl;
            };
            input.click();
        });
    },

    // ── Load Default From wwwroot/iAssets ─────────────────────────────────
    loadFromAssets: async function (heightmapUrl, paintmapUrl) {
        const result = { heights: null, paint: null };

        if (heightmapUrl) {
            try {
                const resp = await fetch(heightmapUrl);
                if (resp.ok) {
                    const bytes = new Uint8Array(await resp.arrayBuffer());
                    let binary = '';
                    for (let i = 0; i < bytes.length; i++)
                        binary += String.fromCharCode(bytes[i]);
                    result.heights = btoa(binary);
                    console.log('[Landscape] Default heightmap loaded:', bytes.length, 'bytes');
                }
            } catch (e) {
                console.warn('[Landscape] Default heightmap not found:', heightmapUrl);
            }
        }

        if (paintmapUrl) {
            try {
                const resp = await fetch(paintmapUrl);
                if (resp.ok) {
                    const objectUrl = URL.createObjectURL(await resp.blob());
                    const img = new Image();
                    await new Promise((res, rej) => {
                        img.onload = res;
                        img.onerror = rej;
                        img.src = objectUrl;
                    });
                    const canvas = document.createElement('canvas');
                    canvas.width = img.width;
                    canvas.height = img.height;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0);
                    const data = ctx.getImageData(0, 0, img.width, img.height).data;
                    let binary = '';
                    for (let i = 0; i < data.length; i++)
                        binary += String.fromCharCode(data[i]);
                    URL.revokeObjectURL(objectUrl);
                    result.paint = { pixels: btoa(binary), width: img.width, height: img.height };
                    console.log('[Landscape] Default paintmap loaded:', img.width, 'x', img.height);
                }
            } catch (e) {
                console.warn('[Landscape] Default paintmap not found:', paintmapUrl);
            }
        }

        return result;
    }
};

window.SpectralTextSystem = (function () {
    const _atlases = {};
    const _atlasTextures = {};
    const _atlasImages = {};  // ADD — cache image for re-upload on context switch



    function reset() {
        //  console.log("[TextSystem] Resetting textures after context change");

        for (const key in _atlasImages) {
            _atlasTextures[key] = _uploadAtlasTexture(_atlasImages[key]);
        }
    }


    async function loadAtlas(fontKey, jsonUrl, texUrl) {


        if (_atlases[fontKey] && _atlasImages[fontKey]) {
            // JSON already loaded but texture might be stale — re-upload
            _atlasTextures[fontKey] = _uploadAtlasTexture(_atlasImages[fontKey]);
            return;
        }



        //  console.log('[TextSystem] Loading atlas:', fontKey);

        const [jsonRes, img] = await Promise.all([
            fetch(jsonUrl).then(r => r.json()),
            new Promise((res, rej) => {
                const i = new Image();
                i.onload = () => res(i);
                i.onerror = rej;
                i.src = texUrl;
            })
        ]);


        const tex = _uploadAtlasTexture(img);


        _atlases[fontKey] = jsonRes;
        _atlasTextures[fontKey] = tex;
        _atlasImages[fontKey] = img;
        //  console.log('[TextSystem] Atlas ready:', fontKey,
        //    'glyphs:', Object.keys(jsonRes.glyphs || {}).length);
    }










    function _uploadAtlasTexture(img) {
        const canvas = document.getElementById('SpectralX-Viewport');
        if (!canvas) return null;
        const gl = canvas.getContext('webgl2') || canvas.getContext('webgl');
        if (!gl) return null;
        const tex = gl.createTexture();
        gl.bindTexture(gl.TEXTURE_2D, tex);
        gl.pixelStorei(gl.UNPACK_PREMULTIPLY_ALPHA_WEBGL, false);
        gl.texImage2D(gl.TEXTURE_2D, 0, gl.RGBA, gl.RGBA, gl.UNSIGNED_BYTE, img);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.LINEAR);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.CLAMP_TO_EDGE);
        gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
        return tex;
    }

    function buildTextGeometry(meshId, text, fontKey, fontSize, letterSpacing, align) {
        const atlas = _atlases[fontKey];
        if (!atlas) {
            //  console.warn('[TextSystem] Atlas not loaded:', fontKey);
            return false;
        }

        const glyphMap = {};
        for (const g of atlas.glyphs) {
            glyphMap[g.unicode] = g;
        }

        const metrics = atlas.metrics;
        const atlaW = atlas.atlas.width;
        const atlaH = atlas.atlas.height;
        const scale = fontSize / metrics.emSize;

        const verts = [];
        const uvs = [];
        const norms = [];

        // First pass — measure total width for alignment
        let totalWidth = 0;
        for (const ch of text) {
            const g = glyphMap[ch.charCodeAt(0)];
            if (!g) continue;
            totalWidth += (g.advance + letterSpacing) * scale;
        }

        let offsetX = align === 1 ? -totalWidth / 2 :  // Center
            align === 2 ? -totalWidth : 0;    // Right

        // Second pass — build quads
        for (const ch of text) {
            const g = glyphMap[ch.charCodeAt(0)];
            if (!g || !g.planeBounds) {
                if (g) offsetX += g.advance * scale;
                else offsetX += fontSize * 0.3;
                continue;
            }

            if (g.planeBounds && g.atlasBounds) {
                const pb = g.planeBounds;
                const ab = g.atlasBounds;

                const x0 = offsetX + pb.left * scale;
                const x1 = offsetX + pb.right * scale;
                const y0 = pb.bottom * scale;
                const y1 = pb.top * scale;

                const u0 = ab.left / atlaW;
                const u1 = ab.right / atlaW;
                const v0 = 1 - ab.top / atlaH;
                const v1 = 1 - ab.bottom / atlaH;

                // Triangle 1
                verts.push(x0, y0, 0, x1, y0, 0, x1, y1, 0);
                uvs.push(u0, v0, u1, v0, u1, v1);
                // Triangle 2
                verts.push(x0, y0, 0, x1, y1, 0, x0, y1, 0);
                uvs.push(u0, v0, u1, v1, u0, v1);

                for (let i = 0; i < 6; i++)
                    norms.push(0, 0, 1);
            }

            offsetX += (g.advance + letterSpacing) * scale;
        }

        if (verts.length === 0) {
            //   console.warn('[TextSystem] No geometry built for:', text);
            return false;
        }

        window.SpectralGLInterop.uploadParsedMesh(meshId, {
            vertices: new Float32Array(verts),
            normals: new Float32Array(norms),
            uvs: new Float32Array(uvs),
            matBreaks: [verts.length / 3],
            matIndices: [0],
        }, [], []);

        //   console.log('[TextSystem] Built:', meshId,
        //  'chars:', text.length, 'verts:', verts.length / 3);
        return true;
    }

    function getAtlasTexture(fontKey) {
        return _atlasTextures[fontKey] || null;
    }

    function isAtlasLoaded(fontKey) {
        return !!_atlases[fontKey];
    }

    return { loadAtlas, buildTextGeometry, getAtlasTexture, isAtlasLoaded, reset };
})();




// ============================================================
// PRIM SQUARE — hardcoded quad upload for static object sharing
// Matches C# CreateSquare() exactly:
// verts: (-1,1,0) (1,1,0) (1,-1,0) (-1,-1,0)
// quad split into 2 triangles with correct UVs
// ============================================================
window.SpectralPrimLoader = {
    upload: function () {
        const verts = new Float32Array([
            -1, 1, 0, 1, 1, 0, 1, -1, 0,  // tri 1
            -1, 1, 0, 1, -1, 0, -1, -1, 0,  // tri 2
        ]);
        const norms = new Float32Array([
            0, 0, 1, 0, 0, 1, 0, 0, 1,
            0, 0, 1, 0, 0, 1, 0, 0, 1,
        ]);
        const uvs = new Float32Array([
            0, 0, 1, 0, 1, 1,  // tri 1
            0, 0, 1, 1, 0, 1,  // tri 2
        ]);
        window.SpectralGLInterop.uploadParsedMesh('PrimSquare', {
            vertices: verts, normals: norms, uvs: uvs,
            matBreaks: [6], matIndices: [0]
        }, [], []);
        console.log('[PrimLoader] PrimSquare uploaded to JS');
    }
};

