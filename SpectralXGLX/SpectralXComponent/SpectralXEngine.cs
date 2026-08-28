
using static SpectralXGLX.SpectralXComponent.WebGLFrameData;

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        public static class SceneID
        {
            public const int SpectralXDemo = 1;  // Scene 1 → portal → Scene 2
            public const int SpectralXTown = 2;  // Scene 2 town
            public const int Home = 3;  // Home page Scene 3
            public const int BWPScene1 = 4; // BWP Scene 1 / Forest 1 
            public const int BWPScene2 = 5; // BWP Scene 2 / BloodTown
            public const int BWPScene3 = 6; // BWP Scene 2 / Forest 2 
            public const int BWPScene4 = 7; // BWP Scene 2 / Forest 3 add skelwar
            public const int BWPScene5 = 8; // BWP Scene 2 / Forest 4
            public const int BWPScene6 = 9; // BWP Scene 2 / Forest 5
            public const int BWPScene7 = 10; // BWP Scene 2 / Forest 6 add goatman
            public const int BWPScene8 = 11; // BWP Scene 2 / Forest 7
            public const int BWPScene9 = 12; // BWP Scene 2 / Gravyard has all enemies but goatman and scavboss with Skele boss as last wave 10 final boss
            public const int BWPScene10 = 13; // BWP Scene 2 / Caves has only scavboss enemy and zombiepyscho
            public const int BWPScene11 = 14; // BWP Scene 2 / Snow Town West entrance no enemies

            private static readonly HashSet<int> _bwpSceneIds = new() { BWPScene1, BWPScene2, BWPScene3, BWPScene4, BWPScene5, BWPScene6, BWPScene7, BWPScene8, BWPScene9, BWPScene10, BWPScene11 };

            private static readonly HashSet<int> _WebViewSceneIds = new() { Home };

            private static readonly HashSet<int> _3DSceneIds = new() { SpectralXTown };

            private static readonly HashSet<int> _testSceneIds = new() { SpectralXDemo };

            public static bool IsWebViewScene(int id) => _WebViewSceneIds.Contains(id);
            public static bool IsBWPScene(int id) => _bwpSceneIds.Contains(id);
            public static bool IsTestScene(int id) => _testSceneIds.Contains(id);
            public static bool Is3DScene(int id) => _3DSceneIds.Contains(id);
        }

        public enum ShadowMode
        {
            PCF_V1 = 0,
            PCSS_V1 = 1,
            SpecXS_VDS_V1 = 2,
            SpecXS_RPD_V2 = 3,
            SpecXS_IGN_V3 = 4
        }

        public ShadowMode ActiveShadow { get; set; } = ShadowMode.PCF_V1;

        public SpectralXShadow Shadow { get; set; } = new SpectralXShadow();
        public enum AAMode
        {
            None = 0,
            MSAA = 1,
            FXAA = 2,
            SMAA = 3,
            TAA = 4,
            SpectralAA = 5,
            SpectralAAV2 = 6,
            SpectralAAV3 = 7
        }
        public enum CameraMode
        {
            WebpageView = 0,
            FreeCam = 1,
            Orthographic = 2,
            Orbit = 3
        }

        public CameraMode ActiveCameraMode { get; set; } = CameraMode.FreeCam;
        public void SetCameraMode(CameraMode mode)
        {
            Console.WriteLine($"[Camera] SetCameraMode called: {mode} from {Environment.StackTrace}");
            var previous = ActiveCameraMode;
             ActiveCameraMode = mode;  
            // Forces FreeCam for testing
            // ActiveCameraMode = CameraMode.FreeCam;
             
            switch (mode)
            {
                case CameraMode.WebpageView:
                    Camera.Rotation = new CustomVec3(0f, 0f, 0f);
                    Camera.Position = new CustomVec3(Camera.RailX, Camera.RailY, Camera.Position.Z);
                    break;

                case CameraMode.FreeCam:
                    break;

                case CameraMode.Orthographic:
                    OrthoCamera.Reset(0f, 0f, 10f);
                    OrthoCamera.ResetMousePos();
                    Console.WriteLine("[SpectralXEngine] Switched to Orthographic camera");
                    break;

                case CameraMode.Orbit:
                    if (previous == CameraMode.FreeCam)
                    {
                        OrbitCamera.InitFromFreeCam(
                            Camera.Position,
                            Camera.Forward,
                            pivotDistance: 20f);
                        OrbitCamera.Projection =
                            SpectralXOrbitCamera.ProjectionMode.Perspective;
                    }
                    else if (previous == CameraMode.Orthographic)
                    {
                        OrbitCamera.InitFromOrthoCamera(
                            OrthoCamera.TargetX,
                            OrthoCamera.TargetY,
                            OrthoCamera.OrthoSize,
                            SpectralXOrbitCamera.ProjectionMode.Orthographic);
                    }
                    else
                    {
                        OrbitCamera.Reset();
                    }
                    Console.WriteLine("[SpectralXEngine] Switched to Orbit camera");
                    break;
            }
            
        }
        public void HandleIsoCameraMouseMove(float x, float y)
        {
            if (ActiveCameraMode != CameraMode.Orthographic) return;
            OrthoCamera.SetMousePosition(x, y);
        }

        [JSInvokable("SetIsoCameraMousePos")]
        public void SetIsoCameraMousePos(float x, float y)
        {
            if (ActiveCameraMode != CameraMode.Orthographic) return;
            OrthoCamera.SetMousePosition(x, y);
        }

        public void HandleIsoCameraScroll(float delta)
        {
            if (ActiveCameraMode == CameraMode.Orthographic)
            {
                OrthoCamera.Zoom(delta * 0.5f);
                return;
            }
            if (ActiveCameraMode == CameraMode.Orbit)
            {
                OrbitCamera.Zoom(delta * 0.5f);
                return;
            }
        }


        public AAMode ActiveAA { get; set; } = AAMode.None;

        private readonly IJSRuntime _js;

        private readonly SpectralXViewport Viewport;
        public readonly SpectralXInput Input;
        public SpectralXCamera Camera;
        public SpectralXOrthoCamera OrthoCamera { get; private set; } = new SpectralXOrthoCamera();
        public SpectralXOrbitCamera OrbitCamera { get; private set; } = new SpectralXOrbitCamera();
        private readonly SpectralXCanvas SpectralXGLX;
        private readonly GamepadService _gamepad;
        public SpectralLevel LevelSystem { get; } = new SpectralLevel();
        public SpectralXMeshLibrary MeshLibrary { get; }
        public SpectralXScene Scene { get; }
        public SpectralXScene Scene2 { get; } = new SpectralXScene();
        public SpectralXScene Scene3 { get; } = new SpectralXScene();
        public SpectralXScene Scene4 { get; } = new SpectralXScene();
        public SpectralXScene Scene5 { get; } = new SpectralXScene();
        public SpectralXScene Scene6 { get; } = new SpectralXScene();
        public SpectralXScene Scene7 { get; } = new SpectralXScene();
        public SpectralXScene Scene8 { get; } = new SpectralXScene();
        public SpectralXScene Scene9 { get; } = new SpectralXScene();
        public SpectralXScene Scene10 { get; } = new SpectralXScene();
        public SpectralXScene Scene11 { get; } = new SpectralXScene();
        public SpectralXScene Scene12 { get; } = new SpectralXScene();
        public SpectralXScene Scene13 { get; } = new SpectralXScene();
        public SpectralXScene Scene14 { get; } = new SpectralXScene();
        public SpectralXBloodWarrior? Warrior { get; private set; }
        public SpectralXRogue? Rogue { get; private set; }
        public SpectralXMonk? Monk { get; private set; }
        public SpectralXMage? Mage { get; private set; }
        public SpectralXSkeleton? Skeleton { get; private set; }
        public SpectralWaveSys WaveSystem { get; } = new();
        private bool _skeletonTexPreloaded = false;
        public IEnumerable<ISpectralEnemy> GetAllAttackableTargets()
        {
            return WaveSystem.GetAllEnemies()
                .Concat(Cows)
                .Concat(Cats)
                .Concat(TownSluts);
        }
        public List<SpectralXCow> Cows { get; } = new();
        public List<SpectralXCat> Cats { get; } = new();
        public List<SpectralXTownSlut> TownSluts { get; } = new();

        private static readonly HashSet<string> PersistentMeshes = new()
{
    "WarriorSquare",
    "RogueSquare",
    "MonkSquare",
    "MageSquare",
};
        public ISpectralCharacter? ActiveCharacter =>
            Warrior as ISpectralCharacter
            ?? Rogue as ISpectralCharacter
            ?? Monk as ISpectralCharacter
            ?? Mage as ISpectralCharacter;

        public SpectralXWeatherClass? Weather { get; private set; }

        public SpectralXLandTileMap TileMap { get; private set; } = new();
        private bool _tileMapTexturesUploaded = false;
        private bool _tilePBRUploaded = false;

        private float _lastMouseX = 0f;
        private float _lastMouseY = 0f;
        private bool _isMousePainting = false;

        public SpectralXSun Sun { get; private set; } = new SpectralXSun();

        private SpectralXLight? _sunLight = null;
        private SpectralXLight? _sunLight2 = null;



        public int ActiveScene { get; private set; } = 1;
        private const float PortalTriggerRadius = 1.5f;
        public PerformanceMonitor Performance { get; } = new();
        public bool IsRunning { get; private set; }

        private bool _warriorTexPreloaded = false;
        private bool _rogueTexPreloaded = false;
        private bool _monkTexPreloaded = false;
        private bool _mageTexPreloaded = false;
        private bool _warriorTexCache = false;

        private readonly HashSet<string> _expectedMeshIds = new();
        public int UploadedMeshCount => _uploadedMeshes.Count;
        public int ExpectedMeshCount => _expectedMeshIds.Count;
        public SpectralProps? StaticProps { get; private set; }

        private readonly HashSet<string> _uploadedTextures = new();
        private readonly HashSet<string> _uploadedMeshes = new(); 

        private readonly DateTime _startTime = DateTime.UtcNow;

        public SpectralXLight? PrimaryLight =>
    Scene.Lights.Count > 0 ? Scene.Lights[0] : null;

        private readonly SpectralXPropScatter _propScatter = new SpectralXPropScatter(seed: 42);
        private List<FoliageInstanceGroup> _foliageGroups = new();
        private List<FoliageInstanceGroup>? _cachedFoliageFrameData = null;

        private readonly List<WebGLMeshData> _meshDataCache = new();
        private bool _meshCacheDirty = true;

        private float[] _lightPositionsBuf = new float[32 * 3];
        private float[] _lightColorsBuf = new float[32 * 3];
        private float[] _lightDirsBuf = new float[32 * 3];
        private float[] _lightIntsBuf = new float[32];
        private float[] _lightRangesBuf = new float[32];
        private int[] _lightTypesBuf = new int[32];
        private float[] _lightSpotsBuf = new float[32];
        private bool[] _lightShadowsBuf = new bool[32];
        private float[][] _lightVPsBuf = new float[32][];

        private readonly List<SpectralXLight> _activeLightsCache = new();
        private int _lastLightCount = -1;
        private bool _lightsDirty = true;

        private static readonly Dictionary<string, int> _gizmoMap = new()
{
   // ── Scene 1 ──────────────────────────────────────────────────────────
    { "S1_LightGizmo_L1",       0 },
    { "S1_LightCore_L1",        0 },
    { "S1_LightAuraInner_L1",   0 },
    { "S1_LightAuraOuter_L1",   0 },
    { "S1_LightGizmo_L2",       1 },
    { "S1_LightAura_L2",        1 },
    { "S1_LightGizmo_L3",       2 },
    { "S1_LightAura_L3",        2 },

    // ── Scene 2 ──────────────────────────────────────────────────────────
{ "S2_LightGizmo_L1",           0 },
{ "S2_LightCore_L1",            0 },
{ "S2_LightAuraInner_L1",       0 },
{ "S2_LightAuraOuter_L1",       0 },
{ "S2_LightGizmo_L2",           1 },
{ "S2_LightAura_L2",            1 },
{ "S2_LightGizmo_L3",           2 },
{ "S2_LightAura_L3",            2 },
{ "S2_SpotGizmo_L1",            3 },
{ "S2_SpotAura_L1",             3 },
{ "S2_AreaGizmo_L1",            4 },
{ "S2_AreaAura_L1",             4 },
{ "S2_RedSpotGizmo_L1",         5 },
{ "S2_RedSpotAura_L1",          5 },
{ "S2_GreenPointGizmo_L1",      6 },
{ "S2_GreenPointAura_L1",       6 },
{ "S2_PurplePointGizmo_L1",     7 },
{ "S2_PurplePointAura_L1",      7 },
{ "S2_OrangePointGizmo_L1",     8 },
{ "S2_OrangePointAura_L1",      8 },
{ "S2_PurpleAreaGizmo_L1",      9 },
{ "S2_PurpleAreaAura_L1",       9 },
{ "S2_CyanPointGizmo_L1",       10 },
{ "S2_CyanPointAura_L1",        10 },
{ "S2_DeepBluePointGizmo_L1",   11 },
{ "S2_DeepBluePointAura_L1",    11 },
{ "S2_WarmYellowPointGizmo_L1", 12 },
{ "S2_WarmYellowPointAura_L1",  12 },
{ "S2_ColdWhitePointGizmo_L1",  13 },
{ "S2_ColdWhitePointAura_L1",   13 },
{ "S2_SicklyGreenPointGizmo_L1",14 },
{ "S2_SicklyGreenPointAura_L1", 14 },
{ "S2_DeepRedPointGizmo_L1",    15 },
{ "S2_DeepRedPointAura_L1",     15 },
{ "S2_PinkPointGizmo_L1",       16 },
{ "S2_PinkPointAura_L1",        16 },

// ── Scene 3 ──────────────────────────────────────────────────────────
{ "S3_LightGizmo_L1",       0 },
{ "S3_LightCore_L1",        0 },
{ "S3_LightAuraInner_L1",   0 },
{ "S3_LightAuraOuter_L1",   0 },
{ "S3_LightGizmo_L2",       1 },
{ "S3_LightAura_L2",        1 },
{ "S3_LightGizmo_L3",       2 },
{ "S3_LightAura_L3",        2 },

// ── Scene 4 ──────────────────────────────────────────────────────────
{ "S4_LightGizmo_L1",       0 },
{ "S4_LightCore_L1",        0 },
{ "S4_LightAuraInner_L1",   0 },
{ "S4_LightAuraOuter_L1",   0 },
{ "S4_LightGizmo_L2",       1 },
{ "S4_LightAura_L2",        1 },
{ "S4_LightGizmo_L3",       2 },
{ "S4_LightAura_L3",        2 },

// ── Scene 5 ──────────────────────────────────────────────────────────
{ "S5_LightGizmo_L1",       0 },
{ "S5_LightCore_L1",        0 },
{ "S5_LightAuraInner_L1",   0 },
{ "S5_LightAuraOuter_L1",   0 },
{ "S5_LightGizmo_L2",       1 },
{ "S5_LightAura_L2",        1 },
{ "S5_LightGizmo_L3",       2 },
{ "S5_LightAura_L3",        2 },

};





        public (float screenX, float screenY) ProjectToScreen(Vector3 worldPos)
        {
            float aspect = (float)Viewport.ViewportWidth / Viewport.ViewportHeight;

            CustomMat4 view;
            CustomMat4 proj;

            switch (ActiveCameraMode)
            {
                case CameraMode.Orthographic:
                    (view, proj) = OrthoCamera.GetMatrices(aspect);
                    break;
                case CameraMode.Orbit:
                    (view, proj) = OrbitCamera.GetMatrices(aspect);
                    break;
                default:
                    view = Camera.GetViewMatrix();
                    proj = CustomMat4.CreatePerspective(
                        90f * (MathF.PI / 180f), aspect, 0.1f, 2000f);
                    break;
            }

            CustomMat4 vp = proj * view;

            float x = worldPos.X * vp.M[0] + worldPos.Y * vp.M[4] + worldPos.Z * vp.M[8] + vp.M[12];
            float y = worldPos.X * vp.M[1] + worldPos.Y * vp.M[5] + worldPos.Z * vp.M[9] + vp.M[13];
            float w = worldPos.X * vp.M[3] + worldPos.Y * vp.M[7] + worldPos.Z * vp.M[11] + vp.M[15];

            float ndcX = x / w;
            float ndcY = y / w;

            float screenX = (ndcX * 0.5f + 0.5f) * Viewport.ViewportWidth;
            float screenY = (1.0f - (ndcY * 0.5f + 0.5f)) * Viewport.ViewportHeight;

            return (screenX, screenY);
        }


        public SpectralXEngine(
        SpectralXCanvas spectralX,
        SpectralXViewport viewport,
        SpectralXCamera camera,
        SpectralXInput input,
        GamepadService gamepad,
        IJSRuntime js)
        {
            SpectralXGLX = spectralX;
            Viewport = viewport;
            Camera = camera;
            Input = input;
            _gamepad = gamepad;
            MeshLibrary = new SpectralXMeshLibrary();
            Scene = new SpectralXScene();
            _js = js;

        }


        public SpectralXMesh AddText(
    string text,
    Vector3 position,
    float fontSize = 1f,
    string fontKey = "Diablo",
    Vector4? color = null,
    float letterSpacing = 0f,
    TextAlignment align = TextAlignment.Left)
        {
            var mesh = MeshLibrary.CreateTextMesh(text, fontKey, fontSize, position, align);
            mesh.Color = color ?? new Vector4(1f, 1f, 1f, 1f);
            mesh.LetterSpacing = letterSpacing;
            GetActiveScene().AddMesh(mesh);
            Console.WriteLine($"[Engine] AddText: '{text}' font:{fontKey} size:{fontSize}");
            return mesh;
        }


        private SpectralXMesh CreateGizmoFrom(string gizmoName, string sourceMeshName)
        {
            var gizmo = new SpectralXMesh(gizmoName);
            var source = MeshLibrary.GetMesh(sourceMeshName) as SpectralXMesh;
            if (source != null && source.Vertices.Count > 0)
            {
                gizmo.Vertices.AddRange(source.Vertices);
                gizmo.Normals.AddRange(source.Normals);
                gizmo.UVs.AddRange(source.UVs);
                foreach (var face in source.Faces)
                    gizmo.Faces.Add(face);
            }
            else
            {
               
                gizmo.JSSourceMesh = sourceMeshName;
            }
            return gizmo;
        }
     
        private SpectralXMesh AddSignpost(SpectralXScene targetScene, string uniqueName, Vector3 position)
        {
            var signpost = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (signpost == null) return null;

            signpost.Name = uniqueName; // 
            signpost.Position = position;
            signpost.Size = new Vector3(1f, 1f, 1f);
            signpost.Rotation += new Vector3(0f, 85f, 0f);
            signpost.TextureDataUrl = "iAssets/SignPost001.png";
            signpost.TextureIsRawRGBA = false;

            targetScene.AddMesh(signpost);
            return signpost;
        }
        public enum CharacterClass { Warrior, Rogue, Monk, Mage }
        public void SwitchToScene(int sceneId)
        {

            _ = _js.InvokeVoidAsync("SpectralGLInterop.resetWebGPUMeshes");
            _js.InvokeVoidAsync("SpectralGLLoader.reset",
             SceneID.Is3DScene(ActiveScene) || SceneID.IsBWPScene(ActiveScene) || SceneID.IsTestScene(ActiveScene) || SceneID.IsWebViewScene(ActiveScene),
             false);
            // Clear stale shadow VP matrices — prevents old ortho frustum bleeding into new scene
            for (int i = 0; i < 32; i++)
                _lightVPsBuf[i] = CustomMat4.Identity().M;

            var previousCharacterClass = Warrior != null ? CharacterClass.Warrior
                : Rogue != null ? CharacterClass.Rogue
                : Monk != null ? CharacterClass.Monk
                : Mage != null ? CharacterClass.Mage
                : (CharacterClass?)null;

            bool carryCharacter = SceneID.IsBWPScene(ActiveScene) && SceneID.IsBWPScene(sceneId);

            ISpectralCharacter? carriedCharacter = carryCharacter ? ActiveCharacter : null;

            Warrior = null;
            Rogue = null;
            Monk = null;
            Mage = null;
            _meshCacheDirty = true;
            _cachedSkySphere = null;
            _cachedFoliageFrameData = null;             
            _uploadedMeshes.RemoveWhere(k => !PersistentMeshes.Contains(k));
            _uploadedTextures.Clear();
            _uploadedMeshes.Remove("SkySphere");
            _uploadedTextures.Remove("SkySphere"); 
            _foliageGroups.Clear();

            ActiveScene = sceneId;

            if (sceneId == SceneID.SpectralXTown)
            {
                _lightsDirty = true;        
              //  _lightsWereOnAtNight = true;  
                _sunLight = null;              
                _lastLightCount = -1;          
                _ = _js.InvokeVoidAsync("SpectralGLInterop.resetTileMap");
                _ = _js.InvokeVoidAsync("SpectralGLInterop.resetFoliage");
                _ = _js.InvokeVoidAsync("SpectralGLInterop.resetParticles");
                _ = _js.InvokeVoidAsync("SpectralGLInterop.clearAllShadowMaps");
                Scene2.Clear();
                InitScene2();
                _tileMapTexturesUploaded = false;
                _tilePBRUploaded = false;
                SetCameraMode(CameraMode.FreeCam);
                Camera.Position = new CustomVec3(54, -54, 4);
            }
            else if (sceneId == SceneID.Home)
            {
                _lightsDirty = true;         
              //  _lightsWereOnAtNight = true;   
                _sunLight = null;              
                _lastLightCount = -1;          
                Weather?.Reset();
                _ = _js.InvokeVoidAsync("SpectralGLInterop.resetTileMap");
                _ = _js.InvokeVoidAsync("SpectralGLInterop.resetFoliage");
                _ = _js.InvokeVoidAsync("SpectralGLInterop.resetParticles");
                _ = _js.InvokeVoidAsync("SpectralGLInterop.clearAllShadowMaps");
                Scene3.Clear();
                InitScene3();
                _tileMapTexturesUploaded = false;
                _tilePBRUploaded = false;          // ← reset PBR guard for new scene
                SetCameraMode(CameraMode.WebpageView);
                Camera.Position = new CustomVec3(0, -10, 4);
                //  _ = _js.InvokeVoidAsync("SpectralGLInterop.resetCubeCity");
            }
            else if (sceneId == SceneID.BWPScene1)
            {
                ResetBWPSceneState();
                Scene4.Clear();
                InitScene4();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene1 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene2)
            {
                ResetBWPSceneState();
                Scene5.Clear();
                InitScene5();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene2 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene3)
            {
                ResetBWPSceneState();
                Scene6.Clear();
                InitScene6();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene3 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene4)
            {
                ResetBWPSceneState();
                Scene7.Clear();
                InitScene7();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene4 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene5)
            {
                ResetBWPSceneState();
                Scene8.Clear();
                InitScene8();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene5 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene6)
            {
                ResetBWPSceneState();
                Scene9.Clear();
                InitScene9();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene6 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene7)
            {
                ResetBWPSceneState();
                Scene10.Clear();
                InitScene10();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene7 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene8)
            {
                ResetBWPSceneState();
                Scene11.Clear();
                InitScene11();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene8 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene9)
            {
                ResetBWPSceneState();
                Scene12.Clear();
                InitScene12();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene9 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene10)
            {
                ResetBWPSceneState();
                Scene13.Clear();
                InitScene13();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene10 loaded — Orthographic mode set");
            }
            else if (sceneId == SceneID.BWPScene11)
            {
                ResetBWPSceneState();
                Scene14.Clear();
                InitScene14();
                Camera.Position = _pendingSpawnPos.HasValue
                    ? new CustomVec3(_pendingSpawnPos.Value.X, _pendingSpawnPos.Value.Y, _pendingSpawnPos.Value.Z)
                    : new CustomVec3(0, -10, 4);
                _pendingSpawnPos = null;
                SetCameraMode(CameraMode.Orthographic);
                if (carryCharacter && previousCharacterClass.HasValue)
                    RespawnCharacterAt(previousCharacterClass.Value, carriedCharacter);
                Console.WriteLine("[SpectralXEngine] BWPScene11 loaded — Orthographic mode set");
            }

            else if (SceneID.IsTestScene(sceneId))
            {
                _lastLightCount = -1;  
                _lightsDirty = true;
                _cachedSkySphere = null;
                Weather?.Reset();
                Scene.Clear();
                Init();
                Camera.Position = new CustomVec3(0, -10, 4);
                SetCameraMode(CameraMode.FreeCam);
            }
            else
            {
                Console.WriteLine($"[SpectralXEngine] WARNING: SwitchToScene called with unhandled sceneId: {sceneId}");
            }


        }

        private void RespawnCharacterAt(CharacterClass cls, ISpectralCharacter? carried)
        {
            var activeScene = GetActiveScene();
            var mesh = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (mesh == null) return;

            string meshName = cls switch
            {
                CharacterClass.Warrior => "WarriorSquare",
                CharacterClass.Rogue => "RogueSquare",
                CharacterClass.Monk => "MonkSquare",
                CharacterClass.Mage => "MageSquare",
                _ => "CharSquare"
            };
            mesh.Name = meshName;
            mesh.CastsShadow = false;
            mesh.Position = new Vector3(0f, 0f, 1.0f); 

            switch (cls)
            {
                case CharacterClass.Warrior:
                    Warrior = (carried as SpectralXBloodWarrior) ?? new SpectralXBloodWarrior();
                    Warrior.InitMesh(mesh, activeScene, MeshLibrary);
                    break;
                case CharacterClass.Rogue:
                    Rogue = (carried as SpectralXRogue) ?? new SpectralXRogue();
                    Rogue.InitMesh(mesh, activeScene, MeshLibrary);
                    break;
                case CharacterClass.Monk:
                    Monk = (carried as SpectralXMonk) ?? new SpectralXMonk();
                    Monk.InitMesh(mesh, activeScene, MeshLibrary);
                    break;
                case CharacterClass.Mage:
                    Mage = (carried as SpectralXMage) ?? new SpectralXMage();
                    Mage.InitMesh(mesh, activeScene, MeshLibrary);
                    break;
            }

            activeScene.AddMesh(mesh);

            if (ActiveCharacter != null)
            {
                ActiveCharacter.WorldX = 0f;
                ActiveCharacter.WorldY = 0f;
                ActiveCharacter.WorldZ = 1.0f;
                if (ActiveCharacter.CharMesh != null)
                    ActiveCharacter.CharMesh.Position = new Vector3(0f, 0f, 1.0f);
            }
        }

        private void ResetBWPSceneState()
        {
            foreach (var cow in Cows) DespawnCow(cow);
            foreach (var cat in Cats) DespawnCat(cat);
            foreach (var ts in TownSluts) DespawnTownSlut(ts);
            WaveSystem.ClearAll();
            SplatterPuddleRegistry.Clear(GetActiveScene());
            Cows.Clear();
            Cats.Clear();
            TownSluts.Clear();
            _lastLightCount = -1;   
            _lightsDirty = true;
            _cachedSkySphere = null;
            _warriorTexPreloaded = false;
            _rogueTexPreloaded = false;
            _monkTexPreloaded = false;
            _mageTexPreloaded = false;
            _breakTexPreloaded = false;
            _splatterTexPreloaded = false;
            _skeletonTexPreloaded = false;
            _psychoSkeletonTexPreloaded = false;
            _zombiePsychoTexPreloaded = false;
            _skeletonWarTexPreloaded = false;
            _goatmanTexPreloaded = false;
            _scavBossTexPreloaded = false;
            _skeletonBossTexPreloaded = false;
            _cowTexPreloaded = false;
            _catTexPreloaded = false;
            _townSlutTexPreloaded = false;
            _ = _js.InvokeVoidAsync("SpectralGLInterop.resetParticles");
            _tileMapTexturesUploaded = false;
            _tilePBRUploaded = false;
            TileMap.MarkDirty();
            _ = _js.InvokeVoidAsync("SpectralGLInterop.resetTileMap");
            _ = _js.InvokeVoidAsync("SpectralGLInterop.clearAllShadowMaps");
        }
        private SpectralXScene GetActiveScene()
        {
            return ActiveScene switch
            {
                SceneID.SpectralXDemo=>Scene,
                SceneID.SpectralXTown => Scene2,
                SceneID.Home => Scene3,
                SceneID.BWPScene1 => Scene4,
                SceneID.BWPScene2 => Scene5,
                SceneID.BWPScene3 => Scene6,
                SceneID.BWPScene4 => Scene7,
                SceneID.BWPScene5 => Scene8,
                SceneID.BWPScene6 => Scene9,
                SceneID.BWPScene7 => Scene10,
                SceneID.BWPScene8 => Scene11,
                SceneID.BWPScene9 => Scene12,
                SceneID.BWPScene10 => Scene13,
                SceneID.BWPScene11 => Scene14,

                _ => Scene
            };
        }

        private readonly struct SceneTransitionTrigger
        {
            public readonly int FromScene;
            public readonly Vector3 TriggerPos;
            public readonly int ToScene;
            public readonly Vector3 SpawnPos;

            public SceneTransitionTrigger(int fromScene, Vector3 triggerPos, int toScene, Vector3 spawnPos)
            {
                FromScene = fromScene;
                TriggerPos = triggerPos;
                ToScene = toScene;
                SpawnPos = spawnPos;
            }
        }

        private static readonly SceneTransitionTrigger[] _sceneTransitions = new[]
   {
    // ── Demo / Town 3D scenes ────────────────────────────────────────────
    new SceneTransitionTrigger(
        fromScene: SceneID.SpectralXDemo,
        triggerPos: new Vector3(0, 11, 11),
        toScene: SceneID.SpectralXTown,
        spawnPos: new Vector3(0, -10, 4)),

    new SceneTransitionTrigger(
        fromScene: SceneID.SpectralXTown,
        triggerPos: new Vector3(64, -54, 2),
        toScene: SceneID.SpectralXDemo,
        spawnPos: new Vector3(0, -10, 4)),

    // ── Blood Town ───────────────────────────────────────────────────────
    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene1,
        triggerPos: new Vector3(-22f, 0f, 1f),  // west to blood town
        toScene: SceneID.BWPScene2,
        spawnPos: new Vector3(20f, 0f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene2,
        triggerPos: new Vector3(-20f, 0f, 1f),  // east back to forest 1
        toScene: SceneID.BWPScene1,
        spawnPos: new Vector3(-18f, 0f, 1f)),

    // ── Forest chain north/south ─────────────────────────────────────────
    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene1,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to forest 2
        toScene: SceneID.BWPScene3,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene3,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to forest 1
        toScene: SceneID.BWPScene1,
        spawnPos: new Vector3(0f, 18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene3,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to forest 3
        toScene: SceneID.BWPScene4,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene4,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to forest 2
        toScene: SceneID.BWPScene3,
        spawnPos: new Vector3(0f, 18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene4,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to forest 4
        toScene: SceneID.BWPScene5,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene5,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to forest 3
        toScene: SceneID.BWPScene4,
        spawnPos: new Vector3(0f, 18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene5,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to forest 5
        toScene: SceneID.BWPScene6,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene6,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to forest 4
        toScene: SceneID.BWPScene5,
        spawnPos: new Vector3(0f, 18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene6,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to forest 6
        toScene: SceneID.BWPScene7,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene7,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to forest 5
        toScene: SceneID.BWPScene6,
        spawnPos: new Vector3(0f, 18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene7,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to dark forest 1
        toScene: SceneID.BWPScene8,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene8,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to forest 6
        toScene: SceneID.BWPScene7,
        spawnPos: new Vector3(0f, 18f, 1f)),

    // ── Snow Town ────────────────────────────────────────────────────────
    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene8,
        triggerPos: new Vector3(0f, 22f, 1f),   // north to snow town
        toScene: SceneID.BWPScene11,
        spawnPos: new Vector3(0f, -18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene11,
        triggerPos: new Vector3(0f, -22f, 1f),  // south back to dark forest
        toScene: SceneID.BWPScene8,
        spawnPos: new Vector3(0f, 18f, 1f)),

    // ── Graveyard ────────────────────────────────────────────────────────
    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene8,
        triggerPos: new Vector3(22f, 0f, 1f),   // east to graveyard
        toScene: SceneID.BWPScene9,
        spawnPos: new Vector3(-18f, 0f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene9,
        triggerPos: new Vector3(-22f, 0f, 1f),  // west back to dark forest
        toScene: SceneID.BWPScene8,
        spawnPos: new Vector3(18f, 0f, 1f)),

    // ── Caves ────────────────────────────────────────────────────────────
    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene1,
        triggerPos: new Vector3(0f, -22f, 1f),  // south to caves
        toScene: SceneID.BWPScene10,
        spawnPos: new Vector3(0f, 18f, 1f)),

    new SceneTransitionTrigger(
        fromScene: SceneID.BWPScene10,
        triggerPos: new Vector3(0f, 22f, 1f),   // north back to forest 1
        toScene: SceneID.BWPScene1,
        spawnPos: new Vector3(0f, -18f, 1f)),
};

   
        private static Vector3 GetTriggerPos(int fromScene, int toScene)
        {
            foreach (var t in _sceneTransitions)
                if (t.FromScene == fromScene && t.ToScene == toScene)
                    return t.TriggerPos;
            return Vector3.Zero;
        }

        private void CheckPortalTrigger()
        {
           
            Vector3 currentPos = GetPlayerWorldPosition();

            foreach (var trigger in _sceneTransitions)
            {
                if (trigger.FromScene != ActiveScene) continue;

                float dx = currentPos.X - trigger.TriggerPos.X;
                float dy = currentPos.Y - trigger.TriggerPos.Y;
                float dz = currentPos.Z - trigger.TriggerPos.Z;
                float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                if (dist < PortalTriggerRadius)
                {
                    _pendingSpawnPos = trigger.SpawnPos;
                    SwitchToScene(trigger.ToScene);
                    return; 
                }
            }
        }

        private Vector3? _pendingSpawnPos = null;

        private Vector3 GetPlayerWorldPosition()
        {
            if (SceneID.IsBWPScene(ActiveScene) && ActiveCharacter?.CharMesh != null)
                return ActiveCharacter.CharMesh.Position;

            if (ActiveCameraMode == CameraMode.Orthographic)
                return new Vector3(OrthoCamera.TargetX, OrthoCamera.TargetY, 0f);

            if (ActiveCameraMode == CameraMode.Orbit)
            {
                var (ox, oy, oz) = OrbitCamera.GetPosition();
                return new Vector3(ox, oy, oz);
            }

            return new Vector3(Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
        }

        private float _lastFrameDelta = 0f;

        [JSInvokable("OnCanvasResized")]
        public void OnCanvasResized(int width, int height)
        {
            Viewport.SetSize(width, height);
            Console.WriteLine($"[SpectralX] Canvas resized to {width}x{height}");
        }


        [JSInvokable("TickAndGetFrame")]
        public WebGLFrameData TickAndGetFrame()
        {
            Performance.StartFrame();

            float now = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            _lastFrameDelta = Math.Min(now - _lastTickTime, 0.1f); 

            Input.ProcessHeldKeys();
            Input.HandleGamepadInput();



            bool isBWP = SceneID.IsBWPScene(ActiveScene);
            var bwpScene = GetActiveScene(); 

            if (isBWP)
                ActiveCharacter?.Tick(_lastFrameDelta);
            //Disabled for testing
            /*
            if (isBWP && ActiveCharacter?.CharMesh != null && bwpScene.Lights.Count > 0)
                bwpScene.Lights[0].Position = ActiveCharacter.CharMesh.Position + new Vector3(-0.5f, 0, 1f);
            */
            if (isBWP && ActiveCharacter != null)
                SpectralDynManager.TickAll(ActiveCharacter, _lastFrameDelta);
            if (isBWP && ActiveCharacter != null)
                SpectralBreakManager.TickAll(ActiveCharacter, _lastFrameDelta);
            if (isBWP && ActiveCharacter != null)
                WaveSystem.TickAll(ActiveCharacter, _lastFrameDelta);
            if (isBWP && ActiveCharacter != null)
                TickAmbientTraps(ActiveCharacter, _lastFrameDelta);
            if (isBWP)
                SplatterPuddleRegistry.Tick(bwpScene, now);
            if (SceneID.IsBWPScene(ActiveScene))
                SpectralUndeadGFRegistry.TickAll(ActiveCharacter, _lastFrameDelta, WaveSystem.GetAllEnemies());

            TickAnimations();                
            TickWeather();



            // if (ActiveScene == SceneID.Home)
            //   TickCubeCityBuilder(_lastFrameDelta);


            SyncLightGizmos();
            SyncSkySphere();
            TickSun();
            
            if (ActiveScene == SceneID.Home && ActiveCameraMode == CameraMode.WebpageView)
            {
                Camera.TickRail(_lastFrameDelta);
            }


            else if (SceneID.IsBWPScene(ActiveScene) && ActiveCameraMode == CameraMode.Orthographic)
            {
                if (OrthoCamera.LockToPlayer && ActiveCharacter != null)
                    OrthoCamera.Tick(_lastFrameDelta, Viewport.ViewportWidth, Viewport.ViewportHeight,
                        ActiveCharacter.WorldX, ActiveCharacter.WorldY);
                else
                    OrthoCamera.Tick(_lastFrameDelta, Viewport.ViewportWidth, Viewport.ViewportHeight);
            }
            if (SceneID.IsTestScene(ActiveScene) && ActiveCameraMode != CameraMode.FreeCam)
            {
                SetCameraMode(CameraMode.FreeCam);
            }
            
          //  TickSceneLighting();
            CheckPortalTrigger();
            Performance.EndFrame();
            return BuildWebGLFrame();
        }

        private SpectralXMesh? _cachedSkySphere = null;

        private void SyncSkySphere()
        {
            var activeScene = GetActiveScene();
            if (activeScene.Meshes.Count == 0) return; 
            if (_cachedSkySphere == null)
                _cachedSkySphere = activeScene.Meshes.OfType<SpectralXMesh>()
                    .FirstOrDefault(m => m.Name == "SkySphere");
            if (_cachedSkySphere == null) return;


            float skyX, skyY, skyZ;
            if (SceneID.IsBWPScene(ActiveScene))
            {
                var (ox, oy, oz) = OrthoCamera.GetPosition();
                skyX = ox; skyY = oy; skyZ = 0f;
            }
            else if (ActiveScene == SceneID.Home)
            {
                skyX = Camera.Position.X; skyY = Camera.Position.Y; skyZ = 0f;
            }
            else
            {
                skyX = Camera.Position.X; skyY = Camera.Position.Y; skyZ = Camera.Position.Z;
            }
            _cachedSkySphere.Position = new Vector3(skyX, skyY, skyZ);
            _cachedSkySphere.TextureDirty = true;

        }

        
        private void TickSun()
        {
            bool isSunScene = ActiveScene == SceneID.SpectralXTown ||
                               ActiveScene == SceneID.Home ||
                               SceneID.IsBWPScene(ActiveScene);
            if (!isSunScene || _sunLight == null) return;

            float now = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            float delta = now - _lastTickTime;
            Sun.Tick(delta);
            Sun.Apply(_sunLight);
            /*
            Console.WriteLine($"[Sun] TimeOfDay: {Sun.TimeOfDay:F3} Dir: {_sunLight.Direction.X:F3},{_sunLight.Direction.Y:F3},{_sunLight.Direction.Z:F3} Intensity: {_sunLight.Intensity:F3}");
            */
            _lightsDirty = true; 
        }

        private float _lastWeatherTick = 0f;


        private void TickWeather()
        {
            if (Weather == null) return;
            bool isWeatherScene = ActiveScene == SceneID.SpectralXTown ||
                                  ActiveScene == SceneID.Home ||
                                  SceneID.IsBWPScene(ActiveScene);
            if (!isWeatherScene) return;


            float now = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            float delta = now - _lastWeatherTick;
            _lastWeatherTick = now;
            Weather.Tick(delta, Camera);
        }

        // Disabled for light and shadow tests

        /*
        private bool _lightsWereOnAtNight = true;  // force sync on first tick
                
        
        private void TickSceneLighting()
        {
            bool isLightingScene = ActiveScene == SceneID.SpectralXTown ||
                                    ActiveScene == SceneID.Home ||
                                    SceneID.IsBWPScene(ActiveScene);
            if (!isLightingScene || _sunLight == null) return;

            float t = Sun.TimeOfDay;
            bool isNight = t < 0.25f || t > 0.75f;

            if (isNight == _lightsWereOnAtNight) return;
            _lightsWereOnAtNight = isNight;

            // Error in this sytem fixed. for scene 2

            // But overall Adding scene 3 to this shouldnt cause a light miscast.

            // adding scene 2 this requires the directiona light force light in active lights to get shadows to cast form objects, this is wrong.

            var scenes = new[] { Scene2,Scene4, Scene5, Scene6, Scene7, Scene8, Scene9, Scene10, Scene11, Scene12, Scene13, Scene14 };

            foreach (var scene in scenes)
            {
                foreach (var light in scene.Lights.Where(l => l.Type == LightType.Point))
                {
                    light.Enabled = isNight;
                }
            }
            _lightsDirty = true;
        }
         */

        private float _lastTickTime = 0f;
        private void TickAnimations()
        {
            float now = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            float delta = now - _lastTickTime;
            _lastTickTime = now;

            var activeScene = GetActiveScene();

            foreach (var mesh in activeScene.Meshes)
            {
                if (mesh is not SpectralXMesh sm) continue;
                if (sm.GlowPulseSpeed <= 0f) continue;

                float pulse = (MathF.Sin(now * sm.GlowPulseSpeed) * 0.5f + 0.5f);
                sm.GlowRadius = sm.GlowPulseMin + pulse * (sm.GlowPulseMax - sm.GlowPulseMin);
               
            }
            foreach (var mesh in activeScene.Meshes)
            {
                if (mesh is not SpectralXMesh sm || !sm.IsAnimated) continue;

                sm.FrameTimer += delta;

                float frameDuration = 1f / sm.FrameRate;
                while (sm.FrameTimer >= frameDuration)
                {
                    sm.FrameTimer -= frameDuration;
                    sm.CurrentFrame = (sm.CurrentFrame + 1) % sm.FrameCount;
                    if (sm.SheetWidth <= 0f || sm.SheetHeight <= 0f ||
                        sm.FramePixelWidth <= 0f || sm.FramePixelHeight <= 0f) continue;

                    float cols = sm.SheetWidth / sm.FramePixelWidth;
                    float rows = sm.SheetHeight / sm.FramePixelHeight;

                    int frameX = sm.CurrentFrame % (int)cols;
                    int frameY = sm.CurrentFrame / (int)cols;

                    float frameScaleX = sm.FramePixelWidth / sm.SheetWidth;
                    sm.UVScaleX = sm.FacingRight ? frameScaleX : -frameScaleX;
                    sm.UVScaleY = sm.FramePixelHeight / sm.SheetHeight;
                    sm.UVOffsetX = sm.FacingRight ? frameX * frameScaleX : frameX * frameScaleX + frameScaleX;
                    sm.UVOffsetY = frameY * sm.UVScaleY;
                }
            }
        }



        private void SyncLightGizmos()
        {
            var activeScene = GetActiveScene();


            foreach (var mesh in activeScene.Meshes)
            {
                if (_gizmoMap.TryGetValue(mesh.Name, out int lightIndex))
                {
                    if (lightIndex < activeScene.Lights.Count)
                        mesh.Position = activeScene.Lights[lightIndex].Position;
                }
            }
        }

        private bool _breakTexPreloaded = false;
        private bool _splatterTexPreloaded = false;

        private BreakTexSwap[]? BuildBreakTexSwaps()
        {
            var swaps = new List<BreakTexSwap>();
            foreach (var dummy in SpectralBreakables.DummyRegistry.All)
            {
                if (dummy.BreakMesh == null || !dummy.BreakMesh.TextureDirty) continue;
                swaps.Add(new BreakTexSwap
                {
                    MeshId = dummy.BreakMesh.Name,
                    TexUrl = dummy.BreakMesh.TextureDataUrl ?? ""
                });
                dummy.BreakMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private bool _psychoSkeletonTexPreloaded = false;

        private bool _zombiePsychoTexPreloaded = false;
        private bool _skeletonWarTexPreloaded = false;
        private bool _goatmanTexPreloaded = false;
        private bool _scavBossTexPreloaded = false;
        private bool _skeletonBossTexPreloaded = false;
        private bool _cowTexPreloaded = false;
        private bool _catTexPreloaded = false;
        private bool _townSlutTexPreloaded = false;



        private EnemyTexSwap[]? BuildPsychoSkeletonTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();

            foreach (var psycho in WaveSystem.PsychoSkeletons)
            {
                if (psycho.EnemyMesh == null || !psycho.EnemyMesh.TextureDirty)
                    continue;

                swaps.Add(new EnemyTexSwap
                {
                    MeshId = psycho.EnemyMesh.Name,
                    TexUrl = psycho.EnemyMesh.TextureDataUrl ?? ""
                });

                psycho.EnemyMesh.TextureDirty = false;
            }

            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildSkeletonTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var skeleton in WaveSystem.Skeletons)
            {
                if (skeleton.EnemyMesh == null || !skeleton.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = skeleton.EnemyMesh.Name,
                    TexUrl = skeleton.EnemyMesh.TextureDataUrl ?? ""
                });
                skeleton.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildZombiePsychoTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var zombiePsycho in WaveSystem.ZombiePsycho)
            {
                if (zombiePsycho.EnemyMesh == null || !zombiePsycho.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = zombiePsycho.EnemyMesh.Name,
                    TexUrl = zombiePsycho.EnemyMesh.TextureDataUrl ?? ""
                });
                zombiePsycho.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildSkeletonWarTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var skeletonWar in WaveSystem.SkeletonWar)
            {
                if (skeletonWar.EnemyMesh == null || !skeletonWar.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = skeletonWar.EnemyMesh.Name,
                    TexUrl = skeletonWar.EnemyMesh.TextureDataUrl ?? ""
                });
                skeletonWar.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildGoatmanTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var goatman in WaveSystem.Goatman)
            {
                if (goatman.EnemyMesh == null || !goatman.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = goatman.EnemyMesh.Name,
                    TexUrl = goatman.EnemyMesh.TextureDataUrl ?? ""
                });
                goatman.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildScavBossTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var scavBoss in WaveSystem.ScavBoss)
            {
                if (scavBoss.EnemyMesh == null || !scavBoss.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = scavBoss.EnemyMesh.Name,
                    TexUrl = scavBoss.EnemyMesh.TextureDataUrl ?? ""
                });
                scavBoss.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildSkeletonBossTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var skeletonBoss in WaveSystem.SkeletonBoss)
            {
                if (skeletonBoss.EnemyMesh == null || !skeletonBoss.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = skeletonBoss.EnemyMesh.Name,
                    TexUrl = skeletonBoss.EnemyMesh.TextureDataUrl ?? ""
                });
                skeletonBoss.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildCowTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var cow in Cows) 
            {
                if (cow.EnemyMesh == null || !cow.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = cow.EnemyMesh.Name,
                    TexUrl = cow.EnemyMesh.TextureDataUrl ?? ""
                });
                cow.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildCatTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var cat in Cats)
            {
                if (cat.EnemyMesh == null || !cat.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = cat.EnemyMesh.Name,
                    TexUrl = cat.EnemyMesh.TextureDataUrl ?? ""
                });
                cat.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }

        private EnemyTexSwap[]? BuildTownSlutTexSwaps()
        {
            var swaps = new List<EnemyTexSwap>();
            foreach (var townSlut in TownSluts)
            {
                if (townSlut.EnemyMesh == null || !townSlut.EnemyMesh.TextureDirty) continue;
                swaps.Add(new EnemyTexSwap
                {
                    MeshId = townSlut.EnemyMesh.Name,
                    TexUrl = townSlut.EnemyMesh.TextureDataUrl ?? ""
                });
                townSlut.EnemyMesh.TextureDirty = false;
            }
            return swaps.Count > 0 ? swaps.ToArray() : null;
        }


        public void DespawnActiveCharacter()
        {
         
            var meshName = Warrior?.CharMesh?.Name
                        ?? Rogue?.CharMesh?.Name
                        ?? Monk?.CharMesh?.Name
                        ?? Mage?.CharMesh?.Name;

            if (meshName != null)
            {
                var activeScene = GetActiveScene();
                var mesh = activeScene.Meshes.FirstOrDefault(m => m.Name == meshName);
                if (mesh != null)
                    activeScene.RemoveMesh(mesh);
            }

            Warrior = null;
            Rogue = null;
            Monk = null;
            Mage = null;

            Console.WriteLine("[SpectralXEngine] Active character despawned");
        }


        private WebGLFrameData BuildWebGLFrame()
        {
            _meshDataCache.Clear();
            var meshDataList = _meshDataCache;

            float aspect = (float)Viewport.ViewportWidth / Viewport.ViewportHeight;

            CustomMat4 view;
            CustomMat4 proj;

             switch (ActiveCameraMode)
             {
                 case CameraMode.Orthographic:
                    (view, proj) = OrthoCamera.GetMatrices(aspect);
                    break;
                case CameraMode.Orbit:
                     (view, proj) = OrbitCamera.GetMatrices(aspect);
                     break;
                case CameraMode.WebpageView:
                 case CameraMode.FreeCam:
                 default:
            view = Camera.GetViewMatrix();
            proj = CustomMat4.CreatePerspective(90f * (MathF.PI / 180f), aspect, 0.1f, 2000f);
                break;
             }


            CustomMat4 vp = proj * view;

            var activeScene = GetActiveScene();
            if (SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.SpectralXTown || ActiveScene == SceneID.Home)
                ExtractFrustumPlanes(vp);

            foreach (var mesh in activeScene.Meshes)
            {
                if (mesh == null) continue;
                if (SceneID.IsBWPScene(ActiveScene) && mesh.Name.StartsWith("Static_"))
                {
                    float radius = mesh.Size.X * 1.5f; 
                    if (!SphereInFrustum(mesh.Position, radius))
                        continue;
                }
                CustomMat4 mvp = vp * mesh.WorldMatrix;

                if (mesh.Name.StartsWith("ParticlePool_") || mesh.Name.StartsWith("ParticleGeo_"))
                    continue;

                WebGLMeshUpload? upload = null;

                bool isParticle = mesh.Name.StartsWith("ParticlePool_");
                string jsSource = (mesh is SpectralXMesh smjs) ? smjs.JSSourceMesh : null;
                string meshBufferKey = (mesh.Name == "SkySphere") ? "SkySphere" : (jsSource ?? mesh.Name);

                string uploadKey = isParticle
             ? "ParticleGeo_" + ((mesh as SpectralXMesh)?.TextureDataUrl ?? mesh.Name)
             : (mesh.Name == "SkySphere" ? "SkySphere" : meshBufferKey);

                string texCacheKey = isParticle
                    ? "ParticleGeo_" + mesh.TextureDataUrl
                    : mesh.Name;

                if (mesh.Name == "SkySphere") jsSource = null;

                SpectralXMesh? tdm = mesh as SpectralXMesh;
                bool texDirty = tdm != null && tdm.TextureDirty;

                if (texDirty)
                {
                    _uploadedTextures.Remove(texCacheKey);

                    bool preserveMesh =
                            PersistentMeshes.Contains(uploadKey) ||
                            uploadKey.StartsWith("BreakDummy_") ||
                            uploadKey.StartsWith("SkeletonSquare_") ||
                            uploadKey.StartsWith("PsychoSkeletonSquare_") ||
                            uploadKey.StartsWith("ZombiePsychoSquare_") ||
                            uploadKey.StartsWith("SkeletonWarSquare_") ||
                            uploadKey.StartsWith("GoatmanSquare_") ||
                            uploadKey.StartsWith("ScavBossSquare_") ||
                            uploadKey.StartsWith("SkeletonBossSquare_") ||
                            uploadKey.StartsWith("CowSquare_") ||
                            uploadKey.StartsWith("CatSquare_") ||
                            uploadKey.StartsWith("TownSlutSquare_") ||
                            uploadKey.StartsWith("WarriorSquare") ||
                            uploadKey.StartsWith("RogueSquare") ||
                            uploadKey.StartsWith("MonkSquare") ||
                            uploadKey.StartsWith("MageSquare");

                    if (!preserveMesh)
                        _uploadedMeshes.Remove(uploadKey);

                    if (!uploadKey.StartsWith("BreakDummy_")
                         && !uploadKey.StartsWith("SkeletonSquare_")
                         && !uploadKey.StartsWith("PsychoSkeletonSquare_")
                         && !uploadKey.StartsWith("ZombiePsychoSquare_")
                         && !uploadKey.StartsWith("SkeletonWarSquare_")
                         && !uploadKey.StartsWith("GoatmanSquare_")
                         && !uploadKey.StartsWith("ScavBossSquare_")
                         && !uploadKey.StartsWith("SkeletonBossSquare_")
                         && !uploadKey.StartsWith("CowSquare_")
                         && !uploadKey.StartsWith("CatSquare_")
                         && !uploadKey.StartsWith("TownSlutSquare_")
                         && !uploadKey.StartsWith("WarriorSquare")
                         && !uploadKey.StartsWith("RogueSquare")
                         && !uploadKey.StartsWith("MonkSquare")
                         && !uploadKey.StartsWith("MageSquare"))
                    {
                        tdm!.TextureDirty = false;
                    }
                }




                if (!_uploadedMeshes.Contains(uploadKey))
                {
                    if (jsSource != null)
                    {
                      _uploadedMeshes.Add(uploadKey);
                    }
                    else
                    {
                        var verts = new List<float>();
                        var normals = new List<float>();
                        var uvs = new List<float>();
                        var matBreaks = new List<int>();
                        var matIndices = new List<int>();

                        int? lastMatIdx = null;
                        int vertsAtLastBreak = 0;

                        var sortedFaces = mesh.Faces;

                        foreach (var face in sortedFaces)
                        {
                            int matIdx = face.MaterialIndex;

                            if (lastMatIdx.HasValue && matIdx != lastMatIdx.Value)
                            {
                                matBreaks.Add(verts.Count / 3 - vertsAtLastBreak);
                                matIndices.Add(lastMatIdx.Value);
                                vertsAtLastBreak = verts.Count / 3;
                            }
                            lastMatIdx = matIdx;

                            int[] vertIndices;
                            int[] uvIndices;

                            if (face.Type == FaceType.Quad)
                            {
                                vertIndices = new[] { face.A, face.B, face.C, face.A, face.C, face.D };
                                uvIndices = new[] { face.UVA, face.UVB, face.UVC, face.UVA, face.UVC, face.UVD };
                            }
                            else
                            {
                                vertIndices = new[] { face.A, face.B, face.C };
                                uvIndices = new[] { face.UVA, face.UVB, face.UVC };
                            }

                            var fv0 = mesh.Vertices[face.A];
                            var fv1 = mesh.Vertices[face.B];
                            var fv2 = mesh.Vertices[face.C];
                            Vector3 edge1 = fv1 - fv0;
                            Vector3 edge2 = fv2 - fv0;
                            Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(edge1, edge2));

                            for (int i = 0; i < vertIndices.Length; i++)
                            {
                                int vertIdx = vertIndices[i];
                                int uvIdx = uvIndices[i];

                                var v = mesh.Vertices[vertIdx];
                                verts.Add(v.X); verts.Add(v.Y); verts.Add(v.Z);

                                Vector3 n;
                                if (mesh.PolygonNormals.Count > 0)
                                {
                                    int[] polyMap = face.Type == FaceType.Quad
                                        ? new[] { 0, 1, 2, 0, 2, 3 }
                                        : new[] { 0, 1, 2 };
                                    int pni = face.PolygonNormalBase + polyMap[i];
                                    n = pni < mesh.PolygonNormals.Count
                                        ? mesh.PolygonNormals[pni]
                                        : faceNormal;
                                }
                                else
                                {
                                    n = faceNormal;
                                    if (mesh.Normals.Count > vertIdx) n = mesh.Normals[vertIdx];
                                }
                                normals.Add(n.X); normals.Add(n.Y); normals.Add(n.Z);

                                if (uvIdx >= 0 && uvIdx < mesh.UVs.Count)
                                { uvs.Add(mesh.UVs[uvIdx].X); uvs.Add(mesh.UVs[uvIdx].Y); }
                                else
                                { uvs.Add(0f); uvs.Add(0f); }
                            }
                        }

                        if (lastMatIdx.HasValue)
                        {
                            matBreaks.Add(verts.Count / 3 - vertsAtLastBreak);
                            matIndices.Add(lastMatIdx.Value);
                        }

                        string? urlToSend = null;
                        if (mesh.HasTexture && mesh.TextureDataUrl != null
                            && !_uploadedTextures.Contains(texCacheKey))
                        {
                            urlToSend = mesh.TextureDataUrl;
                            _uploadedTextures.Add(texCacheKey);
                        }

                        upload = new WebGLMeshUpload
                        {
                            MeshId = uploadKey,
                            Vertices = verts.ToArray(),
                            Normals = normals.ToArray(),
                            UVs = uvs.ToArray(),
                            TextureDataUrl = urlToSend,
                            HasTexture = mesh.HasTexture,
                            TextureWidth = mesh.TextureWidth,
                            TextureHeight = mesh.TextureHeight,
                            TextureIsRawRGBA = mesh.TextureIsRawRGBA,
                            MaterialTextures = mesh.MaterialTextures.Select((t, i) => t).ToArray(),
                            MaterialColors = mesh.MaterialColors != null && mesh.MaterialColors.Count > 0
                                ? mesh.MaterialColors.Select(c => $"{c.X},{c.Y},{c.Z},{c.W}").ToArray()
                                : Array.Empty<string>(),
                            MatBreaks = matBreaks.ToArray(),
                            MatIndices = matIndices.ToArray(),
                            TextureDirty = texDirty,
                        };

                        _uploadedMeshes.Add(uploadKey);
                    }
                }
                else if (isParticle && !_uploadedMeshes.Contains(mesh.Name))
                {
                    string sharedGeoKey = "ParticleGeo_" +
                        ((mesh as SpectralXMesh)?.TextureDataUrl ?? mesh.Name);

                    upload = new WebGLMeshUpload
                    {
                        MeshId = mesh.Name,
                        Vertices = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        UVs = Array.Empty<float>(),
                        HasTexture = false,
                        TextureDataUrl = sharedGeoKey,
                        TextureIsRawRGBA = false,
                        MatBreaks = Array.Empty<int>(),
                        MatIndices = Array.Empty<int>(),
                        MaterialTextures = Array.Empty<string>(),
                        MaterialColors = Array.Empty<string>(),
                        OverlayTextureDataUrl = (mesh is SpectralXMesh om && om.OverlayDirty)
    ? om.OverlayTextureDataUrl : null,
                        OverlayAlpha = (mesh is SpectralXMesh om2) ? om2.OverlayAlpha : 0f,
                    };
                    if (_uploadedMeshes.Contains(sharedGeoKey))
                        _uploadedMeshes.Add(mesh.Name);
                }

                CustomMat4 model = mesh.WorldMatrix;

                meshDataList.Add(new WebGLMeshData
                {
                    MeshId = meshBufferKey,
                    Mvp = mvp.M,
                    Model = mesh.TransformDirty ? model.M : null,
                    Upload = upload,
                    R = mesh.Color.X,
                    G = mesh.Color.Y,
                    B = mesh.Color.Z,
                    A = mesh.Color.W,
                    IsEmissive = mesh.IsEmissive,
                    EmissiveIntensity = mesh.EmissiveIntensity,
                    UVOffsetX = (mesh is SpectralXMesh sm) ? sm.UVOffsetX : 0f,
                    UVOffsetY = (mesh is SpectralXMesh sm2) ? sm2.UVOffsetY : 0f,
                    UVScaleX = (mesh is SpectralXMesh sm3) ? sm3.UVScaleX : 1f,
                    UVScaleY = (mesh is SpectralXMesh sm4) ? sm4.UVScaleY : 1f,
                    TransformDirty = mesh.TransformDirty,
                    CastsShadow = (mesh is SpectralXMesh sm5) ? sm5.CastsShadow : true,
                    ReceivesShadow = (mesh is SpectralXMesh sm6) ? sm6.ReceivesShadow : true,
                    OverlayTextureDataUrl = (mesh is SpectralXMesh omesh && omesh.OverlayDirty)
        ? omesh.OverlayTextureDataUrl : null,
                    OverlayAlpha = (mesh is SpectralXMesh omesh2) ? omesh2.OverlayAlpha : 0f,

                });

                if (mesh is SpectralXMesh dirtyMesh)
                {
                    if (!mesh.Name.StartsWith("ParticlePool_"))
                        dirtyMesh.TransformDirty = false;
                }
                if (mesh is SpectralXMesh overlayMesh && overlayMesh.OverlayDirty)
                    overlayMesh.OverlayDirty = false;
            }

            var particleTextures = new[]
            {
        ("ParticleGeo_/iAssets/RainDrop01.png",   "/iAssets/RainDrop01.png"),
        ("ParticleGeo_/iAssets/SnowFlake06.png",  "/iAssets/SnowFlake06.png"),
        ("ParticleGeo_/iAssets/GOkuCloud001.png", "/iAssets/GOkuCloud001.png"),
        ("ParticleGeo_/iAssets/LBolt002.png",     "/iAssets/LBolt002.png"),
    };

            foreach (var (cacheKey, texPath) in particleTextures)
            {
                if (_uploadedMeshes.Contains(cacheKey)) continue;

                meshDataList.Add(new WebGLMeshData
                {
                    MeshId = cacheKey,
                    Mvp = CustomMat4.Identity().M,
                    Model = CustomMat4.Identity().M,
                    Upload = new WebGLMeshUpload
                    {
                        MeshId = cacheKey,
                        Vertices = Array.Empty<float>(),
                        Normals = Array.Empty<float>(),
                        UVs = Array.Empty<float>(),
                        HasTexture = true,
                        TextureDataUrl = texPath,
                        TextureIsRawRGBA = false,
                        MatBreaks = Array.Empty<int>(),
                        MatIndices = Array.Empty<int>(),
                        MaterialTextures = Array.Empty<string>(),
                        MaterialColors = Array.Empty<string>(),
                    },
                    R = 1f,
                    G = 1f,
                    B = 1f,
                    A = 0f,
                    IsEmissive = false,
                    EmissiveIntensity = 0f,
                    UVScaleX = 1f,
                    UVScaleY = 1f,
                });

                _uploadedMeshes.Add(cacheKey);
                _uploadedTextures.Add(cacheKey);
            }


            int currentLightCount = _activeLightsCache.Count;
            if (_lightsDirty || currentLightCount != _lastLightCount)
            {
                _activeLightsCache.Clear();
                if (_sunLight != null && _sunLight.Enabled)
                    _activeLightsCache.Add(_sunLight);

                // var seen = new HashSet<Vector3>();
                // if (_sunLight != null) seen.Add(_sunLight.Position);

                foreach (var l in activeScene.Lights)
                {
                    if (!l.Enabled) continue;
                    if (l == _sunLight) continue;
                    // if (l.Type == LightType.Directional) continue;  // DISABLED FOR TEST
                    // if (seen.Contains(l.Position)) continue;        // DISABLED FOR TEST
                    // seen.Add(l.Position);                           // DISABLED FOR TEST
                    _activeLightsCache.Add(l);
                    if (_activeLightsCache.Count == 32) break;
                }
                _lastLightCount = _activeLightsCache.Count;
                _lightsDirty = false;
            }

            var activeLights = _activeLightsCache;
            int lightCount = activeLights.Count;
            if (ActiveScene == SceneID.SpectralXTown || ActiveScene == SceneID.Home
        || SceneID.IsBWPScene(ActiveScene))
            {
                Vector3 camPos = ActiveCameraMode == CameraMode.Orthographic
                    ? new Vector3(OrthoCamera.GetPosition().x, OrthoCamera.GetPosition().y, OrthoCamera.GetPosition().z)
                    : ActiveCameraMode == CameraMode.Orbit
                    ? new Vector3(OrbitCamera.GetPosition().x, OrbitCamera.GetPosition().y, OrbitCamera.GetPosition().z)
                    : new Vector3(Camera.Position.X, Camera.Position.Y, Camera.Position.Z);

                if (ActiveScene == SceneID.SpectralXTown)
                {
                    activeLights = activeLights
                      .OrderBy(l => l.Type == LightType.Directional ? 0 : 1)
                      .ThenBy(l => l.CastsShadows ? 0 : 1)
                      .ThenBy(l => Vector3.DistanceSquared(l.Position, camPos))
                      .ToList();
                }
                else if (ActiveScene == SceneID.Home)
                {
                    activeLights = activeLights
                      .OrderBy(l => l.Type == LightType.Directional ? 0 : 1)
                      .ThenBy(l => l.CastsShadows ? 0 : 1)
                      .ThenBy(l => Vector3.DistanceSquared(l.Position, camPos))
                      .ToList();

                }
     
                else if (SceneID.IsBWPScene(ActiveScene))
                {
                    activeLights = activeLights
                      .OrderBy(l => l.Type == LightType.Directional ? 0 : 1)
                      .ThenBy(l => l.CastsShadows ? 0 : 1)
                      .ThenBy(l => Vector3.DistanceSquared(l.Position, camPos))
                      .ToList();
                }



            }

     

            for (int i = 0; i < lightCount; i++)
            {
                var l = activeLights[i];

                _lightPositionsBuf[i * 3] = l.Position.X;
                _lightPositionsBuf[i * 3 + 1] = l.Position.Y;
                _lightPositionsBuf[i * 3 + 2] = l.Position.Z;

                _lightColorsBuf[i * 3] = l.Color.X;
                _lightColorsBuf[i * 3 + 1] = l.Color.Y;
                _lightColorsBuf[i * 3 + 2] = l.Color.Z;

                _lightDirsBuf[i * 3] = l.Direction.X;
                _lightDirsBuf[i * 3 + 1] = l.Direction.Y;
                _lightDirsBuf[i * 3 + 2] = l.Direction.Z;

                _lightIntsBuf[i] = l.Intensity;
                _lightRangesBuf[i] = l.Range;
                _lightTypesBuf[i] = (int)l.Type;
                _lightSpotsBuf[i] = l.SpotAngle;
                _lightShadowsBuf[i] = l.CastsShadows;




                if (l.CastsShadows && i < 8)
                {
                    var lPos = new CustomVec3(l.Position.X, l.Position.Y, l.Position.Z);

                    if (l.Type == LightType.Directional)
                    {
                        CustomVec3 upVec = new CustomVec3(0f, 1f, 0f);

                        bool isShadowCastingScene = SceneID.Is3DScene(ActiveScene)
                                                  || SceneID.IsBWPScene(ActiveScene)
                                                  || SceneID.IsWebViewScene(ActiveScene);

                        if (!isShadowCastingScene)
                        {
                            _lightVPsBuf[i] = CustomMat4.Identity().M;  // send identity, no shadow cast
                            continue;  // or just skip the lProj/lView calculation
                        }

                        CustomVec3 camCenter = SceneID.IsWebViewScene(ActiveScene)
       ? new CustomVec3(0f, 0f, 0f)  // fixed tilemap center for scene 3
       : new CustomVec3(
           Camera.Position.X,
           Camera.Position.Y,
           Camera.Position.Z);

                        CustomVec3 lPosTracked = new CustomVec3(
                            lPos.X + camCenter.X,
                            lPos.Y + camCenter.Y,
                            lPos.Z);

                        CustomMat4 lView = CustomMat4.CreateLookAt(
                            lPosTracked,
                            new CustomVec3(camCenter.X, camCenter.Y, camCenter.Z),  // ← match Z
                            upVec);

                        float orthoSize = SceneID.Is3DScene(ActiveScene) ? 512f
                                         : SceneID.IsBWPScene(ActiveScene) ? 128f
                                         : SceneID.IsWebViewScene(ActiveScene) ? 128f
                                         : 128f;

                        CustomMat4 lProj = CustomMat4.CreateOrthographic(
                            -orthoSize, orthoSize, -orthoSize, orthoSize, 1f, 600f);
                        _lightVPsBuf[i] = (lProj * lView).M;



                    }
                    else
                    {

                        CustomVec3 lookTarget;
                        CustomVec3 upVec;

                        if (l.Type == LightType.Spot && l.Direction.LengthSquared() > 0.0001f)
                        {
               
                            var dirNorm = Vector3.Normalize(l.Direction);
                            lookTarget = new CustomVec3(
                                lPos.X + dirNorm.X * 10f,
                                lPos.Y + dirNorm.Y * 10f,
                                lPos.Z + dirNorm.Z * 10f);
                        }
                        else
                        {
                
                            lookTarget = new CustomVec3(lPos.X, lPos.Y, lPos.Z - 10f);
                        }

                        CustomVec3 forward = new CustomVec3(
                            lookTarget.X - lPos.X,
                            lookTarget.Y - lPos.Y,
                            lookTarget.Z - lPos.Z);
                        float forwardLen = MathF.Sqrt(
                            forward.X * forward.X + forward.Y * forward.Y + forward.Z * forward.Z);
                        CustomVec3 forwardNorm = forwardLen > 0.0001f
                            ? new CustomVec3(forward.X / forwardLen, forward.Y / forwardLen, forward.Z / forwardLen)
                            : new CustomVec3(0f, 0f, -1f);

                        upVec = MathF.Abs(forwardNorm.Z) > 0.95f
                            ? new CustomVec3(0f, 1f, 0f)
                            : new CustomVec3(0f, 0f, 1f);

                        CustomMat4 lView = CustomMat4.CreateLookAt(lPos, lookTarget, upVec);
                        CustomMat4 lProj = CustomMat4.CreatePerspective(130f * (MathF.PI / 180f), 1.0f, 0.5f, 50f);
                        _lightVPsBuf[i] = (lProj * lView).M;
                    }
                }
                else
                {
                    _lightVPsBuf[i] = CustomMat4.Identity().M;
                }
            }

            /*
            for (int i = 0; i < lightCount; i++)
            {
                Console.WriteLine($"[Light{i}] Type:{_lightTypesBuf[i]} CastsShadow:{_lightShadowsBuf[i]} Pos:{_lightPositionsBuf[i * 3]:F1},{_lightPositionsBuf[i * 3 + 1]:F1},{_lightPositionsBuf[i * 3 + 2]:F1}");
            }
            */

            var jitter = GetTAAJitter();

            var camRight = new float[] { view.M[0], view.M[4], view.M[8] };
            var camUp = new float[] { view.M[1], view.M[5], view.M[9] };

            List<ParticleInstanceGroup>? particleGroups = null;
            if ((ActiveScene == SceneID.SpectralXTown ||
         ActiveScene == SceneID.Home ||
         SceneID.IsBWPScene(ActiveScene)
         ) && Weather != null)
            {
                particleGroups = Weather.BuildInstanceGroups();
            }

            var frameData = new WebGLFrameData
            {
                BreakTexUrls = !_breakTexPreloaded && SceneID.IsBWPScene(ActiveScene)
    ? new[] { "/iAssets/Dummy005.png", "/iAssets/DummyHit005.png", "/iAssets/DummyCooked005.png" }
    : null,
                BreakTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildBreakTexSwaps()
    : null,
                SplatterTexUrls = !_splatterTexPreloaded && SceneID.IsBWPScene(ActiveScene)
    ? new[] { SplatterPuddleRegistry.BloodTexturePath, SplatterPuddleRegistry.BoneTexturePath }
    : null,
                SkeletonTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_skeletonTexPreloaded)
    ? SpectralXSkeleton.SpritePaths.Values
        .Append(SpectralXSkeleton.HitEffectPath)
        .Append(SpectralXSkeleton.DeadSpritePath)
        .ToArray()
    : null,
                SkeletonTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildSkeletonTexSwaps() : null,
                PsychoSkeletonTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_psychoSkeletonTexPreloaded)
    ? SpectralXPsychoSkeleton.SpritePaths.Values
        .Append(SpectralXPsychoSkeleton.HitEffectPath)
        .Append(SpectralXPsychoSkeleton.DeadSpritePath)
        .ToArray()
    : null,

                PsychoSkeletonTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildPsychoSkeletonTexSwaps()
    : null,
                ZombiePsychoTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_zombiePsychoTexPreloaded)
    ? SpectralXZombiePsycho.SpritePaths.Values
        .Append(SpectralXZombiePsycho.HitEffectPath)
        .Append(SpectralXZombiePsycho.DeadSpritePath)
        .ToArray()
    : null,
                ZombiePsychoTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildZombiePsychoTexSwaps()
    : null,

                SkeletonWarTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_skeletonWarTexPreloaded)
    ? SpectralXSkeletonWar.SpritePaths.Values
        .Append(SpectralXSkeletonWar.HitEffectPath)
        .Append(SpectralXSkeletonWar.DeadSpritePath)
        .ToArray()
    : null,
                SkeletonWarTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildSkeletonWarTexSwaps()
    : null,

                GoatmanTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_goatmanTexPreloaded)
    ? SpectralXGoatman.SpritePaths.Values
        .Append(SpectralXGoatman.HitEffectPath)
        .Append(SpectralXGoatman.DeadSpritePath)
        .ToArray()
    : null,
                GoatmanTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildGoatmanTexSwaps()
    : null,

                ScavBossTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_scavBossTexPreloaded)
    ? SpectralXScavBoss.SpritePaths.Values
        .Append(SpectralXScavBoss.HitEffectPath)
        .Append(SpectralXScavBoss.DeadSpritePath)
        .ToArray()
    : null,
                ScavBossTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildScavBossTexSwaps()
    : null,

                SkeletonBossTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_skeletonBossTexPreloaded)
    ? SpectralXSkeletonBoss.SpritePaths.Values
        .Append(SpectralXSkeletonBoss.HitEffectPath)
        .Append(SpectralXSkeletonBoss.DeadSpritePath)
        .ToArray()
    : null,
                SkeletonBossTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildSkeletonBossTexSwaps()
    : null,

                CowTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_cowTexPreloaded)
    ? SpectralXCow.SpritePaths.Values
        .Append(SpectralXCow.HitEffectPath)
        .Append(SpectralXCow.DeadSpritePath)
        .ToArray()
    : null,
                CowTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildCowTexSwaps()
    : null,

                CatTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_catTexPreloaded)
    ? SpectralXCat.SpritePaths.Values
        .Append(SpectralXCat.HitEffectPath)
        .Append(SpectralXCat.DeadSpritePath)
        .ToArray()
    : null,
                CatTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildCatTexSwaps()
    : null,

                TownSlutTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_townSlutTexPreloaded)
    ? SpectralXTownSlut.SpritePaths.Values
        .Append(SpectralXTownSlut.HitEffectPath)
        .Append(SpectralXTownSlut.DeadSpritePath)
        .ToArray()
    : null,
                TownSlutTexSwaps = SceneID.IsBWPScene(ActiveScene)
    ? BuildTownSlutTexSwaps()
    : null,
                Width = Viewport.ViewportWidth,
                Height = Viewport.ViewportHeight,
                VP = vp.M,
                CamRight = camRight,
                CamUp = camUp,
                ParticleInstances = particleGroups,
                FoliageInstances = ActiveScene == 2 || ActiveScene == 3 ? BuildFoliageFrameData() : null,
                CubeCityInstances = null,
              //  CubeCityInstances = ActiveScene == SceneID.Home ? BuildCubeCityFrameData() : null,
                Meshes = meshDataList,
                LightCount = lightCount,
                LightPositions = _lightPositionsBuf,
                LightColors = _lightColorsBuf,
                LightDirections = _lightDirsBuf,
                LightIntensities = _lightIntsBuf,
                LightRanges = _lightRangesBuf,
                LightTypes = _lightTypesBuf,
                LightSpotAngles = _lightSpotsBuf,
                LightCastsShadows = _lightShadowsBuf,
                LightVPs = _lightVPsBuf,
                CamX = ActiveCameraMode == CameraMode.Orthographic ? OrthoCamera.GetPosition().x
     : ActiveCameraMode == CameraMode.Orbit ? OrbitCamera.GetPosition().x
     : Camera.Position.X,
                CamY = ActiveCameraMode == CameraMode.Orthographic ? OrthoCamera.GetPosition().y
     : ActiveCameraMode == CameraMode.Orbit ? OrbitCamera.GetPosition().y
     : Camera.Position.Y,
                CamZ = ActiveCameraMode == CameraMode.Orthographic ? OrthoCamera.GetPosition().z
     : ActiveCameraMode == CameraMode.Orbit ? OrbitCamera.GetPosition().z
     : Camera.Position.Z,
                AAMode = (int)ActiveAA,
                JitterX = (int)ActiveAA == 4 ? jitter.x : 0f,
                JitterY = (int)ActiveAA == 4 ? jitter.y : 0f,
                ShadowMode = (int)ActiveShadow,
                ShadowSoftnessBias = Shadow.SoftnessBias,
                ShadowBlockerSearchRadius = Shadow.BlockerSearchRadius,
                ShadowKernelSize = Shadow.KernelSize,
                ShadowContactSharpness = Shadow.ContactSharpness,
                ShadowDepthBias = Shadow.DepthBias,
                ShadowTintR = Shadow.TintR,
                ShadowTintG = Shadow.TintG,
                ShadowTintB = Shadow.TintB,
                ShadowTintStrength = Shadow.TintStrength,
                ShadowPenumbraTintStrength = Shadow.PenumbraTintStrength,
                TimeOfDay = Sun.TimeOfDay,
                SkyBlend = Sun.SkyBlend,
                CloudOpacity = Sun.CloudOpacity,
                RainbowIntensity = Sun.RainbowIntensity,
                SkyZenithR = Sun.SkyZenithColor.X,
                SkyZenithG = Sun.SkyZenithColor.Y,
                SkyZenithB = Sun.SkyZenithColor.Z,
                SkyHorizonR = Sun.SkyHorizonColor.X,
                SkyHorizonG = Sun.SkyHorizonColor.Y,
                SkyHorizonB = Sun.SkyHorizonColor.Z,
                SunIntensity = Sun.SunIntensity,
                AmbientR = Sun.AmbientColor.X,
                AmbientG = Sun.AmbientColor.Y,
                AmbientB = Sun.AmbientColor.Z,
                SunColorR = Sun.SunColor.X,
                SunColorG = Sun.SunColor.Y,
                SunColorB = Sun.SunColor.Z,
                SunDirX = SceneID.IsBWPScene(ActiveScene)
    ? Sun.SunDirectionTile.X : Sun.SunDirection.X,
                SunDirY = SceneID.IsBWPScene(ActiveScene)
    ? Sun.SunDirectionTile.Y : Sun.SunDirection.Y,
                SunDirZ = SceneID.IsBWPScene(ActiveScene)
    ? Sun.SunDirectionTile.Z : Sun.SunDirection.Z,
                SunDirSkyX = Sun.SunDirectionSky.X,
                SunDirSkyY = Sun.SunDirectionSky.Y,
                SunDirSkyZ = Sun.SunDirectionSky.Z,
                CloudOffset = Sun.CloudOffset,
                StarOffset = Sun.StarOffset,
                CloudScale = Sun.CloudScale,
                StarScale = Sun.StarScale,
                MoonDirX = Sun.MoonDirection.X,
                MoonDirY = Sun.MoonDirection.Y,
                MoonDirZ = Sun.MoonDirection.Z,
                MoonDirSkyX = Sun.MoonDirectionSky.X,
                MoonDirSkyY = Sun.MoonDirectionSky.Y,
                MoonDirSkyZ = Sun.MoonDirectionSky.Z,
                MoonColorR = Sun.MoonColor.X,
                MoonColorG = Sun.MoonColor.Y,
                MoonColorB = Sun.MoonColor.Z,
                MoonGlow = Sun.MoonGlow,
                ActiveScene = this.ActiveScene,
                TileMap = (ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home)
                    ? this.TileMap.BuildFrameData() : null,
                TileMapTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tileMapTexturesUploaded)
    ? TileMap.EffectiveTexturePaths : null,
                TileMapUploaded = _tileMapTexturesUploaded,
                TileMapNormalTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveNormalMapPaths : null,
                TileMapRoughnessTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveRoughnessMapPaths : null,
                TileMapMetallicTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveMetallicMapPaths : null,
                TileMapAOTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveAOMapPaths : null,
                TileMapSpecularTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveSpecularMapPaths : null,
                TileMapEmissiveTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveEmissiveMapPaths : null,
                TileMapDisplacementTextures = ((ActiveScene == 2 || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home) && !_tilePBRUploaded)
    ? TileMap.EffectiveDisplacementMapPaths : null,
                TileMapPBRUploaded = _tilePBRUploaded,
                TileRoughnessValues = TileMap.RoughnessValues,
                TileMetallicValues = TileMap.MetallicValues,
                TileAOValues = TileMap.AOValues,
                TileSpecularValues = TileMap.SpecularValues,
                TileEmissiveIntensityValues = TileMap.EmissiveIntensityValues,
                TileDisplacementStrengthValues = TileMap.DisplacementStrengthValues,
                TileParallaxScaleValues = TileMap.ParallaxScaleValues,
                ViewMatrix = view.M,
                ProjMatrix = proj.M,
                BrushWorldX = TileMap.BrushWorldX,
                BrushWorldY = TileMap.BrushWorldY,
                BrushRadius = TileMap.BrushSize * SpectralXLandTileMap.TileSize,
                LandscapeActive = TileMap.IsActive,
                IsMousePainting = TileMap.IsMousePainting,
                ActivePaintMaterial = (int)TileMap.ActiveMaterial,
                PaintStrength = TileMap.BlendStrength,
                SkyDayTexUrl = (ActiveScene == SceneID.SpectralXTown ||
                ActiveScene == SceneID.Home ||
                SceneID.IsBWPScene(ActiveScene))
    ? "/iAssets/SkyCubeMap012.png" : null,

                SkyNightTexUrl = ActiveScene == SceneID.SpectralXTown
    ? "/iAssets/StarsCubeMap015.png"
    : (ActiveScene == SceneID.Home || SceneID.IsBWPScene(ActiveScene))
    ? "/iAssets/StarsCubeMap015.png"
    : null,

                WarriorTexUrl = (SceneID.IsBWPScene(ActiveScene) && Warrior?.CharMesh != null)
    ? Warrior.CharMesh.TextureDataUrl : null,
                WarriorTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_warriorTexPreloaded)
    ? SpectralXBloodWarrior.SpritePaths.Values
        .Append(SpectralXBloodWarrior.HitEffectPath)
        .Append(SpectralXBloodWarrior.DeadSpritePath)
        .ToArray()
    : null,

                RogueTexUrl = (SceneID.IsBWPScene(ActiveScene) && Rogue?.CharMesh != null)
    ? Rogue.CharMesh.TextureDataUrl : null,
                RogueTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_rogueTexPreloaded)
    ? SpectralXRogue.SpritePaths.Values
        .Append(SpectralXRogue.HitEffectPath)
        .Append(SpectralXRogue.DeadSpritePath)
        .ToArray()
    : null,

                MonkTexUrl = (SceneID.IsBWPScene(ActiveScene) && Monk?.CharMesh != null)
    ? Monk.CharMesh.TextureDataUrl : null,
                MonkTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_monkTexPreloaded)
    ? SpectralXMonk.SpritePaths.Values
        .Append(SpectralXMonk.HitEffectPath)
        .Append(SpectralXMonk.DeadSpritePath)
        .ToArray()
    : null,

                MageTexUrl = (SceneID.IsBWPScene(ActiveScene) && Mage?.CharMesh != null)
    ? Mage.CharMesh.TextureDataUrl : null,
                MageTexUrls = (SceneID.IsBWPScene(ActiveScene) && !_mageTexPreloaded)
    ? SpectralXMage.SpritePaths.Values
        .Append(SpectralXMage.HitEffectPath)
        .Append(SpectralXMage.DeadSpritePath)
        .ToArray()
    : null,
                TextMeshes = BuildTextFrameData(vp),
                ScrollbarZ = Camera.Position.Z,
                CameraMode = (int)ActiveCameraMode,
              
            };

            if (frameData.WarriorTexUrls != null) _warriorTexPreloaded = true;
            if (frameData.RogueTexUrls != null) _rogueTexPreloaded = true;
            if (frameData.MonkTexUrls != null) _monkTexPreloaded = true;
            if (frameData.MageTexUrls != null) _mageTexPreloaded = true;
            if (frameData.BreakTexUrls != null) _breakTexPreloaded = true;  // ADD
            if (frameData.SplatterTexUrls != null) _splatterTexPreloaded = true;
            if (frameData.SkeletonTexUrls != null) _skeletonTexPreloaded = true;
            if (frameData.PsychoSkeletonTexUrls != null) _psychoSkeletonTexPreloaded = true;
            if (frameData.ZombiePsychoTexUrls != null) _zombiePsychoTexPreloaded = true;
            if (frameData.SkeletonWarTexUrls != null) _skeletonWarTexPreloaded = true;
            if (frameData.GoatmanTexUrls != null) _goatmanTexPreloaded = true;
            if (frameData.ScavBossTexUrls != null) _scavBossTexPreloaded = true;
            if (frameData.SkeletonBossTexUrls != null) _skeletonBossTexPreloaded = true;
            if (frameData.CowTexUrls != null) _cowTexPreloaded = true;
            if (frameData.CatTexUrls != null) _catTexPreloaded = true;
            if (frameData.TownSlutTexUrls != null) _townSlutTexPreloaded = true;
            return frameData;
        }

        private List<WebGLTextData> BuildTextFrameData(CustomMat4 vp)
        {
            var result = new List<WebGLTextData>();
            var activeScene = GetActiveScene();
            foreach (var mesh in activeScene.Meshes)
            {
                if (mesh is not SpectralXMesh sm || !sm.IsSDFText) continue;
                var atlas = MeshLibrary.GetFontAtlas(sm.FontKey);
                if (atlas == null) continue;
                CustomMat4 mvp = vp * mesh.WorldMatrix;
                result.Add(new WebGLTextData
                {
                    MeshId = mesh.Name,
                    Text = sm.Text,
                    FontKey = sm.FontKey,
                    JsonUrl = atlas.JsonUrl,
                    TexUrl = atlas.TextureUrl,
                    FontSize = sm.FontSize,
                    Mvp = mvp.M,
                    R = sm.Color.X,
                    G = sm.Color.Y,
                    B = sm.Color.Z,
                    A = sm.Color.W,
                    OutlineR = sm.OutlineColor.X,
                    OutlineG = sm.OutlineColor.Y,
                    OutlineB = sm.OutlineColor.Z,
                    OutlineA = sm.OutlineColor.W,
                    OutlineWidth = sm.OutlineWidth,
                    LetterSpacing = sm.LetterSpacing,
                    Align = (int)sm.TextAlign,
                    NeedsRebuild = sm.TextDirty,
                    GlowRadius = sm.GlowRadius,
                    GlowStrength = sm.GlowStrength,
                    GlowR = sm.GlowColor.X,
                    GlowG = sm.GlowColor.Y,
                    GlowB = sm.GlowColor.Z,
                    GlowA = sm.GlowColor.W,
                    ShadowBlur = sm.ShadowBlur,
                    ShadowR = sm.ShadowColor.X,
                    ShadowG = sm.ShadowColor.Y,
                    ShadowB = sm.ShadowColor.Z,
                    ShadowA = sm.ShadowColor.W,
                });
                sm.TextDirty = false;
            }
            return result;
        }


        private List<float> _foliageCullPosScratch = new();
        private List<float> _foliageCullScaleScratch = new();
        private List<float> _foliageCullRotScratch = new();

        private List<FoliageInstanceGroup> BuildFoliageFrameData()
        {
            var result = new List<FoliageInstanceGroup>(_foliageGroups.Count);

            // No frustum data yet this frame (only computed for BWP scenes currently) —
            // fall back to sending everything unculled rather than dropping foliage.
            bool haveFrustum = _frustumPlanes != null;

            foreach (var g in _foliageGroups)
            {
                if (!haveFrustum)
                {
                    result.Add(new FoliageInstanceGroup
                    {
                        MeshId = g.MeshId,
                        TexKey = g.TexKey,
                        Count = g.Count,
                        Positions = g.Positions,
                        Scales = g.Scales,
                        Rotations = g.Rotations,
                        IsStatic = g.IsStatic,
                        BoundingRadius = g.BoundingRadius,
                        Color = g.Color,
                    });
                    continue;
                }

                _foliageCullPosScratch.Clear();
                _foliageCullScaleScratch.Clear();
                _foliageCullRotScratch.Clear();

                for (int i = 0; i < g.Count; i++)
                {
                    float x = g.Positions[i * 3];
                    float y = g.Positions[i * 3 + 1];
                    float z = g.Positions[i * 3 + 2];
                    float scale = g.Scales[i];

                    float testRadius = g.BoundingRadius * scale;
                    if (!SphereInFrustum(new Vector3(x, y, z), testRadius))
                        continue;

                    _foliageCullPosScratch.Add(x);
                    _foliageCullPosScratch.Add(y);
                    _foliageCullPosScratch.Add(z);
                    _foliageCullScaleScratch.Add(scale);
                    _foliageCullRotScratch.Add(g.Rotations[i]);
                }

                result.Add(new FoliageInstanceGroup
                {
                    MeshId = g.MeshId,
                    TexKey = g.TexKey,
                    Count = _foliageCullScaleScratch.Count,
                    Positions = _foliageCullPosScratch.ToArray(),
                    Scales = _foliageCullScaleScratch.ToArray(),
                    Rotations = _foliageCullRotScratch.ToArray(),
                    IsStatic = g.IsStatic,
                    BoundingRadius = g.BoundingRadius,
                    Color = g.Color,
                });
            }

            return result;
        }

        private int _taaFrame = 0;

        private (float x, float y) GetTAAJitter()
        {
            _taaFrame = (_taaFrame + 1) % 16; // more frames = smoother distribution
            float x = HaltonSequence(_taaFrame, 2) - 0.5f;
            float y = HaltonSequence(_taaFrame, 3) - 0.5f;
            // REPLACE — scale way down, 0.1 instead of 0.5
            return (x * 0.1f / Viewport.ViewportWidth, y * 0.1f / Viewport.ViewportHeight);
        }

        private float HaltonSequence(int index, int b)
        {
            float f = 1f, r = 0f;
            int i = index;
            while (i > 0)
            {
                f /= b;
                r += f * (i % b);
                i = (int)MathF.Floor(i / b);
            }
            return r;
        }



        public void SetTimeOfDay(float t)
        {
            Console.WriteLine($"[Sun] SetTimeOfDay called: {t} → SkyBlend will be: {Sun.SkyBlend}");
            Sun.SetTime(t);
            Console.WriteLine($"[Sun] After SetTime SkyBlend: {Sun.SkyBlend}");
            if (_sunLight != null)
                Sun.Apply(_sunLight);
            _lightsDirty = true;
        }
              

        // ── TileMap UI API ───────────────────────────────────────────────────────
        public void ToggleLandscapeActive()
        {
            TileMap.IsActive = !TileMap.IsActive;
        }
        public void SetActiveMaterial(TileMaterial mat)
        {
            TileMap.ActiveMaterial = mat;
        }

        public void SetPaintMode(TilePaintMode mode)
        {
            TileMap.PaintMode = mode;
        }

        public void SetBrushSize(int size)
        {
            TileMap.BrushSize = Math.Clamp(size, 0, 8);
        }

        public void SetTopologyStrength(float strength)
        {
            TileMap.TopologyStrength = Math.Clamp(strength, 0.05f, 1.0f);
        }

        public void SetFlattenTarget(float height)
        {
            TileMap.FlattenTargetHeight = height;
        }
        public void SetBlendStrength(float strength)
        {
            TileMap.BlendStrength = Math.Clamp(strength, 0f, 1f);
        }

        // ── Export Height Map (.r16 download) ────────────────────────────────────
        public async Task ExportHeightMap()
        {
            var bytes = TileMap.ExportHeightMapBytes();
            var base64 = Convert.ToBase64String(bytes);
            await _js.InvokeVoidAsync("SpectralLandscape.exportR16", base64, "heightmap.r16");
            Console.WriteLine("[Engine] Height map exported");
        }

        // ── Import Height Map (file picker) ──────────────────────────────────────
        public async Task ImportHeightMap()
        {
            var base64 = await _js.InvokeAsync<string?>("SpectralLandscape.importR16");
            if (string.IsNullOrEmpty(base64)) return;
            TileMap.ImportHeightMapBytes(Convert.FromBase64String(base64));
            Console.WriteLine("[Engine] Height map imported");
        }

        public async Task ExportPaintMap()
        {
            var pixels = TileMap.ExportPaintMapBytes();
            var base64 = Convert.ToBase64String(pixels);
            await _js.InvokeVoidAsync("SpectralLandscape.exportPng",
                base64, TileMap.GridSize, TileMap.GridSize, "paintmap.png");
            Console.WriteLine("[Engine] Paint map exported");
        }

        public async Task ImportPaintMap()
        {
            var result = await _js.InvokeAsync<PaintImportResult?>("SpectralLandscape.importPng");
            if (result == null) return;
            TileMap.ImportPaintMapBytes(Convert.FromBase64String(result.Pixels), result.Width, result.Height);
            Console.WriteLine("[Engine] Paint map imported");
        }

        public void LoadDefaultLandscape()
        {
            TileMap.LoadDefault();
        }

        public async Task LoadDefaultImportLandscape()
        {
            var result = await _js.InvokeAsync<DefaultImportResult?>(
                "SpectralLandscape.loadFromAssets",
                "/iAssets/DefaultHeightmap.r16",
                "/iAssets/DefaultPaintmap.png");

            if (result == null) return;

            if (!string.IsNullOrEmpty(result.Heights))
                TileMap.ImportHeightMapBytes(Convert.FromBase64String(result.Heights));

            if (result.Paint != null && !string.IsNullOrEmpty(result.Paint.Pixels))
                TileMap.ImportPaintMapBytes(
                    Convert.FromBase64String(result.Paint.Pixels),
                    result.Paint.Width,
                    result.Paint.Height);

            Console.WriteLine("[Engine] Default landscape loaded from assets");
        }


        private string LandscapeStorageKey => ActiveScene switch
        {
            SceneID.Home => "spectralx_landscape_home",
            SceneID.SpectralXTown => "spectralx_landscape_town",
            SceneID.BWPScene1 => "spectralx_landscape_bwp1",
            SceneID.BWPScene2 => "spectralx_landscape_bwp2",
            SceneID.BWPScene3 => "spectralx_landscape_bwp3",
            SceneID.BWPScene4 => "spectralx_landscape_bwp4",
            SceneID.BWPScene5 => "spectralx_landscape_bwp5",
            SceneID.BWPScene6 => "spectralx_landscape_bwp6",
            SceneID.BWPScene7 => "spectralx_landscape_bwp7",
            SceneID.BWPScene8 => "spectralx_landscape_bwp8",
            SceneID.BWPScene9 => "spectralx_landscape_bwp9",
            SceneID.BWPScene10 => "spectralx_landscape_bwp10",
            SceneID.BWPScene11 => "spectralx_landscape_bwp11",
            _ => "spectralx_landscape"
        };

        public async Task SaveLandscape()
        {
            var data = TileMap.ExportSaveData();
            var json = JsonSerializer.Serialize(data);
            await _js.InvokeVoidAsync("SpectralLandscape.save", json, LandscapeStorageKey);
        }

        public async Task LoadLandscape()
        {
            try
            {
                var json = await _js.InvokeAsync<string>("SpectralLandscape.load", LandscapeStorageKey);
                if (string.IsNullOrEmpty(json)) return;
                var data = JsonSerializer.Deserialize<LandscapeSaveData>(json);
                if (data != null)
                    await TileMap.ImportSaveDataAsync(data);
            }
            catch { }
        }

        public void HandleTileMapMouseDown(float screenX, float screenY)
        {
            if (!TileMap.IsActive) return;
            _isMousePainting = true;
            var (worldX, worldY) = UnprojectToGroundPlane(screenX, screenY);
            TileMap.BrushWorldX = worldX;
            TileMap.BrushWorldY = worldY;
            TryPaintAtScreen(screenX, screenY);
        }

        public void HandleTileMapMouseMove(float screenX, float screenY)
        {
            if (ActiveScene == SceneID.SpectralXTown || SceneID.IsBWPScene(ActiveScene) || ActiveScene == SceneID.Home)
            {
                var (worldX, worldY) = UnprojectToGroundPlane(screenX, screenY);
                TileMap.BrushWorldX = worldX;
                TileMap.BrushWorldY = worldY;
            }

            if (!_isMousePainting) return;
            TryPaintAtScreen(screenX, screenY);
        }

        public void HandleTileMapMouseUp()
        {
            _isMousePainting = false;
            TileMap.IsMousePainting = false;
        }

  
        private void TryPaintAtScreen(float screenX, float screenY)
        {
            if (ActiveScene != SceneID.SpectralXTown && !SceneID.IsBWPScene(ActiveScene) && ActiveScene != SceneID.Home) return;

            var (worldX, worldY) = UnprojectToGroundPlane(screenX, screenY);
            var (tileX, tileY, hit) = TileMap.WorldToTile(worldX, worldY);

            if (hit)
                TileMap.Paint(tileX, tileY);
        }

        private (float worldX, float worldY) UnprojectToGroundPlane(float screenX, float screenY)
        {
            float ndcX = (screenX / Viewport.ViewportWidth) * 2f - 1f;
            float ndcY = 1f - (screenY / Viewport.ViewportHeight) * 2f;

            float aspect = (float)Viewport.ViewportWidth / Viewport.ViewportHeight;
            CustomMat4 view;
            CustomMat4 proj;

            switch (ActiveCameraMode)
            {
                case CameraMode.Orthographic:
                    (view, proj) = OrthoCamera.GetMatrices(aspect);
                    break;
                case CameraMode.Orbit:
                    (view, proj) = OrbitCamera.GetMatrices(aspect);
                    break;
                default:
                    view = Camera.GetViewMatrix();
                    proj = CustomMat4.CreatePerspective(
                        90f * (MathF.PI / 180f), aspect, 0.1f, 2000f);
                    break;
            }

            CustomMat4 vp = proj * view;
            CustomMat4 vpInv = CustomMat4.Invert(vp);
            var nearNDC = new Vector4(ndcX, ndcY, -1f, 1f);
            var farNDC = new Vector4(ndcX, ndcY, 1f, 1f);
            Vector4 nearWorld = Transform(vpInv, nearNDC);
            Vector4 farWorld = Transform(vpInv, farNDC);
            var nearPos = new Vector3(nearWorld.X, nearWorld.Y, nearWorld.Z) / nearWorld.W;
            var farPos = new Vector3(farWorld.X, farWorld.Y, farWorld.Z) / farWorld.W;
            var rayDir = Vector3.Normalize(farPos - nearPos);
            if (MathF.Abs(rayDir.Z) < 0.0001f)
                return (0f, 0f); 
            float t = -nearPos.Z / rayDir.Z;
            float worldX = nearPos.X + t * rayDir.X;
            float worldY = nearPos.Y + t * rayDir.Y;
            return (worldX, worldY);
        }

        private static Vector4 Transform(CustomMat4 m, Vector4 v)
        {
            return new Vector4(
                m.M[0] * v.X + m.M[4] * v.Y + m.M[8] * v.Z + m.M[12] * v.W,
                m.M[1] * v.X + m.M[5] * v.Y + m.M[9] * v.Z + m.M[13] * v.W,
                m.M[2] * v.X + m.M[6] * v.Y + m.M[10] * v.Z + m.M[14] * v.W,
                m.M[3] * v.X + m.M[7] * v.Y + m.M[11] * v.Z + m.M[15] * v.W);
        }



        [JSInvokable("OnTileTexturesUploaded")]
        public void OnTileTexturesUploaded()
        {
            _tileMapTexturesUploaded = true;
            Console.WriteLine("[TileMap] Texture upload confirmed by JS");
        }

        [JSInvokable("OnTilePBRTexturesUploaded")]
        public void OnTilePBRTexturesUploaded()
        {
            _tilePBRUploaded = true;
            Console.WriteLine("[TileMap] PBR texture upload confirmed by JS — all sets loaded");
        }


        public async Task PushShadersToJS()
        {
            var bundle = new
            {
                fsSourcePCF = ShaderLoader.Load("shadow_pcf.frag"),
                fsSourcePCSS = ShaderLoader.Load("shadow_pcss.frag"),
                fsSourceSpectralXSV1 = ShaderLoader.Load("spectral_xs_V1.frag"),
                fsSourceSpectralXSV2 = ShaderLoader.Load("spectral_xs_V2.frag"),
                fsSourceSpectralXSV3 = ShaderLoader.Load("spectral_xs_V2.frag"),
                vsSourceMain = ShaderLoader.Load("vsSource.vert"),
            };

            await _js.InvokeVoidAsync("SpectralGLInterop.initShaders", bundle);
            Console.WriteLine("[SpectralXEngine] PCF shader pushed from C#");
        }

        private float[]? _frustumPlanes = null;

        private void ExtractFrustumPlanes(CustomMat4 vp)
        {
            _frustumPlanes ??= new float[24];
            var m = vp.M;

            // Left
            _frustumPlanes[0] = m[3] + m[0];
            _frustumPlanes[1] = m[7] + m[4];
            _frustumPlanes[2] = m[11] + m[8];
            _frustumPlanes[3] = m[15] + m[12];
            // Right
            _frustumPlanes[4] = m[3] - m[0];
            _frustumPlanes[5] = m[7] - m[4];
            _frustumPlanes[6] = m[11] - m[8];
            _frustumPlanes[7] = m[15] - m[12];
            // Bottom
            _frustumPlanes[8] = m[3] + m[1];
            _frustumPlanes[9] = m[7] + m[5];
            _frustumPlanes[10] = m[11] + m[9];
            _frustumPlanes[11] = m[15] + m[13];
            // Top
            _frustumPlanes[12] = m[3] - m[1];
            _frustumPlanes[13] = m[7] - m[5];
            _frustumPlanes[14] = m[11] - m[9];
            _frustumPlanes[15] = m[15] - m[13];
            // Near
            _frustumPlanes[16] = m[3] + m[2];
            _frustumPlanes[17] = m[7] + m[6];
            _frustumPlanes[18] = m[11] + m[10];
            _frustumPlanes[19] = m[15] + m[14];
            // Far
            _frustumPlanes[20] = m[3] - m[2];
            _frustumPlanes[21] = m[7] - m[6];
            _frustumPlanes[22] = m[11] - m[10];
            _frustumPlanes[23] = m[15] - m[14];
        }

        private bool SphereInFrustum(Vector3 center, float radius)
        {
            if (_frustumPlanes == null) return true;
            for (int i = 0; i < 6; i++)
            {
                float nx = _frustumPlanes[i * 4];
                float ny = _frustumPlanes[i * 4 + 1];
                float nz = _frustumPlanes[i * 4 + 2];
                float d = _frustumPlanes[i * 4 + 3];
                if (nx * center.X + ny * center.Y + nz * center.Z + d < -radius)
                    return false;
            }
            return true;
        }

        [JSInvokable("OnTileGridRebuilding")]
        public void OnTileGridRebuilding()
        {
            TileMap.MarkDirty();
            _tileMapTexturesUploaded = false;
            Console.WriteLine("[TileMap] JS grid rebuilding — marked dirty for resend");
        }

    }

}