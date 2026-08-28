using System.Collections.Generic;

namespace SpectralXGLX.SpectralXComponent.SpectralXWeather
{
    public interface ISpectralWeather
    {
        WeatherParticleType CurrentWeather { get; }
        bool IsActive { get; }

        void Init(SpectralXScene scene, SpectralXMeshLibrary meshLibrary,
            Dictionary<WeatherParticleType, ParticleVolume> volumes);
        void Tick(float deltaTime, SpectralXCamera camera);
        void Reset();

        IEnumerable<SpectralXParticle> GetActiveParticles();
        int ActiveParticleCount { get; }
    }

    public enum WeatherParticleType
    {
        Clear,
        Rain,
        Snow,
        Cloud,
        Lightning
    }
}