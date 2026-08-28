

namespace WarCraftLibrary
{
    public partial class WarMap : ComponentBase, IDisposable
    {

        private int lastClickWorldX = 0;
        private int lastClickWorldY = 0;
        private int lastClickGridX = 0;
        private int lastClickGridY = 0;

        // ===== DRAG SELECTION STATE =====
        private bool isDragging = false;
        private int dragStartX = 0;
        private int dragStartY = 0;
        private int dragCurrentX = 0;
        private int dragCurrentY = 0;
        private bool justCompletedDrag = false; // ✅ ADD THIS LINE


        // ✅ PART 3: DOUBLE-CLICK DETECTION
        private DateTime lastClickTime = DateTime.MinValue;
        private WarUnit? lastClickedUnit = null;
        private const int DoubleClickThresholdMs = 500; // 500ms window for double-click
        // Calculated drag box (world coordinates)
        private int dragBoxWorldX => Math.Min(dragStartX, dragCurrentX);
        private int dragBoxWorldY => Math.Min(dragStartY, dragCurrentY);
        private int dragBoxWidth => Math.Abs(dragCurrentX - dragStartX);
        private int dragBoxHeight => Math.Abs(dragCurrentY - dragStartY);

        private ElementReference viewportRef;
        private ElementReference minimapRef;

        [SupplyParameterFromQuery]
        public string? Map { get; set; }

        [SupplyParameterFromQuery]
        public string? Race { get; set; }


        // ===== KEYBOARD HANDLERS (keep existing WASD scroll, add ESC) =====
        private TechTree? techTree; // Add this field at the top with other fields

        private bool hasInitialized = false; // ✅ ADD THIS FIELD at top of class

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                Game.RequestUIRefresh = () => InvokeAsync(() =>
                {
                    StateHasChanged();
                    warUI?.Refresh();
                });

                // ===== PARSE URL PARAMETERS =====
                var uri = new Uri(NavManager.Uri);
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                string mapId = query["map"] ?? "forest";
                string? playersParam = query["players"];
                string? teamTogetherParam = query["teamTogether"];

                // Parse multiplayer data if present
                if (!string.IsNullOrEmpty(playersParam))
                {
                    Console.WriteLine($"🎮 Multiplayer lobby data detected!");

                    // Parse player data: "0:Player:Human:0|1:AI 1:Orc:1"
                    var playerEntries = playersParam.Split('|');

                    Game.Players.Clear();

                    foreach (var entry in playerEntries)
                    {
                        var parts = entry.Split(':');
                        if (parts.Length == 4)
                        {
                            int slotIndex = int.Parse(parts[0]);
                            string name = parts[1];
                            RaceType race = Enum.Parse<RaceType>(parts[2]);
                            int team = int.Parse(parts[3]);

                            Game.Players.Add(new PlayerConfig
                            {
                                SlotIndex = slotIndex,
                                Name = name,
                                Race = race,
                                Team = team,
                                IsHuman = (slotIndex == 0)
                            });

                            Console.WriteLine($"  📋 Slot {slotIndex}: {name} ({race}) Team {team}");
                        }
                    }

                    // Set team together flag
                    if (!string.IsNullOrEmpty(teamTogetherParam))
                    {
                        Game.TeamTogetherEnabled = bool.Parse(teamTogetherParam);
                        Console.WriteLine($"  🤝 Team Together: {Game.TeamTogetherEnabled}");
                    }

                    Console.WriteLine($"✅ Parsed {Game.Players.Count} players from URL");

                    // Initialize game with multiplayer data (race doesn't matter here)
                    Game.InitializeGame(mapId, RaceType.Human);
                }
                else
                {
                    // Legacy mode: Use Map and Race query params
                    if (!string.IsNullOrEmpty(Map) && !string.IsNullOrEmpty(Race))
                    {
                        if (Enum.TryParse<RaceType>(Race, out var raceType))
                        {
                            Game.InitializeGame(Map, raceType);
                        }
                    }
                    else
                    {
                        Game.InitializeGame("forest", RaceType.Human);
                    }
                }

                Game.InitEdgeScrollTimer();
                await InvokeAsync(StateHasChanged);
            }
        }

        private void HandleKeyDown(KeyboardEventArgs e)
        {
            // Camera scrolling
            if (e.Key == "w" || e.Key == "W" || e.Key == "ArrowUp")
            {
                Game.ScrollCameraUp();
            }
            else if (e.Key == "s" || e.Key == "S" || e.Key == "ArrowDown")
            {
                Game.ScrollCameraDown();
            }
            else if (e.Key == "a" || e.Key == "A" || e.Key == "ArrowLeft")
            {
                Game.ScrollCameraLeft();
            }
            else if (e.Key == "d" || e.Key == "D" || e.Key == "ArrowRight")
            {
                Game.ScrollCameraRight();
            }
            // Cancel build mode
            // Cancel build mode OR cast mode
            else if (e.Key == "Escape")
            {
                if (Game.IsCastMode)
                {
                    Game.CancelCastMode();
                }
                else if (Game.IsBuildMode)
                {
                    Game.CancelBuildMode();
                }
            }
            // ✅ NEW: Toggle Tech Tree with "T" key
            else if (e.Key == "t" || e.Key == "T")
            {
                techTree?.Toggle();
            }
            else if (e.Key == "h" || e.Key == "H")
            {
                warUI?.ToggleVisibility();
            }
            // Debug toggles
            else if (e.Key == "F1")
            {
                Game.ShowDebugGrid = !Game.ShowDebugGrid;
            }
            else if (e.Key == "F2")
            {
                Game.ShowDebugInfo = !Game.ShowDebugInfo;
            }
            // ✅ NEW: F3 toggles Fog of War
            else if (e.Key == "F3")
            {
                Game.FogOfWarEnabled = !Game.FogOfWarEnabled;
                Console.WriteLine($"🌫️ Fog of War: {(Game.FogOfWarEnabled ? "ON" : "OFF")}");
            }
            StateHasChanged();
        }
        private void HandleKeyUp(KeyboardEventArgs e)
        {
            // Reserved for future
        }

        private string GetBuildingSprite()
        {
            return Game.BuildingToBuild switch
            {
                "Barracks" => Game.PlayerFaction?.Race == RaceType.Human
                    ? "/wc1sprites/buildings/HumanBar001.png"
                    : "/wc1sprites/buildings/OrcBar001.png",
                _ => ""
            };
        }
        // ===== MOUSE MOVEMENT (keep existing edge scroll, add ghost building) =====
        [Inject] IJSRuntime JS { get; set; } = default!;
        private WarBuilding? hoveredBuilding;
        private WarUnit? hoveredUnit;
        private async void HandleMouseMove(MouseEventArgs e)
        {
            var rect = await JS.InvokeAsync<DomRect>("getElementRect", viewportRef);
            double viewportX = e.ClientX - rect.Left;
            double viewportY = e.ClientY - rect.Top;

            // Convert to world coords (do this ONCE at the top)
            int worldX = (int)(viewportX + Game.CameraX);
            int worldY = (int)(viewportY + Game.CameraY);

            // ✅ NEW: Update drag selection box
            if (isDragging)
            {
                dragCurrentX = worldX;
                dragCurrentY = worldY;
                StateHasChanged(); // Update drag box visual
                return; // Don't do edge scrolling while dragging
            }

            // Edge scrolling (existing code)
            Game.UpdateEdgeScroll(viewportX, viewportY);

            // ✅ Update ghost building position if in build mode
            if (Game.IsBuildMode)
            {
                // Snap to grid
                int gridX = worldX / Game.GridCellSize;
                int gridY = worldY / Game.GridCellSize;
                int snappedWorldX = gridX * Game.GridCellSize;
                int snappedWorldY = gridY * Game.GridCellSize;

                Game.GhostBuildingPosition = (snappedWorldX, snappedWorldY);

                // Check if placement is valid
                Game.IsGhostPlacementValid = Game.CanPlaceBuilding(gridX, gridY, Game.BuildingToBuild!);

                // 🔍 DEBUG: Uncomment this to see what's happening
                // Console.WriteLine($"Ghost: grid({gridX},{gridY}) world({snappedWorldX},{snappedWorldY}) valid={Game.IsGhostPlacementValid}");

                StateHasChanged();
            }

            // Hover detection
            hoveredBuilding = WarRegistry.GetBuildingAt(worldX, worldY);
            hoveredUnit = WarRegistry.GetUnitAt(worldX, worldY);
        }
        // ===== MOUSE DOWN - START DRAG SELECTION =====
        private async void HandleMouseDown(MouseEventArgs e)
        {
            // Only left-click (button 0) starts drag
            if (e.Button != 0) return;

            // Don't start drag in build mode
            if (Game.IsBuildMode) return;

            var rect = await JS.InvokeAsync<DomRect>("getElementRect", viewportRef);
            double viewportX = e.ClientX - rect.Left;
            double viewportY = e.ClientY - rect.Top;

            // Convert to world coordinates
            int worldX = (int)(viewportX + Game.CameraX);
            int worldY = (int)(viewportY + Game.CameraY);

            // Check if clicking on a unit or building
            var clickedUnit = WarRegistry.GetUnitAt(worldX, worldY);
            var clickedBuilding = WarRegistry.GetBuildingAt(worldX, worldY);

            // If clicking on something, don't start drag (let HandleClick handle it)
            if (clickedUnit != null || clickedBuilding != null)
            {
                return;
            }

            // Start drag selection
            isDragging = true;
            dragStartX = worldX;
            dragStartY = worldY;
            dragCurrentX = worldX;
            dragCurrentY = worldY;

            Console.WriteLine($"🖱️ Started drag at ({worldX}, {worldY})");
        }

        // ===== MOUSE UP - COMPLETE DRAG SELECTION =====
        private void HandleMouseUp(MouseEventArgs e)
        {
            if (!isDragging) return;

            isDragging = false;

            // Minimum drag distance (prevents accidental drags)
            if (dragBoxWidth < 10 && dragBoxHeight < 10)
            {
                Console.WriteLine("🖱️ Drag too small, canceling selection");
                StateHasChanged();
                return;
            }

            var selectedUnits = WarRegistry.Units
     .Where(u => u.OwnerPlayerIndex == 0 &&  // ✅ NEW: Only player's units
                 u.State != UnitState.Dead &&
                 u.PosX + u.Width >= dragBoxWorldX &&
                 u.PosX <= dragBoxWorldX + dragBoxWidth &&
                 u.PosY + u.Height >= dragBoxWorldY &&
                 u.PosY <= dragBoxWorldY + dragBoxHeight)
     .ToList();

            if (selectedUnits.Count > 0)
            {
                WarRegistry.SelectMultipleUnits(selectedUnits);
                Console.WriteLine($"✅ Selected {selectedUnits.Count} units via drag box");
                justCompletedDrag = true; // ✅ ADD THIS - Prevent click from clearing selection
            }
            else
            {
                WarRegistry.ClearSelection();
                Console.WriteLine("❌ No units in drag box");
            }

            StateHasChanged();
            warUI?.Refresh();
        }

        private void HandleMouseLeave(MouseEventArgs e)
        {
            Game.StopEdgeScroll();
        }

        public class DomRect
        {
            public double Left { get; set; }
            public double Top { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
        }

        // Add this field at top of class
        private WarUI? warUI;

        // ===== UPDATE HandleClick METHOD IN WarMap.razor.cs =====
        // Replace your existing HandleClick method with this:

        private async void HandleClick(MouseEventArgs e)
        {
            // ✅ Ignore click if we just completed a drag selection
            if (justCompletedDrag)
            {
                justCompletedDrag = false;
                Console.WriteLine("🖱️ Ignoring click - just completed drag selection");
                return;
            }

            // Get viewport position relative to the page
            var rect = await JS.InvokeAsync<DomRect>("getElementRect", viewportRef);
            double viewportX = e.ClientX - rect.Left;
            double viewportY = e.ClientY - rect.Top;

            // Convert to world coordinates
            int worldX = (int)(viewportX + Game.CameraX);
            int worldY = (int)(viewportY + Game.CameraY);

            // Convert to grid
            var (gridX, gridY) = Game.WorldToGrid(worldX, worldY);

            lastClickWorldX = worldX;
            lastClickWorldY = worldY;
            lastClickGridX = gridX;
            lastClickGridY = gridY;

            // Build mode - place building
            if (Game.IsBuildMode)
            {
                Game.PlaceBuilding(worldX, worldY);
                StateHasChanged();
                return;
            }

            // ✅ Check what was clicked
            var clickedBuilding = WarRegistry.GetBuildingAt(worldX, worldY);
            var clickedUnit = WarRegistry.GetUnitAt(worldX, worldY);
            var clickedGoldMine = WarRegistry.GetGoldMineAt(worldX, worldY);
            var clickedTree = WarRegistry.GetTreeAt(worldX, worldY);

            // ✅ PART 3: DOUBLE-CLICK DETECTION FOR UNITS
            if (clickedUnit != null)
            {
                // ✅ NEW: Allow selecting ANY unit (enemy, ally, own)
                var now = DateTime.Now;
                var timeSinceLastClick = (now - lastClickTime).TotalMilliseconds;

                // Check if this is a double-click on the same unit
                if (timeSinceLastClick < DoubleClickThresholdMs && lastClickedUnit == clickedUnit)
                {
                    // ✅ Only select multiple if it's player's unit
                    if (clickedUnit.OwnerPlayerIndex == 0)
                    {
                        SelectAllNearbyUnitsOfType(clickedUnit);
                    }
                    else
                    {
                        // Just select this one enemy/ally unit
                        WarRegistry.SelectUnit(clickedUnit);
                        Console.WriteLine($"✅ Selected enemy/ally unit: {clickedUnit.PlaceholderName}");
                    }

                    lastClickTime = DateTime.MinValue;
                    lastClickedUnit = null;
                }
                else
                {
                    // Single click - select any unit
                    WarRegistry.SelectUnit(clickedUnit);
                    Console.WriteLine($"✅ Selected unit: {clickedUnit.PlaceholderName} (Owner: Player {clickedUnit.OwnerPlayerIndex})");

                    lastClickTime = now;
                    lastClickedUnit = clickedUnit;
                }

                StateHasChanged();
                warUI?.Refresh();
                return;
            }

            // Reset double-click tracking if clicking something else
            lastClickTime = DateTime.MinValue;
            lastClickedUnit = null;

            if (clickedBuilding != null)
            {
                // ✅ NEW: Allow selecting ANY building (enemy, ally, own)
                WarRegistry.SelectBuilding(clickedBuilding);
                Console.WriteLine($"✅ Selected building: {clickedBuilding.PlaceholderName} (Owner: Player {clickedBuilding.OwnerPlayerIndex})");
            }
            else if (clickedGoldMine != null)
            {
                WarRegistry.SelectGoldMine(clickedGoldMine);
                Console.WriteLine($"✅ Selected Gold Mine: {clickedGoldMine.GoldRemaining} gold remaining");
            }
            else if (clickedTree != null)
            {
                WarRegistry.SelectTree(clickedTree);
                Console.WriteLine($"✅ Selected Tree: {clickedTree.LumberRemaining} lumber remaining");
            }
            else
            {
                WarRegistry.ClearSelection();
                Console.WriteLine($"❌ Clicked empty space at ({worldX},{worldY})");
            }

            // Refresh both map and UI
            StateHasChanged();
            warUI?.Refresh();
        }

        // ✅ PART 3: SELECT ALL NEARBY UNITS OF SAME TYPE
        private void SelectAllNearbyUnitsOfType(WarUnit sourceUnit)
        {
            const int SelectionRadius = 500; // pixels

            // Get source unit's center position
            int sourceCenterX = sourceUnit.PosX + sourceUnit.Width / 2;
            int sourceCenterY = sourceUnit.PosY + sourceUnit.Height / 2;

            var nearbyUnits = WarRegistry.Units
        .Where(u =>
            u.OwnerPlayerIndex == 0 &&  // ✅ NEW: Only player's units
            u.GetType() == sourceUnit.GetType() && // Same unit type
            u.State != UnitState.Dead) // Not dead// Not dead
                    .Where(u =>
                {
                    // Calculate distance from source unit
                    int unitCenterX = u.PosX + u.Width / 2;
                    int unitCenterY = u.PosY + u.Height / 2;

                    int dx = unitCenterX - sourceCenterX;
                    int dy = unitCenterY - sourceCenterY;
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    return distance <= SelectionRadius;
                })
                .ToList();

            if (nearbyUnits.Count > 0)
            {
                WarRegistry.SelectMultipleUnits(nearbyUnits);
                Console.WriteLine($"🖱️🖱️ DOUBLE-CLICK: Selected {nearbyUnits.Count} {sourceUnit.PlaceholderName}(s) within {SelectionRadius}px");
            }
            else
            {
                // Fallback - just select the clicked unit
                WarRegistry.SelectUnit(sourceUnit);
                Console.WriteLine($"🖱️ No nearby units found, selected single {sourceUnit.PlaceholderName}");
            }
        }



        private async void HandleRightClick(MouseEventArgs e)
        {
            if (Game.IsBuildMode)
            {
                Game.CancelBuildMode();
                StateHasChanged();
                return;
            }

            var rect = await JS.InvokeAsync<DomRect>("getElementRect", viewportRef);
            double viewportX = e.ClientX - rect.Left;
            double viewportY = e.ClientY - rect.Top;

            int worldX = (int)(viewportX + Game.CameraX);
            int worldY = (int)(viewportY + Game.CameraY);

            Console.WriteLine($"🖱️ RIGHT-CLICK at world ({worldX}, {worldY})");

            if (WarRegistry.SelectedBuilding != null &&
      WarRegistry.SelectedBuilding.OwnerPlayerIndex == 0)  // ✅ NEW: Only player's buildings
            {
                // Set rally point for selected building
                WarRegistry.SelectedBuilding.RallyX = worldX;
                WarRegistry.SelectedBuilding.RallyY = worldY;
                WarRegistry.SelectedBuilding.HasCustomRallyPoint = true;

                Console.WriteLine($"🚩 Rally point set for {WarRegistry.SelectedBuilding.PlaceholderName} at ({worldX}, {worldY})");
                StateHasChanged();
                return;
            }

            // ✅ TESTING: Check for ANY units (friendly fire enabled)
            var clickedUnit = WarRegistry.GetUnitAt(worldX, worldY);
            var clickedBuilding = WarRegistry.GetBuildingAt(worldX, worldY);

            // ✅ TESTING: FRIENDLY FIRE - Removed race check
            // ✅ NEW: CAST MODE - Cast spell on right-clicked target
            if (Game.IsCastMode)
            {
                Game.CastSpellOnTarget(worldX, worldY);
                StateHasChanged();
                return;
            }

            // ✅ CANCEL BUILD MODE (existing code)
            if (Game.IsBuildMode)
            {
                Game.CancelBuildMode();
                StateHasChanged();
                return;
            }
            if (clickedUnit != null)
            {
                Console.WriteLine($"⚔️ Attack command on {clickedUnit.PlaceholderName} (Friendly fire enabled for testing)");
                Game.HandleAttackCommand(worldX, worldY);
            }
            else if (clickedBuilding != null && clickedBuilding.Race!= Game.PlayerFaction?.Race)
            {
                Console.WriteLine($"⚔️ Attack command on building {clickedBuilding.PlaceholderName}");
                Game.HandleAttackCommand(worldX, worldY);
            }
            // ✅ NEW: Manual resource return to Town Hall/Castle/Lumber Mill
            else if (clickedBuilding != null &&
            (clickedBuilding is TownHall || clickedBuilding is Castle || clickedBuilding is LumberMill) &&
            clickedBuilding.OwnerPlayerIndex == 0)  // ✅ NEW: Only player's buildings
            {
                // Filter peasants carrying resources
                var peasantsWithResources = WarRegistry.SelectedUnits
                    .OfType<Peasant>()
                    .Where(p => p.CarryingGold > 0 || p.CarryingLumber > 0)
                    .ToList();

                if (peasantsWithResources.Count > 0)
                {
                    foreach (var peasant in peasantsWithResources)
                    {
                        peasant.State = UnitState.ReturningResources;
                        peasant.TargetTownHall = clickedBuilding;
                        peasant.TargetX = clickedBuilding.PosX + clickedBuilding.Width / 2;
                        peasant.TargetY = clickedBuilding.PosY + clickedBuilding.Height / 2;

                        Console.WriteLine($"💰 {peasant.PlaceholderName} manually returning resources to {clickedBuilding.PlaceholderName}");
                    }

                    StateHasChanged();
                    return; // Don't process as movement command
                }
                else
                {
                    // No peasants with resources - fallback to normal movement
                    Console.WriteLine($"📍 Calling HandleRightClick in GameService (movement)");
                    Game.HandleRightClick(worldX, worldY);
                }
            }
            else
            {
                // ✅ NEW: Only allow commands if player units selected
                if (WarRegistry.SelectedUnits.Any(u => u.OwnerPlayerIndex != 0))
                {
                    Console.WriteLine($"❌ Cannot control enemy/ally units");
                    return;
                }

                Console.WriteLine($"📍 Calling HandleRightClick in GameService (movement)");
                Game.HandleRightClick(worldX, worldY);
            }

            StateHasChanged();
        }

        /* ===== MINIMAP RIGHT-CLICK - MOVE UNITS =====*/
        private async void HandleMinimapRightClick(MouseEventArgs e)
        {
            // Prevent default context menu
          //  e.PreventDefault();

            // No units selected? Nothing to do
            if (WarRegistry.SelectedUnits.Count == 0)
            {
                Console.WriteLine("❌ No units selected for minimap move command");
                return;
            }

            // ✅ Get accurate minimap position
            var rect = await JS.InvokeAsync<DomRect>("getMinimapRect", minimapRef);
            double minimapX = e.ClientX - rect.Left;
            double minimapY = e.ClientY - rect.Top;

            // Clamp to minimap bounds
            minimapX = Math.Clamp(minimapX, 0, Game.MinimapSize);
            minimapY = Math.Clamp(minimapY, 0, Game.MinimapSize);

            // ✅ Convert minimap coords to world coords
            int worldX = (int)(minimapX / Game.MinimapScale);
            int worldY = (int)(minimapY / Game.MinimapScale);

            Console.WriteLine($"🗺️ Minimap RIGHT-CLICK: Minimap({minimapX:F0}, {minimapY:F0}) → World({worldX}, {worldY})");

            // ✅ Send all selected units to this location
            foreach (var unit in WarRegistry.SelectedUnits)
            {
                unit.TargetX = worldX;
                unit.TargetY = worldY;
                unit.State = UnitState.Moving;
                Console.WriteLine($"  → {unit.PlaceholderName} moving to world ({worldX}, {worldY})");
            }

            Console.WriteLine($"✅ {WarRegistry.SelectedUnits.Count} units ordered to minimap location");

            StateHasChanged();
        }
        /* ===== MINIMAP CLICK =====*/

        private async void HandleMinimapClick(MouseEventArgs e)
        {
            var rect = await JS.InvokeAsync<DomRect>("getMinimapRect", minimapRef);
            double minimapX = e.ClientX - rect.Left;
            double minimapY = e.ClientY - rect.Top;

            minimapX = Math.Clamp(minimapX, 0, Game.MinimapSize);
            minimapY = Math.Clamp(minimapY, 0, Game.MinimapSize);

            Console.WriteLine($"🖱️ Minimap click: Raw({minimapX:F0}, {minimapY:F0}) Size: {Game.MinimapSize}px");

            // ✅ FIXED: Always jump to clicked position, don't adjust for selected units
            Game.JumpCameraToMinimap((int)minimapX, (int)minimapY);
            Console.WriteLine($"📍 Minimap clicked - Camera: ({Game.CameraX}, {Game.CameraY})");

            // ✅ Keep selection active - don't clear
            StateHasChanged();
            warUI?.Refresh();
        }

        // ===== HELPER: GET ANIMATION CONFIG =====
        private AnimationConfig GetAnimationConfig(AnimationState state)
        {
            return state switch
            {
                AnimationState.Idle => AnimationConfig.Idle,
                AnimationState.Move => AnimationConfig.Move,
                AnimationState.Attack => AnimationConfig.Attack,
                AnimationState.Death => AnimationConfig.Death,
                _ => AnimationConfig.Idle
            };
        }

      

        private void ReturnToMenu()
        {
            // ✅ PART 2: Proper cleanup before navigation
            Game.StopGameTickTimer();
            Game.StopEdgeScrollTimer();
            Game.ClearAll();
            Game.IsGameInitialized = false;
            WarRegistry.ClearAll();
            // ✅ Reset initialization flag so new game can initialize
            hasInitialized = false;

            NavManager.NavigateTo("/WarMainMenu");
            Console.WriteLine("🏠 Returned to menu with full cleanup");
        }
        // ===== GET VISION CIRCLES FOR SVG RENDERING =====
        private List<(int x, int y, int radius)> GetVisionCircles()
        {
            return FogOfWar.GetVisionCirclesForViewport(Game.PlayerFaction, Game);
        }
        // ===== FORMAT GAME DURATION =====
        // ===== FORMAT GAME DURATION =====
        private string FormatGameDuration()
        {
            var duration = DateTime.Now - Game.GameStartTime;
            return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        // ===== PLAYER STATS =====
        private int GetPlayerUnitsCount()
        {
            return WarRegistry.Units.Count(u => u.Race == Game.PlayerFaction?.Race && u.State != UnitState.Dead);
        }

        private int GetPlayerBuildingsCount()
        {
            return WarRegistry.Buildings.Count(b => b.Race == Game.PlayerFaction?.Race);
        }

        private int GetPlayerUnitsKilledCount()
        {
            // Total player units trained - remaining = killed
            return Game.UnitsTrainedCount - GetPlayerUnitsCount();
        }

        private int GetPlayerBuildingsDestroyedCount()
        {
            // Total buildings built - remaining = destroyed
            return Game.BuildingsConstructedCount - GetPlayerBuildingsCount();
        }

        // ===== ENEMY STATS =====
        private int GetEnemyUnitsCount()
        {
            return WarRegistry.Units.Count(u => u.Race == Game.AIFaction?.Race && u.State != UnitState.Dead);
        }

        private int GetEnemyBuildingsCount()
        {
            return WarRegistry.Buildings.Count(b => b.Race == Game.AIFaction?.Race);
        }

        private int GetEnemyUnitsTrainedCount()
        {
            // We need to track this - for now estimate from current + killed
            return GetEnemyUnitsCount() + Game.EnemyUnitsKilledCount;
        }

        private int GetEnemyBuildingsConstructedCount()
        {
            // Start with 1 (town hall) + current + destroyed
            return 1 + GetEnemyBuildingsCount() + Game.EnemyBuildingsDestroyedCount;
        }

        private int GetEnemyGoldGathered()
        {
            // Estimate: current gold + what was spent
            return (Game.AIFaction?.Gold ?? 0) + (Game.EnemyUnitsKilledCount * 400); // rough estimate
        }

        private int GetEnemyLumberGathered()
        {
            // Estimate: current lumber + what was spent
            return (Game.AIFaction?.Lumber ?? 0) + (Game.EnemyBuildingsDestroyedCount * 200); // rough estimate
        }
        // ===== CLEANUP =====

        // ===== CANVAS TILE RENDERING =====
        private ElementReference tileCanvasRef;
        private bool canvasNeedsRedraw = true;
        private int lastCanvasCameraX = -999;
        private int lastCanvasCameraY = -999;

        private async Task DrawTilesToCanvas()
        {
            if (Game.TileMap == null || Game.TileAtlas == null) return;

            // Only redraw if camera moved significantly (64px threshold)
            int deltaX = Math.Abs(Game.CameraX - lastCanvasCameraX);
            int deltaY = Math.Abs(Game.CameraY - lastCanvasCameraY);

            // ✅ CHANGE: From 64px to 32px for smoother scrolling
            if (deltaX < 32 && deltaY < 32 && !canvasNeedsRedraw) return;
            // Calculate visible tile range
            int tileSize = 32;
            int startX = Math.Max(0, (Game.CameraX - 32) / tileSize);
            int endX = Math.Min(Game.TileMap.GridWidth - 1, (Game.CameraX + Game.ViewportWidth + 32) / tileSize);
            int startY = Math.Max(0, (Game.CameraY - 32) / tileSize);
            int endY = Math.Min(Game.TileMap.GridHeight - 1, (Game.CameraY + Game.ViewportHeight + 32) / tileSize);

            // Build tile data for JavaScript
            var tileData = new List<object>();
            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int tileId = Game.TileMap.GetTile(x, y);
                    var (atlasX, atlasY) = Game.TileAtlas.GetAtlasPosition(tileId);

                    tileData.Add(new
                    {
                        x = x * tileSize,
                        y = y * tileSize,
                        atlasX = atlasX,
                        atlasY = atlasY
                    });
                }
            }

            // Call JavaScript to draw tiles
            await JS.InvokeVoidAsync("drawTilesToCanvas", tileCanvasRef, Game.TileAtlas.AtlasPath, tileData);

            lastCanvasCameraX = Game.CameraX;
            lastCanvasCameraY = Game.CameraY;
            canvasNeedsRedraw = false;

            Console.WriteLine($"🎨 Drew {tileData.Count} tiles to canvas");
        }
        /*
        // ===== DEBUG: FORCE END GAME METHODS =====
        private void ForceVictory()
        {
            Game.GameEndState = "VICTORY";
            Game.IsGameOver = true;
            Console.WriteLine("🏆 DEBUG: Forced VICTORY screen");
            StateHasChanged();
        }

        private void ForceDefeat()
        {
            Game.GameEndState = "DEFEAT";
            Game.IsGameOver = true;
            Console.WriteLine("💀 DEBUG: Forced DEFEAT screen");
            StateHasChanged();
        }


        // ===== DEBUG: KILL ALL ORCS (Test Victory) =====
        private void KillAllOrcs()
        {
            int unitsKilled = 0;
            int buildingsKilled = 0;

            // Kill all Orc units
            foreach (var unit in WarRegistry.Units.Where(u => u.Race == RaceType.Orc).ToList())
            {
                unit.HP = 0;
                unit.State = UnitState.Dead;
                unitsKilled++;
            }

            // Destroy all Orc buildings
            foreach (var building in WarRegistry.Buildings.Where(b => b.Race == RaceType.Orc).ToList())
            {
                building.HP = 0;
                building.CurrentBuildingState = BuildingAnimationState.Destroyed;
                buildingsKilled++;
            }

            Console.WriteLine($"☠️ DEBUG: Killed {unitsKilled} Orc units and {buildingsKilled} Orc buildings");
            StateHasChanged();
        }

        // ===== DEBUG: KILL ALL HUMANS (Test Defeat) =====
        private void KillAllHumans()
        {
            int unitsKilled = 0;
            int buildingsKilled = 0;

            // Kill all Human units
            foreach (var unit in WarRegistry.Units.Where(u => u.Race == RaceType.Human).ToList())
            {
                unit.HP = 0;
                unit.State = UnitState.Dead;
                unitsKilled++;
            }

            // Destroy all Human buildings
            foreach (var building in WarRegistry.Buildings.Where(b => b.Race == RaceType.Human).ToList())
            {
                building.HP = 0;
                building.CurrentBuildingState = BuildingAnimationState.Destroyed;
                buildingsKilled++;
            }

            Console.WriteLine($"☠️ DEBUG: Killed {unitsKilled} Human units and {buildingsKilled} Human buildings");
            StateHasChanged();
        }
        */
        public void Dispose()
        {
            Game.StopEdgeScrollTimer();
            Game.StopGameTickTimer();
        }
    }
}


