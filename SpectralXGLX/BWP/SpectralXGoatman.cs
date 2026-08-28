using System;
using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of Goatman.
    /// Heavy melee charger with high HP and Goat Fury resource.
    /// </summary>
    public class SpectralXGoatman : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "Goatman";
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
        public int EnemyHitPoints { get; set; } = 30;
        public int EnemyMaxHP { get; set; } = 30;
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 3;
        public int EnemyAlacrity { get; set; } = 3;
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
        public string EnemyHPColor => "rgba(139,69,19,.8)";     // earthy brown
        public string EnemyInvColor => "rgba(34,139,34,1.0)";   // forest green
        public string EnemyEnergyColor => "rgba(255,215,0,.7)"; // golden fury

        // ── Hunger (unused, kept for interface parity) ────────────
        public int EnemyHungerCurrent { get; set; } = 0;
        public int EnemyHungerFull { get; set; } = 0;
        public int EnemyHungerDurationSeconds { get; set; } = 0;

        // ── Class Resource: Goat Fury ─────────────────────────────
        public int EnemyFuryPoints { get; set; } = 0;
        public int EnemyMaxFuryPoints { get; set; } = 10;
        public int EnemyFuryOnHit { get; set; } = 1;

        public string EnemyResourceName => "Goat Fury";
        public int EnemyResourceValue { get => EnemyFuryPoints; set => EnemyFuryPoints = value; }
        public string EnemyRegenLabel => "Fury on Charge";
        public int EnemyRegenValue { get => EnemyFuryOnHit; set => EnemyFuryOnHit = value; }
        public string EnemyMaxResourceName => "Max Goat Fury";
        public int EnemyMaxResourceValue { get => EnemyMaxFuryPoints; set => EnemyMaxFuryPoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 2.1f; // punchRange 21 → 2.1f

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.6f;

        // ── Hit / Death Phase ─────────────────────────────────────
        private enum GoatPhase { Alive, HitEffect, Dead }
        private GoatPhase _phase = GoatPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Animation State ───────────────────────────────────────
        public enum GoatmanAnimationState
        {
            Idle,
            WalkLeft,
            WalkRight,
            Attack
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private GoatmanAnimationState _currentAnimation = GoatmanAnimationState.Idle;
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
        public static readonly Dictionary<GoatmanAnimationState, string> SpritePaths = new()
        {
            { GoatmanAnimationState.Idle,      "/iAssets/GoatManIdle01.png"        },
            { GoatmanAnimationState.WalkLeft,  "/iAssets/GoatManWalkLeft01.png"    },
            { GoatmanAnimationState.WalkRight, "/iAssets/GoatManWalkRight01.png"   },
            { GoatmanAnimationState.Attack,    "/iAssets/GoatManAttack01.png"      },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";

        public string CharHitTexturePath => HitEffectPath;
        public string CharDeadTexturePath => DeadSpritePath;
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<GoatmanAnimationState, int> FrameCounts = new()
        {
            { GoatmanAnimationState.Idle,      12 },
            { GoatmanAnimationState.WalkLeft,  8 },
            { GoatmanAnimationState.WalkRight, 8 },
            { GoatmanAnimationState.Attack,    12 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<GoatmanAnimationState, float> AnimSpeeds = new()
        {
            { GoatmanAnimationState.Idle,      0.05f },
            { GoatmanAnimationState.WalkLeft,  0.05f },
            { GoatmanAnimationState.WalkRight, 0.05f },
            { GoatmanAnimationState.Attack,    0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<GoatmanAnimationState, float> SheetWidths = new()
        {
            { GoatmanAnimationState.Idle,      12 * FrameW }, // 1008px
            { GoatmanAnimationState.WalkLeft,  8 * FrameW },  // 672px
            { GoatmanAnimationState.WalkRight, 8 * FrameW },  // 672px
            { GoatmanAnimationState.Attack,    12 * FrameW }, // 1008px
        };

        // ── Corpse Linger ─────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 20f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ── Patrol Bounds Fallback ────────────────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        public float PatrolTopBound { get; set; } = 24f;
        public float PatrolBottomBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ── Constructor ───────────────────────────────────────────
        public SpectralXGoatman()
        {
            Console.WriteLine("[SpectralXGoatman] Created");
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
            Console.WriteLine($"[SpectralXGoatman] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            if (_phase == GoatPhase.HitEffect)
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

                float speed = AnimSpeeds.TryGetValue(_currentAnimation, out var s) ? s : 0.05f;
                int frames = FrameCounts.TryGetValue(_currentAnimation, out var f) ? f : 1;
                float totalDuration = speed * frames;

                if (_animationTimer >= totalDuration)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(GoatmanAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == GoatPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            Console.WriteLine($"[Goatman] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP}");

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
            _phase = GoatPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = GoatPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = GoatPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case GoatPhase.HitEffect:
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;
                    break;

                case GoatPhase.Alive:
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

                case GoatPhase.Dead:
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
            EnemyFuryPoints = Math.Min(EnemyFuryPoints + EnemyFuryOnHit, EnemyMaxFuryPoints);

            SetAnimation(GoatmanAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[GoatmanAnimationState.Attack];
            float perFrame = AnimSpeeds[GoatmanAnimationState.Attack];
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
                    float speed = EnemyCelerity * 0.06f; // slightly faster charger
                    float vx = dx / dist;
                    float vy = dy / dist;

                    WorldX -= vx * speed;
                    WorldY -= vy * speed;

                    if (MathF.Abs(vx) > MathF.Abs(vy))
                    {
                        if (vx > 0)
                        {
                            SetAnimation(GoatmanAnimationState.WalkLeft);
                            _facingRight = true;   // flipped
                        }
                        else
                        {
                            SetAnimation(GoatmanAnimationState.WalkRight);
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

            float patrolSpeed = EnemyCelerity * 0.04f;

            if (_movingRight)
            {
                WorldX += patrolSpeed;
                SetAnimation(GoatmanAnimationState.WalkRight);
                _facingRight = false;  // flipped

                if (WorldX >= PatrolRightBound)
                    _movingRight = false;
            }
            else
            {
                WorldX -= patrolSpeed;
                SetAnimation(GoatmanAnimationState.WalkLeft);
                _facingRight = true;   // flipped

                if (WorldX <= PatrolLeftBound)
                    _movingRight = true;
            }

            WorldX = Math.Clamp(WorldX, PatrolLeftBound, PatrolRightBound);
            WorldY = Math.Clamp(WorldY, PatrolTopBound, PatrolBottomBound);

            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // ── Animation ─────────────────────────────────────────────
        private void SetAnimation(GoatmanAnimationState newState)
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

            Console.WriteLine($"[Goatman] Animation → {_currentAnimation}");
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
        // TODO: Charge special attack using Fury
        // TODO: SplatterPuddles when FX system is ported
        // TODO: Fury‑based buff system
    }
}
