

namespace SpectralXGLX.SpectralXComponent
{

    public class PaintImportResult
    {
            [JsonPropertyName("pixels")] public string Pixels { get; set; } = "";
            [JsonPropertyName("width")] public int Width { get; set; }
            [JsonPropertyName("height")] public int Height { get; set; }
    }

    public class DefaultImportResult
    {
            [JsonPropertyName("heights")] public string? Heights { get; set; }
            [JsonPropertyName("paint")] public PaintImportResult? Paint { get; set; }
    }

    public class EnemyTexSwap
    {
            [JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "";
            [JsonPropertyName("texUrl")]
            public string TexUrl { get; set; } = "";
    }
    public class BreakTexSwap
    {
            [JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "";
            [JsonPropertyName("texUrl")]
            public string TexUrl { get; set; } = "";
    }
    public class WebGLFrameData
        {
            [JsonPropertyName("breakTexUrls")]
            public string[]? BreakTexUrls { get; set; }

            [JsonPropertyName("breakTexSwaps")]
            public BreakTexSwap[]? BreakTexSwaps { get; set; }

            [JsonPropertyName("splatterTexUrls")]
            public string[]? SplatterTexUrls { get; set; }

            [JsonPropertyName("skeletonTexUrls")]
            public string[]? SkeletonTexUrls { get; set; }

            [JsonPropertyName("skeletonTexSwaps")]
            public EnemyTexSwap[]? SkeletonTexSwaps { get; set; }
            [JsonPropertyName("psychoSkeletonTexUrls")]
            public string[]? PsychoSkeletonTexUrls { get; set; }

            [JsonPropertyName("psychoSkeletonTexSwaps")]
            public EnemyTexSwap[]? PsychoSkeletonTexSwaps { get; set; }

            [JsonPropertyName("zombiePsychoTexUrls")]
            public string[]? ZombiePsychoTexUrls { get; set; }

            [JsonPropertyName("zombiePsychoTexSwaps")]
            public EnemyTexSwap[]? ZombiePsychoTexSwaps { get; set; }

            [JsonPropertyName("skeletonWarTexUrls")]
            public string[]? SkeletonWarTexUrls { get; set; }

            [JsonPropertyName("skeletonWarTexSwaps")]
            public EnemyTexSwap[]? SkeletonWarTexSwaps { get; set; }

            [JsonPropertyName("goatmanTexUrls")]
            public string[]? GoatmanTexUrls { get; set; }

            [JsonPropertyName("goatmanTexSwaps")]
            public EnemyTexSwap[]? GoatmanTexSwaps { get; set; }

            [JsonPropertyName("scavBossTexUrls")]
            public string[]? ScavBossTexUrls { get; set; }

            [JsonPropertyName("scavBossTexSwaps")]
            public EnemyTexSwap[]? ScavBossTexSwaps { get; set; }

            [JsonPropertyName("skeletonBossTexUrls")]
            public string[]? SkeletonBossTexUrls { get; set; }

            [JsonPropertyName("skeletonBossTexSwaps")]
            public EnemyTexSwap[]? SkeletonBossTexSwaps { get; set; }

            [JsonPropertyName("cowTexUrls")]
            public string[]? CowTexUrls { get; set; }

            [JsonPropertyName("cowTexSwaps")]
            public EnemyTexSwap[]? CowTexSwaps { get; set; }

            [JsonPropertyName("catTexUrls")]
            public string[]? CatTexUrls { get; set; }

            [JsonPropertyName("catTexSwaps")]
            public EnemyTexSwap[]? CatTexSwaps { get; set; }

            [JsonPropertyName("townSlutTexUrls")]
            public string[]? TownSlutTexUrls { get; set; }

            [JsonPropertyName("townSlutTexSwaps")]
            public EnemyTexSwap[]? TownSlutTexSwaps { get; set; }


            [JsonPropertyName("warriorTexUrl")]
            public string? WarriorTexUrl { get; set; }

            [JsonPropertyName("warriorTexUrls")]
            public string[]? WarriorTexUrls { get; set; }

            [JsonPropertyName("rogueTexUrl")]
            public string? RogueTexUrl { get; set; }

            [JsonPropertyName("rogueTexUrls")]
            public string[]? RogueTexUrls { get; set; }

            [JsonPropertyName("monkTexUrl")]
            public string? MonkTexUrl { get; set; }

            [JsonPropertyName("monkTexUrls")]
            public string[]? MonkTexUrls { get; set; }

            [JsonPropertyName("mageTexUrl")]
            public string? MageTexUrl { get; set; }

            [JsonPropertyName("mageTexUrls")]
            public string[]? MageTexUrls { get; set; }

            [JsonPropertyName("width")]
            public int Width { get; set; }
            [JsonPropertyName("height")]
            public int Height { get; set; }
            [JsonPropertyName("meshes")]
            public List<WebGLMeshData> Meshes { get; set; } = new();

            [JsonPropertyName("lightCount")]
            public int LightCount { get; set; }
            [JsonPropertyName("lightPositions")]
            public float[] LightPositions { get; set; } = Array.Empty<float>();
            [JsonPropertyName("lightColors")]
            public float[] LightColors { get; set; } = Array.Empty<float>();
            [JsonPropertyName("lightIntensities")]
            public float[] LightIntensities { get; set; } = Array.Empty<float>();
            [JsonPropertyName("lightRanges")]
            public float[] LightRanges { get; set; } = Array.Empty<float>();
            [JsonPropertyName("lightTypes")]
            public int[] LightTypes { get; set; } = Array.Empty<int>();
            [JsonPropertyName("lightDirections")]
            public float[] LightDirections { get; set; } = Array.Empty<float>();
            [JsonPropertyName("lightSpotAngles")]
            public float[] LightSpotAngles { get; set; } = Array.Empty<float>();
            [JsonPropertyName("lightCastsShadows")]
            public bool[] LightCastsShadows { get; set; } = Array.Empty<bool>();
            [JsonPropertyName("lightVPs")]
            public float[][] LightVPs { get; set; } = Array.Empty<float[]>();

            [JsonPropertyName("camX")]
            public float CamX { get; set; }
            [JsonPropertyName("camY")]
            public float CamY { get; set; }
            [JsonPropertyName("camZ")]
            public float CamZ { get; set; }

            [JsonPropertyName("aaMode")]
            public int AAMode { get; set; }
            [JsonPropertyName("jitterX")]
            public float JitterX { get; set; }
            [JsonPropertyName("jitterY")]
            public float JitterY { get; set; }

            [JsonPropertyName("shadowMode")]
            public int ShadowMode { get; set; }

            [JsonPropertyName("shadowSoftnessBias")]
            public float ShadowSoftnessBias { get; set; }

            [JsonPropertyName("shadowBlockerSearchRadius")]
            public float ShadowBlockerSearchRadius { get; set; }

            [JsonPropertyName("shadowKernelSize")]
            public float ShadowKernelSize { get; set; }

            [JsonPropertyName("shadowContactSharpness")]
            public float ShadowContactSharpness { get; set; }

            [JsonPropertyName("shadowDepthBias")]
            public float ShadowDepthBias { get; set; }

            [JsonPropertyName("shadowTintR")]
            public float ShadowTintR { get; set; }

            [JsonPropertyName("shadowTintG")]
            public float ShadowTintG { get; set; }

            [JsonPropertyName("shadowTintB")]
            public float ShadowTintB { get; set; }

            [JsonPropertyName("shadowTintStrength")]
            public float ShadowTintStrength { get; set; }
            [JsonPropertyName("shadowPenumbraTintStrength")]
            public float ShadowPenumbraTintStrength { get; set; }

            [JsonPropertyName("timeOfDay")]
            public float TimeOfDay { get; set; }

            [JsonPropertyName("skyBlend")]
            public float SkyBlend { get; set; }

            [JsonPropertyName("cloudOpacity")]
            public float CloudOpacity { get; set; }

            [JsonPropertyName("rainbowIntensity")]
            public float RainbowIntensity { get; set; }

            [JsonPropertyName("skyZenithR")]
            public float SkyZenithR { get; set; }

            [JsonPropertyName("skyZenithG")]
            public float SkyZenithG { get; set; }

            [JsonPropertyName("skyZenithB")]
            public float SkyZenithB { get; set; }

            [JsonPropertyName("skyHorizonR")]
            public float SkyHorizonR { get; set; }

            [JsonPropertyName("skyHorizonG")]
            public float SkyHorizonG { get; set; }

            [JsonPropertyName("skyHorizonB")]
            public float SkyHorizonB { get; set; }

            [JsonPropertyName("sunIntensity")]
            public float SunIntensity { get; set; }

            [JsonPropertyName("ambientR")]
            public float AmbientR { get; set; }

            [JsonPropertyName("ambientG")]
            public float AmbientG { get; set; }

            [JsonPropertyName("ambientB")]
            public float AmbientB { get; set; }

            [JsonPropertyName("sunColorR")]
            public float SunColorR { get; set; }

            [JsonPropertyName("sunColorG")]
            public float SunColorG { get; set; }

            [JsonPropertyName("sunColorB")]
            public float SunColorB { get; set; }

            [JsonPropertyName("sunDirX")]
            public float SunDirX { get; set; }

            [JsonPropertyName("sunDirY")]
            public float SunDirY { get; set; }

            [JsonPropertyName("sunDirZ")]
            public float SunDirZ { get; set; }

            [JsonPropertyName("sunDirSkyX")]
            public float SunDirSkyX { get; set; }
            [JsonPropertyName("sunDirSkyY")]
            public float SunDirSkyY { get; set; }
            [JsonPropertyName("sunDirSkyZ")]
            public float SunDirSkyZ { get; set; }

            [JsonPropertyName("moonDirSkyX")]
            public float MoonDirSkyX { get; set; }
            [JsonPropertyName("moonDirSkyY")]
            public float MoonDirSkyY { get; set; }
            [JsonPropertyName("moonDirSkyZ")]
            public float MoonDirSkyZ { get; set; }

            [JsonPropertyName("cloudOffset")]
            public float CloudOffset { get; set; }

            [JsonPropertyName("starOffset")]
            public float StarOffset { get; set; }

            [JsonPropertyName("cloudScale")]
            public float CloudScale { get; set; }

            [JsonPropertyName("starScale")]
            public float StarScale { get; set; }

            [JsonPropertyName("moonDirX")]
            public float MoonDirX { get; set; }

            [JsonPropertyName("moonDirY")]
            public float MoonDirY { get; set; }

            [JsonPropertyName("moonDirZ")]
            public float MoonDirZ { get; set; }

            [JsonPropertyName("moonColorR")]
            public float MoonColorR { get; set; }

            [JsonPropertyName("moonColorG")]
            public float MoonColorG { get; set; }

            [JsonPropertyName("moonColorB")]
            public float MoonColorB { get; set; }

            [JsonPropertyName("moonGlow")]
            public float MoonGlow { get; set; }

            [JsonPropertyName("particleInstances")]
            public List<ParticleInstanceGroup>? ParticleInstances { get; set; }

            [JsonPropertyName("foliageInstances")]
            public List<FoliageInstanceGroup>? FoliageInstances { get; set; }

            [JsonPropertyName("cubeCityInstances")]
            public CubeCityInstanceGroup? CubeCityInstances { get; set; }

            [JsonPropertyName("vp")]
            public float[] VP { get; set; } = Array.Empty<float>();

            [JsonPropertyName("camRight")]
            public float[] CamRight { get; set; } = Array.Empty<float>();

            [JsonPropertyName("camUp")]
            public float[] CamUp { get; set; } = Array.Empty<float>();

            [JsonPropertyName("tileMap")]
            public TileMapFrameData? TileMap { get; set; }

            [JsonPropertyName("tileMapTextures")]
            public string[]? TileMapTextures { get; set; }

            [JsonPropertyName("tileMapUploaded")]
            public bool TileMapUploaded { get; set; }

            [JsonPropertyName("tileMapNormalTextures")]
            public string?[]? TileMapNormalTextures { get; set; }

            [JsonPropertyName("tileMapRoughnessTextures")]
            public string?[]? TileMapRoughnessTextures { get; set; }

            [JsonPropertyName("tileMapMetallicTextures")]
            public string?[]? TileMapMetallicTextures { get; set; }

            [JsonPropertyName("tileMapAOTextures")]
            public string?[]? TileMapAOTextures { get; set; }

            [JsonPropertyName("tileMapSpecularTextures")]
            public string?[]? TileMapSpecularTextures { get; set; }

            [JsonPropertyName("tileMapEmissiveTextures")]
            public string?[]? TileMapEmissiveTextures { get; set; }

            [JsonPropertyName("tileMapDisplacementTextures")]
            public string?[]? TileMapDisplacementTextures { get; set; }

            [JsonPropertyName("tileMapPBRUploaded")]
            public bool TileMapPBRUploaded { get; set; }

            [JsonPropertyName("tileRoughnessValues")]
            public float[] TileRoughnessValues { get; set; } = new float[6];

            [JsonPropertyName("tileMetallicValues")]
            public float[] TileMetallicValues { get; set; } = new float[6];

            [JsonPropertyName("tileAOValues")]
            public float[] TileAOValues { get; set; } = new float[6];

            [JsonPropertyName("tileSpecularValues")]
            public float[] TileSpecularValues { get; set; } = new float[6];

            [JsonPropertyName("tileEmissiveIntensityValues")]
            public float[] TileEmissiveIntensityValues { get; set; } = new float[6];

            [JsonPropertyName("tileDisplacementStrengthValues")]
            public float[] TileDisplacementStrengthValues { get; set; } = new float[6];

            [JsonPropertyName("tileParallaxScaleValues")]
            public float[] TileParallaxScaleValues { get; set; } = new float[6];

            [JsonPropertyName("viewMatrix")]
            public float[]? ViewMatrix { get; set; }

            [JsonPropertyName("projMatrix")]
            public float[]? ProjMatrix { get; set; }

            [JsonPropertyName("activeScene")]
            public int ActiveScene { get; set; }

            [JsonPropertyName("brushWorldX")]
            public float BrushWorldX { get; set; }

            [JsonPropertyName("brushWorldY")]
            public float BrushWorldY { get; set; }

            [JsonPropertyName("brushRadius")]
            public float BrushRadius { get; set; }

            [JsonPropertyName("isActive")]
            public bool IsActive { get; set; }
            [JsonPropertyName("landscapeActive")]
            public bool LandscapeActive { get; set; }

            [JsonPropertyName("isMousePainting")]
            public bool IsMousePainting { get; set; }

            [JsonPropertyName("activePaintMaterial")]
            public int ActivePaintMaterial { get; set; }

            [JsonPropertyName("paintStrength")]
            public float PaintStrength { get; set; }

            [JsonPropertyName("skyDayTexUrl")]
            public string? SkyDayTexUrl { get; set; }

            [JsonPropertyName("skyNightTexUrl")]
            public string? SkyNightTexUrl { get; set; }

            [JsonPropertyName("textMeshes")]
            public List<WebGLTextData>? TextMeshes { get; set; }

            [JsonPropertyName("scrollbarZ")]
            public float ScrollbarZ { get; set; }

            [JsonPropertyName("cameraMode")]
            public int CameraMode { get; set; }
   

    }

        public class WebGLMeshUpload
        {
            [System.Text.Json.Serialization.JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "";
            [System.Text.Json.Serialization.JsonPropertyName("vertices")]
            public float[] Vertices { get; set; } = Array.Empty<float>();
            [System.Text.Json.Serialization.JsonPropertyName("normals")]
            public float[] Normals { get; set; } = Array.Empty<float>();
            [System.Text.Json.Serialization.JsonPropertyName("uvs")]
            public float[] UVs { get; set; } = Array.Empty<float>();
            [System.Text.Json.Serialization.JsonPropertyName("textureDataUrl")]
            public string? TextureDataUrl { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("hasTexture")]
            public bool HasTexture { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("textureWidth")]
            public int TextureWidth { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("textureHeight")]
            public int TextureHeight { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("textureIsRawRGBA")]
            public bool TextureIsRawRGBA { get; set; }

            [JsonPropertyName("materialTextures")]
            public string[] MaterialTextures { get; set; } = Array.Empty<string>();

            [JsonPropertyName("materialColors")]
            public string[] MaterialColors { get; set; } = Array.Empty<string>();

            [JsonPropertyName("matBreaks")]
            public int[] MatBreaks { get; set; } = Array.Empty<int>();
            [JsonPropertyName("matIndices")]
            public int[] MatIndices { get; set; } = Array.Empty<int>();
            [JsonPropertyName("textureDirty")]
            public bool TextureDirty { get; set; } = false;

            [JsonPropertyName("overlayTextureDataUrl")]
            public string? OverlayTextureDataUrl { get; set; }

            [JsonPropertyName("overlayAlpha")]
            public float OverlayAlpha { get; set; }
    }




        public class WebGLMeshData
        {
            [System.Text.Json.Serialization.JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "";
            [System.Text.Json.Serialization.JsonPropertyName("mvp")]
            public float[] Mvp { get; set; } = Array.Empty<float>();
            [System.Text.Json.Serialization.JsonPropertyName("upload")]
            public WebGLMeshUpload? Upload { get; set; } = null;
            [System.Text.Json.Serialization.JsonPropertyName("r")]
            public float R { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("g")]
            public float G { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("b")]
            public float B { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("a")]
            public float A { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("model")]
            public float[]? Model { get; set; } = null;

            [JsonPropertyName("isEmissive")]
            public bool IsEmissive { get; set; }

            [JsonPropertyName("emissiveIntensity")]
            public float EmissiveIntensity { get; set; }
            [JsonPropertyName("uvOffsetX")]
            public float UVOffsetX { get; set; }
            [JsonPropertyName("uvOffsetY")]
            public float UVOffsetY { get; set; }

            [JsonPropertyName("uvScaleX")]
            public float UVScaleX { get; set; }
            [JsonPropertyName("uvScaleY")]
            public float UVScaleY { get; set; }
            [JsonPropertyName("transformDirty")]
            public bool TransformDirty { get; set; }
            [JsonPropertyName("castsShadow")]
            public bool CastsShadow { get; set; } = true;

            [JsonPropertyName("receivesShadow")]
            public bool ReceivesShadow { get; set; } = true;

            [JsonPropertyName("overlayTextureDataUrl")]
            public string? OverlayTextureDataUrl { get; set; }

            [JsonPropertyName("overlayAlpha")]
            public float OverlayAlpha { get; set; }

    }

        public class WebGLTextData
        {
            [JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "";
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";
            [JsonPropertyName("fontKey")]
            public string FontKey { get; set; } = "";
            [JsonPropertyName("jsonUrl")]
            public string JsonUrl { get; set; } = "";
            [JsonPropertyName("texUrl")]
            public string TexUrl { get; set; } = "";
            [JsonPropertyName("fontSize")]
            public float FontSize { get; set; } = 1f;
            [JsonPropertyName("mvp")]
            public float[] Mvp { get; set; } = Array.Empty<float>();
            [JsonPropertyName("r")] public float R { get; set; } = 1f;
            [JsonPropertyName("g")] public float G { get; set; } = 1f;
            [JsonPropertyName("b")] public float B { get; set; } = 1f;
            [JsonPropertyName("a")] public float A { get; set; } = 1f;
            [JsonPropertyName("outlineR")] public float OutlineR { get; set; }
            [JsonPropertyName("outlineG")] public float OutlineG { get; set; }
            [JsonPropertyName("outlineB")] public float OutlineB { get; set; }
            [JsonPropertyName("outlineA")] public float OutlineA { get; set; }
            [JsonPropertyName("outlineWidth")] public float OutlineWidth { get; set; }
            [JsonPropertyName("letterSpacing")] public float LetterSpacing { get; set; }
            [JsonPropertyName("align")] public int Align { get; set; }
            [JsonPropertyName("needsRebuild")] public bool NeedsRebuild { get; set; } = true;
            [JsonPropertyName("glowRadius")] public float GlowRadius { get; set; } = 0.25f;
            [JsonPropertyName("glowStrength")] public float GlowStrength { get; set; } = 0.8f;

            // Glow color
            [JsonPropertyName("glowR")] public float GlowR { get; set; } = 1f;
            [JsonPropertyName("glowG")] public float GlowG { get; set; } = 1f;
            [JsonPropertyName("glowB")] public float GlowB { get; set; } = 1f;
            [JsonPropertyName("glowA")] public float GlowA { get; set; } = 1f;

            // Shadow blur
            [JsonPropertyName("shadowBlur")] public float ShadowBlur { get; set; } = 0f;
            [JsonPropertyName("shadowR")] public float ShadowR { get; set; } = 0f;
            [JsonPropertyName("shadowG")] public float ShadowG { get; set; } = 0f;
            [JsonPropertyName("shadowB")] public float ShadowB { get; set; } = 0f;
            [JsonPropertyName("shadowA")] public float ShadowA { get; set; } = 0f;
        }


        public class ParticleInstanceGroup
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = "";

            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("offsets")]
            public float[] Offsets { get; set; } = Array.Empty<float>(); 

            [JsonPropertyName("colors")]
            public float[] Colors { get; set; } = Array.Empty<float>(); 

            [JsonPropertyName("sizes")]
            public float[] Sizes { get; set; } = Array.Empty<float>(); 

            [JsonPropertyName("texKey")]
            public string TexKey { get; set; } = ""; 
        }


        public class TileMapFrameData
        {
            [JsonPropertyName("tileGridSize")]
            public int TileGridSize { get; set; }
            [JsonPropertyName("heights")]
            public float[] Heights { get; set; } = Array.Empty<float>();

            [JsonPropertyName("normals")]
            public float[] Normals { get; set; } = Array.Empty<float>();

            [JsonPropertyName("materials")]
            public int[] Materials { get; set; } = Array.Empty<int>();

            [JsonPropertyName("blendWeights")]
            public float[] BlendWeights { get; set; } = Array.Empty<float>();

            [JsonPropertyName("blendMaterials")]
            public int[] BlendMaterials { get; set; } = Array.Empty<int>();

            [JsonPropertyName("isDirty")]
            public bool IsDirty { get; set; }

            [JsonPropertyName("isFullUpload")]
            public bool IsFullUpload { get; set; }

            [JsonPropertyName("dirtyX")]
            public int DirtyX { get; set; }

            [JsonPropertyName("dirtyY")]
            public int DirtyY { get; set; }

            [JsonPropertyName("dirtyW")]
            public int DirtyW { get; set; }

            [JsonPropertyName("dirtyH")]
            public int DirtyH { get; set; }

        }

        public class FoliageInstanceGroup
        {
            [JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "";

            [JsonPropertyName("texKey")]
            public string TexKey { get; set; } = "";

            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("positions")]
            public float[] Positions { get; set; } = Array.Empty<float>();

            [JsonPropertyName("scales")]
            public float[] Scales { get; set; } = Array.Empty<float>();

            [JsonPropertyName("rotations")]
            public float[] Rotations { get; set; } = Array.Empty<float>();

            [JsonPropertyName("isStatic")]
            public bool IsStatic { get; set; } = true;

            [JsonIgnore]
            public bool Uploaded { get; set; } = false;

        [JsonPropertyName("boundingRadius")]
        public float BoundingRadius { get; set; } = 2.0f;   // NEW — mesh-space radius at scale 1.0
        [JsonPropertyName("color")]
        public float[] Color { get; set; } = new[] { 1.0f, 1.0f, 1.0f, 1.0f }; // RGBA

    }


        public class CubeCityInstanceGroup
        {
            [JsonPropertyName("meshId")]
            public string MeshId { get; set; } = "FBXCube";

            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("positions")]
            public float[] Positions { get; set; } = Array.Empty<float>(); 

            [JsonPropertyName("scales")]
            public float[] Scales { get; set; } = Array.Empty<float>();    

            [JsonPropertyName("rotations")]
            public float[] Rotations { get; set; } = Array.Empty<float>();  

            [JsonPropertyName("colors")]
            public float[] Colors { get; set; } = Array.Empty<float>();    

            [JsonPropertyName("phase")]
            public int Phase { get; set; }   
        }




}
