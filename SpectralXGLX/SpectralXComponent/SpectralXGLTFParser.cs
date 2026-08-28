using System.Text;
using System.Text.Json;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Pure C# GLTF 2.0 / GLB Parser - Works in Blazor WASM
    /// Fallback path when JS helper is unavailable.
    /// Mirrors SpectralXFBXParser structure exactly.
    /// Supports: Static meshes, multi-material, normals, UVs, embedded textures
    /// </summary>
    public class SpectralXGLTFParser
    {
        // GLB magic number: "glTF" in little-endian
        private const uint GLB_MAGIC = 0x46546C67;

        // GLB chunk types
        private const uint CHUNK_JSON = 0x4E4F534A;
        private const uint CHUNK_BIN = 0x004E4942;

        // GLTF component type sizes in bytes
        private static readonly Dictionary<int, int> ComponentSizes = new()
        {
            { 5120, 1 }, // BYTE
            { 5121, 1 }, // UNSIGNED_BYTE
            { 5122, 2 }, // SHORT
            { 5123, 2 }, // UNSIGNED_SHORT
            { 5125, 4 }, // UNSIGNED_INT
            { 5126, 4 }, // FLOAT
        };

        // GLTF type element counts
        private static readonly Dictionary<string, int> TypeCounts = new()
        {
            { "SCALAR", 1 }, { "VEC2", 2 }, { "VEC3", 3 }, { "VEC4", 4 },
            { "MAT2", 4 },   { "MAT3", 9 }, { "MAT4", 16 }
        };

        public static SpectralXMesh Parse(byte[] data, string meshName = "GLTFMesh")
        {
            try
            {
                Console.WriteLine($"[GLTFParser] Starting parse of {data.Length} bytes");

                bool isGLB = IsGLB(data);
                Console.WriteLine($"[GLTFParser] Format: {(isGLB ? "GLB (binary)" : "GLTF (JSON)")}");

                JsonElement gltf;
                byte[]? binBuffer = null;

                if (isGLB)
                {
                    var parsed = ParseGLB(data);
                    if (parsed == null)
                    {
                        Console.WriteLine("[GLTFParser] GLB parse failed");
                        return new SpectralXMesh(meshName);
                    }
                    gltf = parsed.Value.Json;
                    binBuffer = parsed.Value.Bin;
                }
                else
                {
                    // Plain GLTF JSON — no external bin support in C# fallback
                    var text = Encoding.UTF8.GetString(data);
                    gltf = JsonDocument.Parse(text).RootElement;
                    binBuffer = null;
                }

                var mesh = new SpectralXMesh(meshName);
                ExtractMeshData(gltf, binBuffer, mesh);
                return mesh;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GLTFParser] Parse failed: {ex.Message}");
                Console.WriteLine($"[GLTFParser] Stack: {ex.StackTrace}");
                return new SpectralXMesh(meshName);
            }
        }


        // ============================================================
        // GLB Detection + Parsing
        // ============================================================

        private static bool IsGLB(byte[] data)
        {
            if (data.Length < 12) return false;
            uint magic = BitConverter.ToUInt32(data, 0);
            return magic == GLB_MAGIC;
        }

        private static (JsonElement Json, byte[]? Bin)? ParseGLB(byte[] data)
        {
            // GLB header: magic(4) + version(4) + length(4)
            uint magic = BitConverter.ToUInt32(data, 0);
            uint version = BitConverter.ToUInt32(data, 4);
            uint total = BitConverter.ToUInt32(data, 8);

            Console.WriteLine($"[GLTFParser] GLB version:{version} length:{total}");

            if (magic != GLB_MAGIC)
            {
                Console.WriteLine("[GLTFParser] Invalid GLB magic");
                return null;
            }

            int offset = 12;
            JsonElement? json = null;
            byte[]? bin = null;

            while (offset < data.Length - 8)
            {
                uint chunkLength = BitConverter.ToUInt32(data, offset);
                uint chunkType = BitConverter.ToUInt32(data, offset + 4);
                offset += 8;

                if (offset + chunkLength > data.Length) break;

                if (chunkType == CHUNK_JSON)
                {
                    var jsonText = Encoding.UTF8.GetString(data, offset, (int)chunkLength);
                    json = JsonDocument.Parse(jsonText).RootElement;
                    Console.WriteLine("[GLTFParser] JSON chunk parsed");
                }
                else if (chunkType == CHUNK_BIN)
                {
                    bin = new byte[chunkLength];
                    Array.Copy(data, offset, bin, 0, (int)chunkLength);
                    Console.WriteLine($"[GLTFParser] BIN chunk: {chunkLength} bytes");
                }

                offset += (int)chunkLength;
            }

            if (json == null)
            {
                Console.WriteLine("[GLTFParser] No JSON chunk found");
                return null;
            }

            return (json.Value, bin);
        }


        // ============================================================
        // Accessor Reading
        // ============================================================

        private static float[] ReadFloatAccessor(JsonElement gltf, byte[]? bin,
            int accessorIndex)
        {
            if (bin == null) return Array.Empty<float>();
            if (!gltf.TryGetProperty("accessors", out var accessors)) return Array.Empty<float>();
            if (accessorIndex >= accessors.GetArrayLength()) return Array.Empty<float>();

            var accessor = accessors[accessorIndex];
            var bufferView = GetBufferView(gltf, accessor.GetProperty("bufferView").GetInt32());

            int componentType = accessor.GetProperty("componentType").GetInt32();
            string typeStr = accessor.GetProperty("type").GetString() ?? "SCALAR";
            int count = accessor.GetProperty("count").GetInt32();
            int typeCount = TypeCounts.GetValueOrDefault(typeStr, 1);

            int bufOffset = (bufferView.ByteOffset) +
                             (accessor.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0);
            int byteStride = bufferView.ByteStride > 0
                ? bufferView.ByteStride
                : ComponentSizes.GetValueOrDefault(componentType, 4) * typeCount;

            var result = new float[count * typeCount];
            int idx = 0;

            for (int i = 0; i < count; i++)
            {
                int elemOffset = bufOffset + i * byteStride;
                for (int j = 0; j < typeCount; j++)
                {
                    int bytePos = elemOffset + j * ComponentSizes.GetValueOrDefault(componentType, 4);
                    result[idx++] = componentType switch
                    {
                        5126 => BitConverter.ToSingle(bin, bytePos),
                        5123 => BitConverter.ToUInt16(bin, bytePos),
                        5122 => BitConverter.ToInt16(bin, bytePos),
                        5121 => bin[bytePos],
                        5120 => (sbyte)bin[bytePos],
                        _ => BitConverter.ToSingle(bin, bytePos)
                    };
                }
            }

            return result;
        }

        private static int[] ReadIndexAccessor(JsonElement gltf, byte[]? bin,
            int accessorIndex)
        {
            if (bin == null) return Array.Empty<int>();
            if (!gltf.TryGetProperty("accessors", out var accessors)) return Array.Empty<int>();
            if (accessorIndex >= accessors.GetArrayLength()) return Array.Empty<int>();

            var accessor = accessors[accessorIndex];
            var bufferView = GetBufferView(gltf, accessor.GetProperty("bufferView").GetInt32());
            int componentType = accessor.GetProperty("componentType").GetInt32();
            int count = accessor.GetProperty("count").GetInt32();

            int bufOffset = bufferView.ByteOffset +
                            (accessor.TryGetProperty("byteOffset", out var ao) ? ao.GetInt32() : 0);
            int compSize = ComponentSizes.GetValueOrDefault(componentType, 2);

            var result = new int[count];
            for (int i = 0; i < count; i++)
            {
                int bytePos = bufOffset + i * compSize;
                result[i] = componentType switch
                {
                    5125 => (int)BitConverter.ToUInt32(bin, bytePos),
                    5123 => BitConverter.ToUInt16(bin, bytePos),
                    5122 => BitConverter.ToInt16(bin, bytePos),
                    5121 => bin[bytePos],
                    _ => BitConverter.ToUInt16(bin, bytePos)
                };
            }

            return result;
        }

        private static (int ByteOffset, int ByteLength, int ByteStride)
            GetBufferView(JsonElement gltf, int index)
        {
            if (!gltf.TryGetProperty("bufferViews", out var views))
                return (0, 0, 0);
            if (index >= views.GetArrayLength())
                return (0, 0, 0);

            var bv = views[index];
            int offset = bv.TryGetProperty("byteOffset", out var bo) ? bo.GetInt32() : 0;
            int length = bv.TryGetProperty("byteLength", out var bl) ? bl.GetInt32() : 0;
            int stride = bv.TryGetProperty("byteStride", out var bs) ? bs.GetInt32() : 0;

            return (offset, length, stride);
        }


        // ============================================================
        // Mesh Data Extraction
        // ============================================================

        private static void ExtractMeshData(JsonElement gltf, byte[]? bin,
            SpectralXMesh mesh)
        {
            if (!gltf.TryGetProperty("meshes", out var meshes) ||
                meshes.GetArrayLength() == 0)
            {
                Console.WriteLine("[GLTFParser] No meshes found");
                return;
            }

            int primitiveIndex = 0;
            int normalBase = 0;

            foreach (var gltfMesh in meshes.EnumerateArray())
            {
                if (!gltfMesh.TryGetProperty("primitives", out var primitives))
                    continue;

                foreach (var prim in primitives.EnumerateArray())
                {
                    // --- Attributes ---
                    if (!prim.TryGetProperty("attributes", out var attrs))
                    {
                        primitiveIndex++;
                        continue;
                    }

                    // Positions (required)
                    if (!attrs.TryGetProperty("POSITION", out var posAccessorEl))
                    {
                        primitiveIndex++;
                        continue;
                    }
                    var positions = ReadFloatAccessor(gltf, bin, posAccessorEl.GetInt32());
                    if (positions.Length == 0)
                    {
                        primitiveIndex++;
                        continue;
                    }

                    // Normals (optional)
                    float[]? normals = null;
                    if (attrs.TryGetProperty("NORMAL", out var normAccessorEl))
                        normals = ReadFloatAccessor(gltf, bin, normAccessorEl.GetInt32());

                    // UVs (optional)
                    float[]? uvs = null;
                    if (attrs.TryGetProperty("TEXCOORD_0", out var uvAccessorEl))
                        uvs = ReadFloatAccessor(gltf, bin, uvAccessorEl.GetInt32());

                    // Indices (optional)
                    int[]? indices = null;
                    if (prim.TryGetProperty("indices", out var indicesEl))
                        indices = ReadIndexAccessor(gltf, bin, indicesEl.GetInt32());

                    // Material index
                    int matIdx = prim.TryGetProperty("material", out var matEl)
                        ? matEl.GetInt32() : 0;

                    // --- Add vertices to mesh ---
                    int vertexBase = mesh.Vertices.Count;
                    int vertexCount = positions.Length / 3;

                    for (int i = 0; i < vertexCount; i++)
                    {
                        mesh.Vertices.Add(new Vector3(
                            positions[i * 3],
                            positions[i * 3 + 1],
                            positions[i * 3 + 2]));

                        if (uvs != null && i * 2 + 1 < uvs.Length)
                            mesh.UVs.Add(new Vector2(
                                uvs[i * 2],
                                1.0f - uvs[i * 2 + 1])); // flip V
                        else
                            mesh.UVs.Add(Vector2.Zero);
                    }

                    // --- Build faces from indices ---
                    var indexList = indices ?? Enumerable.Range(0, vertexCount).ToArray();

                    for (int i = 0; i < indexList.Length - 2; i += 3)
                    {
                        int a = vertexBase + indexList[i];
                        int b = vertexBase + indexList[i + 1];
                        int c = vertexBase + indexList[i + 2];

                        int uvA = a;
                        int uvB = b;
                        int uvC = c;

                        mesh.AddTriangleFace(a, b, c, uvA, uvB, uvC);

                        // Store polygon normals
                        if (normals != null)
                        {
                            void AddNorm(int idx)
                            {
                                if (idx * 3 + 2 < normals.Length)
                                    mesh.PolygonNormals.Add(new Vector3(
                                        normals[idx * 3],
                                        normals[idx * 3 + 1],
                                        normals[idx * 3 + 2]));
                                else
                                    mesh.PolygonNormals.Add(Vector3.UnitZ);
                            }
                            AddNorm(indexList[i]);
                            AddNorm(indexList[i + 1]);
                            AddNorm(indexList[i + 2]);
                        }
                        else
                        {
                            // Compute face normal
                            var v0 = mesh.Vertices[a];
                            var v1 = mesh.Vertices[b];
                            var v2 = mesh.Vertices[c];
                            var fn = Vector3.Normalize(
                                Vector3.Cross(v1 - v0, v2 - v0));
                            mesh.PolygonNormals.Add(fn);
                            mesh.PolygonNormals.Add(fn);
                            mesh.PolygonNormals.Add(fn);
                        }

                        // Set face material + normal base
                        var face = mesh.Faces[mesh.Faces.Count - 1];
                        face.MaterialIndex = matIdx;
                        face.PolygonNormalBase = normalBase;
                        mesh.Faces[mesh.Faces.Count - 1] = face;

                        normalBase += 3;
                    }

                    Console.WriteLine($"[GLTFParser] Primitive {primitiveIndex}: " +
                        $"verts:{vertexCount} " +
                        $"faces:{indexList.Length / 3} " +
                        $"mat:{matIdx}");

                    primitiveIndex++;
                }
            }

            // Extract embedded textures + material colors
            ExtractTextureData(gltf, bin, mesh);

            Console.WriteLine($"[GLTFParser] Done — " +
                $"verts:{mesh.Vertices.Count} " +
                $"faces:{mesh.Faces.Count} " +
                $"normals:{mesh.PolygonNormals.Count}");
        }


        // ============================================================
        // Texture + Material Extraction
        // ============================================================

        private static void ExtractTextureData(JsonElement gltf, byte[]? bin,
            SpectralXMesh mesh)
        {
            if (!gltf.TryGetProperty("materials", out var materials)) return;

            var slotTextures = new List<string>();
            var slotColors = new List<Vector4>();

            foreach (var mat in materials.EnumerateArray())
            {
                // Base color factor
                var color = new Vector4(1f, 1f, 1f, 1f);
                if (mat.TryGetProperty("pbrMetallicRoughness", out var pbr))
                {
                    if (pbr.TryGetProperty("baseColorFactor", out var bcf))
                    {
                        var arr = bcf.EnumerateArray().Select(e => e.GetSingle()).ToArray();
                        if (arr.Length >= 3)
                            color = new Vector4(arr[0], arr[1], arr[2],
                                arr.Length > 3 ? arr[3] : 1f);
                    }

                    // Base color texture
                    if (pbr.TryGetProperty("baseColorTexture", out var bct) && bin != null)
                    {
                        int texIdx = bct.GetProperty("index").GetInt32();
                        var dataUrl = GetTextureDataUrl(gltf, bin, texIdx);
                        slotTextures.Add(dataUrl ?? string.Empty);
                    }
                    else
                    {
                        slotTextures.Add(string.Empty);
                    }
                }
                else
                {
                    slotTextures.Add(string.Empty);
                }

                slotColors.Add(color);
            }

            // Assign to mesh — mirrors FBX parser output
            mesh.MaterialTextures = slotTextures;
            mesh.MaterialColors = slotColors;

            // Set primary texture if any slot has one
            var firstTex = slotTextures.FirstOrDefault(t => !string.IsNullOrEmpty(t));
            if (firstTex != null)
            {
                mesh.TextureDataUrl = firstTex;
                mesh.TextureIsRawRGBA = false;
                mesh.TextureWidth = 0;
                mesh.TextureHeight = 0;
            }

            Console.WriteLine($"[GLTFParser] Materials: {slotTextures.Count} slots, " +
                $"textures: {slotTextures.Count(t => !string.IsNullOrEmpty(t))}");
        }

        private static string? GetTextureDataUrl(JsonElement gltf, byte[] bin,
            int textureIndex)
        {
            if (!gltf.TryGetProperty("textures", out var textures)) return null;
            if (textureIndex >= textures.GetArrayLength()) return null;

            var tex = textures[textureIndex];
            if (!tex.TryGetProperty("source", out var srcEl)) return null;
            int imgIndex = srcEl.GetInt32();

            if (!gltf.TryGetProperty("images", out var images)) return null;
            if (imgIndex >= images.GetArrayLength()) return null;

            var img = images[imgIndex];

            // Embedded base64 URI
            if (img.TryGetProperty("uri", out var uriEl))
            {
                var uri = uriEl.GetString() ?? string.Empty;
                if (uri.StartsWith("data:")) return uri;
            }

            // BufferView-referenced image (GLB embedded)
            if (img.TryGetProperty("bufferView", out var bvEl))
            {
                var bv = GetBufferView(gltf, bvEl.GetInt32());
                var mimeType = img.TryGetProperty("mimeType", out var mt)
                    ? mt.GetString() ?? "image/png" : "image/png";

                var bytes = new byte[bv.ByteLength];
                Array.Copy(bin, bv.ByteOffset, bytes, 0, bv.ByteLength);
                var base64 = Convert.ToBase64String(bytes);
                return $"data:{mimeType};base64,{base64}";
            }

            return null;
        }
    }
}