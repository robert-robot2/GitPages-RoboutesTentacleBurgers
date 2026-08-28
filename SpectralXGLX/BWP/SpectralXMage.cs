using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodMage.
    /// Renders as a PrimSquare billboard mesh in world space.
    /// Animation state machine drives texture swap per state.
    ///
    /// Resource: Mana — regens over time (1/sec).
    /// Normal attack (Cast) gates on Mana > 0 but does NOT consume mana.
    /// Special (FireWall) costs exactly 5 mana, deals 2× Intelligence damage.
    ///
    /// Damage model: Mage deals 0 melee damage. All damage driven by
    /// CharIntelligence (MageDamageAmount = 4).
    ///
    /// Mid-range caster (PunchRadius 0.6f), moderate speed (Celerity 9).
    /// All positions are world space floats.
    /// </summary>
    public class SpectralXMage : ISpectralCharacter
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? CharMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string CharClassName => "Mage";
        public bool CharIsAlive => CharHitPoints > 0;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        // ── Core Stats ────────────────────────────────────────────
        public int CharHitPoints { get; set; } = 8;
        public int CharMaxHP { get; set; } = 8;
        public int CharLevel { get; set; } = 1;
        public int CharXP { get; set; } = 0;
        public int CharXPPerLevel { get; set; } = 5;
        public int CharLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int CharStrength { get; set; } = 0;         // Mage deals no melee damage
        public int CharAlacrity { get; set; } = 4;
        public int CharCelerity { get; set; } = 4;
        public int CharLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int CharIntelligence { get; set; } = 4;     // All damage sourced from here
        public int CharLifeRegen { get; set; } = 0;
        public int CharStatPoints { get; set; } = 0;
        public bool MageLevelUpTriggered { get; set; } = false;
        public int LastHPGain { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string CharHPColor => "rgba(255,0,0,.7)";
        public string CharInvColor => "rgba(0,0,255,1.0)";
        public string CharEnergyColor => "rgba(0,0,255,.7)";

        // ── Hunger ────────────────────────────────────────────────
        public int CharHungerCurrent { get; set; } = 2000;
        public int CharHungerFull { get; set; } = 2000;
        public int CharHungerDurationSeconds { get; set; } = 86400;

        // ── Class Resource: Mana ──────────────────────────────────
        // Mana regens passively. Normal attack gates on Mana > 0 (no cost).
        // FireWall costs 5 mana — requires Mana > 4 to cast.
        public int CharManaPoints { get; set; } = 20;
        public int CharMaxManaPoints { get; set; } = 20;
        public int CharManaRegenRate { get; set; } = 1;

        // ── ISpectralCharacter Resource Wiring ────────────────────
        public string CharResourceName => "Mana";
        public int CharResourceValue { get => CharManaPoints; set => CharManaPoints = value; }
        public string CharRegenLabel => "Mana Regen";
        public int CharRegenValue { get => CharManaRegenRate; set => CharManaRegenRate = value; }
        public string CharMaxResourceName => "Max Mana";
        public int CharMaxResourceValue { get => CharMaxManaPoints; set => CharMaxManaPoints = value; }

        // ── Spell / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 0.6f;     // Limenity 6 — mid-range caster

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;
        public bool IsOneShotPlaying => _isOneShotAnimation;
        // ── Animation State ───────────────────────────────────────
        public enum MageAnimationState
        {
            Idle,
            WalkDown,
            WalkUp,
            WalkLeft,
            WalkRight,
            Punch,      // cast animation — MageCast02.png, 12 frames
            FireWall    // special — MageFirewallAttack01.png, 10 frames, costs 5 mana
        }
        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private MageAnimationState _currentAnimation = MageAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private bool _facingRight = true;
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";   // ← confirm actual filename
        public const string DeadSpritePath = "/iAssets/MageCooled01.png"; // ← confirm actual filename
        public string CharHitTexturePath => "/iAssets/WarriorGothit01.png";
        public string CharDeadTexturePath => "/iAssets/MageCooled01.png";
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string CharHitOverlayTexturePath => HitOverlayTexturePath;
        // ── Hit / Death Phase ───────────────────────────────────────────
        private enum MagePhase { Alive, HitEffect, Dead }
        private MagePhase _phase = MagePhase.Alive;

        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Timers ────────────────────────────────────────────────
        private float _lifeRegenTimer = 0f;
        private float _manaRegenTimer = 0f;
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

        // ── Sprite Sheet Paths ────────────────────────────────────
        // 84×84 frames — same sheets as original BloodMage
        public static readonly Dictionary<MageAnimationState, string> SpritePaths = new()
        {
            { MageAnimationState.Idle,      "/iAssets/MageIdlecell01.png"         },
            { MageAnimationState.WalkDown,  "/iAssets/MageWalkDown01.png"         },
            { MageAnimationState.WalkUp,    "/iAssets/MageWalkUp01.png"           },
            { MageAnimationState.WalkLeft,  "/iAssets/MageWalkLeft01.png"         },
            { MageAnimationState.WalkRight, "/iAssets/MageWalkRight01.png"        },
            { MageAnimationState.Punch,     "/iAssets/MageCast02.png"             },
            { MageAnimationState.FireWall,  "/iAssets/MageFirewallAttack01.png"   },
        };

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<MageAnimationState, int> FrameCounts = new()
        {
            { MageAnimationState.Idle,      20 },
            { MageAnimationState.WalkDown,  8  },
            { MageAnimationState.WalkUp,    8  },
            { MageAnimationState.WalkLeft,  8  },
            { MageAnimationState.WalkRight, 8  },
            { MageAnimationState.Punch,     12 },   // cast has 12 frames
            { MageAnimationState.FireWall,  10 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<MageAnimationState, float> AnimSpeeds = new()
        {
            { MageAnimationState.Idle,      0.12f },
            { MageAnimationState.WalkDown,  0.12f },
            { MageAnimationState.WalkUp,    0.12f },
            { MageAnimationState.WalkLeft,  0.12f },
            { MageAnimationState.WalkRight, 0.12f },
            { MageAnimationState.Punch,     0.05f },
            { MageAnimationState.FireWall,  0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<MageAnimationState, float> SheetWidths = new()
        {
            { MageAnimationState.Idle,      20 * FrameW },  // 1680px
            { MageAnimationState.WalkDown,  8  * FrameW },  // 672px
            { MageAnimationState.WalkUp,    8  * FrameW },  // 672px
            { MageAnimationState.WalkLeft,  8  * FrameW },  // 672px
            { MageAnimationState.WalkRight, 8  * FrameW },  // 672px
            { MageAnimationState.Punch,     12 * FrameW },  // 1008px
            { MageAnimationState.FireWall,  10 * FrameW },  // 840px
        };

        // ── Constructor ───────────────────────────────────────────
        public SpectralXMage()
        {
            Console.WriteLine("[SpectralXMage] Created");
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


            WorldZ = CharMesh.Position.Z;

            ApplyAnimationToMesh();
            Console.WriteLine("[SpectralXMage] Mesh initialized");
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
            if (_phase == MagePhase.Alive && (newState != _currentAnimation || _isOneShotAnimation))
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
                SetAnimation(MageAnimationState.Idle);
                _lastMoveDirection = Vector2.Zero;
            }
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        // ── Tick ──────────────────────────────────────────────────
        public void Tick(float delta)
        {
            _runningTime += delta;
            // ── Hit Flash Phase ─────────────────────────────────────────────
            if (_phase == MagePhase.HitEffect)
            {
                _hitFlashTimer -= delta;

                if (CharMesh != null)
                    CharMesh.Color = ColorHit;

                if (_hitFlashTimer <= 0f)
                    TransitionToAlive();
            }

            if (!CharIsAlive) return;
            if (CharMesh == null) return;

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

            // ── Mana regen ────────────────────────────────────────
            _manaRegenTimer += delta;
            if (_manaRegenTimer >= 1f)
            {
                CharManaPoints = Math.Min(CharManaPoints + CharManaRegenRate, CharMaxManaPoints);
                _manaRegenTimer = 0f;
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
                    SetAnimation(MageAnimationState.Idle);
                }
            }
        }

        // ── ISpectralCharacter.TakeDamage ─────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == MagePhase.Dead) return;

            CharHitPoints = Math.Max(CharHitPoints - amount, 0);
            Console.WriteLine($"[Mage] TakeDamage:{amount} HP:{CharHitPoints}/{CharMaxHP}");

            SpawnBloodSplatter(amount);

            if (!CharIsAlive)
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
            _phase = MagePhase.HitEffect;
            _hitFlashTimer = HitFlashDuration;

            if (CharMesh != null)
                CharMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = MagePhase.Alive;

            if (CharMesh != null)
                CharMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = MagePhase.Dead;

            if (CharMesh != null)
                CharMesh.Color = ColorDead;

            SyncMeshVisuals();
        }
        private void SyncMeshVisuals()
        {
            if (CharMesh == null) return;

            switch (_phase)
            {
                case MagePhase.HitEffect:
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

                case MagePhase.Alive:
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
                    CharMesh.TextureDirty = true;
                    ApplyAnimationToMesh();
                    break;

                case MagePhase.Dead:
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

        // ── Normal Attack: Cast ───────────────────────────────────
        /// <summary>
        /// Gates on Mana > 0 but does NOT consume mana.
        /// Mana is just the requirement to cast at all.
        /// Damage = CharIntelligence.
        /// </summary>
        public void CharAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharManaPoints <= 0) return;    // mana is gate, not consumed

            _attackCooldown = AttackCooldownDuration;

            SetAnimation(MageAnimationState.Punch);
            _isOneShotAnimation = true;

            int frames = FrameCounts[MageAnimationState.Punch];
            float perFrame = AnimSpeeds[MageAnimationState.Punch];
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
                        dummy.BreakTakeDamage(CharIntelligence);    // spell damage, not melee
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
                            enemy.TakeDamage(CharIntelligence);
                            _spectralLevel.AddXp(this, enemy.EnemyClassName);
                        }
                    }
                }
            };
        }

        // ── Special Attack: FireWall ──────────────────────────────
        /// <summary>
        /// Costs 5 mana. Requires Mana > 4 to cast.
        /// Deals CharIntelligence × 2 damage.
        /// </summary>
        public void CharSpecialAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharManaPoints <= 4) return;    // needs at least 5 mana

            _attackCooldown = AttackCooldownDuration;
            CharManaPoints -= 5;

            SetAnimation(MageAnimationState.FireWall);
            _isOneShotAnimation = true;

            int frames = FrameCounts[MageAnimationState.FireWall];
            float perFrame = AnimSpeeds[MageAnimationState.FireWall];
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
                        dummy.BreakTakeDamage(CharIntelligence * 2);
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
                            enemy.TakeDamage(CharIntelligence * 2);
                            _spectralLevel.AddXp(this, enemy.EnemyClassName, 2);
                        }
                    }
                }
            };
        }

        // ── Animation ─────────────────────────────────────────────
        private void SetAnimation(MageAnimationState newState)
        {
            // Only skip if same state AND not recovering from a one-shot
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

            Console.WriteLine($"[Mage] Animation → {_currentAnimation}");
        }

        // ── Direction Helper ──────────────────────────────────────
        private MageAnimationState GetStateFromDir(Vector2 dir)
        {
            if (MathF.Abs(dir.X) > MathF.Abs(dir.Y))
                return dir.X > 0 ? MageAnimationState.WalkRight : MageAnimationState.WalkLeft;
            else
                return dir.Y > 0 ? MageAnimationState.WalkDown : MageAnimationState.WalkUp;
        }

        // ── Height Follow ─────────────────────────────────────────
        /// <summary>
        /// Call after Move() to snap mage Z to tile map height.
        /// </summary>
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f;
            if (CharMesh != null)
                CharMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // TODO: ShowHitEffect when FX system is ready
        // TODO: BloodSplatter when FX system is ready
        // TODO: Death animation when death state is added
        // TODO: MageLevelUp() when XP system is ready
        // TODO: GetCollisionBox3D() when 3D collision bounds needed
        // TODO: SplatterPuddles when FX system is ported
        // TODO: Secondary mana cost on normal cast if design changes
    }
}