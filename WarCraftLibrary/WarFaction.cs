using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace WarCraftLibrary
{
    // ===== PLAYER CONFIG (FOR MULTIPLAYER SETUP) =====
    public class PlayerConfig
    {
        public int SlotIndex { get; set; }          // 0-15
        public string Name { get; set; } = "";
        public RaceType Race { get; set; }
        public int Team { get; set; } = 0;          // 0 = FFA, 1-4 = team number
        public (int gridX, int gridY) SpawnPosition { get; set; }
        public bool IsHuman { get; set; } = false;  // Slot 0 only
        public Faction? Faction { get; set; }       // Faction data (resources, upgrades, etc.)
    }
    // ===== RACE TYPES =====
    public enum RaceType
    {
        Human,
        Orc,
        Undead,    // Placeholder for future
        Neutral
    }

    public class Faction
    {
        public RaceType Race { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string BannerPath { get; set; } = "";
        public string IconPath { get; set; } = "";
        public bool IsPlayable { get; set; } = true;

        // Starting resources
        public int StartingGold { get; set; } = 2000;
        public int StartingLumber { get; set; } = 1000;

        // AI resources (for AI faction)
        public int Gold { get; set; } = 2000;
        public int Lumber { get; set; } = 1000;

        // Population
        public int CurrentPop { get; set; } = 5;
        public int MaxPop { get; set; } = 5;

        // ✅ NEW: RANGED UNIT UPGRADES (Lumber Mill)
        public int RangedDamageLevel { get; set; } = 0;  // 0-3 (each level adds +1 damage)
        public int RangedArmorLevel { get; set; } = 0;   // 0-3 (each level adds +1 HP)

        // ✅ NEW: MELEE UNIT UPGRADES (Blacksmith - for Phase 2)
        public int MeleeDamageLevel { get; set; } = 0;   // 0-3 (each level adds +1 damage)
        public int MeleeArmorLevel { get; set; } = 0;    // 0-3 (each level adds +1 HP)


        // ✅ NEW: MOUNT UPGRADES
        public bool HasMountedCombatUpgrade { get; set; } = false;  // Human - unlocks Knight transformation
        public bool HasMountedWarfareUpgrade { get; set; } = false; // Orc - unlocks Raider transformation

        // ✅ NEW: Helper methods to get upgrade costs
        public (int gold, int lumber) GetRangedDamageUpgradeCost()
        {
            return RangedDamageLevel switch
            {
                0 => (800, 100),   // Level 1
                1 => (1600, 200),  // Level 2
                2 => (2400, 300),  // Level 3
                _ => (0, 0)        // Max level
            };
        }

        public (int gold, int lumber) GetRangedArmorUpgradeCost()
        {
            return RangedArmorLevel switch
            {
                0 => (600, 150),   // Level 1
                1 => (1200, 300),  // Level 2
                2 => (1800, 450),  // Level 3
                _ => (0, 0)        // Max level
            };
        }

        // ✅ NEW: MELEE UPGRADE COSTS
        public (int gold, int lumber) GetMeleeDamageUpgradeCost()
        {
            return MeleeDamageLevel switch
            {
                0 => (800, 100),   // Level 1
                1 => (1600, 200),  // Level 2
                2 => (2400, 300),  // Level 3
                _ => (0, 0)        // Max level
            };
        }

        public (int gold, int lumber) GetMeleeArmorUpgradeCost()
        {
            return MeleeArmorLevel switch
            {
                0 => (600, 150),   // Level 1
                1 => (1200, 300),  // Level 2
                2 => (1800, 450),  // Level 3
                _ => (0, 0)        // Max level
            };
        }
        // ✅ NEW: MOUNT UPGRADE COSTS
        public (int gold, int lumber) GetMountedCombatUpgradeCost()
        {
            // One-time research (not leveled)
            return HasMountedCombatUpgrade ? (0, 0) : (1000, 500);
        }

        public (int gold, int lumber) GetMountedWarfareUpgradeCost()
        {
            // One-time research (not leveled)
            return HasMountedWarfareUpgrade ? (0, 0) : (1000, 500);
        }
        // ✅ NEW: Apply upgrades to units
        public void ApplyUpgradesToUnit(WarUnit unit)
        {
            // Apply ranged upgrades to archers/axe throwers
            if (unit is Archer || unit is TrollAxeThrower)
            {
                unit.AttackDamage += RangedDamageLevel;
                unit.MaxHP += RangedArmorLevel;
                unit.HP = Math.Min(unit.HP + RangedArmorLevel, unit.MaxHP); // Heal if upgraded
            }

            // ✅ UPDATED: Apply melee upgrades to all melee units
            if (unit is Footman || unit is Brigand || unit is Ogre)
            {
                unit.AttackDamage += MeleeDamageLevel;
                unit.MaxHP += MeleeArmorLevel;
                unit.HP = Math.Min(unit.HP + MeleeArmorLevel, unit.MaxHP);
            }
        }
    }

    // ===== FACTION LIBRARY =====
    public static class FactionLibrary
    {
        public static List<Faction> AvailableFactions = new()
    {
        new Faction
        {
            Race = RaceType.Human,
            Name = "Kingdom of Azeroth",
            Description = "The noble humans defend their lands with knights and paladins. Masters of architecture and holy magic.",
            BannerPath = "/wc1sprites/factions/HumanBanner001.png",
            IconPath = "/wc1sprites/factions/human_icon.png",
            IsPlayable = true
        },

        new Faction
        {
            Race = RaceType.Orc,
            Name = "Orcish Horde",
            Description = "Brutal orc warriors from the dark portal. Savage fighters with powerful shamanic magic.",
            BannerPath = "/wc1sprites/factions/OrcBanner001.png",
            IconPath = "/wc1sprites/factions/orc_icon.png",
            IsPlayable = true
        },

        new Faction
        {
            Race = RaceType.Undead,
            Name = "Scourge",
            Description = "COMING SOON - The undead legions rise from their graves.",
            BannerPath = "/wc1sprites/factions/undead_banner.png",
            IconPath = "/wc1sprites/factions/undead_icon.png",
            IsPlayable = false // Not implemented yet
        }
    };

        public static Faction GetFaction(RaceType race)
        {
            return AvailableFactions.First(f => f.Race == race);
        }
    }

    // ===== GAME MAP CLASS =====
    public class GameMap
    {
        public string MapId { get; set; } = "";
        public string MapName { get; set; } = "";
        public string Description { get; set; } = "";
        public string TilesetPath { get; set; } = "";
        public string PreviewPath { get; set; } = "";
        public int Width { get; set; } = 4096;
        public int Height { get; set; } = 4096;

        // Spawn locations (grid coordinates)
        public (int x, int y) PlayerSpawn { get; set; } = (16, 16);
        public (int x, int y) AISpawn { get; set; } = (112, 112);
    }

    // ===== MAP LIBRARY =====
    public static class MapLibrary
    {
        public static List<GameMap> AvailableMaps = new()
    {
        new GameMap
        {
            MapId = "forest",
            MapName = "Forest of Elwynn",
            Description = "A lush forest valley rich with gold and timber. Ideal for new commanders.",
           // TilesetPath = "/wc1sprites/tilesets/forest_4096004.png",
            TilesetPath = "/wc1sprites/tilesets/TestMap0004.png",
            // also for default in wargameervice.cs will change
            PreviewPath = "/wc1sprites/tilesets/forest_4096004.png",
            PlayerSpawn = (16, 16),   // Top-left area
            AISpawn = (112, 112)       // Bottom-right area
        },

        new GameMap
        {
            MapId = "swamp",
            MapName = "Swamp of Sorrows",
            Description = "Dark wetlands shrouded in fog. Treacherous terrain with scattered resources.",
                   TilesetPath = "/wc1sprites/tilesets/TestMap0005.png",
            PreviewPath = "/wc1sprites/tilesets/swamp_4096004.png",
            PlayerSpawn = (8, 8),
            AISpawn = (108, 108)
        },

        new GameMap
        {
            MapId = "dungeon",
            MapName = "Dead Mines",
            Description = "Abandoned mines filled with darkness. High risk, high reward gold veins.",
                 TilesetPath = "/wc1sprites/tilesets/TestMap0006.png",
            PreviewPath = "/wc1sprites/tilesets/Mines_4096004.png",
            PlayerSpawn = (12, 12),
            AISpawn = (116, 116)
        }
    };

        public static GameMap GetMap(string mapId)
        {
            return AvailableMaps.First(m => m.MapId == mapId);
        }


        // ===== ENHANCED AI CONTROLLER =====
        public static class AIController
        {
            private static int aiTickCounter = 0;
            private const int AI_THINK_INTERVAL = 180; // Think every 3 seconds

            // AI difficulty scaling
            private static int aiGameAge = 0; // Increases over time
            private const int EARLY_GAME = 1800;  // 0-30 seconds
            private const int MID_GAME = 5400;    // 30-90 seconds
            private const int LATE_GAME = 10800;  // 90+ seconds

            public static void TickAI(WarGameService game, Faction aiFaction)
            {
                if (aiFaction == null) return;

                aiTickCounter++;
                aiGameAge++;

                if (aiTickCounter >= AI_THINK_INTERVAL)
                {
                    aiTickCounter = 0;
                    MakeAIDecisions(game, aiFaction);
                }
                ManageSpellcasters(game, aiFaction);
                ManageGathering(game, aiFaction);
                ManageAttacks(game, aiFaction);
                ManageMounting(game, aiFaction);
            }
            private static void MakeAIDecisions(WarGameService game, Faction aiFaction)
            {
                // ✅ NEW: Get AI player info
                var aiPlayer = game.Players.FirstOrDefault(p => p.Faction == aiFaction);
                if (aiPlayer == null)
                {
                    Console.WriteLine("⚠️ AI player not found in player list");
                    return;
                }

                int aiPlayerIndex = aiPlayer.SlotIndex;
                int aiTeam = aiPlayer.Team;
                var aiRace = aiFaction.Race;

                // Get AI base building (Town Hall or Castle)
                var aiBase = WarRegistry.Buildings
                    .FirstOrDefault(b => b.OwnerPlayerIndex == aiPlayerIndex && (b is TownHall || b is Castle));

                if (aiBase == null) return;

                // ✅ NEW: Get AI unit counts (only units THIS AI owns)
                var aiPeons = WarRegistry.Units.OfType<Peasant>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex).ToList();
                var aiFootmen = WarRegistry.Units.OfType<Footman>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex).ToList();
                var aiArchers = WarRegistry.Units.OfType<Archer>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex).ToList();
                var aiAxeThrowers = WarRegistry.Units.OfType<TrollAxeThrower>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex).ToList();
                var aiBrigands = WarRegistry.Units.OfType<Brigand>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex).ToList();
                var aiOgres = WarRegistry.Units.OfType<Ogre>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex).ToList();

                // ✅ NEW: Get AI buildings (only buildings THIS AI owns)
                var aiBarracks = WarRegistry.Buildings.OfType<Barracks>()
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                var aiFarms = WarRegistry.Buildings.OfType<Farm>()
                    .Where(f => f.OwnerPlayerIndex == aiPlayerIndex && f.IsConstructed).ToList();
                var aiBlacksmiths = WarRegistry.Buildings.OfType<Blacksmith>()
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                var aiLumberMills = WarRegistry.Buildings.OfType<LumberMill>()
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                var aiArcheryRanges = WarRegistry.Buildings.OfType<ArcheryRange>()
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();

                // ===== PRIORITY 1: ECONOMY =====

                // Build farms if population capped
                if (aiFaction.CurrentPop >= aiFaction.MaxPop - 2 && aiFaction.Gold >= 500 && aiFaction.Lumber >= 250)
                {
                    var placement = FindAIBuildingPlacement(game, aiBase, 2, "Farm");
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newFarm = new Farm(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newFarm.OwnerFaction = aiFaction;
                        newFarm.OwnerPlayerIndex = aiPlayerIndex;
                        newFarm.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newFarm);
                        aiFaction.Gold -= 500;
                        aiFaction.Lumber -= 250;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Building Farm at ({gridX}, {gridY})");
                    }
                }

                // Train workers (aim for 8-12 depending on game age)
                int targetWorkers = aiGameAge < EARLY_GAME ? 8 : (aiGameAge < MID_GAME ? 10 : 12);
                if (aiPeons.Count < targetWorkers && aiFaction.Gold >= 400)
                {
                    string workerType = aiRace == RaceType.Human ? "Peasant" : "Peon";
                    if (ProductionSystem.QueueUnit(aiBase, workerType, game, true))
                    {
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training {workerType} ({aiPeons.Count + 1}/{targetWorkers})");
                    }
                }

                // ===== PRIORITY 2: TECH BUILDINGS =====

                // Build Lumber Mill (if none, early priority)
                if (aiLumberMills.Count == 0 && aiFaction.Gold >= 600 && aiFaction.Lumber >= 450)
                {
                    var placement = FindAIBuildingPlacement(game, aiBase, 3, "LumberMill");
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newLumberMill = new LumberMill(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newLumberMill.OwnerFaction = aiFaction;
                        newLumberMill.OwnerPlayerIndex = aiPlayerIndex;
                        newLumberMill.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newLumberMill);
                        aiFaction.Gold -= 600;
                        aiFaction.Lumber -= 450;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Building Lumber Mill at ({gridX}, {gridY})");
                    }
                }

                // Build Blacksmith (if none, mid-game priority)
                if (aiBlacksmiths.Count == 0 && aiFaction.Gold >= 800 && aiFaction.Lumber >= 450 && aiGameAge > EARLY_GAME)
                {
                    var placement = FindAIBuildingPlacement(game, aiBase, 3, "Blacksmith");
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newBlacksmith = new Blacksmith(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newBlacksmith.OwnerFaction = aiFaction;
                        newBlacksmith.OwnerPlayerIndex = aiPlayerIndex;
                        newBlacksmith.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newBlacksmith);
                        aiFaction.Gold -= 800;
                        aiFaction.Lumber -= 450;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Building Blacksmith at ({gridX}, {gridY})");
                    }
                }

                // Build Barracks (up to 2)
                if (aiBarracks.Count < 2 && aiFaction.Gold >= 700 && aiFaction.Lumber >= 450)
                {
                    var placement = FindAIBuildingPlacement(game, aiBase, 4, "Barracks");
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newBarracks = new Barracks(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newBarracks.OwnerFaction = aiFaction;
                        newBarracks.OwnerPlayerIndex = aiPlayerIndex;
                        newBarracks.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newBarracks);
                        aiFaction.Gold -= 700;
                        aiFaction.Lumber -= 450;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Building Barracks #{aiBarracks.Count + 1} at ({gridX}, {gridY})");
                    }
                }

                // Build Archery Range (if Lumber Mill exists, mid-game)
                if (aiArcheryRanges.Count == 0 && aiLumberMills.Count > 0 && aiFaction.Gold >= 800 && aiFaction.Lumber >= 400 && aiGameAge > EARLY_GAME)
                {
                    var placement = FindAIBuildingPlacement(game, aiBase, 4, "ArcheryRange");
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newArchery = new ArcheryRange(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newArchery.OwnerFaction = aiFaction;
                        newArchery.OwnerPlayerIndex = aiPlayerIndex;
                        newArchery.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newArchery);
                        aiFaction.Gold -= 800;
                        aiFaction.Lumber -= 400;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Building Archery Range at ({gridX}, {gridY})");
                    }
                }

                // Build Church/Cultist Hut (if none, mid-game priority for spellcasters)
                var aiMagicBuildings = aiRace == RaceType.Human
                    ? WarRegistry.Buildings.OfType<Church>().Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).Count()
                    : WarRegistry.Buildings.OfType<CultistHut>().Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).Count();

                if (aiMagicBuildings == 0 && aiFaction.Gold >= 800 && aiFaction.Lumber >= 450 && aiGameAge > EARLY_GAME)
                {
                    string magicBuildingType = aiRace == RaceType.Human ? "Church" : "CultistHut";
                    var placement = FindAIBuildingPlacement(game, aiBase, 3, magicBuildingType);

                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;

                        WarBuilding newMagicBuilding = aiRace == RaceType.Human
                            ? new Church(gridX, gridY, aiRace)
                            : new CultistHut(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newMagicBuilding.OwnerFaction = aiFaction;
                        newMagicBuilding.OwnerPlayerIndex = aiPlayerIndex;
                        newMagicBuilding.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newMagicBuilding);
                        aiFaction.Gold -= 800;
                        aiFaction.Lumber -= 450;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Building {magicBuildingType} at ({gridX}, {gridY})");
                    }
                }

                // ===== PRIORITY 2.5: DEFENSIVE STRUCTURES (MID-LATE GAME) =====

                // Build Wood Towers (mid-game defense, max 3)
                var aiWoodTowers = WarRegistry.Buildings.OfType<WoodTower>()
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                if (aiWoodTowers.Count < 3 && aiFaction.Gold >= 500 && aiFaction.Lumber >= 300 && aiGameAge > MID_GAME)
                {
                    var placement = FindAITowerPlacement(game, aiBase);
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newTower = new WoodTower(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newTower.OwnerFaction = aiFaction;
                        newTower.OwnerPlayerIndex = aiPlayerIndex;
                        newTower.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newTower);
                        aiFaction.Gold -= 500;
                        aiFaction.Lumber -= 300;
                        Console.WriteLine($"🗼 AI Player {aiPlayerIndex}: Building Wood Tower at ({gridX}, {gridY})");
                    }
                }

                // Upgrade to Stone Towers (late game, if has Castle)
                var hasCastle = aiBase is Castle;
                var aiStoneTowers = WarRegistry.Buildings.OfType<StoneTower>()
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                if (hasCastle && aiStoneTowers.Count < 4 && aiFaction.Gold >= 800 && aiFaction.Lumber >= 500 && aiGameAge > LATE_GAME)
                {
                    var placement = FindAITowerPlacement(game, aiBase);
                    if (placement != null)
                    {
                        var (gridX, gridY) = placement.Value;
                        var newStoneTower = new StoneTower(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newStoneTower.OwnerFaction = aiFaction;
                        newStoneTower.OwnerPlayerIndex = aiPlayerIndex;
                        newStoneTower.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newStoneTower);
                        aiFaction.Gold -= 800;
                        aiFaction.Lumber -= 500;
                        Console.WriteLine($"🏰 AI Player {aiPlayerIndex}: Building Stone Tower at ({gridX}, {gridY})");
                    }
                }

                // Build Wooden Walls (strategic gaps for entry/exit, late game only)
                var aiWalls = WarRegistry.Buildings
                    .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && (b is WoodenWall || b is StoneWall)).ToList();
                if (aiWalls.Count < 15 && aiFaction.Gold >= 100 && aiFaction.Lumber >= 50 && aiGameAge > LATE_GAME)
                {
                    var wallPlacements = FindAIWallPlacements(game, aiBase, 5);
                    foreach (var (gridX, gridY) in wallPlacements)
                    {
                        var newWall = new WoodenWall(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newWall.OwnerFaction = aiFaction;
                        newWall.OwnerPlayerIndex = aiPlayerIndex;
                        newWall.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newWall);
                        aiFaction.Gold -= 100;
                        aiFaction.Lumber -= 50;
                        Console.WriteLine($"🧱 AI Player {aiPlayerIndex}: Building Wooden Wall at ({gridX}, {gridY})");
                    }
                }

                // Build Roads (connect base to resources, low priority)
                var aiRoads = WarRegistry.Buildings.OfType<Road>()
                    .Where(r => r.OwnerPlayerIndex == aiPlayerIndex).ToList();
                if (aiRoads.Count < 20 && aiFaction.Gold >= 50 && aiFaction.Lumber >= 25 && aiGameAge > MID_GAME && aiFaction.Gold > 3000)
                {
                    var roadPlacement = FindAIRoadPlacement(game, aiBase);
                    if (roadPlacement != null)
                    {
                        var (gridX, gridY) = roadPlacement.Value;
                        var newRoad = new Road(gridX, gridY, aiRace);

                        // ✅ NEW: Assign ownership
                        newRoad.OwnerFaction = aiFaction;
                        newRoad.OwnerPlayerIndex = aiPlayerIndex;
                        newRoad.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Add(newRoad);
                        aiFaction.Gold -= 50;
                        aiFaction.Lumber -= 25;
                        Console.WriteLine($"🛤️ AI Player {aiPlayerIndex}: Building Road at ({gridX}, {gridY})");
                    }
                }

                // ===== PRIORITY 3: UPGRADES =====

                // Blacksmith upgrades (prioritize damage first)
                if (aiBlacksmiths.Count > 0)
                {
                    // Melee damage upgrades
                    if (aiFaction.MeleeDamageLevel < 3)
                    {
                        var (gold, lumber) = aiFaction.GetMeleeDamageUpgradeCost();
                        if (aiFaction.Gold >= gold && aiFaction.Lumber >= lumber && aiGameAge > MID_GAME)
                        {
                            aiFaction.Gold -= gold;
                            aiFaction.Lumber -= lumber;
                            aiFaction.MeleeDamageLevel++;

                            // ✅ NEW: Apply to existing units THIS AI OWNS
                            foreach (var unit in WarRegistry.Units.Where(u => u.OwnerPlayerIndex == aiPlayerIndex && (u is Footman || u is Brigand || u is Ogre)))
                            {
                                unit.AttackDamage++;
                            }
                            Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Upgraded Melee Damage to level {aiFaction.MeleeDamageLevel}");
                        }
                    }

                    // Melee armor upgrades
                    if (aiFaction.MeleeArmorLevel < 3 && aiFaction.MeleeDamageLevel >= 2)
                    {
                        var (gold, lumber) = aiFaction.GetMeleeArmorUpgradeCost();
                        if (aiFaction.Gold >= gold && aiFaction.Lumber >= lumber && aiGameAge > LATE_GAME)
                        {
                            aiFaction.Gold -= gold;
                            aiFaction.Lumber -= lumber;
                            aiFaction.MeleeArmorLevel++;

                            foreach (var unit in WarRegistry.Units.Where(u => u.OwnerPlayerIndex == aiPlayerIndex && (u is Footman || u is Brigand || u is Ogre)))
                            {
                                unit.MaxHP++;
                                unit.HP = Math.Min(unit.HP + 1, unit.MaxHP);
                            }
                            Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Upgraded Melee Armor to level {aiFaction.MeleeArmorLevel}");
                        }
                    }
                }

                // Lumber Mill upgrades
                if (aiLumberMills.Count > 0 && aiArcheryRanges.Count > 0)
                {
                    // Ranged damage upgrades
                    if (aiFaction.RangedDamageLevel < 3)
                    {
                        var (gold, lumber) = aiFaction.GetRangedDamageUpgradeCost();
                        if (aiFaction.Gold >= gold && aiFaction.Lumber >= lumber && aiGameAge > MID_GAME)
                        {
                            aiFaction.Gold -= gold;
                            aiFaction.Lumber -= lumber;
                            aiFaction.RangedDamageLevel++;

                            foreach (var unit in WarRegistry.Units.Where(u => u.OwnerPlayerIndex == aiPlayerIndex && (u is Archer || u is TrollAxeThrower)))
                            {
                                unit.AttackDamage++;
                            }
                            Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Upgraded Ranged Damage to level {aiFaction.RangedDamageLevel}");
                        }
                    }

                    // Ranged armor upgrades
                    if (aiFaction.RangedArmorLevel < 3 && aiFaction.RangedDamageLevel >= 2)
                    {
                        var (gold, lumber) = aiFaction.GetRangedArmorUpgradeCost();
                        if (aiFaction.Gold >= gold && aiFaction.Lumber >= lumber && aiGameAge > LATE_GAME)
                        {
                            aiFaction.Gold -= gold;
                            aiFaction.Lumber -= lumber;
                            aiFaction.RangedArmorLevel++;

                            foreach (var unit in WarRegistry.Units.Where(u => u.OwnerPlayerIndex == aiPlayerIndex && (u is Archer || u is TrollAxeThrower)))
                            {
                                unit.MaxHP++;
                                unit.HP = Math.Min(unit.HP + 1, unit.MaxHP);
                            }
                            Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Upgraded Ranged Armor to level {aiFaction.RangedArmorLevel}");
                        }
                    }
                }


                // Build Stables (Human) if has Castle, before KnightsHold
                if (aiRace == RaceType.Human && hasCastle)
                {
                    var aiStables = WarRegistry.Buildings.OfType<Stables>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();

                    if (aiStables.Count == 0 && aiFaction.Gold >= 800 && aiFaction.Lumber >= 400 && aiGameAge > MID_GAME)
                    {
                        var placement = FindAIBuildingPlacement(game, aiBase, 2, "Stables");
                        if (placement != null)
                        {
                            var (gridX, gridY) = placement.Value;
                            var newStables = new Stables(gridX, gridY, aiRace);

                            newStables.OwnerFaction = aiFaction;
                            newStables.OwnerPlayerIndex = aiPlayerIndex;
                            newStables.OwnerTeam = aiTeam;

                            WarRegistry.Buildings.Add(newStables);
                            aiFaction.Gold -= 800;
                            aiFaction.Lumber -= 400;
                            Console.WriteLine($"🐴 AI Player {aiPlayerIndex}: Building Stables at ({gridX}, {gridY})");
                        }
                    }
                }

                // Build Pen (Orc) if has Fortress, before RaiderLair
                if (aiRace == RaceType.Orc && hasCastle)
                {
                    var aiPens = WarRegistry.Buildings.OfType<Pen>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();

                    if (aiPens.Count == 0 && aiFaction.Gold >= 800 && aiFaction.Lumber >= 400 && aiGameAge > MID_GAME)
                    {
                        var placement = FindAIBuildingPlacement(game, aiBase, 2, "Pen");
                        if (placement != null)
                        {
                            var (gridX, gridY) = placement.Value;
                            var newPen = new Pen(gridX, gridY, aiRace);

                            newPen.OwnerFaction = aiFaction;
                            newPen.OwnerPlayerIndex = aiPlayerIndex;
                            newPen.OwnerTeam = aiTeam;

                            WarRegistry.Buildings.Add(newPen);
                            aiFaction.Gold -= 800;
                            aiFaction.Lumber -= 400;
                            Console.WriteLine($"🐺 AI Player {aiPlayerIndex}: Building Pen at ({gridX}, {gridY})");
                        }
                    }
                }

                // Build Knights Hold (Human Tier 2) if has Stables
                if (aiRace == RaceType.Human && hasCastle)
                {
                    var aiStables = WarRegistry.Buildings.OfType<Stables>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).Count();
                    var aiKnightsHolds = WarRegistry.Buildings.OfType<KnightsHold>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();

                    if (aiStables > 0 && aiKnightsHolds.Count == 0 && aiFaction.Gold >= 1000 && aiFaction.Lumber >= 600 && aiGameAge > LATE_GAME)
                    {
                        var placement = FindAIBuildingPlacement(game, aiBase, 3, "KnightsHold");
                        if (placement != null)
                        {
                            var (gridX, gridY) = placement.Value;
                            var newKnightsHold = new KnightsHold(gridX, gridY, aiRace);

                            newKnightsHold.OwnerFaction = aiFaction;
                            newKnightsHold.OwnerPlayerIndex = aiPlayerIndex;
                            newKnightsHold.OwnerTeam = aiTeam;

                            WarRegistry.Buildings.Add(newKnightsHold);
                            aiFaction.Gold -= 1000;
                            aiFaction.Lumber -= 600;
                            Console.WriteLine($"⚔️ AI Player {aiPlayerIndex}: Building Knights Hold at ({gridX}, {gridY})");
                        }
                    }
                }

                // Build Raider Lair (Orc Tier 2) if has Pen
                if (aiRace == RaceType.Orc && hasCastle)
                {
                    var aiPens = WarRegistry.Buildings.OfType<Pen>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).Count();
                    var aiRaiderLairs = WarRegistry.Buildings.OfType<RaiderLair>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();

                    if (aiPens > 0 && aiRaiderLairs.Count == 0 && aiFaction.Gold >= 1000 && aiFaction.Lumber >= 600 && aiGameAge > LATE_GAME)
                    {
                        var placement = FindAIBuildingPlacement(game, aiBase, 3, "RaiderLair");
                        if (placement != null)
                        {
                            var (gridX, gridY) = placement.Value;
                            var newRaiderLair = new RaiderLair(gridX, gridY, aiRace);

                            newRaiderLair.OwnerFaction = aiFaction;
                            newRaiderLair.OwnerPlayerIndex = aiPlayerIndex;
                            newRaiderLair.OwnerTeam = aiTeam;

                            WarRegistry.Buildings.Add(newRaiderLair);
                            aiFaction.Gold -= 1000;
                            aiFaction.Lumber -= 600;
                            Console.WriteLine($"🪓 AI Player {aiPlayerIndex}: Building Raider Lair at ({gridX}, {gridY})");
                        }
                    }
                }

                // Research Mounted Combat (Human) if has Stables
                if (aiRace == RaceType.Human && !aiFaction.HasMountedCombatUpgrade)
                {
                    var aiStables = WarRegistry.Buildings.OfType<Stables>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).Count();

                    if (aiStables > 0 && aiFaction.Gold >= 1000 && aiFaction.Lumber >= 500 && aiGameAge > MID_GAME)
                    {
                        aiFaction.Gold -= 1000;
                        aiFaction.Lumber -= 500;
                        aiFaction.HasMountedCombatUpgrade = true;

                        // Enable mounting for all Footmen
                        foreach (var unit in WarRegistry.Units.OfType<Footman>().Where(u => u.OwnerPlayerIndex == aiPlayerIndex))
                        {
                            unit.CanMount = true;
                        }

                        Console.WriteLine($"🐴 AI Player {aiPlayerIndex}: Researched Mounted Combat!");
                    }
                }

                // Research Mounted Warfare (Orc) if has Pen
                if (aiRace == RaceType.Orc && !aiFaction.HasMountedWarfareUpgrade)
                {
                    var aiPens = WarRegistry.Buildings.OfType<Pen>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).Count();

                    if (aiPens > 0 && aiFaction.Gold >= 1000 && aiFaction.Lumber >= 500 && aiGameAge > MID_GAME)
                    {
                        aiFaction.Gold -= 1000;
                        aiFaction.Lumber -= 500;
                        aiFaction.HasMountedWarfareUpgrade = true;

                        // Enable mounting for all Grunts
                        foreach (var unit in WarRegistry.Units.OfType<Footman>().Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.Race == RaceType.Orc))
                        {
                            unit.CanMount = true;
                        }

                        Console.WriteLine($"🐺 AI Player {aiPlayerIndex}: Researched Mounted Warfare!");
                    }
                }
                // ===== PRIORITY 4: MILITARY PRODUCTION =====

                // Early game: Focus on basic infantry
                if (aiGameAge < EARLY_GAME)
                {
                    foreach (var barracks in aiBarracks)
                    {
                        if (aiBlacksmiths.Count > 0 && aiFaction.Gold >= 600 && aiFootmen.Count < 3)
                        {
                            string soldierType = aiRace == RaceType.Human ? "Footman" : "Grunt";
                            if (ProductionSystem.QueueUnit(barracks, soldierType, game, true))
                            {
                                Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training {soldierType} (Early Game Defense)");
                            }
                        }
                    }
                }

                // Mid game: Mix of infantry and ranged
                else if (aiGameAge < MID_GAME)
                {
                    foreach (var barracks in aiBarracks)
                    {
                        if (aiBlacksmiths.Count > 0 && aiFaction.Gold >= 600 && aiFootmen.Count < 6)
                        {
                            string soldierType = aiRace == RaceType.Human ? "Footman" : "Grunt";
                            if (ProductionSystem.QueueUnit(barracks, soldierType, game, true))
                            {
                                Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training {soldierType} (Mid Game Army)");
                            }
                        }
                    }

                    // Train archers
                    foreach (var archery in aiArcheryRanges)
                    {
                        if (aiFaction.Gold >= 500 && aiFaction.Lumber >= 50 && aiArchers.Count < 4)
                        {
                            string archerType = aiRace == RaceType.Human ? "Archer" : "TrollAxeThrower";
                            if (ProductionSystem.QueueUnit(archery, archerType, game, true))
                            {
                                Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training {archerType} (Ranged Support)");
                            }
                        }
                    }
                }
                // Late game: Elite units and mass production
                else
                {
                    foreach (var barracks in aiBarracks)
                    {
                        // Train elite infantry
                        if (aiBlacksmiths.Count > 0)
                        {
                            if (aiRace == RaceType.Human && aiFaction.Gold >= 750 && aiFaction.Lumber >= 100 && aiBrigands.Count < 8)
                            {
                                if (ProductionSystem.QueueUnit(barracks, "Brigand", game, true))
                                {
                                    Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training Brigand (Elite Infantry)");
                                }
                            }
                            else if (aiRace == RaceType.Orc && aiFaction.Gold >= 800 && aiFaction.Lumber >= 150 && aiOgres.Count < 8)
                            {
                                if (ProductionSystem.QueueUnit(barracks, "Ogre", game, true))
                                {
                                    Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training Ogre (Heavy Infantry)");
                                }
                            }
                            // Fallback to regular infantry
                            else if (aiFaction.Gold >= 600 && aiFootmen.Count < 10)
                            {
                                string soldierType = aiRace == RaceType.Human ? "Footman" : "Grunt";
                                if (ProductionSystem.QueueUnit(barracks, soldierType, game, true))
                                {
                                    Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training {soldierType} (Regular Infantry)");
                                }
                            }
                        }
                    }

                    // Mass archers
                    foreach (var archery in aiArcheryRanges)
                    {
                        if (aiFaction.Gold >= 500 && aiFaction.Lumber >= 50 && aiArchers.Count < 8)
                        {
                            string archerType = aiRace == RaceType.Human ? "Archer" : "TrollAxeThrower";
                            if (ProductionSystem.QueueUnit(archery, archerType, game, true))
                            {
                                Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training {archerType} (Mass Ranged)");
                            }
                        }
                    }
                }

                // Train spellcasters (if Church/Cultist Hut exists, mid-late game)
                if (aiGameAge > MID_GAME)
                {
                    if (aiRace == RaceType.Human)
                    {
                        var aiChurches = WarRegistry.Buildings.OfType<Church>()
                            .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                        var aiPriests = WarRegistry.Units.OfType<Priest>()
                            .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.State != UnitState.Dead).Count();

                        foreach (var church in aiChurches)
                        {
                            if (aiFaction.Gold >= 1000 && aiFaction.Lumber >= 50 && aiPriests < 3)
                            {
                                if (ProductionSystem.QueueUnit(church, "Priest", game, true))
                                {
                                    Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training Priest (Healer #{aiPriests + 1})");
                                }
                            }
                        }
                    }
                    else // Orc
                    {
                        var aiCultistHuts = WarRegistry.Buildings.OfType<CultistHut>()
                            .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                        var aiCultists = WarRegistry.Units.OfType<Cultist>()
                            .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.State != UnitState.Dead).Count();

                        foreach (var hut in aiCultistHuts)
                        {
                            if (aiFaction.Gold >= 1000 && aiFaction.Lumber >= 50 && aiCultists < 3)
                            {
                                if (ProductionSystem.QueueUnit(hut, "Cultist", game, true))
                                {
                                    Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: Training Cultist (Buffer #{aiCultists + 1})");
                                }
                            }
                        }
                    }
                }

                // ===== PRIORITY 5: EXPANSION =====
                // Train Horses (Human) if has Stables
                if (aiRace == RaceType.Human && aiGameAge > MID_GAME)
                {
                    var aiStables = WarRegistry.Buildings.OfType<Stables>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                    var aiHorses = WarRegistry.Units.OfType<Horse>()
                        .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.State != UnitState.Dead).Count();
                    var aiFootmen2 = WarRegistry.Units.OfType<Footman>()
                        .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.CanMount).Count();

                    // Keep 1 horse per mountable footman
                    foreach (var stables in aiStables)
                    {
                        if (aiHorses < aiFootmen2 && aiFaction.Gold >= 400 && aiFaction.Lumber >= 100)
                        {
                            if (ProductionSystem.QueueUnit(stables, "Horse", game, true))
                            {
                                Console.WriteLine($"🐴 AI Player {aiPlayerIndex}: Training Horse for mounting");
                            }
                        }
                    }
                }

                // Train Wolves (Orc) if has Pen
                if (aiRace == RaceType.Orc && aiGameAge > MID_GAME)
                {
                    var aiPens = WarRegistry.Buildings.OfType<Pen>()
                        .Where(b => b.OwnerPlayerIndex == aiPlayerIndex && b.IsConstructed).ToList();
                    var aiWolves = WarRegistry.Units.OfType<Wolf>()
                        .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.State != UnitState.Dead).Count();
                    var aiGrunts = WarRegistry.Units.OfType<Footman>()
                        .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.Race == RaceType.Orc && u.CanMount).Count();

                    // Keep 1 wolf per mountable grunt
                    foreach (var pen in aiPens)
                    {
                        if (aiWolves < aiGrunts && aiFaction.Gold >= 400 && aiFaction.Lumber >= 100)
                        {
                            if (ProductionSystem.QueueUnit(pen, "Wolf", game, true))
                            {
                                Console.WriteLine($"🐺 AI Player {aiPlayerIndex}: Training Wolf for mounting");
                            }
                        }
                    }
                }
                // Late game: Attempt Castle upgrade
                if (aiGameAge > LATE_GAME && aiBase is TownHall townHall && aiBarracks.Count > 0 && aiArcheryRanges.Count > 0)
                {
                    if (aiFaction.Gold >= 2000 && aiFaction.Lumber >= 1000)
                    {
                        var castle = new Castle(townHall);

                        // ✅ NEW: Transfer ownership
                        castle.OwnerFaction = aiFaction;
                        castle.OwnerPlayerIndex = aiPlayerIndex;
                        castle.OwnerTeam = aiTeam;

                        WarRegistry.Buildings.Remove(townHall);
                        WarRegistry.Buildings.Add(castle);
                        aiFaction.Gold -= 2000;
                        aiFaction.Lumber -= 1000;
                        Console.WriteLine($"🤖 AI Player {aiPlayerIndex}: UPGRADED TO CASTLE! 👑");
                    }
                }
            }

            private static (int gridX, int gridY)? FindAIBuildingPlacement(WarGameService game, WarBuilding nearBuilding, int buildingSize, string buildingType)
            {
                int baseGridX = nearBuilding.PosX / 32;
                int baseGridY = nearBuilding.PosY / 32;

                // Try concentric circles around base
                for (int radius = 6; radius < 25; radius += 2)
                {
                    for (int angle = 0; angle < 360; angle += 30)
                    {
                        int offsetX = (int)(radius * Math.Cos(angle * Math.PI / 180));
                        int offsetY = (int)(radius * Math.Sin(angle * Math.PI / 180));

                        int tryX = baseGridX + offsetX;
                        int tryY = baseGridY + offsetY;

                        if (game.CanPlaceBuilding(tryX, tryY, buildingType))
                        {
                            return (tryX, tryY);
                        }
                    }
                }

                return null;
            }
            // ===== SMART TOWER PLACEMENT (DEFENSIVE PERIMETER) =====
            private static (int gridX, int gridY)? FindAITowerPlacement(WarGameService game, WarBuilding nearBuilding)
            {
                int baseGridX = nearBuilding.PosX / 32;
                int baseGridY = nearBuilding.PosY / 32;

                // Place towers in cardinal directions around base (N, S, E, W, NE, NW, SE, SW)
                var directions = new[]
                {
(0, -15),   // North
(0, 15),    // South
(15, 0),    // East
(-15, 0),   // West
(10, -10),  // Northeast
(-10, -10), // Northwest
(10, 10),   // Southeast
(-10, 10)   // Southwest
};

                foreach (var (offsetX, offsetY) in directions)
                {
                    int tryX = baseGridX + offsetX;
                    int tryY = baseGridY + offsetY;

                    // Check if tower placement is valid (2x2 grid for 64x64 tower)
                    if (tryX >= 0 && tryY >= 0 && tryX + 2 < game.GridWidth && tryY + 2 < game.GridHeight)
                    {
                        // Check if no other towers nearby (avoid clustering)
                        bool tooClose = WarRegistry.Buildings
                            .Where(b => b is WoodTower || b is StoneTower)
                            .Any(t => Math.Abs(t.PosX / 32 - tryX) < 8 && Math.Abs(t.PosY / 32 - tryY) < 8);

                        if (!tooClose && game.CanPlaceBuilding(tryX, tryY, "WoodTower"))
                        {
                            return (tryX, tryY);
                        }
                    }
                }

                return null; // No valid tower spot found
            }
            // ===== SMART WALL PLACEMENT (WITH STRATEGIC GAPS) =====
            private static List<(int gridX, int gridY)> FindAIWallPlacements(WarGameService game, WarBuilding nearBuilding, int count)
            {
                var placements = new List<(int gridX, int gridY)>();
                int baseGridX = nearBuilding.PosX / 32;
                int baseGridY = nearBuilding.PosY / 32;

                // Build walls in a semi-circle (leave gaps at cardinal directions for entry/exit)
                int radius = 12; // Distance from base
                var angles = new[] { 30, 60, 120, 150, 210, 240, 300, 330 }; // Skip 0°, 90°, 180°, 270° for gaps

                foreach (int angle in angles)
                {
                    if (placements.Count >= count) break;

                    int offsetX = (int)(radius * Math.Cos(angle * Math.PI / 180));
                    int offsetY = (int)(radius * Math.Sin(angle * Math.PI / 180));

                    int tryX = baseGridX + offsetX;
                    int tryY = baseGridY + offsetY;

                    // Check if wall placement is valid (1x1 grid for 32x32 wall)
                    if (tryX >= 0 && tryY >= 0 && tryX < game.GridWidth && tryY < game.GridHeight)
                    {
                        // Don't block roads or towers
                        bool blocksImportant = WarRegistry.Buildings
                            .Any(b => (b is Road || b is WoodTower || b is StoneTower) &&
                                     Math.Abs(b.PosX / 32 - tryX) < 3 && Math.Abs(b.PosY / 32 - tryY) < 3);

                        if (!blocksImportant && game.CanPlaceBuilding(tryX, tryY, "WoodenWall"))
                        {
                            placements.Add((tryX, tryY));
                        }
                    }
                }

                return placements;
            }
            // ===== ROAD PLACEMENT (CONNECT BASE TO RESOURCES) =====
            private static (int gridX, int gridY)? FindAIRoadPlacement(WarGameService game, WarBuilding nearBuilding)
            {
                int baseGridX = nearBuilding.PosX / 32;
                int baseGridY = nearBuilding.PosY / 32;

                // Find nearest gold mine
                var nearestMine = WarRegistry.GoldMines
                    .OrderBy(m => Math.Abs(m.PosX / 32 - baseGridX) + Math.Abs(m.PosY / 32 - baseGridY))
                    .FirstOrDefault();

                if (nearestMine == null) return null;

                int mineGridX = nearestMine.PosX / 32;
                int mineGridY = nearestMine.PosY / 32;

                // Build road towards mine (simple pathfinding)
                int deltaX = mineGridX - baseGridX;
                int deltaY = mineGridY - baseGridY;

                // Move one step closer
                int stepX = deltaX != 0 ? baseGridX + Math.Sign(deltaX) * 3 : baseGridX;
                int stepY = deltaY != 0 ? baseGridY + Math.Sign(deltaY) * 3 : baseGridY;

                // Check if road already exists here
                bool roadExists = WarRegistry.Buildings
                    .OfType<Road>()
                    .Any(r => Math.Abs(r.PosX / 32 - stepX) < 2 && Math.Abs(r.PosY / 32 - stepY) < 2);

                if (!roadExists && game.CanPlaceBuilding(stepX, stepY, "Road"))
                {
                    return (stepX, stepY);
                }

                return null; // Road already exists or can't place
            }
            private static void ManageGathering(WarGameService game, Faction aiFaction)
            {
                var aiPlayer = game.Players.FirstOrDefault(p => p.Faction == aiFaction);
                if (aiPlayer == null) return;

                int aiPlayerIndex = aiPlayer.SlotIndex;

                // ===== HANDLE EXISTING SCOUTS =====
                var scoutingPeons = WarRegistry.Units
                    .OfType<Peasant>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex && u.State == UnitState.Scouting)
                    .ToList();

                foreach (var scout in scoutingPeons)
                {
                    bool reachedDestination =
                        scout.TargetX.HasValue && scout.TargetY.HasValue &&
                        Math.Abs(scout.PosX - scout.TargetX.Value) < 50 &&
                        Math.Abs(scout.PosY - scout.TargetY.Value) < 50;

                    if (reachedDestination || !scout.TargetX.HasValue)
                    {
                        scout.TargetX = Random.Shared.Next(100, game.MapWidth - 100);
                        scout.TargetY = Random.Shared.Next(100, game.MapHeight - 100);
                    }
                }

                // ===== RESUME WORKERS THAT JUST DROPPED OFF =====
                var idleButAssigned = WarRegistry.Units
                    .OfType<Peasant>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex &&
                                u.State == UnitState.Idle &&
                                (u.TargetMine != null || u.TargetTree != null))
                    .ToList();

                foreach (var peon in idleButAssigned)
                {
                    if (peon.TargetMine != null && !peon.TargetMine.IsDepleted)
                    {
                        peon.State = UnitState.GatheringGold;
                      //  peon.TargetX = peon.TargetMine.PosX + 32;
                      //  peon.TargetY = peon.TargetMine.PosY + 32;
                    }
                    else if (peon.TargetTree != null && !peon.TargetTree.IsChopped)
                    {
                        peon.State = UnitState.GatheringLumber;
                     //   peon.TargetX = peon.TargetTree.PosX + 16;
                      //  peon.TargetY = peon.TargetTree.PosY + 16;
                    }
                    else
                    {
                        peon.TargetMine = null;
                        peon.TargetTree = null;
                    }
                }

                // ===== GET TRULY IDLE PEASANTS =====
                var idlePeons = WarRegistry.Units
                    .OfType<Peasant>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex &&
                                u.State == UnitState.Idle &&
                                u.TargetMine == null &&
                                u.TargetTree == null)
                    .ToList();

                if (idlePeons.Count == 0) return;

                // ===== SPLIT INTO THIRDS =====
                int totalPeons = WarRegistry.Units
                    .OfType<Peasant>()
                    .Count(u => u.OwnerPlayerIndex == aiPlayerIndex);

                int scoutsTarget = Math.Max(1, totalPeons / 3);
                int goldTarget = Math.Max(1, totalPeons / 3);
                int lumberTarget = totalPeons - scoutsTarget - goldTarget;

                int currentScouts = scoutingPeons.Count;
                int currentGold = WarRegistry.Units
                    .OfType<Peasant>()
                    .Count(u => u.OwnerPlayerIndex == aiPlayerIndex && u.TargetMine != null);

                int currentLumber = WarRegistry.Units
                    .OfType<Peasant>()
                    .Count(u => u.OwnerPlayerIndex == aiPlayerIndex && u.TargetTree != null);

                // ===== ASSIGN SCOUTS =====
                int scoutsNeeded = scoutsTarget - currentScouts;
                for (int i = 0; i < scoutsNeeded && idlePeons.Count > 0; i++)
                {
                    var scout = idlePeons[0];
                    idlePeons.RemoveAt(0);

                    scout.State = UnitState.Scouting;
                    scout.TargetX = Random.Shared.Next(100, game.MapWidth - 100);
                    scout.TargetY = Random.Shared.Next(100, game.MapHeight - 100);
                }
                // ===== ASSIGN GOLD =====
                int goldNeeded = goldTarget - currentGold;
                for (int i = 0; i < goldNeeded && idlePeons.Count > 0; i++)
                {
                    var peon = idlePeons[0];
                    idlePeons.RemoveAt(0);

                    var mine = WarRegistry.GoldMines
                        .Where(m => !m.IsDepleted)
                        .OrderBy(m => DistanceTo(peon, m))
                        .FirstOrDefault();

                    if (mine == null) break;

                    peon.TargetMine = mine;
                    peon.TargetTree = null;
                    peon.State = UnitState.GatheringGold;
                    // ✅ REMOVE THESE TWO LINES:
                    // peon.TargetX = mine.PosX + 32;
                    // peon.TargetY = mine.PosY + 32;
                }

                // ===== ASSIGN LUMBER =====
                int lumberNeeded = lumberTarget - currentLumber;
                for (int i = 0; i < lumberNeeded && idlePeons.Count > 0; i++)
                {
                    var peon = idlePeons[0];
                    idlePeons.RemoveAt(0);

                    var tree = WarRegistry.Trees
                        .Where(t => !t.IsChopped)
                        .OrderBy(t => DistanceTo(peon, t))
                        .FirstOrDefault();

                    if (tree == null) break;

                    peon.TargetTree = tree;
                    peon.TargetMine = null;
                    peon.State = UnitState.GatheringLumber;
                    // ✅ REMOVE THESE TWO LINES:
                    // peon.TargetX = tree.PosX + 16;
                    // peon.TargetY = tree.PosY + 16;
                }
            }


            private static void ManageAttacks(WarGameService game, Faction aiFaction)
            {
                // ✅ NEW: Get AI player info
                var aiPlayer = game.Players.FirstOrDefault(p => p.Faction == aiFaction);
                if (aiPlayer == null) return;

                int aiPlayerIndex = aiPlayer.SlotIndex;
                int aiTeam = aiPlayer.Team;
                var aiRace = aiFaction.Race;

                // Get all AI military units (ONLY THIS AI'S UNITS)
                var aiSoldiers = WarRegistry.Units
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex &&
                                (u is Footman || u is Brigand || u is Ogre || u is Archer || u is TrollAxeThrower))
                    .ToList();

                // Determine aggression based on game age
                int aggressionRange = aiGameAge < EARLY_GAME ? 300 : (aiGameAge < MID_GAME ? 600 : 1200);
                bool shouldRush = aiGameAge > LATE_GAME && aiSoldiers.Count > 10;

                // ===== PEASANT DEFENSE (ALWAYS) =====
                var aiPeasants = WarRegistry.Units
                    .OfType<Peasant>()
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex)
                    .ToList();

                foreach (var peasant in aiPeasants)
                {
                    // Skip if already returning resources from a full load
                    if (peasant.State == UnitState.ReturningResources)
                        continue;

                    // Skip scouts - don't interrupt them!
                    if (peasant.State == UnitState.Scouting)
                        continue;

                    // ✅ NEW: Find nearby enemies (DIFFERENT TEAM)
                    var nearbyEnemy = WarRegistry.Units
                        .Where(u => u.OwnerTeam != aiTeam && u.HP > 0)
                        .OrderBy(u => DistanceTo(peasant, u))
                        .FirstOrDefault();

                    if (nearbyEnemy != null && DistanceTo(peasant, nearbyEnemy) < 400)
                    {
                        peasant.State = UnitState.Attacking;
                        peasant.AttackTarget = nearbyEnemy;
                        peasant.TargetX = nearbyEnemy.PosX;
                        peasant.TargetY = nearbyEnemy.PosY;
                        continue;
                    }

                    // Otherwise, idle peasants go back to gathering (but don't interrupt scouts!)
                    if (peasant.State != UnitState.GatheringGold &&
                        peasant.State != UnitState.GatheringLumber &&
                        peasant.State != UnitState.Scouting)
                    {
                        peasant.State = UnitState.Idle;
                    }
                }

                foreach (var soldier in aiSoldiers)
                {
                    // Continue attacking current target
                    if (soldier.State == UnitState.Attacking && soldier.AttackTarget != null)
                    {
                        if (soldier.AttackTarget.HP > 0 && soldier.AttackTarget.State != UnitState.Dead)
                        {
                            continue;
                        }

                        soldier.AttackTarget = null;
                        soldier.State = UnitState.Idle;
                    }

                    // ✅ NEW: Look for nearby enemies (TEAM-AWARE)
                    var nearestEnemy = WarRegistry.Units
                        .Where(u => u.OwnerTeam != aiTeam && u.HP > 0)
                        .OrderBy(u => DistanceTo(soldier, u))
                        .FirstOrDefault();

                    if (nearestEnemy != null && DistanceTo(soldier, nearestEnemy) < aggressionRange)
                    {
                        soldier.State = UnitState.Attacking;
                        soldier.AttackTarget = nearestEnemy;
                        soldier.TargetX = nearestEnemy.PosX;
                        soldier.TargetY = nearestEnemy.PosY;
                        continue;
                    }

                    // ✅ NEW: Attack buildings (TEAM-AWARE)
                    var nearestBuilding = WarRegistry.Buildings
                        .Where(b => b.OwnerTeam != aiTeam && b.HP > 0)
                        .OrderBy(b => DistanceTo(soldier, b))
                        .FirstOrDefault();

                    if (nearestBuilding != null && DistanceTo(soldier, nearestBuilding) < aggressionRange / 2)
                    {
                        soldier.State = UnitState.Attacking;
                        soldier.AttackTargetBuilding = nearestBuilding;
                        soldier.TargetX = nearestBuilding.PosX + nearestBuilding.Width / 2;
                        soldier.TargetY = nearestBuilding.PosY + nearestBuilding.Height / 2;
                        continue;
                    }

                    // Late game rush strategy
                    if (shouldRush && (soldier.State == UnitState.Idle || soldier.TargetX == null))
                    {
                        // ✅ NEW: Find enemy base (DIFFERENT TEAM)
                        var enemyBase = WarRegistry.Buildings
                            .FirstOrDefault(b => b.OwnerTeam != aiTeam && (b is TownHall || b is Castle));

                        if (enemyBase != null)
                        {
                            soldier.State = UnitState.Moving;
                            soldier.TargetX = enemyBase.PosX + 64;
                            soldier.TargetY = enemyBase.PosY + 64;
                            continue;
                        }
                    }

                    // Early/mid game: Defensive patrol near base
                    if (soldier.State == UnitState.Idle && aiGameAge < LATE_GAME)
                    {
                        var aiBase = WarRegistry.Buildings
                            .FirstOrDefault(b => b.OwnerPlayerIndex == aiPlayerIndex && (b is TownHall || b is Castle));

                        if (aiBase != null)
                        {
                            int patrolX = aiBase.PosX + new Random().Next(-200, 200);
                            int patrolY = aiBase.PosY + new Random().Next(-200, 200);

                            soldier.State = UnitState.Moving;
                            soldier.TargetX = Math.Clamp(patrolX, 0, game.MapWidth);
                            soldier.TargetY = Math.Clamp(patrolY, 0, game.MapHeight);
                        }
                    }
                }
            }

            private static double DistanceTo(WarEntity from, WarEntity to)
            {
                int dx = (from.PosX + from.Width / 2) - (to.PosX + to.Width / 2);
                int dy = (from.PosY + from.Height / 2) - (to.PosY + to.Height / 2);
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // In WarFaction.cs, in TickAI method, add after ManageAttacks(game);


            private static void ManageSpellcasters(WarGameService game, Faction aiFaction)
            {
                // ✅ NEW: Get AI player info
                var aiPlayer = game.Players.FirstOrDefault(p => p.Faction == aiFaction);
                if (aiPlayer == null) return;

                int aiPlayerIndex = aiPlayer.SlotIndex;
                int aiTeam = aiPlayer.Team;

                // ✅ NEW: Only control THIS AI's spellcasters
                foreach (var caster in WarRegistry.Units
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex &&
                                (u is Priest || u is Cultist) &&
                                u.Mana >= u.AbilityManaCost &&
                                u.AbilityCooldown <= 0 &&
                                u.State != UnitState.Dead))
                {
                    if (caster is Priest)
                    {
                        // ✅ NEW: Heal allies (SAME TEAM)
                        var woundedAlly = WarRegistry.Units
                            .Where(u => u.OwnerTeam == aiTeam &&
                                        u.HP < u.MaxHP &&
                                        u.State != UnitState.Dead &&
                                        DistanceTo(caster, u) <= caster.AbilityRange)
                            .OrderBy(u => u.HP)
                            .FirstOrDefault();

                        if (woundedAlly != null)
                        {
                            caster.Mana -= caster.AbilityManaCost;
                            caster.AbilityCooldown = caster.AbilityCooldownMax;
                            woundedAlly.HP = Math.Min(woundedAlly.HP + 5, woundedAlly.MaxHP);
                            var healProjectile = ((Priest)caster).CreateHealProjectile(woundedAlly);
                            WarRegistry.Projectiles.Add(healProjectile);
                            Console.WriteLine($"⚕️ AI Player {aiPlayerIndex}: Priest heals {woundedAlly.PlaceholderName}");
                        }
                    }
                    else if (caster is Cultist)
                    {
                        // ✅ NEW: Buff allies (SAME TEAM)
                        var unbuffedAlly = WarRegistry.Units
                            .Where(u => u.OwnerTeam == aiTeam &&
                                        (u is Footman || u is Brigand || u is Ogre) &&
                                        u.BloodlustDuration <= 0 &&
                                        u.State != UnitState.Dead &&
                                        DistanceTo(caster, u) <= caster.AbilityRange)
                            .FirstOrDefault();

                        if (unbuffedAlly != null)
                        {
                            caster.Mana -= caster.AbilityManaCost;
                            caster.AbilityCooldown = caster.AbilityCooldownMax;
                            unbuffedAlly.BloodlustBonus = 1;
                            unbuffedAlly.BloodlustDuration = 300;
                            var bloodlustProjectile = ((Cultist)caster).CreateBloodlustProjectile(unbuffedAlly);
                            WarRegistry.Projectiles.Add(bloodlustProjectile);
                            Console.WriteLine($"🩸 AI Player {aiPlayerIndex}: Cultist buffs {unbuffedAlly.PlaceholderName}");
                        }
                    }
                }
            }



            // ===== MANAGE MOUNTING (AUTO-MOUNT FOOTMEN/GRUNTS) =====
            private static void ManageMounting(WarGameService game, Faction aiFaction)
            {
                var aiPlayer = game.Players.FirstOrDefault(p => p.Faction == aiFaction);
                if (aiPlayer == null) return;

                int aiPlayerIndex = aiPlayer.SlotIndex;

                // Find all mountable infantry (Footmen/Grunts with CanMount = true)
                var mountableInfantry = WarRegistry.Units
                    .Where(u => u.OwnerPlayerIndex == aiPlayerIndex &&
                                u is Footman &&
                                u.CanMount &&
                                u.MountType != null &&
                                u.State != UnitState.Dead)
                    .ToList();

                foreach (var infantry in mountableInfantry)
                {
                    // Find nearby mount (Horse or Wolf)
                    var nearbyMount = WarRegistry.Units
                        .Where(u => u.IsMountable &&
                                    u.PlaceholderName.Contains(infantry.MountType) &&
                                    u.OwnerPlayerIndex == aiPlayerIndex &&
                                    u.State != UnitState.Dead)
                        .OrderBy(u => DistanceTo(infantry, u))
                        .FirstOrDefault(u => DistanceTo(infantry, u) <= 400);

                    if (nearbyMount != null)
                    {
                        // Auto-mount!
                        game.ActivateMountSkill(infantry);
                        Console.WriteLine($"🐴 AI Player {aiPlayerIndex}: Auto-mounted {infantry.PlaceholderName} → Knight/Raider");
                    }
                }
            }
        }




    }
}