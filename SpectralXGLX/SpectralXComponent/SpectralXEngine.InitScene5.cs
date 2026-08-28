namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        public void InitScene5()
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
            Weather.Init(Scene5, MeshLibrary, new Dictionary<WeatherParticleType, ParticleVolume>
            {
                { WeatherParticleType.Rain,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
                { WeatherParticleType.Snow,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
                { WeatherParticleType.Cloud,     new ParticleVolume(-64f, 64f, -64f, 64f, 10f, 30f) },
                { WeatherParticleType.Lightning, new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
            });

            // ── Scene 5 Lighting ────────────────────────────────────────────────────────

            // White point light
            var scene5PointL1 = new SpectralXLight(
                position: new Vector3(0, 0, 2),
                color: new Vector3(1f, 1f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene5PointL1.CastsShadows = false;
            Scene5.AddLight(scene5PointL1);

            // Blue point light
            var scene5PointL2 = new SpectralXLight(
                position: new Vector3(-5, 5, 4),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene5PointL2.CastsShadows = false;
            Scene5.AddLight(scene5PointL2);

            // Purple point light
            var scene5PointL3 = new SpectralXLight(
                position: new Vector3(5, 5, 4),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 5.0f,
                range: 8f);
            scene5PointL3.CastsShadows = false;
            Scene5.AddLight(scene5PointL3);

           

            // ── Scene 5 Light Gizmos ─────────────────────────────────────────────────────

            // White point L1
            var scene5L1Gizmo = CreateGizmoFrom("S5_LightGizmo_L1", "LightBulb");
            scene5L1Gizmo.Position = scene5PointL1.Position;
            scene5L1Gizmo.Size = new Vector3(0.3f, 0.3f, 0.3f);
            scene5L1Gizmo.Color = new Vector4(1f, 0.98f, 0.85f, 0.4f);
            scene5L1Gizmo.IsEmissive = true;
            scene5L1Gizmo.CastsShadow = false;
            scene5L1Gizmo.ReceivesShadow = false;
            scene5L1Gizmo.EmissiveIntensity = 0.8f;
            Scene5.AddMesh(scene5L1Gizmo);

            var scene5L1Core = CreateGizmoFrom("S5_LightCore_L1", "SmoothSphere");
            scene5L1Core.Position = scene5PointL1.Position;
            scene5L1Core.Size = new Vector3(0.08f, 0.08f, 0.08f);
            scene5L1Core.Color = new Vector4(1f, 0.95f, 0.6f, 1f);
            scene5L1Core.IsEmissive = true;
            scene5L1Core.CastsShadow = false;
            scene5L1Core.ReceivesShadow = false;
            scene5L1Core.EmissiveIntensity = 3.0f;
            Scene5.AddMesh(scene5L1Core);

            var scene5L1AuraInner = CreateGizmoFrom("S5_LightAuraInner_L1", "SmoothSphere");
            scene5L1AuraInner.Position = scene5PointL1.Position;
            scene5L1AuraInner.Size = new Vector3(0.35f, 0.35f, 0.35f);
            scene5L1AuraInner.Color = new Vector4(1f, 0.85f, 0.4f, 0.12f);
            scene5L1AuraInner.IsEmissive = true;
            scene5L1AuraInner.CastsShadow = false;
            scene5L1AuraInner.ReceivesShadow = false;
            scene5L1AuraInner.EmissiveIntensity = 1.2f;
            Scene5.AddMesh(scene5L1AuraInner);

            var scene5L1AuraOuter = CreateGizmoFrom("S5_LightAuraOuter_L1", "SmoothSphere");
            scene5L1AuraOuter.Position = scene5PointL1.Position;
            scene5L1AuraOuter.Size = new Vector3(0.6f, 0.6f, 0.6f);
            scene5L1AuraOuter.Color = new Vector4(1f, 0.75f, 0.3f, 0.05f);
            scene5L1AuraOuter.IsEmissive = true;
            scene5L1AuraOuter.CastsShadow = false;
            scene5L1AuraOuter.ReceivesShadow = false;
            scene5L1AuraOuter.EmissiveIntensity = 0.6f;
            Scene5.AddMesh(scene5L1AuraOuter);

            // Blue point L2
            var scene5L2Gizmo = CreateGizmoFrom("S5_LightGizmo_L2", "SmoothSphere");
            scene5L2Gizmo.Position = scene5PointL2.Position;
            scene5L2Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene5L2Gizmo.Color = new Vector4(0f, 0.4f, 1f, 1f);
            scene5L2Gizmo.IsEmissive = true;
            scene5L2Gizmo.CastsShadow = false;
            scene5L2Gizmo.ReceivesShadow = false;
            scene5L2Gizmo.EmissiveIntensity = 2.0f;
            Scene5.AddMesh(scene5L2Gizmo);

            var scene5L2Aura = CreateGizmoFrom("S5_LightAura_L2", "SmoothSphere");
            scene5L2Aura.Position = scene5PointL2.Position;
            scene5L2Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene5L2Aura.Color = new Vector4(0f, 0.4f, 1f, 0.08f);
            scene5L2Aura.IsEmissive = true;
            scene5L2Aura.CastsShadow = false;
            scene5L2Aura.ReceivesShadow = false;
            scene5L2Aura.EmissiveIntensity = 0.8f;
            Scene5.AddMesh(scene5L2Aura);

            // Purple point L3
            var scene5L3Gizmo = CreateGizmoFrom("S5_LightGizmo_L3", "SmoothSphere");
            scene5L3Gizmo.Position = scene5PointL3.Position;
            scene5L3Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene5L3Gizmo.Color = new Vector4(0.6f, 0f, 1f, 1f);
            scene5L3Gizmo.IsEmissive = true;
            scene5L3Gizmo.CastsShadow = false;
            scene5L3Gizmo.ReceivesShadow = false;
            scene5L3Gizmo.EmissiveIntensity = 2.0f;
            Scene5.AddMesh(scene5L3Gizmo);

            var scene5L3Aura = CreateGizmoFrom("S5_LightAura_L3", "SmoothSphere");
            scene5L3Aura.Position = scene5PointL3.Position;
            scene5L3Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene5L3Aura.Color = new Vector4(0.6f, 0f, 1f, 0.08f);
            scene5L3Aura.IsEmissive = true;
            scene5L3Aura.CastsShadow = false;
            scene5L3Aura.ReceivesShadow = false;
            scene5L3Aura.EmissiveIntensity = 0.8f;
            Scene5.AddMesh(scene5L3Aura);

            // ── Skysphere ────────────────────────────────────────────────────────
            var skySphere5 = CreateGizmoFrom("SkySphere", "FBXCube");
            skySphere5.Name = "SkySphere";
            skySphere5.Position = new Vector3(Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
            skySphere5.Size = new Vector3(120f, 120f, 120f);
            skySphere5.Color = new Vector4(1f, 1f, 1f, 1f);
            skySphere5.IsEmissive = true;
            skySphere5.EmissiveIntensity = 1.0f;
            skySphere5.MaterialTextures.Add("/iAssets/SkyCubeMap012.png");
            skySphere5.MaterialTextures.Add("/iAssets/StarsCubeMap015.png");
            skySphere5.Rotation = new Vector3(0f, 0f, 0f);
            Scene5.AddMesh(skySphere5);

            // ── Sun Directional Light ────────────────────────────────────────────
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
            Scene5.AddLight(_sunLight);

            // ── Tile Map ─────────────────────────────────────────────────────────
            TileMap = new SpectralXLandTileMap();
            TileMap.SetGridSize(128);
            TileMap.CustomTexturePaths = new[]
            {
                "/iAssets/DirtTile010.png",
                "/iAssets/RockTile010.png",
                "/iAssets/GrassTownTile010.png",
                "/iAssets/SnowTile010.png",
                "/iAssets/WaterTile010.png",
                "/iAssets/IceTile010.png",
            };

            TileMap.CustomNormalMapPaths = new string?[6];
            TileMap.CustomSpecularMapPaths = new string?[6];
            TileMap.CustomRoughnessMapPaths = new string?[6];
            TileMap.CustomMetallicMapPaths = new string?[6];
            TileMap.CustomAOMapPaths = new string?[6];
            TileMap.CustomEmissiveMapPaths = new string?[6];
            TileMap.CustomDisplacementMapPaths = new string?[6];

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
            Camera.Position = new CustomVec3(0, -10, 4);

            OrthoCamera.Reset(0f, 0f, 10f);
            OrthoCamera.LockToPlayer = false;
            // ── Signpost — visual marker for the Scene4 -> Scene5 transition trigger ──
            // ── Signpost — visual marker for the Scene5 -> Scene4 transition trigger ──
            AddSignpost(Scene5, "Scene5_SignpostToForest",
         GetTriggerPos(SceneID.BWPScene2, SceneID.BWPScene1));
            // ── Static Objects ────────────────────────────────────
            // NOTE: reuses the same StaticObjects field as Scene4 — if Scene4 and
            // Scene5 need DIFFERENT static prop sets, StaticObjects needs to become
            // per-scene (e.g. StaticObjects4 / StaticObjects5) rather than shared.
            // Leaving shared for now since spawn logic overhaul is a separate task.
            /*
            StaticObjects = new SpectralXBWPStaticObjects();
            StaticObjects.SpawnAll(
                meshLibrary: MeshLibrary,
                scene: Scene5,
                spawnRadius: 64f,
                seed: 42
            );
            */
            StaticProps = new SpectralProps();

            StaticProps.SpawnFixed(
                MeshLibrary, Scene5,
                new[]
                {
        new SpectralPropPlacement("Tavern001", 10f, 8f),
        new SpectralPropPlacement("House001", -12f, 6f),
        new SpectralPropPlacement("House001", -12f, -6f),
        new SpectralPropPlacement("Fountain001", 0f, 0f),
                },
                SpectralPropDefs.All);
            var tavernMesh = Scene5.Meshes.OfType<SpectralXMesh>()
    .FirstOrDefault(m => m.Name.StartsWith("Static_Tavern001"));
            if (tavernMesh != null)
                tavernMesh.Position += new Vector3(0f, 0f, 0.1f); // nudge up — tweak until base sits flush
            StaticProps.SpawnScattered(
                MeshLibrary, Scene5,
                SpectralPropDefs.TownDictionary(),
                SpectralPropDefs.All,
                spawnRadius: 64f, clearRadius: 3f, seed: 42);
            SpectralDynManager.SpawnAll(Scene5, MeshLibrary, spawnRadius: 60f, seed: 42);
          //  SpectralBreakManager.SpawnAll(Scene5, MeshLibrary, spawnRadius: 12f, seed: 99);

            // ── NO WAVE ENEMIES IN TOWN ──────────────────────────────────────────
            // Town scenes are safe zones — WaveSystem.SetSpawnOrigin/SetSpawnCallback/
            // LoadWave are intentionally omitted. WaveSystem.TickAll (called from
            // TickAndGetFrame) is harmless to call against an empty/unloaded wave
            // state, so no extra guard is needed there.

            // ── Ambient/Trap NPCs — town keeps these (cows/cats/townsluts) ───────
            for (int i = 0; i < 3; i++)
            {
                var (cx, cy) = RandomScatterPoint(0f, 0f, 12f);
                Cows.Add(SpawnCow(Scene5, cx, cy, 1.0f));
            }
            for (int i = 0; i < 3; i++)
            {
                var (catx, caty) = RandomScatterPoint(0f, 0f, 12f);
    
                Cats.Add(SpawnCat(Scene5, catx, caty, 1.0f));       // InitScene5
               
            }
            for (int i = 0; i < 3; i++)
            {
                var (tsx, tsy) = RandomScatterPoint(0f, 0f, 12f);
                TownSluts.Add(SpawnTownSlut(Scene5, tsx, tsy, 1.0f)); // InitScene5
            }

         //   Console.WriteLine($"[InitScene5] Static objects spawned: {StaticObjects.Spawned.Count}");
            Console.WriteLine("[InitScene5] Scene5 ready");
        }





    }
}