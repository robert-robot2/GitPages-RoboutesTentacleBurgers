using System;
using System.Collections.Generic;
using System.Text;
using static SpectralXGLX.BWP.SpectralWaveSys;

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        public void InitScene9()
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
            Weather.Init(Scene9, MeshLibrary, new Dictionary<WeatherParticleType, ParticleVolume>
    {
        { WeatherParticleType.Rain,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
        { WeatherParticleType.Snow,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
        { WeatherParticleType.Cloud,     new ParticleVolume(-64f, 64f, -64f, 64f, 10f, 30f) },
        { WeatherParticleType.Lightning, new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
    });

            // ── Lighting ─────────────────────────────────────────
            var light1 = new SpectralXLight(
                position: new Vector3(0, 0, 2),
                color: new Vector3(1f, 1f, 1f),
                intensity: 2.0f,
                range: 2f);
            light1.CastsShadows = false;
            Scene9.AddLight(light1);

            var light2 = new SpectralXLight(
                position: new Vector3(-5, 5, 4),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 5.0f,
                range: 8f);
            light2.CastsShadows = false;
            Scene9.AddLight(light2);

            var light3 = new SpectralXLight(
                position: new Vector3(5, 5, 4),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 5.0f,
                range: 8f);
            light3.CastsShadows = false;
            Scene9.AddLight(light3);

            // ── Skysphere ─────────────────────────────────────────
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
            Scene9.AddMesh(skySphere3);

            // ── Sun Directional Light ─────────────────────────────
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
            Scene9.AddLight(_sunLight);

            // ── Tile Map ─────────────────────────────────────────
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

            TileMap.CustomNormalMapPaths = new string?[6];
            TileMap.CustomSpecularMapPaths = new string?[6];
            TileMap.CustomRoughnessMapPaths = new string?[6];
            TileMap.CustomMetallicMapPaths = new string?[6];
            TileMap.CustomAOMapPaths = new string?[6];
            TileMap.CustomEmissiveMapPaths = new string?[6];
            TileMap.CustomDisplacementMapPaths = new string?[6];

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

            OrthoCamera.Reset(0f, 0f, 15f);
            OrthoCamera.LockToPlayer = false;

            // ── BWPScene6 (Forest 5) ─────────────────────────────────────────────
            AddSignpost(Scene9, "Scene9_SignpostToForest4",
                GetTriggerPos(SceneID.BWPScene6, SceneID.BWPScene5));
            AddSignpost(Scene9, "Scene9_SignpostToForest6",
                GetTriggerPos(SceneID.BWPScene6, SceneID.BWPScene7));

            StaticProps = new SpectralProps();
            StaticProps.SpawnScattered(
                MeshLibrary, Scene9,
                SpectralPropDefs.DarkForestDictionary(),
                SpectralPropDefs.All,
                spawnRadius: 64f, clearRadius: 3f, seed: 42);

            SpectralDynManager.SpawnAll(Scene9, MeshLibrary, spawnRadius: 60f, seed: 42);

            SpectralUndeadGFRegistry.Spawn(
                Scene9, MeshLibrary,
                count: 6,
                originX: 0f, originY: 0f, originZ: 0.1f,
                radius: 12f,
                seed: 99);

            WaveSystem.SetSpawnOrigin(0f, 0f, 1.0f);
            WaveSystem.SetSpawnCallback(SpawnSkeleton, DespawnSkeleton, SpawnPsychoSkeleton, DespawnPsychoSkeleton);
            WaveSystem.SetSpawnCallback(SpawnZombiePsycho, DespawnZombiePsycho, SpawnSkeletonWar, DespawnSkeletonWar,
                SpawnGoatman, DespawnGoatman, SpawnScavBoss, DespawnScavBoss, SpawnSkeletonBoss, DespawnSkeletonBoss);
            WaveSystem.RegisterSceneWaves(SceneID.BWPScene6, new WaveDefinition[]
            {
    // Wave 1
    new WaveDefinition { Skeletons = new[]{2}, PsychoSkeletons = new[]{2}, ZombiePsycho = new[]{1}, Goatman = new[]{0}, SkeletonWar = new[]{1}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0}, OnWaveStart = () => Console.WriteLine("[Scene9] Wave 1 started!") },
    // Wave 2
    new WaveDefinition { Skeletons = new[]{2}, PsychoSkeletons = new[]{2}, ZombiePsycho = new[]{2}, Goatman = new[]{0}, SkeletonWar = new[]{2}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 3
    new WaveDefinition { Skeletons = new[]{3}, PsychoSkeletons = new[]{2}, ZombiePsycho = new[]{2}, Goatman = new[]{1}, SkeletonWar = new[]{2}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 4
    new WaveDefinition { Skeletons = new[]{3}, PsychoSkeletons = new[]{3}, ZombiePsycho = new[]{3}, Goatman = new[]{1}, SkeletonWar = new[]{3}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 5
    new WaveDefinition { Skeletons = new[]{2}, PsychoSkeletons = new[]{3}, ZombiePsycho = new[]{3}, Goatman = new[]{2}, SkeletonWar = new[]{3}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 6
    new WaveDefinition { Skeletons = new[]{2}, PsychoSkeletons = new[]{3}, ZombiePsycho = new[]{3}, Goatman = new[]{2}, SkeletonWar = new[]{4}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 7
    new WaveDefinition { Skeletons = new[]{1}, PsychoSkeletons = new[]{2}, ZombiePsycho = new[]{4}, Goatman = new[]{3}, SkeletonWar = new[]{4}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 8
    new WaveDefinition { Skeletons = new[]{1}, PsychoSkeletons = new[]{2}, ZombiePsycho = new[]{4}, Goatman = new[]{3}, SkeletonWar = new[]{5}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 9
    new WaveDefinition { Skeletons = new[]{0}, PsychoSkeletons = new[]{1}, ZombiePsycho = new[]{5}, Goatman = new[]{4}, SkeletonWar = new[]{5}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
    // Wave 10
    new WaveDefinition { Skeletons = new[]{0}, PsychoSkeletons = new[]{1}, ZombiePsycho = new[]{5}, Goatman = new[]{4}, SkeletonWar = new[]{6}, ScavBoss = new[]{0}, SkeletonBoss = new[]{0} },
            });

            WaveSystem.SetScene(SceneID.BWPScene6);
            WaveSystem.LoadWave(1);

      

            Console.WriteLine("[SpectralXEngine] Skeleton spawned from menu");
            Console.WriteLine("[InitScene9] Scene9 ready");
        }



    }
}
