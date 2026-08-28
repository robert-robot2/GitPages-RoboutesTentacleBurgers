using System;
using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of ScavBoss.
    /// Fast, aggressive mini‑boss with high damage, high mobility,
    /// and a Boss Authority resource. Includes special attack animation.
    /// </summary>
    public class SpectralXScavBoss : ISpectralEnemy
    {
        // ─────────────────────────────────────────────────────────────
        // Mesh Reference
        // ─────────────────────────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ─────────────────────────────────────────────────────────────
        // Identity
        // ─────────────────────────────────────────────────────────────
        public string EnemyClassName => "ScavBoss";
        public bool EnemyIsAlive => EnemyHitPoints > 0;
        public bool IsDead => !EnemyIsAlive;

        // ─────────────────────────────────────────────────────────────
        // World Position
        // ─────────────────────────────────────────────────────────────
        public float WorldX { get; set; }
        public float WorldY { get; set; }
        public float WorldZ { get; set; }
        private float _spawnX = 0f;
        private float _spawnY = 0f;
        public const float PatrolRadius = 12f;

        // ─────────────────────────────────────────────────────────────
        // Aggression Target
        // ─────────────────────────────────────────────────────────────
        private ISpectralCharacter? _aggroTarget;
        public bool HasAggressionTarget => _aggroTarget != null && _aggroTarget.CharIsAlive;

        public void SetAggressionTarget(ISpectralCharacter target)
        {
            if (!EnemyIsAlive) return;
            _aggroTarget = target;
        }

        // ─────────────────────────────────────────────────────────────
        // Core Stats
        // ─────────────────────────────────────────────────────────────
        public int EnemyHitPoints { get; set; } = 15;
        public int EnemyMaxHP { get; set; } = 15;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ─────────────────────────────────────────────────────────────
        // Combat Stats
        // ─────────────────────────────────────────────────────────────
        public int EnemyStrength { get; set; } = 5;
        public int EnemyAlacrity { get; set; } = 3;
        public int EnemyCelerity { get; set; } = 12;
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        public float PunchRadius { get; set; } = 0.9f;
        public int EnemyLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }

        // ─────────────────────────────────────────────────────────────
        // Color Theme
        // ─────────────────────────────────────────────────────────────
        public string EnemyHPColor => "rgba(178,34,34,.8)";
        public string EnemyInvColor => "rgba(255,140,0,1.0)";
        public string EnemyEnergyColor => "rgba(255,255,0,.7)";
     
// Hunger (unused, kept for interface parity)
// ─────────────────────────────────────────────────────────────
public int EnemyHungerCurrent { get; set; } = 0;
        public int EnemyHungerFull { get; set; } = 0;
        public int EnemyHungerDurationSeconds { get; set; } = 0;
        // ─────────────────────────────────────────────────────────────
        // Resource: Boss Authority
        // ─────────────────────────────────────────────────────────────
        public int EnemyAuthorityPoints { get; set; } = 0;
        public int EnemyMaxAuthorityPoints { get; set; } = 10;
        public int EnemyAuthorityOnHit { get; set; } = 1;

        public string EnemyResourceName => "Boss Authority";
        public int EnemyResourceValue { get => EnemyAuthorityPoints; set => EnemyAuthorityPoints = value; }
        public string EnemyRegenLabel => "Authority on Command";
        public int EnemyRegenValue { get => EnemyAuthorityOnHit; set => EnemyAuthorityOnHit = value; }
        public string EnemyMaxResourceName => "Max Boss Authority";
        public int EnemyMaxResourceValue { get => EnemyMaxAuthorityPoints; set => EnemyMaxAuthorityPoints = value; }

        // ─────────────────────────────────────────────────────────────
        // Collision
        // ─────────────────────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.45f;

        // ─────────────────────────────────────────────────────────────
        // Hit / Death Phase
        // ─────────────────────────────────────────────────────────────
        private enum BossPhase { Alive, HitEffect, Dead }
        private BossPhase _phase = BossPhase.Alive;

        public bool ShowHitFlash { get; private set; }
        private float _hitFlashTimer;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new(1f, 1f, 1f, 0.4f);

        // ─────────────────────────────────────────────────────────────
        // Animation State
        // ─────────────────────────────────────────────────────────────
        public enum ScavBossAnimationState
        {
            Idle,
            WalkLeft,
            WalkRight,
            Attack,
            SpAttack
        }
        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private ScavBossAnimationState _currentAnimation = ScavBossAnimationState.Idle;
        private int _animationFrame;
        private float _animationTimer;
        private bool _isOneShotAnimation;
        private bool _facingRight = true;

        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime;

        // ─────────────────────────────────────────────────────────────
        // Attack Cooldown
        // ─────────────────────────────────────────────────────────────
        private float _attackCooldown;
        private const float AttackCooldownDuration = 0.5f;

        // ─────────────────────────────────────────────────────────────
        // Sprite Sheets
        // ─────────────────────────────────────────────────────────────
        public static readonly Dictionary<ScavBossAnimationState, string> SpritePaths = new()
        {
            { ScavBossAnimationState.Idle,      "/iAssets/ScavIdle001.png" },
            { ScavBossAnimationState.WalkLeft,  "/iAssets/ScavLeftWalk001.png" },
            { ScavBossAnimationState.WalkRight, "/iAssets/ScavRightWalk001.png" },
            { ScavBossAnimationState.Attack,    "/iAssets/ScavAttack001.png" },
            { ScavBossAnimationState.SpAttack,  "/iAssets/ScavSPAttack001.png" },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";

        public string CharHitTexturePath => HitEffectPath;
        public string CharDeadTexturePath => DeadSpritePath;
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ─────────────────────────────────────────────────────────────
        // Frame Counts
        // ─────────────────────────────────────────────────────────────
        public static readonly Dictionary<ScavBossAnimationState, int> FrameCounts = new()
        {
            { ScavBossAnimationState.Idle,      12 },
            { ScavBossAnimationState.WalkLeft,  8 },
            { ScavBossAnimationState.WalkRight, 8 },
            { ScavBossAnimationState.Attack,    12 },
            { ScavBossAnimationState.SpAttack,  11 },
        };

        // ─────────────────────────────────────────────────────────────
        // Animation Speeds
        // ─────────────────────────────────────────────────────────────
        public static readonly Dictionary<ScavBossAnimationState, float> AnimSpeeds = new()
        {
            { ScavBossAnimationState.Idle,      0.05f },
            { ScavBossAnimationState.WalkLeft,  0.05f },
            { ScavBossAnimationState.WalkRight, 0.05f },
            { ScavBossAnimationState.Attack,    0.05f },
            { ScavBossAnimationState.SpAttack,  0.05f },
        };

        // ─────────────────────────────────────────────────────────────
        // Frame Dimensions
        // ─────────────────────────────────────────────────────────────
        public const float FrameW = 48f;
        public const float FrameH = 48f;

        // ─────────────────────────────────────────────────────────────
        // Sheet Widths
        // ─────────────────────────────────────────────────────────────
        public static readonly Dictionary<ScavBossAnimationState, float> SheetWidths = new()
        {
            { ScavBossAnimationState.Idle,      12 * FrameW },
            { ScavBossAnimationState.WalkLeft,  8 * FrameW },
            { ScavBossAnimationState.WalkRight, 8 * FrameW },
            { ScavBossAnimationState.Attack,    12 * FrameW },
            { ScavBossAnimationState.SpAttack,  11 * FrameW },
        };

        // ─────────────────────────────────────────────────────────────
        // Corpse Linger
        // ─────────────────────────────────────────────────────────────
        private float _deadTimer;
        public const float CorpseLingerDuration = 20f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ─────────────────────────────────────────────────────────────
        // Patrol Bounds
        // ─────────────────────────────────────────────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        public float PatrolTopBound { get; set; } = 24f;
        public float PatrolBottomBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ─────────────────────────────────────────────────────────────
        // Constructor
        // ─────────────────────────────────────────────────────────────
        public SpectralXScavBoss()
        {
            Console.WriteLine("[SpectralXScavBoss] Created");
        }

        // ─────────────────────────────────────────────────────────────
        // Mesh Init
        // ─────────────────────────────────────────────────────────────
        public void InitMesh(SpectralXMesh mesh, float x, float y, float z,
         SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            // SpectralXScavBoss.cs — InitMesh()
            EnemyMesh.Size = new Vector3(0.6f, 0.6f, 0.6f); // was (1, 1, 1) — tune to taste
            EnemyMesh.CastsShadow = false;
            EnemyMesh.Color = ColorNormal;

            EnemyMesh.Rotation = new Vector3(
          5f * (MathF.PI / 180f),
         0f,
          0f
      );


            WorldX = x;
            WorldY = y;
            WorldZ = z;

            WorldZ = 0.1f;
            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);

            ApplyAnimationToMesh();
            Console.WriteLine($"[SpectralXScavBoss] Mesh initialized at ({x},{y},{z})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ─────────────────────────────────────────────────────────────
        // Tick
        // ─────────────────────────────────────────────────────────────
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

            if (_isOneShotAnimation)
            {
                _animationTimer += delta;

                float speed = AnimSpeeds[_currentAnimation];
                int frames = FrameCounts[_currentAnimation];
                float total = speed * frames;

                if (_animationTimer >= total)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(ScavBossAnimationState.Idle);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Damage
        // ─────────────────────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == BossPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[ScavBoss] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

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

        // ─────────────────────────────────────────────────────────────
        // Attack
        // ─────────────────────────────────────────────────────────────
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
            EnemyAuthorityPoints = Math.Min(EnemyAuthorityPoints + EnemyAuthorityOnHit, EnemyMaxAuthorityPoints);

            SetAnimation(ScavBossAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[ScavBossAnimationState.Attack];
            float perFrame = AnimSpeeds[ScavBossAnimationState.Attack];
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                if (target.CharIsAlive)
                    target.TakeDamage(EnemyStrength);
            };
        }
        // ─────────────────────────────────────────────────────────────
        // Movement / Aggro AI
        // ─────────────────────────────────────────────────────────────
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
                    float speed = EnemyCelerity * 0.07f; // boss is fast
                    float vx = dx / dist;
                    float vy = dy / dist;

                    WorldX -= vx * speed;
                    WorldY -= vy * speed;

                    if (MathF.Abs(vx) > MathF.Abs(vy))
                    {
                        if (vx > 0)
                        {
                            SetAnimation(ScavBossAnimationState.WalkLeft);
                            _facingRight = true;   // flipped
                        }
                        else
                        {
                            SetAnimation(ScavBossAnimationState.WalkRight);
                            _facingRight = false;  // flipped
                        }
                    }
                }

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            float patrolSpeed = EnemyCelerity * 0.04f;

            if (_movingRight)
            {
                WorldX += patrolSpeed;
                SetAnimation(ScavBossAnimationState.WalkRight);
                _facingRight = false;  // flipped

                if (WorldX >= PatrolRightBound)
                    _movingRight = false;
            }
            else
            {
                WorldX -= patrolSpeed;
                SetAnimation(ScavBossAnimationState.WalkLeft);
                _facingRight = true;   // flipped

                if (WorldX <= PatrolLeftBound)
                    _movingRight = true;
            }

            WorldX = Math.Clamp(WorldX, PatrolLeftBound, PatrolRightBound);
            WorldY = Math.Clamp(WorldY, PatrolTopBound, PatrolBottomBound);

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // ─────────────────────────────────────────────────────────────
        // Animation
        // ─────────────────────────────────────────────────────────────
        private void SetAnimation(ScavBossAnimationState newState)
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

            Console.WriteLine($"[ScavBoss] Animation → {_currentAnimation}");
        }

        // ─────────────────────────────────────────────────────────────
        // Terrain Height
        // ─────────────────────────────────────────────────────────────
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (EnemyMesh != null)
                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }
        // TODO: clamp movement to _spawnX/_spawnY + PatrolRadius
        // when no character is active — currently enemies run off map on character death
        // ─────────────────────────────────────────────────────────────
        // TODOs / Future Expansion
        // ─────────────────────────────────────────────────────────────
        // TODO: Implement special boss attack logic (SpAttack animation)
        // TODO: Add boss‑specific FX (shockwave, roar, authority burst)
        // TODO: Add corpse FX (boss puddle, smoke, fade‑out)
        // TODO: Add boss buff system (Authority thresholds)
        // TODO: Add multi‑phase boss behavior (phase 2 at 50% HP)
    }
}

