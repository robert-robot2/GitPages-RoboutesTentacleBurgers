using SpectralXGLX.SpectralGL.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpectralXGLX.SpectralXComponent
{

    public partial class SpectralXEngine
    {
        // At the top of the class (with other private fields):
        // At the top of the class (with other private fields):

        private List<CubeMazeCell> _cubeMazeCells = new();
        private float _mazeExplodeTimer = 0f;
        private bool _mazeExploding = false;
        private float _mazeReformTimer = 0f;
        private const float ExplodeInterval = 8f;
        private const float ExplodeDuration = 1.2f;
        private const float ReformDuration = 1.5f;

        private int _currentMazeEffect = 0;       // which effect is active
        private bool _mazeEffectActive = false;
        private float _mazeEffectTimer = 0f;
        private float _mazeEffectDuration = 0f;
        private float _mazeNextEffectTimer = 8f;  // countdown to next effect
        private Vector3[]? _effectHomeSnapshot;

        private class CubeMazeCell
        {
            public SpectralXMesh? Mesh;
            public Vector3 HomePos;
            public Vector3 Velocity;
            public Vector3 ExplodeVel;
            public float BumpTimer;
            public float BumpDuration;
            public Vector3 BumpVelocity;
            public bool Bumped;
            public Vector3 EffectTargetPos;   // used by formation/spaceship effects
            public float EffectPhase;         // per-cell phase offset for wave effects
            public bool IsVoid;               // true for "void cube" invaders (effect 2)
        }



        public void InitScene3()
        {

            
            _propScatter.Reset();
            _foliageGroups.Clear();

            // The Shadow Knows...
            // Initialize scene shadow settings — SpectralXS modes read from this
            Shadow = new SpectralXShadow();

            Shadow.SoftnessBias = 0.008f;   // tighter than before
            Shadow.KernelSize = 3.0f;       // controls overall disk radius
            Shadow.DepthBias = 0.003f;      // reduce acne without over-softening
            Shadow.ContactSharpness = 0.0005f; // decrease for sharper contact shadows
            Shadow.TintR = 0.2f;               // add a warm tint to shadows
            Shadow.TintStrength = 0.3f;        // how strong the tint is
            Shadow.PenumbraTintStrength = 0.4f;  // how much light color bleeds into penumbra edges


            Weather = new SpectralXWeatherClass();
            Weather.Init(Scene3, MeshLibrary, new Dictionary<WeatherParticleType, ParticleVolume>
{
  // Rain volume centered on Roboute text mesh
// X: wide enough to cover full text width left to right
// Y: shallow depth just in front of the text
// Z: starts above text at Z 13, falls down through and below to Z 8
{ WeatherParticleType.Rain, new ParticleVolume(
    -18f, 18f,   // X — covers full text width
    -2f,  2f,    // Y — thin slab just in front of mesh
    8f,   13f    // Z — above text down through it
)},
    { WeatherParticleType.Snow,      new ParticleVolume(-15f, 15f, -15f, 15f,  -50f, 20f) },
    { WeatherParticleType.Cloud,     new ParticleVolume(-15f, 15f, -15f, 15f,  -50f, 20f) },
    { WeatherParticleType.Lightning, new ParticleVolume(-15f, 15f, -15f, 15f,  -50f, 20f) },


});


            // ── Scene 3 Lighting ────────────────────────────────────────────────────────

            // White point light
            var scene3PointL1 = new SpectralXLight(
                position: new Vector3(-10, -2, 8.5f),
                color: new Vector3(1f, 1f, 1f),
                intensity: 4.0f,
                range: 15f);
            scene3PointL1.Type = LightType.Point;
            scene3PointL1.CastsShadows = true;
            Scene3.AddLight(scene3PointL1);

            // Blue point light
            var scene3PointL2 = new SpectralXLight(
                position: new Vector3(-5, -2, 9),
                color: new Vector3(0f, 0.4f, 1f),
                intensity: 2.0f,
                range: 15f);
            scene3PointL2.Type = LightType.Point;
            scene3PointL2.CastsShadows = true;
            Scene3.AddLight(scene3PointL2);

            // Purple point light
            var scene3PointL3 = new SpectralXLight(
                position: new Vector3(5, -2, 9),
                color: new Vector3(0.6f, 0f, 1f),
                intensity: 2.0f,
                range: 15f);
            scene3PointL3.Type = LightType.Point;
            scene3PointL3.CastsShadows = true;
            Scene3.AddLight(scene3PointL3);

            // Sun directional light — added last
            _sunLight = new SpectralXLight(
                position: new Vector3(0f, -25f, 50f),
                color: new Vector3(1f, 0.98f, 0.90f),
                intensity: 5.0f,
                range: 200f);
            _sunLight.Type = LightType.Directional;
            _sunLight.Direction = new Vector3(0f, -0.5f, -1f);
            _sunLight.CastsShadows = true;
            _sunLight.Enabled = true;
            Sun.Apply(_sunLight);
            Scene3.AddLight(_sunLight);

            // ── Scene 3 Light Gizmos ─────────────────────────────────────────────────────

            // White point L1
            var scene3L1Gizmo = CreateGizmoFrom("S3_LightGizmo_L1", "LightBulb");
            scene3L1Gizmo.Position = scene3PointL1.Position;
            scene3L1Gizmo.Size = new Vector3(0.3f, 0.3f, 0.3f);
            scene3L1Gizmo.Color = new Vector4(1f, 0.98f, 0.85f, 0.4f);
            scene3L1Gizmo.IsEmissive = false;
            scene3L1Gizmo.CastsShadow = false;
            scene3L1Gizmo.EmissiveIntensity = 0.8f;
            Scene3.AddMesh(scene3L1Gizmo);

            var scene3L1Core = CreateGizmoFrom("S3_LightCore_L1", "SmoothSphere");
            scene3L1Core.Position = scene3PointL1.Position;
            scene3L1Core.Size = new Vector3(0.08f, 0.08f, 0.08f);
            scene3L1Core.Color = new Vector4(1f, 0.95f, 0.6f, 1f);
            scene3L1Core.IsEmissive = true;
            scene3L1Core.CastsShadow = false;
            scene3L1Core.EmissiveIntensity = 3.0f;
            Scene3.AddMesh(scene3L1Core);

            var scene3L1AuraInner = CreateGizmoFrom("S3_LightAuraInner_L1", "SmoothSphere");
            scene3L1AuraInner.Position = scene3PointL1.Position;
            scene3L1AuraInner.Size = new Vector3(0.35f, 0.35f, 0.35f);
            scene3L1AuraInner.Color = new Vector4(1f, 0.85f, 0.4f, 0.12f);
            scene3L1AuraInner.IsEmissive = true;
            scene3L1AuraInner.CastsShadow = false;
            scene3L1AuraInner.EmissiveIntensity = 1.2f;
            Scene3.AddMesh(scene3L1AuraInner);

            var scene3L1AuraOuter = CreateGizmoFrom("S3_LightAuraOuter_L1", "SmoothSphere");
            scene3L1AuraOuter.Position = scene3PointL1.Position;
            scene3L1AuraOuter.Size = new Vector3(0.6f, 0.6f, 0.6f);
            scene3L1AuraOuter.Color = new Vector4(1f, 0.75f, 0.3f, 0.05f);
            scene3L1AuraOuter.IsEmissive = true;
            scene3L1AuraOuter.CastsShadow = false;
            scene3L1AuraOuter.EmissiveIntensity = 0.6f;
            Scene3.AddMesh(scene3L1AuraOuter);

            // Blue point L2
            var scene3L2Gizmo = CreateGizmoFrom("S3_LightGizmo_L2", "SmoothSphere");
            scene3L2Gizmo.Position = scene3PointL2.Position;
            scene3L2Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene3L2Gizmo.Color = new Vector4(0f, 0.4f, 1f, 1f);
            scene3L2Gizmo.IsEmissive = true;
            scene3L2Gizmo.CastsShadow = false;
            scene3L2Gizmo.EmissiveIntensity = 2.0f;
            Scene3.AddMesh(scene3L2Gizmo);

            var scene3L2Aura = CreateGizmoFrom("S3_LightAura_L2", "SmoothSphere");
            scene3L2Aura.Position = scene3PointL2.Position;
            scene3L2Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene3L2Aura.Color = new Vector4(0f, 0.4f, 1f, 0.08f);
            scene3L2Aura.IsEmissive = true;
            scene3L2Aura.CastsShadow = false;
            scene3L2Aura.EmissiveIntensity = 0.8f;
            Scene3.AddMesh(scene3L2Aura);

            // Purple point L3
            var scene3L3Gizmo = CreateGizmoFrom("S3_LightGizmo_L3", "SmoothSphere");
            scene3L3Gizmo.Position = scene3PointL3.Position;
            scene3L3Gizmo.Size = new Vector3(0.2f, 0.2f, 0.2f);
            scene3L3Gizmo.Color = new Vector4(0.6f, 0f, 1f, 1f);
            scene3L3Gizmo.IsEmissive = true;
            scene3L3Gizmo.CastsShadow = false;
            scene3L3Gizmo.EmissiveIntensity = 2.0f;
            Scene3.AddMesh(scene3L3Gizmo);

            var scene3L3Aura = CreateGizmoFrom("S3_LightAura_L3", "SmoothSphere");
            scene3L3Aura.Position = scene3PointL3.Position;
            scene3L3Aura.Size = new Vector3(0.5f, 0.5f, 0.5f);
            scene3L3Aura.Color = new Vector4(0.6f, 0f, 1f, 0.08f);
            scene3L3Aura.IsEmissive = true;
            scene3L3Aura.CastsShadow = false;
            scene3L3Aura.EmissiveIntensity = 0.8f;
            Scene3.AddMesh(scene3L3Aura);


            //  InitCubeCityBuilder();



            // ── Skysphere ────────────────────────────────────────────────────────
            // Large inverted sphere, always camera-centered, rendered emissive
            // so it skips all lighting calculations.
            // MaterialTextures[0] = day panorama, MaterialTextures[1] = night panorama
            // WITH:
            // ── Skysphere for Home Page ─────────────────────────────────────────────────────
            var skySphere2 = CreateGizmoFrom("SkySphere", "FBXCube");
            skySphere2.Name = "SkySphere";                    // ← THIS WAS MISSING
            skySphere2.Position = new Vector3(0, 0, 0f);
            skySphere2.Size = new Vector3(100f, 100f, 100f);
            skySphere2.Color = new Vector4(1f, 1f, 1f, 1f);
            skySphere2.Rotation = new Vector3(0f, 0f, 0f);
            skySphere2.IsEmissive = true;
            skySphere2.EmissiveIntensity = 1.0f;
            skySphere2.MaterialTextures.Clear();
            skySphere2.MaterialTextures.Add("/iAssets/SkyCubeMap012.png");
            skySphere2.MaterialTextures.Add("/iAssets/StarsCubeMap015.png");
            skySphere2.TextureDirty = true;

            Scene3.AddMesh(skySphere2);

            Console.WriteLine("[InitScene3] SkySphere created for Home page");

         
            

            // Mesh Break


            // In InitScene3(), replace the roboute block with:
            var roboute = MeshLibrary.GetMesh("Roboute");
            if (roboute != null)
            {
                if (roboute is SpectralXMesh smRoboute) smRoboute.JSSourceMesh = "Roboute";
                Scene3.AddMesh(roboute);
                roboute.Position = new Vector3(-16, 0, 11);
                roboute.Size = new Vector3(2f, 2f, 2f);
                roboute.Color = new Vector4(0f, 0f, 0.75f, 0.99f);
                roboute.Rotation += new Vector3(MathF.PI / 2f, 0f, 0f);
              //  roboute.IsEmissive = true;
              //  roboute.EmissiveIntensity = 3f;
              //  roboute.GlowRadius = 0.5f;
            //    roboute.GlowStrength = 0.4f;
             //   roboute.GlowColor = new Vector4(0f, 0f, 1f, 1f);   // pure blue glow tint
                roboute.ShadowBlur = 0.04f;
                roboute.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);

            }

            // ── Font Registration ────────────────────────────────────────────────────
            MeshLibrary.RegisterFont("Diablo",
                "/iAssets/Fonts/DiabloAtlas.json",
                "/iAssets/Fonts/DiabloAtlas.png");




            // ── Tavern Tagline ───────────────────────────────────────────────────────
            // ── Tavern Tagline ───────────────────────────────────────────────────────
            var welcomeText = AddText("Welcome to the Tavern of The Rising Sun!",
                position: new Vector3(-3f, 0f, 8f),
                fontSize: 0.5f,
                fontKey: "Diablo",

                // Bright icy blue like your header text
                color: new Vector4(0.514f, 0.933f, 1f, 1f), // #83EEFF

                align: TextAlignment.Center);

            welcomeText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);

            // Keep the glow soft
            welcomeText.GlowRadius = 0.03f;
            welcomeText.GlowStrength = 0.45f;
            welcomeText.EmissiveIntensity = 0.55f;

            // WHITE outer glow/shadow
            welcomeText.ShadowBlur = 0.08f;
            welcomeText.ShadowColor = new Vector4(1f, 1f, 1f, 0.55f);

            // Deep blue inner glow tint
            welcomeText.GlowColor = new Vector4(0f, 0.25f, 1f, 1f);

            welcomeText.GlowPulseSpeed = 1.5f;
            welcomeText.GlowPulseMin = 0.03f;
            welcomeText.GlowPulseMax = 0.18f;

            var warningTitle2 = AddText(" Where your nightmares are dreams and your dreams are nightmares!",
         position: new Vector3(-3f, 0f, 7f),
         fontSize: 0.5f, fontKey: "Diablo",
         color: new Vector4(1f, 0.2f, 0.2f, 1f),
         align: TextAlignment.Center);
            warningTitle2.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningTitle2.GlowRadius = 0.02f;
            warningTitle2.GlowStrength = 0.4f;
            warningTitle2.GlowColor = new Vector4(1f, 0f, 0f, 0.8f);
            warningTitle2.ShadowBlur = 0.04f;
            warningTitle2.ShadowColor = new Vector4(1f, 0f, 0f, 0.3f);
            warningTitle2.GlowPulseSpeed = 2.0f;
            warningTitle2.GlowPulseMin = 0.01f;
            warningTitle2.GlowPulseMax = 0.06f;


    

            // ── Portfolio Header ─────────────────────────────────────────────────────
            var nameText2 = AddText("Press Four for Webgl Menu",
                position: new Vector3(-3f, 0f, 5f),
                fontSize: 0.5f,
                      fontKey: "Diablo",
                            color: new Vector4(0.514f, 0.933f, 1f, 1f),  // #83EEFF
                align: TextAlignment.Center);
            nameText2.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            nameText2.GlowRadius = 0.02f;
            nameText2.GlowStrength = 0.4f;
            nameText2.GlowColor = new Vector4(0f, 0f, 1f, 1f);   // pure blue glow tint
            nameText2.ShadowBlur = 0.04f;
            nameText2.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);


            var warningTitle6 = AddText("PhotoSensitivity Warning!!!",
   position: new Vector3(-3f, 0f, 3f),
   fontSize: 1.0f, fontKey: "Diablo",
   color: new Vector4(1f, 0.2f, 0.2f, 1f),
   align: TextAlignment.Center);
            warningTitle6.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningTitle6.GlowRadius = 0.02f;
            warningTitle6.GlowStrength = 0.4f;
            warningTitle6.GlowColor = new Vector4(1f, 0f, 0f, 0.8f);
            warningTitle6.ShadowBlur = 0.04f;
            warningTitle6.ShadowColor = new Vector4(1f, 0f, 0f, 0.3f);
            warningTitle6.GlowPulseSpeed = 2.0f;
            warningTitle6.GlowPulseMin = 0.01f;
            warningTitle6.GlowPulseMax = 0.06f;

            // ── Portfolio Header ─────────────────────────────────────────────────────
            var nameText = AddText("Robert Chilewski",
                position: new Vector3(-3f, 0f, 1f),
                fontSize: 1.2f,
                      fontKey: "Diablo",
           color: new Vector4(0.514f, 0.933f, 1f, 1f),  // #83EEFF
                align: TextAlignment.Center);
            nameText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            nameText.GlowRadius = 0.02f;
            nameText.GlowStrength = 0.4f;
            nameText.GlowColor = new Vector4(0f, 0f, 1f, 1f);   // pure blue glow tint
            nameText.ShadowBlur = 0.04f;
            nameText.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);


            // ── Personality Line ─────────────────────────────────────────────────────
            var personalityText = AddText("Temple University Chemistry and Electrical Engineering",
                position: new Vector3(-3f, 0f, 0f),
                fontSize: 0.7f,
                fontKey: "Diablo",
                                   color: new Vector4(0.514f, 0.933f, 1f, 1f),  // #83EEFF
                align: TextAlignment.Center);
            personalityText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            personalityText.GlowRadius = 0.02f;
            personalityText.GlowStrength = 0.4f;
            personalityText.GlowColor = new Vector4(0f, 0f, 1f, 1f);   // pure blue glow tint
            personalityText.ShadowBlur = 0.04f;
            personalityText.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);

            var warningTitle5 = AddText("Their is no God to Save your soul in this matrix Human, Prepare Yourself!",
   position: new Vector3(-3f, 0f, -1f),
   fontSize: 1.0f, fontKey: "Diablo",
   color: new Vector4(1f, 0.2f, 0.2f, 1f),
   align: TextAlignment.Center);
            warningTitle5.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningTitle5.GlowRadius = 0.02f;
            warningTitle5.GlowStrength = 0.4f;
            warningTitle5.GlowColor = new Vector4(1f, 0f, 0f, 0.8f);
            warningTitle5.ShadowBlur = 0.04f;
            warningTitle5.ShadowColor = new Vector4(1f, 0f, 0f, 0.3f);
            warningTitle5.GlowPulseSpeed = 2.0f;
            warningTitle5.GlowPulseMin = 0.01f;
            warningTitle5.GlowPulseMax = 0.06f;


            // ── Warnings Section ─────────────────────────────────────────────────────

            var warningTitle = AddText("! Adult Content Warning 18+ !",
                position: new Vector3(-3f, 0f, -3f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.2f, 0.2f, 1f),
                align: TextAlignment.Center);
            warningTitle.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningTitle.GlowRadius = 0.02f;
            warningTitle.GlowStrength = 0.4f;
            warningTitle.GlowColor = new Vector4(1f, 0f, 0f, 0.8f);
            warningTitle.ShadowBlur = 0.04f;
            warningTitle.ShadowColor = new Vector4(1f, 0f, 0f, 0.3f);
            warningTitle.GlowPulseSpeed = 2.0f;
            warningTitle.GlowPulseMin = 0.01f;
            warningTitle.GlowPulseMax = 0.06f;

            var warningText1 = AddText("Mature themes and strong language inspired by classic Diablo-style games.",
                position: new Vector3(-3f, 0f, -9.5f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.3f, 0.3f, 0.9f),
                align: TextAlignment.Center);
            warningText1.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningText1.GlowRadius = 0.01f;
            warningText1.GlowStrength = 0.2f;

            var warningText2 = AddText("No real-life references aside from city and country names.",
                position: new Vector3(-3f, 0f, -10.5f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.3f, 0.3f, 0.9f),
                align: TextAlignment.Center);
            warningText2.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningText2.GlowRadius = 0.01f;
            warningText2.GlowStrength = 0.2f;

            var warningText3 = AddText("All representations are virtual recreations set in a fictional timeline.",
                position: new Vector3(-3f, 0f, -11.5f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.3f, 0.3f, 0.9f),
                align: TextAlignment.Center);
            warningText3.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningText3.GlowRadius = 0.01f;
            warningText3.GlowStrength = 0.2f;

            // ── Photosensitivity & Physiological ────────────────────────────────────

            var warningText4 = AddText("Photosensitivity Warning: Flashing Lights, Flicker Effects, FPS Vertigo.",
                position: new Vector3(-3f, 0f, -12.5f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.6f, 0.1f, 0.9f),
                align: TextAlignment.Center);
            warningText4.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningText4.GlowRadius = 0.01f;
            warningText4.GlowStrength = 0.2f;

            var warningText5 = AddText("Physiological Warning: Moderation.",
                position: new Vector3(-3f, 0f, -13.5f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.6f, 0.1f, 0.9f),
                align: TextAlignment.Center);
            warningText5.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            warningText5.GlowRadius = 0.01f;
            warningText5.GlowStrength = 0.2f;

            // ── Psychological Warning ────────────────────────────────────────────────

            var psychTitle = AddText("Psychological Warning",
                position: new Vector3(-3f, 0f, -14.5f),
                fontSize: 0.6f, fontKey: "Diablo",
                color: new Vector4(0.8f, 0.2f, 1f, 1f),
                align: TextAlignment.Center);
            psychTitle.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            psychTitle.GlowRadius = 0.02f;
            psychTitle.GlowStrength = 0.4f;
            psychTitle.GlowColor = new Vector4(0.8f, 0.2f, 1f, 0.8f);
            psychTitle.ShadowBlur = 0.04f;
            psychTitle.ShadowColor = new Vector4(0.5f, 0f, 1f, 0.3f);

            var psychText1 = AddText("Virtual Psychosis · Dementia · Schizophrenia · Intense Horror Themes",
                position: new Vector3(-3f, 0f, -15.5f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.8f, 0.2f, 1f, 0.9f),
                align: TextAlignment.Center);
            psychText1.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            psychText1.GlowRadius = 0.01f;
            psychText1.GlowStrength = 0.2f;

            var psychText2 = AddText("Disturbing Imagery · Emotional Volatility · Cognitive Distortion · Paranoia",
                position: new Vector3(-3f, 0f, -16.3f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.8f, 0.2f, 1f, 0.9f),
                align: TextAlignment.Center);
            psychText2.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            psychText2.GlowRadius = 0.01f;
            psychText2.GlowStrength = 0.2f;

            var psychText3 = AddText("Arcane Corruption · Eldritch Whispers · Infernal Influence · Shadow Entities",
                position: new Vector3(-3f, 0f, -17.1f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.8f, 0.2f, 1f, 0.9f),
                align: TextAlignment.Center);
            psychText3.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            psychText3.GlowRadius = 0.01f;
            psychText3.GlowStrength = 0.2f;

            var psychText4 = AddText("Cosmic Uncertainty · Moral Ambiguity · Forbidden Knowledge · Occult Undertones",
                position: new Vector3(-3f, 0f, -17.9f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.8f, 0.2f, 1f, 0.9f),
                align: TextAlignment.Center);
            psychText4.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            psychText4.GlowRadius = 0.01f;
            psychText4.GlowStrength = 0.2f;

            var psychText5 = AddText("And every thematic element capable of evoking extreme psychological tension.",
                position: new Vector3(-3f, 0f, -18.7f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.8f, 0.2f, 1f, 0.9f),
                align: TextAlignment.Center);
            psychText5.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            psychText5.GlowRadius = 0.01f;
            psychText5.GlowStrength = 0.2f;

            // ── Site Notices ─────────────────────────────────────────────────────────

            var noticeText1 = AddText("For site issues please refer to Privacy for contact information.",
                position: new Vector3(-3f, 0f, -19.7f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.2f, 0.2f, 0.9f),
                align: TextAlignment.Center);
            noticeText1.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            noticeText1.GlowRadius = 0.01f;
            noticeText1.GlowStrength = 0.2f;

            // ── PROJECTS Section ─────────────────────────────────────────────────────

            var projectsText = AddText("PROJECTS",
                position: new Vector3(-3f, 0f, -21f),
                fontSize: 2f, fontKey: "Diablo",
                color: new Vector4(1f, 0.2f, 0.2f, 1f),
                align: TextAlignment.Center);
            projectsText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            projectsText.GlowRadius = 0.02f;
            projectsText.GlowStrength = 0.5f;
            projectsText.GlowColor = new Vector4(1f, 0.2f, 0.2f, 0.8f);
            projectsText.ShadowBlur = 0.05f;
            projectsText.ShadowColor = new Vector4(1f, 0f, 0f, 0.4f);
            projectsText.GlowPulseSpeed = 1.2f;
            projectsText.GlowPulseMin = 0.01f;
            projectsText.GlowPulseMax = 0.06f;

            // ── Title Line ───────────────────────────────────────────────────────────
            var titleText = AddText("Software Engineer — Systems, Rendering & Performance",
                position: new Vector3(-3f, 0f, -23f),
                fontSize: 0.7f,
                      fontKey: "Diablo",
                           color: new Vector4(0.2f, 0.6f, 1f, 1f),
                align: TextAlignment.Center);
            titleText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            titleText.GlowRadius = 0.02f;
            titleText.GlowStrength = 0.4f;
            titleText.GlowColor = new Vector4(0.2f, 0.6f, 1f, 0.8f);
            titleText.ShadowBlur = 0.04f;
            titleText.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);
            // ── Tech Stack ───────────────────────────────────────────────────────────
            var techText = AddText("C# · Blazor · WebGL · JavaScript · HTML · CSS · FBX Pipelines · Real-Time Rendering",
                position: new Vector3(-3f, 0f, -24f),
                fontSize: 0.7f,
                fontKey: "Diablo",
                          color: new Vector4(0.2f, 0.6f, 1f, 1f),
                align: TextAlignment.Center);
            techText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            techText.GlowRadius = 0.02f;
            techText.GlowStrength = 0.4f;
            techText.GlowColor = new Vector4(0.2f, 0.6f, 1f, 0.8f);
            techText.ShadowBlur = 0.04f;
            techText.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);

            // ── Projects Callout ─────────────────────────────────────────────────────
            var projectsCallout = AddText("SpectralX WebGL2 Custom XYZ Engine",
                position: new Vector3(-3f, 0f, -25f),
                fontSize: 0.7f,
                    fontKey: "Diablo",
                   color: new Vector4(0.2f, 0.6f, 1f, 1f),
                align: TextAlignment.Center);
            projectsCallout.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            projectsCallout.GlowRadius = 0.02f;
            projectsCallout.GlowStrength = 0.4f;
            projectsCallout.GlowColor = new Vector4(0.2f, 0.6f, 1f, 0.8f);
            projectsCallout.ShadowBlur = 0.04f;
            projectsCallout.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);

            // ── System Requirements ──────────────────────────────────────────────────

            var sysTitle = AddText("System Requirements",
                position: new Vector3(-3f, 0f, -26f),
                fontSize: 0.8f, fontKey: "Diablo",
                color: new Vector4(0.2f, 0.6f, 1f, 1f),
                align: TextAlignment.Center);
            sysTitle.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysTitle.GlowRadius = 0.02f;
            sysTitle.GlowStrength = 0.4f;
            sysTitle.GlowColor = new Vector4(0.2f, 0.6f, 1f, 0.8f);
            sysTitle.ShadowBlur = 0.04f;
            sysTitle.ShadowColor = new Vector4(0f, 0.4f, 1f, 0.3f);

            var sysText1 = AddText("Desktop and Laptop PC only for Games. Mobile for page view.",
                position: new Vector3(-3f, 0f, -27f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText1.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText1.GlowRadius = 0.01f;
            sysText1.GlowStrength = 0.2f;

            var sysText2 = AddText("CPU: Pentium I minimum — tick logic under 16ms for 60 FPS.",
                position: new Vector3(-3f, 0f, -28.0f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText2.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText2.GlowRadius = 0.01f;
            sysText2.GlowStrength = 0.2f;

            var sysText3 = AddText("GPU: Nvidia Riva TNT, Intel onboard, or any WebGPU capable device.",
                position: new Vector3(-3f, 0f, -29f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText3.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText3.GlowRadius = 0.01f;
            sysText3.GlowStrength = 0.2f;

            var sysText4 = AddText("Browser: Latest Chrome, Edge, or Firefox with WebAssembly and WebGPU.",
                position: new Vector3(-3f, 0f, -30f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText4.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText4.GlowRadius = 0.01f;
            sysText4.GlowStrength = 0.2f;

            var sysText5 = AddText("OS: Windows 3.1, Linux, or any OS supporting browser capabilities. DOS loads.",
                position: new Vector3(-3f, 0f, -31f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText5.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText5.GlowRadius = 0.01f;
            sysText5.GlowStrength = 0.2f;

            var sysText6 = AddText("Network: Interweeb or Waifu required.",
                position: new Vector3(-3f, 0f, -32f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText6.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText6.GlowRadius = 0.01f;
            sysText6.GlowStrength = 0.2f;

            var sysText7 = AddText("Civilization Requirement: Electricity. Maybe chair. Snack recommended.",
                position: new Vector3(-3f, 0f, -33.0f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText7.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText7.GlowRadius = 0.01f;
            sysText7.GlowStrength = 0.2f;

            var sysText8 = AddText("Performance Law: Objects update at sovereign cadence. 60 FPS. Scalable to 120.",
                position: new Vector3(-3f, 0f, -34f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            sysText8.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText8.GlowRadius = 0.01f;
            sysText8.GlowStrength = 0.2f;

            var sysText9 = AddText("Cache Warning: Clear web cache when new updates are released.",
                position: new Vector3(-3f, 0f, -35f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.9f, 0.2f, 0.9f),
                align: TextAlignment.Center);
            sysText9.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText9.GlowRadius = 0.01f;
            sysText9.GlowStrength = 0.2f;

            var sysText10 = AddText("If server is inaccessible during patches please check back later.",
                position: new Vector3(-3f, 0f, -36f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(1f, 0.9f, 0.2f, 0.9f),
                align: TextAlignment.Center);
            sysText10.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            sysText10.GlowRadius = 0.01f;
            sysText10.GlowStrength = 0.2f;

            // ── Tech Stack Note ──────────────────────────────────────────────────────

            var techNote = AddText("C# OOP · SpectralGL WebGL2 GPU Backend · CLR / WASM NetCore 10",
                position: new Vector3(-3f, 0f, -37f),
                fontSize: 0.5f, fontKey: "Diablo",
                color: new Vector4(0.4f, 0.8f, 1f, 0.9f),
                align: TextAlignment.Center);
            techNote.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            techNote.GlowRadius = 0.01f;
            techNote.GlowStrength = 0.2f;

            // ── CONTACT Section ──────────────────────────────────────────────────────

            var contactText = AddText("CONTACT",
                position: new Vector3(-2f, 0f, -40f),
                fontSize: 2f, fontKey: "Diablo",
                color: new Vector4(0.2f, 1f, 0.4f, 1f),
                align: TextAlignment.Center);
            contactText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            contactText.GlowRadius = 0.02f;
            contactText.GlowStrength = 0.5f;
            contactText.GlowColor = new Vector4(0.2f, 1f, 0.4f, 0.8f);
            contactText.ShadowBlur = 0.05f;
            contactText.ShadowColor = new Vector4(0f, 1f, 0.3f, 0.4f);
            contactText.GlowPulseSpeed = 1.2f;
            contactText.GlowPulseMin = 0.01f;
            contactText.GlowPulseMax = 0.06f;

            // ── Contact Line ─────────────────────────────────────────────────────────
            var contactInfoText = AddText("GitHub · LinkedIn · robertchilewski83@gmail.com",
                position: new Vector3(-3f, 0f, -42f),
                fontSize: 0.7f,
                fontKey: "Diablo",
                color: new Vector4(0.7f, 0.7f, 0.7f, 0.8f),
                align: TextAlignment.Center);
            contactInfoText.Rotation = new Vector3(-MathF.PI / 2f, 0f, 0f);
            contactInfoText.GlowRadius = 0.01f;
            contactInfoText.GlowStrength = 0.2f;
            



            // ── Tile Map ─────────────────────────────────────────────────────────
            TileMap = new SpectralXLandTileMap();
            TileMap.SetGridSize(128);

            // ── Base Color (Albedo) Textures ──────────────────────────────────────
            TileMap.CustomTexturePaths = new[]
            {
    "/iAssets/DirtTile002.png",      // slot 0 - Dirt
    "/iAssets/RockTile002.png",      // slot 1 - Rock
    "/iAssets/SnowTile012.png",      // slot 2 - Snow    ← primary snow
    "/iAssets/SnowTile013.png",      // slot 3 - Snow2   ← secondary snow
    "/iAssets/WaterTile002.png",     // slot 4 - Water
    "/iAssets/IceTile002.png",       // slot 5 - Ice
};

            // ── Normal Maps ───────────────────────────────────────────────────────
            TileMap.CustomNormalMapPaths = new string?[]
            {
    null,                                       // slot 0 - Dirt    (add DirtTileNormal001.png)
    null,                                       // slot 1 - Rock    (add RockTileNormal001.png)
    "/iAssets/SnowTileNormal001.png",           // slot 2 - Snow    ✅ ACTIVE
    null,                                       // slot 3 - Snow2   (add SnowTile013Normal001.png)
    null,                                       // slot 4 - Water   (add WaterTileNormal001.png)
    null,                                       // slot 5 - Ice     (add IceTileNormal001.png)
            };

            // ── Specular Maps ─────────────────────────────────────────────────────
            TileMap.CustomSpecularMapPaths = new string?[]
            {
    null,                                       // slot 0 - Dirt    (add DirtTileSpecular001.png)
    null,                                       // slot 1 - Rock    (add RockTileSpecular001.png)
    "/iAssets/SnowTileSpecular001.png",         // slot 2 - Snow    ✅ ACTIVE
    null,                                       // slot 3 - Snow2   (add SnowTile013Specular001.png)
    null,                                       // slot 4 - Water   (add WaterTileSpecular001.png)
    null,                                       // slot 5 - Ice     (add IceTileSpecular001.png)
            };

            // ── Roughness Maps ────────────────────────────────────────────────────
            TileMap.CustomRoughnessMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileRoughness001.png)
    null,   // slot 1 - Rock    (add RockTileRoughness001.png)
    null,   // slot 2 - Snow    (add SnowTileRoughness001.png)
    null,   // slot 3 - Snow2   (add SnowTile013Roughness001.png)
    null,   // slot 4 - Water   (add WaterTileRoughness001.png)
    null,   // slot 5 - Ice     (add IceTileRoughness001.png)
            };

            // ── Metallic Maps ─────────────────────────────────────────────────────
            TileMap.CustomMetallicMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileMetallic001.png)
    null,   // slot 1 - Rock    (add RockTileMetallic001.png)
    null,   // slot 2 - Snow    (add SnowTileMetallic001.png)
    null,   // slot 3 - Snow2   (add SnowTile013Metallic001.png)
    null,   // slot 4 - Water   (add WaterTileMetallic001.png)
    null,   // slot 5 - Ice     (add IceTileMetallic001.png)
            };

            // ── Ambient Occlusion Maps ────────────────────────────────────────────
            TileMap.CustomAOMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileAO001.png)
    null,   // slot 1 - Rock    (add RockTileAO001.png)
    null,   // slot 2 - Snow    (add SnowTileAO001.png)
    null,   // slot 3 - Snow2   (add SnowTile013AO001.png)
    null,   // slot 4 - Water   (add WaterTileAO001.png)
    null,   // slot 5 - Ice     (add IceTileAO001.png)
            };

            // ── Emissive Maps ─────────────────────────────────────────────────────
            TileMap.CustomEmissiveMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileEmissive001.png)
    null,   // slot 1 - Rock    (add RockTileEmissive001.png)
    null,   // slot 2 - Snow    (add SnowTileEmissive001.png)
    null,   // slot 3 - Snow2   (add SnowTile013Emissive001.png)
    null,   // slot 4 - Water   (add WaterTileEmissive001.png)
    null,   // slot 5 - Ice     (add IceTileEmissive001.png)
            };

            // ── Displacement Maps (Parallax Occlusion Mapping) ────────────────────
            TileMap.CustomDisplacementMapPaths = new string?[]
            {
    null,   // slot 0 - Dirt    (add DirtTileDisplace001.png)
    null,   // slot 1 - Rock    (add RockTileDisplace001.png)
    null,   // slot 2 - Snow    (add SnowTileDisplace001.png)
    null,   // slot 3 - Snow2   (add SnowTile013Displace001.png)
    null,   // slot 4 - Water   (add WaterTileDisplace001.png)
    null,   // slot 5 - Ice     (add IceTileDisplace001.png)
            };

            // ── PBR Scalar Parameters ─────────────────────────────────────────────
            //                                   Dirt   Rock   Snow   Snow2  Water  Ice
            TileMap.RoughnessValues = new[] { 0.9f, 0.8f, 0.4f, 0.5f, 0.1f, 0.2f };
            TileMap.MetallicValues = new[] { 0.0f, 0.0f, 0.5f, 0.3f, 0.0f, 0.4f };
            TileMap.AOValues = new[] { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
            TileMap.SpecularValues = new[] { 0.3f, 0.4f, 0.7f, 0.6f, 0.8f, 0.9f };
            TileMap.EmissiveIntensityValues = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            TileMap.DisplacementStrengthValues = new[] { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            TileMap.ParallaxScaleValues = new[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f };

            TileMap.Init();
            _tileMapTexturesUploaded = false;
            _tilePBRUploaded = false;
            _ = LoadLandscape();



            // ── Spawn exclusion zone — prevent foliage blocking camera entry ──
            _propScatter.RegisterFootprint(0f, -10f, 5f);    // camera spawn
            // ── Prop Scatter ─────────────────────────────────────────────
            // ── Foliage Scatter — instanced rendering, no scene mesh entries ──────────
            var scatterConfigs = new[]
            {
                  new GridBoundedScatterConfig("Bush001",  50, -64f, 64f, -64f, 64f),
    new GridBoundedScatterConfig("Rock002", 50, -64f, 64f, -64f, 64f),
  new GridBoundedScatterConfig("Tree002", 55, -64f, 64f, -64f, 64f),
   new GridBoundedScatterConfig("Grass001", 200, -64f, 64f, -64f, 64f),
};

            foreach (var config in scatterConfigs)
                _foliageGroups.Add(_propScatter.ScatterInGrid(config));


            /*
            // ── Graveyard Grid ────────────────────────────────────────────────────────
            var graveyardConfigs = new[]
            {
    new GridBoundedScatterConfig("Grave001",  20, -76f, -52f, 52f, 76f),
    new GridBoundedScatterConfig("GraveS001", 20, -76f, -52f, 52f, 76f),
};

            foreach (var config in graveyardConfigs)
                _foliageGroups.Add(_propScatter.ScatterInGrid(config));
            */
            Camera.Position = new CustomVec3(0, -10, 4);



        }








    }
}
