using System;
using System.Collections.Generic;

namespace WarCraftLibrary
{
    /// <summary>
    /// Building animation sprite path database
    /// Manages placeholder sprite sheets for construction, damage, and destruction states
    /// Similar to WarAnimations.cs whitelist system
    /// </summary>
    public static class BuildingAnimationData
    {
        // ===== SPRITE PATH TEMPLATE =====
        // Construction: /wc1sprites/buildings/{BuildingType}_{Race}_Construction_{Sheet}.png
        //   Example: /wc1sprites/buildings/TownHall_Human_Construction_0.png (0%)
        //   Example: /wc1sprites/buildings/Barracks_Orc_Construction_2.png (50%)
        //   Example: /wc1sprites/buildings/Farm_Human_Construction_4.png (100%)
        //
        // Damage: /wc1sprites/buildings/{BuildingType}_{Race}_Damage_{Level}.png
        //   Example: /wc1sprites/buildings/TownHall_Human_Damage_1.png (light)
        //   Example: /wc1sprites/buildings/Barracks_Orc_Damage_3.png (heavy)
        //
        // Destroyed: /wc1sprites/buildings/{BuildingType}_{Race}_Destroyed.png
        //   Example: /wc1sprites/buildings/TownHall_Human_Destroyed.png (rubble)

        // ===== WHITELIST: AVAILABLE SPRITE SHEETS =====
        // Add sprite paths here as you create them (like WarAnimations.cs)
        private static readonly HashSet<string> _availableSprites = new HashSet<string>
        {
            // ========== TOWN HALL (HUMAN) ==========
            // Construction stages (0%, 25%, 50%, 75%, 100%)
            "TownHall_Human_Construction_0",
            "TownHall_Human_Construction_1",
            "TownHall_Human_Construction_2",
          //  "TownHall_Human_Construction_3",
          //  "TownHall_Human_Construction_4",
            
            // Damage states (light, medium, heavy)
            // "TownHall_Human_Damage_1",
            // "TownHall_Human_Damage_2",
            // "TownHall_Human_Damage_3",
            
            // Destroyed (rubble)
            // "TownHall_Human_Destroyed",

            // ========== STRONGHOLD (ORC TOWN HALL) ==========
            // Construction stages
            "Stronghold_Orc_Construction_0",
            "Stronghold_Orc_Construction_1",
            "Stronghold_Orc_Construction_2",
          //  "Stronghold_Orc_Construction_3",
          //  "Stronghold_Orc_Construction_4",
            
            // Damage states
            // "Stronghold_Orc_Damage_1",
            // "Stronghold_Orc_Damage_2",
            // "Stronghold_Orc_Damage_3",
            
            // Destroyed
            // "Stronghold_Orc_Destroyed",

// ========== WOOD TOWER ==========
//"WoodTower_Human_Construction_0",
//"WoodTower_Human_Construction_1",
//"WoodTower_Human_Construction_2",
      //  "WoodTower_Human_Construction_3",
          //  "WoodTower_Human_Construction_4",
// Damage
//"WoodTower_Human_Damage_0",
//"WoodTower_Human_Damage_1",
//"WoodTower_Human_Damage_2",

// Destroyed
//"WoodTower_Human_Destroyed",

//"WoodTower_Orc_Construction_0",
//"WoodTower_Orc_Construction_1",
//"WoodTower_Orc_Construction_2",
  //  "WoodTower_Orc_Construction_3",
          //  "WoodTower_Orc_Construction_4",
// Damage
//"WoodTower_Orc_Damage_0",
//"WoodTower_Orc_Damage_1",
//"WoodTower_Orc_Damage_2",

// Destroyed
//"WoodTower_Orc_Destroyed",

// ========== STONE TOWER ==========
//"StoneTower_Human_Construction_0",
//"StoneTower_Human_Construction_1",
//"StoneTower_Human_Construction_2",
  //  "StoneTower_Human_Construction_3",
          //  "StoneTower_Human_Construction_4",
// Damage
//"StoneTower_Human_Damage_0",
//"StoneTower_Human_Damage_1",
//"StoneTower_Human_Damage_2",

// Destroyed
//"StoneTower_Human_Destroyed",

//"StoneTower_Orc_Construction_0",
//"StoneTower_Orc_Construction_1",
//"StoneTower_Orc_Construction_2",
  //  "StoneTower_Orc_Construction_3",
          //  "StoneTower_Orc_Construction_4",
// Damage
//"StoneTower_Orc_Damage_0",
//"StoneTower_Orc_Damage_1",
//"StoneTower_Orc_Damage_2",

// Destroyed
//"StoneTower_Orc_Destroyed",


            // ========== BARRACKS (HUMAN) ==========
            "Barracks_Human_Construction_0",
            "Barracks_Human_Construction_1",
            "Barracks_Human_Construction_2",
          //  "Barracks_Human_Construction_3",
         //   "Barracks_Human_Construction_4",
            
            // "Barracks_Human_Damage_1",
            // "Barracks_Human_Damage_2",
            // "Barracks_Human_Damage_3",
            // "Barracks_Human_Destroyed",

            // ========== BARRACKS (ORC) ==========
            "Barracks_Orc_Construction_0",
            "Barracks_Orc_Construction_1",
            "Barracks_Orc_Construction_2",
           // "Barracks_Orc_Construction_3",
          //  "Barracks_Orc_Construction_4",
            
            // "Barracks_Orc_Damage_1",
            // "Barracks_Orc_Damage_2",
            // "Barracks_Orc_Damage_3",
            // "Barracks_Orc_Destroyed",


            // ========== ROAD ==========
//"Road_Construction_0",
//"Road_Construction_1",
//"Road_Construction_2",

            /*
             // ========== WOODEN WALL ==========
"WoodenWall_Human_Construction_0",
"WoodenWall_Human_Construction_1",
"WoodenWall_Human_Construction_2",

"WoodenWall_Orc_Construction_0",
"WoodenWall_Orc_Construction_1",
"WoodenWall_Orc_Construction_2",

// ========== STONE WALL ==========
"StoneWall_Human_Construction_0",
"StoneWall_Human_Construction_1",
"StoneWall_Human_Construction_2",

"StoneWall_Orc_Construction_0",
"StoneWall_Orc_Construction_1",
"StoneWall_Orc_Construction_2",
             
             
             
             
             */


            // ========== FARM (HUMAN) ==========
            "Farm_Human_Construction_0",
            "Farm_Human_Construction_1",
            "Farm_Human_Construction_2",
          //  "Farm_Human_Construction_3",
         //   "Farm_Human_Construction_4",
            
            // "Farm_Human_Damage_1",
            // "Farm_Human_Damage_2",
            // "Farm_Human_Damage_3",
            // "Farm_Human_Destroyed",

            // ========== PIG FARM (ORC FARM) ==========
            "PigFarm_Orc_Construction_0",
            "PigFarm_Orc_Construction_1",
            "PigFarm_Orc_Construction_2",
        //    "PigFarm_Orc_Construction_3",
           // "PigFarm_Orc_Construction_4",
            
            // "PigFarm_Orc_Damage_1",
            // "PigFarm_Orc_Damage_2",
            // "PigFarm_Orc_Damage_3",
            // "PigFarm_Orc_Destroyed",

            // ========== LUMBER MILL (HUMAN) ==========
            "LumberMill_Human_Construction_0",
            "LumberMill_Human_Construction_1",
            "LumberMill_Human_Construction_2",
          //  "LumberMill_Human_Construction_3",
           // "LumberMill_Human_Construction_4",
            
            // "LumberMill_Human_Damage_1",
            // "LumberMill_Human_Damage_2",
            // "LumberMill_Human_Damage_3",
            // "LumberMill_Human_Destroyed",

            // ========== LUMBER MILL (ORC) ==========
            "LumberMill_Orc_Construction_0",
            "LumberMill_Orc_Construction_1",
            "LumberMill_Orc_Construction_2",
          //  "LumberMill_Orc_Construction_3",
           // "LumberMill_Orc_Construction_4",
            
            // "LumberMill_Orc_Damage_1",
            // "LumberMill_Orc_Damage_2",
            // "LumberMill_Orc_Damage_3",
            // "LumberMill_Orc_Destroyed",

            // ========== ARCHERY RANGE (HUMAN) ==========
            "ArcheryRange_Human_Construction_0",
            "ArcheryRange_Human_Construction_1",
            "ArcheryRange_Human_Construction_2",
         //  "ArcheryRange_Human_Construction_3",
          //  "ArcheryRange_Human_Construction_4",
            
            // "ArcheryRange_Human_Damage_1",
            // "ArcheryRange_Human_Damage_2",
            // "ArcheryRange_Human_Damage_3",
            // "ArcheryRange_Human_Destroyed",

            // ========== ARCHERY RANGE (ORC) ==========
            "ArcheryRange_Orc_Construction_0",
            "ArcheryRange_Orc_Construction_1",
            "ArcheryRange_Orc_Construction_2",
         //   "ArcheryRange_Orc_Construction_3",
          //  "ArcheryRange_Orc_Construction_4",
            
            // "ArcheryRange_Orc_Damage_1",
            // "ArcheryRange_Orc_Damage_2",
            // "ArcheryRange_Orc_Damage_3",
            // "ArcheryRange_Orc_Destroyed",

            // ========== BLACKSMITH (HUMAN) ==========
            "Blacksmith_Human_Construction_0",
            "Blacksmith_Human_Construction_1",
            "Blacksmith_Human_Construction_2",
          //  "Blacksmith_Human_Construction_3",
          //  "Blacksmith_Human_Construction_4",
            
            // "Blacksmith_Human_Damage_1",
            // "Blacksmith_Human_Damage_2",
            // "Blacksmith_Human_Damage_3",
            // "Blacksmith_Human_Destroyed",

            // ========== BLACKSMITH (ORC) ==========
            "Blacksmith_Orc_Construction_0",
            "Blacksmith_Orc_Construction_1",
            "Blacksmith_Orc_Construction_2",
          //  "Blacksmith_Orc_Construction_3",
          //  "Blacksmith_Orc_Construction_4",
            
            // "Blacksmith_Orc_Damage_1",
            // "Blacksmith_Orc_Damage_2",
            // "Blacksmith_Orc_Damage_3",
            // "Blacksmith_Orc_Destroyed",

            // ========== CASTLE (HUMAN) ==========
            "Castle_Human_Construction_0",
            "Castle_Human_Construction_1",
            "Castle_Human_Construction_2",
          //  "Castle_Human_Construction_3",
           // "Castle_Human_Construction_4",
            
            // "Castle_Human_Damage_1",
            // "Castle_Human_Damage_2",
            // "Castle_Human_Damage_3",
            // "Castle_Human_Destroyed",

            // ========== FORTRESS (ORC CASTLE) ==========
            "Fortress_Orc_Construction_0",
            "Fortress_Orc_Construction_1",
            "Fortress_Orc_Construction_2",
          //  "Fortress_Orc_Construction_3",
          //  "Fortress_Orc_Construction_4",
            
            // "Fortress_Orc_Damage_1",
            // "Fortress_Orc_Damage_2",
            // "Fortress_Orc_Damage_3",
            // "Fortress_Orc_Destroyed",
        };

        // ===== GET CONSTRUCTION SPRITE PATH =====
        /// <summary>
        /// Returns sprite path for construction stage (0-4)
        /// </summary>
        /// <param name="buildingType">Base building type (e.g., "TownHall", "Barracks")</param>
        /// <param name="race">Race (Human/Orc)</param>
        /// <param name="sheetIndex">Sprite sheet index (0=0%, 1=25%, 2=50%, 3=75%, 4=100%)</param>
        /// <returns>Sprite path if available, otherwise fallback to original sprite</returns>
        public static string GetConstructionSprite(string buildingType, RaceType race, int sheetIndex)
        {
            // Get race-specific building name (e.g., "TownHall" → "Stronghold" for Orc)
            string raceSpecificName = BuildingTypeHelper.GetRaceSpecificName(buildingType, race);
            string raceString = race.ToString();

            // Construct sprite key
            string key = $"{raceSpecificName}_{raceString}_Construction_{sheetIndex}";

            // Check if sprite exists in whitelist
            if (_availableSprites.Contains(key))
            {
                return $"/wc1sprites/buildings/{key}.png";
            }

            // Fallback: Return empty (controller will use original sprite)
            return "";
        }

        // ===== GET DAMAGE SPRITE PATH =====
        /// <summary>
        /// Returns sprite path for damage state (1-3)
        /// </summary>
        /// <param name="buildingType">Base building type</param>
        /// <param name="race">Race (Human/Orc)</param>
        /// <param name="damageLevel">Damage level (1=light, 2=medium, 3=heavy)</param>
        /// <returns>Sprite path if available, otherwise empty string</returns>
        public static string GetDamageSprite(string buildingType, RaceType race, int damageLevel)
        {
            if (damageLevel < 1 || damageLevel > 3) return "";

            string raceSpecificName = BuildingTypeHelper.GetRaceSpecificName(buildingType, race);
            string raceString = race.ToString();

            string key = $"{raceSpecificName}_{raceString}_Damage_{damageLevel}";

            if (_availableSprites.Contains(key))
            {
                return $"/wc1sprites/buildings/{key}.png";
            }

            return ""; // No damage sprite available - use pristine sprite
        }

        // ===== GET DESTROYED SPRITE PATH =====
        /// <summary>
        /// Returns sprite path for destroyed/rubble state
        /// </summary>
        public static string GetDestroyedSprite(string buildingType, RaceType race)
        {
            string raceSpecificName = BuildingTypeHelper.GetRaceSpecificName(buildingType, race);
            string raceString = race.ToString();

            string key = $"{raceSpecificName}_{raceString}_Destroyed";

            if (_availableSprites.Contains(key))
            {
                return $"/wc1sprites/buildings/{key}.png";
            }

            return ""; // No destroyed sprite - building just disappears
        }

        // ===== CHECK IF SPRITE EXISTS =====
        /// <summary>
        /// Checks if a sprite sheet exists in the whitelist
        /// Useful for debugging and asset management
        /// </summary>
        public static bool HasConstructionSprite(string buildingType, RaceType race, int sheetIndex)
        {
            string raceSpecificName = BuildingTypeHelper.GetRaceSpecificName(buildingType, race);
            string key = $"{raceSpecificName}_{race}_Construction_{sheetIndex}";
            return _availableSprites.Contains(key);
        }

        public static bool HasDamageSprite(string buildingType, RaceType race, int damageLevel)
        {
            string raceSpecificName = BuildingTypeHelper.GetRaceSpecificName(buildingType, race);
            string key = $"{raceSpecificName}_{race}_Damage_{damageLevel}";
            return _availableSprites.Contains(key);
        }

        public static bool HasDestroyedSprite(string buildingType, RaceType race)
        {
            string raceSpecificName = BuildingTypeHelper.GetRaceSpecificName(buildingType, race);
            string key = $"{raceSpecificName}_{race}_Destroyed";
            return _availableSprites.Contains(key);
        }

        // ===== FALLBACK SPRITE (ORIGINAL) =====
        /// <summary>
        /// Returns the original static sprite path from WarEntity.cs
        /// Used as fallback when animated sprites don't exist
        /// </summary>
        public static string GetOriginalSprite(WarBuilding building)
        {
            // This uses the existing SpritePath from the building's constructor
            return building.SpritePath;
        }
    }
}