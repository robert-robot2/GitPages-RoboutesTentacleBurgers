// SpectralXEngine.CubeCityBuilder.cs
// Replaces SpectralXEngine.CubeMaze.cs entirely.
// Pure C# state machine — JS side renders via instancing (same pattern as foliage).
// No per-cube SpectralXMesh objects. All transforms packed into float[] buffers.

using SpectralXGLX.SpectralGL.Math;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        // ============================================================
        // CITY BUILDER — PHASE ENUM
        // ============================================================
        public enum CityPhase
        {
            Dormant = 0,
            FirstCube = 1,
            SpawnVehicles = 2,
            LayFoundations = 3,
            BuildVillage = 4,
            BuildTown = 5,
            BuildCity = 6,
            BuildMetropolis = 7,
            BuildFuture = 8,
            PeacefulLife = 9,
            CelebrationFireworks = 10,
            GrandExplosion = 11,
            Rebuild = 12,

            // ── NEW POST-EXPLOSION SLOW-BUILD PHASES ──────────────────
            RuinsSettle = 13,        // debris slows, dust settles
            FoundationCrawl = 14,    // new foundations creep out from center slowly
            DowntownCore = 15,       // dense tall rectangles erupt in the center district
            ResidentialSprawl = 16,  // low wide blocks spread outward like suburbs
            IndustrialZone = 17,     // chunky dark factory blocks on the west quadrant
            CommercialStrip = 18,    // medium colorful blocks along main avenues
            HologramSigns = 19,      // cyberpunk ad billboards pop up across the city
            NeonNight = 20,          // all buildings pulse with neon color shifts
            ChaosDistrict = 21,      // random sized blocks fill remaining gaps wildly
            MegaStructure = 22,      // one enormous central tower assembles cube by cube
            CityAtPeace2 = 23,       // second peaceful phase — longer, slower drift
            FinalFireworks = 24,     // grand finale fireworks before full reset
        }

        // ============================================================
        // PEACEFUL LIFE STATE
        // ============================================================

        // Stored base colors per instance — prevents compounding darkness bug
        // Indexed same as _cityInstances
        private Vector3[] _cityBaseColors = new Vector3[MAX_CITY_CUBES];
        private bool _baseColorsCaptured = false;

        // Holo building light cubes — small glowing cubes on building tops
        private const int MAX_HOLO_LIGHTS = 120;
        private int _holoLightStartIdx = -1; // first index in _cityInstances for holo lights
        private int _holoLightCount = 0;

        // Emergency vehicle state
        private struct EmergencyVehicle
        {
            public int InstanceIdx;   // index into _cityInstances
            public Vector3 PathStart;
            public Vector3 PathEnd;
            public float T;           // 0-1 along current path segment
            public float Speed;
            public bool IsCop;        // true=cop (blue/red), false=ambulance (red/white)
            public float FlashTimer;
            public bool FlashState;
        }

        private const int MAX_EMERGENCY = 8;
        private EmergencyVehicle[] _emergencyVehicles = new EmergencyVehicle[MAX_EMERGENCY];
        private int _emergencyCount = 0;
        // ============================================================
        // MINECRAFT GRID CONSTANTS
        // ============================================================

        // World units per grid cell — 1 cube (scale 1.0) + 1 gap = 2 units per cell
        private const float MC_CELL = 2.0f;

        // Phase build radii — how many grid cells out from center each phase reaches
        // Each phase ADDS to the previous, never removes
        private static readonly int[] PhaseRadius = new int[]
        {
            0,   // 0  Dormant
            0,   // 1  FirstCube
            0,   // 2  SpawnVehicles
            6,   // 3  LayFoundations  — foundation plate, radius 6
            6,   // 4  BuildVillage    — same area, stack up to 2 floors
            10,  // 5  BuildTown       — expand to radius 10
            14,  // 6  BuildCity       — expand to radius 14
            18,  // 7  BuildMetropolis — expand to radius 18
            20,  // 8  BuildFuture     — expand to radius 20 (near 40x40 edge)
        };

        // Building size ranges per phase [minW, maxW, minD, maxD, minFloors, maxFloors]
        private static readonly int[,] PhaseBuildingParams = new int[,]
        {
            { 0,0,0,0,0,0 }, // 0 Dormant
            { 0,0,0,0,0,0 }, // 1 FirstCube
            { 0,0,0,0,0,0 }, // 2 SpawnVehicles
            { 1,1,1,1,1,1 }, // 3 LayFoundations — 1x1 flat plates only
            { 1,3,1,3,1,2 }, // 4 BuildVillage   — small huts, 1-2 floors
            { 2,4,2,4,2,4 }, // 5 BuildTown      — houses, 2-4 floors
            { 2,5,2,5,3,8 }, // 6 BuildCity      — offices, 3-8 floors
            { 3,6,3,6,5,16 },// 7 BuildMetropolis — towers, 5-16 floors
            { 2,5,2,5,8,28 },// 8 BuildFuture    — spires, 8-28 floors
        };

        // Tracks which MC grid cells are occupied — key = packed (cellX << 16 | cellZ)
        // Persists across phases so old buildings are never overwritten
        private readonly Dictionary<int, float> _mcGrid = new(); // value = top floor index

        // Village center footprint — locked in during BuildVillage, never overwritten
        private readonly HashSet<int> _villageFootprint = new();

        // ============================================================
        // MC GRID HELPERS
        // ============================================================

        /// Convert MC grid cell coords to world XY position (center of cell)
        private Vector3 MCGridToWorld(int cellX, int cellZ, int floor)
        {
            return new Vector3(
                cellX * MC_CELL,
                cellZ * MC_CELL,
                floor + 0.5f);   // +0.5 centers cube on floor level
        }

        private static int MCKey(int cx, int cz) => (cx + 256) << 16 | (cz + 256);

        private bool MCCellFree(int cx, int cz, int w, int d)
        {
            for (int x = cx; x < cx + w; x++)
                for (int z = cz; z < cz + d; z++)
                    if (_mcGrid.ContainsKey(MCKey(x, z))) return false;
            return true;
        }

        private void MCClaimCells(int cx, int cz, int w, int d, float topFloor)
        {
            for (int x = cx; x < cx + w; x++)
                for (int z = cz; z < cz + d; z++)
                    _mcGrid[MCKey(x, z)] = topFloor;
        }
        // ============================================================
        // CITY BUILDER — INSTANCE DATA STRUCTS
        // ============================================================
        private struct CubeInstance
        {
            public Vector3 Pos;
            public Vector3 Vel;
            public Vector3 HomePos;
            public Vector3 Color;       // rgb 0-1
            public float Scale;
            public float Phase;         // per-instance random phase
            public float VehicleT;      // 0-1 along vehicle path
            public bool IsVehicle;
            public bool IsActive;
            public int StructureLayer;  // which build phase placed this cube
        }

        // ============================================================
        // CITY BUILDER — STATE
        // ============================================================
        private CityPhase _cityPhase = CityPhase.Dormant;
        private float _cityPhaseTimer = 0f;
        private float _cityNextPhaseAt = 0f;
        private int _citySpawnIndex = 0;       // used during incremental spawning
        private float _citySpawnAccum = 0f;
        private Random _cityRand = new Random(1337); // leet cubes

        // Master instance buffer — up to 10,000 cubes
        private const int MAX_CITY_CUBES = 5_000;
        private CubeInstance[] _cityInstances = new CubeInstance[MAX_CITY_CUBES];
        private int _cityActiveCount = 0;

        // Vehicle paths — each vehicle is a CubeInstance with IsVehicle=true
        // Vehicle carries a payload of cubes that follow behind it
        private const int MAX_VEHICLES = 24;
        private Vector3[] _vehicleTargets = new Vector3[MAX_VEHICLES];
        private int _vehicleCount = 0;

        // Build grid — tracks which cells are occupied
        private const int CITY_GRID = 40;        // 40×40 build area
        private bool[,] _cityGrid = new bool[CITY_GRID, CITY_GRID];
        private float[,] _cityHeight = new float[CITY_GRID, CITY_GRID]; // current top Z per cell

        // Packed GPU buffers — rebuilt each frame from _cityInstances
        // Matches FoliageInstanceGroup layout exactly
        private float[] _cityPositionBuf = new float[MAX_CITY_CUBES * 3];
        private float[] _cityScaleBuf = new float[MAX_CITY_CUBES];
        private float[] _cityRotBuf = new float[MAX_CITY_CUBES];
        private float[] _cityColorBuf = new float[MAX_CITY_CUBES * 3]; // extra channel — packed in scale as tint
                                                                       // ============================================================
                                                                       // HOLOGRAM SIGN DATA
                                                                       // ============================================================
        private struct HologramSign
        {
            public Vector3 Pos;
            public Vector3 Color;       // glow color
            public float Width;
            public float Height;
            public float Phase;
            public float PulseSpeed;
            public int SignType;        // 0-9 maps to ad names below
            public bool IsActive;
        }

        private static readonly string[] AdNames = new[]
        {
            "JIBBA COLA",
            "CAT SHOW",
            "NOODLE PALACE",
            "VOID BANK",
            "MEGA CUBES",
            "DR TENTACLE",
            "PIXEL BREW",
            "CUBE AIR",
            "SHADOW NET",
            "ROBOUTE INC",
        };

        private static readonly Vector3[] AdColors = new[]
        {
            new Vector3(0.0f, 1.0f, 0.3f),   // Jibba Cola — neon green
            new Vector3(1.0f, 0.2f, 0.8f),   // Cat Show — hot pink
            new Vector3(1.0f, 0.6f, 0.0f),   // Noodle Palace — orange
            new Vector3(0.3f, 0.3f, 1.0f),   // Void Bank — deep blue
            new Vector3(0.0f, 1.0f, 1.0f),   // Mega Cubes — cyan
            new Vector3(0.8f, 0.0f, 1.0f),   // Dr Tentacle — purple
            new Vector3(1.0f, 0.9f, 0.0f),   // Pixel Brew — yellow
            new Vector3(0.0f, 0.8f, 1.0f),   // Cube Air — sky blue
            new Vector3(1.0f, 0.1f, 0.1f),   // Shadow Net — red
            new Vector3(0.5f, 1.0f, 0.5f),   // Roboute Inc — lime
        };

        private const int MAX_HOLO_SIGNS = 80;
        private HologramSign[] _holoSigns = new HologramSign[MAX_HOLO_SIGNS];
        private int _holoSignCount = 0;

        // ============================================================
        // RECTANGULAR BUILDING REGISTRY
        // Tracks footprint and height for varied-size buildings
        // ============================================================
        private struct BuildingRect
        {
            public int GridX, GridZ;    // top-left corner in city grid
            public int FootW, FootD;    // footprint width/depth in cells
            public float Height;
            public Vector3 Color;
            public int District;        // 0=core 1=residential 2=industrial 3=commercial 4=chaos
        }

        private const int MAX_BUILDINGS = 16;
        private BuildingRect[] _buildings = new BuildingRect[MAX_BUILDINGS];
        private int _buildingCount = 0;

        // Extended tile map grid — full 128x128 occupancy tracking
        private const int FULL_GRID = 64;
        private bool[,] _fullGrid = new bool[FULL_GRID, FULL_GRID];
        private float[,] _fullHeight = new float[FULL_GRID, FULL_GRID];

        // Mega structure state
        private Vector3 _megaStructureCenter = new Vector3(0f, 0f, 0f);
        private float _megaStructureTargetHeight = 50f;
        private int _megaSpawnedLayers = 0;

        // Occlusion
        private readonly Dictionary<int, float> _occlusionTopZ = new();
        private float _occlusionBias = 1.5f;
        private float _cullRadius = 80f;

        // Culling stats — read these in debug to tune the parameters
        private int _lastFrameCulledFrustum = 0;
        private int _lastFrameCulledOcclusion = 0;
        private int _lastFrameCulledDistance = 0;
        private int _lastFrameDrawn = 0;

        // Phase durations (seconds)
        private static readonly float[] PhaseDurations = new float[]
          {
            5f,  // 0  Dormant           — short, just a breath
            5f,  // 1  FirstCube         — pulse and ring spawn, quick
            5f,    // 2  SpawnVehicles      — vehicles roll, watch them go
            7f,    // 3  LayFoundations     — satisfying grid fill
            10f,   // 4  BuildVillage       — let the village breathe
            10f,   // 5  BuildTown          — town grows nicely
            10f,   // 6  BuildCity          — real city feel
            10f,   // 7  BuildMetropolis    — dense and flying things
            10f,   // 8  BuildFuture        — spires and orbital rings
            -1f,   // 9  PeacefulLife       — RANDOM 25-55s (set in AdvanceCityPhase)
            14f,   // 10 CelebrationFireworks
            4f,    // 11 GrandExplosion     — fast nova
            6f,    // 12 Rebuild            — drift home
            5f,    // 13 RuinsSettle
            12f,   // 14 FoundationCrawl
            30f,   // 15 DowntownCore
            35f,   // 16 ResidentialSprawl
            28f,   // 17 IndustrialZone
            25f,   // 18 CommercialStrip
            20f,   // 19 HologramSigns
            -1f,   // 20 NeonNight          — RANDOM 20-45s
            30f,   // 21 ChaosDistrict
            40f,   // 22 MegaStructure
            -1f,   // 23 CityAtPeace2       — RANDOM 30-60s
            16f,   // 24 FinalFireworks
          };
        public string CityPhaseDebug => $"{_cityPhase} ({_cityPhaseTimer:F1}s / {_cityNextPhaseAt:F1}s)";
        public int CityActiveCount => _cityActiveCount;
        public int CityPackedCount => _cityPackedCount;
        public int CityDrawn => _lastFrameDrawn;
        public int CityCulledFrustum => _lastFrameCulledFrustum;
        public int CityCulledOcclusion => _lastFrameCulledOcclusion;
        public int CityCulledDistance => _lastFrameCulledDistance;
        // ============================================================
        // PUBLIC INIT — called from InitScene3 instead of old cube maze
        // ============================================================
        public void InitCubeCityBuilder()
        {
            _cityPhase = CityPhase.Dormant;
            _cityPhaseTimer = 0f;
            _cityNextPhaseAt = PhaseDurations[(int)CityPhase.Dormant];
            _cityActiveCount = 0;
            _citySpawnIndex = 0;
            _citySpawnAccum = 0f;
            _cityRand = new Random(1337);

            Array.Clear(_cityInstances, 0, MAX_CITY_CUBES);
            Array.Clear(_cityGrid, 0, CITY_GRID * CITY_GRID);
            Array.Clear(_cityHeight, 0, CITY_GRID * CITY_GRID);
            Array.Clear(_fullGrid, 0, FULL_GRID * FULL_GRID);
            Array.Clear(_fullHeight, 0, FULL_GRID * FULL_GRID);

            _mcGrid.Clear();
            _villageFootprint.Clear();
            _occlusionTopZ.Clear();
            _buildingCount = 0;
            _holoSignCount = 0;

            // Seed origin cube at center
            SpawnCube(
                pos: new Vector3(0f, 0f, 0f),
                home: new Vector3(0f, 0f, 0f),
                color: new Vector3(0.4f, 0.9f, 1.0f),
                scale: 1.0f,
                isVehicle: false,
                layer: 0);

            Console.WriteLine("[CubeCityBuilder] Initialized — MC grid ready");
        }

        // ============================================================
        // TICK — called from TickAndGetFrame when ActiveScene == Home
        // Replace TickCubeMaze(delta) call with TickCubeCityBuilder(delta)
        // ============================================================
        public void TickCubeCityBuilder(float delta)
        {
           // Console.WriteLine($"Phase: {_cityPhase} | Timer: {_cityPhaseTimer:F1}/{_cityNextPhaseAt:F1} | ActiveCubes: {_cityActiveCount}");
            if (_cityInstances == null) return;

            float now = (float)(DateTime.UtcNow - _startTime).TotalSeconds;
            _cityPhaseTimer += delta;

            // --- Phase advance ---
            if (_cityPhaseTimer >= _cityNextPhaseAt
      && _cityPhase != CityPhase.Rebuild
      && _cityPhase != CityPhase.CityAtPeace2
      && _cityPhase != CityPhase.FinalFireworks) // ← ADD THIS
            {
                AdvanceCityPhaseExpanded();
            }

            // --- Per-phase tick ---
            switch (_cityPhase)
            {
                case CityPhase.Dormant:
                    TickDormant(delta, now);
                    break;
                case CityPhase.FirstCube:
                    TickFirstCube(delta, now);
                    break;
                case CityPhase.SpawnVehicles:
                    TickSpawnVehicles(delta, now);
                    break;
                case CityPhase.LayFoundations:
                    TickLayFoundations(delta, now);
                    break;
                case CityPhase.BuildVillage:
                    TickBuild(delta, now, maxHeight: 1.5f, cubesPerSec: 6f, layer: 4);
                    break;
                case CityPhase.BuildTown:
                    TickBuild(delta, now, maxHeight: 4f, cubesPerSec: 10f, layer: 5);
                    break;
                case CityPhase.BuildCity:
                    TickBuild(delta, now, maxHeight: 10f, cubesPerSec: 14f, layer: 6);
                    break;
                case CityPhase.BuildMetropolis:
                    TickBuild(delta, now, maxHeight: 20f, cubesPerSec: 20f, layer: 7);
                    TickFlyingTransports(delta, now);
                    break;
                case CityPhase.BuildFuture:
                    TickBuild(delta, now, maxHeight: 35f, cubesPerSec: 25f, layer: 8);
                    TickOrbitalRings(delta, now);
                    break;
                case CityPhase.PeacefulLife:
                    TickPeacefulLife(delta, now);
                    break;
                case CityPhase.CelebrationFireworks:
                    TickFireworks(delta, now);
                    break;
                case CityPhase.GrandExplosion:
                    TickGrandExplosion(delta, now);
                    break;
                case CityPhase.Rebuild:
                    TickRebuild(delta, now);
                    break;
                case CityPhase.RuinsSettle:
                    TickRuinsSettle(delta, now);
                    break;
                case CityPhase.FoundationCrawl:
                    TickFoundationCrawl(delta, now);
                    break;
                case CityPhase.DowntownCore: //broken
                    TickDistrictBuild(delta, now, layer: 13);
                    break;
                case CityPhase.ResidentialSprawl: // culling s breaking buildings on this
                    TickDistrictBuild(delta, now, layer: 14);
                    break;
                case CityPhase.IndustrialZone:// broken
                    TickDistrictBuild(delta, now, layer: 15);
                    break;
                case CityPhase.CommercialStrip: //broken
                    TickDistrictBuild(delta, now, layer: 16);
                    break;
                case CityPhase.HologramSigns:
                    TickHologramSigns(delta, now);
                    break;
                case CityPhase.NeonNight:
                    TickNeonNight(delta, now);
                    break;
                case CityPhase.ChaosDistrict:
                    TickDistrictBuild(delta, now, layer: 17);
                    break;
                case CityPhase.MegaStructure:
                    TickMegaStructure(delta, now);
                    break;
                case CityPhase.CityAtPeace2:
                    TickCityAtPeace2(delta, now);
                    break;
                case CityPhase.FinalFireworks:
                    TickFireworks(delta, now);
                    break;
            }

            // --- Pack GPU buffers ---
            PackBuffers();
        }

        // ============================================================
        // PHASE ADVANCE
        // ============================================================
        private void AdvanceCityPhaseExpanded()
        {
            int next = (int)_cityPhase + 1;

            bool HoldOnFinalPeace = true;
            if (next > (int)CityPhase.FinalFireworks)
                next = HoldOnFinalPeace
                    ? (int)CityPhase.CityAtPeace2
                    : 0;
            _cityPhase = (CityPhase)next;
            _cityPhaseTimer = 0f;
            _citySpawnIndex = 0;
            _citySpawnAccum = 0f;

            float dur = (next < PhaseDurations.Length) ? PhaseDurations[next] : 10f;
            if (dur < 0f)
            {
                dur = _cityPhase switch
                {
                    CityPhase.PeacefulLife => 25f + (float)_cityRand.NextDouble() * 30f,
                    CityPhase.NeonNight => 20f + (float)_cityRand.NextDouble() * 25f,
                    CityPhase.CityAtPeace2 => 30f + (float)_cityRand.NextDouble() * 30f,
                    _ => 15f,
                };
            }
            _cityNextPhaseAt = dur;

            Console.WriteLine($"[CubeCityBuilder] Phase → {_cityPhase} ({dur:F1}s)");
            RebuildOcclusionMap();
            switch (_cityPhase)
            {
                case CityPhase.SpawnVehicles:
                    SetupVehicles();
                    break;
                case CityPhase.GrandExplosion:
                    TriggerExplosion();
                    break;
                case CityPhase.PeacefulLife:
                    SetupPeacefulLife();
                    break;
                case CityPhase.Rebuild:
                    SetupRebuild();
                    break;
                case CityPhase.RuinsSettle:
                    SetupRuinsSettle();
                    break;
                case CityPhase.FoundationCrawl:
                    SetupFoundationCrawl();
                    break;
                case CityPhase.DowntownCore:
                    SetupDistrict(district: 0, footMin: 2, footMax: 6, heightMax: 40f, countTarget: 60);
                    break;
                case CityPhase.ResidentialSprawl:
                    SetupDistrict(district: 1, footMin: 2, footMax: 4, heightMax: 6f, countTarget: 120);
                    break;
                case CityPhase.IndustrialZone:
                    SetupDistrict(district: 2, footMin: 3, footMax: 8, heightMax: 12f, countTarget: 50);
                    break;
                case CityPhase.CommercialStrip:
                    SetupDistrict(district: 3, footMin: 1, footMax: 3, heightMax: 15f, countTarget: 80);
                    break;
                case CityPhase.HologramSigns:
                    SetupHologramSigns();
                    break;
                case CityPhase.ChaosDistrict:
                    SetupDistrict(district: 4, footMin: 1, footMax: 10, heightMax: 25f, countTarget: 150);
                    break;
                case CityPhase.MegaStructure:
                    SetupMegaStructure();
                    break;
                case CityPhase.Dormant:
                    InitCubeCityBuilder();
                    break;
            }
        }
        private void RebuildOcclusionMap()
        {
            _occlusionTopZ.Clear();

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;
                if (c.IsVehicle) continue;

                int gx = (int)MathF.Round(c.HomePos.X * 2f);
                int gz = (int)MathF.Round(c.HomePos.Y * 2f);
                int key = (gx + 512) << 16 | (gz + 512);

                float topZ = c.HomePos.Z + c.Scale;

                if (!_occlusionTopZ.TryGetValue(key, out float existing) || topZ > existing)
                    _occlusionTopZ[key] = topZ;
            }

            Console.WriteLine($"[CubeCityBuilder] Occlusion map rebuilt — {_occlusionTopZ.Count} columns");
        }

        private void SetupRuinsSettle()
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;
                c.Vel *= 0.3f;
                c.Color = Vector3.Lerp(c.Color, new Vector3(0.4f, 0.35f, 0.3f), 0.5f);
            }
            Array.Clear(_fullGrid, 0, FULL_GRID * FULL_GRID);
            Array.Clear(_fullHeight, 0, FULL_GRID * FULL_GRID);
            _buildingCount = 0;
            _holoSignCount = 0;
        }

        private void SetupFoundationCrawl()
        {
            int kept = 0;
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (kept < 3)
                {
                    c.Pos = new Vector3(
      (float)(_cityRand.NextDouble() - 0.5f) * 4f,
      (float)(_cityRand.NextDouble() - 0.5f) * 4f,
      1.5f + 1.5f);
                    c.HomePos = c.Pos;
                    c.Vel = Vector3.Zero;
                    c.Scale = 0.8f;
                    c.Color = new Vector3(0.4f, 0.9f, 1.0f);
                    c.IsActive = true;
                    kept++;
                }
                else
                {
                    c.IsActive = false;
                }
            }
            _cityActiveCount = kept;
        }

        private void SetupDistrict(int district, int footMin, int footMax,
            float heightMax, int countTarget)
        {
            int placed = 0;
            int attempts = 0;
            int maxAttempts = countTarget * 8;

            while (placed < countTarget && attempts < maxAttempts
                   && _buildingCount < MAX_BUILDINGS
                   && _cityActiveCount < MAX_CITY_CUBES - 50)
            {
                attempts++;

                int gx, gz;
                GetDistrictCell(district, out gx, out gz);

                int fw = footMin + _cityRand.Next(footMax - footMin + 1);
                int fd = footMin + _cityRand.Next(footMax - footMin + 1);

                if (!FootprintClear(gx, gz, fw, fd)) continue;

                ClaimFootprint(gx, gz, fw, fd);

                float h = 1f + (float)_cityRand.NextDouble() * heightMax;
                var col = DistrictColor(district, h, heightMax, _cityRand);

                if (_buildingCount < MAX_BUILDINGS)
                {
                    _buildings[_buildingCount++] = new BuildingRect
                    {
                        GridX = gx,
                        GridZ = gz,
                        FootW = fw,
                        FootD = fd,
                        Height = h,
                        Color = col,
                        District = district,
                    };
                }

                int floors = (int)MathF.Ceiling(h);
                int cellsPerFloor = fw * fd;
                int maxCubesForBuilding = Math.Min(floors * cellsPerFloor, 80);
                int cubesPlaced = 0;

                for (int floor = 0; floor < floors && cubesPlaced < maxCubesForBuilding; floor++)
                {
                    for (int bx = 0; bx < fw && cubesPlaced < maxCubesForBuilding; bx++)
                    {
                        for (int bz = 0; bz < fd && cubesPlaced < maxCubesForBuilding; bz++)
                        {
                            var worldPos = FullGridToWorld(gx + bx, gz + bz, floor);
                            var dropFrom = worldPos + new Vector3(0f, 0f, 12f + floor * 0.5f);

                            float floorT = (float)floor / floors;
                            var floorCol = Vector3.Lerp(col, col * 1.3f, floorT);
                            floorCol = new Vector3(
                                Math.Clamp(floorCol.X, 0f, 1f),
                                Math.Clamp(floorCol.Y, 0f, 1f),
                                Math.Clamp(floorCol.Z, 0f, 1f));

                            int idx = SpawnCube(dropFrom, worldPos,
                                color: floorCol,
                                scale: 1.0f,
                                isVehicle: false,
                                layer: 13 + district);

                            if (idx >= 0) cubesPlaced++;
                        }
                    }
                }

                placed++;
            }

            Console.WriteLine($"[CubeCityBuilder] District {district} placed {placed} buildings");
        }

        private void SetupHologramSigns()
        {
            _holoSignCount = 0;
            int signsToPlace = 40 + _cityRand.Next(40);

            for (int i = 0; i < signsToPlace && _holoSignCount < MAX_HOLO_SIGNS; i++)
            {
                float wx = (float)(_cityRand.NextDouble() - 0.5f) * 100f;
                float wy = (float)(_cityRand.NextDouble() - 0.5f) * 100f;

                int gx = (int)(wx + FULL_GRID / 2f);
                int gz = (int)(wy + FULL_GRID / 2f);
                gx = Math.Clamp(gx, 0, FULL_GRID - 1);
                gz = Math.Clamp(gz, 0, FULL_GRID - 1);
                float baseH = _fullHeight[gx, gz];
                float signH = Math.Max(3f, baseH + 2f + (float)_cityRand.NextDouble() * 5f);

                int signType = i % AdNames.Length;
                var col = AdColors[signType];

                int signW = 2 + _cityRand.Next(3);
                int signH2 = 1 + _cityRand.Next(2);

                _holoSigns[_holoSignCount++] = new HologramSign
                {
                    Pos = new Vector3(wx, wy, signH + 1.5f),
                    Color = col,
                    Width = signW,
                    Height = signH2,
                    Phase = (float)_cityRand.NextDouble() * MathF.PI * 2f,
                    PulseSpeed = 1.5f + (float)_cityRand.NextDouble() * 3f,
                    SignType = signType,
                    IsActive = true,
                };

                for (int sx = 0; sx < signW && _cityActiveCount < MAX_CITY_CUBES; sx++)
                {
                    for (int sz = 0; sz < signH2 && _cityActiveCount < MAX_CITY_CUBES; sz++)
                    {
                        var pos = new Vector3(
                            wx + sx * 1.1f - signW * 0.5f,
                            wy,
                            signH + sz * 1.1f + 1.5f);

                        int idx = SpawnCube(pos, pos,
                            color: col,
                            scale: 0.9f,
                            isVehicle: false,
                            layer: 19);

                        if (idx >= 0)
                        {
                            _cityInstances[idx].Phase = _holoSigns[_holoSignCount - 1].Phase
                                + sx * 0.3f + sz * 0.2f;
                        }
                    }
                }
            }

            Console.WriteLine($"[CubeCityBuilder] Hologram signs placed: {_holoSignCount}");
        }

        private void SetupMegaStructure()
        {
            _megaStructureCenter = new Vector3(0f, 0f, 0f);
            _megaStructureTargetHeight = 50f;
            _megaSpawnedLayers = 0;

            for (int gx = FULL_GRID / 2 - 2; gx < FULL_GRID / 2 + 2; gx++)
                for (int gz = FULL_GRID / 2 - 2; gz < FULL_GRID / 2 + 2; gz++)
                    if (gx >= 0 && gx < FULL_GRID && gz >= 0 && gz < FULL_GRID)
                        _fullHeight[gx, gz] = 1.5f + 1.5f;
        }

        private void TickRuinsSettle(float delta, float now)
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;
                c.Vel *= (1f - delta * 3f);
                c.Pos += c.Vel * delta;
                if (c.Pos.Z > 0.1f)
                    c.Vel = new Vector3(c.Vel.X, c.Vel.Y, c.Vel.Z - 6f * delta);
                else c.Pos = new Vector3(c.Pos.X, c.Pos.Y, 1.5f + 1.5f);
                c.Color = Vector3.Lerp(c.Color, new Vector3(0.35f, 0.3f, 0.28f), delta * 1.2f);
            }
        }

        private void TickFoundationCrawl(float delta, float now)
        {
            _citySpawnAccum += delta * 2f;
            int toPlace = (int)_citySpawnAccum;
            _citySpawnAccum -= toPlace;

            float crawlRadius = 1f + _cityPhaseTimer * 1.5f;

            for (int n = 0; n < toPlace && _cityActiveCount < MAX_CITY_CUBES; n++)
            {
                float angle = (float)_cityRand.NextDouble() * MathF.PI * 2f;
                float r = (float)_cityRand.NextDouble() * crawlRadius;
                var pos = new Vector3(
                    MathF.Cos(angle) * r,
                    MathF.Sin(angle) * r,
                    0f);

                int gx = (int)(pos.X + FULL_GRID / 2f);
                int gz = (int)(pos.Y + FULL_GRID / 2f);
                if (gx < 0 || gx >= FULL_GRID || gz < 0 || gz >= FULL_GRID) continue;
                if (_fullGrid[gx, gz]) continue;
                if (gx % 6 == 0 || gz % 6 == 0) continue;

                _fullGrid[gx, gz] = true;
                var worldPos = FullGridToWorld(gx, gz, 1.5f);
                SpawnCube(worldPos + Vector3.UnitZ * 4f, worldPos,
                    color: new Vector3(0.45f, 0.5f, 0.55f),
                    scale: 1.0f, isVehicle: false, layer: 14);
            }

            DropLayerIntoPlace(delta, layer: 14);
        }

        private void TickDistrictBuild(float delta, float now, int layer)
        {
            DropLayerIntoPlace(delta, layer);

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.StructureLayer != layer) continue;
                if (c.HomePos.Z > 8f)
                {
                    float sway = MathF.Sin(now * 0.4f + c.Phase) * 0.03f;
                    c.Pos = new Vector3(c.HomePos.X + sway, c.HomePos.Y, c.Pos.Z);
                }
            }
        }

        private void TickHologramSigns(float delta, float now)
        {
            DropLayerIntoPlace(delta, layer: 19);

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.StructureLayer != 19) continue;
                if (Vector3.DistanceSquared(c.Pos, c.HomePos) > 0.5f) continue;

                float pulse = 0.6f + 0.4f * MathF.Sin(now * 3f + c.Phase);
                float flicker = (float)_cityRand.NextDouble() < 0.002f ? 0f : 1f;

                Vector3 adColor = new Vector3(0f, 1f, 0.8f);
                for (int s = 0; s < _holoSignCount; s++)
                {
                    if (!_holoSigns[s].IsActive) continue;
                    float dx = c.HomePos.X - _holoSigns[s].Pos.X;
                    float dy = c.HomePos.Y - _holoSigns[s].Pos.Y;
                    if (dx * dx + dy * dy < 25f) { adColor = _holoSigns[s].Color; break; }
                }

                c.Color = adColor * (pulse * flicker);
                c.Pos = new Vector3(
                    c.HomePos.X,
                    c.HomePos.Y,
                    c.HomePos.Z + MathF.Sin(now * 1.2f + c.Phase) * 0.12f);

                c.Scale = 0.85f + 0.1f * pulse;
            }
        }

        private void TickNeonNight(float delta, float now)
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                float hueInput = now * 0.05f + c.Phase * 0.15f;
                float hue = hueInput - MathF.Floor(hueInput);

                var neon = HueToRgb(hue);

                float pulse = 0.7f + 0.3f * MathF.Sin(now * 2f + c.Phase);

                c.Color = Lerp(c.Color, neon * pulse, delta * 0.8f);

                if (c.HomePos.Z > 5f)
                {
                    float sway = MathF.Sin(now * 0.6f + c.Phase) * 0.04f;
                    c.Pos = new Vector3(c.HomePos.X + sway, c.HomePos.Y, c.HomePos.Z);
                }
            }
        }

        private void TickMegaStructure(float delta, float now)
        {
            _citySpawnAccum += delta * 0.8f;
            int toPlace = (int)_citySpawnAccum;
            _citySpawnAccum -= toPlace;

            for (int n = 0; n < toPlace && _megaSpawnedLayers < _megaStructureTargetHeight; n++)
            {
                int floor = _megaSpawnedLayers;
                _megaSpawnedLayers++;

                float t = (float)floor / _megaStructureTargetHeight;

                int width = (int)Lerp(4f, 2f, t);
                int depth = width;

                var baseCol = new Vector3(0.3f, 0.3f, 0.35f);
                var topCol = HueToRgb(t);
                var col = Lerp(baseCol, topCol, t);

                for (int bx = -width / 2; bx < width / 2 && _cityActiveCount < MAX_CITY_CUBES; bx++)
                {
                    for (int bz2 = -depth / 2; bz2 < depth / 2 && _cityActiveCount < MAX_CITY_CUBES; bz2++)
                    {
                        var home = new Vector3(_megaStructureCenter.X + bx, _megaStructureCenter.Y + bz2, floor + 1.5f + 1.5f);

                        var spawn = home + new Vector3(0f, 0f, 60f + floor * 0.3f);

                        int idx = SpawnCube(spawn, home, color: col, scale: 1.0f, isVehicle: false, layer: 22);
                        if (idx >= 0)
                            _cityInstances[idx].Vel = new Vector3(0f, 0f, -15f - floor * 0.2f);
                    }
                }
            }

            DropLayerIntoPlace(delta, layer: 22);

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.StructureLayer != 22) continue;

                if (c.HomePos.Z > _megaStructureTargetHeight * 0.7f)
                    c.Phase += delta * 0.5f;
            }
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return a + (b - a) * t;
        }

        private void TickCityAtPeace2(float delta, float now)
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                float bob = MathF.Sin(now * 0.8f + c.Phase) * 0.04f;
                if (c.HomePos.Z > 2f)
                    c.Pos = new Vector3(c.HomePos.X, c.HomePos.Y, c.HomePos.Z + bob);

                float warm = 0.9f + 0.1f * MathF.Sin(now * 1.5f + c.Phase);
                c.Color = new Vector3(
                    Math.Clamp(c.Color.X * warm, 0f, 1f),
                    Math.Clamp(c.Color.Y * warm, 0f, 1f),
                    Math.Clamp(c.Color.Z * warm, 0f, 1f));
            }
        }

        private void DropLayerIntoPlace(float delta, int layer)
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.StructureLayer != layer) continue;
                if (c.Pos.Z <= c.HomePos.Z + 0.05f) continue;

                c.Vel = new Vector3(0f, 0f, c.Vel.Z - 20f * delta);
                c.Pos = new Vector3(c.HomePos.X, c.HomePos.Y, c.Pos.Z + c.Vel.Z * delta);
                if (c.Pos.Z < c.HomePos.Z)
                {
                    c.Pos = c.HomePos;
                    c.Vel = Vector3.Zero;
                }
            }
        }

        private void GetDistrictCell(int district, out int gx, out int gz)
        {
            int half = FULL_GRID / 2;
            switch (district)
            {
                case 0:
                    gx = half - 15 + _cityRand.Next(30);
                    gz = half - 15 + _cityRand.Next(30);
                    break;
                case 1:
                    float angle = (float)_cityRand.NextDouble() * MathF.PI * 2f;
                    float r = 20f + (float)_cityRand.NextDouble() * 40f;
                    gx = Math.Clamp((int)(half + MathF.Cos(angle) * r), 2, FULL_GRID - 10);
                    gz = Math.Clamp((int)(half + MathF.Sin(angle) * r), 2, FULL_GRID - 10);
                    break;
                case 2:
                    gx = _cityRand.Next(2, half - 5);
                    gz = _cityRand.Next(2, FULL_GRID - 10);
                    break;
                case 3:
                    gx = (_cityRand.Next(FULL_GRID / 10)) * 10 + _cityRand.Next(3);
                    gz = _cityRand.Next(2, FULL_GRID - 10);
                    break;
                default:
                    gx = _cityRand.Next(2, FULL_GRID - 10);
                    gz = _cityRand.Next(2, FULL_GRID - 10);
                    break;
            }
        }

        private bool FootprintClear(int gx, int gz, int fw, int fd)
        {
            if (gx < 0 || gz < 0 || gx + fw >= FULL_GRID || gz + fd >= FULL_GRID)
                return false;
            for (int x = gx; x < gx + fw; x++)
                for (int z = gz; z < gz + fd; z++)
                    if (_fullGrid[x, z]) return false;
            return true;
        }

        private void ClaimFootprint(int gx, int gz, int fw, int fd)
        {
            for (int x = gx; x < gx + fw && x < FULL_GRID; x++)
                for (int z = gz; z < gz + fd && z < FULL_GRID; z++)
                {
                    _fullGrid[x, z] = true;
                }
        }

        private Vector3 FullGridToWorld(int gx, int gz, float height)
        {
            float ox = -(FULL_GRID / 2f);
            float oz = -(FULL_GRID / 2f);
            return new Vector3(ox + gx, oz + gz, height);
        }

        private static Vector3 DistrictColor(int district, float height, float maxH, Random rand)
        {
            float t = height / maxH;
            float jitter = (float)(rand.NextDouble() - 0.5f) * 0.15f;
            return district switch
            {
                0 => Vector3.Lerp(new Vector3(0.3f + jitter, 0.5f, 0.85f), new Vector3(0.1f, 0.95f, 1.0f), t),
                1 => Vector3.Lerp(new Vector3(0.75f + jitter, 0.6f, 0.4f), new Vector3(0.9f, 0.5f, 0.25f), t),
                2 => Vector3.Lerp(new Vector3(0.25f + jitter, 0.22f, 0.2f), new Vector3(0.6f, 0.3f, 0.1f), t),
                3 => HueToRgb((t + (float)rand.NextDouble() * 0.3f) % 1f),
                _ => HueToRgb((float)rand.NextDouble()),
            };
        }

        private void TickDormant(float delta, float now) { }
        private void TickFirstCube(float delta, float now)
        {
            if (_cityActiveCount < 1) return;
            ref var c = ref _cityInstances[0];
            float pulse = 0.9f + 0.1f * MathF.Sin(now * 4f);
            c.Scale = pulse;
            c.Pos = new Vector3(0f, 0f, MathF.Sin(now * 2f) * 0.2f + 1.5f);

            if (_cityPhaseTimer > 1.5f && _cityActiveCount == 1)
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i / 8f * MathF.PI * 2f;
                    float r = 2.5f;
                    var seedPos = new Vector3(MathF.Cos(angle) * r, MathF.Sin(angle) * r, 0f);
                    SpawnCube(seedPos, seedPos, color: HueToRgb(i / 8f), scale: 0.7f, isVehicle: false, layer: 1);
                }
            }
        }

        private void SetupVehicles()
        {
            _vehicleCount = 0;

            // 8 vehicles drive outward along the MC road lines
            for (int i = 0; i < 8 && _cityActiveCount < MAX_CITY_CUBES; i++)
            {
                float angle = i / 8f * MathF.PI * 2f;
                // Target = road intersection at radius 5
                int targetCX = (int)MathF.Round(MathF.Cos(angle) * 5f / MC_CELL) * 7;
                int targetCZ = (int)MathF.Round(MathF.Sin(angle) * 5f / MC_CELL) * 7;
                var target = MCGridToWorld(targetCX, targetCZ, 0);
                target = new Vector3(target.X, target.Y, 0f);

                _vehicleTargets[_vehicleCount] = target;
                _vehicleCount++;

                // Vehicle cube — yellow, bigger, rides at Z=1 above ground
                SpawnCube(
                    new Vector3(0f, 0f, 0f),
                    target,
                    new Vector3(1f, 0.75f, 0.1f),
                    1.3f, true, 2);

                // 3 payload cubes trail behind
                for (int p = 0; p < 3; p++)
                {
                    var payloadHome = target + new Vector3(
                        (float)(_cityRand.NextDouble() - 0.5f) * MC_CELL,
                        (float)(_cityRand.NextDouble() - 0.5f) * MC_CELL,
                        0f);
                    SpawnCube(
                        new Vector3(0f, 0f, 0f),
                        payloadHome,
                        new Vector3(0.75f, 0.75f, 0.8f),
                        0.85f, false, 2);
                }
            }
        }

        private void TickSpawnVehicles(float delta, float now)
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                if (c.IsVehicle)
                {
                    var toHome = c.HomePos - c.Pos;
                    float dist = toHome.Length();
                    if (dist > 0.1f)
                    {
                        c.Vel = Vector3.Normalize(toHome) * MathF.Min(dist * 3f, 8f);
                        c.Pos += c.Vel * delta;
                        c.Pos = new Vector3(c.Pos.X, c.Pos.Y, 0.5f + 1.5f + MathF.Sin(now * 6f + c.Phase) * 0.2f);
                    }
                    c.Phase += delta * 4f;
                }
                else if (c.StructureLayer == 2)
                {
                    var toHome = c.HomePos - c.Pos;
                    float dist = toHome.Length();
                    if (dist > 0.05f)
                    {
                        c.Vel = Vector3.Normalize(toHome) * MathF.Min(dist * 2f, 6f);
                        c.Pos += c.Vel * delta;
                    }
                }
            }
        }


        private void TickLayFoundations(float delta, float now)
        {
            _citySpawnAccum += delta * 20f; // 20 foundation plates per second
            int toPlace = (int)_citySpawnAccum;
            _citySpawnAccum -= toPlace;

            int radius = PhaseRadius[3];
            int placed = 0;

            for (int n = 0; n < toPlace && _cityActiveCount < MAX_CITY_CUBES - 10; n++)
            {
                // Spiral outward using _citySpawnIndex
                int cx, cz;
                SpiralIndexMC(_citySpawnIndex, out cx, out cz);
                _citySpawnIndex++;

                if (Math.Abs(cx) > radius || Math.Abs(cz) > radius) continue;
                if (_mcGrid.ContainsKey(MCKey(cx, cz))) continue;

                // Leave road gaps every 7 cells from center
                if ((cx % 7 == 0 && Math.Abs(cx) > 2) ||
                    (cz % 7 == 0 && Math.Abs(cz) > 2)) continue;

                var worldPos = MCGridToWorld(cx, cz, 0);
                _mcGrid[MCKey(cx, cz)] = 0f;

                SpawnCube(
                    worldPos + new Vector3(0f, 0f, 10f), // drop from above
                    worldPos,
                    new Vector3(0.5f, 0.52f, 0.55f),
                    1.0f, false, 3);

                placed++;
            }

            DropLayerIntoPlace(delta, layer: 3);
        }

        // Spiral index for MC grid (centered at 0,0)
        // Replace the SpiralIndexMC method with this guarded implementation
        private void SpiralIndexMC(int n, out int cx, out int cz)
        {
            // Handle trivial cases to avoid division by zero
            if (n == 0) { cx = 0; cz = 0; return; }
            if (n == 1) { cx = 1; cz = 0; return; }

            int layer = (int)MathF.Ceiling((MathF.Sqrt(n) - 1f) / 2f);
            int leg = n - (2 * layer - 1) * (2 * layer - 1);
            int side = leg / (2 * layer);
            int pos = leg % (2 * layer);

            switch (side)
            {
                case 0: cx = -layer + pos; cz = -layer; break;
                case 1: cx = layer; cz = -layer + pos; break;
                case 2: cx = layer - pos; cz = layer; break;
                default: cx = -layer; cz = layer - pos; break;
            }
        }


        private void TickBuild(float delta, float now, float maxHeight,
            float cubesPerSec, int layer)
        {
            // Map layer to phase index for radius + building params
            int phaseIdx = layer; // layers 4-8 map directly to phase indices 4-8
            if (phaseIdx < 0 || phaseIdx >= PhaseRadius.Length) phaseIdx = 4;

            int radius = PhaseRadius[phaseIdx];
            int minW = PhaseBuildingParams[phaseIdx, 0];
            int maxW = PhaseBuildingParams[phaseIdx, 1];
            int minD = PhaseBuildingParams[phaseIdx, 2];
            int maxD = PhaseBuildingParams[phaseIdx, 3];
            int minFloors = PhaseBuildingParams[phaseIdx, 4];
            int maxFloors = PhaseBuildingParams[phaseIdx, 5];

            // Spawn new buildings incrementally — one building per accumulator tick
            _citySpawnAccum += delta * (cubesPerSec / 8f); // buildings per second
            int toPlace = (int)_citySpawnAccum;
            _citySpawnAccum -= toPlace;

            for (int n = 0; n < toPlace && _cityActiveCount < MAX_CITY_CUBES - 200; n++)
            {
                // Pick random cell within this phase's radius ring
                // Favor the outer ring (new expansion) but allow anywhere free
                int attempts = 0;
                int cx = 0, cz = 0;
                int bw = 0, bd = 0;
                bool found = false;

                while (attempts++ < 60)
                {
                    float angle = (float)_cityRand.NextDouble() * MathF.PI * 2f;
                    // Bias toward outer ring — between prev radius and current radius
                    int prevRadius = (phaseIdx > 3) ? PhaseRadius[phaseIdx - 1] : 0;
                    float r = prevRadius + (float)_cityRand.NextDouble() * (radius - prevRadius);
                    cx = (int)MathF.Round(MathF.Cos(angle) * r);
                    cz = (int)MathF.Round(MathF.Sin(angle) * r);

                    bw = minW + _cityRand.Next(maxW - minW + 1);
                    bd = minD + _cityRand.Next(maxD - minD + 1);

                    // Center the building footprint on chosen cell
                    int originX = cx - bw / 2;
                    int originZ = cz - bd / 2;

                    // Check radius bounds — whole building must fit in radius
                    bool inBounds = true;
                    for (int fx = originX; fx < originX + bw; fx++)
                        for (int fz = originZ; fz < originZ + bd; fz++)
                            if (Math.Abs(fx) > radius || Math.Abs(fz) > radius)
                            { inBounds = false; break; }
                    if (!inBounds) continue;

                    // Check footprint free on MC grid
                    if (!MCCellFree(originX, originZ, bw, bd)) continue;

                    cx = originX;
                    cz = originZ;
                    found = true;
                    break;
                }

                if (!found) continue;

                int floors = minFloors + _cityRand.Next(maxFloors - minFloors + 1);
                var baseCol = BuildColor(layer, 0f, maxHeight);
                var topCol = BuildColor(layer, maxHeight, maxHeight);

                // Spawn cubes floor by floor, cell by cell
                // Each cell in footprint = one cube column
                for (int fx = cx; fx < cx + bw && _cityActiveCount < MAX_CITY_CUBES - 20; fx++)
                {
                    for (int fz = cz; fz < cz + bd && _cityActiveCount < MAX_CITY_CUBES - 20; fz++)
                    {
                        for (int floor = 0; floor < floors; floor++)
                        {
                            var home = MCGridToWorld(fx, fz, floor);
                            // Drop from above — higher floors drop from higher
                            var spawn = home + new Vector3(0f, 0f, 12f + floor * 1.5f);

                            float t = (float)floor / MathF.Max(floors - 1, 1);
                            var col = Vector3.Lerp(baseCol, topCol, t);

                            // Slight color jitter per column for visual variety
                            float jit = (float)(_cityRand.NextDouble() - 0.5f) * 0.08f;
                            col = new Vector3(
                                Math.Clamp(col.X + jit, 0f, 1f),
                                Math.Clamp(col.Y + jit, 0f, 1f),
                                Math.Clamp(col.Z + jit, 0f, 1f));

                            SpawnCube(spawn, home, col, 1.0f, false, layer);
                        }
                    }
                }

                // Claim footprint AFTER spawning so building is fully placed
                MCClaimCells(cx, cz, bw, bd, floors);

                // Lock village footprint during BuildVillage — these cells
                // are the town square and will never be overwritten
                if (layer == 4)
                {
                    for (int fx2 = cx; fx2 < cx + bw; fx2++)
                        for (int fz2 = cz; fz2 < cz + bd; fz2++)
                            _villageFootprint.Add(MCKey(fx2, fz2));
                }
            }

            // Drop all cubes of this layer toward their home position
            DropLayerIntoPlace(delta, layer);
        }

        private void TickFlyingTransports(float delta, float now)
        {
            int flyCount = 0;
            for (int i = 0; i < _cityActiveCount; i++)
                if (_cityInstances[i].IsVehicle && _cityInstances[i].StructureLayer == 7)
                    flyCount++;

            if (flyCount < 6 && _cityActiveCount < MAX_CITY_CUBES)
            {
                float angle = (float)_cityRand.NextDouble() * MathF.PI * 2f;
                float r = 8f + (float)_cityRand.NextDouble() * 6f;
                SpawnCube(new Vector3(MathF.Cos(angle) * r, MathF.Sin(angle) * r, 12f + 1.5f), Vector3.Zero, new Vector3(0.3f, 1f, 0.8f), 0.9f, true, 7);
            }

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || !c.IsVehicle || c.StructureLayer != 7) continue;
                float orbitAngle = now * 0.4f + c.Phase;
                float orbitR = 8f + MathF.Sin(now * 0.3f + c.Phase) * 4f;
                c.Pos = new Vector3(
                    MathF.Cos(orbitAngle) * orbitR,
                    MathF.Sin(orbitAngle) * orbitR,
                    10f + 1.5f + MathF.Sin(now * 0.8f + c.Phase) * 2f);
            }
        }

        private void TickOrbitalRings(float delta, float now)
        {
            int ringCount = 0;
            for (int i = 0; i < _cityActiveCount; i++)
                if (_cityInstances[i].StructureLayer == 8 && _cityInstances[i].IsVehicle)
                    ringCount++;

            if (ringCount < 32 && _cityActiveCount < MAX_CITY_CUBES)
            {
                float angle = (float)ringCount / 32f * MathF.PI * 2f;
                float r = 18f;
                SpawnCube(new Vector3(MathF.Cos(angle) * r, MathF.Sin(angle) * r, 30f + 1.5f), Vector3.Zero, new Vector3(0.8f, 0.4f, 1.0f), 0.7f, true, 8);
            }

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || !c.IsVehicle || c.StructureLayer != 8) continue;
                float orbitAngle = now * 0.25f + c.Phase;
                float r = 18f;
                c.Pos = new Vector3(
                    MathF.Cos(orbitAngle) * r,
                    MathF.Sin(orbitAngle) * r,
                    30f + 1.5f + MathF.Sin(now * 0.5f + c.Phase) * 1.5f);
            }
        }
        private void SetupPeacefulLife()
        {
            _baseColorsCaptured = false;
            _holoLightCount = 0;
            _emergencyCount = 0;

            // Capture base colors once — these never get modified
            // Fixes the compounding darkness bug in the old version
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                _cityBaseColors[i] = c.IsActive ? c.Color : Vector3.Zero;
            }
            _baseColorsCaptured = true;

            // Spawn holo building lights — small glowing cubes on top of buildings
            // Pick random building-top positions from existing city cubes
            _holoLightStartIdx = _cityActiveCount;
            var rand = _cityRand;

            int lightsToSpawn = 80 + rand.Next(40);
            for (int i = 0; i < lightsToSpawn && _cityActiveCount < MAX_CITY_CUBES - 10; i++)
            {
                // Find a random existing building cube that's near its column top
                int attempts = 0;
                int sourceIdx = -1;
                while (attempts++ < 30)
                {
                    int si = rand.Next(_cityActiveCount - (_cityActiveCount > 0 ? 1 : 0));
                    if (!_cityInstances[si].IsActive) continue;
                    if (_cityInstances[si].IsVehicle) continue;
                    if (_cityInstances[si].HomePos.Z < 1.5f) continue; // skip ground floor
                    sourceIdx = si;
                    break;
                }
                if (sourceIdx < 0) continue;

                var buildingTop = _cityInstances[sourceIdx].HomePos;
                // Place light 1.2 units above building top
                var lightPos = new Vector3(
                    buildingTop.X + (float)(rand.NextDouble() - 0.5f) * 0.3f,
                    buildingTop.Y + (float)(rand.NextDouble() - 0.5f) * 0.3f,
                    buildingTop.Z + 1.2f);

                // Color — city window colors: warm yellow, cool white, blue, green
                Vector3 lightColor = (rand.Next(4)) switch
                {
                    0 => new Vector3(1.0f, 0.9f, 0.4f),  // warm yellow window
                    1 => new Vector3(0.8f, 0.9f, 1.0f),  // cool white office
                    2 => new Vector3(0.3f, 0.6f, 1.0f),  // blue accent
                    _ => new Vector3(0.4f, 1.0f, 0.5f),  // green exit sign
                };

                int idx = SpawnCube(lightPos, lightPos, lightColor, 0.3f, false, layer: 9);
                if (idx >= 0)
                {
                    _cityInstances[idx].Phase = (float)rand.NextDouble() * MathF.PI * 2f;
                    _holoLightCount++;
                }
            }

            // Spawn emergency vehicles — drive along MC road gaps (every 7 cells = 14 world units)
            SpawnEmergencyVehicles();
        }

        private void SpawnEmergencyVehicles()
        {
            _emergencyCount = 0;
            var rand = _cityRand;

            // 3 cop cars + 2 ambulances
            int cops = 3, ambs = 2;
            for (int v = 0; v < cops + ambs && _cityActiveCount < MAX_CITY_CUBES - 5; v++)
            {
                bool isCop = v < cops;

                // Start on a random road intersection
                // Roads are every 7 MC cells = every 14 world units
                int roadX = (rand.Next(5) - 2) * 7; // -14, -7, 0, 7, 14
                int roadZ = (rand.Next(5) - 2) * 7;
                var startPos = MCGridToWorld(roadX, roadZ, 0);
                startPos = new Vector3(startPos.X, startPos.Y, 1.8f); // ride above ground

                // Target = another random road intersection
                int targetX = (rand.Next(5) - 2) * 7;
                int targetZ = (rand.Next(5) - 2) * 7;
                var endPos = MCGridToWorld(targetX, targetZ, 0);
                endPos = new Vector3(endPos.X, endPos.Y, 1.8f);

                // Vehicle color — cop starts blue, ambulance starts red
                var col = isCop
                    ? new Vector3(0.1f, 0.3f, 1.0f)   // cop blue
                    : new Vector3(1.0f, 0.15f, 0.15f); // ambulance red

                int idx = SpawnCube(startPos, startPos, col, 1.1f, true, layer: 9);
                if (idx < 0) continue;

                _emergencyVehicles[_emergencyCount++] = new EmergencyVehicle
                {
                    InstanceIdx = idx,
                    PathStart = startPos,
                    PathEnd = endPos,
                    T = 0f,
                    Speed = 3.5f + (float)rand.NextDouble() * 2f,
                    IsCop = isCop,
                    FlashTimer = 0f,
                    FlashState = false,
                };
            }
        }
        private void TickPeacefulLife(float delta, float now)
        {
            if (!_baseColorsCaptured) return;

            // ── Building gentle pulse — reads from base color, never compounds ──
            for (int i = 0; i < _holoLightStartIdx && i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.IsVehicle) continue;
                if (c.StructureLayer == 9) continue; // skip holo lights

                // Gentle sine pulse — ALWAYS applied to base color, not current color
                // This is the fix — _cityBaseColors[i] never changes
                float pulse = 0.82f + 0.18f * MathF.Sin(now * 0.9f + c.Phase);
                var base3 = _cityBaseColors[i];

                c.Color = new Vector3(
                    Math.Clamp(base3.X * pulse, 0f, 1f),
                    Math.Clamp(base3.Y * pulse, 0f, 1f),
                    Math.Clamp(base3.Z * pulse, 0f, 1f));

                // Upper floors bob very gently
                if (c.HomePos.Z > 3f)
                {
                    float bob = MathF.Sin(now * 0.6f + c.Phase) * 0.03f;
                    c.Pos = new Vector3(c.HomePos.X, c.HomePos.Y, c.HomePos.Z + bob);
                }
            }

            // ── Holo building lights — flash on/off like real windows ──
            for (int i = _holoLightStartIdx; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.StructureLayer != 9 || c.IsVehicle) continue;

                // Each light flashes at its own rate based on Phase
                float flashRate = 0.3f + MathF.Sin(c.Phase) * 0.2f; // 0.1 - 0.5 Hz
                float cycle = MathF.Sin(now * flashRate * MathF.PI * 2f + c.Phase);

                // Hard on/off flash — looks like real window lights switching
                bool lightOn = cycle > 0.1f;

                // Occasional random flicker — office lights turning on/off
                if ((float)_cityRand.NextDouble() < 0.001f)
                {
                    // Random window event — briefly very bright then normal
                    c.Color = _cityBaseColors[i] * 2.5f;
                }
                else
                {
                    float brightness = lightOn ? (0.9f + 0.1f * cycle) : 0.05f;
                    c.Color = _cityBaseColors[i] * brightness;
                }

                // Lights float very slightly
                c.Pos = new Vector3(
                    c.HomePos.X,
                    c.HomePos.Y,
                    c.HomePos.Z + MathF.Sin(now * 0.4f + c.Phase) * 0.05f);
            }

            // ── Emergency vehicles ──
            TickEmergencyVehicles(delta, now);
        }
        private void TickEmergencyVehicles(float delta, float now)
        {
            var rand = _cityRand;

            for (int v = 0; v < _emergencyCount; v++)
            {
                ref var ev = ref _emergencyVehicles[v];
                if (ev.InstanceIdx < 0 || ev.InstanceIdx >= _cityActiveCount) continue;

                ref var cube = ref _cityInstances[ev.InstanceIdx];
                if (!cube.IsActive) continue;

                // Move along path
                ev.T += delta * ev.Speed / Vector3.Distance(ev.PathStart, ev.PathEnd);
                ev.T = Math.Clamp(ev.T, 0f, 1f);

                cube.Pos = Vector3.Lerp(ev.PathStart, ev.PathEnd, ev.T);

                // Arrived — pick new destination on road grid
                if (ev.T >= 1f)
                {
                    ev.PathStart = ev.PathEnd;
                    int newX = (rand.Next(5) - 2) * 7;
                    int newZ = (rand.Next(5) - 2) * 7;
                    var newEnd = MCGridToWorld(newX, newZ, 0);
                    ev.PathEnd = new Vector3(newEnd.X, newEnd.Y, 1.8f);
                    ev.T = 0f;
                    // Randomize speed slightly each leg
                    ev.Speed = 3.5f + (float)rand.NextDouble() * 2.5f;
                }

                // Flash lights — cop alternates blue/red, ambulance alternates red/white
                ev.FlashTimer += delta;
                if (ev.FlashTimer >= 0.25f) // flash every 0.25s
                {
                    ev.FlashTimer = 0f;
                    ev.FlashState = !ev.FlashState;

                    if (ev.IsCop)
                    {
                        // Blue/red alternating — classic cop lights
                        cube.Color = ev.FlashState
                            ? new Vector3(0.1f, 0.2f, 1.0f)   // blue flash
                            : new Vector3(1.0f, 0.05f, 0.05f); // red flash
                    }
                    else
                    {
                        // Red/white alternating — ambulance lights
                        cube.Color = ev.FlashState
                            ? new Vector3(1.0f, 0.1f, 0.1f)   // red flash
                            : new Vector3(1.0f, 1.0f, 1.0f);  // white flash
                    }

                    // Scale pulse — vehicle "throbs" with the lights
                    cube.Scale = ev.FlashState ? 1.2f : 1.0f;
                }
            }
        }
        private void TickFireworks(float delta, float now)
        {
            _citySpawnAccum += delta * 3f;
            int toFire = (int)_citySpawnAccum;
            _citySpawnAccum -= toFire;

            for (int n = 0; n < toFire && _cityActiveCount < MAX_CITY_CUBES; n++)
            {
                float angle = (float)_cityRand.NextDouble() * MathF.PI * 2f;
                float r = (float)_cityRand.NextDouble() * 10f;
                var launchPos = new Vector3(MathF.Cos(angle) * r, MathF.Sin(angle) * r, 0f + 1.5f);

                int idx = SpawnCube(launchPos, launchPos + Vector3.UnitZ * 20f,
                    color: HueToRgb((float)_cityRand.NextDouble()),
                    scale: 0.6f, isVehicle: true, layer: 10);

                if (idx >= 0)
                {
                    _cityInstances[idx].Vel = new Vector3(
                        (float)(_cityRand.NextDouble() - 0.5f) * 3f,
                        (float)(_cityRand.NextDouble() - 0.5f) * 3f,
                        18f + (float)_cityRand.NextDouble() * 8f);
                }
            }

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive || c.StructureLayer != 10) continue;

                c.Vel = new Vector3(c.Vel.X * 0.98f, c.Vel.Y * 0.98f, c.Vel.Z - 12f * delta);
                c.Pos += c.Vel * delta;

                if (c.Vel.Z < 0f && c.Pos.Z > 8f + 1.5f && _cityActiveCount < MAX_CITY_CUBES - 8)
                {
                    var burstPos = c.Pos;
                    var burstColor = c.Color;
                    c.IsActive = false;

                    for (int b = 0; b < 8; b++)
                    {
                        float ba = b / 8f * MathF.PI * 2f;
                        float bs = 5f + (float)_cityRand.NextDouble() * 4f;
                        int bi = SpawnCube(burstPos, burstPos, burstColor, 0.25f, false, 11);
                        if (bi >= 0)
                        {
                            _cityInstances[bi].Vel = new Vector3(
                                MathF.Cos(ba) * bs,
                                MathF.Sin(ba) * bs,
                                (float)(_cityRand.NextDouble() - 0.3f) * 3f);
                        }
                    }
                }

                if (c.StructureLayer == 11)
                {
                    c.Scale = MathF.Max(0f, c.Scale - delta * 0.4f);
                    if (c.Pos.Z < 0f + 1.5f) c.IsActive = false;
                }
            }
        }

        private void TriggerExplosion()
        {
            var center = new Vector3(0f, 0f, 0f);
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                var dir = c.Pos - center;
                float dist = dir.Length();
                if (dist < 0.1f) dir = new Vector3((float)(_cityRand.NextDouble() - 0.5f), (float)(_cityRand.NextDouble() - 0.5f), 1f);

                float speed = 6f + (float)_cityRand.NextDouble() * 10f + c.HomePos.Z * 0.5f;
                c.Vel = Vector3.Normalize(dir) * speed;
                c.Vel = new Vector3(c.Vel.X, c.Vel.Y, c.Vel.Z + 2f + (float)_cityRand.NextDouble() * 4f);
                c.Color = HueToRgb((float)_cityRand.NextDouble());
            }
        }

        private void TickGrandExplosion(float delta, float now)
        {
            float t = _cityPhaseTimer / PhaseDurations[(int)CityPhase.GrandExplosion];

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                c.Vel = new Vector3(c.Vel.X, c.Vel.Y, c.Vel.Z - 4f * delta);
                c.Vel *= (1f - delta * 0.3f);
                c.Pos += c.Vel * delta;
                c.Phase += delta * 5f;

                float flash = 1f - t;
                c.Color = new Vector3(
                    c.Color.X * 0.99f + flash * 0.5f * delta,
                    c.Color.Y * 0.98f,
                    c.Color.Z * 0.97f);
            }
        }

        private void SetupRebuild()
        {
            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;
                float angle = (float)i / Math.Max(_cityActiveCount, 1) * MathF.PI * 2f;
                float r = 1f + (i % 10) * 0.5f;
                c.HomePos = new Vector3(MathF.Cos(angle) * r, MathF.Sin(angle) * r, 0f);
                c.Vel = Vector3.Zero;
            }

            Array.Clear(_cityGrid, 0, CITY_GRID * CITY_GRID);
            Array.Clear(_cityHeight, 0, CITY_GRID * CITY_GRID);
            Array.Clear(_fullGrid, 0, FULL_GRID * FULL_GRID);
            Array.Clear(_fullHeight, 0, FULL_GRID * FULL_GRID);

            _mcGrid.Clear();
            _villageFootprint.Clear();
            _occlusionTopZ.Clear();
            _buildingCount = 0;
            _holoSignCount = 0;

            Console.WriteLine("[CubeCityBuilder] Rebuild — all grids cleared for next cycle");
        }

        private void TickRebuild(float delta, float now)
        {
            float t = _cityPhaseTimer / PhaseDurations[(int)CityPhase.Rebuild];
            bool allHome = true;

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                var toHome = c.HomePos - c.Pos;
                float dist = toHome.Length();
                if (dist > 0.1f)
                {
                    allHome = false;
                    c.Vel = Vector3.Lerp(c.Vel, Vector3.Normalize(toHome) * 6f, delta * 4f);
                    c.Pos += c.Vel * delta;
                }
                else
                {
                    c.Pos = c.HomePos;
                    c.Vel = Vector3.Zero;
                }
                c.Color = Vector3.Lerp(c.Color, new Vector3(0.6f, 0.7f, 0.9f), delta * 1.5f);
            }

            if ((allHome || _cityPhaseTimer >= _cityNextPhaseAt) && _cityPhase == CityPhase.Rebuild)
            {
                AdvanceCityPhaseExpanded();
            }
        }

        private int SpawnCube(Vector3 pos, Vector3 home, Vector3 color, float scale, bool isVehicle, int layer)
        {
            if (_cityActiveCount >= MAX_CITY_CUBES) return -1;
            int idx = _cityActiveCount++;
            ref var c = ref _cityInstances[idx];
            c.Pos = new Vector3(pos.X, pos.Y, pos.Z + 1.5f);
            c.HomePos = new Vector3(home.X, home.Y, home.Z + 1.5f);
            c.Color = color;
            c.Scale = scale;
            c.IsVehicle = isVehicle;
            c.StructureLayer = layer;
            c.IsActive = true;
            c.Phase = (float)_cityRand.NextDouble() * MathF.PI * 2f;
            c.Vel = Vector3.Zero;
            c.VehicleT = 0f;
            return idx;
        }

        private Vector3 GridToWorld(int gx, int gz, float height)
        {
            float ox = -(CITY_GRID / 2f);
            float oz = -(CITY_GRID / 2f);
            return new Vector3(ox + gx, oz + gz, height);
        }

        private void SpiralIndex(int n, out int gx, out int gz)
        {
            int cx = CITY_GRID / 2;
            int cz = CITY_GRID / 2;
            if (n == 0) { gx = cx; gz = cz; return; }
            int layer = (int)MathF.Ceiling((MathF.Sqrt(n) - 1) / 2f);
            int leg = n - (2 * layer - 1) * (2 * layer - 1);
            int side = leg / (2 * layer);
            int pos = leg % (2 * layer);
            switch (side)
            {
                case 0: gx = cx - layer + pos; gz = cz - layer; break;
                case 1: gx = cx + layer; gz = cz - layer + pos; break;
                case 2: gx = cx + layer - pos; gz = cz + layer; break;
                default: gx = cx - layer; gz = cz + layer - pos; break;
            }
        }

        private Vector3 BuildColor(int layer, float height, float maxHeight)
        {
            float t = maxHeight > 0f ? height / maxHeight : 0f;
            return layer switch
            {
                3 => new Vector3(0.48f, 0.50f, 0.53f),  // Foundation — cool grey stone
                4 => Vector3.Lerp(                        // Village — earthy wood/thatch
                         new Vector3(0.72f, 0.55f, 0.32f),
                         new Vector3(0.85f, 0.68f, 0.38f), t),
                5 => Vector3.Lerp(                        // Town — stone with brick warmth
                         new Vector3(0.65f, 0.58f, 0.50f),
                         new Vector3(0.75f, 0.45f, 0.35f), t),
                6 => Vector3.Lerp(                        // City — grey stone to glass blue
                         new Vector3(0.55f, 0.60f, 0.68f),
                         new Vector3(0.25f, 0.75f, 0.95f), t),
                7 => Vector3.Lerp(                        // Metropolis — dark steel to neon
                         new Vector3(0.20f, 0.22f, 0.28f),
                         new Vector3(0.85f, 0.55f, 0.10f), t),
                8 => Vector3.Lerp(                        // Future — plasma purple to cyan
                         new Vector3(0.70f, 0.25f, 0.95f),
                         new Vector3(0.15f, 0.95f, 0.85f), t),
                _ => new Vector3(0.50f, 0.50f, 0.55f),
            };
        }

        private static Vector3 HueToRgb(float h)
        {
            float r = MathF.Abs(h * 6f - 3f) - 1f;
            float g = 2f - MathF.Abs(h * 6f - 2f);
            float b = 2f - MathF.Abs(h * 6f - 4f);
            return new Vector3(
                Math.Clamp(r, 0f, 1f),
                Math.Clamp(g, 0f, 1f),
                Math.Clamp(b, 0f, 1f));
        }

        private void PackBuffers()
        {

            float camX = ActiveCameraMode == CameraMode.Orthographic ? OrthoCamera.GetPosition().x : Camera.Position.X;
            float camY = ActiveCameraMode == CameraMode.Orthographic ? OrthoCamera.GetPosition().y : Camera.Position.Y;
            float camZ = ActiveCameraMode == CameraMode.Orthographic ? OrthoCamera.GetPosition().z : Camera.Position.Z;

            float cullRadSq = _cullRadius * _cullRadius;
            bool doDistanceCull = _cullRadius > 0f;
            bool doOcclusionCull = _occlusionTopZ.Count > 0;
            bool doFrustumCull = _frustumPlanes != null;

            int count = 0;
            int culledFrustum = 0;
            int culledOcclusion = 0;
            int culledDistance = 0;

            for (int i = 0; i < _cityActiveCount; i++)
            {
                ref var c = ref _cityInstances[i];
                if (!c.IsActive) continue;

                float px = c.Pos.X;
                float py = c.Pos.Y;
                float pz = c.Pos.Z;
                float radius = c.Scale * 0.87f;

                if (doDistanceCull)
                {
                    float dx = px - camX;
                    float dy = py - camY;
                    float dz = pz - camZ;
                    float distSq = dx * dx + dy * dy + dz * dz;
                    if (distSq > cullRadSq)
                    {
                        culledDistance++;
                        continue;
                    }
                }

                if (doFrustumCull)
                {
                    bool outside = false;
                    for (int p = 0; p < 6; p++)
                    {
                        float nx = _frustumPlanes[p * 4 + 0];
                        float ny = _frustumPlanes[p * 4 + 1];
                        float nz = _frustumPlanes[p * 4 + 2];
                        float d = _frustumPlanes[p * 4 + 3];

                        float dist = nx * px + ny * py + nz * pz + d;
                        if (dist < -radius)
                        {
                            outside = true;
                            break;
                        }
                    }
                    if (outside)
                    {
                        culledFrustum++;
                        continue;
                    }
                }

                if (doOcclusionCull && !c.IsVehicle && c.StructureLayer >= 3 && c.StructureLayer <= 22)
                {
                    int gx = (int)MathF.Round(c.HomePos.X * 2f);
                    int gz = (int)MathF.Round(c.HomePos.Y * 2f);
                    int key = (gx + 512) << 16 | (gz + 512);

                    if (_occlusionTopZ.TryGetValue(key, out float columnTopZ))
                    {
                        float cubeTopZ = pz + c.Scale;
                        if (cubeTopZ < columnTopZ - _occlusionBias)
                        {
                            culledOcclusion++;
                            continue;
                        }
                    }
                }

                _cityPositionBuf[count * 3 + 0] = px;
                _cityPositionBuf[count * 3 + 1] = py;
                _cityPositionBuf[count * 3 + 2] = pz;

                _cityScaleBuf[count] = c.Scale;
                _cityRotBuf[count] = c.Phase;

                _cityColorBuf[count * 3 + 0] = c.Color.X;
                _cityColorBuf[count * 3 + 1] = c.Color.Y;
                _cityColorBuf[count * 3 + 2] = c.Color.Z;

                count++;
            }

            _cityPackedCount = count;
            _lastFrameDrawn = count;
            _lastFrameCulledFrustum = culledFrustum;
            _lastFrameCulledOcclusion = culledOcclusion;
            _lastFrameCulledDistance = culledDistance;
        }

        private int _cityPackedCount = 0;

        public CubeCityInstanceGroup? BuildCubeCityFrameData()
        {
            if (_cityPackedCount == 0) return null;

            return new CubeCityInstanceGroup
            {
                MeshId = "FBXCube",
                Count = _cityPackedCount,
                Positions = _cityPositionBuf[..(_cityPackedCount * 3)],
                Scales = _cityScaleBuf[.._cityPackedCount],
                Rotations = _cityRotBuf[.._cityPackedCount],
                Colors = _cityColorBuf[..(_cityPackedCount * 3)],
                Phase = (int)_cityPhase,
            };
        }
    }
}