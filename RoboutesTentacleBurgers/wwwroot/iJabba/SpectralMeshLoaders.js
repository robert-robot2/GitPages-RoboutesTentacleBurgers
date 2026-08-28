








//SpectralMeshLoader.js

// FBX Parser JS Helper


window.SpectralFBXHelper = {

    loadMesh: async function (url) {
        try {
            // Native fetch — bypasses WASM HTTP entirely
            const response = await fetch(url);
            if (!response.ok) return null;

            const buffer = await response.arrayBuffer();
            const view = new DataView(buffer);

            // Validate FBX binary magic header
            const magic = new Uint8Array(buffer, 0, 18);
            const header = String.fromCharCode(...magic);
            if (!header.startsWith('Kaydara FBX Binary')) {
                console.warn('[FBXHelper] Not binary FBX:', url);
                return null;
            }

            // FBX version at byte 23
            const version = view.getUint32(23, true);
            console.log('[FBXHelper] Parsing:', url, 'version:', version);

            const result = await this.parseFBX(buffer, view, version);

            // Null check — handles FBX files with no geometry or empty vertex arrays
            if (!result || !result.vertices || result.vertices.length === 0) {
                console.warn('[FBXHelper] Empty result for:', url);
                return null;
            }

            console.log('[FBXHelper] Done:', url,
                'verts:', result.vertices.length / 3,
                'faces:', result.rawIndices.length);

            return result;

        } catch (ex) {
            console.error('[FBXHelper] Failed:', url, ex);
            return null;
        }
    },

    parseFBX: async function (buffer, view, version) {
        try {
            const nodes = [];
            let offset = 27;
            const byteLength = buffer.byteLength;

            while (offset < byteLength - 160) {
                const node = this.readNode(buffer, view, offset, version);
                if (!node || node.endOffset === 0) break;
                nodes.push(node);
                offset = node.endOffset;
            }

            const objectsNode = nodes.find(n => n.name === 'Objects');
            if (!objectsNode) return null;

            const geomNode = objectsNode.children.find(n => n.name === 'Geometry');
            if (!geomNode) return null;

            const vertNode = geomNode.children.find(n => n.name === 'Vertices');
            const vertices = vertNode ? await this.readDoubleArray(vertNode) : [];

            const idxNode = geomNode.children.find(n => n.name === 'PolygonVertexIndex');
            const rawIndices = idxNode ? await this.readIntArray(idxNode) : [];

            const uvLayerNode = geomNode.children.find(n => n.name === 'LayerElementUV');
            let uvs = [], uvIndices = [];
            if (uvLayerNode) {
                const uvNode = uvLayerNode.children.find(n => n.name === 'UV');
                const uvIdxNode = uvLayerNode.children.find(n => n.name === 'UVIndex');
                uvs = uvNode ? await this.readDoubleArray(uvNode) : [];
                uvIndices = uvIdxNode ? await this.readIntArray(uvIdxNode) : [];
            }

            const normLayerNode = geomNode.children.find(n => n.name === 'LayerElementNormal');
            let normals = [], normalIndices = [];
            if (normLayerNode) {
                const normNode = normLayerNode.children.find(n => n.name === 'Normals');
                const normIdxNode = normLayerNode.children.find(n => n.name === 'NormalsIndex');
                normals = normNode ? await this.readDoubleArray(normNode) : [];
                normalIndices = normIdxNode ? await this.readIntArray(normIdxNode) : [];
            }

            const matLayerNode = geomNode.children.find(n => n.name === 'LayerElementMaterial');
            let materialIndices = [];
            if (matLayerNode) {
                const matIdxNode = matLayerNode.children.find(n => n.name === 'Materials');
                materialIndices = matIdxNode ? await this.readIntArray(matIdxNode) : [];
            }

            // NEW — extract embedded textures
            const textures = this.extractTextures(nodes, objectsNode);
            const materialColors = this.extractMaterialColors(nodes, objectsNode);

            return {
                vertices, rawIndices, uvs, uvIndices,
                normals, normalIndices, materialIndices,
                textures,
                materialColors  // NEW
            };

        } catch (ex) {
            console.error('[FBXHelper] parseFBX error:', ex);
            return null;
        }
    },

    loadAndUpload: async function (url, meshName) {
        try {
            // Wait for GL to be ready — uploadParsedMesh needs _gl initialized
            if (!window.SpectralGLInterop) {
                console.warn('[FBXHelper] SpectralGLInterop not ready for:', meshName);
                return false;
            }

            const raw = await this.loadMesh(url);
            if (!raw) {
                console.warn('[FBXHelper] loadMesh returned null for:', meshName);
                return false;
            }

            const processed = this.processMesh(raw);
            if (!processed) {
                console.warn('[FBXHelper] processMesh returned null for:', meshName);
                return false;
            }
            if (meshName === 'Bush001') {
                console.log('[Bush Full Debug]',
                    'raw verts:', raw.vertices.length,
                    'raw indices:', raw.rawIndices.length,
                    'processed verts:', processed.vertices.length,
                    'matBreaks:', processed.matBreaks,
                    'matIndices:', processed.matIndices,
                    'textures count:', raw.textures?.length,
                    'materialColors:', raw.materialColors,
                    'textures[0] exists:', !!raw.textures?.[0],
                    'textures[0] length:', raw.textures?.[0]?.length
                );
            }
            // Upload directly to WebGL — no C# round-trip for geometry
            window.SpectralGLInterop.uploadParsedMesh(meshName, processed, raw.textures || [], raw.materialColors || []);

            console.log('[FBXHelper] loadAndUpload success:', meshName,
                'verts:', processed.vertices.length / 3);
            return true;

        } catch (ex) {
            console.error('[FBXHelper] loadAndUpload failed:', meshName, ex);
            return false;
        }
    },

    loadAllAndUploadJson: async function (meshListJson) {
        // Parse the JSON string — guarantees a real JS array regardless of
        // how Blazor serialized the C# anonymous type array
        const meshList = JSON.parse(meshListJson);
        return await this.loadAllAndUpload(meshList);
    },

    loadAllAndUpload: async function (meshList) {
        console.log('[FBXHelper] loadAllAndUpload starting —', meshList.length, 'meshes');

        // Wait for GL to be ready — poll until _gl is initialized
        // REPLACE the existing await new Promise block with:
        await new Promise(resolve => {
            const check = () => {
                if (window.SpectralGLInterop &&
                    document.getElementById('SpectralX-Viewport')) {
                    resolve();
                } else {
                    setTimeout(check, 50);
                }
            };
            check();
        });
        // All fetches run in parallel — browser handles concurrent requests
        const results = await Promise.all(
            meshList.map(m => this.loadAndUpload(m.url, m.name))
        );

        const succeeded = results.filter(r => r === true).length;
        const failed = results.filter(r => r === false).length;

        console.log('[FBXHelper] loadAllAndUpload complete —',
            succeeded, 'succeeded,', failed, 'failed');

        return results;
    },




    extractTextures: function (rootNodes, objectsNode) {
        // Step 1 — find embedded video/image content
        const videoDataUrls = {};
        const videoNodes = objectsNode.children.filter(n => n.name === 'Video');

        for (const videoNode of videoNodes) {
            const contentNode = videoNode.children.find(n => n.name === 'Content');
            if (!contentNode) continue;

            const offset = contentNode.propsStart;
            const view = contentNode.view;
            const typeCode = String.fromCharCode(view.getUint8(offset));
            if (typeCode !== 'R') continue;

            const byteLen = view.getUint32(offset + 1, true);
            if (byteLen === 0) continue;

            const bytes = new Uint8Array(contentNode.buffer, offset + 5, byteLen);
            let mimeType = 'image/png';
            if (bytes[0] === 0xFF && bytes[1] === 0xD8) mimeType = 'image/jpeg';

            // Convert to base64
            let binary = '';
            const chunkSize = 8192;
            for (let i = 0; i < bytes.length; i += chunkSize) {
                binary += String.fromCharCode(...bytes.subarray(i, i + chunkSize));
            }
            const dataUrl = `data:${mimeType};base64,${btoa(binary)}`;

            // Get video ID from already-parsed properties
            const idProp = videoNode.numProps > 0
                ? this.readFirstPropId(videoNode)
                : 0;

            if (idProp !== 0) {
                videoDataUrls[idProp] = dataUrl;
                console.log('[FBXHelper] Texture extracted, id:', idProp,
                    'bytes:', byteLen, 'type:', mimeType);
            }
        }

        if (Object.keys(videoDataUrls).length === 0) return [];

        // Step 2 — read Connections to map material -> texture -> video
        const connectionsNode = rootNodes.find(n => n.name === 'Connections');
        const texToVideo = {};
        const matToTex = {};

        if (connectionsNode) {
            for (const conn of connectionsNode.children) {
                if (!conn || conn.numProps < 3) continue;
                try {
                    const connType = this.readPropString(conn, 0);
                    const idA = this.readPropId(conn, 1);
                    const idB = this.readPropId(conn, 2);

                    if (connType === 'OO' && videoDataUrls[idA] !== undefined)
                        texToVideo[idB] = idA;
                    if (connType === 'OP')
                        matToTex[idB] = idA;
                } catch (e) { continue; }
            }
        }

        // Step 3 — map material nodes to texture slots
        const materialNodes = objectsNode.children.filter(n => n.name === 'Material');
        const slotTextures = [];

        for (const matNode of materialNodes) {
            const matId = this.readFirstPropId(matNode);
            const texId = matToTex[matId];
            const vidId = texId !== undefined ? texToVideo[texId] : undefined;
            slotTextures.push(vidId !== undefined ? videoDataUrls[vidId] : null);
        }

        console.log('[FBXHelper] extractTextures slots:', slotTextures.length,
            'with textures:', slotTextures.filter(t => t).length);
        return slotTextures;
    },

    // Helper — read the first property as a numeric ID (Long or Int)
    readFirstPropId: function (node) {
        const offset = node.propsStart;
        const view = node.view;
        const typeCode = String.fromCharCode(view.getUint8(offset));
        if (typeCode === 'L') return Number(view.getBigInt64(offset + 1, true));
        if (typeCode === 'I') return view.getInt32(offset + 1, true);
        return 0;
    },

    // Helper — read the Nth property as a numeric ID
    readPropId: function (node, propIndex) {
        let offset = node.propsStart;
        const view = node.view;
        for (let i = 0; i < propIndex; i++) {
            const tc = String.fromCharCode(view.getUint8(offset)); offset++;
            offset += this.skipPropBytes(view, offset, tc);
        }
        const typeCode = String.fromCharCode(view.getUint8(offset)); offset++;
        if (typeCode === 'L') return Number(view.getBigInt64(offset, true));
        if (typeCode === 'I') return view.getInt32(offset, true);
        return 0;
    },

    // Helper — read the Nth property as a string
    readPropString: function (node, propIndex) {
        let offset = node.propsStart;
        const view = node.view;
        for (let i = 0; i < propIndex; i++) {
            const tc = String.fromCharCode(view.getUint8(offset)); offset++;
            offset += this.skipPropBytes(view, offset, tc);
        }
        const typeCode = String.fromCharCode(view.getUint8(offset)); offset++;
        if (typeCode === 'S') {
            const len = view.getUint32(offset, true);
            const bytes = new Uint8Array(node.buffer, offset + 4, len);
            return String.fromCharCode(...bytes);
        }
        return '';
    },

    // Helper — how many bytes does this property value occupy
    skipPropBytes: function (view, offset, typeCode) {
        switch (typeCode) {
            case 'Y': return 2;
            case 'C': return 1;
            case 'I': case 'F': return 4;
            case 'D': case 'L': return 8;
            case 'S': case 'R': return 4 + view.getUint32(offset, true);
            case 'f': case 'd': case 'l': case 'i': case 'b':
                return 12 + view.getUint32(offset + 8, true) *
                    (typeCode === 'd' || typeCode === 'l' ? 8 : 4);
            default: return 0;
        }
    },

    extractMaterialColors: function (rootNodes, objectsNode) {
        const materialNodes = objectsNode.children.filter(n => n.name === 'Material');
        console.log('[FBXHelper] extractMaterialColors — material nodes found:', materialNodes.length);
        const colors = [];

        for (const matNode of materialNodes) {
            const props70 = matNode.children.find(n => n.name === 'Properties70');
            if (!props70) { colors.push('1,1,1,1'); continue; }

            let r = 1, g = 1, b = 1;
            for (const p of props70.children) {
                const propName = this.readPropString(p, 0);
                if (propName !== 'DiffuseColor') continue;
                try {
                    r = this.readPropFloat(p, 4);
                    g = this.readPropFloat(p, 5);
                    b = this.readPropFloat(p, 6);
                } catch (e) { }
                break;
            }
            colors.push(`${r},${g},${b},1`);
        }
        console.log('[FBXHelper] extractMaterialColors result:', colors);
        return colors;
    },

    readPropFloat: function (node, propIndex) {
        let offset = node.propsStart;
        const view = node.view;
        for (let i = 0; i < propIndex; i++) {
            const tc = String.fromCharCode(view.getUint8(offset)); offset++;
            offset += this.skipPropBytes(view, offset, tc);
        }
        const typeCode = String.fromCharCode(view.getUint8(offset)); offset++;
        if (typeCode === 'D') return view.getFloat64(offset, true);
        if (typeCode === 'F') return view.getFloat32(offset, true);
        return 1;
    },








    readNode: function (buffer, view, offset, version) {
        try {
            let endOffset, numProps, propListLen;

            if (version >= 7500) {
                endOffset = Number(view.getBigUint64(offset, true)); offset += 8;
                numProps = Number(view.getBigUint64(offset, true)); offset += 8;
                propListLen = Number(view.getBigUint64(offset, true)); offset += 8;
            } else {
                endOffset = view.getUint32(offset, true); offset += 4;
                numProps = view.getUint32(offset, true); offset += 4;
                propListLen = view.getUint32(offset, true); offset += 4;
            }

            if (endOffset === 0) return null;

            const nameLen = view.getUint8(offset); offset += 1;
            const nameBytes = new Uint8Array(buffer, offset, nameLen);
            const name = String.fromCharCode(...nameBytes);
            offset += nameLen;

            // Store property offsets for lazy reading
            const propsStart = offset;
            offset += propListLen;

            // Read children
            const children = [];
            while (offset < endOffset - (version >= 7500 ? 25 : 13)) {
                const child = this.readNode(buffer, view, offset, version);
                if (!child || child.endOffset === 0) break;
                children.push(child);
                offset = child.endOffset;
            }

            return {
                name,
                endOffset,
                numProps,
                propsStart,
                propListLen,
                children,
                buffer,
                view,
                version
            };

        } catch (ex) {
            console.error('[FBXHelper] readNode error at offset:', offset, ex);
            return null;
        }
    },

    readDoubleArray: async function (node) {
        // First property of the node contains the array
        const offset = node.propsStart;
        const view = node.view;
        const typeCode = String.fromCharCode(view.getUint8(offset));

        if (typeCode === 'd') {
            return await this.readTypedArray(node.buffer, view, offset + 1, 'double');
        }
        if (typeCode === 'f') {
            return await this.readTypedArray(node.buffer, view, offset + 1, 'float');
        }
        return [];
    },

    readIntArray: async function (node) {
        const offset = node.propsStart;
        const view = node.view;
        const typeCode = String.fromCharCode(view.getUint8(offset));

        if (typeCode === 'i') {
            return await this.readTypedArray(node.buffer, view, offset + 1, 'int32');
        }
        if (typeCode === 'l') {
            return await this.readTypedArray(node.buffer, view, offset + 1, 'int64');
        }
        return [];
    },





    readTypedArray: async function (buffer, view, offset, type) {
        const arrayLength = view.getUint32(offset, true); offset += 4;
        const encoding = view.getUint32(offset, true); offset += 4;
        const compressedLength = view.getUint32(offset, true); offset += 4;

        let rawBytes;

        if (encoding === 1) {
            const compressed = buffer.slice(offset, offset + compressedLength);
            rawBytes = await this.decompress(compressed);
        } else {
            const byteSize = this.getByteSize(type);
            rawBytes = buffer.slice(offset, offset + arrayLength * byteSize);
        }

        return this.bytesToTypedArray(rawBytes, type, arrayLength);
    },

    decompress: async function (buffer) {
        const data = new Uint8Array(buffer);

        // Detect zlib header (0x78 0x9C / 0x78 0x01 / 0x78 0xDA / 0x78 0x5E)
        const hasZlibHeader = data[0] === 0x78 &&
            (data[1] === 0x9C || data[1] === 0x01 ||
                data[1] === 0xDA || data[1] === 0x5E);

        // Try zlib ('deflate' in the Streams API means deflate+zlib-header)
        if (hasZlibHeader) {
            try {
                return await this._decompress(buffer, 'deflate');
            } catch (e) {
                console.warn('[FBXHelper] zlib decompress failed, trying raw:', e);
            }
        }

        // Try raw deflate (Blender FBX typically uses this without a header)
        try {
            return await this._decompress(buffer, 'deflate-raw');
        } catch (e) {
            console.warn('[FBXHelper] raw deflate also failed:', e);
            return null;
        }
    },

    _decompress: async function (buffer, format) {
        const ds = new DecompressionStream(format);
        const writer = ds.writable.getWriter();
        const reader = ds.readable.getReader();

        writer.write(new Uint8Array(buffer));
        writer.close();

        const chunks = [];
        let totalLen = 0;
        while (true) {
            const { done, value } = await reader.read();
            if (done) break;
            chunks.push(value);
            totalLen += value.length;
        }

        const result = new Uint8Array(totalLen);
        let off = 0;
        for (const chunk of chunks) { result.set(chunk, off); off += chunk.length; }
        return result.buffer;
    },





    bytesToTypedArray: function (buffer, type, length) {
        const view = new DataView(buffer instanceof ArrayBuffer ? buffer : buffer.buffer);
        const result = [];
        let offset = 0;

        if (type === 'double') {
            for (let i = 0; i < length; i++) {
                result.push(view.getFloat64(offset, true));
                offset += 8;
            }
        } else if (type === 'float') {
            for (let i = 0; i < length; i++) {
                result.push(view.getFloat32(offset, true));
                offset += 4;
            }
        } else if (type === 'int32') {
            for (let i = 0; i < length; i++) {
                result.push(view.getInt32(offset, true));
                offset += 4;
            }
        } else if (type === 'int64') {
            for (let i = 0; i < length; i++) {
                result.push(Number(view.getBigInt64(offset, true)));
                offset += 8;
            }
        }
        return result;
    },
    processMesh: function (raw) {
        const { vertices, rawIndices, uvs, uvIndices, normals } = raw;
        if (!vertices.length || !rawIndices.length) return null;

        const outVerts = [];
        const outNorms = [];
        const outUVs = [];

        const matBreaks = [];
        const matIndices = [];
        let lastMatIdx = -1;
        let vertsAtLastBreak = 0;
        let totalVerts = 0;
        let faceCounter = 0;

        let i = 0;
        while (i < rawIndices.length) {
            // Collect polygon
            const poly = [];
            while (i < rawIndices.length) {
                const idx = rawIndices[i];
                poly.push({
                    v: idx < 0 ? ~idx : idx,
                    uvi: (uvIndices && uvIndices[i] >= 0) ? uvIndices[i] : -1,
                    ni: (raw.normalIndices && raw.normalIndices[i] >= 0)
                        ? raw.normalIndices[i]
                        : (idx < 0 ? ~idx : idx)
                });
                i++;
                if (idx < 0) break;
            }

            if (poly.length < 3) { faceCounter++; continue; }

            // Material index for this face
            const matIdx = (raw.materialIndices && raw.materialIndices[faceCounter] !== undefined)
                ? raw.materialIndices[faceCounter] : 0;

            if (matIdx !== lastMatIdx) {
                if (lastMatIdx >= 0) {
                    matBreaks.push(totalVerts - vertsAtLastBreak);
                    matIndices.push(lastMatIdx);
                    vertsAtLastBreak = totalVerts;
                }
                lastMatIdx = matIdx;
            }

            faceCounter++;

            if (poly.length === 3) {
                for (const p of [poly[0], poly[1], poly[2]]) {
                    this._pushVert(outVerts, outNorms, outUVs,
                        vertices, normals, uvs, p);
                }
            } else if (poly.length === 4) {
                const order = [poly[0], poly[3], poly[2],
                poly[0], poly[2], poly[1]];
                for (const p of order) {
                    this._pushVert(outVerts, outNorms, outUVs,
                        vertices, normals, uvs, p, true);
                }

            } else {
                // Reversed fan winding to match C# parser XYZ normal direction
                for (let t = 1; t < poly.length - 1; t++) {
                    for (const p of [poly[0], poly[t + 1], poly[t]]) {
                        this._pushVert(outVerts, outNorms, outUVs,
                            vertices, normals, uvs, p, true);
                    }
                }
            }

            totalVerts = outVerts.length / 3;
        }

        // Close final material break
        if (lastMatIdx >= 0) {
            matBreaks.push(totalVerts - vertsAtLastBreak);
            matIndices.push(lastMatIdx);
        }

        if (outVerts.length === 0) return null;

        return {
            vertices: new Float32Array(outVerts),
            normals: new Float32Array(outNorms),
            uvs: new Float32Array(outUVs),
            matBreaks,
            matIndices,
        };
    },

    // Shared vertex push — keeps processMesh readable
    _pushVert: function (outVerts, outNorms, outUVs,
        vertices, normals, uvs, p, flipNormal = false) {
        outVerts.push(
            vertices[p.v * 3],
            vertices[p.v * 3 + 1],
            vertices[p.v * 3 + 2]
        );

        if (normals.length > 0 && p.ni * 3 + 2 < normals.length) {
            const nx = normals[p.ni * 3];
            const ny = normals[p.ni * 3 + 1];
            const nz = normals[p.ni * 3 + 2];

            outNorms.push(
                flipNormal ? -nx : nx,
                flipNormal ? -ny : ny,
                flipNormal ? -nz : nz
            );

        } else {
            outNorms.push(0, 0, 1);
        }
        // FBX stores UV V-axis as bottom-origin (0=bottom, 1=top)
        // WebGL expects top-origin (0=top, 1=bottom)
        // Flip V with (1.0 - v) to correct texture display on tri faces
        // Matches the C# parser: uvLookup.Add(new Vector2(u, 1.0f - v))
        if (p.uvi >= 0 && p.uvi * 2 + 1 < uvs.length) {
            outUVs.push(uvs[p.uvi * 2], 1.0 - uvs[p.uvi * 2 + 1]);
        } else if (uvs.length > 0) {
            const fallbackIdx = (outUVs.length / 2) % (uvs.length / 2);
            outUVs.push(uvs[fallbackIdx * 2], 1.0 - uvs[fallbackIdx * 2 + 1]);
        } else {
            outUVs.push(0, 0);
        }

    },



    getByteSize: function (type) {
        if (type === 'double' || type === 'int64') return 8;
        if (type === 'float' || type === 'int32') return 4;
        return 4;
    }
};



// STL Parser
// STL Parser
// STL Parser


window.SpectralSTLHelper = {

    loadAndUpload: async function (url, meshName, options) {
        try {
            const smooth = options?.smooth ?? false;

            console.log('[STLHelper] Loading:', url, 'mesh:', meshName, 'smooth:', smooth);

            const response = await fetch(url);
            if (!response.ok) {
                console.warn('[STLHelper] Fetch failed:', url, response.status);
                return false;
            }

            const buffer = await response.arrayBuffer();
            if (!buffer || buffer.byteLength === 0) {
                console.warn('[STLHelper] Empty buffer:', url);
                return false;
            }

            const isBinary = this.detectBinary(buffer);
            console.log('[STLHelper] Format:', isBinary ? 'Binary' : 'ASCII');

            let triangles = null;
            if (isBinary) {
                triangles = this.parseBinary(buffer);
            } else {
                triangles = this.parseASCII(buffer);
            }

            if (!triangles || triangles.length === 0) {
                console.warn('[STLHelper] No triangles parsed:', meshName);
                return false;
            }

            console.log('[STLHelper] Triangles parsed:', triangles.length);

            const processed = smooth
                ? this.buildSmooth(triangles)
                : this.buildFlat(triangles);

            if (!processed || processed.vertices.length === 0) {
                console.warn('[STLHelper] Build failed:', meshName);
                return false;
            }

            // No textures, no material colors — STL is pure geometry
            // Display material is assigned by the engine in C#
            window.SpectralGLInterop.uploadParsedMesh(
                meshName, processed, [], []
            );

            console.log('[STLHelper] Uploaded:', meshName,
                'verts:', processed.vertices.length / 3,
                'smooth:', smooth);

            return true;

        } catch (ex) {
            console.error('[STLHelper] loadAndUpload failed:', meshName, ex);
            return false;
        }
    },

    // ----------------------------------------------------------------
    // Detect binary vs ASCII
    // STL binary: 80 byte header that does NOT start with "solid"
    // ASCII STL:  starts with "solid" followed by a name
    // Edge case: some binary STLs start with "solid" in the header —
    // so we also check if the file has the correct binary size
    // ----------------------------------------------------------------
    detectBinary: function (buffer) {
        if (buffer.byteLength < 84) return false;

        const view = new DataView(buffer);
        const triCount = view.getUint32(80, true);
        const expectedSize = 84 + triCount * 50;

        // If file size matches binary formula its binary regardless of header
        if (buffer.byteLength === expectedSize) return true;

        // Check ASCII header "solid"
        const header = new Uint8Array(buffer, 0, 5);
        const headerStr = String.fromCharCode(...header).toLowerCase();
        if (headerStr === 'solid') return false;

        return true;
    },

    // ----------------------------------------------------------------
    // Binary STL parser
    // Layout per triangle: 12 bytes normal + 36 bytes (3x verts) + 2 bytes attr
    // Total per triangle: 50 bytes
    // File layout: 80 byte header + 4 byte count + N * 50 bytes
    // ----------------------------------------------------------------
    parseBinary: function (buffer) {
        const view = new DataView(buffer);
        const triCount = view.getUint32(80, true);

        console.log('[STLHelper] Binary triangle count:', triCount);

        if (triCount === 0) return [];

        const triangles = [];
        let offset = 84;

        for (let i = 0; i < triCount; i++) {
            // Face normal — 3 floats
            const nx = view.getFloat32(offset, true);
            const ny = view.getFloat32(offset + 4, true);
            const nz = view.getFloat32(offset + 8, true);
            offset += 12;

            // 3 vertices — each 3 floats
            const v0x = view.getFloat32(offset, true);
            const v0y = view.getFloat32(offset + 4, true);
            const v0z = view.getFloat32(offset + 8, true);
            offset += 12;

            const v1x = view.getFloat32(offset, true);
            const v1y = view.getFloat32(offset + 4, true);
            const v1z = view.getFloat32(offset + 8, true);
            offset += 12;

            const v2x = view.getFloat32(offset, true);
            const v2y = view.getFloat32(offset + 4, true);
            const v2z = view.getFloat32(offset + 8, true);
            offset += 12;

            // Attribute byte count — skip 2 bytes
            offset += 2;

            triangles.push({
                normal: [nx, ny, nz],
                v0: [v0x, v0y, v0z],
                v1: [v1x, v1y, v1z],
                v2: [v2x, v2y, v2z]
            });
        }

        return triangles;
    },

    // ----------------------------------------------------------------
    // ASCII STL parser
    // Format:
    //   solid name
    //     facet normal nx ny nz
    //       outer loop
    //         vertex x y z
    //         vertex x y z
    //         vertex x y z
    //       endloop
    //     endfacet
    //   endsolid
    // ----------------------------------------------------------------
    parseASCII: function (buffer) {
        const text = new TextDecoder().decode(buffer);
        const triangles = [];

        // Split into facet blocks
        const facetRegex = /facet\s+normal\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+outer\s+loop\s+vertex\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+vertex\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+vertex\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+endloop\s+endfacet/gi;

        let match;
        while ((match = facetRegex.exec(text)) !== null) {
            triangles.push({
                normal: [
                    parseFloat(match[1]),
                    parseFloat(match[2]),
                    parseFloat(match[3])
                ],
                v0: [parseFloat(match[4]), parseFloat(match[5]), parseFloat(match[6])],
                v1: [parseFloat(match[7]), parseFloat(match[8]), parseFloat(match[9])],
                v2: [parseFloat(match[10]), parseFloat(match[11]), parseFloat(match[12])]
            });
        }

        console.log('[STLHelper] ASCII triangles found:', triangles.length);
        return triangles;
    },

    // ----------------------------------------------------------------
    // Flat shading build
    // Each triangle gets its own 3 vertices with the STL face normal
    // No vertex sharing — produces the hard edge crystalline look
    // STL face normals are used directly — exact to the source file
    // ----------------------------------------------------------------
    buildFlat: function (triangles) {
        const verts = [];
        const norms = [];
        const uvs = [];

        for (const tri of triangles) {
            // Use STL face normal — or compute if STL normal is zero
            let nx = tri.normal[0];
            let ny = tri.normal[1];
            let nz = tri.normal[2];

            const nLen = Math.sqrt(nx * nx + ny * ny + nz * nz);
            if (nLen < 0.0001) {
                // Compute face normal from vertices
                const e1x = tri.v1[0] - tri.v0[0];
                const e1y = tri.v1[1] - tri.v0[1];
                const e1z = tri.v1[2] - tri.v0[2];
                const e2x = tri.v2[0] - tri.v0[0];
                const e2y = tri.v2[1] - tri.v0[1];
                const e2z = tri.v2[2] - tri.v0[2];
                nx = e1y * e2z - e1z * e2y;
                ny = e1z * e2x - e1x * e2z;
                nz = e1x * e2y - e1y * e2x;
                const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
                if (len > 0) { nx /= len; ny /= len; nz /= len; }
            } else {
                nx /= nLen; ny /= nLen; nz /= nLen;
            }

            for (const v of [tri.v0, tri.v1, tri.v2]) {
                verts.push(v[0], v[1], v[2]);
                norms.push(nx, ny, nz);
                // Triplanar UV — project dominant axis onto UV
                uvs.push(...this.triplanarUV(v, nx, ny, nz));
            }
        }

        return {
            vertices: new Float32Array(verts),
            normals: new Float32Array(norms),
            uvs: new Float32Array(uvs),
            matBreaks: [verts.length / 3],
            matIndices: [0]
        };
    },

    // ----------------------------------------------------------------
    // Smooth shading build
    // Welds vertices that share the same position within tolerance
    // Averages normals across shared vertices
    // Produces smooth organic look — good for rounded anatomy models
    // ----------------------------------------------------------------
    buildSmooth: function (triangles) {
        const TOLERANCE = 0.0001;

        // Pass 1 — collect all positions and weld
        const posMap = new Map(); // key -> welded index
        const positions = [];     // welded positions
        const normalAccum = [];   // accumulated normals per welded vertex

        const getKey = (x, y, z) => {
            const qx = Math.round(x / TOLERANCE);
            const qy = Math.round(y / TOLERANCE);
            const qz = Math.round(z / TOLERANCE);
            return `${qx},${qy},${qz}`;
        };

        // First pass — register all unique positions
        for (const tri of triangles) {
            for (const v of [tri.v0, tri.v1, tri.v2]) {
                const key = getKey(v[0], v[1], v[2]);
                if (!posMap.has(key)) {
                    posMap.set(key, positions.length / 3);
                    positions.push(v[0], v[1], v[2]);
                    normalAccum.push(0, 0, 0);
                }
            }
        }

        // Second pass — accumulate face normals at each welded vertex
        for (const tri of triangles) {
            let nx = tri.normal[0];
            let ny = tri.normal[1];
            let nz = tri.normal[2];

            const nLen = Math.sqrt(nx * nx + ny * ny + nz * nz);
            if (nLen < 0.0001) {
                const e1x = tri.v1[0] - tri.v0[0];
                const e1y = tri.v1[1] - tri.v0[1];
                const e1z = tri.v1[2] - tri.v0[2];
                const e2x = tri.v2[0] - tri.v0[0];
                const e2y = tri.v2[1] - tri.v0[1];
                const e2z = tri.v2[2] - tri.v0[2];
                nx = e1y * e2z - e1z * e2y;
                ny = e1z * e2x - e1x * e2z;
                nz = e1x * e2y - e1y * e2x;
                const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
                if (len > 0) { nx /= len; ny /= len; nz /= len; }
            } else {
                nx /= nLen; ny /= nLen; nz /= nLen;
            }

            for (const v of [tri.v0, tri.v1, tri.v2]) {
                const key = getKey(v[0], v[1], v[2]);
                const wi = posMap.get(key);
                normalAccum[wi * 3] += nx;
                normalAccum[wi * 3 + 1] += ny;
                normalAccum[wi * 3 + 2] += nz;
            }
        }

        // Normalize accumulated normals
        const smoothNormals = [];
        for (let i = 0; i < normalAccum.length; i += 3) {
            let nx = normalAccum[i];
            let ny = normalAccum[i + 1];
            let nz = normalAccum[i + 2];
            const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
            if (len > 0) { nx /= len; ny /= len; nz /= len; }
            smoothNormals.push(nx, ny, nz);
        }

        // Third pass — build final flat arrays using welded indices
        const verts = [];
        const norms = [];
        const uvs = [];

        for (const tri of triangles) {
            for (const v of [tri.v0, tri.v1, tri.v2]) {
                const key = getKey(v[0], v[1], v[2]);
                const wi = posMap.get(key);
                verts.push(v[0], v[1], v[2]);
                norms.push(
                    smoothNormals[wi * 3],
                    smoothNormals[wi * 3 + 1],
                    smoothNormals[wi * 3 + 2]
                );
                uvs.push(...this.triplanarUV(v,
                    smoothNormals[wi * 3],
                    smoothNormals[wi * 3 + 1],
                    smoothNormals[wi * 3 + 2]));
            }
        }

        return {
            vertices: new Float32Array(verts),
            normals: new Float32Array(norms),
            uvs: new Float32Array(uvs),
            matBreaks: [verts.length / 3],
            matIndices: [0]
        };
    },

    // ----------------------------------------------------------------
    // Triplanar UV projection
    // Projects UV based on dominant normal axis
    // X dominant -> use YZ plane
    // Y dominant -> use XZ plane
    // Z dominant -> use XY plane
    // Scale 0.5 matches tilemap triplanar scale — tune per model if needed
    // ----------------------------------------------------------------
    triplanarUV: function (pos, nx, ny, nz) {
        const ax = Math.abs(nx);
        const ay = Math.abs(ny);
        const az = Math.abs(nz);
        const scale = 0.5;

        if (ax >= ay && ax >= az)
            return [pos[1] * scale, pos[2] * scale]; // YZ plane
        else if (ay >= ax && ay >= az)
            return [pos[0] * scale, pos[2] * scale]; // XZ plane
        else
            return [pos[0] * scale, pos[1] * scale]; // XY plane
    },

    // ----------------------------------------------------------------
    // Batch load — mirrors SpectralFBXHelper.loadAllAndUploadJson
    // meshList format: [{ url, name, smooth }]
    // ----------------------------------------------------------------
    loadAllAndUploadJson: async function (meshListJson) {
        const meshList = JSON.parse(meshListJson);
        return await this.loadAllAndUpload(meshList);
    },

    loadAllAndUpload: async function (meshList) {
        console.log('[STLHelper] loadAllAndUpload —', meshList.length, 'meshes');

        await new Promise(resolve => {
            const check = () => {
                if (window.SpectralGLInterop &&
                    document.getElementById('SpectralX-Viewport')) {
                    resolve();
                } else {
                    setTimeout(check, 50);
                }
            };
            check();
        });

        const results = await Promise.all(
            meshList.map(m => this.loadAndUpload(m.url, m.name, { smooth: m.smooth ?? false }))
        );

        const succeeded = results.filter(r => r === true).length;
        const failed = results.filter(r => r === false).length;

        console.log('[STLHelper] Complete —', succeeded, 'succeeded,', failed, 'failed');
        return results;
    }
};


// GLTF 2.0/ GLB

// ============================================================
// SpectralGLTFHelper.js
// GLTF 2.0 / GLB Parser — SpectraX WebGL2 Engine
// Mirrors SpectralFBXHelper pattern exactly:
//   loadAndUpload -> processMesh -> uploadParsedMesh
// Supports: Static meshes, multi-material, normals, UVs
// Animation data parsed and stored for Part 2
// ============================================================

window.SpectralGLTFHelper = {

    // ============================================================
    // Public API
    // ============================================================

    // Single mesh load + upload — mirrors SpectralFBXHelper.loadAndUpload
    loadAndUpload: async function (url, meshName) {
        try {
            console.log('[GLTFHelper] Loading:', url, 'mesh:', meshName);

            const response = await fetch(url);
            if (!response.ok) {
                console.warn('[GLTFHelper] Fetch failed:', url, response.status);
                return false;
            }

            const buffer = await response.arrayBuffer();
            if (!buffer || buffer.byteLength === 0) {
                console.warn('[GLTFHelper] Empty buffer:', url);
                return false;
            }

            // Detect GLB (binary) vs GLTF (JSON)
            const isGLB = this.detectGLB(buffer);
            console.log('[GLTFHelper] Format:', isGLB ? 'GLB (binary)' : 'GLTF (JSON)');

            let gltf = null;
            let binBuffer = null;

            if (isGLB) {
                const parsed = this.parseGLB(buffer);
                if (!parsed) {
                    console.warn('[GLTFHelper] GLB parse failed:', meshName);
                    return false;
                }
                gltf = parsed.json;
                binBuffer = parsed.bin;
            } else {
                // Plain GLTF JSON — bin loaded separately if needed
                const text = new TextDecoder().decode(buffer);
                gltf = JSON.parse(text);
                binBuffer = await this.loadExternalBin(url, gltf);
            }

            if (!gltf) {
                console.warn('[GLTFHelper] No JSON data:', meshName);
                return false;
            }

            // Extract all mesh primitives
            const meshData = this.extractMesh(gltf, binBuffer);
            if (!meshData || meshData.vertices.length === 0) {
                console.warn('[GLTFHelper] No geometry extracted:', meshName);
                return false;
            }

            // Extract textures (base64 data URLs from embedded images)
            const textures = this.extractTextures(gltf, binBuffer);

            // Extract material colors
            const materialColors = this.extractMaterialColors(gltf);

            // Upload to WebGL via shared interop
            window.SpectralGLInterop.uploadParsedMesh(
                meshName, meshData, textures, materialColors
            );

            console.log('[GLTFHelper] Upload success:', meshName,
                'verts:', meshData.vertices.length / 3,
                'matBreaks:', meshData.matBreaks,
                'textures:', textures.length);

            return true;

        } catch (ex) {
            console.error('[GLTFHelper] loadAndUpload failed:', meshName, ex);
            return false;
        }
    },

    // Batch load — mirrors SpectralFBXHelper.loadAllAndUploadJson
    // meshList format: [{ url, name }]
    loadAllAndUploadJson: async function (meshListJson) {
        const meshList = JSON.parse(meshListJson);
        return await this.loadAllAndUpload(meshList);
    },

    loadAllAndUpload: async function (meshList) {
        console.log('[GLTFHelper] loadAllAndUpload —', meshList.length, 'meshes');

        // Wait for GL + canvas ready — same pattern as FBX helper
        await new Promise(resolve => {
            const check = () => {
                if (window.SpectralGLInterop &&
                    document.getElementById('SpectralX-Viewport')) {
                    resolve();
                } else {
                    setTimeout(check, 50);
                }
            };
            check();
        });

        const results = await Promise.all(
            meshList.map(m => this.loadAndUpload(m.url, m.name))
        );

        const succeeded = results.filter(r => r === true).length;
        const failed = results.filter(r => r === false).length;

        console.log('[GLTFHelper] Complete —', succeeded, 'succeeded,', failed, 'failed');
        return results;
    },


    // ============================================================
    // GLB Detection + Parsing
    // ============================================================

    // GLB magic number: 0x46546C67 = "glTF" in little-endian
    detectGLB: function (buffer) {
        if (buffer.byteLength < 12) return false;
        const view = new DataView(buffer);
        const magic = view.getUint32(0, true);
        return magic === 0x46546C67;
    },

    // GLB binary layout:
    //   12 byte header: magic(4) + version(4) + length(4)
    //   Chunk 0: length(4) + type(4) + JSON data
    //   Chunk 1: length(4) + type(4) + BIN data (optional)
    parseGLB: function (buffer) {
        const view = new DataView(buffer);

        const magic = view.getUint32(0, true);
        if (magic !== 0x46546C67) {
            console.error('[GLTFHelper] Not a valid GLB file');
            return null;
        }

        const version = view.getUint32(4, true);
        const totalLength = view.getUint32(8, true);
        console.log('[GLTFHelper] GLB version:', version, 'length:', totalLength);

        let offset = 12;
        let json = null;
        let bin = null;

        // Parse chunks
        while (offset < totalLength) {
            const chunkLength = view.getUint32(offset, true);
            const chunkType = view.getUint32(offset + 4, true);
            offset += 8;

            // Chunk type 0x4E4F534A = "JSON"
            if (chunkType === 0x4E4F534A) {
                const jsonBytes = new Uint8Array(buffer, offset, chunkLength);
                const jsonText = new TextDecoder().decode(jsonBytes);
                json = JSON.parse(jsonText);
                console.log('[GLTFHelper] JSON chunk parsed,',
                    'meshes:', json.meshes?.length ?? 0,
                    'materials:', json.materials?.length ?? 0,
                    'animations:', json.animations?.length ?? 0);
            }
            // Chunk type 0x004E4942 = "BIN\0"
            else if (chunkType === 0x004E4942) {
                bin = buffer.slice(offset, offset + chunkLength);
                console.log('[GLTFHelper] BIN chunk:', chunkLength, 'bytes');
            }

            offset += chunkLength;
        }

        if (!json) {
            console.error('[GLTFHelper] No JSON chunk found in GLB');
            return null;
        }

        return { json, bin };
    },

    // Load external .bin file for plain GLTF (non-GLB)
    loadExternalBin: async function (gltfUrl, gltf) {
        if (!gltf.buffers || gltf.buffers.length === 0) return null;

        const buf = gltf.buffers[0];

        // Embedded base64 data URI
        if (buf.uri && buf.uri.startsWith('data:')) {
            const base64 = buf.uri.split(',')[1];
            const binary = atob(base64);
            const bytes = new Uint8Array(binary.length);
            for (let i = 0; i < binary.length; i++)
                bytes[i] = binary.charCodeAt(i);
            return bytes.buffer;
        }

        // External .bin file — relative to GLTF path
        if (buf.uri) {
            const base = gltfUrl.substring(0, gltfUrl.lastIndexOf('/') + 1);
            const binUrl = base + buf.uri;
            try {
                const res = await fetch(binUrl);
                return await res.arrayBuffer();
            } catch (ex) {
                console.warn('[GLTFHelper] Failed to load external bin:', binUrl, ex);
                return null;
            }
        }

        return null;
    },


    // ============================================================
    // Accessor Helpers
    // ============================================================

    // GLTF component types
    COMPONENT_TYPE: {
        5120: { size: 1, ArrayType: Int8Array },
        5121: { size: 1, ArrayType: Uint8Array },
        5122: { size: 2, ArrayType: Int16Array },
        5123: { size: 2, ArrayType: Uint16Array },
        5125: { size: 4, ArrayType: Uint32Array },
        5126: { size: 4, ArrayType: Float32Array },
    },

    // GLTF element counts per type string
    TYPE_COUNT: {
        'SCALAR': 1, 'VEC2': 2, 'VEC3': 3,
        'VEC4': 4, 'MAT2': 4, 'MAT3': 9, 'MAT4': 16
    },

    // Read a GLTF accessor into a flat JS array
    readAccessor: function (gltf, binBuffer, accessorIndex) {
        if (accessorIndex === undefined || accessorIndex === null) return null;
        if (!gltf.accessors || accessorIndex >= gltf.accessors.length) return null;

        const accessor = gltf.accessors[accessorIndex];
        const bufferView = gltf.bufferViews[accessor.bufferView];
        const componentInfo = this.COMPONENT_TYPE[accessor.componentType];
        const typeCount = this.TYPE_COUNT[accessor.type];

        if (!componentInfo || !typeCount) {
            console.warn('[GLTFHelper] Unknown accessor type:', accessor.componentType, accessor.type);
            return null;
        }

        // Byte offsets
        const bufferOffset = (bufferView.byteOffset || 0) + (accessor.byteOffset || 0);
        const byteStride = bufferView.byteStride || (componentInfo.size * typeCount);
        const count = accessor.count;

        const result = [];
        const view = new DataView(binBuffer);

        for (let i = 0; i < count; i++) {
            const elemOffset = bufferOffset + i * byteStride;
            for (let j = 0; j < typeCount; j++) {
                const byteOff = elemOffset + j * componentInfo.size;
                let val;

                switch (accessor.componentType) {
                    case 5120: val = view.getInt8(byteOff); break;
                    case 5121: val = view.getUint8(byteOff); break;
                    case 5122: val = view.getInt16(byteOff, true); break;
                    case 5123: val = view.getUint16(byteOff, true); break;
                    case 5125: val = view.getUint32(byteOff, true); break;
                    case 5126: val = view.getFloat32(byteOff, true); break;
                    default: val = 0;
                }

                result.push(val);
            }
        }

        return result;
    },


    // ============================================================
    // Mesh Extraction
    // ============================================================

    // Extract all mesh primitives, flatten into a single uploadable buffer
    // with matBreaks + matIndices for multi-material support
    // Matches the exact format SpectralGLInterop.uploadParsedMesh expects
    extractMesh: function (gltf, binBuffer) {
        if (!gltf.meshes || gltf.meshes.length === 0) {
            console.warn('[GLTFHelper] No meshes in GLTF');
            return null;
        }

        const outVerts = [];
        const outNorms = [];
        const outUVs = [];
        const matBreaks = [];
        const matIndices = [];

        // Process all mesh primitives across all meshes
        // Each primitive = one material slot
        let primitiveIndex = 0;

        for (const mesh of gltf.meshes) {
            if (!mesh.primitives) continue;

            for (const prim of mesh.primitives) {
                const vertsBefore = outVerts.length / 3;

                // --- Positions (required) ---
                const positions = this.readAccessor(gltf, binBuffer,
                    prim.attributes['POSITION']);
                if (!positions || positions.length === 0) {
                    console.warn('[GLTFHelper] Primitive has no POSITION:', primitiveIndex);
                    primitiveIndex++;
                    continue;
                }

                // --- Normals (optional — compute if missing) ---
                let normals = null;
                if (prim.attributes['NORMAL'] !== undefined) {
                    normals = this.readAccessor(gltf, binBuffer,
                        prim.attributes['NORMAL']);
                }

                // --- UVs (optional) ---
                let uvs = null;
                if (prim.attributes['TEXCOORD_0'] !== undefined) {
                    uvs = this.readAccessor(gltf, binBuffer,
                        prim.attributes['TEXCOORD_0']);
                }

                // --- Indices (optional — if absent use sequential) ---
                let indices = null;
                if (prim.indices !== undefined) {
                    indices = this.readAccessor(gltf, binBuffer, prim.indices);
                }

                // --- Build flat vertex arrays from indices ---
                const vertCount = positions.length / 3;
                const indexList = indices
                    ? indices
                    : Array.from({ length: vertCount }, (_, i) => i);

                for (const idx of indexList) {
                    // Position
                    outVerts.push(
                        positions[idx * 3],
                        positions[idx * 3 + 1],
                        positions[idx * 3 + 2]
                    );

                    // Normal — use stored or compute face normal below
                    if (normals) {
                        outNorms.push(
                            normals[idx * 3],
                            normals[idx * 3 + 1],
                            normals[idx * 3 + 2]
                        );
                    } else {
                        // Placeholder — will be replaced by face normal pass below
                        outNorms.push(0, 0, 1);
                    }

                    // UV — flip V axis (GLTF is bottom-origin, WebGL top-origin)
                    if (uvs) {
                        outUVs.push(
                            uvs[idx * 2],
                            1.0 - uvs[idx * 2 + 1]
                        );
                    } else {
                        outUVs.push(0, 0);
                    }
                }

                // If normals were missing, compute per-face normals
                // This fixes the smooth shading bug for meshes without normal data
                if (!normals) {
                    const start = vertsBefore * 3;
                    const end = outVerts.length;
                    for (let i = start; i < end; i += 9) {
                        const ax = outVerts[i], ay = outVerts[i + 1], az = outVerts[i + 2];
                        const bx = outVerts[i + 3], by = outVerts[i + 4], bz = outVerts[i + 5];
                        const cx = outVerts[i + 6], cy = outVerts[i + 7], cz = outVerts[i + 8];

                        const e1x = bx - ax, e1y = by - ay, e1z = bz - az;
                        const e2x = cx - ax, e2y = cy - ay, e2z = cz - az;

                        let nx = e1y * e2z - e1z * e2y;
                        let ny = e1z * e2x - e1x * e2z;
                        let nz = e1x * e2y - e1y * e2x;
                        const len = Math.sqrt(nx * nx + ny * ny + nz * nz);
                        if (len > 0) { nx /= len; ny /= len; nz /= len; }

                        const normStart = vertsBefore * 3 + (i - start);
                        outNorms[normStart] = nx;
                        outNorms[normStart + 1] = ny;
                        outNorms[normStart + 2] = nz;
                        outNorms[normStart + 3] = nx;
                        outNorms[normStart + 4] = ny;
                        outNorms[normStart + 5] = nz;
                        outNorms[normStart + 6] = nx;
                        outNorms[normStart + 7] = ny;
                        outNorms[normStart + 8] = nz;
                    }
                }

                const vertsAdded = outVerts.length / 3 - vertsBefore;
                if (vertsAdded > 0) {
                    matBreaks.push(vertsAdded);
                    matIndices.push(prim.material ?? primitiveIndex);
                }

                primitiveIndex++;
            }
        }

        if (outVerts.length === 0) return null;

        console.log('[GLTFHelper] Extracted',
            outVerts.length / 3, 'verts,',
            matBreaks.length, 'material segments');

        return {
            vertices: new Float32Array(outVerts),
            normals: new Float32Array(outNorms),
            uvs: new Float32Array(outUVs),
            matBreaks,
            matIndices
        };
    },


    // ============================================================
    // Texture Extraction
    // ============================================================

    // Extract embedded textures as base64 data URLs
    // Returns array indexed by material slot — matches FBX helper output
    extractTextures: function (gltf, binBuffer) {
        if (!gltf.materials || gltf.materials.length === 0) return [];

        const textures = [];

        for (const mat of gltf.materials) {
            // PBR base color texture is the primary diffuse
            const pbr = mat.pbrMetallicRoughness;
            if (!pbr || pbr.baseColorTexture === undefined) {
                textures.push(null);
                continue;
            }

            const texIndex = pbr.baseColorTexture.index;
            const dataUrl = this.getTextureDataUrl(gltf, binBuffer, texIndex);
            textures.push(dataUrl);
        }

        console.log('[GLTFHelper] Textures extracted:',
            textures.filter(t => t).length, '/', textures.length);

        return textures;
    },

    // Convert a GLTF texture index to a base64 data URL
    getTextureDataUrl: function (gltf, binBuffer, textureIndex) {
        if (!gltf.textures || textureIndex >= gltf.textures.length) return null;

        const texture = gltf.textures[textureIndex];
        const imageIndex = texture.source;

        if (!gltf.images || imageIndex >= gltf.images.length) return null;

        const image = gltf.images[imageIndex];

        // Embedded base64 data URI
        if (image.uri && image.uri.startsWith('data:')) {
            return image.uri;
        }

        // BufferView-referenced image (GLB embedded)
        if (image.bufferView !== undefined) {
            const bv = gltf.bufferViews[image.bufferView];
            const mimeType = image.mimeType || 'image/png';
            const bytes = new Uint8Array(binBuffer,
                bv.byteOffset || 0, bv.byteLength);

            // Convert to base64 in chunks to avoid stack overflow
            let binary = '';
            const chunkSize = 8192;
            for (let i = 0; i < bytes.length; i += chunkSize) {
                binary += String.fromCharCode(
                    ...bytes.subarray(i, i + chunkSize));
            }
            return `data:${mimeType};base64,${btoa(binary)}`;
        }

        return null;
    },


    // ============================================================
    // Material Color Extraction
    // ============================================================

    // Extract PBR base color factors as "r,g,b,a" strings
    // Matches FBX helper materialColors format exactly
    extractMaterialColors: function (gltf) {
        if (!gltf.materials || gltf.materials.length === 0) return [];

        return gltf.materials.map(mat => {
            const pbr = mat.pbrMetallicRoughness;
            if (!pbr || !pbr.baseColorFactor) return '1,1,1,1';

            const [r, g, b, a] = pbr.baseColorFactor;
            return `${r},${g},${b},${a ?? 1.0}`;
        });
    },


    // ============================================================
    // Animation Data (Parsed for Part 2 — stored, not yet applied)
    // ============================================================

    // Parse animation data from GLTF — returns structured animation object
    // Ready to be consumed by the animation system in Part 2
    parseAnimations: function (gltf, binBuffer) {
        if (!gltf.animations || gltf.animations.length === 0) return null;

        const animations = [];

        for (const anim of gltf.animations) {
            const channels = [];

            for (const channel of anim.channels) {
                const sampler = anim.samplers[channel.sampler];
                const targetNode = channel.target.node;
                const targetPath = channel.target.path; // translation/rotation/scale

                // Input = keyframe timestamps
                const times = this.readAccessor(gltf, binBuffer, sampler.input);
                // Output = values at each keyframe (vec3 for T/S, quat for R)
                const values = this.readAccessor(gltf, binBuffer, sampler.output);

                if (!times || !values) continue;

                channels.push({
                    node: targetNode,
                    path: targetPath,
                    interpolation: sampler.interpolation || 'LINEAR',
                    times,
                    values
                });
            }

            animations.push({
                name: anim.name || `Anim_${animations.length}`,
                duration: Math.max(...animations.flatMap(a =>
                    a.channels?.flatMap(c => c.times) ?? [0]), 0),
                channels
            });

            console.log('[GLTFHelper] Animation parsed:',
                anim.name, 'channels:', channels.length);
        }

        return animations.length > 0 ? animations : null;
    },

};