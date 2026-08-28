using System.Numerics;

namespace SpectralXGLX.SpectralXComponent
{
    // ── Tile Material Indices ────────────────────────────────────────────────
    public enum TileMaterial
    {
        Dirt = 0,
        Rock = 1,
        Grass = 2,
        Snow = 3,
        Water = 4,
        Ice = 5
    }

    // ── Paint Mode ───────────────────────────────────────────────────────────
    public enum TilePaintMode
    {
        PaintMaterial,
        RaiseTopology,
        LowerTopology,
        SmoothTopology,
        FlattenTopology
    }

    // ── Per-Tile Data ────────────────────────────────────────────────────────
    public struct TileData
    {
        // Primary material — which texture this tile shows
        public int MaterialIndex;

        // Secondary material for edge blending
        public int BlendMaterial;

        // Blend weight toward BlendMaterial (0 = pure primary, 1 = pure blend)
        public float BlendWeight;

        // Z height for topology sculpting
        public float Height;

        // Recalculated world-space normal — updated after any topology change
        public Vector3 Normal;

        public TileData()
        {
            MaterialIndex = (int)TileMaterial.Grass;
            BlendMaterial = (int)TileMaterial.Grass;
            BlendWeight = 0f;
            Height = 0f;
            Normal = Vector3.UnitZ; // Z = up in XYZ
        }
    }



    // ── Main TileMap Class ───────────────────────────────────────────────────
    public class SpectralXLandTileMap
    {
        private float[]? _cachedHeights;
        private float[]? _cachedNormals;
        private int[]? _cachedMaterials;
        private float[]? _cachedBlendWeights;
        private int[]? _cachedBlendMats;
        // _smoothSnapshot sized dynamically in Init() — TileCount is now instance property
        private float[] _smoothSnapshot = Array.Empty<float>();

        // Dirty region tracking — only send changed rectangle to JS
        private int _dirtyMinX = int.MaxValue, _dirtyMinY = int.MaxValue;
        private int _dirtyMaxX = int.MinValue, _dirtyMaxY = int.MinValue;
        private bool _fullUploadPending = true; // force full on first frame

        // Shared vertex corner heights — sized dynamically in Init()
        // Cannot use readonly + inline init because GridSize is now an instance property
        private float[] _vertexHeights = Array.Empty<float>();
        private float[] _vertexSnapshot = Array.Empty<float>();

        // ── Constants ───────────────────────────────────────────────────────────
        // GridSize is now per-instance — Scene4 uses 128, Scene2 uses 512 (default)
        public int GridSize { get; private set; } = 512;
        public int TileCount => GridSize * GridSize;
        public const float TileSize = 1.0f; // 1 metre per tile in world space

        // Origin offset — recomputed from instance GridSize
        private float GridOriginX => -(GridSize * TileSize) / 2f;
        private float GridOriginY => -(GridSize * TileSize) / 2f;

        // ── Tile Data Array ──────────────────────────────────────────────────
        // Sized dynamically in Init() — GridSize is now instance property
        private TileData[] _tiles = Array.Empty<TileData>();

        // ── Dirty Tracking ───────────────────────────────────────────────────
        private bool _isDirty = true;

        // ── UI State ─────────────────────────────────────────────────────────
        public TileMaterial ActiveMaterial { get; set; } = TileMaterial.Grass;
        public TilePaintMode PaintMode { get; set; } = TilePaintMode.PaintMaterial;
        public int BrushSize { get; set; } = 1; // radius in tiles
        public float TopologyStrength { get; set; } = 0.25f;
        public float FlattenTargetHeight { get; set; } = 0f;
        public float BlendStrength { get; set; } = 0.4f; // 0 = hard edge, 1 = maximum blend zone
        public float BrushWorldX { get; set; } = 0f;
        public float BrushWorldY { get; set; } = 0f;
        public bool IsActive { get; set; } = false;

        // Set true each frame material paint fires — GPU reads this to trigger brush stamp
        public bool IsMousePainting { get; set; } = false;

        // ── Texture Paths — sent to JS for GPU upload ────────────────────────
        // Static fallback used by Scene2 (512 grid, unchanged)
        public static readonly string[] TexturePaths = new[]
        {
    "/iAssets/DirtTile002.png",
    "/iAssets/RockTile002.png",
    "/iAssets/GrassTile011.png",
    "/iAssets/SnowTile012.png",
    "/iAssets/WaterTile002.png",
    "/iAssets/IceTile002.png",
};

        // Per-instance override — set before Init() for custom scenes
        public string[]? CustomTexturePaths { get; set; } = null;
        public string[] EffectiveTexturePaths => CustomTexturePaths ?? TexturePaths;

        // ── PBR Texture Paths — per material slot, all nullable ──────────────
        // Set CustomXxxMapPaths before Init() — null slots fall back to scalar values
        // Normal maps — tangent-space RGB normal textures
        public string?[]? CustomNormalMapPaths { get; set; } = null;
        public static readonly string?[] DefaultNormalMapPaths = new string?[6];
        public string?[] EffectiveNormalMapPaths => CustomNormalMapPaths ?? DefaultNormalMapPaths;

        // Roughness maps — greyscale, 0=smooth 1=rough
        public string?[]? CustomRoughnessMapPaths { get; set; } = null;
        public static readonly string?[] DefaultRoughnessMapPaths = new string?[6];
        public string?[] EffectiveRoughnessMapPaths => CustomRoughnessMapPaths ?? DefaultRoughnessMapPaths;

        // Metallic maps — greyscale, 0=dielectric 1=conductor
        public string?[]? CustomMetallicMapPaths { get; set; } = null;
        public static readonly string?[] DefaultMetallicMapPaths = new string?[6];
        public string?[] EffectiveMetallicMapPaths => CustomMetallicMapPaths ?? DefaultMetallicMapPaths;

        // Ambient Occlusion maps — greyscale, 0=occluded 1=open
        public string?[]? CustomAOMapPaths { get; set; } = null;
        public static readonly string?[] DefaultAOMapPaths = new string?[6];
        public string?[] EffectiveAOMapPaths => CustomAOMapPaths ?? DefaultAOMapPaths;

        // Specular maps — greyscale or RGB specular intensity/color
        public string?[]? CustomSpecularMapPaths { get; set; } = null;
        public static readonly string?[] DefaultSpecularMapPaths = new string?[6];
        public string?[] EffectiveSpecularMapPaths => CustomSpecularMapPaths ?? DefaultSpecularMapPaths;

        // Emissive maps — RGB additive glow textures
        public string?[]? CustomEmissiveMapPaths { get; set; } = null;
        public static readonly string?[] DefaultEmissiveMapPaths = new string?[6];
        public string?[] EffectiveEmissiveMapPaths => CustomEmissiveMapPaths ?? DefaultEmissiveMapPaths;

        // Displacement maps — greyscale height used for Parallax Occlusion Mapping
        public string?[]? CustomDisplacementMapPaths { get; set; } = null;
        public static readonly string?[] DefaultDisplacementMapPaths = new string?[6];
        public string?[] EffectiveDisplacementMapPaths => CustomDisplacementMapPaths ?? DefaultDisplacementMapPaths;

        // ── PBR Scalar Parameters — fallback when no texture is set ──────────
        // One value per material slot (6 slots: Dirt Rock Grass/Snow Snow2 Water Ice)
        // Roughness: 0.0=mirror smooth, 1.0=fully rough/diffuse
        public float[] RoughnessValues { get; set; } = new[] { 0.9f, 0.8f, 0.4f, 0.5f, 0.1f, 0.2f };

        // Metallic: 0.0=dielectric(plastic/stone), 1.0=conductor(metal)
        public float[] MetallicValues { get; set; } = new[] { 0.0f, 0.0f, 0.5f, 0.3f, 0.0f, 0.4f };

        // Ambient Occlusion: 1.0=fully lit, 0.0=fully occluded
        public float[] AOValues { get; set; } = new[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };

        // Specular: 0.0=no specular response, 1.0=full specular
        public float[] SpecularValues { get; set; } = new[] { 0.3f, 0.4f, 0.7f, 0.6f, 0.8f, 0.9f };

        // Emissive Intensity: 0.0=no glow, >0=additive emissive multiplier
        public float[] EmissiveIntensityValues { get; set; } = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

        // Displacement Strength: 0.0=no parallax, 0.02-0.08=typical range
        public float[] DisplacementStrengthValues { get; set; } = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

        // Parallax Scale: controls depth of parallax occlusion mapping UV offset
        public float[] ParallaxScaleValues { get; set; } = new[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };

        // ── Material Display Names ───────────────────────────────────────────
        public static readonly string[] MaterialNames = new[]
        {
            "Dirt", "Rock", "Grass", "Snow", "Water", "Ice"
        };


        // ── Grid Size Configuration — call BEFORE Init() ─────────────────────
        // Safe to call multiple times — only takes effect on next Init()
        public void SetGridSize(int size)
        {
            if (size <= 0 || (size & (size - 1)) != 0)
            {
                Console.WriteLine($"[TileMap] WARNING: GridSize {size} is not power of 2 — using 128");
                size = 128;
            }
            GridSize = size;
            Console.WriteLine($"[TileMap] GridSize set to {GridSize}");
        }

        // ── Init ─────────────────────────────────────────────────────────────
        public void Init()
        {
            // Allocate arrays from current GridSize — must be called after SetGridSize()
            int vertCount = (GridSize + 1) * (GridSize + 1);
            _vertexHeights = new float[vertCount];
            _vertexSnapshot = new float[vertCount];
            _smoothSnapshot = new float[TileCount];

            // Re-allocate tile array if grid size changed
            // Allocate tile array to current TileCount
            if (_tiles == null || _tiles.Length != TileCount)
            {
                _tiles = new TileData[TileCount];
            }

            for (int i = 0; i < TileCount; i++)
            {
                _tiles[i] = new TileData
                {
                    MaterialIndex = (int)TileMaterial.Grass,
                    BlendMaterial = (int)TileMaterial.Grass,
                    BlendWeight = 0f,
                    Height = 0f,
                    Normal = Vector3.UnitZ
                };
            }

            Array.Clear(_vertexHeights, 0, _vertexHeights.Length);
            _fullUploadPending = true;
            _isDirty = true;

            Console.WriteLine($"[TileMap] Init complete — GridSize:{GridSize} TileCount:{TileCount} VertCount:{vertCount}");
        }

        // ── Tile Index Helpers ────────────────────────────────────────────────
        // Non-static now — needs instance GridSize
        public int TileIndex(int x, int y) => y * GridSize + x;

        // Non-static now — needs instance GridSize
        public (int x, int y) TileCoord(int index) =>
            (index % GridSize, index / GridSize);

        // Non-static now — needs instance GridSize
        public bool InBounds(int x, int y) =>
            x >= 0 && x < GridSize && y >= 0 && y < GridSize;
        // Non-static now — needs instance GridSize
        public int VertexIndex(int x, int y) => y * (GridSize + 1) + x;

        // ── World Position → Tile Coordinate ─────────────────────────────────
        // Call from engine when mouse ray hits Z=0 plane
        public (int tileX, int tileY, bool hit) WorldToTile(float worldX, float worldY)
        {
            float localX = worldX - GridOriginX;
            float localY = worldY - GridOriginY;

            int tileX = (int)MathF.Floor(localX / TileSize);
            int tileY = (int)MathF.Floor(localY / TileSize);

            if (!InBounds(tileX, tileY))
                return (0, 0, false);

            return (tileX, tileY, true);
        }

        // ── Tile World Position ───────────────────────────────────────────────
        public Vector3 TileWorldPosition(int x, int y)
        {
            float wx = GridOriginX + x * TileSize + TileSize * 0.5f;
            float wy = GridOriginY + y * TileSize + TileSize * 0.5f;
            float wz = _tiles[TileIndex(x, y)].Height;
            return new Vector3(wx, wy, wz);
        }

        // ── Paint Stroke — called on mouse drag ──────────────────────────────
        public void Paint(int centerX, int centerY)
        {
            if (!IsActive) return;

            // Signal GPU to stamp brush this frame — only material paint triggers the splat pass
            IsMousePainting = PaintMode == TilePaintMode.PaintMaterial;
            switch (PaintMode)
            {
                case TilePaintMode.PaintMaterial:
                    PaintMaterial(centerX, centerY);
                    break;
                case TilePaintMode.RaiseTopology:
                    AdjustHeight(centerX, centerY, +TopologyStrength);
                    break;
                case TilePaintMode.LowerTopology:
                    AdjustHeight(centerX, centerY, -TopologyStrength);
                    break;
                case TilePaintMode.SmoothTopology:
                    SmoothHeight(centerX, centerY);
                    break;
                case TilePaintMode.FlattenTopology:
                    FlattenHeight(centerX, centerY);
                    break;
            }

            RecalculateNormalsScoped(centerX, centerY);
            MarkDirtyRegion(centerX, centerY, BrushSize);
        }

        // ── Material Painting ─────────────────────────────────────────────────
        private void PaintMaterial(int cx, int cy)
        {
            ForEachInBrush(cx, cy, (x, y, falloff) =>
            {
                int idx = TileIndex(x, y);
                var tile = _tiles[idx];

                // C# tracks the winning material index only — for save/load seeding
                // Blend weights and soft transitions are owned by the GPU splat map
                tile.MaterialIndex = (int)ActiveMaterial;
                tile.BlendMaterial = (int)ActiveMaterial;
                tile.BlendWeight = 0f;

                _tiles[idx] = tile;
            });
        }

        // ── Height Adjustment ─────────────────────────────────────────────────
        private void AdjustHeight(int cx, int cy, float delta)
        {
            ForEachInBrush(cx, cy, (x, y, falloff) =>
            {
                // Write to all 4 corners of this tile
                // Guard vertex coords against GridSize boundary
                int[] vx = { x, x + 1, x, x + 1 };
                int[] vy = { y, y, y + 1, y + 1 };
                for (int c = 0; c < 4; c++)
                {
                    // Clamp vertex coords to valid range (0 to GridSize inclusive)
                    if (vx[c] < 0 || vx[c] > GridSize) continue;
                    if (vy[c] < 0 || vy[c] > GridSize) continue;
                    int vi = VertexIndex(vx[c], vy[c]);
                    if (vi < 0 || vi >= _vertexHeights.Length) continue;
                    _vertexHeights[vi] = Math.Clamp(
                        _vertexHeights[vi] + delta * falloff, -5f, 10f);
                }
                SyncTileHeight(x, y);
            });
        }

        // ── Smooth Height ─────────────────────────────────────────────────────
        private void SmoothHeight(int cx, int cy)
        {
            // Snapshot vertex heights before smoothing
            // Guard array length — snapshot must match current vertexHeights size
            if (_vertexSnapshot.Length != _vertexHeights.Length)
                _vertexSnapshot = new float[_vertexHeights.Length];

            Array.Copy(_vertexHeights, _vertexSnapshot, _vertexHeights.Length);

            ForEachInBrush(cx, cy, (x, y, falloff) =>
            {
                int[] vx = { x, x + 1, x, x + 1 };
                int[] vy = { y, y, y + 1, y + 1 };
                for (int c = 0; c < 4; c++)
                {
                    int pvx = vx[c], pvy = vy[c];
                    // Guard vertex coords against GridSize boundary
                    if (pvx < 0 || pvx > GridSize) continue;
                    if (pvy < 0 || pvy > GridSize) continue;
                    int baseVi = VertexIndex(pvx, pvy);
                    if (baseVi < 0 || baseVi >= _vertexSnapshot.Length) continue;

                    float sum = _vertexSnapshot[baseVi];
                    int count = 1;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = pvx + dx, ny = pvy + dy;
                            if (nx < 0 || nx > GridSize || ny < 0 || ny > GridSize) continue;
                            int nvi = VertexIndex(nx, ny);
                            if (nvi < 0 || nvi >= _vertexSnapshot.Length) continue;
                            sum += _vertexSnapshot[nvi];
                            count++;
                        }
                    float smoothed = sum / count;
                    int vi = VertexIndex(pvx, pvy);
                    if (vi < 0 || vi >= _vertexHeights.Length) continue;
                    _vertexHeights[vi] = Lerp(_vertexHeights[vi], smoothed, falloff * TopologyStrength);
                }
                SyncTileHeight(x, y);
            });
        }

        // ── Flatten Height ────────────────────────────────────────────────────
        private void FlattenHeight(int cx, int cy)
        {
            ForEachInBrush(cx, cy, (x, y, falloff) =>
            {
                int[] vx = { x, x + 1, x, x + 1 };
                int[] vy = { y, y, y + 1, y + 1 };
                for (int c = 0; c < 4; c++)
                {
                    // Guard vertex coords against GridSize boundary
                    if (vx[c] < 0 || vx[c] > GridSize) continue;
                    if (vy[c] < 0 || vy[c] > GridSize) continue;
                    int vi = VertexIndex(vx[c], vy[c]);
                    if (vi < 0 || vi >= _vertexHeights.Length) continue;
                    _vertexHeights[vi] = Lerp(
                        _vertexHeights[vi], FlattenTargetHeight,
                        falloff * TopologyStrength * 2f);
                }
                SyncTileHeight(x, y);
            });
        }

        private void SyncTileHeight(int x, int y)
        {
            int idx = TileIndex(x, y);
            var tile = _tiles[idx];
            tile.Height = (
                _vertexHeights[VertexIndex(x, y)] +
                _vertexHeights[VertexIndex(x + 1, y)] +
                _vertexHeights[VertexIndex(x, y + 1)] +
                _vertexHeights[VertexIndex(x + 1, y + 1)]) * 0.25f;
            _tiles[idx] = tile;
        }

        // ── Normal Recalculation ──────────────────────────────────────────────
        // Uses central difference across neighbours for smooth cross-tile normals
        private void RecalculateNormals()
        {
            for (int y = 0; y < GridSize; y++)
                for (int x = 0; x < GridSize; x++)
                {
                    // Sample heights of cardinal neighbours, clamp at borders
                    float hL = GetHeight(x - 1, y);
                    float hR = GetHeight(x + 1, y);
                    float hD = GetHeight(x, y - 1);
                    float hU = GetHeight(x, y + 1);

                    // Central difference gradient
                    float dX = (hR - hL) / (2f * TileSize);
                    float dY = (hU - hD) / (2f * TileSize);

                    // Normal from gradient — XYZ space, Z = up
                    var normal = Vector3.Normalize(new Vector3(-dX, -dY, 1f));

                    int idx = TileIndex(x, y);
                    var tile = _tiles[idx];
                    tile.Normal = normal;
                    _tiles[idx] = tile;
                }
        }

        private void RecalculateNormalsScoped(int cx, int cy)
        {
            // Only recalculate tiles within brush radius + 1 padding
            // The +1 ensures neighbours used in central difference are also updated
            int radius = BrushSize + 1;

            int minX = Math.Max(0, cx - radius);
            int maxX = Math.Min(GridSize - 1, cx + radius);
            int minY = Math.Max(0, cy - radius);
            int maxY = Math.Min(GridSize - 1, cy + radius);

            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float hL = GetHeight(x - 1, y);
                    float hR = GetHeight(x + 1, y);
                    float hD = GetHeight(x, y - 1);
                    float hU = GetHeight(x, y + 1);

                    float dX = (hR - hL) / (2f * TileSize);
                    float dY = (hU - hD) / (2f * TileSize);

                    var normal = Vector3.Normalize(new Vector3(-dX, -dY, 1f));

                    int idx = TileIndex(x, y);
                    var tile = _tiles[idx];
                    tile.Normal = normal;
                    _tiles[idx] = tile;
                }
        }











        private float GetHeight(int x, int y)
        {
            if (!InBounds(x, y)) return 0f;
            return _tiles[TileIndex(x, y)].Height;
        }

        // ── Brush Iterator ────────────────────────────────────────────────────
        // Calls action for every tile within BrushSize radius of center
        // falloff = 1.0 at center, 0.0 at edge — smooth circular falloff
        private void ForEachInBrush(int cx, int cy, Action<int, int, float> action)
        {
            int radius = Math.Max(0, BrushSize);

            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (!InBounds(x, y)) continue;

                    // Circular test: include tile when its center is inside the radius
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    if (dist > radius) continue;

                    // Smooth falloff: 1.0 at center, 0.0 at radius
                    float falloff = radius > 0
                        ? 1f - Math.Clamp(dist / radius, 0f, 1f)
                        : 1f;

                    // Smooth the falloff curve (ease in/out)
                    falloff = falloff * falloff * (3f - 2f * falloff);

                    action(x, y, falloff);
                }
        }

        // ── Frame Data Builder ────────────────────────────────────────────────
        // Called from BuildWebGLFrame — only sends data when dirty
        public TileMapFrameData? BuildFrameData(bool forceFullUpload = false)
        {
            if (!_isDirty && !forceFullUpload) return null;
            Console.WriteLine($"[TileMap] BuildFrameData firing — GridSize:{GridSize} full:{_fullUploadPending}");

            _isDirty = false;

            bool sendFull = _fullUploadPending || forceFullUpload;
            _fullUploadPending = false;

            int vertCount = (GridSize + 1) * (GridSize + 1);

            // Reallocate cache arrays if GridSize changed since last build
            if (_cachedHeights == null || _cachedHeights.Length != vertCount)
            {
                _cachedHeights = new float[vertCount];
                _cachedNormals = new float[TileCount * 3];
                _cachedMaterials = new int[TileCount];
                _cachedBlendWeights = new float[TileCount];
                _cachedBlendMats = new int[TileCount];
                Console.WriteLine($"[TileMap] Cache arrays reallocated — vertCount:{vertCount} tileCount:{TileCount}");
            }

            if (sendFull)
            {
                // Full upload — init or force
                Array.Copy(_vertexHeights, _cachedHeights, vertCount);

                for (int y = 0; y < GridSize; y++)
                    for (int x = 0; x < GridSize; x++)
                    {
                        int idx = TileIndex(x, y);
                        var tile = _tiles[idx];
                        _cachedNormals[idx * 3] = tile.Normal.X;
                        _cachedNormals[idx * 3 + 1] = tile.Normal.Y;
                        _cachedNormals[idx * 3 + 2] = tile.Normal.Z;
                        _cachedMaterials[idx] = tile.MaterialIndex;
                        _cachedBlendWeights[idx] = tile.BlendWeight;
                        _cachedBlendMats[idx] = tile.BlendMaterial;
                    }

                // Reset dirty region to full after full upload
                _dirtyMinX = int.MaxValue; _dirtyMinY = int.MaxValue;
                _dirtyMaxX = int.MinValue; _dirtyMaxY = int.MinValue;

                return new TileMapFrameData
                {
                    TileGridSize = GridSize,
                    Heights = _cachedHeights,
                    Normals = Array.Empty<float>(), // JS computes from heights
                    Materials = _cachedMaterials,
                    BlendWeights = _cachedBlendWeights,
                    BlendMaterials = _cachedBlendMats,
                    IsDirty = true,
                    IsFullUpload = true,
                    DirtyX = 0,
                    DirtyY = 0,
                    DirtyW = GridSize,
                    DirtyH = GridSize,
                };
            }
            else
            {
                // Partial upload — only dirty rectangle
                int minX = _dirtyMinX, minY = _dirtyMinY;
                int maxX = _dirtyMaxX, maxY = _dirtyMaxY;

                // Guard — if dirty region was never set, force full upload instead
                if (minX == int.MaxValue || maxX == int.MinValue || minX > maxX || minY > maxY)
                {
                    Console.WriteLine("[TileMap] Partial upload guard — dirty rect invalid, forcing full");
                    _fullUploadPending = true;
                    _isDirty = true;
                    return BuildFrameData(false);
                }

                int w = maxX - minX + 1;
                int h = maxY - minY + 1;

                // Vertex region — one extra border for corner verts
                int vMinX = Math.Max(0, minX);
                int vMinY = Math.Max(0, minY);
                int vMaxX = Math.Min(GridSize, maxX + 1);
                int vMaxY = Math.Min(GridSize, maxY + 1);
                int vW = vMaxX - vMinX + 1;
                int vH = vMaxY - vMinY + 1;

                var partialHeights = new float[vW * vH];
                for (int y = vMinY; y <= vMaxY; y++)
                    for (int x = vMinX; x <= vMaxX; x++)
                        partialHeights[(y - vMinY) * vW + (x - vMinX)] =
                            _vertexHeights[VertexIndex(x, y)];

                var partialNormals = new float[w * h * 3];
                var partialMats = new int[w * h];
                var partialBlend = new float[w * h];
                var partialBlendMat = new int[w * h];

                for (int y = minY; y <= maxY; y++)
                    for (int x = minX; x <= maxX; x++)
                    {
                        int src = TileIndex(x, y);
                        int dst = (y - minY) * w + (x - minX);
                        var tile = _tiles[src];
                        partialNormals[dst * 3] = tile.Normal.X;
                        partialNormals[dst * 3 + 1] = tile.Normal.Y;
                        partialNormals[dst * 3 + 2] = tile.Normal.Z;
                        partialMats[dst] = tile.MaterialIndex;
                        partialBlend[dst] = tile.BlendWeight;
                        partialBlendMat[dst] = tile.BlendMaterial;
                    }

                // Reset dirty region
                _dirtyMinX = int.MaxValue; _dirtyMinY = int.MaxValue;
                _dirtyMaxX = int.MinValue; _dirtyMaxY = int.MinValue;

                return new TileMapFrameData
                {
                    TileGridSize = GridSize,
                    Heights = partialHeights,
                    Normals = partialNormals,
                    Materials = partialMats,
                    BlendWeights = partialBlend,
                    BlendMaterials = partialBlendMat,
                    IsDirty = true,
                    IsFullUpload = false,
                    DirtyX = minX,
                    DirtyY = minY,
                    DirtyW = w,
                    DirtyH = h,
                };
            }
        }








        // ── Public Tile Read ──────────────────────────────────────────────────
        public TileData GetTile(int x, int y) => _tiles[TileIndex(x, y)];
        public TileData GetTile(int index) => _tiles[index];

        // ── Force Dirty — call after any external change ──────────────────────
        public void MarkDirty() => _isDirty = true;

        public void MarkDirtyRegion(int cx, int cy, int radius)
        {
            int pad = radius + 1; // previously +2
            _dirtyMinX = Math.Max(0, Math.Min(_dirtyMinX, cx - pad));
            _dirtyMinY = Math.Max(0, Math.Min(_dirtyMinY, cy - pad));
            _dirtyMaxX = Math.Min(GridSize - 1, Math.Max(_dirtyMaxX, cx + pad));
            _dirtyMaxY = Math.Min(GridSize - 1, Math.Max(_dirtyMaxY, cy + pad));
            _isDirty = true;
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static float Lerp(float a, float b, float t) =>
            a + (b - a) * Math.Clamp(t, 0f, 1f);

        public LandscapeSaveData ExportSaveData()
        {
            int vertCount = (GridSize + 1) * (GridSize + 1);

            // Guard — if Init() was never called arrays may be empty
            if (_vertexHeights.Length == 0 || _tiles.Length == 0)
            {
                Console.WriteLine("[TileMap] ExportSaveData called before Init() — returning empty");
                return new LandscapeSaveData();
            }

            var heights = new float[vertCount];
            Array.Copy(_vertexHeights, heights, Math.Min(vertCount, _vertexHeights.Length));

            var materials = new int[TileCount];
            var blendWeights = new float[TileCount];
            var blendMats = new int[TileCount];

            for (int i = 0; i < TileCount; i++)
            {
                materials[i] = _tiles[i].MaterialIndex;
                blendWeights[i] = _tiles[i].BlendWeight;
                blendMats[i] = _tiles[i].BlendMaterial;
            }

            Console.WriteLine($"[TileMap] ExportSaveData — GridSize:{GridSize} vertCount:{vertCount}");

            return new LandscapeSaveData
            {
                Heights = heights,
                Materials = materials,
                BlendWeights = blendWeights,
                BlendMats = blendMats,

                // Save current PBR scalars so they restore on load
                RoughnessValues = (float[])RoughnessValues.Clone(),
                MetallicValues = (float[])MetallicValues.Clone(),
                AOValues = (float[])AOValues.Clone(),
                SpecularValues = (float[])SpecularValues.Clone(),
                EmissiveIntensityValues = (float[])EmissiveIntensityValues.Clone(),
                DisplacementStrengthValues = (float[])DisplacementStrengthValues.Clone(),
                ParallaxScaleValues = (float[])ParallaxScaleValues.Clone(),
            };
        }

        public async Task ImportSaveDataAsync(LandscapeSaveData data)
        {
            if (data == null) return;

            int vertCount = (GridSize + 1) * (GridSize + 1);
            if (data.Heights?.Length == vertCount)
            {
                Array.Copy(data.Heights, _vertexHeights, vertCount);
            }
            else if (data.Heights != null)
            {
                // Grid size mismatch — saved data is different resolution, skip height load
                Console.WriteLine($"[TileMap] ImportSaveData — height array size mismatch." +
                    $" Expected:{vertCount} Got:{data.Heights.Length} — heights skipped");
            }

            if (data.Materials?.Length == TileCount)
            {
                for (int i = 0; i < TileCount; i++)
                {
                    var tile = _tiles[i];
                    tile.MaterialIndex = data.Materials[i];
                    tile.BlendWeight = data.BlendWeights?[i] ?? 0f;
                    tile.BlendMaterial = data.BlendMats?[i] ?? (int)TileMaterial.Grass;

                    var (x, y) = TileCoord(i);
                    tile.Height =
                        (_vertexHeights[VertexIndex(x, y)] +
                         _vertexHeights[VertexIndex(x + 1, y)] +
                         _vertexHeights[VertexIndex(x, y + 1)] +
                         _vertexHeights[VertexIndex(x + 1, y + 1)]) * 0.25f;

                    _tiles[i] = tile;

                    // Yield every 4096 tiles — keeps render thread responsive
                    if (i % 4096 == 0)
                        await Task.Yield();
                }
            }
            // ── Restore PBR scalars if saved data has them ────────────────────
            // Guard length — old save files won't have these arrays
            if (data.RoughnessValues?.Length == 6)
                RoughnessValues = (float[])data.RoughnessValues.Clone();
            if (data.MetallicValues?.Length == 6)
                MetallicValues = (float[])data.MetallicValues.Clone();
            if (data.AOValues?.Length == 6)
                AOValues = (float[])data.AOValues.Clone();
            if (data.SpecularValues?.Length == 6)
                SpecularValues = (float[])data.SpecularValues.Clone();
            if (data.EmissiveIntensityValues?.Length == 6)
                EmissiveIntensityValues = (float[])data.EmissiveIntensityValues.Clone();
            if (data.DisplacementStrengthValues?.Length == 6)
                DisplacementStrengthValues = (float[])data.DisplacementStrengthValues.Clone();
            if (data.ParallaxScaleValues?.Length == 6)
                ParallaxScaleValues = (float[])data.ParallaxScaleValues.Clone();

            // Chunked normal recalculation — yields every 32 rows
            await RecalculateNormalsAsync();
            MarkDirty();
        }

        // ── Load Default — flat grass reset ──────────────────────────────────────
        public void LoadDefault()
        {
            Array.Clear(_vertexHeights, 0, _vertexHeights.Length);
            for (int i = 0; i < TileCount; i++)
            {
                _tiles[i] = new TileData
                {
                    MaterialIndex = (int)TileMaterial.Grass,
                    BlendMaterial = (int)TileMaterial.Grass,
                    BlendWeight = 0f,
                    Height = 0f,
                    Normal = Vector3.UnitZ
                };
            }
            _fullUploadPending = true;
            _isDirty = true;
            Console.WriteLine("[TileMap] LoadDefault — reset to flat grass");
        }

        // ── Export Height Map → .r16 bytes ───────────────────────────────────────
        public byte[] ExportHeightMapBytes()
        {
            int vertCount = (GridSize + 1) * (GridSize + 1);
            var bytes = new byte[vertCount * 2];
            const float heightMin = -5f;
            const float heightRange = 15f; // -5 to 10

            for (int i = 0; i < vertCount; i++)
            {
                float normalized = (_vertexHeights[i] - heightMin) / heightRange;
                ushort value = (ushort)(Math.Clamp(normalized, 0f, 1f) * 65535f);
                bytes[i * 2] = (byte)(value & 0xFF);
                bytes[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
            }

            Console.WriteLine($"[TileMap] ExportHeightMap — {vertCount} verts, {bytes.Length} bytes");
            return bytes;
        }

        // ── Import Height Map ← .r16 bytes ───────────────────────────────────────
        public void ImportHeightMapBytes(byte[] data)
        {
            int vertCount = (GridSize + 1) * (GridSize + 1);
            int expected = vertCount * 2;

            if (data.Length != expected)
            {
                Console.WriteLine($"[TileMap] ImportHeightMap — size mismatch. Expected:{expected} Got:{data.Length}");
                return;
            }

            const float heightMin = -5f;
            const float heightRange = 15f;

            for (int i = 0; i < vertCount; i++)
            {
                ushort value = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
                _vertexHeights[i] = heightMin + (value / 65535f) * heightRange;
            }

            for (int y = 0; y < GridSize; y++)
                for (int x = 0; x < GridSize; x++)
                    SyncTileHeight(x, y);

            RecalculateNormals();
            _fullUploadPending = true;
            _isDirty = true;
            Console.WriteLine($"[TileMap] ImportHeightMap — {vertCount} verts loaded");
        }

        // ── Export Paint Map → RGBA bytes (one pixel per tile) ───────────────────
        public byte[] ExportPaintMapBytes()
        {
            var bytes = new byte[TileCount * 4];
            for (int i = 0; i < TileCount; i++)
            {
                var t = _tiles[i];
                bytes[i * 4] = (byte)t.MaterialIndex;
                bytes[i * 4 + 1] = (byte)t.BlendMaterial;
                bytes[i * 4 + 2] = (byte)(t.BlendWeight * 255f);
                bytes[i * 4 + 3] = 255;
            }
            Console.WriteLine($"[TileMap] ExportPaintMap — {TileCount} tiles");
            return bytes;
        }

        // ── Import Paint Map ← RGBA bytes ────────────────────────────────────────
        public void ImportPaintMapBytes(byte[] pixels, int width, int height)
        {
            if (width != GridSize || height != GridSize)
            {
                Console.WriteLine($"[TileMap] ImportPaintMap — size mismatch. Expected:{GridSize}x{GridSize} Got:{width}x{height}");
                return;
            }

            for (int i = 0; i < TileCount; i++)
            {
                var t = _tiles[i];
                t.MaterialIndex = Math.Clamp((int)pixels[i * 4], 0, 5);
                t.BlendMaterial = Math.Clamp((int)pixels[i * 4 + 1], 0, 5);
                t.BlendWeight = pixels[i * 4 + 2] / 255f;
                _tiles[i] = t;
            }

            _fullUploadPending = true;
            _isDirty = true;
            Console.WriteLine($"[TileMap] ImportPaintMap — {TileCount} tiles loaded");
        }

        private async Task RecalculateNormalsAsync()
        {
            const int RowsPerChunk = 32;
            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    float hL = GetHeight(x - 1, y);
                    float hR = GetHeight(x + 1, y);
                    float hD = GetHeight(x, y - 1);
                    float hU = GetHeight(x, y + 1);

                    float dX = (hR - hL) / (2f * TileSize);
                    float dY = (hU - hD) / (2f * TileSize);

                    var normal = Vector3.Normalize(new Vector3(-dX, -dY, 1f));
                    int idx = TileIndex(x, y);
                    var tile = _tiles[idx];
                    tile.Normal = normal;
                    _tiles[idx] = tile;
                }

                // Yield every RowsPerChunk rows
                if (y % RowsPerChunk == 0)
                    await Task.Yield();
            }
        }



    }

    public class LandscapeSaveData
    {
        public float[] Heights { get; set; } = Array.Empty<float>();
        public int[] Materials { get; set; } = Array.Empty<int>();
        public float[] BlendWeights { get; set; } = Array.Empty<float>();
        public int[] BlendMats { get; set; } = Array.Empty<int>();

        // ── PBR Scalar Overrides — saved per landscape so each scene
        // can restore its own material feel after load ─────────────────
        public float[] RoughnessValues { get; set; }
            = new[] { 0.9f, 0.8f, 0.4f, 0.5f, 0.1f, 0.2f };
        public float[] MetallicValues { get; set; }
            = new[] { 0.0f, 0.0f, 0.5f, 0.3f, 0.0f, 0.4f };
        public float[] AOValues { get; set; }
            = new[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
        public float[] SpecularValues { get; set; }
            = new[] { 0.3f, 0.4f, 0.7f, 0.6f, 0.8f, 0.9f };
        public float[] EmissiveIntensityValues { get; set; }
            = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        public float[] DisplacementStrengthValues { get; set; }
            = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        public float[] ParallaxScaleValues { get; set; }
            = new[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };
    }

}