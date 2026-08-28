using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;
using System;
using System.Numerics;
using static SpectralXGLX.BWP.SpectralXCow;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodCat.Skeleton (Cat variant).
    /// Agile melee creature — idles, prowls, and attacks when the
    /// player enters claw range. Uses Bone Rage as its class resource.
    /// </summary>
    public class SpectralXCat : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "Cat";
        public bool EnemyIsAlive => EnemyHitPoints > 0;
        public bool IsDead => !EnemyIsAlive;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        /*
        // ── Aggression Target ─────────────────────────────────────
        public bool HasAggressionTarget => _aggroTarget != null && _aggroTarget.CharIsAlive;
        private ISpectralCharacter? _aggroTarget;

        public void SetAggressionTarget(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            _aggroTarget = target;
        }
        */
        // ── Core Stats ────────────────────────────────────────────
        public int EnemyHitPoints { get; set; } = 250;
        public int EnemyMaxHP { get; set; } = 5000;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 1;
        public int EnemyAlacrity { get; set; } = 5;
        public int EnemyCelerity { get; set; } = 7;
        public int EnemyLimenity
        {
            get => clawRange;
            set => clawRange = value;
        }
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string EnemyHPColor => "rgba(255,0,0,.7)";
        public string EnemyInvColor => "rgba(255,100,0,1.0)";
        public string EnemyEnergyColor => "rgba(255,100,0,.7)";

        // ── Hunger (unused) ───────────────────────────────────────
        public int EnemyHungerCurrent { get; set; } = 2000;
        public int EnemyHungerFull { get; set; } = 2000;
        public int EnemyHungerDurationSeconds { get; set; } = 86400;

        // ── Class Resource: Bone Rage ─────────────────────────────
        public int CatRagePoints { get; set; } = 0;
        public int CatMaxRagePoints { get; set; } = 10;
        public int CatRageOnHit { get; set; } = 1;

        public string EnemyResourceName => "Bone Rage";
        public int EnemyResourceValue { get => CatRagePoints; set => CatRagePoints = value; }
        public string EnemyRegenLabel => "Rage on Hit";
        public int EnemyRegenValue { get => CatRageOnHit; set => CatRageOnHit = value; }
        public string EnemyMaxResourceName => "Max Rage";
        public int EnemyMaxResourceValue { get => CatMaxRagePoints; set => CatMaxRagePoints = value; }

        // ── Claw / Reach ──────────────────────────────────────────
        private int clawRange = 6;
        public float PunchRadius { get; set; } = 0.6f;

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.4f;

        // ── Hit / Death Phase ─────────────────────────────────────
        private enum CatPhase { Alive, HitEffect, Dead }
        private CatPhase _phase = CatPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.3f, 0.3f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Animation State ───────────────────────────────────────
        public enum CatAnimationState
        {
            Idle,
            Walk,
            Attack,
            Flex
        }
        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;
        private CatAnimationState _currentAnimation = CatAnimationState.Idle;
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
        public static readonly Dictionary<CatAnimationState, string> SpritePaths = new()
        {
            { CatAnimationState.Idle,   "/iAssets/BCatIdle001.png"   },
            { CatAnimationState.Walk,   "/iAssets/BCatwalkR001.png"  },
            { CatAnimationState.Attack, "/iAssets/BCatAttack001.png" },
            { CatAnimationState.Flex,   "/iAssets/BCatwalkPD002.png" },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";
        public string CharHitTexturePath => HitEffectPath;
        public string CharDeadTexturePath => DeadSpritePath;
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<CatAnimationState, int> FrameCounts = new()
        {
            { CatAnimationState.Idle,   5 },
            { CatAnimationState.Walk,   8 },
            { CatAnimationState.Attack, 7 },
            { CatAnimationState.Flex,   1 },
        };

        // ── Animation Speeds ──────────────────────────────────────
        public static readonly Dictionary<CatAnimationState, float> AnimSpeeds = new()
        {
            { CatAnimationState.Idle,   0.05f },
            { CatAnimationState.Walk,   0.05f },
            { CatAnimationState.Attack, 0.05f },
            { CatAnimationState.Flex,   0.075f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 40f;
        public const float FrameH = 40f;

        // ── Sheet Widths ──────────────────────────────────────────
        public static readonly Dictionary<CatAnimationState, float> SheetWidths = new()
        {
            { CatAnimationState.Idle,   5 * FrameW },
            { CatAnimationState.Walk,   8 * FrameW },
            { CatAnimationState.Attack, 7 * FrameW },
            { CatAnimationState.Flex,   1 * FrameW },
        };

        // ── Corpse Linger ─────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 20f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ── Patrol Bounds ─────────────────────────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;

        private bool _movingRight = true;

        // ── Spawn / Patrol Origin ──────────────────────────────────
        private float _spawnX = 0f;
        private float _spawnY = 0f;
        public const float PatrolRadius = 12f;

        // ── Constructor ───────────────────────────────────────────
        public SpectralXCat()
        {
            Console.WriteLine("[SpectralXCat] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        public void InitMesh(SpectralXMesh mesh, float spawnX, float spawnY, float spawnZ,
       SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            EnemyMesh.Size = new Vector3(0.5f, 0.5f, 0.5f);
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
            Console.WriteLine($"[SpectralXCat] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;

        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            if (_phase == CatPhase.HitEffect)
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

            if (_isOneShotAnimation)
            {
                _animationTimer += delta;

                float speed = AnimSpeeds[_currentAnimation];
                int frames = FrameCounts[_currentAnimation];
                float totalDuration = speed * frames;

                if (_animationTimer >= totalDuration)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(CatAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == CatPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            CatRagePoints = Math.Min(CatRagePoints + CatRageOnHit, CatMaxRagePoints);

            Console.WriteLine($"[Cat] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP} Rage:{CatRagePoints}/{CatMaxRagePoints}");

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
            _phase = CatPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = CatPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = CatPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case CatPhase.HitEffect:
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;
                    break;

                case CatPhase.Alive:
                    EnemyMesh.IsAnimated = false;
                    EnemyMesh.FrameCount = 1;
                    EnemyMesh.SheetWidth = FrameW;
                    EnemyMesh.SheetHeight = FrameH;
                    EnemyMesh.FramePixelWidth = FrameW;
                    EnemyMesh.FramePixelHeight = FrameH;
                    EnemyMesh.UVScaleX = _facingRight ? -1f : 1f;
                    EnemyMesh.UVOffsetX = _facingRight ? 1f : 0f;
                    EnemyMesh.UVScaleY = 1f;            
                    EnemyMesh.UVOffsetY = 0f;
                    // Restore animation sheet (use existing helper so behavior stays consistent)
                    EnemyMesh.Color = ColorNormal;
                    EnemyMesh.TextureDirty = true;
                    ApplyAnimationToMesh();
                    break;

                case CatPhase.Dead:
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

            SetAnimation(CatAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[CatAnimationState.Attack];
            float perFrame = AnimSpeeds[CatAnimationState.Attack];
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                if (target.CharIsAlive)
                    target.TakeDamage(EnemyStrength);
            };
        }
        // ── Idle / Patrol Behavior State (frame-counted, no delta needed) ──
        private int _idleFramesRemaining = 0;
        private bool _isIdling = false;

        private bool _isTired = false;
        private int _tiredFramesRemaining = 0;

        private CatAnimationState? _lastPatrolAnim = null;
        private static readonly Random _catRng = new();
        // ── Movement / AI ───────────────────────────────────

        // ── Movement / AI ───────────────────────────────────
        public void EnemyMove(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            if (EnemyMesh == null) return;

            // Attack check only — no chasing, no aggro resolution
            if (target != null && target.CharIsAlive)
            {
                float dx = WorldX - target.WorldX;
                float dy = WorldY - target.WorldY;
                float distSq = dx * dx + dy * dy;
                float attackRange = CollisionRadius + target.CollisionRadius;

                if (distSq <= attackRange * attackRange)
                {
                    EnemyAttack(target);
                    EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                    return;
                }
            }

            // ── Idling — cat is paused (possibly flexing) ──
            if (_isIdling)
            {
                _idleFramesRemaining--;

                if (_currentAnimation != CatAnimationState.Idle && _currentAnimation != CatAnimationState.Flex)
                {
                    SetAnimation(CatAnimationState.Idle);
                    _lastPatrolAnim = null;
                }

                if (_idleFramesRemaining <= 0)
                    _isIdling = false;

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            // ── Random chance to flex (idle flavor) ──
            if (_catRng.NextDouble() < 0.01)
            {
                SetAnimation(CatAnimationState.Flex);
                _isOneShotAnimation = true;
                BeginIdle(_catRng.Next(20, 60)); // ~0.3-0.9s at 60fps
                return;
            }

            // ── Random chance to reverse direction mid-patrol ──
            if (_catRng.NextDouble() < 0.03)
            {
                _movingRight = !_movingRight;
                BeginIdle(_catRng.Next(12, 36)); // ~0.2-0.6s at 60fps
                return;
            }

            // ── Random chance to get tired ──
            if (!_isTired && _catRng.NextDouble() < 0.02)
            {
                _isTired = true;
                _tiredFramesRemaining = _catRng.Next(60, 120); // ~1-2s at 60fps
            }

            if (_isTired)
            {
                _tiredFramesRemaining--;
                if (_tiredFramesRemaining <= 0)
                    _isTired = false;
            }

            // ── Patrol left/right within PatrolRadius of spawn point ──
            float patrolSpeed = _isTired ? EnemyCelerity * 0.01f : EnemyCelerity * 0.02f;

            if (_movingRight)
            {
                WorldX += patrolSpeed;
                if (WorldX >= _spawnX + PatrolRadius)
                {
                    _movingRight = false;
                    BeginIdle(_catRng.Next(20, 60));
                }
                _facingRight = true;  // flipped
            }
            else
            {
                WorldX -= patrolSpeed;
                if (WorldX <= _spawnX - PatrolRadius)
                {
                    _movingRight = true;
                    BeginIdle(_catRng.Next(20, 60));
                }
                _facingRight = false;   // flipped
            }

            WorldX = Math.Clamp(WorldX, _spawnX - PatrolRadius, _spawnX + PatrolRadius);

            if (_lastPatrolAnim != CatAnimationState.Walk)
            {
                SetAnimation(CatAnimationState.Walk);
                _lastPatrolAnim = CatAnimationState.Walk;
            }

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        private void BeginIdle(int frames)
        {
            _isIdling = true;
            _idleFramesRemaining = frames;
            _lastPatrolAnim = null;
        }
     



        // ─────────────────────────────────────────────────────────────
        // Animation
        // ─────────────────────────────────────────────────────────────
        private void SetAnimation(CatAnimationState newState)
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

            float sheetW = SheetWidths[_currentAnimation];
            int frameCount = FrameCounts[_currentAnimation];
            float animSpeed = AnimSpeeds[_currentAnimation];

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
            EnemyMesh.UVScaleX = _facingRight ? -frameScale : frameScale;
            EnemyMesh.UVOffsetX = _facingRight ? frameScale : 0f;
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
    }
}
