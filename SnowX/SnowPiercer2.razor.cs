using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Web;

namespace SnowX
{
    public partial class SnowPiercer2
    {

        private ElementReference backgroundCanvasRef;
        private double lastCanvasScrollOffset = 0;
        private double lastCanvasViewportOffset = 0;
        private ElementReference foregroundCanvasRef;
        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;
        private SnowX.Lighting.SnowLightCycle lightCycle = new();
        private SnowX.Weather.SnowWeatherCycle weatherCycle = new();


        private Random random = new Random();
        private const int MaxEnemies = 1500; // adjust as needed
        private int totalKills = 0;

        // Cocaine Bear buff system
        private DateTime lastBearBuff = DateTime.Now;
        private DateTime buffExpiresAt = DateTime.MinValue;
        private bool isBuffActive = false;
        private const int BearBuffIntervalMs = 20000; // 20 seconds

        private const int MaxCars = 15;
        private bool isDiscardMode = false;



        // Add these private fields with your other private fields at the top:
        private bool isDragging = false;
        private double dragStartX = 0;
        private double viewportOffsetX = 0;
        private double initialViewportOffset = 0;
     
        enum GameState { Menu, Playing, LevelUp, LevelUpSecond, GameOver }  // ADD LevelUpSecond
        enum TrajectoryType
        {
            Linear,      // Straight line (current behavior - lerp to target)
            Ballistic,   // Arced projectile with gravity
            Homing       // Curves toward target (future feature)
        }
        // 🆕 ADD THIS NEW ENUM:
        enum MovementPattern
        {
            Straight,      // No vertical movement (default)
            Sine,          // Smooth wave motion
            Bounce,        // Up and down bouncing
            Erratic        // Random jerky movement
        }
        private GameState gameState = GameState.Menu;
        private int score = 0;
        private int xp = 0;
        private int level = 1;
        private int xpNeeded = 100;
        private int environment = 0;

     
        private Dictionary<int, double> carRotationAngles = new();
        private List<TrainCar> trainCars = new();


        private List<Enemy> enemies = new();
        private List<Effect> effects = new();
        private List<EnemyProjectile> enemyProjectiles = new();
        private Dictionary<string, DateTime> lastEnemyShot = new();
        private double scrollOffset = 0;
        private double gameSpeed = 2;
        private List<UpgradeChoice> upgradeChoices = new();
        private List<UpgradeChoice> upgradeChoicesSecond = new(); // ADD THIS LINE
        private System.Timers.Timer? gameTimer;

        private int trainHp = 100;
        private int trainMaxHp = 100;
        private double throttle = 0.5; // 0.0 to 1.0
        private double baseGameSpeed = 2;

        private int? draggedCarIndex = null;
        private int? dropTargetIndex = null;

        // Culling

        private int visibleEnemyCount = 0;
        private int culledEnemyCount = 0;

        // ADD THESE FIELDS TO TRACK ATTACK STATES:
        private Dictionary<int, DateTime> carAttackStates = new(); // Track which cars are attacking
        private const int AttackAnimationDurationMs = 300; // How long attack animation plays

        // ENGINE REGENERATION SYSTEM
        private DateTime lastEngineRegen = DateTime.Now;
        private const int EngineRegenIntervalMs = 2000; // 2 seconds
        private int EngineRegenAmount = 1; // 1 HP per tick (upgradeable)

        // PER-WEAPON UPGRADE TRACKING - ADD THIS LINE
        private Dictionary<Guid, WeaponUpgradeState> weaponUpgrades = new();

        //
        private string playerInitials = "";
        private bool showInitialsInput = false;
        private List<HighScore> highScores = new List<HighScore>();
        private const int MaxHighScores = 10;

        private DateTime lastShamanRegen = DateTime.Now;
        private const int ShamanRegenIntervalMs = 1000; // 1 second

        private async Task DrawBackgroundCanvas()
        {
            if (gameState != GameState.Playing) return;

            var layers = new[]
            {
        new {
            imageKey = GetEnvironmentImageKey(),
            parallax = 0.1,
            y = 0,
            height = 800,
            width = 3200,
            patternWidth = 3200
        },
        new {
            imageKey = GetGroundImageKey(),
            parallax = 0.4,
            y = 700,
            height = 100,
            width = 200,
            patternWidth = 200
        },
        new {
            imageKey = GetFoliageLayer2ImageKey(),
            parallax = 0.3,
            y = 400,
            height = 300,
            width = 800,
            patternWidth = 800
        },
        new {
            imageKey = GetFoliageLayer1ImageKey(),
            parallax = 0.2,
            y = 675,
            height = 30,
            width = 800,
            patternWidth = 800
        }
    };

            await JSRuntime.InvokeVoidAsync("drawBackgroundLayers",
                backgroundCanvasRef, layers, scrollOffset, viewportOffsetX);
        }
        private async Task DrawForegroundCanvas()
        {
            if (gameState != GameState.Playing) return;

            var layers = new[]
            {
        new {
            imageKey = GetFoliageLayer3ImageKey(),
            parallax = 0.4,
            y = 720,
            height = 80,
            width = 800,
            patternWidth = 800
        }
    };

            await JSRuntime.InvokeVoidAsync("drawBackgroundLayers",
                foregroundCanvasRef, layers, scrollOffset, viewportOffsetX);
        }

        // ADD THIS METHOD TO CHECK IF A CAR IS ATTACKING:
        private bool IsCarAttacking(int carIndex)
        {
            if (!carAttackStates.ContainsKey(carIndex)) return false;

            var timeSinceAttack = (DateTime.Now - carAttackStates[carIndex]).TotalMilliseconds;
            return timeSinceAttack < AttackAnimationDurationMs;
        }


        private Dictionary<string, DateTime> lastFire = new();

        private Dictionary<string, string> images = new()
        {
            // Backgrounds
            ["bgForest"] = "/iAssets/ForestCity2.png",
            ["bgDesert"] = "/iAssets/DesertCity002.png",
            ["bgSnow"] = "/iAssets/SnowCity2.png",

            // Engines
            ["trainEngine"] = "/iAssets/Engine005A.png",

            // Resources
            ["trainResource"] = "/iAssets/CoalCar009A.png",
            // Rhino Armor
            ["trainRhinoarmor"] = "/iAssets/RhinoCar002A.png",
            ["trainElephant"] = "/iAssets/EleCar001A.png",
            // Cars
            ["trainCannon"] = "/iAssets/Cannoncar005A.png",
            ["trainMissile"] = "/iAssets/Misslecar003A.png",
            ["trainTurret"] = "/iAssets/Turretcar003A.png",
            ["trainDrone"] = "/iAssets/Dronecar003A.png",
            ["trainFlame"] = "/iAssets/fthowercar002.png",
            ["trainAaflak"] = "/iAssets/FlakCar002A.png",
            ["trainAajavelin"] = "/iAssets/USARMY003A.png",
            ["trainFlare"] = "/iAssets/Flarecar002A.png",
            ["trainGatlin"] = "/iAssets/Gatlincar002A.png",           
            ["trainGrenade"] = "/iAssets/GrenadeCar001.png",
            ["trainClustergrenade"] = "/iAssets/RusskeCar002A.png",
            ["trainTesla"] = "/iAssets/TeslaCar001A.png",

            ["trainChina"] = "/iAssets/ChinaCar001.png",
            ["trainChinaku"] = "/iAssets/ChinaKuCar001.png",

            // Car Attack Animations
            ["trainGatlinAttack"] = "/iAssets/Gatlincar002T.png",
            ["trainGrenadeAttack"] = "/iAssets/GrenadeCar001.png",
            ["trainClustergrenadeAttack"] = "/iAssets/RusskeCar002A.png",
            ["trainAaflakAttack"] = "/iAssets/FlakCar002AT.png",
            ["trainTurretAttack"] = "/iAssets/Turretcar003AT.png",
            ["trainMissileAttack"] = "/iAssets/Misslecar003T.png",
            ["trainDroneAttack"] = "/iAssets/Dronecar003T.png",
            ["trainChinaAttack"] = "/iAssets/ChinaCar001.png",
            ["trainCannonAttack"] = "/iAssets/Cannoncar005AT.png",
            ["trainChinakuAttack"] = "/iAssets/ChinaKuCar001.png",
            ["trainFlareAttack"] = "/iAssets/Flarecar002T.png",
            ["trainAajavelinAttack"] = "/iAssets/USARMY003T.png",
            ["trainTeslaAttack"] = "/iAssets/TeslaCar001A.png",
            // Projectiles
            ["projGrenade"] = "/iAssets/GrenadeFX01.png",
            ["projClustergrenade"] = "/iAssets/GrenadeFX02.png",
            // Explosion effect
            ["projExplosion"] = "/iAssets/GrenExplode002.png",
            ["projFlareExplosion"] = "/iAssets/Flaregfx.png",    
            ["projChina"] = "/iAssets/ChinaFire001A.png",

            // Cocaine Bear
            ["trainCocainebear"] = "/iAssets/BearCar001.png",
            ["trainCocainebearAttack"] = "/iAssets/BearCar001.png",  // Can use same for now
            ["projBuffbear"] = "/iAssets/Healtiki002.png",  // new white texture

            // Medic Car
            ["trainMedic"] = "/iAssets/Mediccar002.png",
            ["trainMedicAttack"] = "/iAssets/Mediccar002.png",  // Can use same sprite or healing animation

            // Shaman 
            ["trainShaman"] = "/iAssets/TikiCar001.png",
            ["trainShamanAttack"] = "/iAssets/TikiCar001.png",

            // Heal effects
            ["projHeal"] = "/iAssets/Healcross001.png",  // Green cross or plus sign
            ["projAura"] = "/iAssets/Healtiki001.png",  // Purple mystical glow


            // Caboose
            ["trainCaboose"] = "/iAssets/Caboose002A.png",



            // Drone Projectile Textures
            ["projDroneBullet"] = "/iAssets/DroneBullet001.png",  // 🚁 NEW: Drone's weapon projectile

            // === PROJECTILES ===
            // Train Projectiles
            ["projEngine"] = "/iAssets/GOkuCloud002.png",
            ["projCannon"] = "/iAssets/CannonB002A.png",
            ["projMissile"] = "/iAssets/Missle001.png",
            ["projTurret"] = "/iAssets/Gunfire001.png",
            ["projDrone"] = "/iAssets/Drone001.png",
            ["projFlame"] = "/iAssets/Fthrowergfx.png",
            ["projAaflak"] = "/iAssets/FlakCloud001.png",
            ["projAajavelin"] = "/iAssets/Javelin001.png",
            ["projFlare"] = "/iAssets/Flare002.png",
            ["projGatlin"] = "/iAssets/Gatlingfx.png",
            ["projChinaku"] = "/iAssets/ChinaKufx.png",
            ["projTesla"] = "/iAssets/Teslafx.png",



            // === ENEMY SPRITES ===
            // Tree Enemy (move, attack, death)
            ["enemyTree"] = "/iAssets/TreeMonster001.png",
            ["enemyTreeMove"] = "/iAssets/TreeMonster001.png",     // Optional: walking animation
            ["enemyTreeAttack"] = "/iAssets/TreeMonster001.png",  // Optional: attack animation
            ["enemyTreeDeath"] = "/iAssets/PixelDeath001.png",   // Optional: death animation

            // Rock Enemy (move, attack, death)
            ["enemyRock"] = "/iAssets/RockMonster001.png",
            ["enemyRockMove"] = "/iAssets/RockMonster001.png",
            ["enemyRockAttack"] = "/iAssets/RockMonster001.png",
            ["enemyRockDeath"] = "/iAssets/PixelDeath001.png",

            // Monster1 Enemy (move, attack, death)
            ["enemyMonster1"] = "/iAssets/PoopMonster001.png",
            ["enemyMonster1Move"] = "/iAssets/PoopMonster001.png",
            ["enemyMonster1Attack"] = "/iAssets/PoopMonster001.png",
            ["enemyMonster1Death"] = "/iAssets/PixelDeath001.png",

            // Monster2 Enemy (move, attack, death)
            ["enemyMonster2"] = "/iAssets/PinkMonster001.png",
            ["enemyMonster2Move"] = "/iAssets/PinkMonster001.png",
            ["enemyMonster2Attack"] = "/iAssets/PinkMonster001.png",
            ["enemyMonster2Death"] = "/iAssets/PixelDeath001.png",

            // Bat Enemy (move, attack, death)
            ["enemyBat"] = "/iAssets/BatMonster002.png",
            ["enemyBatMove"] = "/iAssets/BatMonster002.png",
            ["enemyBatAttack"] = "/iAssets/BatMonster002.png",
            ["enemyBatDeath"] = "/iAssets/PixelDeath001.png",

            // Thrower Enemy (NEW - ranged enemy)
            ["enemyThrower"] = "/iAssets/SpaceMonster002.png",
            ["enemyThrowerMove"] = "/iAssets/SpaceMonster002.png",
            ["enemyThrowerAttack"] = "/iAssets/SpaceMonster002.png",// Throwing animation
            ["enemyThrowerDeath"] = "/iAssets/PixelDeath001.png",
            // 🆕 ADD BIRD ENEMY:
            ["enemyBird"] = "/iAssets/AlienBird001.png",         // You'll need to create this
            ["enemyBirdMove"] = "/iAssets/AlienBird001.png",
            ["enemyBirdAttack"] = "/iAssets/AlienBird001.png",
            ["enemyBirdDeath"] = "/iAssets/PixelDeath001.png",
            // Boss Tree (optional - can reuse tree sprites but scaled)
            ["enemyBossTree"] = "/iAssets/TreeMonster001.png", // Reuses tree sprite, just scaled bigger

            // === ENEMY PROJECTILES ===
            ["enemyProj_thrower"] = "/iAssets/Spear001.png",  // ✅ Fixed underscore naming
            ["enemyProj_tree"] = "/iAssets/Rock001.png",       // If trees ever shoot (future)
            ["enemyProj_rock"] = "/iAssets/Rock001.png",       // If rocks ever shoot (future)
            ["enemyProj_monster1"] = "/iAssets/Spear001.png",  // Fallback
            ["enemyProj_monster2"] = "/iAssets/Spear001.png",  // Fallback
            ["enemyProj_bat"] = "/iAssets/Spear001.png",       // Fallback
            ["enemyProj_bird"] = "/iAssets/Spear001.png",      // Fallback
            ["enemyProj_generic"] = "/iAssets/Spear001.png",   // Generic fallback

            // === RAIL TEXTURES ===
            ["railsForest"] = "/iAssets/SnowRail001.png",
            ["railsDesert"] = "/iAssets/SnowRail001.png",
            ["railsSnow"] = "/iAssets/SnowRail001.png",

            // === WOOD TIES TEXTURES ===
            ["tiesForest"] = "/iAssets/Forestwood001.png",
            ["tiesDesert"] = "/iAssets/Forestwood001.png",
            ["tiesSnow"] = "/iAssets/Forestwood001.png",

            // === GROUND TEXTURES ===
            ["groundForest"] = "/iAssets/ForestGround002.png",
            ["groundDesert"] = "/iAssets/DesertGround002.png",
            ["groundSnow"] = "/iAssets/SnowGround002.png",

            // === BALLAST TEXTURES ===
            ["ballastForest"] = "/iAssets/ForestBallast001.png",
            ["ballastDesert"] = "/iAssets/ForestBallast001.png",
            ["ballastSnow"] = "/iAssets/ForestBallast001.png",

            // === FOLIAGE LAYER TEXTURES ===
            // Layer 1 - Mid-distance (y=675)
            ["foliageForestLayer1"] = "/iAssets/ForestFoliageLay102.png",
            ["foliageDesertLayer1"] = "/iAssets/DesertFoliageLay102.png",
            ["foliageSnowLayer1"] = "/iAssets/SnowFoliageLay102.png",

            // Layer 2 - Far distance (y=500)
            ["foliageForestLayer2"] = "/iAssets/ForestFoliageLay2.png",
            ["foliageDesertLayer2"] = "/iAssets/DesertFoliageLay2.png",
            ["foliageSnowLayer2"] = "/iAssets/SnowFoliageLay2.png",

            // Layer 3 - Foreground (y=720)
            ["foliageForestLayer3"] = "/iAssets/ForestFoliageLay302.png",
            ["foliageDesertLayer3"] = "/iAssets/DesertFoliageLay302.png",
            ["foliageSnowLayer3"] = "/iAssets/SnowFoliageLay302.png",
        };


        private Dictionary<string, CarInfo> carTypes = new()
        {
            ["engine"] = new()
            {
                Name = "Engine",
                Damage = 0,
                FireRate = 500,
                FireRangeX = 50,
                FireRangeY = -50,
                FireRadius = 50,
                Color = "#555",
                MaxHp = 300  // CHANGED: was 200
            },
            ["rhinoarmor"] = new()
            {
                Name = "Africa Rhino Armor",
                Damage = 0,
                FireRate = 0,
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 0,
                Color = "#696969",  // Dark gray
                MaxHp = 600,        // TANKY! (2x engine HP)
                ProjectileWidth = 0,
                ProjectileHeight = 0
            },
            ["resource"] = new()
            {
                Name = "Resource Car",
                Damage = 0,
                FireRate = 0,
                Color = "#8B4513",
                IsLocked = false,
                MaxHp = 130  // CHANGED: was 120
            },
            ["elephant"] = new()
            {
                Name = "UM Elephant Car",
                Damage = 0,
                FireRate = 0,
                Color = "#8B4513",
                IsLocked = false,
                MaxHp = 1000  // CHANGED: was 120
            },
            ["cannon"] = new()
            {
                Name = "Cannon Car",
                FiringPositions = new List<(int x, int y)> { (0, 0) },
                Damage = 3,
                FireRate = 4500,
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 350,
                Color = "#ff6b6b",
                MaxHp = 90,  // CHANGED: was 80
                ExplosionRadius = 50,
                ProjectileWidth = 40,
                ProjectileHeight = 40
            },

            ["missile"] = new()
            {
                Name = "Missile Car",
                Damage = 2,
                FireRate = 2000,
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 300,
                Color = "#ffd700",
                MaxHp = 80,  // CHANGED: was 70
                ExplosionRadius = 80,
                ProjectileWidth = 35,
                ProjectileHeight = 35
            },

            ["turret"] = new()
            {
                Name = "Auto-Turret",
                Damage = 1,
                FireRate = 500,
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 250,
                Color = "#4ecdc4",
                MaxHp = 110,  // CHANGED: was 100
                FiringPositions = new List<(int x, int y)> { (-30, 10), (30, 10) },
                ProjectileWidth = 28,
                ProjectileHeight = 28
            },

            ["drone"] = new()
            {
                Name = "Drone Bay",
                Damage = 4,
                FireRate = 3000,
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 350,
                Color = "#a78bfa",
                MaxHp = 70,  // CHANGED: was 60
                ProjectileWidth = 30,
                ProjectileHeight = 30
            },

            ["flame"] = new()
            {
                Name = "Flame Thrower",
                Damage = 5,
                FireRate = 100,
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 100,
                Color = "#FAC28B",
                MaxHp = 100,  // CHANGED: was 90
                ScatterCount = 5,
                ScatterAngle = 30,
                ProjectileWidth = 25,
                ProjectileHeight = 25
            },
            ["tesla"] = new()
            {
                Name = "Tesla Coil Car",
                Damage = 0.5f,
                FireRate = 1500,
                FireRangeX = 0,
                FireRangeY = -400,

                // Detection box
                DetectionBoxX = 100,
                DetectionBoxY = 600,
                Color = "#FAC28B",
                MaxHp = 100,  // CHANGED: was 90
                ScatterCount = 3,
                ProjectileWidth = 25,
                ProjectileHeight = 25,
                BurstDelayMin = 3,
                BurstDelayMax = 6,
                FiringPositions = new List<(int x, int y)> { (-40, -25), (10, -25), (30, -20) }

            },
            ["aajavelin"] = new()
            {
                Name = "AA Javelin",
                Damage = 6,
                FireRate = 3500,
                FireRangeX = 0,
                FireRangeY = -500,
                FireRadius = 150,
                Color = "#1E90FF",
                MaxHp = 85,  // CHANGED: was 75
                ExplosionRadius = 60,
                ProjectileWidth = 38,
                ProjectileHeight = 38
            },

            ["gatlin"] = new()
            {
                Name = "Gatlin Car",
                Damage = 0.1f,
                FireRate = 100,
                FireRangeX = 100,
                FireRangeY = -350,
                FireRadius = 150,
                Color = "#4ecdc4",
                MaxHp = 110,
                FiringPositions = new List<(int x, int y)> { (-40, -25), (10, -25), (30, -20) },
                ProjectileCount = 2,
                ProjectileWidth = 20,
                ProjectileHeight = 20,

                // 🆕 ADD THESE THREE LINES:
                ProjectileTrajectory = TrajectoryType.Ballistic,
                ProjectileGravity = 0.5,      // Gravity strength (higher = more arc)
                ProjectileSpeed = 12           // Initial bullet speed
            },
            ["chinaku"] = new()
            {
                Name = "China Ku",
                Damage = 0.2f,
                FireRate = 300,
                FireRangeX = 100,
                FireRangeY = -250,
                FireRadius = 200,
                Color = "#4ecdc4",
                MaxHp = 120,
                FiringPositions = new List<(int x, int y)> { (-40, -25), (10, -25), (30, -20), (-20, -20), (15, -30), (10, -25) },
                ProjectileCount = 1,
                ProjectileWidth = 20,
                ProjectileHeight = 20,

                // 🆕 ADD THESE THREE LINES:
                ProjectileTrajectory = TrajectoryType.Ballistic,
                ProjectileGravity = 0.5,      // Gravity strength (higher = more arc)
                ProjectileSpeed = 4           // Initial bullet speed
            },
            ["aaflak"] = new()
            {
                Name = "AA Flak",
                Damage = 2,
                FireRate = 1600,
                FireRangeX = 0,
                FireRangeY = -550,
              
                // Detection box
                DetectionBoxX = 400,
                DetectionBoxY = 300,

                // 🚁 NEW: Shot spawns offset from car center
              //  ShotOffsetX = 0,      // Adjust this! (e.g., 50 = spawn 50px to the right)
              //  ShotOffsetY = 500,   // Adjust this! (e.g., -100 = spawn 100px above car)

                Color = "#FF4500",
                MaxHp = 95,
                ExplosionRadius = 120,
                ScatterCount = 3,
                ScatterAngle = 25,
                ProjectileWidth = 45,
                ProjectileHeight = 45,
                InstantEffect = true,
                BurstDelayMin = 3,
                BurstDelayMax = 6
            },

            ["flare"] = new()
            {
                Name = "Flare Car",
                Damage = 3,
                FireRate = 4000,
                FireRangeX = 100,
                FireRangeY = -550,
                FireRadius = 150,
                Color = "#8B4513",
                MaxHp = 95,
                ExplosionRadius = 120,
                ProjectileWidth = 35,
                ProjectileHeight = 35,

                // Ballistic trajectory like Gatlin
                ProjectileTrajectory = TrajectoryType.Ballistic,
                ProjectileGravity = 0.6,      // Higher gravity = steeper arc
                ProjectileSpeed = 10,          // Slower than bullets

                // NEW: Mark this weapon as needing explosion animation
                PlayExplosionAnimation = true      // ADD THIS
            },
            ["grenade"] = new()
            {
                Name = "Grenade Launcher",
                Damage = 8,
                FireRate = 4000,
                FireRangeX = 100,
                FireRangeY = -350,
                FireRadius = 150,
                Color = "#8B4513",
                MaxHp = 95,
                ExplosionRadius = 120,
                ProjectileWidth = 35,
                ProjectileHeight = 35,

                // Ballistic trajectory like Gatlin
                ProjectileTrajectory = TrajectoryType.Ballistic,
                ProjectileGravity = 0.6,      // Higher gravity = steeper arc
                ProjectileSpeed = 10,          // Slower than bullets

                // NEW: Mark this weapon as needing explosion animation
                PlayExplosionAnimation = true
            },
            ["clustergrenade"] = new()
            {
                Name = "Russke Cluster",
                // Basic stats
                Damage = 4,
                FireRate = 4000,

                // Use ballistic trajectory (like grenade)
                ProjectileTrajectory = TrajectoryType.Ballistic,
                ProjectileGravity = 0.6,
                ProjectileSpeed = 10,

                // Use 3 hardpoints (like gatlin)
                FiringPositions = new List<(int x, int y)> { (-40, -25), (10, -25), (30, -20) },

                // Use cluster scatter (like flak)
                ScatterCount = 5,      // 5 cluster pieces per grenade
                ScatterAngle = 40,     // Spread pattern

                // Use explosion animation (like grenade)
                PlayExplosionAnimation = true,

                // Range - adjusted Y for low-mid targets
                FireRangeX = 100,
                FireRangeY = -250,     // LOWER than grenade's -350
                FireRadius = 150,

                ExplosionRadius = 90,
                ProjectileWidth = 35,
                ProjectileHeight = 35,
            },
            ["china"] = new()
            {
                Name = "China Cart",
                Damage = 2,                    // Damage per fireball hit
                FireRate = 100,                // How often it checks for collisions
                FireRangeX = 0,                // Not used (always centers on car)
                FireRangeY = 0,                // Not used
                FireRadius = 0,                // Not used (fireballs have fixed positions)
                Color = "#FF4500",             // Orange-red
                MaxHp = 100,

                // 🆕 CHINA CART SPECIAL PROPERTIES
                ProjectileCount = 6,           // Number of fireballs on the rotating line
                ProjectileWidth = 32,
                ProjectileHeight = 32,

                // We'll use ScatterCount to store the line length (distance from car center)
                ScatterCount = 150,            // Max distance from car center (pixels)

                // Special flag to mark this as a rotating weapon
                IsRotatingWeapon = true
            },

            ["medic"] = new()
            {
                Name = "Medic Car",
                Damage = 0,
                FireRate = 30000,  // 30 seconds (we'll use this as heal cooldown)
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 0,
                Color = "#FF6B9D",  // Pink/red medical color
                MaxHp = 90,
                ProjectileWidth = 40,
                ProjectileHeight = 40,
                IsHealingCar = true,  // NEW FLAG
                HealAmount = 50
            },

            ["shaman"] = new()
            {
                Name = "Voodoo Shaman",
                Damage = 0,
                FireRate = 1000,  // 1 second tick for regen application
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 0,
                Color = "#9D4EDD",  // Purple mystical color
                MaxHp = 80,
                IsRegenAura = true,  // NEW FLAG
                RegenPerSecond = 0.5  // 0.5 HP per second per shaman
            },
            ["cocainebear"] = new()
            {
                Name = "Cocaine Bear",
                Damage = 0,
                FireRate = 20000,  // 20 seconds (we'll use this as buff cooldown)
                FireRangeX = 0,
                FireRangeY = 0,
                FireRadius = 0,
                Color = "#8B4513",  // Brown
                MaxHp = 95,
                ProjectileWidth = 40,
                ProjectileHeight = 40,
                IsBuffCar = true,  // NEW FLAG (we'll add this property next)
                BuffDuration = 3000,  // 3 seconds
                FireRateBoostMultiplier = 0.67  // 50% faster (NEW PROPERTY)
            },

            ["caboose"] = new()
            {
                Name = "Caboose",
                Damage = 0,
                FireRate = 0,
                Color = "#8b4513",
                IsLocked = true,
                MaxHp = 160  // CHANGED: was 150
            }
        };
        private Dictionary<string, EnemyInfo> enemyTypes = new()
        {
            ["tree"] = new()
            {
                Hp = 3,
                Xp = 5,
                Points = 10,
                Speed = 1,
                Color = "#2d5016",
                SpawnXMin = 500,
                SpawnXMax = 3200,
                SpawnYMin = 600,
                SpawnYMax = 750,
                MinLevel = 1,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [1] = 0.5,   // 100% weight at level 1-9
                    [10] = 0.8,  // 80% weight at level 10-19
                    [20] = 0.9,  // 50% weight at level 20-29
                    [30] = 1.2,  // 30% weight at level 30+
                }
            },

            ["rock"] = new()
            {
                Hp = 5,
                Xp = 8,
                Points = 15,
                Speed = 1,
                Color = "#808080",
                SpawnXMin = 500,
                SpawnXMax = 3200,
                SpawnYMin = 550,
                SpawnYMax = 700,
                MinLevel = 1,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [1] = 0.6,   // Slightly less common than trees early
                    [10] = 0.8,  // Full weight mid-game
                    [20] = 0.9,
                    [30] = 1.2,
                }
            },

            ["monster1"] = new()
            {
                Hp = 15,
                Xp = 30,
                Points = 50,
                Speed = 1.5,
                Color = "#8b0000",
                SpawnXMin = 500,
                SpawnXMax = 3200,
                SpawnYMin = 200,
                SpawnYMax = 650,
                MinLevel = 1,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [1] = 0.2,   // Rare early game
                    [10] = 0.5,  // Common mid-game
                    [20] = 0.8,  // Very common late game
                    [30] = 1.0,
                }
            },

            ["monster2"] = new()
            {
                Hp = 20,
                Xp = 60,
                Points = 100,
                Speed = 2,
                Color = "#4b0082",
                SpawnXMin = 500,
                SpawnXMax = 3200,
                SpawnYMin = 350,
                SpawnYMax = 600,
                MinLevel = 3,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [3] = 0.3,
                    [10] = 0.5,
                    [20] = 0.8,
                    [30] = 1.8,
                },
                // 🆕 ADD THESE LINES FOR BOUNCING:
                MovementPattern = MovementPattern.Bounce,
                MovementAmplitude = 60,   // Bounces 60 pixels up/down
                MovementFrequency = 0.08  // Moderate bounce speed
            },

            ["bat"] = new()
            {
                Hp = 8,
                Xp = 15,
                Points = 25,
                Speed = 2,
                Color = "#4a0e4e",
                SpawnXMin = 500,
                SpawnXMax = 3200,
                SpawnYMin = 50,
                SpawnYMax = 200,
                MinLevel = 4,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [4] = 0.3,
                    [10] = 0.6,
                    [20] = 0.8,
                    [30] = 1.2,  // Less common very late
                }
            },

            ["thrower"] = new()
            {
                Hp = 12,
                Xp = 20,
                Points = 40,
                Speed = 0.8,
                Color = "#8b008b",
                SpawnXMin = 800,
                SpawnXMax = 1500,
                SpawnYMin = 300,
                SpawnYMax = 500,
                CanShoot = true,
                ShootRange = 600,
                ShootRate = 2000,
                ProjectileDamage = 5,
                MinLevel = 8,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [8] = 0.1,
                    [10] = 0.2,
                    [20] = 0.4,
                    [30] = 0.6,
                }
            },
            ["bird"] = new()
            {
                Hp = 6,
                Xp = 12,
                Points = 30,
                Speed = 2.5,  // Fast flyer
                Color = "#87CEEB",  // Sky blue
                SpawnXMin = 600,
                SpawnXMax = 3200,
                SpawnYMin = 100,
                SpawnYMax = 300,  // Flies high
                MinLevel = 12,
                LevelSpawnWeights = new Dictionary<int, double>
                {
                    [12] = 0.5,  // Uncommon when first available
                    [20] = 1.0,  // Common mid-game
                    [30] = 1.2,  // Very common late game
                },
                // 🆕 BIRD HAS SINE WAVE MOVEMENT:
                MovementPattern = MovementPattern.Sine,
                MovementAmplitude = 40,   // Flies up/down 40 pixels
                MovementFrequency = 0.05  // Smooth gentle waves
            },

            ["bossTree"] = new()
            {
                Hp = 100,
                Xp = 200,
                Points = 500,
                Speed = 0.5,
                Color = "#1a3d0f",
                SpawnXMin = 1200,
                SpawnXMax = 1500,
                SpawnYMin = 500,
                SpawnYMax = 600,
                MinLevel = 5,  // Boss only (manual spawn)
                LevelSpawnWeights = new Dictionary<int, double>() // Empty - bosses don't random spawn
            }
        };

        private Dictionary<int, string> bossSpawns = new()
        {
            [5] = "tree",
            [10] = "monster2",
            [15] = "rock",
            [20] = "monster1",
            [25] = "bat",
            [30] = "tree",
            [35] = "monster2",
            [40] = "rock",
            [45] = "monster1",
            [50] = "bat",
            [55] = "tree",
            [60] = "monster2",
            [65] = "rock",
            [70] = "monster1",
            [75] = "bat",
            [80] = "tree",
            [85] = "monster2",
            [90] = "rock",
            [95] = "monster1",
            [100] = "monster2"  // Just add this! Megaboss wtf
            // Add more as needed!
        };





        protected override void OnInitialized()
        {
            trainCars = new List<TrainCar>
    {
         new TrainCar { Type = "engine", Hp = carTypes["engine"].MaxHp },
        new TrainCar { Type = "resource", Hp = carTypes["resource"].MaxHp },
        new TrainCar { Type = "cannon", Hp = carTypes["cannon"].MaxHp },
        new TrainCar { Type = "aaflak", Hp = carTypes["aaflak"].MaxHp },
          new TrainCar { Type = "medic", Hp = carTypes["medic"].MaxHp },
            new TrainCar { Type = "chinaku", Hp = carTypes["chinaku"].MaxHp },
        new TrainCar { Type = "caboose", Hp = carTypes["caboose"].MaxHp }
    };
          
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    var result = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "highscores");
                    Console.WriteLine($"OnAfterRender - RAW: '{result}'");

                    if (!string.IsNullOrEmpty(result))
                    {
                        highScores = System.Text.Json.JsonSerializer.Deserialize<List<HighScore>>(result)
                            ?? new List<HighScore>();
                        Console.WriteLine($"Loaded {highScores.Count} scores");
                    }
                    else
                    {
                        Console.WriteLine("No scores found, initializing empty list");
                        highScores = new List<HighScore>();
                    }
                    // NEW: Cache background images
                    var biomes = new[] { "Forest", "Desert", "Snow" };
                    var layerKeys = new[]
                    {
    ("bg", "bg{0}"),
    ("ground", "ground{0}"),
    ("foliageLayer2", "foliage{0}Layer2"),
    ("foliageLayer1", "foliage{0}Layer1"),
     ("foliageLayer3", "foliage{0}Layer3")  // NEW
};

                    foreach (var biome in biomes)
                    {
                        foreach (var (layerType, keyPattern) in layerKeys)
                        {
                            var key = string.Format(keyPattern, biome);

                            if (images.ContainsKey(key))
                            {
                                await JSRuntime.InvokeVoidAsync("cacheBackgroundImage", key, images[key]);
                            }
                        }
                    }

                    // NEW: Draw initial canvas
                    await DrawBackgroundCanvas();
                    await DrawForegroundCanvas();  // NEW
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Load error: {ex.Message}");
                    highScores = new List<HighScore>();
                }
            }
        }

      
        //"missile","turret","drone","flame" 
        private async Task LoadImage(InputFileChangeEventArgs e, string key)
        {
            var file = e.File;
            if (file != null)
            {
                var buffer = new byte[file.Size];
                await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).ReadAsync(buffer);
                var base64 = Convert.ToBase64String(buffer);
                images[key] = $"data:{file.ContentType};base64,{base64}";
            }
        }

        private void StartGame()
        {
            gameState = GameState.Playing;
            score = 0;
            xp = 0;
            level = 1;
            xpNeeded = 100;
            totalKills = 0;
            playerInitials = "";
            showInitialsInput = false;

          
            trainCars = new List<TrainCar>
    {
     new TrainCar { Type = "engine", Hp = carTypes["engine"].MaxHp },
        new TrainCar { Type = "resource", Hp = carTypes["resource"].MaxHp },
        new TrainCar { Type = "cannon", Hp = carTypes["cannon"].MaxHp },
        new TrainCar { Type = "aaflak", Hp = carTypes["aaflak"].MaxHp },
              new TrainCar { Type = "medic", Hp = carTypes["medic"].MaxHp },
                  new TrainCar { Type = "chinaku", Hp = carTypes["chinaku"].MaxHp },
        new TrainCar { Type = "caboose", Hp = carTypes["caboose"].MaxHp }
    };

            enemies.Clear();
            effects.Clear();
            // In StartGame() method, add:
            enemyProjectiles.Clear();
            lastEnemyShot.Clear();
            gameSpeed = 2;
            scrollOffset = 0;
            environment = new Random().Next(3);

            weatherCycle.CurrentBiome = (SnowX.Weather.BiomeType)environment;

            lastFire.Clear();

            gameTimer?.Dispose();
            gameTimer = new System.Timers.Timer(16);
            gameTimer.Elapsed += GameTick;
            gameTimer.AutoReset = true;
            gameTimer.Start();
        }


        private void GameTick(object? sender, ElapsedEventArgs e)
        {
            InvokeAsync(() =>
            {
                if (gameState != GameState.Playing) return;

           
                // At the start of GameTick, update speed based on throttle
                gameSpeed = baseGameSpeed * throttle;


                // Update scroll
                scrollOffset += gameSpeed;

                if (scrollOffset != lastCanvasScrollOffset || viewportOffsetX != lastCanvasViewportOffset)
                {
                    _ = DrawBackgroundCanvas();
                    _ = DrawForegroundCanvas();  // NEW
                    lastCanvasScrollOffset = scrollOffset;
                    lastCanvasViewportOffset = viewportOffsetX;
                }
                // ENGINE REGENERATION - ADD THIS ENTIRE BLOCK
                var engineCar = trainCars.FirstOrDefault(c => c.Type == "engine");
                if (engineCar != null)
                {
                    var timeSinceRegen = (DateTime.Now - lastEngineRegen).TotalMilliseconds;
                    if (timeSinceRegen >= EngineRegenIntervalMs)
                    {
                        if (engineCar.Hp < carTypes["engine"].MaxHp)
                        {
                            engineCar.Hp = Math.Min(engineCar.Hp + EngineRegenAmount, carTypes["engine"].MaxHp);
                        }
                        lastEngineRegen = DateTime.Now;
                    }
                }
                // RHINO ARMOR REGENERATION
                foreach (var rhinoCar in trainCars.Where(c => c.Type == "rhinoarmor"))
                {
                    var timeSinceRhinoRegen = (DateTime.Now - lastEngineRegen).TotalMilliseconds;
                    if (timeSinceRhinoRegen >= 1000)  // 1 second (vs engine's 2 seconds)
                    {
                        if (rhinoCar.Hp < carTypes["rhinoarmor"].MaxHp)
                        {
                            rhinoCar.Hp = Math.Min(rhinoCar.Hp + 1, carTypes["rhinoarmor"].MaxHp);
                        }
                    }
                }
                // Adds Death animation

                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var enemy = enemies[i];

                    // Handle death animation
                    if (enemy.MarkedForDeath)
                    {
                        var timeSinceDeath = (DateTime.Now - enemy.StateChangeTime).TotalMilliseconds;
                        if (timeSinceDeath > 500)
                        {
                            enemies.RemoveAt(i);
                            continue;
                        }
                        // Still playing death animation, don't move
                    }
                    else if (enemy.X < -50 || enemy.Hp <= 0)
                    {
                        // Mark for death if off screen or no HP
                        if (!enemy.MarkedForDeath && enemy.Hp <= 0)
                        {
                            enemy.MarkedForDeath = true;
                            enemy.AnimState = "death";
                            enemy.StateChangeTime = DateTime.Now;
                        }
                        else if (enemy.X < -50)
                        {
                            enemies.RemoveAt(i);
                            continue;
                        }
                    }
                    else
                    {
                        // 🆕 NORMAL MOVEMENT WITH PATTERNS:

                        // Horizontal movement (always moves left)
                        enemy.X -= enemy.Speed;

                        // 🆕 VERTICAL MOVEMENT BASED ON PATTERN:
                        enemy.MovementTime += 1; // Increment time counter

                        switch (enemy.Pattern)
                        {
                            case MovementPattern.Sine:
                                // Smooth sine wave
                                enemy.Y = enemy.BaseY + Math.Sin(enemy.MovementTime * enemy.Frequency) * enemy.Amplitude;
                                break;

                            case MovementPattern.Bounce:
                                // Triangle wave (linear up/down bounce)
                                double bouncePhase = (enemy.MovementTime * enemy.Frequency) % (Math.PI * 2);
                                double bounceValue = Math.Abs((bouncePhase / Math.PI) - 1) * 2 - 1; // -1 to 1 triangle
                                enemy.Y = enemy.BaseY + bounceValue * enemy.Amplitude;
                                break;

                            case MovementPattern.Erratic:
                                // Random jerky movement every 20 frames
                                if ((int)enemy.MovementTime % 20 == 0)
                                {
                                    enemy.Y = enemy.BaseY + (random.NextDouble() * 2 - 1) * enemy.Amplitude;
                                }
                                break;

                            case MovementPattern.Straight:
                            default:
                                // No vertical movement
                                enemy.Y = enemy.BaseY;
                                break;
                        }

                        // Return to move state after attack animation
                        if (enemy.AnimState == "attack")
                        {
                            var timeSinceAttack = (DateTime.Now - enemy.StateChangeTime).TotalMilliseconds;
                            if (timeSinceAttack > 300)
                            {
                                enemy.AnimState = "move";
                            }
                        }
                    }
                }


                // Update effects
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    var effect = effects[i];

                    // Handle spawn delay
                    if (effect.SpawnDelay > 0)
                    {
                        effect.SpawnDelay--;
                        if (effect.SpawnDelay == 0)
                        {
                            effect.IsVisible = true;
                        }
                        continue;
                    }

                    // Decrement life
                    effect.Life--;

                    // 🆕 CHECK IF EXPLOSION PROJECTILE SHOULD EXPLODE
                    // 🆕 CHECK IF PROJECTILE SHOULD EXPLODE (works for all explosion types)
                    if (effect.Life <= 0 && !effect.HasExploded && carTypes.ContainsKey(effect.Type) && carTypes[effect.Type].PlayExplosionAnimation)
                    {
                        // Spawn explosion animation at projectile's final position
                        effect.HasExploded = true;

                        // 🎯 Determine which explosion type to use
                        string explosionType = effect.Type switch
                        {
                            "flare" => "flareExplosion",
                            "grenade" => "explosion",//place holder for new explosionanimations jsut quick add here-->>
                            _ => "explosion"  // Default fallback
                        };

                        effects.Add(new Effect
                        {
                            X = effect.X,
                            Y = effect.Y,
                            TargetX = effect.X,
                            TargetY = effect.Y,
                            Type = explosionType,  // ✅ Now uses correct explosion type!
                            Life = 20,
                            Width = 64,
                            Height = 64,
                            IsExplosion = true,
                            IsVisible = true
                        });
                    }
                    // UPDATE POSITION BASED ON TRAJECTORY TYPE
                    if (effect.Trajectory == TrajectoryType.Ballistic)
                    {
                        // Apply velocity
                        effect.X += effect.VelocityX;
                        effect.Y += effect.VelocityY;

                        // Apply gravity to Y velocity
                        effect.VelocityY += effect.Gravity;

                        // UPDATE ROTATION TO MATCH CURRENT VELOCITY DIRECTION
                        effect.RotationAngle = Math.Atan2(effect.VelocityY, effect.VelocityX) * (180.0 / Math.PI);
                    }
                    else
                    {
                        // Linear trajectory (original lerp behavior)
                        effect.X += (effect.TargetX - effect.X) * 0.1;
                        effect.Y += (effect.TargetY - effect.Y) * 0.1;
                    }

                    // Remove if life expired
                    if (effect.Life <= 0)
                    {
                        effects.RemoveAt(i);
                    }
                }

                // Spawn enemies - double spawn rate every 5 levels
                double spawnRate = 0.01 * Math.Pow(2, (level - 1) / 5);

                // Only spawn if under max
                if (enemies.Count < MaxEnemies && random.NextDouble() < spawnRate)
                {
                    // 🆕 BUILD WEIGHTED POOL OF AVAILABLE ENEMIES
                    var weightedEnemies = new List<(string type, double weight)>();

                    foreach (var enemyType in enemyTypes.Keys)
                    {
                        // Skip bosses and enemies not yet available
                        if (enemyType == "bossTree") continue;

                        double weight = GetSpawnWeightForEnemy(enemyType, level);

                        if (weight > 0)
                        {
                            weightedEnemies.Add((enemyType, weight));
                        }
                    }

                    // 🆕 WEIGHTED RANDOM SELECTION
                    if (weightedEnemies.Count > 0)
                    {
                        double totalWeight = weightedEnemies.Sum(e => e.weight);
                        double roll = random.NextDouble() * totalWeight;
                        double cumulative = 0;

                        string selectedType = weightedEnemies[0].type;

                        foreach (var (type, weight) in weightedEnemies)
                        {
                            cumulative += weight;
                            if (roll <= cumulative)
                            {
                                selectedType = type;
                                break;
                            }
                        }

                        var enemyInfo = enemyTypes[selectedType];

                        // Use enemy-specific spawn ranges
                        double randomX = enemyInfo.SpawnXMin + random.NextDouble() * (enemyInfo.SpawnXMax - enemyInfo.SpawnXMin);
                        double randomY = enemyInfo.SpawnYMin + random.NextDouble() * (enemyInfo.SpawnYMax - enemyInfo.SpawnYMin);

                        enemies.Add(new Enemy
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = selectedType,
                            X = randomX,
                            Y = randomY,
                            Hp = enemyInfo.Hp,
                            MaxHp = enemyInfo.Hp,
                            Speed = enemyInfo.Speed,
                            // 🆕 INITIALIZE MOVEMENT PROPERTIES:
                            BaseY = randomY,
                            MovementTime = 0,
                            Pattern = enemyInfo.MovementPattern,
                            Amplitude = enemyInfo.MovementAmplitude,
                            Frequency = enemyInfo.MovementFrequency
                        });
                    }
                }

                // 🚁 DRONE SHOOTING SYSTEM
                for (int i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];

                    // Only active drones shoot
                    if (effect.Type != "drone" || effect.MaxHp == 0 || effect.Hp <= 0)
                        continue;

                    // Check if drone is close to target position (hovering)
                    double distToTarget = Math.Sqrt(
                        Math.Pow(effect.X - effect.TargetX, 2) +
                        Math.Pow(effect.Y - effect.TargetY, 2)
                    );

                    if (distToTarget < 50) // Close enough to start shooting
                    {
                        // Fire every 300ms
                        var timeSinceShot = (DateTime.Now - effect.LastDroneShot).TotalMilliseconds;
                        if (timeSinceShot > 300)
                        {
                            // Find nearest enemy within 150px range
                            var nearestEnemy = enemies
                                .Where(e => e.Hp > 0 &&
                                    Math.Sqrt(Math.Pow(e.X - effect.X, 2) + Math.Pow(e.Y - effect.Y, 2)) <= 150)
                                .OrderBy(e => Math.Sqrt(Math.Pow(e.X - effect.X, 2) + Math.Pow(e.Y - effect.Y, 2)))
                                .FirstOrDefault();

                            if (nearestEnemy != null)
                            {
                                effect.LastDroneShot = DateTime.Now;

                                // Spawn drone bullet
                                effects.Add(new Effect
                                {
                                    X = effect.X,
                                    Y = effect.Y,
                                    TargetX = nearestEnemy.X,
                                    TargetY = nearestEnemy.Y,
                                    Type = "droneBullet",  // ✅ CHANGED: Was "drone", now "droneBullet"
                                    Life = 30,
                                    Width = 20,
                                    Height = 20
                                });
                            }
                        }
                    }
                }
                // Check enemy collisions with train
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    var enemy = enemies[i];

                    // 🚁 NEW: Check collision with drones first
                    bool hitDrone = false;
                    for (int d = effects.Count - 1; d >= 0; d--)  // ✅ CHANGED: Loop backwards
                    {
                        var drone = effects[d];
                        if (drone.Type != "drone" || drone.MaxHp == 0 || drone.Hp <= 0)
                            continue;

                        // Simple box collision
                        if (Math.Abs(enemy.X - drone.X) < 40 && Math.Abs(enemy.Y - drone.Y) < 40)
                        {
                            drone.Hp -= enemy.Hp;  // Drone takes damage

                            // ✅ ADDED: Mark enemy for death animation
                            if (!enemy.MarkedForDeath)
                            {
                                enemy.MarkedForDeath = true;
                                enemy.AnimState = "death";
                                enemy.StateChangeTime = DateTime.Now;

                                var enemyInfo = enemyTypes[enemy.Type];
                                score += enemyInfo.Points;
                                xp += enemyInfo.Xp;
                                totalKills++;
                            }

                            hitDrone = true;

                            if (drone.Hp <= 0)
                            {
                                effects.RemoveAt(d);
                            }
                            break;
                        }
                    }

                    if (hitDrone)
                    {
                        continue;  // ✅ CHANGED: Don't remove enemy here, let death animation play
                    }

                    if (enemy.X < 0 && enemy.Hp > 0)
                    {
                        // Find which car was hit based on X position
                        int carIndex = (int)((enemy.X - 50 + viewportOffsetX) / 150);
                        carIndex = Math.Max(0, Math.Min(trainCars.Count - 1, carIndex));

                        // Get TrainCar instance
                        var car = trainCars[carIndex];

                        // Subtract HP from this car instance
                        car.Hp -= (int)enemy.Hp;

                        // Check if it's the engine that died
                        if (car.Type == "engine" && car.Hp <= 0)
                        {
                            gameTimer?.Stop();
                            CheckHighScore();
                            return;
                        }

                        // Remove destroyed non-engine cars
                        if (car.Hp <= 0 && car.Type != "engine")
                        {
                            trainCars.RemoveAt(carIndex);
                        }

                        // Remove the enemy
                        enemies.RemoveAt(i);
                    }
                }

                // Enemy shooting
                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    var enemyInfo = enemyTypes[enemy.Type];

                    if (!enemyInfo.CanShoot) continue;
                    if (enemy.Hp <= 0 || enemy.MarkedForDeath) continue;

                    var shootKey = enemy.Id;

                    // Check if in range and ready to shoot
                    if (enemy.X < enemyInfo.ShootRange)
                    {
                        if (!lastEnemyShot.ContainsKey(shootKey) ||
                            (DateTime.Now - lastEnemyShot[shootKey]).TotalMilliseconds > enemyInfo.ShootRate)
                        {
                            lastEnemyShot[shootKey] = DateTime.Now;

                            // Change to attack state briefly
                            enemy.AnimState = "attack";
                            enemy.StateChangeTime = DateTime.Now;

                            // 🆕 PICK A RANDOM TRAIN CAR TO TARGET
                            if (trainCars.Count > 0)
                            {
                                int targetCarIndex = random.Next(trainCars.Count);
                                int targetCarX = 50 + targetCarIndex * 192 + 96;  // Car center X
                                int targetCarY = 660 + 32;  // Car center Y

                                // Calculate projectile velocity to hit target
                                double dx = targetCarX - enemy.X;
                                double dy = targetCarY - (enemy.Y + 20);
                                double distance = Math.Sqrt(dx * dx + dy * dy);

                                // Normalize and apply speed
                                double projectileSpeed = 4.0;
                                double velocityX = (dx / distance) * projectileSpeed;
                                double velocityY = (dy / distance) * projectileSpeed;

                                // Spawn projectile with targeting
                                enemyProjectiles.Add(new EnemyProjectile
                                {
                                    Type = enemy.Type,
                                    X = enemy.X,
                                    Y = enemy.Y + 20,  // Shoot from center-ish of enemy
                                    Speed = projectileSpeed,
                                    Damage = enemyInfo.ProjectileDamage,
                                    Width = 24,
                                    Height = 24,
                                    // 🆕 SET TARGET AND VELOCITY:
                                    TargetX = targetCarX,
                                    TargetY = targetCarY,
                                    VelocityX = velocityX,
                                    VelocityY = velocityY
                                });
                            }
                        }
                    }
                }
                // Update enemy projectiles
                for (int i = enemyProjectiles.Count - 1; i >= 0; i--)
                {
                    var proj = enemyProjectiles[i];

                    // 🆕 USE VELOCITY FOR MOVEMENT (instead of just left)
                    proj.X += proj.VelocityX;
                    proj.Y += proj.VelocityY;

                    // Remove if off screen (expanded bounds to catch projectiles going any direction)
                    if (proj.X < -100 || proj.X > 3300 || proj.Y < -100 || proj.Y > 900)
                    {
                        enemyProjectiles.RemoveAt(i);
                        continue;
                    }

                    // 🆕 IMPROVED COLLISION WITH TRAIN CARS (with bigger hitbox tolerance)
                    for (int carIdx = 0; carIdx < trainCars.Count; carIdx++)
                    {
                        int carX = 50 + carIdx * 192;
                        int carY = 660;

                        // Expanded hitbox (projectile center within car bounds + tolerance)
                        double tolerance = 20; // Extra pixels for easier hits
                        if (proj.X >= carX - tolerance && proj.X <= carX + 192 + tolerance &&
                            proj.Y >= carY - tolerance && proj.Y <= carY + 64 + tolerance)
                        {
                            // Hit this car!
                            var car = trainCars[carIdx];
                            car.Hp -= (int)proj.Damage;

                            // Check if engine died
                            if (car.Type == "engine" && car.Hp <= 0)
                            {
                                gameTimer?.Stop();
                                CheckHighScore();
                                return;
                            }

                            // Remove destroyed non-engine cars
                            if (car.Hp <= 0 && car.Type != "engine")
                            {
                                trainCars.RemoveAt(carIdx);
                            }

                            // Remove projectile
                            enemyProjectiles.RemoveAt(i);
                            break;
                        }
                    }
                }
                // === MEDIC CAR HEALING (Burst Heal) ===
                for (int idx = 0; idx < trainCars.Count; idx++)
                {
                    var car = trainCars[idx];
                    if (car.Type != "medic") continue;

                    var carInfo = carTypes["medic"];
                    var key = $"medic_{idx}";

                    // Check if 30 seconds have passed
                    if (!lastFire.ContainsKey(key) || (DateTime.Now - lastFire[key]).TotalMilliseconds > carInfo.FireRate)
                    {
                        lastFire[key] = DateTime.Now;

                        // Find most damaged car (prioritize low HP%)
                        var damagedCar = trainCars
                            .Where(c => c.Hp < carTypes[c.Type].MaxHp)
                            .OrderBy(c => (double)c.Hp / carTypes[c.Type].MaxHp)  // Lowest HP% first
                            .FirstOrDefault();

                        if (damagedCar != null)
                        {
                            // Get effective heal amount with upgrades
                            var effectiveHeal = carInfo.HealAmount;
                            if (weaponUpgrades.ContainsKey(car.Id))
                            {
                                effectiveHeal = (int)(effectiveHeal * weaponUpgrades[car.Id].HealAmountMultiplier);
                            }
                            int healAmount = effectiveHeal;
                            int maxHp = carTypes[damagedCar.Type].MaxHp;

                            damagedCar.Hp = Math.Min(damagedCar.Hp + healAmount, maxHp);

                            // Spawn heal effect visual
                            int carX = 50 + trainCars.IndexOf(damagedCar) * 192 + 96;
                            int carY = 660 + 32;

                            effects.Add(new Effect
                            {
                                X = carX,
                                Y = carY - 30,  // Above the car
                                TargetX = carX,
                                TargetY = carY - 60,  // Rise upward
                                Type = "heal",
                                Life = 20,
                                Width = 40,
                                Height = 40,
                                IsVisible = true
                            });
                        }
                    }
                }

                // === SHAMAN REGEN AURA (Passive Regen) ===
                var shamanCount = trainCars.Count(c => c.Type == "shaman");
                if (shamanCount > 0)
                {
                    var timeSinceRegen = (DateTime.Now - lastShamanRegen).TotalMilliseconds;
                    if (timeSinceRegen >= ShamanRegenIntervalMs)
                    {
                        lastShamanRegen = DateTime.Now;

                        double regenPerTick = shamanCount * carTypes["shaman"].RegenPerSecond;

                        foreach (var car in trainCars)
                        {
                            if (car.Hp < carTypes[car.Type].MaxHp)
                            {
                                car.Hp = Math.Min(car.Hp + (int)regenPerTick, carTypes[car.Type].MaxHp);
                            }
                        }

                        // Spawn subtle aura effect on each shaman
                        for (int idx = 0; idx < trainCars.Count; idx++)
                        {
                            if (trainCars[idx].Type != "shaman") continue;

                            int carX = 50 + idx * 192 + 96;
                            int carY = 660 + 32;

                            effects.Add(new Effect
                            {
                                X = carX,
                                Y = carY,
                                TargetX = carX,
                                TargetY = carY,
                                Type = "aura",
                                Life = 10,
                                Width = 60,
                                Height = 60,
                                IsVisible = true
                            });
                        }
                    }
                }

                // === COCAINE BEAR BUFF (Fire Rate Boost) ===
                var bearCount = trainCars.Count(c => c.Type == "cocainebear");
                if (bearCount > 0)
                {
                    var timeSinceBuff = (DateTime.Now - lastBearBuff).TotalMilliseconds;

                    // Check if it's time to activate buff
                    if (timeSinceBuff >= BearBuffIntervalMs && !isBuffActive)
                    {
                        lastBearBuff = DateTime.Now;
                        isBuffActive = true;
                        buffExpiresAt = DateTime.Now.AddMilliseconds(carTypes["cocainebear"].BuffDuration);

                        // Spawn visual effect on each cocaine bear
                        for (int idx = 0; idx < trainCars.Count; idx++)
                        {
                            if (trainCars[idx].Type != "cocainebear") continue;

                            // Mark as attacking (for animation)
                            carAttackStates[idx] = DateTime.Now;

                            int carX = 50 + idx * 192 + 96;
                            int carY = 660 + 32;

                            // Spawn buff effect visual
                            effects.Add(new Effect
                            {
                                X = carX,
                                Y = carY - 30,
                                TargetX = carX,
                                TargetY = carY - 60,
                                Type = "buffbear",
                                Life = 20,
                                Width = 40,
                                Height = 40,
                                IsVisible = true
                            });
                        }
                    }

                    // Check if buff should expire
                    if (isBuffActive && DateTime.Now >= buffExpiresAt)
                    {
                        isBuffActive = false;
                    }
                }





                // Auto-fire with new weapon systems
                for (int idx = 0; idx < trainCars.Count; idx++)
                {
                    var car = trainCars[idx];
                    if (car.Type == "caboose" || car.Type == "resource" || car.Type == "medic" || car.Type == "shaman") continue;

                    var baseCarInfo = carTypes[car.Type];

                    // GET EFFECTIVE STATS (includes upgrades)
                    var effectiveStats = GetEffectiveStats(car, baseCarInfo);

                    // Create a temporary CarInfo with effective stats for this firing cycle
                    var carInfo = new CarInfo
                    {
                        Damage = effectiveStats.damage,
                        FireRate = effectiveStats.fireRate,
                        FireRadius = effectiveStats.range,
                        FireRangeX = baseCarInfo.FireRangeX,
                        FireRangeY = baseCarInfo.FireRangeY,
                        // 🚁 ADD THESE TWO LINES:
                        ShotOffsetX = baseCarInfo.ShotOffsetX,
                        ShotOffsetY = baseCarInfo.ShotOffsetY,
                        ExplosionRadius = effectiveStats.explosion,
                        ProjectileCount = effectiveStats.projectiles,
                        ScatterCount = effectiveStats.scatter,
                        ScatterAngle = effectiveStats.scatterAngle,
                        FiringPositions = baseCarInfo.FiringPositions,
                        ProjectileWidth = baseCarInfo.ProjectileWidth,
                        ProjectileHeight = baseCarInfo.ProjectileHeight,
                        InstantEffect = baseCarInfo.InstantEffect,
                        BurstDelayMin = baseCarInfo.BurstDelayMin,  // ADD THIS
                        BurstDelayMax = baseCarInfo.BurstDelayMax,

                        // 🆕 ADD THESE TWO LINES:
                        DetectionBoxX = baseCarInfo.DetectionBoxX,
                        DetectionBoxY = baseCarInfo.DetectionBoxY,  // ADD THIS
                                                                    // 🆕 ADD THESE THREE LINES:
                        ProjectileTrajectory = baseCarInfo.ProjectileTrajectory,
                        ProjectileGravity = baseCarInfo.ProjectileGravity,
                        ProjectileSpeed = baseCarInfo.ProjectileSpeed

                    };




                    var key = $"{idx}";

                    if (!lastFire.ContainsKey(key) || (DateTime.Now - lastFire[key]).TotalMilliseconds > carInfo.FireRate)
                    {
                        int carX = 50 + idx * 192 + 96;
                        int carY = 660 + 32;

                        // Special handling for engine smoke
                        if (car.Type == "engine")
                        {
                            lastFire[key] = DateTime.Now;
                            effects.Add(new Effect
                            {
                                X = carX,
                                Y = carY,
                                TargetX = carX + carInfo.FireRangeX,
                                TargetY = carY + carInfo.FireRangeY,
                                Type = car.Type,
                                Life = 30,
                                Width = carInfo.ProjectileWidth,
                                Height = carInfo.ProjectileHeight
                            });
                            continue;
                        }

                        // Determine firing positions
                        var firingPositions = carInfo.FiringPositions.Count > 0
                            ? carInfo.FiringPositions
                            : new List<(int x, int y)> { (0, 0) };

                        // Fire from each hardpoint
                        foreach (var (offsetX, offsetY) in firingPositions)
                        {
                            int firingX = carX + offsetX;
                            int firingY = carY + offsetY;

                            // 🚁 NEW: Apply weapon-specific shot offset
                            firingX += carInfo.ShotOffsetX;
                            firingY += carInfo.ShotOffsetY;

                            int targetX = firingX + carInfo.FireRangeX;
                            int targetY = firingY + carInfo.FireRangeY;
                            Enemy? target = null;

                            // 🆕 CHECK IF WEAPON USES RECTANGULAR DETECTION
                            if (carInfo.DetectionBoxX.HasValue && carInfo.DetectionBoxY.HasValue)
                            {
                                // RECTANGULAR DETECTION (for AA Flak, etc.)
                                double halfWidth = carInfo.DetectionBoxX.Value / 2.0;
                                double halfHeight = carInfo.DetectionBoxY.Value / 2.0;

                                target = enemies
                                    .Where(e => e.Hp > 0 &&
                                                Math.Abs(e.X - targetX) <= halfWidth &&
                                                Math.Abs(e.Y - targetY) <= halfHeight)
                                    .OrderBy(e => Math.Abs(e.Y - targetY))  // Prioritize closest Y (vertical distance)
                                    .FirstOrDefault();
                            }
                            else
                            {
                                // CIRCULAR DETECTION (all other weapons)
                                target = enemies
                                    .Where(e => e.Hp > 0 &&
                                                Math.Sqrt(Math.Pow(e.X - targetX, 2) + Math.Pow(e.Y - targetY, 2)) <= carInfo.FireRadius)
                                    .OrderBy(e => Math.Sqrt(Math.Pow(e.X - targetX, 2) + Math.Pow(e.Y - targetY, 2)))
                                    .FirstOrDefault();
                            }

                            if (target != null)
                            {
                                lastFire[key] = DateTime.Now;
                                carAttackStates[idx] = DateTime.Now;

                                // NEW: Check if this is an instant effect weapon (like flak)
                                if (carInfo.InstantEffect)
                                {
                                    // INSTANT EFFECT: Spawn effects directly at detonation points
                                    if (carInfo.ScatterCount > 1)
                                    {
                                        // Multiple burst clouds in a pattern
                                        double angleStep = carInfo.ScatterAngle / (carInfo.ScatterCount - 1);
                                        double startAngle = -carInfo.ScatterAngle / 2;

                                        for (int s = 0; s < carInfo.ScatterCount; s++)
                                        {
                                            double angle = startAngle + (s * angleStep);
                                            double radians = angle * Math.PI / 180;

                                            // NEW: Add randomness to burst positions for more natural scatter
                                            double randomOffset = random.Next(-30, 30);
                                            double randomAngleOffset = (random.NextDouble() - 0.5) * 20; // ±10 degrees
                                            double adjustedAngle = radians + (randomAngleOffset * Math.PI / 180);

                                            // Calculate burst position offset from target
                                            double burstDistance = carInfo.ExplosionRadius * 0.5 + randomOffset;
                                            double burstX = target.X + Math.Cos(adjustedAngle) * burstDistance;
                                            double burstY = target.Y + Math.Sin(adjustedAngle) * burstDistance;

                                            // NEW: Stagger spawn delays using weapon-specific timing
                                            int delayFrames = s * random.Next(carInfo.BurstDelayMin, carInfo.BurstDelayMax);

                                            // Spawn burst effect at calculated position
                                            effects.Add(new Effect
                                            {
                                                X = burstX,
                                                Y = burstY,
                                                TargetX = burstX, // No movement
                                                TargetY = burstY,
                                                Type = car.Type,
                                                Life = 20, // Burst clouds linger briefly
                                                Width = carInfo.ProjectileWidth,
                                                Height = carInfo.ProjectileHeight,
                                                SpawnDelay = delayFrames,  // NEW: Delayed appearance
                                                IsVisible = delayFrames == 0  // NEW: First one is visible immediately
                                            });
                                        }
                                    }
                                    else
                                    {
                                        // Single instant burst at target location
                                        effects.Add(new Effect
                                        {
                                            X = target.X,
                                            Y = target.Y,
                                            TargetX = target.X,
                                            TargetY = target.Y,
                                            Type = car.Type,
                                            Life = 20,
                                            Width = carInfo.ProjectileWidth,
                                            Height = carInfo.ProjectileHeight,
                                            SpawnDelay = 0,
                                            IsVisible = true
                                        });
                                    }
                                }


                                else
                                {
                                    // NORMAL PROJECTILE: Travels from gun to target
                                    // Handle scatter projectiles
                                    if (carInfo.ScatterCount > 1)
                                    {
                                        double angleStep = carInfo.ScatterAngle / (carInfo.ScatterCount - 1);
                                        double startAngle = -carInfo.ScatterAngle / 2;

                                        for (int s = 0; s < carInfo.ScatterCount; s++)
                                        {
                                            double angle = startAngle + (s * angleStep);
                                            double radians = angle * Math.PI / 180;

                                            double distance = Math.Sqrt(Math.Pow(target.X - firingX, 2) + Math.Pow(target.Y - firingY, 2));
                                            double baseAngle = Math.Atan2(target.Y - firingY, target.X - firingX);
                                            double scatterAngle = baseAngle + radians;

                                            double scatterTargetX = firingX + Math.Cos(scatterAngle) * distance;
                                            double scatterTargetY = firingY + Math.Sin(scatterAngle) * distance;

                                            effects.Add(new Effect
                                            {
                                                X = firingX,
                                                Y = firingY,
                                                TargetX = scatterTargetX,
                                                TargetY = scatterTargetY,
                                                Type = car.Type,
                                                Life = 30,
                                                Width = carInfo.ProjectileWidth,
                                                Height = carInfo.ProjectileHeight
                                            });
                                        }
                                    }
                                    // Handle multiple projectiles (burst fire)
                                    else if (carInfo.ProjectileCount > 1)
                                    {
                                        for (int p = 0; p < carInfo.ProjectileCount; p++)
                                        {
                                            // Small random spread for multiple projectiles
                                            double spreadX = random.Next(-20, 20);
                                            double spreadY = random.Next(-20, 20);

                                            effects.Add(new Effect
                                            {
                                                X = firingX,
                                                Y = firingY,
                                                TargetX = target.X + spreadX,
                                                TargetY = target.Y + spreadY,
                                                Type = car.Type,
                                                Life = 30,
                                                Width = carInfo.ProjectileWidth,
                                                Height = carInfo.ProjectileHeight
                                            });
                                        }
                                    }
                                    // Standard single projectile
                                    else
                                    {
                                        // CALCULATE INITIAL VELOCITY FOR BALLISTIC PROJECTILES
                                        double velocityX = 0;
                                        double velocityY = 0;
                                        double rotationAngle = 0;

                                        if (carInfo.ProjectileTrajectory == TrajectoryType.Ballistic)
                                        {
                                            // 🆕 PROPER BALLISTIC ARC CALCULATION
                                            double dx = target.X - firingX;
                                            double dy = target.Y - firingY;
                                            double gravity = carInfo.ProjectileGravity;

                                            // Calculate time to reach target (using projectile motion formula)
                                            // Approximation: assume medium arc trajectory
                                            double timeToTarget = Math.Sqrt(2 * Math.Abs(dy) / gravity + Math.Pow(dx / carInfo.ProjectileSpeed, 2));

                                            // If timeToTarget is too small or invalid, use direct calculation
                                            if (timeToTarget < 0.1 || double.IsNaN(timeToTarget))
                                            {
                                                timeToTarget = Math.Abs(dx) / carInfo.ProjectileSpeed;
                                            }

                                            // Calculate required velocities to hit target
                                            velocityX = dx / timeToTarget;
                                            velocityY = (dy / timeToTarget) - (0.5 * gravity * timeToTarget);

                                            // CALCULATE ROTATION ANGLE based on initial velocity
                                            rotationAngle = Math.Atan2(velocityY, velocityX) * (180.0 / Math.PI);
                                        }
                                        else
                                        {
                                            // 🆕 CALCULATE ROTATION FOR LINEAR PROJECTILES TOO
                                            double dx = target.X - firingX;
                                            double dy = target.Y - firingY;
                                            rotationAngle = Math.Atan2(dy, dx) * (180.0 / Math.PI);
                                        }
                                        // Create the effect/projectile
                                        var newEffect = new Effect
                                        {
                                            X = firingX,
                                            Y = firingY,
                                            TargetX = target.X,
                                            TargetY = target.Y,
                                            Type = car.Type,
                                            Life = 30,  // Default for most projectiles
                                            Width = carInfo.ProjectileWidth,
                                            Height = carInfo.ProjectileHeight,

                                            Trajectory = carInfo.ProjectileTrajectory,
                                            VelocityX = velocityX,
                                            VelocityY = velocityY,
                                            Gravity = carInfo.ProjectileGravity,
                                            RotationAngle = rotationAngle
                                        };

                                        // 🚁 SPECIAL HANDLING FOR DRONES
                                        if (car.Type == "drone")
                                        {
                                            newEffect.Life = 200;  // 10 seconds (200 ticks × 50ms)
                                            newEffect.Hp = 10;
                                            newEffect.MaxHp = 10;
                                            newEffect.LastDroneShot = DateTime.Now;
                                        }

                                        effects.Add(newEffect);
                                    }
                                }

                                // Apply damage with explosion radius
                                if (carInfo.ExplosionRadius > 0)
                                {
                                    // Area damage - hit all enemies in explosion radius
                                    var enemiesInRadius = enemies
                                        .Where(e => e.Hp > 0 &&
                                                    Math.Sqrt(Math.Pow(e.X - target.X, 2) + Math.Pow(e.Y - target.Y, 2)) <= carInfo.ExplosionRadius)
                                        .ToList();


                                    foreach (var enemy in enemiesInRadius)
                                    {
                                       

                                        // To this:
                                        enemy.Hp -= carInfo.Damage;
                                        if (enemy.Hp <= 0 && !enemy.MarkedForDeath)
                                        {
                                            enemy.MarkedForDeath = true;
                                            enemy.AnimState = "death";
                                            enemy.StateChangeTime = DateTime.Now;

                                            var enemyInfo = enemyTypes[enemy.Type];
                                            score += enemyInfo.Points;
                                            xp += enemyInfo.Xp;
                                            totalKills++;
                                        }

                                    }
                                }
                                else
                                {
                                    // Single target damage
                                    target.Hp -= carInfo.Damage;
                                    if (target.Hp <= 0)
                                    {
                                        var enemyInfo = enemyTypes[target.Type];
                                        score += enemyInfo.Points;
                                        xp += enemyInfo.Xp;
                                        totalKills++;
                                    }
                                }
                            }
                        }
                    }
                }

                // Check level up
                if (xp >= xpNeeded)
                {
                    gameState = GameState.LevelUp;
                    gameTimer?.Stop();
                    GenerateUpgradeChoices();

                    // DOUBLE CHOICE: Every 5 levels (boss levels), generate second set
                    if (level % 5 == 0)
                    {
                        upgradeChoicesSecond.Clear();
                        // Generate a second set of 3-5 choices
                        var secondTypes = new List<string> { "newCar", "speed", "damage", "firerate", "firerange" }
                            .OrderBy(x => random.Next()).Take(random.Next(3, 6)).ToList();

                        foreach (var type in secondTypes)
                        {
                            if (type == "newCar" && trainCars.Count < MaxCars)
                            {
                                var allCars = carTypes.Keys.Where(k => k != "engine" && k != "caboose").ToList();
                                if (allCars.Count > 0)
                                {
                                    var car = allCars[random.Next(allCars.Count)];
                                    upgradeChoicesSecond.Add(new UpgradeChoice
                                    {
                                        Type = "newCar",
                                        Car = car,
                                        Description = "Add new car to train"
                                    });
                                }
                            }
                            else if (type == "speed")
                            {
                                upgradeChoicesSecond.Add(new UpgradeChoice { Type = "speed", Description = "Increase train speed +20%" });
                            }
                            else if (type == "damage")
                            {
                                upgradeChoicesSecond.Add(new UpgradeChoice { Type = "stat", StatType = "damage", Description = "Increase all weapon damage +25%" });
                            }
                            else if (type == "firerate")
                            {
                                upgradeChoicesSecond.Add(new UpgradeChoice { Type = "stat", StatType = "firerate", Description = "Increase all fire rates +20%" });
                            }
                            else if (type == "firerange")
                            {
                                upgradeChoicesSecond.Add(new UpgradeChoice { Type = "stat", StatType = "firerange", Description = "Increase all weapon ranges +30%" });
                            }
                        }
                    }
                }

                // ADD THIS LINE:
                UpdateRotatingWeapons();
                visibleEnemyCount = enemies.Count(e => IsEnemyVisible(e));
                culledEnemyCount = enemies.Count - visibleEnemyCount;
                // ADD THIS: Update weather
                weatherCycle.Update(0.05); // 50ms delta time
                lightCycle.Update();


                // At the end of GameTick, before UpdateAnimations():

                // Cleanup old dead enemies (keep list from growing)
                enemies.RemoveAll(e => e.MarkedForDeath &&
                    (DateTime.Now - e.StateChangeTime).TotalMilliseconds > 600);

                // Cleanup off-screen enemies
                enemies.RemoveAll(e => e.X < -200 && !e.MarkedForDeath);

                // Cleanup old effects
                effects.RemoveAll(e => e.Life <= 0);

                // Cleanup off-screen enemy projectiles
                enemyProjectiles.RemoveAll(p => p.X < -200 || p.X > 3400);

                // In GameTick, after enemy cleanup:
                cleanupCounter++;
                if (cleanupCounter > 300) // Every ~5 seconds at 60 FPS
                {
                    cleanupCounter = 0;

                    // Clean up lastEnemyShot for dead enemies
                    var deadEnemyIds = enemies.Where(e => e.MarkedForDeath).Select(e => e.Id).ToHashSet();
                    foreach (var id in lastEnemyShot.Keys.ToList())
                    {
                        if (deadEnemyIds.Contains(id))
                            lastEnemyShot.Remove(id);
                    }

                    // Clean up old attack states
                    var now24 = DateTime.Now;
                    foreach (var key in carAttackStates.Keys.ToList())
                    {
                        if ((now24 - carAttackStates[key]).TotalSeconds > 5)
                            carAttackStates.Remove(key);
                    }
                }
                // In the cleanup block above:
                // Remove animations for non-existent enemies
                var activeEnemyTypes = enemies.Select(e => e.Type).Distinct().ToHashSet();
                foreach (var key in animations.Keys.ToList())
                {
                    if (key.StartsWith("enemy_"))
                    {
                        var enemyType = key.Split('_')[1];
                        if (!activeEnemyTypes.Contains(enemyType))
                        {
                            // Don't remove, just let it stay (reusable)
                            // Actually, animations should be fine - they're reused
                        }
                    }
                }
                // In the cleanup block:
                foreach (var idx in carRotationAngles.Keys.ToList())
                {
                    if (idx >= trainCars.Count)
                        carRotationAngles.Remove(idx);
                }
                // In the cleanup block:
                foreach (var idx in carRotationAngles.Keys.ToList())
                {
                    if (idx >= trainCars.Count)
                        carRotationAngles.Remove(idx);
                }
                // === PERIODIC CLEANUP TO PREVENT MEMORY LEAKS ===
                enemies.RemoveAll(e => e.MarkedForDeath &&
                    (DateTime.Now - e.StateChangeTime).TotalMilliseconds > 600);
                enemies.RemoveAll(e => e.X < -200 && !e.MarkedForDeath);
                effects.RemoveAll(e => e.Life <= 0);
                enemyProjectiles.RemoveAll(p => p.X < -200 || p.X > 3400);

                cleanupCounter++;
                if (cleanupCounter > 300) // Every ~5 seconds
                {
                    cleanupCounter = 0;

                    var deadEnemyIds = enemies.Where(e => e.MarkedForDeath).Select(e => e.Id).ToHashSet();
                    foreach (var id in lastEnemyShot.Keys.ToList())
                        if (deadEnemyIds.Contains(id)) lastEnemyShot.Remove(id);

                    var now35 = DateTime.Now;
                    foreach (var key in carAttackStates.Keys.ToList())
                        if ((now35 - carAttackStates[key]).TotalSeconds > 5) carAttackStates.Remove(key);

                    var activeCarIds = trainCars.Select(c => c.Id).ToHashSet();
                    foreach (var id in weaponUpgrades.Keys.ToList())
                        if (!activeCarIds.Contains(id)) weaponUpgrades.Remove(id);

                    foreach (var idx in carRotationAngles.Keys.ToList())
                        if (idx >= trainCars.Count) carRotationAngles.Remove(idx);
                }


                UpdateAnimations();


                StateHasChanged();
            });
        }
        private Queue<Action> renderQueue = new Queue<Action>();
        private bool isProcessingRenderQueue = false;
        // Rendering optimization
        private bool isRenderPending = false;
        private readonly object renderLock = new object();
        private int cleanupCounter = 0;
        private void QueueRender(Action updateAction = null)
        {
            if (updateAction != null)
                renderQueue.Enqueue(updateAction);

            if (!isProcessingRenderQueue)
            {
                isProcessingRenderQueue = true;
                InvokeAsync(async () =>
                {
                    while (renderQueue.Count > 0)
                    {
                        renderQueue.Dequeue()?.Invoke();
                    }
                    StateHasChanged();
                    await Task.Delay(16); // Max 60 FPS
                    isProcessingRenderQueue = false;
                });
            }
        }

        private void UpdateRotatingWeapons()
        {
            // Update rotation angles for all china carts
            for (int idx = 0; idx < trainCars.Count; idx++)
            {
                var car = trainCars[idx];
                if (car.Type != "china") continue;

                var carInfo = carTypes[car.Type];

                // Initialize rotation if doesn't exist
                if (!carRotationAngles.ContainsKey(idx))
                {
                    carRotationAngles[idx] = 0;
                }

                // Increment rotation angle (adjust speed here: higher = faster rotation)
                carRotationAngles[idx] += 2.0; // 2 degrees per frame
                if (carRotationAngles[idx] >= 360)
                {
                    carRotationAngles[idx] -= 360;
                }

                // Calculate car center position
                int carX = 50 + idx * 192 + 96;  // Car center X
                int carY = 660 + 32;              // Car center Y

                double currentAngle = carRotationAngles[idx];
                double angleRadians = currentAngle * Math.PI / 180.0;

                // Calculate fireball positions along the rotating line
                int fireballCount = carInfo.ProjectileCount;
                double maxDistance = carInfo.ScatterCount;  // Max distance from car

                for (int f = 0; f < fireballCount; f++)
                {
                    // Distance from car center (evenly spaced along the line)
                    double distanceFromCenter = (maxDistance / (fireballCount - 1)) * f;

                    // Calculate position using rotation angle
                    double fireballX = carX + Math.Cos(angleRadians) * distanceFromCenter;
                    double fireballY = carY + Math.Sin(angleRadians) * distanceFromCenter;

                    // Check collision with enemies
                    var hitEnemies = enemies.Where(e =>
                        e.Hp > 0 &&
                        Math.Sqrt(Math.Pow(e.X - fireballX, 2) + Math.Pow(e.Y - fireballY, 2)) <= (carInfo.ProjectileWidth / 2)
                    ).ToList();

                    // Damage enemies hit by this fireball
                    foreach (var enemy in hitEnemies)
                    {
                        enemy.Hp -= carInfo.Damage * 0.5f;  // Reduced damage since it hits rapidly

                        if (enemy.Hp <= 0 && !enemy.MarkedForDeath)
                        {
                            enemy.MarkedForDeath = true;
                            enemy.AnimState = "death";
                            enemy.StateChangeTime = DateTime.Now;

                            var enemyInfo = enemyTypes[enemy.Type];
                            score += enemyInfo.Points;
                            xp += enemyInfo.Xp;
                            totalKills++;
                        }
                    }
                }
            }
        }
      
        private void GenerateUpgradeChoices()
        {
            upgradeChoices.Clear();
            var random = new Random();

            // Get list of weapon types currently in the train (exclude engine, caboose, resource)
            var ownedWeapons = trainCars
                .Where(c => c.Type != "engine" && c.Type != "caboose" && c.Type != "resource")
                .Select(c => c.Type)
                .Distinct()
                .ToList();

            // Build weighted upgrade pool
            var availableUpgrades = new List<(string type, string subtype, string weapon, double weight)>();

            // GLOBAL UPGRADES (lower weight, weaker bonuses)
            availableUpgrades.Add(("speed", "", "", 1.0));
            availableUpgrades.Add(("globalStat", "damage", "", 0.8));
            availableUpgrades.Add(("globalStat", "firerate", "", 0.8));
            availableUpgrades.Add(("globalStat", "firerange", "", 0.8));

            // ENGINE & DURABILITY UPGRADES - ADD THESE NEW LINES
            availableUpgrades.Add(("engineHp", "", "", 1.2));
            availableUpgrades.Add(("allCarsHp", "", "", 1.0));
            availableUpgrades.Add(("engineRegen", "", "", 0.9));

            // NEW CAR (only if not at max) - INCREASED WEIGHT FROM 1.5 to 5.0
            if (trainCars.Count < MaxCars)
            {
                availableUpgrades.Add(("newCar", "", "", 20.0));
            }
            // Special healing upgrades (only if you own healing cars)
            if (ownedWeapons.Contains("medic"))
            {
                availableUpgrades.Add(("weaponStat", "healAmount", "medic", 1.5));
                availableUpgrades.Add(("weaponStat", "healCooldown", "medic", 1.2));
            }

            if (ownedWeapons.Contains("shaman"))
            {
                availableUpgrades.Add(("weaponStat", "regenRate", "shaman", 1.5));
            }
            // WEAPON-SPECIFIC UPGRADES (higher weight, stronger bonuses)
            foreach (var weaponType in ownedWeapons)
            {
                var carInfo = carTypes[weaponType];

                // Basic stats (all weapons)
                availableUpgrades.Add(("weaponStat", "damage", weaponType, 2.0));
                availableUpgrades.Add(("weaponStat", "firerate", weaponType, 2.0));
                availableUpgrades.Add(("weaponStat", "firerange", weaponType, 2.0));

                // Explosion radius (only for weapons with explosions)
                if (carInfo.ExplosionRadius > 0)
                {
                    availableUpgrades.Add(("weaponStat", "explosion", weaponType, 1.5));
                }

                // Projectile count (only for burst weapons)
                if (carInfo.ProjectileCount > 1)
                {
                    availableUpgrades.Add(("weaponStat", "projectiles", weaponType, 1.2));
                }

                // Scatter upgrades (only for scatter weapons)
                if (carInfo.ScatterCount > 1)
                {
                    availableUpgrades.Add(("weaponStat", "scatterCount", weaponType, 1.2));
                    availableUpgrades.Add(("weaponStat", "scatterAngle", weaponType, 1.0));
                }
            }

            // Randomly select 3-5 upgrades based on weights
            var selectedCount = random.Next(3, 6);
            var totalWeight = availableUpgrades.Sum(u => u.weight);

            for (int i = 0; i < selectedCount && availableUpgrades.Count > 0; i++)
            {
                // Weighted random selection
                double roll = random.NextDouble() * totalWeight;
                double cumulative = 0;

                (string type, string subtype, string weapon, double weight) selected = availableUpgrades[0];

                foreach (var upgrade in availableUpgrades)
                {
                    cumulative += upgrade.weight;
                    if (roll <= cumulative)
                    {
                        selected = upgrade;
                        break;
                    }
                }

                // Create upgrade choice
                if (selected.type == "newCar")
                {
                    var allCars = carTypes.Keys.Where(k => k != "engine" && k != "caboose").ToList();
                    if (allCars.Count > 0)
                    {
                        var car = allCars[random.Next(allCars.Count)];
                        upgradeChoices.Add(new UpgradeChoice
                        {
                            Type = "newCar",
                            Car = car,
                            Description = $"Add {carTypes[car].Name} to train",
                            IsGlobal = false
                        });
                    }
                }
                else if (selected.type == "speed")
                {
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "speed",
                        Description = "Train Speed +20%",
                        IsGlobal = true
                    });
                }
                else if (selected.type == "globalStat")
                {
                    var desc = selected.subtype switch
                    {
                        "damage" => "All Weapons: Damage +15%",
                        "firerate" => "All Weapons: Fire Rate +15%",
                        "firerange" => "All Weapons: Range +10%",
                        _ => ""
                    };
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "stat",
                        StatType = selected.subtype,
                        Description = desc,
                        IsGlobal = true
                    });
                }
                else if (selected.type == "weaponStat")
                {
                    var weaponName = carTypes[selected.weapon].Name;
                    var desc = selected.subtype switch
                    {
                        "healAmount" => $"{weaponName}: Heal Amount +50%",
                        "healCooldown" => $"{weaponName}: Heal Cooldown -20%",
                        "regenRate" => $"{weaponName}: Regen Rate +50%",
                        "damage" => $"{weaponName}: Damage +35%",
                        "firerate" => $"{weaponName}: Fire Rate +30%",
                        "firerange" => $"{weaponName}: Range +10%",
                        "explosion" => $"{weaponName}: Explosion Radius +50%",
                        "projectiles" => $"{weaponName}: +1 Projectile",
                        "scatterCount" => $"{weaponName}: +1 Scatter Shot",
                        "scatterAngle" => $"{weaponName}: Scatter Spread +30%",
                        _ => ""
                    };
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "weaponStat",
                        StatType = selected.subtype,
                        TargetWeaponType = selected.weapon,
                        Description = desc,
                        IsGlobal = false
                    });
                }
                else if (selected.type == "speed")
                {
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "speed",
                        Description = "Train Speed +20%",
                        IsGlobal = true
                    });
                }
                // ADD THESE THREE NEW CASES:
                else if (selected.type == "engineHp")
                {
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "engineHp",
                        Description = "Engine Max HP +50",
                        IsGlobal = false
                    });
                }
                else if (selected.type == "allCarsHp")
                {
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "allCarsHp",
                        Description = "All Cars: Max HP +10",
                        IsGlobal = true
                    });
                }
                else if (selected.type == "engineRegen")
                {
                    upgradeChoices.Add(new UpgradeChoice
                    {
                        Type = "engineRegen",
                        Description = "Engine Regen +1 HP/2s",
                        IsGlobal = false
                    });
                }
                // Remove selected upgrade and adjust total weight
                availableUpgrades.Remove(selected);
                totalWeight -= selected.weight;
            }
        }
        private void HandleCarDragStart(int index)
        {
            draggedCarIndex = index;
        }

        private void HandleCarDragOver(int index)
        {
            dropTargetIndex = index;
        }

        private void HandleCarDrop(int index)
        {
            if (draggedCarIndex.HasValue && draggedCarIndex.Value != index)
            {
                var car = trainCars[draggedCarIndex.Value];
                trainCars.RemoveAt(draggedCarIndex.Value);
                trainCars.Insert(index, car);
            }
            draggedCarIndex = null;
            dropTargetIndex = null;
        }

        private void HandleCarDragEnd()
        {
            draggedCarIndex = null;
            dropTargetIndex = null;
        }
        private void ToggleDiscardMode()
        {
            isDiscardMode = !isDiscardMode;
        }

        private void HandleCarClick(int index)
        {
            if (!isDiscardMode) return;

            var car = trainCars[index];

            // Protect engine and caboose from being discarded
            if (car.Type == "engine" || car.Type == "caboose")
            {
                return; // Can't discard these!
            }

            // Remove the car
            trainCars.RemoveAt(index);

            // Auto-exit discard mode after removing a car
            isDiscardMode = false;
        }
        private void HandleUpgrade(UpgradeChoice choice)
        {
            if (choice.Type == "newCar")
            {
                // Insert a new TrainCar instance before the caboose
                trainCars.Insert(
                    trainCars.Count - 1,
                    new TrainCar
                    {
                        Type = choice.Car,
                        Hp = carTypes[choice.Car].MaxHp
                    }
                );
            }
            else if (choice.Type == "speed")
            {
                baseGameSpeed *= 1.2;
            }
            else if (choice.Type == "stat")
            {
                // GLOBAL upgrade - weaker bonuses (15-20% instead of 25-30%)
                foreach (var carKey in carTypes.Keys.Where(k => k != "engine" && k != "caboose" && k != "resource"))
                {
                    if (choice.StatType == "damage")
                    {
                        carTypes[carKey].Damage *= 1.5f;
                    }
                    else if (choice.StatType == "firerate")
                    {
                        carTypes[carKey].FireRate = (int)(carTypes[carKey].FireRate * 0.85); // 15% faster
                    }
                    else if (choice.StatType == "firerange")
                    {
                        carTypes[carKey].FireRadius = (int)(carTypes[carKey].FireRadius * 0.6);
                    }
                }
            }
            else if (choice.Type == "weaponStat")
            {
                // WEAPON-SPECIFIC upgrade - stronger bonuses (30-50%)
                // Apply to all cars of this type
                var targetCars = trainCars.Where(c => c.Type == choice.TargetWeaponType).ToList();

                foreach (var car in targetCars)
                {
                    // Initialize upgrade state if doesn't exist
                    if (!weaponUpgrades.ContainsKey(car.Id))
                    {
                        weaponUpgrades[car.Id] = new WeaponUpgradeState();
                    }

                    var upgradeState = weaponUpgrades[car.Id];

                    switch (choice.StatType)
                    {
                        case "damage":
                            upgradeState.DamageMultiplier *= 2.0; // 35% stronger
                            break;
                        case "firerate":
                            upgradeState.FireRateMultiplier *= 0.70; // 30% faster
                            break;
                        case "firerange":
                            upgradeState.RangeMultiplier *= 0.6; // 40% more range
                            break;
                        case "explosion":
                            upgradeState.ExplosionRadiusMultiplier *= 1.5; // 50% bigger boom
                            break;
                        case "projectiles":
                            upgradeState.BonusProjectileCount += 1;
                            break;
                        case "scatterCount":
                            upgradeState.BonusScatterCount += 1;
                            break;
                        case "scatterAngle":
                            upgradeState.ScatterAngleMultiplier *= 1.3; // 30% wider spread
                            break;
                        case "healAmount":
                            upgradeState.HealAmountMultiplier *= 1.5; // +50% heal
                            break;
                        case "healCooldown":
                            upgradeState.HealCooldownMultiplier *= 0.8; // 20% faster
                            break;
                        case "regenRate":
                            upgradeState.RegenRateMultiplier *= 1.5; // +50% regen
                            break;
                    }
                }
            }
            else if (choice.Type == "speed")
            {
                baseGameSpeed *= 1.2;
            }
            // ADD THESE THREE NEW CASES:
            else if (choice.Type == "engineHp")
            {
                carTypes["engine"].MaxHp += 50;
                // Also heal the engine by the bonus amount
                var engineCar = trainCars.FirstOrDefault(c => c.Type == "engine");
                if (engineCar != null)
                {
                    engineCar.Hp += 50;
                }
            }
            else if (choice.Type == "allCarsHp")
            {
                // Increase max HP for all car types
                foreach (var carKey in carTypes.Keys)
                {
                    carTypes[carKey].MaxHp += 10;
                }
                // Heal all current cars
                foreach (var car in trainCars)
                {
                    car.Hp += 10;
                }
            }
            else if (choice.Type == "engineRegen")
            {
                // We'll track this with a new field - add this at the top of your class later
                EngineRegenAmount += 1;
            }
            // CHECK IF THIS IS A BOSS LEVEL (every 5) AND WE STILL HAVE SECOND PANEL
            if (level % 5 == 0 && upgradeChoicesSecond.Count > 0 && gameState == GameState.LevelUp)
            {
                // Move to second upgrade panel
                gameState = GameState.LevelUpSecond;
                return; // Don't continue to level-up logic yet
            }

            // NORMAL LEVEL UP CONTINUATION
            level++;
            xp = 0;
            xpNeeded = (int)(100 * Math.Pow(level, 1.5));

            // SPAWN BOSS IF THIS LEVEL HAS ONE
            if (bossSpawns.ContainsKey(level))
            {
                string bossEnemyType = bossSpawns[level];
                var bossType = enemyTypes[bossEnemyType];
                int hpMultiplier = 10 + (level / 5);

                enemies.Add(new Enemy
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = bossEnemyType,
                    X = 1200,
                    Y = bossType.SpawnYMin + 50,
                    Hp = bossType.Hp * hpMultiplier,
                    MaxHp = bossType.Hp * hpMultiplier,
                    Speed = bossType.Speed * 0.5
                });
            }

            upgradeChoicesSecond.Clear();
            gameState = GameState.Playing;
            gameTimer?.Start();
        }
        private double GetSpawnWeightForEnemy(string enemyType, int currentLevel)
        {
            if (!enemyTypes.ContainsKey(enemyType))
                return 0;

            var enemyInfo = enemyTypes[enemyType];

            // Enemy not available yet
            if (currentLevel < enemyInfo.MinLevel)
                return 0;

            // No weights defined = always spawn at weight 1.0
            if (enemyInfo.LevelSpawnWeights.Count == 0)
                return 0; // Bosses return 0 (manual spawn only)

            // Find the appropriate weight for current level
            double weight = 1.0;
            int lastMilestone = enemyInfo.MinLevel;

            foreach (var kvp in enemyInfo.LevelSpawnWeights.OrderBy(x => x.Key))
            {
                if (currentLevel >= kvp.Key)
                {
                    weight = kvp.Value;
                    lastMilestone = kvp.Key;
                }
                else
                {
                    break;
                }
            }

            return weight;
        }



        private (float damage, int fireRate, int range, int explosion, int projectiles, int scatter, double scatterAngle) GetEffectiveStats(TrainCar car, CarInfo baseStats)
        {
            // Start with base stats
            float damage = baseStats.Damage;
            int fireRate = baseStats.FireRate;
            int range = baseStats.FireRadius;
            int explosion = baseStats.ExplosionRadius;
            int projectiles = baseStats.ProjectileCount;
            int scatter = baseStats.ScatterCount;
            double scatterAngle = baseStats.ScatterAngle;

            // Apply weapon-specific upgrades
            if (weaponUpgrades.ContainsKey(car.Id))
            {
                var upgrades = weaponUpgrades[car.Id];
                damage *= (float)upgrades.DamageMultiplier;
                fireRate = (int)(fireRate * upgrades.FireRateMultiplier);
                range = (int)(range * upgrades.RangeMultiplier);
                explosion = (int)(explosion * upgrades.ExplosionRadiusMultiplier);
                projectiles += upgrades.BonusProjectileCount;
                scatter += upgrades.BonusScatterCount;
                scatterAngle *= upgrades.ScatterAngleMultiplier;
            }

            // 🐻 COCAINE BEAR BUFF: Apply fire rate boost if active
            if (isBuffActive)
            {
                fireRate = (int)(fireRate * carTypes["cocainebear"].FireRateBoostMultiplier);
            }

            return (damage, fireRate, range, explosion, projectiles, scatter, scatterAngle);
        }
        private string GetEnvironmentColor()
        {
            return environment switch
            {
                0 => "#2d5016",
                1 => "#d4a574",
                2 => "#e8f4f8",
                _ => "#2d5016"
            };
        }
        private string GetEnvironmentImageKey()
        {
            return environment switch
            {
                0 => "bgForest",
                1 => "bgDesert",
                2 => "bgSnow",
                _ => "bgForest"
            };
        }

        private string GetRailsImageKey()
        {
            return environment switch
            {
                0 => "railsForest",
                1 => "railsDesert",
                2 => "railsSnow",
                _ => "railsForest"
            };
        }

        private string GetTiesImageKey()
        {
            return environment switch
            {
                0 => "tiesForest",
                1 => "tiesDesert",
                2 => "tiesSnow",
                _ => "tiesForest"
            };
        }

        private string GetGroundImageKey()
        {
            return environment switch
            {
                0 => "groundForest",
                1 => "groundDesert",
                2 => "groundSnow",
                _ => "groundForest"
            };
        }
        private string GetBallastImageKey()
        {
            return environment switch
            {
                0 => "ballastForest",
                1 => "ballastDesert",
                2 => "ballastSnow",
                _ => "ballastForest"
            };
        }
        private string GetFoliageLayer1ImageKey()
        {
            return environment switch
            {
                0 => "foliageForestLayer1",
                1 => "foliageDesertLayer1",
                2 => "foliageSnowLayer1",
                _ => "foliageForestLayer1"
            };
        }

        private string GetFoliageLayer2ImageKey()
        {
            return environment switch
            {
                0 => "foliageForestLayer2",
                1 => "foliageDesertLayer2",
                2 => "foliageSnowLayer2",
                _ => "foliageForestLayer2"
            };
        }

        private string GetFoliageLayer3ImageKey()
        {
            return environment switch
            {
                0 => "foliageForestLayer3",
                1 => "foliageDesertLayer3",
                2 => "foliageSnowLayer3",
                _ => "foliageForestLayer3"
            };
        }
        // Add this helper method:
        private string GetEnemyAnimationKey(Enemy enemy)
        {
            return $"enemy_{enemy.Type}_{enemy.AnimState}";
        }
        private string GetCarColor(string car)
        {
            return carTypes.ContainsKey(car) ? carTypes[car].Color : "#555";
        }
        private void HandleMouseDown(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
        {
            if (e.Button == 2) // Right mouse button
            {
                isDragging = true;
                dragStartX = e.ClientX;
                initialViewportOffset = viewportOffsetX;
            }
        }

        private void HandleMouseMove(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
        {
            if (isDragging)
            {
                double deltaX = e.ClientX - dragStartX;
                viewportOffsetX = initialViewportOffset + deltaX;

                // Limit scrolling range - world is 3200 wide, viewport is 1600
                double maxOffset = 0;
                double minOffset = -(3200 - 1600); // Can scroll 1600 pixels to the left
                viewportOffsetX = Math.Max(minOffset, Math.Min(maxOffset, viewportOffsetX));
            }
        }

        private void HandleMouseUp(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
        {
            isDragging = false;
        }

        // Culling
        private bool IsEffectVisible(Effect effect)
        {
            double effectScreenX = effect.X + viewportOffsetX;
            const double buffer = 200;
            return effectScreenX > -buffer && effectScreenX < 1600 + buffer;
        }
        private bool IsEnemyVisible(Enemy enemy)
        {
            double enemyScreenX = enemy.X + viewportOffsetX;

            // Buffer zone so enemies don't pop in/out abruptly at viewport edges
            const double buffer = 200;

            return enemyScreenX > -buffer && enemyScreenX < 1600 + buffer;
        }


        // REPLACE YOUR SpriteAnimation CLASS AND ANIMATION CODE WITH THIS:

        class SpriteAnimation
        {
            public int FrameCount { get; set; }
            public int FrameWidth { get; set; }
            public int FrameHeight { get; set; }
            public int CurrentFrame { get; set; }
            public int FrameDelayMs { get; set; }
            public int TotalWidth { get; set; }
            public DateTime LastFrameChange { get; set; } = DateTime.Now;
        }

        // ADD THESE FIELDS AT THE TOP OF YOUR CLASS:
        private Dictionary<string, SpriteAnimation> animations = new();

        // REPLACE YOUR UpdateAnimations METHOD WITH THIS:
        private void UpdateAnimations()
        {
            var now = DateTime.Now;
            // Define all animations - format: (frames, width, height, totalWidth, delayMs)
            var animDefs = new Dictionary<string, (int frames, int width, int height, int totalWidth, int delayMs)>
            {
                // === TRAIN CAR MOVEMENT ANIMATIONS ===
                ["engine_move"] = (5, 192, 64, 960, 100),
                ["rhinoarmor_move"] = (5, 192, 64, 960, 100),
                ["elephant_move"] = (5, 192, 64, 960, 100),
                ["resource_move"] = (5, 192, 64, 960, 100),
                ["cannon_move"] = (5, 192, 64, 960, 100),
                ["missile_move"] = (5, 192, 64, 960, 100),    // Placeholder
                ["turret_move"] = (5, 192, 64, 960, 100),
                ["drone_move"] = (5, 192, 64, 960, 100),      // Placeholder
                ["flame_move"] = (1, 192, 64, 192, 100),      // Placeholder
                ["aaflak_move"] = (5, 192, 64, 960, 100),     // Placeholder
                ["aajavelin_move"] = (5, 192, 64, 960, 100),  // Placeholder
                ["gatlin_move"] = (5, 192, 64, 960, 100),     // Placeholder
                ["flare_move"] = (5, 192, 64, 960, 100),      // Placeholder
                ["chinaku_move"] = (1, 192, 64, 192, 100),      // Placeholder
                ["tesla_move"] = (5, 192, 64, 960, 100),      // Placeholder
                ["caboose_move"] = (5, 192, 64, 960, 100),

                // === TRAIN CAR ATTACK ANIMATIONS ===
                ["engine_attack"] = (1, 192, 64, 192, 100),
                ["cannon_attack"] = (5, 192, 64, 960, 60),
                ["missile_attack"] = (5, 192, 64, 960, 80),    // Placeholder
                ["turret_attack"] = (5, 192, 64, 960, 100),
                ["drone_attack"] = (5, 192, 64, 960, 100),     // Placeholder
                ["flame_attack"] = (1, 192, 64, 192, 100),     // Placeholder
                ["aaflak_attack"] = (5, 192, 64, 960, 100),    // Placeholder
                ["aajavelin_attack"] = (5, 192, 64, 960, 100), // Placeholder
                ["gatlin_attack"] = (5, 192, 64, 960, 50),     // Placeholder - faster for gatling
                ["flare_attack"] = (5, 192, 64, 960, 100),     // Placeholder
                ["chinaku_attack"] = (1, 192, 64, 192, 100),      // Placeholder
                ["tesla_attack"] = (5, 192, 64, 960, 100),      // Placeholder
                // === ENEMY MOVEMENT ANIMATIONS ===
                ["enemy_tree_move"] = (1, 30, 50, 30, 100),         // Placeholder - trees don't really walk
                ["enemy_rock_move"] = (1, 30, 30, 30, 100),         // Placeholder - rocks roll maybe?
                ["enemy_monster1_move"] = (4, 30, 40, 120, 120),    // 4-frame walk cycle
                ["enemy_monster2_move"] = (4, 30, 40, 120, 100),    // 4-frame walk cycle, faster
                ["enemy_bat_move"] = (4, 30, 30, 120, 80),          // 4-frame flight cycle, fast wings
                ["enemy_thrower_move"] = (4, 30, 40, 120, 150),     // 4-frame walk, slower (he's cautious)
                ["enemy_bossTree_move"] = (1, 30, 50, 30, 100),     // Reuses tree

                // === ENEMY ATTACK ANIMATIONS ===
                ["enemy_tree_attack"] = (1, 30, 50, 30, 100),       // Placeholder
                ["enemy_rock_attack"] = (1, 30, 30, 30, 100),       // Placeholder
                ["enemy_monster1_attack"] = (3, 30, 40, 90, 100),   // 3-frame attack swipe
                ["enemy_monster2_attack"] = (3, 30, 40, 90, 80),    // 3-frame attack, faster
                ["enemy_bat_attack"] = (2, 30, 30, 60, 100),        // 2-frame dive attack
                ["enemy_thrower_attack"] = (4, 30, 40, 120, 100),   // 4-frame throwing animation
                ["enemy_bossTree_attack"] = (1, 30, 50, 30, 100),   // Placeholder

                // === ENEMY DEATH ANIMATIONS ===
                ["enemy_tree_death"] = (3, 30, 50, 90, 150),        // 3-frame fall/break
                ["enemy_rock_death"] = (3, 30, 30, 90, 150),        // 3-frame crumble
                ["enemy_monster1_death"] = (4, 30, 40, 120, 120),   // 4-frame death
                ["enemy_monster2_death"] = (4, 30, 40, 120, 120),   // 4-frame death
                ["enemy_bat_death"] = (3, 30, 30, 90, 100),         // 3-frame death spiral
                ["enemy_thrower_death"] = (4, 30, 40, 120, 120),    // 4-frame death
                ["enemy_bossTree_death"] = (3, 30, 50, 90, 150),    // Reuses tree death

                // === TRAIN PROJECTILE EFFECTS ===
                ["effect_smoke"] = (1, 32, 32, 32, 100),
                ["effect_explosion"] = (1, 32, 32, 32, 100),
                ["effect_engine"] = (1, 32, 32, 32, 100),           // Placeholder
                ["effect_cannon"] = (5, 32, 32, 160, 60),
                ["effect_missile"] = (1, 35, 35, 35, 100),          // Placeholder
                ["effect_turret"] = (1, 28, 28, 28, 100),           // Placeholder
                ["effect_drone"] = (1, 30, 30, 30, 100),            // Placeholder
                ["effect_flame"] = (1, 25, 25, 25, 60),             // 3-frame fire effect
                ["effect_aaflak"] = (1, 45, 45, 45, 100),           // Static cloud burst
                ["effect_aajavelin"] = (1, 38, 38, 38, 100),        // Placeholder
                ["effect_gatlin"] = (1, 20, 20, 20, 100),           // Placeholder
                ["effect_flare"] = (1, 42, 42, 42, 100),            // Placeholder
                ["effect_chinaku"] = (1, 42, 42, 42, 100),            // Placeholder
                ["effect_tesla"] = (1, 20, 20, 20, 100),           // Placeholder
                // === DRONE PROJECTILE EFFECTS ===

                ["effect_droneBullet"] = (1, 20, 20, 20, 80),  // 🚁 NEW: Drone's bullets (3-frame laser/energy shot)

                // === HEALING CAR ANIMATIONS ===
                ["medic_move"] = (1, 192, 64, 192, 100),
                ["medic_attack"] = (1, 192, 64, 192, 100),  // Healing animation
                ["shaman_move"] = (1, 192, 64, 192, 100),
                ["shaman_attack"] = (1, 192, 64, 192, 100),

                // === HEALING EFFECT ANIMATIONS ===
                ["effect_heal"] = (1, 40, 40, 40, 80),  // 4-frame green cross rising
                ["effect_aura"] = (1, 40, 40, 40, 100),  // 6-frame purple pulse

                // === COCAINE BEAR BUFF CAR ===
                ["cocainebear_move"] = (1, 192, 64, 192, 100),
                ["cocainebear_attack"] = (1, 192, 64, 192, 100),

                // === BUFF EFFECT ANIMATION ===
                ["effect_buffbear"] = (1, 40, 40, 40, 80),  // White glowing buff effect


                // === ENEMY PROJECTILE EFFECTS ===
                ["enemyEffect_thrower"] = (2, 24, 24, 48, 100),    // 2-frame spinning spear
["enemyEffect_tree"] = (1, 24, 24, 24, 100),       // Static rock
["enemyEffect_rock"] = (1, 24, 24, 24, 100),       // Static rock
["enemyEffect_monster1"] = (2, 24, 24, 48, 100),   // Generic spin
["enemyEffect_monster2"] = (2, 24, 24, 48, 100),   // Generic spin
["enemyEffect_bat"] = (2, 24, 24, 48, 120),        // Fast spin
["enemyEffect_bird"] = (2, 24, 24, 48, 100),       // Generic spin
["enemyEffect_generic"] = (2, 24, 24, 48, 100),    // Fallback


                // 🆕 ADD BIRD:
                ["enemy_bird_move"] = (4, 30, 30, 120, 70),  // 4-frame wing flap, fast
                ["enemy_bird_attack"] = (3, 30, 30, 90, 80),  // 3-frame dive attack
                ["enemy_bird_death"] = (3, 30, 30, 90, 120),  // 3-frame falling death

                // === Grenade car with explosion effects===
                ["effect_explosion"] = (1, 64, 64, 64, 100),  // 6 frames, fast animation
                ["grenade_move"] = (1, 192, 64, 192, 100),      // Placeholder
                ["grenade_attack"] = (1, 192, 64, 192, 100),     // Placeholder
                ["effect_grenade"] = (1, 42, 42, 42, 100),            // Placeholder

         
                ["clustergrenade_move"] = (5, 192, 64, 960, 100),      // Placeholder
                ["clustergrenade_attack"] = (5, 192, 64, 960, 100),     // Placeholder
                ["effect_clustergrenade"] = (1, 20, 20, 42, 100),            // Placeholder




                // === CHINA CART ===
                ["china_move"] = (1, 192, 64, 192, 100),
                ["china_attack"] = (1, 192, 64, 192, 100),
                ["effect_china"] = (4, 32, 32, 128, 80),  // 4-frame spinning fireball

                ["effect_flareexplosion"] = (1, 32, 32, 64, 100),
            };

            foreach (var kvp in animDefs)
            {
                var key = kvp.Key;
                var (frames, width, height, totalWidth, delayMs) = kvp.Value;

                // Initialize if doesn't exist
                if (!animations.ContainsKey(key))
                {
                    animations[key] = new SpriteAnimation
                    {
                        FrameCount = frames,
                        FrameWidth = width,
                        FrameHeight = height,
                        TotalWidth = totalWidth,
                        FrameDelayMs = delayMs,
                        CurrentFrame = 0,
                        LastFrameChange = now
                    };
                }

                var anim = animations[key];

                // Update frame if enough time has passed
                var elapsed = (now - anim.LastFrameChange).TotalMilliseconds;
                if (elapsed >= anim.FrameDelayMs)
                {
                    anim.CurrentFrame = (anim.CurrentFrame + 1) % anim.FrameCount;
                    anim.LastFrameChange = now;
                }
            }
        }

        // ADD THIS HELPER METHOD TO GET THE RIGHT ANIMATION KEY:
        private string GetAnimationKey(string carType, string state = "move")
        {
            return $"{carType}_{state}";
        }


        // Change LoadHighScores to return Task:
        // 2. Change LoadHighScores to LoadHighScoresAsync:
        private async Task LoadHighScoresAsync()
        {
            try
            {
                var result = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "highscores");
                Console.WriteLine($"RAW STORAGE: {result}"); // ADD THIS DEBUG LINE
                if (!string.IsNullOrEmpty(result))
                {
                    highScores = System.Text.Json.JsonSerializer.Deserialize<List<HighScore>>(result)
                        ?? new List<HighScore>();
                    Console.WriteLine($"LOADED {highScores.Count} scores"); // ADD THIS TOO
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}"); // SEE ACTUAL ERROR
                highScores = new List<HighScore>();
            }
        }

      
     

        private async void CheckHighScore()
        {
            await LoadHighScoresAsync(); // Make it async and await it
            StateHasChanged(); // Force UI update

            if (highScores.Count < MaxHighScores || score > highScores.Last().Score)
            {
                showInitialsInput = true;
                gameState = GameState.GameOver;
            }
            else
            {
                gameState = GameState.GameOver;
            }
        }

        private async Task SaveHighScoresAsync()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(highScores);
                Console.WriteLine($"Saving {highScores.Count} scores: {json.Substring(0, Math.Min(100, json.Length))}...");

                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "highscores", json);

                // Verify it saved
                var verify = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "highscores");
                Console.WriteLine($"Verified save: {verify != null && verify.Length > 0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Save error: {ex.Message}");
            }
        }

        private async void SubmitHighScore()
        {
            playerInitials = string.IsNullOrWhiteSpace(playerInitials) ? "AAA" : playerInitials.Trim().ToUpper();
            if (playerInitials.Length > 3) playerInitials = playerInitials.Substring(0, 3);

            highScores.Add(new HighScore
            {
                Initials = playerInitials,
                Score = score,
                Level = level,
                Kills = totalKills,
                Date = DateTime.Now
            });

            highScores = highScores.OrderByDescending(h => h.Score).Take(MaxHighScores).ToList();

            Console.WriteLine($"About to save {highScores.Count} scores");
            await SaveHighScoresAsync();

            showInitialsInput = false;
            StateHasChanged();
        }






        public void Dispose()
        {
            gameTimer?.Dispose();
        }

        class CarInfo
        {
            public string Name { get; set; } = "";
            public float Damage { get; set; }
            public int FireRate { get; set; }
            public string Color { get; set; } = "";
            public int FireRangeX { get; set; }
            public int FireRangeY { get; set; }
            public int FireRadius { get; set; }

            // 🆕 ADD THESE TWO LINES:
            public int ShotOffsetX { get; set; } = 0;  // Offset from car center for spawn position
            public int ShotOffsetY { get; set; } = 0;  // Offset from car center for spawn position

            // 🆕 ADD THESE TWO LINES:
            public int? DetectionBoxX { get; set; } = null;  // Rectangular detection width
            public int? DetectionBoxY { get; set; } = null;  // Rectangular detection height

            public bool IsLocked { get; set; } = false;
            public int MaxHp { get; set; } = 100;

            // NEW WEAPON PROPERTIES
            public int ExplosionRadius { get; set; } = 0;
            public int ProjectileWidth { get; set; } = 32;
            public int ProjectileHeight { get; set; } = 32;
            public int ScatterCount { get; set; } = 1;
            public double ScatterAngle { get; set; } = 0;
            public List<(int x, int y)> FiringPositions { get; set; } = new();
            public int ProjectileCount { get; set; } = 1;
            public bool InstantEffect { get; set; } = false;
            public int BurstDelayMin { get; set; } = 2;  // NEW: Min frames between bursts
            public int BurstDelayMax { get; set; } = 5;  // NEW: Max frames between bursts


            // 🆕 ADD THESE TRAJECTORY PROPERTIES:
            public TrajectoryType ProjectileTrajectory { get; set; } = TrajectoryType.Linear;
            public double ProjectileGravity { get; set; } = 0;
            public double ProjectileSpeed { get; set; } = 15;  // Pixels per frame
            public bool PlayExplosionAnimation { get; set; } = false;
            public bool IsRotatingWeapon { get; set; } = false;  // 🆕 ADD THIS

            public bool IsHealingCar { get; set; } = false;
            public int HealAmount { get; set; } = 0;
            public bool IsRegenAura { get; set; } = false;
            public double RegenPerSecond { get; set; } = 0;

            // Buff car properties
            public bool IsBuffCar { get; set; } = false;
            public int BuffDuration { get; set; } = 0;
            public double FireRateBoostMultiplier { get; set; } = 1.0;
        }

        class TrainCar  // instance
        {
            public string Type { get; set; } = "";
            public int Hp { get; set; }
            public Guid Id { get; set; } = Guid.NewGuid();
        }



        class EnemyInfo
        {
            public int Hp { get; set; }
            public int Xp { get; set; }
            public int Points { get; set; }
            public double Speed { get; set; }
            public string Color { get; set; } = "";
            public double SpawnXMin { get; set; }
            public double SpawnXMax { get; set; }
            public double SpawnYMin { get; set; }
            public double SpawnYMax { get; set; }

            // NEW: Ranged attack properties
            public bool CanShoot { get; set; } = false;
            public int ShootRange { get; set; } = 0;
            public int ShootRate { get; set; } = 0;  // milliseconds between shots
            public float ProjectileDamage { get; set; } = 0;
            // 🆕 ADD THESE NEW PROPERTIES:
            public int MinLevel { get; set; } = 1;  // When this enemy first appears
            public Dictionary<int, double> LevelSpawnWeights { get; set; } = new();  // Level milestone -> spawn weight
                                                                                     // 🆕 ADD THESE MOVEMENT PROPERTIES:
            public MovementPattern MovementPattern { get; set; } = MovementPattern.Straight;
            public double MovementAmplitude { get; set; } = 0;    // How far up/down (pixels)
            public double MovementFrequency { get; set; } = 0;    // How fast the pattern cycles
        }
    

        class Enemy
        {
            public string Id { get; set; } = "";
            public string Type { get; set; } = "";
            public double X { get; set; }
            public double Y { get; set; }
            public double Hp { get; set; }
            public double MaxHp { get; set; }
            public double Speed { get; set; }
            public string AnimState { get; set; } = "move";  // NEW: move, attack, death
            public DateTime StateChangeTime { get; set; } = DateTime.Now;  // NEW: when state changed
            public bool MarkedForDeath { get; set; } = false;  // NEW: playing death animation
                                                               // 🆕 ADD THESE MOVEMENT TRACKING PROPERTIES:
        public double BaseY { get; set; }              // Original Y spawn position (center of oscillation)
        public double MovementTime { get; set; } = 0;  // Tracks time for wave calculations
        public MovementPattern Pattern { get; set; } = MovementPattern.Straight;
        public double Amplitude { get; set; } = 0;
        public double Frequency { get; set; } = 0;
    }
        class EnemyProjectile
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Type { get; set; } = "";  // Which enemy type shot it
            public double X { get; set; }
            public double Y { get; set; }
            public double Speed { get; set; } = 3;
            public float Damage { get; set; }
            public int Width { get; set; } = 32;
            public int Height { get; set; } = 32;

            // 🆕 ADD TARGETING PROPERTIES:
            public double TargetX { get; set; }  // Where it's aiming
            public double TargetY { get; set; }
            public double VelocityX { get; set; }  // X movement per frame
            public double VelocityY { get; set; }  // Y movement per frame
        }
        class Effect
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double TargetX { get; set; }
            public double TargetY { get; set; }
            public string Type { get; set; } = "";
            public int Life { get; set; }
            public int Width { get; set; } = 32;
            public int Height { get; set; } = 32;
            public int SpawnDelay { get; set; } = 0;
            public bool IsVisible { get; set; } = true;

            // 🆕 ADD THESE NEW PROPERTIES FOR TRAJECTORY:
            public TrajectoryType Trajectory { get; set; } = TrajectoryType.Linear;
            public double VelocityX { get; set; } = 0;
            public double VelocityY { get; set; } = 0;
            public double Gravity { get; set; } = 0;
            public string TargetEnemyId { get; set; } = "";  // For homing (future feature)


            // 🆕 ADD THIS:
            public double RotationAngle { get; set; } = 0;  // Degrees (0 = right, 90 = down, -90 = up)
            public bool IsExplosion { get; set; } = false;
            public bool HasExploded { get; set; } = false;
            // 🚁 DRONE HP SYSTEM
            public double Hp { get; set; } = 0;
            public double MaxHp { get; set; } = 0;
            public DateTime LastDroneShot { get; set; } = DateTime.Now;
        }

        class UpgradeChoice
        {
            public string Type { get; set; } = ""; // "newCar", "stat", "weaponStat", "speed"
            public string Car { get; set; } = "";
            public string Description { get; set; } = "";
            public string StatType { get; set; } = ""; // "damage", "firerate", "firerange", "explosion", "projectiles", "scatter"
            public string TargetWeaponType { get; set; } = ""; // For weapon-specific upgrades
            public bool IsGlobal { get; set; } = false; // True for all-weapon upgrades
        }

        class WeaponUpgradeState
        {
            public double DamageMultiplier { get; set; } = 1.0;
            public double FireRateMultiplier { get; set; } = 1.0;
            public double RangeMultiplier { get; set; } = 1.0;
            public double ExplosionRadiusMultiplier { get; set; } = 1.0;
            public int BonusProjectileCount { get; set; } = 0;
            public int BonusScatterCount { get; set; } = 0;
            public double ScatterAngleMultiplier { get; set; } = 1.0;
            public double HealAmountMultiplier { get; set; } = 1.0;
            public double HealCooldownMultiplier { get; set; } = 1.0;
            public double RegenRateMultiplier { get; set; } = 1.0;
        }

        class HighScore
        {
            public string Initials { get; set; } = "AAA";
            public int Score { get; set; }
            public int Level { get; set; }
            public int Kills { get; set; }
            public DateTime Date { get; set; } = DateTime.Now;
        }




    }
}
