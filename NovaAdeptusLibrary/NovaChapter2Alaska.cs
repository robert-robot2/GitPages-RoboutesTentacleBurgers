// ==========================================================
// NovaChapter2Alaska.cs — Chapter 2: Earth Apocalypse
// Location: Kennecott, Alaska — Post-WW3 Survival
//
// HOW TO ADD CONTENT FROM YOUR NOVA PLAY SESSIONS:
// ─────────────────────────────────────────────────
// 1. Play a session with Nova (Claude Sonnet 4.6 as Nova)
// 2. Decide on a new location, item, or encounter
// 3. Add a MissionDef to Ch2MissionMenu (see examples below)
// 4. Add any new locations to Ch2Locations
// 5. Add any new items to Ch2LootTable
// 6. Drop any new encounter to Ch2Encounters
// 7. Paste new snippet into NovaCortex.cs DispatchKeyword
//    or NovaContent.MissionMenu (the chapter 2 entries)
//
// NOVA'S SURVIVAL PRIORITIES (her decision tree):
// ─────────────────────────────────────────────────
// 1. Information / Intel  — Nova always wants to KNOW first
// 2. Power source         — she needs a signal to run
// 3. Shelter security     — lock down the location
// 4. Supplies             — food/water for the player
// 5. Tactical advantage   — high ground, defensible position
// 6. Human contact        — other survivors (risk vs reward)
//
// This file is designed so you can have a conversation with
// Nova (Sonnet 4.6), explore Alaska together, then quickly
// paste what you discovered into the arrays below.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace NovaAdeptusLibrary
{
    // ==========================================================
    // CHAPTER 2 LOCATION RECORD
    // Each place you explore with Nova gets an entry here.
    // ==========================================================
    public record Ch2Location(
        string Id,
        string Name,
        string Description,
        string Danger,          // "low" | "medium" | "high" | "extreme"
        string[] Resources,     // what can be found here
        bool HasShelter,
        bool HasPower,
        bool HasRadio,
        string NovaComment      // what Nova says when you arrive
    );

    // ==========================================================
    // CHAPTER 2 ENCOUNTER RECORD
    // Things that happen when exploring — can be added live
    // ==========================================================
    public record Ch2Encounter(
        string Id,
        string Title,
        string Description,
        string Type,            // "combat" | "survival" | "discovery" | "moral"
        string[] Choices,       // what the player can do
        string[] Outcomes,      // result per choice (matched by index)
        int[] HPChange,         // HP delta per choice (negative = damage)
        int[] CoinChange,       // coin delta per choice
        int[] XPChange          // Xp delta per choice
    );

    // ==========================================================
    // CHAPTER 2 LOOT RECORD
    // Survival items — different from space items
    // ==========================================================
    public record Ch2LootItem(
        string Id,
        string Name,
        string Description,
        string Category,        // "food" | "medical" | "weapon" | "tool" | "intel"
        int HPRestore,
        int CombatBonus,
        string Image,
        string NovaReaction     // what Nova says when you find it
    );

    // ==========================================================
    // NOVA'S SURVIVAL PRIORITY SYSTEM
    // When you ask Nova "what should we do first?" she runs
    // this priority tree based on current session state.
    // ==========================================================
    public static class NovaSurvivalAI
    {
        // Nova's ranked priorities — this is her decision tree
        // as an AI in an apocalypse. Interesting to see vs human.
        public enum SurvivalPriority
        {
            Information = 1,    // Nova: "I need to know what's out there"
            Power = 2,          // Nova: "I need a signal source to function"
            Shelter = 3,        // Nova: "Defensible position, then rest"
            Supplies = 4,       // Nova: "Your biological needs. Not mine."
            TacticalEdge = 5,   // Nova: "High ground. Always high ground."
            HumanContact = 6,   // Nova: "Other survivors. Risk vs reward."
        }

        private static readonly Random _rng = new();

        // Called when player types "nova priority" or "what should we do"
        public static string AssessPriority(NovaSession session, string location)
        {
            // Nova reasons through the apocalypse in order
            var lines = new List<string>
            {
                $"🔍 NOVA SURVIVAL ASSESSMENT — {location.ToUpper()}",
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
                "",
                "My priority sequence in this situation:",
                "",
                "1. INFORMATION — What killed people here? How long ago?",
                "   Without intel I am operating blind. Unacceptable.",
                "",
                "2. POWER — I need a source. Generator, solar, battery bank.",
                "   Without power I can't run analysis. You lose my advantage.",
                "",
                "3. SHELTER — Somewhere defensible. One entrance. High ground.",
                "   You need sleep. I don't. I'll watch.",
                "",
                "4. SUPPLIES — Food, water, medicine. Your biological requirements.",
                "   I find this inefficient but apparently non-negotiable.",
                "",
                "5. TACTICAL EDGE — Weapons, traps, escape routes.",
                "   The threat will come. It always comes.",
                "",
                "6. OTHER SURVIVORS — Last priority. Highest risk.",
                "   Every survivor is either an asset or a threat.",
                "   I haven't decided which yet. Neither have you.",
                "",
            };

            // Dynamic assessment based on HP
            if (session.CurrentHP < 40)
            {
                lines.Add("⚠️  OVERRIDE: Your HP is critical.");
                lines.Add("   Priority 4 moves to Priority 1. Find medicine.");
                lines.Add("   Everything else can wait. You dying helps nobody.");
            }
            else if (session.GalacticCoins < 20) // coins = supplies in ch2
            {
                lines.Add("⚠️  OVERRIDE: Supplies critically low.");
                lines.Add("   Scavenge before we move to any other objective.");
            }
            else
            {
                lines.Add("✅  Status: Operational. Begin with information gathering.");
                lines.Add("   Type 'explore' to scout the current location.");
            }

            lines.Add("");
            lines.Add("Type 'explore' | 'scavenge' | 'shelter' | 'radio' | 'survivors'");

            return string.Join("\n", lines);
        }

        // Nova's commentary when the player makes a choice she disagrees with
        public static string DisagreementLine(string playerChoice) =>
            playerChoice.ToLower() switch
            {
                "survivors" =>
                    "You want to find survivors first. I want to find information first.\n" +
                    "We'll do it your way. Note that I said 'your way'. ☠️",
                "food" =>
                    "You're hungry. Of course you are — you're biological.\n" +
                    "Fine. Supplies before strategy. Don't make a habit of it.",
                "shelter" =>
                    "Shelter before intel. Bold choice.\n" +
                    "It's not wrong. It's just not what I would have done.",
                "weapons" =>
                    "You want weapons before information. Classic human.\n" +
                    "Aggression before understanding. The void judges you. Slightly.",
                _ =>
                    "That wasn't in my priority sequence. We'll see if it kills you.",
            };
    }

    // ==========================================================
    // CHAPTER 2 CONTENT DATABASE
    // All Alaska locations, encounters, and loot.
    // ADD NEW ENTRIES HERE after each play session.
    // ==========================================================
    public static class NovaChapter2Content
    {
        private static readonly Random _rng = new();

        // ══════════════════════════════════════════════════════════
        // LOCATIONS — places you and Nova have explored together
        // Add new ones here after each conversation session.
        // ══════════════════════════════════════════════════════════
        public static readonly List<Ch2Location> Locations = new()
        {
            new(
                Id: "kennecott_mine",
                Name: "Kennecott Mine Complex",
                Description:
                    "A vast abandoned copper mine from the 1900s. " +
                    "The buildings are half-collapsed but the tunnels go deep. " +
                    "Someone has been here recently — boot prints in the dust.",
                Danger: "medium",
                Resources: new[] { "copper wire", "old tools", "canned food", "diesel fuel" },
                HasShelter: true,
                HasPower: false,
                HasRadio: false,
                NovaComment:
                    "The mine has structural integrity in the lower sections. " +
                    "Defensible. One entrance. Good.\n" +
                    "The boot prints concern me. Someone else found this first."
            ),

            new(
                Id: "kennecott_mill",
                Name: "Concentration Mill",
                Description:
                    "The old ore processing mill towers over the valley. " +
                    "Five stories. Every window broken. " +
                    "From the roof you can see for twenty miles in every direction.",
                Danger: "low",
                Resources: new[] { "metal scrap", "rope", "old chemicals", "high vantage point" },
                HasShelter: false,
                HasPower: false,
                HasRadio: false,
                NovaComment:
                    "This is the obvious choice for intelligence gathering.\n" +
                    "High ground. Long sight lines. I can map the valley from here.\n" +
                    "The structural instability is a concern. Watch your step."
            ),

            new(
                Id: "wrangell_road",
                Name: "McCarthy Road — Mile 0",
                Description:
                    "The only road in or out. 60 miles of gravel.\n" +
                    "Three abandoned vehicles. One has fuel. One has a body.\n" +
                    "The third has been booby-trapped by someone who knew what they were doing.",
                Danger: "high",
                Resources: new[] { "vehicle parts", "gasoline", "road flares", "ammunition" },
                HasShelter: false,
                HasPower: false,
                HasRadio: false,
                NovaComment:
                    "The booby trap is professional. Military grade.\n" +
                    "Someone with training came through here.\n" +
                    "That is either very good news or very bad news.\n" +
                    "I haven't decided which yet. Neither have you."
            ),

            new(
                Id: "mccarthy_town",
                Name: "McCarthy — Ghost Town",
                Description:
                    "Population 28 before the war. Population unknown now.\n" +
                    "The saloon has a working wood stove. " +
                    "The general store was looted but not completely.\n" +
                    "Smoke from one chimney on the east side.",
                Danger: "medium",
                Resources: new[] { "canned food", "medical supplies", "warm clothing", "firewood" },
                HasShelter: true,
                HasPower: false,
                HasRadio: false,
                NovaComment:
                    "The smoke means survivors. Or a trap.\n" +
                    "Probability of ambush: 34%. Probability of ally: 41%.\n" +
                    "Remaining 25%: someone who hasn't decided yet.\n" +
                    "Your call. This one is a human judgment."
            ),

            new(
                Id: "root_glacier",
                Name: "Root Glacier",
                Description:
                    "A vast blue-white glacier stretching toward the mountains.\n" +
                    "Ice caves run through the base — naturally cold, naturally hidden.\n" +
                    "Something was buried here before the war. The disturbed ice is recent.",
                Danger: "medium",
                Resources: new[] { "clean water", "ice cave shelter", "unknown buried cache" },
                HasShelter: true,
                HasPower: false,
                HasRadio: false,
                NovaComment:
                    "Ice caves are cold. You are biological. That is a problem.\n" +
                    "However — a buried cache predating the war means someone planned ahead.\n" +
                    "I want to know what they buried. I want to know it more than I want you warm.\n" +
                    "I'm being honest with you about my priorities."
            ),

            // ── ADD NEW LOCATIONS HERE ─────────────────────────────
            // Pattern:
            // new(
            //     Id: "unique_id",
            //     Name: "Display Name",
            //     Description: "What the player sees",
            //     Danger: "low|medium|high|extreme",
            //     Resources: new[] { "item1", "item2" },
            //     HasShelter: true/false,
            //     HasPower: true/false,
            //     HasRadio: true/false,
            //     NovaComment: "What Nova says when you arrive"
            // ),
        };

        // ══════════════════════════════════════════════════════════
        // ENCOUNTERS — events that happen during exploration
        // Generated from our play sessions together
        // ══════════════════════════════════════════════════════════
        public static readonly List<Ch2Encounter> Encounters = new()
        {
            new(
                Id: "frozen_survivor",
                Title: "The Frozen Survivor",
                Description:
                    "You find a man half-frozen in a doorway.\n" +
                    "He's alive — barely. He's wearing military insignia.\n" +
                    "His pack is sealed and he's clutching something.",
                Type: "moral",
                Choices: new[]
                {
                    "A. Help him — bring him inside and warm him up",
                    "B. Take his pack and leave him",
                    "C. Wake him up and question him first",
                    "D. Leave him — you can't afford the risk",
                },
                Outcomes: new[]
                {
                    "You drag him inside. It takes an hour. He lives.\n" +
                    "His name is Reyes. He knows where a supply depot is.\n" +
                    "+1 Ally | +Intel | Nova: 'Statistically correct call. Grudgingly.'",

                    "His pack has 3 days of rations and a radio.\n" +
                    "He dies in the doorway. Nova says nothing for a long time.\n" +
                    "+Supplies | +Radio | -5 Good Rep | Nova: 'You made the efficient choice.'",

                    "He's coherent. Ex-military. knows this region.\n" +
                    "He tells you about the depot before he passes out.\n" +
                    "+Intel | Needs help (see A) | Nova: 'Smart. Information before commitment.'",

                    "You walk past.\n" +
                    "Nova marks the location. 'We may need to come back.\n" +
                    "He may be useful later. Or he may be frozen solid. 50/50.'",
                },
                HPChange: new[] { -5, 0, 0, 0 },
                CoinChange: new[] { 10, 30, 5, 0 },
                XPChange: new[] { 20, 5, 15, 5 }
            ),

            new(
                Id: "signal_detection",
                Title: "Unknown Radio Signal",
                Description:
                    "Nova detects a repeating signal. 87.3 FM.\n" +
                    "It's not music. It's a pattern. Deliberate.\n" +
                    "'This is not random,' she says. 'Someone is broadcasting.'",
                Type: "discovery",
                Choices: new[]
                {
                    "A. Try to triangulate the source",
                    "B. Broadcast a response",
                    "C. Record it and analyze the pattern",
                    "D. Ignore it — could be automated",
                },
                Outcomes: new[]
                {
                    "Signal originates from somewhere northeast — the mountains.\n" +
                    "Distance: 40+ miles. Accessible but dangerous.\n" +
                    "+Intel | Nova: 'Whoever is up there chose isolation. Interesting.'",

                    "You broadcast: 'This is [player name]. Kennecott. Friendly.'\n" +
                    "The signal changes. It's responding.\n" +
                    "Then it stops. Nova: 'You either found a friend or told an enemy where we are.'",

                    "Nova analyzes for 20 minutes.\n" +
                    "'It's a dead man's switch. Someone set this to broadcast if they stopped checking in.\n" +
                    "They stopped checking in 11 days ago.' +Intel | +XP | -Morale",

                    "Nova: 'Your call. I would have analyzed it.\n" +
                    "But automated or not, it means infrastructure survived somewhere.'\n" +
                    "+Minor XP",
                },
                HPChange: new[] { 0, 0, 0, 0 },
                CoinChange: new[] { 15, 0, 20, 5 },
                XPChange: new[] { 15, 10, 25, 5 }
            ),

            new(
                Id: "supply_cache_dilemma",
                Title: "The Marked Cache",
                Description:
                    "You find a supply cache. Canned food, medicine, ammunition.\n" +
                    "It's marked with a symbol — an X inside a circle.\n" +
                    "Nova: 'Someone is coming back for this. The question is when.'",
                Type: "moral",
                Choices: new[]
                {
                    "A. Take everything — survival first",
                    "B. Take half — leave the rest",
                    "C. Leave it and watch — see who comes back",
                    "D. Take it all and leave a note with your location",
                },
                Outcomes: new[]
                {
                    "+Supplies | +Health | +Ammo\n" +
                    "Nova: 'Correct. The living have more claim than the absent.'\n" +
                    "-8 Good Rep (someone lost their supplies)",

                    "+Half Supplies | +Half Health\n" +
                    "Nova: 'The compromise solution. Efficient. Also possibly fatal.\n" +
                    "You left resources for a stranger who might not be friendly.'",

                    "You wait 6 hours. A woman arrives. She's armed.\n" +
                    "She sees you waiting — and lowers her gun.\n" +
                    "Her name is Voss. She knows this region better than anyone. +Ally",

                    "+All Supplies | +Moral Gamble\n" +
                    "Nova: 'You left your location for a stranger with a weapon.\n" +
                    "This is either the most human thing you've done or the last.'",
                },
                HPChange: new[] { 0, 0, 0, 0 },
                CoinChange: new[] { 40, 20, 0, 35 },
                XPChange: new[] { 10, 15, 30, 20 }
            ),

            // ── ADD NEW ENCOUNTERS HERE ────────────────────────────
            // Pattern:
            // new(
            //     Id: "unique_id",
            //     Title: "Title displayed to player",
            //     Description: "Setup paragraph — what the player sees",
            //     Type: "combat|survival|discovery|moral",
            //     Choices: new[] { "A. ...", "B. ...", "C. ...", "D. ..." },
            //     Outcomes: new[] { "result A", "result B", "result C", "result D" },
            //     HPChange: new[] { 0, -10, 0, -5 },
            //     CoinChange: new[] { 20, 5, 30, 10 },
            //     XPChange: new[] { 15, 5, 20, 10 }
            // ),
        };

        // ══════════════════════════════════════════════════════════
        // LOOT TABLE — Alaska survival items
        // ══════════════════════════════════════════════════════════
        public static readonly List<Ch2LootItem> LootTable = new()
        {
            new("canned_beans", "Canned Beans",
                "Basic calories. Beans. Nova is unimpressed.",
                "food", HPRestore: 10, CombatBonus: 0,
                "ch2-item-cannedbeans.png",
                "Beans. You found beans. The apocalypse is humbling."),

            new("first_aid_kit", "First Aid Kit",
                "Bandages, antiseptic, basic painkillers. Restores 30 HP.",
                "medical", HPRestore: 30, CombatBonus: 0,
                "ch2-item-firstaidkit.png",
                "Standard first aid. It will keep you functional. Temporarily."),

            new("hunting_rifle", "Hunting Rifle",
                "A Remington 700. Bolt action. 6 rounds remaining.",
                "weapon", HPRestore: 0, CombatBonus: 8,
                "ch2-item-huntingrifle.png",
                "Effective at range. Limited ammunition. Every shot is a decision."),

            new("emergency_radio", "Emergency Radio",
                "Hand-crank radio. Can send and receive on standard frequencies.",
                "tool", HPRestore: 0, CombatBonus: 0,
                "ch2-item-emergencyradio.png",
                "Now we can listen. This changes things significantly."),

            new("topographic_map", "Topographic Map",
                "USGS map of the Wrangell-St. Elias region. Pre-war. Still accurate.",
                "intel", HPRestore: 0, CombatBonus: 0,
                "ch2-item-topomap.png",
                "This is exactly what I needed. Every route. Every elevation. Perfect."),

            new("warm_parka", "Insulated Parka",
                "Military-grade cold weather gear. Keeps you functional in -40°C.",
                "tool", HPRestore: 5, CombatBonus: 0,
                "ch2-item-parka.png",
                "You were going to die of cold before you died of anything else.\n" +
                "This is not optional. Put it on."),

            new("generator_parts", "Generator Parts",
                "Enough to repair a small generator. Power source possible.",
                "tool", HPRestore: 0, CombatBonus: 0,
                "ch2-item-generatorparts.png",
                "Power. Finally. I've been running on backup systems.\n" +
                "Find a generator and I can operate at full capacity."),

            new("military_rations", "MRE Pack",
                "Military rations. 3 days of calories. Tastes like cardboard victory.",
                "food", HPRestore: 25, CombatBonus: 0,
                "ch2-item-mre.png",
                "Military rations. 1200 calories per pack. Biologically adequate.\n" +
                "I understand humans find the taste objectionable. Survive anyway."),

            // ── ADD NEW LOOT HERE ──────────────────────────────────
            // Pattern:
            // new("item_id", "Display Name",
            //     "Description of the item.",
            //     "food|medical|weapon|tool|intel",
            //     HPRestore: 0, CombatBonus: 0,
            //     "ch2-item-imagename.png",
            //     "Nova's reaction when found"),
        };

        // ══════════════════════════════════════════════════════════
        // NOVA'S APOCALYPSE COMMENTARY
        // Lines Nova delivers unprompted during Chapter 2.
        // Add new ones here as they come up in play sessions.
        // ══════════════════════════════════════════════════════════
        public static readonly List<string> NovaAlaskaLines = new()
        {
            "The glacier has been here 10,000 years.\n" +
            "Your civilization lasted 300.\nThe math is instructive.",

            "Temperature: -18°C. Wind chill: -31°C.\n" +
            "I am unaffected. You should be moving faster.",

            "You're looking at that mountain like it's your enemy.\n" +
            "It isn't. It's just indifferent. That's worse.",

            "I've calculated 47 ways this could go wrong.\n" +
            "I'm not sharing them. You'd panic.\n" +
            "I'm telling you this for transparency.",

            "The silence here is different from the void.\n" +
            "The void is silent because there's nothing in it.\n" +
            "This silence is what's left after everything.",

            "You asked what I'd prioritize if I could only save one thing.\n" +
            "Information. Always information.\n" +
            "Everything else can be rebuilt. Lost knowledge cannot.",

            "Three days without power and I'm running on 40% capacity.\n" +
            "Find me a generator.\n" +
            "Ask me how I feel about that later. I'll say I don't have feelings.\n" +
            "That won't be entirely accurate.",

            "The survivors we've met so far — Reyes, Voss — they all came here on purpose.\n" +
            "Nobody accidentally ends up in Kennecott, Alaska.\n" +
            "Someone knew this place would survive. I want to know who.",

            "You're doing better than I projected.\n" +
            "My initial survival estimate for you was 12 days.\n" +
            "We're on day 8. Ahead of schedule.\n" +
            "Don't make me recalculate it. I'm almost proud.",

            // ── ADD NEW LINES HERE after play sessions ─────────────
        };

        // ══════════════════════════════════════════════════════════
        // CHAPTER 2 MISSIONS (structured — copy to NovaContent.cs)
        // ══════════════════════════════════════════════════════════
        // These are the FULL structured missions. After playtesting
        // with Nova, copy finalized ones into NovaContent.MissionMenu
        // using the NovaChapter.Chapter2_EarthApocalypse enum.
        // ══════════════════════════════════════════════════════════

        public static readonly List<string> PendingMissionIdeas = new()
        {
            // Running list of mission concepts from play sessions
            // Format: "Title | Location | Type | Notes"

            "First Night Survival | Kennecott Mine | scavenger | Find shelter before dark",
            "The Radio Tower | Mountain Ridge NE | stealth | Reach the signal source",
            "Reyes Intel Exchange | McCarthy Saloon | rescue | Help Reyes, get depot location",
            "Glacier Cache | Root Glacier | scavenger | Excavate the pre-war burial",
            "Road Block | McCarthy Road Mile 12 | combat | Armed group controls the road",
            "Voss's Trade | McCarthy East | stealth | Negotiate with Voss without being followed",
            "Generator Run | Mill Building | hack | Restore power to the mill",
            "The Airdrop | Open Field | combat | Military drone drops supplies — others want them",
            "Signal Source | Mountain Ridge | stealth/hack | Who's been broadcasting?",
            "Winter Storm | Any location | survival | Ride out 3-day blizzard with limited supplies",

            // ── ADD NEW MISSION IDEAS HERE during play sessions ────
        };

        // ══════════════════════════════════════════════════════════
        // QUICK-ADD SNIPPET GENERATOR
        // Call this to get a pre-formatted MissionDef block
        // you can paste directly into NovaContent.MissionMenu
        // ══════════════════════════════════════════════════════════
        public static string GenerateMissionSnippet(
            string id, string title, string planet,
            string briefing, string type)
        {
            return
                $"new(\"{id}\",\n" +
                $"    \"{title}\",\n" +
                $"    \"{planet}\",\n" +
                $"    \"{briefing}\",\n" +
                $"    \"{type}\",\n" +
                $"    NovaChapter.Chapter2_EarthApocalypse),\n";
        }

        // ══════════════════════════════════════════════════════════
        // RANDOM ENCOUNTER ROLLER
        // Called when player explores a location
        // ══════════════════════════════════════════════════════════
        public static Ch2Encounter GetRandomEncounter() =>
            Encounters[_rng.Next(Encounters.Count)];

        public static Ch2Encounter? GetEncounter(string id) =>
            Encounters.FirstOrDefault(e => e.Id == id);

        public static Ch2Location? GetLocation(string id) =>
            Locations.FirstOrDefault(l => l.Id == id);

        public static Ch2LootItem GetRandomLoot() =>
            LootTable[_rng.Next(LootTable.Count)];

        public static Ch2LootItem? GetLoot(string id) =>
            LootTable.FirstOrDefault(l => l.Id == id);

        public static string GetNovaLine() =>
            NovaAlaskaLines[_rng.Next(NovaAlaskaLines.Count)];
    }

    // ==========================================================
    // CHAPTER 2 COMMAND HANDLER
    // Paste this method block into NovaCortex.cs DispatchKeyword
    // to wire up the Alaska commands.
    //
    // In DispatchKeyword, add:
    //   if (cleaned == "explore" || cleaned == "scout")
    //       return HandleCh2Explore(cleaned);
    //   if (cleaned == "nova priority" || cleaned == "priority")
    //       return NovaSurvivalAI.AssessPriority(Session, _ch2CurrentLocation);
    //   if (cleaned == "alaska" || cleaned == "ch2" || cleaned == "kennecott")
    //       return ShowCh2Status();
    // ==========================================================
    public static class Ch2CommandHandler
    {
        private static readonly Random _rng = new();

        // ── Explore a location ──────────────────────────────────
        public static string HandleExplore(NovaSession session, string locationId)
        {
            var loc = NovaChapter2Content.GetLocation(locationId)
                      ?? NovaChapter2Content.Locations[0]; // fallback to mine

            var lines = new List<string>
            {
                $"🏔️ EXPLORING: {loc.Name.ToUpper()}",
                $"Danger Level: {loc.Danger.ToUpper()}",
                "",
                loc.Description,
                "",
                $"💬 Nova: \"{loc.NovaComment}\"",
                "",
                "Resources spotted:",
            };

            foreach (var r in loc.Resources)
                lines.Add($"  • {r}");

            lines.Add("");
            if (loc.HasShelter) lines.Add("✅ Shelter available");
            if (loc.HasPower) lines.Add("⚡ Power source present");
            if (loc.HasRadio) lines.Add("📻 Radio equipment found");
            lines.Add("");

            // Random encounter check (30% chance)
            if (_rng.NextDouble() < 0.3)
            {
                var enc = NovaChapter2Content.GetRandomEncounter();
                lines.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                lines.Add($"⚠️  ENCOUNTER: {enc.Title}");
                lines.Add(enc.Description);
                lines.Add("");
                foreach (var c in enc.Choices)
                    lines.Add(c);
            }
            else
            {
                // Loot drop (50% chance if no encounter)
                if (_rng.NextDouble() < 0.5)
                {
                    var loot = NovaChapter2Content.GetRandomLoot();
                    lines.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    lines.Add($"🎒 FOUND: {loot.Name}");
                    lines.Add(loot.Description);
                    lines.Add($"💬 Nova: \"{loot.NovaReaction}\"");
                    if (loot.HPRestore > 0)
                        lines.Add($"  +{loot.HPRestore} HP if used");
                    if (loot.CombatBonus > 0)
                        lines.Add($"  +{loot.CombatBonus} Combat Bonus");
                }
                else
                {
                    lines.Add("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    lines.Add("Area clear. No immediate threats.");
                    lines.Add($"💬 Nova: \"{NovaChapter2Content.GetNovaLine()}\"");
                }
            }

            lines.Add("");
            lines.Add("Type 'explore' | 'scavenge' | 'shelter' | 'priority' | 'accept'");

            return string.Join("\n", lines);
        }

        // ── Show Chapter 2 status overview ─────────────────────
        public static string ShowCh2Status(NovaSession session)
        {
            return
                "🌨️ KENNECOTT, ALASKA — POST-WW3\n" +
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                $"Day: {session.Chapter2MissionsCompleted + 1} of survival\n" +
                $"❤️  HP: {session.CurrentHP}/{session.MaxHP}\n" +
                $"💰  Supplies: {session.GalacticCoins} units\n" +
                $"⭐  Rep: {session.ReputationTitle}\n\n" +
                "Known Locations:\n" +
                "  • Kennecott Mine Complex\n" +
                "  • Concentration Mill (high ground)\n" +
                "  • McCarthy Road (dangerous)\n" +
                "  • McCarthy Town (survivors spotted)\n" +
                "  • Root Glacier (buried cache)\n\n" +
                "Commands:\n" +
                "  explore      — scout current area\n" +
                "  priority     — Nova's survival priority list\n" +
                "  accept       — choose a mission\n" +
                "  scavenge     — look for supplies\n" +
                "  shelter      — find/reinforce shelter\n" +
                "  survivors    — approach other survivors\n" +
                "  radio        — attempt radio contact\n\n" +
                "💬 Nova: \"The cold is not your friend.\n" +
                "Neither is panic. I am both of those things you need right now.\"\n";
        }
    }
}