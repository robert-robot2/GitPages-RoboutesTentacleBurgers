using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodRogue.
    /// Renders as a PrimSquare billboard mesh in world space.
    /// Animation state machine drives texture swap per state.
    /// Resource: Energy — regens over time, consumed by Dags special attack.
    /// All positions are world space floats.
    /// </summary>
    public class SpectralXRogue : ISpectralCharacter
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? CharMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string CharClassName => "Rogue";
        public bool CharIsAlive => CharHitPoints > 0;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        // ── Core Stats ────────────────────────────────────────────
        public int CharHitPoints { get; set; } = 12;
        public int CharMaxHP { get; set; } = 12;
        public int CharLevel { get; set; } = 1;
        public int CharXP { get; set; } = 0;
        public int CharXPPerLevel { get; set; } = 5;
        public int CharLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int CharStrength { get; set; } = 3;
        public int CharAlacrity { get; set; } = 3;
        public int CharCelerity { get; set; } = 3;
        public int CharLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int CharIntelligence { get; set; } = 0;
        public int CharLifeRegen { get; set; } = 0;
        public int CharStatPoints { get; set; } = 0;
        public bool RogueLevelUpTriggered { get; set; } = false;
        public int LastHPGain { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string CharHPColor => "rgba(255,0,0,.7)";
        public string CharInvColor => "rgba(0,255,0,1.0)";
        public string CharEnergyColor => "rgba(0,255,0,.7)";

        // ── Hunger ────────────────────────────────────────────────
        public int CharHungerCurrent { get; set; } = 2000;
        public int CharHungerFull { get; set; } = 2000;
        public int CharHungerDurationSeconds { get; set; } = 86400;

        // ── Class Resource: Energy ────────────────────────────────
        public int CharEnergyPoints { get; set; } = 10;
        public int CharMaxEnergyPoints { get; set; } = 10;
        public int CharEnergyRegenRate { get; set; } = 1;

        // ── ISpectralCharacter Resource Wiring ────────────────────
        public string CharResourceName => "Energy";
        public int CharResourceValue { get => CharEnergyPoints; set => CharEnergyPoints = value; }
        public string CharRegenLabel => "Energy Regen";
        public int CharRegenValue { get => CharEnergyRegenRate; set => CharEnergyRegenRate = value; }
        public string CharMaxResourceName => "Max Energy";
        public int CharMaxResourceValue { get => CharMaxEnergyPoints; set => CharMaxEnergyPoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 0.9f;   // CharLimenity 9 → 0.9f

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;
        public bool IsOneShotPlaying => _isOneShotAnimation;
        // ── Animation State ───────────────────────────────────────
        public enum RogueAnimationState
        {
            Idle,
            WalkDown,
            WalkUp,
            WalkLeft,
            WalkRight,
            Punch,
            Dags        // special attack — costs 2 energy, 2× damage
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private RogueAnimationState _currentAnimation = RogueAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private bool _facingRight = true;
        // ── Timers ────────────────────────────────────────────────
        private float _lifeRegenTimer = 0f;
        private float _energyRegenTimer = 0f;
        private float _hungerTimer = 0f;

        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime = 0f;

        // ── Attack Cooldown ───────────────────────────────────────
        private float _attackCooldown = 0f;
        private const float AttackCooldownDuration = 0.6f;

        // ── Stop Grace Period ─────────────────────────────────────
        private float _stopTimer = 0f;
        private const float StopDelay = 0.08f;
        // ── Hit / Death Sprites ──────────────────────────────────
        // Single-frame images — not part of the animation sheet dictionaries
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";   // ← confirm actual filename
        public const string DeadSpritePath = "/iAssets/RogueCooled01.png"; // ← confirm actual filename
        public string CharHitTexturePath => "/iAssets/WarriorGothit01.png";
        public string CharDeadTexturePath => "/iAssets/RogueCooled01.png";
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string CharHitOverlayTexturePath => HitOverlayTexturePath;
        private enum RoguePhase { Alive, HitEffect, Dead }
        private RoguePhase _phase = RoguePhase.Alive;

        public bool ShowHitFlash { get; private set; } = false;
        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f; // matches dummy's brief flash

        // ── Phase timers (delta based) ────────────────────────
        private float _hitTimer = 0f;   // counts up during HitEffect phase
        private float _deadTimer = 0f;   // counts up during Dead phase
        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);
        // ── Sprite Sheet Paths ────────────────────────────────────
        // 84×84 frames — same sheets as original BloodRogue
        public static readonly Dictionary<RogueAnimationState, string> SpritePaths = new()
        {
            { RogueAnimationState.Idle,      "/iAssets/RogueIdleCell01.png" },
            { RogueAnimationState.WalkDown,  "/iAssets/RogueDownCell01.png" },
            { RogueAnimationState.WalkUp,    "/iAssets/RogueUpCell01.png" },
            { RogueAnimationState.WalkLeft,  "/iAssets/RogueLeftCell01.png" },
            { RogueAnimationState.WalkRight, "/iAssets/RogueRightCell01.png" },
            { RogueAnimationState.Punch,     "/iAssets/RogueKickCell01.png" },
            { RogueAnimationState.Dags,      "/iAssets/RogueDags01.png" },
        };

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<RogueAnimationState, int> FrameCounts = new()
        {
            { RogueAnimationState.Idle,      20 },
            { RogueAnimationState.WalkDown,  8  },
            { RogueAnimationState.WalkUp,    8  },
            { RogueAnimationState.WalkLeft,  8  },
            { RogueAnimationState.WalkRight, 8  },
            { RogueAnimationState.Punch,     17 },  // Rogue kick has 17 frames
            { RogueAnimationState.Dags,      10 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<RogueAnimationState, float> AnimSpeeds = new()
        {
            { RogueAnimationState.Idle,      0.12f },
            { RogueAnimationState.WalkDown,  0.12f },
            { RogueAnimationState.WalkUp,    0.12f },
            { RogueAnimationState.WalkLeft,  0.12f },
            { RogueAnimationState.WalkRight, 0.12f },
            { RogueAnimationState.Punch,     0.05f },
            { RogueAnimationState.Dags,      0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<RogueAnimationState, float> SheetWidths = new()
        {
            { RogueAnimationState.Idle,      20 * FrameW },   // 1680px
            { RogueAnimationState.WalkDown,  8  * FrameW },   // 672px
            { RogueAnimationState.WalkUp,    8  * FrameW },   // 672px
            { RogueAnimationState.WalkLeft,  8  * FrameW },   // 672px
            { RogueAnimationState.WalkRight, 8  * FrameW },   // 672px
            { RogueAnimationState.Punch,     17 * FrameW },   // 1428px
            { RogueAnimationState.Dags,      10 * FrameW },   // 840px
        };

        // ── Constructor ───────────────────────────────────────────
        public SpectralXRogue()
        {
            Console.WriteLine("[SpectralXRogue] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        /// <summary>
        /// Call from InitScene after PrimSquare is available.
        /// Assigns the mesh and sets up initial animation state.
        /// </summary>
        public void InitMesh(SpectralXMesh mesh, SpectralXScene scene, SpectralXMeshLibrary lib)
        {
            CharMesh = mesh;
            _scene = scene;
            _meshLib = lib;
            CharMesh.Size = new Vector3(1f, 1f, 1f);
            CharMesh.CastsShadow = false;
            CharMesh.Color = new Vector4(1f, 1f, 1f, 1f);

            CharMesh.Rotation = new Vector3(
        5f * (MathF.PI / 180f),
       0f,
        0f
    );

            // Sync WorldZ from mesh position so Move() never resets it
            WorldZ = CharMesh.Position.Z;

            ApplyAnimationToMesh();
            Console.WriteLine("[SpectralXRogue] Mesh initialized");
        }

        // ── ISpectralCharacter.Move ───────────────────────────────
        public void Move(Vector2 isoDir)
        {
            if (!CharIsAlive) return;
            if (isoDir == Vector2.Zero) return;
            _stopTimer = StopDelay;

            float speed = CharCelerity * 0.05f;
            WorldX += isoDir.X * speed;  // was isoDir.Y
            WorldY -= isoDir.Y * speed;  // was isoDir.X
            if (CharMesh != null)
                CharMesh.Position = new Vector3(WorldX, WorldY, WorldZ);

            // in Move()
            var newState = GetStateFromDir(isoDir);
            if (_phase == RoguePhase.Alive && (newState != _currentAnimation || _isOneShotAnimation))
                SetAnimation(newState);
            _lastMoveDirection = isoDir;
            if (MathF.Abs(isoDir.X) > MathF.Abs(isoDir.Y))
                _facingRight = isoDir.X > 0;  
        }

        public void Stop()
        {
            if (_isOneShotAnimation) return;

            if (_stopTimer <= 0f)
            {
                SetAnimation(RogueAnimationState.Idle);
                _lastMoveDirection = Vector2.Zero;
            }
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            if (CharMesh == null) return;
            _runningTime += delta;
            if (_phase == RoguePhase.HitEffect)
            {
                _hitFlashTimer -= delta;
                CharMesh.Color = ColorHit; // re-push every frame, same pattern as Dummy/Skeleton

                if (_hitFlashTimer <= 0f)
                {
                    _hitFlashTimer = 0f;
                    TransitionToAlive();
                }
            }

            if (!CharIsAlive) return; // dead — nothing further to tick today

            if (_stopTimer > 0f)
                _stopTimer -= delta;
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

            // ── Life regen ────────────────────────────────────────
            if (CharLifeRegen > 0)
            {
                _lifeRegenTimer += delta;
                if (_lifeRegenTimer >= 1f)
                {
                    CharHitPoints = Math.Min(CharHitPoints + CharLifeRegen, CharMaxHP);
                    _lifeRegenTimer = 0f;
                }
            }

            // ── Energy regen ──────────────────────────────────────
            _energyRegenTimer += delta;
            if (_energyRegenTimer >= 1f)
            {
                CharEnergyPoints = Math.Min(CharEnergyPoints + CharEnergyRegenRate, CharMaxEnergyPoints);
                _energyRegenTimer = 0f;
            }

            // ── Hunger degen ──────────────────────────────────────
            if (CharHungerCurrent > 0)
            {
                _hungerTimer += delta;
                if (_hungerTimer >= 1f)
                {
                    float perSecondLoss = (float)CharHungerFull / CharHungerDurationSeconds;
                    CharHungerCurrent = (int)Math.Max(0, CharHungerCurrent - perSecondLoss);
                    _hungerTimer = 0f;
                }
            }

            // ── One-shot animation completion check ───────────────
            if (_isOneShotAnimation && _phase == RoguePhase.Alive)
            {
                _animationTimer += delta;

                float speed = AnimSpeeds.TryGetValue(_currentAnimation, out var s) ? s : 0.12f;
                int frames = FrameCounts.TryGetValue(_currentAnimation, out var f) ? f : 1;
                float totalDuration = speed * frames;

                if (_animationTimer >= totalDuration)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(RogueAnimationState.Idle);
                }
            }
        }

        // ── ISpectralCharacter.TakeDamage ─────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == RoguePhase.Dead) return;

            CharHitPoints = Math.Max(CharHitPoints - amount, 0);
            Console.WriteLine($"[Rogue] TakeDamage:{amount} HP:{CharHitPoints}/{CharMaxHP}");

            SpawnBloodSplatter(amount);

            if (!CharIsAlive)
                TransitionToDead();
            else
                TransitionToHitEffect();
        }

        // ── Blood Splatter ────────────────────────────────────────
        // Ported from old BloodRogue.RogueTakeDamage — spawns a puddle
        // jittered ±5px (in old-engine pixel terms) around the collision
        // box, scaled up with damage amount, capped/faded by the registry.
        private static readonly Random _splatterRand = new Random();

        private void SpawnBloodSplatter(int amount)
        {
            if (_scene == null || _meshLib == null) return;

            // Old engine jitter was ±5px at 84px = 1 world unit baseline
            const float JitterPx = 5f;
            const float Wpx = 84f;
            float jitterRangeWorld = JitterPx / Wpx;

            float jitterX = ((float)_splatterRand.NextDouble() * 2f - 1f) * jitterRangeWorld;
            float jitterY = ((float)_splatterRand.NextDouble() * 2f - 1f) * jitterRangeWorld;

            // Ported scale formula: Math.Min(1.0 + amount*0.25, 3.0) * (0.8 + rand*0.4)
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

        public void BreakClearHitEffects()
        {
            if (_phase != RoguePhase.HitEffect) return;
            TransitionToAlive();
        }

        public void DynTickUpdate(ISpectralCharacter character, float delta)
        {
            switch (_phase)
            {
                case RoguePhase.HitEffect:
                    _hitTimer += delta;
                    // Push red tint every frame — campfire pattern
                    if (CharMesh != null)
                        CharMesh.Color = ColorHit;
                    if (_hitTimer >= HitFlashDuration)
                        TransitionToAlive();
                    break;

                case RoguePhase.Dead:
                    _deadTimer += delta;
                    break;

                case RoguePhase.Alive:
                default:
                    break;
            }
        }

        private void TransitionToHitEffect()
        {
            _phase = RoguePhase.HitEffect;
            _hitFlashTimer = HitFlashDuration;
            CharMesh.Color = ColorHit;
            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = RoguePhase.Alive;
            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = RoguePhase.Dead;
            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (CharMesh == null) return;

            switch (_phase)
            {
                case RoguePhase.HitEffect:
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
                    CharMesh.Color = ColorHit;
                    CharMesh.OverlayTextureDataUrl = CharHitOverlayTexturePath;
                    CharMesh.OverlayAlpha = 1f;
                    CharMesh.OverlayDirty = true;
                    CharMesh.TextureDirty = true;

                    break;

                case RoguePhase.Alive:
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
                    // Restore animation sheet (use existing helper so behavior stays consistent)
                    CharMesh.Color = ColorNormal;
                    CharMesh.OverlayAlpha = 0f;
                    CharMesh.OverlayDirty = true;
                    CharMesh.TextureDirty = true;
                    ApplyAnimationToMesh();
                    break;

                case RoguePhase.Dead:
                    // Single-frame dead texture
                    CharMesh.IsAnimated = false;
                    CharMesh.FrameCount = 1;
                    CharMesh.SheetWidth = FrameW;
                    CharMesh.SheetHeight = FrameH;
                    CharMesh.FramePixelWidth = FrameW;
                    CharMesh.FramePixelHeight = FrameH;
                    CharMesh.UVScaleX = 1f;
                    CharMesh.UVScaleY = 1f;
                    CharMesh.UVOffsetX = 0f;
                    CharMesh.UVOffsetY = 0f;
                    CharMesh.Color = ColorDead;
                    CharMesh.TextureDataUrl = CharDeadTexturePath;
                    CharMesh.TextureDirty = true;
                    break;
            }
        }


        // ── Normal Attack ─────────────────────────────────────────
        /// <summary>
        /// Kick attack. Requires at least 1 energy point to execute.
        /// Does not consume energy — energy is the gate, not the cost.
        /// </summary>
        public void CharAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharEnergyPoints <= 0) return;

            _attackCooldown = AttackCooldownDuration / Math.Max(1, CharAlacrity);
            SetAnimation(RogueAnimationState.Punch);
            _isOneShotAnimation = true;

            int frames = FrameCounts[RogueAnimationState.Punch];
            float perFrame = AnimSpeeds[RogueAnimationState.Punch] / Math.Max(1, CharAlacrity);
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                foreach (var dummy in SpectralBreakables.DummyRegistry.All)
                {
                    if (!dummy.BreakIsAlive) continue;
                    float dx = WorldX - dummy.BreakX;
                    float dy = WorldY - dummy.BreakY;
                    float distSq = dx * dx + dy * dy;
                    float minDist = CollisionRadius + dummy.BreakCollisionRadius + PunchRadius;
                    if (distSq <= minDist * minDist)
                    {
                        dummy.BreakTakeDamage(CharStrength);
                        _spectralLevel.AddXp(this, "Dummy");
                    }
                }

                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (!enemy.EnemyIsAlive) continue;
                        float dx = WorldX - enemy.WorldX;
                        float dy = WorldY - enemy.WorldY;
                        float distSq = dx * dx + dy * dy;
                        float minDist = CollisionRadius + enemy.CollisionRadius + PunchRadius;
                        if (distSq <= minDist * minDist)
                        {
                            enemy.TakeDamage(CharStrength);
                            _spectralLevel.AddXp(this, enemy.EnemyClassName);
                        }
                    }
                }
            };
        }

        // ── Special Attack: Dags ──────────────────────────────────
        /// <summary>
        /// Dagger strike. Costs 2 energy, deals 2× melee damage, wider arc.
        /// </summary>
        public void CharSpecialAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharEnergyPoints < 2) return;

            _attackCooldown = AttackCooldownDuration / Math.Max(1, CharAlacrity);
            CharEnergyPoints -= 2;
            SetAnimation(RogueAnimationState.Dags);
            _isOneShotAnimation = true;

            int frames = FrameCounts[RogueAnimationState.Dags];
            float perFrame = AnimSpeeds[RogueAnimationState.Dags] / Math.Max(1, CharAlacrity);
            _attackDamageTimer = perFrame * Math.Max(1, frames - 2);
            _attackDamagePending = true;

            _pendingAttackDamage = () =>
            {
                foreach (var dummy in SpectralBreakables.DummyRegistry.All)
                {
                    if (!dummy.BreakIsAlive) continue;
                    float dx = WorldX - dummy.BreakX;
                    float dy = WorldY - dummy.BreakY;
                    float distSq = dx * dx + dy * dy;
                    float minDist = CollisionRadius + dummy.BreakCollisionRadius + PunchRadius;
                    if (distSq <= minDist * minDist)
                    {
                        dummy.BreakTakeDamage(CharStrength * 2);
                        _spectralLevel.AddXp(this, "Dummy", 2.0);
                    }
                }

                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                    {
                        if (!enemy.EnemyIsAlive) continue;
                        float dx = WorldX - enemy.WorldX;
                        float dy = WorldY - enemy.WorldY;
                        float distSq = dx * dx + dy * dy;
                        float minDist = CollisionRadius + enemy.CollisionRadius + PunchRadius;
                        if (distSq <= minDist * minDist)
                        {
                            enemy.TakeDamage(CharStrength * 2);
                            _spectralLevel.AddXp(this, enemy.EnemyClassName, 2);
                        }
                    }
                }
            };
        }

        // ── Animation ─────────────────────────────────────────────
        private void SetAnimation(RogueAnimationState newState)
        {
            if (newState == _currentAnimation && !_isOneShotAnimation) return;

            _currentAnimation = newState;
            _animationFrame = 0;
            _animationTimer = 0f;
            ApplyAnimationToMesh();
        }

        private void ApplyAnimationToMesh()
        {
            if (CharMesh == null) return;

            float sheetW = SheetWidths.TryGetValue(_currentAnimation, out var sw) ? sw : FrameW;
            int frameCount = FrameCounts.TryGetValue(_currentAnimation, out var fc) ? fc : 1;
            float animSpeed = AnimSpeeds.TryGetValue(_currentAnimation, out var spd) ? spd : 0.12f;

            CharMesh.IsAnimated = true;
            CharMesh.FrameCount = frameCount;
            CharMesh.FrameRate = 1f / animSpeed;
            CharMesh.SheetWidth = sheetW;
            CharMesh.SheetHeight = FrameH;
            CharMesh.FramePixelWidth = FrameW;
            CharMesh.FramePixelHeight = FrameH;
            var newTexUrl = SpritePaths[_currentAnimation];
            if (CharMesh.TextureDataUrl != newTexUrl)
            {
                CharMesh.TextureDataUrl = newTexUrl;
                CharMesh.TextureDirty = true;
            }
            // always resync — SheetWidth/FrameCount above are unconditional, this has to be too
            CharMesh.CurrentFrame = 0;
            CharMesh.FrameTimer = 0f;
            float frameScale = FrameW / sheetW;
            CharMesh.UVScaleX = _facingRight ? frameScale : -frameScale;
            CharMesh.UVOffsetX = _facingRight ? 0f : frameScale;
            CharMesh.UVScaleY = 1f;
            CharMesh.FacingRight = _facingRight;


            Console.WriteLine($"[Rogue] Animation → {_currentAnimation}");
        }

        // ── Direction Helper ──────────────────────────────────────
        private RogueAnimationState GetStateFromDir(Vector2 dir)
        {
            if (MathF.Abs(dir.X) > MathF.Abs(dir.Y))
                return dir.X > 0 ? RogueAnimationState.WalkRight : RogueAnimationState.WalkLeft;
            else
                return dir.Y > 0 ? RogueAnimationState.WalkDown : RogueAnimationState.WalkUp;
        }

        // ── Height Follow ─────────────────────────────────────────
        /// <summary>
        /// Call after Move() to snap rogue Z to tile map height.
        /// </summary>
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (CharMesh != null)
                CharMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // TODO: RogueStealth() when stealth/invisibility system is ready
        // TODO: ShowHitEffect when FX system is ready
        // TODO: BloodSplatter when FX system is ready
        // TODO: Death animation when death state is added
        // TODO: RogueLevelUp() when XP system is ready
        // TODO: GetCollisionBox3D() when 3D collision bounds needed
        // TODO: SplatterPuddles when FX system is ported
    }
}