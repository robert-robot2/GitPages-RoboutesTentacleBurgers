using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SpectralSplatter — blood puddle decals for BWP Scene4
    //
    //  Ported from old div engine's SplatterPuddle / BloodSplatterRegistry
    //  → WebGL2 PrimSquare flat ground decals in world space.
    //
    //  Old engine: puddles were absolutely-positioned <img> tags, one per hit,
    //  with no lifetime — they persisted forever and had no cap.
    //
    //  New engine: puddles are flat (unrotated) PrimSquare clones lying on the
    //  ground plane, capped at MaxPuddles, and fade out linearly over
    //  FadeDuration seconds before being removed from the scene.
    //
    //  PrimSquare geometry is authored flat in the XY plane at Z=0 with a
    //  +Z-facing normal (see SpectralXMeshLibrary.CreateSquare), so no tilt
    //  rotation is needed to lay it on the ground — only an optional random
    //  yaw (Z-axis spin) for visual variety between puddles.
    // ═══════════════════════════════════════════════════════════════════════════

    public class SplatterPuddle
    {
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float WorldZ { get; set; }

        public float Scale { get; set; } = 1.0f;

        public float SpawnTime { get; set; }

        public SpectralXMesh? Mesh { get; set; }
    }

    public static class SplatterPuddleRegistry
    {
        // ── Config ────────────────────────────────────────────────
        public const int MaxPuddles = 200;
        public const float FadeDuration = 20f;

        public const string BloodTexturePath = "/iAssets/BPuddle01.png";
        public const string BoneTexturePath = "/iAssets/BoneSplatter001.png";

        // Puddle sprite baseline — matches old engine's 24x24 default Width/Height
        // at Warrior's 84px = 1 world unit baseline.
        private const float PuddlePixelSize = 24f;
        private const float Wpx = 84f;
        private const float BaseWorldScale = PuddlePixelSize / Wpx; // ~0.2857

        // Tiny Z lift to avoid z-fighting with terrain/tile map
        private const float GroundLift = 0.02f;

        public static readonly List<SplatterPuddle> All = new();

        private static readonly Random _rand = new Random();

        // ── Spawn ─────────────────────────────────────────────────
        /// <summary>
        /// Spawns one blood puddle at the given world position.
        /// Call from a character's TakeDamage handler.
        /// </summary>
        /// <param name="scene">Active scene (Scene4 for BWP).</param>
        /// <param name="lib">Mesh library, used to clone PrimSquare.</param>
        /// <param name="x">World X position (already jittered by caller if desired).</param>
        /// <param name="y">World Y position (already jittered by caller if desired).</param>
        /// <param name="z">Ground/terrain Z at this position.</param>
        /// <param name="scaleMultiplier">
        /// Extra scale multiplier on top of the base puddle size — pass the old
        /// engine's damage-based scale (e.g. Math.Min(1.0 + amount*0.25, 3.0) * jitter).
        /// </param>
        /// <param name="now">
        /// Current engine time in seconds (e.g. from SpectralXEngine's frame clock),
        /// used to drive fade timing consistently with the rest of the tick loop.
        /// </param>
        /// <param name="texturePath">
        /// Splatter decal texture — defaults to blood. Pass BoneTexturePath (or any
        /// other preloaded splatter texture) for non-blood variants (e.g. undead).
        /// </param>
        public static void Spawn(
            SpectralXScene scene,
            SpectralXMeshLibrary lib,
            float x,
            float y,
            float z,
            float scaleMultiplier,
            float now,
            string texturePath = BloodTexturePath)
        {
            var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
            if (prim == null)
            {
                Console.WriteLine("[SplatterPuddle] Spawn failed — PrimSquare missing");
                return;
            }

            float finalScale = BaseWorldScale * MathF.Max(0.01f, scaleMultiplier);

            var meshName = $"DynSplatter_{Guid.NewGuid():N}";
            var mesh = new SpectralXMesh(meshName);
            mesh.Vertices.AddRange(prim.Vertices);
            mesh.Normals.AddRange(prim.Normals);
            mesh.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) mesh.Faces.Add(f);

            mesh.Size = new Vector3(finalScale, finalScale, finalScale);
            mesh.Position = new Vector3(x, y, z + GroundLift);

            // Flat on ground — PrimSquare's default orientation already lies in
            // the XY plane with a +Z normal, so no tilt is needed. Random yaw
            // spin only, for visual variety between puddles.
            float yaw = (float)(_rand.NextDouble() * MathF.PI * 2.0);
            mesh.Rotation = new Vector3(0f, 0f, yaw);

            mesh.TextureDataUrl = texturePath;
            mesh.TextureDirty = true;
            mesh.Color = new Vector4(1f, 1f, 1f, 1f);
            mesh.IsEmissive = false;
            mesh.CastsShadow = false;
            mesh.ReceivesShadow = false;
            mesh.UVScaleX = 1f;
            mesh.UVScaleY = 1f;
            mesh.TransformDirty = true;

            scene.AddMesh(mesh);

            var puddle = new SplatterPuddle
            {
                WorldX = x,
                WorldY = y,
                WorldZ = z,
                Scale = finalScale,
                SpawnTime = now,
                Mesh = mesh,
            };

            All.Add(puddle);

            // Evict oldest if over cap
            if (All.Count > MaxPuddles)
            {
                var oldest = All[0];
                All.RemoveAt(0);
                if (oldest.Mesh != null)
                    scene.RemoveMesh(oldest.Mesh);
            }
        }

        // ── Tick ──────────────────────────────────────────────────
        /// <summary>
        /// Ages all active puddles, fading alpha linearly over FadeDuration
        /// seconds, and removes any that have fully expired.
        /// Call once per frame from TickAndGetFrame when ActiveScene == BWPScene1.
        /// </summary>
        public static void Tick(SpectralXScene scene, float now)
        {
            if (All.Count == 0) return;

            for (int i = All.Count - 1; i >= 0; i--)
            {
                var puddle = All[i];
                if (puddle.Mesh == null)
                {
                    All.RemoveAt(i);
                    continue;
                }

                float age = now - puddle.SpawnTime;

                if (age >= FadeDuration)
                {
                    scene.RemoveMesh(puddle.Mesh);
                    All.RemoveAt(i);
                    continue;
                }

                float alpha = 1f - (age / FadeDuration);
                var c = puddle.Mesh.Color;
                if (!FloatEquals(c.W, alpha))
                {
                    puddle.Mesh.Color = new Vector4(c.X, c.Y, c.Z, alpha);
                }
            }
        }

        private static bool FloatEquals(float a, float b, float epsilon = 0.001f)
            => MathF.Abs(a - b) < epsilon;

        // ── Clear ─────────────────────────────────────────────────
        /// <summary>
        /// Removes all active puddle meshes from the scene and clears the
        /// registry. Call from SwitchToScene() whenever BWPScene1 resets.
        /// </summary>
        public static void Clear(SpectralXScene scene)
        {
            foreach (var puddle in All)
            {
                if (puddle.Mesh != null)
                    scene.RemoveMesh(puddle.Mesh);
            }
            All.Clear();
        }
    }
}