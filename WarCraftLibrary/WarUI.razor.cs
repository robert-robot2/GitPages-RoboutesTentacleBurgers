// ===== UPDATED WarUI.razor.cs =====
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Text;
using WarCraftLibrary;
using static WarCraftLibrary.WarMap;

namespace WarCraftLibrary
{
    public partial class WarUI : ComponentBase
    {
        // At the top of the class, add:
        public bool IsVisible { get; set; } = true;

        public void ToggleVisibility()
        {
            IsVisible = !IsVisible;
            StateHasChanged();
        }

        // Add this new method for multi-unit stance control:
        private void SetMultiUnitStance(UnitStance newStance)
        {
            foreach (var unit in WarRegistry.SelectedUnits)
            {
                unit.Stance = newStance;

                // Special behavior for Hold stance
                if (newStance == UnitStance.Hold)
                {
                    unit.TargetX = null;
                    unit.TargetY = null;
                    unit.State = UnitState.Idle;
                }
            }

            Console.WriteLine($"🎯 Set stance to {newStance} for {WarRegistry.SelectedUnits.Count} units");
            StateHasChanged();
        }

        // ===== NEW: CASTLE UPGRADE COMMAND =====
        private void UpgradeToCastle(TownHall townHall)
        {
            if (Game.UpgradeTownHallToCastle(townHall))
            {
                Console.WriteLine("✅ Town Hall upgraded to Castle!");
                StateHasChanged();
            }
            else
            {
                Console.WriteLine("❌ Failed to upgrade to Castle");
            }
        }

        private void TrainPeasant(WarBuilding building)
        {
            string unitType = building.Race == RaceType.Human ? "Peasant" : "Peon";
            bool success = ProductionSystem.QueueUnit(building, unitType, Game);
            if (success)
            {
                Console.WriteLine($"✅ Queued {unitType}");
                // ✅ FIX: Don't change selection - just refresh UI
                StateHasChanged();
            }
            else
            {
                Console.WriteLine($"❌ Failed to queue {unitType}");
            }
        }
        private void TrainFootman(WarBuilding building)
        {
            string unitType = building.Race == RaceType.Human ? "Footman" : "Grunt";
            bool success = ProductionSystem.QueueUnit(building, unitType, Game);
            if (success)
            {
                Console.WriteLine($"✅ Queued {unitType}");
                // ✅ FIX: Don't change selection - just refresh UI
                StateHasChanged();
            }
        }
        // ===== BUILD ROAD COMMAND =====
        private void BuildRoad()
        {
            Game.EnterBuildMode("Road");
            Console.WriteLine("🛤️ Entered Road build mode");
        }
        private void BuildFarm()
        {
            Game.EnterBuildMode("Farm");
            Console.WriteLine("🏗️ Entered farm build mode");
        }
        // In WarUI.razor.cs
        private void BuildBarracks(MouseEventArgs e)
        {
            Game.EnterBuildMode("Barracks");
            Console.WriteLine("🏗️ Entered build mode");

            // ✅ This prevents the click from reaching the map
            StateHasChanged();
        }

        // ===== ABILITY CASTING =====
        private void CastAbility(WarUnit unit)
        {
            if (unit.AbilityName == null) return;

            // Enter cast mode
            Game.EnterCastMode(unit.AbilityName, unit);
            Console.WriteLine($"✨ Click an ally to cast {unit.AbilityName}");
            StateHasChanged();
        }

        // ===== TOGGLE AUTO-CAST =====
        private void ToggleAutoCast(WarUnit unit)
        {
            unit.AutoCastEnabled = !unit.AutoCastEnabled;
            Console.WriteLine($"🔄 {unit.PlaceholderName} auto-cast: {(unit.AutoCastEnabled ? "ON" : "OFF")}");
            StateHasChanged();
        }
        // ===== TRAIN PRIEST/CULTIST =====
        private void TrainPriest(WarBuilding building)
{
    bool success = ProductionSystem.QueueUnit(building, "Priest", Game);
    if (success)
    {
        Console.WriteLine($"✅ Queued Priest");
        StateHasChanged();
    }
}

private void TrainCultist(WarBuilding building)
{
    bool success = ProductionSystem.QueueUnit(building, "Cultist", Game);
    if (success)
    {
        Console.WriteLine($"✅ Queued Cultist");
        StateHasChanged();
    }
}
        // ===== BUILD CHURCH/CULTIST HUT =====
        private void BuildChurch()
        {
            Game.EnterBuildMode("Church");
            Console.WriteLine("⛪ Entered Church build mode");
        }

        private void BuildCultistHut()
        {
            Game.EnterBuildMode("CultistHut");
            Console.WriteLine("🔮 Entered Cultist Hut build mode");
        }
        // ===== BUILD TOWER COMMANDS =====
        private void BuildWoodTower()
        {
            Game.EnterBuildMode("WoodTower");
            Console.WriteLine("🗼 Entered Wood Tower build mode");
        }
        // ===== BUILD WALL COMMANDS =====
        private void BuildWoodenWall()
        {
            Game.EnterBuildMode("WoodenWall");
            Console.WriteLine("🪵 Entered Wooden Wall build mode");
        }
        // ===== BUILD STABLES (HUMAN TIER 1) =====
        private void BuildStables()
        {
            Game.EnterBuildMode("Stables");
            Console.WriteLine("🐴 Entered Stables build mode");
        }

        // ===== BUILD PEN (ORC TIER 1) =====
        private void BuildPen()
        {
            Game.EnterBuildMode("Pen");
            Console.WriteLine("🐺 Entered Pen build mode");
        }

        // ===== BUILD KNIGHTS HOLD (HUMAN TIER 2) =====
        private void BuildKnightsHold()
        {
            // Check if has Castle
            var hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == Game.PlayerFaction?.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build Knights Hold: Requires Castle!");
                return;
            }

            Game.EnterBuildMode("KnightsHold");
            Console.WriteLine("⚔️ Entered Knights Hold build mode");
        }

        // ===== BUILD RAIDER LAIR (ORC TIER 2) =====
        private void BuildRaiderLair()
        {
            // Check if has Fortress
            var hasFortress = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == Game.PlayerFaction?.Race && c.IsConstructed);

            if (!hasFortress)
            {
                Console.WriteLine("❌ Cannot build Raider Lair: Requires Fortress!");
                return;
            }

            Game.EnterBuildMode("RaiderLair");
            Console.WriteLine("🏴‍☠️ Entered Raider Lair build mode");
        }
        // ===== RESEARCH MOUNTED COMBAT (HUMAN) =====
        private void ResearchMountedCombat()
        {
            if (Game.PurchaseMountedCombatUpgrade())
            {
                Console.WriteLine("✅ Mounted Combat researched!");
                StateHasChanged();
            }
        }

        // ===== RESEARCH MOUNTED WARFARE (ORC) =====
        private void ResearchMountedWarfare()
        {
            if (Game.PurchaseMountedWarfareUpgrade())
            {
                Console.WriteLine("✅ Mounted Warfare researched!");
                StateHasChanged();
            }
        }

        // ===== ACTIVATE MOUNT SKILL =====
        private void ActivateMount(WarUnit unit)
        {
            Game.ActivateMountSkill(unit);
            StateHasChanged();
        }
        // ===== TRAIN HORSE (FROM STABLES) =====
        private void TrainHorse(WarBuilding building)
        {
            bool success = ProductionSystem.QueueUnit(building, "Horse", Game);
            if (success)
            {
                Console.WriteLine($"✅ Queued Horse");
                StateHasChanged();
            }
        }

        // ===== TRAIN WOLF (FROM PEN) =====
        private void TrainWolf(WarBuilding building)
        {
            bool success = ProductionSystem.QueueUnit(building, "Wolf", Game);
            if (success)
            {
                Console.WriteLine($"✅ Queued Wolf");
                StateHasChanged();
            }
        }
        private void BuildStoneWall()
        {
            // Check if has Castle
            var hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == Game.PlayerFaction?.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build Stone Wall: Requires Castle!");
                return;
            }

            Game.EnterBuildMode("StoneWall");
            Console.WriteLine("🧱 Entered Stone Wall build mode");
        }
        private void BuildStoneTower()
        {
            // Check if has Castle
            var hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == Game.PlayerFaction?.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build Stone Tower: Requires Castle!");
                return;
            }

            Game.EnterBuildMode("StoneTower");
            Console.WriteLine("🏰 Entered Stone Tower build mode");
        }

        // ===== NEW: BUILD COMMANDS =====
        private void BuildLumberMill()
        {
            Game.EnterBuildMode("LumberMill");
            Console.WriteLine("🏗️ Entered Lumber Mill build mode");
        }

        private void BuildArcheryRange()
        {
            Game.EnterBuildMode("ArcheryRange");
            Console.WriteLine("🏗️ Entered Archery Range build mode");
        }

        // ===== NEW: TRAIN RANGED UNIT =====
        private void TrainRangedUnit(WarBuilding building)
        {
            string unitType = building.Race == RaceType.Human ? "Archer" : "TrollAxeThrower";
            bool success = ProductionSystem.QueueUnit(building, unitType, Game);

            if (success)
            {
                Console.WriteLine($"✅ Queued {unitType}");
                StateHasChanged();
            }
            else
            {
                Console.WriteLine($"❌ Failed to queue {unitType}");
            }
        }

        // ===== NEW: UPGRADE COMMANDS =====
        private void UpgradeRangedDamage()
        {
            if (Game.PurchaseRangedDamageUpgrade())
            {
                Console.WriteLine("✅ Ranged damage upgraded!");
                StateHasChanged();
            }
        }

        private void UpgradeRangedArmor()
        {
            if (Game.PurchaseRangedArmorUpgrade())
            {
                Console.WriteLine("✅ Ranged armor upgraded!");
                StateHasChanged();
            }
        }

        // ===== NEW: BLACKSMITH BUILD COMMAND =====
        private void BuildBlacksmith()
        {
            Game.EnterBuildMode("Blacksmith");
            Console.WriteLine("🏗️ Entered Blacksmith build mode");
        }


        // ===== NEW: TRAIN ADVANCED INFANTRY =====
        private void TrainAdvancedInfantry(WarBuilding building)
        {
            string unitType = building.Race == RaceType.Human ? "Brigand" : "Ogre";
            bool success = ProductionSystem.QueueUnit(building, unitType, Game);

            if (success)
            {
                Console.WriteLine($"✅ Queued {unitType}");
                StateHasChanged();
            }
            else
            {
                Console.WriteLine($"❌ Failed to queue {unitType}");
            }
        }


        // ===== NEW: MELEE UPGRADE COMMANDS =====
        private void UpgradeMeleeDamage()
        {
            if (Game.PurchaseMeleeDamageUpgrade())
            {
                Console.WriteLine("✅ Melee damage upgraded!");
                StateHasChanged();
            }
        }

        private void UpgradeMeleeArmor()
        {
            if (Game.PurchaseMeleeArmorUpgrade())
            {
                Console.WriteLine("✅ Melee armor upgraded!");
                StateHasChanged();
            }
        }
        // ===== BUILD TOWN HALL (REQUIRES CASTLE) =====
        private void BuildTownHall()
        {
            // Check if has Castle
            var hasCastle = WarRegistry.Buildings
                .OfType<Castle>()
                .Any(c => c.Race == Game.PlayerFaction?.Race && c.IsConstructed);

            if (!hasCastle)
            {
                Console.WriteLine("❌ Cannot build Town Hall: Requires Castle!");
                return;
            }

            Game.EnterBuildMode("TownHall");
            Console.WriteLine("🏰 Entered Town Hall build mode");
        }

        public void Refresh()
        {
            StateHasChanged();
        }

        // ✅ NEW: Helper methods for resource nodes
        private bool HasResourceNodeSelected()
        {
            return WarRegistry.SelectedGoldMine != null || WarRegistry.SelectedTree != null;
        }

        private string GetResourceNodeName()
        {
            if (WarRegistry.SelectedGoldMine != null)
                return "Gold Mine";
            if (WarRegistry.SelectedTree != null)
                return "Tree";
            return "";
        }

        private string GetResourceAmount()
        {
            if (WarRegistry.SelectedGoldMine != null)
                return $"{WarRegistry.SelectedGoldMine.GoldRemaining} Gold";
            if (WarRegistry.SelectedTree != null)
                return $"{WarRegistry.SelectedTree.LumberRemaining} Lumber";
            return "";
        }

        private string GetResourceIcon()
        {
            if (WarRegistry.SelectedGoldMine != null)
                return "💰";
            if (WarRegistry.SelectedTree != null)
                return "🌲";
            return "";
        }

     

        // ===== NEW: TECH TREE HELPER METHODS =====
        private bool HasBuilding<T>() where T : WarBuilding
        {
            return WarRegistry.Buildings
                .OfType<T>()
                .Any(b => b.Race == Game.PlayerFaction?.Race && b.IsConstructed);
        }

        private bool HasBlacksmith()
        {
            return HasBuilding<Blacksmith>();
        }

        private bool HasLumberMill()
        {
            return HasBuilding<LumberMill>();
        }

        private bool HasArcheryRange()
        {
            return HasBuilding<ArcheryRange>();
        }

        private bool HasBarracks()
        {
            return HasBuilding<Barracks>();
        }

        private bool CanTrainUnit(string unitType)
        {
            // Check building requirements for specific units
            return unitType switch
            {
                "Footman" or "Grunt" or "Brigand" or "Ogre" => HasBlacksmith(),
                "Archer" or "TrollAxeThrower" => true, // No requirement (for now)
                _ => true
            };
        }

        private string GetUnitRequirement(string unitType)
        {
            return unitType switch
            {
                "Footman" or "Grunt" => "Requires Blacksmith",
                "Brigand" or "Ogre" => "Requires Blacksmith",
                _ => ""
            };
        }

        // ===== ICON HELPER METHODS =====

        private string GetUnitIcon(WarUnit unit)
        {
            return unit.PlaceholderName switch
            {
                // Workers
                "Peasant_Human" => "/wc1sprites/icons/Peasant001.png",
                "Peon_Orc" => "/wc1sprites/icons/Peon001.png",

                // Basic Infantry
                "Footman_Human" => "/wc1sprites/icons/Footmantiny001.png",
                "Grunt_Orc" => "/wc1sprites/icons/Grunttiny001.png",

                // Ranged Units
                "Archer_Human" => "/wc1sprites/icons/Archertiny001.png",
                "TrollAxeThrower_Orc" => "/wc1sprites/icons/AxeThrowertiny001.png",

                // Advanced Infantry
                "Brigand_Human" => "/wc1sprites/icons/BriganTiny001.png",
                "Ogre_Orc" => "/wc1sprites/icons/OgredTiny001.png",
                // Advanced Infantry
                "Priest_Human" => "/wc1sprites/icons/PriestIcon001.png",
                "Cultist_Orc" => "/wc1sprites/icons/CultistIcon001.png",
                // Mounts
                "Horse_Human" => "/wc1sprites/icons/NightmareIcon001.png",
                "Wolf_Orc" => "/wc1sprites/icons/HellHoundsIcon001.png",

                // Mounted Units
                "Knight_Human" => "/wc1sprites/icons/KnightIcon001.png",
                "OrcRaider_Orc" => "/wc1sprites/icons/OrcRaiderIcon001.png",
                // Default fallback
                _ => "/wc1sprites/icons/Peasant001.png"
            };
        }

        private string GetBuildingIcon(WarBuilding building)
        {
            return building.PlaceholderName switch
            {
                // Town Halls
                "TownHall_Human" => "/wc1sprites/icons/TownHallTiny001.png",
                "Stronghold_Orc" => "/wc1sprites/icons/OrcSHTiny001.png",

                // Castles
                "Castle_Human" => "/wc1sprites/icons/CastleTiny001.png",
                "Fortress_Orc" => "/wc1sprites/icons/OrcFortressTiny001.png",

                // Barracks
                "Barracks_Human" => "/wc1sprites/icons/Barrackstiny001.png",
                "Barracks_Orc" => "/wc1sprites/icons/OrcBTiny001.png",

                "Church_Human" => "/wc1sprites/icons/ChurchIcon001.png",
                "CultistHut_Orc" => "/wc1sprites/icons/CultistHutIcon001.png",
                // Stables/Pen
                "Stables_Human" => "/wc1sprites/icons/StablesIcon001.png",
                "Pen_Orc" => "/wc1sprites/icons/PensIcon001.png",

                // Knights Hold/Raider Lair
                "KnightsHold_Human" => "/wc1sprites/icons/KnightsHoldIcon001.png",
                "RaiderLair_Orc" => "/wc1sprites/icons/RaiderLairIcon001.png",

                // Farms
                "Farm_Human" => "/wc1sprites/icons/FarmTiny001.png",
                "PigFarm_Orc" => "/wc1sprites/icons/OrcFarmTiny001.png",
                "Road" => "/wc1sprites/buildings/Road001.png", // Temp until you have road icon

                "WoodenWall_Human" => "/wc1sprites/buildings/WoodenWall001.png",

                "WoodenWall_Orc" => "/wc1sprites/buildings/OrcWoodenWall001.png",
                "StoneWall_Human" => "/wc1sprites/buildings/StoneWall001.png",
                "StoneWall_Orc" => "/wc1sprites/buildings/OrcStoneWall001.png",
                // Lumber Mills
                "LumberMill_Human" => "/wc1sprites/icons/LumberHumantiny001.png",
                "LumberMill_Orc" => "/wc1sprites/icons/LumberOrctiny001.png",

                // Archery Ranges
                "ArcheryRange_Human" => "/wc1sprites/icons/Archerytiny001.png",
                "ArcheryRange_Orc" => "/wc1sprites/icons/AxeBuildingtiny001.png",

                // Blacksmiths
                "Blacksmith_Human" => "/wc1sprites/icons/HumanBStiny001.png",
                "Blacksmith_Orc" => "/wc1sprites/icons/OrcBStiny001.png",

                // ✅ NEW: TOWERS
                "WoodTower_Human" => "/wc1sprites/icons/TinyHumanWoodTower002.png",
                "WoodTower_Orc" => "/wc1sprites/icons/TinyOrcWoodTower002.png",
                "StoneTower_Human" => "/wc1sprites/icons/TinyHumanStoneTower001.png",
                "StoneTower_Orc" => "/wc1sprites/icons/TinyOrcStoneTower001.png",
         
                // Default fallback
                _ => "/wc1sprites/icons/TownHallTiny001.png"
            };
        }
        // ===== SELECT SINGLE UNIT FROM MULTI-SELECT =====
        private void SelectSingleUnitFromMulti(WarUnit unit)
        {
            Console.WriteLine($"🖱️ Portrait clicked: {unit.PlaceholderName}");
            WarRegistry.SelectUnit(unit);
            StateHasChanged();
        }
        // ===== STANCE CONTROL METHODS =====
        private void SetUnitStance(WarUnit unit, UnitStance newStance)
        {
            unit.Stance = newStance;

            Console.WriteLine($"🎯 {unit.PlaceholderName} stance changed to: {newStance}");

            // Special behavior for Hold stance - stop moving
            if (newStance == UnitStance.Hold)
            {
                unit.TargetX = null;
                unit.TargetY = null;
                unit.State = UnitState.Idle;
                Console.WriteLine($"🔒 {unit.PlaceholderName} holding position");
            }

            StateHasChanged();
        }

    }
}

