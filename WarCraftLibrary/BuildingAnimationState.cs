using System;

namespace WarCraftLibrary
{
    // ===== BUILDING ANIMATION STATE ENUM =====
    /// <summary>
    /// Represents the current visual/gameplay state of a building
    /// </summary>
    public enum BuildingAnimationState
    {
        Constructing,  // Building is under construction (0-100% progress)
        Built,         // Building is complete and operational
        Damaged,       // Building has taken damage (< 100% HP)
        Destroyed      // Building is destroyed (0 HP, rubble remains)
    }

    // ===== CONSTRUCTION PROGRESS HELPER =====
    /// <summary>
    /// Converts construction time remaining into sprite sheet index (0-4)
    /// </summary>
    public static class ConstructionProgress
    {
        /// <summary>
        /// Maps construction progress to sprite sheet index
        /// </summary>
        /// <param name="timeRemaining">Ticks remaining until construction complete</param>
        /// <param name="totalTime">Total construction time in ticks</param>
        /// <returns>Sprite sheet index: 0 (0%), 1 (25%), 2 (50%), 3 (75%), 4 (100%)</returns>
        public static int GetSpriteSheetIndex(int timeRemaining, int totalTime)
        {
            if (totalTime <= 0) return 4; // Safety: Avoid divide by zero

            // Calculate progress as percentage (0.0 to 1.0)
            float progress = 1.0f - ((float)timeRemaining / totalTime);
            progress = Math.Clamp(progress, 0.0f, 1.0f);

            // Map to sprite sheet index (0-4)
            // 0.00-0.20 → Sheet 0 (0%)
            // 0.20-0.40 → Sheet 1 (25%)
            // 0.40-0.60 → Sheet 2 (50%)
            // 0.60-0.80 → Sheet 3 (75%)
            // 0.80-1.00 → Sheet 4 (100%)
            int sheetIndex = (int)(progress * 5);
            return Math.Clamp(sheetIndex, 0, 4);
        }

        /// <summary>
        /// Gets construction progress as a percentage (0-100)
        /// </summary>
        public static int GetProgressPercentage(int timeRemaining, int totalTime)
        {
            if (totalTime <= 0) return 100;

            float progress = 1.0f - ((float)timeRemaining / totalTime);
            return (int)(Math.Clamp(progress, 0.0f, 1.0f) * 100);
        }
    }

    // ===== DAMAGE LEVEL HELPER =====
    /// <summary>
    /// Calculates damage visual state based on HP percentage
    /// </summary>
    public static class DamageLevel
    {
        /// <summary>
        /// Determines damage sprite level based on current HP
        /// </summary>
        /// <param name="currentHP">Current hit points</param>
        /// <param name="maxHP">Maximum hit points</param>
        /// <returns>
        /// 0 = Pristine (100-75% HP)
        /// 1 = Light damage (75-50% HP)
        /// 2 = Medium damage (50-25% HP)
        /// 3 = Heavy damage (< 25% HP)
        /// </returns>
        public static int GetDamageLevel(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return 0; // Safety

            float hpPercentage = (float)currentHP / maxHP;

            if (hpPercentage >= 0.75f)
                return 0; // Pristine (no damage sprite)
            else if (hpPercentage >= 0.50f)
                return 1; // Light damage
            else if (hpPercentage >= 0.25f)
                return 2; // Medium damage
            else
                return 3; // Heavy damage
        }

        /// <summary>
        /// Checks if building should show damage visuals
        /// </summary>
        public static bool ShouldShowDamage(int currentHP, int maxHP)
        {
            if (maxHP <= 0) return false;
            float hpPercentage = (float)currentHP / maxHP;
            return hpPercentage < 0.75f; // Show damage below 75% HP
        }
    }

    // ===== BUILDING TYPE HELPER =====
    /// <summary>
    /// Helper to extract building type name for sprite paths
    /// </summary>
    public static class BuildingTypeHelper
    {
        /// <summary>
        /// Extracts base building type from PlaceholderName
        /// Example: "TownHall_Human" → "TownHall"
        /// </summary>
        public static string GetBuildingType(string placeholderName)
        {
            if (string.IsNullOrEmpty(placeholderName))
                return "Unknown";

            // Split on underscore and take first part
            string[] parts = placeholderName.Split('_');
            return parts[0];
        }

        /// <summary>
        /// Gets race-specific building name
        /// Example: "TownHall", Human → "TownHall" (Orc would be "Stronghold")
        /// </summary>
        public static string GetRaceSpecificName(string buildingType, RaceType race)
        {
            // Handle special cases where races have different building names
            if (buildingType == "TownHall" && race == RaceType.Orc)
                return "Stronghold";

            if (buildingType == "Farm" && race == RaceType.Orc)
                return "PigFarm";

            if (buildingType == "Castle" && race == RaceType.Orc)
                return "Fortress";

            // Default: use same name for both races
            return buildingType;
        }
    }
}