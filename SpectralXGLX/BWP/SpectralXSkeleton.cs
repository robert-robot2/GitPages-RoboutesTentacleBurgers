using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodEnemy.Skeleton.
    /// Renders as a PrimSquare billboard mesh in world space.
    /// Patrols within a small radius of its spawn point, attacks the
    /// active character on proximity, and swaps to a cooked sprite on death.
    ///
    /// TESTING NOTE: Patrol radius is clamped to 12f from spawn origin
    /// (instead of the old engine's full map bounds) so skeletons stay
    /// near the player during development.
    ///
    /// HP is intentionally low (2) to match the original, but may need
    /// to be bumped to 10-15 temporarily during testing if character
    /// damage output makes them die too fast to observe behavior.
    ///
    /// Resource: Bone Rage — gains on hit, currently unused (no special
    /// attack animation exists yet). Wiring is kept for future use.
    /// </summary>
    public class SpectralXSkeleton : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "Skeleton";
        public bool EnemyIsAlive => EnemyHitPoints > 0;
        public bool IsDead => !EnemyIsAlive;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        // ── Spawn / Patrol Origin ──────────────────────────────────
        // Recorded once in InitMesh — patrol logic stays within
        // PatrolRadius of this point instead of full map bounds.
        private float _spawnX = 0f;
        private float _spawnY = 0f;
        public const float PatrolRadius = 12f; // ← TESTING CLAMP

        // ── Core Stats ────────────────────────────────────────────
        public int EnemyHitPoints { get; set; } = 10;
        public int EnemyMaxHP { get; set; } = 10;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 1;
        public int EnemyAlacrity { get; set; } = 2;
        public int EnemyCelerity { get; set; } = 6;
        public int EnemyLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string EnemyHPColor => "rgba(255,0,0,.7)";
        public string EnemyInvColor => "rgba(255,100,0,1.0)";
        public string EnemyEnergyColor => "rgba(255,100,0,.7)";

        // ── Hunger (unused by Skeleton, kept for interface parity) ─────────
        public int EnemyHungerCurrent { get; set; } = 0;
        public int EnemyHungerFull { get; set; } = 0;
        public int EnemyHungerDurationSeconds { get; set; } = 0;

        // ── Class Resource: Bone Rage (unused for now) ──────────────────────
        public int EnemyRagePoints { get; set; } = 0;
        public int EnemyMaxRagePoints { get; set; } = 10;
        public int EnemyRageOnHit { get; set; } = 1;

        public string EnemyResourceName => "Bone Rage";
        public int EnemyResourceValue { get => EnemyRagePoints; set => EnemyRagePoints = value; }
        public string EnemyRegenLabel => "Rage on Hit";
        public int EnemyRegenValue { get => EnemyRageOnHit; set => EnemyRageOnHit = value; }
        public string EnemyMaxResourceName => "Max Rage";
        public int EnemyMaxResourceValue { get => EnemyMaxRagePoints; set => EnemyMaxRagePoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 0.6f; // Limenity 6

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;

        // ── Hit Flash (debug/testing visual feedback) ──────────────────────
        public bool ShowHitFlash { get; private set; } = false;
        // ── Hit / Death Phase ───────────────────────────────────────────
        private enum SkeletonPhase { Alive, HitEffect, Dead }
        private SkeletonPhase _phase = SkeletonPhase.Alive;

        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);



        // ── Animation State ───────────────────────────────────────
        public enum SkeletonAnimationState
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

        private SkeletonAnimationState _currentAnimation = SkeletonAnimationState.Idle;
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
        // 84×84 frames — same sheets as original BloodEnemy.Skeleton
        public static readonly Dictionary<SkeletonAnimationState, string> SpritePaths = new()
        {
            { SkeletonAnimationState.Idle,      "/iAssets/SkeletonIdle01.png"     },
            { SkeletonAnimationState.WalkLeft,  "/iAssets/SkeletonLeftWalk01.png" },
            { SkeletonAnimationState.WalkRight, "/iAssets/SkeletonRightWalk01.png"},
            { SkeletonAnimationState.Attack,    "/iAssets/SkeletonPunch01.png"    },
            { SkeletonAnimationState.Flex,      "/iAssets/SkeletonFlex01.png"     },
            { SkeletonAnimationState.WalkUp,    "/iAssets/SkeletonUpWalk01.png"   },
            { SkeletonAnimationState.WalkDown,  "/iAssets/SkeletonDownWalk01.png" },
        };

        // Dead/cooked sprite — single frame, swapped in once EnemyIsAlive is false
        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";

        // Hit flash overlay path (kept for reference — actual flash is a
        // render-side brightness/tint boost rather than a texture swap)
        public const string HitEffectPath = "/iAssets/SkeleHit01.png";
        public string CharHitTexturePath => "/iAssets/SkeleHit01.png";
        public string CharDeadTexturePath => "/iAssets/SkeleCooked01.png";

        public const string HitOverlayTexturePath = "/iAssets/SkeleHit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        // Mixed frame counts preserved exactly from the original.
        public static readonly Dictionary<SkeletonAnimationState, int> FrameCounts = new()
        {
            { SkeletonAnimationState.Idle,      6 },
            { SkeletonAnimationState.WalkLeft,  6 },
            { SkeletonAnimationState.WalkRight, 6 },
            { SkeletonAnimationState.Attack,    6 },
            { SkeletonAnimationState.Flex,      8 },
            { SkeletonAnimationState.WalkUp,    8 },
            { SkeletonAnimationState.WalkDown,  8 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<SkeletonAnimationState, float> AnimSpeeds = new()
        {
            { SkeletonAnimationState.Idle,      0.12f },
            { SkeletonAnimationState.WalkLeft,  0.05f },
            { SkeletonAnimationState.WalkRight, 0.05f },
            { SkeletonAnimationState.Attack,    0.05f },
            { SkeletonAnimationState.Flex,      0.05f },
            { SkeletonAnimationState.WalkUp,    0.05f },
            { SkeletonAnimationState.WalkDown,  0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<SkeletonAnimationState, float> SheetWidths = new()
        {
            { SkeletonAnimationState.Idle,      6 * FrameW }, // 504px
            { SkeletonAnimationState.WalkLeft,  6 * FrameW }, // 504px
            { SkeletonAnimationState.WalkRight, 6 * FrameW }, // 504px
            { SkeletonAnimationState.Attack,    6 * FrameW }, // 504px
            { SkeletonAnimationState.Flex,      8 * FrameW }, // 672px
            { SkeletonAnimationState.WalkUp,    8 * FrameW }, // 672px
            { SkeletonAnimationState.WalkDown,  8 * FrameW }, // 672px
        };

        // ── Patrol AI State ────────────────────────────────────────
        private enum PatrolDirection { Left, Right, Up, Down }
        private PatrolDirection? _currentDirection = null;
        private PatrolDirection? _lastPatrolDirection = null;
        private static readonly Random _rng = new();

        private float _idleUntilTimer = 0f;     // counts down — 0 means not idling
        private bool _isTired = false;
        private float _tiredUntilTimer = 0f;    // counts down — 0 means not tired
                                                // ── Corpse Linger (mirrors Dummy's DeadRemoveThreshold pattern) ───
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 15f; // seconds corpse stays visible
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;
        // ── Constructor ───────────────────────────────────────────
        public SpectralXSkeleton()
        {
            Console.WriteLine("[SpectralXSkeleton] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        /// <summary>
        /// Call after spawning the mesh. Records spawn position as the
        /// patrol origin and sets up initial animation state.
        /// </summary>
        public void InitMesh(SpectralXMesh mesh, float spawnX, float spawnY, float spawnZ,
            SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            EnemyMesh.Size = new Vector3(1f, 1f, 1f);
            EnemyMesh.CastsShadow = false;
            EnemyMesh.Color = new Vector4(1f, 1f, 1f, 1f);

            EnemyMesh.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );


            WorldX = spawnX;
            WorldY = spawnY;
            WorldZ = spawnZ;
            _spawnX = spawnX;
            _spawnY = spawnY;

            WorldZ = 0.1f;
            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);

            ApplyAnimationToMesh();
            Console.WriteLine($"[SpectralXSkeleton] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;

            // ── Hit Flash Phase ─────────────────────────────────────────────
            if (_phase == SkeletonPhase.HitEffect)
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
                return; // no animation/cooldown logic needed once dead
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

            if (_phase == SkeletonPhase.HitEffect)
            {
                _hitFlashTimer -= delta;
                if (EnemyMesh != null)
                    EnemyMesh.Color = ColorHit; // re-push every frame like Dummy does

                if (_hitFlashTimer <= 0f)
                {
                    _hitFlashTimer = 0f;
                    TransitionToAlive();
                }
            }

            if (EnemyLifeRegen > 0)
            {
                EnemyHitPoints = Math.Min(EnemyHitPoints + 0, EnemyMaxHP); // life regen tick placeholder — no timer wired yet
            }

            // ── One-shot animation completion check ───────────────
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
                    SetAnimation(SkeletonAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ─────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == SkeletonPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[Skeleton] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

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
            _phase = SkeletonPhase.HitEffect;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }


        private void TransitionToAlive()
        {
            _phase = SkeletonPhase.Alive;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }


        private void TransitionToDead()
        {
            _phase = SkeletonPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case SkeletonPhase.HitEffect:
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

                case SkeletonPhase.Alive:
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

                case SkeletonPhase.Dead:
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
        /// <summary>
        /// Proximity-based melee attack. Fires when the character is within
        /// punch range and the cooldown has elapsed. No special attack variant.
        /// </summary>
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
            EnemyRagePoints = Math.Min(EnemyRagePoints + EnemyRageOnHit, EnemyMaxRagePoints);

            SetAnimation(SkeletonAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[SkeletonAnimationState.Attack];
            float perFrame = AnimSpeeds[SkeletonAnimationState.Attack];
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                if (target.CharIsAlive)
                    target.TakeDamage(EnemyStrength);
            };
        }

        // ── Movement / Patrol AI ────────────────────────────────────
        /// <summary>
        /// Patrol + aggro driver. Stays within PatrolRadius of spawn point.
        /// Attempts an attack first if the target is in range; otherwise
        /// patrols randomly with idle pauses, tired slowdowns, and flex breaks.
        /// </summary>
        public void EnemyMove(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (EnemyMesh == null) return;

            // Attack check takes priority
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

            const float delta = 1f / 60f; // patrol AI ticks at an approximate fixed step

            // Idle timer countdown
            if (_idleUntilTimer > 0f)
            {
                if (_currentAnimation != SkeletonAnimationState.Idle && !_isOneShotAnimation)
                {
                    SetAnimation(SkeletonAnimationState.Idle);
                    _lastPatrolDirection = null;
                }
                _idleUntilTimer -= delta;
                return;
            }

            // Random chance to flex
            if (_rng.NextDouble() < 0.01)
            {
                SetAnimation(SkeletonAnimationState.Flex);
                _isOneShotAnimation = true;
                _idleUntilTimer = (float)(_rng.Next(300, 900) / 1000f);
                _lastPatrolDirection = null;
                return;
            }

            // Random chance to change direction mid-path
            if (_rng.NextDouble() < 0.03)
            {
                _currentDirection = GetRandomDirection();
                _idleUntilTimer = (float)(_rng.Next(200, 600) / 1000f);
            }

            // Random chance to get tired
            if (!_isTired && _rng.NextDouble() < 0.02)
            {
                _isTired = true;
                _tiredUntilTimer = (float)(_rng.Next(1000, 2000) / 1000f);
            }

            // Recover from tired
            if (_isTired)
            {
                _tiredUntilTimer -= delta;
                if (_tiredUntilTimer <= 0f)
                    _isTired = false;
            }

            // Auto-initialize patrol direction if not set
            if (_currentDirection == null)
                _currentDirection = GetRandomDirection();

            float moveSpeed = (_isTired ? EnemyCelerity * 0.5f : EnemyCelerity) * 0.05f;

            switch (_currentDirection)
            {
                case PatrolDirection.Right:
                    WorldX += moveSpeed;
                    if (_lastPatrolDirection != PatrolDirection.Right)
                    {
                        SetAnimation(SkeletonAnimationState.WalkRight);
                        _facingRight = false;  // flipped
                        _lastPatrolDirection = PatrolDirection.Right;
                    }
                    if (WorldX >= _spawnX + PatrolRadius)
                    {
                        _currentDirection = GetRandomDirection();
                        _idleUntilTimer = (float)(_rng.Next(300, 900) / 1000f);
                    }
                    break;

                case PatrolDirection.Left:
                    WorldX -= moveSpeed;
                    if (_lastPatrolDirection != PatrolDirection.Left)
                    {
                        SetAnimation(SkeletonAnimationState.WalkLeft);
                        _facingRight = true;   // flipped
                        _lastPatrolDirection = PatrolDirection.Left;
                    }
                    if (WorldX <= _spawnX - PatrolRadius)
                    {
                        _currentDirection = GetRandomDirection();
                        _idleUntilTimer = (float)(_rng.Next(300, 900) / 1000f);
                    }
                    break;

                case PatrolDirection.Up:
                    WorldY -= moveSpeed;
                    if (_lastPatrolDirection != PatrolDirection.Up)
                    {
                        SetAnimation(SkeletonAnimationState.WalkUp);
                        _lastPatrolDirection = PatrolDirection.Up;
                    }
                    if (WorldY <= _spawnY - PatrolRadius)
                    {
                        _currentDirection = GetRandomDirection();
                        _idleUntilTimer = (float)(_rng.Next(300, 900) / 1000f);
                    }
                    break;

                case PatrolDirection.Down:
                    WorldY += moveSpeed;
                    if (_lastPatrolDirection != PatrolDirection.Down)
                    {
                        SetAnimation(SkeletonAnimationState.WalkDown);
                        _lastPatrolDirection = PatrolDirection.Down;
                    }
                    if (WorldY >= _spawnY + PatrolRadius)
                    {
                        _currentDirection = GetRandomDirection();
                        _idleUntilTimer = (float)(_rng.Next(300, 900) / 1000f);
                    }
                    break;
            }

            // Clamp hard in case of overshoot — keeps skeleton inside the test radius
            WorldX = Math.Clamp(WorldX, _spawnX - PatrolRadius, _spawnX + PatrolRadius);
            WorldY = Math.Clamp(WorldY, _spawnY - PatrolRadius, _spawnY + PatrolRadius);

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        private PatrolDirection GetRandomDirection()
        {
            var dirs = Enum.GetValues(typeof(PatrolDirection));
            return (PatrolDirection)dirs.GetValue(_rng.Next(dirs.Length))!;
        }

        // ── Animation ─────────────────────────────────────────────
        private void SetAnimation(SkeletonAnimationState newState)
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
            if (!EnemyIsAlive) return; // dead sprite handled separately

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

            Console.WriteLine($"[Skeleton] Animation → {_currentAnimation}");
        }

        // ── Death Sprite ──────────────────────────────────────────
        private void ApplyDeadSpriteToMesh()
        {
            if (EnemyMesh == null) return;

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

            if (EnemyMesh.TextureDataUrl != DeadSpritePath)
            {
                EnemyMesh.TextureDataUrl = DeadSpritePath;
                EnemyMesh.TextureDirty = true;
                EnemyMesh.CurrentFrame = 0;
                EnemyMesh.FrameTimer = 0f;
            }

            // Faded/grayscale render — color alpha drop as a simple first pass.
            // True grayscale would need a shader flag; alpha fade is enough for now.
            EnemyMesh.Color = new Vector4(1f, 1f, 1f, 0.4f);

            Console.WriteLine("[Skeleton] Dead sprite applied");
        }

        // ── Height Follow ─────────────────────────────────────────
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (EnemyMesh != null)
                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // TODO: EnemySpecialAttack when special attack animations are ready
        // TODO: GetCollisionBox3D() when 3D collision bounds needed
        // TODO: SplatterPuddles when FX system is ported
        // TODO: Real grayscale shader flag instead of alpha-fade death state
    }
}