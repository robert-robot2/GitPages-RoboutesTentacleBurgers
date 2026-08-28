using System;

namespace WarCraftLibrary
{
    // ===== TILE TYPE ENUM =====
    public enum TileType
    {
        // Basic terrain
        Grass = 0,
        Dirt = 1,
        Road = 2,
        Water = 3,
        Rock = 4,
        Sand = 5,

        // Variants (for visual variety)
        Grass2 = 10,
        Grass3 = 11,
        Dirt2 = 12,
        Dirt3 = 13,

        // Special tiles
        Bridge = 20,
        Ruins = 21,
        Lava = 22,

        // Forest tiles
        ForestGrass = 30,
        ForestDirt = 31,

        // Swamp tiles
        SwampWater = 40,
        SwampMud = 41,

        // Dungeon tiles
        DungeonFloor = 50,
        DungeonWall = 51
    }

    // ===== TILE CLASS =====
    public class WarTile
    {
        public int TileId { get; set; }
        public TileType Type { get; set; }
        public bool IsWalkable { get; set; } = true;
        public int PathfindingCost { get; set; } = 1; // Default movement cost

        // Atlas position (calculated from TileId)
        public int AtlasX { get; set; }
        public int AtlasY { get; set; }

        // Constructor
        public WarTile(int tileId, TileType type, bool walkable = true, int cost = 1)
        {
            TileId = tileId;
            Type = type;
            IsWalkable = walkable;
            PathfindingCost = cost;

            // Calculate atlas position (assuming 8x8 atlas grid)
            AtlasX = (tileId % 8) * 32;
            AtlasY = (tileId / 8) * 32;
        }

        // Static helper: Get default tile properties by type
        public static WarTile CreateFromType(TileType type)
        {
            return type switch
            {
                TileType.Grass => new WarTile(0, TileType.Grass, true, 1),
                TileType.Dirt => new WarTile(1, TileType.Dirt, true, 1),
                TileType.Road => new WarTile(2, TileType.Road, true, 1),
                TileType.Water => new WarTile(3, TileType.Water, false, 99), // Not walkable
                TileType.Rock => new WarTile(4, TileType.Rock, false, 99),
                TileType.Sand => new WarTile(5, TileType.Sand, true, 2),

                // Variants
                TileType.Grass2 => new WarTile(10, TileType.Grass2, true, 1),
                TileType.Grass3 => new WarTile(11, TileType.Grass3, true, 1),

                // Default
                _ => new WarTile(0, TileType.Grass, true, 1)
            };
        }
    }
}