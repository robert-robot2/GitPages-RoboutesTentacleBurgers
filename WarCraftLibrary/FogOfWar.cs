using System;
using System.Collections.Generic;
using System.Linq;

namespace WarCraftLibrary
{
    /// <summary>
    /// Fog of War System
    /// Handles vision calculations and entity visibility for player faction
    /// Supports future AI fog implementation with faction parameter
    /// </summary>
    public static class FogOfWar
    {
        // ===== VISION RANGE CONSTANTS =====
        /// <summary>
        /// Vision radius for standard units (5 tiles)
        /// </summary>
        public const int UNIT_VISION_RANGE = 160; // 32px * 5 tiles

        /// <summary>
        /// Vision radius for buildings (7 tiles)
        /// </summary>
        public const int BUILDING_VISION_RANGE = 224; // 32px * 7 tiles

        /// <summary>
        /// Vision radius for Town Hall and Castle (9 tiles)
        /// </summary>
        public const int TOWN_HALL_VISION_RANGE = 288; // 32px * 9 tiles

        // ===== VISION SOURCE DATA =====
        /// <summary>
        /// Represents a single source of vision (unit or building)
        /// </summary>
        public class VisionSource
        {
            public int WorldX { get; set; }      // World position X
            public int WorldY { get; set; }      // World position Y
            public int VisionRange { get; set; } // Vision radius in pixels
            public string SourceName { get; set; } = "Unknown"; // For debugging
        }

        // ===== GET ALL VISION SOURCES FOR A FACTION =====
        /// <summary>
        /// Returns all vision sources (units + buildings) for a given faction
        /// </summary>
        /// <param name="faction">The faction to get vision for (Player or AI)</param>
        /// <param name="game">Game service for accessing faction data</param>
        /// <returns>List of vision sources with position and range</returns>
        public static List<VisionSource> GetVisionSources(Faction? faction, WarGameService game)
        {
            var visionSources = new List<VisionSource>();

            if (faction == null) return visionSources;

            // Get player's team number
            int playerTeam = game.Players.Count > 0 ? game.Players[0].Team : 1;

            // ===== ADD UNIT VISION =====
            foreach (var unit in WarRegistry.Units)
            {
                // Skip dead units
                if (unit.State == UnitState.Dead) continue;

                // Include units from this faction OR allied factions (same team)
                bool isOwnUnit = unit.OwnerFaction == faction;
                bool isAllyUnit = (playerTeam > 0 && unit.OwnerTeam == playerTeam);

                if (!isOwnUnit && !isAllyUnit) continue;

                // Calculate unit center position
                int centerX = unit.PosX + unit.Width / 2;
                int centerY = unit.PosY + unit.Height / 2;

                visionSources.Add(new VisionSource
                {
                    WorldX = centerX,
                    WorldY = centerY,
                    VisionRange = UNIT_VISION_RANGE,
                    SourceName = unit.PlaceholderName
                });
            }

            // ===== ADD BUILDING VISION =====
            foreach (var building in WarRegistry.Buildings)
            {
                // Skip destroyed buildings
                if (building.CurrentBuildingState == BuildingAnimationState.Destroyed)
                    continue;

                // Include buildings from this faction OR allied factions (same team)
                bool isOwnBuilding = building.OwnerFaction == faction;
                bool isAllyBuilding = (playerTeam > 0 && building.OwnerTeam == playerTeam);

                if (!isOwnBuilding && !isAllyBuilding) continue;

                // Calculate building center position
                int centerX = building.PosX + building.Width / 2;
                int centerY = building.PosY + building.Height / 2;

                // Town Halls and Castles have extended vision
                int visionRange = (building is TownHall || building is Castle)
                    ? TOWN_HALL_VISION_RANGE
                    : BUILDING_VISION_RANGE;

                visionSources.Add(new VisionSource
                {
                    WorldX = centerX,
                    WorldY = centerY,
                    VisionRange = visionRange,
                    SourceName = building.PlaceholderName
                });
            }

            return visionSources;
        }

        // ===== CHECK IF WORLD POSITION IS VISIBLE =====
        /// <summary>
        /// Checks if a world position is within vision range of any faction's units/buildings
        /// </summary>
        /// <param name="worldX">World X coordinate to check</param>
        /// <param name="worldY">World Y coordinate to check</param>
        /// <param name="faction">Faction whose vision to check</param>
        /// <param name="game">Game service</param>
        /// <returns>True if position is visible, false if in fog</returns>
        public static bool IsPositionVisible(int worldX, int worldY, Faction? faction, WarGameService game)
        {
            if (faction == null) return false;

            // Get all vision sources for this faction
            var visionSources = GetVisionSources(faction, game);

            // Check if position is within range of any vision source
            foreach (var source in visionSources)
            {
                int dx = worldX - source.WorldX;
                int dy = worldY - source.WorldY;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                if (distance <= source.VisionRange)
                {
                    return true; // Position is visible
                }
            }

            return false; // Position is in fog
        }

        // ===== CHECK IF ENTITY IS VISIBLE =====
        /// <summary>
        /// Checks if an entity (unit or building) is visible to a faction
        /// Checks entity's center position
        /// </summary>
        /// <param name="entity">Entity to check (unit or building)</param>
        /// <param name="faction">Faction whose vision to check</param>
        /// <param name="game">Game service</param>
        /// <returns>True if entity is visible, false if in fog</returns>
        public static bool IsEntityVisible(WarEntity entity, Faction? faction, WarGameService game)
        {
            // Calculate entity center position
            int centerX = entity.PosX + entity.Width / 2;
            int centerY = entity.PosY + entity.Height / 2;

            return IsPositionVisible(centerX, centerY, faction, game);
        }

        // ===== OVERLOAD: CHECK IF ENTITY IS VISIBLE (Uses PlayerFaction by default) =====
        /// <summary>
        /// Convenience method - checks if entity is visible to player faction
        /// </summary>
        public static bool IsEntityVisible(WarEntity entity, WarGameService game)
        {
            return IsEntityVisible(entity, game.PlayerFaction, game);
        }

        // ===== GET VISION CIRCLES FOR VIEWPORT RENDERING =====
        /// <summary>
        /// Returns list of vision circles in viewport coordinates (for SVG rendering)
        /// Only returns circles that are visible in current viewport
        /// </summary>
        /// <param name="faction">Faction to get vision circles for</param>
        /// <param name="game">Game service (for camera position)</param>
        /// <returns>List of (viewportX, viewportY, radius) tuples for SVG circles</returns>
        public static List<(int x, int y, int radius)> GetVisionCirclesForViewport(Faction? faction, WarGameService game)
        {
            var circles = new List<(int x, int y, int radius)>();

            if (faction == null) return circles;

            var visionSources = GetVisionSources(faction, game);

            foreach (var source in visionSources)
            {
                // Convert world position to viewport position
                int viewportX = source.WorldX - game.CameraX;
                int viewportY = source.WorldY - game.CameraY;

                // Only include circles that are at least partially visible in viewport
                // (Circle center can be off-screen but circle edge still visible)
                int buffer = source.VisionRange;
                bool isInViewport = viewportX + buffer > 0 &&
                                   viewportX - buffer < game.ViewportWidth &&
                                   viewportY + buffer > 0 &&
                                   viewportY - buffer < game.ViewportHeight;

                if (isInViewport)
                {
                    circles.Add((viewportX, viewportY, source.VisionRange));
                }
            }

            return circles;
        }

        // ===== HELPER: CALCULATE DISTANCE BETWEEN TWO POINTS =====
        /// <summary>
        /// Calculates Euclidean distance between two world positions
        /// </summary>
        public static double CalculateDistance(int x1, int y1, int x2, int y2)
        {
            int dx = x2 - x1;
            int dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // ===== DEBUG: GET VISION INFO =====
        /// <summary>
        /// Returns debug information about vision sources
        /// </summary>
        public static string GetDebugInfo(Faction? faction, WarGameService game)
        {
            if (faction == null) return "No faction";

            var sources = GetVisionSources(faction, game);
            int unitSources = sources.Count(s => s.VisionRange == UNIT_VISION_RANGE);
            int buildingSources = sources.Count(s => s.VisionRange == BUILDING_VISION_RANGE);
            int townHallSources = sources.Count(s => s.VisionRange == TOWN_HALL_VISION_RANGE);

            return $"Vision Sources: {sources.Count} total " +
                   $"({unitSources} units, {buildingSources} buildings, {townHallSources} town halls)";
        }
    }
}