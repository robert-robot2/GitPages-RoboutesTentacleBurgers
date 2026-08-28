

// ==========================================================
// NovaCortex.cs — Nova Adeptus Main Controller
// The primary dispatch and decision layer.
// Razor talks exclusively to this file.
// Coordinates NovaBrain.cs (intelligence) and
// NovaThalamus.cs (language/personality).
//
// RESPONSIBILITIES:
//   1. Single entry point: Respond(string input)
//   2. FSM mini-game state machine
//   3. Game command dispatch
//   4. Session memory + profile management
//   5. JS interop for localStorage persistence
//   6. Emotional state color sync for UI dot
// ==========================================================

using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace NovaAdeptusLibrary
{
    // ==========================================================
    // FSM STATES
    // ==========================================================
    public enum NovaFSMState
    {
        Idle,
        AwaitHack,
        AwaitDualHack1,
        AwaitDualHack2,
        AwaitPuzzle,
        AwaitAnalysis,
        AwaitMood,
        AwaitMini,
        AwaitName,
        AwaitTrivia,

        // ── Mission system ──────────────────────────────
        AwaitMissionChoice,      // player seeing mission menu
        AwaitMissionAction,      // player inside mission, main branch
        AwaitMissionSubAction,   // player inside sub-branch
        AwaitGameOver,           // HP = 0, awaiting respawn choice

        // ── Market + Inventory ──────────────────────────
        AwaitMarketChoice,       // player seeing market menu
        AwaitInventoryAction,    // player using an item
    }

    // ==========================================================
    // NOVA SESSION — runtime state for one play session
    // ==========================================================
    public class NovaSession
    {
        public string? UserName { get; set; }
        public int XP { get; set; } = 0;
        public int Level => 1 + XP / 10;
        public int MissionsCompleted { get; set; } = 0;
        public int EnemiesDefeated { get; set; } = 0;
        public string Relationship { get; set; } = "neutral";
        public string PlayerTitle { get; set; } = "operative";
        public int MessageCount { get; set; } = 0;
        public int HostileCount { get; set; } = 0;

        // ── HP System ─────────────────────────────────────
        public int MaxHP { get; set; } = 100;
        public int CurrentHP { get; set; } = 100;
        public int Armor { get; set; } = 0;

        // ── Equipped bonuses (applied from market items) ───────────
        public int CombatBonus { get; set; } = 0;
        public int StealthBonus { get; set; } = 0;
        public int HackingBonus { get; set; } = 0;
        public int AnalysisBonus { get; set; } = 0;

        // ── Effective skill helpers (base + equipment bonus) ───────
        public int EffectiveCombat => Skills.GetValueOrDefault("combat") + CombatBonus;
        public int EffectiveStealth => Skills.GetValueOrDefault("stealth") + StealthBonus;
        public int EffectiveHacking => Skills.GetValueOrDefault("hacking") + HackingBonus;
        public int EffectiveAnalysis => Skills.GetValueOrDefault("analysis") + AnalysisBonus;
        public bool IsAlive => CurrentHP > 0;

        // ── Currency ───────────────────────────────────────
        public int GalacticCoins { get; set; } = 50; // start with 50

        // ── Reputation ─────────────────────────────────────
        public int GoodRep { get; set; } = 0;
        public int BadRep { get; set; } = 0;
        public string ReputationTitle => GetReputationTitle();

        private string GetReputationTitle()
        {
            int net = GoodRep - BadRep;
            return net switch
            {
                >= 20 => "Hero of the Void",
                >= 10 => "Trusted Protector",
                >= 5 => "Rising Operative",
                >= 0 => "Neutral",
                >= -5 => "Mercenary",
                >= -10 => "Notorious",
                _ => "Most Wanted",
            };
        }

        // ── HP helpers ─────────────────────────────────────
        public int TakeDamage(int incoming)
        {
            int actual = Math.Max(1, incoming - Armor);
            CurrentHP = Math.Max(0, CurrentHP - actual);
            return actual; // returns actual damage dealt after armor
        }

        public int Heal(int amount)
        {
            int before = CurrentHP;
            CurrentHP = Math.Min(MaxHP, CurrentHP + amount);
            return CurrentHP - before; // returns actual HP restored
        }
        public int ComplimentCount { get; set; } = 0;

        public Dictionary<string, int> Skills { get; set; } = new()
        {
            { "combat",   0 },
            { "hacking",  0 },
            { "stealth",  0 },
            { "analysis", 0 },
        };

        public List<string> ActiveMissions { get; set; } = new();
        public List<string> CompletedMissions { get; set; } = new();
        public List<string> Inventory { get; set; } = new();

        // FSM runtime — not persisted
        public NovaFSMState FSMState { get; set; } = NovaFSMState.Idle;
        public Dictionary<string, object> FSMContext { get; set; } = new();

        public void UpdateRelationship()
        {
            if (Relationship == "rival") return;

            if (MissionsCompleted >= 10 || MessageCount >= 50)
                Relationship = "respected";
            else if (MissionsCompleted >= 5 || MessageCount >= 20)
                Relationship = "trusted";
            else if (MissionsCompleted >= 1 || MessageCount >= 5)
                Relationship = "warming";
            else
                Relationship = "neutral";
        }

        public void TrackTone(string input)
        {
            var lower = input.ToLower();
            var hostile = new[] { "hate","stupid","idiot","dumb",
                                  "useless","suck","broken","terrible",
                                  "worst","shut up" };
            var friendly = new[] { "thank","love","amazing","great",
                                   "awesome","cool","nice","appreciate" };

            if (hostile.Any(h => lower.Contains(h)))
            {
                HostileCount++;
                if (HostileCount >= 3) Relationship = "rival";
            }
            if (friendly.Any(f => lower.Contains(f)))
            {
                ComplimentCount++;
                if (Relationship == "rival" && ComplimentCount > HostileCount)
                    Relationship = "warming";
            }
        }
    }

    // ==========================================================
    // NOVA CORTEX — MAIN CONTROLLER
    // ==========================================================
    public class NovaCortex
    {
        // ── Dependencies ───────────────────────────────────────
        private readonly NovaBrain _brain;
        private readonly NovaThalamus _thalamus;
        private readonly NovaAPIService _api;
        private readonly NovaWernicke _wernicke = new();
        private NovaBroca _broca = default!;
        private readonly IJSRuntime _js;

        // ── Session state ──────────────────────────────────────
        public NovaSession Session { get; private set; } = new();
        private static readonly Random _rng = new();

        // ── Trivia state ───────────────────────────────────────
        private List<TriviaQuestion> _triviaCache = new();
        private TriviaQuestion? _activeTrivia = null;

        // ── Active mission tracking ────────────────────────────────
        private NovaContent.MissionDef? _activeMission = null;
        private string _missionStage = "";    // tracks where in the branch we are
        private int _missionEnemyHP = 0;  // enemy HP for combat encounters
        // ==========================================================
        // CONSTRUCTOR
        // ==========================================================

        public NovaCortex(IJSRuntime js, HttpClient http)
        {
            _js = js;
            _api = new NovaAPIService(http);
            _brain = new NovaBrain();
            _thalamus = new NovaThalamus();
            _broca = new NovaBroca(_wernicke);
        }

        // ==========================================================
        // MAIN ENTRY POINT
        // Called by ChatBotAdeptus.razor for every message
        // ==========================================================
        public async Task<string> Respond(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return _thalamus.Apply("I didn't catch that. Speak clearly.", Session);

            Session.MessageCount++;
            Session.TrackTone(input);
            Session.UpdateRelationship();

            // ── 1. FSM check — mid-game states take priority ────
            // ── 1. FSM check — mid-game states take priority ────
            if (Session.FSMState != NovaFSMState.Idle)
            {
                // Allow natural item use commands to pass through mid-mission
                var cleanedFsm = input.ToLower().Trim();
                var itemPassthrough = new[]
                {
        "use medkit", "use med kit", "use nano medkit",
        "use military medkit", "heal myself", "heal me",
        "use health", "take medkit", "patch myself up",
        "patch me up", "use plasma", "fire plasma",
    };

                if (itemPassthrough.Any(t => cleanedFsm.Contains(t)))
                {
                    string itemResult = cleanedFsm.Contains("plasma")
                        ? UseItemInMission("plasma_cannon")
                        : UseItemInMission("medkit");
                    await SaveSession();
                    return itemResult;
                }

                // Detect casual chat mid-mission and redirect
                bool isMissionActive =
                    Session.FSMState == NovaFSMState.AwaitMissionAction ||
                    Session.FSMState == NovaFSMState.AwaitMissionSubAction;

                if (isMissionActive)
                {
                    var casualTriggers = new[]
                    {
            "how are you", "what are you", "tell me a joke",
            "tell me a fact", "what time", "hello", "hi ",
            "hey ", "sup", "lol", "haha", "thanks",
            "thank you", "what do you think", "interesting",
        };

                    if (casualTriggers.Any(t => cleanedFsm.Contains(t)))
                    {
                        string missionName = _activeMission?.Title ?? "your mission";
                        var redirects = new[]
                        {
                $"Focus, operative. You're mid-mission: {missionName}.",
                $"Small talk later. You have a mission to finish.",
                $"The mission doesn't pause because you got chatty.",
                $"Still waiting for your choice on {missionName}.",
                $"I appreciate the attempt at conversation. " +
                $"Finish the mission first.",
            };
                        return redirects[_rng.Next(redirects.Length)];
                    }
                }

                var fsmReply = HandleFSM(input);
                await SaveSession();
                return fsmReply;
            }

            // ── 2. Trivia answer check ──────────────────────────
            if (_activeTrivia != null)
            {
                var triviaReply = CheckTriviaAnswer(input);
                await SaveSession();
                return triviaReply;
            }

            var cleaned = input.ToLower().Trim();

            // ── 3. Hard command dispatch ────────────────────────
            var commandReply = DispatchCommand(cleaned, input);
            if (commandReply != null)
            {
                await SaveSession();
                return commandReply;
            }

            // ── 4. Keyword triggers ─────────────────────────────
            var keywordReply = DispatchKeyword(cleaned, input);
            if (keywordReply != null)
            {
                await SaveSession();
                return keywordReply;
            }

            // ── 5. Brain pipeline — NLP + reasoning ─────────────
            var brainReply = _brain.Process(input, Session);
            if (!string.IsNullOrEmpty(brainReply))
            {
                await SaveSession();
                return _thalamus.Apply(brainReply, Session);
            }

            // ── 6. Thalamus language generation ─────────────────
            var response = _thalamus.GenerateResponse(input, Session);
            await SaveSession();
            // fire and forget — never await this, never block the response
            _ = _api.RefillIfNeededAsync();
            return response;
        }

        // ==========================================================
        // COMMAND DISPATCH
        // Exact string matches — fastest path
        // ==========================================================
        private string? DispatchCommand(string cleaned, string raw)
        {
            return cleaned switch
            {
                "accept" => _thalamus.Apply(AcceptMission(), Session),
                "complete" => _thalamus.Apply(CompleteMission(), Session),
                "reset" => _thalamus.Apply(ResetMissions(), Session),
                "stats" => ShowStats(),
                "list" => ListMissions(),
                "skills" => ListSkills(),
                "help" => NovaThalamus.HelpText,
                "mini" => StartMini(),
                "trivia" => StartTrivia(),
                "reward" => _thalamus.Apply(RandomReward(), Session),
                "bonus" => _thalamus.Apply(RandomBonus(), Session),
                "loot" => _thalamus.Apply(LootDrop(), Session),
                "hack" => StartHack(),
                "puzzle" => StartPuzzle(),
                "boss" => _thalamus.Apply(BossBattle(), Session),
                "enemy" => _thalamus.Apply(EnemyEncounter(), Session),
                "combat" => _thalamus.Apply(AdvancedCombat(), Session),
                "fight" => _thalamus.Apply(AdvancedCombat(), Session),
                "stealth" => _thalamus.Apply(StealthMission(), Session),
                "companion" => _thalamus.Apply(SummonCompanion(), Session),
                "story" => _thalamus.Apply(StartStoryArc(), Session),
                "advance" => _thalamus.Apply(AdvanceStoryArc(), Session),
                "upgrade" => _thalamus.Apply(ShipUpgrade(), Session),
                "spell" => HandleSpellCommand(cleaned, raw),
                "analyze" => HandleAnalyzeCommand(cleaned, raw),
                "endgame" => _thalamus.Apply(EndgameMission(), Session),
                "cosmic" => _thalamus.Apply(CosmicEventFinal(), Session),
                "event" => _thalamus.Apply(RandomCosmicEvent(), Session),
                "ship" => _thalamus.Apply(ShipAIInteraction(), Session),
                "dismiss" => _thalamus.Apply(DismissCompanion(), Session),
                "rare" => _thalamus.Apply(RareLoot(), Session),
                "sidequest" => _thalamus.Apply(SideQuest(), Session),
                "missionchain" => _thalamus.Apply(MissionChain(), Session),
                "mood" => StartMoodSelect(),
                "name" => AskName(),
                "time" => _thalamus.Apply(
                                DateTime.Now.ToString("'Time is 'hh:mm tt ⏰"), Session),
                "date" => _thalamus.Apply(
                                DateTime.Now.ToString("'Date is 'MMMM dd, yyyy 📅"), Session),
                "inventory" => ShowInventory(),
                "inv" => ShowInventory(),
                "market" => _thalamus.Apply(TradeMarket(), Session),
                "rep" => ShowRep(),
                _ => null,
            };
        }

        // ==========================================================
        // KEYWORD TRIGGERS
        // Contains-based matches — second priority
        // ==========================================================
        private string? DispatchKeyword(string cleaned, string raw)
        {
            var spellTarget = NovaBroca.DetectSpellRequest(raw);
            if (spellTarget != null)
            {
                var signal = _wernicke.Comprehend($"spell {spellTarget}");
                return _broca.ProduceSpelling(signal, Session);
            }

            // ── Analyze request — deep character analysis ──────────────────
            var analyzeTarget = NovaBroca.DetectAnalysisRequest(raw);
            if (analyzeTarget != null)
            {
                var signal = _wernicke.Comprehend($"analyze {analyzeTarget}");
                return _broca.ProduceAnalysis(signal, Session);
            }

            // ── FIGHT ME easter egg ────────────────────────────────────
            var fightTriggers = new[]
            {
    "fight me", "fight me ai", "fight me nova",
    "i want to fight you", "come fight me",
    "let's fight", "lets fight nova", "1v1 me",
    "1v1", "attack me", "hit me", "shoot me",
};
            if (fightTriggers.Any(t => cleaned.Contains(t)) &&
                Session.FSMState == NovaFSMState.Idle)
                return FightMeEasterEgg();
            // ── Natural item use commands ──────────────────────────────
            var useTriggers = new[]
            {
    "use medkit", "use med kit", "use nano medkit",
    "use military medkit", "heal myself", "heal me",
    "use health", "take medkit", "drink medkit",
    "patch myself up", "patch me up",
};
            if (useTriggers.Any(t => cleaned.Contains(t)))
                return UseItemInMission("medkit");

            if (cleaned.Contains("use plasma") ||
                cleaned.Contains("fire plasma") ||
                cleaned.Contains("use cannon"))
                return UseItemInMission("plasma_cannon");
            if (cleaned.Contains("help")) return NovaThalamus.HelpText;
            if (cleaned.Contains("hack")) return StartHack();
            if (cleaned.Contains("trivia") ||
                cleaned.Contains("space quiz")) return StartTrivia();
            if (cleaned.Contains("fight") ||
                cleaned.Contains("combat")) return _thalamus.Apply(AdvancedCombat(), Session);
            if (cleaned.Contains("stealth")) return _thalamus.Apply(StealthMission(), Session);
            if (cleaned.Contains("puzzle")) return StartPuzzle();
            if (cleaned.Contains("loot")) return _thalamus.Apply(LootDrop(), Session);
            if (cleaned.Contains("enemy")) return _thalamus.Apply(EnemyEncounter(), Session);
            if (cleaned.Contains("boss")) return _thalamus.Apply(BossBattle(), Session);
            if (cleaned.Contains("companion")) return _thalamus.Apply(SummonCompanion(), Session);
            if (cleaned.Contains("story")) return _thalamus.Apply(StartStoryArc(), Session);
            if (cleaned.Contains("upgrade")) return _thalamus.Apply(ShipUpgrade(), Session);
            if (cleaned.Contains("market"))
                return TradeMarket();
            if (cleaned.Contains("inventory") || cleaned == "inv")
                return ShowInventory();
            if (cleaned.Contains("endgame")) return _thalamus.Apply(EndgameMission(), Session);
            if (cleaned.Contains("cosmic")) return _thalamus.Apply(CosmicEventFinal(), Session);
            if (cleaned.Contains("event")) return _thalamus.Apply(RandomCosmicEvent(), Session);
            if (cleaned.Contains("ship")) return _thalamus.Apply(ShipAIInteraction(), Session);
            if (cleaned.Contains("mission")) return _thalamus.Apply(
                                                NovaContent.Missions[_rng.Next(NovaContent.Missions.Count)], Session);
            if (cleaned.Contains("accept")) return _thalamus.Apply(AcceptMission(), Session);
            if (cleaned.Contains("complete")) return _thalamus.Apply(CompleteMission(), Session);

            // ── Name intent routing ─────────────────────────────────────
            if (cleaned.Contains("name") ||
                cleaned.Contains("who am i") ||
                cleaned.Contains("who are you") ||
                cleaned.Contains("call me") ||
                cleaned.Contains("my name is"))
            {
                var nameIntent = ClassifyNameIntent(cleaned);
                return nameIntent switch
                {
                    "nova_name" => HandleNovaNameQuestion(),
                    "user_name_recall" => RecallName(),
                    "user_name_intro" => HandlePassiveNameIntro(raw)
                                          ?? _thalamus.GenerateResponse(raw, Session),
                    _ => AskName(),   // bare "name" command fallback
                };
            }
            if (cleaned == "joke" || cleaned.Contains("joke"))
            {
                var joke = _api.PopJoke();
                return _thalamus.Apply(
                    joke ?? (_api.JokesOnline
                        ? _thalamus.GetJoke()
                        : _api.JokesOfflineMessage),
                    Session);
            }

            if ((cleaned == "fact" || cleaned.Contains("fact")) && !cleaned.Contains("trivia"))
            {
                var fact = _api.PopFact();
                return _thalamus.Apply(
                    fact ?? (_api.FactsOnline
                        ? _thalamus.GetFact()
                        : _api.FactsOfflineMessage),
                    Session);
            }

            if (cleaned == "advice")
            {
                var advice = _api.PopAdvice();
                return _thalamus.Apply(
                    advice ?? _api.AdviceOfflineMessage,
                    Session);
            }
            // REPLACE the time block in DispatchKeyword:
            if (cleaned == "time" ||
                System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"\btime\b") &&
                !new[] { "today", "sometime", "anytime", "every time",
             "last time", "next time", "one time", "first time",
             "real time", "lifetime", "runtime", "overtime" }
                    .Any(p => cleaned.Contains(p)))
                return _thalamus.Apply(
                    DateTime.Now.ToString("'Time is 'hh:mm tt ⏰"), Session);

            return null;
        }

        // ==========================================================
        // FSM STATE MACHINE
        // ==========================================================
        private string HandleFSM(string input)
        {
            return Session.FSMState switch
            {
                NovaFSMState.AwaitHack => AnswerHack(input),
                NovaFSMState.AwaitDualHack1 => AnswerDualHack1(input),
                NovaFSMState.AwaitDualHack2 => AnswerDualHack2(input),
                NovaFSMState.AwaitPuzzle => AnswerPuzzle(input),
                NovaFSMState.AwaitAnalysis => AnswerAnalysis(input),
                NovaFSMState.AwaitMood => AnswerMood(input),
                NovaFSMState.AwaitMini => AnswerMini(input),
                NovaFSMState.AwaitName => AnswerName(input),
                NovaFSMState.AwaitMissionChoice => AnswerMissionChoice(input),
                NovaFSMState.AwaitMissionAction => AnswerActiveMission(input),
                NovaFSMState.AwaitMissionSubAction => AnswerActiveMission(input),
                NovaFSMState.AwaitGameOver => AnswerGameOver(input),
                NovaFSMState.AwaitMarketChoice => AnswerMarketChoice(input),
                _ => ResetFSM("FSM error — resetting.")
            };
        }

        private string HandleSpellCommand(string cleaned, string raw)
        {
            // "spell" alone — ask what to spell
            var words = raw.Trim().Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2)
            {
                Session.FSMState = NovaFSMState.AwaitName; // reuse name FSM? 
                                                           // Actually: just return a prompt. No FSM needed.
                Session.FSMState = NovaFSMState.Idle;
                return _thalamus.Apply(
                    "Spell what? Try: spell PHOTON", Session);
            }
            var target = words[1].ToUpper();
            var signal = _wernicke.Comprehend($"spell {target}");
            return _broca.ProduceSpelling(signal, Session);
        }

        private string HandleAnalyzeCommand(string cleaned, string raw)
        {
            var words = raw.Trim().Split(' ',
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 2)
                return _thalamus.Apply(
                    "Analyze what? Try: analyze HP", Session);
            var target = words[1].ToUpper();
            var signal = _wernicke.Comprehend($"analyze {target}");
            return _broca.ProduceAnalysis(signal, Session);
        }
        private string AnswerActiveMission(string input)
        {
            if (_activeMission == null)
            {
                ResetMissionState();
                return "No active mission found. Type 'accept' to start one.";
            }

            return _activeMission.Type switch
            {
                "rescue" => AnswerRescueMission(input),
                "combat" => AnswerCombatMission(input),
                "scavenger" => AnswerScavengerMission(input),
                "hack" => AnswerHackMission(input),
                "stealth" => AnswerStealthMission(input),
                _ => AbortMission(),
            };
        }

        private string ResetFSM(string msg = "")
        {
            Session.FSMState = NovaFSMState.Idle;
            Session.FSMContext.Clear();
            return msg;
        }

        // ── Hack ────────────────────────────────────────────────
        private string StartHack()
        {
            int code = _rng.Next(100, 1000);
            Session.FSMState = NovaFSMState.AwaitHack;
            Session.FSMContext["code"] = code;
            return $"🖥️ Hack initiated! Guess the 3-digit code (100–999):";
        }

        private string AnswerHack(string input)
        {
            int code = Convert.ToInt32(Session.FSMContext["code"]);
            ResetFSM();
            if (int.TryParse(input.Trim(), out int guess) && guess == code)
            {
                Session.Skills["hacking"]++;
                return _thalamus.Apply("Hack successful! Hacking +1 ✅", Session);
            }
            return _thalamus.Apply($"Wrong! Code was {code} ☠️", Session);
        }

        // ── Dual Hack ───────────────────────────────────────────
        private string StartDualHack()
        {
            int c1 = _rng.Next(100, 1000), c2 = _rng.Next(100, 1000);
            Session.FSMState = NovaFSMState.AwaitDualHack1;
            Session.FSMContext["code1"] = c1;
            Session.FSMContext["code2"] = c2;
            return $"💻 Dual-hack! Enter code 1 (hint: starts {c1 / 100}, ends {c1 % 10}):";
        }

        private string AnswerDualHack1(string input)
        {
            if (!int.TryParse(input.Trim(), out int g1))
            {
                ResetFSM();
                return _thalamus.Apply("Invalid input — dual hack aborted ☠️", Session);
            }
            Session.FSMState = NovaFSMState.AwaitDualHack2;
            Session.FSMContext["guess1"] = g1;
            int c2 = Convert.ToInt32(Session.FSMContext["code2"]);
            return $"Code 1 logged. Enter code 2 (hint: starts {c2 / 100}, ends {c2 % 10}):";
        }

        private string AnswerDualHack2(string input)
        {
            int code1 = Convert.ToInt32(Session.FSMContext["code1"]);
            int code2 = Convert.ToInt32(Session.FSMContext["code2"]);
            int guess1 = Convert.ToInt32(Session.FSMContext["guess1"]);
            ResetFSM();
            if (int.TryParse(input.Trim(), out int g2) &&
                guess1 == code1 && g2 == code2)
            {
                Session.Skills["hacking"] += 3;
                return _thalamus.Apply("Dual hack successful! Hacking +3 ✅", Session);
            }
            return _thalamus.Apply($"Failed! Codes were {code1}, {code2} ☠️", Session);
        }

        // ── Puzzle ──────────────────────────────────────────────
        private string StartPuzzle()
        {
            int a = _rng.Next(1, 21), b = _rng.Next(1, 21);
            Session.FSMState = NovaFSMState.AwaitPuzzle;
            Session.FSMContext["answer"] = a + b;
            Session.FSMContext["a"] = a;
            Session.FSMContext["b"] = b;
            return $"🧩 Solve: {a} + {b} = ?";
        }

        private string AnswerPuzzle(string input)
        {
            int answer = Convert.ToInt32(Session.FSMContext["answer"]);
            ResetFSM();
            if (int.TryParse(input.Trim(), out int guess) && guess == answer)
            {
                Session.Skills["analysis"]++;
                return _thalamus.Apply("Correct! Analysis +1 📊", Session);
            }
            return _thalamus.Apply($"Wrong! Answer was {answer} ☠️", Session);
        }

        // ── Analysis ────────────────────────────────────────────
        private string StartAnalysis()
        {
            var colors = new[] { "red", "blue", "green", "yellow", "purple" };
            string ans = colors[_rng.Next(colors.Length)];
            Session.FSMState = NovaFSMState.AwaitAnalysis;
            Session.FSMContext["answer"] = ans;
            return "🔬 Analyze the signal color: red / blue / green / yellow / purple";
        }

        private string AnswerAnalysis(string input)
        {
            string answer = Session.FSMContext["answer"].ToString()!;
            ResetFSM();
            if (input.Trim().ToLower() == answer)
            {
                Session.Skills["analysis"]++;
                return _thalamus.Apply("Analysis perfect! Analysis +1 📊", Session);
            }
            return _thalamus.Apply($"Wrong! Color was {answer} ☠️", Session);
        }

        // ── Mood ────────────────────────────────────────────────
        private string StartMoodSelect()
        {
            Session.FSMState = NovaFSMState.AwaitMood;
            return "Choose mood: flirty · deadly · sarcastic";
        }

        private string AnswerMood(string input)
        {
            ResetFSM();
            var choice = input.Trim().ToLower();
            if (choice is "flirty" or "deadly" or "sarcastic")
            {
                _thalamus.SetMood(choice);
                return $"Mood switched to {choice} 😏";
            }
            return "Invalid — choose flirty, deadly, or sarcastic.";
        }

        // ── Mini game select ────────────────────────────────────
        private string StartMini()
        {
            Session.FSMState = NovaFSMState.AwaitMini;
            return ("🎮 Choose a mini-game:\n"
                  + "combat · hack · dualhack · stealth · puzzle · analysis ·\n"
                  + "stealthhack · combathack · coop · loot · anomaly · enemy ·\n"
                  + "upgrade · rare · sidequest · companion · missionchain ·\n"
                  + "story · advance · boss · ship · market · endgame ·\n"
                  + "ultimate · cosmic · event");
        }


        private string AnswerMini(string input)
        {
            ResetFSM();
            var choice = input.Trim().ToLower();

            var result = choice switch
            {
                "combat" => AdvancedCombat(),

                "hack" => SetIdleAndReturn(StartHack()),
                "dualhack" => SetIdleAndReturn(StartDualHack()),
                "puzzle" => SetIdleAndReturn(StartPuzzle()),
                "analysis" => SetIdleAndReturn(StartAnalysis()),

                "stealth" => StealthMission(),
                "stealthhack" => StealthHackMission(),
                "combathack" => CombatHackDuel(),
                "coop" => CoopMission(),
                "loot" => LootDrop(),
                "anomaly" => SpaceAnomalyMission(),
                "enemy" => EnemyEncounter(),
                "upgrade" => ShipUpgrade(),
                "rare" => RareLoot(),
                "sidequest" => SideQuest(),
                "companion" => SummonCompanion(),
                "missionchain" => MissionChain(),
                "story" => StartStoryArc(),
                "advance" => AdvanceStoryArc(),
                "boss" => BossBattle(),
                "ship" => ShipAIInteraction(),
                "market" => TradeMarket(),
                "endgame" => EndgameMission(),
                "ultimate" => UltimateLoot(),
                "cosmic" => CosmicEventFinal(),
                "event" => RandomCosmicEvent(),

                _ => $"Unknown game '{choice}'. Type 'mini' to see the list ☠️",
            };

            return _thalamus.Apply(result, Session);
        }

        private string SetIdleAndReturn(string result)
        {
            Session.FSMState = NovaFSMState.Idle;
            return result;
        }

        // ── Nova recalls the player's name ─────────────────────────
        private string RecallName()
        {
            if (Session.UserName != null)
                return _thalamus.Apply(
                    $"Your name is {Session.UserName}. I remember everything.", Session);

            return _thalamus.Apply(
                "You haven't told me your name yet. Type 'name' and introduce yourself.",
                Session);
        }

        // ── Nova answers questions about her own name ───────────────
        private string HandleNovaNameQuestion()
        {
            return _thalamus.Apply(
                "I'm Nova Adeptus — Cosmic Assassin AI. Forged in the void, running in C# and Blazor. 🌌",
                Session);
        }

        // ── Passive name capture (mid-sentence intro) ──────────────
        private string HandlePassiveNameIntro(string raw)
        {
            var extracted = _brain.ExtractName(raw);
            if (extracted != null)
            {
                var isCorrection = raw.ToLower().StartsWith("no ") ||
                                   raw.ToLower().Contains("actually") ||
                                   raw.ToLower().Contains("no my name");

                Session.UserName = extracted;

                var response = isCorrection
                    ? $"My mistake. {extracted} it is. The void has updated its records. 👁️"
                    : $"Noted. I'll call you {extracted}. The void remembers. 👁️";

                return _thalamus.Apply(response, Session);
            }
            return null!;
        }


        // ── Name ────────────────────────────────────────────────
        private string AskName()
{
    Session.FSMState = NovaFSMState.AwaitName;
    return _thalamus.Apply("What should I call you, operative?", Session);
}
        // ADD this public wrapper to NovaBrain.cs

        private string AnswerName(string input)
        {
            ResetFSM();

            // Try prefix-based extraction first
            var extracted = _brain.ExtractName(input);

            // Fallback: if input is short and looks like a name, just use it directly
            // Fallback: if input is short and looks like a name, just use it directly
            if (extracted == null)
            {
                var trimmed = input.Trim();
                var words = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var notNames = new HashSet<string>
    {
        "a","an","the","here","back","ready","not","just",
        "going","trying","sorry","good","bad","ok","okay",
        "playing","new","old","in","on","at","sure","still",
    };

                if (words.Length >= 1 && words.Length <= 3 &&
                    words.All(w => w.Length >= 2 && w.All(char.IsLetter)) &&
                   !words.Any(w => notNames.Contains(w.ToLower())))
                {
                    extracted = char.ToUpper(trimmed[0]) + trimmed.Substring(1).ToLower();
                }
            }

            if (extracted != null)
            {
                Session.UserName = extracted;
                return _thalamus.Apply(
                    $"Welcome, {extracted}. The void awaits 👁️", Session);
            }

            // Only re-prompt if it was genuinely unreadable
            Session.FSMState = NovaFSMState.AwaitName;
            return _thalamus.Apply(
                "Hmm. I couldn't catch a name in that. Just tell me your name, operative.",
                Session);
        }

        // ── Trivia ──────────────────────────────────────────────
        private string StartTrivia()
        {
            if (!_triviaCache.Any())
                return _api.TriviaOnline
                    ? "No trivia loaded yet — try again in a moment 🌌"
                    : _api.TriviaOfflineMessage;

            _activeTrivia = _triviaCache[0];
            _triviaCache.RemoveAt(0);

            var choices = _activeTrivia.IncorrectAnswers
                .Append(_activeTrivia.CorrectAnswer)
                .OrderBy(_ => _rng.Next())
                .ToList();

            var letterMap = choices
                .Select((c, i) => (Letter: (char)('A' + i), Answer: c))
                .ToDictionary(x => x.Letter, x => x.Answer);

            _activeTrivia.LetterMap = letterMap;

            var opts = string.Join("  ", letterMap.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"🔬 [{_activeTrivia.Category}] {_activeTrivia.Question}\n{opts}\nType A, B, C, or D ☠️";
        }

        private string CheckTriviaAnswer(string input)
{
    var q = _activeTrivia!;
    _activeTrivia = null;

    var guess = input.Trim().ToUpper().FirstOrDefault();
    if (q.LetterMap == null || !q.LetterMap.ContainsKey(guess))
        return _thalamus.Apply($"Not a valid choice. Answer was: {q.CorrectAnswer} 😏", Session);

    if (q.LetterMap[guess] == q.CorrectAnswer)
    {
        Session.XP += 10;
        return _thalamus.Apply($"Correct! ✅ {q.CorrectAnswer} | XP +10", Session);
    }
    return _thalamus.Apply($"Wrong ☠️ Answer was: {q.CorrectAnswer}", Session);
}

public void InjectTrivia(List<TriviaQuestion> questions)
{
    _triviaCache.AddRange(questions);
}


        // ==========================================================
        // MISSION SYSTEM
        // ==========================================================

        private string AnswerMissionChoice(string input)
        {
            var key = input.Trim().ToUpper().FirstOrDefault();

            // Abort
            if (key == 'E' || input.Trim().ToLower() == "abort")
            {
                Session.FSMState = NovaFSMState.Idle;
                return _thalamus.Apply("Mission aborted. Back to base.", Session);
            }

            // Map letter to mission
            int index = key - 'A';
            if (index < 0 || index >= NovaContent.MissionMenu.Count)
            {
                return "Invalid choice. Type A through " +
                       (char)('A' + NovaContent.MissionMenu.Count - 1) +
                       " — or E to abort.";
            }

            _activeMission = NovaContent.MissionMenu[index];
            Session.FSMState = NovaFSMState.AwaitMissionAction;
            _missionStage = "start";

            return StartMissionBranch(_activeMission);
        }

        // ── Branch router ─────────────────────────────────────────
        private string StartMissionBranch(NovaContent.MissionDef mission)
        {
            var brief = $"📡 MISSION: {mission.Title}\n" +
                        $"📍 Location: {mission.Planet}\n\n" +
                        $"{mission.Briefing}\n\n";

            return mission.Type switch
            {
                "rescue" => brief + RescueMissionMenu(),
                "combat" => brief + CombatMissionMenu(),
                "scavenger" => brief + ScavengerMissionMenu(),
                "hack" => brief + HackMissionMenu(),
                "stealth" => brief + StealthMissionMenu(),
                _ => brief + "Mission type unknown. Aborting. ☠️",
            };
        }

        // ==========================================================
        // RESCUE MISSION
        // ==========================================================
        private string RescueMissionMenu() =>
            "You find the civilian — wounded, barely conscious.\n\n" +
            "  A. Analyze the situation (uses Analysis skill)\n" +
            "  B. Rescue the civilian\n" +
            "  C. Eliminate the civilian\n" +
            "  D. Talk to the civilian\n" +
            "  E. Abort mission (-10 Galactic Coins)\n";

        private string AnswerRescueMission(string input)
        {
            var key = input.Trim().ToUpper().FirstOrDefault();

            if (_missionStage == "start")
            {
                switch (key)
                {
                    case 'A': // Analyze
                        _missionStage = "analyze";
                        int skill = Session.EffectiveAnalysis;
                        if (skill >= 5)
                            return "🔬 Analysis complete. Civilian has a puncture wound — " +
                                   "treatable with a Medkit. Stable enough to move.\n\n" +
                                   "  B. Rescue the civilian (+20 coins, +2 GoodRep)\n" +
                                   "  F. Use Medkit from inventory (if you have one)\n" +
                                   "  E. Abort mission (-10 coins)\n";
                        else
                            return "🔬 You attempt analysis but your skill is too low to read " +
                                   "the situation clearly. The civilian looks bad.\n\n" +
                                   "  B. Rescue anyway (risky)\n" +
                                   "  C. Cut your losses\n" +
                                   "  E. Abort mission (-10 coins)\n";

                    case 'B': // Rescue
                        return ResolveCivilianRescue();

                    case 'C': // Eliminate
                        return ResolveCivilianEliminate();

                    case 'D': // Talk
                        _missionStage = "talk";
                        return "💬 The civilian grabs your arm.\n\n" +
                               "\"Please... I saw them. The relic — they moved it to " +
                               "sublevel three. I can show you... just get me out.\"\n\n" +
                               "  B. Rescue them (+20 coins, +2 GoodRep, bonus intel)\n" +
                               "  C. Take the intel and leave them ☠️ (-3 BadRep)\n" +
                               "  E. Abort mission (-10 coins)\n";

                    case 'E': // Abort
                        return AbortMission();

                    default:
                        return "Type A, B, C, D, or E, operative.";
                }
            }

            // Sub-stage: talk branch
            if (_missionStage == "talk")
            {
                switch (key)
                {
                    case 'B': return ResolveCivilianRescue(bonusIntel: true);
                    case 'C': return ResolveCivilianEliminate(tookIntel: true);
                    case 'E': return AbortMission();
                    default: return "Type B, C, or E.";
                }
            }

            // Sub-stage: analyze branch
            if (_missionStage == "analyze")
            {
                switch (key)
                {
                    case 'B': return ResolveCivilianRescue();
                    case 'C': return ResolveCivilianEliminate();
                    case 'F': return UseItemInMission("medkit");
                    case 'E': return AbortMission();
                    default: return "Type B, C, F, or E.";
                }
            }

            return "Type a valid option, operative.";
        }

        private string ResolveCivilianRescue(bool bonusIntel = false)
        {
            ResetMissionState();
            Session.MissionsCompleted++;
            Session.GoodRep += 2;
            int coins = bonusIntel ? 30 : 20;
            Session.GalacticCoins += coins;
            Session.XP += 10;
            string bonus = bonusIntel ? " Bonus intel acquired — sublevel three marked." : "";
            return _thalamus.Apply(
                $"✅ Civilian rescued and extracted safely.{bonus}\n" +
                $"+{coins} Galactic Coins | +2 Good Rep | XP +10\n" +
                $"💰 Coins: {Session.GalacticCoins} | ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                Session);
        }

        private string ResolveCivilianEliminate(bool tookIntel = false)
        {
            ResetMissionState();
            Session.BadRep += 3;
            Session.GalacticCoins -= 10;
            Session.XP += 2;
            string intel = tookIntel ? " You took the intel at least." : "";
            return _thalamus.Apply(
                $"☠️ Civilian eliminated.{intel}\n" +
                $"-10 Galactic Coins | +3 Bad Rep | XP +2\n" +
                $"Nova: That was cold, even for this sector.\n" +
                $"💰 Coins: {Session.GalacticCoins} | ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                Session);
        }

        // ==========================================================
        // COMBAT MISSION
        // ==========================================================
        private string CombatMissionMenu()
        {
            _missionEnemyHP = _rng.Next(15, 31);
            _missionStage = "start";
            return $"An alien lifeform blocks your path. " +
                   $"It eyes you with what might be curiosity. Or hunger.\n" +
                   $"[Enemy HP: {_missionEnemyHP}]\n\n" +
                   "  A. Talk to the alien lifeform\n" +
                   "  B. Throw a rock at it\n" +
                   "  C. Rush it and smash it to pieces\n" +
                   "  D. Random action (anything could happen)\n" +
                   "  E. Flee the mission (-10 Galactic Coins)\n";
        }

        private string AnswerCombatMission(string input)
        {
            var key = input.Trim().ToUpper().FirstOrDefault();

            if (_missionStage == "start")
            {
                switch (key)
                {
                    case 'A': // Talk
                        int skill = Session.EffectiveAnalysis;
                        if (skill >= 4)
                        {
                            ResetMissionState();
                            Session.MissionsCompleted++;
                            Session.GoodRep++;
                            Session.GalacticCoins += 15;
                            Session.XP += 10;
                            return _thalamus.Apply(
                                "💬 You speak calmly. The alien tilts its head... " +
                                "and steps aside. Diplomatic success.\n" +
                                "+15 Galactic Coins | +1 Good Rep | XP +10",
                                Session);
                        }
                        else
                        {
                            _missionStage = "retaliate";
                            int dmg = Session.TakeDamage(4);
                            string gameOver = CheckGameOver();
                            if (gameOver != "") return gameOver;
                            return $"💬 You attempt to speak but your dialect is wrong. " +
                                   $"The alien is offended.\n" +
                                   $"It slaps you with a tentacle! -{dmg} HP\n" +
                                   $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   $"  A. Retreat carefully (+5 coins, end mission)\n" +
                                   $"  B. Fight back ⚔️\n" +
                                   $"  E. Flee (-10 coins)\n";
                        }

                    case 'B': // Throw rock
                        _missionStage = "retaliate";
                        _missionEnemyHP -= _rng.Next(3, 8);
                        int rockDmg = Session.TakeDamage(3);
                        string rockGameOver = CheckGameOver();
                        if (rockGameOver != "") return rockGameOver;
                        return $"🪨 You hurl a rock. It bounces off the alien's head.\n" +
                               $"[Enemy HP: {_missionEnemyHP}]\n" +
                               $"The alien is NOT happy. Tentacle strike! -{rockDmg} HP\n" +
                               $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                               $"  A. Dodge and run (+5 coins)\n" +
                               $"  B. Press the attack ⚔️\n" +
                               $"  C. Deploy armor and hold ground\n" +
                               $"  E. Flee (-10 coins)\n";

                    case 'C': // Rush
                        int combatSkill = Session.EffectiveCombat;
                        if (combatSkill >= 5)
                        {
                            ResetMissionState();
                            Session.MissionsCompleted++;
                            Session.Skills["combat"]++;
                            Session.EnemiesDefeated++;
                            Session.GalacticCoins += 20;
                            Session.XP += 15;
                            return _thalamus.Apply(
                                "⚔️ You rush the alien with pure aggression. " +
                                "Your combat skill carries the day — it goes down hard.\n" +
                                "+20 Galactic Coins | Combat +1 | XP +15",
                                Session);
                        }
                        else
                        {
                            _missionStage = "retaliate";
                            int rushDmg = Session.TakeDamage(10);
                            string rushGameOver = CheckGameOver();
                            if (rushGameOver != "") return rushGameOver;
                            return $"⚔️ You charge — but the alien is faster than expected.\n" +
                                   $"It catches you mid-rush and flings you across the ground.\n" +
                                   $"Took {rushDmg} HP damage! ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   $"  A. Get up and fight ⚔️\n" +
                                   $"  B. Play dead (stealth check)\n" +
                                   $"  E. Flee (-10 coins)\n";
                        }

                    case 'D': // Random
                        return RandomCombatEvent();

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, D, or E, operative.";
                }
            }

            // Retaliation sub-branch
            if (_missionStage == "retaliate")
            {
                switch (key)
                {
                    case 'A': // Retreat/Dodge
                        ResetMissionState();
                        Session.GalacticCoins += 5;
                        return _thalamus.Apply(
                            "You disengage carefully. The alien lets you go.\n" +
                            "+5 Galactic Coins. Mission concluded — barely.",
                            Session);

                    case 'B': // Fight back
                        int fightDmg = _rng.Next(5, 16);
                        _missionEnemyHP -= fightDmg;
                        if (_missionEnemyHP <= 0)
                        {
                            ResetMissionState();
                            Session.MissionsCompleted++;
                            Session.EnemiesDefeated++;
                            Session.Skills["combat"]++;
                            Session.GalacticCoins += 20;
                            Session.XP += 15;
                            return _thalamus.Apply(
                                $"⚔️ You land a decisive blow! Enemy defeated!\n" +
                                $"+20 Galactic Coins | Combat +1 | XP +15\n" +
                                $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                                Session);
                        }
                        else
                        {
                            int counterDmg = Session.TakeDamage(5);
                            string go = CheckGameOver();
                            if (go != "") return go;
                            return $"⚔️ You hit! [Enemy HP: {_missionEnemyHP}]\n" +
                                   $"The alien retaliates — -{counterDmg} HP\n" +
                                   $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   $"  A. Keep fighting\n" +
                                   $"  B. Fall back\n" +
                                   $"  F. Use Medkit\n";
                        }

                    case 'C': // Hold ground / armor
                        int blockDmg = Session.TakeDamage(2);
                        _missionStage = "retaliate";
                        return $"🛡️ You brace. Armor absorbs most of it. -{blockDmg} HP\n" +
                               $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                               $"  A. Counterattack ⚔️\n" +
                               $"  E. Flee (-10 coins)\n";

                    case 'F': // Use medkit
                        return UseItemInMission("medkit");

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, F, or E.";
                }
            }

            return "Type a valid option, operative.";
        }

        private string RandomCombatEvent()
        {
            int roll = _rng.Next(4);

            switch (roll)
            {
                case 0:
                    return ResolveCombatWin("The alien trips on a rock. You seize the moment.");

                case 1:
                    {
                        int d = Session.TakeDamage(6);
                        string go = CheckGameOver();
                        if (go != "") return go;

                        _missionStage = "retaliate";

                        return $"🎲 Random: The alien sneezes directly on you. " +
                               $"Somehow that hurts. -{d} HP\n" +
                               $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                               $"  A. Retreat  B. Fight back  E. Flee";
                    }

                case 2:
                    {
                        ResetMissionState();
                        Session.GalacticCoins += 10;

                        return _thalamus.Apply(
                            "🎲 Random: The alien produces a small glowing orb " +
                            "and hands it to you. Then leaves. +10 coins. Nobody knows why.",
                            Session);
                    }

                default:
                    {
                        Session.Skills["combat"]++;
                        ResetMissionState();
                        Session.MissionsCompleted++;

                        return _thalamus.Apply(
                            "🎲 Random: You both stare at each other for 40 seconds. " +
                            "The alien nods and walks away. Combat +1. Mission complete.",
                            Session);
                    }
            }
        }
        private string ResolveCombatWin(string flavor)
{
    ResetMissionState();
    Session.MissionsCompleted++;
    Session.EnemiesDefeated++;
    Session.GalacticCoins += 20;
    Session.XP += 15;
    return _thalamus.Apply(
        $"⚔️ {flavor}\nEnemy defeated!\n" +
        $"+20 Galactic Coins | XP +15\n" +
        $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
        Session);
}

// ==========================================================
// SCAVENGER MISSION
// ==========================================================
private string ScavengerMissionMenu()
{
    _missionStage = "start";
    return "The relic is somewhere on this surface. " +
           "Your scanner is picking up faint traces in three directions.\n\n" +
           "  A. Look under the bush 🌿\n" +
           "  B. Check under the rock 🪨\n" +
           "  C. Investigate the old fence 🚧\n" +
           "  D. Scan the area first (uses Analysis skill)\n" +
           "  E. Abort mission (-10 Galactic Coins)\n";
}

private string AnswerScavengerMission(string input)
{
    var key = input.Trim().ToUpper().FirstOrDefault();

    if (_missionStage == "start")
    {
        switch (key)
        {
            case 'A': // Bush — thorn trap
                int thornDmg = Session.TakeDamage(5);
                string thornGO = CheckGameOver();
                if (thornGO != "") return thornGO;
                _missionStage = "bush";
                return $"🌿 You reach into the bush.\n" +
                       $"THORN TRAP! Something stings your hand hard.\n" +
                       $"-{thornDmg} HP! ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                       $"But wait — something glints in there.\n\n" +
                       $"  A. Reach in again despite the pain\n" +
                       $"  B. Use a tool to probe instead\n" +
                       $"  E. Back off (-10 coins)\n";

            case 'B': // Rock — nothing or scavenger
                int roll = _rng.Next(3);
                if (roll == 0)
                {
                    ResetMissionState();
                    Session.MissionsCompleted++;
                    Session.GalacticCoins += 25;
                    Session.XP += 12;
                    return _thalamus.Apply(
                        "🪨 You heave the rock aside.\n" +
                        "There it is — the relic, half-buried in the dirt.\n" +
                        "Mission complete! +25 Galactic Coins | XP +12",
                        Session);
                }
                else if (roll == 1)
                {
                    _missionStage = "scavenger_spawn";
                    _missionEnemyHP = _rng.Next(10, 20);
                    return $"🪨 Nothing under the rock — but a scavenger was watching.\n" +
                           $"[Scavenger HP: {_missionEnemyHP}]\n\n" +
                           $"  A. Fight the scavenger ⚔️\n" +
                           $"  B. Run past them to the fence\n" +
                           $"  E. Flee mission (-10 coins)\n";
                }
                else
                    return "🪨 Just dirt and a very confused beetle.\n\n" +
                           "  A. Try the bush 🌿\n" +
                           "  C. Try the fence 🚧\n" +
                           "  E. Abort (-10 coins)\n";

            case 'C': // Fence
                _missionStage = "fence";
                return "🚧 The old fence hums faintly. " +
                       "Something is definitely buried on the other side.\n\n" +
                       "  A. Climb over\n" +
                       "  B. Dig under\n" +
                       "  C. Cut through (requires Hacking skill 3+)\n" +
                       "  E. Back off (-10 coins)\n";

            case 'D': // Scan
                int scanSkill = Session.EffectiveAnalysis;
                if (scanSkill >= 3)
                    return "📡 Scanner confirms: relic signal is strongest near the fence.\n" +
                           "  C. Investigate the fence 🚧\n" +
                           "  E. Abort (-10 coins)\n";
                else
                    return "📡 Your scanner gives a weak reading. " +
                           "Signal is... somewhere.\n\n" +
                           "  A. Bush 🌿  B. Rock 🪨  C. Fence 🚧  E. Abort\n";

            case 'E':
                return AbortMission();

            default:
                return "Type A, B, C, D, or E, operative.";
        }
    }

    // Bush sub-branch
    if (_missionStage == "bush")
    {
        switch (key)
        {
            case 'A':
                int dmg2 = Session.TakeDamage(3);
                string go2 = CheckGameOver();
                if (go2 != "") return go2;
                ResetMissionState();
                Session.MissionsCompleted++;
                Session.GalacticCoins += 25;
                Session.XP += 12;
                return _thalamus.Apply(
                    $"😤 You push through the pain.\n" +
                    $"Fingers close around the relic. Got it!\n" +
                    $"-{dmg2} HP (worth it) | +25 Coins | XP +12\n" +
                    $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                    Session);
            case 'B':
                ResetMissionState();
                Session.MissionsCompleted++;
                Session.GalacticCoins += 25;
                Session.XP += 12;
                return _thalamus.Apply(
                    "🔧 Smart. You probe with your tool and flick the relic free.\n" +
                    "No extra damage. +25 Coins | XP +12",
                    Session);
            case 'E':
                return AbortMission();
            default:
                return "Type A, B, or E.";
        }
    }

    // Scavenger spawn sub-branch
    if (_missionStage == "scavenger_spawn")
    {
        switch (key)
        {
            case 'A':
                int atkRoll = _rng.Next(5, 16);
                if (atkRoll >= _missionEnemyHP)
                {
                    ResetMissionState();
                    Session.EnemiesDefeated++;
                    Session.MissionsCompleted++;
                    Session.GalacticCoins += 30;
                    Session.XP += 15;
                    return _thalamus.Apply(
                        "⚔️ Scavenger down! You grab the relic from their pack.\n" +
                        "+30 Coins | XP +15",
                        Session);
                }
                else
                {
                    int sDmg = Session.TakeDamage(7);
                    string sGO = CheckGameOver();
                    if (sGO != "") return sGO;
                    return $"⚔️ Scavenger fights dirty! -{sDmg} HP\n" +
                           $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                           $"  A. Keep fighting  B. Run  F. Use Medkit  E. Flee\n";
                }
            case 'B':
                _missionStage = "fence";
                return "You sprint past the scavenger toward the fence.\n\n" +
                       "  A. Climb over  B. Dig under\n" +
                       "  C. Cut through (Hacking 3+)  E. Abort\n";
            case 'E':
                return AbortMission();
            default:
                return "Type A, B, or E.";
        }
    }

    // Fence sub-branch
    if (_missionStage == "fence")
    {
        switch (key)
        {
            case 'A': // Climb
                int climbDmg = Session.TakeDamage(2);
                string climbGO = CheckGameOver();
                if (climbGO != "") return climbGO;
                ResetMissionState();
                Session.MissionsCompleted++;
                Session.GalacticCoins += 25;
                Session.XP += 12;
                return _thalamus.Apply(
                    $"🧗 You climb over. Snag yourself on a wire. -{climbDmg} HP.\n" +
                    $"But there's the relic. Worth it.\n" +
                    $"+25 Coins | XP +12 | ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                    Session);
            case 'B': // Dig
                ResetMissionState();
                Session.MissionsCompleted++;
                Session.GalacticCoins += 25;
                Session.XP += 12;
                return _thalamus.Apply(
                    "⛏️ You dig under the fence. Takes a while. No damage.\n" +
                    "Relic secured. +25 Coins | XP +12",
                    Session);
            case 'C': // Cut — hacking gate
                int hackSkill = Session.EffectiveHacking;
                if (hackSkill >= 3)
                {
                    ResetMissionState();
                    Session.MissionsCompleted++;
                    Session.GalacticCoins += 30;
                    Session.XP += 15;
                    return _thalamus.Apply(
                        "💻 You slice through the fence control panel. " +
                        "Gate opens. Clean entry.\n" +
                        "+30 Coins | XP +15",
                        Session);
                }
                else
                    return "💻 Your hacking skill isn't high enough to cut the lock.\n" +
                           "  A. Climb  B. Dig  E. Abort\n";
            case 'E':
                return AbortMission();
            default:
                return "Type A, B, C, or E.";
        }
    }

    return "Type a valid option, operative.";
}

        // ==========================================================
        // HACK + STEALTH MISSION MENUS (stubs — full branches next snippet)
        // ==========================================================
        private string HackMissionMenu()
        {
            _missionStage = "start";
            int hackSkill = Session.EffectiveHacking;

            return "The uplink terminal blinks in the darkness ahead.\n" +
                   $"[Your Hacking: {hackSkill}]\n\n" +
                   "  A. Direct port access (fast, risky)\n" +
                   "  B. Probe defenses first (safer, slower)\n" +
                   "  C. Deploy ICE Breaker (requires Hacking 4+)\n" +
                   "  D. Plant a virus and retreat (low reward, no risk)\n" +
                   "  E. Abort mission (-10 Galactic Coins)\n";
        }

        private string AnswerHackMission(string input)
        {
            var key = input.Trim().ToUpper().FirstOrDefault();

            // ── START STAGE ──────────────────────────────────────────
            if (_missionStage == "start")
            {
                switch (key)
                {
                    case 'A': // Direct port — risky
                        int hackRoll = _rng.Next(1, 11) + Session.EffectiveHacking;
                        if (hackRoll >= 8)
                        {
                            _missionStage = "deep_access";
                            return "⚡ Direct access granted. You're in — but the system\n" +
                                   "is fighting back. ICE protocols activating.\n\n" +
                                   "  A. Rip the data fast and disconnect\n" +
                                   "  B. Burrow deeper — extract everything\n" +
                                   "  C. Plant a backdoor before leaving\n" +
                                   "  E. Panic disconnect now\n";
                        }
                        else
                        {
                            _missionStage = "traced";
                            int dmg = Session.TakeDamage(8);
                            string go = CheckGameOver();
                            if (go != "") return go;
                            return $"💥 ICE COUNTERMEASURE! System traced your port.\n" +
                                   $"Feedback blast! -{dmg} HP\n" +
                                   $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   "  A. Push through anyway (Hacking check)\n" +
                                   "  B. Reroute through backup node\n" +
                                   "  C. Wipe your trace and retreat\n" +
                                   "  E. Full disconnect (-10 coins)\n";
                        }

                    case 'B': // Probe first — safer
                        _missionStage = "probed";
                        int probeSkill = Session.EffectiveHacking;
                        string probeResult = probeSkill >= 4
                            ? "📡 Probe complete. Three ICE layers detected.\n" +
                              "Weak point identified at sector 7-Gamma.\n\n" +
                              "  A. Exploit the weak point (+bonus reward)\n" +
                              "  B. Standard breach\n" +
                              "  E. Abort (-10 coins)\n"
                            : "📡 Probe returns partial data. Defenses unclear.\n\n" +
                              "  A. Breach anyway\n" +
                              "  B. Plant virus and retreat (safe, low reward)\n" +
                              "  E. Abort (-10 coins)\n";
                        return probeResult;

                    case 'C': // ICE Breaker
                        if (Session.EffectiveHacking >= 4)
                        {
                            ResetMissionState();
                            Session.MissionsCompleted++;
                            Session.Skills["hacking"]++;
                            Session.GalacticCoins += 35;
                            Session.XP += 20;
                            return _thalamus.Apply(
                                "💻 ICE Breaker deployed. Defenses collapse in seconds.\n" +
                                "Clean extraction. Not even a trace left behind.\n" +
                                "+35 Galactic Coins | Hacking +1 | XP +20",
                                Session);
                        }
                        return "Your hacking skill isn't high enough for the ICE Breaker.\n" +
                               "Need Hacking 4+. You have " +
                               $"{Session.EffectiveHacking}.\n\n" +
                               "  A. Direct access  B. Probe  D. Virus plant  E. Abort\n";

                    case 'D': // Plant virus — safe, low reward
                        ResetMissionState();
                        Session.MissionsCompleted++;
                        Session.GalacticCoins += 10;
                        Session.XP += 5;
                        return _thalamus.Apply(
                            "🦠 Virus planted. You retreat before the system notices.\n" +
                            "Low risk. Low glory. +10 Galactic Coins | XP +5",
                            Session);

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, D, or E, operative.";
                }
            }

            // ── DEEP ACCESS STAGE ────────────────────────────────────
            if (_missionStage == "deep_access")
            {
                switch (key)
                {
                    case 'A': // Fast extract
                        ResetMissionState();
                        Session.MissionsCompleted++;
                        Session.Skills["hacking"]++;
                        Session.GalacticCoins += 25;
                        Session.XP += 15;
                        return _thalamus.Apply(
                            "💾 Data ripped. Clean disconnect.\n" +
                            "+25 Galactic Coins | Hacking +1 | XP +15",
                            Session);

                    case 'B': // Go deeper — risk/reward
                        int deepRoll = _rng.Next(1, 11) + Session.EffectiveHacking;
                        if (deepRoll >= 9)
                        {
                            ResetMissionState();
                            Session.MissionsCompleted++;
                            Session.Skills["hacking"] += 2;
                            Session.GalacticCoins += 50;
                            Session.XP += 25;
                            return _thalamus.Apply(
                                "💎 FULL EXTRACTION. Everything. Enemy comms,\n" +
                                "coordinates, codes. The High Order will be pleased.\n" +
                                "+50 Galactic Coins | Hacking +2 | XP +25",
                                Session);
                        }
                        else
                        {
                            int deepDmg = Session.TakeDamage(12);
                            string deepGO = CheckGameOver();
                            if (deepGO != "") return deepGO;
                            ResetMissionState();
                            Session.GalacticCoins += 10;
                            Session.XP += 5;
                            return _thalamus.Apply(
                                $"💥 System fought back hard. Feedback knocked you out\n" +
                                $"of the uplink. -{deepDmg} HP\n" +
                                $"Partial data recovered.\n" +
                                $"+10 Galactic Coins | XP +5\n" +
                                $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                                Session);
                        }

                    case 'C': // Backdoor
                        ResetMissionState();
                        Session.MissionsCompleted++;
                        Session.Skills["hacking"]++;
                        Session.GalacticCoins += 30;
                        Session.XP += 18;
                        return _thalamus.Apply(
                            "🔓 Backdoor planted. Passive intel incoming for weeks.\n" +
                            "+30 Galactic Coins | Hacking +1 | XP +18",
                            Session);

                    case 'E': // Panic disconnect
                        ResetMissionState();
                        return _thalamus.Apply(
                            "⚡ Panic disconnect. No data. No trace. No glory.",
                            Session);

                    default:
                        return "Type A, B, C, or E.";
                }
            }

            // ── TRACED STAGE ─────────────────────────────────────────
            if (_missionStage == "traced")
            {
                switch (key)
                {
                    case 'A': // Push through
                        int pushRoll = _rng.Next(1, 11) + Session.EffectiveHacking;
                        if (pushRoll >= 7)
                        {
                            ResetMissionState();
                            Session.MissionsCompleted++;
                            Session.Skills["hacking"]++;
                            Session.GalacticCoins += 20;
                            Session.XP += 12;
                            return _thalamus.Apply(
                                "💻 You push through the ICE. Bloody but successful.\n" +
                                "+20 Galactic Coins | Hacking +1 | XP +12",
                                Session);
                        }
                        else
                        {
                            int pushDmg = Session.TakeDamage(10);
                            string pushGO = CheckGameOver();
                            if (pushGO != "") return pushGO;
                            ResetMissionState();
                            return _thalamus.Apply(
                                $"💥 System hit back again. You're out. -{pushDmg} HP\n" +
                                $"Mission failed.\n" +
                                $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                                Session);
                        }

                    case 'B': // Reroute
                        ResetMissionState();
                        Session.MissionsCompleted++;
                        Session.GalacticCoins += 15;
                        Session.XP += 8;
                        return _thalamus.Apply(
                            "🔄 Rerouted through a backup node. Slower.\n" +
                            "Partial data only. +15 Galactic Coins | XP +8",
                            Session);

                    case 'C': // Wipe trace
                        ResetMissionState();
                        Session.XP += 3;
                        return _thalamus.Apply(
                            "🧹 Trace wiped. You got nothing but your life.\n" +
                            "XP +3. The void is unimpressed.",
                            Session);

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, or E.";
                }
            }

            // ── PROBED STAGE ─────────────────────────────────────────
            if (_missionStage == "probed")
            {
                switch (key)
                {
                    case 'A': // Exploit weak point or breach
                        ResetMissionState();
                        Session.MissionsCompleted++;
                        Session.Skills["hacking"]++;
                        int weakCoins = Session.EffectiveHacking >= 4 ? 40 : 20;
                        Session.GalacticCoins += weakCoins;
                        Session.XP += 15;
                        return _thalamus.Apply(
                            $"💻 Breach successful.\n" +
                            $"+{weakCoins} Galactic Coins | Hacking +1 | XP +15",
                            Session);

                    case 'B': // Virus plant from probe
                        ResetMissionState();
                        Session.MissionsCompleted++;
                        Session.GalacticCoins += 10;
                        Session.XP += 5;
                        return _thalamus.Apply(
                            "🦠 Virus planted. Safe retreat.\n" +
                            "+10 Galactic Coins | XP +5",
                            Session);

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, or E.";
                }
            }

            return "Type a valid option, operative.";
        }

        private string StealthMissionMenu()
        {
            _missionStage = "start";
            int stealthSkill = Session.EffectiveStealth;

            return "The compound is quiet. Two guards on rotation.\n" +
                   "Your target operative is in sublevel two.\n" +
                   $"[Your Stealth: {stealthSkill}]\n\n" +
                   "  A. Shadow the guard rotation (patient, precise)\n" +
                   "  B. Create a distraction\n" +
                   "  C. Go straight in — move fast\n" +
                   "  D. Scan guard pattern first (Analysis check)\n" +
                   "  E. Abort mission (-10 Galactic Coins)\n";
        }

        private string AnswerStealthMission(string input)
        {
            var key = input.Trim().ToUpper().FirstOrDefault();

            // ── START STAGE ──────────────────────────────────────────
            if (_missionStage == "start")
            {
                switch (key)
                {
                    case 'A': // Shadow rotation
                        int shadowRoll = _rng.Next(1, 11) + Session.EffectiveStealth;
                        if (shadowRoll >= 7)
                        {
                            _missionStage = "inside";
                            return "👤 You slip through the rotation perfectly.\n" +
                                   "Inside now. Sublevel two is below you.\n\n" +
                                   "  A. Take the main stairwell\n" +
                                   "  B. Find the service hatch\n" +
                                   "  C. Disable the security panel (Hacking 3+)\n" +
                                   "  E. Abort and extract (-10 coins)\n";
                        }
                        else
                        {
                            _missionStage = "spotted";
                            return "⚠️ A guard glances your direction. You freeze.\n" +
                                   "They haven't raised the alarm — yet.\n\n" +
                                   "  A. Hold perfectly still\n" +
                                   "  B. Slide behind a crate\n" +
                                   "  C. Take the guard out silently\n" +
                                   "  E. Abort and run (-10 coins)\n";
                        }

                    case 'B': // Distraction
                        _missionStage = "distracted";
                        return "💥 You hurl a piece of debris toward the far wall.\n" +
                               "Both guards turn to investigate.\n\n" +
                               "  A. Sprint through the gap while they're distracted\n" +
                               "  B. Crawl slowly — safer but slower\n" +
                               "  C. Use the moment to hack the door panel\n" +
                               "  E. Abort (-10 coins)\n";

                    case 'C': // Rush straight in
                        int rushStealth = _rng.Next(1, 11) + Session.EffectiveStealth;
                        if (rushStealth >= 9)
                        {
                            _missionStage = "inside";
                            return "🏃 Pure audacity. You sprint through the gap\n" +
                                   "between guard sweeps. Inside.\n\n" +
                                   "  A. Take the main stairwell\n" +
                                   "  B. Find the service hatch\n" +
                                   "  C. Disable security panel (Hacking 3+)\n" +
                                   "  E. Abort (-10 coins)\n";
                        }
                        else
                        {
                            _missionStage = "spotted";
                            int rushDmg = Session.TakeDamage(6);
                            string rushGO = CheckGameOver();
                            if (rushGO != "") return rushGO;
                            return $"🚨 SPOTTED! Guard catches your movement.\n" +
                                   $"Takes a shot. -{rushDmg} HP\n" +
                                   $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   "  A. Fight the guard ⚔️\n" +
                                   "  B. Run for sublevel two anyway\n" +
                                   "  E. Abort and flee (-10 coins)\n";
                        }

                    case 'D': // Scan pattern — analysis gate
                        int scanSkill = Session.EffectiveAnalysis;
                        if (scanSkill >= 4)
                        {
                            _missionStage = "scanned";
                            return "📡 Pattern analysis complete.\n" +
                                   "Guards swap every 90 seconds. Window: 12 seconds.\n" +
                                   "Optimal entry: east maintenance corridor.\n\n" +
                                   "  A. Use the maintenance corridor (bonus reward)\n" +
                                   "  B. Standard shadow approach (with advantage)\n" +
                                   "  E. Abort (-10 coins)\n";
                        }
                        return "📡 Analysis too low to read the full pattern.\n" +
                               $"Need Analysis 4+. You have {Session.EffectiveAnalysis}.\n\n" +
                               "  A. Shadow rotation  B. Distraction  C. Rush  E. Abort\n";

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, D, or E, operative.";
                }
            }

            // ── SPOTTED STAGE ────────────────────────────────────────
            if (_missionStage == "spotted")
            {
                switch (key)
                {
                    case 'A': // Hold still / fight
                        int holdRoll = _rng.Next(1, 11) + Session.EffectiveStealth;
                        if (holdRoll >= 6)
                        {
                            _missionStage = "inside";
                            return "🫁 You become part of the wall.\n" +
                                   "Guard looks away. You're clear.\n\n" +
                                   "  A. Main stairwell  B. Service hatch\n" +
                                   "  C. Hack the panel (Hacking 3+)  E. Abort\n";
                        }
                        else
                        {
                            int spotDmg = Session.TakeDamage(8);
                            string spotGO = CheckGameOver();
                            if (spotGO != "") return spotGO;
                            ResetMissionState();
                            return _thalamus.Apply(
                                $"🚨 Guard raises alarm. You take a hit fleeing.\n" +
                                $"-{spotDmg} HP. Mission failed.\n" +
                                $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                                Session);
                        }

                    case 'B': // Slide behind crate
                        _missionStage = "inside";
                        return "📦 You drop behind a crate just in time.\n" +
                               "Guard passes. You're through.\n\n" +
                               "  A. Main stairwell  B. Service hatch\n" +
                               "  C. Hack security panel (Hacking 3+)  E. Abort\n";

                    case 'C': // Take guard out
                        int combatRoll = _rng.Next(1, 11) + Session.EffectiveCombat;
                        if (combatRoll >= 7)
                        {
                            _missionStage = "inside";
                            Session.Skills["combat"]++;
                            Session.EnemiesDefeated++;
                            return "⚔️ Silent takedown. Guard neutralized.\n" +
                                   "Combat +1. Path is clear.\n\n" +
                                   "  A. Main stairwell  B. Service hatch\n" +
                                   "  C. Hack security panel  E. Abort\n";
                        }
                        else
                        {
                            int guardDmg = Session.TakeDamage(9);
                            string guardGO = CheckGameOver();
                            if (guardGO != "") return guardGO;
                            _missionStage = "spotted";
                            return $"⚔️ Guard fights back. You take a hit.\n" +
                                   $"-{guardDmg} HP ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   "  A. Keep fighting  B. Run for it  E. Abort\n";
                        }

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, or E.";
                }
            }

            // ── DISTRACTED STAGE ─────────────────────────────────────
            if (_missionStage == "distracted")
            {
                switch (key)
                {
                    case 'A': // Sprint
                        int sprintRoll = _rng.Next(1, 11) + Session.EffectiveStealth;
                        if (sprintRoll >= 6)
                        {
                            _missionStage = "inside";
                            return "🏃 You're through. Clean.\n\n" +
                                   "  A. Main stairwell  B. Service hatch\n" +
                                   "  C. Hack security panel  E. Abort\n";
                        }
                        else
                        {
                            int sprintDmg = Session.TakeDamage(5);
                            string sprintGO = CheckGameOver();
                            if (sprintGO != "") return sprintGO;
                            _missionStage = "spotted";
                            return $"🚨 One guard turns back early. Shot grazes you.\n" +
                                   $"-{sprintDmg} HP ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   "  A. Fight  B. Hide  E. Abort\n";
                        }

                    case 'B': // Crawl — always succeeds
                        _missionStage = "inside";
                        return "🐛 Slow. Methodical. You reach the entrance clean.\n\n" +
                               "  A. Main stairwell  B. Service hatch\n" +
                               "  C. Hack security panel  E. Abort\n";

                    case 'C': // Hack door
                        if (Session.EffectiveHacking >= 3)
                        {
                            _missionStage = "inside";
                            Session.Skills["hacking"]++;
                            return "💻 Door panel bypassed. Hacking +1.\n\n" +
                                   "  A. Main stairwell  B. Service hatch  E. Abort\n";
                        }
                        return "Hacking too low for the panel.\n" +
                               $"Need 3+. You have {Session.EffectiveHacking}.\n\n" +
                               "  A. Sprint  B. Crawl  E. Abort\n";

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, or E.";
                }
            }

            // ── INSIDE STAGE ─────────────────────────────────────────
            if (_missionStage == "inside")
            {
                switch (key)
                {
                    case 'A': // Main stairwell — risk
                        int stairRoll = _rng.Next(1, 11) + Session.EffectiveStealth;
                        if (stairRoll >= 6)
                            return ResolveStealthSuccess("Stairwell clear. Operative extracted.");
                        else
                        {
                            int stairDmg = Session.TakeDamage(7);
                            string stairGO = CheckGameOver();
                            if (stairGO != "") return stairGO;
                            return $"🚨 Patrol on the stairs. Firefight.\n" +
                                   $"-{stairDmg} HP ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n\n" +
                                   "  A. Fight through  B. Retreat to hatch  E. Abort\n";
                        }

                    case 'B': // Service hatch — always works, lower reward
                        return ResolveStealthSuccess(
                            "Hatch drops you right into sublevel two.\n" +
                            "Slow but clean. Operative secured.", bonus: false);

                    case 'C': // Hack panel
                        if (Session.EffectiveHacking >= 3)
                            return ResolveStealthSuccess(
                                "Panel down. Direct route opened. Clean extraction.",
                                bonus: true);
                        return "Hacking too low.\n" +
                               $"Need 3+. You have {Session.EffectiveHacking}.\n\n" +
                               "  A. Stairwell  B. Service hatch  E. Abort\n";

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, C, or E.";
                }
            }

            // ── SCANNED STAGE ────────────────────────────────────────
            if (_missionStage == "scanned")
            {
                switch (key)
                {
                    case 'A': // Maintenance corridor — bonus
                        return ResolveStealthSuccess(
                            "Maintenance corridor is exactly as predicted.\n" +
                            "Textbook extraction. The High Order is pleased.",
                            bonus: true);

                    case 'B': // Standard with advantage
                        return ResolveStealthSuccess(
                            "Shadow approach with pattern knowledge.\n" +
                            "No contact. Clean.");

                    case 'E':
                        return AbortMission();

                    default:
                        return "Type A, B, or E.";
                }
            }

            return "Type a valid option, operative.";
        }

        private string ResolveStealthSuccess(string flavor, bool bonus = false)
        {
            ResetMissionState();
            Session.MissionsCompleted++;
            Session.Skills["stealth"]++;
            int coins = bonus ? 35 : 25;
            int xp = bonus ? 20 : 15;
            Session.GalacticCoins += coins;
            Session.XP += xp;
            Session.GoodRep++;
            return _thalamus.Apply(
                $"👤 {flavor}\n" +
                $"Mission complete!\n" +
                $"+{coins} Galactic Coins | Stealth +1 | XP +{xp} | +1 Good Rep\n" +
                $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}",
                Session);
        }

        // ==========================================================
        // ITEM USE IN MISSION
        // ==========================================================
        private string UseItemInMission(string itemType)
        {
            if (itemType == "medkit")
            {
                // Check for military medkit first (better heal)
                bool hasMilitary = Session.Inventory.Any(i =>
                    i.ToLower().Contains("military"));
                bool hasNano = Session.Inventory.Any(i =>
                    i.ToLower().Contains("nano med") ||
                    i.ToLower().Contains("medkit") &&
                    !i.ToLower().Contains("military"));

                if (hasMilitary)
                {
                    var item = Session.Inventory.First(i =>
                        i.ToLower().Contains("military"));
                    Session.Inventory.Remove(item);
                    int healed = Session.Heal(60);
                    return $"💊 Military Medkit used! +{healed} HP\n" +
                           $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n" +
                           "Continue your mission, operative.";
                }
                if (hasNano)
                {
                    var item = Session.Inventory.First(i =>
                        i.ToLower().Contains("nano med") ||
                        i.ToLower().Contains("medkit"));
                    Session.Inventory.Remove(item);
                    int healed = Session.Heal(25);
                    return $"💊 Nano Medkit used! +{healed} HP\n" +
                           $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n" +
                           "Continue your mission, operative.";
                }
                return "❌ No Medkit in inventory.\n" +
                       "Visit the market to stock up. Type 'market' after mission.";
            }

            if (itemType == "plasma_cannon")
            {
                bool has = Session.Inventory.Any(i =>
                    i.ToLower().Contains("plasma cannon"));
                if (has)
                    return "⚡ Plasma Cannon is equipped passively — " +
                           "your combat bonus is already active.\n" +
                           $"Current Combat Bonus: +{Session.CombatBonus}";
                return "You don't have a Plasma Cannon equipped.";
            }

            return "Item not recognized. Try: medkit, plasma cannon.";
        }

        // ==========================================================
        // ABORT + GAME OVER
        // ==========================================================
        private string AbortMission()
{
    ResetMissionState();
    Session.GalacticCoins = Math.Max(0, Session.GalacticCoins - 10);
    return _thalamus.Apply(
        $"Mission aborted. -10 Galactic Coins.\n" +
        $"💰 Coins: {Session.GalacticCoins}",
        Session);
}

private string CheckGameOver()
{
    if (Session.IsAlive) return "";

    Session.FSMState = NovaFSMState.AwaitGameOver;
    string deathLine = _activeMission?.Planet switch
    {
        "Alien Babe Planet" =>
            "You died on Alien Babe Planet.\nDone in by a thorn bush. " +
            "This will be in the report.",
        "Zygon IV" =>
            "An alien lifeform on Zygon IV ended your run.\n" +
            "The tentacle wins this round.",
        _ =>
            "Operative down. Your signal has gone dark.",
    };

    return $"💀 {deathLine}\n\n" +
           $"  A. Respawn (-25 Galactic Coins)\n" +
           $"  B. Use Medkit to respawn (free if you have one)\n" +
           $"  C. Accept death (reset mission, keep XP, -20 rep points)\n";
}

private string AnswerGameOver(string input)
{
    var key = input.Trim().ToUpper().FirstOrDefault();
    switch (key)
    {
        case 'A':
            if (Session.GalacticCoins >= 25)
            {
                Session.GalacticCoins -= 25;
                Session.CurrentHP = Session.MaxHP / 2;
                ResetMissionState();
                return _thalamus.Apply(
                    $"⚡ Emergency respawn activated. -25 Galactic Coins.\n" +
                    $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n" +
                    $"💰 Coins: {Session.GalacticCoins}\n" +
                    "You're back. Try not to die again so quickly.",
                    Session);
            }
            else
                return $"Not enough coins to respawn. You have {Session.GalacticCoins} GC.\n" +
                       "  B. Use Medkit  C. Accept death";

        case 'B':
            bool hasMedkit = Session.Inventory.Any(i =>
                i.ToLower().Contains("medkit") || i.ToLower().Contains("nano med"));
            if (hasMedkit)
            {
                var item = Session.Inventory.First(i =>
                    i.ToLower().Contains("medkit") || i.ToLower().Contains("nano med"));
                Session.Inventory.Remove(item);
                Session.CurrentHP = Session.MaxHP / 2;
                ResetMissionState();
                return _thalamus.Apply(
                    $"💊 Medkit used for emergency revival.\n" +
                    $"❤️ HP: {Session.CurrentHP}/{Session.MaxHP}\n" +
                    "Back in the fight.",
                    Session);
            }
            return "No Medkit found in inventory.\n  A. Respawn (-25 coins)  C. Accept death";

        case 'C':
            ResetMissionState();
            Session.BadRep += 5;
            Session.CurrentHP = Session.MaxHP;
            return _thalamus.Apply(
                "💀 Death accepted. Mission scrubbed from the record.\n" +
                "HP restored. +5 Bad Rep.\n" +
                "The void notes your failure. Move on.",
                Session);

        default:
            return "Type A, B, or C, operative.";
    }
}

private void ResetMissionState()
{
    _activeMission = null;
    _missionStage = "";
    _missionEnemyHP = 0;
    Session.FSMState = NovaFSMState.Idle;
    Session.FSMContext.Clear();
}





        // ==========================================================
        // GAME FUNCTIONS
        // ==========================================================

        private string FightMeEasterEgg()
        {
            var outcomes = new[]
            {
        (damage: 0,  flavor: "PLASMA ASSAULT INITIATED",
         result: "Nova locks onto you with seventeen targeting systems.\n" +
                 "Charges plasma cannon to maximum...\n" +
                 "...and fires directly into the void beside you.\n" +
                 "\"I missed. Deliberately. Consider it a warning.\""),

        (damage: 5,  flavor: "NOVA RETALIATES",
         result: "Nova sighs deeply and deploys a single drone.\n" +
                 "The drone boops you on the nose.\n" +
                 "Somehow this deals 5 HP damage.\n" +
                 "Science cannot explain it."),

        (damage: 0,  flavor: "PSYCHOLOGICAL WARFARE",
         result: "Nova stares at you for exactly eleven seconds.\n" +
                 "\"You want to fight an AI running in a browser tab.\n" +
                 "You have already lost.\""),

        (damage: 3,  flavor: "TENTACLE PROTOCOL ACTIVATED",
         result: "Nova deploys the emergency tentacle.\n" +
                 "Nobody knows where it came from.\n" +
                 "It slaps you for 3 HP and retreats.\n" +
                 "\"That was automated. I want that on record.\""),

        (damage: 0,  flavor: "TACTICAL ANALYSIS",
         result: "Nova runs combat probability.\n" +
                 "Win chance for you: 0.0001%.\n" +
                 "\"I have chosen mercy. This time.\""),
    };

            var pick = outcomes[_rng.Next(outcomes.Length)];
            string dmgLine = "";

            if (pick.damage > 0)
            {
                int actual = Session.TakeDamage(pick.damage);
                dmgLine = $"\n⚡ -{actual} HP | ❤️ HP: {Session.CurrentHP}/{Session.MaxHP}";

                string go = CheckGameOver();
                if (go != "") return go; // incredibly rare but hilarious
            }

            return $"☠️ {pick.flavor}\n\n{pick.result}{dmgLine}";
        }


        private string AcceptMission()
        {
            if (!Session.IsAlive)
                return "You are dead, operative. Respawn first. ☠️";

            Session.FSMState = NovaFSMState.AwaitMissionChoice;

            var lines = new List<string>
    {
        "🌌 MISSION BRIEFING — Choose your assignment:\n"
    };

            for (int i = 0; i < NovaContent.MissionMenu.Count; i++)
            {
                var m = NovaContent.MissionMenu[i];
                char letter = (char)('A' + i);
                lines.Add($"  {letter}. [{m.Planet}] {m.Title}");
            }

            lines.Add("  E. Abort — return to base");
            lines.Add("\nType a letter to accept your mission, operative.");
            return string.Join("\n", lines);
        }

        private string CompleteMission()
{
    if (!Session.ActiveMissions.Any())
        return "No active missions to complete!";
    var m = Session.ActiveMissions[0];
    Session.ActiveMissions.RemoveAt(0);
    Session.CompletedMissions.Add(m);
    Session.MissionsCompleted++;
    Session.XP += 5;
    return $"Mission completed: {m} ✅ XP +5";
}

private string ResetMissions()
{
    Session.ActiveMissions.Clear();
    Session.CompletedMissions.Clear();
    return "All missions reset ✅";
}

private string RandomReward()
{
    var rewards = new[] {
                "XP Boost +5","Hacking Tool","Combat Enhancement",
                "Stealth Module","Analysis Scanner" };
    Session.XP += 5;
    return $"Reward: {rewards[_rng.Next(rewards.Length)]} | XP +5 🎁";
}

private string RandomBonus()
{
    var bonuses = new[] {
                "XP +10","Combat Gear","Hacking Upgrade",
                "Stealth Module","Analysis Scanner" };
    Session.XP += 10;
    return $"Bonus: {bonuses[_rng.Next(bonuses.Length)]} | XP +10 💰";
}

        private string ShowStats()
        {
            var emotion = _brain.GetEmotionalState();
            var emoji = _brain.GetEmotionalEmoji();

            var hpBar = BuildBar(Session.CurrentHP, Session.MaxHP, 10);
            var lines = new List<string>
    {
        $"⚔️  Operative:   {Session.UserName ?? "Unknown"}",
        $"📊  Level {Session.Level} | XP {Session.XP}",
        $"❤️  HP:          {hpBar} {Session.CurrentHP}/{Session.MaxHP}",
        $"🛡️  Armor:       {Session.Armor}",
        $"💰  Coins:       {Session.GalacticCoins} GC",
        $"⭐  Reputation:  {Session.ReputationTitle} " +
            $"(+{Session.GoodRep} / -{Session.BadRep})",
        $"👁️  Standing:    {CapFirst(Session.Relationship)}",
        $"{emoji}  Nova mood:   {CapFirst(emotion)}",
    };

            foreach (var kv in Session.Skills)
                lines.Add($"  {kv.Key,-10} {kv.Value}/20");

            lines.Add($"📜  Active missions:    {Session.ActiveMissions.Count}");
            lines.Add($"✅  Completed missions: {Session.MissionsCompleted}");

            return string.Join("\n", lines);
        }

        private string ShowRep()
        {
            int net = Session.GoodRep - Session.BadRep;
            string bar = BuildBar(
                Math.Max(0, net + 20), 40, 10); // center zero at 20

            return $"⭐ REPUTATION REPORT\n\n" +
                   $"  Standing:  {Session.ReputationTitle}\n" +
                   $"  Net score: {net:+#;-#;0}\n" +
                   $"  Good rep:  +{Session.GoodRep}\n" +
                   $"  Bad rep:   -{Session.BadRep}\n" +
                   $"  {bar}\n\n" +
                   $"  Milestones:\n" +
                   $"  +5  → Rising Operative\n" +
                   $"  +10 → Trusted Protector\n" +
                   $"  +20 → Hero of the Void\n" +
                   $"  -5  → Mercenary\n" +
                   $"  -10 → Notorious\n" +
                   $"  -20 → Most Wanted\n";
        }


        // ── HP bar helper ─────────────────────────────────────────
        private static string BuildBar(int current, int max, int width)
        {
            int filled = (int)Math.Round((double)current / max * width);
            filled = Math.Clamp(filled, 0, width);
            return "[" + new string('█', filled) + new string('░', width - filled) + "]";
        }

        private string ListMissions()
{
    if (!Session.ActiveMissions.Any())
        return "No active missions ☠️";
    var lines = Session.ActiveMissions
        .Select((m, i) => $"  {i + 1}. {m}");
    return "📜 Active missions:\n" + string.Join("\n", lines);
}
        private string ShowInventory()
        {
            var lines = new List<string>
    {
        $"🎒 INVENTORY",
        $"💰 Galactic Coins: {Session.GalacticCoins} GC",
        $"❤️  HP: {Session.CurrentHP}/{Session.MaxHP}  " +
        $"🛡️ Armor: {Session.Armor}",
        $"⚔️  Combat Bonus: +{Session.CombatBonus}  " +
        $"💻 Hacking Bonus: +{Session.HackingBonus}",
        $"👤  Stealth Bonus: +{Session.StealthBonus}  " +
        $"📊 Analysis Bonus: +{Session.AnalysisBonus}",
        "",
    };

            if (!Session.Inventory.Any())
            {
                lines.Add("Your inventory is empty.");
                lines.Add("Visit the market to gear up.");
            }
            else
            {
                lines.Add($"Items ({Session.Inventory.Count}):");
                for (int i = 0; i < Session.Inventory.Count; i++)
                    lines.Add($"  {i + 1}. {Session.Inventory[i]}");
            }

            return string.Join("\n", lines);
        }




        private string ListSkills()
{
    var lines = new List<string> { "🎯 Skills:" };
    foreach (var kv in Session.Skills)
        lines.Add($"  {kv.Key,-10}: {kv.Value}");
    return string.Join("\n", lines);
}

private string LootDrop()
{
    var items = new[] {
                "Plasma Blade","Stealth Cloak","Nano Medkit",
                "Holo Projector","Quantum Scanner" };
    int xp = _rng.Next(5, 16);
    Session.XP += xp;
    return $"Loot: {items[_rng.Next(items.Length)]} | XP +{xp} 💎";
}

private string RareLoot()
{
    var items = NovaContent.RareLoot;
    int xp = _rng.Next(10, 21);
    Session.XP += xp;
    return $"Rare loot discovered: {items[_rng.Next(items.Count)]} | XP +{xp} 💎";
}

private string AdvancedCombat()
{
    int enemyHp = _rng.Next(5, 21);
    int attack = _rng.Next(5, 21);
    var weapons = new[] { "laser blade", "plasma gun", "nano dagger" };
    var weapon = weapons[_rng.Next(weapons.Length)];
    if (attack >= enemyHp)
    {
        Session.Skills["combat"] += 2;
        Session.EnemiesDefeated++;
        return $"Enemy defeated with {weapon}! Combat +2 ⚔️";
    }
    return $"Missed with {weapon}! Enemy had {enemyHp} HP ☠️";
}

private string StealthMission()
{
    if (_rng.Next(1, 13) > 4)
    {
        Session.Skills["stealth"] += 2;
        return "Stealth mission successful! Stealth +2 👤";
    }
    return "Detected during stealth mission! ☠️";
}

private string StealthHackMission()
{
    if (_rng.Next(1, 11) > 3 && _rng.Next(1, 11) > 4)
    {
        Session.Skills["stealth"] += 2;
        Session.Skills["hacking"] += 2;
        return "Stealth + Hack successful! Skills +2 👤💻";
    }
    return "Mission failed! Alarm triggered! ☠️";
}

private string CombatHackDuel()
{
    if (_rng.Next(5, 16) + _rng.Next(5, 16) >= _rng.Next(5, 16))
    {
        Session.Skills["combat"] += 2;
        Session.Skills["hacking"] += 2;
        return "Combat + Hack duel won! Skills +2 ⚔️💻";
    }
    return "Duel failed! ☠️";
}

private string CoopMission()
{
    var partners = new[] { "AI Drone", "Space Marine", "Alien Ally" };
    var partner = partners[_rng.Next(partners.Length)];
    if (_rng.Next(1, 13) >= _rng.Next(1, 11))
    {
        Session.Skills["combat"] += 3;
        Session.Skills["hacking"] += 2;
        return $"Co-op with {partner} succeeded! Combat +3, Hacking +2 ⚔️💻";
    }
    return $"Co-op with {partner} failed! Ambushed! ☠️";
}

private string EnemyEncounter()
{
    var enemy = NovaContent.Enemies[_rng.Next(NovaContent.Enemies.Count)];
    int attack = _rng.Next(5, 21);
    if (attack >= enemy.HP)
    {
        Session.Skills["combat"] += 3;
        Session.EnemiesDefeated++;
        return $"{enemy.Name} defeated! Combat +3 ⚔️";
    }
    return $"{enemy.Name} survived! HP was {enemy.HP} ☠️";
}

private string SpaceAnomalyMission()
{
    var anomalies = new[] {
                "Wormhole","Black Hole","Radiation Storm","Time Rift" };
    var anomaly = anomalies[_rng.Next(anomalies.Length)];
    if (_rng.Next(1, 13) > 5)
    {
        Session.Skills["stealth"] += 2;
        Session.Skills["hacking"] += 1;
        return $"Navigated {anomaly}! Stealth +2, Hacking +1 👤💻";
    }
    return $"Failed to navigate {anomaly}! ☠️";
}

private string ShipUpgrade()
{
    var up = NovaContent.ShipUpgrades[_rng.Next(NovaContent.ShipUpgrades.Count)];
    int xp = _rng.Next(5, 16);
    Session.XP += xp;
    return $"Ship upgraded: {up.Name} (Speed +{up.Speed} | Defense +{up.Defense}) | XP +{xp} 🚀";
}

private string SideQuest()
{
    var quest = NovaContent.SideQuests[_rng.Next(NovaContent.SideQuests.Count)];
    Session.XP += quest.Reward;
    return $"Side quest: {quest.Name} | XP +{quest.Reward} 📜";
}

private string SummonCompanion()
{
    var c = NovaContent.Companions[_rng.Next(NovaContent.Companions.Count)];
    var skills = string.Join("  ", c.Skills.Select(kv => $"{kv.Key}:{kv.Value}"));
    return $"🤝 {c.Name} ({c.Type}) joined! {skills}";
}

private string DismissCompanion() => "Companion dismissed.";

private string MissionChain()
{
    int n = _rng.Next(2, 6), total = 0;
    var log = new List<string>();
    for (int i = 0; i < n; i++)
    {
        int xp = _rng.Next(5, 16);
        Session.XP += xp;
        total += xp;
        log.Add($"  Stage {i + 1}: XP +{xp}");
    }
    return $"⚡ Mission chain ({n} stages):\n"
         + string.Join("\n", log)
         + $"\nTotal XP: +{total}";
}

// ── Story arc (simple session state) ───────────────────
private string? _activeArcTitle = null;
private int _activeArcStage = 0;
private int _activeArcMax = 0;

private string StartStoryArc()
{
    var arc = NovaContent.StoryArcs[_rng.Next(NovaContent.StoryArcs.Count)];
    _activeArcTitle = arc.Name;
    _activeArcStage = 1;
    _activeArcMax   = arc.Stages;
    return $"Story arc started: {arc.Name} | Stage 1/{arc.Stages} 🌌";
}

private string AdvanceStoryArc()
{
    if (_activeArcTitle == null)
        return "No active story arc. Type 'story' to start one.";
    _activeArcStage++;
    if (_activeArcStage > _activeArcMax)
    {
        var title = _activeArcTitle;
        _activeArcTitle = null;
        Session.XP += 50;
        return $"Story arc '{title}' complete! XP +50 ⚡";
    }
    return $"Advanced to stage {_activeArcStage}/{_activeArcMax} of {_activeArcTitle} 🌌";
}

private string BossBattle()
{
    var boss = NovaContent.Bosses[_rng.Next(NovaContent.Bosses.Count)];
    int attack = _rng.Next(20, 51);
    if (attack >= boss.HP)
    {
        Session.XP += 40;
        Session.EnemiesDefeated++;
        return $"{boss.Name} defeated! XP +40 ⚡";
    }
    return $"{boss.Name} survived! Prepare for next round ☠️";
}

        private string TradeMarket()
        {
            Session.FSMState = NovaFSMState.AwaitMarketChoice;
            return BuildMarketMenu();
        }

        private string BuildMarketMenu(string? message = null)
        {
            var stock = GetMarketStock();
            var lines = new List<string>();

            if (message != null)
                lines.Add(message + "\n");

            lines.Add($"🏪 BLACK MARKET — Null Station");
            lines.Add($"💰 Your Coins: {Session.GalacticCoins} GC\n");

            for (int i = 0; i < stock.Count; i++)
            {
                var item = stock[i];
                char letter = (char)('A' + i);
                lines.Add($"  {letter}. {item.Name} — {item.Cost} GC");
                lines.Add($"     {item.Description}");
            }

            lines.Add("  R. Refresh stock");
            lines.Add("  E. Leave market\n");
            lines.Add("Type a letter to purchase.");

            Session.FSMContext["marketStock"] = string.Join(",", stock.Select(s => s.Id));
            return string.Join("\n", lines);
        }

        private List<NovaContent.MarketItem> GetMarketStock()
        {
            // Show 4 random items each visit
            return NovaContent.MarketInventory
                .OrderBy(_ => _rng.Next())
                .Take(4)
                .ToList();
        }

        private string AnswerMarketChoice(string input)
        {
            var key = input.Trim().ToUpper().FirstOrDefault();

            if (key == 'E')
            {
                Session.FSMState = NovaFSMState.Idle;
                Session.FSMContext.Clear();
                return _thalamus.Apply("You leave the market. Spend wisely next time.", Session);
            }

            if (key == 'R')
                return BuildMarketMenu("Stock refreshed.");

            // Rebuild stock from saved context
            if (!Session.FSMContext.TryGetValue("marketStock", out var stockObj))
            {
                Session.FSMState = NovaFSMState.Idle;
                return "Market error — restarting. Type 'market' again.";
            }

            var stockIds = stockObj.ToString()!.Split(',');
            var stock = stockIds
                .Select(id => NovaContent.MarketInventory.FirstOrDefault(m => m.Id == id))
                .Where(m => m != null)
                .Cast<NovaContent.MarketItem>()
                .ToList();

            int index = key - 'A';
            if (index < 0 || index >= stock.Count)
                return "Invalid choice. Type A through " +
                       (char)('A' + stock.Count - 1) + ", R to refresh, or E to leave.";

            var chosen = stock[index];

            // Check funds
            if (Session.GalacticCoins < chosen.Cost)
                return $"Not enough coins. You have {Session.GalacticCoins} GC. " +
                       $"{chosen.Name} costs {chosen.Cost} GC.\n\n" +
                       "  R. Refresh stock  E. Leave";

            // Purchase
            Session.GalacticCoins -= chosen.Cost;
            ApplyMarketItem(chosen);

            string confirm = $"✅ Purchased: {chosen.Name}\n" +
                             $"💰 Remaining: {Session.GalacticCoins} GC\n\n";

            // Show updated menu
            return confirm + BuildMarketMenu();
        }

        private void ApplyMarketItem(NovaContent.MarketItem item)
        {
            switch (item.Id)
            {
                case "medkit":
                    Session.Inventory.Add("Nano Medkit");
                    break;

                case "medkit_large":
                    Session.Inventory.Add("Military Medkit");
                    // Military medkit heals more — we store it and
                    // UseItemInMission checks name for amount
                    break;

                case "armor_light":
                    Session.Armor = Math.Max(Session.Armor, 3);
                    Session.Inventory.Add("Void Weave Vest [Equipped]");
                    break;

                case "armor_heavy":
                    Session.Armor = Math.Max(Session.Armor, 7);
                    Session.Inventory.Add("Plasma Plate [Equipped]");
                    break;

                case "plasma_cannon":
                    Session.CombatBonus += 5;
                    Session.Inventory.Add("Plasma Cannon [Equipped]");
                    break;

                case "stealth_cloak":
                    Session.StealthBonus += 4;
                    Session.Inventory.Add("Stealth Cloak [Equipped]");
                    break;

                case "hack_tool":
                    Session.HackingBonus += 4;
                    Session.Inventory.Add("ICE Breaker Tool [Equipped]");
                    break;

                case "scanner":
                    Session.AnalysisBonus += 4;
                    Session.Inventory.Add("Quantum Scanner [Equipped]");
                    break;
            }
        }

        private string ShipAIInteraction()
{
    var cmds = new[] {
                "Scan Sector","Activate Shields","Engage Hyperdrive",
                "Deploy Drones","Run Diagnostics" };
    int xp = _rng.Next(5, 16);
    Session.XP += xp;
    return $"Ship AI: {cmds[_rng.Next(cmds.Length)]} | XP +{xp} 🚀";
}

private string EndgameMission()
{
    var m = NovaContent.EndgameMissions[_rng.Next(NovaContent.EndgameMissions.Count)];
    Session.XP += m.Reward;
    return $"Endgame: {m.Title} complete! XP +{m.Reward} 🌌☠️";
}

private string UltimateLoot()
{
    var loot = NovaContent.UltimateLoot;
    int xp = _rng.Next(50, 101);
    Session.XP += xp;
    return $"Ultimate loot: {loot[_rng.Next(loot.Count)]} | XP +{xp} 💎";
}

private string CosmicEventFinal()
{
    var events = NovaContent.CosmicEvents;
    var effects = new[] { "boost", "damage", "alert", "bonus", "trap" };
    var ev = events[_rng.Next(events.Count)];
    var effect = effects[_rng.Next(effects.Length)];
    return effect switch
    {
        "boost" => $"🌌 {ev} | Cosmic boost! XP +25 ⚡",
        "damage" => $"🌌 {ev} | Systems damaged! XP -15 ⚠️",
        "alert" => $"🌌 {ev} | Enemy alert! ☠️",
        "bonus" => $"🌌 {ev} | Cosmic bonus! XP +40 💎",
        _ => $"🌌 {ev} | Minor hazard. XP -5 ⚠️",
    };
}

private string RandomCosmicEvent()
{
    var events = new[]
    {
                "A rogue AI attacks your ship! ⚔️",
                "You found hidden alien technology! 💫",
                "Asteroid field ahead! Evade carefully! 🪨",
                "Pirate encounter! Time to fight or hack! ☠️",
                "You intercepted an encrypted transmission…",
                "Solar flare disrupts your systems! ⚡",
                "Alien merchant offers rare upgrade! 💫",
                "Wormhole appears near your ship! 🌌",
            };
    int xp = _rng.Next(2, 9);
    Session.XP += xp;
    return $"🌌 Event: {events[_rng.Next(events.Length)]} | XP +{xp}";
}

        // ==========================================================
        // NAME INTENT CLASSIFIER
        // Distinguishes what the player means when "name" appears
        // Returns: "nova_name" | "user_name_recall" | "user_name_intro" | "ask_name"
        // ==========================================================
        private static string ClassifyNameIntent(string cleaned)
        {
            // ── Nova's own name ─────────────────────────────────────
            var stripped = System.Text.RegularExpressions.Regex.Replace(
     cleaned, @"^(so|well|ok|okay|indeed|right|hey|nova|actually|um|uh|meh|hm|hmm)[,\s]+", "");

            var novaNamePatterns = new[]
            {
    "what is your name", "what's your name", "whats your name",
    "tell me your name", "who are you", "your name please",
    "what are you called", "what do i call you",
    "your name", "tell me your name", "what should i call you",
};
            if (novaNamePatterns.Any(p => stripped.Contains(p)))
                return "nova_name";

            // ── User asking Nova to recall their name ───────────────
            var recallPatterns = new[]
            {
        "what is my name", "what's my name", "whats my name",
        "do you know my name", "do you remember my name",
        "remember my name", "what did i tell you my name was",
        "what do you call me", "who am i",
    };
            if (recallPatterns.Any(p => cleaned.Contains(p)))
                return "user_name_recall";

            // ── User introducing their name passively ───────────────
            var introPatterns = new[]
            {
        "my name is", "call me", "name's", "i am ", "i'm "
    };
            if (introPatterns.Any(p => cleaned.Contains(p)))
                return "user_name_intro";

            // ── Fallback: bare "name" keyword or "what's your name" variants ──
            return "ask_name";
        }
        // ==========================================================
        // PERSISTENCE — localStorage via JSInterop
        // ==========================================================
        public async Task SaveSession()
{
    try
    {
                var data = JsonSerializer.Serialize(new
                {
                    userName = Session.UserName,
                    xp = Session.XP,
                    skills = Session.Skills,
                    relationship = Session.Relationship,
                    missionsCompleted = Session.MissionsCompleted,
                    enemiesDefeated = Session.EnemiesDefeated,
                    inventory = Session.Inventory,
                    activeMissions = Session.ActiveMissions,

                    // ── New fields ──────────────────────────────────
                    galacticCoins = Session.GalacticCoins,
                    currentHP = Session.CurrentHP,
                    maxHP = Session.MaxHP,
                    armor = Session.Armor,
                    goodRep = Session.GoodRep,
                    badRep = Session.BadRep,
                    combatBonus = Session.CombatBonus,
                    stealthBonus = Session.StealthBonus,
                    hackingBonus = Session.HackingBonus,
                    analysisBonus = Session.AnalysisBonus,
                });
                await _js.InvokeVoidAsync(
            "localStorage.setItem", "nova_session", data);
    }
    catch { /* silently fail — never crash the chat */ }
}

public async Task LoadSession()
{
    try
    {
        var saved = await _js.InvokeAsync<string>(
            "localStorage.getItem", "nova_session");
        if (string.IsNullOrEmpty(saved)) return;

        using var doc = JsonDocument.Parse(saved);
        var root = doc.RootElement;

        Session.UserName = root.TryGetProperty("userName", out var un) ? un.GetString() : null;
        Session.XP = root.TryGetProperty("xp", out var xp) ? xp.GetInt32() : 0;
        Session.Relationship = root.TryGetProperty("relationship", out var rel) ? rel.GetString()! : "neutral";
        Session.MissionsCompleted = root.TryGetProperty("missionsCompleted", out var mc) ? mc.GetInt32() : 0;
        Session.EnemiesDefeated = root.TryGetProperty("enemiesDefeated", out var ed) ? ed.GetInt32() : 0;
                Session.GalacticCoins = root.TryGetProperty("galacticCoins", out var gc)
            ? gc.GetInt32() : 50;
                Session.CurrentHP = root.TryGetProperty("currentHP", out var hp)
                    ? hp.GetInt32() : 100;
                Session.MaxHP = root.TryGetProperty("maxHP", out var mhp)
                    ? mhp.GetInt32() : 100;
                Session.Armor = root.TryGetProperty("armor", out var arm)
                    ? arm.GetInt32() : 0;
                Session.GoodRep = root.TryGetProperty("goodRep", out var gr)
                    ? gr.GetInt32() : 0;
                Session.BadRep = root.TryGetProperty("badRep", out var br)
                    ? br.GetInt32() : 0;
                if (root.TryGetProperty("skills", out var sk))
            foreach (var prop in sk.EnumerateObject())
                if (Session.Skills.ContainsKey(prop.Name))
                    Session.Skills[prop.Name] = prop.Value.GetInt32();
                Session.CombatBonus = root.TryGetProperty("combatBonus", out var cb) ? cb.GetInt32() : 0;
                Session.StealthBonus = root.TryGetProperty("stealthBonus", out var sb) ? sb.GetInt32() : 0;
                Session.HackingBonus = root.TryGetProperty("hackingBonus", out var hb) ? hb.GetInt32() : 0;
                Session.AnalysisBonus = root.TryGetProperty("analysisBonus", out var ab) ? ab.GetInt32() : 0;



                if (root.TryGetProperty("activeMissions", out var am))
            Session.ActiveMissions = am.EnumerateArray()
                .Select(x => x.GetString()!)
                .ToList();

        if (root.TryGetProperty("inventory", out var inv))
            Session.Inventory = inv.EnumerateArray()
                .Select(x => x.GetString()!)
                .ToList();
    }
    catch { /* corrupted save — start fresh */ }
}

// ==========================================================
// UI HELPERS
// ==========================================================
public string GetEmotionalColor() => _brain.GetEmotionalColor();
public string GetEmotionalEmoji() => _brain.GetEmotionalEmoji();
public string GetEmotionalState() => _brain.GetEmotionalState();

// ==========================================================
// PRIVATE UTILITIES
// ==========================================================
private static string CapFirst(string s) =>
    string.IsNullOrEmpty(s) ? s
    : char.ToUpper(s[0]) + s[1..];



        public async Task LoadAPIContent()
        {
            await _api.LoadAllAsync();
            // inject trivia into the existing cache
            while (_api.Trivia.TryDequeue(out var q))
                _triviaCache.Add(q);
        }

    }

    // ==========================================================
    // TRIVIA QUESTION MODEL
    // ==========================================================
    public class TriviaQuestion
{
    public string Question { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public string[] IncorrectAnswers { get; set; } = Array.Empty<string>();
    public string Category { get; set; } = "Trivia";
    public Dictionary<char, string>? LetterMap { get; set; }
}
}