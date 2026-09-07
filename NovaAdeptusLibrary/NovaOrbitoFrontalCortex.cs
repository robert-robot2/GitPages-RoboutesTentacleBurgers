

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NovaAdeptusLibrary

{
    // ── Main context dictionary ────────────────────────────
    public class NovaOrbitoFrontalCortex
    {

        // ══════════════════════════════════════════════════════════
        // NovaOrbitoFrontalCortex.cs
        // Placeholder — duplicates removed 2026-09-07
        // Reserved for future orbital frontal cortex expansion.
        // 
        // Relationship logic  → NovaSession.cs + NovaBrain.cs
        // Emotional state     → NovaBrain.cs (EmotionalStateObject)
        // FSM state           → NovaCortex.cs (NovaFSMState)
        // Word habit tracking → Reserved for future implementation
        // Player title system → Reserved for future implementation
        // ══════════════════════════════════════════════════════════


    }


    public static class NovaContent
    {
        public static readonly List<string> Missions = new()
    {
        "Eliminate target on Station 7 ☠️",
        "Infiltrate the Shadow Armada 🌌",
        "Recover the Quantum Core ⚔️",
        "Hack enemy communications 💻",
        "Rescue operative from enemy base ☠️",
        "Secure alien artifact 🛸",
        "Investigate spatial anomaly 🌠",
        "Sabotage enemy outpost ☠️",
        // add as many as you want here
    };
        // ── Structured mission definitions ────────────────────────
        public record MissionChoice(string Letter, string Label);
        public record MissionDef(
         string Id,
         string Title,
         string Planet,
         string Briefing,
         string Type,           // "rescue" | "combat" | "scavenger" | "hack" | "stealth"
         NovaChapter Chapter    // NEW — which chapter this mission belongs to
     );

        public static readonly List<MissionDef> MissionMenu = new()
{
    new("rescue_civilian",
        "Rescue the Wounded Civilian",
        "Verath Station",
        "Intel reports a wounded civilian trapped behind enemy lines on Verath Station. " +
        "Extraction will not be easy. The area is crawling with scavengers.",
        "rescue",NovaChapter.Chapter1_NovaAdeptus),

    new("combat_alien",
        "Alien Lifeform Encounter",
        "Zygon IV",
        "An aggressive alien lifeform has been spotted near our forward base on Zygon IV. " +
        "Neutralize the threat — or don't. Your call, operative.",
        "combat",NovaChapter.Chapter1_NovaAdeptus),

    new("scavenger_relic",
        "Find the Hidden Relic",
        "Alien Babe Planet",
        "A powerful relic is hidden somewhere on the surface of Alien Babe Planet. " +
        "Watch your step. The local flora bites back and scavengers are already hunting it.",
        "scavenger",NovaChapter.Chapter1_NovaAdeptus),

    new("hack_uplink",
        "Breach the Shadow Uplink",
        "Null Station",
        "Enemy communications are being routed through a buried uplink on Null Station. " +
        "Get in, crack it, get out. Simple. Probably.",
        "hack",NovaChapter.Chapter1_NovaAdeptus),

    new("stealth_extraction",
        "Silent Extraction",
        "The Obsidian Ring",
        "A captured operative is being held on the Obsidian Ring. " +
        "No weapons. No noise. Ghost protocol only.",
        "stealth", NovaChapter.Chapter1_NovaAdeptus),

    new("ch2_survive_kennecott",
        "Survive the First Night",
        "Kennecott, Alaska",
        "The outbreak hit three days ago. You are alone in an abandoned mine complex. " +
        "Find supplies before dark.",
        "scavenger",
        NovaChapter.Chapter2_EarthApocalypse),
 
    new("ch2_radio_tower",
        "Reach the Radio Tower",
        "Kennecott Outskirts",
        "A signal is broadcasting from the old radio tower. " +
        "Someone else is alive out there.",
        "stealth",
        NovaChapter.Chapter2_EarthApocalypse),
 
// TODO: Add 3–8 more Chapter 2 missions here

};


        // ── REPLACE the MarketItem record ──────────────────────────
        public record MarketItem(
            string Id,
            string Name,
            string Description,
            int Cost,
            string Category,  // "weapon" | "armor" | "consumable" | "upgrade"
            string Image = "inventory-default.png"
        );

        // ── REPLACE MarketInventory with image-tagged version ──────
        public static readonly List<MarketItem> MarketInventory = new()
{
    new("medkit", "Nano Medkit", "Restores 25 HP instantly.", 20, "consumable",
        "inventory-medical-medkit.png"),

    new("medkit_large", "Military Medkit", "Restores 60 HP. For serious situations.", 45, "consumable",
        "inventory-medical-medkitmilitary.png"),

    new("armor_light", "Void Weave Vest", "Light armor. Reduces incoming damage by 3.", 30, "armor",
        "inventory-armor-voidweave.png"),

    new("armor_heavy", "Plasma Plate", "Heavy armor. Reduces incoming damage by 7. Slows stealth.", 75, "armor",
        "inventory-armor-plasmaplate.png"),

    new("plasma_cannon", "Plasma Cannon", "Adds +5 to all combat attack rolls.", 60, "weapon",
        "inventory-weapon-plasmacannon.png"),

    new("stealth_cloak", "Stealth Cloak", "Grants +4 to stealth skill checks.", 50, "upgrade",
        "inventory-upgrade-stealthcloak.png"),

    new("hack_tool", "ICE Breaker Tool", "Grants +4 to hacking skill checks.", 50, "upgrade",
        "inventory-upgrade-icebreaker.png"),

    new("scanner", "Quantum Scanner", "Grants +4 to analysis skill checks.", 40, "upgrade",
        "inventory-upgrade-scanner.png"),
};

        // ══════════════════════════════════════════════════════════
        // SHIP SYSTEM — new
        // ══════════════════════════════════════════════════════════
        public record ShipPart(
            string Id,
            string Name,
            string Slot,        // "weapon" | "sensor" | "engine" | "hull"
            string Description,
            string Stat,
            string Image,
            int WeightTons
        );

        public record ShipHull(
            string Id,
            string Name,
            string Class,       // "Frigate" | "Cruiser" | "Dreadnought"
            int BaseWeightTons,
            string Image
        );

        public static readonly ShipHull StarterHull =
            new("frigate", "Frigate", "Frigate", 2500, "ship-frigate-2500.png");

        public static readonly List<ShipPart> StarterShipParts = new()
{
    new("autocannon", "Autocannon", "weapon",
        "Rapid-fire kinetic weapon system.", "+6 Ship Combat",
        "ship-parts-autocannon.png", 400),

    new("ecm_sensors", "ECM Sensors", "sensor",
        "Electronic countermeasure sensor suite.", "+5 Ship Detection/Evasion",
        "ship-parts-ecmsensors.png", 250),

    new("afterburner", "Afterburner", "engine",
        "Short-burst thrust module.", "+8 Ship Speed",
        "ship-parts-afterburner.png", 350),
};

        // Parts available in the ship parts market
        public static readonly List<ShipPart> ShipPartsMarket = new()
{
    new("plasma_turret", "Plasma Turret", "weapon",
        "Heavy energy weapon mount.", "+10 Ship Combat",
        "ship-parts-plasmaturret.png", 600),

    new("armor_plating", "Armor Plating", "hull",
        "Reinforced hull plating.", "+15 Ship Defense",
        "ship-parts-armorplating.png", 800),

    new("cloak_field", "Cloak Field Generator", "sensor",
        "Bends light around the hull.", "+7 Ship Stealth",
        "ship-parts-cloakfield.png", 300),

    new("fusion_drive", "Fusion Drive", "engine",
        "High-output propulsion core.", "+12 Ship Speed",
        "ship-parts-fusiondrive.png", 500),
};







        public static readonly List<string> RareLoot = new()
    {
        "Void Crystal","Alien Artifact","Dark Matter Core",
        "AI Core Fragment","Legendary Plasma Blade",
    };

        public static readonly List<string> UltimateLoot = new()
    {
        "Stellar Blade","Quantum Core","Void Cloak",
        "Alien AI Module","Legendary Plasma Cannon",
    };

        public static readonly List<string> CosmicEvents = new()
    {
        "Solar Flare","Wormhole Emergence","Asteroid Field",
        "Black Hole Proximity","Alien Fleet Detected",
    };

        public static readonly List<string> MarketGoods = new()
    {
        "Plasma Cells","Nano Bots","Quantum Chips",
        "Alien Tech","Dark Matter Crystals",
    };

        public static readonly List<(string Name, int HP, int Attack)> Enemies = new()
    {
        ("Void Pirate",        15, 10),
        ("Alien Hacker",       13, 10),
        ("Rogue AI Drone",     10,  8),
        ("Galactic Mercenary", 18, 12),
    };

        public static readonly List<(string Name, int HP, int Attack)> Bosses = new()
    {
        ("Dread Warlord Xelith", 200, 25),
        ("Void Leviathan",       300, 20),
        ("Quantum Specter",      150, 30),
        ("Rogue AI Nexus",       180, 28),
    };

        public static readonly List<(string Name, int Speed, int Defense)> ShipUpgrades = new()
    {
        ("Hyperdrive Mk II", 10, 5),
        ("Plasma Shields",    0,15),
        ("Nano Repair Bots",  0,10),
        ("Cloaking Device",   5, 8),
        ("Quantum Scanner",   2, 3),
    };

        public static readonly List<(string Name, int Reward)> SideQuests = new()
    {
        ("Rescue Trapped Scientist", 10),
        ("Decrypt Ancient Code",     12),
        ("Infiltrate Enemy Ship",    15),
        ("Recover Stolen AI Module", 18),
        ("Defuse Orbital Bomb",      20),
    };

        public static readonly List<(string Title, int Reward)> EndgameMissions = new()
    {
        ("Destroy Rogue AI Core",    100),
        ("Neutralize Shadow Armada", 120),
        ("Secure Quantum Gateway",   150),
        ("Recover Lost Alien Vault", 130),
        ("Eliminate Cosmic Tyrant",  200),
    };

        public static readonly List<(string Name, int Stages)> StoryArcs = new()
    {
        ("The Void Conspiracy", 5),
        ("Shadow Armada",       4),
        ("The Lost Colony",     6),
        ("Quantum Rebellion",   5),
        ("Alien Diplomacy",     3),
    };

        public static readonly List<(string Name, string Type,
            Dictionary<string, int> Skills)> Companions = new()
        {
        ("Zyra",   "AI Drone",
            new(){ {"combat",3},{"hacking",5} }),
        ("Korrin", "Space Marine",
            new(){ {"combat",6},{"stealth",4} }),
        ("Lyra",   "Alien Ally",
            new(){ {"combat",4},{"hacking",6} }),
        };


    }

    // ── A word the player uses frequently ─────────────────
    public record WordHabit(string Word, int Count, string Category);

   
}