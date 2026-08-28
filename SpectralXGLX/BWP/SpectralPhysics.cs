using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SpectralPhysics — small kickable physics props for BWP Scene4
    //
    //  Ported from old div engine's BloodPhysics.UndeadGF / IPhysics
    //  → WebGL2 PrimSquare billboards with 1D velocity + friction + a simple
    //  collision-triggered kick, using the same registry/spawn/tick pattern
    //  as SpectralDynObjects (CampFire, HealPot, etc).
    //
    //  Old engine: UndeadGF was a floating head prop with no gravity/Y motion —
    //  just horizontal velocity, friction, and a kick impulse applied whenever
    //  the active character's collision box overlapped it. Rendering was baked
    //  directly into IPhysics (PhysSpriteStyle etc) since Blazor iterated
    //  IPhysics objects to emit <div> tags.
    //
    //  New engine: rendering is handled the normal way through SpectralXMesh,
    //  same as every other dynamic object — ISpectralPhysics is now purely a
    //  data contract (position, velocity, mass, collision radius) so the
    //  shared math in this file can operate on any future physics prop
    //  (limbs, torso, etc) without duplicating the tick logic per type.
    // ═══════════════════════════════════════════════════════════════════════════

    public interface ISpectralPhysics
    {
        float WorldX { get; set; }
        float WorldY { get; set; }
        float VelocityX { get; set; }
        float Mass { get; }
        bool IsActive { get; }
        float CollisionRadius { get; }
        SpectralXMesh? PhysMesh { get; }
    }

    public class SpectralUndeadGF : ISpectralPhysics
    {
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float WorldZ { get; set; }

        public float VelocityX { get; set; }
        public float Mass { get; set; } = 1.0f;
        public bool IsActive { get; set; } = true;

        public float CollisionRadius { get; set; } = 0.3f; // ~24px at 84px = 1 world unit baseline

        public SpectralXMesh? PhysMesh { get; set; }

        // Friction and kick force — ported directly from old TickKickGF
        private const float Friction = 0.88f;
        private const float KickForce = 1.2f;

        /// <summary>
        /// Integrates velocity into position, applies friction, and kicks
        /// away from the given character on collision. Ported 1:1 from
        /// BloodPhysics.UndeadGF.TickKickGF.
        /// </summary>
        /// <summary>
        /// Integrates velocity into position, applies friction, and kicks
        /// away when the given character (and/or any enemy in enemies)
        /// overlaps. Ported 1:1 from BloodPhysics.UndeadGF.TickKickGF, with
        /// enemy collision added on top using the same distance check.
        /// </summary>
        public void Tick(ISpectralCharacter? character, IEnumerable<ISpectralEnemy>? enemies = null)
        {
            if (!IsActive) return;

            // Apply velocity to position
            WorldX += VelocityX;

            // Apply friction
            VelocityX *= Friction;

            // Kick logic — old engine only ever kicked right (+1.2f), no
            // directionality was computed from the character's side. Kept
            // identical here for a faithful port.
            if (character != null && character.CharIsAlive)
            {
                float dx = WorldX - character.WorldX;
                float dy = WorldY - character.WorldY;
                float distSq = dx * dx + dy * dy;
                float minDist = CollisionRadius + character.CollisionRadius;

                if (distSq <= minDist * minDist)
                {
                    VelocityX += KickForce;
                }
            }

            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (!enemy.EnemyIsAlive) continue;

                    float edx = WorldX - enemy.WorldX;
                    float edy = WorldY - enemy.WorldY;
                    float eDistSq = edx * edx + edy * edy;
                    float eMinDist = CollisionRadius + enemy.CollisionRadius;

                    if (eDistSq <= eMinDist * eMinDist)
                    {
                        VelocityX += KickForce;
                    }
                }
            }

            if (PhysMesh != null)
                PhysMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }
    }

    public static class SpectralUndeadGFRegistry
    {
        public static readonly List<SpectralUndeadGF> All = new();

        private const string TexturePath = "/iAssets/UndeadGFhead.png";

        // ── Spawn ─────────────────────────────────────────────────
        /// <summary>
        /// Spawns count UndeadGF heads scattered within radius of the
        /// given origin point. Call from InitScene4() alongside
        /// SpectralDynManager.SpawnAll() / SpectralBreakManager.SpawnAll().
        /// </summary>
        public static void Spawn(
            SpectralXScene scene,
            SpectralXMeshLibrary lib,
            int count,
            float originX,
            float originY,
            float originZ,
            float radius,
            int seed)
        {
            All.Clear();

            var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
            if (prim == null)
            {
                Console.WriteLine("[SpectralUndeadGF] Spawn failed — PrimSquare missing");
                return;
            }

            var rand = new Random(seed);

            // 16x16 old-engine pixel size, at 84px = 1 world unit baseline
            const float Wpx = 84f;
            const float SizePx = 16f;
            float worldScale = SizePx / Wpx;

            for (int i = 0; i < count; i++)
            {
                double angle = rand.NextDouble() * Math.PI * 2.0;
                double dist = Math.Sqrt(rand.NextDouble()) * radius;
                float x = originX + (float)(Math.Cos(angle) * dist);
                float y = originY + (float)(Math.Sin(angle) * dist);

                var meshName = $"UndeadGF_{i}";
                var mesh = new SpectralXMesh(meshName);
                mesh.Vertices.AddRange(prim.Vertices);
                mesh.Normals.AddRange(prim.Normals);
                mesh.UVs.AddRange(prim.UVs);
                foreach (var f in prim.Faces) mesh.Faces.Add(f);

                mesh.Size = new Vector3(worldScale, worldScale, worldScale);
                mesh.Position = new Vector3(x, y, originZ);
                mesh.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );

                mesh.TextureDataUrl = TexturePath;
                mesh.TextureDirty = true;
                mesh.Color = new Vector4(1f, 1f, 1f, 1f);
                mesh.CastsShadow = false;
                mesh.TransformDirty = true;

                scene.AddMesh(mesh);

                var gf = new SpectralUndeadGF
                {
                    WorldX = x,
                    WorldY = y,
                    WorldZ = originZ,
                    PhysMesh = mesh,
                };

                All.Add(gf);
            }

            Console.WriteLine($"[SpectralUndeadGF] Spawned {count} heads");
        }

        /// <summary>
        /// Ticks all active UndeadGF props against the given character and
        /// any active wave enemies. Call once per frame from
        /// TickAndGetFrame when ActiveScene == BWPScene1.
        /// </summary>
        public static void TickAll(ISpectralCharacter? character, float delta,
            IEnumerable<ISpectralEnemy>? enemies = null)
        {
            foreach (var gf in All)
            {
                if (gf.IsActive)
                    gf.Tick(character, enemies);
            }
        }

        // ── Clear ─────────────────────────────────────────────────
        /// <summary>
        /// Removes all UndeadGF meshes from the scene and clears the
        /// registry. Call from SwitchToScene() whenever BWPScene1 resets.
        /// </summary>
        public static void Clear(SpectralXScene scene)
        {
            foreach (var gf in All)
            {
                if (gf.PhysMesh != null)
                    scene.RemoveMesh(gf.PhysMesh);
            }
            All.Clear();
        }
    }
}