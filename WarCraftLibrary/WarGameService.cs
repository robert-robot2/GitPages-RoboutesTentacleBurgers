using System;
using System.Collections.Generic;
using System.Text;

using System.Timers;
using static WarCraftLibrary.MapLibrary;

namespace WarCraftLibrary
{

    public class WarGameService
    {
        // ===== TILE RENDER CACHING =====
        public List<(int x, int y, int tileId)> CachedVisibleTiles { get; set; } = new();
        private int lastTileCameraX = -999;
        private int lastTileCameraY = -999;
        private const int TILE_UPDATE_THRESHOLD = 64; // Only update tiles if camera moved 64px (2 tiles)


        // ===== UPDATE VISIBLE TILES (CACHED) =====
        public void UpdateVisibleTiles()
        {
            if (TileMap == null) return;

            // Check if camera moved enough to warrant tile update
            int deltaX = Math.Abs(CameraX - lastTileCameraX);
            int deltaY = Math.Abs(CameraY - lastTileCameraY);

            if (deltaX >= TILE_UPDATE_THRESHOLD || deltaY >= TILE_UPDATE_THRESHOLD || CachedVisibleTiles.Count == 0)
            {
                // Recalculate visible tiles
                int tileSize = 32;
                int startX = Math.Max(0, (CameraX - 32) / tileSize);
                int endX = Math.Min(TileMap.GridWidth - 1, (CameraX + ViewportWidth + 32) / tileSize);
                int startY = Math.Max(0, (CameraY - 32) / tileSize);
                int endY = Math.Min(TileMap.GridHeight - 1, (CameraY + ViewportHeight + 32) / tileSize);

                CachedVisibleTiles.Clear();

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        int tileId = TileMap.GetTile(x, y);
                        CachedVisibleTiles.Add((x, y, tileId));
                    }
                }

                lastTileCameraX = CameraX;
                lastTileCameraY = CameraY;

                Console.WriteLine($"🔄 Tiles updated: {CachedVisibleTiles.Count} visible");
            }
        }








        // ===== VIEWPORT CONFIGURATION =====
        public int ViewportWidth { get; set; } = 1024;
        public int ViewportHeight { get; set; } = 768;
        public int MapWidth { get; set; } = 4096;
        public int MapHeight { get; set; } = 4096;

        // Camera Position (top-left corner of viewport)
        public int CameraX { get; set; } = 0;
        public int CameraY { get; set; } = 0;

        // Camera scroll speed
        public int ScrollSpeed { get; set; } = 8;
        public int EdgeScrollThreshold { get; set; } = 20; // Reduced from 20 to 5

        // CSS positioning for map container
        public string ViewportXpx => $"-{CameraX}px";
        public string ViewportYpx => $"-{CameraY}px";


        // ===== MAP CONFIGURATION =====
        public string CurrentMapTileset { get; set; } = "/wc1sprites/tilesets/TestMap0004.png";

        // ===== TILE SYSTEM (ADD THESE NEW PROPERTIES) =====
        public WarTileAtlas TileAtlas { get; set; }
        public WarTileMap TileMap { get; set; }
        // ===== GRID SYSTEM (32x32 cells) =====
        public int GridCellSize { get; set; } = 32;
        public int GridWidth => MapWidth / GridCellSize; // 128 cells
        public int GridHeight => MapHeight / GridCellSize; // 128 cells


        // ===== DEBUG =====
        public bool ShowDebugGrid { get; set; } = false;
        public bool ShowDebugInfo { get; set; } = false;
        public bool FogOfWarEnabled { get; set; } = true; // ✅ NEW: Fog of War toggle

        // ===== UI REFRESH CALLBACK =====
        public Func<Task>? RequestUIRefresh { get; set; }


        // ===== CAMERA MOVEMENT =====
        public void ScrollCamera(int deltaX, int deltaY)
        {
            CameraX = Math.Clamp(CameraX + deltaX, 0, MapWidth - ViewportWidth);
            CameraY = Math.Clamp(CameraY + deltaY, 0, MapHeight - ViewportHeight);
        }

        public void ScrollCameraUp() => ScrollCamera(0, -ScrollSpeed);
        public void ScrollCameraDown() => ScrollCamera(0, ScrollSpeed);
        public void ScrollCameraLeft() => ScrollCamera(-ScrollSpeed, 0);
        public void ScrollCameraRight() => ScrollCamera(ScrollSpeed, 0);


        // ===== COORDINATE CONVERSION =====

        // Convert viewport mouse position to world coordinates
        public (int worldX, int worldY) ViewportToWorld(int viewportX, int viewportY)
        {
            return (CameraX + viewportX, CameraY + viewportY);
        }

        // Convert world coordinates to grid position
        public (int gridX, int gridY) WorldToGrid(int worldX, int worldY)
        {
            return (worldX / GridCellSize, worldY / GridCellSize);
        }

        // Convert grid position to world coordinates (top-left of cell)
        public (int worldX, int worldY) GridToWorld(int gridX, int gridY)
        {
            return (gridX * GridCellSize, gridY * GridCellSize);
        }


        // ===== MINIMAP =====
        public int MinimapSize { get; set; } = 164; // Scales proportionally with new viewport
        public double MinimapScale => (double)MinimapSize / MapWidth; // 128/4096 = 0.03125

        // Camera rectangle on minimap
        public int MinimapCameraX => (int)(CameraX * MinimapScale);
        public int MinimapCameraY => (int)(CameraY * MinimapScale);
        public int MinimapCameraWidth => (int)(ViewportWidth * MinimapScale);
       public int MinimapCameraHeight => (int)(ViewportHeight * MinimapScale);


        // Click minimap to jump camera
        // Click minimap to jump camera
        public void JumpCameraToMinimap(int minimapX, int minimapY)
        {
            int worldX = (int)(minimapX / MinimapScale);
            int worldY = (int)(minimapY / MinimapScale);

            // ✅ UPDATED: Center camera on clicked position with proper bounds checking
            int targetCameraX = worldX - (ViewportWidth / 2);
            int targetCameraY = worldY - (ViewportHeight / 2);

            // Clamp to map boundaries
            CameraX = Math.Clamp(targetCameraX, 0, MapWidth - ViewportWidth);
            CameraY = Math.Clamp(targetCameraY, 0, MapHeight - ViewportHeight);

            Console.WriteLine($"📍 Minimap jump: World({worldX}, {worldY}) → Camera({CameraX}, {CameraY})");
        }

        // ✅ NEW: Adjust camera to keep selected units visible
        public void EnsureSelectedUnitsVisible()
        {
            // If no units selected, nothing to do
            if (WarRegistry.SelectedUnits.Count == 0) return;

            // Calculate bounding box of all selected units
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var unit in WarRegistry.SelectedUnits)
            {
                minX = Math.Min(minX, unit.PosX);
                minY = Math.Min(minY, unit.PosY);
                maxX = Math.Max(maxX, unit.PosX + unit.Width);
                maxY = Math.Max(maxY, unit.PosY + unit.Height);
            }

            // Calculate center of selected units
            int centerX = (minX + maxX) / 2;
            int centerY = (minY + maxY) / 2;

            // Check if center is outside viewport
            bool outsideViewport = centerX < CameraX ||
                                  centerX > CameraX + ViewportWidth ||
                                  centerY < CameraY ||
                                  centerY > CameraY + ViewportHeight;

            if (outsideViewport)
            {
                // Smoothly pan camera to center on selected units
                int targetCameraX = centerX - (ViewportWidth / 2);
                int targetCameraY = centerY - (ViewportHeight / 2);

                CameraX = Math.Clamp(targetCameraX, 0, MapWidth - ViewportWidth);
                CameraY = Math.Clamp(targetCameraY, 0, MapHeight - ViewportHeight);

                Console.WriteLine($"🎯 Adjusted camera to keep selected units visible at ({CameraX}, {CameraY})");
            }
        }

        // ===== TILE SYSTEM CSS =====
        public string TileCSS { get; set; } = "";
        // ===== INITIALIZATION =====
        public void InitializeMap()
        {
            // ===== STEP 1: INITIALIZE TILE SYSTEM FIRST =====
            Console.WriteLine("🗺️ Initializing tile system...");

            // Create tile atlas
            TileAtlas = new WarTileAtlas(CurrentMapTileset);

            // Create tile map
            TileMap = new WarTileMap(TileAtlas);

            // ===== CREATE TEST PATTERN =====
            TileMap.FillPattern(0, 0, 127, 127, new[] { 9, 10, 11 });

            for (int i = 0; i < 40; i++)
            {
                TileMap.SetTile(20 + i, 20 + i, TileType.Road);
                TileMap.SetTile(21 + i, 20 + i, TileType.Dirt);
            }

            TileMap.SetTileRegion(64, 64, 72, 72, TileType.Rock);
            TileMap.SetTileRegion(10, 10, 18, 18, TileType.Water);
            TileMap.SetTileRegion(0, 0, 127, 0, TileType.Sand);
            TileMap.SetTileRegion(0, 127, 127, 127, TileType.Sand);
            TileMap.SetTileRegion(0, 0, 0, 127, TileType.Sand);
            TileMap.SetTileRegion(127, 0, 127, 127, TileType.Sand);

            Console.WriteLine("🎨 Building tile style cache...");
            TileMap.BuildStyleCache();

            Console.WriteLine("🎨 Generating tile CSS...");
            TileCSS = TileMap.GenerateTileCSS();
            Console.WriteLine("✅ Tile system initialized with test pattern");

            // ===== STEP 2: NOW INITIALIZE GRID OCCUPANCY MAP (needs TileMap) =====
            Console.WriteLine("🗺️ Initializing grid occupancy map...");
            GridOccupancyMap.Initialize(TileMap);

            // ===== STEP 3: CLEAR AND SPAWN ENTITIES =====
            WarRegistry.ClearAll();
            WarRegistry.SpawnGoldMines();
            WarRegistry.SpawnTrees();

            // ===== STEP 4: MARK INITIAL ENTITIES ON GRID =====
            Console.WriteLine("📍 Marking initial entities on grid...");

            foreach (var mine in WarRegistry.GoldMines)
            {
                GridOccupancyMap.MarkGoldMine(mine);
            }

            foreach (var tree in WarRegistry.Trees)
            {
                GridOccupancyMap.MarkTree(tree);
            }

            Console.WriteLine($"✅ Grid initialized: {WarRegistry.GoldMines.Count} mines, {WarRegistry.Trees.Count} trees marked");
            Console.WriteLine($"Map initialized: {WarRegistry.GoldMines.Count} gold mines, {WarRegistry.Trees.Count} trees");
        }

        // ===== GAME STATE =====
        public Faction? PlayerFaction { get; set; }
        public Faction? AIFaction { get; set; }
        public GameMap? CurrentMap { get; set; }
        public bool IsGameInitialized { get; set; } = false;

        // ===== PLAYER RESOURCES =====
        public int PlayerGold { get; set; } = 2000;
        public int PlayerLumber { get; set; } = 1000;



        public void InitializeGame(string mapId, RaceType playerRace)
        {
            ClearAll();
            ResetGameState();

            CurrentMap = MapLibrary.GetMap(mapId);
            CurrentMapTileset = CurrentMap.TilesetPath;

            // ===== NEW: Check if we have multiplayer data =====
            if (Players.Count > 0)
            {
                // MULTIPLAYER MODE: Use Players list
                Console.WriteLine($"🎮 MULTIPLAYER MODE: {Players.Count} players");

                // ✅ NEW: Create unique faction instance for each player
                var humanPlayer = Players.FirstOrDefault(p => p.IsHuman);
                if (humanPlayer != null)
                {
                    // Create NEW instance (not shared)
                    var factionTemplate = FactionLibrary.GetFaction(humanPlayer.Race);
                    humanPlayer.Faction = new Faction
                    {
                        Race = factionTemplate.Race,
                        Name = factionTemplate.Name,
                        Description = factionTemplate.Description,
                        BannerPath = factionTemplate.BannerPath,
                        IconPath = factionTemplate.IconPath,
                        IsPlayable = factionTemplate.IsPlayable,
                        StartingGold = factionTemplate.StartingGold,
                        StartingLumber = factionTemplate.StartingLumber,
                        Gold = factionTemplate.StartingGold,
                        Lumber = factionTemplate.StartingLumber,
                        CurrentPop = 5,
                        MaxPop = 5
                    };

                    PlayerFaction = humanPlayer.Faction;
                    PlayerGold = PlayerFaction.StartingGold;
                    PlayerLumber = PlayerFaction.StartingLumber;
                }

                // ✅ NEW: Create unique faction instance for each AI
                var aiPlayers = Players.Where(p => !p.IsHuman).ToList();
                foreach (var aiPlayer in aiPlayers)
                {
                    var factionTemplate = FactionLibrary.GetFaction(aiPlayer.Race);
                    aiPlayer.Faction = new Faction
                    {
                        Race = factionTemplate.Race,
                        Name = factionTemplate.Name,
                        Description = factionTemplate.Description,
                        BannerPath = factionTemplate.BannerPath,
                        IconPath = factionTemplate.IconPath,
                        IsPlayable = factionTemplate.IsPlayable,
                        StartingGold = factionTemplate.StartingGold,
                        StartingLumber = factionTemplate.StartingLumber,
                        Gold = factionTemplate.StartingGold,
                        Lumber = factionTemplate.StartingLumber,
                        CurrentPop = 5,
                        MaxPop = 5
                    };

                    Console.WriteLine($"✅ Created faction for AI Player {aiPlayer.SlotIndex}: {aiPlayer.Faction.Name}");
                }

                // For backward compatibility: Set AIFaction to first AI
                if (aiPlayers.Count > 0)
                {
                    AIFaction = aiPlayers[0].Faction;
                }

                // Generate random spawns
                GenerateRandomSpawns(Players);

                // Initialize map (tiles, resources)
                InitializeMap();

                // Spawn all players
                WarRegistry.SpawnStartingBuildings(Players);

                // Center camera on human player spawn
                if (humanPlayer != null)
                {
                    var (spawnGridX, spawnGridY) = humanPlayer.SpawnPosition;
                    var (spawnWorldX, spawnWorldY) = GridToWorld(spawnGridX, spawnGridY);
                    CameraX = Math.Clamp(spawnWorldX - ViewportWidth / 2, 0, MapWidth - ViewportWidth);
                    CameraY = Math.Clamp(spawnWorldY - ViewportHeight / 2, 0, MapHeight - ViewportHeight);
                }
            }
            else
            {
                // ===== LEGACY MODE: Single player vs single AI =====
                Console.WriteLine($"🎮 LEGACY MODE: 1v1");

                PlayerFaction = FactionLibrary.GetFaction(playerRace);
                PlayerGold = PlayerFaction.StartingGold;
                PlayerLumber = PlayerFaction.StartingLumber;

                AIFaction = playerRace == RaceType.Human
                    ? FactionLibrary.GetFaction(RaceType.Orc)
                    : FactionLibrary.GetFaction(RaceType.Human);

                InitializeMap();

                // Use old spawn system
                WarRegistry.SpawnStartingBuildings(CurrentMap, PlayerFaction.Race, AIFaction.Race);

                var (spawnGridX, spawnGridY) = CurrentMap.PlayerSpawn;
                var (spawnWorldX, spawnWorldY) = GridToWorld(spawnGridX, spawnGridY);
                CameraX = Math.Clamp(spawnWorldX - ViewportWidth / 2, 0, MapWidth - ViewportWidth);
                CameraY = Math.Clamp(spawnWorldY - ViewportHeight / 2, 0, MapHeight - ViewportHeight);
            }

            InitGameTickTimer();
            IsGameInitialized = true;
            GameStartTime = DateTime.Now;

            Console.WriteLine($"✅ Game initialized: {CurrentMap.MapName}");
        }

        // ===== GAME END STATE =====
        public bool IsGameOver { get; set; } = false;
        public string GameEndState { get; set; } = ""; // "VICTORY" or "DEFEAT"

        // ===== STATISTICS TRACKING =====
        public int UnitsTrainedCount { get; set; } = 0;
        public int BuildingsConstructedCount { get; set; } = 0;
        public int EnemyUnitsKilledCount { get; set; } = 0;
        public int EnemyBuildingsDestroyedCount { get; set; } = 0;
        public int TotalGoldGathered { get; set; } = 0;
        public int TotalLumberGathered { get; set; } = 0;
        public DateTime GameStartTime { get; set; }






        // ===== EDGE SCROLLING TIMER =====
        private System.Timers.Timer? edgeScrollTimer;
        public bool IsEdgeScrolling { get; private set; } = false;

        // Edge scroll directions (set by mouse position)
        public bool ScrollingUp { get; set; } = false;
        public bool ScrollingDown { get; set; } = false;
        public bool ScrollingLeft { get; set; } = false;
        public bool ScrollingRight { get; set; } = false;


        // ===== INITIALIZE EDGE SCROLL TIMER =====
        public void InitEdgeScrollTimer()
        {
            if (edgeScrollTimer == null)
            {
                edgeScrollTimer = new System.Timers.Timer(16); // ~60fps
                edgeScrollTimer.Elapsed += (sender, e) => EdgeScrollTick();
                edgeScrollTimer.AutoReset = true;
                edgeScrollTimer.Start();
            }
        }

        // ===== EDGE SCROLL TICK (runs continuously) =====
        private void EdgeScrollTick()
        {
            bool didScroll = false;

            if (ScrollingUp)
            {
                CameraY = Math.Max(0, CameraY - ScrollSpeed);
                didScroll = true;
            }
            if (ScrollingDown)
            {
                CameraY = Math.Min(MapHeight -ViewportHeight, CameraY + ScrollSpeed);
                didScroll = true;
            }
            if (ScrollingLeft)
            {
                CameraX = Math.Max(0, CameraX - ScrollSpeed);
                didScroll = true;
            }
            if (ScrollingRight)
            {
                CameraX = Math.Min(MapWidth - ViewportWidth, CameraX + ScrollSpeed);
                didScroll = true;
            }

            // Request UI update if camera moved
            if (didScroll)
            {
                RequestUIRefresh?.Invoke();
            }
        }

        // ===== UPDATE EDGE SCROLL STATE =====
        public void UpdateEdgeScroll(double mouseX, double mouseY)
        {

            ScrollingLeft = mouseX <= EdgeScrollThreshold;
            ScrollingRight = mouseX >= ViewportWidth - EdgeScrollThreshold;
            ScrollingUp = mouseY <= EdgeScrollThreshold;
            ScrollingDown = mouseY >= ViewportHeight - EdgeScrollThreshold;

            IsEdgeScrolling = ScrollingUp || ScrollingDown || ScrollingLeft || ScrollingRight;


            // ADD DETAILED DEBUG LOGGING
            if (IsEdgeScrolling)
            {
                Console.WriteLine($"🔍 EdgeScroll: Mouse({mouseX:F0},{mouseY:F0}) | " +
                                 $"Up:{ScrollingUp} Down:{ScrollingDown} Left:{ScrollingLeft} Right:{ScrollingRight} | " +
                                 $"Thresholds: <{EdgeScrollThreshold}, >{ViewportWidth - EdgeScrollThreshold}, >{ViewportHeight - EdgeScrollThreshold}");
            }
            Console.WriteLine($"Mouse({mouseX},{mouseY}) Viewport({ViewportWidth},{ViewportHeight})");

        }

        // ===== STOP EDGE SCROLLING (when mouse leaves viewport) =====
        public void StopEdgeScroll()
        {
            ScrollingUp = false;
            ScrollingDown = false;
            ScrollingLeft = false;
            ScrollingRight = false;
            IsEdgeScrolling = false;
        }

        // ===== CLEANUP =====
        public void StopEdgeScrollTimer()
        {
            if (edgeScrollTimer != null)
            {
                edgeScrollTimer.Stop();
                edgeScrollTimer.Dispose();
                edgeScrollTimer = null;  // ✅ ADD THIS LINE
            }
        }

        // ===== ADD TO EXISTING WarGameService.cs =====
        // Add after existing resource properties
        public int PlayerCurrentPop { get; set; } = 5; // Start with 5 workers
        public int PlayerMaxPop { get; set; } = 5; // Base capacity
        public const int AbsoluteMaxPop = 500;

        public void UpdatePopulation()
        {
            // ===== PLAYER POPULATION (Player is always slot 0) =====
            PlayerCurrentPop = WarRegistry.Units
                .Count(u => u.OwnerPlayerIndex == 0 && u.State != UnitState.Dead);

            // Count player's farms
            int playerFarms = WarRegistry.Buildings
                .OfType<Farm>()
                .Count(f => f.OwnerPlayerIndex == 0 && f.IsConstructed);

            PlayerMaxPop = Math.Min(AbsoluteMaxPop, 5 + (playerFarms * 5));

            // ✅ FIX: Update PlayerFaction if it exists
            if (PlayerFaction != null)
            {
                PlayerFaction.CurrentPop = PlayerCurrentPop;
                PlayerFaction.MaxPop = PlayerMaxPop;
                PlayerFaction.Gold = PlayerGold;      // ✅ SYNC RESOURCES
                PlayerFaction.Lumber = PlayerLumber;  // ✅ SYNC RESOURCES
            }

            // ===== AI POPULATION (for each AI player) =====
            if (Players.Count > 0)
            {
                foreach (var player in Players.Where(p => !p.IsHuman))
                {
                    if (player.Faction == null) continue;

                    player.Faction.CurrentPop = WarRegistry.Units
                        .Count(u => u.OwnerPlayerIndex == player.SlotIndex && u.State != UnitState.Dead);

                    int aiFarms = WarRegistry.Buildings
                        .OfType<Farm>()
                        .Count(f => f.OwnerPlayerIndex == player.SlotIndex && f.IsConstructed);

                    player.Faction.MaxPop = Math.Min(AbsoluteMaxPop, 5 + (aiFarms * 5));
                }
            }
            else if (AIFaction != null)
            {
                // Legacy mode - single AI faction
                AIFaction.CurrentPop = WarRegistry.Units
                    .Count(u => u.OwnerPlayerIndex == 1 && u.State != UnitState.Dead);

                int aiFarms = WarRegistry.Buildings
                    .OfType<Farm>()
                    .Count(f => f.OwnerPlayerIndex == 1 && f.IsConstructed);

                AIFaction.MaxPop = Math.Min(AbsoluteMaxPop, 5 + (aiFarms * 5));
            }
        }
        // Selection state
        public bool IsBuildMode { get; set; } = false;
        public string? BuildingToBuild { get; set; }
        public (int x, int y)? GhostBuildingPosition { get; set; }


        // ✅ NEW: SPELL CAST MODE
        public bool IsCastMode { get; set; } = false;
        public string? SpellToCast { get; set; } = null;        // "Heal", "Bloodlust"
        public WarUnit? CasterUnit { get; set; } = null;        // The spellcaster
        // Game tick timer
        private System.Timers.Timer? gameTickTimer;
        private bool _isTicking = false;


        // ===== INITIALIZE GAME TICK TIMER =====
        public void InitGameTickTimer()
        {
            if (gameTickTimer == null)
            {
                gameTickTimer = new System.Timers.Timer(16.666); // ~60fps
                gameTickTimer.Elapsed += (sender, e) => _ = GameTickAsync();
                gameTickTimer.AutoReset = true;
                gameTickTimer.Start();
                Console.WriteLine("Game tick timer started");
            }
        }


        // ===== GAME TICK LOOP =====
        private async Task GameTickAsync()
        {
            if (_isTicking) return;
            // ✅ PART 2: Stop ticking if game over
            if (IsGameOver) return;
            _isTicking = true;

            try
            {        // ===== NEW: UPDATE UNIT POSITIONS ON GRID =====
                GridOccupancyMap.UpdateUnitPositions();

                // ===== EXISTING CODE (keep all of this) =====
                foreach (var building in WarRegistry.Buildings.ToList())
                {
                    ProductionSystem.TickProduction(building, this);
                    ProductionSystem.TickConstruction(building);
                    BuildingAnimationController.UpdateBuildingAnimations(building);

                    if (building.CurrentBuildingState == BuildingAnimationState.Destroyed)
                    {
                        building.DestructionTimer--;

                        if (building.DestructionTimer <= 0)
                        {
                            // ===== NEW: CLEAR BUILDING FROM GRID BEFORE REMOVAL =====
                            GridOccupancyMap.ClearBuilding(building);

                            WarRegistry.Buildings.Remove(building);
                            Console.WriteLine($"💥 {building.PlaceholderName} rubble cleared from battlefield");
                        }
                    }
                }

                // Production queues
                foreach (var building in WarRegistry.Buildings)
                {
                    ProductionSystem.TickProduction(building, this);
                    ProductionSystem.TickConstruction(building);
                }
                // Production queues & animations
                // ✅ Use ToList() to avoid collection modification during iteration
                foreach (var building in WarRegistry.Buildings.ToList())
                {
                    ProductionSystem.TickProduction(building, this);
                    ProductionSystem.TickConstruction(building);
                    BuildingAnimationController.UpdateBuildingAnimations(building);

                    // ✅ NEW: Handle destroyed building cleanup
                    if (building.CurrentBuildingState == BuildingAnimationState.Destroyed)
                    {
                        building.DestructionTimer--;

                        if (building.DestructionTimer <= 0)
                        {
                            // Destruction display time expired - remove building
                            Console.WriteLine($"💥 {building.PlaceholderName} rubble cleared from battlefield");
                            WarRegistry.Buildings.Remove(building);
                        }
                    }
                }
                // Unit AI
                // ✅ Use ToList() to avoid modifying collection during iteration
                foreach (var unit in WarRegistry.Units.ToList())
                {
                    TickUnitAI(unit);
                }

                // ✅ ADD COMBAT TICK
                TickCombat();
                // ✅ NEW: Calculate proximity buffs
                CalculateProximityBuffs();
                // ✅ ADD TOWER ATTACKS
                TickTowerAttacks();
                // In GameTickAsync(), add after unit AI tick:
                UpdatePopulation();

                // ✅ NEW: Tick projectiles
                TickProjectiles();
                // ✅ NEW: MANA REGENERATION
                TickManaRegen();

                // ✅ NEW: SPELLCASTER AUTO-CAST AI
                TickSpellcasters();
                // ✅ ADD AI TICK
                // ✅ AI TICK (only for AI players, NOT human)
                if (Players.Count > 0)
                {
                    foreach (var p in Players.Where(x => !x.IsHuman && x.Faction != null))
                    {
                        AIController.TickAI(this, p.Faction);
                    }
                }
                else if (AIFaction != null)
                {
                    AIController.TickAI(this, AIFaction);
                }

                // ✅ PART 2: Check if game ended
                CheckVictoryConditions();
                // Request UI update
                if (RequestUIRefresh != null)
                {
                    await RequestUIRefresh();
                }
            }
            finally
            {
                _isTicking = false;
            }
        }


        // ===== CHECK VICTORY CONDITIONS (TEAM-AWARE) =====
        private void CheckVictoryConditions()
        {
            if (IsGameOver) return; // Already ended

            if (Players.Count > 0)
            {
                // ===== MULTIPLAYER MODE - TEAM-BASED VICTORY =====

                // Group players by team
                var teams = Players.GroupBy(p => p.Team).ToList();

                // Check each team's survival
                foreach (var team in teams)
                {
                    bool teamHasUnits = false;
                    bool teamHasBuildings = false;

                    // Check if ANY player on this team has units or buildings
                    foreach (var player in team)
                    {
                        bool playerHasUnits = WarRegistry.Units.Any(u =>
                            u.OwnerPlayerIndex == player.SlotIndex &&
                            u.State != UnitState.Dead &&
                            u.HP > 0);

                        bool playerHasBuildings = WarRegistry.Buildings.Any(b =>
                            b.OwnerPlayerIndex == player.SlotIndex &&
                            b.HP > 0 &&
                            b.CurrentBuildingState != BuildingAnimationState.Destroyed);

                        if (playerHasUnits) teamHasUnits = true;
                        if (playerHasBuildings) teamHasBuildings = true;
                    }

                    // If team has no units AND no buildings, they're eliminated
                    if (!teamHasUnits && !teamHasBuildings)
                    {
                        // Check if this was the player's team
                        bool isPlayerTeam = team.Any(p => p.IsHuman);

                        if (isPlayerTeam)
                        {
                            GameEndState = "DEFEAT";
                            IsGameOver = true;
                            Console.WriteLine($"💀 DEFEAT - Team {team.Key} eliminated!");
                            return;
                        }
                        else
                        {
                            // Check if all enemy teams are eliminated
                            var playerTeamNumber = Players.First(p => p.IsHuman).Team;
                            bool allEnemiesDefeated = teams
                                .Where(t => t.Key != playerTeamNumber)
                                .All(t => !t.Any(p =>
                                    WarRegistry.Units.Any(u => u.OwnerPlayerIndex == p.SlotIndex && u.HP > 0) ||
                                    WarRegistry.Buildings.Any(b => b.OwnerPlayerIndex == p.SlotIndex && b.HP > 0)
                                ));

                            if (allEnemiesDefeated)
                            {
                                GameEndState = "VICTORY";
                                IsGameOver = true;
                                Console.WriteLine($"🏆 VICTORY - All enemy teams eliminated!");
                                return;
                            }
                        }
                    }
                }
            }
            else
            {
                // ===== LEGACY MODE - 1v1 =====
                if (PlayerFaction == null || AIFaction == null) return;

                bool playerHasUnits = WarRegistry.Units.Any(u =>
                    u.OwnerPlayerIndex == 0 &&
                    u.State != UnitState.Dead &&
                    u.HP > 0);

                bool playerHasBuildings = WarRegistry.Buildings.Any(b =>
                    b.OwnerPlayerIndex == 0 &&
                    b.HP > 0 &&
                    b.CurrentBuildingState != BuildingAnimationState.Destroyed);

                bool aiHasUnits = WarRegistry.Units.Any(u =>
                    u.OwnerPlayerIndex == 1 &&
                    u.State != UnitState.Dead &&
                    u.HP > 0);

                bool aiHasBuildings = WarRegistry.Buildings.Any(b =>
                    b.OwnerPlayerIndex == 1 &&
                    b.HP > 0 &&
                    b.CurrentBuildingState != BuildingAnimationState.Destroyed);

                // Player defeat
                if (!playerHasUnits && !playerHasBuildings)
                {
                    GameEndState = "DEFEAT";
                    IsGameOver = true;
                    Console.WriteLine("💀 DEFEAT - All player forces destroyed!");
                }
                // Player victory
                else if (!aiHasUnits && !aiHasBuildings)
                {
                    GameEndState = "VICTORY";
                    IsGameOver = true;
                    Console.WriteLine("🏆 VICTORY - Enemy destroyed!");
                }
            }
        }

        private void TickProjectiles()
        {
            var projectilesToRemove = new List<Projectile>();

            foreach (var projectile in WarRegistry.Projectiles)
            {
                // Move projectile
                bool arrived = projectile.MoveTowardTarget();

                if (arrived)
                {
                    // ✅ SPELL PROJECTILES: Visual effect only, no damage
                    if (projectile.IsSpellProjectile)
                    {
                        Console.WriteLine($"✨ {projectile.SpellType} spell effect reached {projectile.TargetUnit?.PlaceholderName}");
                        projectilesToRemove.Add(projectile);
                        continue; // Don't deal damage
                    }

                    // ✅ ATTACK PROJECTILES: Deal damage (existing code)
                    if (projectile.TargetUnit != null && projectile.TargetUnit.HP > 0)
                    {
                        projectile.TargetUnit.HP -= projectile.Damage;
                        Console.WriteLine($"🏹 Projectile hit {projectile.TargetUnit.PlaceholderName} for {projectile.Damage} damage (HP: {projectile.TargetUnit.HP}/{projectile.TargetUnit.MaxHP})");

                        if (projectile.TargetUnit.HP <= 0)
                        {
                            projectile.TargetUnit.State = UnitState.Dead;
                            WarAnimations.SetAnimationState(projectile.TargetUnit, AnimationState.Death);
                            WarRegistry.Units.Remove(projectile.TargetUnit);
                            if (WarRegistry.SelectedUnits.Contains(projectile.TargetUnit))
                            {
                                WarRegistry.SelectedUnits.Remove(projectile.TargetUnit);
                            }
                            Console.WriteLine($"💀 {projectile.TargetUnit.PlaceholderName} has been slain!");
                        }
                    }
                    else if (projectile.TargetBuilding != null && projectile.TargetBuilding.HP > 0)
                    {
                        projectile.TargetBuilding.HP -= projectile.Damage;
                        Console.WriteLine($"🏹 Projectile hit {projectile.TargetBuilding.PlaceholderName} for {projectile.Damage} damage (HP: {projectile.TargetBuilding.HP}/{projectile.TargetBuilding.MaxHP})");

                        if (projectile.TargetBuilding.HP <= 0)
                        {
                            WarRegistry.Buildings.Remove(projectile.TargetBuilding);
                            if (WarRegistry.SelectedBuilding == projectile.TargetBuilding)
                            {
                                WarRegistry.SelectedBuilding = null;
                            }
                            Console.WriteLine($"💥 {projectile.TargetBuilding.PlaceholderName} has been destroyed!");
                        }
                    }

                    projectilesToRemove.Add(projectile);
                }
            }

            // Clean up projectiles
            foreach (var projectile in projectilesToRemove)
            {
                WarRegistry.Projectiles.Remove(projectile);
            }
        }

        // ===== TICK MANA REGENERATION =====
        private void TickManaRegen()
        {
            foreach (var unit in WarRegistry.Units)
            {
                // Only regen mana for units with MaxMana > 0 (spellcasters)
                if (unit.MaxMana > 0 && unit.Mana < unit.MaxMana)
                {
                    unit.Mana = Math.Min(unit.Mana + unit.ManaRegen, unit.MaxMana);
                }

                // ✅ Tick down ability cooldowns
                if (unit.AbilityCooldown > 0)
                {
                    unit.AbilityCooldown--;
                }

                // ✅ Tick down bloodlust duration
                if (unit.BloodlustDuration > 0)
                {
                    unit.BloodlustDuration--;

                    // Remove buff when expired
                    if (unit.BloodlustDuration <= 0)
                    {
                        unit.BloodlustBonus = 0;
                        Console.WriteLine($"🩸 Bloodlust expired on {unit.PlaceholderName}");
                    }
                }
            }
        }

        // ===== TICK SPELLCASTERS (AUTO-CAST AI) =====
        private void TickSpellcasters()
        {
            foreach (var caster in WarRegistry.Units.Where(u => u.AbilityName != null).ToList())
            {
                // Skip if auto-cast disabled
                if (!caster.AutoCastEnabled) continue;

                // Skip if not enough mana
                if (caster.Mana < caster.AbilityManaCost) continue;

                // Skip if on cooldown
                if (caster.AbilityCooldown > 0) continue;

                // Skip if dead
                if (caster.State == UnitState.Dead || caster.HP <= 0) continue;

                // ✅ HEAL AUTO-CAST (Priest)
                if (caster.AbilityName == "Heal")
                {
                    AutoCastHeal(caster);
                }
                // ✅ BLOODLUST AUTO-CAST (Cultist)
                else if (caster.AbilityName == "Bloodlust")
                {
                    AutoCastBloodlust(caster);
                }
            }
        }

        // ===== AUTO-CAST HEAL =====
        private void AutoCastHeal(WarUnit caster)
        {
            // Find wounded ally within range
            var woundedAlly = WarRegistry.Units
                .Where(u => u.Race == caster.Race &&           // Same faction
                            u.State != UnitState.Dead &&       // Alive
                            u.HP < u.MaxHP &&                  // Actually wounded
                            u.HP > 0)                          // Not at 0 HP
                .OrderBy(u => DistanceTo(caster, u))           // Closest first
                .FirstOrDefault(u => DistanceTo(caster, u) <= caster.AbilityRange);

            if (woundedAlly != null)
            {
                CastHeal(caster, woundedAlly);
            }
        }

        // ===== AUTO-CAST BLOODLUST =====
        private void AutoCastBloodlust(WarUnit caster)
        {
            // Find ally in combat without bloodlust
            var allyInCombat = WarRegistry.Units
                .Where(u => u.Race == caster.Race &&                    // Same faction
                            u.State != UnitState.Dead &&                // Alive
                            u.BloodlustDuration <= 0 &&                 // Not already buffed
                            (u.State == UnitState.Attacking ||          // In combat
                             u.AttackTarget != null))
                .OrderBy(u => DistanceTo(caster, u))
                .FirstOrDefault(u => DistanceTo(caster, u) <= caster.AbilityRange);

            if (allyInCombat != null)
            {
                CastBloodlust(caster, allyInCombat);
            }
        }

        // ===== CAST HEAL SPELL =====
        private void CastHeal(WarUnit caster, WarUnit target)
        {
            // Deduct mana
            caster.Mana -= caster.AbilityManaCost;
            caster.AbilityCooldown = caster.AbilityCooldownMax;

            // Heal target
            int healAmount = 5;
            int oldHP = target.HP;
            target.HP = Math.Min(target.HP + healAmount, target.MaxHP);
            int actualHeal = target.HP - oldHP;

            // ✅ Spawn heal projectile (visual effect)
            if (caster is Priest priest)
            {
                var healProjectile = priest.CreateHealProjectile(target);
                WarRegistry.Projectiles.Add(healProjectile);
            }

            Console.WriteLine($"⚕️ {caster.PlaceholderName} heals {target.PlaceholderName} for {actualHeal} HP (Mana: {caster.Mana}/{caster.MaxMana})");
        }

        // ===== CAST BLOODLUST SPELL =====
        private void CastBloodlust(WarUnit caster, WarUnit target)
        {
            // Deduct mana
            caster.Mana -= caster.AbilityManaCost;
            caster.AbilityCooldown = caster.AbilityCooldownMax;

            // Apply bloodlust buff
            target.BloodlustBonus = 1;           // +1 damage
            target.BloodlustDuration = 300;      // 5 seconds (300 ticks at 60 ticks/sec)

            // ✅ Spawn bloodlust projectile (visual effect)
            if (caster is Cultist cultist)
            {
                var bloodlustProjectile = cultist.CreateBloodlustProjectile(target);
                WarRegistry.Projectiles.Add(bloodlustProjectile);
            }

            Console.WriteLine($"🩸 {caster.PlaceholderName} casts Bloodlust on {target.PlaceholderName} (+1 dmg for 5s, Mana: {caster.Mana}/{caster.MaxMana})");
        }




        // ===== ADD COMBAT TICK =====
        private void TickCombat()
{
    var unitsToRemove = new List<WarUnit>();
    var buildingsToRemove = new List<WarBuilding>();

    foreach (var unit in WarRegistry.Units)
    {
        if (unit.State != UnitState.Attacking) continue;

        // ✅ HANDLE UNIT TARGET
        if (unit.AttackTarget != null)
        {
            var target = unit.AttackTarget;

            // Check if target died
            if (target.HP <= 0)
            {
                target.State = UnitState.Dead;
                WarAnimations.SetAnimationState(target, AnimationState.Death);
                unitsToRemove.Add(target);
                unit.AttackTarget = null;
                unit.State = UnitState.Idle;

                Console.WriteLine($"💀 {target.PlaceholderName} has been slain!");
                continue;
            }

                    // Calculate distance
                    double distance = CalculateDistance(unit, target);

                    // Too far - chase target
                    if (distance > unit.AttackRange)
                    {
                        unit.TargetX = target.PosX;
                        unit.TargetY = target.PosY;
                        WarPathfinding.MoveToward(unit);
                        WarAnimations.SetAnimationState(unit, AnimationState.Move);
                        continue;
                    }

                    // In range - face the target for attack animation
                    int deltaX = target.PosX - unit.PosX;
            int deltaY = target.PosY - unit.PosY;
            unit.CurrentDirection = WarAnimations.CalculateDirection(deltaX, deltaY);

            // In range - attack
            unit.AttackCooldown--;

            if (unit.AttackCooldown <= 0)
            {
                        // Spawn projectile for ranged units
                        // Spawn projectile for ranged units
                        if (unit is Archer || unit is TrollAxeThrower)
                        {
                            int projectileStartX = unit.PosX + unit.Width / 2;
                            int projectileStartY = unit.PosY + unit.Height / 2;
                            int projectileTargetX = target.PosX + target.Width / 2;
                            int projectileTargetY = target.PosY + target.Height / 2;

                            int projectileDamage = unit.AttackDamage + unit.ProximityBuffDamage + unit.BloodlustBonus; // ✅ ADD BloodlustBonus
                            projectileDamage = Math.Max(1, projectileDamage);

                            var projectile = new Projectile(
                                projectileStartX,
                                projectileStartY,
                                projectileTargetX,
                                projectileTargetY,
                                projectileDamage,
                                unit.Race,
                                target
                            );

                            WarRegistry.Projectiles.Add(projectile);
                            Console.WriteLine($"🏹 {unit.PlaceholderName} fires projectile at {target.PlaceholderName}");
                        }
                        else
                {
                            // Melee damage
                            int actualDamage = unit.AttackDamage + unit.ProximityBuffDamage + unit.BloodlustBonus; // ✅ ADD BloodlustBonus
                            actualDamage = Math.Max(1, actualDamage);

                            target.HP -= actualDamage;

                            string buffText = unit.HasProximityBuff ? $" (Buff: {unit.ProximityBuffDamage:+0;-0})" : "";
                            string bloodlustText = unit.BloodlustBonus > 0 ? $" (🩸 Bloodlust: +{unit.BloodlustBonus})" : ""; // ✅ ADD THIS

                            Console.WriteLine($"⚔️ {unit.PlaceholderName} attacks {target.PlaceholderName} for {actualDamage} dmg{buffText}{bloodlustText} (HP: {target.HP}/{target.MaxHP})");
                        }

                unit.AttackCooldown = unit.AttackSpeed;

                        // Check if target died
                        if (target.HP <= 0)
                        {
                            target.State = UnitState.Dead;
                            unitsToRemove.Add(target);
                            unit.AttackTarget = null;
                            unit.State = UnitState.Idle;

                            // ✅ NEW: Track enemy kill (owner-based)
                            if (target.OwnerPlayerIndex != 0)
                            {
                                EnemyUnitsKilledCount++;
                            }

                            Console.WriteLine($"💀 {target.PlaceholderName} has been slain!");
                        }
                    }
        }

        // ✅ NEW: HANDLE BUILDING TARGET
        if (unit.AttackTargetBuilding != null)
        {
            var targetBuilding = unit.AttackTargetBuilding;

            // Check if building destroyed
            if (targetBuilding.HP <= 0)
            {
                buildingsToRemove.Add(targetBuilding);
                unit.AttackTargetBuilding = null;
                unit.State = UnitState.Idle;

                Console.WriteLine($"💥 {targetBuilding.PlaceholderName} has been destroyed!");
                continue;
            }

            // Calculate distance
            double distance = CalculateDistance(unit, targetBuilding);

            // Too far - move closer
            if (distance > unit.AttackRange)
            {
                unit.TargetX = targetBuilding.PosX + targetBuilding.Width / 2;
                unit.TargetY = targetBuilding.PosY + targetBuilding.Height / 2;
                WarPathfinding.MoveToward(unit);
                WarAnimations.SetAnimationState(unit, AnimationState.Move);
                continue;
            }

            // In range - face the building for attack animation
            int deltaX = (targetBuilding.PosX + targetBuilding.Width / 2) - unit.PosX;
            int deltaY = (targetBuilding.PosY + targetBuilding.Height / 2) - unit.PosY;
            unit.CurrentDirection = WarAnimations.CalculateDirection(deltaX, deltaY);

            // In range - attack
            unit.AttackCooldown--;

            if (unit.AttackCooldown <= 0)
            {
                // Spawn projectile for ranged units
                if (unit is Archer || unit is TrollAxeThrower)
                {
                    int projectileStartX = unit.PosX + unit.Width / 2;
                    int projectileStartY = unit.PosY + unit.Height / 2;
                    int projectileTargetX = targetBuilding.PosX + targetBuilding.Width / 2;
                    int projectileTargetY = targetBuilding.PosY + targetBuilding.Height / 2;

                    int projectileDamage = unit.AttackDamage + unit.ProximityBuffDamage;
                    projectileDamage = Math.Max(1, projectileDamage);

                    var projectile = new Projectile(
                        projectileStartX,
                        projectileStartY,
                        projectileTargetX,
                        projectileTargetY,
                        projectileDamage,
                        unit.Race,
                        targetBuilding
                    );

                    WarRegistry.Projectiles.Add(projectile);
                    Console.WriteLine($"🏹 {unit.PlaceholderName} fires projectile at {targetBuilding.PlaceholderName}");
                }
                else
                {
                    // Melee damage to building
                    int actualDamage = unit.AttackDamage + unit.ProximityBuffDamage;
                    actualDamage = Math.Max(1, actualDamage);

                    targetBuilding.HP -= actualDamage;

                    string buffText = unit.HasProximityBuff ? $" (Buff: {unit.ProximityBuffDamage:+0;-0})" : "";
                    Console.WriteLine($"⚔️ {unit.PlaceholderName} attacks {targetBuilding.PlaceholderName} for {actualDamage} dmg{buffText} (HP: {targetBuilding.HP}/{targetBuilding.MaxHP})");
                }

                unit.AttackCooldown = unit.AttackSpeed;

                        if (targetBuilding.HP <= 0)
                        {
                            buildingsToRemove.Add(targetBuilding);
                            unit.AttackTargetBuilding = null;
                            unit.State = UnitState.Idle;
                            // ✅ NEW: Track building destroyed (owner-based)
                            if (targetBuilding.OwnerPlayerIndex != 0)
                            {
                                EnemyBuildingsDestroyedCount++;
                            }
                            Console.WriteLine($"💥 {targetBuilding.PlaceholderName} has been destroyed!");
                        }
                    }
        }
    }

            foreach (var deadUnit in unitsToRemove)
            {
                // Just deselect dead units, don't remove from registry yet
                if (WarRegistry.SelectedUnits.Contains(deadUnit))
                {
                    WarRegistry.SelectedUnits.Remove(deadUnit);
                }
                deadUnit.IsSelected = false;  // ← ADD THIS LINE
                Console.WriteLine($"💀 {deadUnit.PlaceholderName} death animation starting...");
            }

            // ✅ DON'T REMOVE YET - Let destruction animation/sprite show first
            foreach (var destroyedBuilding in buildingsToRemove)
            {
                // Just deselect, don't remove from registry yet
                if (WarRegistry.SelectedBuilding == destroyedBuilding)
                {
                    WarRegistry.SelectedBuilding = null;
                }

                // ✅ Set destroyed state (BuildingAnimationController already does this)
                Console.WriteLine($"💥 {destroyedBuilding.PlaceholderName} destruction animation starting...");
            }
        }


        // ===== TICK TOWER ATTACKS =====
        private void TickTowerAttacks()
        {
            foreach (var building in WarRegistry.Buildings)
            {
                // Only towers that are constructed
                if ((building is WoodTower || building is StoneTower) && building.IsConstructed)
                {
                    TickTowerAttack(building);
                }
            }
        }

        // ===== TICK SINGLE TOWER ATTACK =====
        private void TickTowerAttack(WarBuilding tower)
        {
            // Get tower combat properties
            int attackDamage = 0;
            int attackRange = 0;
            int attackSpeed = 0;
            WarUnit? attackTarget = null;

            if (tower is WoodTower woodTower)
            {
                attackDamage = woodTower.AttackDamage;
                attackRange = woodTower.AttackRange;
                attackSpeed = woodTower.AttackSpeed;
                attackTarget = woodTower.AttackTarget;
                woodTower.AttackCooldown--;
            }
            else if (tower is StoneTower stoneTower)
            {
                attackDamage = stoneTower.AttackDamage;
                attackRange = stoneTower.AttackRange;
                attackSpeed = stoneTower.AttackSpeed;
                attackTarget = stoneTower.AttackTarget;
                stoneTower.AttackCooldown--;
            }
            else
            {
                return; // Not a tower
            }

            // Find nearest enemy if no current target
            if (attackTarget == null || attackTarget.HP <= 0 || attackTarget.State == UnitState.Dead)
            {
                attackTarget = WarRegistry.Units
                    .Where(u => u.Race != tower.Race && u.State != UnitState.Dead)
                    .OrderBy(u => DistanceTo(tower, u))
                    .FirstOrDefault();

                // Update tower's target
                if (tower is WoodTower wt)
                    wt.AttackTarget = attackTarget;
                else if (tower is StoneTower st)
                    st.AttackTarget = attackTarget;
            }

            // No valid target found
            if (attackTarget == null) return;

            // Check if target in range
            double distance = DistanceTo(tower, attackTarget);
            if (distance > attackRange)
            {
                // Target out of range - clear target
                if (tower is WoodTower wt)
                    wt.AttackTarget = null;
                else if (tower is StoneTower st)
                    st.AttackTarget = null;
                return;
            }

            // Attack on cooldown
            int cooldown = tower is WoodTower ? ((WoodTower)tower).AttackCooldown : ((StoneTower)tower).AttackCooldown;

            if (cooldown <= 0)
            {
                // Spawn projectile
                int projectileStartX = tower.PosX + tower.Width / 2;
                int projectileStartY = tower.PosY + tower.Height / 2;
                int projectileTargetX = attackTarget.PosX + attackTarget.Width / 2;
                int projectileTargetY = attackTarget.PosY + attackTarget.Height / 2;

                var projectile = new Projectile(
                    projectileStartX,
                    projectileStartY,
                    projectileTargetX,
                    projectileTargetY,
                    attackDamage,
                    tower.Race,
                    attackTarget
                );

                WarRegistry.Projectiles.Add(projectile);
                Console.WriteLine($"🗼 {tower.PlaceholderName} fires at {attackTarget.PlaceholderName} ({distance:F0}px away)");

                // Reset cooldown
                if (tower is WoodTower wt)
                    wt.AttackCooldown = attackSpeed;
                else if (tower is StoneTower st)
                    st.AttackCooldown = attackSpeed;
            }
        }
        // ===== PROXIMITY BUFF SYSTEM =====
        private void CalculateProximityBuffs()
        {
            const int PROXIMITY_RANGE = 96; // 3 tiles (32px * 3)
            const int MIN_ALLIES_FOR_BUFF = 3; // Need 3+ nearby allies

            foreach (var unit in WarRegistry.Units)
            {
                // Skip dead units
                if (unit.State == UnitState.Dead) continue;

                // Only Rally (Aggressive) and Defend (Defensive) stances get buffs
                if (unit.Stance != UnitStance.Aggressive && unit.Stance != UnitStance.Defensive)
                {
                    unit.HasProximityBuff = false;
                    unit.ProximityBuffDamage = 0;
                    continue;
                }

                // Count nearby allies (same race, not self)
                int nearbyAllies = WarRegistry.Units
                    .Where(ally => ally != unit && // Not self
                                  ally.Race == unit.Race && // Same race
                                  ally.State != UnitState.Dead && // Alive
                                  DistanceBetweenUnits(unit, ally) <= PROXIMITY_RANGE) // Within range
                    .Count();

                // Apply buff if enough allies nearby
                if (nearbyAllies >= MIN_ALLIES_FOR_BUFF)
                {
                    unit.HasProximityBuff = true;

                    // Rally (Aggressive) = +1 damage
                    // Defend (Defensive) = -1 damage taken (we'll simulate as -1 attack for enemies)
                    unit.ProximityBuffDamage = unit.Stance == UnitStance.Aggressive ? 1 : -1;
                }
                else
                {
                    unit.HasProximityBuff = false;
                    unit.ProximityBuffDamage = 0;
                }
            }
        }

        // ===== HELPER: DISTANCE BETWEEN UNITS =====
        private double DistanceBetweenUnits(WarUnit unit1, WarUnit unit2)
        {
            int centerX1 = unit1.PosX + unit1.Width / 2;
            int centerY1 = unit1.PosY + unit1.Height / 2;
            int centerX2 = unit2.PosX + unit2.Width / 2;
            int centerY2 = unit2.PosY + unit2.Height / 2;

            int dx = centerX2 - centerX1;
            int dy = centerY2 - centerY1;

            return Math.Sqrt(dx * dx + dy * dy);
        }
        // ===== HELPER: CALCULATE DISTANCE =====
        private double CalculateDistance(WarEntity from, WarEntity to)
        {
            int fromCenterX = from.PosX + from.Width / 2;
            int fromCenterY = from.PosY + from.Height / 2;
            int toCenterX = to.PosX + to.Width / 2;
            int toCenterY = to.PosY + to.Height / 2;

            int dx = toCenterX - fromCenterX;
            int dy = toCenterY - fromCenterY;

            return Math.Sqrt(dx * dx + dy * dy);
        }



        private void TickUnitAI(WarUnit unit)
        {
            // ===== UPDATE ANIMATION STATES BASED ON UNIT STATE =====
            switch (unit.State)
            {
                case UnitState.Moving:
                    WarAnimations.SetAnimationState(unit, AnimationState.Move);
                    WarPathfinding.MoveToward(unit);
                    break;
                // ✅ ADD THIS CASE FOR SCOUTING
                case UnitState.Scouting:
                    WarAnimations.SetAnimationState(unit, AnimationState.Move);
                    WarPathfinding.MoveToward(unit);  // Same movement as regular moving
                    break;
                case UnitState.GatheringGold:
                    // Moving to mine = Move animation, At mine = Attack animation (mining)
                    if (unit.TargetX != null)
                        WarAnimations.SetAnimationState(unit, AnimationState.Move);
                    else
                        WarAnimations.SetAnimationState(unit, AnimationState.Attack);

                    TickGatherGold(unit);
                    break;

                case UnitState.GatheringLumber:
                    // Moving to tree = Move animation, At tree = Attack animation (chopping)
                    if (unit.TargetX != null)
                        WarAnimations.SetAnimationState(unit, AnimationState.Move);
                    else
                        WarAnimations.SetAnimationState(unit, AnimationState.Attack);

                    TickGatherLumber(unit);
                    break;

                case UnitState.ReturningResources:
                    WarAnimations.SetAnimationState(unit, AnimationState.Move);
                    TickReturnResources(unit);
                    break;

                case UnitState.Attacking:
                    // ✅ NEW: Check stance behavior
                    if (unit.Stance == UnitStance.Hold)
                    {
                        // Hold stance: Don't chase, only attack in range
                        if (unit.AttackTarget != null)
                        {
                            double distance = CalculateDistance(unit, unit.AttackTarget);
                            if (distance > unit.AttackRange)
                            {
                                // Too far - cancel attack and stay in place
                                unit.AttackTarget = null;
                                unit.State = UnitState.Idle;
                                WarAnimations.SetAnimationState(unit, AnimationState.Idle);
                            }
                            else
                            {
                                WarAnimations.SetAnimationState(unit, AnimationState.Attack);
                            }
                        }
                    }
                    else if (unit.Stance == UnitStance.Passive)
                    {
                        // Passive: Only attack if directly ordered (don't auto-engage)
                        if (unit.AttackTarget != null)
                        {
                            double distance = CalculateDistance(unit, unit.AttackTarget);
                            if (distance > unit.AttackRange)
                                WarAnimations.SetAnimationState(unit, AnimationState.Move);
                            else
                                WarAnimations.SetAnimationState(unit, AnimationState.Attack);
                        }
                    }
                    else
                    {
                        // Normal attack behavior (Defensive, Aggressive, AttackMove)
                        if (unit.AttackTarget != null)
                        {
                            double distance = CalculateDistance(unit, unit.AttackTarget);
                            if (distance > unit.AttackRange)
                                WarAnimations.SetAnimationState(unit, AnimationState.Move);
                            else
                                WarAnimations.SetAnimationState(unit, AnimationState.Attack);
                        }
                    }
                    // Combat handled in TickCombat()
                    break;

                case UnitState.Dead:
                    WarAnimations.SetAnimationState(unit, AnimationState.Death);

                    // ✅ Check if death animation finished playing
                    if (!unit.IsDeathAnimationPlaying)
                    {
                        // Animation finished - now we can remove the unit
                        Console.WriteLine($"💀 {unit.PlaceholderName} death animation complete - removing corpse");
                        WarRegistry.Units.Remove(unit);
                    }
                    // ✅ REMOVED the else block that was clearing corpses immediately
                    break;

                case UnitState.Idle:
                default:
                    WarAnimations.SetAnimationState(unit, AnimationState.Idle);

                    // ✅ NEW: Auto-attack for Aggressive stance
                    if (unit.Stance == UnitStance.Aggressive && unit.Race == PlayerFaction?.Race)
                    {
                        // Look for nearby enemies
                        var nearbyEnemy = WarRegistry.Units
                            .Where(u => u.Race != unit.Race && u.State != UnitState.Dead)
                            .OrderBy(u => DistanceBetweenUnits(unit, u))
                            .FirstOrDefault();

                        if (nearbyEnemy != null && DistanceBetweenUnits(unit, nearbyEnemy) <= 200)
                        {
                            // Auto-engage!
                            unit.State = UnitState.Attacking;
                            unit.AttackTarget = nearbyEnemy;
                            unit.TargetX = nearbyEnemy.PosX;
                            unit.TargetY = nearbyEnemy.PosY;
                            Console.WriteLine($"⚔️ {unit.PlaceholderName} (Rally) auto-engaging {nearbyEnemy.PlaceholderName}!");
                        }
                    }
                    break;

            }

            // ===== UPDATE ANIMATION FRAME =====
            WarAnimations.UpdateAnimationFrame(unit);
        }
        // ===== GATHER GOLD AI =====
        private void TickGatherGold(WarUnit unit)
        {
            if (unit.TargetMine == null)
            {
                unit.State = UnitState.Idle;
                return;
            }

            // ✅ INLINE: Calculate nearest edge of gold mine (not a WarBuilding)
            int unitCenterX = unit.PosX + unit.Width / 2;
            int unitCenterY = unit.PosY + unit.Height / 2;
            int mineEdgeX = Math.Clamp(unitCenterX, unit.TargetMine.PosX, unit.TargetMine.PosX + unit.TargetMine.Width);
            int mineEdgeY = Math.Clamp(unitCenterY, unit.TargetMine.PosY, unit.TargetMine.PosY + unit.TargetMine.Height);

            unit.TargetX = mineEdgeX;
            unit.TargetY = mineEdgeY;

            // Move to mine edge
            if (unit.TargetX != null)
            {
                WarPathfinding.MoveToward(unit);

                // ✅ Check if near mine EDGE
                int dx = mineEdgeX - unitCenterX;
                int dy = mineEdgeY - unitCenterY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                bool isNear = distance <= 40;

                if (!isNear)
                {
                    return; // Keep moving to edge
                }
            }

            // ✅ At edge - harvest gold
            int harvested = unit.TargetMine.Harvest(unit.CarryCapacity);
            unit.CarryingGold = harvested;
            Console.WriteLine($"⛏️ {unit.PlaceholderName}: Harvested {harvested} gold!");

            // Find town hall OR castle to return to
            unit.TargetTownHall = WarRegistry.Buildings
                .Where(b => b.OwnerPlayerIndex == unit.OwnerPlayerIndex && b.IsConstructed &&
                           (b is TownHall || b is Castle))
                .OrderBy(b => DistanceTo(unit, b))
                .FirstOrDefault();

            if (unit.TargetTownHall != null)
            {
                unit.State = UnitState.ReturningResources;
                var (returnEdgeX, returnEdgeY) = WarPathfinding.GetNearestBuildingEdge(unit, unit.TargetTownHall);
                unit.TargetX = returnEdgeX;
                unit.TargetY = returnEdgeY;
            }
            else
            {
                Console.WriteLine($"❌ {unit.PlaceholderName}: NO TOWN HALL FOUND!");
                unit.State = UnitState.Idle;
            }
        }

        // ===== GATHER LUMBER AI =====
        private void TickGatherLumber(WarUnit unit)
        {
            if (unit.TargetTree == null || unit.TargetTree.IsChopped)
            {
                // Tree is dead - search for a new one
                Tree? newTree = WarPathfinding.FindNearestTree(unit, 300);

                if (newTree != null)
                {
                    // Found new tree - assign and continue
                    unit.TargetTree = newTree;
                    Console.WriteLine($"🌲 {unit.PlaceholderName}: Tree depleted, found new tree nearby");
                }
                else
                {
                    // No trees nearby - go idle
                    unit.TargetTree = null;
                    unit.State = UnitState.Idle;
                    Console.WriteLine($"💤 {unit.PlaceholderName}: No trees found, going idle");
                    return;
                }
            }

            // ✅ INLINE: Calculate nearest edge point on tree
            int unitCenterX = unit.PosX + unit.Width / 2;
            int unitCenterY = unit.PosY + unit.Height / 2;
            int treeEdgeX = Math.Clamp(unitCenterX, unit.TargetTree.PosX, unit.TargetTree.PosX + unit.TargetTree.Width);
            int treeEdgeY = Math.Clamp(unitCenterY, unit.TargetTree.PosY, unit.TargetTree.PosY + unit.TargetTree.Height);

            unit.TargetX = treeEdgeX;
            unit.TargetY = treeEdgeY;

            // Move to tree edge
            if (unit.TargetX != null)
            {
                WarPathfinding.MoveToward(unit);

                // ✅ Check if near tree edge
                int dx = treeEdgeX - unitCenterX;
                int dy = treeEdgeY - unitCenterY;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                bool isNear = distance <= 60; // Trees are smaller, use 60px range

                if (!isNear)
                {
                    return; // Keep moving to edge
                }
            }

            // ✅ At edge - chop lumber
            int toChop = Math.Min(unit.CarryCapacity, unit.TargetTree.LumberRemaining);
            unit.TargetTree.Chop(toChop);
            unit.CarryingLumber = toChop;
            Console.WriteLine($"🪓 {unit.PlaceholderName}: Chopped {toChop} lumber!");

            // Find town hall, castle, or lumber mill to return to
            unit.TargetTownHall = WarRegistry.Buildings
                .Where(b => b.OwnerPlayerIndex == unit.OwnerPlayerIndex && b.IsConstructed &&
                           (b is TownHall || b is Castle || b is LumberMill))
                .OrderBy(b => DistanceTo(unit, b))
                .FirstOrDefault();

            if (unit.TargetTownHall != null)
            {
                unit.State = UnitState.ReturningResources;
                var (returnEdgeX, returnEdgeY) = WarPathfinding.GetNearestBuildingEdge(unit, unit.TargetTownHall);
                unit.TargetX = returnEdgeX;
                unit.TargetY = returnEdgeY;
            }
            else
            {
                Console.WriteLine($"❌ {unit.PlaceholderName}: NO TOWN HALL FOUND!");
                unit.State = UnitState.Idle;
            }
        }
        private void TickReturnResources(WarUnit unit)
        {
            // ✅ UPDATED: Find nearest drop-off point (Town Hall OR Lumber Mill for lumber)
            WarBuilding? dropOffPoint = null;

            if (unit.CarryingLumber > 0)
            {
                // For lumber: Accept Town Hall, Castle, OR Lumber Mill
                dropOffPoint = WarRegistry.Buildings
                    .Where(b => b.OwnerPlayerIndex == unit.OwnerPlayerIndex && b.IsConstructed &&
                               (b is TownHall || b is Castle || b is LumberMill))
                    .OrderBy(b => DistanceTo(unit, b))
                    .FirstOrDefault();
            }
            else if (unit.CarryingGold > 0)
            {
                // For gold: Town Hall OR Castle
                dropOffPoint = WarRegistry.Buildings
                    .Where(b => b.OwnerPlayerIndex == unit.OwnerPlayerIndex && b.IsConstructed &&
                               (b is TownHall || b is Castle))
                    .OrderBy(b => DistanceTo(unit, b))
                    .FirstOrDefault();
            }

            if (dropOffPoint == null)
            {
                Console.WriteLine($"❌ {unit.PlaceholderName}: No drop-off point found!");
                unit.State = UnitState.Idle;
                return;
            }

            // ✅ ALWAYS recalculate nearest edge every tick
            unit.TargetTownHall = dropOffPoint;
            var (edgeX, edgeY) = WarPathfinding.GetNearestBuildingEdge(unit, dropOffPoint);
            unit.TargetX = edgeX;
            unit.TargetY = edgeY;

            // Move to drop-off point
            if (unit.TargetX != null)
            {
                WarPathfinding.MoveToward(unit);

                // ✅ Check if near building EDGE (not center)
                bool nearBuilding = WarPathfinding.IsNearBuildingEdge(unit, dropOffPoint, 40);

                if (!nearBuilding)
                {
                    return; // Keep moving
                }
            }

            // ✅ At this point, we're near the edge - deposit!

            // ✅ SAVE what we're carrying BEFORE clearing it
            bool wasCarryingGold = unit.CarryingGold > 0;
            bool wasCarryingLumber = unit.CarryingLumber > 0;

            // Deposit resources to correct owner
            if (unit.OwnerPlayerIndex == 0)
            {
                // Player's unit
                PlayerGold += unit.CarryingGold;
                PlayerLumber += unit.CarryingLumber;
                TotalGoldGathered += unit.CarryingGold;
                TotalLumberGathered += unit.CarryingLumber;
                Console.WriteLine($"💰 Player 0: Deposited {unit.CarryingGold}g {unit.CarryingLumber}w at {dropOffPoint.PlaceholderName} → Total: {PlayerGold}g {PlayerLumber}w");
            }
            else if (unit.OwnerPlayerIndex > 0)
            {
                // AI's unit - find the correct AI player
                var ownerPlayer = Players.FirstOrDefault(p => p.SlotIndex == unit.OwnerPlayerIndex);

                if (ownerPlayer?.Faction != null)
                {
                    ownerPlayer.Faction.Gold += unit.CarryingGold;
                    ownerPlayer.Faction.Lumber += unit.CarryingLumber;
                    Console.WriteLine($"🤖 AI Player {unit.OwnerPlayerIndex}: Deposited {unit.CarryingGold}g {unit.CarryingLumber}w → Total: {ownerPlayer.Faction.Gold}g {ownerPlayer.Faction.Lumber}w");
                }
                else if (AIFaction != null)
                {
                    // Legacy mode fallback
                    AIFaction.Gold += unit.CarryingGold;
                    AIFaction.Lumber += unit.CarryingLumber;
                    Console.WriteLine($"🤖 AI (Legacy): Deposited {unit.CarryingGold}g {unit.CarryingLumber}w → Total: {AIFaction.Gold}g {AIFaction.Lumber}w");
                }
            }

            unit.CarryingGold = 0;
            unit.CarryingLumber = 0;

            Console.WriteLine($"🔍 After deposit: {unit.PlaceholderName} - TargetMine: {unit.TargetMine != null}, TargetTree: {unit.TargetTree != null}");

            // ✅ FIXED: Go back based on what was JUST deposited (not what targets exist)
            if (wasCarryingGold && unit.TargetMine != null)
            {
                unit.State = UnitState.GatheringGold;
                unit.TargetX = unit.TargetMine.PosX + 32;
                unit.TargetY = unit.TargetMine.PosY + 32;
            }
            else if (wasCarryingLumber && unit.TargetTree != null)
            {
                // CHECK: Is the tree still alive?
                if (!unit.TargetTree.IsChopped)
                {
                    unit.State = UnitState.GatheringLumber;
                    unit.TargetX = unit.TargetTree.PosX + 16;
                    unit.TargetY = unit.TargetTree.PosY + 16;
                    Console.WriteLine($"🔄 {unit.PlaceholderName}: Returning to same tree");
                }
                else
                {
                    Tree? newTree = WarPathfinding.FindNearestTree(unit, 300);

                    if (newTree != null)
                    {
                        unit.TargetTree = newTree;
                        unit.State = UnitState.GatheringLumber;
                        unit.TargetX = newTree.PosX + 16;
                        unit.TargetY = newTree.PosY + 16;
                        Console.WriteLine($"🌲 {unit.PlaceholderName}: Found new tree, continuing work");
                    }
                    else
                    {
                        unit.TargetTree = null;
                        unit.State = UnitState.Idle;
                        Console.WriteLine($"💤 {unit.PlaceholderName}: No trees found, staying at base");
                    }
                }
            }
            else
            {
                unit.State = UnitState.Idle;
            }
        }

        // ✅ ADD THIS HELPER METHOD (if not already present)
        private double DistanceTo(WarEntity from, WarEntity to)
        {
            int dx = (from.PosX + from.Width / 2) - (to.PosX + to.Width / 2);
            int dy = (from.PosY + from.Height / 2) - (to.PosY + to.Height / 2);
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // ===== ATTACK COMMAND =====
        public void HandleAttackCommand(int worldX, int worldY)
        {
            // ✅ NEW: Filter to only player-owned units
            var playerUnits = WarRegistry.SelectedUnits
                .Where(u => u.OwnerPlayerIndex == 0)
                .ToList();

            if (playerUnits.Count == 0)
            {
                Console.WriteLine("❌ No player units selected");
                return;
            }

            // Check if clicked on unit
            var targetUnit = WarRegistry.GetUnitAt(worldX, worldY);
            var targetBuilding = WarRegistry.GetBuildingAt(worldX, worldY);

            if (targetUnit != null)
            {
                // Get player's team
                int playerTeam = Players.Count > 0 ? Players[0].Team : 1;

                // ✅ NEW: Check if target is ally
                if (targetUnit.OwnerTeam == playerTeam && targetUnit.OwnerTeam > 0)
                {
                    Console.WriteLine($"❌ Cannot attack ally: {targetUnit.PlaceholderName} (Team {targetUnit.OwnerTeam})");
                    return;
                }

                // Attack enemy unit
                foreach (var unit in playerUnits)
                {
                    unit.State = UnitState.Attacking;
                    unit.AttackTarget = targetUnit;
                    unit.AttackTargetBuilding = null;
                    unit.TargetX = targetUnit.PosX;
                    unit.TargetY = targetUnit.PosY;

                    Console.WriteLine($"⚔️ {unit.PlaceholderName} ordered to attack {targetUnit.PlaceholderName}");
                }
            }
            else if (targetBuilding != null)
            {
                // Get player's team
                int playerTeam = Players.Count > 0 ? Players[0].Team : 1;

                // ✅ NEW: Check if target is ally building
                if (targetBuilding.OwnerTeam == playerTeam && targetBuilding.OwnerTeam > 0)
                {
                    Console.WriteLine($"❌ Cannot attack ally building: {targetBuilding.PlaceholderName} (Team {targetBuilding.OwnerTeam})");
                    return;
                }

                // Attack enemy building
                foreach (var unit in playerUnits)
                {
                    unit.State = UnitState.Attacking;
                    unit.AttackTarget = null;
                    unit.AttackTargetBuilding = targetBuilding;
                    unit.TargetX = targetBuilding.PosX + targetBuilding.Width / 2;
                    unit.TargetY = targetBuilding.PosY + targetBuilding.Height / 2;

                    Console.WriteLine($"⚔️ {unit.PlaceholderName} ordered to attack {targetBuilding.PlaceholderName}");
                }
            }
        }

        // ===== HANDLE RIGHT CLICK (Move/Gather) =====
        public void HandleRightClick(int worldX, int worldY)
        {
            // ✅ NEW: Filter to only player-owned units
            var playerUnits = WarRegistry.SelectedUnits
                .Where(u => u.OwnerPlayerIndex == 0)
                .ToList();

            if (playerUnits.Count == 0)
            {
                Console.WriteLine("❌ No player units selected");
                return;
            }

            // Check if clicked on gold mine
            var goldMine = WarRegistry.GetGoldMineAt(worldX, worldY);
            if (goldMine != null)
            {
                foreach (var unit in playerUnits.OfType<Peasant>())
                {
                    unit.State = UnitState.GatheringGold;
                    unit.TargetMine = goldMine;
                    unit.TargetX = goldMine.PosX + 32;
                    unit.TargetY = goldMine.PosY + 32;
                    Console.WriteLine($"Peasant sent to gather gold at ({goldMine.PosX}, {goldMine.PosY})");
                }
                return;
            }

            // Check if clicked on tree
            var tree = WarRegistry.GetTreeAt(worldX, worldY);
            if (tree != null)
            {
                Console.WriteLine($"🌲 Found tree! Sending {playerUnits.Count} units to chop");

                foreach (var unit in playerUnits.OfType<Peasant>())
                {
                    Console.WriteLine($"👷 Sending {unit.PlaceholderName} to chop lumber");
                    unit.State = UnitState.GatheringLumber;
                    unit.TargetTree = tree;
                    unit.TargetX = tree.PosX + 16;
                    unit.TargetY = tree.PosY + 16;
                }
                return;
            }

            // Otherwise, move to location
            foreach (var unit in playerUnits)
            {
                unit.TargetX = worldX;
                unit.TargetY = worldY;
                unit.State = UnitState.Moving;
            }
        }



        // ===== ENTER BUILD MODE =====
        public void EnterBuildMode(string buildingType)
        {
            IsBuildMode = true;
            BuildingToBuild = buildingType;

            // ✅ Initialize ghost at center of viewport
            int centerWorldX = CameraX + ViewportWidth / 2;
            int centerWorldY = CameraY + ViewportHeight / 2;
            int gridX = centerWorldX / GridCellSize;
            int gridY = centerWorldY / GridCellSize;

            // Set initial ghost position (centered on screen)
            GhostBuildingPosition = (gridX * GridCellSize, gridY * GridCellSize);
            IsGhostPlacementValid = CanPlaceBuilding(gridX, gridY, buildingType);

            Console.WriteLine($"✅ Entered build mode: {buildingType} - Ghost initialized at grid ({gridX}, {gridY})");
        }

        public void CancelBuildMode()
        {
            IsBuildMode = false;
            BuildingToBuild = null;
            GhostBuildingPosition = null;
        }

        // ===== ENTER SPELL CAST MODE =====
        public void EnterCastMode(string spellName, WarUnit caster)
        {
            IsCastMode = true;
            SpellToCast = spellName;
            CasterUnit = caster;
            Console.WriteLine($"✨ Entered cast mode: {spellName} (Click target to cast)");
        }

        // ===== CANCEL SPELL CAST MODE =====
        public void CancelCastMode()
        {
            IsCastMode = false;
            SpellToCast = null;
            CasterUnit = null;
            Console.WriteLine("❌ Cast mode canceled");
        }

        // ===== MANUAL CAST SPELL =====
        public void CastSpellOnTarget(int worldX, int worldY)
        {
            if (!IsCastMode || CasterUnit == null || SpellToCast == null) return;

            // Find target unit at click position
            var targetUnit = WarRegistry.GetUnitAt(worldX, worldY);

            if (targetUnit == null)
            {
                Console.WriteLine("❌ No valid target at that location");
                CancelCastMode();
                return;
            }

            // Check if target is valid (same faction for both Heal and Bloodlust)
            if (targetUnit.Race != CasterUnit.Race)
            {
                Console.WriteLine("❌ Can only cast spells on allied units");
                CancelCastMode();
                return;
            }

            // Check range
            double distance = DistanceTo(CasterUnit, targetUnit);
            if (distance > CasterUnit.AbilityRange)
            {
                Console.WriteLine($"❌ Target out of range ({distance:F0}px > {CasterUnit.AbilityRange}px)");
                CancelCastMode();
                return;
            }

            // Check mana
            if (CasterUnit.Mana < CasterUnit.AbilityManaCost)
            {
                Console.WriteLine($"❌ Not enough mana ({CasterUnit.Mana}/{CasterUnit.AbilityManaCost})");
                CancelCastMode();
                return;
            }

            // Check cooldown
            if (CasterUnit.AbilityCooldown > 0)
            {
                Console.WriteLine($"❌ Ability on cooldown ({CasterUnit.AbilityCooldown} ticks)");
                CancelCastMode();
                return;
            }

            // ✅ CAST THE SPELL!
            if (SpellToCast == "Heal")
            {
                CastHealManual(CasterUnit, targetUnit);
            }
            else if (SpellToCast == "Bloodlust")
            {
                CastBloodlustManual(CasterUnit, targetUnit);
            }

            CancelCastMode();
        }

        // ===== MANUAL HEAL CAST =====
        private void CastHealManual(WarUnit caster, WarUnit target)
        {
            // Deduct mana
            caster.Mana -= caster.AbilityManaCost;
            caster.AbilityCooldown = caster.AbilityCooldownMax;

            // Heal target
            int healAmount = 5;
            int oldHP = target.HP;
            target.HP = Math.Min(target.HP + healAmount, target.MaxHP);
            int actualHeal = target.HP - oldHP;

            // Spawn projectile
            if (caster is Priest priest)
            {
                var healProjectile = priest.CreateHealProjectile(target);
                WarRegistry.Projectiles.Add(healProjectile);
            }

            Console.WriteLine($"⚕️ [MANUAL] {caster.PlaceholderName} heals {target.PlaceholderName} for {actualHeal} HP");
        }

        // ===== MANUAL BLOODLUST CAST =====
        private void CastBloodlustManual(WarUnit caster, WarUnit target)
        {
            // Deduct mana
            caster.Mana -= caster.AbilityManaCost;
            caster.AbilityCooldown = caster.AbilityCooldownMax;

            // Apply buff
            target.BloodlustBonus = 1;
            target.BloodlustDuration = 300; // 5 seconds

            // Spawn projectile
            if (caster is Cultist cultist)
            {
                var bloodlustProjectile = cultist.CreateBloodlustProjectile(target);
                WarRegistry.Projectiles.Add(bloodlustProjectile);
            }

            Console.WriteLine($"🩸 [MANUAL] {caster.PlaceholderName} casts Bloodlust on {target.PlaceholderName}");
        }

     





        public bool IsGhostPlacementValid { get; set; } = false;
        public bool CanPlaceBuilding(int gridX, int gridY, string buildingType)
        {
            // ✅ UPDATED: Calculate building size dynamically
            int buildingGridSize = buildingType switch
            {
                "Road" => 1,           // ✅ NEW: 1x1 grid (32x32 pixels)
                "Farm" => 2,           // 2x2 grid (64x64 pixels)
                "WoodenWall" => 1,     // ✅ NEW: 1x1 grid (32x32 pixels)
                "StoneWall" => 1,      // ✅ NEW: 1x1 grid (32x32 pixels)
                "WoodTower" => 2,      // ✅ NEW: 2x2 grid (64x64 pixels)
                "StoneTower" => 2,     // ✅ NEW: 2x2 grid (64x64 pixels)
                "Church" => 3,          // ✅ NEW
                "CultistHut" => 3,      // ✅ NEW
                "Barracks" => 4,       // 4x4 grid (128x128 pixels)
                "LumberMill" => 3,     // 3x3 grid (96x96 pixels)
                "ArcheryRange" => 4,   // 4x4 grid (128x128 pixels)
                "Blacksmith" => 3,     // ✅ NEW: 3x3 grid (96x96 pixels)
                "Stables" => 2,        // ✅ NEW: 2x2 grid (64x64 pixels)
                "Pen" => 2,            // ✅ NEW: 2x2 grid (64x64 pixels)
                "KnightsHold" => 3,    // ✅ NEW: 2x2 grid (64x64 pixels)
                "RaiderLair" => 3,     // ✅ NEW: 2x2 grid (64x64 pixels)
                "TownHall" => 4,       // ✅ NEW: 4x4 grid (128x128)
                _ => 4                 // Default to 4x4
            };

            int buildingPixelSize = buildingGridSize * GridCellSize;

            // Check bounds
            if (gridX < 0 || gridY < 0 ||
                gridX + buildingGridSize > GridWidth ||
                gridY + buildingGridSize > GridHeight)
            {
                Console.WriteLine($"❌ Out of bounds: gridX={gridX}, gridY={gridY}");
                return false;
            }

            // Check for overlaps
            int worldX = gridX * GridCellSize;
            int worldY = gridY * GridCellSize;

            foreach (var building in WarRegistry.Buildings)
            {
                bool overlapsX = worldX < building.PosX + building.Width &&
                                worldX + buildingPixelSize > building.PosX;
                bool overlapsY = worldY < building.PosY + building.Height &&
                                worldY + buildingPixelSize > building.PosY;

                if (overlapsX && overlapsY)
                {
                    Console.WriteLine($"❌ Overlaps with {building.PlaceholderName} at ({building.PosX}, {building.PosY})");
                    return false;
                }
            }
            // ✅ UPDATE: Check Stone Tower AND Stone Wall requirement
            if ((buildingType == "StoneTower" || buildingType == "StoneWall") && !CanBuildStoneStructures())
            {
                return false;
            }
            // ✅ NEW: Check Knights Hold requirement (requires Castle)
            if (buildingType == "KnightsHold" && !CanBuildKnightsHold())
            {
                return false;
            }

            // ✅ NEW: Check Raider Lair requirement (requires Fortress)
            if (buildingType == "RaiderLair" && !CanBuildRaiderLair())
            {
                return false;
            }
            // ✅ NEW: Check Town Hall requirement
            if (buildingType == "TownHall" && !CanBuildTownHall())
            {
                return false;
            }
            // Check resources
            if (!ProductionSystem.BuildingCosts.ContainsKey(buildingType))
            {
                Console.WriteLine($"❌ Unknown building type: {buildingType}");
                return false;
            }

            var (gold, lumber, time) = ProductionSystem.BuildingCosts[buildingType];
            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Not enough resources: Need {gold}g {lumber}w, Have {PlayerGold}g {PlayerLumber}w");
                return false;
            }

            Console.WriteLine($"✅ Valid placement at grid ({gridX}, {gridY})");
            return true;
        }
        // ===== CHECK CASTLE REQUIREMENT FOR STONE TOWER =====
        // ===== CHECK CASTLE REQUIREMENT FOR STONE STRUCTURES =====
        private bool CanBuildStoneStructures()
        {
            if (PlayerFaction == null) return false;

            bool hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == PlayerFaction.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build stone structures: Requires Castle!");
                return false;
            }

            return true;
        }
        // ===== PLACE BUILDING =====
        public bool PlaceBuilding(int worldX, int worldY)
        {
            if (BuildingToBuild == null || PlayerFaction == null) return false;

            int gridX = worldX / GridCellSize;
            int gridY = worldY / GridCellSize;

            if (!CanPlaceBuilding(gridX, gridY, BuildingToBuild))
            {
                Console.WriteLine($"❌ Cannot place {BuildingToBuild} here!");
                return false;
            }

            if (!ProductionSystem.BuildingCosts.ContainsKey(BuildingToBuild)) return false;

            var (gold, lumber, time) = ProductionSystem.BuildingCosts[BuildingToBuild];

            PlayerGold -= gold;
            PlayerLumber -= lumber;

            // ===== EXISTING CODE: Create building (keep this entire switch) =====
            WarBuilding? newBuilding = BuildingToBuild switch
            {
                "TownHall" => new TownHall(gridX, gridY, PlayerFaction.Race), // ✅ NEW
                "Barracks" => new Barracks(gridX, gridY, PlayerFaction.Race),
                "WoodTower" => new WoodTower(gridX, gridY, PlayerFaction.Race),
                "StoneTower" => new StoneTower(gridX, gridY, PlayerFaction.Race),
                "WoodenWall" => new WoodenWall(gridX, gridY, PlayerFaction.Race),
                "StoneWall" => new StoneWall(gridX, gridY, PlayerFaction.Race),
                "Road" => new Road(gridX, gridY, PlayerFaction.Race),
                "Farm" => new Farm(gridX, gridY, PlayerFaction.Race),
                "Church" => new Church(gridX, gridY, PlayerFaction.Race),
                "CultistHut" => new CultistHut(gridX, gridY, PlayerFaction.Race),
                "LumberMill" => new LumberMill(gridX, gridY, PlayerFaction.Race),
                "ArcheryRange" => new ArcheryRange(gridX, gridY, PlayerFaction.Race),
                "Blacksmith" => new Blacksmith(gridX, gridY, PlayerFaction.Race),
                "Stables" => new Stables(gridX, gridY, PlayerFaction.Race),
                "Pen" => new Pen(gridX, gridY, PlayerFaction.Race),
                "KnightsHold" => new KnightsHold(gridX, gridY, PlayerFaction.Race),
                "RaiderLair" => new RaiderLair(gridX, gridY, PlayerFaction.Race),
                _ => null
            };

            if (newBuilding != null)
            {
                newBuilding.OwnerFaction = PlayerFaction;
                newBuilding.OwnerPlayerIndex = 0;
                newBuilding.OwnerTeam = Players.Count > 0
                    ? Players[0].Team
                    : 1;

                WarRegistry.Buildings.Add(newBuilding);

                // ===== NEW: MARK BUILDING ON GRID =====
                GridOccupancyMap.MarkBuilding(newBuilding);

                BuildingsConstructedCount++;
                Console.WriteLine($"✅ Placed {BuildingToBuild} at grid ({gridX}, {gridY}) - marked on occupancy grid");
            }

            CancelBuildMode();
            return true;
        }
        // ===== CHECK CASTLE REQUIREMENT FOR TOWN HALL =====
        private bool CanBuildTownHall()
        {
            if (PlayerFaction == null) return false;

            bool hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == PlayerFaction.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build Town Hall: Requires Castle!");
                return false;
            }

            return true;
        }

        // ===== CHECK CASTLE REQUIREMENT FOR KNIGHTS HOLD =====
        private bool CanBuildKnightsHold()
        {
            if (PlayerFaction == null) return false;

            bool hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == PlayerFaction.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build Knights Hold: Requires Castle!");
                return false;
            }

            return true;
        }

        // ===== CHECK FORTRESS REQUIREMENT FOR RAIDER LAIR =====
        private bool CanBuildRaiderLair()
        {
            if (PlayerFaction == null) return false;

            bool hasFortress = WarRegistry.Buildings
                .OfType<Castle>() // Fortress is also a Castle class (Orc version)
                .Any(c => c.Race == PlayerFaction.Race && c.IsConstructed);

            if (!hasFortress)
            {
                Console.WriteLine("❌ Cannot build Raider Lair: Requires Fortress!");
                return false;
            }

            return true;
        }
        // ===== CULLING HELPER: CHECK IF ENTITY IS IN VIEWPORT =====
        public bool IsEntityVisible(WarEntity entity, int buffer = 32)
        {
            // Calculate viewport bounds with buffer
            int viewLeft = CameraX - buffer;
            int viewRight = CameraX + ViewportWidth + buffer;
            int viewTop = CameraY - buffer;
            int viewBottom = CameraY + ViewportHeight + buffer;

            // Check if entity overlaps viewport
            bool isVisible = entity.PosX + entity.Width > viewLeft &&
                             entity.PosX < viewRight &&
                             entity.PosY + entity.Height > viewTop &&
                             entity.PosY < viewBottom;

            return isVisible;
        }

        // Overload for gold mines and trees (if they don't inherit from WarEntity)
        public bool IsEntityVisible(int posX, int posY, int width, int height, int buffer = 32)
        {
            int viewLeft = CameraX - buffer;
            int viewRight = CameraX + ViewportWidth + buffer;
            int viewTop = CameraY - buffer;
            int viewBottom = CameraY + ViewportHeight + buffer;

            return posX + width > viewLeft &&
                   posX < viewRight &&
                   posY + height > viewTop &&
                   posY < viewBottom;
        }
        // ===== CLEANUP =====
        // ===== CLEANUP =====
        public void StopGameTickTimer()
        {
            if (gameTickTimer != null)
            {
                gameTickTimer.Stop();
                gameTickTimer.Dispose();
                gameTickTimer = null;  // ✅ ADD THIS LINE
            }
        }

        // Update ClearAll to stop timers
        public void ClearAll()
        {
          //  base.ClearAll(); // If you have a base ClearAll, otherwise remove this line
            WarRegistry.ClearAll();
            StopGameTickTimer();
            StopEdgeScrollTimer();
        }
        // ===== UPGRADE SYSTEM =====
        public bool PurchaseRangedDamageUpgrade()
        {
            if (PlayerFaction == null || PlayerFaction.RangedDamageLevel >= 3)
            {
                Console.WriteLine("❌ Ranged damage already max level!");
                return false;
            }

            var (gold, lumber) = PlayerFaction.GetRangedDamageUpgradeCost();

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford ranged damage upgrade: Need {gold}g {lumber}w");
                return false;
            }

            PlayerGold -= gold;
            PlayerLumber -= lumber;
            PlayerFaction.RangedDamageLevel++;

            // Apply upgrade to all existing ranged units
            foreach (var unit in WarRegistry.Units.Where(u => u.Race == PlayerFaction.Race && (u is Archer || u is TrollAxeThrower)))
            {
                unit.AttackDamage++;
            }

            Console.WriteLine($"✅ Ranged damage upgraded to level {PlayerFaction.RangedDamageLevel}!");
            return true;
        }

        public bool PurchaseRangedArmorUpgrade()
        {
            if (PlayerFaction == null || PlayerFaction.RangedArmorLevel >= 3)
            {
                Console.WriteLine("❌ Ranged armor already max level!");
                return false;
            }

            var (gold, lumber) = PlayerFaction.GetRangedArmorUpgradeCost();

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford ranged armor upgrade: Need {gold}g {lumber}w");
                return false;
            }

            PlayerGold -= gold;
            PlayerLumber -= lumber;
            PlayerFaction.RangedArmorLevel++;

            // Apply upgrade to all existing ranged units
            foreach (var unit in WarRegistry.Units.Where(u => u.Race == PlayerFaction.Race && (u is Archer || u is TrollAxeThrower)))
            {
                unit.MaxHP++;
                unit.HP = Math.Min(unit.HP + 1, unit.MaxHP);
            }

            Console.WriteLine($"✅ Ranged armor upgraded to level {PlayerFaction.RangedArmorLevel}!");
            return true;
        }

        // ✅ NEW: MELEE UPGRADE METHODS
        public bool PurchaseMeleeDamageUpgrade()
        {
            if (PlayerFaction == null || PlayerFaction.MeleeDamageLevel >= 3)
            {
                Console.WriteLine("❌ Melee damage already max level!");
                return false;
            }

            var (gold, lumber) = PlayerFaction.GetMeleeDamageUpgradeCost();

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford melee damage upgrade: Need {gold}g {lumber}w");
                return false;
            }

            PlayerGold -= gold;
            PlayerLumber -= lumber;
            PlayerFaction.MeleeDamageLevel++;

            // Apply upgrade to all existing melee units
            foreach (var unit in WarRegistry.Units.Where(u => u.Race == PlayerFaction.Race &&
                                                              (u is Footman || u is Brigand || u is Ogre)))
            {
                unit.AttackDamage++;
            }

            Console.WriteLine($"✅ Melee damage upgraded to level {PlayerFaction.MeleeDamageLevel}!");
            return true;
        }

        public bool PurchaseMeleeArmorUpgrade()
        {
            if (PlayerFaction == null || PlayerFaction.MeleeArmorLevel >= 3)
            {
                Console.WriteLine("❌ Melee armor already max level!");
                return false;
            }

            var (gold, lumber) = PlayerFaction.GetMeleeArmorUpgradeCost();

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford melee armor upgrade: Need {gold}g {lumber}w");
                return false;
            }

            PlayerGold -= gold;
            PlayerLumber -= lumber;
            PlayerFaction.MeleeArmorLevel++;

            // Apply upgrade to all existing melee units
            foreach (var unit in WarRegistry.Units.Where(u => u.Race == PlayerFaction.Race &&
                                                              (u is Footman || u is Brigand || u is Ogre)))
            {
                unit.MaxHP++;
                unit.HP = Math.Min(unit.HP + 1, unit.MaxHP);
            }

            Console.WriteLine($"✅ Melee armor upgraded to level {PlayerFaction.MeleeArmorLevel}!");
            return true;
        }

        // ===== PURCHASE MOUNTED COMBAT UPGRADE (HUMAN) =====
        public bool PurchaseMountedCombatUpgrade()
        {
            if (PlayerFaction == null || PlayerFaction.HasMountedCombatUpgrade)
            {
                Console.WriteLine("❌ Mounted Combat already researched!");
                return false;
            }

            var (gold, lumber) = PlayerFaction.GetMountedCombatUpgradeCost();

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford Mounted Combat: Need {gold}g {lumber}w");
                return false;
            }

            PlayerGold -= gold;
            PlayerLumber -= lumber;
            PlayerFaction.HasMountedCombatUpgrade = true;

            // Enable mounting for all Footmen
            foreach (var unit in WarRegistry.Units.OfType<Footman>().Where(u => u.Race == RaceType.Human))
            {
                unit.CanMount = true;
            }

            Console.WriteLine($"✅ Mounted Combat researched! Footmen can now mount Horses.");
            return true;
        }

        // ===== PURCHASE MOUNTED WARFARE UPGRADE (ORC) =====
        public bool PurchaseMountedWarfareUpgrade()
        {
            if (PlayerFaction == null || PlayerFaction.HasMountedWarfareUpgrade)
            {
                Console.WriteLine("❌ Mounted Warfare already researched!");
                return false;
            }

            var (gold, lumber) = PlayerFaction.GetMountedWarfareUpgradeCost();

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford Mounted Warfare: Need {gold}g {lumber}w");
                return false;
            }

            PlayerGold -= gold;
            PlayerLumber -= lumber;
            PlayerFaction.HasMountedWarfareUpgrade = true;

            // Enable mounting for all Grunts
            foreach (var unit in WarRegistry.Units.Where(u => u is Footman && u.Race == RaceType.Orc))
            {
                unit.CanMount = true;
            }

            Console.WriteLine($"✅ Mounted Warfare researched! Grunts can now mount Wolves.");
            return true;
        }

        // ===== ACTIVATE MOUNT SKILL =====
        public void ActivateMountSkill(WarUnit infantry)
        {
            if (!infantry.CanMount || infantry.MountType == null)
            {
                Console.WriteLine("❌ Unit cannot mount!");
                return;
            }

            // Search for compatible mount within 400px radius
            var nearbyMount = WarRegistry.Units
                .Where(u => u.IsMountable &&
                            u.PlaceholderName.Contains(infantry.MountType) &&
                            u.OwnerPlayerIndex == infantry.OwnerPlayerIndex &&
                            u.State != UnitState.Dead)
                .OrderBy(u => DistanceTo(infantry, u))
                .FirstOrDefault(u => DistanceTo(infantry, u) <= 400);

            if (nearbyMount == null)
            {
                Console.WriteLine($"❌ No {infantry.MountType} found within 400px!");
                return;
            }

            // Calculate spawn position (average of infantry and mount)
            int spawnX = (infantry.PosX + nearbyMount.PosX) / 2;
            int spawnY = (infantry.PosY + nearbyMount.PosY) / 2;

            // Create mounted unit
            WarUnit? mountedUnit = null;

            if (infantry.Race == RaceType.Human && nearbyMount is Horse)
            {
                mountedUnit = new Knight(spawnX, spawnY, RaceType.Human);
            }
            else if (infantry.Race == RaceType.Orc && nearbyMount is Wolf)
            {
                mountedUnit = new OrcRaider(spawnX, spawnY, RaceType.Orc);
            }

            if (mountedUnit != null)
            {
                // Inherit ownership
                mountedUnit.OwnerFaction = infantry.OwnerFaction;
                mountedUnit.OwnerPlayerIndex = infantry.OwnerPlayerIndex;
                mountedUnit.OwnerTeam = infantry.OwnerTeam;

                // Remove infantry and mount
                WarRegistry.Units.Remove(infantry);
                WarRegistry.Units.Remove(nearbyMount);

                // Deselect if they were selected
                if (WarRegistry.SelectedUnits.Contains(infantry))
                {
                    WarRegistry.SelectedUnits.Remove(infantry);
                }
                if (WarRegistry.SelectedUnits.Contains(nearbyMount))
                {
                    WarRegistry.SelectedUnits.Remove(nearbyMount);
                }

                // Add mounted unit
                WarRegistry.Units.Add(mountedUnit);

                // Auto-select the new mounted unit
                WarRegistry.SelectUnit(mountedUnit);

                Console.WriteLine($"🐴 {infantry.PlaceholderName} mounted {nearbyMount.PlaceholderName} → {mountedUnit.PlaceholderName} created!");
            }
        }
        // ===== CASTLE UPGRADE SYSTEM =====
        // ===== CASTLE UPGRADE SYSTEM =====
        public bool UpgradeTownHallToCastle(TownHall townHall)
        {
            if (PlayerFaction == null)
            {
                Console.WriteLine("❌ No player faction!");
                return false;
            }

            // ✅ Check requirements: Must have Barracks, Archery Range, AND Church/Cultist Hut
            bool hasBarracks = WarRegistry.Buildings
                .OfType<Barracks>()
                .Any(b => b.Race == PlayerFaction.Race && b.IsConstructed);

            bool hasArcheryRange = WarRegistry.Buildings
                .OfType<ArcheryRange>()
                .Any(b => b.Race == PlayerFaction.Race && b.IsConstructed);

            bool hasChurch = PlayerFaction.Race == RaceType.Human
                ? WarRegistry.Buildings.OfType<Church>().Any(b => b.Race == PlayerFaction.Race && b.IsConstructed)
                : WarRegistry.Buildings.OfType<CultistHut>().Any(b => b.Race == PlayerFaction.Race && b.IsConstructed);

            if (!hasBarracks)
            {
                Console.WriteLine("❌ Cannot upgrade to Castle: Requires Barracks!");
                return false;
            }

            if (!hasArcheryRange)
            {
                Console.WriteLine("❌ Cannot upgrade to Castle: Requires Archery Range!");
                return false;
            }

            if (!hasChurch)
            {
                string churchName = PlayerFaction.Race == RaceType.Human ? "Church" : "Cultist Hut";
                Console.WriteLine($"❌ Cannot upgrade to Castle: Requires {churchName}!");
                return false;
            }

            // ✅ Check cost
            if (!ProductionSystem.BuildingCosts.ContainsKey("Castle"))
            {
                Console.WriteLine("❌ Castle upgrade cost not found!");
                return false;
            }

            var (gold, lumber, time) = ProductionSystem.BuildingCosts["Castle"];

            if (PlayerGold < gold || PlayerLumber < lumber)
            {
                Console.WriteLine($"❌ Cannot afford Castle upgrade: Need {gold}g {lumber}w, have {PlayerGold}g {PlayerLumber}w");
                return false;
            }

            // ✅ Deduct resources
            PlayerGold -= gold;
            PlayerLumber -= lumber;

            // ✅ Replace Town Hall with Castle
            var castle = new Castle(townHall);

            // ✅ Transfer ownership from Town Hall to Castle
            castle.OwnerFaction = townHall.OwnerFaction;
            castle.OwnerPlayerIndex = townHall.OwnerPlayerIndex;
            castle.OwnerTeam = townHall.OwnerTeam;

            // Remove old Town Hall
            WarRegistry.Buildings.Remove(townHall);

            // Add new Castle
            WarRegistry.Buildings.Add(castle);

            // ✅ Update selection if Town Hall was selected
            if (WarRegistry.SelectedBuilding == townHall)
            {
                WarRegistry.SelectedBuilding = castle;
                castle.IsSelected = true;
            }

            Console.WriteLine($"✅ Town Hall upgraded to Castle! HP: {castle.HP}/{castle.MaxHP}");
            return true;
        }


        // ===== MULTIPLAYER SUPPORT =====
        public List<PlayerConfig> Players { get; set; } = new();
        public bool TeamTogetherEnabled { get; set; } = true;
        // ===== GENERATE RANDOM SPAWN POINTS =====
        public void GenerateRandomSpawns(List<PlayerConfig> players)
        {
            Random rand = new Random();
            const int MIN_GRID = 11;   // 350px buffer from edge
            const int MAX_GRID = 113;  // 3618px (4096 - 350 - 128)

            if (!TeamTogetherEnabled)
            {
                // ===== SIMPLE SCATTER MODE =====
                // Generate random positions with 400px (12 grid) minimum distance
                foreach (var player in players)
                {
                    bool validSpawn = false;
                    int attempts = 0;

                    while (!validSpawn && attempts < 500)
                    {
                        attempts++;
                        int gridX = rand.Next(MIN_GRID, MAX_GRID + 1);
                        int gridY = rand.Next(MIN_GRID, MAX_GRID + 1);

                        // Check distance from all existing spawns
                        validSpawn = true;
                        foreach (var other in players)
                        {
                            if (other == player || other.SpawnPosition == (0, 0)) continue;

                            int dx = gridX - other.SpawnPosition.gridX;
                            int dy = gridY - other.SpawnPosition.gridY;
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance < 12) // 12 grid cells = ~400px
                            {
                                validSpawn = false;
                                break;
                            }
                        }

                        if (validSpawn)
                        {
                            player.SpawnPosition = (gridX, gridY);
                        }
                    }

                    if (attempts >= 500)
                    {
                        Console.WriteLine($"⚠️ Warning: Could not find valid spawn for {player.Name}, using fallback");
                        player.SpawnPosition = (rand.Next(MIN_GRID, MAX_GRID), rand.Next(MIN_GRID, MAX_GRID));
                    }
                }

                Console.WriteLine($"✅ Generated {players.Count} random spawn points (SCATTER mode)");
            }
            else
            {
                // ===== TEAM CLUSTERING MODE =====
                var teamGroups = players.GroupBy(p => p.Team).ToList();
                var teamCenters = new Dictionary<int, (int x, int y)>();

                // Step 1: Generate team center points (800px / 25 grid apart)
                foreach (var team in teamGroups)
                {
                    int teamNum = team.Key;
                    bool validCenter = false;
                    int attempts = 0;

                    while (!validCenter && attempts < 500)
                    {
                        attempts++;
                        int centerX = rand.Next(MIN_GRID + 10, MAX_GRID - 10); // Extra buffer for teammates
                        int centerY = rand.Next(MIN_GRID + 10, MAX_GRID - 10);

                        // Check distance from other team centers
                        validCenter = true;
                        foreach (var otherCenter in teamCenters.Values)
                        {
                            int dx = centerX - otherCenter.x;
                            int dy = centerY - otherCenter.y;
                            double distance = Math.Sqrt(dx * dx + dy * dy);

                            if (distance < 25) // 25 grid cells = ~800px
                            {
                                validCenter = false;
                                break;
                            }
                        }

                        if (validCenter)
                        {
                            teamCenters[teamNum] = (centerX, centerY);
                        }
                    }

                    if (attempts >= 500)
                    {
                        Console.WriteLine($"⚠️ Warning: Could not find valid center for Team {teamNum}, using fallback");
                        teamCenters[teamNum] = (rand.Next(MIN_GRID, MAX_GRID), rand.Next(MIN_GRID, MAX_GRID));
                    }
                }

                // Step 2: Spawn teammates around their team center (200px / 6 grid radius)
                foreach (var team in teamGroups)
                {
                    int teamNum = team.Key;
                    var center = teamCenters[teamNum];

                    foreach (var player in team)
                    {
                        bool validSpawn = false;
                        int attempts = 0;

                        while (!validSpawn && attempts < 100)
                        {
                            attempts++;

                            // Spawn within 6 grid cells of center
                            int offsetX = rand.Next(-6, 7);
                            int offsetY = rand.Next(-6, 7);
                            int gridX = Math.Clamp(center.x + offsetX, MIN_GRID, MAX_GRID);
                            int gridY = Math.Clamp(center.y + offsetY, MIN_GRID, MAX_GRID);

                            // Check distance from teammates (minimum 3 grid / ~100px)
                            validSpawn = true;
                            foreach (var teammate in team)
                            {
                                if (teammate == player || teammate.SpawnPosition == (0, 0)) continue;

                                int dx = gridX - teammate.SpawnPosition.gridX;
                                int dy = gridY - teammate.SpawnPosition.gridY;
                                double distance = Math.Sqrt(dx * dx + dy * dy);

                                if (distance < 3) // Minimum 3 grid cells apart within team
                                {
                                    validSpawn = false;
                                    break;
                                }
                            }

                            if (validSpawn)
                            {
                                player.SpawnPosition = (gridX, gridY);
                            }
                        }

                        if (attempts >= 100)
                        {
                            Console.WriteLine($"⚠️ Warning: Could not find valid spawn for {player.Name}, using team center");
                            player.SpawnPosition = center;
                        }
                    }
                }

                Console.WriteLine($"✅ Generated spawns for {teamGroups.Count} teams (CLUSTER mode)");
                foreach (var team in teamGroups)
                {
                    Console.WriteLine($"   Team {team.Key}: {team.Count()} players at center {teamCenters[team.Key]}");
                }
            }
        }

        // ===== RESET ALL GAME STATE =====
        public void ResetGameState()
        {
            // Stop timers
            StopGameTickTimer();
            StopEdgeScrollTimer();

            // Clear entities
            ClearAll();

            // Reset camera
            CameraX = 0;
            CameraY = 0;

            // Reset edge scroll state
            ScrollingUp = false;
            ScrollingDown = false;
            ScrollingLeft = false;
            ScrollingRight = false;
            IsEdgeScrolling = false;

            // Reset build mode
            IsBuildMode = false;
            BuildingToBuild = null;
            GhostBuildingPosition = null;
            IsGhostPlacementValid = false;

            // Reset resources
            PlayerGold = 0;
            PlayerLumber = 0;
            PlayerCurrentPop = 0;
            PlayerMaxPop = 5;

            // Reset game state flags
            IsGameInitialized = false;
            IsGameOver = false;
            GameEndState = "";

            // Reset statistics
            UnitsTrainedCount = 0;
            BuildingsConstructedCount = 0;
            EnemyUnitsKilledCount = 0;
            EnemyBuildingsDestroyedCount = 0;
            TotalGoldGathered = 0;
            TotalLumberGathered = 0;

            Console.WriteLine("🔄 Game state fully reset");
        }









    }
}


