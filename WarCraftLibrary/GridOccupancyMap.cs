using System;
using System.Collections.Generic;

namespace WarCraftLibrary
{
    // === OCCUPANCY TYPE ENUM ===
    public enum OccupancyType
    {
        Empty,
        Building,
        Unit,
        Tree,
        GoldMine,
        Unwalkable  // Water, rock from TileMap
    }

    // === OCCUPANCY CELL STRUCT ===
    public struct OccupancyCell
    {
        public OccupancyType Type { get; set; }
        public WarEntity? Occupant { get; set; }  // Reference to what's there
        public bool IsWalkable { get; set; }
    }

    // === MAIN GRID OCCUPANCY MAP (STATIC SINGLETON) ===
    public static class GridOccupancyMap
    {
        // Grid data
        private static OccupancyCell[,]? _grid;
        private static int _gridWidth = 128;
        private static int _gridHeight = 128;
        private static bool _isInitialized = false;

        // === INITIALIZATION ===
        public static void Initialize(WarTileMap tileMap)
        {
            // Create empty grid
            _grid = new OccupancyCell[_gridWidth, _gridHeight];

            // Initialize all cells as empty
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    _grid[x, y] = new OccupancyCell
                    {
                        Type = OccupancyType.Empty,
                        Occupant = null,
                        IsWalkable = true
                    };
                }
            }

            // Mark unwalkable tiles from TileMap
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (!tileMap.IsTileWalkable(x, y))
                    {
                        _grid[x, y] = new OccupancyCell
                        {
                            Type = OccupancyType.Unwalkable,
                            Occupant = null,
                            IsWalkable = false
                        };
                    }
                }
            }

            _isInitialized = true;
            Console.WriteLine($"✅ GridOccupancyMap initialized: {_gridWidth}x{_gridHeight} = {_gridWidth * _gridHeight} cells");
        }

        // === QUERY: IS CELL FREE? ===
        public static bool IsCellFree(int gridX, int gridY)
        {
            if (!_isInitialized || _grid == null) return false;

            // Bounds check
            if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
                return false;

            return _grid[gridX, gridY].IsWalkable && _grid[gridX, gridY].Type == OccupancyType.Empty;
        }

        // === MARK: OCCUPY A CELL ===
        public static void MarkCell(int gridX, int gridY, WarEntity entity, OccupancyType type)
        {
            if (!_isInitialized || _grid == null) return;

            // Bounds check
            if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
                return;

            _grid[gridX, gridY] = new OccupancyCell
            {
                Type = type,
                Occupant = entity,
                IsWalkable = false
            };
        }

        // === CLEAR: FREE A CELL ===
        public static void ClearCell(int gridX, int gridY)
        {
            if (!_isInitialized || _grid == null) return;

            // Bounds check
            if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
                return;

            // Don't clear unwalkable terrain
            if (_grid[gridX, gridY].Type == OccupancyType.Unwalkable)
                return;

            _grid[gridX, gridY] = new OccupancyCell
            {
                Type = OccupancyType.Empty,
                Occupant = null,
                IsWalkable = true
            };
        }

        // === GET ADJACENT FREE CELLS (8 directions) ===
        public static List<(int x, int y)> GetAdjacentFreeCells(int gridX, int gridY)
        {
            var freeCells = new List<(int x, int y)>();

            if (!_isInitialized || _grid == null) return freeCells;

            // Check all 8 adjacent cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    // Skip center cell
                    if (dx == 0 && dy == 0) continue;

                    int checkX = gridX + dx;
                    int checkY = gridY + dy;

                    if (IsCellFree(checkX, checkY))
                    {
                        freeCells.Add((checkX, checkY));
                    }
                }
            }

            return freeCells;
        }

        // === MARK BUILDING (occupies multiple cells) ===
        public static void MarkBuilding(WarBuilding building)
        {
            if (!_isInitialized || _grid == null) return;

            int startGridX = building.PosX / 32;
            int startGridY = building.PosY / 32;
            int endGridX = (building.PosX + building.Width) / 32;
            int endGridY = (building.PosY + building.Height) / 32;

            for (int x = startGridX; x < endGridX; x++)
            {
                for (int y = startGridY; y < endGridY; y++)
                {
                    MarkCell(x, y, building, OccupancyType.Building);
                }
            }

            Console.WriteLine($"🏗️ Marked building {building.PlaceholderName} at grid ({startGridX},{startGridY}) to ({endGridX},{endGridY})");
        }

        // === CLEAR BUILDING ===
        public static void ClearBuilding(WarBuilding building)
        {
            if (!_isInitialized || _grid == null) return;

            int startGridX = building.PosX / 32;
            int startGridY = building.PosY / 32;
            int endGridX = (building.PosX + building.Width) / 32;
            int endGridY = (building.PosY + building.Height) / 32;

            for (int x = startGridX; x < endGridX; x++)
            {
                for (int y = startGridY; y < endGridY; y++)
                {
                    ClearCell(x, y);
                }
            }

            Console.WriteLine($"🗑️ Cleared building {building.PlaceholderName} from grid");
        }

        // === MARK TREE ===
        public static void MarkTree(Tree tree)
        {
            if (!_isInitialized || _grid == null) return;

            int gridX = tree.PosX / 32;
            int gridY = tree.PosY / 32;

            MarkCell(gridX, gridY, tree, OccupancyType.Tree);
        }

        // === CLEAR TREE (when chopped) ===
        public static void ClearTree(Tree tree)
        {
            if (!_isInitialized || _grid == null) return;

            int gridX = tree.PosX / 32;
            int gridY = tree.PosY / 32;

            ClearCell(gridX, gridY);

            Console.WriteLine($"🪓 Tree at grid ({gridX},{gridY}) cleared - now walkable");
        }

        // === MARK GOLD MINE ===
        public static void MarkGoldMine(GoldMine mine)
        {
            if (!_isInitialized || _grid == null) return;

            // Gold mine is 3x3 (96x96 pixels)
            int startGridX = mine.PosX / 32;
            int startGridY = mine.PosY / 32;
            int endGridX = (mine.PosX + mine.Width) / 32;
            int endGridY = (mine.PosY + mine.Height) / 32;

            for (int x = startGridX; x < endGridX; x++)
            {
                for (int y = startGridY; y < endGridY; y++)
                {
                    MarkCell(x, y, mine, OccupancyType.GoldMine);
                }
            }

            Console.WriteLine($"⛏️ Marked gold mine at grid ({startGridX},{startGridY})");
        }

        // === UPDATE UNIT POSITIONS (called every tick) ===
        public static void UpdateUnitPositions()
        {
            if (!_isInitialized || _grid == null) return;

            // Clear all unit occupancy
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_grid[x, y].Type == OccupancyType.Unit)
                    {
                        _grid[x, y] = new OccupancyCell
                        {
                            Type = OccupancyType.Empty,
                            Occupant = null,
                            IsWalkable = true
                        };
                    }
                }
            }

            // Re-mark all living units
            foreach (var unit in WarRegistry.Units)
            {
                if (unit.State != UnitState.Dead)
                {
                    int gridX = unit.PosX / 32;
                    int gridY = unit.PosY / 32;

                    // Only mark if in bounds
                    if (gridX >= 0 && gridX < _gridWidth && gridY >= 0 && gridY < _gridHeight)
                    {
                        MarkCell(gridX, gridY, unit, OccupancyType.Unit);
                    }
                }
            }
        }

        // === DEBUG: GET CELL INFO ===
        public static string GetCellDebugInfo(int gridX, int gridY)
        {
            if (!_isInitialized || _grid == null) return "Grid not initialized";

            if (gridX < 0 || gridX >= _gridWidth || gridY < 0 || gridY >= _gridHeight)
                return "Out of bounds";

            var cell = _grid[gridX, gridY];
            return $"Grid({gridX},{gridY}): {cell.Type}, Walkable:{cell.IsWalkable}, Occupant:{cell.Occupant?.PlaceholderName ?? "None"}";
        }
    }
}