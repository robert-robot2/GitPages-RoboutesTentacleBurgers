using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;
using System;
using System.Numerics;
using static SpectralXGLX.BWP.SpectralXCow;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodTownSlut.TownSlut.
    /// Charm-based melee enemy — idles and attacks when the
    /// player enters influence/punch range. Uses Charm Influence
    /// as its class resource.
    /// </summary>
    public class SpectralXTownSlut : ISpectralEnemy
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? EnemyMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string EnemyClassName => "TownSlut";
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
        // TownSlut
        public int EnemyHitPoints { get; set; } = 100000; // unkillable by design — ambient trap, not a real enemy
        public int EnemyMaxHP { get; set; } = 100000;

   
        public int EnemyLevel { get; set; } = 1;
        public int EnemyXP { get; set; } = 0;
        public int EnemyXPPerLevel { get; set; } = 50;
        public int EnemyLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int EnemyStrength { get; set; } = 0;   // townSlutDamageAmount — 0 = she doesn't damage player (if intended)    public int EnemyStrength { get; set; } = 0;   // townSlutDamageAmount
        public int EnemyAlacrity { get; set; } = 1;   // attack cadence
        public int EnemyCelerity { get; set; } = 4;   // movement speed (mostly idle)
        public int EnemyLimenity
        {
            get => punchRange;
            set => punchRange = value;
        }
        public int EnemyIntelligence { get; set; } = 0; // SpellDamage
        public int EnemyLifeRegen { get; set; } = 0;
        public int EnemyStatPoints { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string EnemyHPColor => "rgba(255,20,147,.8)";
        public string EnemyInvColor => "rgba(255,182,193,1.0)";
        public string EnemyEnergyColor => "rgba(255,105,180,.7)";

        // ── Hunger (kept for parity, mostly unused) ───────────────
        public int EnemyHungerCurrent { get; set; } = 2000;
        public int EnemyHungerFull { get; set; } = 2000;
        public int EnemyHungerDurationSeconds { get; set; } = 86400;

        // ── Class Resource: Charm Influence ───────────────────────
        public int TownSlutInfluencePoints { get; set; } = 0;
        public int TownSlutMaxInfluencePoints { get; set; } = 10;
        public int TownSlutInfluenceOnHit { get; set; } = 1;

        public string EnemyResourceName => "Charm Influence";
        public int EnemyResourceValue { get => TownSlutInfluencePoints; set => TownSlutInfluencePoints = value; }
        public string EnemyRegenLabel => "Influence on Contact";
        public int EnemyRegenValue { get => TownSlutInfluenceOnHit; set => TownSlutInfluenceOnHit = value; }
        public string EnemyMaxResourceName => "Max Charm Influence";
        public int EnemyMaxResourceValue { get => TownSlutMaxInfluencePoints; set => TownSlutMaxInfluencePoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        private int punchRange = 15;
        public float PunchRadius { get; set; } = 1.5f;

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.8f;

        // ── Hit / Death Phase ─────────────────────────────────────
        private enum TownSlutPhase { Alive, HitEffect, Dead }
        private TownSlutPhase _phase = TownSlutPhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.4f, 0.7f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Animation State ───────────────────────────────────────
        public enum TownSlutAnimationState
        {
            Idle,
            Attack
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private TownSlutAnimationState _currentAnimation = TownSlutAnimationState.Idle;
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
        public static readonly Dictionary<TownSlutAnimationState, string> SpritePaths = new()
        {
            { TownSlutAnimationState.Idle,   "/iAssets/BTSlut001.png"      },
            { TownSlutAnimationState.Attack, "/iAssets/HarlotAttack002.png" },
        };

        public const string DeadSpritePath = "/iAssets/SkeleCooked01.png";
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";
        public string CharHitTexturePath => HitEffectPath;
        public string CharDeadTexturePath => DeadSpritePath;
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string EnemyHitOverlayTexturePath => HitOverlayTexturePath;

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<TownSlutAnimationState, int> FrameCounts = new()
        {
            { TownSlutAnimationState.Idle,   12 },
            { TownSlutAnimationState.Attack, 16 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<TownSlutAnimationState, float> AnimSpeeds = new()
        {
            { TownSlutAnimationState.Idle,   0.05f },
            { TownSlutAnimationState.Attack, 0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 64f;
        public const float FrameH = 64f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<TownSlutAnimationState, float> SheetWidths = new()
        {
            { TownSlutAnimationState.Idle,   12 * FrameW },
            { TownSlutAnimationState.Attack, 16 * FrameW },
        };

        // ── Corpse Linger ─────────────────────────────────────────
        private float _deadTimer = 0f;
        public const float CorpseLingerDuration = 20f;
        public bool IsCorpseExpired => !EnemyIsAlive && _deadTimer >= CorpseLingerDuration;

        // ── Patrol Bounds (mostly idle NPC) ───────────────────────
        public float PatrolLeftBound { get; set; } = 24f;
        public float PatrolRightBound { get; set; } = 1994f;
        private bool _movingRight = true;

        // ── Constructor ───────────────────────────────────────────
        public SpectralXTownSlut()
        {
            Console.WriteLine("[SpectralXTownSlut] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        public void InitMesh(SpectralXMesh mesh, float spawnX, float spawnY, float spawnZ,
       SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            EnemyMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            // SpectralXTownSlut.cs — InitMesh()
            EnemyMesh.Size = new Vector3(1.0f, 1.0f, 1.0f); // was (1.5, 1.5, 1.5)
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
            Console.WriteLine($"[SpectralXTownSlut] Mesh initialized at ({spawnX},{spawnY},{spawnZ})");
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            if (_phase == TownSlutPhase.HitEffect)
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
                    SetAnimation(TownSlutAnimationState.Idle);
                }
            }
        }

        // ── TakeDamage ────────────────────────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == TownSlutPhase.Dead) return;

            EnemyHitPoints = Math.Max(EnemyHitPoints - amount, 0);
            TownSlutInfluencePoints = Math.Min(TownSlutInfluencePoints + TownSlutInfluenceOnHit, TownSlutMaxInfluencePoints);

            Console.WriteLine($"[TownSlut] TakeDamage:{amount} HP:{EnemyHitPoints}/{EnemyMaxHP} Influence:{TownSlutInfluencePoints}/{TownSlutMaxInfluencePoints}");

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
            _phase = TownSlutPhase.HitEffect;
            ShowHitFlash = true;
            _hitFlashTimer = HitFlashDuration;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = TownSlutPhase.Alive;
            ShowHitFlash = false;

            if (EnemyMesh != null)
                EnemyMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = TownSlutPhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (EnemyMesh == null) return;

            switch (_phase)
            {
                case TownSlutPhase.HitEffect:
                    EnemyMesh.Color = ColorHit;
                    EnemyMesh.OverlayTextureDataUrl = EnemyHitOverlayTexturePath;
                    EnemyMesh.OverlayAlpha = 1f;
                    EnemyMesh.OverlayDirty = true;
                    EnemyMesh.TextureDirty = true;
                    break;

                case TownSlutPhase.Alive:
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

                case TownSlutPhase.Dead:
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

            SetAnimation(TownSlutAnimationState.Attack);
            _isOneShotAnimation = true;

            int frames = FrameCounts[TownSlutAnimationState.Attack];
            float perFrame = AnimSpeeds[TownSlutAnimationState.Attack];
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
                    SetAnimation(TownSlutAnimationState.Idle); // gliding approach
                }

                EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
                return;
            }

            // NPC: mostly idle when no target
            SetAnimation(TownSlutAnimationState.Idle);
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
            SetAnimation(TownSlutAnimationState.Idle);
            EnemyMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }


        // ─────────────────────────────────────────────────────────────
        // Animation
        // ─────────────────────────────────────────────────────────────
        private void SetAnimation(TownSlutAnimationState newState)
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
    }
}
