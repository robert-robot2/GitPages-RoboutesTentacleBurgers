

using static SpectralXGLX.BWP.SpectralWaveSys;

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        // ── Static Object Set ─────────────────────────────────────
        public SpectralXBWPStaticObjects? StaticObjects { get; private set; }

        public void InitScene4()
        {
            Shadow = new SpectralXShadow();
            Shadow.SoftnessBias = 0.008f;
            Shadow.KernelSize = 3.0f;
            Shadow.DepthBias = 0.003f;
            Shadow.ContactSharpness = 0.0005f;
            Shadow.TintR = 0.2f;
            Shadow.TintStrength = 0.3f;
            Shadow.PenumbraTintStrength = 0.4f;


            Weather = new SpectralXWeatherClass();
            Weather.Init(Scene4, MeshLibrary, new Dictionary<WeatherParticleType, ParticleVolume>
{
    { WeatherParticleType.Rain,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
    { WeatherParticleType.Snow,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
    { WeatherParticleType.Cloud,     new ParticleVolume(-64f, 64f, -64f, 64f, 10f, 30f) },
    { WeatherParticleType.Lightning, new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
});

            // the third point light is not casting on landscape, second is not casting correct color onto landscape

            // ── Scene 4 Lighting ────────────────────────────────────────────────────────

            // White point light
            var scene4PointL1 = new SpectralXLight(
                position: new Vector3(0, 0, 2),
                color: new Vector3(1f, 1f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene4PointL1.CastsShadows = false;
            Scene4.AddLight(scene4PointL1);

            // Blue point light
            var scene4PointL2 = new SpectralXLight(
                position: new Vector3(-5, 5, 4),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene4PointL2.CastsShadows = false;
            Scene4.AddLight(scene4PointL2);

            // Purple point light
            var scene4PointL3 = new SpectralXLight(
                position: new Vector3(5, 5, 4),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene4PointL3.CastsShadows = false;
            Scene4.AddLight(scene4PointL3);

         
          
            // ── Scene 4 Light Gizmos ─────────────────────────────────────────────────────

            // White point L1
            var scene4L1Gizmo = CreateGizmoFrom("S4_LightGizmo_L1", "LightBulb");
            scene4L1Gizmo.Position = scene4PointL1.Position;
            scene4L1Gizmo.Size = new Vector3(0.3f, 0.3f, 0.3f);
            scene4L1Gizmo.Color = new Vector4(1f, 0.98f, 0.85f, 0.4f);
            scene4L1Gizmo.IsEmissive = true;
            scene4L1Gizmo.CastsShadow = false;
            scene4L1Gizmo.ReceivesShadow = false;
            scene4L1Gizmo.EmissiveIntensity = 0.8f;
            Scene4.AddMesh(scene4L1Gizmo);

            var scene4L1Core = CreateGizmoFrom("S4_LightCore_L1", "SmoothSphere");
            scene4L1Core.Position = scene4PointL1.Position;
            scene4L1Core.Size = new Vector3(0.08f, 0.08f, 0.08f);
            scene4L1Core.Color = new Vector4(1f, 0.95f, 0.6f, 1f);
            scene4L1Core.IsEmissive = true;
            scene4L1Core.CastsShadow = false;
            scene4L1Core.ReceivesShadow = false;
            scene4L1Core.EmissiveIntensity = 3.0f;
            Scene4.AddMesh(scene4L1Core);

            var scene4L1AuraInner = CreateGizmoFrom("S4_LightAuraInner_L1", "SmoothSphere");
            scene4L1AuraInner.Position = scene4PointL1.Position;
            scene4L1AuraInner.Size = new Vector3(0.35f, 0.35f, 0.35f);
            scene4L1AuraInner.Color = new Vector4(1f, 0.85f, 0.4f, 0.12f);
            scene4L1AuraInner.IsEmissive = true;
            scene4L1AuraInner.CastsShadow = false;
            scene4L1AuraInner.ReceivesShadow = false;
            scene4L1AuraInner.EmissiveIntensity = 1.2f;
            Scene4.AddMesh(scene4L1AuraInner);

            var scene4L1AuraOuter = CreateGizmoFrom("S4_LightAuraOuter_L1", "SmoothSphere");
            scene4L1AuraOuter.Position = scene4PointL1.Position;
            scene4L1AuraOuter.Size = new Vector3(0.6f, 0.6f, 0.6f);
            scene4L1AuraOuter.Color = new Vector4(1f, 0.75f, 0.3f, 0.05f);
            scene4L1AuraOuter.IsEmissive = true;
            scene4L1AuraOuter.CastsShadow = false;
            scene4L1AuraOuter.ReceivesShadow = false;
            scene4L1AuraOuter.EmissiveIntensity = 0.6f;
            Scene4.AddMesh(scene4L1AuraOuter);

            // Blue point L2
            var scene4L2Gizmo = CreateGizmoFrom("S4_LightGizmo_L2", "SmoothSphere");
            scene4L2Gizmo.Position = scene4PointL2.Position;
            scene4L2Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene4L2Gizmo.Color = new Vector4(0f, 0.4f, 1f, 1f);
            scene4L2Gizmo.IsEmissive = true;
            scene4L2Gizmo.CastsShadow = false;
            scene4L2Gizmo.ReceivesShadow = false;
            scene4L2Gizmo.EmissiveIntensity = 2.0f;
            Scene4.AddMesh(scene4L2Gizmo);

            var scene4L2Aura = CreateGizmoFrom("S4_LightAura_L2", "SmoothSphere");
            scene4L2Aura.Position = scene4PointL2.Position;
            scene4L2Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene4L2Aura.Color = new Vector4(0f, 0.4f, 1f, 0.08f);
            scene4L2Aura.IsEmissive = true;
            scene4L2Aura.CastsShadow = false;
            scene4L2Aura.ReceivesShadow = false;
            scene4L2Aura.EmissiveIntensity = 0.8f;
            Scene4.AddMesh(scene4L2Aura);

            // Purple point L3
            var scene4L3Gizmo = CreateGizmoFrom("S4_LightGizmo_L3", "SmoothSphere");
            scene4L3Gizmo.Position = scene4PointL3.Position;
            scene4L3Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene4L3Gizmo.Color = new Vector4(0.6f, 0f, 1f, 1f);
            scene4L3Gizmo.IsEmissive = true;
            scene4L3Gizmo.CastsShadow = false;
            scene4L3Gizmo.ReceivesShadow = false;
            scene4L3Gizmo.EmissiveIntensity = 2.0f;
            Scene4.AddMesh(scene4L3Gizmo);

            var scene4L3Aura = CreateGizmoFrom("S4_LightAura_L3", "SmoothSphere");
            scene4L3Aura.Position = scene4PointL3.Position;
            scene4L3Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene4L3Aura.Color = new Vector4(0.6f, 0f, 1f, 0.08f);
            scene4L3Aura.IsEmissive = true;
            scene4L3Aura.CastsShadow = false;
            scene4L3Aura.ReceivesShadow = false;
            scene4L3Aura.EmissiveIntensity = 0.8f;
            Scene4.AddMesh(scene4L3Aura);








            // ── Skysphere ────────────────────────────────────────────────────────
            // Large inverted sphere, always camera-centered, rendered emissive
            // so it skips all lighting calculations.
            // MaterialTextures[0] = day panorama, MaterialTextures[1] = night panorama
            var skySphere3 = CreateGizmoFrom("SkySphere", "FBXCube");
            skySphere3.Name = "SkySphere";
            skySphere3.Position = new Vector3(Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
            skySphere3.Size = new Vector3(120f, 120f, 120f);
            skySphere3.Color = new Vector4(1f, 1f, 1f, 1f);
            skySphere3.IsEmissive = true;
            skySphere3.EmissiveIntensity = 1.0f;
            skySphere3.MaterialTextures.Add("/iAssets/SkyCubeMap012.png");
            skySphere3.MaterialTextures.Add("/iAssets/StarsCubeMap015.png");
            skySphere3.Rotation = new Vector3(0f, 0f, 0f);
            Scene4.AddMesh(skySphere3);


            // Sun directional light — added last
            _sunLight = new SpectralXLight(
                position: new Vector3(0f, -15f, 20f),
                color: new Vector3(1f, 0.98f, 0.90f),
                intensity: 5.0f,
                range: 200f);
            _sunLight.Type = LightType.Directional;
            _sunLight.Direction = new Vector3(0f, -0.5f, -1f);
            _sunLight.CastsShadows = true;
            _sunLight.Enabled = true;
            Sun.Apply(_sunLight);
            Scene4.AddLight(_sunLight);


            // ── Tile Map ─────────────────────────────────────────────────────────
            TileMap = new SpectralXLandTileMap();
            TileMap.SetGridSize(128);
            TileMap.CustomTexturePaths = new[]
            {
    "/iAssets/DirtTile010.png",
    "/iAssets/RockTile010.png",
    "/iAssets/GrassTile010.png",
    "/iAssets/SnowTile010.png",
    "/iAssets/WaterTile010.png",
    "/iAssets/IceTile010.png",
};

            // ── Normal Maps ───────────────────────────────────────────────────────
            TileMap.CustomNormalMapPaths = new string?[6]; // all null — add paths when ready
                                                           // ── Specular Maps ─────────────────────────────────────────────────────
            TileMap.CustomSpecularMapPaths = new string?[6]; // all null — add paths when ready
                                                             // ── Roughness Maps ────────────────────────────────────────────────────
            TileMap.CustomRoughnessMapPaths = new string?[6]; // all null — add paths when ready
                                                              // ── Metallic Maps ─────────────────────────────────────────────────────
            TileMap.CustomMetallicMapPaths = new string?[6]; // all null — add paths when ready
                                                             // ── AO Maps ───────────────────────────────────────────────────────────
            TileMap.CustomAOMapPaths = new string?[6]; // all null — add paths when ready
                                                       // ── Emissive Maps ─────────────────────────────────────────────────────
            TileMap.CustomEmissiveMapPaths = new string?[6]; // all null — add paths when ready
                                                             // ── Displacement Maps ─────────────────────────────────────────────────
            TileMap.CustomDisplacementMapPaths = new string?[6]; // all null — add paths when ready

            // ── PBR Scalar Parameters ─────────────────────────────────────────────
            //                                   Dirt   Rock   Grass  Snow   Water  Ice
            TileMap.RoughnessValues = new[] { 0.9f, 0.8f, 0.85f, 0.4f, 0.1f, 0.2f };
            TileMap.MetallicValues = new[] { 0.0f, 0.0f, 0.0f, 0.3f, 0.0f, 0.4f };
            TileMap.AOValues = new[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
            TileMap.SpecularValues = new[] { 0.3f, 0.4f, 0.2f, 0.6f, 0.8f, 0.9f };
            TileMap.EmissiveIntensityValues = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            TileMap.DisplacementStrengthValues = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            TileMap.ParallaxScaleValues = new[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };
            TileMap.Init();
            _tileMapTexturesUploaded = false;
            _tilePBRUploaded = false;   
            _ = LoadLandscape();

            // ── Camera ───────────────────────────────────────────
            Camera.Position = new   CustomVec3(0, -10, 4);

            OrthoCamera.Reset(0f, 0f, 10f);
            OrthoCamera.LockToPlayer = false;


            // ── Signpost — visual marker for the Scene4 -> Scene5 transition trigger ──
            AddSignpost(Scene4, "Scene4_SignpostToTown",
                GetTriggerPos(SceneID.BWPScene1, SceneID.BWPScene2));

            AddSignpost(Scene4, "Scene4_SignpostToForest2",
                GetTriggerPos(SceneID.BWPScene1, SceneID.BWPScene3));

            AddSignpost(Scene4, "Scene4_SignpostToCaves",
                GetTriggerPos(SceneID.BWPScene1, SceneID.BWPScene10));


            StaticProps = new SpectralProps();
            StaticProps.SpawnScattered(
                MeshLibrary, Scene4,
                SpectralPropDefs.ForestDictionary(),
                SpectralPropDefs.All,
                spawnRadius: 64f, clearRadius: 3f, seed: 42);


            SpectralDynManager.SpawnAll(Scene4, MeshLibrary, spawnRadius: 60f, seed: 42);
            SpectralBreakManager.SpawnAll(Scene4, MeshLibrary, spawnRadius: 12f, seed: 99);

            SpectralUndeadGFRegistry.Spawn(
    Scene4, MeshLibrary,
    count: 6,
    originX: 0f, originY: 0f, originZ: 0.1f,
    radius: 12f,
    seed: 99);




            WaveSystem.SetSpawnOrigin(0f, 0f, 1.0f);
            WaveSystem.SetSpawnCallback(SpawnSkeleton, DespawnSkeleton, SpawnPsychoSkeleton, DespawnPsychoSkeleton);
            WaveSystem.SetSpawnCallback(SpawnZombiePsycho, DespawnZombiePsycho, SpawnSkeletonWar, DespawnSkeletonWar,
                SpawnGoatman, DespawnGoatman, SpawnScavBoss, DespawnScavBoss, SpawnSkeletonBoss, DespawnSkeletonBoss);

            // Register FIRST
            WaveSystem.RegisterSceneWaves(SceneID.BWPScene1, new WaveDefinition[]
         {
    // Wave 1
    new WaveDefinition { Skeletons = new[]{2}, PsychoSkeletons = new[]{0}, ZombiePsycho = new[]{2}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0}, OnWaveStart = () => Console.WriteLine("[Scene4] Wave 1 started!") },
    // Wave 2
    new WaveDefinition { Skeletons = new[]{3}, PsychoSkeletons = new[]{1}, ZombiePsycho = new[]{0}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 3
    new WaveDefinition { Skeletons = new[]{4}, PsychoSkeletons = new[]{3}, ZombiePsycho = new[]{1}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 4
    new WaveDefinition { Skeletons = new[]{4}, PsychoSkeletons = new[]{3}, ZombiePsycho = new[]{2}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 5
    new WaveDefinition { Skeletons = new[]{3}, PsychoSkeletons = new[]{3}, ZombiePsycho = new[]{3}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 6
    new WaveDefinition { Skeletons = new[]{2}, PsychoSkeletons = new[]{2}, ZombiePsycho = new[]{0}, Goatman = new[]{0}, SkeletonWar = new[]{1}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 7
    new WaveDefinition { Skeletons = new[]{1}, PsychoSkeletons = new[]{1}, ZombiePsycho = new[]{0}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 8
    new WaveDefinition { Skeletons = new[]{0}, PsychoSkeletons = new[]{0}, ZombiePsycho = new[]{0}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 9
    new WaveDefinition { Skeletons = new[]{0}, PsychoSkeletons = new[]{0}, ZombiePsycho = new[]{0}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 10
    new WaveDefinition { Skeletons = new[]{0}, PsychoSkeletons = new[]{0}, ZombiePsycho = new[]{0}, Goatman = new[]{0}, SkeletonWar = new[]{0}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
         });

            // SetScene SECOND — populates _waveDefs
            WaveSystem.SetScene(SceneID.BWPScene1);

            // LoadWave LAST — now _waveDefs is populated so it uses scene defs not static arrays
            WaveSystem.LoadWave(1);

            // ── Ambient/Trap NPCs — spawned directly, not part of WaveSystem ──
            for (int i = 0; i < 3; i++)
            {
                float cx, cy;
                do { (cx, cy) = RandomScatterPoint(0f, 0f, 12f); }
                while (cx * cx + cy * cy < 25f); // keep retrying if inside radius 5 of spawn
                Cows.Add(SpawnCow(Scene4, cx, cy, 1.0f));
            }
            for (int i = 0; i < 3; i++)
            {
                float catx, caty;
                do { (catx, caty) = RandomScatterPoint(0f, 0f, 12f); }
                while (catx * catx + caty * caty < 25f); // keep outside radius 5 of player spawn
                Cats.Add(SpawnCat(Scene4, catx, caty, 1.0f));
            }
            for (int i = 0; i < 3; i++)
            {
                float tsx, tsy;
                do { (tsx, tsy) = RandomScatterPoint(0f, 0f, 12f); }
                while (tsx * tsx + tsy * tsy < 25f);
                TownSluts.Add(SpawnTownSlut(Scene4, tsx, tsy, 1.0f));
            }
            Console.WriteLine("[SpectralXEngine] Skeleton spawned from menu");
          //  Console.WriteLine($"[InitScene4] Static objects spawned: {StaticObjects.Spawned.Count}");
            Console.WriteLine("[InitScene4] Scene4 ready");
        }


        private static readonly Random _scatterRng = new();
        private (float x, float y) RandomScatterPoint(float originX, float originY, float radius)
        {
            double angle = _scatterRng.NextDouble() * Math.PI * 2.0;
            double dist = Math.Sqrt(_scatterRng.NextDouble()) * radius;
            float x = originX + (float)(Math.Cos(angle) * dist);
            float y = originY + (float)(Math.Sin(angle) * dist);
            return (x, y);
        }
        private void ClearExpiredAmbientCorpses()
        {
            for (int i = Cows.Count - 1; i >= 0; i--)
            {
                if (Cows[i].IsCorpseExpired)
                {
                    DespawnCow(Cows[i]);
                    Cows.RemoveAt(i);
                }
            }

            for (int i = Cats.Count - 1; i >= 0; i--)
            {
                if (Cats[i].IsCorpseExpired)
                {
                    DespawnCat(Cats[i]);
                    Cats.RemoveAt(i);
                }
            }

            for (int i = TownSluts.Count - 1; i >= 0; i--)
            {
                if (TownSluts[i].IsCorpseExpired)
                {
                    DespawnTownSlut(TownSluts[i]);
                    TownSluts.RemoveAt(i);
                }
            }
        }

        private void TickAmbientTraps(ISpectralCharacter target, float delta)
        {
            foreach (var cow in Cows)
            {
                if (cow.EnemyIsAlive)
                    cow.EnemyMove(target);

                cow.Tick(delta);
            }

            foreach (var cat in Cats)
            {
                if (cat.EnemyIsAlive)
                    cat.EnemyMove(target);

                cat.Tick(delta);
            }

            foreach (var townSlut in TownSluts)
            {
                if (townSlut.EnemyIsAlive)
                    townSlut.EnemyMove(target);

                townSlut.Tick(delta);
            }

            ClearExpiredAmbientCorpses();
        }
        public void SpawnWarrior()
        {
            if (Warrior != null) return; // already spawned
            var warriorMesh = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (warriorMesh == null) return;

            warriorMesh.Name = "WarriorSquare";
            warriorMesh.Position = new Vector3(0f, 0f, 0.1f);
            warriorMesh.CastsShadow = false;

            var activeScene = GetActiveScene();          // ← use whichever scene is actually active
            Warrior = new SpectralXBloodWarrior();
            Warrior.InitMesh(warriorMesh, activeScene, MeshLibrary);
            activeScene.AddMesh(warriorMesh);
            Console.WriteLine("[SpectralXEngine] Warrior spawned from menu");
        }

        public void SpawnRogue()
        {
            if (Rogue != null) return;
            var mesh = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (mesh == null) return;
            mesh.Name = "RogueSquare";
            mesh.Position = new Vector3(0f, 0f, 0.1f);
            mesh.CastsShadow = false;

            var activeScene = GetActiveScene();
            Rogue = new SpectralXRogue();
            Rogue.InitMesh(mesh, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh);
            Console.WriteLine("[SpectralXEngine] Rogue spawned");
        }

        public void SpawnMonk()
        {
            if (Monk != null) return;
            var mesh = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (mesh == null) return;
            mesh.Name = "MonkSquare";
            mesh.Position = new Vector3(0f, 0f, 0.1f);
            mesh.CastsShadow = false;

            var activeScene = GetActiveScene();
            Monk = new SpectralXMonk();
            Monk.InitMesh(mesh, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh);
            Console.WriteLine("[SpectralXEngine] Monk spawned");
        }

        public void SpawnMage()
        {
            if (Mage != null) return;
            var mesh = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            if (mesh == null) return;
            mesh.Name = "MageSquare";
            mesh.Position = new Vector3(0f, 0f, 0.1f);
            mesh.CastsShadow = false;

            var activeScene = GetActiveScene();
            Mage = new SpectralXMage();
            Mage.InitMesh(mesh, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh);
            Console.WriteLine("[SpectralXEngine] Mage spawned");
        }

        private int _skeletonSpawnCounter = 0;

        public SpectralXSkeleton SpawnSkeleton(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var skeleton = new SpectralXSkeleton();
            if (prim == null) return skeleton;
            var activeScene = GetActiveScene();
            var meshName = $"SkeletonSquare_{_skeletonSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;
            skeleton.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh); // was Scene4.AddMesh
            return skeleton;
        }

        public void DespawnSkeleton(SpectralXSkeleton skeleton)
        {
            if (skeleton.EnemyMesh != null)
                GetActiveScene().RemoveMesh(skeleton.EnemyMesh);
        }
        private int _psychoSkeletonSpawnCounter = 0;

        public SpectralXPsychoSkeleton SpawnPsychoSkeleton(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var psycho = new SpectralXPsychoSkeleton();
            if (prim == null) return psycho;
            var activeScene = GetActiveScene();
            var meshName = $"PsychoSkeletonSquare_{_psychoSkeletonSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);

            // Copy PrimSquare geometry
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            psycho.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh); // was Scene4.AddMesh

            return psycho;
        }

        public void DespawnPsychoSkeleton(SpectralXPsychoSkeleton psycho)
        {
            if (psycho.EnemyMesh != null)
                GetActiveScene().RemoveMesh(psycho.EnemyMesh);
        }

        private int _zombiePsychoSpawnCounter = 0;

        public SpectralXZombiePsycho SpawnZombiePsycho(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var zombiePsycho = new SpectralXZombiePsycho();
            if (prim == null) return zombiePsycho;
            var activeScene = GetActiveScene();
            var meshName = $"ZombiePsychoSquare_{_zombiePsychoSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            zombiePsycho.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh); // was Scene4.AddMesh
            return zombiePsycho;
        }

        public void DespawnZombiePsycho(SpectralXZombiePsycho zombiePsycho)
        {
            if (zombiePsycho.EnemyMesh != null)
                GetActiveScene().RemoveMesh(zombiePsycho.EnemyMesh);
        }

        private int _skeletonWarSpawnCounter = 0;

        public SpectralXSkeletonWar SpawnSkeletonWar(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var skeletonWar = new SpectralXSkeletonWar();
            if (prim == null) return skeletonWar;
            var activeScene = GetActiveScene();
            var meshName = $"SkeletonWarSquare_{_skeletonWarSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            skeletonWar.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh); // was Scene4.AddMesh
            return skeletonWar;
        }
        public void DespawnSkeletonWar(SpectralXSkeletonWar skeletonWar)
        {
            if (skeletonWar.EnemyMesh != null)
                GetActiveScene().RemoveMesh(skeletonWar.EnemyMesh);
        }

        private int _goatmanSpawnCounter = 0;

        public SpectralXGoatman SpawnGoatman(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var goatman = new SpectralXGoatman();
            if (prim == null) return goatman;
            var activeScene = GetActiveScene();
            var meshName = $"GoatmanSquare_{_goatmanSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            goatman.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh); // was Scene4.AddMesh
            return goatman;
        }

        public void DespawnGoatman(SpectralXGoatman goatman)
        {
            if (goatman.EnemyMesh != null)
                GetActiveScene().RemoveMesh(goatman.EnemyMesh);
        }

        private int _scavBossSpawnCounter = 0;

        public SpectralXScavBoss SpawnScavBoss(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var scavBoss = new SpectralXScavBoss();
            if (prim == null) return scavBoss;
            var activeScene = GetActiveScene();
            var meshName = $"ScavBossSquare_{_scavBossSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            scavBoss.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh); // was Scene4.AddMesh
            return scavBoss;
        }

        public void DespawnScavBoss(SpectralXScavBoss scavBoss)
        {
            if (scavBoss.EnemyMesh != null)
                GetActiveScene().RemoveMesh(scavBoss.EnemyMesh);
        }

        private int _skeletonBossSpawnCounter = 0;

        public SpectralXSkeletonBoss SpawnSkeletonBoss(float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var skeletonBoss = new SpectralXSkeletonBoss();
            if (prim == null) return skeletonBoss;

            var activeScene = GetActiveScene();
            var meshName = $"SkeletonBossSquare_{_skeletonBossSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            skeletonBoss.InitMesh(mesh, x, y, z, activeScene, MeshLibrary);
            activeScene.AddMesh(mesh);
            return skeletonBoss;
        }

        public void DespawnSkeletonBoss(SpectralXSkeletonBoss skeletonBoss)
        {
            if (skeletonBoss.EnemyMesh != null)
                GetActiveScene().RemoveMesh(skeletonBoss.EnemyMesh);
        }

        private int _cowSpawnCounter = 0;

        public SpectralXCow SpawnCow(SpectralXScene scene, float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var cow = new SpectralXCow();
            if (prim == null) return cow;

            var meshName = $"CowSquare_{_cowSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;

            cow.InitMesh(mesh, x, y, z, scene, MeshLibrary);
            scene.AddMesh(mesh);
            return cow;
        }

        public void DespawnCow(SpectralXCow cow)
        {
            if (cow.EnemyMesh != null)
                GetActiveScene().RemoveMesh(cow.EnemyMesh);
        }

        private int _catSpawnCounter = 0;

        public SpectralXCat SpawnCat(SpectralXScene scene, float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var cat = new SpectralXCat();
            if (prim == null) return cat;
            var meshName = $"CatSquare_{_catSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);
            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;
            cat.InitMesh(mesh, x, y, z, scene, MeshLibrary);
            scene.AddMesh(mesh);
            return cat;
        }

        public void DespawnCat(SpectralXCat cat)
        {
            if (cat.EnemyMesh != null)
                Scene4.RemoveMesh(cat.EnemyMesh);
        }

        private int _townSlutSpawnCounter = 0;

        public SpectralXTownSlut SpawnTownSlut(SpectralXScene scene, float x, float y, float z)
        {
            var prim = MeshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
            var townSlut = new SpectralXTownSlut();
            if (prim == null) return townSlut;
            var meshName = $"TownSlutSquare_{_townSlutSpawnCounter++}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);
            mesh.Position = new Vector3(x, y, z);
            mesh.CastsShadow = false;
            townSlut.InitMesh(mesh, x, y, z, scene, MeshLibrary);
            scene.AddMesh(mesh);
            return townSlut;
        }

        public void DespawnTownSlut(SpectralXTownSlut townSlut)
        {
            if (townSlut.EnemyMesh != null)
                Scene4.RemoveMesh(townSlut.EnemyMesh);
        }











    }
}