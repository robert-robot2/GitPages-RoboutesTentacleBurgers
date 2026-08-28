using System;

namespace WarCraftLibrary
{
    /// <summary>
    /// Building Animation Controller
    /// Handles building state transitions, construction progress, damage visuals, and destruction
    /// Called every game tick to update building animation states
    /// </summary>
    public static class BuildingAnimationController
    {
        // ===== MAIN UPDATE METHOD (Called every game tick) =====
        /// <summary>
        /// Updates building animation state based on construction progress and HP
        /// Call this from WarGameService.GameTickAsync() for each building
        /// </summary>
        public static void UpdateBuildingAnimations(WarBuilding building)
        {
            // ===== STEP 1: UPDATE CONSTRUCTION PROGRESS =====
            if (!building.IsConstructed && building.ConstructionTimeRemaining > 0)
            {
                UpdateConstructionProgress(building);
            }

            // ===== STEP 2: CHECK IF CONSTRUCTION JUST COMPLETED =====
            if (!building.IsConstructed && building.ConstructionTimeRemaining <= 0)
            {
                CompleteConstruction(building);
            }

            // ===== STEP 3: UPDATE DAMAGE STATE (for built buildings) =====
            if (building.IsConstructed && building.HP > 0)
            {
                UpdateDamageState(building);
            }

            // ===== STEP 4: HANDLE DESTRUCTION (HP = 0) =====
            if (building.HP <= 0 && building.CurrentBuildingState != BuildingAnimationState.Destroyed)
            {
                TriggerDestruction(building);
            }
        }

        // ===== UPDATE CONSTRUCTION PROGRESS =====
        /// <summary>
        /// Calculates construction progress and updates sprite sheet index
        /// Maps time remaining to sprite sheet (0%, 25%, 50%, 75%, 100%)
        /// </summary>
        private static void UpdateConstructionProgress(WarBuilding building)
        {
            // Ensure we're in constructing state
            building.CurrentBuildingState = BuildingAnimationState.Constructing;

            // Calculate sprite sheet index (0-4)
            int newSheetIndex = ConstructionProgress.GetSpriteSheetIndex(
                building.ConstructionTimeRemaining,
                building.TotalConstructionTime
            );

            // Update sprite sheet if changed
            if (building.ConstructionSpriteSheet != newSheetIndex)
            {
                building.ConstructionSpriteSheet = newSheetIndex;

                // Optional: Log progress milestones
                int progress = ConstructionProgress.GetProgressPercentage(
                    building.ConstructionTimeRemaining,
                    building.TotalConstructionTime
                );

                if (newSheetIndex == 1 || newSheetIndex == 2 || newSheetIndex == 3)
                {
                    Console.WriteLine($"🏗️ {building.PlaceholderName}: Construction {progress}% complete (Sheet {newSheetIndex})");
                }
            }
        }

        // ===== COMPLETE CONSTRUCTION =====
        /// <summary>
        /// Transitions building from Constructing to Built state
        /// </summary>
        private static void CompleteConstruction(WarBuilding building)
        {
            building.IsConstructed = true;
            building.CurrentBuildingState = BuildingAnimationState.Built;
            building.ConstructionSpriteSheet = 4; // Ensure at 100% sprite
            building.DamageLevel = 0; // Pristine condition
            building.ConstructionTimeRemaining = 0;

            Console.WriteLine($"✅ {building.PlaceholderName}: Construction complete!");
        }

        // ===== UPDATE DAMAGE STATE =====
        /// <summary>
        /// Checks HP percentage and updates damage visual level
        /// Transitions between Built and Damaged states based on HP thresholds
        /// </summary>
        private static void UpdateDamageState(WarBuilding building)
        {
            // Calculate current damage level based on HP
            int newDamageLevel = DamageLevel.GetDamageLevel(building.HP, building.MaxHP);

            // Check if we should show damage
            bool shouldShowDamage = DamageLevel.ShouldShowDamage(building.HP, building.MaxHP);

            // Update state
            if (shouldShowDamage)
            {
                building.CurrentBuildingState = BuildingAnimationState.Damaged;
            }
            else
            {
                building.CurrentBuildingState = BuildingAnimationState.Built;
            }

            // Log damage level changes
            if (building.DamageLevel != newDamageLevel)
            {
                int oldLevel = building.DamageLevel;
                building.DamageLevel = newDamageLevel;

                // Only log significant damage changes
                if (newDamageLevel > 0)
                {
                    float hpPercent = (float)building.HP / building.MaxHP * 100f;
                    string damageDesc = newDamageLevel switch
                    {
                        1 => "Light damage",
                        2 => "Medium damage",
                        3 => "Heavy damage",
                        _ => "Pristine"
                    };

                    Console.WriteLine($"💥 {building.PlaceholderName}: {damageDesc} ({hpPercent:F0}% HP) - Damage Level {oldLevel} → {newDamageLevel}");
                }
                else if (oldLevel > 0)
                {
                    // Building was repaired back to pristine
                    Console.WriteLine($"🔧 {building.PlaceholderName}: Repaired to pristine condition");
                }
            }
        }

        // ===== TRIGGER DESTRUCTION =====
        /// <summary>
        /// Handles building destruction (HP = 0)
        /// Transitions to Destroyed state and displays rubble sprite
        /// </summary>
        public static void TriggerDestruction(WarBuilding building)
        {
            building.CurrentBuildingState = BuildingAnimationState.Destroyed;
            building.DamageLevel = 3; // Max damage level
            building.HP = 0;

            // ✅ Start destruction timer (building will be removed after this expires)
            building.DestructionTimer = WarBuilding.DestructionDisplayDuration;

            Console.WriteLine($"💀 {building.PlaceholderName}: DESTROYED! Rubble will remain for {WarBuilding.DestructionDisplayDuration / 60} seconds");

            // TODO Phase 5: Trigger destruction effects
            // - Screen shake
            // - Explosion particle effect
            // - Sound effect
        }

        // ===== GET CURRENT SPRITE (Main rendering method) =====
        /// <summary>
        /// Returns the correct sprite path for the building's current state
        /// This is called by WarMap.razor to render the building
        /// </summary>
        /// <param name="building">The building to get sprite for</param>
        /// <returns>Full sprite path (e.g., "/wc1sprites/buildings/TownHall_Human_Construction_2.png")</returns>
        public static string GetCurrentSprite(WarBuilding building)
        {
            // Extract building type from PlaceholderName (e.g., "TownHall_Human" → "TownHall")
            string buildingType = BuildingTypeHelper.GetBuildingType(building.PlaceholderName);

            switch (building.CurrentBuildingState)
            {
                case BuildingAnimationState.Constructing:
                    return GetConstructionSprite(building, buildingType);

                case BuildingAnimationState.Built:
                    return GetBuiltSprite(building, buildingType);

                case BuildingAnimationState.Damaged:
                    return GetDamagedSprite(building, buildingType);

                case BuildingAnimationState.Destroyed:
                    return GetDestroyedSprite(building, buildingType);

                default:
                    return BuildingAnimationData.GetOriginalSprite(building);
            }
        }

        // ===== GET CONSTRUCTION SPRITE =====
        private static string GetConstructionSprite(WarBuilding building, string buildingType)
        {
            // Get construction sprite for current progress level
            string constructionSprite = BuildingAnimationData.GetConstructionSprite(
                buildingType,
                building.Race,
                building.ConstructionSpriteSheet
            );

            // If construction sprite exists, use it
            if (!string.IsNullOrEmpty(constructionSprite))
            {
                return constructionSprite;
            }

            // Fallback: Use original sprite at reduced opacity (handled in Razor)
            return BuildingAnimationData.GetOriginalSprite(building);
        }

        // ===== GET BUILT SPRITE =====
        private static string GetBuiltSprite(WarBuilding building, string buildingType)
        {
            // Building is fully constructed and pristine
            // Try to get 100% construction sprite (sheet 4), otherwise use original
            string builtSprite = BuildingAnimationData.GetConstructionSprite(
                buildingType,
                building.Race,
                4 // 100% complete
            );

            if (!string.IsNullOrEmpty(builtSprite))
            {
                return builtSprite;
            }

            // Fallback: Original sprite
            return BuildingAnimationData.GetOriginalSprite(building);
        }

        // ===== GET DAMAGED SPRITE =====
        private static string GetDamagedSprite(WarBuilding building, string buildingType)
        {
            // Try to get damage sprite for current damage level
            string damageSprite = BuildingAnimationData.GetDamageSprite(
                buildingType,
                building.Race,
                building.DamageLevel
            );

            // If damage sprite exists, use it
            if (!string.IsNullOrEmpty(damageSprite))
            {
                return damageSprite;
            }

            // Fallback: Use built sprite (damage overlay can be added in Phase 4/5)
            return GetBuiltSprite(building, buildingType);
        }

        // ===== GET DESTROYED SPRITE =====
        private static string GetDestroyedSprite(WarBuilding building, string buildingType)
        {
            // Try to get rubble sprite
            string destroyedSprite = BuildingAnimationData.GetDestroyedSprite(
                buildingType,
                building.Race
            );

            if (!string.IsNullOrEmpty(destroyedSprite))
            {
                return destroyedSprite;
            }

            // Fallback: Use original sprite at very low opacity (handled in Razor)
            // Or return empty string to hide building completely
            return BuildingAnimationData.GetOriginalSprite(building);
        }

        // ===== HELPER: GET CONSTRUCTION PROGRESS PERCENTAGE =====
        /// <summary>
        /// Returns construction progress as 0-100 percentage
        /// Useful for progress bars in UI
        /// </summary>
        public static int GetConstructionProgressPercentage(WarBuilding building)
        {
            if (building.IsConstructed) return 100;
            if (building.TotalConstructionTime <= 0) return 0;

            return ConstructionProgress.GetProgressPercentage(
                building.ConstructionTimeRemaining,
                building.TotalConstructionTime
            );
        }

        // ===== HELPER: GET HP PERCENTAGE =====
        /// <summary>
        /// Returns HP as 0-100 percentage
        /// Useful for HP bars in UI
        /// </summary>
        public static int GetHPPercentage(WarBuilding building)
        {
            if (building.MaxHP <= 0) return 0;
            return (int)((float)building.HP / building.MaxHP * 100f);
        }

        // ===== HELPER: SHOULD SHOW SMOKE EFFECT =====
        /// <summary>
        /// Determines if building should show smoke particle effect
        /// Typically for medium/heavy damage (< 50% HP)
        /// </summary>
        public static bool ShouldShowSmoke(WarBuilding building)
        {
            if (building.CurrentBuildingState != BuildingAnimationState.Damaged) return false;
            return building.DamageLevel >= 2; // Medium or heavy damage
        }

        // ===== HELPER: SHOULD SHOW FIRE EFFECT =====
        /// <summary>
        /// Determines if building should show fire particle effect
        /// Typically for heavy damage (< 25% HP)
        /// </summary>
        public static bool ShouldShowFire(WarBuilding building)
        {
            if (building.CurrentBuildingState != BuildingAnimationState.Damaged) return false;
            return building.DamageLevel >= 3; // Heavy damage only
        }

        // ===== DEBUG: GET STATE INFO =====
        /// <summary>
        /// Returns debug string with building animation state info
        /// </summary>
        public static string GetDebugInfo(WarBuilding building)
        {
            return $"{building.PlaceholderName}: State={building.CurrentBuildingState}, " +
                   $"HP={building.HP}/{building.MaxHP} ({GetHPPercentage(building)}%), " +
                   $"Construction={GetConstructionProgressPercentage(building)}%, " +
                   $"Sheet={building.ConstructionSpriteSheet}, " +
                   $"DamageLevel={building.DamageLevel}";
        }
    }
}