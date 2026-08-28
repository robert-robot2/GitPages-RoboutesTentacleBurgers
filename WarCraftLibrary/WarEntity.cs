using System;
using System.Collections.Generic;
using System.Text;

namespace WarCraftLibrary
{

    // ===== BASE ENTITY CLASS =====
   
    public abstract class WarEntity
    {
        public string PlaceholderName { get; set; } = "Unknown";
        public string SpritePath { get; set; } = "";
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        // ✅ NEW: Owner tracking properties
        public Faction? OwnerFaction { get; set; }      // Which faction owns this entity
        public int OwnerPlayerIndex { get; set; } = -1; // Which player slot (0 = human, 1-15 = AI)
        public int OwnerTeam { get; set; } = 0;         // Team number (0 = FFA, 1-4 = team)
        public string EntityStyle => $"position:absolute; left:{PosX}px; top:{PosY}px; width:{Width}px; height:{Height}px;";
        
        // ✅ NEW: Collision properties
        // This radius is used for collision detection and pathfinding
        public int CollisionRadius => Math.Max(Width, Height) / 2;
    }


    // ===== TREE ENTITY =====
    public class Tree : WarEntity
    {
        public int LumberRemaining { get; set; } = 1000;
        public bool IsChopped { get; set; } = false;

        public Tree(int gridX, int gridY)
        {
            PlaceholderName = "Tree";
            SpritePath = "/wc1sprites/objects/TreeWC1.png";
            Width = 32;
            Height = 32;

            PosX = gridX * 32;
            PosY = gridY * 32;
        }

        public void Chop(int amount = 10)
        {
            LumberRemaining -= amount;

            if (LumberRemaining <= 0)
            {
                LumberRemaining = 0;
                IsChopped = true;
                SpritePath = "/wc1sprites/objects/DeadTree002.png";

                // ===== NEW: CLEAR TREE FROM GRID (now walkable) =====
                GridOccupancyMap.ClearTree(this);
                Console.WriteLine($"🪓 Tree chopped - grid cell now walkable");
            }
        }
    }


    public class GoldMine : WarEntity
    {
        public int GoldRemaining { get; set; } = 150000;
        public bool IsDepleted { get; set; } = false;

        public GoldMine(int gridX, int gridY)
        {
            PlaceholderName = "GoldMine";
            SpritePath = "/wc1sprites/buildings/GoldMineWChasgold001.png";

            Width = 96;
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;
        }

        public int Harvest(int amount = 100)
        {
            if (IsDepleted) return 0;

            int harvested = Math.Min(amount, GoldRemaining);
            GoldRemaining -= harvested;

            if (GoldRemaining <= 0)
            {
                GoldRemaining = 0;
                IsDepleted = true;
                SpritePath = "/wc1sprites/buildings/GoldMineWC1.png";

                // ===== NEW: KEEP MINE ON GRID (still blocks movement) =====
                // Gold mines stay as obstacles even when depleted
                Console.WriteLine($"⛏️ Gold mine depleted - remains as obstacle");
            }

            return harvested;
        }
    }


    // ===== NEW: BUILDING BASE CLASS =====
    public abstract class WarBuilding : WarEntity
    {
        public RaceType Race { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public bool IsConstructed { get; set; } = true;
        public int ConstructionTimeRemaining { get; set; } = 0; // In ticks (60 = 1 second)

        // ✅ NEW: Track how long to show destruction sprite before removal
        public int DestructionTimer { get; set; } = 0;
        public const int DestructionDisplayDuration = 1800; // 30 seconds (same as unit corpses)
        // ✅ NEW: BUILDING ANIMATION PROPERTIES (PHASE 1)
        /// <summary>
        /// Current animation state (Constructing, Built, Damaged, Destroyed)
        /// </summary>
        public BuildingAnimationState CurrentBuildingState { get; set; } = BuildingAnimationState.Built;

        /// <summary>
        /// Construction sprite sheet index (0-4)
        /// 0 = 0% complete, 1 = 25%, 2 = 50%, 3 = 75%, 4 = 100%
        /// </summary>
        public int ConstructionSpriteSheet { get; set; } = 4;

        /// <summary>
        /// Damage visual level (0-3)
        /// 0 = Pristine (no damage), 1 = Light, 2 = Medium, 3 = Heavy
        /// </summary>
        public int DamageLevel { get; set; } = 0;

        /// <summary>
        /// Total construction time (in ticks) - stored for progress calculation
        /// </summary>
        public int TotalConstructionTime { get; set; } = 0;

        // Production queue
        public Queue<ProductionOrder> ProductionQueue { get; set; } = new();

        // Rally point (where units spawn)
        public int RallyX { get; set; }
        public int RallyY { get; set; }
        // ✅ PART 2: Track if rally point was manually set
        public bool HasCustomRallyPoint { get; set; } = false;
        // Selection
        public bool IsSelected { get; set; } = false;

        public WarBuilding()
        {
            Width = 128; // Buildings are larger than 32x32
            Height = 128;
        }

        // ✅ NEW: Helper methods for ownership/team checks
        public bool IsOwnedByPlayer() => OwnerPlayerIndex == 0;

        public bool IsOwnedByAI() => OwnerPlayerIndex > 0;

        public bool IsAlly(WarEntity other)
        {
            // Same team and not FFA (team 0)
            return OwnerTeam > 0 && OwnerTeam == other.OwnerTeam;
        }

        public bool IsEnemy(WarEntity other)
        {
            // Different owner and not allies
            return !IsAlly(other) && OwnerPlayerIndex != other.OwnerPlayerIndex;
        }
    }

// town hall castle may need adjsutment with code here-->>
    // ===== TOWN HALL =====
    public class TownHall : WarBuilding
    {
        public TownHall(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "TownHall_Human" : "Stronghold_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/TownHall001.png"
                : "/wc1sprites/buildings/OrcSH001.png";

            MaxHP = 1200;
            HP = 1200;
            IsConstructed = false; // placeholder: start as unbuilt

            // ✅ Use build time from ProductionSystem if exists
            if (ProductionSystem.BuildingCosts.TryGetValue("TownHall", out var cost))
            {
                TotalConstructionTime = cost.time * 60; // ✅ NEW: Store total time
                ConstructionTimeRemaining = cost.time * 60; // convert seconds to ticks
            }
            else
            {
                TotalConstructionTime = 120 * 60; // ✅ NEW
                ConstructionTimeRemaining = 120 * 60; // fallback: 2 minutes
            }

            // ✅ NEW: Initialize animation state
            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0; // Start at 0% sprite

            Width = 128;
            Height = 128;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 64;
            RallyY = PosY + 150;
        }
    }

    // ===== CASTLE (UPGRADED TOWN HALL) =====
    public class Castle : WarBuilding
    {
        public Castle(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "Castle_Human" : "Fortress_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/Castle001.png"
                : "/wc1sprites/buildings/OrcFortress001.png";

            MaxHP = 2000;  // More HP than Town Hall
            HP = 2000;
            IsConstructed = true;  // Upgraded buildings start constructed

            // ✅ NEW: Castle starts fully built
            CurrentBuildingState = BuildingAnimationState.Built;
            ConstructionSpriteSheet = 4; // 100% complete
            DamageLevel = 0; // Pristine

            Width = 160;  // 4x4 grid (same as Town Hall)
            Height = 160;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 64;
            RallyY = PosY + 150;
        }

        // ✅ Constructor for upgrading from existing Town Hall
        public Castle(TownHall townHall)
        {
            Race = townHall.Race;
            PlaceholderName = townHall.Race == RaceType.Human ? "Castle_Human" : "Fortress_Orc";
            SpritePath = townHall.Race == RaceType.Human
                ? "/wc1sprites/buildings/Castle001.png"
                : "/wc1sprites/buildings/OrcFortress001.png";

            MaxHP = 2000;
            HP = townHall.HP + 800; // Heal +800 HP on upgrade
            IsConstructed = true;

            // ✅ NEW: Castle starts fully built (upgraded instantly)
            CurrentBuildingState = BuildingAnimationState.Built;
            ConstructionSpriteSheet = 4;

            // DamageLevel = DamageLevel.GetDamageLevel(HP, MaxHP); // ✅ Calculate damage state from inherited HP
            DamageLevel = 0;


            Width = townHall.Width;
            Height = townHall.Height;
            PosX = townHall.PosX;
            PosY = townHall.PosY;
            RallyX = townHall.RallyX;
            RallyY = townHall.RallyY;

            // ✅ Inherit production queue and selection state
            ProductionQueue = townHall.ProductionQueue;
            IsSelected = townHall.IsSelected;
        }
    }

    // ===== BARRACKS =====
    public class Barracks : WarBuilding
    {
        public Barracks(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "Barracks_Human" : "Barracks_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/HumanBar001.png"
                : "/wc1sprites/buildings/OrcBar001.png";

            MaxHP = 800;
            HP = 800;
            IsConstructed = false;

            // ✅ Use build time from ProductionSystem
            if (ProductionSystem.BuildingCosts.TryGetValue("Barracks", out var cost))
            {
                TotalConstructionTime = cost.time * 60; // ✅ NEW
                ConstructionTimeRemaining = cost.time * 60; // convert seconds to ticks
            }
            else
            {
                TotalConstructionTime = 60 * 60; // ✅ NEW
                ConstructionTimeRemaining = 60 * 60; // fallback: 60 seconds
            }

            // ✅ NEW: Initialize animation state
            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 128;
            Height = 128;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 64;
            RallyY = PosY + 150;
        }
    }

    // ===== FARM / HOUSE =====
    public class Farm : WarBuilding
    {
        public int PopulationProvided { get; set; } = 5;

        public Farm(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "Farm_Human" : "PigFarm_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/HumanFarm001.png"
                : "/wc1sprites/buildings/OrcFarm001.png";

            MaxHP = 400;
            HP = 400;
            IsConstructed = false;

            // ✅ Use build time from ProductionSystem
            if (ProductionSystem.BuildingCosts.TryGetValue("Farm", out var cost))
            {
                TotalConstructionTime = cost.time * 60; // ✅ NEW
                ConstructionTimeRemaining = cost.time * 60; // convert seconds to ticks
            }
            else
            {
                TotalConstructionTime = 45 * 60; // ✅ NEW
                ConstructionTimeRemaining = 45 * 60; // fallback: 45 seconds
            }

            // ✅ NEW: Initialize animation state
            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 64; // 2x2 grid
            Height = 64;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }

    public abstract class WarUnit : WarEntity
    {
        public RaceType Race { get; set; }
        public int HP { get; set; }
        public int MaxHP { get; set; }
        public UnitState State { get; set; } = UnitState.Idle;


        // ✅ NEW: Stance System
        public UnitStance Stance { get; set; } = UnitStance.Defensive; // Default stance
        public bool HasProximityBuff { get; set; } = false;            // Is buff active?
        public int ProximityBuffDamage { get; set; } = 0;              // +/- damage from buff
        // Movement
        public int MoveSpeed { get; set; } = 2;
        public int? TargetX { get; set; }
        public int? TargetY { get; set; }

        public int? WaypointX { get; set; }
        public int? WaypointY { get; set; }
        public int WaypointTimeout { get; set; } = 0;

        // Combat
        public int AttackDamage { get; set; }
        public int AttackRange { get; set; }
        public WarUnit? AttackTarget { get; set; }
        public WarBuilding? AttackTargetBuilding { get; set; }
        public int AttackCooldown { get; set; } = 0;
        public int AttackSpeed { get; set; } = 60;

        // Unstuck tracking
        public int StuckCounter { get; set; } = 0;
        public int LastPosX { get; set; }
        public int LastPosY { get; set; }

        // ✅ NEW: MANA SYSTEM (PHASE 1)
        public int Mana { get; set; } = 0;           // Current mana
        public int MaxMana { get; set; } = 0;        // Maximum mana
        public int ManaRegen { get; set; } = 0;      // Mana per tick (60 ticks = 1 sec)

        // ✅ NEW: SPELL/ABILITY SYSTEM (PHASE 1 - Just properties, no logic yet)
        public string? AbilityName { get; set; } = null;     // "Heal", "Bloodlust", null
        public int AbilityManaCost { get; set; } = 5;        // Mana cost per cast
        public int AbilityRange { get; set; } = 100;         // Cast range in pixels
        public int AbilityCooldown { get; set; } = 0;        // Ticks until can cast again
        public int AbilityCooldownMax { get; set; } = 30;    // 0.5 seconds between casts
        public bool AutoCastEnabled { get; set; } = true;    // Toggle for auto-casting

        // ✅ NEW: BLOODLUST BUFF (for Cultist targets)
        public int BloodlustBonus { get; set; } = 0;         // +1 damage when buffed
        public int BloodlustDuration { get; set; } = 0;      // Ticks remaining (300 = 5 sec)
                                                             // ✅ NEW: MOUNT SKILL PROPERTIES
        public bool CanMount { get; set; } = false;          // Can this unit mount? (Footman/Grunt)
        public string? MountType { get; set; } = null;       // "Horse" or "Wolf"
        public bool IsMountable { get; set; } = false;       // Is this a mountable creature? (Horse/Wolf)
        // ===== NEW: SPATIAL AWARENESS PROPERTIES =====

        /// <summary>
        /// Current grid X position (calculated from pixel position)
        /// </summary>
        public int CurrentGridX => PosX / 32;

        /// <summary>
        /// Current grid Y position (calculated from pixel position)
        /// </summary>
        public int CurrentGridY => PosY / 32;

        /// <summary>
        /// Last grid position (for stuck detection)
        /// </summary>
        public (int x, int y) LastGridPosition { get; set; } = (0, 0);

       

        // ===== QUEUE SYSTEM (NEW) =====

        /// <summary>
        /// The unit we're waiting behind in a queue
        /// </summary>
        public WarUnit? WaitingForUnit { get; set; } = null;

        /// <summary>
        /// How many ticks we've been waiting for another unit to move
        /// </summary>
        public int WaitingTimer { get; set; } = 0;

        // ===== NEW: SPATIAL QUERY METHODS =====

        /// <summary>
        /// Check if the next cell in direction of target is blocked
        /// </summary>
        public bool IsNextCellBlocked()
        {
            if (TargetX == null || TargetY == null) return false;

            // Calculate next grid cell in direction of target
            int deltaX = TargetX.Value - PosX;
            int deltaY = TargetY.Value - PosY;

            if (deltaX == 0 && deltaY == 0) return false;

            int nextGridX = CurrentGridX + Math.Sign(deltaX);
            int nextGridY = CurrentGridY + Math.Sign(deltaY);

            return !GridOccupancyMap.IsCellFree(nextGridX, nextGridY);
        }

        /// <summary>
        /// Get list of adjacent free cells (8 directions)
        /// </summary>
        public List<(int x, int y)> GetAdjacentFreeCells()
        {
            return GridOccupancyMap.GetAdjacentFreeCells(CurrentGridX, CurrentGridY);
        }

        /// <summary>
        /// Check if unit has moved since last tick
        /// </summary>
        public bool HasMovedGridCell()
        {
            return (CurrentGridX, CurrentGridY) != LastGridPosition;
        }
        // Resource gathering
        public int CarryingGold { get; set; } = 0;
        public int CarryingLumber { get; set; } = 0;
        public int CarryCapacity { get; set; } = 0;
        public GoldMine? TargetMine { get; set; }
        public Tree? TargetTree { get; set; }
        public WarBuilding? TargetTownHall { get; set; }

        // Selection
        public bool IsSelected { get; set; } = false;

        // ===== NEW: ANIMATION PROPERTIES =====
        public AnimationState CurrentAnimationState { get; set; } = AnimationState.Idle;
        public Direction CurrentDirection { get; set; } = Direction.S;  // Default facing South
        public int AnimationFrame { get; set; } = 0;                    // Current frame (0-4)
        public int AnimationTickCounter { get; set; } = 0;              // Ticks since last frame change
        public bool IsDeathAnimationPlaying { get; set; } = false;      // True while death animation plays

        public WarUnit()
        {
            Width = 32;
            Height = 32;
        }

        // ✅ NEW: Helper methods for ownership/team checks
        public bool IsOwnedByPlayer() => OwnerPlayerIndex == 0;

        public bool IsOwnedByAI() => OwnerPlayerIndex > 0;

        public bool IsAlly(WarEntity other)
        {
            // Same team and not FFA (team 0)
            return OwnerTeam > 0 && OwnerTeam == other.OwnerTeam;
        }

        public bool IsEnemy(WarEntity other)
        {
            // Different owner and not allies
            return !IsAlly(other) && OwnerPlayerIndex != other.OwnerPlayerIndex;
        }
    }
    public enum UnitState
    {
        Idle,
        Moving,
        Scouting,
        Attacking,
        GatheringGold,
        GatheringLumber,
        ReturningResources,
        Constructing,
        Dead
    }
    // ===== UNIT STANCE ENUM =====
    public enum UnitStance
    {
        Passive,      // Don't auto-attack, only defend when attacked
        Defensive,    // Attack enemies that come close
        Aggressive,   // Actively seek and attack enemies
        AttackMove,   // Move to location and attack anything on the way
        Hold,         // Don't move, attack enemies in range
        Patrol        // Move between two points, attack enemies
    }

    // ===== PEASANT / PEON =====
    public class Peasant : WarUnit
    {
        public Peasant(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "Peasant_Human" : "Peon_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/units/Peasant001T.png"
                : "/wc1sprites/units/Peon001T.png";

            MaxHP = 30;
            HP = 30;
            MoveSpeed = 3;
            AttackDamage = 3;
            AttackRange = 18;
            CarryCapacity = 100;
            // ✅ NEW: Mount capability (unlocked by upgrade)
            CanMount = false; // Will be set to true when upgrade researched
            MountType = race == RaceType.Human ? "Horse" : "Wolf";
            PosX = worldX;
            PosY = worldY;
        }
    }


    // ===== FOOTMAN / GRUNT =====
    public class Footman : WarUnit
    {
        public Footman(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "Footman_Human" : "Grunt_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/units/Footman1T.png"
                : "/wc1sprites/units/Grunt001T.png";

            MaxHP = 60;
            HP = 60;
            MoveSpeed = 4;
            AttackDamage = 8;
            AttackRange = 15;
            // ✅ ADD THESE TWO LINES:
            CanMount = false; // Gets set to true by upgrade
            MountType = race == RaceType.Human ? "Horse" : "Wolf";
            PosX = worldX;
            PosY = worldY;
        }
    }

    // ===== LUMBER MILL =====
    public class LumberMill : WarBuilding
    {
        public LumberMill(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "LumberMill_Human" : "LumberMill_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/HumanLumberMill001.png"
                : "/wc1sprites/buildings/OrcLumberMill001.png";

            MaxHP = 600;
            HP = 600;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("LumberMill", out var cost))
            {
                TotalConstructionTime = cost.time * 60; // ✅ NEW
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 60 * 60; // ✅ NEW
                ConstructionTimeRemaining = 60 * 60; // fallback: 60 seconds
            }

            // ✅ NEW: Initialize animation state
            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 96;  // 3x3 grid
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 48;
            RallyY = PosY + 110;
        }
    }


    // ===== ARCHERY RANGE =====
    public class ArcheryRange : WarBuilding
    {
        public ArcheryRange(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "ArcheryRange_Human" : "ArcheryRange_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/HumanArchRange001.png"
                : "/wc1sprites/buildings/OrcAxeRange001.png";

            MaxHP = 800;
            HP = 800;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("ArcheryRange", out var cost))
            {
                TotalConstructionTime = cost.time * 60; // ✅ NEW
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 80 * 60; // ✅ NEW
                ConstructionTimeRemaining = 80 * 60; // fallback: 80 seconds
            }

            // ✅ NEW: Initialize animation state
            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 128;  // 4x4 grid
            Height = 128;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 64;
            RallyY = PosY + 150;
        }
    }


    // ===== ARCHER (HUMAN RANGED UNIT) =====
    public class Archer : WarUnit
    {
        public Archer(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Archer_Human";
            SpritePath = "/wc1sprites/units/Archer00122.png";

            MaxHP = 40;
            HP = 40;
            MoveSpeed = 3;
            AttackDamage = 6;
            AttackRange = 120; // Ranged unit - much longer range
            AttackSpeed = 90; // Slower attack speed (1.5 seconds)

            PosX = worldX;
            PosY = worldY;
        }
    }


    // ===== TROLL AXE THROWER (ORC RANGED UNIT) =====
    public class TrollAxeThrower : WarUnit
    {
        public TrollAxeThrower(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "TrollAxeThrower_Orc";
            SpritePath = "/wc1sprites/units/Axethrower001.png";

            MaxHP = 40;
            HP = 40;
            MoveSpeed = 3;
            AttackDamage = 6;
            AttackRange = 120; // Ranged unit
            AttackSpeed = 90; // Slower attack speed

            PosX = worldX;
            PosY = worldY;
        }
    }
    // ===== BLACKSMITH =====
    public class Blacksmith : WarBuilding
    {
        public Blacksmith(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "Blacksmith_Human" : "Blacksmith_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/HumanBlackSmith001.png"
                : "/wc1sprites/buildings/OrcBlackSmith001.png";

            MaxHP = 600;
            HP = 600;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("Blacksmith", out var cost))
            {
                TotalConstructionTime = cost.time * 60; // ✅ NEW
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 60 * 60; // ✅ NEW
                ConstructionTimeRemaining = 60 * 60; // fallback: 60 seconds
            }

            // ✅ NEW: Initialize animation state
            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 96;  // 3x3 grid
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 48;
            RallyY = PosY + 110;
        }
    }

    // ===== CHURCH (HUMAN MAGIC BUILDING) =====
    public class Church : WarBuilding
    {
        public Church(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Church_Human";
            SpritePath = "/wc1sprites/buildings/Church001.png";

            MaxHP = 600;
            HP = 600;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("Church", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 60 * 60;
                ConstructionTimeRemaining = 60 * 60; // 60 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 96;  // 3x3 grid (same as Lumber Mill/Blacksmith)
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 48;
            RallyY = PosY + 110;
        }
    }

    // ===== CULTIST HUT (ORC MAGIC BUILDING) =====
    public class CultistHut : WarBuilding
    {
        public CultistHut(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "CultistHut_Orc";
            SpritePath = "/wc1sprites/buildings/CultistHut001.png";

            MaxHP = 600;
            HP = 600;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("CultistHut", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 60 * 60;
                ConstructionTimeRemaining = 60 * 60; // 60 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 96;  // 3x3 grid
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 48;
            RallyY = PosY + 110;
        }
    }
    // ===== WOOD TOWER =====
    public class WoodTower : WarBuilding
    {
        // Combat properties
        public int AttackDamage { get; set; } = 6;
        public int AttackRange { get; set; } = 150;
        public int AttackSpeed { get; set; } = 90; // Same as Archer
        public int AttackCooldown { get; set; } = 0;
        public WarUnit? AttackTarget { get; set; }

        public WoodTower(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "WoodTower_Human" : "WoodTower_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/WoodTower003.png"
                : "/wc1sprites/buildings/OrcWoodTower002.png";

            MaxHP = 400;
            HP = 400;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("WoodTower", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 15 * 60;
                ConstructionTimeRemaining = 15 * 60; // 15 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 64;  // 2x2 grid
            Height = 64;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }

    // ===== STONE TOWER (UPGRADED) =====
    public class StoneTower : WarBuilding
    {
        // Combat properties
        public int AttackDamage { get; set; } = 10; // +4 from Wood
        public int AttackRange { get; set; } = 150;
        public int AttackSpeed { get; set; } = 90;
        public int AttackCooldown { get; set; } = 0;
        public WarUnit? AttackTarget { get; set; }

        public StoneTower(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "StoneTower_Human" : "StoneTower_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/StoneTower001.png"
                : "/wc1sprites/buildings/OrcStoneTower001.png";

            MaxHP = 800; // Double Wood Tower
            HP = 800;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("StoneTower", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 20 * 60;
                ConstructionTimeRemaining = 20 * 60; // 20 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 64;  // 2x2 grid
            Height = 64;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }

    // ===== WOODEN WALL =====
    public class WoodenWall : WarBuilding
    {
        public WoodenWall(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "WoodenWall_Human" : "WoodenWall_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/WoodenWall001.png"
                : "/wc1sprites/buildings/OrcWoodenWall001.png";

            MaxHP = 50;
            HP = 50;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("WoodenWall", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 5 * 60;
                ConstructionTimeRemaining = 5 * 60; // 5 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 32;  // 1x1 grid (tile-sized)
            Height = 32;

            PosX = gridX * 32;
            PosY = gridY * 32;

            // Walls don't need rally points
            RallyX = PosX + 16;
            RallyY = PosY + 16;
        }
    }

    // ===== STONE WALL (UPGRADED) =====
    public class StoneWall : WarBuilding
    {
        public StoneWall(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = race == RaceType.Human ? "StoneWall_Human" : "StoneWall_Orc";
            SpritePath = race == RaceType.Human
                ? "/wc1sprites/buildings/StoneWall001.png"
                : "/wc1sprites/buildings/OrcStoneWall001.png";

            MaxHP = 150; // 3x stronger than wooden
            HP = 150;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("StoneWall", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 10 * 60;
                ConstructionTimeRemaining = 10 * 60; // 10 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 32;  // 1x1 grid (tile-sized)
            Height = 32;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 16;
            RallyY = PosY + 16;
        }
    }
    // ===== ROAD =====
    public class Road : WarBuilding
    {
        public Road(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Road"; // Same for both races
            SpritePath = "/wc1sprites/buildings/Road001.png"; // Same sprite for both

            MaxHP = 100;
            HP = 100;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("Road", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 3 * 60;
                ConstructionTimeRemaining = 3 * 60; // 3 seconds - fast!
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 32;  // 1x1 grid (tile-sized)
            Height = 32;

            PosX = gridX * 32;
            PosY = gridY * 32;

            // Roads don't need rally points
            RallyX = PosX + 16;
            RallyY = PosY + 16;
        }
    }

    // ===== STABLES (HUMAN TIER 1 - TRAINS HORSES) =====
    public class Stables : WarBuilding
    {
        public Stables(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Stables_Human";
            SpritePath = "/wc1sprites/buildings/Stables001.png";

            MaxHP = 400;
            HP = 400;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("Stables", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 30 * 60;
                ConstructionTimeRemaining = 30 * 60; // 30 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 64;  // 2x2 grid
            Height = 64;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }

    // ===== PEN (ORC TIER 1 - TRAINS WOLVES) =====
    public class Pen : WarBuilding
    {
        public Pen(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Pen_Orc";
            SpritePath = "/wc1sprites/buildings/Pens001.png";

            MaxHP = 400;
            HP = 400;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("Pen", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 30 * 60;
                ConstructionTimeRemaining = 30 * 60;
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 64;  // 2x2 grid
            Height = 64;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }

    // ===== KNIGHTS HOLD (HUMAN TIER 2 - REQUIRES CASTLE) =====
    public class KnightsHold : WarBuilding
    {
        public KnightsHold(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "KnightsHold_Human";
            SpritePath = "/wc1sprites/buildings/KnightsHold001.png";

            MaxHP = 800;
            HP = 800;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("KnightsHold", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 40 * 60;
                ConstructionTimeRemaining = 40 * 60; // 40 seconds
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 96;  // 2x2 grid
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }

    // ===== RAIDER LAIR (ORC TIER 2 - REQUIRES FORTRESS) =====
    public class RaiderLair : WarBuilding
    {
        public RaiderLair(int gridX, int gridY, RaceType race)
        {
            Race = race;
            PlaceholderName = "RaiderLair_Orc";
            SpritePath = "/wc1sprites/buildings/RaiderLair001.png";

            MaxHP = 800;
            HP = 800;
            IsConstructed = false;

            if (ProductionSystem.BuildingCosts.TryGetValue("RaiderLair", out var cost))
            {
                TotalConstructionTime = cost.time * 60;
                ConstructionTimeRemaining = cost.time * 60;
            }
            else
            {
                TotalConstructionTime = 40 * 60;
                ConstructionTimeRemaining = 40 * 60;
            }

            CurrentBuildingState = BuildingAnimationState.Constructing;
            ConstructionSpriteSheet = 0;

            Width = 96;  // 2x2 grid
            Height = 96;

            PosX = gridX * 32;
            PosY = gridY * 32;

            RallyX = PosX + 32;
            RallyY = PosY + 80;
        }
    }
    // ===== BRIGAND (HUMAN ADVANCED INFANTRY) =====
    public class Brigand : WarUnit
    {
        public Brigand(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Brigand_Human";
            SpritePath = "/wc1sprites/units/Brigand001.png";

            MaxHP = 80;
            HP = 80;
            MoveSpeed = 4;
            AttackDamage = 12;
            AttackRange = 15;
            AttackSpeed = 55; // Slightly faster than Footman

            PosX = worldX;
            PosY = worldY;
        }
    }


    // ===== OGRE (ORC ADVANCED INFANTRY) =====
    public class Ogre : WarUnit
    {
        public Ogre(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Ogre_Orc";
            SpritePath = "/wc1sprites/units/Ogre001.png";

            MaxHP = 90;
            HP = 90;
            MoveSpeed = 3; // Slower but tankier
            AttackDamage = 14;
            AttackRange = 15;
            AttackSpeed = 65; // Slower attack speed

            PosX = worldX;
            PosY = worldY;
        }
    }
    // ===== PRIEST (HUMAN SPELLCASTER) =====
    // ===== PRIEST (HUMAN SPELLCASTER) =====
    public class Priest : WarUnit
    {
        public Priest(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Priest_Human";
            SpritePath = "/wc1sprites/units/Priest001.png";

            MaxHP = 40;
            HP = 40;
            MoveSpeed = 3;
            AttackDamage = 0;
            AttackRange = 0;

            MaxMana = 1000;
            Mana = 1000;
            ManaRegen = 1;

            AbilityName = "Heal";
            AbilityManaCost = 250;
            AbilityRange = 100;
            AbilityCooldownMax = 30;
            AutoCastEnabled = true;

            PosX = worldX;
            PosY = worldY;
        }

        // ✅ NEW: Spawn heal projectile (visual effect)
        public Projectile CreateHealProjectile(WarUnit target)
        {
            int startX = PosX + Width / 2;
            int startY = PosY + Height / 2;
            int targetX = target.PosX + target.Width / 2;
            int targetY = target.PosY + target.Height / 2;

            return new Projectile(startX, startY, targetX, targetY, 0, Race, target, "Heal");
        }
    }
    // ===== CULTIST (ORC SPELLCASTER) =====
    public class Cultist : WarUnit
    {
        public Cultist(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Cultist_Orc";
            SpritePath = "/wc1sprites/units/Cultist001.png";

            MaxHP = 40;
            HP = 40;
            MoveSpeed = 3;
            AttackDamage = 0;
            AttackRange = 0;

            MaxMana = 1000;
            Mana = 1000;
            ManaRegen = 1;

            AbilityName = "Bloodlust";
            AbilityManaCost = 250;
            AbilityRange = 100;
            AbilityCooldownMax = 30;
            AutoCastEnabled = true;

            PosX = worldX;
            PosY = worldY;
        }

        // ✅ NEW: Spawn bloodlust projectile (visual effect)
        public Projectile CreateBloodlustProjectile(WarUnit target)
        {
            int startX = PosX + Width / 2;
            int startY = PosY + Height / 2;
            int targetX = target.PosX + target.Width / 2;
            int targetY = target.PosY + target.Height / 2;

            return new Projectile(startX, startY, targetX, targetY, 0, Race, target, "Bloodlust");
        }
    }

    // ===== HORSE (HUMAN MOUNT - 48x48) =====
    public class Horse : WarUnit
    {
        public Horse(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Horse_Human";
            SpritePath = "/wc1sprites/units/Horse002.png";

            MaxHP = 50;
            HP = 50;
            MoveSpeed = 5; // Faster than infantry
            AttackDamage = 3;
            AttackRange = 32; // Ranged unit
            AttackSpeed = 90; // Slower attack speed

            Width = 48;  // ✅ 48x48 size
            Height = 48;
            // ✅ NEW: This is a mountable creature
            IsMountable = true;
            PosX = worldX;
            PosY = worldY;
        }
    }

    // ===== WOLF (ORC MOUNT - 48x48) =====
    public class Wolf : WarUnit
    {
        public Wolf(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Wolf_Orc";
            SpritePath = "/wc1sprites/units/Wolf002.png";

            MaxHP = 50;
            HP = 50;
            MoveSpeed = 5; // Faster than infantry
            AttackDamage = 4;
            AttackRange = 32; // Ranged unit
            AttackSpeed = 70; // Slower attack speed

            Width = 48;  // ✅ 48x48 size
            Height = 48;
            // ✅ NEW: This is a mountable creature
            IsMountable = true;
            PosX = worldX;
            PosY = worldY;
        }
    }

    // ===== KNIGHT (HUMAN MOUNTED UNIT - 64x64) =====
    public class Knight : WarUnit
    {
        public Knight(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "Knight_Human";
            SpritePath = "/wc1sprites/units/Knight002.png";

            MaxHP = 90;  // More HP than Footman
            HP = 90;
            MoveSpeed = 5; // Faster than Footman
            AttackDamage = 12; // More damage than Footman
            AttackRange = 15;
            AttackSpeed = 50; // Faster attacks

            Width = 64;  // ✅ 64x64 size (larger mounted unit)
            Height = 64;

            PosX = worldX;
            PosY = worldY;
        }
    }

    // ===== ORC RAIDER (ORC MOUNTED UNIT - 64x64) =====
    public class OrcRaider : WarUnit
    {
        public OrcRaider(int worldX, int worldY, RaceType race)
        {
            Race = race;
            PlaceholderName = "OrcRaider_Orc";
            SpritePath = "/wc1sprites/units/OrcRaider002.png";

            MaxHP = 90;  // More HP than Grunt
            HP = 90;
            MoveSpeed = 5; // Faster than Grunt
            AttackDamage = 12; // More damage than Grunt
            AttackRange = 15;
            AttackSpeed = 50; // Faster attacks

            Width = 64;  // ✅ 64x64 size (larger mounted unit)
            Height = 64;

            PosX = worldX;
            PosY = worldY;
        }
    }


    // ===== PROJECTILE ENTITY =====
    public class Projectile : WarEntity
    {
        public int TargetX { get; set; }
        public int TargetY { get; set; }
        public int Damage { get; set; }
        public WarUnit? TargetUnit { get; set; }
        public WarBuilding? TargetBuilding { get; set; }
        public RaceType OwnerRace { get; set; }
        public int Speed { get; set; } = 8; // Pixels per tick

        // ✅ NEW: Spell projectile flag
        public bool IsSpellProjectile { get; set; } = false;
        public string? SpellType { get; set; } = null; // "Heal", "Bloodlust"

        // ✅ UPDATED CONSTRUCTOR: Support both attack and spell projectiles
        public Projectile(int startX, int startY, int targetX, int targetY, int damage, RaceType race, WarEntity target, string? spellType = null)
        {
            PosX = startX;
            PosY = startY;
            TargetX = targetX;
            TargetY = targetY;
            Damage = damage;
            OwnerRace = race;
            SpellType = spellType;
            IsSpellProjectile = spellType != null;

            if (target is WarUnit unit)
                TargetUnit = unit;
            else if (target is WarBuilding building)
                TargetBuilding = building;

            // ✅ UPDATED: Set sprite based on projectile type
            if (IsSpellProjectile)
            {
                // Spell projectiles
                SpritePath = spellType switch
                {
                    "Heal" => "/iAssets/Healtiki001.png",        // Holy light effect
                    "Bloodlust" => "/wc1sprites/projectiles/BloodlustSpell001.png", // Red energy
                    _ => "/wc1sprites/projectiles/MagicSpell001.png"               // Generic magic
                };

                PlaceholderName = spellType ?? "Spell";
                Speed = 12; // Spells travel faster
            }
            else
            {
                // Attack projectiles (existing logic)
                SpritePath = race == RaceType.Human
                    ? "/wc1sprites/projectiles/Arrow001.png"
                    : "/wc1sprites/projectiles/ThrowAxe001.png";

                PlaceholderName = race == RaceType.Human ? "Arrow" : "Axe";
                Speed = 8;
            }

            Width = 16;
            Height = 16;
        }

        // Move projectile toward target (existing method - no changes needed)
        public bool MoveTowardTarget()
        {
            int deltaX = TargetX - PosX;
            int deltaY = TargetY - PosY;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (distance < Speed)
            {
                PosX = TargetX;
                PosY = TargetY;
                return true; // Arrived
            }

            double moveX = (deltaX / distance) * Speed;
            double moveY = (deltaY / distance) * Speed;

            PosX += (int)moveX;
            PosY += (int)moveY;

            return false; // Still traveling
        }
    }

}