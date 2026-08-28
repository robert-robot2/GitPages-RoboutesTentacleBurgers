using System;
using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of Cow NPC.
    /// Massive, mostly-passive creature — idles and attacks if the
    /// player gets too close. Treated as an enemy for interface
    /// parity but usually spawned as a scene NPC.
    ///
    /// Resource: Milk Vitality — gains on graze/hit, currently unused
    /// for special attacks. Wiring is kept for future use.
    /// </summary>
    public class SpectralXCow : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "Cow";
        public bool EnemyIsAlive => EnemyHitPoints > 0;
        public bool IsDead => !EnemyIsAlive;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        // ── Aggression Target (usually unused for NPC cow) ────────
        /*
        public bool HasAggressionTarget => _aggroTarget != null && _aggroTarget.CharIsAlive;
        private ISpectralCharacter? _aggroTarget;

        public void SetAggressionTarget(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            _aggroTarget = target;
        }
        */
        // ── Core Stats ────────────────────────────────────────────
        public int EnemyHitPoints { get; set; } = 100000;
        public int EnemyMaxHP { get; set; } = 100000;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 1000;
        public int EnemyAlacrity { get; set; } = 1;
        public int EnemyCelerity { get; set; } = 4;
        public int EnemyLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string EnemyHPColor => "rgba(139,69,19,.8)";     // brown hide
        public string EnemyInvColor => "rgba(255,255,255,1.0)"; // white milk
        public string EnemyEnergyColor => "rgba(0,128,0,.7)";   // pasture green

        // ── Hunger (kept for parity, mostly unused) ───────────────
        public int EnemyHungerCurrent { get; set; } = 2000;
        public int EnemyHungerFull { get; set; } = 2000;
        public int EnemyHungerDurationSeconds { get; set; } = 86400;

        // ── Class Resource: Milk Vitality ─────────────────────────
        public int CowMilkPoints { get; set; } = 0;
        public int CowMaxMilkPoints { get; set; } = 1;
        public int CowMilkOnGraze { get; set; } = 10;

        public string EnemyResourceName => "Milk Vitality";
        public int EnemyResourceValue { get => CowMilkPoints; set => CowMilkPoints = value; }
        public string EnemyRegenLabel => "Milk on Graze";
        public int EnemyRegenValue { get => CowMilkOnGraze; set => CowMilkOnGraze = value; }
        public string EnemyMaxResourceName => "Max Milk Vitality";
        public int EnemyMaxResourceValue { get => CowMaxMilkPoints; set => CowMaxMilkPoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 1.5f; // Limenity 15

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.8f;

        // ── Hit / Death Phase ─────────────────────────────────────
        private enum CowPhase { Alive, HitEffect, Dead }
        private CowPhase _phase = CowPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.2f, 0.2f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Animation State ───────────────────────────────────────
        public enum CowAnimationState
        {
            Idle,
            Attack
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private CowAnimationState _currentAnimation = CowAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private bool _facingRight = true;

        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime = 0f;

        // ── Attack Cooldown ───────────────────────────────────────
        private float _attackCooldown = 0f;
        private const float AttackCooldownDuration = 1.0f;

        // ── Sprite Sheet Paths ────────────────────────────────────
        public static readonly Dictionary<CowAnimationState, string> SpritePaths = new()
        {
            { CowAnimationState.Idle,   "/iAssets/CowIdle01.png"   },
            { CowAnimationState.Attack, "/iAssets/CowAttack01.png" },
        };
        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";
        public string CharHitTexturePath => HitEffectPath;
        public string CharDeadTexturePath => DeadSpritePath;
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<CowAnimationState, int> FrameCounts = new()
        {
            { CowAnimationState.Idle,   12 },
            { CowAnimationState.Attack, 12 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<CowAnimationState, float> AnimSpeeds = new()
        {
            { CowAnimationState.Idle,   0.05f }, // 50ms
            { CowAnimationState.Attack, 0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<CowAnimationState, float> SheetWidths = new()
        {
            { CowAnimationState.Idle,   12 * FrameW }, // 1008px
            { CowAnimationState.Attack, 12 * FrameW }, // 1008px
        };

        // ── Corpse Linger ─────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 20f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ── Patrol Bounds ─────────────────────────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        public float PatrolTopBound { get; set; } = 24f;
        public float PatrolBottomBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ── Constructor ───────────────────────────────────────────
        public SpectralXCow()
        {
            Console.WriteLine("[SpectralXCow] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        public void InitMesh(SpectralXMesh mesh, float spawnX, float spawnY, float spawnZ,
      SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            EnemyMesh.Size = new Vector3(1.5f, 1.0f, 1.5f);
            EnemyMesh.CastsShadow = false;
            EnemyMesh.Color = ColorNormal;

            EnemyMesh.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );


            WorldX = spawnX;
            WorldY = spawnY;
            WorldZ = spawnZ;

            WorldZ = 0.1f;
            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);

            ApplyAnimationToMesh();
            Console.WriteLine($"[SpectralXCow] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            // Hit phase
            if (_phase == CowPhase.HitEffect)
            {
                _hitFlashTimer -= delta;

                if (EnemyMesh != null)
                    EnemyMesh.Color = ColorHit;

                if (_hitFlashTimer <= 0f)
                    TransitionToAlive();
            }

            if (EnemyMesh == null) return;

            if (!EnemyIsAlive)
            {
                _deadTimer += delta;
                return;
            }
            if (_attackDamagePending)
            {
                _attackDamageTimer -= delta;
                if (_attackDamageTimer <= 0f)
                {
                    _attackDamagePending = false;
                    _pendingAttackDamage?.Invoke();
                }
            }
            if (_attackCooldown > 0f)
                _attackCooldown -= delta;

            if (EnemyLifeRegen > 0)
            {
                EnemyHitPoints = Math.Min(EnemyHitPoints + EnemyLifeRegen, EnemyMaxHP);
            }

            // One-shot animation completion
            if (_isOneShotAnimation)
            {
                _animationTimer += delta;

                float speed = AnimSpeeds.TryGetValue(_currentAnimation, out var s) ? s : 0.05f;
                int frames = FrameCounts.TryGetValue(_currentAnimation, out var f) ? f : 1;
                float totalDuration = speed * frames;

                if (_animationTimer >= totalDuration)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(CowAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == CowPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[Cow] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

            SpawnBloodSplatter(amount);

            if (!EnemyIsAlive)
                TransitionToDead();
            else
                TransitionToHitEffect();
        }

        // ── Blood Splatter ────────────────────────────────────────
        private static readonly Random _splatterRand = new Random();

        private void SpawnBloodSplatter(int amount)
        {
            if (_scene == null || _meshLib == null) return;

            const float JitterPx = 5f;
            const float Wpx = 84f;
            float jitterRangeWorld = JitterPx / Wpx;

            float jitterX = ((float)_splatterRand.NextDouble() * 2f - 1f) * jitterRangeWorld;
            float jitterY = ((float)_splatterRand.NextDouble() * 2f - 1f) * jitterRangeWorld;

            float scaleMultiplier = MathF.Min(1.0f + amount * 0.25f, 3.0f)
                * (0.8f + (float)_splatterRand.NextDouble() * 0.4f);

            SplatterPuddleRegistry.Spawn(
                _scene,
                _meshLib,
                WorldX + jitterX,
                WorldY + jitterY,
                WorldZ,
                scaleMultiplier,
                _runningTime);
        }

        private void TransitionToHitEffect()
        {
            _phase = CowPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = CowPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = CowPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case CowPhase.HitEffect:
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;
                    break;

                case CowPhase.Alive:
                    EnemyMesh.IsAnimated = false;
                    EnemyMesh.FrameCount = 1;
                    EnemyMesh.SheetWidth = FrameW;
                    EnemyMesh.SheetHeight = FrameH;
                    EnemyMesh.FramePixelWidth = FrameW;
                    EnemyMesh.FramePixelHeight = FrameH;
                    EnemyMesh.UVScaleX = _facingRight ? 1f : -1f;
                    EnemyMesh.UVScaleY = 1f;
                    EnemyMesh.UVOffsetX = _facingRight ? 0f : 1f;
                    EnemyMesh.UVOffsetY = 0f;
                    // Restore animation sheet (use existing helper so behavior stays consistent)
                    EnemyMesh.Color = ColorNormal;
                    EnemyMesh.TextureDirty = true;
                    ApplyAnimationToMesh();
                    break;

                case CowPhase.Dead:
                    EnemyMesh.IsAnimated = false;
                    EnemyMesh.FrameCount = 1;
                    EnemyMesh.SheetWidth = FrameW;
                    EnemyMesh.SheetHeight = FrameH;
                    EnemyMesh.FramePixelWidth = FrameW;
                    EnemyMesh.FramePixelHeight = FrameH;
                    EnemyMesh.UVScaleX = 1f;
                    EnemyMesh.UVScaleY = 1f;
                    EnemyMesh.UVOffsetX = 0f;
                    EnemyMesh.UVOffsetY = 0f;
                    EnemyMesh.Color = ColorDead;
                    EnemyMesh.TextureDataUrl = CharDeadTexturePath;
                    EnemyMesh.TextureDirty = true;
                    break;
            }
        }

        // ── Attack ─────────────────────────────────────────────────
        public void EnemyAttack(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (target == null || !target.CharIsAlive) return;
            if (_attackCooldown > 0f) return;

            float dx = WorldX - target.WorldX;
            float dy = WorldY - target.WorldY;
            float distSq = dx * dx + dy * dy;
            float minDist = CollisionRadius + target.CollisionRadius;

            if (distSq > minDist * minDist) return;

            _attackCooldown = AttackCooldownDuration;

            SetAnimation(CowAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[CowAnimationState.Attack];
            float perFrame = AnimSpeeds[CowAnimationState.Attack];
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                if (target.CharIsAlive)
                    target.TakeDamage(EnemyStrength);
            };
        }
        /*
        // ── Movement / Aggro AI ───────────────────────────────────
        public void EnemyMove(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (EnemyMesh == null) return;

            // Cow is mostly NPC; it only chases if explicitly given a target.
            ISpectralCharacter? effectiveTarget =
                HasAggressionTarget ? _aggroTarget :
                (target != null && target.CharIsAlive ? target : null);

            if (effectiveTarget != null)
            {
                float dx = WorldX - effectiveTarget.WorldX;
                float dy = WorldY - effectiveTarget.WorldY;
                float distSq = dx * dx + dy * dy;

                float attackRange = CollisionRadius + effectiveTarget.CollisionRadius + PunchRadius;

                if (distSq <= attackRange * attackRange)
                {
                    EnemyAttack(effectiveTarget);
                    EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                    return;
                }

                if (distSq > 0f)
                {
                    float dist = MathF.Sqrt(distSq);
                    float speed = EnemyCelerity * 0.04f;
                    float vx = dx / dist;
                    float vy = dy / dist;

                    WorldX -= vx * speed;
                    WorldY -= vy * speed;

                    _facingRight = vx < 0;
                    SetAnimation(CowAnimationState.Idle); // lumbering movement
                }

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            // ─────────────────────────────────────────────────────────────
            // NPC Cow: No patrol, no chase, no wander — just idle unless attacked
            // ─────────────────────────────────────────────────────────────
            SetAnimation(CowAnimationState.Idle);
            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }
        */
        public void EnemyMove(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (EnemyMesh == null) return;

            // Attack check only — no chasing, no aggro resolution
            if (target != null && target.CharIsAlive)
            {
                float tdx = WorldX - target.WorldX;
                float tdy = WorldY - target.WorldY;
                float tDistSq = tdx * tdx + tdy * tdy;
                float attackRange = CollisionRadius + target.CollisionRadius;

                if (tDistSq <= attackRange * attackRange)
                {
                    EnemyAttack(target);
                    return;
                }
            }

            // Cow stays put — matches its original "no patrol, no chase, no wander" intent
            SetAnimation(CowAnimationState.Idle);
            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }



        // ─────────────────────────────────────────────────────────────
        // Animation
        // ─────────────────────────────────────────────────────────────
        private void SetAnimation(CowAnimationState newState)
        {
            if (newState == _currentAnimation && !_isOneShotAnimation) return;

            _currentAnimation = newState;
            _animationFrame = 0;
            _animationTimer = 0f;
            ApplyAnimationToMesh();
        }

        private void ApplyAnimationToMesh()
        {
            if (EnemyMesh == null) return;
            if (!EnemyIsAlive) return;

            float sheetW = SheetWidths.TryGetValue(_currentAnimation, out var sw) ? sw : FrameW;
            int frameCount = FrameCounts.TryGetValue(_currentAnimation, out var fc) ? fc : 1;
            float animSpeed = AnimSpeeds.TryGetValue(_currentAnimation, out var spd) ? spd : 0.05f;

            EnemyMesh.IsAnimated = true;
            EnemyMesh.FrameCount = frameCount;
            EnemyMesh.FrameRate = 1f / animSpeed;
            EnemyMesh.SheetWidth = sheetW;
            EnemyMesh.SheetHeight = FrameH;
            EnemyMesh.FramePixelWidth = FrameW;
            EnemyMesh.FramePixelHeight = FrameH;

            var newTexUrl = SpritePaths[_currentAnimation];
            if (EnemyMesh.TextureDataUrl != newTexUrl)
            {
                EnemyMesh.TextureDataUrl = newTexUrl;
                EnemyMesh.TextureDirty = true;
            }
            // always resync — SheetWidth/FrameCount above are unconditional, this has to be too
            EnemyMesh.CurrentFrame = 0;
            EnemyMesh.FrameTimer = 0f;
            float frameScale = FrameW / sheetW;
            EnemyMesh.UVScaleX = _facingRight ? frameScale : -frameScale;
            EnemyMesh.UVOffsetX = _facingRight ? 0f : frameScale;
            EnemyMesh.UVScaleY = 1f;
            EnemyMesh.FacingRight = _facingRight;
        }

        // ─────────────────────────────────────────────────────────────
        // Height Follow
        // ─────────────────────────────────────────────────────────────
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (EnemyMesh != null)
                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // ─────────────────────────────────────────────────────────────
        // TODO: Cow special graze animation, milk FX, idle chewing loop
        // TODO: Cow death FX (large dust puff, heavy collapse)
        // TODO: Cow resource interactions (milk collection)
        // TODO: Cow ambient sound hooks (moo, snort)
    }
}
