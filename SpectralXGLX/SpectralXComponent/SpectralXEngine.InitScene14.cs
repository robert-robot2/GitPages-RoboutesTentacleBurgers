using System;
using System.Collections.Generic;
using System.Text;

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {
        public void InitScene14()
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
            Weather.Init(Scene14, MeshLibrary, new Dictionary<WeatherParticleType, ParticleVolume>
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
            Scene14.AddLight(light1);

            var light2 = new SpectralXLight(
                position: new Vector3(-5, 5, 4),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 5.0f,
                range: 8f);
            light2.CastsShadows = false;
            Scene14.AddLight(light2);

            var light3 = new SpectralXLight(
                position: new Vector3(5, 5, 4),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 5.0f,
                range: 8f);
            light3.CastsShadows = false;
            Scene14.AddLight(light3);

            // ── Skysphere ─────────────────────────────────────────
            var skySphere14 = CreateGizmoFrom("SkySphere", "FBXCube");
            skySphere14.Name = "SkySphere";
            skySphere14.Position = new Vector3(Camera.Position.X, Camera.Position.Y, Camera.Position.Z);
            skySphere14.Size = new Vector3(120f, 120f, 120f);
            skySphere14.Color = new Vector4(1f, 1f, 1f, 1f);
            skySphere14.IsEmissive = true;
            skySphere14.EmissiveIntensity = 1.0f;
            skySphere14.MaterialTextures.Add("/iAssets/SkyCubeMap012.png");
            skySphere14.MaterialTextures.Add("/iAssets/StarsCubeMap015.png");
            skySphere14.Rotation = new Vector3(0f, 0f, 0f);
            Scene14.AddMesh(skySphere14);

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
            Scene14.AddLight(_sunLight);

            // ── Tile Map ─────────────────────────────────────────
            TileMap = new SpectralXLandTileMap();
            TileMap.SetGridSize(128);
            TileMap.CustomTexturePaths = new[]
            {
        "/iAssets/DirtTile010.png",
        "/iAssets/RockTile010.png",
        "/iAssets/SnowTile010.png",
        "/iAssets/GrassTile010.png",
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

            // ── Signpost ─────────────────────────────────────────
            // ── BWPScene11 (Snow Town) ───────────────────────────────────────────
            AddSignpost(Scene14, "Scene14_SignpostToDarkForest",
                GetTriggerPos(SceneID.BWPScene11, SceneID.BWPScene8));

            // ── Static Props (Town) ───────────────────────────────
            StaticProps = new SpectralProps();

            StaticProps.SpawnFixed(
                MeshLibrary, Scene14,
                new[]
                {
            new SpectralPropPlacement("Tavern001", 10f, 8f),
            new SpectralPropPlacement("House001", -12f, 6f),
            new SpectralPropPlacement("House001", -12f, -6f),
            new SpectralPropPlacement("Fountain001", 0f, 0f),
                },
                SpectralPropDefs.All);

            var tavernMesh = Scene14.Meshes.OfType<SpectralXMesh>()
                .FirstOrDefault(m => m.Name.StartsWith("Static_Tavern001"));
            if (tavernMesh != null)
                tavernMesh.Position += new Vector3(0f, 0f, 0.1f);

            StaticProps.SpawnScattered(
                MeshLibrary, Scene14,
                SpectralPropDefs.SnowForestDictionary(),
                SpectralPropDefs.All,
                spawnRadius: 64f, clearRadius: 3f, seed: 42);

            SpectralDynManager.SpawnAll(Scene14, MeshLibrary, spawnRadius: 60f, seed: 42);
            SpectralBreakManager.SpawnAll(Scene14, MeshLibrary, spawnRadius: 12f, seed: 99);

            // ── Town is SAFE — no waves ───────────────────────────
            // (same as Scene5)

            // ── Ambient NPCs ──────────────────────────────────────
            for (int i = 0; i < 3; i++)
            {
                var (cx, cy) = RandomScatterPoint(0f, 0f, 12f);
                Cows.Add(SpawnCow(Scene14, cx, cy, 1.0f));
            }
            for (int i = 0; i < 3; i++)
            {
                var (catx, caty) = RandomScatterPoint(0f, 0f, 12f);
                Cats.Add(SpawnCat(Scene14, catx, caty, 1.0f));
            }
            for (int i = 0; i < 3; i++)
            {
                var (tsx, tsy) = RandomScatterPoint(0f, 0f, 12f);
                TownSluts.Add(SpawnTownSlut(Scene14, tsx, tsy, 1.0f));
            }

            Console.WriteLine("[InitScene14] Scene14 ready");
        }



    }
}
