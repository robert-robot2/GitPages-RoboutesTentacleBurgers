using System.Numerics;
using SpectralXGLX.SpectralXComponent;

namespace SpectralXGLX.BWP
{
    // ═══════════════════════════════════════════════════════════════════════════
    //  SpectralDynObjects — all dynamic interactable objects for BWP Scene4
    //
    //  Ported from BloodDynamicObj (2D CSS / Rectangle collision)
    //  → WebGL2 PrimSquare billboards with sphere collision in world space.
    //
    //  Warrior baseline:  84 px = 1 world unit.
    //  Billboard Z:       pixelHeight / 84  (centres the sprite, base on ground).
    //  Rotation:          (PI/2, 0, -PI/2)  matches warrior & static objects.
    //
    //  Old bug fixed:
    //    CampFire.DynWidth/DynHeight previously called themselves recursively.
    //    Now backed by proper float fields.
    // ═══════════════════════════════════════════════════════════════════════════

    public class SpectralDynObjects
    {
        // Warrior baseline — 84 px = 1 world unit
        private const float Wpx = 84f;

        /// <summary>Convert pixel dimensions to a uniform world-space scale vector.</summary>
        public static Vector3 ToWorldScale(float px, float py)
        {
            float s = MathF.Max(px, py) / Wpx;
            return new Vector3(s, s, s);
        }

        // ── Shared collision helper ───────────────────────────────
        private static bool IsNear(ISpectralCharacter c, float ox, float oy, float objRadius)
        {
            float dx = c.WorldX - ox;
            float dy = c.WorldY - oy;
            float min = c.CollisionRadius + objRadius;
            return dx * dx + dy * dy < min * min;
        }

        // ── Shared mesh builder ───────────────────────────────────
        /// <summary>
        /// Clones PrimSquare geometry into a new mesh with billboard rotation, texture, and
        /// position derived from the IDynamicO object.  Mirrors SpectralXBWPStaticObjects pattern.
        /// </summary>
        public static SpectralXMesh BuildMesh(
            string id,
            SpectralXMesh prim,
            Vector3 scale,
            IDynamicO obj)
        {
            var m = new SpectralXMesh(id);
            m.Vertices.AddRange(prim.Vertices);
            m.Normals.AddRange(prim.Normals);
            m.UVs.AddRange(prim.UVs);
            foreach (var f in prim.Faces) m.Faces.Add(f);

            m.Size = scale;
            m.Position = new Vector3(obj.DynX, obj.DynY, obj.DynZ);
          
            m.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );

            m.TextureDataUrl = obj.DynTexturePath;
            m.TextureDirty = true;
            m.Color = new Vector4(1f, 1f, 1f, 1f);
            m.IsEmissive = obj.DynIsEmissive;
            m.EmissiveIntensity = obj.DynEmissiveIntensity;
            m.CastsShadow = false;
            m.UVScaleX = 1f;
            m.UVScaleY = 1f;
            m.TransformDirty = true;
            return m;
        }

        // ── Hide a collected/deactivated pickup ───────────────────
        public static void HideMesh(SpectralXMesh? mesh)
        {
            if (mesh == null) return;
            mesh.Color = new Vector4(0f, 0f, 0f, 0f); // fully transparent — propagates via BuildWebGLFrame
        }


        // ═══════════════════════════════════════════════════════════
        //  CAMPFIRE  —  48×48 px, emissive, flickering
        // ═══════════════════════════════════════════════════════════

        public class CampFire : IDynamicO
        {
            // ── IDynamicO position ────────────────────────────────
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 48f / Wpx;   // 0.571 — centres 48 px sprite

            // ── IDynamicO size ────────────────────────────────────
            private float _w = 48f / Wpx;
            private float _h = 48f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }

            // ── IDynamicO core ────────────────────────────────────
            public float DynCollisionRadius => 0.8f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/Campfire0003.png";

            // ── Emissive / flicker state ──────────────────────────
            public bool DynIsEmissive => true;
            public Vector4 DynEmissiveColor { get; private set; } = new Vector4(1f, 0.5f, 0.05f, 1f);
            public float DynEmissiveIntensity { get; private set; } = 1.4f;

            // Per-instance phase so campfires don't pulse in lockstep — same idea as c.Phase in TickHologramSigns
            private readonly float _phase;
            private DateTime _lastTick = DateTime.Now;
            private float _elapsed = 0f;
            private static readonly Random _rand = new Random();

            public CampFire()
            {
                _phase = (float)(_rand.NextDouble() * 2f * MathF.PI);
            }

            // ── Tick ──────────────────────────────────────────────
            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive) return;

                var now = DateTime.Now;
                _elapsed += (float)(now - _lastTick).TotalSeconds;
                _lastTick = now;

                // Smooth flame pulse, same shape as TickHologramSigns — ranges 0.2 .. 1.0
                float pulse = 0.6f + 0.4f * MathF.Sin(_elapsed * 5f + _phase);

                // Gentle high-frequency jitter on top — never fully dark
                float flutter = 0.9f + 0.1f * (float)_rand.NextDouble();

                // Hue stays warm orange — G/B drift slightly with the pulse for a bit of color life
                DynEmissiveColor = new Vector4(1f, 0.35f + 0.3f * pulse, 0.15f * pulse, 1f);

                // Intensity carries the main brightness swing — roughly 1.0 .. 2.4
                DynEmissiveIntensity = 1.0f + 1.4f * pulse * flutter;

                if (DynMesh != null)
                {
                    DynMesh.Color = DynEmissiveColor;
                    DynMesh.EmissiveIntensity = DynEmissiveIntensity;
                }
            }
        }

        // ── CampFire Registry ─────────────────────────────────────
        public static class CampFireRegistry
        {
            public static readonly List<CampFire> All = new();

            public static void Spawn(
                SpectralXScene scene,
                SpectralXMeshLibrary lib,
                int count,
                float radius,
                int seed)
            {
                All.Clear();
                var rand = new Random(seed);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) { Console.WriteLine("[DynManager] CampFire: PrimSquare missing"); return; }
                var scale = ToWorldScale(48f, 48f);

                for (int i = 0; i < count; i++)
                {
                    var obj = new CampFire
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    var mesh = BuildMesh($"DynCampfire_{i}", prim, scale, obj);
                    obj.DynMesh = mesh;
                    scene.AddMesh(mesh);
                    All.Add(obj);
                }
                Console.WriteLine($"[DynManager] Spawned {count} campfires");
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  CHEESE  —  16×16 px, restores hunger
        //  TODO: add CharHungerCurrent / CharHungerFull to ISpectralCharacter + SpectralXBloodWarrior
        // ═══════════════════════════════════════════════════════════

        public class Cheese : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 16f / Wpx;
            private float _w = 16f / Wpx, _h = 16f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.25f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/CHEEESE002.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;

                // TODO: uncomment once CharHungerCurrent / CharHungerFull added to ISpectralCharacter
                character.CharHungerCurrent = Math.Min(character.CharHungerCurrent + 50, character.CharHungerFull);

                DynIsActive = false;
                HideMesh(DynMesh);
            }
        }

        public static class CheeseRegistry
        {
            public static readonly List<Cheese> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 10);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(16f, 16f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new Cheese
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynCheese_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  SMALL HEAL POT  —  32×32 px, +10 HP
        // ═══════════════════════════════════════════════════════════

        public class HealPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 32f / Wpx;
            private float _w = 32f / Wpx, _h = 32f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.30f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/SmallPot.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;
                character.CharHitPoints = Math.Min(character.CharHitPoints + 10, character.CharMaxHP);
                DynIsActive = false;
                HideMesh(DynMesh);
            }
        }

        public static class HealPotRegistry
        {
            public static readonly List<HealPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 20);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(32f, 32f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new HealPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynHealPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  MED HEAL POT  —  48×48 px, +25 HP
        // ═══════════════════════════════════════════════════════════

        public class MedHealPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 48f / Wpx;
            private float _w = 48f / Wpx, _h = 48f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.35f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/healpot01.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;
                character.CharHitPoints = Math.Min(character.CharHitPoints + 25, character.CharMaxHP);
                DynIsActive = false;
                HideMesh(DynMesh);
            }
        }

        public static class MedHealPotRegistry
        {
            public static readonly List<MedHealPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 30);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(48f, 48f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new MedHealPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynMedHealPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  MANA POT  —  32×32 px, +5 resource
        // ═══════════════════════════════════════════════════════════

        public class ManaPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 32f / Wpx;
            private float _w = 32f / Wpx, _h = 32f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.30f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/ManaPot.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;
                character.CharResourceValue = Math.Min(character.CharResourceValue + 5, character.CharMaxResourceValue);
                DynIsActive = false;
                HideMesh(DynMesh);
            }
        }

        public static class ManaPotRegistry
        {
            public static readonly List<ManaPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 40);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(32f, 32f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new ManaPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynManaPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  STRENGTH ELIXIR  —  32×32 px, +5 STR for 10 s
        // ═══════════════════════════════════════════════════════════

        public class StrPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 32f / Wpx;
            private float _w = 32f / Wpx, _h = 32f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.30f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/StrElixer.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;
                character.CharStrength += 5;
                DynIsActive = false;
                HideMesh(DynMesh);
                _ = RemoveEffectAsync(character);
            }

            private static async Task RemoveEffectAsync(ISpectralCharacter c)
            {
                await Task.Delay(10_000);
                if (c.CharIsAlive) c.CharStrength -= 5;
            }
        }

        public static class StrPotRegistry
        {
            public static readonly List<StrPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 50);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(32f, 32f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new StrPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynStrPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  CELERITY ELIXIR  —  32×32 px, +5 CEL for 10 s
        // ═══════════════════════════════════════════════════════════

        public class CelPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 32f / Wpx;
            private float _w = 32f / Wpx, _h = 32f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.30f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/CelElixir.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;
                character.CharCelerity += 5;
                DynIsActive = false;
                HideMesh(DynMesh);
                _ = RemoveEffectAsync(character);
            }

            private static async Task RemoveEffectAsync(ISpectralCharacter c)
            {
                await Task.Delay(10_000);
                if (c.CharIsAlive) c.CharCelerity -= 5;
            }
        }

        public static class CelPotRegistry
        {
            public static readonly List<CelPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 60);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(32f, 32f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new CelPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynCelPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  ALACRITY ELIXIR  —  32×32 px, +5 ALC for 10 s
        // ═══════════════════════════════════════════════════════════

        public class AlcPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 32f / Wpx;
            private float _w = 32f / Wpx, _h = 32f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.30f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/AlacrityElixir.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;
                character.CharAlacrity += 5;
                DynIsActive = false;
                HideMesh(DynMesh);
                _ = RemoveEffectAsync(character);
            }

            private static async Task RemoveEffectAsync(ISpectralCharacter c)
            {
                await Task.Delay(10_000);
                if (c.CharIsAlive) c.CharAlacrity -= 5;
            }
        }

        public static class AlcPotRegistry
        {
            public static readonly List<AlcPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 70);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(32f, 32f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new AlcPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynAlcPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }


        // ═══════════════════════════════════════════════════════════
        //  INTELLIGENCE ELIXIR  —  32×32 px, +5 INT for 10 s
        //  TODO: add CharIntelligence to ISpectralCharacter + SpectralXBloodWarrior
        // ═══════════════════════════════════════════════════════════

        public class IntPot : IDynamicO
        {
            public float DynX { get; set; }
            public float DynY { get; set; }
            public float DynZ { get; set; } = 32f / Wpx;
            private float _w = 32f / Wpx, _h = 32f / Wpx;
            public float DynWidth { get => _w; set => _w = value; }
            public float DynHeight { get => _h; set => _h = value; }
            public float DynCollisionRadius => 0.30f;
            public SpectralXMesh? DynMesh { get; set; }
            public bool DynIsActive { get; private set; } = true;
            public string DynTexturePath => "/iAssets/IntElixir.png";
            public bool DynIsEmissive => false;
            public Vector4 DynEmissiveColor => Vector4.One;
            public float DynEmissiveIntensity => 0f;

            public void DynTickUpdate(ISpectralCharacter character)
            {
                if (!DynIsActive || !character.CharIsAlive) return;
                if (!IsNear(character, DynX, DynY, DynCollisionRadius)) return;

                // TODO: uncomment once CharIntelligence is added to ISpectralCharacter + SpectralXBloodWarrior
                 character.CharIntelligence += 5;
                 _ = RemoveEffectAsync(character);

                DynIsActive = false;
                HideMesh(DynMesh);
            }

            // TODO: uncomment when CharIntelligence is available
             private static async Task RemoveEffectAsync(ISpectralCharacter c)
             {
                await Task.Delay(10_000);
                 if (c.CharIsAlive) c.CharIntelligence -= 5;
             }
        }

        public static class IntPotRegistry
        {
            public static readonly List<IntPot> All = new();
            public static void Spawn(SpectralXScene scene, SpectralXMeshLibrary lib, int count, float radius, int seed)
            {
                All.Clear();
                var rand = new Random(seed + 80);
                var prim = lib.GetMesh("PrimSquare") as SpectralXMesh;
                if (prim == null) return;
                var scale = ToWorldScale(32f, 32f);
                for (int i = 0; i < count; i++)
                {
                    var obj = new IntPot
                    {
                        DynX = (float)(rand.NextDouble() * 2 - 1) * radius,
                        DynY = (float)(rand.NextDouble() * 2 - 1) * radius,
                    };
                    obj.DynMesh = BuildMesh($"DynIntPot_{i}", prim, scale, obj);
                    scene.AddMesh(obj.DynMesh);
                    All.Add(obj);
                }
            }
        }
    }


    // ═══════════════════════════════════════════════════════════════
    //  SPECTRAL DYN MANAGER
    //  Orchestrates all dynamic objects for BWP Scene4.
    //
    //  Usage:
    //    InitScene4()        → SpectralDynManager.SpawnAll(Scene4, MeshLibrary, ...)
    //    TickAndGetFrame()   → SpectralDynManager.TickAll(Warrior, _lastFrameDelta)
    //    SwitchToScene()     → SpectralDynManager.Clear()   (called by SpawnAll too)
    // ═══════════════════════════════════════════════════════════════

    public static class SpectralDynManager
    {
        // Default spawn counts — tune these for gameplay feel
        public static int CountCampfires = 8;
        public static int CountCheese = 12;
        public static int CountHealPot = 6;
        public static int CountMedHealPot = 3;
        public static int CountManaPot = 6;
        public static int CountStrPot = 4;
        public static int CountCelPot = 4;
        public static int CountAlcPot = 4;
        public static int CountIntPot = 4;

        /// <summary>
        /// Clear all registries and spawn fresh objects into scene.
        /// Call from InitScene4() after StaticObjects.SpawnAll().
        /// Uses seed + offset per type so each type scatters differently.
        /// </summary>
        public static void SpawnAll(
            SpectralXScene scene,
            SpectralXMeshLibrary lib,
            float spawnRadius = 60f,
            int seed = 42)
        {
            Clear();

            SpectralDynObjects.CampFireRegistry.Spawn(scene, lib, CountCampfires, spawnRadius, seed);
            SpectralDynObjects.CheeseRegistry.Spawn(scene, lib, CountCheese, spawnRadius, seed);
            SpectralDynObjects.HealPotRegistry.Spawn(scene, lib, CountHealPot, spawnRadius, seed);
            SpectralDynObjects.MedHealPotRegistry.Spawn(scene, lib, CountMedHealPot, spawnRadius, seed);
            SpectralDynObjects.ManaPotRegistry.Spawn(scene, lib, CountManaPot, spawnRadius, seed);
            SpectralDynObjects.StrPotRegistry.Spawn(scene, lib, CountStrPot, spawnRadius, seed);
            SpectralDynObjects.CelPotRegistry.Spawn(scene, lib, CountCelPot, spawnRadius, seed);
            SpectralDynObjects.AlcPotRegistry.Spawn(scene, lib, CountAlcPot, spawnRadius, seed);
            SpectralDynObjects.IntPotRegistry.Spawn(scene, lib, CountIntPot, spawnRadius, seed);

            Console.WriteLine("[SpectralDynManager] SpawnAll complete");
        }

        /// <summary>
        /// Tick all active dynamic objects.
        /// Call from TickAndGetFrame() when ActiveScene == BWPScene1 and Warrior != null.
        ///
        ///   if (ActiveScene == SceneID.BWPScene1 && Warrior != null)
        ///       SpectralDynManager.TickAll(Warrior, _lastFrameDelta);
        /// </summary>
        public static void TickAll(ISpectralCharacter character, float delta)
        {
            foreach (var o in SpectralDynObjects.CampFireRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.CheeseRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.HealPotRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.MedHealPotRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.ManaPotRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.StrPotRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.CelPotRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.AlcPotRegistry.All) o.DynTickUpdate(character);
            foreach (var o in SpectralDynObjects.IntPotRegistry.All) o.DynTickUpdate(character);
        }

        /// <summary>Clear all registries. Called automatically by SpawnAll(); also call from SwitchToScene().</summary>
        public static void Clear()
        {
            SpectralDynObjects.CampFireRegistry.All.Clear();
            SpectralDynObjects.CheeseRegistry.All.Clear();
            SpectralDynObjects.HealPotRegistry.All.Clear();
            SpectralDynObjects.MedHealPotRegistry.All.Clear();
            SpectralDynObjects.ManaPotRegistry.All.Clear();
            SpectralDynObjects.StrPotRegistry.All.Clear();
            SpectralDynObjects.CelPotRegistry.All.Clear();
            SpectralDynObjects.AlcPotRegistry.All.Clear();
            SpectralDynObjects.IntPotRegistry.All.Clear();
        }
    }
}