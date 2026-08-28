using System;
using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of SkeletonWar.
    /// Warbone Authority skeleton — faster, more disciplined melee
    /// attacker with sword animations and authority resource.
    /// </summary>
    public class SpectralXSkeletonWar : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "SkeletonWar";
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
        public int EnemyHitPoints { get; set; } = 10;
        public int EnemyMaxHP { get; set; } = 10;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 2;
        public int EnemyAlacrity { get; set; } = 4;
        public int EnemyCelerity { get; set; } = 8;
        public int EnemyLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int EnemyIntelligence { get; set; } = 0;
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string EnemyHPColor => "rgba(70,130,180,.8)";    // steel blue
        public string EnemyInvColor => "rgba(192,192,192,1.0)"; // silver
        public string EnemyEnergyColor => "rgba(0,191,255,.7)"; // cyan authority

        // ── Hunger (unused, kept for interface parity) ────────────
        public int EnemyHungerCurrent { get; set; } = 0;
        public int EnemyHungerFull { get; set; } = 0;
        public int EnemyHungerDurationSeconds { get; set; } = 0;

        // ── Class Resource: Warbone Authority ─────────────────────
        public int EnemyAuthorityPoints { get; set; } = 0;
        public int EnemyMaxAuthorityPoints { get; set; } = 10;
        public int EnemyAuthorityOnHit { get; set; } = 1;

        public string EnemyResourceName => "Warbone Authority";
        public int EnemyResourceValue { get => EnemyAuthorityPoints; set => EnemyAuthorityPoints = value; }
        public string EnemyRegenLabel => "Authority on Strike";
        public int EnemyRegenValue { get => EnemyAuthorityOnHit; set => EnemyAuthorityOnHit = value; }
        public string EnemyMaxResourceName => "Max Warbone Authority";
        public int EnemyMaxResourceValue { get => EnemyMaxAuthorityPoints; set => EnemyMaxAuthorityPoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 1.5f; // punchRange 15 → 1.5f

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;

        // ── Hit / Death Phase ─────────────────────────────────────
        private enum WarPhase { Alive, HitEffect, Dead }
        private WarPhase _phase = WarPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Animation State ───────────────────────────────────────
        public enum SkeletonWarAnimationState
        {
            Idle,
            WalkLeft,
            WalkRight,
            Attack
        }
        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;
        private SkeletonWarAnimationState _currentAnimation = SkeletonWarAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private bool _facingRight = true;
        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime = 0f;
        // ── Attack Cooldown ───────────────────────────────────────
        private float _attackCooldown = 0f;
        private const float AttackCooldownDuration = 0.5f;

        // ── Sprite Sheet Paths ────────────────────────────────────
        public static readonly Dictionary<SkeletonWarAnimationState, string> SpritePaths = new()
        {
            { SkeletonWarAnimationState.Idle,      "/iAssets/SkeleSwordIdle01.png"   },
            { SkeletonWarAnimationState.WalkLeft,  "/iAssets/SkeleSwordLeft01.png"   },
            { SkeletonWarAnimationState.WalkRight, "/iAssets/SkeleSwordRight01.png"  },
            { SkeletonWarAnimationState.Attack,    "/iAssets/SkeleSwordAttack01.png" },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/SkeleHit01.png";

        public string CharHitTexturePath => HitEffectPath;
        public string CharDeadTexturePath => DeadSpritePath;
        public const string HitOverlayTexturePath = "/iAssets/SkeleHit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<SkeletonWarAnimationState, int> FrameCounts = new()
        {
            { SkeletonWarAnimationState.Idle,      7 },
            { SkeletonWarAnimationState.WalkLeft,  8 },
            { SkeletonWarAnimationState.WalkRight, 8 },
            { SkeletonWarAnimationState.Attack,    12 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<SkeletonWarAnimationState, float> AnimSpeeds = new()
        {
            { SkeletonWarAnimationState.Idle,      0.12f },
            { SkeletonWarAnimationState.WalkLeft,  0.05f },
            { SkeletonWarAnimationState.WalkRight, 0.05f },
            { SkeletonWarAnimationState.Attack,    0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<SkeletonWarAnimationState, float> SheetWidths = new()
        {
            { SkeletonWarAnimationState.Idle,      7 * FrameW },  // 588px
            { SkeletonWarAnimationState.WalkLeft,  8 * FrameW },  // 672px
            { SkeletonWarAnimationState.WalkRight, 8 * FrameW },  // 672px
            { SkeletonWarAnimationState.Attack,    12 * FrameW }, // 1008px
        };

        // ── Corpse Linger ─────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 15f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ── Patrol Bounds Fallback ────────────────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        public float PatrolTopBound { get; set; } = 24f;
        public float PatrolBottomBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ── Constructor ───────────────────────────────────────────
        public SpectralXSkeletonWar()
        {
            Console.WriteLine("[SpectralXSkeletonWar] Created");
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
            Console.WriteLine($"[SpectralXSkeletonWar] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            if (_phase == WarPhase.HitEffect)
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
                    SetAnimation(SkeletonWarAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == WarPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[SkeletonWar] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

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
            _phase = WarPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = WarPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = WarPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case WarPhase.HitEffect:
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;
                    break;

                case WarPhase.Alive:
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

                case WarPhase.Dead:
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
            EnemyAuthorityPoints = Math.Min(EnemyAuthorityPoints + EnemyAuthorityOnHit, EnemyMaxAuthorityPoints);

            SetAnimation(SkeletonWarAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[SkeletonWarAnimationState.Attack];
            float perFrame = AnimSpeeds[SkeletonWarAnimationState.Attack];
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

            ISpectralCharacter? effectiveTarget = null;
            if (HasAggressionTarget)
                effectiveTarget = _aggroTarget;
            else if (target != null && target.CharIsAlive)
                effectiveTarget = target;

            if (effectiveTarget != null && effectiveTarget.CharIsAlive)
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
                    float speed = EnemyCelerity * 0.05f;
                    float vx = dx / dist;
                    float vy = dy / dist;

                    WorldX -= vx * speed;
                    WorldY -= vy * speed;

                    if (MathF.Abs(vx) > MathF.Abs(vy))
                    {
                        if (vx > 0)
                        {
                            SetAnimation(SkeletonWarAnimationState.WalkLeft);
                            _facingRight = true;   // flipped
                        }
                        else
                        {
                            SetAnimation(SkeletonWarAnimationState.WalkRight);
                            _facingRight = false;  // flipped
                        }
                    }
                    else
                    {
                        // vertical movement keeps current horizontal anim
                    }
                }

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            float patrolSpeed = EnemyCelerity * 0.05f;

            if (_movingRight)
            {
                WorldX += patrolSpeed;
                SetAnimation(SkeletonWarAnimationState.WalkRight);
                _facingRight = false;  // flipped

                if (WorldX >= PatrolRightBound)
                    _movingRight = false;
            }
            else
            {
                WorldX -= patrolSpeed;
                SetAnimation(SkeletonWarAnimationState.WalkLeft);
                _facingRight = true;   // flipped

                if (WorldX <= PatrolLeftBound)
                    _movingRight = true;
            }

            WorldX = Math.Clamp(WorldX, PatrolLeftBound, PatrolRightBound);
            WorldY = Math.Clamp(WorldY, PatrolTopBound, PatrolBottomBound);

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // ── Animation ─────────────────────────────────────────────
        private void SetAnimation(SkeletonWarAnimationState newState)
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

            Console.WriteLine($"[SkeletonWar] Animation → {_currentAnimation}");
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
        // TODO: Sword special attack when animations exist
        // TODO: SplatterPuddles when FX system is ported
        // TODO: Authority‑based buff system
    }
}
