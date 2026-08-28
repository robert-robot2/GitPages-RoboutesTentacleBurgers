using System;
using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodSkelPyscho.
    /// Aggressive skeleton variant — immediately chases the active character
    /// instead of patrolling a small radius. Uses the same sprite sheets as
    /// Skeleton but with a pure aggro movement model.
    ///
    /// Resource: Bone Frenzy — gains on hit, currently unused (no special
    /// attack animation exists yet). Wiring is kept for future use.
    /// </summary>
    public class SpectralXPsychoSkeleton : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "PsychoSkeleton";
        public bool EnemyIsAlive => EnemyHitPoints > 0;
        public bool IsDead => !EnemyIsAlive;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;
        private float _spawnX = 0f;
        private float _spawnY = 0f;
        public const float PatrolRadius = 12f;

        // ── Aggression Target ─────────────────────────────────────
        public bool HasAggressionTarget => _aggroTarget != null && _aggroTarget.CharIsAlive;
        private ISpectralCharacter? _aggroTarget;

        public void SetAggressionTarget(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            _aggroTarget = target;
        }

        // ── Core Stats ────────────────────────────────────────────
        public int EnemyHitPoints { get; set; } = 4;
        public int EnemyMaxHP { get; set; } = 4;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 1;
        public int EnemyAlacrity { get; set; } = 2;
        public int EnemyCelerity { get; set; } = 11;
        public int EnemyLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string EnemyHPColor => "rgba(200,0,0,.8)";
        public string EnemyInvColor => "rgba(100,255,0,1.0)";
        public string EnemyEnergyColor => "rgba(150,0,150,.7)";

        // ── Hunger (unused, kept for interface parity) ────────────
        public int EnemyHungerCurrent { get; set; } = 0;
        public int EnemyHungerFull { get; set; } = 0;
        public int EnemyHungerDurationSeconds { get; set; } = 0;

        // ── Class Resource: Bone Frenzy ───────────────────────────
        public int EnemyRagePoints { get; set; } = 0;
        public int EnemyMaxRagePoints { get; set; } = 10;
        public int EnemyRageOnHit { get; set; } = 1;

        public string EnemyResourceName => "Bone Frenzy";
        public int EnemyResourceValue { get => EnemyRagePoints; set => EnemyRagePoints = value; }
        public string EnemyRegenLabel => "Frenzy on Hit";
        public int EnemyRegenValue { get => EnemyRageOnHit; set => EnemyRageOnHit = value; }
        public string EnemyMaxResourceName => "Max Bone Frenzy";
        public int EnemyMaxResourceValue { get => EnemyMaxRagePoints; set => EnemyMaxRagePoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 0.6f; // Limenity 6

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;

        // ── Hit / Death Phase ─────────────────────────────────────
        private enum PsychoPhase { Alive, HitEffect, Dead }
        private PsychoPhase _phase = PsychoPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Animation State ───────────────────────────────────────
        public enum PsychoAnimationState
        {
            Idle,
            WalkLeft,
            WalkRight,
            WalkUp,
            WalkDown,
            Attack
        }
        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private PsychoAnimationState _currentAnimation = PsychoAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private bool _facingRight = true;
        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime = 0f;
        // ── Attack Cooldown ───────────────────────────────────────
        private float _attackCooldown = 0f;
        private const float AttackCooldownDuration = 0.6f;

        // ── Sprite Sheet Paths ────────────────────────────────────
        // Reuses Skeleton sheets.
        public static readonly Dictionary<PsychoAnimationState, string> SpritePaths = new()
        {
            { PsychoAnimationState.Idle,      "/iAssets/SkeletonIdle01.png"     },
            { PsychoAnimationState.WalkLeft,  "/iAssets/SkeletonLeftWalk01.png" },
            { PsychoAnimationState.WalkRight, "/iAssets/SkeletonRightWalk01.png"},
            { PsychoAnimationState.Attack,    "/iAssets/SkeletonPunch01.png"    },
            { PsychoAnimationState.WalkUp,    "/iAssets/SkeletonUpWalk01.png"   },
            { PsychoAnimationState.WalkDown,  "/iAssets/SkeletonDownWalk01.png" },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";

        // Hit flash overlay path (kept for reference — actual flash is a
        // render-side brightness/tint boost rather than a texture swap)
        public const string HitEffectPath = "/iAssets/SkeleHit01.png";
        public string CharHitTexturePath => "/iAssets/SkeleHit01.png";
        public string CharDeadTexturePath => "/iAssets/SkeleCooked01.png";
        public const string HitOverlayTexturePath = "/iAssets/SkeleHit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<PsychoAnimationState, int> FrameCounts = new()
        {
            { PsychoAnimationState.Idle,      6 },
            { PsychoAnimationState.WalkLeft,  6 },
            { PsychoAnimationState.WalkRight, 6 },
            { PsychoAnimationState.Attack,    6 },
            { PsychoAnimationState.WalkUp,    8 },
            { PsychoAnimationState.WalkDown,  8 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<PsychoAnimationState, float> AnimSpeeds = new()
        {
            { PsychoAnimationState.Idle,      0.12f },
            { PsychoAnimationState.WalkLeft,  0.05f },
            { PsychoAnimationState.WalkRight, 0.05f },
            { PsychoAnimationState.Attack,    0.05f },
            { PsychoAnimationState.WalkUp,    0.05f },
            { PsychoAnimationState.WalkDown,  0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<PsychoAnimationState, float> SheetWidths = new()
        {
            { PsychoAnimationState.Idle,      6 * FrameW }, // 504px
            { PsychoAnimationState.WalkLeft,  6 * FrameW }, // 504px
            { PsychoAnimationState.WalkRight, 6 * FrameW }, // 504px
            { PsychoAnimationState.Attack,    6 * FrameW }, // 504px
            { PsychoAnimationState.WalkUp,    8 * FrameW }, // 672px
            { PsychoAnimationState.WalkDown,  8 * FrameW }, // 672px
        };

        // ── Corpse Linger ─────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 15f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ── Patrol Bounds Fallback (when no target) ───────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        public float PatrolTopBound { get; set; } = 24f;
        public float PatrolBottomBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ── Constructor ───────────────────────────────────────────
        public SpectralXPsychoSkeleton()
        {
            Console.WriteLine("[SpectralXPsychoSkeleton] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        public void InitMesh(SpectralXMesh mesh, float spawnX, float spawnY, float spawnZ,
          SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            EnemyMesh.Size = new Vector3(1f, 1f, 1f);
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
            Console.WriteLine($"[SpectralXPsychoSkeleton] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            // Hit phase
            if (_phase == PsychoPhase.HitEffect)
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
                EnemyHitPoints = Math.Min(EnemyHitPoints + 0, EnemyMaxHP); // placeholder
            }

            // One-shot animation completion
            if (_isOneShotAnimation)
            {
                _animationTimer += delta;

                float speed = AnimSpeeds.TryGetValue(_currentAnimation, out var s) ? s : 0.12f;
                int frames = FrameCounts.TryGetValue(_currentAnimation, out var f) ? f : 1;
                float totalDuration = speed * frames;

                if (_animationTimer >= totalDuration)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(PsychoAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == PsychoPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[SkelPyscho] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

            SpawnBoneSplatter(amount);

            if (!EnemyIsAlive)
                TransitionToDead();
            else
                TransitionToHitEffect();
        }

        // ── Bone Splatter (undead variant — same registry, bone texture) ──
        private static readonly Random _splatterRand = new Random();

        private void SpawnBoneSplatter(int amount)
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
                _runningTime,
                SplatterPuddleRegistry.BoneTexturePath);
        }

        private void TransitionToHitEffect()
        {
            _phase = PsychoPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = PsychoPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = PsychoPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case PsychoPhase.HitEffect:
                    // Single-frame hit texture
                    // this is error in the pipeline when texture hit enabled causes character to go invisble and not show red flash hit but does show texture
                    /*
                    CharMesh.IsAnimated = false;
                    CharMesh.FrameCount = 1;
                    CharMesh.SheetWidth = FrameW;
                    CharMesh.SheetHeight = FrameH;
                    CharMesh.FramePixelWidth = FrameW;
                    CharMesh.FramePixelHeight = FrameH;
                    CharMesh.UVScaleX = _facingRight ? 1f : -1f;
                    CharMesh.UVScaleY = 1f;
                    CharMesh.UVOffsetX = _facingRight ? 0f : 1f;
                    CharMesh.UVOffsetY = 0f;
                    CharMesh.TextureDataUrl = CharHitTexturePath;
                    */
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;

                    break;

                case PsychoPhase.Alive:
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

                case PsychoPhase.Dead:
                    // Single-frame dead texture
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
            float minDist = CollisionRadius + target.CollisionRadius + PunchRadius;

            if (distSq > minDist * minDist) return;

            _attackCooldown = AttackCooldownDuration;
            EnemyRagePoints = Math.Min(EnemyRagePoints + EnemyRageOnHit, EnemyMaxRagePoints);

            SetAnimation(PsychoAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[PsychoAnimationState.Attack];
            float perFrame = AnimSpeeds[PsychoAnimationState.Attack];
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                if (target.CharIsAlive)
                    target.TakeDamage(EnemyStrength);
            };
        }

        // ── Movement / Aggro AI ───────────────────────────────────
        public void EnemyMove(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (EnemyMesh == null) return;

            // Choose aggression target: explicit aggro or passed-in active
            ISpectralCharacter? effectiveTarget = null;
            if (HasAggressionTarget)
                effectiveTarget = _aggroTarget;
            else if (target != null && target.CharIsAlive)
                effectiveTarget = target;

            if (effectiveTarget != null && effectiveTarget.CharIsAlive)
            {
                // Attack check first
                float dx = WorldX - effectiveTarget.WorldX;
                float dy = WorldY - effectiveTarget.WorldY;
                float distSq = dx * dx + dy * dy;
                float attackRange = CollisionRadius + effectiveTarget.CollisionRadius + PunchRadius;

                if (distSq <= attackRange * attackRange)
                {
                    EnemyAttack(effectiveTarget);
                    return;
                }

                // Chase movement
                if (distSq > 0f)
                {
                    float dist = MathF.Sqrt(distSq);
                    float speed = EnemyCelerity * 0.05f;
                    float vx = dx / dist;
                    float vy = dy / dist;

                    // Move toward target (subtract direction)
                    WorldX -= vx * speed;
                    WorldY -= vy * speed;

                    // Facing + animation
                    if (MathF.Abs(vx) > MathF.Abs(vy))
                    {
                        if (vx > 0)
                        {
                            SetAnimation(PsychoAnimationState.WalkLeft);
                            _facingRight = true;   // flipped
                        }
                        else
                        {
                            SetAnimation(PsychoAnimationState.WalkRight);
                            _facingRight = false;  // flipped
                        }
                    }
                    else
                    {
                        if (vy > 0)
                            SetAnimation(PsychoAnimationState.WalkUp);
                        else
                            SetAnimation(PsychoAnimationState.WalkDown);
                    }
                }

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            // Fallback: simple horizontal patrol if no target
            float patrolSpeed = EnemyCelerity * 0.05f;

            if (_movingRight)
            {
                WorldX += patrolSpeed;
                SetAnimation(PsychoAnimationState.WalkRight);
                _facingRight = false;  // flipped

                if (WorldX >= PatrolRightBound)
                    _movingRight = false;
            }
            else
            {
                WorldX -= patrolSpeed;
                SetAnimation(PsychoAnimationState.WalkLeft);
                _facingRight = true;   // flipped

                if (WorldX <= PatrolLeftBound)
                    _movingRight = true;
            }

            WorldX = Math.Clamp(WorldX, PatrolLeftBound, PatrolRightBound);
            WorldY = Math.Clamp(WorldY, PatrolTopBound, PatrolBottomBound);

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // ── Animation ─────────────────────────────────────────────
        private void SetAnimation(PsychoAnimationState newState)
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
            float animSpeed = AnimSpeeds.TryGetValue(_currentAnimation, out var spd) ? spd : 0.12f;

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

            Console.WriteLine($"[SkelPyscho] Animation → {_currentAnimation}");
        }

        // ── Height Follow ─────────────────────────────────────────
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (EnemyMesh != null)
                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }
        // TODO: clamp movement to _spawnX/_spawnY + PatrolRadius
        // when no character is active — currently enemies run off map on character death
        // TODO: EnemySpecialAttack when special attack animations are ready
        // TODO: GetCollisionBox3D() when 3D collision bounds needed
        // TODO: SplatterPuddles when FX system is ported
        // TODO: Real grayscale shader flag instead of alpha-fade death state
    }
}
