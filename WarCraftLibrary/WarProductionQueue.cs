using System;
using System.Collections.Generic;
using System.Text;

namespace WarCraftLibrary
{

    public class ProductionOrder
    {
        public string UnitType { get; set; } = ""; // "Peasant", "Footman", etc.
        public int GoldCost { get; set; }
        public int LumberCost { get; set; }
        public int BuildTimeRemaining { get; set; } // In ticks (60 ticks = 1 sec)
        public int TotalBuildTime { get; set; }
        public RaceType Race { get; set; }
    }

    public static class ProductionSystem
    {
        // Unit costs: (gold, lumber, time in seconds)
        public static Dictionary<string, (int gold, int lumber, int time)> UnitCosts = new()
{
    { "Peasant", (400, 0, 5) },
    { "Peon", (400, 0, 5) },
    { "Footman", (600, 0, 5) },
    { "Grunt", (600, 0, 5) },
    { "Archer", (500, 50, 7) },
    { "Priest", (1000, 50, 10) },      // ✅ NEW - Expensive gold cost!
{ "Cultist", (1000, 50, 10) },   // ✅ NEW
    { "TrollAxeThrower", (500, 50, 7) },
    { "Brigand", (750, 100, 8) },      // ✅ NEW - Human advanced infantry
    { "Ogre", (800, 150, 10) },         // ✅ NEW - Orc advanced infantry
            { "Horse", (300, 0, 6) },          // ✅ NEW - Human mount
    { "Wolf", (300, 0, 6) },           // ✅ NEW - Orc mount
    { "Knight", (0, 0, 0) },           // ✅ NEW - Created by mounting (not trained)
    { "OrcRaider", (0, 0, 0) }         // ✅ NEW - Created by mounting (not trained)
};

        // Building costs
        public static Dictionary<string, (int gold, int lumber, int time)> BuildingCosts = new()
{
    { "Barracks", (700, 450, 10) },
    { "Farm", (500, 250, 10) },
    { "TownHall", (1200, 800, 15) }, // ✅ NEW: Allow building additional Town Halls
    { "Church", (800, 450, 15) },
{ "CultistHut", (800, 450, 15) },
    { "LumberMill", (600, 450, 15) },
    { "ArcheryRange", (800, 400, 20) },
    { "Blacksmith", (800, 450, 15) },
    { "WoodTower", (500, 300, 15) },
{ "StoneTower", (800, 500, 20) },
{ "Road", (50, 25, 3) }, // Cheap and fast!
{ "WoodenWall", (100, 50, 5) },
{ "StoneWall", (200, 100, 10) },
    { "Castle", (2000, 1000, 30) },  // ✅ NEW - Expensive upgrade!
    { "Stables", (400, 200, 30) },     // ✅ NEW - Human Tier 1
    { "Pen", (400, 200, 30) },         // ✅ NEW - Orc Tier 1
    { "KnightsHold", (800, 400, 40) }, // ✅ NEW - Human Tier 2
    { "RaiderLair", (800, 400, 40) }   // ✅ NEW - Orc Tier 2
};
        /*
             public static bool QueueUnit(WarBuilding building, string unitType, WarGameService game, bool isAI = false)
          {
              Console.WriteLine($"🔍 QueueUnit called: {unitType} from {building.PlaceholderName}, isAI={isAI}");

              if (!UnitCosts.ContainsKey(unitType))
              {
                  Console.WriteLine($"❌ Unit type not found in costs!");
                  return false;
              }

              var (gold, lumber, time) = UnitCosts[unitType];
              Console.WriteLine($"💰 Cost: {gold}g {lumber}w");

              if (!CheckUnitRequirements(building, unitType, game, isAI))
              {
                  Console.WriteLine($"❌ Requirements check failed!");
                  return false;
              }

              Faction? ownerFaction = building.OwnerFaction ?? (isAI ? game.AIFaction : game.PlayerFaction);

              if (ownerFaction == null)
              {
                  Console.WriteLine($"❌ No owner faction!");
                  return false;
              }

              // ✅ FIX: Update population counts BEFORE checking capacity
              game.UpdatePopulation();
              ownerFaction = game.PlayerFaction; // Re-get faction after update
              Console.WriteLine($"✅ Owner faction: {ownerFaction.Name}, Gold: {ownerFaction.Gold}, Lumber: {ownerFaction.Lumber}, Pop: {ownerFaction.CurrentPop}/{ownerFaction.MaxPop}");

              if (ownerFaction.Gold < gold || ownerFaction.Lumber < lumber)
              {
                  Console.WriteLine($"❌ Can't afford!");
                  return false;
              }

              if (ownerFaction.CurrentPop >= ownerFaction.MaxPop)
              {
                  Console.WriteLine($"❌ Population capped!");
                  return false;
              }

              ownerFaction.Gold -= gold;
              ownerFaction.Lumber -= lumber;

              if (!isAI)
              {
                  game.PlayerGold = ownerFaction.Gold;
                  game.PlayerLumber = ownerFaction.Lumber;
              }

              var order = new ProductionOrder
              {
                  UnitType = unitType,
                  GoldCost = gold,
                  LumberCost = lumber,
                  TotalBuildTime = time * 60,
                  BuildTimeRemaining = time * 60,
                  Race = building.Race
              };

              building.ProductionQueue.Enqueue(order);
              Console.WriteLine($"✅ SUCCESS! Queued {unitType}");
              return true;
          }


         */
        /*
        // ===== QUEUE UNIT =====
        public static bool QueueUnit(WarBuilding building, string unitType, WarGameService game, bool isAI = false)
        {
            if (!UnitCosts.ContainsKey(unitType)) return false;
            var (gold, lumber, time) = UnitCosts[unitType];
            if (!CheckUnitRequirements(building, unitType, game, isAI)) return false;

            if (isAI)
            {
                var aiFaction = building.OwnerFaction ?? game.AIFaction;
                if (aiFaction == null) return false;
                if (aiFaction.Gold < gold || aiFaction.Lumber < lumber) return false;
                if (aiFaction.CurrentPop >= 500) return false;

                aiFaction.Gold -= gold;
                aiFaction.Lumber -= lumber;
            }
            else
            {
                if (game.PlayerGold < gold || game.PlayerLumber < lumber) return false;
                if (game.PlayerCurrentPop >= 500) return false;

                game.PlayerGold -= gold;
                game.PlayerLumber -= lumber;
            }

            building.ProductionQueue.Enqueue(new ProductionOrder
            {
                UnitType = unitType,
                GoldCost = gold,
                LumberCost = lumber,
                TotalBuildTime = time * 60,
                BuildTimeRemaining = time * 60,
                Race = building.Race
            });
            return true;
        }
        */
        public static bool QueueUnit(WarBuilding building, string unitType, WarGameService game, bool isAI = false)
        {
            if (!UnitCosts.ContainsKey(unitType)) return false;
            var (gold, lumber, time) = UnitCosts[unitType];

            if (!CheckUnitRequirements(building, unitType, game, isAI)) return false;

            // ✅ FIX: Get the correct faction reference
            Faction? ownerFaction = building.OwnerFaction;

            if (ownerFaction == null)
            {
                Console.WriteLine($"❌ No owner faction for building!");
                return false;
            }

            // ✅ FIX: Update population BEFORE checking
            game.UpdatePopulation();

            // ✅ FIX: Check resources and population from the correct faction
            if (ownerFaction.Gold < gold || ownerFaction.Lumber < lumber)
            {
                Console.WriteLine($"❌ Not enough resources! Need {gold}g {lumber}w, Have {ownerFaction.Gold}g {ownerFaction.Lumber}w");
                return false;
            }

            if (ownerFaction.CurrentPop >= ownerFaction.MaxPop)
            {
                Console.WriteLine($"❌ Population capped! {ownerFaction.CurrentPop}/{ownerFaction.MaxPop}");
                return false;
            }

            // ✅ FIX: Deduct resources from the correct faction
            ownerFaction.Gold -= gold;
            ownerFaction.Lumber -= lumber;

            // ✅ FIX: If this is the player's faction, also update game service resources
            if (!isAI && building.OwnerPlayerIndex == 0)
            {
                game.PlayerGold = ownerFaction.Gold;
                game.PlayerLumber = ownerFaction.Lumber;
            }

            // ✅ Queue the production order
            var order = new ProductionOrder
            {
                UnitType = unitType,
                GoldCost = gold,
                LumberCost = lumber,
                TotalBuildTime = time * 60,
                BuildTimeRemaining = time * 60,
                Race = building.Race
            };

            building.ProductionQueue.Enqueue(order);

            Console.WriteLine($"✅ Queued {unitType} for Player {building.OwnerPlayerIndex} (Gold: {ownerFaction.Gold}, Lumber: {ownerFaction.Lumber}, Pop: {ownerFaction.CurrentPop}/{ownerFaction.MaxPop})");

            return true;
        }

        // ===== TICK PRODUCTION (called every frame) =====
        public static void TickProduction(WarBuilding building, WarGameService game)
        {
            if (!building.IsConstructed) return;
            if (building.ProductionQueue.Count == 0) return;

            var order = building.ProductionQueue.Peek();
            order.BuildTimeRemaining--;

            // Check if unit finished
            if (order.BuildTimeRemaining <= 0)
            {
                SpawnUnit(order.UnitType, building, game);
                building.ProductionQueue.Dequeue();
                Console.WriteLine($"{order.UnitType} training complete! {building.ProductionQueue.Count} remaining in queue");
            }
        }


        // ===== SPAWN UNIT =====
        private static void SpawnUnit(string unitType, WarBuilding building, WarGameService game)
        {
            WarUnit? newUnit = unitType switch
            {
                "Peasant" => new Peasant(building.RallyX, building.RallyY, RaceType.Human),
                "Peon" => new Peasant(building.RallyX, building.RallyY, RaceType.Orc),
                "Footman" => new Footman(building.RallyX, building.RallyY, RaceType.Human),
                "Grunt" => new Footman(building.RallyX, building.RallyY, RaceType.Orc),
                "Priest" => new Priest(building.RallyX, building.RallyY, RaceType.Human),
                "Cultist" => new Cultist(building.RallyX, building.RallyY, RaceType.Orc),
                "Archer" => new Archer(building.RallyX, building.RallyY, RaceType.Human),
                "TrollAxeThrower" => new TrollAxeThrower(building.RallyX, building.RallyY, RaceType.Orc),
                "Brigand" => new Brigand(building.RallyX, building.RallyY, RaceType.Human),
                "Ogre" => new Ogre(building.RallyX, building.RallyY, RaceType.Orc),
                "Horse" => new Horse(building.RallyX, building.RallyY, RaceType.Human),
                "Wolf" => new Wolf(building.RallyX, building.RallyY, RaceType.Orc),
                "Knight" => new Knight(building.RallyX, building.RallyY, RaceType.Human),
                "OrcRaider" => new OrcRaider(building.RallyX, building.RallyY, RaceType.Orc),
                _ => null
            };

            if (newUnit != null)
            {
                // ✅ NEW: Inherit ownership from parent building
                newUnit.OwnerFaction = building.OwnerFaction;
                newUnit.OwnerPlayerIndex = building.OwnerPlayerIndex;
                newUnit.OwnerTeam = building.OwnerTeam;

                WarRegistry.Units.Add(newUnit);
                game.UnitsTrainedCount++;
                Console.WriteLine($"✅ Spawned {unitType} at ({building.RallyX}, {building.RallyY}) - Owner: Player {newUnit.OwnerPlayerIndex}, Team {newUnit.OwnerTeam}");
            }
        }


        // ===== TICK CONSTRUCTION =====
        public static void TickConstruction(WarBuilding building)
        {
            if (building.IsConstructed) return;

            building.ConstructionTimeRemaining--;

            if (building.ConstructionTimeRemaining <= 0)
            {
                building.IsConstructed = true;
                Console.WriteLine($"{building.PlaceholderName} construction complete!");
            }
        }

        // ===== CHECK UNIT REQUIREMENTS =====
        private static bool CheckUnitRequirements(WarBuilding building, string unitType, WarGameService game, bool isAI)
        {
            var faction = isAI ? game.AIFaction : game.PlayerFaction;
            if (faction == null) return false;

            // Check if Blacksmith is required
            bool requiresBlacksmith = unitType switch
            {
                "Footman" => false,
                "Grunt" => false,
                "Brigand" => true,
                "Ogre" => true,
                _ => false
            };

            if (requiresBlacksmith)
            {
                bool hasBlacksmith = WarRegistry.Buildings
                    .OfType<Blacksmith>()
                    .Any(b => b.Race == faction.Race && b.IsConstructed);

                if (!hasBlacksmith)
                {
                    if (!isAI)
                    {
                        Console.WriteLine($"❌ Cannot train {unitType}: Requires Blacksmith!");
                    }
                    return false;
                }
            }

            // Check if Lumber Mill is required (for archers - already works but documenting)
            bool requiresLumberMill = unitType switch
            {
                "Archer" => false,  // No requirement for now
                "TrollAxeThrower" => false,
                _ => false
            };

            return true; // All requirements met
        }

    }
}
