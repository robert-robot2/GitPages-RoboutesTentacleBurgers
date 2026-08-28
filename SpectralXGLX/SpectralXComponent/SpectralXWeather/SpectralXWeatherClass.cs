using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SpectralXGLX.SpectralXComponent.SpectralXWeather
{
    public class SpectralXWeatherClass : ISpectralWeather
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly Random _rng = new Random();
        private readonly SpectralXParticleManager _particles = new();

        private double _lastWeatherChangeTime = 0;
        private double _lastSpawnTime = 0;
        private const double WeatherCycleInterval = 30.0;
        private const double SpawnInterval = 0.5;

        public WeatherParticleType CurrentWeather { get; private set; } = WeatherParticleType.Rain;
        public bool IsActive { get; private set; } = false;
        public int ActiveParticleCount => _particles.ActiveParticleCount;

        public IEnumerable<SpectralXParticle> GetActiveParticles()
            => _particles.GetActiveParticles();

        private const int MaxParticles = 1000;
        private float[] _offsetsCache = new float[MaxParticles * 3];
        private float[] _colorsCache = new float[MaxParticles * 4];
        private float[] _sizesCache = new float[MaxParticles];

        public void Init(SpectralXScene scene, SpectralXMeshLibrary meshLibrary,
            Dictionary<WeatherParticleType, ParticleVolume> volumes)
        {
            _particles.Init(scene, meshLibrary, volumes);
            IsActive = true;
            _clock.Restart();
            _lastWeatherChangeTime = 0;
            _lastSpawnTime = 0;
            CurrentWeather = WeatherParticleType.Rain;
        }

        public void Tick(float deltaTime, SpectralXCamera camera)
        {
            if (!IsActive) return;

            double now = _clock.Elapsed.TotalSeconds;
            UpdateWeatherCycle(now);

            if (now - _lastSpawnTime > SpawnInterval)
            {
                _lastSpawnTime = now;
                SpawnBatch();
            }

            _particles.Tick(deltaTime, camera);
        }

        public void Reset()
        {
            _particles.Reset();
            IsActive = false;
            _clock.Reset();
        }

        private void UpdateWeatherCycle(double now)
        {
            if (now - _lastWeatherChangeTime < WeatherCycleInterval) return;
            _lastWeatherChangeTime = now;

            int roll = _rng.Next(0, 100);
            if (roll < 35) CurrentWeather = WeatherParticleType.Rain;
            else if (roll < 75) CurrentWeather = WeatherParticleType.Snow;
            else if (roll < 85) CurrentWeather = WeatherParticleType.Cloud;
            else CurrentWeather = WeatherParticleType.Clear;
        }

        private void SpawnBatch()
        {
            switch (CurrentWeather)
            {
                case WeatherParticleType.Rain:
                    SpawnRain(25);
                    if (_rng.NextDouble() < 0.05)
                        SpawnLightning();
                    break;
                case WeatherParticleType.Snow:
                    SpawnSnow(20);
                    break;
                case WeatherParticleType.Cloud:
                    SpawnClouds(5);
                    break;
                case WeatherParticleType.Clear:
                    break;
            }
        }

        private void SpawnRain(int count)
        {
            for (int i = 0; i < count; i++)
                _particles.Spawn(WeatherParticleType.Rain,
                    vx: 0f, vy: 0f, vz: -4f,
                    lifetime: 2.0f, opacity: 0.9f);
        }

        private void SpawnSnow(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float drift = (_rng.NextSingle() - 0.5f) * 0.3f;
                _particles.Spawn(WeatherParticleType.Snow,
                    vx: drift, vy: drift, vz: -0.8f,
                    lifetime: 5.0f, opacity: 1f);
            }
        }

        private void SpawnClouds(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float drift = (_rng.NextSingle()) * 0.5f + 0.2f;
                _particles.Spawn(WeatherParticleType.Cloud,
                    vx: drift, vy: 0f, vz: 0f,
                    lifetime: 10.0f, opacity: 0.5f);
            }
        }

        private void SpawnLightning()
        {
            _particles.Spawn(WeatherParticleType.Lightning,
                vx: 0f, vy: 0f, vz: 0f,
                lifetime: 0.3f, opacity: 0.9f);
        }

        public List<ParticleInstanceGroup> BuildInstanceGroups()
        {
            var groups = new List<ParticleInstanceGroup>();

            var typeMap = new[]
            {
                (Type: "Rain",      Particles: _particles.GetPoolSlots(WeatherParticleType.Rain),
                    TexKey: "ParticleGeo_/iAssets/RainDrop01.png"),
                (Type: "Snow",      Particles: _particles.GetPoolSlots(WeatherParticleType.Snow),
                    TexKey: "ParticleGeo_/iAssets/SnowFlake06.png"),
                (Type: "Cloud",     Particles: _particles.GetPoolSlots(WeatherParticleType.Cloud),
                    TexKey: "ParticleGeo_/iAssets/GOkuCloud001.png"),
                (Type: "Lightning", Particles: _particles.GetPoolSlots(WeatherParticleType.Lightning),
                    TexKey: "ParticleGeo_/iAssets/LBolt002.png"),
            };

            foreach (var entry in typeMap)
            {
                int count = 0;
                foreach (var p in entry.Particles)
                {
                    if (!p.IsAlive) continue;
                    _offsetsCache[count * 3] = p.X;
                    _offsetsCache[count * 3 + 1] = p.Y;
                    _offsetsCache[count * 3 + 2] = p.Z;
                    _colorsCache[count * 4] = 1f;
                    _colorsCache[count * 4 + 1] = 1f;
                    _colorsCache[count * 4 + 2] = 1f;
                    _colorsCache[count * 4 + 3] = p.Opacity;
                    _sizesCache[count] = p.Width;
                    count++;
                }
                if (count == 0) continue;

                groups.Add(new ParticleInstanceGroup
                {
                    Type = entry.Type,
                    Count = count,
                    Offsets = _offsetsCache[..(count * 3)],
                    Colors = _colorsCache[..(count * 4)],
                    Sizes = _sizesCache[..count],
                    TexKey = entry.TexKey,
                });
            }

            return groups;
        }
    }
}