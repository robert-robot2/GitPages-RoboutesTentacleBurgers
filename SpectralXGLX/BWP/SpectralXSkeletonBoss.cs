using System;
using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 boss rebuild of SkeletonBoss.
    /// Large 256×256 sprite boss that patrols with
    /// flex/emote behavior and random tired slowdowns.
    ///
    /// Resource: Bone Rage — gains on hit, reserved for
    /// future boss special attacks.
    /// </summary>
    public class SpectralXSkeletonBoss : ISpectralEnemy
    {
        // ─────────────────────────────────────────────────────────
        // Mesh Reference
        // ─────────────────────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ─────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────
        public string EnemyClassName => "SkeletonBoss";
        public bool EnemyIsAlive => EnemyHitPoints > 0;
        public bool IsDead => !EnemyIsAlive;

        // ─────────────────────────────────────────────────────────
        // World Position
        // ─────────────────────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;
        private float _spawnX = 0f;
        private float _spawnY = 0f;
        public const float PatrolRadius = 12f;

        // ─────────────────────────────────────────────────────────
        // Aggression Target
        // ─────────────────────────────────────────────────────────
        public bool HasAggressionTarget => _aggroTarget != null && _aggroTarget.CharIsAlive;
        private ISpectralCharacter? _aggroTarget;

        public void SetAggressionTarget(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            _aggroTarget = target;
        }

        // ─────────────────────────────────────────────────────────
        // Core Stats
        // ─────────────────────────────────────────────────────────
        public int EnemyHitPoints { get; set; } = 1000;
        public int EnemyMaxHP { get; set; } = 1000;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ─────────────────────────────────────────────────────────
        // Combat Stats
        // ─────────────────────────────────────────────────────────
        public int EnemyStrength { get; set; } = 10;
        public int EnemyAlacrity { get; set; } = 5;
        public int EnemyCelerity { get; set; } = 7;
        public int EnemyLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ─────────────────────────────────────────────────────────
        // Color Theme
        // ─────────────────────────────────────────────────────────
        public string EnemyHPColor => "rgba(255,0,0,.7)";
        public string EnemyInvColor => "rgba(255,100,0,1.0)";
        public string EnemyEnergyColor => "rgba(255,100,0,.7)";

        // ─────────────────────────────────────────────────────────
        // Hunger (unused, kept for interface parity)
        // ─────────────────────────────────────────────────────────
        public int EnemyHungerCurrent { get; set; } = 0;
        public int EnemyHungerFull { get; set; } = 0;
        public int EnemyHungerDurationSeconds { get; set; } = 0;

        // ─────────────────────────────────────────────────────────
        // Class Resource: Bone Rage
        // ─────────────────────────────────────────────────────────
        public int EnemyRagePoints { get; set; } = 0;
        public int EnemyMaxRagePoints { get; set; } = 10;
        public int EnemyRageOnHit { get; set; } = 1;

        public string EnemyResourceName => "Bone Rage";
        public int EnemyResourceValue { get => EnemyRagePoints; set => EnemyRagePoints = value; }
        public string EnemyRegenLabel => "Rage on Hit";
        public int EnemyRegenValue { get => EnemyRageOnHit; set => EnemyRageOnHit = value; }
        public string EnemyMaxResourceName => "Max Rage";
        public int EnemyMaxResourceValue { get => EnemyMaxRagePoints; set => EnemyMaxRagePoints = value; }

        // ─────────────────────────────────────────────────────────
        // Punch / Reach
        // ─────────────────────────────────────────────────────────
        public float PunchRadius { get; set; } = 0.6f; // Limenity 6

        // ─────────────────────────────────────────────────────────
        // Collision
        // ─────────────────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;

        // ─────────────────────────────────────────────────────────
        // Hit / Death Phase
        // ─────────────────────────────────────────────────────────
        private enum BossPhase { Alive, HitEffect, Dead }
        private BossPhase _phase = BossPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ─────────────────────────────────────────────────────────
        // Animation State
        // ─────────────────────────────────────────────────────────
        public enum BossAnimationState
        {
            Idle,
            WalkLeft,
            WalkRight,
            Attack,
            Flex,
            WalkUp,
            WalkDown
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private BossAnimationState _currentAnimation = BossAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private bool _facingRight = true;

        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime = 0f;

        // ─────────────────────────────────────────────────────────
        // Attack Cooldown
        // ─────────────────────────────────────────────────────────
        private float _attackCooldown = 0f;
        private const float AttackCooldownDuration = 0.6f;

        // ─────────────────────────────────────────────────────────
        // Sprite Sheet Paths (256×256 boss sheets)
        // ─────────────────────────────────────────────────────────
        public static readonly Dictionary<BossAnimationState, string> SpritePaths = new()
        {
            { BossAnimationState.Idle,      "/iAssets/SkeletonIdle01.png"     },
            { BossAnimationState.WalkLeft,  "/iAssets/SkeletonLeftWalk01.png" },
            { BossAnimationState.WalkRight, "/iAssets/SkeletonRightWalk01.png"},
            { BossAnimationState.Attack,    "/iAssets/SkeletonPunch01.png"    },
            { BossAnimationState.Flex,      "/iAssets/SkeletonFlex01.png"     },
            { BossAnimationState.WalkUp,    "/iAssets/SkeletonUpWalk01.png"   },
            { BossAnimationState.WalkDown,  "/iAssets/SkeletonDownWalk01.png" },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/SkeleHit01.png";
        public string CharHitTexturePath => "/iAssets/SkeleHit01.png";
        public string CharDeadTexturePath => "/iAssets/SkeleCooked01.png";
        public const string HitOverlayTexturePath = "/iAssets/SkeleHit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ─────────────────────────────────────────────────────────
        // Frame Counts
        // ─────────────────────────────────────────────────────────
        public static readonly Dictionary<BossAnimationState, int> FrameCounts = new()
        {
            { BossAnimationState.Idle,      6 },
            { BossAnimationState.WalkLeft,  6 },
            { BossAnimationState.WalkRight, 6 },
            { BossAnimationState.Attack,    6 },
            { BossAnimationState.Flex,      8 },
            { BossAnimationState.WalkUp,    8 },
            { BossAnimationState.WalkDown,  8 },
        };

        // ─────────────────────────────────────────────────────────
        // Animation Speeds (seconds per frame)
        // ─────────────────────────────────────────────────────────
        public static readonly Dictionary<BossAnimationState, float> AnimSpeeds = new()
        {
            { BossAnimationState.Idle,      0.12f },
            { BossAnimationState.WalkLeft,  0.05f },
            { BossAnimationState.WalkRight, 0.05f },
            { BossAnimationState.Attack,    0.05f },
            { BossAnimationState.Flex,      0.05f },
            { BossAnimationState.WalkUp,    0.05f },
            { BossAnimationState.WalkDown,  0.05f },
        };

        // ─────────────────────────────────────────────────────────
        // Frame Dimensions (boss size)
        // ─────────────────────────────────────────────────────────
        public const float FrameW = 256f;
        public const float FrameH = 256f;

        // ─────────────────────────────────────────────────────────
        // Sheet Widths per state
        // ─────────────────────────────────────────────────────────
        public static readonly Dictionary<BossAnimationState, float> SheetWidths = new()
        {
            { BossAnimationState.Idle,      6 * FrameW }, // 1536px
            { BossAnimationState.WalkLeft,  6 * FrameW }, // 1536px
            { BossAnimationState.WalkRight, 6 * FrameW }, // 1536px
            { BossAnimationState.Attack,    6 * FrameW }, // 1536px
            { BossAnimationState.Flex,      8 * FrameW }, // 2048px
            { BossAnimationState.WalkUp,    8 * FrameW }, // 2048px
            { BossAnimationState.WalkDown,  8 * FrameW }, // 2048px
        };

        // ─────────────────────────────────────────────────────────
        // Corpse Linger
        // ─────────────────────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 20f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ─────────────────────────────────────────────────────────
        // Patrol Bounds
        // ─────────────────────────────────────────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        public float PatrolTopBound { get; set; } = 24f;
        public float PatrolBottomBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ─────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────
        public SpectralXSkeletonBoss()
        {
            Console.WriteLine("[SpectralXSkeletonBoss] Created");
        }

        // ─────────────────────────────────────────────────────────
        // Mesh Init
        // ─────────────────────────────────────────────────────────
        public void InitMesh(SpectralXMesh mesh, float spawnX, float spawnY, float spawnZ,
            SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            EnemyMesh.Size = new Vector3(3.0f, 3.0f, 3.0f);
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
            Console.WriteLine($"[SpectralXSkeletonBoss] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ─────────────────────────────────────────────────────────
        // Tick
        // ─────────────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            if (_phase == BossPhase.HitEffect)
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
                    SetAnimation(BossAnimationState.Idle);
                }
            }
        }

        // ─────────────────────────────────────────────────────────
        // TakeDamage
        // ─────────────────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == BossPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[SkeletonBoss] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

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
            _phase = BossPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = BossPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = BossPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case BossPhase.HitEffect:
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;
                    break;

                case BossPhase.Alive:
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
                    EnemyMesh.Color = ColorNormal;
                    EnemyMesh.TextureDirty = true;
                    ApplyAnimationToMesh();
                    break;

                case BossPhase.Dead:
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

        // ─────────────────────────────────────────────────────────
        // Attack
        // ─────────────────────────────────────────────────────────
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

            SetAnimation(BossAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[BossAnimationState.Attack];
            float perFrame = AnimSpeeds[BossAnimationState.Attack];
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                if (target.CharIsAlive)
                    target.TakeDamage(EnemyStrength);
            };
        }
        // ─────────────────────────────────────────────────────────
        // Movement / Aggro AI
        // ─────────────────────────────────────────────────────────
        private enum PatrolDirection { Left, Right, Up, Down }
        private PatrolDirection? _currentDirection;
        private readonly Random _rng = new();

        private bool _isTired = false;
        private float _tiredTimer = 0f;

        public void EnemyMove(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (EnemyMesh == null) return;

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
                    return;
                }

                if (distSq > 0f)
                {
                    float dist = MathF.Sqrt(distSq);
                    float speed = EnemyCelerity * 0.07f;
                    float vx = dx / dist;
                    float vy = dy / dist;

                    WorldX -= vx * speed;
                    WorldY -= vy * speed;

                    if (MathF.Abs(vx) > MathF.Abs(vy))
                    {
                        if (vx > 0)
                        {
                            SetAnimation(BossAnimationState.WalkLeft);
                            _facingRight = true;   // flipped
                        }
                        else
                        {
                            SetAnimation(BossAnimationState.WalkRight);
                            _facingRight = false;  // flipped
                        }
                    }
                    else
                    {
                        if (vy > 0)
                            SetAnimation(BossAnimationState.WalkUp);
                        else
                            SetAnimation(BossAnimationState.WalkDown);
                    }
                }

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            // Boss patrol + flex/tired behavior
            if (_rng.NextDouble() < 0.01)
            {
                SetAnimation(BossAnimationState.Flex);
                return;
            }

            if (!_isTired && _rng.NextDouble() < 0.02)
            {
                _isTired = true;
                _tiredTimer = 1.5f;
            }

            if (_isTired)
            {
                _tiredTimer -= 0.016f;
                if (_tiredTimer <= 0f)
                    _isTired = false;
            }

            if (_currentDirection == null)
            {
                _currentDirection = GetRandomDirection();
            }

            float baseSpeed = EnemyCelerity * 0.05f;
            float moveSpeed = _isTired ? baseSpeed * 0.5f : baseSpeed;

            switch (_currentDirection)
            {
                case PatrolDirection.Right:
                    WorldX += moveSpeed;
                    SetAnimation(BossAnimationState.WalkRight);
                    _facingRight = false;  // flipped
                    if (WorldX >= PatrolRightBound)
                        _currentDirection = GetRandomDirection();
                    break;

                case PatrolDirection.Left:
                    WorldX -= moveSpeed;
                    SetAnimation(BossAnimationState.WalkLeft);
                    _facingRight = true;   // flipped
                    if (WorldX <= PatrolLeftBound)
                        _currentDirection = GetRandomDirection();
                    break;

                case PatrolDirection.Up:
                    WorldY -= moveSpeed;
                    SetAnimation(BossAnimationState.WalkUp);
                    if (WorldY <= PatrolTopBound)
                        _currentDirection = GetRandomDirection();
                    break;

                case PatrolDirection.Down:
                    WorldY += moveSpeed;
                    SetAnimation(BossAnimationState.WalkDown);
                    if (WorldY >= PatrolBottomBound)
                        _currentDirection = GetRandomDirection();
                    break;
            }

            WorldX = Math.Clamp(WorldX, PatrolLeftBound, PatrolRightBound);
            WorldY = Math.Clamp(WorldY, PatrolTopBound, PatrolBottomBound);

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        private PatrolDirection GetRandomDirection()
        {
            var dirs = Enum.GetValues(typeof(PatrolDirection));
            return (PatrolDirection)dirs.GetValue(_rng.Next(dirs.Length))!;
        }

        // ─────────────────────────────────────────────────────────
        // Animation
        // ─────────────────────────────────────────────────────────
        private void SetAnimation(BossAnimationState newState)
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

            Console.WriteLine($"[SkeletonBoss] Animation → {_currentAnimation}");
        }

        // ─────────────────────────────────────────────────────────
        // Height Follow
        // ─────────────────────────────────────────────────────────
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (EnemyMesh != null)
                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }
        // TODO: clamp movement to _spawnX/_spawnY + PatrolRadius
        // when no character is active — currently enemies run off map on character death
        // TODO: Boss special attacks using Bone Rage
        // TODO: Boss phase changes at HP thresholds
        // TODO: FX hooks for flex/emote and death
    }
}

