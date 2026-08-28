using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Pure C# FBX Parser - Works in Blazor WASM
    /// Parses binary FBX format and extracts mesh data including textures
    /// Converts directly to System.Numerics types
    /// </summary>
    public class SpectralXFBXParser
    {
        private const int FBX_BINARY_MAGIC = 0x58424B;
        private const string FBX_ASCII_HEADER = "; FBX";

        public static SpectralXMesh Parse(byte[] data, string meshName = "FBXMesh")
        {
            try
            {
                Console.WriteLine($"[FBXParser] Starting parse of {data.Length} bytes");

                bool isBinary = IsBinaryFBX(data);
                Console.WriteLine($"[FBXParser] Format detected: {(isBinary ? "Binary" : "ASCII")}");

                if (isBinary)
                {
                    return ParseBinaryFBX(data, meshName);
                }
                else
                {
                    return ParseASCIIFBX(data, meshName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FBXParser] Parse failed: {ex.Message}");
                Console.WriteLine($"[FBXParser] Stack: {ex.StackTrace}");
                return new SpectralXMesh(meshName);
            }
        }

        private static bool IsBinaryFBX(byte[] data)
        {
            if (data.Length < 23) return false;
            string header = Encoding.ASCII.GetString(data, 0, Math.Min(18, data.Length));
            return header.StartsWith("Kaydara FBX Binary");
        }

        private static SpectralXMesh ParseBinaryFBX(byte[] data, string meshName)
        {
            var mesh = new SpectralXMesh(meshName);

            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                reader.BaseStream.Seek(23, SeekOrigin.Begin);
                int version = reader.ReadInt32();
                Console.WriteLine($"[FBXParser] FBX Version: {version}");

                var rootNodes = new List<FBXNode>();
                while (reader.BaseStream.Position < reader.BaseStream.Length - 160)
                {
                    try
                    {
                        var node = ReadNode(reader, version);
                        if (node == null) break;
                        rootNodes.Add(node);
                    }
                    catch
                    {
                        break;
                    }
                }

                Console.WriteLine($"[FBXParser] Found {rootNodes.Count} root nodes");
                ExtractMeshData(rootNodes, mesh);
            }

            return mesh;
        }

        private static FBXNode ReadNode(BinaryReader reader, int version, int depth = 0)
        {
            if (depth > 64)
            {
                Console.WriteLine("[FBXParser] Max node depth exceeded, aborting");
                return null;
            }
            long startPos = reader.BaseStream.Position;

            long endOffset = version >= 7500 ? reader.ReadInt64() : reader.ReadUInt32();
            long numProperties = version >= 7500 ? reader.ReadInt64() : reader.ReadUInt32();
            long propertyListLen = version >= 7500 ? reader.ReadInt64() : reader.ReadUInt32();

            byte nameLen = reader.ReadByte();
            string name = nameLen > 0 ? Encoding.ASCII.GetString(reader.ReadBytes(nameLen)) : "";

            if (endOffset == 0) return null;

            var node = new FBXNode { Name = name };

            for (int i = 0; i < numProperties; i++)
            {
                var prop = ReadProperty(reader);
                node.Properties.Add(prop);
            }

            while (reader.BaseStream.Position < endOffset)
            {
                long before = reader.BaseStream.Position;

                var child = ReadNode(reader, version, depth + 1);
                if (child == null) break;

                if (reader.BaseStream.Position <= before)
                {
                    Console.WriteLine("[FBXParser] ReadNode did not advance stream, aborting");
                    break;
                }

                node.Children.Add(child);
                child.Parent = node;
            }


            if (reader.BaseStream.Position < endOffset)
                reader.BaseStream.Seek(endOffset, SeekOrigin.Begin);

            return node;
        }

        private static FBXProperty ReadProperty(BinaryReader reader)
        {
            char typeCode = (char)reader.ReadByte();
            var prop = new FBXProperty { TypeCode = typeCode };

            switch (typeCode)
            {
                case 'Y': prop.Value = reader.ReadInt16(); break;
                case 'C': prop.Value = reader.ReadByte() != 0; break;
                case 'I': prop.Value = reader.ReadInt32(); break;
                case 'F': prop.Value = reader.ReadSingle(); break;
                case 'D': prop.Value = reader.ReadDouble(); break;
                case 'L': prop.Value = reader.ReadInt64(); break;
                case 'S':
                    int len = reader.ReadInt32();
                    prop.Value = Encoding.ASCII.GetString(reader.ReadBytes(len));
                    break;
                case 'R':
                    int rawLen = reader.ReadInt32();
                    prop.Value = reader.ReadBytes(rawLen);
                    break;
                case 'f': prop.Value = ReadArray(reader, r => r.ReadSingle()); break;
                case 'd': prop.Value = ReadArray(reader, r => r.ReadDouble()); break;
                case 'l': prop.Value = ReadArray(reader, r => r.ReadInt64()); break;
                case 'i': prop.Value = ReadArray(reader, r => r.ReadInt32()); break;
                case 'b': prop.Value = ReadArray(reader, r => r.ReadByte() != 0); break;
                default:
                    Console.WriteLine($"[FBXParser] Unknown property type: {typeCode}");
                    break;
            }

            return prop;
        }

        private static T[] ReadArray<T>(BinaryReader reader, Func<BinaryReader, T> readFunc)
        {
            int arrayLength = reader.ReadInt32();
            int encoding = reader.ReadInt32();
            int compressedLength = reader.ReadInt32();

            if (encoding == 0)
            {
                var arr = new T[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                    arr[i] = readFunc(reader);
                return arr;
            }

            if (encoding == 1)
            {
                byte[] compressed = reader.ReadBytes(compressedLength);
                int zlibHeader = 2;
                var ms = new MemoryStream(compressed, zlibHeader, compressedLength - zlibHeader);

                using var deflate = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress);
                using var uncompressedStream = new MemoryStream();
                deflate.CopyTo(uncompressedStream);

                byte[] uncompressed = uncompressedStream.ToArray();
                using var ub = new BinaryReader(new MemoryStream(uncompressed));

                var arr = new T[arrayLength];
                for (int i = 0; i < arrayLength; i++)
                    arr[i] = readFunc(ub);

                return arr;
            }

            Console.WriteLine($"[FBXParser] Unknown array encoding {encoding}");
            return new T[0];
        }
        private static void ExtractMeshData(List<FBXNode> nodes, SpectralXMesh mesh)
        {
            var objectsNode = FindNode(nodes, "Objects");
            if (objectsNode == null)
            {
                Console.WriteLine("[FBXParser] No Objects node found");
                return;
            }

            var geometryNodes = FindNodes(objectsNode.Children, "Geometry");
            Console.WriteLine($"[FBXParser] Found {geometryNodes.Count} geometry nodes");

            if (geometryNodes.Count == 0) return;

            var geomNode = geometryNodes[0];

            // ========= Extract vertices =========
            var verticesNode = FindNode(geomNode.Children, "Vertices");
            if (verticesNode != null && verticesNode.Properties.Count > 0)
            {
                var vertArray = verticesNode.Properties[0].Value as double[];
                if (vertArray != null)
                {
                    for (int i = 0; i < vertArray.Length; i += 3)
                    {
                        float x = (float)vertArray[i];
                        float y = (float)vertArray[i + 1];
                        float z = (float)vertArray[i + 2];
                        mesh.Vertices.Add(new Vector3(x, y, z));
                    }
                    Console.WriteLine($"[FBXParser] Loaded {mesh.Vertices.Count} vertices");
                }
            }

            // ========= Extract raw UV lookup table FIRST (needed during face building) =========
            var uvLookup = new List<Vector2>();
            int[]? uvIndexArray = null;

            var uvNode = FindNode(geomNode.Children, "LayerElementUV");
            if (uvNode != null)
            {
                var uvArrayNode = FindNode(uvNode.Children, "UV");
                var uvIndexNode = FindNode(uvNode.Children, "UVIndex");

                if (uvArrayNode != null && uvArrayNode.Properties.Count > 0)
                {
                    var uvArray = uvArrayNode.Properties[0].Value as double[];
                    if (uvArray != null)
                    {
                        for (int i = 0; i < uvArray.Length; i += 2)
                        {
                            float u = (float)uvArray[i];
                            float v = (float)uvArray[i + 1];
                            uvLookup.Add(new Vector2(u, 1.0f - v));
                        }
                        Console.WriteLine($"[FBXParser] UV lookup built: {uvLookup.Count} entries");
                    }
                }

                if (uvIndexNode != null && uvIndexNode.Properties.Count > 0)
                    uvIndexArray = uvIndexNode.Properties[0].Value as int[];
            }

            // ========= Extract normals lookup BEFORE face building =========
            var normalLookup = new List<Vector3>();
            int[]? normalIndexArray = null;

            var normalsNode = FindNode(geomNode.Children, "LayerElementNormal");
            if (normalsNode != null)
            {
                var normalArrayNode = FindNode(normalsNode.Children, "Normals");
                var normalIndexNode = FindNode(normalsNode.Children, "NormalsIndex");

                if (normalArrayNode?.Properties.Count > 0)
                {
                    var normalArray = normalArrayNode.Properties[0].Value as double[];
                    if (normalArray != null)
                    {
                        for (int i = 0; i < normalArray.Length; i += 3)
                            normalLookup.Add(new Vector3(
                                (float)normalArray[i],
                                (float)normalArray[i + 1],
                                (float)normalArray[i + 2]));
                        Console.WriteLine($"[FBXParser] Normal lookup: {normalLookup.Count} entries");
                    }
                }

                if (normalIndexNode?.Properties.Count > 0)
                    normalIndexArray = normalIndexNode.Properties[0].Value as int[];

                Console.WriteLine($"[FBXParser] NormalsIndex array: {(normalIndexArray != null ? normalIndexArray.Length.ToString() : "none")}");
            }


            // ========= Extract per-face material indices =========
            int[]? materialIndexArray = null;
            var layerMatNode = FindNode(geomNode.Children, "LayerElementMaterial");
            if (layerMatNode != null)
            {
                var matIndexNode = FindNode(layerMatNode.Children, "Materials");
                if (matIndexNode?.Properties.Count > 0)
                    materialIndexArray = matIndexNode.Properties[0].Value as int[];

                // DEBUG BLOCK 1 — face index distribution
                if (materialIndexArray != null)
                {
                    Console.WriteLine($"[FBXParser] === FACE INDEX DEBUG for {mesh.Name} ===");
                    var counts = new Dictionary<int, int>();
                    foreach (var idx in materialIndexArray)
                    {
                        if (!counts.ContainsKey(idx)) counts[idx] = 0;
                        counts[idx]++;
                    }
                    foreach (var kvp in counts.OrderBy(k => k.Key))
                        Console.WriteLine($"[FBXParser] Face index {kvp.Key}: {kvp.Value} faces");
                    Console.WriteLine($"[FBXParser] First 20 raw indices: {string.Join(", ", materialIndexArray.Take(20))}");
                }
                else
                    Console.WriteLine($"[FBXParser] WARNING: materialIndexArray is NULL for {mesh.Name}");


                Console.WriteLine($"[FBXParser] Material index array: {(materialIndexArray != null ? materialIndexArray.Length.ToString() : "none")}");
            }




            // ========= Extract faces + UV + Normal indices in single synchronized pass =========
            var indicesNode = FindNode(geomNode.Children, "PolygonVertexIndex");
            if (indicesNode != null && indicesNode.Properties.Count > 0)
            {
                var indexArray = indicesNode.Properties[0].Value as int[];
                if (indexArray != null)
                {
                    var polygon = new List<int>();
                    var polygonUVs = new List<int>();
                    var polygonNormIdxs = new List<int>();
                    int polyVertexCounter = 0;
                    int faceCounter = 0;
                    int normalBaseCounter = 0;  // ADD THIS
                    foreach (var idx in indexArray)
                    {
                        if (polygon.Count > 32)
                        {
                            Console.WriteLine("[FBXParser] Polygon overflow, aborting face");
                            polygon.Clear();
                            polygonUVs.Clear();
                            polygonNormIdxs.Clear();
                            polyVertexCounter++;
                            continue;
                        }

                        int actualIndex = idx < 0 ? ~idx : idx;
                        polygon.Add(actualIndex);

                        // UV index
                        int uvIdx = -1;
                        if (uvIndexArray != null && polyVertexCounter < uvIndexArray.Length)
                            uvIdx = uvIndexArray[polyVertexCounter];
                        else if (uvLookup.Count > 0 && polyVertexCounter < uvLookup.Count)
                            uvIdx = polyVertexCounter;
                        polygonUVs.Add(uvIdx);

                        // Normal index
                        int normIdx = -1;
                        if (normalIndexArray != null && polyVertexCounter < normalIndexArray.Length)
                            normIdx = normalIndexArray[polyVertexCounter];
                        else if (normalLookup.Count > polyVertexCounter)
                            normIdx = polyVertexCounter;
                        polygonNormIdxs.Add(normIdx);

                        polyVertexCounter++;

                        if (idx < 0) // end of polygon
                        {
                            void AddNormals(int[] normIndices, bool flipNormals = false)
                            {
                                foreach (var ni in normIndices)
                                {
                                    if (ni >= 0 && ni < normalLookup.Count)
                                    {
                                        var n = normalLookup[ni];
                                        mesh.PolygonNormals.Add(flipNormals ? -n : n);
                                    }
                                    else
                                        mesh.PolygonNormals.Add(Vector3.Zero);
                                }
                            }

                            if (polygon.Count == 3)
                            {
                                mesh.AddTriangleFace(
                                    polygon[0], polygon[1], polygon[2],
                                    polygonUVs[0], polygonUVs[1], polygonUVs[2]);
                                AddNormals(new[] { polygonNormIdxs[0], polygonNormIdxs[1], polygonNormIdxs[2] }, flipNormals: false);

                                // Always bake normal base - regardless of material array
                                var f = mesh.Faces[mesh.Faces.Count - 1];
                                f.PolygonNormalBase = normalBaseCounter;
                                if (materialIndexArray != null && faceCounter < materialIndexArray.Length)
                                    f.MaterialIndex = materialIndexArray[faceCounter];
                                mesh.Faces[mesh.Faces.Count - 1] = f;

                                normalBaseCounter += 3;
                                faceCounter++;
                            }
                            else if (polygon.Count == 4)
                            {
                                mesh.AddQuadFace(
                                    polygon[0], polygon[3], polygon[2], polygon[1],
                                    polygonUVs[0], polygonUVs[3], polygonUVs[2], polygonUVs[1]);
                                AddNormals(new[] {
        polygonNormIdxs[0], polygonNormIdxs[3], polygonNormIdxs[2], polygonNormIdxs[1]  // match vertex order!
    }, flipNormals: true);

                                var f = mesh.Faces[mesh.Faces.Count - 1];
                                f.PolygonNormalBase = normalBaseCounter;
                                if (materialIndexArray != null && faceCounter < materialIndexArray.Length)
                                    f.MaterialIndex = materialIndexArray[faceCounter];
                                mesh.Faces[mesh.Faces.Count - 1] = f;
                                normalBaseCounter += 4;
                                faceCounter++;
                            }


                         


                            else
                            {
                                // Ngon fan — fix: track PolygonNormalBase and faceCounter correctly
                                int v0 = polygon[0];
                                int uv0 = polygonUVs[0];
                                int n0 = polygonNormIdxs[0];
                                for (int i = 1; i < polygon.Count - 1; i++)
                                {
                                    mesh.AddTriangleFace(
     v0, polygon[i], polygon[i + 1],  // swapped
     uv0, polygonUVs[i], polygonUVs[i + 1]);  // swap UVs to match
                                    AddNormals(new[] { n0, polygonNormIdxs[i], polygonNormIdxs[i + 1] });

                                    var f = mesh.Faces[mesh.Faces.Count - 1];
                                    f.PolygonNormalBase = normalBaseCounter;
                                    if (materialIndexArray != null && faceCounter < materialIndexArray.Length)
                                        f.MaterialIndex = materialIndexArray[faceCounter];
                                    mesh.Faces[mesh.Faces.Count - 1] = f;

                                    normalBaseCounter += 3;
                                }
                                faceCounter++;
                            }

                            polygon.Clear();
                            polygonUVs.Clear();
                            polygonNormIdxs.Clear();
                        }
                    }

                    Console.WriteLine(
      $"[FBXParser] Loaded {mesh.TriangleFaceCount} triangle faces, {mesh.QuadFaceCount} quad faces"
  );

                    // ADD HERE:
                    if (materialIndexArray != null)
                    {
                        var slotCounts = new Dictionary<int, int>();
                        foreach (var idx in materialIndexArray)
                        {
                            if (!slotCounts.ContainsKey(idx)) slotCounts[idx] = 0;
                            slotCounts[idx]++;
                        }
                        Console.WriteLine($"[FBXParser] Face distribution by material slot:");
                        foreach (var kvp in slotCounts.OrderBy(k => k.Key))
                            Console.WriteLine($"[FBXParser]   Slot {kvp.Key}: {kvp.Value} faces");
                    }
                }
            }



            // to add for smooth shading

            // ========= Store UV lookup table on mesh =========
            mesh.UVs.AddRange(uvLookup);
            Console.WriteLine($"[FBXParser] Stored {mesh.UVs.Count} UVs on mesh");

            // ========= Extract texture from materials =========
            ExtractTextureData(objectsNode, mesh, nodes, materialIndexArray);
        }
        private static void ExtractTextureData(FBXNode objectsNode, SpectralXMesh mesh, List<FBXNode> rootNodes, int[]? materialIndexArray)
        {
            bool IsWhite(Vector4 c) => c.X >= 0.99f && c.Y >= 0.99f && c.Z >= 0.99f;
            bool IsZero(Vector4 c) => c.X == 0f && c.Y == 0f && c.Z == 0f && c.W == 0f;

            Console.WriteLine($"[FBXParser] ExtractTextureData called for: {mesh.Name}");

            // Step 1 — Build videoId -> base64 dataUrl map
            var videoNodes = FindNodes(objectsNode.Children, "Video");
            var videoDataUrls = new Dictionary<long, string>();

            foreach (var videoNode in videoNodes)
            {
                if (videoNode.Properties.Count == 0) continue;
                long videoId = 0;
                if (videoNode.Properties[0].Value is long l) videoId = l;
                else if (videoNode.Properties[0].Value is int i) videoId = i;
                if (videoId == 0) continue;

                var contentNode = FindNode(videoNode.Children, "Content");
                if (contentNode?.Properties.Count > 0)
                {
                    var imageBytes = contentNode.Properties[0].Value as byte[];
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        string mimeType = "image/png";
                        if (imageBytes.Length > 2 && imageBytes[0] == 0xFF && imageBytes[1] == 0xD8)
                            mimeType = "image/jpeg";
                        videoDataUrls[videoId] = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
                    }
                }
            }

            // Step 2 — Build textureId -> videoId map from Texture nodes
            var textureNodes = FindNodes(objectsNode.Children, "Texture");
            var textureToVideo = new Dictionary<long, long>();

            foreach (var texNode in textureNodes)
            {
                if (texNode.Properties.Count == 0) continue;
                long texId = 0;
                if (texNode.Properties[0].Value is long tl) texId = tl;
                else if (texNode.Properties[0].Value is int ti) texId = ti;
                if (texId == 0) continue;
                textureToVideo[texId] = 0;
            }

            // Step 3 — Parse Connections
            var connectionsNode = FindNode(rootNodes, "Connections");
            var texToVideoConn = new Dictionary<long, long>();
            var matToTexConn = new Dictionary<long, long>();

            if (connectionsNode != null)
            {
                foreach (var connChild in connectionsNode.Children)
                {
                    if (connChild.Properties.Count < 3) continue;
                    string connType = connChild.Properties[0].Value as string ?? "";
                    long idA = 0, idB = 0;
                    if (connChild.Properties[1].Value is long la) idA = la;
                    else if (connChild.Properties[1].Value is int ia) idA = ia;
                    if (connChild.Properties[2].Value is long lb) idB = lb;
                    else if (connChild.Properties[2].Value is int ib) idB = ib;

                    if (connType == "OO")
                    {
                        if (videoDataUrls.ContainsKey(idA))
                            texToVideoConn[idB] = idA;
                    }
                    if (connType == "OP")
                    {
                        matToTexConn[idB] = idA;
                    }
                }
            }

            // Step 4 — Build materialId list in connection order
            var meshId = GetObjectId(objectsNode, mesh.Name);
            var materialIds = new List<long>();

            if (connectionsNode != null)
            {
                foreach (var connChild in connectionsNode.Children)
                {
                    if (connChild.Properties.Count < 3) continue;
                    string connType = connChild.Properties[0].Value as string ?? "";
                    long idA = 0, idB = 0;
                    if (connChild.Properties[1].Value is long la) idA = la;
                    else if (connChild.Properties[1].Value is int ia) idA = ia;
                    if (connChild.Properties[2].Value is long lb) idB = lb;
                    else if (connChild.Properties[2].Value is int ib) idB = ib;

                    if (connType == "OO" && idB == meshId
       && !videoDataUrls.ContainsKey(idA)
       && !texToVideoConn.ContainsKey(idA))
                    {
                        // Skip the phantom "unknown" slot Blender always inserts first
                        // Detect it: no Material node exists for this ID in Objects
                        var matNodesCheck = FindNodes(objectsNode.Children, "Material");
                        bool isPhantom = !matNodesCheck.Any(mn => {
                            long mnId = 0;
                            if (mn.Properties.Count > 0)
                            {
                                if (mn.Properties[0].Value is long l) mnId = l;
                                else if (mn.Properties[0].Value is int i) mnId = i;
                            }
                            return mnId == idA;
                        });

                        if (!isPhantom)
                            materialIds.Add(idA);
                    }
                }
            }

            // DEBUG BLOCK — slot order with names, printed ONCE after materialIds is fully built
            Console.WriteLine($"[FBXParser] === SLOT ORDER DEBUG for {mesh.Name} ===");
            Console.WriteLine($"[FBXParser] Total slots: {materialIds.Count}");
            var matNodesForDebug = FindNodes(objectsNode.Children, "Material");
            for (int s = 0; s < materialIds.Count; s++)
            {
                long matId = materialIds[s];
                string matName = "unknown";
                foreach (var mn in matNodesForDebug)
                {
                    long mnId = 0;
                    if (mn.Properties.Count > 0)
                    {
                        if (mn.Properties[0].Value is long l) mnId = l;
                        else if (mn.Properties[0].Value is int i) mnId = i;
                    }
                    if (mnId == matId && mn.Properties.Count > 1)
                    {
                        matName = mn.Properties[1].Value as string ?? "unknown";
                        break;
                    }
                }
                Console.WriteLine($"[FBXParser] Slot {s}: ID={matId} Name='{matName}'");
            }

            // DEBUG BLOCK — face index distribution
            if (materialIndexArray != null)
            {
                Console.WriteLine($"[FBXParser] === FACE INDEX DEBUG for {mesh.Name} ===");
                var counts = new Dictionary<int, int>();
                foreach (var idx in materialIndexArray)
                {
                    if (!counts.ContainsKey(idx)) counts[idx] = 0;
                    counts[idx]++;
                }
                foreach (var kvp in counts.OrderBy(k => k.Key))
                    Console.WriteLine($"[FBXParser] Face index {kvp.Key}: {kvp.Value} faces");
                Console.WriteLine($"[FBXParser] First 20 raw indices: {string.Join(", ", materialIndexArray.Take(20))}");
            }
            else
                Console.WriteLine($"[FBXParser] WARNING: materialIndexArray is NULL for {mesh.Name}");

            // Step 5 — Build slot-indexed MaterialTextures array
            int slotCount = Math.Max(materialIds.Count, 1);
            var slotTextures = new List<string>(new string[slotCount]);

            for (int s = 0; s < materialIds.Count; s++)
            {
                long matId = materialIds[s];
                if (matToTexConn.TryGetValue(matId, out long texId))
                {
                    if (texToVideoConn.TryGetValue(texId, out long vidId))
                    {
                        if (videoDataUrls.TryGetValue(vidId, out string? dataUrl))
                        {
                            slotTextures[s] = dataUrl;
                            Console.WriteLine($"[FBXParser] Slot {s}: HAS TEXTURE");
                        }
                    }
                }
                else
                    Console.WriteLine($"[FBXParser] Slot {s}: NO TEXTURE");
            }

            // Orphan texture cascade
            if (materialIndexArray != null)
            {
                var faceCounts = new Dictionary<int, int>();
                foreach (var idx in materialIndexArray)
                {
                    if (!faceCounts.ContainsKey(idx)) faceCounts[idx] = 0;
                    faceCounts[idx]++;
                }

                var orphanTextures = new Queue<string>();
                for (int s = 0; s < slotTextures.Count; s++)
                {
                    bool hasTexture = !string.IsNullOrEmpty(slotTextures[s]);
                    bool hasFaces = faceCounts.ContainsKey(s) && faceCounts[s] > 0;
                    if (hasTexture && !hasFaces)
                    {
                        orphanTextures.Enqueue(slotTextures[s]);
                        slotTextures[s] = "";
                        Console.WriteLine($"[FBXParser] Orphan slot {s} queued for cascade");
                    }
                }
                if (orphanTextures.Count > 0)
                {
                    for (int s = slotTextures.Count - 1; s >= 0; s--)
                    {
                        bool hasTexture = !string.IsNullOrEmpty(slotTextures[s]);
                        bool hasFaces = faceCounts.ContainsKey(s) && faceCounts[s] > 0;
                        if (hasFaces && !hasTexture && orphanTextures.Count > 0)
                        {
                            slotTextures[s] = orphanTextures.Dequeue();
                            Console.WriteLine($"[FBXParser] Cascade: texture assigned to slot {s}");
                        }
                    }
                }
            }

            // Extract per-slot material colors
            mesh.MaterialColors = new List<Vector4>(new Vector4[slotCount]);
            for (int s = 0; s < materialIds.Count; s++)
            {
                mesh.MaterialColors[s] = ExtractMaterialColor(objectsNode, materialIds[s]);
                Console.WriteLine($"[FBXParser] Slot {s} color: {mesh.MaterialColors[s]}");
            }

            // Color cascade
            if (materialIndexArray != null)
            {
                var faceCounts = new Dictionary<int, int>();
                foreach (var idx in materialIndexArray)
                {
                    if (!faceCounts.ContainsKey(idx)) faceCounts[idx] = 0;
                    faceCounts[idx]++;
                }

                var orphanColors = new Queue<Vector4>();
                for (int s = 0; s < mesh.MaterialColors.Count; s++)
                {
                    bool hasRealColor = !IsWhite(mesh.MaterialColors[s]) && !IsZero(mesh.MaterialColors[s]);
                    bool hasFaces = faceCounts.ContainsKey(s) && faceCounts[s] > 0;
                    if (hasRealColor && !hasFaces)
                    {
                        orphanColors.Enqueue(mesh.MaterialColors[s]);
                        mesh.MaterialColors[s] = Vector4.One;
                    }
                }

                if (orphanColors.Count > 0)
                {
                    for (int s = mesh.MaterialColors.Count - 1; s >= 0; s--)
                    {
                        bool isDefault = IsWhite(mesh.MaterialColors[s]);
                        bool hasFaces = faceCounts.ContainsKey(s) && faceCounts[s] > 0;
                        if (hasFaces && isDefault && orphanColors.Count > 0)
                        {
                            mesh.MaterialColors[s] = orphanColors.Dequeue();
                            Console.WriteLine($"[FBXParser] Color cascade: slot {s} assigned orphan color");
                        }
                    }
                }
            }

            // Fallbacks
            if (mesh.MaterialColors.All(c => c == Vector4.Zero))
                for (int s = 0; s < slotCount; s++)
                    mesh.MaterialColors[s] = new Vector4(1f, 1f, 1f, 1f);

            if (mesh.MaterialTextures.Count > 0 &&
                mesh.MaterialTextures.All(t => t == null) &&
                videoDataUrls.Count > 0)
            {
                mesh.MaterialTextures[0] = videoDataUrls.First().Value;
                Console.WriteLine($"[FBXParser] Fallback: assigned only video to slot 0");
            }

            var firstTex = slotTextures.FirstOrDefault(t => !string.IsNullOrEmpty(t));
            if (firstTex != null)
            {
                mesh.TextureDataUrl = firstTex;
                mesh.TextureIsRawRGBA = false;
                mesh.TextureWidth = 0;
                mesh.TextureHeight = 0;
            }

            mesh.MaterialTextures = slotTextures;

            Console.WriteLine($"[FBXParser] Final slots: {slotTextures.Count}");
            for (int s = 0; s < slotTextures.Count; s++)
                Console.WriteLine($"[FBXParser] Slot {s}: {(string.IsNullOrEmpty(slotTextures[s]) ? "NO TEXTURE" : "HAS TEXTURE")}");
        }

        private static long GetObjectId(FBXNode objectsNode, string meshName)
        {
            var modelNodes = FindNodes(objectsNode.Children, "Model");
            if (modelNodes.Count > 0)
            {
                var first = modelNodes[0];
                if (first.Properties[0].Value is long lid) return lid;
                if (first.Properties[0].Value is int iid) return iid;
            }
            return 0;
        }


        private static Vector4 ExtractMaterialColor(FBXNode objectsNode, long materialId)
        {
            var materialNodes = FindNodes(objectsNode.Children, "Material");
            foreach (var matNode in materialNodes)
            {
                if (matNode.Properties.Count == 0) continue;
                long id = 0;
                if (matNode.Properties[0].Value is long l) id = l;
                else if (matNode.Properties[0].Value is int i) id = i;
                if (id != materialId) continue;

                var props70 = FindNode(matNode.Children, "Properties70");
                if (props70 == null) continue;

                foreach (var p in props70.Children)
                {
                    if (p.Properties.Count < 5) continue;
                    string propName = p.Properties[0].Value as string ?? "";
                    if (propName != "DiffuseColor") continue;

                    double r = 1, g = 1, b = 1;
                    if (p.Properties[4].Value is double dr) r = dr;
                    else if (p.Properties[4].Value is float fr) r = fr;
                    if (p.Properties[5].Value is double dg) g = dg;
                    else if (p.Properties[5].Value is float fg) g = fg;
                    if (p.Properties[6].Value is double db) b = db;
                    else if (p.Properties[6].Value is float fb) b = fb;

                    Console.WriteLine($"[FBXParser] Material ID {materialId} DiffuseColor: {r:F2},{g:F2},{b:F2}");
                    return new Vector4((float)r, (float)g, (float)b, 1f);
                }
            }
            return new Vector4(1f, 1f, 1f, 1f); // fallback white
        }




        private static FBXNode FindNode(List<FBXNode> nodes, string name)
        {
            foreach (var node in nodes)
            {
                if (node.Name == name) return node;
            }
            return null;
        }

        private static List<FBXNode> FindNodes(List<FBXNode> nodes, string name)
        {
            var results = new List<FBXNode>();
            foreach (var node in nodes)
            {
                if (node.Name == name) results.Add(node);
            }
            return results;
        }

        private static SpectralXMesh ParseASCIIFBX(byte[] data, string meshName)
        {
            var mesh = new SpectralXMesh(meshName);
            Console.WriteLine("[FBXParser] ASCII FBX parsing not yet implemented");
            return mesh;
        }

        private class FBXNode
        {
            public string Name { get; set; } = "";
            public List<FBXProperty> Properties { get; set; } = new();
            public List<FBXNode> Children { get; set; } = new();

            public FBXNode? Parent { get; set; } = null;
        }

        private class FBXProperty
        {
            public char TypeCode { get; set; }
            public object Value { get; set; }
        }
    }
}