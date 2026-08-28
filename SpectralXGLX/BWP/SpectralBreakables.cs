using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SpectralBreakables — all destructible world objects for BWP Scene4
    //
    //  Ported from BloodBreakables (2D CSS / Rectangle collision)
    //  → WebGL2 PrimSquare billboards with sphere collision in world space.
    //
    //  Warrior baseline: 84 px = 1 world unit.
    //  Billboard Z:      pixelHeight / 84  (centres sprite, base on ground).
    //  Rotation:         (PI/2 - PI/6, 0, -PI/2) matches warrior + dynamic objects.
    //
    //  Phase state machine (delta based — no Task.Run, no DateTime):
    //    Alive     → normal texture, white mesh color
    //    HitEffect → hit texture + red tint, _hitTimer drains, then back to Alive
    //    Dead      → cooked texture, _deadTimer counts up to DeadRemoveThreshold
    //
    //  Hit effects are cleared by DynTickUpdate timer, NOT by the warrior.
    //  Warrior calls BreakTakeDamage() only — the dummy owns its own visual state.
    // ═══════════════════════════════════════════════════════════════════════════

    public class SpectralBreakables
    {
        // Warrior baseline — 84 px = 1 world unit
        private const float Wpx = 84f;

        // How long the red hit flash lasts in seconds
        public const float HitFlashDuration = 0.15f;

        // How long the dead cooked sprite stays in the world before it can be removed
        public const float DeadRemoveThreshold = 60f;

        /// <summary>Convert pixel dimensions to a uniform world-space scale vector.</summary>
        public static Vector3 ToWorldScale(float px, float py)
        {
            float s = MathF.Max(px, py) / Wpx;
            return new Vector3(s, s, s);
        }

        /// <summary>
        /// Clones PrimSquare geometry into a new billboard mesh.
        /// Mirrors SpectralDynObjects.BuildMesh pattern exactly.
        /// </summary>
        public static SpectralXMesh BuildMesh(
            string id,
            SpectralXMesh prim,
            Vector3 scale,
            ISpectralBreakable obj)
        {
            var m = new SpectralXMesh(id);
            m.Vertices.AddRange(prim.Vertices);
            m.Normals.AddRange(prim.Normals);
            m.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) m.Faces.Add(f);

            m.Size = scale;
            m.Position = new Vector3(obj.BreakX, obj.BreakY, obj.BreakZ);

            // Billboard rotation matching warrior and dynamic objects
            m.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );


            m.TextureDataUrl = obj.BreakTexturePath;
            m.TextureDirty = true;
            m.Color = new Vector4(1f, 1f, 1f, 1f);
            m.IsEmissive = obj.BreakIsEmissive;
            m.EmissiveIntensity = obj.BreakEmissiveIntensity;
            m.CastsShadow = false;
            m.UVScaleX = 1f;
            m.UVScaleY = 1f;
            m.TransformDirty = true;
            return m;
        }


        // ═══════════════════════════════════════════════════════════════════════
        //  DUMMY  —  64×64 px training target
        //
        //  HP:              5
        //  CollisionRadius: 0.3f world units
        //  Phases:          Alive → HitEffect (0.15s red flash) → Alive again
        //                   On HP=0: Dead (cooked sprite, stays 60s)
        //
        //  Textures:
        //    Alive    → /iAssets/Dummy005.png
        //    HitEffect→ /iAssets/DummyHit005.png  + red tint on mesh
        //    Dead     → /iAssets/DummyCooked005.png
        // ═══════════════════════════════════════════════════════════════════════

        public class Dummy : ISpectralBreakable
        {
            // ── ISpectralBreakable position ───────────────────────
            public float BreakX { get; set; }
            public float BreakY { get; set; }
            // 64px dummy — Z centres the sprite base on the ground
            public float BreakZ { get; set; } = 0.1f;

            // ── ISpectralBreakable size ───────────────────────────
            private float _w = 64f / Wpx;
            private float _h = 64f / Wpx;
            public float BreakWidth { get => _w; set => _w = value; }
            public float BreakHeight { get => _h; set => _h = value; }

            // ── ISpectralBreakable collision ──────────────────────
            public float BreakCollisionRadius => 0.3f;

            // ── ISpectralBreakable mesh ───────────────────────────
            public SpectralXMesh? BreakMesh { get; set; }

            // ── ISpectralBreakable stats ──────────────────────────
            public int BreakHitPoints { get; set; } = 5;
            public int BreakMaxHP { get; set; } = 5;
            public bool BreakIsAlive => BreakHitPoints > 0;

            // ── ISpectralBreakable phase ──────────────────────────
            public BreakPhase Phase { get; private set; } = BreakPhase.Alive;
            public bool BreakIsShowingHitEffect => Phase == BreakPhase.HitEffect;

            // ── ISpectralBreakable textures ───────────────────────
            public string BreakTexturePath => "/iAssets/Dummy005.png";
            public string BreakHitTexturePath => "/iAssets/DummyHit005.png";
            public string BreakDeadTexturePath => "/iAssets/DummyCooked005.png";

            // ── ISpectralBreakable emissive ───────────────────────
            // goes black when true is error
            public bool BreakIsEmissive => false;
            public Vector4 BreakEmissiveColor => Vector4.One;
            public float BreakEmissiveIntensity => 0f;
        

            // ── Phase timers (delta based) ────────────────────────
            private float _hitTimer = 0f;   // counts up during HitEffect phase
            private float _deadTimer = 0f;   // counts up during Dead phase

            // ── Mesh color constants ──────────────────────────────
            private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
            private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
            private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.55f);

            // ── Damage ────────────────────────────────────────────
            public void BreakTakeDamage(int amount)
            {
                if (Phase == BreakPhase.Dead) return;

                BreakHitPoints = Math.Max(BreakHitPoints - amount, 0);

                if (BreakHitPoints <= 0)
                {
                    TransitionToDead();
                }
                else
                {
                    TransitionToHitEffect();
                }
            }

            public void BreakClearHitEffects()
            {
                if (Phase != BreakPhase.HitEffect) return;
                TransitionToAlive();
            }

            // ── Per-frame tick ────────────────────────────────────
            public void DynTickUpdate(ISpectralCharacter character, float delta)
            {
                switch (Phase)
                {
                    case BreakPhase.HitEffect:
                        _hitTimer += delta;
                        // Push red tint every frame — campfire pattern
                        if (BreakMesh != null)
                            BreakMesh.Color = ColorHit;
                        if (_hitTimer >= HitFlashDuration)
                            TransitionToAlive();
                        break;

                    case BreakPhase.Dead:
                        _deadTimer += delta;
                        break;

                    case BreakPhase.Alive:
                    default:
                        break;
                }
            }

            // ── Phase transitions ─────────────────────────────────
            private void TransitionToHitEffect()
            {
                Phase = BreakPhase.HitEffect;
                _hitTimer = 0f;
                Console.WriteLine($"[Dummy] TransitionToHitEffect called — mesh={BreakMesh?.Name}");
                SyncMeshVisuals();
            }

            private void TransitionToAlive()
            {
                Phase = BreakPhase.Alive;
                SyncMeshVisuals();
            }

            private void TransitionToDead()
            {
                Phase = BreakPhase.Dead;
                _deadTimer = 0f;
                Console.WriteLine($"[Dummy] TransitionToDead called — mesh={BreakMesh?.Name}");
                SyncMeshVisuals();
                Console.WriteLine($"[Dummy] After SyncMeshVisuals — TextureDirty={BreakMesh?.TextureDirty} texUrl={BreakMesh?.TextureDataUrl}");
            }

            /// <summary>
            /// Syncs mesh texture and color to current phase.
            /// Called on every phase transition — not every frame.
            /// TransformDirty is NOT set here; position never changes.
            /// TextureDirty flags the JS engine to re-upload the texture next frame.
            /// </summary>
            private void SyncMeshVisuals()
            {
                if (BreakMesh == null) return;

                switch (Phase)
                {
                    case BreakPhase.HitEffect:
                        BreakMesh.Color = ColorHit;
                        BreakMesh.TextureDataUrl = BreakHitTexturePath;  // ADD
                        BreakMesh.TextureDirty = true;                   // ADD
                        break;

                    case BreakPhase.Alive:
                        BreakMesh.Color = ColorNormal;
                        BreakMesh.TextureDataUrl = BreakTexturePath;     // ADD
                        BreakMesh.TextureDirty = true;                   // ADD
                        break;

                    case BreakPhase.Dead:
                        BreakMesh.Color = ColorDead;
                        BreakMesh.TextureDataUrl = BreakDeadTexturePath;
                        BreakMesh.TextureDirty = true;
                        break;
                }
            }

            // ── Dead timer accessor for manager cleanup later ─────
            public float DeadTimer => _deadTimer;
        }


        // ═══════════════════════════════════════════════════════════════════════
        //  DUMMY REGISTRY
        // ═══════════════════════════════════════════════════════════════════════

        public static class DummyRegistry
        {
            public static readonly List<Dummy> All = new();

            /// <summary>
            /// Spawns count dummies at random XY positions within radius of world origin.
            /// Called from SpectralBreakManager.SpawnAll().
            /// </summary>
            public static void SpawnAll(
                SpectralXScene scene,
                SpectralXMeshLibrary lib,
                int count,
                float radius,
                int seed)
            {
                All.Clear();

                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null)
                {
                    Console.WriteLine("[SpectralBreakables] DummyRegistry: PrimSquare missing");
                    return;
                }


                // ── Pre-register hit and dead textures ────────────
                // Engine only uploads textures it has seen — prime the cache
                // with invisible meshes so hit flash and death swap work instantly
                var scale = ToWorldScale(64f, 64f);
                var preloadTextures = new[]
                {
                    "/iAssets/DummyHit005.png",
                    "/iAssets/DummyCooked005.png"
                };

                foreach (var texPath in preloadTextures)
                {
                    var primer = new SpectralXMesh($"BreakDummyPrimer_{texPath.GetHashCode()}");
                    primer.Vertices.AddRange(prim.Vertices);
                    primer.Normals.AddRange(prim.Normals);
                    primer.UVs.AddRange(prim.UVs);
                    foreach (var f in prim.Faces) primer.Faces.Add(f);
                    primer.Size = scale;
                    primer.Position = new Vector3(-9999f, -9999f, -9999f); // off screen
                    primer.Rotation = new Vector3(MathF.PI / 2f - MathF.PI / 6f, 0f, -MathF.PI / 2f);
                    primer.TextureDataUrl = texPath;
                    primer.TextureDirty = true;
                    primer.Color = new Vector4(0f, 0f, 0f, 0f); // fully transparent
                    primer.CastsShadow = false;
                    primer.UVScaleX = 1f;
                    primer.UVScaleY = 1f;
                    scene.AddMesh(primer);
                }
                var rand = new Random(seed);

                for (int i = 0; i < count; i++)
                {
                    var dummy = new Dummy
                    {
                        BreakX = (float)(rand.NextDouble() * 2.0 - 1.0) * radius,
                        BreakY = (float)(rand.NextDouble() * 2.0 - 1.0) * radius,
                    };

                    var mesh = BuildMesh($"BreakDummy_{i}", prim, scale, dummy);
                    dummy.BreakMesh = mesh;
                    scene.AddMesh(mesh);
                    All.Add(dummy);
                }

                Console.WriteLine($"[SpectralBreakables] Spawned {count} dummies — radius:{radius} seed:{seed}");
            }

            public static void Clear() => All.Clear();
        }
    }


    // ═══════════════════════════════════════════════════════════════════════════
    //  SPECTRAL BREAK MANAGER
    //  Orchestrates all breakable objects for BWP Scene4.
    //
    //  Usage:
    //    InitScene4()        → SpectralBreakManager.SpawnAll(Scene4, MeshLibrary, ...)
    //    TickAndGetFrame()   → SpectralBreakManager.TickAll(Warrior, _lastFrameDelta)
    //    SwitchToScene()     → handled automatically — SpawnAll calls Clear() first
    // ═══════════════════════════════════════════════════════════════════════════

    public static class SpectralBreakManager
    {
        // Default dummy count — 5 for initial testing near warrior start position
        public static int CountDummies = 5;

        /// <summary>
        /// Clear all registries and spawn fresh breakables into scene.
        /// Call from InitScene4() after SpectralDynManager.SpawnAll().
        /// SpawnRadius of 12f keeps dummies near the warrior start clearing.
        /// </summary>
        public static void SpawnAll(
            SpectralXScene scene,
            SpectralXMeshLibrary lib,
            float spawnRadius = 12f,
            int seed = 99)
        {
            Clear();
            SpectralBreakables.DummyRegistry.SpawnAll(
                scene, lib, CountDummies, spawnRadius, seed);

            Console.WriteLine("[SpectralBreakManager] SpawnAll complete");
        }

        /// <summary>
        /// Tick all active breakable objects.
        /// Call from TickAndGetFrame() when ActiveScene == BWPScene1 and Warrior != null.
        ///
        ///   if (ActiveScene == SceneID.BWPScene1 and Warrior != null)
        ///       SpectralBreakManager.TickAll(Warrior, _lastFrameDelta);
        /// </summary>
        public static void TickAll(ISpectralCharacter character, float delta)
        {
            foreach (var dummy in SpectralBreakables.DummyRegistry.All)
                dummy.DynTickUpdate(character, delta);
        }

        /// <summary>Clear all registries. Called automatically by SpawnAll().</summary>
        public static void Clear()
        {
            SpectralBreakables.DummyRegistry.Clear();
        }

        /// <summary>
        /// Returns all dummies that are alive — useful for warrior punch range checks.
        /// </summary>
        public static IEnumerable<SpectralBreakables.Dummy> AliveDummies()
            => SpectralBreakables.DummyRegistry.All.Where(d => d.BreakIsAlive);

        /// <summary>
        /// Returns all dummies eligible for scene removal (dead longer than threshold).
        /// Not acted on yet — exposed for future cleanup pass.
        /// </summary>
        public static IEnumerable<SpectralBreakables.Dummy> ExpiredDummies()
            => SpectralBreakables.DummyRegistry.All
                .Where(d => d.Phase == BreakPhase.Dead
                         && d.DeadTimer >= SpectralBreakables.DeadRemoveThreshold);
    }
}