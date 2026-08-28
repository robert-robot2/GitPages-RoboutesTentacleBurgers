using System;
using System.Collections.Generic;
using System.Linq;

namespace SnowX.Weather
{
    public enum WeatherType
    {
        Clear,
        Rain,
        Snow,
        Dust,
        Lightning,
        Blizzard,
        Sandstorm
    }

    public enum BiomeType
    {
        Forest = 0,   // Matches your environment 0
        Desert = 1,   // Matches your environment 1
        Snow = 2      // Matches your environment 2
    }

    public class WeatherParticle
    {
        public string ImagePath { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Life { get; set; }
        public double MaxLife { get; set; }
        public int Width { get; set; } = 16;
        public int Height { get; set; } = 16;
        public double Opacity { get; set; } = 1.0;
        public bool IsPixel { get; set; } = false; // If true, render as circle instead of image
        public string PixelColor { get; set; } = "#fff";
    }

    public class SnowWeatherCycle
    {
        private Random _rng = new Random();
        private double _lastWeatherChangeTime = 0;
        private double _lastParticleSpawnTime = 0;
        private double _gameTime = 0;

        // Weather cycle settings
        public double WeatherCycleIntervalSeconds { get; set; } = 30.0;
        public bool IsWeatherEnabled { get; set; } = true;
        public WeatherType CurrentWeather { get; private set; } = WeatherType.Clear;
        public BiomeType CurrentBiome { get; set; } = BiomeType.Forest;

        // Fog settings
        public bool IsFogEnabled { get; set; } = true;
        public double FogOffsetX { get; private set; } = 0;
        public double FogOffsetY { get; private set; } = 0;
        public double FogSpeedX { get; set; } = 0.4;
        public double FogSpeedY { get; set; } = 0.4;
        public double FogOpacity { get; set; } = 0.1;

        // Particle list
        private List<WeatherParticle> _particles = new List<WeatherParticle>();
        public IEnumerable<WeatherParticle> ActiveParticles => _particles;

        // Image paths (can be overridden)
        public Dictionary<string, string> WeatherImages = new Dictionary<string, string>
        {
            ["rain"] = "/iAssets/RainDrop01.png",
            ["snow"] = "/iAssets/SnowFlake06.png",
            ["dust"] = "/iAssets/DustParticle01.png",
            ["cloud"] = "/iAssets/GOkuCloud001.png",
            ["lightning"] = "/iAssets/LBolt002.png",
            ["fog"] = "/iAssets/fog01SL.png"
        };

        public void Update(double deltaTime)
        {
            _gameTime += deltaTime;

            if (!IsWeatherEnabled) return;

            // Update weather cycle
            if (_gameTime - _lastWeatherChangeTime > WeatherCycleIntervalSeconds)
            {
                _lastWeatherChangeTime = _gameTime;
                ChangeWeatherForBiome();
            }

            // Spawn particles
            if (_gameTime - _lastParticleSpawnTime > 0.05) // Spawn every 50ms
            {
                _lastParticleSpawnTime = _gameTime;
                SpawnWeatherParticles();
            }

            // Update particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.X += p.VelocityX;
                p.Y += p.VelocityY;
                p.Life -= deltaTime;

                // Remove dead particles or those off-screen
                if (p.Life <= 0 || p.Y > 850 || p.X < -100 || p.X > 1700)
                {
                    _particles.RemoveAt(i);
                }
            }

            // Update fog
            if (IsFogEnabled)
            {
                FogOffsetX = (FogOffsetX + FogSpeedX) % 2048;
                FogOffsetY = (FogOffsetY + FogSpeedY) % 2048;
            }
        }

        private void ChangeWeatherForBiome()
        {
            int roll = _rng.Next(0, 100);

            switch (CurrentBiome)
            {
                case BiomeType.Forest:
                    // Forest: Rain (50%), Lightning (20%), Clear (30%)
                    if (roll < 50) CurrentWeather = WeatherType.Rain;
                    else if (roll < 70) CurrentWeather = WeatherType.Lightning;
                    else CurrentWeather = WeatherType.Clear;
                    break;

                case BiomeType.Desert:
                    // Desert: Dust (40%), Sandstorm (25%), Clear (35%)
                    if (roll < 40) CurrentWeather = WeatherType.Dust;
                    else if (roll < 65) CurrentWeather = WeatherType.Sandstorm;
                    else CurrentWeather = WeatherType.Clear;
                    break;

                case BiomeType.Snow:
                    // Snow: Snow (45%), Blizzard (30%), Clear (25%)
                    if (roll < 45) CurrentWeather = WeatherType.Snow;
                    else if (roll < 75) CurrentWeather = WeatherType.Blizzard;
                    else CurrentWeather = WeatherType.Clear;
                    break;
            }
        }

        private void SpawnWeatherParticles()
        {
            switch (CurrentWeather)
            {
                case WeatherType.Rain:
                    SpawnRain(2);
                    break;

                case WeatherType.Lightning:
                    SpawnRain(1);
                    if (_rng.NextDouble() < 0.02) // 2% chance per spawn cycle
                        SpawnLightning();
                    break;

                case WeatherType.Snow:
                    SpawnSnow(2);
                    break;

                case WeatherType.Blizzard:
                    SpawnSnow(5); // More intense
                    break;

                case WeatherType.Dust:
                    SpawnDust(2);
                    break;

                case WeatherType.Sandstorm:
                    SpawnDust(2); // More intense
                    break;

                case WeatherType.Clear:
                    // Occasional cloud
                    if (_rng.NextDouble() < 0.1)
                        SpawnCloud();
                    break;
            }
        }

        private void SpawnRain(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _particles.Add(new WeatherParticle
                {
                    IsPixel = true,
                    PixelColor = "#4d9fff",
                    X = _rng.Next(-100, 1700),
                    Y = _rng.Next(-50, 100),
                    VelocityX = 0,
                    VelocityY = 12,
                    Life = 3.0,
                    MaxLife = 3.0,
                    Width = 2,
                    Height = 8,
                    Opacity = 0.6
                });
            }
        }

        private void SpawnSnow(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _particles.Add(new WeatherParticle
                {
                    IsPixel = true,
                    PixelColor = "#ffffff",
                    X = _rng.Next(-100, 1700),
                    Y = _rng.Next(-50, 100),
                    VelocityX = _rng.Next(-1, 2),
                    VelocityY = _rng.Next(1, 3),
                    Life = 8.0,
                    MaxLife = 8.0,
                    Width = 4,
                    Height = 4,
                    Opacity = 0.7
                });
            }
        }

        private void SpawnDust(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _particles.Add(new WeatherParticle
                {
                    IsPixel = true,
                    PixelColor = "#d4a574",
                    X = _rng.Next(-100, 1700),
                    Y = _rng.Next(200, 700),
                    VelocityX = _rng.Next(2, 5),
                    VelocityY = _rng.Next(-1, 2),
                    Life = 5.0,
                    MaxLife = 5.0,
                    Width = 6,
                    Height = 6,
                    Opacity = 0.4
                });
            }
        }

        private void SpawnCloud()
        {
            // Check if image exists, otherwise use pixel
            bool hasImage = !string.IsNullOrEmpty(WeatherImages.GetValueOrDefault("cloud"));

            _particles.Add(new WeatherParticle
            {
                ImagePath = hasImage ? WeatherImages["cloud"] : "",
                IsPixel = !hasImage,
                PixelColor = "#cccccc",
                X = 1700,
                Y = _rng.Next(50, 300),
                VelocityX = -1,
                VelocityY = 0,
                Life = 20.0,
                MaxLife = 20.0,
                Width = 128,
                Height = 64,
                Opacity = 0.3
            });
        }

        private void SpawnLightning()
        {
            bool hasImage = !string.IsNullOrEmpty(WeatherImages.GetValueOrDefault("lightning"));

            _particles.Add(new WeatherParticle
            {
                ImagePath = hasImage ? WeatherImages["lightning"] : "",
                IsPixel = !hasImage,
                PixelColor = "#ffff00",
                X = _rng.Next(200, 1400),
                Y = 0,
                VelocityX = 0,
                VelocityY = 0,
                Life = 0.2,
                MaxLife = 0.2,
                Width = hasImage ? 32 : 4,
                Height = hasImage ? 200 : 800,
                Opacity = 0.9
            });
        }

        public string GetFogStyle()
        {
            if (!IsFogEnabled) return "";

            return $"position:absolute; left:0; top:0; width:100%; height:100%; " +
                   $"background-image:url('{WeatherImages.GetValueOrDefault("fog")}'); " +
                   $"background-position:{FogOffsetX}px {FogOffsetY}px; " +
                   $"background-repeat:repeat; background-size:cover; " +
                   $"opacity:{FogOpacity}; pointer-events:none;";
        }

        public string GetWeatherName()
        {
            return CurrentWeather switch
            {
                WeatherType.Clear => "Clear",
                WeatherType.Rain => "Rain",
                WeatherType.Snow => "Snow",
                WeatherType.Dust => "Dust",
                WeatherType.Lightning => "Thunderstorm",
                WeatherType.Blizzard => "Blizzard",
                WeatherType.Sandstorm => "Sandstorm",
                _ => "Unknown"
            };
        }
    }
}