using System;
using System.Collections.Generic;
using System.Linq;

namespace WarCraftLibrary
{
    public static class WarCollision
    {
        // ===== CHECK IF POSITION BLOCKED (buildings, trees, mines) =====
        public static bool IsPositionBlocked(int x, int y, int width, int height, WarEntity? ignoreEntity = null)
        {
            foreach (var building in WarRegistry.Buildings)
            {
                if (ignoreEntity != null && building == ignoreEntity) continue;

                bool overlapsX = x < building.PosX + building.Width && x + width > building.PosX;
                bool overlapsY = y < building.PosY + building.Height && y + height > building.PosY;

                if (overlapsX && overlapsY) return true;
            }

            foreach (var tree in WarRegistry.Trees)
            {
                if (tree.IsChopped) continue;
                if (ignoreEntity != null && tree == ignoreEntity) continue;

                bool overlapsX = x < tree.PosX + tree.Width && x + width > tree.PosX;
                bool overlapsY = y < tree.PosY + tree.Height && y + height > tree.PosY;

                if (overlapsX && overlapsY) return true;
            }

            foreach (var mine in WarRegistry.GoldMines)
            {
                if (ignoreEntity != null && mine == ignoreEntity) continue;

                bool overlapsX = x < mine.PosX + mine.Width && x + width > mine.PosX;
                bool overlapsY = y < mine.PosY + mine.Height && y + height > mine.PosY;

                if (overlapsX && overlapsY) return true;
            }

            return false;
        }

        // ===== CHECK IF POSITION BLOCKED BY OTHER UNITS =====
        public static bool IsPositionBlockedByUnit(int x, int y, int width, int height, WarUnit ignoreUnit = null)
        {
            foreach (var other in WarRegistry.Units)
            {
                if (other == ignoreUnit || other.State == UnitState.Dead) continue;

                bool overlapsX = x < other.PosX + other.Width && x + width > other.PosX;
                bool overlapsY = y < other.PosY + other.Height && y + height > other.PosY;

                if (overlapsX && overlapsY) return true;
            }
            return false;
        }

        // ===== GET BUILDING AT POSITION =====
        public static WarBuilding? GetBuildingAt(int x, int y, int width, int height)
        {
            foreach (var building in WarRegistry.Buildings)
            {
                bool overlapsX = x < building.PosX + building.Width && x + width > building.PosX;
                bool overlapsY = y < building.PosY + building.Height && y + height > building.PosY;

                if (overlapsX && overlapsY) return building;
            }
            return null;
        }

        // ===== FIND WAYPOINT AROUND OBSTACLE (BUILDING OR UNIT) =====
        public static (int x, int y) FindWaypointAroundObstacle(WarUnit unit, WarEntity obstacle, int finalTargetX, int finalTargetY)
        {
            int padding = unit.Width + 15;
            int obstacleCenterX = obstacle.PosX + obstacle.Width / 2;
            int obstacleCenterY = obstacle.PosY + obstacle.Height / 2;

            var points = new[]
            {
                (x: obstacle.PosX - padding, y: obstacleCenterY),
                (x: obstacle.PosX + obstacle.Width + padding, y: obstacleCenterY),
                (x: obstacleCenterX, y: obstacle.PosY - padding),
                (x: obstacleCenterX, y: obstacle.PosY + obstacle.Height + padding),
            };

            var valid = points
                .Where(p => !IsPositionBlocked(p.x, p.y, unit.Width, unit.Height, obstacle)
                         && !IsPositionBlockedByUnit(p.x, p.y, unit.Width, unit.Height, unit))
                .OrderBy(p => Math.Sqrt(Math.Pow(p.x - finalTargetX, 2) + Math.Pow(p.y - finalTargetY, 2)))
                .ToList();

            if (valid.Count == 0)
            {
                int ux = unit.PosX + unit.Width / 2;
                int uy = unit.PosY + unit.Height / 2;
                int ox = obstacle.PosX + obstacle.Width / 2;
                int oy = obstacle.PosY + obstacle.Height / 2;
                return (ux + (ux - ox) * 3, uy + (uy - oy) * 3);
            }

            return (valid[0].x, valid[0].y);
        }

        // ===== APPLY UNIT COLLISION =====
        public static void ApplyUnitCollision(WarUnit unit)
        {
            foreach (var other in WarRegistry.Units)
            {
                if (other == unit || other.State == UnitState.Dead) continue;

                int dx = (unit.PosX + unit.Width / 2) - (other.PosX + other.Width / 2);
                int dy = (unit.PosY + unit.Height / 2) - (other.PosY + other.Height / 2);
                double distSq = dx * dx + dy * dy;
                double minDist = (unit.Width + other.Width) / 2.0; // strong separation

                if (distSq < minDist * minDist && distSq > 0)
                {
                    double dist = Math.Sqrt(distSq);
                    double overlap = minDist - dist;

                    // Strong push, works for idle or moving units
                    double pushX = dx / dist * overlap * 0.5;
                    double pushY = dy / dist * overlap * 0.5;

                    unit.PosX += (int)pushX;
                    unit.PosY += (int)pushY;
                    other.PosX -= (int)pushX;
                    other.PosY -= (int)pushY;
                }
            }
        }
    }
}
