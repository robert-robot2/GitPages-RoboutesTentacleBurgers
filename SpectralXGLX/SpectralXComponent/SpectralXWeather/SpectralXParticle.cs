using System.Numerics;

namespace SpectralXGLX.SpectralXComponent.SpectralXWeather
{
    public class SpectralXParticle
    {
        // World position
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        // Velocity
        public float VX { get; set; }
        public float VY { get; set; }
        public float VZ { get; set; }

        // Lifetime
        public float Lifetime { get; set; }
        public float Age { get; set; }
        public bool IsAlive => Age < Lifetime;

        // Visual
        public float Width { get; set; } = 0.3f;
        public float Height { get; set; } = 0.3f;
        public float Opacity { get; set; } = 1f;
        public string TexturePath { get; set; } = string.Empty;
        public WeatherParticleType Type { get; set; }

        // Linked mesh in scene — set by ParticleManager on spawn
        public string MeshName { get; set; } = string.Empty;

        // Y-axis rotation in radians — set once on spawn to face initial camera
        public float RotationY { get; set; } = 0f;

        public Vector3 Position => new Vector3(X, Y, Z);

        public void Tick(float deltaTime)
        {
            X += VX * deltaTime;
            Y += VY * deltaTime;
            Z += VZ * deltaTime;
            Age += deltaTime;
        }
    }
}