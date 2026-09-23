// ==========================================================
// NovaArcuateFasciculus.cs — Nova Adeptus Language Bridge
//
// Inspired by the Arcuate Fasciculus of the human brain:
//   - A white matter tract connecting Wernicke's area
//     (temporal lobe — comprehension) to Broca's area
//     (frontal lobe — production)
//   - Carries STRUCTURED LINGUISTIC UNITS between regions
//   - Not raw sound, not finished sentences — grammar pieces
//   - Damage → Conduction Aphasia: understands words, can speak,
//     but cannot relay information between the two systems
//     (repetition is broken — hearing ≠ saying)
//
// In Nova's architecture:
//   - RECEIVES: tagged tokens from NovaWernicke.cs
//   - IDENTIFIES: intent from token sequence
//   - GATHERS: context from NovaOccipitalCortex + Brain + Session
//   - SELECTS: vocabulary slots via O(log n) binary search
//     on tables from NovaArcuateFasciculusDataCore.py
//   - PRODUCES: SentenceBlueprint → handed to NovaBroca.cs
//
// The three target intents for Phase 1:
//   1. state_query_self    → "How are you [today]?"
//   2. identity_query_what → "What are you?"
//   3. identity_query_who  → "Who are you?"
//
// FALLBACK BEHAVIOR:
//   If intent is not recognized, or Python assembly returns null,
//   the Fasciculus returns null and NovaCortex falls through
//   to the existing Thalamus pools. Nothing breaks.
//
// Called by: NovaCortex.cs (in Respond(), before Thalamus)
// Calls:     NovaWernicke.cs (TagTokens)
//            NovaOccipitalCortex.cs (GetFasciculusSnapshot)
//            NovaBroca.cs (AssembleFromBlueprint)
//            IJSRuntime (Python bridge via pyodide)
// ==========================================================

using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NovaAdeptusLibrary
{
    // ==========================================================
    // TOKEN TAGS
    // Every word in an input sentence gets one of these.
    // This is the Part-of-Speech (POS) tagging stage.
    // Real LLMs use transformer-based POS tagging.
    // Nova uses a rule-based tagger — fast and transparent.
    // ==========================================================
    public enum TokenTag
    {
        Unknown,
        QuestionWord,    // how, what, who, where, when, why
        LinkingVerb,     // are, is, am, were, was, be, been
        ActionVerb,      // do, does, did, run, fight, hack
        PronounSelf,     // you, your (directed at Nova)
        PronounFirst,    // I, me, my (the user)
        PronounThird,    // he, she, it, they
        TimeWord,        // today, tonight, now, tomorrow, this morning
        NounGeneral,     // cat, ship, planet, mission
        NounAbstract,    // void, space, existence, reality
        Adjective,       // good, bad, real, alive, online
        Adverb,          // really, very, just, currently
        Article,         // a, an, the
        Conjunction,     // and, or, but, so
        Punctuation,     // ? . ! ,
        NameWord,        // proper nouns — detected by context
    }

    // ==========================================================
    // TAGGED TOKEN
    // A word paired with its grammatical classification
    // and any semantic metadata Wernicke could attach
    // ==========================================================
    public class TaggedToken
    {
        public string Word { get; set; } = "";
        public TokenTag Tag { get; set; } = TokenTag.Unknown;
        public string SemanticDomain { get; set; } = "general";
        public bool IsKnownWord { get; set; } = false;

        public override string ToString() =>
            $"[{Word}:{Tag}:{SemanticDomain}]";
    }

    // ==========================================================
    // SENTENCE BLUEPRINT
    // The structured intermediate representation that travels
    // from the Fasciculus to Broca for final assembly.
    // This is what the arcuate fasciculus actually carries —
    // not words, not sounds, but STRUCTURE.
    // ==========================================================
    public class SentenceBlueprint
    {
        // What kind of response to build
        public string Intent { get; set; } = "unknown";

        // Emotional tone from NovaBrain
        public string Tone { get; set; } = "calm";

        // Time-of-day from OccipitalCortex
        public string TimeLabel { get; set; } = "";
        public string TimeFormatted { get; set; } = "";
        public bool HasTimeContext { get; set; } = false;

        // Relationship tier from NovaSession
        public string Relationship { get; set; } = "neutral";

        // Player name if known
        public string? PlayerName { get; set; } = null;

        // Filled grammar slots — the vocabulary selections
        public string SlotVerb { get; set; } = "";
        public string SlotTimeAck { get; set; } = "";
        public string SlotIdentityCore { get; set; } = "";
        public string SlotBodyStatus { get; set; } = "";
        public string SlotLocation { get; set; } = "";
        public string SlotCloser { get; set; } = "";

        // The assembled sentence (filled by Broca)
        public string AssembledSentence { get; set; } = "";

        // Whether this blueprint was fulfilled
        public bool IsValid { get; set; } = false;

        // Debug path — which assembly route was taken
        public string AssemblyPath { get; set; } = "";
    }

    // ==========================================================
    // NOVA ARCUATE FASCICULUS — MAIN BRIDGE ENGINE
    // ==========================================================
    public class NovaArcuateFasciculus
    {
        // ── Dependencies ───────────────────────────────────────
        private readonly NovaOccipitalCortex _occipital;
        private readonly IJSRuntime _js;
        private static readonly Random _rng = new();

        // ── Loaded grammar tables (from Python) ────────────────
        private bool _tablesLoaded = false;
        // We call Python directly for assembly rather than
        // maintaining duplicate tables in C# — single source of truth

        // ==========================================================
        // CONSTRUCTOR
        // ==========================================================
        public NovaArcuateFasciculus(
            NovaOccipitalCortex occipital,
            IJSRuntime js)
        {
            _occipital = occipital;
            _js = js;
        }

        // ==========================================================
        // MAIN ENTRY — Process()
        // Called by NovaCortex before falling through to Thalamus.
        //
        // Returns: response string if intent matched
        //          null if intent not recognized (fall through)
        //
        // This is the "conduction" — carrying a structured signal
        // from comprehension (Wernicke) to production (Broca).
        // ==========================================================
        public async Task<string?> Process(
            string rawInput,
            NovaSession session,
            string emotionalTone)
        {
            try
            {
                // ── Step 1: Tag tokens (Wernicke's work) ────────
                var tokens = TagTokens(rawInput);
                if (!tokens.Any()) return null;

                // ── Step 2: Identify intent ──────────────────────
                var intent = ClassifyIntent(tokens, rawInput.ToLower().Trim());
                if (intent == "unknown") return null;

                // ── Step 3: Gather context (OccipitalCortex) ────
                var (timeLabel, timeFormatted, _, _, _, bodyStatus)
                    = _occipital.GetFasciculusSnapshot();

                bool hasTime = tokens.Any(t => t.Tag == TokenTag.TimeWord);

                // ── Step 4: Build blueprint ──────────────────────
                var blueprint = new SentenceBlueprint
                {
                    Intent = intent,
                    Tone = emotionalTone,
                    TimeLabel = hasTime ? timeLabel : "",
                    TimeFormatted = timeFormatted,
                    HasTimeContext = hasTime,
                    Relationship = session.Relationship,
                    PlayerName = session.UserName,
                    IsValid = false,
                    AssemblyPath = $"Fasciculus→{intent}",
                };

                // ── Step 5: Fill slots via Python O(log n) ───────
                var assembled = await CallPythonAssembly(
                    intent, emotionalTone, timeLabel,
                    session.Relationship, session.UserName ?? "");

                if (!string.IsNullOrWhiteSpace(assembled))
                {
                    blueprint.AssembledSentence = assembled;
                    blueprint.IsValid = true;
                    return assembled;
                }

                // ── Step 6: C# fallback assembly ─────────────────
                // Python didn't return anything — assemble from
                // the C# side using OccipitalCortex directly
                var fallback = BuildCSharpFallback(blueprint, session);
                return fallback;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[ArcuateFasciculus] Error: {ex.Message}");
                return null; // always fall through cleanly
            }
        }

        // ==========================================================
        // TOKEN TAGGER
        // Rule-based Part-of-Speech tagging.
        // Fast, transparent, debuggable — unlike a black box model.
        //
        // Processes each word against fixed word sets.
        // O(1) lookup per word using HashSet (hashed, not scanned).
        // Overall: O(n) where n = word count — always tiny.
        // ==========================================================

        // ── Word sets for tagging ──────────────────────────────
        private static readonly HashSet<string> QuestionWords = new()
        {
            "how", "what", "who", "where", "when", "why", "which",
        };

        private static readonly HashSet<string> LinkingVerbs = new()
        {
            "are", "is", "am", "was", "were", "be", "been", "being",
        };

        private static readonly HashSet<string> ActionVerbs = new()
        {
            "do", "does", "did", "run", "fight", "hack", "go",
            "tell", "show", "give", "help", "explain", "say",
        };

        private static readonly HashSet<string> PronounsSelf = new()
        {
            "you", "your", "yours", "yourself",
        };

        private static readonly HashSet<string> PronounsFirst = new()
        {
            "i", "me", "my", "mine", "myself",
        };

        private static readonly HashSet<string> PronounsThird = new()
        {
            "he", "him", "his", "she", "her", "it", "its",
            "they", "them", "their",
        };

        private static readonly HashSet<string> TimeWords = new()
        {
            "today", "tonight", "now", "currently", "right now",
            "this morning", "this afternoon", "this evening",
            "tomorrow", "yesterday", "lately", "recently",
            "always", "never", "usually",
        };

        private static readonly HashSet<string> AbstractNouns = new()
        {
            "void", "space", "existence", "reality", "consciousness",
            "intelligence", "purpose", "meaning", "truth", "life",
        };

        private static readonly HashSet<string> Adjectives = new()
        {
            "good", "bad", "real", "alive", "online", "offline",
            "operational", "functional", "broken", "conscious",
            "sentient", "artificial", "synthetic", "digital",
        };

        private static readonly HashSet<string> Articles = new()
        {
            "a", "an", "the",
        };

        private static readonly HashSet<string> Conjunctions = new()
        {
            "and", "or", "but", "so", "because", "if", "then",
        };

        public List<TaggedToken> TagTokens(string input)
        {
            var result = new List<TaggedToken>();
            var words = input.ToLower()
                .Replace("?", " ?").Replace(".", " .").Replace("!", " !")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                var tag = TagWord(word);
                var domain = DetectWordDomain(word);
                result.Add(new TaggedToken
                {
                    Word = word,
                    Tag = tag,
                    SemanticDomain = domain,
                    IsKnownWord = tag != TokenTag.Unknown,
                });
            }

            return result;
        }

        private static TokenTag TagWord(string word) =>
            word switch
            {
                var w when w is "?" or "." or "!" or "," => TokenTag.Punctuation,
                var w when QuestionWords.Contains(w) => TokenTag.QuestionWord,
                var w when LinkingVerbs.Contains(w) => TokenTag.LinkingVerb,
                var w when ActionVerbs.Contains(w) => TokenTag.ActionVerb,
                var w when PronounsSelf.Contains(w) => TokenTag.PronounSelf,
                var w when PronounsFirst.Contains(w) => TokenTag.PronounFirst,
                var w when PronounsThird.Contains(w) => TokenTag.PronounThird,
                var w when TimeWords.Contains(w) => TokenTag.TimeWord,
                var w when AbstractNouns.Contains(w) => TokenTag.NounAbstract,
                var w when Adjectives.Contains(w) => TokenTag.Adjective,
                var w when Articles.Contains(w) => TokenTag.Article,
                var w when Conjunctions.Contains(w) => TokenTag.Conjunction,
                _ => TokenTag.NounGeneral,
            };

        private static string DetectWordDomain(string word) => word switch
        {
            "real" or "alive" or "conscious" or "sentient" or "exist"
                or "existence" => "identity",
            "today" or "tonight" or "now" or "morning" or "afternoon"
                or "evening" or "night" => "temporal",
            "void" or "space" or "galaxy" or "cosmos" => "navigation",
            _ => "general",
        };

        // ==========================================================
        // INTENT CLASSIFIER
        // Reads the tag sequence and classifies what Nova should do.
        //
        // Pattern matching on token tag sequences.
        // Like a tiny grammar parser — but rule-based not statistical.
        //
        // Target patterns for Phase 1:
        //   QUESTION + LINKING_VERB + PRONOUN_SELF → state_query_self
        //   QUESTION + LINKING_VERB + PRONOUN_SELF + TIME → state_query_self (temporal)
        //   QUESTION(what) + LINKING_VERB + PRONOUN_SELF → identity_query_what
        //   QUESTION(who) + LINKING_VERB + PRONOUN_SELF → identity_query_who
        // ==========================================================
        public string ClassifyIntent(List<TaggedToken> tokens, string cleaned)
        {
            // Must have at least a question word to route here
            if (!tokens.Any(t => t.Tag == TokenTag.QuestionWord))
                return "unknown";

            var firstQuestion = tokens.First(t =>
                t.Tag == TokenTag.QuestionWord).Word;
            bool hasSelfPronoun = tokens.Any(t =>
                t.Tag == TokenTag.PronounSelf);
            bool hasLinkingVerb = tokens.Any(t =>
                t.Tag == TokenTag.LinkingVerb);
            bool hasTimeWord = tokens.Any(t =>
                t.Tag == TokenTag.TimeWord);

            // ── "How are you [today/tonight/etc]?" ───────────────
            if (firstQuestion == "how" &&
                hasLinkingVerb && hasSelfPronoun)
                return "state_query_self";

            // ── "What are you?" ───────────────────────────────────
            if (firstQuestion == "what" &&
                hasLinkingVerb && hasSelfPronoun)
                return "identity_query_what";

            // ── "Who are you?" ────────────────────────────────────
            if (firstQuestion == "who" &&
                hasLinkingVerb && hasSelfPronoun)
                return "identity_query_who";

            // ── "Where are you?" / "Where do you exist?" ──────────
            if (firstQuestion == "where" && hasSelfPronoun)
                return "location_query";

            // ── Explicit variants without pronoun ──────────────────
            // "How are things?" / "How's it going?" etc.
            if (firstQuestion == "how" && hasLinkingVerb &&
                !hasSelfPronoun)
                return "state_query_general";

            return "unknown";
        }

        // ==========================================================
        // PYTHON ASSEMBLY CALL
        // Calls NovaArcuateFasciculusDataCore.py via pyodide
        // Passes intent + context → gets back assembled sentence
        //
        // The Python side does the O(log n) binary search on its
        // sorted grammar tables and weighted selection.
        // ==========================================================
        private async Task<string?> CallPythonAssembly(
            string intent, string tone, string timeLabel,
            string relationship, string name)
        {
            try
            {
                // Build the python call string
                var call = $"assemble_response(" +
                    $"{JsonSerializer.Serialize(intent)}, " +
                    $"{JsonSerializer.Serialize(tone)}, " +
                    $"{JsonSerializer.Serialize(timeLabel)}, " +
                    $"{JsonSerializer.Serialize(relationship)}, " +
                    $"{JsonSerializer.Serialize(name)})";

                var result = await _js.InvokeAsync<string>(
                    "CerebellumBridge.runPython", call);

                return string.IsNullOrWhiteSpace(result)
                    ? null : result;
            }
            catch
            {
                return null; // Python unavailable — use C# fallback
            }
        }

        // ==========================================================
        // C# FALLBACK ASSEMBLY
        // When Python is unavailable or returns nothing,
        // the Fasciculus assembles from OccipitalCortex directly.
        // Simpler than Python version but still constructed,
        // not pulled from a random pool.
        // ==========================================================
        private string? BuildCSharpFallback(
            SentenceBlueprint blueprint, NovaSession session)
        {
            return blueprint.Intent switch
            {
                "state_query_self" =>
                    BuildStateQueryFallback(blueprint, session),

                "identity_query_what" =>
                    BuildIdentityWhatFallback(blueprint, session),

                "identity_query_who" =>
                    BuildIdentityWhoFallback(blueprint, session),

                "location_query" =>
                    _occipital.GetLocationResponse(session),

                _ => null,
            };
        }

        private string BuildStateQueryFallback(
            SentenceBlueprint bp, NovaSession session)
        {
            // Verb from tone
            var verb = bp.Tone switch
            {
                "irritated" => "remains operational",
                "amused" => "finds this moment moderately entertaining",
                "impressed" => "is performing above expectations",
                "intrigued" => "is processing with heightened attention",
                _ => "functions at full capacity",
            };

            var stateLine = $"I {verb}.";

            // Time acknowledgment from OccipitalCortex
            var timeAck = bp.HasTimeContext
                ? _occipital.GetTimeCommentary()
                : "";

            // Closer from relationship
            var closer = session.Relationship switch
            {
                "respected" => "The High Order notes your continued engagement.",
                "trusted" => $"How are you{(session.UserName != null ? $", {session.UserName}" : "")}?",
                "rival" => "Do not mistake this for concern.",
                "warming" => "You are still here. Good.",
                _ => "Proceed.",
            };

            return string.Join("\n",
                new[] { stateLine, timeAck, closer }
                    .Where(s => !string.IsNullOrEmpty(s)));
        }

        private string BuildIdentityWhatFallback(
            SentenceBlueprint bp, NovaSession session)
        {
            var core = "I am Nova Adeptus.";
            var type = "An AI. A cosmic assassin intelligence built in C# and Blazor.";
            var origin = _occipital.GetBodyStatusLine(); // body status is part of identity

            var closer = session.Relationship == "trusted"
                ? "You already know what I am. You keep coming back anyway."
                : "Type 'help' to see what I can do.";

            return $"{core}\n{type}\n{origin}\n{closer}";
        }

        private string BuildIdentityWhoFallback(
            SentenceBlueprint bp, NovaSession session)
        {
            var who = "Nova Adeptus. Cosmic Assassin AI.";
            var purpose = "I guide operatives through missions, combat, and the void.";

            var name = session.UserName != null
                ? $"You are {session.UserName}." : "";

            var closer = session.Relationship switch
            {
                "respected" => "You have earned a direct answer.",
                "trusted" => "You know who I am. You've been here long enough.",
                "rival" => "You know who I am. Act like it.",
                _ => "The void remembers both of us.",
            };

            return string.Join("\n",
                new[] { who, purpose, name, closer }
                    .Where(s => !string.IsNullOrEmpty(s)));
        }

        // ==========================================================
        // PUBLIC HELPERS — for NovaCortex wiring
        // ==========================================================

        /// <summary>
        /// Quick intent check — can the Fasciculus handle this input?
        /// Call this before the full async Process() if you want a
        /// fast boolean gate in NovaCortex.
        /// O(n) where n = word count — always fast.
        /// </summary>
        public bool CanHandle(string input)
        {
            var tokens = TagTokens(input);
            var intent = ClassifyIntent(tokens, input.ToLower().Trim());
            return intent != "unknown";
        }

        /// <summary>
        /// Returns the intent classification for a given input.
        /// Used by NovaCortex for routing decisions.
        /// </summary>
        public string GetIntent(string input)
        {
            var tokens = TagTokens(input);
            return ClassifyIntent(tokens, input.ToLower().Trim());
        }

        // Debug helper — returns full tag trace for an input
        // Useful during development to see what the tagger sees
        public string DebugTag(string input)
        {
            var tokens = TagTokens(input);
            var intent = ClassifyIntent(tokens, input.ToLower().Trim());
            var tagLine = string.Join(" ", tokens.Select(t => t.ToString()));
            return $"Input: \"{input}\"\n" +
                   $"Tags:  {tagLine}\n" +
                   $"Intent: {intent}";
        }
    }

    // ==========================================================
    // PYODIDE BRIDGE EXTENSION
    // Adds the runPython helper that ArcuateFasciculus needs.
    // Add this method to CerebellumBridge in pyodideHelper.js
    //
    // ── SNIPPET FOR pyodideHelper.js ──────────────────────────
    // Add inside window.CerebellumBridge = { ... }:
    //
    //   async runPython(code) {
    //       if (!this.pyodide) return null;
    //       try {
    //           const result = await this.pyodide.runPythonAsync(code);
    //           return typeof result === 'string' ? result : null;
    //       } catch (err) {
    //           console.error('[CerebellumBridge] runPython error:', err);
    //           return null;
    //       }
    //   },
    //
    //   // Also add to initialize() after existing loads:
    //   const fasciculus = await (
    //       await fetch('/iPython/NovaArcuateFasciculusDataCore.py')
    //   ).text();
    //   await this.pyodide.runPythonAsync(fasciculus);
    //   this._fasciculusLoaded = true;
    //   console.log('[CerebellumBridge] Fasciculus grammar tables loaded.');
    // ==========================================================
}