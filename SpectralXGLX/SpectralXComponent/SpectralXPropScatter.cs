using SpectralXGLX.SpectralGL.Math;
using System.Numerics;

namespace SpectralXGLX.SpectralXComponent
{
    /// <summary>
    /// Defines a circular exclusion zone on the XY ground plane.
    /// Buildings register these so props don't spawn on top of them.
    /// </summary>
    public struct PropExclusionZone
    {
        public float X;
        public float Y;
        public float Radius;

        public PropExclusionZone(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public bool Overlaps(float px, float py, float propRadius = 0.5f)
        {
            float dx = px - X;
            float dy = py - Y;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            return dist < (Radius + propRadius);
        }
    }

    /// <summary>
    /// Configuration for a randomly scattered prop type across the full tilemap.
    /// </summary>
    public struct PropScatterConfig
    {
        public string MeshId;
        public int Count;
        public float MinScale;
        public float MaxScale;
        public float MinRotationZ;
        public float MaxRotationZ;
        public float BoundingRadius;   // NEW — mesh-space radius at scale 1.0, used for frustum culling

        public float[] Color;

        public PropScatterConfig(string meshId, int count,
            float minScale = 0.5f, float maxScale = 2.0f, float boundingRadius = 2.0f,
            float[]? color = null)
        {
            MeshId = meshId;
            Count = count;
            MinScale = minScale;
            MaxScale = maxScale;
            MinRotationZ = 0f;
            MaxRotationZ = MathF.PI * 2f;
            BoundingRadius = boundingRadius;
            Color = color ?? new float[] { 1f, 1f, 1f, 1f };
        }
    }

    /// <summary>
    /// Configuration for a grid-bounded scatter — e.g. gravestones inside castle walls.
    /// Spawns only within the defined XY rectangle.
    /// </summary>
    public struct GridBoundedScatterConfig
    {
        public string MeshId;
        public int Count;
        public float MinScale;
        public float MaxScale;
        public float MinRotationZ;
        public float MaxRotationZ;
        public float BoundsMinX;
        public float BoundsMaxX;
        public float BoundsMinY;
        public float BoundsMaxY;
        public float BoundingRadius;   // NEW — mesh-space radius at scale 1.0, used for frustum culling
        public float[] Color;

        public GridBoundedScatterConfig(
            string meshId, int count,
            float boundsMinX, float boundsMaxX,
            float boundsMinY, float boundsMaxY,
            float minScale = 0.5f, float maxScale = 2.0f,
            float boundingRadius = 2.0f,
            float[]? color = null)
        {
            MeshId = meshId;
            Count = count;
            MinScale = minScale;
            MaxScale = maxScale;
            MinRotationZ = 0f;
            MaxRotationZ = MathF.PI * 2f;
            BoundsMinX = boundsMinX;
            BoundsMaxX = boundsMaxX;
            BoundsMinY = boundsMinY;
            BoundsMaxY = boundsMaxY;
            BoundingRadius = boundingRadius;
            Color = color ?? new float[] { 1f, 1f, 1f, 1f };
        }
    }



    /// <summary>
    /// Handles random prop placement across the tilemap while respecting
    /// building exclusion zones. Returns FoliageInstanceGroups for instanced
    /// rendering — no individual mesh scene entries needed.
    /// Call Reset() on scene switch to reuse the same instance.
    /// </summary>
    public class SpectralXPropScatter
    {
        // Tilemap is 512x512 centered at 0,0
        public const float MapMin = -256f;
        public const float MapMax = 256f;

        // Max placement attempts per prop before giving up
        private const int MaxAttempts = 50;

        private readonly List<PropExclusionZone> _exclusionZones = new();
        private readonly List<FoliageInstanceGroup> _groups = new();
        private Random _rng;

        public IReadOnlyList<FoliageInstanceGroup> Groups => _groups;

        public SpectralXPropScatter(int seed = 42)
        {
           // _rng = new Random(seed);
        }

        /// <summary>
        /// Clear state for scene switch reuse — avoids full reconstruction.
        /// Call this at the start of InitScene2 instead of new SpectralXPropScatter().
        /// </summary>
        public void Reset()
        {
            _exclusionZones.Clear();
            _groups.Clear();
            _rng = new Random(); // new random seed every reset
        }

        /// <summary>
        /// Register a building footprint as an exclusion zone.
        /// Call this for every building placed in InitScene2.
        /// </summary>
        public void RegisterFootprint(float worldX, float worldY, float radius)
        {
            _exclusionZones.Add(new PropExclusionZone(worldX, worldY, radius));
        }

        /// <summary>
        /// Derive a footprint radius from a mesh.
        /// Uses XY vertex extents if C# geometry is available,
        /// falls back to max of Size.X/Y for JS-uploaded meshes.
        /// </summary>
        public static float DeriveFootprintRadius(IMesh mesh)
        {
            if (mesh is SpectralXRender.SpectralXMesh sm && sm.Vertices.Count > 0)
            {
                float minX = float.MaxValue, maxX = float.MinValue;
                float minY = float.MaxValue, maxY = float.MinValue;

                foreach (var v in sm.Vertices)
                {
                    if (v.X < minX) minX = v.X;
                    if (v.X > maxX) maxX = v.X;
                    if (v.Y < minY) minY = v.Y;
                    if (v.Y > maxY) maxY = v.Y;
                }

                float halfW = (maxX - minX) * 0.5f * mesh.Size.X;
                float halfH = (maxY - minY) * 0.5f * mesh.Size.Y;
                return MathF.Max(halfW, halfH);
            }

            // Fallback for JS-uploaded meshes
            return MathF.Max(mesh.Size.X, mesh.Size.Y) * 0.5f;
        }
        public float[] Color { get; set; } = new float[] { 1f, 1f, 1f, 1f };
        /// <summary>
        /// Scatter props randomly across the full 512x512 tilemap.
        /// Bakes all transforms into a FoliageInstanceGroup.
        /// Group is stored internally and accessible via Groups property.
        /// </summary>
        public FoliageInstanceGroup Scatter(PropScatterConfig config)
        {
            var positions = new List<float>();
            var scales = new List<float>();
            var rotations = new List<float>();
            int spawned = 0;

            for (int i = 0; i < config.Count; i++)
            {
                if (!TryFindPosition(
                    MapMin, MapMax, MapMin, MapMax,
                    out float x, out float y)) continue;

                positions.Add(x);
                positions.Add(y);
                positions.Add(0f);
                scales.Add(Lerp(config.MinScale, config.MaxScale,
                    (float)_rng.NextDouble()));
                rotations.Add(Lerp(config.MinRotationZ, config.MaxRotationZ,
                    (float)_rng.NextDouble()));
                spawned++;
            }

            var group = BuildGroup(config.MeshId, spawned,
      positions, scales, rotations, config.BoundingRadius, config.Color);
            _groups.Add(group);
            return group;
        }

        /// <summary>
        /// Scatter props within a defined XY grid rectangle.
        /// For bounded spawning like gravestones inside castle walls.
        /// </summary>
        public FoliageInstanceGroup ScatterInGrid(GridBoundedScatterConfig config)
        {
            var positions = new List<float>();
            var scales = new List<float>();
            var rotations = new List<float>();
            int spawned = 0;

            for (int i = 0; i < config.Count; i++)
            {
                if (!TryFindPosition(
                    config.BoundsMinX, config.BoundsMaxX,
                    config.BoundsMinY, config.BoundsMaxY,
                    out float x, out float y)) continue;

                positions.Add(x);
                positions.Add(y);
                positions.Add(0f);
                scales.Add(Lerp(config.MinScale, config.MaxScale,
                    (float)_rng.NextDouble()));
                rotations.Add(Lerp(config.MinRotationZ, config.MaxRotationZ,
                    (float)_rng.NextDouble()));
                spawned++;
            }

            var group = BuildGroup(config.MeshId, spawned,
            positions, scales, rotations, config.BoundingRadius, config.Color);
            _groups.Add(group);
            return group;
        }

        // ── Private Helpers ──────────────────────────────────────────────────

        private static FoliageInstanceGroup BuildGroup(
       string meshId, int count,
       List<float> positions,
       List<float> scales,
       List<float> rotations,
       float boundingRadius,
       float[] color)
        {
            return new FoliageInstanceGroup
            {
                MeshId = meshId,
                TexKey = meshId,
                Count = count,
                Positions = positions.ToArray(),
                Scales = scales.ToArray(),
                Rotations = rotations.ToArray(),
                IsStatic = true,
                Uploaded = false,
                BoundingRadius = boundingRadius,
                Color = color,
            };
        }

        private bool TryFindPosition(
            float minX, float maxX,
            float minY, float maxY,
            out float x, out float y)
        {
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                float cx = Lerp(minX, maxX, (float)_rng.NextDouble());
                float cy = Lerp(minY, maxY, (float)_rng.NextDouble());
                Console.WriteLine($"[Scatter] attempt pos: {cx:F1}, {cy:F1} blocked: {IsBlocked(cx, cy)}");
                if (!IsBlocked(cx, cy))
                {
                    x = cx;
                    y = cy;
                    return true;
                }
            }

            x = 0f;
            y = 0f;
            return false;
        }

        private bool IsBlocked(float x, float y)
        {
            foreach (var zone in _exclusionZones)
            {
                if (zone.Overlaps(x, y))
                    return true;
            }
            return false;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}