using System;
using System.Collections.Generic;
using System.Text;

namespace WarCraftLibrary
{
    public static class WarPathfinding
    {
        // ===== CONSTANTS =====
        public const int TREE_SEARCH_RADIUS = 300; // Pixels to search for new trees

        public static void MoveToward(WarUnit unit)
        {
            // ===== STEP 1: WAYPOINT COOLDOWN =====
            if (unit.WaypointTimeout > 0 && unit.WaypointX == null)
                unit.WaypointTimeout--;

            // ===== STEP 3: DETERMINE CURRENT TARGET =====
            int targetX = unit.WaypointX ?? unit.TargetX ?? unit.PosX;
            int targetY = unit.WaypointY ?? unit.TargetY ?? unit.PosY;

            if (targetX == unit.PosX && targetY == unit.PosY)
            {
                if (unit.TargetX == null && unit.TargetY == null && unit.State == UnitState.Moving)
                    unit.State = UnitState.Idle;
                return;
            }

            // ===== STEP 4: CALCULATE DIRECTION =====
            int deltaX = targetX - unit.PosX;
            int deltaY = targetY - unit.PosY;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            double arrivalThreshold = unit.WaypointX != null ? 20.0 : unit.MoveSpeed;

            if (distance < arrivalThreshold)
            {
                if (unit.WaypointX != null)
                {
                    unit.WaypointX = null;
                    unit.WaypointY = null;
                    unit.WaypointTimeout = 30;
                    return;
                }
                else
                {
                    unit.PosX = targetX;
                    unit.PosY = targetY;
                    unit.TargetX = null;
                    unit.TargetY = null;
                    if (unit.State == UnitState.Moving || unit.State == UnitState.Attacking)
                        unit.State = UnitState.Idle;
                    return;
                }
            }

            unit.CurrentDirection = WarAnimations.CalculateDirection(deltaX, deltaY);

            // ===== STEP 6: CALCULATE MOVE VECTOR =====
            double moveX = (deltaX / distance) * unit.MoveSpeed;
            double moveY = (deltaY / distance) * unit.MoveSpeed;

            int nextPosX = unit.PosX + (int)moveX;
            int nextPosY = unit.PosY + (int)moveY;
            /*
            // ===== STEP 8: BUILDING COLLISION CHECK WITH SLIDE =====
            WarEntity? ignoreBuilding = null;
            if (unit.State == UnitState.ReturningResources && unit.TargetTownHall != null &&
                Math.Sqrt(Math.Pow(unit.PosX - unit.TargetTownHall.PosX, 2) + Math.Pow(unit.PosY - unit.TargetTownHall.PosY, 2)) < 50)
                ignoreBuilding = unit.TargetTownHall;
            else if (unit.State == UnitState.GatheringGold && unit.TargetMine != null &&
                Math.Sqrt(Math.Pow(unit.PosX - unit.TargetMine.PosX, 2) + Math.Pow(unit.PosY - unit.TargetMine.PosY, 2)) < 50)
                ignoreBuilding = unit.TargetMine;

            if (WarCollision.IsPositionBlocked(nextPosX, nextPosY, unit.Width, unit.Height, ignoreBuilding))
            {
                // --- Try sliding ---
                int slideX = unit.PosX + (int)moveX;
                int slideY = unit.PosY;
                int slideX2 = unit.PosX;
                int slideY2 = unit.PosY + (int)moveY;

                bool canSlideX = !WarCollision.IsPositionBlocked(slideX, slideY, unit.Width, unit.Height, ignoreBuilding);
                bool canSlideY = !WarCollision.IsPositionBlocked(slideX2, slideY2, unit.Width, unit.Height, ignoreBuilding);

                if (canSlideX) nextPosX = slideX;
                else if (canSlideY) nextPosY = slideY2;
                else
                {
                    // fallback to existing waypoint detour
                    if (unit.WaypointX == null && unit.WaypointTimeout == 0)
                    {
                        var obstacle = WarCollision.GetBuildingAt(nextPosX, nextPosY, unit.Width, unit.Height);
                        if (obstacle != null)
                        {
                            int finalX = unit.TargetX ?? unit.PosX;
                            int finalY = unit.TargetY ?? unit.PosY;
                            (int waypointX, int waypointY) = WarCollision.FindWaypointAroundObstacle(unit, obstacle, finalX, finalY);
                            unit.WaypointX = waypointX;
                            unit.WaypointY = waypointY;
                            unit.WaypointTimeout = 0;
                        }
                    }
                    return;
                }
            }
            */
            // ===== STEP 9: APPLY MOVEMENT =====
            unit.PosX = nextPosX;
            unit.PosY = nextPosY;

            // ===== STEP 10: UNIT COLLISION =====
          //  WarCollision.ApplyUnitCollision(unit);
        }

        // ===== CHECK IF UNIT IS NEAR TARGET =====
        public static bool IsNear(WarUnit unit, WarEntity target, int range)
        {
            int centerX = unit.PosX + unit.Width / 2;
            int centerY = unit.PosY + unit.Height / 2;
            int targetCenterX = target.PosX + target.Width / 2;
            int targetCenterY = target.PosY + target.Height / 2;
            int deltaX = targetCenterX - centerX;
            int deltaY = targetCenterY - centerY;
            double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            return distance <= range;
        }

        // ===== FIND NEAREST TREE =====
        public static Tree? FindNearestTree(WarUnit unit, int searchRadius)
        {
            Tree? closestTree = null;
            double closestDistance = double.MaxValue;

            // Calculate unit's center position
            int unitCenterX = unit.PosX + unit.Width / 2;
            int unitCenterY = unit.PosY + unit.Height / 2;

            // Loop through all trees
            foreach (var tree in WarRegistry.Trees)
            {
                // Skip chopped trees
                if (tree.IsChopped)
                    continue;

                // Calculate tree's center position
                int treeCenterX = tree.PosX + tree.Width / 2;
                int treeCenterY = tree.PosY + tree.Height / 2;

                // Calculate distance
                int deltaX = treeCenterX - unitCenterX;
                int deltaY = treeCenterY - unitCenterY;
                double distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                // Check if within search radius
                if (distance > searchRadius)
                    continue;

                // Check if closer than current closest
                if (distance < closestDistance)
                {
                    closestTree = tree;
                    closestDistance = distance;
                }
            }

            // Log result
            if (closestTree != null)
            {
                Console.WriteLine($"🌲 {unit.PlaceholderName}: Found tree at distance {closestDistance:F0}px");
            }
            else
            {
                Console.WriteLine($"❌ {unit.PlaceholderName}: No trees found within {searchRadius}px");
            }

            return closestTree;
        }


        // ===== FIND SMART DETOUR AROUND OBSTACLE =====
        private static (int x, int y)? FindSmartDetour(WarUnit unit, int goalX, int goalY)
        {
            // Get all adjacent free cells
            var freeCells = unit.GetAdjacentFreeCells();

            if (freeCells.Count == 0)
            {
                Console.WriteLine($"⚠️ {unit.PlaceholderName}: No free cells adjacent - stuck!");
                return null;
            }

            // Calculate goal direction
            int goalGridX = goalX / 32;
            int goalGridY = goalY / 32;
            int goalDX = goalGridX - unit.CurrentGridX;
            int goalDY = goalGridY - unit.CurrentGridY;

            // Find best free cell that moves toward goal
            var bestCell = freeCells
                .Select(cell => new
                {
                    Cell = cell,
                    // Calculate how much this cell aligns with goal direction
                    Alignment = ((cell.x - unit.CurrentGridX) * goalDX) +
                               ((cell.y - unit.CurrentGridY) * goalDY),
                    // Calculate distance to goal
                    Distance = Math.Abs(cell.x - goalGridX) + Math.Abs(cell.y - goalGridY)
                })
                .OrderByDescending(c => c.Alignment)  // Prefer cells toward goal
                .ThenBy(c => c.Distance)              // Then prefer closer cells
                .FirstOrDefault();

            if (bestCell != null)
            {
                Console.WriteLine($"🧠 {unit.PlaceholderName}: Detour found at grid ({bestCell.Cell.x}, {bestCell.Cell.y})");
                return bestCell.Cell;
            }

            return null;
        }

        // ===== GET NEAREST BUILDING EDGE POINT =====
        public static (int x, int y) GetNearestBuildingEdge(WarUnit unit, WarBuilding building)
        {
            int unitCenterX = unit.PosX + unit.Width / 2;
            int unitCenterY = unit.PosY + unit.Height / 2;

            // Clamp unit's center position to building's bounding box
            // This gives us the closest point ON the building's edge
            int dropX = Math.Clamp(unitCenterX, building.PosX, building.PosX + building.Width);
            int dropY = Math.Clamp(unitCenterY, building.PosY, building.PosY + building.Height);

            return (dropX, dropY);
        }

        // ===== CHECK IF UNIT IS NEAR BUILDING EDGE =====
        public static bool IsNearBuildingEdge(WarUnit unit, WarBuilding building, int range)
        {
            // Get the nearest point on the building's edge to the unit
            var (edgeX, edgeY) = GetNearestBuildingEdge(unit, building);

            // Calculate distance from unit center to that edge point
            int unitCenterX = unit.PosX + unit.Width / 2;
            int unitCenterY = unit.PosY + unit.Height / 2;

            int dx = edgeX - unitCenterX;
            int dy = edgeY - unitCenterY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            return distance <= range;
        }


    }
}