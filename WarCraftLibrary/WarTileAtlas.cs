using System;
using System.Collections.Generic;

namespace WarCraftLibrary
{
    // ===== TILE ATLAS MANAGER =====
    public class WarTileAtlas
    {
        // Atlas image path
        public string AtlasPath { get; set; } = "/wc1sprites/tilesets/tile_atlas_256.png";

        // Atlas dimensions
        public int AtlasWidth { get; set; } = 256;  // 8 tiles * 32px
        public int AtlasHeight { get; set; } = 256; // 8 tiles * 32px
        public int TileSize { get; set; } = 32;
        public int TilesPerRow { get; set; } = 8;

        // Tile registry (TileId -> WarTile)
        private Dictionary<int, WarTile> _tileRegistry = new();

        // Constructor
        public WarTileAtlas(string atlasPath = "/wc1sprites/tilesets/tile_atlas_256.png")
        {
            AtlasPath = atlasPath;
            InitializeDefaultTiles();
        }

        // Initialize default tile registry
        private void InitializeDefaultTiles()
        {
            // Register all tile types
            RegisterTile(0, TileType.Grass);
            RegisterTile(1, TileType.Dirt);
            RegisterTile(2, TileType.Road);
            RegisterTile(3, TileType.Water);
            RegisterTile(4, TileType.Rock);
            RegisterTile(5, TileType.Sand);
            RegisterTile(10, TileType.Grass2);
            RegisterTile(11, TileType.Grass3);
            RegisterTile(12, TileType.Dirt2);
            RegisterTile(13, TileType.Dirt3);
            RegisterTile(20, TileType.Bridge);
            RegisterTile(30, TileType.ForestGrass);
            RegisterTile(31, TileType.ForestDirt);
            RegisterTile(40, TileType.SwampWater);
            RegisterTile(41, TileType.SwampMud);
            RegisterTile(50, TileType.DungeonFloor);
            RegisterTile(51, TileType.DungeonWall);
        }

        // Register a tile
        private void RegisterTile(int tileId, TileType type)
        {
            _tileRegistry[tileId] = WarTile.CreateFromType(type);
        }

        // Get tile by ID
        public WarTile GetTile(int tileId)
        {
            if (_tileRegistry.ContainsKey(tileId))
            {
                return _tileRegistry[tileId];
            }

            // Default to grass if not found
            return _tileRegistry[0];
        }

        // Get atlas position for a tile ID
        public (int x, int y) GetAtlasPosition(int tileId)
        {
            var tile = GetTile(tileId);
            return (tile.AtlasX, tile.AtlasY);
        }

        // Get CSS background-position string
        public string GetBackgroundPosition(int tileId)
        {
            var (x, y) = GetAtlasPosition(tileId);
            return $"-{x}px -{y}px";
        }

        // Check if tile is walkable
        public bool IsTileWalkable(int tileId)
        {
            return GetTile(tileId).IsWalkable;
        }

        // Get pathfinding cost for tile
        public int GetTilePathCost(int tileId)
        {
            return GetTile(tileId).PathfindingCost;
        }
    }
}