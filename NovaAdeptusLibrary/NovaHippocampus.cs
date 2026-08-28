

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace NovaAdeptusLibrary
{
    // ── Inventory item ────────────────────────────────────────
    public record InventoryItem(string Name, string Rarity, int XpBonus)
    {
        public override string ToString() => $"{Name} [{Rarity}] +{XpBonus} XP";
    }

    // ── Achievement ───────────────────────────────────────────
    public record Achievement(string Id, string Title, string Description, int XpReward)
    {
        public bool Unlocked { get; set; } = false;
        public DateTime? UnlockedAt { get; set; }
    }

    // ── Conversation state machine ────────────────────────────
    public enum ConversationState
    {
        Idle,
        AwaitingMissionConfirm,
        InMission,
        AwaitingMoodChoice,
        AwaitingCompanionChoice,
        InTriviaChallengeAwaitingAnswer
    }

    // ── Player profile ─────────────────────────────────────────
    public class NovaPlayerProfile
    {
        public string OperativeName { get; set; } = "Unknown Operative";
        public int XP { get; set; } = 0;
        public int Level => 1 + XP / 10;
        public int MissionsCompleted { get; set; } = 0;
        public int EnemiesDefeated { get; set; } = 0;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public string CurrentMood { get; set; } = "flirty";
        public ConversationState State { get; set; } = ConversationState.Idle;
        public string? PendingTriviaAnswer { get; set; }

        public List<InventoryItem> Inventory { get; set; } = new();
        public List<Achievement> Achievements { get; set; } = AchievementRegistry.All();
        public Dictionary<string, int> Skills { get; set; } = new()
        {
            { "combat",   0 },
            { "hacking",  0 },
            { "stealth",  0 },
            { "analysis", 0 }
        };

        // Give XP and check for level-up
        public string AwardXP(int amount, string reason = "")
        {
            XP += amount;
            var msg = $"XP +{amount}" + (reason.Length > 0 ? $" ({reason})" : "");
            var unlocked = TryUnlockAchievements();
            return unlocked.Count > 0
                ? msg + " | 🏆 Achievement unlocked: " + string.Join(", ", unlocked.Select(a => a.Title))
                : msg;
        }

        // Check all achievements and unlock any that qualify
        public List<Achievement> TryUnlockAchievements()
        {
            var newly = new List<Achievement>();
            foreach (var ach in Achievements.Where(a => !a.Unlocked))
            {
                if (AchievementRegistry.IsUnlocked(ach.Id, this))
                {
                    ach.Unlocked = true;
                    ach.UnlockedAt = DateTime.UtcNow;
                    XP += ach.XpReward;
                    newly.Add(ach);
                }
            }
            return newly;
        }

        // Add an item to inventory (max 20 slots)
        public string AddItem(InventoryItem item)
        {
            if (Inventory.Count >= 20)
                return "Inventory full! Drop something first ☠️";
            Inventory.Add(item);
            return $"Item acquired: {item} ✅";
        }

       
    }

    // ── Achievement registry ──────────────────────────────────
    public static class AchievementRegistry
    {
        public static List<Achievement> All() => new()
        {
            new("first_blood",   "First Blood",       "Defeat your first enemy",           10),
            new("level5",        "Rising Operative",  "Reach level 5",                      15),
            new("level10",       "Veteran",           "Reach level 10",                     25),
            new("mission10",     "Mission Runner",    "Complete 10 missions",               20),
            new("hacker",        "Ghost in the Net",  "Raise hacking to 10",               15),
            new("stealth_ace",   "Shadow Walk",       "Raise stealth to 10",               15),
            new("collector",     "Hoarder",           "Collect 5 inventory items",         10),
            new("chatty",        "Chatterbox",        "Send 50 messages",                   5),
        };

        public static bool IsUnlocked(string id, NovaPlayerProfile p) => id switch
        {
            "first_blood" => p.EnemiesDefeated >= 1,
            "level5" => p.Level >= 5,
            "level10" => p.Level >= 10,
            "mission10" => p.MissionsCompleted >= 10,
            "hacker" => p.Skills.GetValueOrDefault("hacking") >= 10,
            "stealth_ace" => p.Skills.GetValueOrDefault("stealth") >= 10,
            "collector" => p.Inventory.Count >= 5,
            "chatty" => p.XP >= 50,          // XP is a rough proxy for messages
            _ => false
        };
    }

    // ── Scored intent matcher ─────────────────────────────────
    // Returns a ranked list of (intent, score) for a given input.
    // Score = number of matching keywords / total keywords (0..1).
    public static class SmartResponder
    {
        private static readonly Dictionary<string, (string[] Keywords, Func<NovaPlayerProfile, string> Handler)> Intents = new()
        {
            ["stats"] = (
                new[] { "stats", "status", "level", "xp", "score", "progress", "rank" },
                p => $"⚔️ Operative: {p.OperativeName} | Level {p.Level} | XP {p.XP} | " +
                     $"Missions {p.MissionsCompleted} | Enemies {p.EnemiesDefeated}"
            ),
            ["inventory"] = (
                new[] { "inventory", "items", "loot", "gear", "loadout", "bag" },
                p => p.Inventory.Count == 0
                    ? "Your inventory is empty — go find some loot ☠️"
                    : "🎒 Inventory:\n" + string.Join("\n", p.Inventory.Select((it, i) => $"  {i + 1}. {it}"))
            ),
            ["achievements"] = (
                new[] { "achievements", "badges", "medals", "trophies", "unlocked" },
                p => {
                    var done = p.Achievements.Where(a => a.Unlocked).ToList();
                    if (done.Count == 0) return "No achievements yet — get out there and do something! ☠️";
                    return "🏆 Achievements:\n" + string.Join("\n", done.Select(a => $"  ✅ {a.Title} — {a.Description}"));
                }
            ),
            ["greet"] = (
            new[] { "hey", "sup", "yo", "greetings", "howdy" },
                p => $"Hey, {p.OperativeName}! Level {p.Level} operative detected 😏 " +
                     "Type 'help', 'mission', or 'trivia' to dive in."
            ),
            ["skills"] = (
                new[] { "skills", "abilities", "combat", "hacking", "stealth", "analysis" },
                p => "🎯 Skills:\n" + string.Join("\n",
                     p.Skills.Select(kv => $"  {kv.Key,10}: {"█".PadRight(Math.Min(kv.Value, 20), '█').PadRight(20, '░')} {kv.Value}"))
            ),
            ["help"] = (
                new[] { "help", "how", "commands", "what can", "guide", "menu" },
                p => """
                     🌌 NOVA ADEPTUS — COMMANDS
                     ──────────────────────────
                     stats        → your XP, level, missions
                     inventory    → your gear
                     achievements → unlocked badges
                     skills       → skill levels
                     mission      → grab a mission
                     trivia       → space trivia challenge
                     mood         → change my personality
                     companion    → summon an ally
                     help         → this menu
                     """
            ),
        };

        // Returns best matching response, or null if score < threshold
        public static string? Match(string input, NovaPlayerProfile profile, double threshold = 0.25)
        {
            var cleaned = input.ToLower();
            var best = Intents
                .Select(kv => {
                    double score = kv.Value.Keywords.Count(k => cleaned.Contains(k))
                                   / (double)kv.Value.Keywords.Length;
                    return (intent: kv.Key, score, handler: kv.Value.Handler);
                })
                .Where(x => x.score >= threshold)
                .OrderByDescending(x => x.score)
                .FirstOrDefault();

            return best.handler is not null ? best.handler(profile) : null;
        }
    }

    // ── Loot table ────────────────────────────────────────────
    public static class LootTable
    {
        private static readonly Random _rng = new();
        private static readonly List<InventoryItem> Items = new()
        {
            new("Void Crystal",          "Legendary", 30),
            new("Plasma Blade",          "Rare",      20),
            new("Nano Medkit",           "Common",     5),
            new("Stealth Cloak",         "Rare",      15),
            new("Holo Projector",        "Uncommon",  10),
            new("Quantum Scanner",       "Uncommon",  10),
            new("Dark Matter Core",      "Legendary", 35),
            new("Alien AI Fragment",     "Rare",      22),
            new("Standard Issue Pistol", "Common",     3),
            new("Encrypted Data Chip",   "Uncommon",   8),
        };

        public static InventoryItem Roll()
        {
            // Weighted: common 50%, uncommon 30%, rare 15%, legendary 5%
            int roll = _rng.Next(100);
            IEnumerable<InventoryItem> pool = roll switch
            {
                < 50 => Items.Where(i => i.Rarity == "Common"),
                < 80 => Items.Where(i => i.Rarity == "Uncommon"),
                < 95 => Items.Where(i => i.Rarity == "Rare"),
                _ => Items.Where(i => i.Rarity == "Legendary"),
            };
            var list = pool.ToList();
            return list[_rng.Next(list.Count)];
        }
    }
}