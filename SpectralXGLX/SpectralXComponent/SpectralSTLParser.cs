using SpectralXGLX.SpectralXComponent.SpectralXRender;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Pure C# STL Parser — Binary and ASCII
    /// Fallback path when JS helper is unavailable
    /// Matches JS helper output exactly — flat or smooth normals, triplanar UVs
    /// No texture support — STL is pure geometry by spec
    /// Display material is assigned by SpectralXEngine after load
    /// </summary>
    public class SpectralXSTLParser
    {
        // ----------------------------------------------------------------
        // Weld tolerance for smooth normal generation
        // Vertices within this distance are considered the same point
        // ----------------------------------------------------------------
        private const float WELD_TOLERANCE = 0.0001f;

        // ----------------------------------------------------------------
        // Triplanar UV scale — matches JS helper value
        // ----------------------------------------------------------------
        private const float UV_SCALE = 0.5f;

        // ----------------------------------------------------------------
        // Entry point — matches FBXParser.Parse signature pattern
        // smooth: false = flat shading (hard edges, crystalline look)
        //         true  = smooth shading (averaged normals, organic look)
        // ----------------------------------------------------------------
        public static SpectralXMesh Parse(byte[] data, string meshName = "STLMesh",
            bool smooth = false)
        {
            try
            {
                Console.WriteLine($"[STLParser] Starting parse — {data.Length} bytes " +
                    $"mesh:{meshName} smooth:{smooth}");

                bool isBinary = DetectBinary(data);
                Console.WriteLine($"[STLParser] Format: {(isBinary ? "Binary" : "ASCII")}");

                var triangles = isBinary
                    ? ParseBinary(data)
                    : ParseASCII(data);

                Console.WriteLine($"[STLParser] Triangles parsed: {triangles.Count}");

                if (triangles.Count == 0)
                {
                    Console.WriteLine($"[STLParser] No triangles found in: {meshName}");
                    return new SpectralXMesh(meshName);
                }

                var mesh = smooth
                    ? BuildSmooth(triangles, meshName)
                    : BuildFlat(triangles, meshName);

                Console.WriteLine($"[STLParser] Done — verts:{mesh.Vertices.Count} " +
                    $"faces:{mesh.Faces.Count}");

                return mesh;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STLParser] Parse failed: {ex.Message}");
                Console.WriteLine($"[STLParser] Stack: {ex.StackTrace}");
                return new SpectralXMesh(meshName);
            }
        }

        // ----------------------------------------------------------------
        // Binary vs ASCII detection
        // Mirrors JS helper detectBinary logic exactly
        // Binary STL: 80 byte header + 4 byte count + N * 50 bytes
        // ASCII STL:  starts with "solid" keyword
        // Edge case: some binary STLs have "solid" in header —
        // check file size formula first as most reliable signal
        // ----------------------------------------------------------------
        private static bool DetectBinary(byte[] data)
        {
            if (data.Length < 84) return false;

            // Read triangle count from binary header position
            uint triCount = BitConverter.ToUInt32(data, 80);
            long expectedSize = 84 + (long)triCount * 50;

            // File size matches binary formula — definitely binary
            if (data.Length == expectedSize) return true;

            // Check for ASCII "solid" header
            string header = Encoding.ASCII.GetString(data, 0, Math.Min(5, data.Length))
                .ToLowerInvariant();

            if (header == "solid") return false;

            return true;
        }

        // ----------------------------------------------------------------
        // Binary STL parser
        // Layout: 80 byte header | 4 byte uint32 triCount |
        //         Per triangle: 12 bytes normal (3x float32)
        //                       36 bytes verts  (3x 3x float32)
        //                        2 bytes attr   (skip)
        // Total per triangle: 50 bytes
        // ----------------------------------------------------------------
        private static List<STLTriangle> ParseBinary(byte[] data)
        {
            var triangles = new List<STLTriangle>();

            uint triCount = BitConverter.ToUInt32(data, 80);
            Console.WriteLine($"[STLParser] Binary triangle count: {triCount}");

            int offset = 84;

            for (uint i = 0; i < triCount; i++)
            {
                if (offset + 50 > data.Length)
                {
                    Console.WriteLine($"[STLParser] Buffer overrun at triangle {i} — stopping");
                    break;
                }

                // Face normal
                float nx = BitConverter.ToSingle(data, offset); offset += 4;
                float ny = BitConverter.ToSingle(data, offset); offset += 4;
                float nz = BitConverter.ToSingle(data, offset); offset += 4;

                // Vertex 0
                float v0x = BitConverter.ToSingle(data, offset); offset += 4;
                float v0y = BitConverter.ToSingle(data, offset); offset += 4;
                float v0z = BitConverter.ToSingle(data, offset); offset += 4;

                // Vertex 1
                float v1x = BitConverter.ToSingle(data, offset); offset += 4;
                float v1y = BitConverter.ToSingle(data, offset); offset += 4;
                float v1z = BitConverter.ToSingle(data, offset); offset += 4;

                // Vertex 2
                float v2x = BitConverter.ToSingle(data, offset); offset += 4;
                float v2y = BitConverter.ToSingle(data, offset); offset += 4;
                float v2z = BitConverter.ToSingle(data, offset); offset += 4;

                // Attribute byte count — skip
                offset += 2;

                triangles.Add(new STLTriangle
                {
                    Normal = new Vector3(nx, ny, nz),
                    V0 = new Vector3(v0x, v0y, v0z),
                    V1 = new Vector3(v1x, v1y, v1z),
                    V2 = new Vector3(v2x, v2y, v2z)
                });
            }

            return triangles;
        }

        // ----------------------------------------------------------------
        // ASCII STL parser
        // Walks the text looking for facet/vertex blocks
        // Handles varied whitespace and line endings
        // ----------------------------------------------------------------
        private static List<STLTriangle> ParseASCII(byte[] data)
        {
            var triangles = new List<STLTriangle>();
            string text = Encoding.UTF8.GetString(data);

            // Match each facet block — normal + 3 vertices
            var pattern = new Regex(
                @"facet\s+normal\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)" +
                @"\s+outer\s+loop" +
                @"\s+vertex\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)" +
                @"\s+vertex\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)" +
                @"\s+vertex\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)\s+([\d.eE+\-]+)" +
                @"\s+endloop\s+endfacet",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var matches = pattern.Matches(text);
            Console.WriteLine($"[STLParser] ASCII facets found: {matches.Count}");

            foreach (Match m in matches)
            {
                triangles.Add(new STLTriangle
                {
                    Normal = new Vector3(
                        float.Parse(m.Groups[1].Value),
                        float.Parse(m.Groups[2].Value),
                        float.Parse(m.Groups[3].Value)),
                    V0 = new Vector3(
                        float.Parse(m.Groups[4].Value),
                        float.Parse(m.Groups[5].Value),
                        float.Parse(m.Groups[6].Value)),
                    V1 = new Vector3(
                        float.Parse(m.Groups[7].Value),
                        float.Parse(m.Groups[8].Value),
                        float.Parse(m.Groups[9].Value)),
                    V2 = new Vector3(
                        float.Parse(m.Groups[10].Value),
                        float.Parse(m.Groups[11].Value),
                        float.Parse(m.Groups[12].Value))
                });
            }

            return triangles;
        }

        // ----------------------------------------------------------------
        // Flat shading build
        // Each triangle gets its own 3 vertices with the STL face normal
        // No vertex sharing — hard edges preserved exactly as in source file
        // STL normals used directly — zero deviation from source geometry
        // ----------------------------------------------------------------
        private static SpectralXMesh BuildFlat(List<STLTriangle> triangles, string meshName)
        {
            var mesh = new SpectralXMesh(meshName);

            int vertIndex = 0;

            foreach (var tri in triangles)
            {
                // Use STL face normal — compute if zero
                Vector3 normal = GetOrComputeNormal(tri);

                // Add 3 vertices
                mesh.Vertices.Add(tri.V0);
                mesh.Vertices.Add(tri.V1);
                mesh.Vertices.Add(tri.V2);

                // Same flat normal for all 3 verts
                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);

                // Triplanar UVs
                mesh.UVs.Add(TriplanarUV(tri.V0, normal));
                mesh.UVs.Add(TriplanarUV(tri.V1, normal));
                mesh.UVs.Add(TriplanarUV(tri.V2, normal));

                // Triangle face — direct indices
                mesh.AddTriangleFace(vertIndex, vertIndex + 1, vertIndex + 2,
                    vertIndex, vertIndex + 1, vertIndex + 2);

                // Bake normal base and material index
                var face = mesh.Faces[mesh.Faces.Count - 1];
                face.PolygonNormalBase = vertIndex;
                face.MaterialIndex = 0;
                mesh.Faces[mesh.Faces.Count - 1] = face;

                // Bake polygon normals matching vertex order
                mesh.PolygonNormals.Add(normal);
                mesh.PolygonNormals.Add(normal);
                mesh.PolygonNormals.Add(normal);

                vertIndex += 3;
            }

            return mesh;
        }

        // ----------------------------------------------------------------
        // Smooth shading build
        // Welds vertices within WELD_TOLERANCE
        // Averages normals across all faces sharing each welded vertex
        // Produces smooth organic look for curved anatomy models
        // ----------------------------------------------------------------
        private static SpectralXMesh BuildSmooth(List<STLTriangle> triangles, string meshName)
        {
            var mesh = new SpectralXMesh(meshName);

            // Pass 1 — weld vertices and accumulate normals
            var posMap = new Dictionary<string, int>();
            var weldedPositions = new List<Vector3>();
            var normalAccum = new List<Vector3>();

            string GetKey(Vector3 v)
            {
                int qx = (int)MathF.Round(v.X / WELD_TOLERANCE);
                int qy = (int)MathF.Round(v.Y / WELD_TOLERANCE);
                int qz = (int)MathF.Round(v.Z / WELD_TOLERANCE);
                return $"{qx},{qy},{qz}";
            }

            // Register all unique positions
            foreach (var tri in triangles)
            {
                foreach (var v in new[] { tri.V0, tri.V1, tri.V2 })
                {
                    string key = GetKey(v);
                    if (!posMap.ContainsKey(key))
                    {
                        posMap[key] = weldedPositions.Count;
                        weldedPositions.Add(v);
                        normalAccum.Add(Vector3.Zero);
                    }
                }
            }

            // Accumulate face normals at each welded vertex
            foreach (var tri in triangles)
            {
                Vector3 normal = GetOrComputeNormal(tri);

                foreach (var v in new[] { tri.V0, tri.V1, tri.V2 })
                {
                    int wi = posMap[GetKey(v)];
                    normalAccum[wi] += normal;
                }
            }

            // Normalize accumulated normals
            var smoothNormals = normalAccum
                .Select(n => n.LengthSquared() > 0 ? Vector3.Normalize(n) : Vector3.UnitZ)
                .ToList();

            // Pass 2 — build final mesh arrays
            int vertIndex = 0;

            foreach (var tri in triangles)
            {
                foreach (var v in new[] { tri.V0, tri.V1, tri.V2 })
                {
                    int wi = posMap[GetKey(v)];
                    Vector3 sn = smoothNormals[wi];

                    mesh.Vertices.Add(v);
                    mesh.Normals.Add(sn);
                    mesh.UVs.Add(TriplanarUV(v, sn));
                    mesh.PolygonNormals.Add(sn);
                }

                mesh.AddTriangleFace(vertIndex, vertIndex + 1, vertIndex + 2,
                    vertIndex, vertIndex + 1, vertIndex + 2);

                var face = mesh.Faces[mesh.Faces.Count - 1];
                face.PolygonNormalBase = vertIndex;
                face.MaterialIndex = 0;
                mesh.Faces[mesh.Faces.Count - 1] = face;

                vertIndex += 3;
            }

            return mesh;
        }

        // ----------------------------------------------------------------
        // Get STL face normal or compute from vertices if zero
        // Mirrors JS helper normal fallback exactly
        // ----------------------------------------------------------------
        private static Vector3 GetOrComputeNormal(STLTriangle tri)
        {
            float len = tri.Normal.Length();
            if (len > 0.0001f)
                return tri.Normal / len;

            // Compute from cross product
            Vector3 edge1 = tri.V1 - tri.V0;
            Vector3 edge2 = tri.V2 - tri.V0;
            Vector3 computed = Vector3.Cross(edge1, edge2);

            float cLen = computed.Length();
            return cLen > 0 ? computed / cLen : Vector3.UnitZ;
        }

        // ----------------------------------------------------------------
        // Triplanar UV projection
        // Mirrors JS helper triplanarUV exactly
        // Projects onto dominant normal axis plane
        // ----------------------------------------------------------------
        private static Vector2 TriplanarUV(Vector3 pos, Vector3 normal)
        {
            float ax = MathF.Abs(normal.X);
            float ay = MathF.Abs(normal.Y);
            float az = MathF.Abs(normal.Z);

            if (ax >= ay && ax >= az)
                return new Vector2(pos.Y * UV_SCALE, pos.Z * UV_SCALE); // YZ plane
            else if (ay >= ax && ay >= az)
                return new Vector2(pos.X * UV_SCALE, pos.Z * UV_SCALE); // XZ plane
            else
                return new Vector2(pos.X * UV_SCALE, pos.Y * UV_SCALE); // XY plane
        }

        // ----------------------------------------------------------------
        // Internal triangle data structure
        // ----------------------------------------------------------------
        private struct STLTriangle
        {
            public Vector3 Normal;
            public Vector3 V0;
            public Vector3 V1;
            public Vector3 V2;
        }
    }
}