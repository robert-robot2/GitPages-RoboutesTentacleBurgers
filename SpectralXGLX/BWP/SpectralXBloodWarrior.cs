using System.Numerics;
using SpectralXGLX.SpectralXComponent;
using SpectralXGLX.SpectralXComponent.SpectralXRender;

namespace SpectralXGLX.BWP
{
    /// <summary>
    /// WebGL2 rebuild of BloodWarrior.
    /// Renders as a PrimSquare billboard mesh in world space.
    /// Animation state machine drives texture swap per state.
    /// All positions are world space floats.
    /// </summary>
    public class SpectralXBloodWarrior : ISpectralCharacter
    {
        // ── Mesh Reference ────────────────────────────────────────
        public SpectralXMesh? CharMesh { get; private set; }

        // ── Identity ─────────────────────────────────────────────
        public string CharClassName => "Warrior";
        public bool CharIsAlive => CharHitPoints > 0;

        // ── World Position ────────────────────────────────────────
        public float WorldX { get; set; } = 0f;
        public float WorldY { get; set; } = 0f;
        public float WorldZ { get; set; } = 0f;

        // ── Core Stats ────────────────────────────────────────────
        public int CharHitPoints { get; set; } = 20;
        public int CharMaxHP { get; set; } = 20;
        public int CharLevel { get; set; } = 1;
        public int CharXP { get; set; } = 0;
        public int CharXPPerLevel { get; set; } = 5;
        public int CharLevelCap { get; set; } = 100;

        // ── Combat Stats ──────────────────────────────────────────
        public int CharStrength { get; set; } = 1;
        public int CharAlacrity { get; set; } = 1;
        public int CharCelerity { get; set; } = 1;
        public int CharLimenity
        {
            get => (int)(PunchRadius * 10f);
            set => PunchRadius = value * 0.1f;
        }
        public int CharIntelligence { get; set; } = 0;
        public int CharLifeRegen { get; set; } = 0;
        public int CharStatPoints { get; set; } = 0;
        public bool WarriorLevelUpTriggered { get; set; } = false;
        public int LastHPGain { get; set; } = 0;
        public string CharHPColor => "rgba(255,0,0,.7)";
        public string CharInvColor => "rgba(255,100,0,1.0)";
        public string CharEnergyColor => "rgba(255,100,0,.7)";
        public int CharHungerCurrent { get; set; } = 2000;
        public int CharHungerFull { get; set; } = 2000;
        public int CharHungerDurationSeconds { get; set; } = 86400;
        // ── Class Resource ────────────────────────────────────────
        // ── Rage Fields ───────────────────────────────────────────
        public int CharRagePoints { get; set; } = 0;
        public int CharMaxRagePoints { get; set; } = 10;
        public int CharRageOnHit { get; set; } = 1;
        public float PunchRadius { get; set; } = 0.8f;

        // ── ISpectralCharacter Resource Wiring ────────────────────
        public string CharResourceName => "Rage";
        public int CharResourceValue { get => CharRagePoints; set => CharRagePoints = value; }
        public string CharRegenLabel => "Rage on Hit";
        public int CharRegenValue { get => CharRageOnHit; set => CharRageOnHit = value; }
        public string CharMaxResourceName => "Max Rage";
        public int CharMaxResourceValue { get => CharMaxRagePoints; set => CharMaxRagePoints = value; }

        // ── Collision ────────────────────────────────────────────
        public float CollisionRadius { get; } = 0.5f;


        public bool IsOneShotPlaying => _isOneShotAnimation;
        // ── Animation State ───────────────────────────────────────
        public enum WarriorAnimationState
        {
            Idle,
            WalkDown,
            WalkUp,
            WalkLeft,
            WalkRight,
            Punch,
            Shield
        }

        // ── Scene/Library refs — needed for spawning splatter puddles ─────
        private SpectralXScene? _scene;
        private SpectralXMeshLibrary? _meshLib;

        private WarriorAnimationState _currentAnimation = WarriorAnimationState.Idle;
        private int _animationFrame = 0;
        private float _animationTimer = 0f;
        private bool _isOneShotAnimation = false;
        private Vector2 _lastMoveDirection = Vector2.Zero;
        private bool _facingRight = true;
        private float _lifeRegenTimer = 0f;
        private float _hungerTimer = 0f;
        // ── Attack Cooldown ───────────────────────────────────────
        // ── Running clock — accumulated from Tick deltas, used to
        // timestamp splatter puddles consistently with their own fade math ──
        private float _runningTime = 0f;

        // ── Attack Cooldown ───────────────────────────────────────
        private float _attackCooldown = 0f;
        private const float AttackCooldownDuration = 0.6f;
        // ── Hit / Death Phase ───────────────────────────────────────────
        private enum WarriorPhase { Alive, HitEffect, Dead }
        private WarriorPhase _phase = WarriorPhase.Alive;

        private float _hitFlashTimer = 0f;
        private const float HitFlashDuration = 0.15f;

        private static readonly Vector4 ColorNormal = new Vector4(1f, 1f, 1f, 1f);
        private static readonly Vector4 ColorHit = new Vector4(1f, 0.15f, 0.15f, 1f);
        private static readonly Vector4 ColorDead = new Vector4(1f, 1f, 1f, 0.4f);
        public const string HitEffectPath = "/iAssets/WarriorGothit01.png";   // ← confirm actual filename
        public const string DeadSpritePath = "/iAssets/WarriorCooled01.png"; // ← confirm actual filename
        public string CharHitTexturePath => "/iAssets/WarriorGothit01.png";
        public string CharDeadTexturePath => "/iAssets/WarriorCooled01.png";
        public const string HitOverlayTexturePath = "/iAssets/WarriorGothit01.png";
        public string CharHitOverlayTexturePath => HitOverlayTexturePath;
        // ── Sprite Sheet Paths ────────────────────────────────────
        // 84x84 frames — same sheets as original BloodWarrior
        public static readonly Dictionary<WarriorAnimationState, string> SpritePaths = new()
        {
            { WarriorAnimationState.Idle,      "/iAssets/WarriorIdlecell2016x8.png" },
            { WarriorAnimationState.WalkDown,  "/iAssets/WarWalkDown01.png" },
            { WarriorAnimationState.WalkUp,    "/iAssets/WarWalkUp01.png" },
            { WarriorAnimationState.WalkLeft,  "/iAssets/WarWalkLeft02.png" },
            { WarriorAnimationState.WalkRight, "/iAssets/WarWalkRight02.png" },
            { WarriorAnimationState.Punch,     "/iAssets/WarPunch01.png" },
            { WarriorAnimationState.Shield,    "/iAssets/WarShield01.png" },
        };

        // ── Frame Counts ──────────────────────────────────────────
        public static readonly Dictionary<WarriorAnimationState, int> FrameCounts = new()
        {
            { WarriorAnimationState.Idle,      20 },
            { WarriorAnimationState.WalkDown,  8 },
            { WarriorAnimationState.WalkUp,    8 },
            { WarriorAnimationState.WalkLeft,  8 },
            { WarriorAnimationState.WalkRight, 8 },
            { WarriorAnimationState.Punch,     16 },
            { WarriorAnimationState.Shield,    16 },
        };

        // ── Animation Speeds (seconds per frame) ──────────────────
        public static readonly Dictionary<WarriorAnimationState, float> AnimSpeeds = new()
        {
            { WarriorAnimationState.Idle,      0.12f },
            { WarriorAnimationState.WalkDown,  0.12f },
            { WarriorAnimationState.WalkUp,    0.12f },
            { WarriorAnimationState.WalkLeft,  0.12f },
            { WarriorAnimationState.WalkRight, 0.12f },
            { WarriorAnimationState.Punch,     0.05f },
            { WarriorAnimationState.Shield,    0.05f },
        };

        // ── Frame Dimensions ─────────────────────────────────────
        public const float FrameW = 84f;
        public const float FrameH = 84f;

        // ── Sheet Widths per state ────────────────────────────────
        public static readonly Dictionary<WarriorAnimationState, float> SheetWidths = new()
        {
            { WarriorAnimationState.Idle,      20 * FrameW },  // 1680px
            { WarriorAnimationState.WalkDown,  8  * FrameW },  // 672px
            { WarriorAnimationState.WalkUp,    8  * FrameW },  // 672px
            { WarriorAnimationState.WalkLeft,  8  * FrameW },  // 672px
            { WarriorAnimationState.WalkRight, 8  * FrameW },  // 672px
            { WarriorAnimationState.Punch,     16 * FrameW },  // 1344px
            { WarriorAnimationState.Shield,    16 * FrameW },  // 1344px
        };

        // ── Constructor ───────────────────────────────────────────
        public SpectralXBloodWarrior()
        {
            Console.WriteLine("[SpectralXBloodWarrior] Created");
        }

        // ── Mesh Init ─────────────────────────────────────────────
        /// <summary>
        /// Call from InitScene4() after PrimSquare is available.
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
            Console.WriteLine("[SpectralXBloodWarrior] Mesh initialized");
        }

        // ── ISpectralCharacter.Move ───────────────────────────────
        private float _stopTimer = 0f;
        private const float StopDelay = 0.08f; // 80ms grace period

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
            if (_phase == WarriorPhase.Alive && (newState != _currentAnimation || _isOneShotAnimation))
                SetAnimation(newState);
            _lastMoveDirection = isoDir;
            if (MathF.Abs(isoDir.X) > MathF.Abs(isoDir.Y))
                _facingRight = isoDir.X > 0;  
        }

        public void Stop()
        {
            // Don't interrupt a one-shot that is still playing
            if (_isOneShotAnimation) return;

            if (_stopTimer <= 0f)
            {
                SetAnimation(WarriorAnimationState.Idle);
                _lastMoveDirection = Vector2.Zero;
            }
        }
        private bool _attackDamagePending = false;
        private float _attackDamageTimer = 0f;
        private Action? _pendingAttackDamage;
        public void Tick(float delta)
        {
            _runningTime += delta;
            // ── Hit Flash Phase ─────────────────────────────────────────────
            if (_phase == WarriorPhase.HitEffect)
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
            if (CharLifeRegen > 0)
            {
                _lifeRegenTimer += delta;
                if (_lifeRegenTimer >= 1f)
                {
                    CharHitPoints = Math.Min(CharHitPoints + CharLifeRegen, CharMaxHP);
                    _lifeRegenTimer = 0f;
                }
            }
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

                var state = _currentAnimation;
                float speed = AnimSpeeds.TryGetValue(state, out var s) ? s : 0.12f;
                int frames = FrameCounts.TryGetValue(state, out var f) ? f : 1;
                float totalDuration = speed * frames;

                if (_animationTimer >= totalDuration)
                {
                    _isOneShotAnimation = false;
                    _animationTimer = 0f;
                    SetAnimation(WarriorAnimationState.Idle);
                }
            }
        }

        // ── ISpectralCharacter.TakeDamage ─────────────────────────
        public void TakeDamage(int amount)
        {
            if (_phase == WarriorPhase.Dead) return;

            CharHitPoints = Math.Max(CharHitPoints - amount, 0);
            Console.WriteLine($"[Warrior] TakeDamage:{amount} HP:{CharHitPoints}/{CharMaxHP}");

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
            _phase = WarriorPhase.HitEffect;
            _hitFlashTimer = HitFlashDuration;

            if (CharMesh != null)
                CharMesh.Color = ColorHit;

            SyncMeshVisuals();
        }

        private void TransitionToAlive()
        {
            _phase = WarriorPhase.Alive;

            if (CharMesh != null)
                CharMesh.Color = ColorNormal;

            SyncMeshVisuals();
        }

        private void TransitionToDead()
        {
            _phase = WarriorPhase.Dead;

            if (CharMesh != null)
                CharMesh.Color = ColorDead;

            SyncMeshVisuals();
        }

        private void SyncMeshVisuals()
        {
            if (CharMesh == null) return;

            switch (_phase)
            {
                case WarriorPhase.HitEffect:
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

                case WarriorPhase.Alive:
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

                case WarriorPhase.Dead:
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

        public void CharAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharRagePoints >= CharMaxRagePoints) return;

            _attackCooldown = AttackCooldownDuration;
            CharRagePoints = Math.Min(CharRagePoints + CharRageOnHit, CharMaxRagePoints);

            SetAnimation(WarriorAnimationState.Punch);
            _isOneShotAnimation = true;

            int frames = FrameCounts[WarriorAnimationState.Punch];
            float perFrame = AnimSpeeds[WarriorAnimationState.Punch];
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

        public void CharSpecialAttack(SpectralLevel _spectralLevel, IEnumerable<ISpectralEnemy>? enemies = null, bool? forceRight = null)
        {
            if (forceRight.HasValue) _facingRight = forceRight.Value;
            if (!CharIsAlive) return;
            if (_attackCooldown > 0f) return;
            if (CharRagePoints < 5) return;

            _attackCooldown = AttackCooldownDuration;
            CharRagePoints -= 5;

            SetAnimation(WarriorAnimationState.Shield);
            _isOneShotAnimation = true;

            int frames = FrameCounts[WarriorAnimationState.Shield];
            float perFrame = AnimSpeeds[WarriorAnimationState.Shield];
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

        // ── Animation ────────────────────────────────────────────
        private void SetAnimation(WarriorAnimationState newState)
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
            // CharMesh.TransformDirty = true;
            Console.WriteLine($"[Warrior] Animation → {_currentAnimation}");
        }



        // ── Direction Helper ──────────────────────────────────────
        private WarriorAnimationState GetStateFromDir(Vector2 dir)
        {
            if (MathF.Abs(dir.X) > MathF.Abs(dir.Y))
                return dir.X > 0 ? WarriorAnimationState.WalkRight : WarriorAnimationState.WalkLeft;
            else
                return dir.Y > 0 ? WarriorAnimationState.WalkDown : WarriorAnimationState.WalkUp;
        }

        // ── Height Follow ─────────────────────────────────────────
        /// <summary>
        /// Call after Move() to snap warrior Z to tile map height.
        /// </summary>
        public void ApplyTerrainHeight(float terrainZ)
        {
            WorldZ = terrainZ + 0.1f; // float slightly above ground
            if (CharMesh != null)
                CharMesh.Position = new Vector3(WorldX, WorldY, WorldZ);
        }

        // TODO: WarriorAttack(ISpectralEnemy) when enemy system is ready
        // TODO: WarriorShield() when rage system is ready
        // TODO: WarriorRespawn() when respawn system is ready
        // TODO: WarriorLevelUp() when XP system is ready
        // TODO: GetCollisionBox3D() when 3D collision bounds needed
        // TODO: SplatterPuddles when FX system is ported
    }
}