using System;
using System.Collections.Generic;
using System.Linq;

namespace WarCraftLibrary
{
    public static class WarRegistry
    {
        // ===== ENTITY LISTS =====
        public static List<WarBuilding> Buildings { get; set; } = new();
        public static List<WarUnit> Units { get; set; } = new();
        public static List<GoldMine> GoldMines { get; set; } = new();
        public static List<Tree> Trees { get; set; } = new();
        public static List<Projectile> Projectiles { get; set; } = new(); // ✅ NEW

        // ===== SELECTION STATE =====
        public static List<WarUnit> SelectedUnits { get; set; } = new();
        public static WarBuilding? SelectedBuilding { get; set; }

        // ✅ NEW: Resource node selection
        public static GoldMine? SelectedGoldMine { get; set; }
        public static Tree? SelectedTree { get; set; }


        public static void ClearAll()
        {
            Buildings.Clear();
            Units.Clear();
            GoldMines.Clear();
            Trees.Clear();
            Projectiles.Clear();  // ✅ NEW
            SelectedUnits.Clear();
            SelectedBuilding = null;
            SelectedGoldMine = null;
            SelectedTree = null;
        }


        // ===== SELECTION METHODS =====
        public static void SelectUnit(WarUnit unit)
        {
            ClearSelection();
            SelectedUnits.Add(unit);
            unit.IsSelected = true;
        }
        // ===== SELECT MULTIPLE UNITS (DRAG BOX) =====
        public static void SelectMultipleUnits(List<WarUnit> units)
        {
            ClearSelection();

            foreach (var unit in units)
            {
                SelectedUnits.Add(unit);
                unit.IsSelected = true;
            }

            Console.WriteLine($"📦 Selected {units.Count} units");
        }
        public static void SelectBuilding(WarBuilding building)
        {
            ClearSelection();
            SelectedBuilding = building;
            building.IsSelected = true;
        }

        // ✅ NEW: Select gold mine
        public static void SelectGoldMine(GoldMine mine)
        {
            ClearSelection();
            SelectedGoldMine = mine;
            Console.WriteLine($"Selected Gold Mine: {mine.GoldRemaining} gold remaining");
        }

        // ✅ NEW: Select tree
        public static void SelectTree(Tree tree)
        {
            ClearSelection();
            SelectedTree = tree;
            Console.WriteLine($"Selected Tree: {tree.LumberRemaining} lumber remaining");
        }

        public static void ClearSelection()
        {
            foreach (var unit in SelectedUnits)
            {
                unit.IsSelected = false;
            }
            SelectedUnits.Clear();

            if (SelectedBuilding != null)
            {
                SelectedBuilding.IsSelected = false;
                SelectedBuilding = null;
            }

            SelectedGoldMine = null;
            SelectedTree = null;
        }


        // ===== GET ENTITY AT POSITION =====
        public static WarBuilding? GetBuildingAt(int worldX, int worldY)
        {
            return Buildings.FirstOrDefault(b =>
                worldX >= b.PosX && worldX <= b.PosX + b.Width &&
                worldY >= b.PosY && worldY <= b.PosY + b.Height);
        }

        public static WarUnit? GetUnitAt(int worldX, int worldY)
        {
            return Units.FirstOrDefault(u =>
                u.State != UnitState.Dead &&  // ← Filter out dead units
                worldX >= u.PosX && worldX <= u.PosX + u.Width &&
                worldY >= u.PosY && worldY <= u.PosY + u.Height);
        }

        public static GoldMine? GetGoldMineAt(int worldX, int worldY)
        {
            return GoldMines.FirstOrDefault(m =>
                worldX >= m.PosX && worldX <= m.PosX + m.Width &&
                worldY >= m.PosY && worldY <= m.PosY + m.Height);
        }

        // ✅ NEW: Get tree at position
        public static Tree? GetTreeAt(int worldX, int worldY)
        {
            return Trees.FirstOrDefault(t =>
                !t.IsChopped &&
                worldX >= t.PosX && worldX <= t.PosX + t.Width &&
                worldY >= t.PosY && worldY <= t.PosY + t.Height);
        }

     public static void SpawnStartingBuildings(List<PlayerConfig> players)
{
    foreach (var player in players)
    {
                Console.WriteLine($"🔍 DEBUG: Player {player.SlotIndex} - Faction: {(player.Faction == null ? "NULL" : player.Faction.Name)}");

                var (spawnGridX, spawnGridY) = player.SpawnPosition;

        // ===== EXISTING: Create town hall =====
        var townHall = new TownHall(spawnGridX, spawnGridY, player.Race);
        townHall.OwnerFaction = player.Faction;
        townHall.OwnerPlayerIndex = player.SlotIndex;
        townHall.OwnerTeam = player.Team;
        Buildings.Add(townHall);
        
        // ===== NEW: MARK TOWN HALL ON GRID =====
        GridOccupancyMap.MarkBuilding(townHall);

        // ===== EXISTING: Spawn 5 peasants =====
        for (int i = 0; i < 5; i++)
        {
            int offsetX = (i % 3) * 40;
            int offsetY = (i / 3) * 40;
            int peasantWorldX = spawnGridX * 32 + offsetX;
            int peasantWorldY = (spawnGridY + 5) * 32 + offsetY;

            var peasant = player.Race == RaceType.Human
                ? new Peasant(peasantWorldX, peasantWorldY, RaceType.Human)
                : new Peasant(peasantWorldX, peasantWorldY, RaceType.Orc);

            peasant.OwnerFaction = player.Faction;
            peasant.OwnerPlayerIndex = player.SlotIndex;
            peasant.OwnerTeam = player.Team;

            Units.Add(peasant);
        }

        Console.WriteLine($"✅ Spawned {player.Name} at grid ({spawnGridX}, {spawnGridY})");
    }
}
        public static void SpawnStartingBuildings(GameMap map, RaceType playerRace, RaceType aiRace)
        {
            // Player Town Hall
            var (playerGridX, playerGridY) = map.PlayerSpawn;
            var playerTownHall = new TownHall(playerGridX, playerGridY, playerRace);
            playerTownHall.OwnerPlayerIndex = 0;
            playerTownHall.OwnerTeam = 1;
            Buildings.Add(playerTownHall);

            // ===== NEW: MARK ON GRID =====
            GridOccupancyMap.MarkBuilding(playerTownHall);

            // AI Town Hall
            var (aiGridX, aiGridY) = map.AISpawn;
            var aiTownHall = new TownHall(aiGridX, aiGridY, aiRace);
            aiTownHall.OwnerPlayerIndex = 1;
            aiTownHall.OwnerTeam = 2;
            Buildings.Add(aiTownHall);

            // ===== NEW: MARK ON GRID =====
            GridOccupancyMap.MarkBuilding(aiTownHall);

            // Spawn starting workers
            for (int i = 0; i < 5; i++)
            {
                int offsetX = (i % 3) * 40;
                int offsetY = (i / 3) * 40;

                var playerWorker = new Peasant(
                    playerTownHall.PosX + 150 + offsetX,
                    playerTownHall.PosY + 150 + offsetY,
                    playerRace
                );
                // ✅ NEW: Assign ownership
                playerWorker.OwnerPlayerIndex = 0;
                playerWorker.OwnerTeam = 1;

                var aiWorker = new Peasant(
                    aiTownHall.PosX + 150 + offsetX,
                    aiTownHall.PosY + 150 + offsetY,
                    aiRace
                );
                // ✅ NEW: Assign ownership
                aiWorker.OwnerPlayerIndex = 1;
                aiWorker.OwnerTeam = 2;

                Units.Add(playerWorker);
                Units.Add(aiWorker);
            }

            Console.WriteLine($"✅ Spawned starting bases (LEGACY MODE)");
        }
        // ===== SPAWN GOLD MINES (RANDOM) =====
        public static void SpawnGoldMines(int mineCount = 12)
        {
            Random rand = new Random();
            int attempts = 0;
            int maxAttempts = 1000; // ✅ INCREASED from 500

            const int mineSize = 96; // 3 tiles * 32px

            while (GoldMines.Count < mineCount && attempts < maxAttempts)
            {
                attempts++;

                int gridX = rand.Next(5, 120);
                int gridY = rand.Next(5, 120);

                int worldX = gridX * 32;
                int worldY = gridY * 32;

                // ✅ CHECK 1: Not overlapping other mines/trees
                if (IsAreaBlocked(worldX, worldY, mineSize, mineSize))
                    continue;

                // ✅ CHECK 2: At least 64px away from buildings
                if (IsNearBuilding(worldX, worldY, mineSize, mineSize, 64))
                {
                    Console.WriteLine($"⛏️ Gold mine too close to building at ({gridX}, {gridY}) - retrying...");
                    continue;
                }

                // ✅ SAFE TO SPAWN!
                GoldMines.Add(new GoldMine(gridX, gridY));
                Console.WriteLine($"⛏️ Spawned Gold Mine at grid ({gridX}, {gridY})");
            }

            Console.WriteLine($"✅ Spawned {GoldMines.Count} gold mines (Attempts: {attempts})");
        }

        // ===== SPAWN TREES =====
        public static void SpawnTrees(int clusters = 20, int treesPerCluster = 20)
        {
            Random rand = new Random();
            const int treeSize = 32;
            int totalTreesSpawned = 0;

            for (int cluster = 0; cluster < clusters; cluster++)
            {
                int centerX = rand.Next(10, 118);
                int centerY = rand.Next(10, 118);
                int clusterAttempts = 0;
                const int maxClusterAttempts = 100; // Give up on this cluster after 100 fails

                for (int i = 0; i < treesPerCluster; i++)
                {
                    if (clusterAttempts++ > maxClusterAttempts)
                    {
                        Console.WriteLine($"🌲 Cluster {cluster + 1} gave up after {maxClusterAttempts} attempts");
                        break; // Move to next cluster
                    }

                    int gridX = Math.Clamp(centerX + rand.Next(-5, 6), 0, 127);
                    int gridY = Math.Clamp(centerY + rand.Next(-5, 6), 0, 127);

                    int worldX = gridX * 32;
                    int worldY = gridY * 32;

                    // ✅ CHECK 1: Not overlapping other trees/mines
                    if (IsAreaBlocked(worldX, worldY, treeSize, treeSize))
                        continue;

                    // ✅ CHECK 2: At least 96px away from buildings
                    if (IsNearBuilding(worldX, worldY, treeSize, treeSize, 96))
                        continue;

                    // ✅ SAFE TO SPAWN!
                    Trees.Add(new Tree(gridX, gridY));
                    totalTreesSpawned++;
                }
            }

            Console.WriteLine($"✅ Spawned {totalTreesSpawned} trees in {clusters} clusters");
        }


        private static bool RectanglesOverlap(int x1, int y1, int w1, int h1,
                                      int x2, int y2, int w2, int h2)
        {
            return x1 < x2 + w2 &&
                   x1 + w1 > x2 &&
                   y1 < y2 + h2 &&
                   y1 + h1 > y2;
        }

        private static bool IsAreaBlocked(int x, int y, int w, int h)
        {
            foreach (var b in Buildings)
                if (RectanglesOverlap(x, y, w, h, b.PosX, b.PosY, b.Width, b.Height))
                    return true;

            foreach (var m in GoldMines)
                if (RectanglesOverlap(x, y, w, h, m.PosX, m.PosY, m.Width, m.Height))
                    return true;

            foreach (var t in Trees)
                if (!t.IsChopped &&
                    RectanglesOverlap(x, y, w, h, t.PosX, t.PosY, t.Width, t.Height))
                    return true;

            return false;
        }


        // ===== CHECK IF TOO CLOSE TO BUILDINGS =====
        private static bool IsNearBuilding(int x, int y, int w, int h, int buffer)
        {
            foreach (var building in Buildings)
            {
                // Expand building's collision box by buffer amount
                int expandedX = building.PosX - buffer;
                int expandedY = building.PosY - buffer;
                int expandedW = building.Width + (buffer * 2);
                int expandedH = building.Height + (buffer * 2);

                // Check if resource would be inside expanded zone
                if (RectanglesOverlap(x, y, w, h, expandedX, expandedY, expandedW, expandedH))
                {
                    return true; // Too close to a building!
                }
            }

            return false; // Safe distance from all buildings
        }



    }
}