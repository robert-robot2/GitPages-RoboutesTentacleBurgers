using System;
using System.Text.Json;
using System.IO;

namespace WarCraftLibrary
{
    // ===== TILE MAP - 128x128 GRID =====
    public class WarTileMap
    {
        // Tile grid (128x128 = 16,384 tiles)
        public int[,] TileGrid { get; set; }

        // Map dimensions
        public int GridWidth { get; set; } = 128;
        public int GridHeight { get; set; } = 128;
        public int TileSize { get; set; } = 32;

        // Reference to atlas
        public WarTileAtlas Atlas { get; set; }

        // Constructor
        public WarTileMap(WarTileAtlas atlas)
        {
            Atlas = atlas;
            TileGrid = new int[GridWidth, GridHeight];
            InitializeDefaultMap();
        }

        // Initialize with default tiles (all grass for now)
        private void InitializeDefaultMap()
        {
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    TileGrid[x, y] = 0; // Default to Grass (TileId 0)
                }
            }

            Console.WriteLine($"✅ Initialized tile map: {GridWidth}x{GridHeight} = {GridWidth * GridHeight} tiles");
        }

        // ===== INDIVIDUAL TILE ASSIGNMENT =====
        // UPDATE SetTile method
        public void SetTile(int x, int y, int tileId)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
            {
                Console.WriteLine($"❌ SetTile: Out of bounds ({x}, {y})");
                return;
            }

            TileGrid[x, y] = tileId;

            // ✅ NEW: Update style cache for this tile
            if (TileStyleCache.Count > 0) // Only if cache exists
            {
                string bgPos = Atlas.GetBackgroundPosition(tileId);
                TileStyleCache[(x, y)] = $"position:absolute; left:{x * TileSize}px; top:{y * TileSize}px; " +
                                         $"width:{TileSize}px; height:{TileSize}px; " +
                                         $"background-image:url('{Atlas.AtlasPath}'); " +
                                         $"background-position:{bgPos}; pointer-events:none;";
            }
        }

        // Overload: Set tile by TileType enum
        public void SetTile(int x, int y, TileType tileType)
        {
            var tile = WarTile.CreateFromType(tileType);
            SetTile(x, y, tile.TileId);
        }

        // ===== BULK REGION ASSIGNMENT =====
        public void SetTileRegion(int x1, int y1, int x2, int y2, int tileId)
        {
            // Ensure x1 <= x2 and y1 <= y2
            int startX = Math.Min(x1, x2);
            int endX = Math.Max(x1, x2);
            int startY = Math.Min(y1, y2);
            int endY = Math.Max(y1, y2);

            // Clamp to grid bounds
            startX = Math.Clamp(startX, 0, GridWidth - 1);
            endX = Math.Clamp(endX, 0, GridWidth - 1);
            startY = Math.Clamp(startY, 0, GridHeight - 1);
            endY = Math.Clamp(endY, 0, GridHeight - 1);

            int tilesSet = 0;
            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    TileGrid[x, y] = tileId;
                    tilesSet++;
                }
            }

            Console.WriteLine($"✅ SetTileRegion: Set {tilesSet} tiles to TileId {tileId} in region ({startX},{startY}) to ({endX},{endY})");
        }

        // Overload: Set region by TileType enum
        public void SetTileRegion(int x1, int y1, int x2, int y2, TileType tileType)
        {
            var tile = WarTile.CreateFromType(tileType);
            SetTileRegion(x1, y1, x2, y2, tile.TileId);
        }

        // ===== GET TILE =====
        public int GetTile(int x, int y)
        {
            if (x < 0 || x >= GridWidth || y < 0 || y >= GridHeight)
            {
                return 0; // Default to grass if out of bounds
            }

            return TileGrid[x, y];
        }

        // ===== FILL PATTERN (BONUS: Random tile variants) =====
        public void FillPattern(int x1, int y1, int x2, int y2, int[] tileIds)
        {
            Random rand = new Random();

            int startX = Math.Clamp(Math.Min(x1, x2), 0, GridWidth - 1);
            int endX = Math.Clamp(Math.Max(x1, x2), 0, GridWidth - 1);
            int startY = Math.Clamp(Math.Min(y1, y2), 0, GridHeight - 1);
            int endY = Math.Clamp(Math.Max(y1, y2), 0, GridHeight - 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    int randomTileId = tileIds[rand.Next(tileIds.Length)];
                    TileGrid[x, y] = randomTileId;
                }
            }

            Console.WriteLine($"✅ FillPattern: Filled region with random pattern");
        }

        // ===== SAVE/LOAD (JSON SERIALIZATION) =====
        public string SerializeToJson()
        {
            // Convert 2D array to 1D for JSON
            int[] flatArray = new int[GridWidth * GridHeight];
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    flatArray[x * GridHeight + y] = TileGrid[x, y];
                }
            }

            var mapData = new
            {
                Width = GridWidth,
                Height = GridHeight,
                Tiles = flatArray
            };

            return JsonSerializer.Serialize(mapData);
        }

        public void LoadFromJson(string json)
        {
            var mapData = JsonSerializer.Deserialize<JsonElement>(json);

            int width = mapData.GetProperty("Width").GetInt32();
            int height = mapData.GetProperty("Height").GetInt32();
            var tiles = mapData.GetProperty("Tiles");

            if (width != GridWidth || height != GridHeight)
            {
                Console.WriteLine($"❌ Map size mismatch: Expected {GridWidth}x{GridHeight}, got {width}x{height}");
                return;
            }

            int index = 0;
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    TileGrid[x, y] = tiles[index].GetInt32();
                    index++;
                }
            }

            Console.WriteLine($"✅ Loaded tile map from JSON");
        }

        // ===== HELPER: Check if tile is walkable =====
        public bool IsTileWalkable(int x, int y)
        {
            int tileId = GetTile(x, y);
            return Atlas.IsTileWalkable(tileId);
        }

        // ===== HELPER: Get tile pathfinding cost =====
        public int GetTilePathCost(int x, int y)
        {
            int tileId = GetTile(x, y);
            return Atlas.GetTilePathCost(tileId);
        }

        // ===== CACHED TILE STYLES FOR RENDERING =====
        public Dictionary<(int x, int y), string> TileStyleCache { get; private set; } = new();

        // Pre-calculate all tile styles (call this once after setting tiles)
        public void BuildStyleCache()
        {
            TileStyleCache.Clear();

            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    int tileId = TileGrid[x, y];
                    string bgPos = Atlas.GetBackgroundPosition(tileId);

                    // Pre-build the entire style string
                    string style = $"position:absolute; " +
                                  $"left:{x * TileSize}px; " +
                                  $"top:{y * TileSize}px; " +
                                  $"width:{TileSize}px; " +
                                  $"height:{TileSize}px; " +
                                  $"background-image:url('{Atlas.AtlasPath}'); " +
                                  $"background-position:{bgPos}; " +
                                  $"pointer-events:none;";

                    TileStyleCache[(x, y)] = style;
                }
            }

            Console.WriteLine($"✅ Built style cache for {TileStyleCache.Count} tiles");
        }

        // Get cached style for a tile
        public string GetTileStyle(int x, int y)
        {
            if (TileStyleCache.TryGetValue((x, y), out string? style))
            {
                return style;
            }

            // Fallback if not cached
            int tileId = GetTile(x, y);
            string bgPos = Atlas.GetBackgroundPosition(tileId);
            return $"position:absolute; left:{x * TileSize}px; top:{y * TileSize}px; " +
                   $"width:{TileSize}px; height:{TileSize}px; " +
                   $"background-image:url('{Atlas.AtlasPath}'); " +
                   $"background-position:{bgPos}; pointer-events:none;";
        }
        // ===== GENERATE STATIC CSS FOR ALL TILES =====
        // ===== GENERATE SHARED CSS FOR TILE TYPES (OPTIMIZED) =====
        public string GenerateTileCSS()
        {
            StringBuilder css = new StringBuilder();
            css.AppendLine("<style>");

            // Get all unique tile IDs actually used in the map
            HashSet<int> usedTileIds = new HashSet<int>();
            for (int x = 0; x < GridWidth; x++)
            {
                for (int y = 0; y < GridHeight; y++)
                {
                    usedTileIds.Add(TileGrid[x, y]);
                }
            }

            // Generate ONE class per tile type (not per position)
            foreach (int tileId in usedTileIds)
            {
                string bgPos = Atlas.GetBackgroundPosition(tileId);
                css.AppendLine($".tile-type-{tileId} {{ position:absolute; width:{TileSize}px; height:{TileSize}px; background-image:url('{Atlas.AtlasPath}'); background-position:{bgPos}; pointer-events:none; }}");
            }

            css.AppendLine("</style>");
            Console.WriteLine($"✅ Generated CSS for {usedTileIds.Count} unique tile types (instead of {GridWidth * GridHeight} positions)");
            return css.ToString();
        }

    }
}