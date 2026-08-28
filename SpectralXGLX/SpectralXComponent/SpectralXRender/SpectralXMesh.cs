using SpectralXGLX.SpectralGL.Math;
using System.Numerics;

namespace SpectralXGLX.SpectralXComponent.SpectralXRender
{
    public enum FaceType
    {
        Triangle = 3,
        Quad = 4

            // to be a fpolygan face or not to be that is the question?
    }

    public struct Face
    {
        public FaceType Type;

        // Vertex indices
        public int A;
        public int B;
        public int C;
        public int D;

        // UV indices (-1 = no UV)
        public int UVA;
        public int UVB;
        public int UVC;
        public int UVD;

        // Stable per-mesh primitive identity
        public int PrimitiveIndex;
        public int MaterialIndex; // which material slot this face uses
        public int PolygonNormalBase;
    }

    public readonly struct Edge
    {
        public int A { get; }
        public int B { get; }

        public Edge(int a, int b)
        {
            A = a;
            B = b;
        }
    }

    public enum TextAlignment
    {
        Left = 0,
        Center = 1,
        Right = 2
    }

    public class SpectralXMesh : IMesh
    {
        public string Name { get; set; } = string.Empty;
        public bool TransformDirty { get; set; } = true;

        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set { if (_position != value) { _position = value; TransformDirty = true; } }
        }

        private Vector4 _color = new Vector4(1f, 1f, 1f, 1f);
        public Vector4 Color
        {
            get => _color;
            set { if (_color != value) { _color = value; TransformDirty = true; } }
        }

        private Vector3 _size = new Vector3(1f, 1f, 1f);
        public Vector3 Size
        {
            get => _size;
            set { if (_size != value) { _size = value; TransformDirty = true; } }
        }

        private Vector3 _rotation;
        public Vector3 Rotation
        {
            get => _rotation;
            set { if (_rotation != value) { _rotation = value; TransformDirty = true; } }
        }

        // Objective 6: World matrix implementation
        public CustomMat4 WorldMatrix => GetModelMatrix();

        // Geometry
        public List<Vector3> Vertices { get; set; } = new();
        public List<Vector2> UVs { get; set; } = new();
        public List<Vector3> Normals { get; set; } = new();

        public List<Vector3> PolygonNormals { get; set; } = new();
        public List<Face> Faces { get; } = new();

        private int _nextPrimitiveIndex = 0;
        public int PrimitiveCount => _nextPrimitiveIndex;

        // Geometry counts
        public int VertexCount => Vertices.Count;
        public int UVCount => UVs.Count;
        public int NormalCount => Normals.Count;
        public int FaceCount => Faces.Count;


        // Edges
        public List<Edge> Edges { get; } = new();
        public int EdgeCount => Edges.Count;

        public bool HasRenderableFaces => Faces.Count > 0;

        public int TriangleFaceCount
        {
            get { RebuildTopologyCache(); return _triangleFaceCount; }
        }

        public int QuadFaceCount
        {
            get { RebuildTopologyCache(); return _quadFaceCount; }
        }


        public float GlowRadius { get; set; } = 1.0f;
        public float GlowStrength { get; set; } = 1.0f;

        // Glow color — separate from text color, controls mirror glow tint
        public Vector4 GlowColor { get; set; } = new Vector4(1f, 1f, 1f, 1f);

        // Shadow blur — CSS style soft feathered bloom behind text
        public float ShadowBlur { get; set; } = 0f;
        public Vector4 ShadowColor { get; set; } = new Vector4(0f, 0f, 0f, 0f);

        // Glow pulse — animates glow radius over time like CSS animation
        public float GlowPulseSpeed { get; set; } = 0f;
        public float GlowPulseMin { get; set; } = 0f;
        public float GlowPulseMax { get; set; } = 1f;

        public bool IsEmissive { get; set; } = false;
        public float EmissiveIntensity { get; set; } = 1.0f;




        // ── SDF Text Properties ──────────────────────────────────────────────────
        public bool IsSDFText { get; set; } = false;
        public string Text { get; set; } = "";
        public float FontSize { get; set; } = 1f;
        public string FontKey { get; set; } = "Diablo";
        public float LetterSpacing { get; set; } = 0f;
        public float LineHeight { get; set; } = 1.2f;
        public TextAlignment TextAlign { get; set; } = TextAlignment.Left;
        public Vector4 OutlineColor { get; set; } = new Vector4(0f, 0f, 0f, 0f);
        public float OutlineWidth { get; set; } = 0f;
        public bool TextDirty { get; set; } = true;

        public List<string> MaterialTextures { get; set; } = new();

        public List<Vector4> MaterialColors { get; set; } = new();
        public string? OverlayTextureDataUrl { get; set; }
        public float OverlayAlpha { get; set; }
        public bool OverlayDirty { get; set; }
        // Texture support
        public string? TexturePath { get; set; }
        public bool HasTexture => TextureData != null || TextureIsRawRGBA || TextureDataUrl != null;
        public Vector3[,]? TextureData { get; set; }
        public string? TextureDataUrl { get; set; }
        public int TextureWidth { get; set; }
        public int TextureHeight { get; set; }
        public bool TextureIsRawRGBA { get; set; }
        public bool TextureDirty { get; set; } = false;

        private int _triangleFaceCount;
        private int _quadFaceCount;
        private bool _topologyDirty = true;


        // Sprite sheet animation
        public bool IsAnimated { get; set; } = false;
        public int FrameCount { get; set; } = 1;
        public float FrameRate { get; set; } = 10f; // frames per second
        public int CurrentFrame { get; set; } = 0;
        public float FrameTimer { get; set; } = 0f;
        public float SheetWidth { get; set; } = 0f;
        public float SheetHeight { get; set; } = 0f;
        public float FramePixelWidth { get; set; } = 0f;
        public float FramePixelHeight { get; set; } = 0f;

        public float UVOffsetX { get; set; } = 0;
        public float UVOffsetY { get; set; } = 0;

        public float UVScaleX { get; set; } = 1f;
        public float UVScaleY { get; set; } = 1f;

        public bool FacingRight { get; set; } = true;
        /// <summary>
        /// Whether this mesh writes into shadow depth maps.
        /// Set false on light gizmos, auras, and UI geometry.
        /// </summary>

        public bool CastsShadow { get; set; } = true;
        public bool ReceivesShadow { get; set; } = true;  

        public string? JSSourceMesh { get; set; } = null;
        public SpectralXMesh(string name)
        {
            Name = name;
        }
        public CustomMat4 GetModelMatrix()
        {
            var s = CustomMat4.CreateScale(new CustomVec3(Size.X, Size.Y, Size.Z));
            var r = CustomMat4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
            var t = CustomMat4.CreateTranslation(new CustomVec3(Position.X, Position.Y, Position.Z));
            return t * r * s;
        }
        public bool IsValid()
        {
            if (Vertices.Count == 0) return false;
            if (Faces.Count == 0) return false;

            foreach (var face in Faces)
            {
                if (face.PrimitiveIndex < 0)
                    return false;

                if (face.A < 0 || face.A >= Vertices.Count) return false;
                if (face.B < 0 || face.B >= Vertices.Count) return false;
                if (face.C < 0 || face.C >= Vertices.Count) return false;

                if (face.Type == FaceType.Quad)
                {
                    if (face.D < 0 || face.D >= Vertices.Count)
                        return false;
                }
            }

            return true;
        }

        public IMesh Clone()
        {
            var clone = new SpectralXMesh(Name + "_Clone")
            {
                Color = Color,
                Position = Position,
                Size = Size,
                Rotation = Rotation,
                Vertices = new List<Vector3>(Vertices),
                UVs = new List<Vector2>(UVs),
                Normals = new List<Vector3>(Normals),
                PolygonNormals = new List<Vector3>(PolygonNormals),
                TexturePath = TexturePath,
                TextureData = TextureData,
                TextureDataUrl = TextureDataUrl,
                TextureWidth = TextureWidth,
                TextureHeight = TextureHeight,
                TextureIsRawRGBA = TextureIsRawRGBA,
                MaterialTextures = new List<string>(MaterialTextures),
                MaterialColors = new List<Vector4>(MaterialColors), 
                IsAnimated = IsAnimated,
                FrameCount = FrameCount,
                FrameRate = FrameRate,
                CurrentFrame = CurrentFrame,
                FrameTimer = FrameTimer,
                SheetWidth = SheetWidth,
                SheetHeight = SheetHeight,
                FramePixelWidth = FramePixelWidth,
                FramePixelHeight = FramePixelHeight,
                UVOffsetX= UVOffsetX,
                UVOffsetY= UVOffsetY,
                UVScaleX= UVScaleX,
                UVScaleY= UVScaleY,
                JSSourceMesh = JSSourceMesh,
                

            };
            foreach (var face in Faces) clone.Faces.Add(face);
            foreach (var edge in Edges) clone.Edges.Add(edge);
            return clone;
        }

        public (Vector3 min, Vector3 max) GetBounds()
        {
            if (Vertices.Count == 0)
                return (Vector3.Zero, Vector3.Zero);

            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);

            foreach (var v in Vertices)
            {
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }

            return (min, max);
        }

        public Vector3 GetCenter()
        {
            var (min, max) = GetBounds();
            return (min + max) * 0.5f;
        }

        public void AddTriangleFace(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            int startIdx = Vertices.Count;
            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);

            AddTriangleFace(startIdx, startIdx + 1, startIdx + 2);
        }

        public void AddQuadFace(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            int startIdx = Vertices.Count;
            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);

            AddQuadFace(startIdx, startIdx + 1, startIdx + 2, startIdx + 3);
        }
        public void AddTriangleFace(int a, int b, int c, int uvA, int uvB, int uvC)
        {
            Faces.Add(new Face
            {
                Type = FaceType.Triangle,
                A = a,
                B = b,
                C = c,
                D = -1,
                UVA = uvA,
                UVB = uvB,
                UVC = uvC,
                UVD = -1,
                PrimitiveIndex = _nextPrimitiveIndex++
            });

            Edges.Add(new Edge(a, b));
            Edges.Add(new Edge(b, c));
            Edges.Add(new Edge(c, a));

            _topologyDirty = true;
        }

        public void AddQuadFace(int a, int b, int c, int d, int uvA, int uvB, int uvC, int uvD)
        {
            Faces.Add(new Face
            {
                Type = FaceType.Quad,
                A = a,
                B = b,
                C = c,
                D = d,
                UVA = uvA,
                UVB = uvB,
                UVC = uvC,
                UVD = uvD,
                PrimitiveIndex = _nextPrimitiveIndex++
            });

            Edges.Add(new Edge(a, b));
            Edges.Add(new Edge(b, c));
            Edges.Add(new Edge(c, d));
            Edges.Add(new Edge(d, a));

            _topologyDirty = true;
        }
        public void AddTriangleFace(int a, int b, int c)
        {
            Faces.Add(new Face
            {
                Type = FaceType.Triangle,
                A = a,
                B = b,
                C = c,
                D = -1,
                UVA = -1,
                UVB = -1,
                UVC = -1,
                UVD = -1,  // explicit -1
                PrimitiveIndex = _nextPrimitiveIndex++
            });
            Edges.Add(new Edge(a, b));
            Edges.Add(new Edge(b, c));
            Edges.Add(new Edge(c, a));
            _topologyDirty = true;
        }

        public void AddQuadFace(int a, int b, int c, int d)
        {
            Faces.Add(new Face
            {
                Type = FaceType.Quad,
                A = a,
                B = b,
                C = c,
                D = d,
                UVA = -1,
                UVB = -1,
                UVC = -1,
                UVD = -1,  // explicit -1
                PrimitiveIndex = _nextPrimitiveIndex++
            });
            Edges.Add(new Edge(a, b));
            Edges.Add(new Edge(b, c));
            Edges.Add(new Edge(c, d));
            Edges.Add(new Edge(d, a));
            _topologyDirty = true;
        }

        private void RebuildTopologyCache()
        {
            if (!_topologyDirty) return;

            _triangleFaceCount = 0;
            _quadFaceCount = 0;

            foreach (var face in Faces)
            {
                if (face.Type == FaceType.Triangle) _triangleFaceCount++;
                else if (face.Type == FaceType.Quad) _quadFaceCount++;
            }

            _topologyDirty = false;
        }


        /// <summary>
        /// Call once after all faces are added — sorts by material index so
        /// BuildWebGLFrame never needs to sort per frame.
        /// </summary>
        public void SortFacesByMaterial()
        {
            var sorted = Faces.OrderBy(f => f.MaterialIndex).ToList();
            Faces.Clear();
            foreach (var f in sorted)
                Faces.Add(f);
        }
    }
}
