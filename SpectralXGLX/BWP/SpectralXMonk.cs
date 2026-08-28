using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodMonk.
    /// Renders as a PrimSquare billboard mesh in world space.
    /// Animation state machine drives texture swap per state.
    ///
    /// Resource: Kai — gained on hit (normal attack consumes Kai per swing).
    /// Special (Fist10) fires ONLY when Kai = 0, then restores Kai to KaiOnHit + 4.
    ///
    /// Damage model: Monk deals 0 melee damage. All damage is driven by
    /// CharIntelligence (MonkSpellAmount = 5). Normal attack = Intelligence damage,
    /// Fist10 = Intelligence × 2.
    ///
    /// Fastest class (Celerity 10), squishiest (HP 4), shortest reach (PunchRadius 0.3f).
    /// All positions are world space floats.
    /// </summary>
    public class SpectralXMonk : ISpectralCharacter
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? CharMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string CharClassName => "Monk";
        public bool CharIsAlive => CharHitPoints > 0;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        // ── Core Stats ────────────────────────────────────────────
        public int CharHitPoints { get; set; } = 4;
        public int CharMaxHP { get; set; } = 4;
        public int CharLevel { get; set; } = 1;
        public int CharXP { get; set; } = 0;
        public int CharXPPerLevel { get; set; } = 5;
        public int CharLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int CharStrength { get; set; } = 0;         // Monk deals no melee damage
        public int CharAlacrity { get; set; } = 5;
        public int CharCelerity { get; set; } = 5;
        public int CharLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int CharIntelligence { get; set; } = 5;     // All damage sourced from here
        public int CharLifeRegen { get; set; } = 0;
        public int CharStatPoints { get; set; } = 0;
        public bool MonkLevelUpTriggered { get; set; } = false;
        public int LastHPGain { get; set; } = 0;

        // ── Color Theme ───────────────────────────────────────────
        public string CharHPColor => "rgba(255,0,0,.7)";
        public string CharInvColor => "rgba(255,192,0,1)";
        public string CharEnergyColor => "rgba(255,192,0,.7)";

        // ── Hunger ────────────────────────────────────────────────
        public int CharHungerCurrent { get; set; } = 2000;
        public int CharHungerFull { get; set; } = 2000;
        public int CharHungerDurationSeconds { get; set; } = 86400;

        // ── Class Resource: Kai ───────────────────────────────────
        // Kai is consumed on normal attack and restored by Fist10.
        // Normal attack gate: Kai > 0. Special gate: Kai == 0.
        public int CharKaiPoints { get; set; } = 5;
        public int CharMaxKaiPoints { get; set; } = 5;
        public int CharKaiOnHit { get; set; } = 1;         // consumed per normal attack swing

        // ── ISpectralCharacter Resource Wiring ────────────────────
        public string CharResourceName => "Kai";
        public int CharResourceValue { get => CharKaiPoints; set => CharKaiPoints = value; }
        public string CharRegenLabel => "Kai on Hit";
        public int CharRegenValue { get => CharKaiOnHit; set => CharKaiOnHit = value; }
        public string CharMaxResourceName => "Max Kai";
        public int CharMaxResourceValue { get => CharMaxKaiPoints; set => CharMaxKaiPoints = value; }

        // ── Punch / Reach ─────────────────────────────────────────
        public float PunchRadius { get; set; } = 0.3f;     // Limenity 3 — shortest reach

        // ── Collision ─────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;
        public bool IsOneShotPlaying => _isOneShotAnimation;
        // ── Animation State ───────────────────────────────────────
        public enum MonkAnimationState
        {
            Idle,
            WalkDown,
            WalkUp,
            WalkLeft,
            WalkRight,
            Punch,
            Fist10      // special — fires when Kai == 0, restores Kai, 2× Intelligence damage
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private MonkAnimationState _currentAnimation = MonkAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private bool _facingRight = true;
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";   // ← confirm actual filename
        public const string DeadSpritePath = "/iAssets/MonkCooled01.png"; // ← confirm actual filename
        public string CharHitTexturePath => "/iAssets/WarriorGothit01.png";
        public string CharDeadTexturePath => "/iAssets/MonkCooled01.png";
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string CharHitOverlayTexturePath => HitOverlayTexturePath;
        // ── Hit / Death Phase ───────────────────────────────────────────
        private enum MonkPhase { Alive, HitEffect, Dead }
        private MonkPhase _phase = MonkPhase.Alive;

        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);

        // ── Timers ────────────────────────────────────────────────
        private float _lifeRegenTimer = 0f;
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
        // 84×84 frames — same sheets as original BloodMonk
        public static readonly Dictionary<MonkAnimationState, string> SpritePaths = new()
        {
            { MonkAnimationState.Idle,      "/iAssets/MonkIdleCell.png"      },
            { MonkAnimationState.WalkDown,  "/iAssets/MonkDownIdleCell.png"  },
            { MonkAnimationState.WalkUp,    "/iAssets/MonkUpIdleCell.png"    },
            { MonkAnimationState.WalkLeft,  "/iAssets/MonkLeftIdleCell.png"  },
            { MonkAnimationState.WalkRight, "/iAssets/MonkRightidleCell.png" },  // original casing preserved
            { MonkAnimationState.Punch,     "/iAssets/MonkButtCell.png"      },
            { MonkAnimationState.Fist10,    "/iAssets/Monk10Fist.png"        },
        };

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<MonkAnimationState, int> FrameCounts = new()
        {
            { MonkAnimationState.Idle,      20 },
            { MonkAnimationState.WalkDown,  8  },
            { MonkAnimationState.WalkUp,    8  },
            { MonkAnimationState.WalkLeft,  8  },
            { MonkAnimationState.WalkRight, 8  },
            { MonkAnimationState.Punch,     14 },
            { MonkAnimationState.Fist10,    14 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<MonkAnimationState, float> AnimSpeeds = new()
        {
            { MonkAnimationState.Idle,      0.12f },
            { MonkAnimationState.WalkDown,  0.12f },
            { MonkAnimationState.WalkUp,    0.12f },
            { MonkAnimationState.WalkLeft,  0.12f },
            { MonkAnimationState.WalkRight, 0.12f },
            { MonkAnimationState.Punch,     0.05f },
            { MonkAnimationState.Fist10,    0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<MonkAnimationState, float> SheetWidths = new()
        {
            { MonkAnimationState.Idle,      20 * FrameW },  // 1680px
            { MonkAnimationState.WalkDown,  8  * FrameW },  // 672px
            { MonkAnimationState.WalkUp,    8  * FrameW },  // 672px
            { MonkAnimationState.WalkLeft,  8  * FrameW },  // 672px
            { MonkAnimationState.WalkRight, 8  * FrameW },  // 672px
            { MonkAnimationState.Punch,     14 * FrameW },  // 1176px
            { MonkAnimationState.Fist10,    14 * FrameW },  // 1176px
        };

        // ── Constructor ───────────────────────────────────────────
        public SpectralXMonk()
        {
            Console.WriteLine("[SpectralXMonk] Created");
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
            Console.WriteLine("[SpectralXMonk] Mesh initialized");
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
            if (_phase == MonkPhase.Alive && (newState != _currentAnimation || _isOneShotAnimation))
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
                SetAnimation(MonkAnimationState.Idle);
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
            if (_phase == MonkPhase.HitEffect)
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
                    SetAnimation(MonkAnimationState.Idle);
                }
            }
        }

        // ── ISpectralCharacter.TakeDamage ─────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == MonkPhase.Dead) return;

            CharHitPoints = Math.Max(CharHitPoints - amount, 0);
            Console.WriteLine($"[Monk] TakeDamage:{amount} HP:{CharHitPoints}/{CharMaxHP}");

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
            _phase = MonkPhase.HitEffect;
            _hitFlashTimer = HitFlashDuration;

            if (CharMesh != null)
                CharMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = MonkPhase.Alive;

            if (CharMesh != null)
                CharMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = MonkPhase.Dead;

            if (CharMesh != null)
                CharMesh.Color = ColorDead;

            SyncMeshVisuals();
        }
        private void SyncMeshVisuals()
        {
            if (CharMesh == null) return;

            switch (_phase)
            {
                case MonkPhase.HitEffect:
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

                case MonkPhase.Alive:
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

                case MonkPhase.Dead:
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


        // ── Normal Attack: Punch ──────────────────────────────────
        /// <summary>
        /// Consumes KaiOnHit per swing. Gate: Kai > 0.
        /// Damage = CharIntelligence (spell damage, not melee).
        /// </summary>
        public void CharAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharKaiPoints <= 0) return;     // Kai is the gate

            _attackCooldown = AttackCooldownDuration;
            CharKaiPoints = Math.Max(CharKaiPoints - CharKaiOnHit, 0);

            SetAnimation(MonkAnimationState.Punch);
            _isOneShotAnimation = true;

            int frames = FrameCounts[MonkAnimationState.Punch];
            float perFrame = AnimSpeeds[MonkAnimationState.Punch];
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
                        dummy.BreakTakeDamage(CharIntelligence);   // damage = Intelligence, not Strength
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

        // ── Special Attack: 10,000 Fists ─────────────────────────
        /// <summary>
        /// Fires ONLY when Kai == 0. Restores Kai to KaiOnHit + 4.
        /// Deals CharIntelligence × 2 damage.
        /// This is the Monk's comeback mechanic — burn out, then explode.
        /// </summary>
        public void CharSpecialAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharKaiPoints > 0) return;      // must be fully depleted to trigger

            _attackCooldown = AttackCooldownDuration;
            CharKaiPoints = CharKaiOnHit + 4;   // restore Kai as original

            SetAnimation(MonkAnimationState.Fist10);
            _isOneShotAnimation = true;

            int frames = FrameCounts[MonkAnimationState.Fist10];
            float perFrame = AnimSpeeds[MonkAnimationState.Fist10];
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
        private void SetAnimation(MonkAnimationState newState)
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

            Console.WriteLine($"[Monk] Animation → {_currentAnimation}");
        }

        // ── Direction Helper ──────────────────────────────────────
        private MonkAnimationState GetStateFromDir(Vector2 dir)
        {
            if (MathF.Abs(dir.X) > MathF.Abs(dir.Y))
                return dir.X > 0 ? MonkAnimationState.WalkRight : MonkAnimationState.WalkLeft;
            else
                return dir.Y > 0 ? MonkAnimationState.WalkDown : MonkAnimationState.WalkUp;
        }

        // ── Height Follow ─────────────────────────────────────────
        /// <summary>
        /// Call after Move() to snap monk Z to tile map height.
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
        // TODO: MonkLevelUp() when XP system is ready
        // TODO: GetCollisionBox3D() when 3D collision bounds needed
        // TODO: SplatterPuddles when FX system is ported
        // TODO: Kai passive regen option if design calls for it later
    }
}