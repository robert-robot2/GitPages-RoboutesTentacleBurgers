

namespace SpectralXGLX.SpectralXComponent
{
    public partial class SpectralXEngine
    {


        private void InitScene2()
        {

            _propScatter.Reset();
            _foliageGroups.Clear();

            Weather = new SpectralXWeatherClass();
            Weather.Init(Scene2, MeshLibrary, new Dictionary<WeatherParticleType, ParticleVolume>
{
    { WeatherParticleType.Rain,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
    { WeatherParticleType.Snow,      new ParticleVolume(-64f, 64f, -64f, 64f,  0f, 30f) },
    { WeatherParticleType.Cloud,     new ParticleVolume(-64f, 64f, -64f, 64f, 30f, 70f) },
    { WeatherParticleType.Lightning, new ParticleVolume(-64f, 64f, -64f, 64f, 30f, 70f) },
});

            // ── Scene 2 Lighting ────────────────────────────────────────────────────────

            // White point light
            var scene2PointL1 = new SpectralXLight(
                position: new Vector3(44, -42, 10),
                color: new Vector3(1f, 1f, 1f),
                intensity: 1.0f,
                range: 15f);
            scene2PointL1.CastsShadows = false;
            Scene2.AddLight(scene2PointL1);

            // Blue point light
            var scene2PointL2 = new SpectralXLight(
                position: new Vector3(34, -64, 8),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 6.0f,
                range: 15f);
            scene2PointL2.CastsShadows = false;
            Scene2.AddLight(scene2PointL2);

            // Purple point light
            var scene2PointL3 = new SpectralXLight(
                position: new Vector3(24, -64, 10),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 6.0f,
                range: 15f);
            scene2PointL3.CastsShadows = false;
            Scene2.AddLight(scene2PointL3);

            // Spot light — haunted church area
            var scene2SpotL1 = new SpectralXLight(
                position: new Vector3(64f, -80f, 15f),
                color: new Vector3(1f, 0.9f, 0.7f),
                intensity: 15.0f,
                range: 30f);
            scene2SpotL1.Type = LightType.Spot;
            scene2SpotL1.Direction = new Vector3(0f, 0.4f, -1f);
            scene2SpotL1.SpotAngle = 25f;
            scene2SpotL1.SpotSoftness = 0.15f;
            scene2SpotL1.CastsShadows = true;
            Scene2.AddLight(scene2SpotL1);

            // Area light — market district
            var scene2AreaL1 = new SpectralXLight(
                position: new Vector3(48f, -64f, 2f),
                color: new Vector3(0.8f, 0.9f, 1.0f),
                intensity: 3.0f,
                range: 60f);
            scene2AreaL1.Type = LightType.Area;
            scene2AreaL1.Direction = new Vector3(0f, 0f, -1f);
            scene2AreaL1.CastsShadows = true;
            Scene2.AddLight(scene2AreaL1);

            // Red spot — haunted church
            var scene2RedSpotL1 = new SpectralXLight(
                position: new Vector3(-64f, 54f, 22f),
                color: new Vector3(1f, 0f, 0f),
                intensity: 6.0f,
                range: 25f);
            scene2RedSpotL1.Type = LightType.Spot;
            scene2RedSpotL1.Direction = new Vector3(0f, 0f, -1f);
            scene2RedSpotL1.SpotAngle = 25f;
            scene2RedSpotL1.SpotSoftness = 0.15f;
            scene2RedSpotL1.CastsShadows = true;
            scene2RedSpotL1.Enabled = true;
            Scene2.AddLight(scene2RedSpotL1);

            // Green point — plague/swamp
            var scene2GreenPointL1 = new SpectralXLight(
                position: new Vector3(-96f, -96f, 4f),
                color: new Vector3(0f, 1f, 0f),
                intensity: 6.0f,
                range: 15f);
            scene2GreenPointL1.Type = LightType.Point;
            scene2GreenPointL1.CastsShadows = true;
            scene2GreenPointL1.Enabled = true;
            Scene2.AddLight(scene2GreenPointL1);

            // Purple point — witch's hut
            var scene2PurplePointL1 = new SpectralXLight(
                position: new Vector3(128f, 128f, 10f),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 6.0f,
                range: 15f);
            scene2PurplePointL1.Type = LightType.Point;
            scene2PurplePointL1.CastsShadows = true;
            scene2PurplePointL1.Enabled = true;
            Scene2.AddLight(scene2PurplePointL1);

            // Orange point — blacksmith/forge
            var scene2OrangePointL1 = new SpectralXLight(
                position: new Vector3(34f, -32f, 18f),
                color: new Vector3(1f, 0.4f, 0f),
                intensity: 6.0f,
                range: 15f);
            scene2OrangePointL1.Type = LightType.Point;
            scene2OrangePointL1.CastsShadows = true;
            scene2OrangePointL1.Enabled = true;
            Scene2.AddLight(scene2OrangePointL1);

            // Purple area — coven/ritual
            var scene2PurpleAreaL1 = new SpectralXLight(
                position: new Vector3(64f, -96f, 4f),
                color: new Vector3(0.5f, 0f, 0.8f),
                intensity: 6.0f,
                range: 15f);
            scene2PurpleAreaL1.Type = LightType.Area;
            scene2PurpleAreaL1.Direction = new Vector3(0f, 0f, -1f);
            scene2PurpleAreaL1.CastsShadows = true;
            scene2PurpleAreaL1.Enabled = true;
            Scene2.AddLight(scene2PurpleAreaL1);

            // Cyan point — docks/water
            var scene2CyanPointL1 = new SpectralXLight(
                position: new Vector3(0f, -128f, 4f),
                color: new Vector3(0f, 0.25f, 1f),
                intensity: 6.0f,
                range: 15f);
            scene2CyanPointL1.Type = LightType.Point;
            scene2CyanPointL1.CastsShadows = true;
            scene2CyanPointL1.Enabled = true;
            Scene2.AddLight(scene2CyanPointL1);

            // Deep blue point — crypt/underground
            var scene2DeepBluePointL1 = new SpectralXLight(
                position: new Vector3(0f, 64f, 18f),
                color: new Vector3(0f, 0f, 0.8f),
                intensity: 6.0f,
                range: 15f);
            scene2DeepBluePointL1.Type = LightType.Point;
            scene2DeepBluePointL1.CastsShadows = true;
            scene2DeepBluePointL1.Enabled = true;
            Scene2.AddLight(scene2DeepBluePointL1);

            // Warm yellow point — tavern/inn
            var scene2WarmYellowPointL1 = new SpectralXLight(
                position: new Vector3(4f, -32f, 20f),
                color: new Vector3(1f, 0.85f, 0.3f),
                intensity: 8.0f,
                range: 25f);
            scene2WarmYellowPointL1.Type = LightType.Point;
            scene2WarmYellowPointL1.CastsShadows = true;
            scene2WarmYellowPointL1.Enabled = true;
            Scene2.AddLight(scene2WarmYellowPointL1);

            // Cold white point — temple/holy
            var scene2ColdWhitePointL1 = new SpectralXLight(
                position: new Vector3(-10f, -64f, 20f),
                color: new Vector3(0.9f, 0.95f, 1f),
                intensity: 6.0f,
                range: 15f);
            scene2ColdWhitePointL1.Type = LightType.Point;
            scene2ColdWhitePointL1.CastsShadows = true;
            scene2ColdWhitePointL1.Enabled = true;
            Scene2.AddLight(scene2ColdWhitePointL1);

            // Sickly green point — alchemist/poison
            var scene2SicklyGreenPointL1 = new SpectralXLight(
                position: new Vector3(16f, -84f, 10f),
                color: new Vector3(0.6f, 0.9f, 0f),
                intensity: 6.0f,
                range: 15f);
            scene2SicklyGreenPointL1.Type = LightType.Point;
            scene2SicklyGreenPointL1.CastsShadows = true;
            scene2SicklyGreenPointL1.Enabled = true;
            Scene2.AddLight(scene2SicklyGreenPointL1);

            // Deep red point — dungeon entrance
            var scene2DeepRedPointL1 = new SpectralXLight(
                position: new Vector3(0f, -96f, 4f),
                color: new Vector3(0.7f, 0f, 0f),
                intensity: 6.0f,
                range: 15f);
            scene2DeepRedPointL1.Type = LightType.Point;
            scene2DeepRedPointL1.CastsShadows = true;
            scene2DeepRedPointL1.Enabled = true;
            Scene2.AddLight(scene2DeepRedPointL1);

            // Pink point — market/festival
            var scene2PinkPointL1 = new SpectralXLight(
                position: new Vector3(8f, -64f, 10f),
                color: new Vector3(1f, 0.4f, 0.7f),
                intensity: 6.0f,
                range: 15f);
            scene2PinkPointL1.Type = LightType.Point;
            scene2PinkPointL1.CastsShadows = true;
            scene2PinkPointL1.Enabled = true;
            Scene2.AddLight(scene2PinkPointL1);

            // Sun directional light — added last
            _sunLight = new SpectralXLight(
                position: new Vector3(0f, -25f, 80f),
                color: new Vector3(1f, 0.98f, 0.90f),
                intensity: 5.0f,
                range: 200f);
            _sunLight.Type = LightType.Directional;
            _sunLight.Direction = new Vector3(0f, -0.5f, -1f);
            _sunLight.CastsShadows = true;
            _sunLight.Enabled = true;
            Sun.Apply(_sunLight);
            Scene2.AddLight(_sunLight);

            // ── Scene 2 Light Gizmos ─────────────────────────────────────────────────────

            // White point L1
            var scene2L1Gizmo = CreateGizmoFrom("S2_LightGizmo_L1", "LightBulb");
            scene2L1Gizmo.Position = scene2PointL1.Position;
            scene2L1Gizmo.Size = new Vector3(0.3f, 0.3f, 0.3f);
            scene2L1Gizmo.Color = new Vector4(1f, 0.98f, 0.85f, 0.4f);
            scene2L1Gizmo.IsEmissive = true;
            scene2L1Gizmo.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2L1Gizmo);

            var scene2L1Core = CreateGizmoFrom("S2_LightCore_L1", "SmoothSphere");
            scene2L1Core.Position = scene2PointL1.Position;
            scene2L1Core.Size = new Vector3(0.08f, 0.08f, 0.08f);
            scene2L1Core.Color = new Vector4(1f, 0.95f, 0.6f, 1f);
            scene2L1Core.IsEmissive = true;
            scene2L1Core.EmissiveIntensity = 3.0f;
            Scene2.AddMesh(scene2L1Core);

            var scene2L1AuraInner = CreateGizmoFrom("S2_LightAuraInner_L1", "SmoothSphere");
            scene2L1AuraInner.Position = scene2PointL1.Position;
            scene2L1AuraInner.Size = new Vector3(0.35f, 0.35f, 0.35f);
            scene2L1AuraInner.Color = new Vector4(1f, 0.85f, 0.4f, 0.12f);
            scene2L1AuraInner.IsEmissive = true;
            scene2L1AuraInner.EmissiveIntensity = 1.2f;
            Scene2.AddMesh(scene2L1AuraInner);

            var scene2L1AuraOuter = CreateGizmoFrom("S2_LightAuraOuter_L1", "SmoothSphere");
            scene2L1AuraOuter.Position = scene2PointL1.Position;
            scene2L1AuraOuter.Size = new Vector3(0.6f, 0.6f, 0.6f);
            scene2L1AuraOuter.Color = new Vector4(1f, 0.75f, 0.3f, 0.05f);
            scene2L1AuraOuter.IsEmissive = true;
            scene2L1AuraOuter.EmissiveIntensity = 0.6f;
            Scene2.AddMesh(scene2L1AuraOuter);

            // Blue point L2
            var scene2L2Gizmo = CreateGizmoFrom("S2_LightGizmo_L2", "SmoothSphere");
            scene2L2Gizmo.Position = scene2PointL2.Position;
            scene2L2Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2L2Gizmo.Color = new Vector4(0f, 0.4f, 1f, 1f);
            scene2L2Gizmo.IsEmissive = true;
            scene2L2Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2L2Gizmo);

            var scene2L2Aura = CreateGizmoFrom("S2_LightAura_L2", "SmoothSphere");
            scene2L2Aura.Position = scene2PointL2.Position;
            scene2L2Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2L2Aura.Color = new Vector4(0f, 0.4f, 1f, 0.08f);
            scene2L2Aura.IsEmissive = true;
            scene2L2Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2L2Aura);

            // Purple point L3
            var scene2L3Gizmo = CreateGizmoFrom("S2_LightGizmo_L3", "SmoothSphere");
            scene2L3Gizmo.Position = scene2PointL3.Position;
            scene2L3Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2L3Gizmo.Color = new Vector4(0.6f, 0f, 1f, 1f);
            scene2L3Gizmo.IsEmissive = true;
            scene2L3Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2L3Gizmo);

            var scene2L3Aura = CreateGizmoFrom("S2_LightAura_L3", "SmoothSphere");
            scene2L3Aura.Position = scene2PointL3.Position;
            scene2L3Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2L3Aura.Color = new Vector4(0.6f, 0f, 1f, 0.08f);
            scene2L3Aura.IsEmissive = true;
            scene2L3Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2L3Aura);

            // Spot L1
            var scene2SpotL1Gizmo = CreateGizmoFrom("S2_SpotGizmo_L1", "SmoothSphere");
            scene2SpotL1Gizmo.Position = scene2SpotL1.Position;
            scene2SpotL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2SpotL1Gizmo.Color = new Vector4(1f, 0.9f, 0.7f, 1f);
            scene2SpotL1Gizmo.IsEmissive = true;
            scene2SpotL1Gizmo.EmissiveIntensity = 3.0f;
            Scene2.AddMesh(scene2SpotL1Gizmo);

            var scene2SpotL1Aura = CreateGizmoFrom("S2_SpotAura_L1", "SmoothSphere");
            scene2SpotL1Aura.Position = scene2SpotL1.Position;
            scene2SpotL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2SpotL1Aura.Color = new Vector4(1f, 0.9f, 0.7f, 0.08f);
            scene2SpotL1Aura.IsEmissive = true;
            scene2SpotL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2SpotL1Aura);

            // Area L1
            var scene2AreaL1Gizmo = CreateGizmoFrom("S2_AreaGizmo_L1", "SmoothSphere");
            scene2AreaL1Gizmo.Position = scene2AreaL1.Position;
            scene2AreaL1Gizmo.Size = new Vector3(0.3f, 0.3f, 0.3f);
            scene2AreaL1Gizmo.Color = new Vector4(0.8f, 0.9f, 1.0f, 1f);
            scene2AreaL1Gizmo.IsEmissive = true;
            scene2AreaL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2AreaL1Gizmo);

            var scene2AreaL1Aura = CreateGizmoFrom("S2_AreaAura_L1", "SmoothSphere");
            scene2AreaL1Aura.Position = scene2AreaL1.Position;
            scene2AreaL1Aura.Size = new Vector3(0.7f, 0.7f, 0.7f);
            scene2AreaL1Aura.Color = new Vector4(0.8f, 0.9f, 1.0f, 0.06f);
            scene2AreaL1Aura.IsEmissive = true;
            scene2AreaL1Aura.EmissiveIntensity = 0.6f;
            Scene2.AddMesh(scene2AreaL1Aura);

            // Red spot
            var scene2RedSpotL1Gizmo = CreateGizmoFrom("S2_RedSpotGizmo_L1", "SmoothSphere");
            scene2RedSpotL1Gizmo.Position = scene2RedSpotL1.Position;
            scene2RedSpotL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2RedSpotL1Gizmo.Color = new Vector4(1f, 0f, 0f, 1f);
            scene2RedSpotL1Gizmo.IsEmissive = true;
            scene2RedSpotL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2RedSpotL1Gizmo);

            var scene2RedSpotL1Aura = CreateGizmoFrom("S2_RedSpotAura_L1", "SmoothSphere");
            scene2RedSpotL1Aura.Position = scene2RedSpotL1.Position;
            scene2RedSpotL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2RedSpotL1Aura.Color = new Vector4(1f, 0f, 0f, 0.08f);
            scene2RedSpotL1Aura.IsEmissive = true;
            scene2RedSpotL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2RedSpotL1Aura);

            // Green point
            var scene2GreenPointL1Gizmo = CreateGizmoFrom("S2_GreenPointGizmo_L1", "SmoothSphere");
            scene2GreenPointL1Gizmo.Position = scene2GreenPointL1.Position;
            scene2GreenPointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2GreenPointL1Gizmo.Color = new Vector4(0f, 1f, 0f, 1f);
            scene2GreenPointL1Gizmo.IsEmissive = true;
            scene2GreenPointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2GreenPointL1Gizmo);

            var scene2GreenPointL1Aura = CreateGizmoFrom("S2_GreenPointAura_L1", "SmoothSphere");
            scene2GreenPointL1Aura.Position = scene2GreenPointL1.Position;
            scene2GreenPointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2GreenPointL1Aura.Color = new Vector4(0f, 1f, 0f, 0.08f);
            scene2GreenPointL1Aura.IsEmissive = true;
            scene2GreenPointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2GreenPointL1Aura);

            // Purple point
            var scene2PurplePointL1Gizmo = CreateGizmoFrom("S2_PurplePointGizmo_L1", "SmoothSphere");
            scene2PurplePointL1Gizmo.Position = scene2PurplePointL1.Position;
            scene2PurplePointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2PurplePointL1Gizmo.Color = new Vector4(0.6f, 0f, 1f, 1f);
            scene2PurplePointL1Gizmo.IsEmissive = true;
            scene2PurplePointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2PurplePointL1Gizmo);

            var scene2PurplePointL1Aura = CreateGizmoFrom("S2_PurplePointAura_L1", "SmoothSphere");
            scene2PurplePointL1Aura.Position = scene2PurplePointL1.Position;
            scene2PurplePointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2PurplePointL1Aura.Color = new Vector4(0.6f, 0f, 1f, 0.08f);
            scene2PurplePointL1Aura.IsEmissive = true;
            scene2PurplePointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2PurplePointL1Aura);

            // Orange point
            var scene2OrangePointL1Gizmo = CreateGizmoFrom("S2_OrangePointGizmo_L1", "SmoothSphere");
            scene2OrangePointL1Gizmo.Position = scene2OrangePointL1.Position;
            scene2OrangePointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2OrangePointL1Gizmo.Color = new Vector4(1f, 0.4f, 0f, 1f);
            scene2OrangePointL1Gizmo.IsEmissive = true;
            scene2OrangePointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2OrangePointL1Gizmo);

            var scene2OrangePointL1Aura = CreateGizmoFrom("S2_OrangePointAura_L1", "SmoothSphere");
            scene2OrangePointL1Aura.Position = scene2OrangePointL1.Position;
            scene2OrangePointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2OrangePointL1Aura.Color = new Vector4(1f, 0.4f, 0f, 0.08f);
            scene2OrangePointL1Aura.IsEmissive = true;
            scene2OrangePointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2OrangePointL1Aura);

            // Purple area
            var scene2PurpleAreaL1Gizmo = CreateGizmoFrom("S2_PurpleAreaGizmo_L1", "SmoothSphere");
            scene2PurpleAreaL1Gizmo.Position = scene2PurpleAreaL1.Position;
            scene2PurpleAreaL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2PurpleAreaL1Gizmo.Color = new Vector4(0.5f, 0f, 0.8f, 1f);
            scene2PurpleAreaL1Gizmo.IsEmissive = true;
            scene2PurpleAreaL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2PurpleAreaL1Gizmo);

            var scene2PurpleAreaL1Aura = CreateGizmoFrom("S2_PurpleAreaAura_L1", "SmoothSphere");
            scene2PurpleAreaL1Aura.Position = scene2PurpleAreaL1.Position;
            scene2PurpleAreaL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2PurpleAreaL1Aura.Color = new Vector4(0.5f, 0f, 0.8f, 0.08f);
            scene2PurpleAreaL1Aura.IsEmissive = true;
            scene2PurpleAreaL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2PurpleAreaL1Aura);

            // Cyan point
            var scene2CyanPointL1Gizmo = CreateGizmoFrom("S2_CyanPointGizmo_L1", "SmoothSphere");
            scene2CyanPointL1Gizmo.Position = scene2CyanPointL1.Position;
            scene2CyanPointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2CyanPointL1Gizmo.Color = new Vector4(0f, 0.25f, 1f, 1f);
            scene2CyanPointL1Gizmo.IsEmissive = true;
            scene2CyanPointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2CyanPointL1Gizmo);

            var scene2CyanPointL1Aura = CreateGizmoFrom("S2_CyanPointAura_L1", "SmoothSphere");
            scene2CyanPointL1Aura.Position = scene2CyanPointL1.Position;
            scene2CyanPointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2CyanPointL1Aura.Color = new Vector4(0f, 1f, 1f, 0.08f);
            scene2CyanPointL1Aura.IsEmissive = true;
            scene2CyanPointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2CyanPointL1Aura);

            // Deep blue point
            var scene2DeepBluePointL1Gizmo = CreateGizmoFrom("S2_DeepBluePointGizmo_L1", "SmoothSphere");
            scene2DeepBluePointL1Gizmo.Position = scene2DeepBluePointL1.Position;
            scene2DeepBluePointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2DeepBluePointL1Gizmo.Color = new Vector4(0f, 0f, 0.8f, 1f);
            scene2DeepBluePointL1Gizmo.IsEmissive = true;
            scene2DeepBluePointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2DeepBluePointL1Gizmo);

            var scene2DeepBluePointL1Aura = CreateGizmoFrom("S2_DeepBluePointAura_L1", "SmoothSphere");
            scene2DeepBluePointL1Aura.Position = scene2DeepBluePointL1.Position;
            scene2DeepBluePointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2DeepBluePointL1Aura.Color = new Vector4(0f, 0f, 0.8f, 0.08f);
            scene2DeepBluePointL1Aura.IsEmissive = true;
            scene2DeepBluePointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2DeepBluePointL1Aura);

            // Warm yellow point
            var scene2WarmYellowPointL1Gizmo = CreateGizmoFrom("S2_WarmYellowPointGizmo_L1", "SmoothSphere");
            scene2WarmYellowPointL1Gizmo.Position = scene2WarmYellowPointL1.Position;
            scene2WarmYellowPointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2WarmYellowPointL1Gizmo.Color = new Vector4(1f, 0.85f, 0.3f, 1f);
            scene2WarmYellowPointL1Gizmo.IsEmissive = true;
            scene2WarmYellowPointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2WarmYellowPointL1Gizmo);

            var scene2WarmYellowPointL1Aura = CreateGizmoFrom("S2_WarmYellowPointAura_L1", "SmoothSphere");
            scene2WarmYellowPointL1Aura.Position = scene2WarmYellowPointL1.Position;
            scene2WarmYellowPointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2WarmYellowPointL1Aura.Color = new Vector4(1f, 0.85f, 0.3f, 0.08f);
            scene2WarmYellowPointL1Aura.IsEmissive = true;
            scene2WarmYellowPointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2WarmYellowPointL1Aura);

            // Cold white point
            var scene2ColdWhitePointL1Gizmo = CreateGizmoFrom("S2_ColdWhitePointGizmo_L1", "SmoothSphere");
            scene2ColdWhitePointL1Gizmo.Position = scene2ColdWhitePointL1.Position;
            scene2ColdWhitePointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2ColdWhitePointL1Gizmo.Color = new Vector4(0.9f, 0.95f, 1f, 1f);
            scene2ColdWhitePointL1Gizmo.IsEmissive = true;
            scene2ColdWhitePointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2ColdWhitePointL1Gizmo);

            var scene2ColdWhitePointL1Aura = CreateGizmoFrom("S2_ColdWhitePointAura_L1", "SmoothSphere");
            scene2ColdWhitePointL1Aura.Position = scene2ColdWhitePointL1.Position;
            scene2ColdWhitePointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2ColdWhitePointL1Aura.Color = new Vector4(0.9f, 0.95f, 1f, 0.08f);
            scene2ColdWhitePointL1Aura.IsEmissive = true;
            scene2ColdWhitePointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2ColdWhitePointL1Aura);

            // Sickly green point
            var scene2SicklyGreenPointL1Gizmo = CreateGizmoFrom("S2_SicklyGreenPointGizmo_L1", "SmoothSphere");
            scene2SicklyGreenPointL1Gizmo.Position = scene2SicklyGreenPointL1.Position;
            scene2SicklyGreenPointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2SicklyGreenPointL1Gizmo.Color = new Vector4(0.6f, 0.9f, 0f, 1f);
            scene2SicklyGreenPointL1Gizmo.IsEmissive = true;
            scene2SicklyGreenPointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2SicklyGreenPointL1Gizmo);

            var scene2SicklyGreenPointL1Aura = CreateGizmoFrom("S2_SicklyGreenPointAura_L1", "SmoothSphere");
            scene2SicklyGreenPointL1Aura.Position = scene2SicklyGreenPointL1.Position;
            scene2SicklyGreenPointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2SicklyGreenPointL1Aura.Color = new Vector4(0.6f, 0.9f, 0f, 0.08f);
            scene2SicklyGreenPointL1Aura.IsEmissive = true;
            scene2SicklyGreenPointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2SicklyGreenPointL1Aura);

            // Deep red point
            var scene2DeepRedPointL1Gizmo = CreateGizmoFrom("S2_DeepRedPointGizmo_L1", "SmoothSphere");
            scene2DeepRedPointL1Gizmo.Position = scene2DeepRedPointL1.Position;
            scene2DeepRedPointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2DeepRedPointL1Gizmo.Color = new Vector4(0.7f, 0f, 0f, 1f);
            scene2DeepRedPointL1Gizmo.IsEmissive = true;
            scene2DeepRedPointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2DeepRedPointL1Gizmo);

            var scene2DeepRedPointL1Aura = CreateGizmoFrom("S2_DeepRedPointAura_L1", "SmoothSphere");
            scene2DeepRedPointL1Aura.Position = scene2DeepRedPointL1.Position;
            scene2DeepRedPointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2DeepRedPointL1Aura.Color = new Vector4(0.7f, 0f, 0f, 0.08f);
            scene2DeepRedPointL1Aura.IsEmissive = true;
            scene2DeepRedPointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2DeepRedPointL1Aura);

            // Pink point
            var scene2PinkPointL1Gizmo = CreateGizmoFrom("S2_PinkPointGizmo_L1", "SmoothSphere");
            scene2PinkPointL1Gizmo.Position = scene2PinkPointL1.Position;
            scene2PinkPointL1Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene2PinkPointL1Gizmo.Color = new Vector4(1f, 0.4f, 0.7f, 1f);
            scene2PinkPointL1Gizmo.IsEmissive = true;
            scene2PinkPointL1Gizmo.EmissiveIntensity = 2.0f;
            Scene2.AddMesh(scene2PinkPointL1Gizmo);

            var scene2PinkPointL1Aura = CreateGizmoFrom("S2_PinkPointAura_L1", "SmoothSphere");
            scene2PinkPointL1Aura.Position = scene2PinkPointL1.Position;
            scene2PinkPointL1Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene2PinkPointL1Aura.Color = new Vector4(1f, 0.4f, 0.7f, 0.08f);
            scene2PinkPointL1Aura.IsEmissive = true;
            scene2PinkPointL1Aura.EmissiveIntensity = 0.8f;
            Scene2.AddMesh(scene2PinkPointL1Aura);




            // ── Skysphere ────────────────────────────────────────────────────────
            // Large inverted sphere, always camera-centered, rendered emissive
            // so it skips all lighting calculations.
            // MaterialTextures[0] = day panorama, MaterialTextures[1] = night panorama
            var skySphere = CreateGizmoFrom("SkySphere", "FBXCube");
            skySphere.Name = "SkySphere";
            skySphere.Position = new Vector3(0f, 0f, 0f);
            skySphere.Size = new Vector3(100f, 100f, 100f);
            skySphere.Color = new Vector4(1f, 1f, 1f, 1f);
            skySphere.IsEmissive = true;
            skySphere.Rotation = new Vector3(0f, 0f, 0f);
            skySphere.EmissiveIntensity = 1.0f;
            skySphere.MaterialTextures.Add("/iAssets/SkyCubeMap012.png");
            skySphere.MaterialTextures.Add("/iAssets/StarsCubeMap015.png");
            Scene2.AddMesh(skySphere);

           


            // MESH BREAK

            /*
            // Red cube
            var cube = MeshLibrary.GetMesh("PrimCube");
            if (cube != null)
            {
                cube.Name = "Scene2Cube";
                cube.Position = new Vector3(54, -74, 5);
                cube.Size = new Vector3(1f, 1f, 1f);
                cube.Color = new Vector4(1f, 0f, 0f, 1f);
                Scene2.AddMesh(cube);
            }
            */
            /*
            var cube88 = MeshLibrary.GetMesh("FBXCubeRed");
            if (cube88 != null)
            {
                if (cube88 is SpectralXMesh smStorage) smStorage.JSSourceMesh = "Lightcube";
                cube88.Position = new Vector3(54, -74, 20);
                cube88.Size = new Vector3(1f, 1f, 1f);
                // cube2.Color = new Vector4(1f, 0f, 0f, 1f);
                Scene.AddMesh(cube88);
            }
            */


            // Props
            /*
            var bush = MeshLibrary.GetMesh("Bush001");
       
            if (bush != null)
            {
                if (bush is SpectralXMesh smBush) smBush.JSSourceMesh = "Bush001";

                bush.Name = "Scene2Bush";
                bush.Position = new Vector3(-5, -15, 1);
                bush.Size = new Vector3(1f, 1f, 1f);
                //    bush.Color = new Vector4(1f, 0f, 0f, 1f);
                bush.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);    // X +90
                Scene2.AddMesh(bush);
            }

            var rock = MeshLibrary.GetMesh("Rock001");
            if (rock != null)
            {
                if (rock is SpectralXMesh smRock) smRock.JSSourceMesh = "Rock001";  // ADD
                rock.Name = "Scene2Rock";
                rock.Position = new Vector3(16, 16, 0);
                rock.Size = new Vector3(1f, 1f, 1f);
                //  rock.Color = new Vector4(1f, 0f, 0f, 1f);

                Scene2.AddMesh(rock);
            }

            var tree = MeshLibrary.GetMesh("Tree001");
            if (tree != null)
            {
                if (tree is SpectralXMesh smTree) smTree.JSSourceMesh = "Tree001";  // ADD
                tree.Name = "Scene2Tree";
                tree.Position = new Vector3(32,32, 0);
                tree.Size = new Vector3(1f, 1f, 1f);
                //   bush.Color = new Vector4(1f, 0f, 0f, 1f);
                Scene2.AddMesh(tree);
            }
            */
            // Buildings

            var house = MeshLibrary.GetMesh("House001");
            if (house != null)
            {
                if (house is SpectralXMesh smHouse) smHouse.JSSourceMesh = "House001";
                house.Name = "Scene2House";
                house.Position = new Vector3(64, -64, 0);
                house.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house);
                _propScatter.RegisterFootprint(house.Position.X, house.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house));
            }

            var house3 = MeshLibrary.GetMesh("Well001");
            if (house3 != null)
            {
                if (house3 is SpectralXMesh smWell) smWell.JSSourceMesh = "Well001";  // ADD
                house3.Name = "Scene2Well";
                house3.Position = new Vector3(34, -64, 0);
                house3.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);
                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house3.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house3);
                _propScatter.RegisterFootprint(house3.Position.X, house3.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house3));
            }

            var house1 = MeshLibrary.GetMesh("Stable001");
            if (house1 != null)
            {
                if (house1 is SpectralXMesh smStable) smStable.JSSourceMesh = "Stable001";  // ADD
                house1.Name = "Scene2Stable";
                house1.Position = new Vector3(0, 0, 0);
                house1.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);
                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                // house1.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house1);
                _propScatter.RegisterFootprint(house1.Position.X, house1.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house1));
            }

            var house4 = MeshLibrary.GetMesh("Blacksmith001");
            if (house4 != null)
            {
                if (house4 is SpectralXMesh smSmith) smSmith.JSSourceMesh = "Blacksmith001";  // ADD
                house4.Name = "Scene2smith";
                house4.Position = new Vector3(34, -32, 0);
                house4.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);
                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house4.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house4);
                _propScatter.RegisterFootprint(house4.Position.X, house4.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house4));
            }


     


            var house5 = MeshLibrary.GetMesh("Storage001");
            if (house5 != null)
            {
                if (house5 is SpectralXMesh smStorage) smStorage.JSSourceMesh = "Storage001";  // ADD
                house5.Name = "Scene2storage";
                house5.Position = new Vector3(54, -64, 0);
                house5.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);
                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house5.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house5);
                _propScatter.RegisterFootprint(house5.Position.X, house5.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house5));
            }

            var house7 = MeshLibrary.GetMesh("House005");
            if (house7 != null)
            {
                if (house7 is SpectralXMesh smHouse7) smHouse7.JSSourceMesh = "House005";  // ADD
                house7.Name = "Scene2House5";
                house7.Position = new Vector3(-10, -64, 0);
                house7.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house7.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house7);
                _propScatter.RegisterFootprint(house7.Position.X, house7.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house7));
            }

            var house8 = MeshLibrary.GetMesh("Mill001");
            if (house8 != null)
            {
                if (house8 is SpectralXMesh smMill) smMill.JSSourceMesh = "Mill001";  // ADD
                house8.Name = "Scene2mill";
                house8.Position = new Vector3(-34, -64, 0);
                house8.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house8.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house8);
                _propScatter.RegisterFootprint(house8.Position.X, house8.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house8));
            }

            var house9 = MeshLibrary.GetMesh("Market001");
            if (house9 != null)
            {
                if (house9 is SpectralXMesh smMarket) smMarket.JSSourceMesh = "Market001";  // ADD
                house9.Name = "Scene2market";
                house9.Position = new Vector3(16, -64, 0);
                house9.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house9.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house9);
                _propScatter.RegisterFootprint(house9.Position.X, house9.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house9));
            }

            var house6 = MeshLibrary.GetMesh("Temple001");
            if (house6 != null)
            {
                if (house6 is SpectralXMesh smTemple) smTemple.JSSourceMesh = "Temple001";  // ADD
                house6.Name = "Scene2Temple";
                house6.Position = new Vector3(-64, 64, 0);
                house6.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house6.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house6);
                _propScatter.RegisterFootprint(house6.Position.X, house6.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house6));
            }


            var house10 = MeshLibrary.GetMesh("House003");
            if (house10 != null)
            {
                if (house10 is SpectralXMesh smHouse10) smHouse10.JSSourceMesh = "House003";  // ADD
                house10.Name = "Scene2House003";
                house10.Position = new Vector3(44, -84, 0);
                house10.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house10.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house10);
                _propScatter.RegisterFootprint(house10.Position.X, house10.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house10));
            }



            var house11 = MeshLibrary.GetMesh("House006");
            if (house11 != null)
            {
                if (house11 is SpectralXMesh smHouse11) smHouse11.JSSourceMesh = "House006";  // ADD
                house11.Name = "Scene2House006";
                house11.Position = new Vector3(24, -84, 0);
                house11.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house11.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house11);
            }

            var house12 = MeshLibrary.GetMesh("SawMill001");
            if (house12 != null)
            {
                if (house12 is SpectralXMesh smHouse12) smHouse12.JSSourceMesh = "SawMill001";  // ADD
                house12.Name = "Scene2SawMill001";
                house12.Position = new Vector3(-34, -84, 0);
                house12.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house12.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house12);
                _propScatter.RegisterFootprint(house12.Position.X, house12.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house12));
            }

            var house13 = MeshLibrary.GetMesh("Inn001");
            if (house13 != null)
            {
                if (house13 is SpectralXMesh smHouse13) smHouse13.JSSourceMesh = "Inn001";  // ADD
                house13.Name = "Scene2Inn001";
                house13.Position = new Vector3(4, -32, 0);
                house13.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house13.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house13);
                _propScatter.RegisterFootprint(house13.Position.X, house13.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house13));
            }

            var house14 = MeshLibrary.GetMesh("BellTower001");
            if (house14 != null)
            {
                if (house14 is SpectralXMesh smHouse14) smHouse14.JSSourceMesh = "BellTower001";  // ADD
                house14.Name = "Scene2BellTower001";
                house14.Position = new Vector3(54, -32, 0);
                house14.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house14.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house14);
                _propScatter.RegisterFootprint(house14.Position.X, house14.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house14));
            }


            var house15 = MeshLibrary.GetMesh("CastleWall001");
            if (house15 != null)
            {
                if (house15 is SpectralXMesh smHouse15) smHouse15.JSSourceMesh = "CastleWall001";  // ADD
                house15.Name = "Scene2CastleWall001";
                house15.Position = new Vector3(-64, 64, 0);
                house15.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house15.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house15);
                _propScatter.RegisterFootprint(house15.Position.X, house15.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house15));
            }

            var house16 = MeshLibrary.GetMesh("Crypt001");
            if (house16 != null)
            {
                if (house16 is SpectralXMesh smHouse16) smHouse16.JSSourceMesh = "Crypt001";  // ADD
                house16.Name = "Scene2Crypt001";
                house16.Position = new Vector3(0, 64, 0);
                house16.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house16.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house16);
                _propScatter.RegisterFootprint(house16.Position.X, house16.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house16));
            }

            var house17 = MeshLibrary.GetMesh("Shack001");
            if (house17 != null)
            {
                if (house17 is SpectralXMesh smHouse17) smHouse17.JSSourceMesh = "Shack001";  // ADD
                house17.Name = "Scene2Shack001";
                house17.Position = new Vector3(128, 128, 0);
                house17.Size = new Vector3(1f, 1f, 1f);
                // bush.Color = new Vector4(1f, 0f, 0f, 1f);

                //  house.Rotation += new Vector3(MathF.PI, 0f, 0f);          // X 180
                house17.Rotation += new Vector3(0f, 0f, MathF.PI);          // Z 180
                Scene2.AddMesh(house17);
                _propScatter.RegisterFootprint(house17.Position.X, house17.Position.Y,
       SpectralXPropScatter.DeriveFootprintRadius(house17));
            }
            // Blue portal square — return to Scene 1
            var portal = MeshLibrary.GetMesh("PrimSquare");
            if (portal != null)
            {
                portal.Name = "Scene2Portal";
                portal.Position = new Vector3(64, -54, 2);
                portal.Size = new Vector3(1f, 1f, 1f);
                //   portal.Color = new Vector4(0f, 0f, 1f, 0.5f);
                portal.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);
                Scene2.AddMesh(portal);
            }


            // Test Floor
            /*
            var portal2 = MeshLibrary.GetMesh("PrimSquare");
            if (portal2 != null)
            {
                portal2.Name = "Scene2floor";
                portal2.Position = new Vector3(0, 0, 0);
                portal2.Size = new Vector3(3f, 3f, 3f);
                portal2.Color = new Vector4(1f, 1f, 1f, 1f);
             //   portal2.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);
                Scene2.AddMesh(portal2);
            }
            */


            if (portal is SpectralXMesh portalMesh)
            {
                portalMesh.IsAnimated = true;
                portalMesh.FrameCount = 10;
                portalMesh.FrameRate = 10f;
                portalMesh.SheetWidth = 840f;
                portalMesh.SheetHeight = 84f;
                portalMesh.FramePixelWidth = 84f;
                portalMesh.FramePixelHeight = 84f;
                portalMesh.TextureDataUrl = "iAssets/PortalSheet001.png";
                portalMesh.TextureIsRawRGBA = false;
            }

            // Pre-warm particle textures — one sentinel mesh per texture type
            // Uploaded frame 1, parked offscreen, never moved again
            // Texture pre-cache — uploads texture to GPU without adding geometry to scene
            // Particles need these ready before first spawn, zero frame cost after


         



          


            TileMap = new SpectralXLandTileMap();
            TileMap.SetGridSize(512);
            TileMap.CustomTexturePaths = new[]
            {
    "/iAssets/DirtTile002.png",
    "/iAssets/RockTile002.png",
    "/iAssets/GrassTile011.png",
    "/iAssets/SnowTile012.png",
    "/iAssets/WaterTile002.png",
    "/iAssets/IceTile002.png",
};
            // ── Normal Maps ───────────────────────────────────────────────────────
            TileMap.CustomNormalMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileNormal001.png)
    null,   // slot 1 - Rock    (add RockTileNormal001.png)
    null,   // slot 2 - Grass   (add GrassTileNormal001.png)
    null,   // slot 3 - Snow    (add SnowTileNormal001.png)
    null,   // slot 4 - Water   (add WaterTileNormal001.png)
    null,   // slot 5 - Ice     (add IceTileNormal001.png)
            };
            // ── Specular Maps ─────────────────────────────────────────────────────
            TileMap.CustomSpecularMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileSpecular001.png)
    null,   // slot 1 - Rock    (add RockTileSpecular001.png)
    null,   // slot 2 - Grass   (add GrassTileSpecular001.png)
    null,   // slot 3 - Snow    (add SnowTileSpecular001.png)
    null,   // slot 4 - Water   (add WaterTileSpecular001.png)
    null,   // slot 5 - Ice     (add IceTileSpecular001.png)
            };
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
            _tilePBRUploaded = false;   // ← add this line
            _ = LoadLandscape();

            // ── Spawn / Portal exclusion zones — prevent foliage blocking camera entry/exit ──
            _propScatter.RegisterFootprint(54f, -54f, 5f);   // camera spawn
            _propScatter.RegisterFootprint(64f, -54f, 5f);   // portal exit
            // ── Prop Scatter ─────────────────────────────────────────────
            // ── Foliage Scatter — instanced rendering, no scene mesh entries ──────────
            var scatterConfigs = new[]
            {
    new PropScatterConfig("Bush001",  50),
    new PropScatterConfig("Rock002",  50),
    new PropScatterConfig("Tree001",  25),
    new PropScatterConfig("Grass001", 200),
};

            foreach (var config in scatterConfigs)
                _foliageGroups.Add(_propScatter.Scatter(config));

            // ── Graveyard Grid ────────────────────────────────────────────────────────
            var graveyardConfigs = new[]
            {
    new GridBoundedScatterConfig("Grave001",  20, -76f, -52f, 52f, 76f),
    new GridBoundedScatterConfig("GraveS001", 20, -76f, -52f, 52f, 76f),
};

            foreach (var config in graveyardConfigs)
                _foliageGroups.Add(_propScatter.ScatterInGrid(config));



        }











    }
}
