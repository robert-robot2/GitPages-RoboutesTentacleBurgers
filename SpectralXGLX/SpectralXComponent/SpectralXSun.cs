using SpectralXGLX.SpectralXComponent.SpectralXLighting;
using System.Numerics;

namespace SpectralXGLX.SpectralXComponent
{

    public class SpectralXSun
    {
      
        public float TimeOfDay { get; private set; } = 0.5f;
        public float SkyBlend { get; private set; } = 0f;
        public Vector3 SunDirectionSky { get; private set; } = new Vector3(0f, -1f, 0f);
        public Vector3 MoonDirectionSky { get; private set; } = new Vector3(0f, 1f, 0f);
        public Vector3 SunDirection { get; private set; } = new Vector3(0f, 0f, -1f);
        public Vector3 SunColor { get; private set; } = new Vector3(1f, 1f, 1f);
        public float SunIntensity { get; private set; } = 1.0f;
        public Vector3 MoonDirection { get; private set; } = new Vector3(0f, 0f, 1f);
        public Vector3 MoonColor { get; private set; } = new Vector3(0.7f, 0.8f, 1.0f);
        public float MoonGlow { get; private set; } = 0f;
        public float CloudOffset { get; private set; } = 0f;
        public float StarOffset { get; private set; } = 0f;
        public float CloudScale { get; private set; } = 1.0f;
        public float StarScale { get; private set; } = 1.0f;

        private const float CloudBaseSpeed = 0.04f;

        private const float StarBaseSpeed = 0.00015f;
        public Vector3 SkyZenithColor { get; private set; } = new Vector3(0.10f, 0.45f, 0.90f);
        public Vector3 SkyHorizonColor { get; private set; } = new Vector3(0.65f, 0.80f, 1.00f);
        public float CloudOpacity { get; private set; } = 1.0f;

        private float _cloudFadeTime = 0f;
        private const float CloudFadePeriod = 30f; 
        public float RainbowIntensity { get; private set; } = 0f;
        private float _rainbowFadeTime = 0f;
        private const float RainbowFadePeriod = 45f;
        public Vector3 AmbientColor { get; private set; } = new Vector3(0.3f, 0.3f, 0.35f);

        private void ComputeAmbient()
        {
          
            Vector3 nightAmbient = new Vector3(0.04f, 0.05f, 0.12f);
            Vector3 dawnAmbient = new Vector3(0.15f, 0.12f, 0.10f);
            Vector3 dayAmbient = new Vector3(0.30f, 0.30f, 0.32f);
            Vector3 sunsetAmbient = new Vector3(0.18f, 0.12f, 0.08f);

            float t = TimeOfDay;
            Vector3 ambient;

            if (t < 0.20f)
                ambient = Vector3.Lerp(nightAmbient, nightAmbient, t / 0.20f);
            else if (t < 0.30f)
                ambient = Vector3.Lerp(nightAmbient, dawnAmbient, (t - 0.20f) / 0.10f);
            else if (t < 0.40f)
                ambient = Vector3.Lerp(dawnAmbient, dayAmbient, (t - 0.30f) / 0.10f);
            else if (t < 0.60f)
                ambient = dayAmbient;
            else if (t < 0.70f)
                ambient = Vector3.Lerp(dayAmbient, sunsetAmbient, (t - 0.60f) / 0.10f);
            else if (t < 0.80f)
                ambient = Vector3.Lerp(sunsetAmbient, nightAmbient, (t - 0.70f) / 0.10f);
            else
                ambient = nightAmbient;

            AmbientColor = ambient;
        }

        // ── Time Stops ──────────────────────────────────────────────────────────
        private readonly struct SunStop
        {
            public readonly float Time;
            public readonly Vector3 Color;
            public readonly float Intensity;
            public SunStop(float time, float r, float g, float b, float intensity)
            {
                Time = time;
                Color = new Vector3(r, g, b);
                Intensity = intensity;
            }
        }

        private static readonly SunStop[] _stops = new[]
    {
    new SunStop(0.00f,  0.05f, 0.05f, 0.15f, 0.00f),
    new SunStop(0.20f,  0.10f, 0.10f, 0.30f, 0.04f),
    new SunStop(0.25f,  1.00f, 0.45f, 0.15f, 0.30f),
    new SunStop(0.30f,  1.00f, 0.80f, 0.40f, 0.50f),
    new SunStop(0.40f,  1.00f, 0.95f, 0.80f, 0.70f),
    new SunStop(0.50f,  1.00f, 0.98f, 0.90f, 0.80f),
    new SunStop(0.60f,  1.00f, 0.95f, 0.80f, 0.70f),
    new SunStop(0.70f,  1.00f, 0.80f, 0.40f, 0.50f),
    new SunStop(0.75f,  1.00f, 0.35f, 0.10f, 0.30f),
    new SunStop(0.80f,  0.40f, 0.15f, 0.35f, 0.10f),
    new SunStop(0.85f,  0.10f, 0.05f, 0.20f, 0.02f),
    new SunStop(1.00f,  0.05f, 0.05f, 0.15f, 0.00f),
};

        private readonly struct SkyStop
        {
            public readonly float Time;
            public readonly Vector3 Zenith;
            public readonly Vector3 Horizon;
            public SkyStop(float time,
                float zR, float zG, float zB,
                float hR, float hG, float hB)
            {
                Time = time;
                Zenith = new Vector3(zR, zG, zB);
                Horizon = new Vector3(hR, hG, hB);
            }
        }

        private static readonly SkyStop[] _skyStops = new[]
        {
            new SkyStop(0.00f,  0.01f, 0.01f, 0.08f,  0.02f, 0.02f, 0.12f),
            new SkyStop(0.20f,  0.05f, 0.05f, 0.20f,  0.05f, 0.10f, 0.20f),
            new SkyStop(0.25f,  0.10f, 0.15f, 0.50f,  0.90f, 0.45f, 0.20f),
            new SkyStop(0.30f,  0.20f, 0.35f, 0.70f,  0.95f, 0.75f, 0.35f),
            new SkyStop(0.40f,  0.15f, 0.40f, 0.85f,  0.70f, 0.85f, 0.95f),
            new SkyStop(0.50f,  0.10f, 0.45f, 0.90f,  0.65f, 0.80f, 1.00f),
            new SkyStop(0.60f,  0.15f, 0.40f, 0.85f,  0.70f, 0.85f, 0.95f),
            new SkyStop(0.70f,  0.20f, 0.35f, 0.70f,  0.95f, 0.75f, 0.35f),
            new SkyStop(0.75f,  0.10f, 0.15f, 0.50f,  0.95f, 0.35f, 0.15f),
            new SkyStop(0.80f,  0.08f, 0.05f, 0.25f,  0.45f, 0.20f, 0.30f),
            new SkyStop(0.85f,  0.03f, 0.02f, 0.15f,  0.08f, 0.05f, 0.15f),
            new SkyStop(1.00f,  0.01f, 0.01f, 0.08f,  0.02f, 0.02f, 0.12f),
        };

        public void SetTime(float t)
        {
            TimeOfDay = Math.Clamp(t, 0f, 1f);
            Recompute();
        }

        public void Tick(float deltaTime)
        {
           
            float cloudSpeed = CloudBaseSpeed * (0.5f + SunIntensity * 0.1f);
            cloudSpeed = Math.Clamp(cloudSpeed, CloudBaseSpeed * 0.5f, CloudBaseSpeed * 2.0f);

            CloudOffset = (CloudOffset + cloudSpeed * deltaTime) % 1.0f;

      
            StarOffset = (StarOffset + StarBaseSpeed * deltaTime) % 1.0f;
            _cloudFadeTime += deltaTime;
            float cycle = (_cloudFadeTime % CloudFadePeriod) / CloudFadePeriod;

            CloudOpacity = 0.15f + 0.85f * (0.5f + 0.5f * MathF.Sin(cycle * MathF.PI * 2f));
            _rainbowFadeTime += deltaTime;
            float rainbowCycle = (_rainbowFadeTime % RainbowFadePeriod) / RainbowFadePeriod;
      
            RainbowIntensity = Math.Clamp((1.0f - CloudOpacity) * 1.8f, 0f, 1f);

        }

        public void Apply(SpectralXLight light)
        {
            light.Type = LightType.Directional;
            light.Direction = SunDirection;
            light.Color = SunColor;
            light.Intensity = SunIntensity;
            light.Enabled = SunIntensity > 0.01f;

            light.Position = new Vector3(
                -SunDirection.X * 80f,
                -SunDirection.Y * 80f,
                -SunDirection.Z * 80f);
        }

        private void Recompute()
        {
           ComputeSunDirection();
           ComputeSunLight();
            ComputeSkyColors();
            ComputeSkyBlend();
            ComputeMoon();
            ComputeAmbient(); 
        }

        public Vector3 SunDirectionTile { get; private set; } = new Vector3(0f, 0f, -1f);
        private void ComputeSunDirection()
        {
            float angle = (TimeOfDay - 0.25f) * MathF.PI * 2f;
            float sunX = MathF.Cos(angle);
            float sunZ = MathF.Sin(angle);

            SunDirection = -Vector3.Normalize(new Vector3(sunX, 0f, sunZ));
            SunDirectionSky = -Vector3.Normalize(new Vector3(sunX, sunZ, 0f));

            float isoAngle = (TimeOfDay - 0.25f) * MathF.PI * 2f;
            float isoY = MathF.Cos(isoAngle);
            float isoZ = MathF.Sin(isoAngle);
            SunDirectionTile = -Vector3.Normalize(new Vector3(0.4f, isoY, isoZ));

            MoonDirection = -Vector3.Normalize(new Vector3(sunX, 0f, -sunZ));
            MoonDirectionSky = -Vector3.Normalize(new Vector3(-sunX, -sunZ, 0f));
        }

        private void ComputeSunLight()
        {
            var times = _stops.Select(s => s.Time).ToArray();
            (float t0, float t1, float blend) = FindStopBlend(times);

            int i0 = FindStopIndex(times, t0);
            int i1 = FindStopIndex(times, t1);

            SunColor = Vector3.Lerp(_stops[i0].Color, _stops[i1].Color, blend);
            SunIntensity = Lerp(_stops[i0].Intensity, _stops[i1].Intensity, blend);
        }


        private void ComputeMoon()
        {

            MoonGlow = Math.Clamp(SkyBlend * 1.2f - 0.1f, 0f, 1f);

            float angle = (TimeOfDay - 0.25f) * MathF.PI * 2f;
            float moonZ = MathF.Sin(angle + MathF.PI);
            float moonElevation = Math.Clamp(moonZ, 0f, 1f);
            MoonColor = Vector3.Lerp(
                new Vector3(0.9f, 0.85f, 0.7f),
                new Vector3(0.7f, 0.80f, 1.0f),
                moonElevation);
        }



        private void ComputeSkyColors()
        {
            var times = _skyStops.Select(s => s.Time).ToArray();
            (float t0, float t1, float blend) = FindStopBlend(times);

            int i0 = FindStopIndex(times, t0);
            int i1 = FindStopIndex(times, t1);

            SkyZenithColor = Vector3.Lerp(_skyStops[i0].Zenith, _skyStops[i1].Zenith, blend);
            SkyHorizonColor = Vector3.Lerp(_skyStops[i0].Horizon, _skyStops[i1].Horizon, blend);
        }

        private void ComputeSkyBlend()
        {
            float t = TimeOfDay;

            if (t >= 0.30f && t <= 0.70f) SkyBlend = 0f;
            else if (t > 0.70f && t < 0.85f) SkyBlend = (t - 0.70f) / (0.85f - 0.70f);
            else if (t >= 0.85f || t <= 0.15f) SkyBlend = 1f;
            else SkyBlend = 1f - (t - 0.15f) / (0.30f - 0.15f);
        }

        private (float lower, float upper, float blend) FindStopBlend(float[] times)
        {
            float t = TimeOfDay;
            for (int i = 0; i < times.Length - 1; i++)
            {
                if (t >= times[i] && t <= times[i + 1])
                {
                    float range = times[i + 1] - times[i];
                    float blend = range > 0f ? (t - times[i]) / range : 0f;
                    return (times[i], times[i + 1], blend);
                }
            }
            return (times[times.Length - 2], times[times.Length - 1], 1f);
        }

        private int FindStopIndex(float[] times, float time)
        {
            for (int i = 0; i < times.Length; i++)
                if (MathF.Abs(times[i] - time) < 0.0001f) return i;
            return 0;
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}