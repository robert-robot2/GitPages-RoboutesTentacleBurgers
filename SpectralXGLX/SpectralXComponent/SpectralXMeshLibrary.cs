

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Central repository for loading and storing mesh definitions.
    /// Meshes are world-space, static, and reusable.
    /// </summary>
    /// 
    public class FontAtlasInfo
    {
        public string FontKey { get; set; } = "";
        public string JsonUrl { get; set; } = "";
        public string TextureUrl { get; set; } = "";
    }
    public class SpectralXMeshLibrary
    {
        private readonly Dictionary<string, FontAtlasInfo> _fontAtlases = new();
        private readonly Dictionary<string, IMesh> meshCache = new();

        private readonly object _meshLock = new();
        public IReadOnlyDictionary<string, IMesh> Meshes => meshCache;
        public int MeshCount => meshCache.Count;
        private readonly HashSet<string> _jsMeshNames = new();

        public SpectralXMeshLibrary()
        {
            LoadBuiltInPrimitives();
        }

        public IMesh? GetMesh(string name)
        {
            meshCache.TryGetValue(name, out var mesh);
            return mesh?.Clone();
        }

        public bool HasMesh(string name) => meshCache.ContainsKey(name);
        public bool IsJSMesh(string name) => _jsMeshNames.Contains(name);
        public void AddMesh(string name, IMesh mesh)
        {
            mesh.Name = name;

            FinalizeMesh(mesh);

            lock (_meshLock)
            {
                meshCache[name] = mesh;
            }

            Console.WriteLine(
                $"[MeshLibrary] Added mesh: {mesh.Name} " +
                $"({mesh.Vertices.Count} verts, {mesh.Faces.Count} faces)"
            );
        }
        public bool RemoveMesh(string name)
        {
            return meshCache.Remove(name);
        }

        public void RegisterFont(string key, string jsonUrl, string texUrl)
        {
            _fontAtlases[key] = new FontAtlasInfo
            {
                FontKey = key,
                JsonUrl = jsonUrl,
                TextureUrl = texUrl
            };
            Console.WriteLine($"[MeshLibrary] Font registered: {key}");
        }

        public SpectralXMesh CreateTextMesh(string text, string fontKey,
            float fontSize, Vector3 position, TextAlignment align = TextAlignment.Left)
        {
            var mesh = new SpectralXMesh($"Text_{text}_{fontKey}");
            mesh.IsSDFText = true;
            mesh.Text = text;
            mesh.FontKey = fontKey;
            mesh.FontSize = fontSize;
            mesh.Position = position;
            mesh.TextAlign = align;
            mesh.TextDirty = true;
            return mesh;
        }

        public FontAtlasInfo? GetFontAtlas(string key)
        {
            _fontAtlases.TryGetValue(key, out var info);
            return info;
        }



        private void LoadBuiltInPrimitives()
        {

            AddMesh("PrimSquare", CreateSquare());
            AddMesh("PrimTriangle", CreateTriangle());

            AddMesh("PrimCube", CreateCube());
            AddMesh("PrimPyramid", CreatePyramid());
        




        }



        // 2D Prim Factory
        private IMesh CreateSquare()
        {
            var mesh = new SpectralXMesh("PrimSquare");
            mesh.Vertices.Add(new Vector3(-1f, 1f, 0f)); // 0 top-left
            mesh.Vertices.Add(new Vector3(1f, 1f, 0f)); // 1 top-right
            mesh.Vertices.Add(new Vector3(1f, -1f, 0f)); // 2 bottom-right
            mesh.Vertices.Add(new Vector3(-1f, -1f, 0f)); // 3 bottom-left

            mesh.UVs.Add(new Vector2(0f, 0f)); // 0 top-left
            mesh.UVs.Add(new Vector2(1f, 0f)); // 1 top-right
            mesh.UVs.Add(new Vector2(1f, 1f)); // 2 bottom-right
            mesh.UVs.Add(new Vector2(0f, 1f)); // 3 bottom-left
            mesh.AddQuadFace(0, 1, 2, 3, 0, 1, 2, 3);
            return mesh;
        }


        private IMesh CreateTriangle()
        {
            var mesh = new SpectralXMesh("PrimTriangle");

            // Add triangle face (adds vertices AND face in one call)
            mesh.AddTriangleFace(
                new Vector3(0f, 1f, 0f),  // top
                new Vector3(-1f, -1f, 0f),  // bottom-left
                new Vector3(1f, -1f, 0f)   // bottom-right
            );

            return mesh;
        }
    

        // CreateBezierSphere












        // 3D Prim Factory




        /// <summary>
        /// Unit cube centered at origin.
        /// Winding is CCW when viewed from outside.
        /// </summary>
        public static IMesh CreateCube()
        {
            var mesh = new SpectralXMesh("PrimCube");

            // Front
            mesh.AddQuadFace(
                new Vector3(-1, 1, 1),
                new Vector3(1, 1, 1),
                new Vector3(1, -1, 1),
                new Vector3(-1, -1, 1)
            );

            // Back
            mesh.AddQuadFace(
                new Vector3(1, 1, -1),
                new Vector3(-1, 1, -1),
                new Vector3(-1, -1, -1),
                new Vector3(1, -1, -1)
            );

            // Left
            mesh.AddQuadFace(
                new Vector3(-1, 1, -1),
                new Vector3(-1, 1, 1),
                new Vector3(-1, -1, 1),
                new Vector3(-1, -1, -1)
            );

            // Right
            mesh.AddQuadFace(
                new Vector3(1, 1, 1),
                new Vector3(1, 1, -1),
                new Vector3(1, -1, -1),
                new Vector3(1, -1, 1)
            );

            // Top
            mesh.AddQuadFace(
                new Vector3(-1, 1, -1),
                new Vector3(1, 1, -1),
                new Vector3(1, 1, 1),
                new Vector3(-1, 1, 1)
            );

            // Bottom
            mesh.AddQuadFace(
                new Vector3(-1, -1, 1),
                new Vector3(1, -1, 1),
                new Vector3(1, -1, -1),
                new Vector3(-1, -1, -1)
            );

            return mesh;
        }


        private IMesh CreatePyramid()
        {
            var mesh = new SpectralXMesh("PrimPyramid");

            Vector3 apex = new Vector3(0f, 0f, 1f);  // Z+ up

            Vector3 b0 = new Vector3(-1f, -1f, 0f);
            Vector3 b1 = new Vector3(1f, -1f, 0f);
            Vector3 b2 = new Vector3(1f, 1f, 0f);
            Vector3 b3 = new Vector3(-1f, 1f, 0f);

            mesh.AddTriangleFace(apex, b0, b1);
            mesh.AddTriangleFace(apex, b1, b2);
            mesh.AddTriangleFace(apex, b2, b3);
            mesh.AddTriangleFace(apex, b3, b0);
            mesh.AddQuadFace(b0, b3, b2, b1);

            return mesh;
        }







        /// <summary>
        /// Registers a lightweight placeholder for a mesh that was already
        /// uploaded to WebGL directly by the JS helper. BuildWebGLFrame will
        /// find the name in the scene but send no upload data — GPU already has it.
        /// </summary>
        public void RegisterJSMesh(string name)
        {
            if (meshCache.ContainsKey(name))
            {
                Console.WriteLine($"[MeshLibrary] RegisterJSMesh skipped — already exists: {name}");
                return;
            }

            var placeholder = new SpectralXMesh(name);

            lock (_meshLock)
            {
                meshCache[name] = placeholder;
                _jsMeshNames.Add(name);  // track JS-side mesh keys
            }

            Console.WriteLine($"[MeshLibrary] RegisterJSMesh: {name}");
        }







        // Load From FBX Parser
        public async Task<SpectralXMesh?> LoadFromFBXParserAsync(
            HttpClient http,
            string url,
            string name,
            IJSRuntime? js = null)
        {
            Console.WriteLine($"[MeshLibrary] LoadFromFBX called — name:{name} js:{(js == null ? "NULL" : "OK")}");

            try
            {
                // Try JS path first — parses AND uploads directly to WebGL
                // No geometry round-trip through C# at all
                if (js != null)
                {
                    try
                    {
                        Console.WriteLine($"[MeshLibrary] Trying JS helper for: {url}");

                        bool jsSuccess = await js.InvokeAsync<bool>(
                            "SpectralFBXHelper.loadAndUpload", url, name);

                        if (jsSuccess)
                        {
                            Console.WriteLine($"[MeshLibrary] JS helper success: {name}");
                            RegisterJSMesh(name);
                            return meshCache[name] as SpectralXMesh;
                        }

                        Console.WriteLine($"[MeshLibrary] JS helper returned false for: {name}, falling back");
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"[MeshLibrary] JS helper failed, falling back: {jsEx.Message}");
                    }
                }

                // Fallback — C# parser downloads and parses geometry
                Console.WriteLine($"[MeshLibrary] C# parser fallback for: {url}");
                var bytes = await http.GetByteArrayAsync(url);
                if (bytes == null || bytes.Length == 0)
                    return null;

                var fallbackMesh = SpectralXFBXParser.Parse(bytes, name);
                if (fallbackMesh == null || !fallbackMesh.IsValid())
                    return null;

                AddMesh(name, fallbackMesh);
                return fallbackMesh;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeshLibrary] FBX load error: {ex.Message}");
                return null;
            }
        }

        // Load from STL Parser

        public async Task<SpectralXMesh?> LoadFromSTLParserAsync(
    HttpClient http,
    string url,
    string name,
    IJSRuntime? js = null,
    bool smooth = false)
        {
            Console.WriteLine($"[MeshLibrary] LoadFromSTL called — name:{name} " +
                $"smooth:{smooth} js:{(js == null ? "NULL" : "OK")}");

            try
            {
                // Try JS path first — parses AND uploads directly to WebGL
                // Mirrors FBX loader pattern exactly
                if (js != null)
                {
                    try
                    {
                        Console.WriteLine($"[MeshLibrary] Trying JS STL helper for: {url}");

                        bool jsSuccess = await js.InvokeAsync<bool>(
                            "SpectralSTLHelper.loadAndUpload",
                            url,
                            name,
                            new { smooth });

                        if (jsSuccess)
                        {
                            Console.WriteLine($"[MeshLibrary] JS STL helper success: {name}");
                            RegisterJSMesh(name);
                            return meshCache[name] as SpectralXMesh;
                        }

                        Console.WriteLine($"[MeshLibrary] JS STL helper returned false " +
                            $"for: {name}, falling back to C#");
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"[MeshLibrary] JS STL helper failed, " +
                            $"falling back: {jsEx.Message}");
                    }
                }

                // Fallback — C# parser downloads and parses geometry
                Console.WriteLine($"[MeshLibrary] C# STL parser fallback for: {url}");

                var bytes = await http.GetByteArrayAsync(url);
                if (bytes == null || bytes.Length == 0)
                {
                    Console.WriteLine($"[MeshLibrary] Empty bytes for: {url}");
                    return null;
                }

                var stlMesh = SpectralXSTLParser.Parse(bytes, name, smooth);

                if (stlMesh == null || !stlMesh.IsValid())
                {
                    Console.WriteLine($"[MeshLibrary] STL parse invalid for: {name}");
                    return null;
                }

                AddMesh(name, stlMesh);
                Console.WriteLine($"[MeshLibrary] C# STL fallback success: {name} " +
                    $"verts:{stlMesh.Vertices.Count}");

                return stlMesh;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeshLibrary] STL load error: {ex.Message}");
                return null;
            }
        }

        // Load from GLTF Parser
        public async Task<SpectralXMesh?> LoadFromGLTFParserAsync(
            HttpClient http,
            string url,
            string name,
            IJSRuntime? js = null)
        {
            Console.WriteLine($"[MeshLibrary] LoadFromGLTF called — name:{name} js:{(js == null ? "NULL" : "OK")}");

            try
            {
                // JS path first — parses AND uploads directly to WebGL
                // Mirrors FBX and STL loader pattern exactly
                if (js != null)
                {
                    try
                    {
                        Console.WriteLine($"[MeshLibrary] Trying JS GLTF helper for: {url}");

                        bool jsSuccess = await js.InvokeAsync<bool>(
                            "SpectralGLTFHelper.loadAndUpload", url, name);

                        if (jsSuccess)
                        {
                            Console.WriteLine($"[MeshLibrary] JS GLTF helper success: {name}");
                            RegisterJSMesh(name);
                            return meshCache[name] as SpectralXMesh;
                        }

                        Console.WriteLine($"[MeshLibrary] JS GLTF helper returned false for: {name}, falling back");
                    }
                    catch (Exception jsEx)
                    {
                        Console.WriteLine($"[MeshLibrary] JS GLTF helper failed, falling back: {jsEx.Message}");
                    }
                }

                // C# fallback — downloads and parses geometry
                Console.WriteLine($"[MeshLibrary] C# GLTF parser fallback for: {url}");
                var bytes = await http.GetByteArrayAsync(url);
                if (bytes == null || bytes.Length == 0)
                    return null;

                var gltfMesh = SpectralXGLTFParser.Parse(bytes, name);
                if (gltfMesh == null || !gltfMesh.IsValid())
                    return null;

                AddMesh(name, gltfMesh);
                return gltfMesh;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeshLibrary] GLTF load error: {ex.Message}");
                return null;
            }
        }
        private SpectralXMesh? BuildMeshFromJSResult(JsonElement result, string name)
        {
            try
            {
                var mesh = new SpectralXMesh(name);

                // Vertices
                var vertsArray = result.GetProperty("vertices");
                foreach (var v in vertsArray.EnumerateArray())
                    mesh.Vertices.Add(new Vector3(0, 0, 0)); // placeholder — filled below

                // Rebuild vertices as Vector3 list
                mesh.Vertices.Clear();
                var verts = vertsArray.EnumerateArray().Select(v => v.GetDouble()).ToList();
                for (int i = 0; i < verts.Count - 2; i += 3)
                    mesh.Vertices.Add(new Vector3(
                        (float)verts[i],
                        (float)verts[i + 1],
                        (float)verts[i + 2]));

                // UVs
                var uvsArray = result.GetProperty("uvs");
                var uvList = uvsArray.EnumerateArray().Select(v => v.GetDouble()).ToList();
                for (int i = 0; i < uvList.Count - 1; i += 2)
                    mesh.UVs.Add(new Vector2(
                        (float)uvList[i],
                        1.0f - (float)uvList[i + 1])); // flip V

                // Normals
                var normsArray = result.GetProperty("normals");
                var normList = normsArray.EnumerateArray().Select(v => v.GetDouble()).ToList();
                for (int i = 0; i < normList.Count - 2; i += 3)
                    mesh.PolygonNormals.Add(new Vector3(
                        (float)normList[i],
                        (float)normList[i + 1],
                        (float)normList[i + 2]));

                // Raw indices + material indices — build faces
                var rawIndices = result.GetProperty("rawIndices")
                    .EnumerateArray().Select(v => v.GetInt32()).ToList();
                var uvIndices = result.GetProperty("uvIndices")
                    .EnumerateArray().Select(v => v.GetInt32()).ToList();
                var normalIndices = result.GetProperty("normalIndices")
                    .EnumerateArray().Select(v => v.GetInt32()).ToList();
                var matIndices = result.GetProperty("materialIndices")
                    .EnumerateArray().Select(v => v.GetInt32()).ToList();

                // Build faces from polygon vertex index array
                var polygon = new List<int>();
                var polygonUVs = new List<int>();
                var polygonNorms = new List<int>();
                int polyVertCounter = 0;
                int faceCounter = 0;
                int normalBase = 0;

                foreach (var idx in rawIndices)
                {
                    int actualIndex = idx < 0 ? ~idx : idx;
                    polygon.Add(actualIndex);

                    int uvIdx = uvIndices.Count > polyVertCounter ? uvIndices[polyVertCounter] : -1;
                    polygonUVs.Add(uvIdx);

                    int normIdx = normalIndices.Count > polyVertCounter
                        ? normalIndices[polyVertCounter]
                        : (normList.Count > polyVertCounter ? polyVertCounter : -1);
                    polygonNorms.Add(normIdx);

                    polyVertCounter++;

                    if (idx < 0) // end of polygon
                    {
                        int matIdx = matIndices.Count > faceCounter ? matIndices[faceCounter] : 0;

                        if (polygon.Count == 3)
                        {
                            mesh.AddTriangleFace(
                                polygon[0], polygon[1], polygon[2],
                                polygonUVs[0], polygonUVs[1], polygonUVs[2]);

                            var f = mesh.Faces[mesh.Faces.Count - 1];
                            f.PolygonNormalBase = normalBase;
                            f.MaterialIndex = matIdx;
                            mesh.Faces[mesh.Faces.Count - 1] = f;
                            normalBase += 3;
                        }
                        else if (polygon.Count == 4)
                        {
                            mesh.AddQuadFace(
                                polygon[0], polygon[3], polygon[2], polygon[1],
                                polygonUVs[0], polygonUVs[3], polygonUVs[2], polygonUVs[1]);

                            var f = mesh.Faces[mesh.Faces.Count - 1];
                            f.PolygonNormalBase = normalBase;
                            f.MaterialIndex = matIdx;
                            mesh.Faces[mesh.Faces.Count - 1] = f;
                            normalBase += 4;
                        }
                        else if (polygon.Count > 4)
                        {
                            // Ngon fan
                            int v0 = polygon[0];
                            int uv0 = polygonUVs[0];
                            int n0 = polygonNorms[0];
                            for (int i = 1; i < polygon.Count - 1; i++)
                            {
                                mesh.AddTriangleFace(
                                    v0, polygon[i], polygon[i + 1],
                                    uv0, polygonUVs[i], polygonUVs[i + 1]);

                                var f = mesh.Faces[mesh.Faces.Count - 1];
                                f.PolygonNormalBase = normalBase;
                                f.MaterialIndex = matIdx;
                                mesh.Faces[mesh.Faces.Count - 1] = f;
                                normalBase += 3;
                            }
                        }

                        faceCounter++;
                        polygon.Clear();
                        polygonUVs.Clear();
                        polygonNorms.Clear();
                    }
                }

                Console.WriteLine($"[MeshLibrary] JS mesh built: {name} " +
                    $"verts:{mesh.Vertices.Count} faces:{mesh.Faces.Count}");

                return mesh;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MeshLibrary] BuildMeshFromJSResult failed: {ex.Message}");
                return null;
            }
        }



        private void FinalizeMesh(IMesh mesh)
        {
            if (mesh is not SpectralXMesh sxMesh)
                return;

            int primitiveIndex = 0;

            for (int i = 0; i < sxMesh.Faces.Count; i++)
            {
                var face = sxMesh.Faces[i];
                face.PrimitiveIndex = primitiveIndex++;
                sxMesh.Faces[i] = face;
            }

            // Sort faces by material index once at load time
            // so BuildWebGLFrame never needs to sort per frame
            sxMesh.SortFacesByMaterial();

            if (!mesh.IsValid())
            {
                throw new InvalidOperationException(
                    $"Mesh '{mesh.Name}' failed validation during finalization.");
            }
        }


        public void Clear()
        {
            meshCache.Clear();
            LoadBuiltInPrimitives();
        }



    }
}
