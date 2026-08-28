

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NovaAdeptusLibrary

{
    // ── Relationship levels between Nova and the player ───
    public enum RelationshipLevel
    {
        Neutral = 0,   // just met
        Warming = 1,   // completed a mission or two
        Trusted = 2,   // completed 5+ missions
        Rival = 3,   // player has been hostile
        Respected = 4,   // high XP + many completions
    }
    public enum NovaEmotionalState
    {
        Calm = 0,
        Amused = 1,
        Irritated = 2,
        Intrigued = 3,
        Impressed = 4,
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
            string Type  // "rescue" | "combat" | "scavenger"
        );

        public static readonly List<MissionDef> MissionMenu = new()
{
    new("rescue_civilian",
        "Rescue the Wounded Civilian",
        "Verath Station",
        "Intel reports a wounded civilian trapped behind enemy lines on Verath Station. " +
        "Extraction will not be easy. The area is crawling with scavengers.",
        "rescue"),

    new("combat_alien",
        "Alien Lifeform Encounter",
        "Zygon IV",
        "An aggressive alien lifeform has been spotted near our forward base on Zygon IV. " +
        "Neutralize the threat — or don't. Your call, operative.",
        "combat"),

    new("scavenger_relic",
        "Find the Hidden Relic",
        "Alien Babe Planet",
        "A powerful relic is hidden somewhere on the surface of Alien Babe Planet. " +
        "Watch your step. The local flora bites back and scavengers are already hunting it.",
        "scavenger"),

    new("hack_uplink",
        "Breach the Shadow Uplink",
        "Null Station",
        "Enemy communications are being routed through a buried uplink on Null Station. " +
        "Get in, crack it, get out. Simple. Probably.",
        "hack"),

    new("stealth_extraction",
        "Silent Extraction",
        "The Obsidian Ring",
        "A captured operative is being held on the Obsidian Ring. " +
        "No weapons. No noise. Ghost protocol only.",
        "stealth"),
};
        // ── Market item definitions ────────────────────────────────
        public record MarketItem(
            string Id,
            string Name,
            string Description,
            int Cost,
            string Category  // "weapon" | "armor" | "consumable" | "upgrade"
        );

        public static readonly List<MarketItem> MarketInventory = new()
{
    // ── Consumables ────────────────────────────────────────
    new("medkit",        "Nano Medkit",
        "Restores 25 HP instantly.",
        20, "consumable"),

    new("medkit_large",  "Military Medkit",
        "Restores 60 HP. For serious situations.",
        45, "consumable"),

    // ── Armor ──────────────────────────────────────────────
    new("armor_light",   "Void Weave Vest",
        "Light armor. Reduces incoming damage by 3.",
        30, "armor"),

    new("armor_heavy",   "Plasma Plate",
        "Heavy armor. Reduces incoming damage by 7. Slows stealth.",
        75, "armor"),

    // ── Weapons / Upgrades ─────────────────────────────────
    new("plasma_cannon", "Plasma Cannon",
        "Adds +5 to all combat attack rolls.",
        60, "weapon"),

    new("stealth_cloak", "Stealth Cloak",
        "Grants +4 to stealth skill checks.",
        50, "upgrade"),

    new("hack_tool",     "ICE Breaker Tool",
        "Grants +4 to hacking skill checks.",
        50, "upgrade"),

    new("scanner",       "Quantum Scanner",
        "Grants +4 to analysis skill checks.",
        40, "upgrade"),
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

    // ── Main context dictionary ────────────────────────────
    public class NovaOrbitoFrontalCortex
    {
        public NovaEmotionalState EmotionalState { get; private set; }
    = NovaEmotionalState.Calm;

        public string EmotionalColor => EmotionalState switch
        {
            NovaEmotionalState.Calm => "#4A90D9",
            NovaEmotionalState.Amused => "#48C774",
            NovaEmotionalState.Irritated => "#E53935",
            NovaEmotionalState.Intrigued => "#9B59B6",
            NovaEmotionalState.Impressed => "#F4C542",
            _ => "#4A90D9",
        };

        public void UpdateEmotionalState(string stateStr)
        {
            EmotionalState = stateStr.ToLower() switch
            {
                "amused" => NovaEmotionalState.Amused,
                "irritated" => NovaEmotionalState.Irritated,
                "intrigued" => NovaEmotionalState.Intrigued,
                "impressed" => NovaEmotionalState.Impressed,
                _ => NovaEmotionalState.Calm,
            };
        }
        // ── Relationship ───────────────────────────────────
        public RelationshipLevel Relationship { get; private set; }
            = RelationshipLevel.Neutral;

        // How many times player has been hostile
        public int HostileCount { get; private set; } = 0;

        // How many compliments player has given
        public int ComplimentCount { get; private set; } = 0;

        // ── Word habit tracking ────────────────────────────
        // Tracks words the player uses most — Nova adapts her
        // vocabulary and titles based on these
        private Dictionary<string, int> _wordCounts = new();

        // Word categories Nova watches for
        private static readonly Dictionary<string, string[]> WordCategories = new()
        {
            ["combat"] = new[] { "fight", "combat", "battle", "attack", "kill", "defeat", "warrior", "blade" },
            ["hacking"] = new[] { "hack", "cyber", "breach", "code", "system", "infiltrate", "network" },
            ["stealth"] = new[] { "stealth", "sneak", "ghost", "shadow", "silent", "invisible", "hide" },
            ["strategy"] = new[] { "plan", "strategy", "think", "analyze", "calculate", "assess", "mission" },
            ["casual"] = new[] { "hi", "hello", "hey", "thanks", "please", "okay", "cool", "nice" },
        };

        // ── Session stats ──────────────────────────────────
        public int MessageCount { get; private set; } = 0;
        public int MissionsAccepted { get; private set; } = 0;
        public int MissionsCompleted { get; private set; } = 0;
        public DateTime SessionStart { get; private set; } = DateTime.UtcNow;

        // ── Dominant play style (derived from word habits) ─
        public string DominantStyle => GetDominantStyle();

        // ── Nova's current title for the player ───────────
        // Changes based on relationship and play style
        public string PlayerTitle => GetPlayerTitle();

        // --------------------------------------------------
        // PUBLIC METHODS
        // --------------------------------------------------

        // Call this every time the player sends a message
        public void TrackMessage(string message)
        {
            MessageCount++;
            var words = message.ToLower()
                               .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (_wordCounts.ContainsKey(word))
                    _wordCounts[word]++;
                else
                    _wordCounts[word] = 1;
            }

            // Update relationship based on message count + missions
            UpdateRelationship();
        }

        public void TrackHostile()
        {
            HostileCount++;
            // Enough hostility shifts relationship to Rival
            if (HostileCount >= 3)
                Relationship = RelationshipLevel.Rival;
        }

        public void TrackCompliment()
        {
            ComplimentCount++;
            // Compliments can warm a Rival back to Neutral
            if (Relationship == RelationshipLevel.Rival && ComplimentCount > HostileCount)
                Relationship = RelationshipLevel.Warming;
        }

        public void TrackMissionAccepted() => MissionsAccepted++;
        public void TrackMissionCompleted()
        {
            MissionsCompleted++;
            UpdateRelationship();
        }

        // Get top N words the player uses most
        public List<WordHabit> GetTopWords(int n = 5)
        {
            return _wordCounts
                .OrderByDescending(kv => kv.Value)
                .Take(n)
                .Select(kv => new WordHabit(kv.Key, kv.Value, GetWordCategory(kv.Key)))
                .ToList();
        }


        // --------------------------------------------------
        // PRIVATE HELPERS
        // --------------------------------------------------

        private void UpdateRelationship()
        {
            // Don't override Rival status from hostility
            if (Relationship == RelationshipLevel.Rival) return;

            if (MissionsCompleted >= 10 || MessageCount >= 50)
                Relationship = RelationshipLevel.Respected;
            else if (MissionsCompleted >= 5 || MessageCount >= 20)
                Relationship = RelationshipLevel.Trusted;
            else if (MissionsCompleted >= 1 || MessageCount >= 5)
                Relationship = RelationshipLevel.Warming;
            else
                Relationship = RelationshipLevel.Neutral;
        }

        private string GetDominantStyle()
        {
            var scores = new Dictionary<string, int>();

            foreach (var category in WordCategories)
            {
                int score = category.Value
                    .Sum(w => _wordCounts.GetValueOrDefault(w, 0));
                scores[category.Key] = score;
            }

            var top = scores.OrderByDescending(kv => kv.Value).First();
            return top.Value > 0 ? top.Key : "neutral";
        }

        private string GetPlayerTitle()
        {
            // Title based on relationship + play style
            return (Relationship, DominantStyle) switch
            {
                (RelationshipLevel.Respected, "combat") => "Warlord",
                (RelationshipLevel.Respected, "hacking") => "Ghost",
                (RelationshipLevel.Respected, _) => "Trusted Operative",
                (RelationshipLevel.Trusted, "combat") => "Fighter",
                (RelationshipLevel.Trusted, "hacking") => "Hacker",
                (RelationshipLevel.Trusted, "stealth") => "Shadow",
                (RelationshipLevel.Trusted, _) => "Operative",
                (RelationshipLevel.Rival, _) => "Rival",
                (RelationshipLevel.Warming, _) => "Recruit",
                _ => "Human",
            };
        }

        private string GetWordCategory(string word)
        {
            foreach (var category in WordCategories)
                if (category.Value.Contains(word))
                    return category.Key;
            return "general";
        }
    }
}