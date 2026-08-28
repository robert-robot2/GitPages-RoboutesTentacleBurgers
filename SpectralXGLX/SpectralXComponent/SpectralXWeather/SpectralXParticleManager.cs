using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace SpectralXGLX.SpectralXComponent.SpectralXWeather
{
    public struct ParticleVolume
    {
        public float XMin, XMax, YMin, YMax, ZMin, ZMax;
        public ParticleVolume(float xMin, float xMax, float yMin, float yMax, float zMin, float zMax)
        { XMin = xMin; XMax = xMax; YMin = yMin; YMax = yMax; ZMin = zMin; ZMax = zMax; }
    }

    public class SpectralXParticleManager
    {
        private readonly Random _rng = new Random();
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLibrary;
        private readonly Dictionary<string, IMesh> _meshLookup = new();
        private Dictionary<WeatherParticleType, ParticleVolume> _volumes = new();

        private const int RainPoolSize = 400;
        private const int SnowPoolSize = 300;
        private const int CloudPoolSize = 296;
        private const int LightningPoolSize = 4;

        private static readonly Vector3 OffScreen = new Vector3(0f, 0f, -999f);

        private readonly List<SpectralXParticle> _rainPool = new();
        private readonly List<SpectralXParticle> _snowPool = new();
        private readonly List<SpectralXParticle> _cloudPool = new();
        private readonly List<SpectralXParticle> _lightningPool = new();

        private int _activeCount = 0;

        public int ActiveParticleCount => _activeCount;

        public IEnumerable<SpectralXParticle> GetActiveParticles() =>
            _rainPool.Concat(_snowPool).Concat(_cloudPool).Concat(_lightningPool)
                     .Where(p => p.IsAlive);

        public void Init(SpectralXScene scene, SpectralXMeshLibrary meshLibrary,
            Dictionary<WeatherParticleType, ParticleVolume> volumes)
        {
            _scene = scene;
            _meshLibrary = meshLibrary;

            _rainPool.Clear();
            _snowPool.Clear();
            _cloudPool.Clear();
            _lightningPool.Clear();
            _meshLookup.Clear();
            _volumes = volumes;

            AllocatePool(_rainPool, RainPoolSize, WeatherParticleType.Rain,
                "/iAssets/RainDrop01.png", 0.15f, 0.5f);

            AllocatePool(_snowPool, SnowPoolSize, WeatherParticleType.Snow,
                "/iAssets/SnowFlake06.png", 0.3f, 0.3f);

            AllocatePool(_cloudPool, CloudPoolSize, WeatherParticleType.Cloud,
                "/iAssets/GOkuCloud001.png", 6.0f, 4.0f);

            AllocatePool(_lightningPool, LightningPoolSize, WeatherParticleType.Lightning,
                "/iAssets/LBolt002.png", 0.3f, 1.5f);
        }

        private void AllocatePool(
            List<SpectralXParticle> pool,
            int count,
            WeatherParticleType type,
            string texturePath,
            float width,
            float height)
        {
            if (_scene == null || _meshLibrary == null) return;

            for (int i = 0; i < count; i++)
            {
                string meshName = $"ParticlePool_{type}_{i}";

                var mesh = _meshLibrary.GetMesh("PrimSquare") as SpectralXMesh;
                if (mesh == null) continue;

                mesh.Name = meshName;
                mesh.Position = OffScreen;
                mesh.Size = new Vector3(width, width, height);
                mesh.Rotation = new Vector3(MathF.PI / 2f, 0f, 0f);
                mesh.Color = new Vector4(1f, 1f, 1f, 0f);
                mesh.TextureDataUrl = texturePath;
                mesh.TextureIsRawRGBA = false;

                _meshLookup[meshName] = mesh;

                var particle = new SpectralXParticle
                {
                    MeshName = meshName,
                    TexturePath = texturePath,
                    Type = type,
                    Width = width,
                    Height = height,
                    Lifetime = 0f,
                    Age = 1f,
                };

                pool.Add(particle);
            }
        }

        public void Spawn(
            WeatherParticleType type,
            float vx, float vy, float vz,
            float lifetime,
            float opacity = 1f)
        {
            var pool = GetPool(type);
            if (pool == null) return;

            var slot = pool.FirstOrDefault(p => !p.IsAlive);
            if (slot == null) return;

            if (!_volumes.TryGetValue(type, out var vol))
                vol = new ParticleVolume(-15f, 15f, -15f, 15f, 20f, 50f);

            float x = vol.XMin + (float)_rng.NextDouble() * (vol.XMax - vol.XMin);
            float y = vol.YMin + (float)_rng.NextDouble() * (vol.YMax - vol.YMin);
            float z = vol.ZMin + (float)_rng.NextDouble() * (vol.ZMax - vol.ZMin);

            slot.X = x;
            slot.Y = y;
            slot.Z = z;
            slot.VX = vx;
            slot.VY = vy;
            slot.VZ = vz;
            slot.Lifetime = lifetime;
            slot.Age = 0f;
            slot.Opacity = opacity;
            _activeCount++;
        }

        public void Tick(float deltaTime, SpectralXCamera camera)
        {
            if (_scene == null) return;

            var allSlots = _rainPool
                .Concat(_snowPool)
                .Concat(_cloudPool)
                .Concat(_lightningPool);

            foreach (var p in allSlots)
            {
                if (!p.IsAlive) continue;
                p.Tick(deltaTime);
                if (!p.IsAlive) _activeCount--;
            }
        }

        public void Reset()
        {
            if (_scene == null) return;

            var allSlots = _rainPool
                .Concat(_snowPool)
                .Concat(_cloudPool)
                .Concat(_lightningPool);

            foreach (var p in allSlots)
            {
                p.Lifetime = 0f;
                p.Age = 1f;

                var mesh = _scene.Meshes
                    .FirstOrDefault(m => m.Name == p.MeshName);
                if (mesh != null)
                {
                    mesh.Position = OffScreen;
                    mesh.Color = new Vector4(1f, 1f, 1f, 0f);
                }
            }
            _activeCount = 0;
        }

        private List<SpectralXParticle>? GetPool(WeatherParticleType type) => type switch
        {
            WeatherParticleType.Rain => _rainPool,
            WeatherParticleType.Snow => _snowPool,
            WeatherParticleType.Cloud => _cloudPool,
            WeatherParticleType.Lightning => _lightningPool,
            _ => null
        };

        public IEnumerable<SpectralXParticle> GetPoolSlots(WeatherParticleType type) => type switch
        {
            WeatherParticleType.Rain => _rainPool,
            WeatherParticleType.Snow => _snowPool,
            WeatherParticleType.Cloud => _cloudPool,
            WeatherParticleType.Lightning => _lightningPool,
            _ => Enumerable.Empty<SpectralXParticle>()
        };
    }
}