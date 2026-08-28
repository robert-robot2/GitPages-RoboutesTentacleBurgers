// ==========================================================
// NovaWernicke.cs — Nova Adeptus Temporal Lobe
//
// Inspired by Wernicke's area of the human brain:
//   - Located in the posterior superior temporal gyrus
//   - Primary role: LANGUAGE COMPREHENSION
//   - Assigns meaning to words and symbols
//   - Processes semantic content (what words mean)
//   - Damage → fluent but meaningless speech (Wernicke's aphasia)
//
// In Nova's architecture:
//   - Receives raw character/word input
//   - Classifies every character (letter, digit, symbol)
//   - Groups characters into proto-words (token units)
//   - Maps known tokens to semantic domains
//   - Detects "spell X" intent and extracts the target word
//   - Produces a LinguisticSignal → handed to NovaBroca.cs
//   - Tracks new/unseen patterns (adaptive frequency map)
//
// Connected to NovaBroca.cs via the LinguisticSignal object
// (the arcuate fasciculus of Nova's language system).
//
// Called by NovaCortex.cs — never by razor directly.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NovaAdeptusLibrary

{
    // ==========================================================
    // CHARACTER RECORD
    // Every character Nova can understand, fully classified.
    // Mirrors the neuroscientific concept of phoneme recognition —
    // Wernicke's area decodes the building blocks of language before
    // assembling them into meaning.
    // ==========================================================
    public record NovaCharacter(
        char Symbol,
        string Type,          // "letter" | "digit" | "operator" | "punctuation" | "whitespace" | "unknown"
        string Category,      // "vowel" | "consonant" | "number" | "math" | "comparison" | "grouping" | "sentence" | "separator" | "space"
        string PhoneticHint,  // pronunciation or reading aid (e.g. "ay", "bee", "plus")
        string Meaning        // semantic gloss (e.g. "addition", "equality", "open group")
    );

    // ==========================================================
    // TOKEN
    // A proto-word: one classified unit after grouping characters.
    // Equivalent to a lexical entry in Wernicke's mental lexicon.
    // ==========================================================
    public class NovaToken
    {
        public string Raw { get; set; } = "";
        public string TokenType { get; set; } = "word";   // "word" | "number" | "expression" | "abbreviation" | "unknown"
        public string SemanticDomain { get; set; } = "general"; // "health" | "combat" | "hacking" | "math" | "stealth" | "navigation" | "social" | "system"
        public List<NovaCharacter> Characters { get; set; } = new();
        public bool IsKnown { get; set; } = false;        // seen before in domain dictionary
        public int FrequencyCount { get; set; } = 0;      // how many times Wernicke has seen this token
    }

    // ==========================================================
    // LINGUISTIC SIGNAL
    // The arcuate fasciculus — the message object passed from
    // Wernicke (comprehension) to Broca (production).
    //
    // Neuroscience note: The arcuate fasciculus is a white matter
    // tract connecting Wernicke's area and Broca's area. Damage
    // to it causes conduction aphasia — the person can understand
    // and speak independently but cannot relay information between
    // the two systems. This class IS that relay.
    // ==========================================================
    public class LinguisticSignal
    {
        public string OriginalInput { get; set; } = "";
        public string Intent { get; set; } = "unknown";         // "spell" | "analyze" | "define" | "count" | "decode" | "chat"
        public string TargetWord { get; set; } = "";            // the word to spell/analyze
        public List<NovaToken> Tokens { get; set; } = new();    // parsed token stream
        public string DominantDomain { get; set; } = "general"; // highest-frequency domain found
        public bool HasNewPattern { get; set; } = false;        // did Wernicke see something new?
        public string NewPatternLog { get; set; } = "";         // what was new (for adaptive logging)
        public double Confidence { get; set; } = 0.0;           // 0–1 how well Wernicke understood the input
    }

    // ==========================================================
    // NOVA WERNICKE — MAIN COMPREHENSION ENGINE
    // ==========================================================
    public class NovaWernicke
    {
        // ── Character Registry ─────────────────────────────────
        // The complete alphabet of symbols Nova can classify.
        // Modeled on Wernicke's role in phoneme/grapheme recognition.
        private static readonly Dictionary<char, NovaCharacter> CharacterRegistry
            = BuildCharacterRegistry();

        // ── Domain Dictionary ──────────────────────────────────
        // Maps known words/abbreviations to semantic domains.
        // Wernicke's area is responsible for exactly this:
        // linking a word's form to its meaning category.
        private static readonly Dictionary<string, string> DomainDictionary
            = BuildDomainDictionary();

        // ── Frequency Map ──────────────────────────────────────
        // Adaptive layer. Wernicke tracks what it has seen.
        // New patterns are logged. Frequency drives domain confidence.
        private readonly Dictionary<string, int> _frequencyMap = new();

        // ── Spell Intent Patterns ──────────────────────────────
        private static readonly string[] SpellPatterns =
        {
            @"^spell\s+(.+)$",
            @"^how do you spell\s+(.+)$",
            @"^how do i spell\s+(.+)$",
            @"^can you spell\s+(.+)$",
            @"^spell out\s+(.+)$",
            @"^letters in\s+(.+)$",
            @"^what letters are in\s+(.+)$",
            @"^break down\s+(.+)$",
        };

        // ── Analyze Intent Patterns ────────────────────────────
        private static readonly string[] AnalyzePatterns =
        {
            @"^analyze\s+(.+)$",
            @"^analyse\s+(.+)$",
            @"^what is\s+([^\s]+)$",
            @"^define\s+(.+)$",
            @"^what does\s+(.+)\s+mean$",
        };

        // ==========================================================
        // MAIN ENTRY — Parse and comprehend input
        // Returns a LinguisticSignal for Broca to act on.
        // ==========================================================
        public LinguisticSignal Comprehend(string rawInput)
        {
            var signal = new LinguisticSignal
            {
                OriginalInput = rawInput,
                Intent = "chat",
                Confidence = 0.0,
            };

            if (string.IsNullOrWhiteSpace(rawInput))
            {
                signal.Intent = "empty";
                return signal;
            }

            var cleaned = rawInput.Trim().ToLower();

            // ── Step 1: Detect spell intent ──────────────────────
            foreach (var pattern in SpellPatterns)
            {
                var match = Regex.Match(cleaned, pattern,
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    signal.Intent = "spell";
                    signal.TargetWord = match.Groups[1].Value.Trim()
                        .Split(' ').First(); // first word only
                    signal.Confidence = 0.97;
                    signal.Tokens = TokenizeWord(signal.TargetWord.ToUpper());
                    signal.DominantDomain = ResolveDomain(signal.TargetWord);
                    TrackFrequency(signal.TargetWord, ref signal);
                    return signal;
                }
            }

            // ── Step 2: Detect analyze intent ───────────────────
            foreach (var pattern in AnalyzePatterns)
            {
                var match = Regex.Match(cleaned, pattern,
                    RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    signal.Intent = "analyze";
                    signal.TargetWord = match.Groups[1].Value.Trim()
                        .Split(' ').First();
                    signal.Confidence = 0.90;
                    signal.Tokens = TokenizeWord(signal.TargetWord.ToUpper());
                    signal.DominantDomain = ResolveDomain(signal.TargetWord);
                    TrackFrequency(signal.TargetWord, ref signal);
                    return signal;
                }
            }

            // ── Step 3: Tokenize full input ──────────────────────
            signal.Intent = "chat";
            signal.Tokens = TokenizeInput(rawInput);
            signal.DominantDomain = DetectDominantDomain(signal.Tokens);
            signal.Confidence = ComputeConfidence(signal.Tokens);
            TrackAllTokens(signal.Tokens, ref signal);
            return signal;
        }

        // ==========================================================
        // TOKENIZE A SINGLE WORD
        // Breaks a word into classified characters.
        // This is the grapheme-recognition stage — Wernicke's area
        // maps visual symbols onto their phonological representations.
        // ==========================================================
        public List<NovaToken> TokenizeWord(string word)
        {
            var token = new NovaToken
            {
                Raw = word,
                TokenType = DetectTokenType(word),
                SemanticDomain = ResolveDomain(word),
                IsKnown = DomainDictionary.ContainsKey(word.ToUpper()),
            };

            foreach (char c in word)
            {
                if (CharacterRegistry.TryGetValue(c, out var ch))
                    token.Characters.Add(ch);
                else
                    token.Characters.Add(new NovaCharacter(
                        c, "unknown", "unknown", c.ToString(), "unrecognized symbol"));
            }

            return new List<NovaToken> { token };
        }

        // ==========================================================
        // TOKENIZE FULL INPUT
        // Splits input into words/numbers/symbols and classifies each.
        // ==========================================================
        public List<NovaToken> TokenizeInput(string input)
        {
            var tokens = new List<NovaToken>();
            // Split on whitespace but keep symbols as separate tokens
            var parts = Regex.Split(input.Trim(),
                @"(\s+|(?<=[a-zA-Z])(?=[^a-zA-Z])|(?<=[^a-zA-Z])(?=[a-zA-Z]))");

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) continue;
                var token = new NovaToken
                {
                    Raw = part,
                    TokenType = DetectTokenType(part),
                    SemanticDomain = ResolveDomain(part),
                    IsKnown = DomainDictionary.ContainsKey(part.ToUpper()),
                    FrequencyCount = _frequencyMap.GetValueOrDefault(
                        part.ToUpper(), 0),
                };
                foreach (char c in part.ToUpper())
                    if (CharacterRegistry.TryGetValue(c, out var ch))
                        token.Characters.Add(ch);
                tokens.Add(token);
            }

            return tokens;
        }

        // ==========================================================
        // DOMAIN RESOLUTION
        // Given a word string, return its semantic domain.
        // Wernicke's area is fundamentally about this: assigning
        // meaning categories to word forms.
        // ==========================================================
        public string ResolveDomain(string word)
        {
            var upper = word.ToUpper().Trim();

            if (DomainDictionary.TryGetValue(upper, out var domain))
                return domain;

            // Heuristic fallback — partial matches
            if (upper.Contains("HP") || upper.Contains("HEALTH")
                || upper.Contains("MED")) return "health";
            if (upper.Contains("DMG") || upper.Contains("ATK")
                || upper.Contains("FIGHT")) return "combat";
            if (upper.Contains("HACK") || upper.Contains("SYS")
                || upper.Contains("CODE")) return "hacking";
            if (upper.Contains("STEALTH") || upper.Contains("SHADOW")
                || upper.Contains("GHOST")) return "stealth";

            // All digits → math domain
            if (upper.All(char.IsDigit)) return "math";

            return "general";
        }

        // ==========================================================
        // CHARACTER LOOKUP — public interface for Broca
        // ==========================================================
        public NovaCharacter? LookupCharacter(char c)
        {
            CharacterRegistry.TryGetValue(char.ToUpper(c), out var ch);
            return ch;
        }

        // ==========================================================
        // FREQUENCY TRACKING — the adaptive layer
        // Wernicke observes what patterns come through and builds
        // a frequency map. New patterns are flagged in the signal.
        // ==========================================================
        private void TrackFrequency(string word,
            ref LinguisticSignal signal)
        {
            var key = word.ToUpper().Trim();
            bool isNew = !_frequencyMap.ContainsKey(key);
            _frequencyMap[key] = _frequencyMap.GetValueOrDefault(key, 0) + 1;

            if (isNew && !DomainDictionary.ContainsKey(key))
            {
                signal.HasNewPattern = true;
                signal.NewPatternLog = $"New pattern observed: '{key}' — " +
                    $"domain unknown, frequency: 1";
            }
        }

        private void TrackAllTokens(List<NovaToken> tokens,
            ref LinguisticSignal signal)
        {
            var newOnes = new List<string>();
            foreach (var token in tokens)
            {
                var key = token.Raw.ToUpper().Trim();
                if (!_frequencyMap.ContainsKey(key) &&
                    !DomainDictionary.ContainsKey(key) &&
                    key.Length > 1)
                    newOnes.Add(key);
                _frequencyMap[key] =
                    _frequencyMap.GetValueOrDefault(key, 0) + 1;
                token.FrequencyCount = _frequencyMap[key];
            }
            if (newOnes.Any())
            {
                signal.HasNewPattern = true;
                signal.NewPatternLog =
                    $"New patterns: {string.Join(", ", newOnes)}";
            }
        }

        // ==========================================================
        // HELPERS
        // ==========================================================
        private static string DetectTokenType(string s)
        {
            if (string.IsNullOrEmpty(s)) return "unknown";
            if (s.All(char.IsDigit)) return "number";
            if (s.All(char.IsLetter)) return s.Length <= 4
                && s.All(char.IsUpper) ? "abbreviation" : "word";
            if (s.Length == 1 && !char.IsLetterOrDigit(s[0]))
                return "symbol";
            return "expression";
        }

        private static string DetectDominantDomain(List<NovaToken> tokens)
        {
            if (!tokens.Any()) return "general";
            var counts = tokens
                .GroupBy(t => t.SemanticDomain)
                .OrderByDescending(g => g.Count());
            return counts.First().Key;
        }

        private static double ComputeConfidence(List<NovaToken> tokens)
        {
            if (!tokens.Any()) return 0.0;
            int known = tokens.Count(t => t.IsKnown);
            return Math.Round((double)known / tokens.Count, 2);
        }

        // ==========================================================
        // CHARACTER REGISTRY — The full alphabet Nova knows
        // A–Z, a–z, 0–9, operators, punctuation, grouping symbols.
        //
        // Neuroscience parallel: the grapheme-phoneme mapping that
        // Wernicke's area performs when reading — each written symbol
        // is mapped to a sound and a category before meaning is built.
        // ==========================================================
        private static Dictionary<char, NovaCharacter> BuildCharacterRegistry()
        {
            var r = new Dictionary<char, NovaCharacter>();

            // ── Letters ──────────────────────────────────────────
            // Vowels
            foreach (var (ch, phonetic) in new[]
            {
                ('A', "ay"), ('E', "ee"), ('I', "eye"),
                ('O', "oh"), ('U', "you"),
            })
                r[ch] = new(ch, "letter", "vowel", phonetic,
                    $"vowel — {phonetic}");

            // Consonants with phonetic hints
            var consonants = new Dictionary<char, (string phonetic, string meaning)>
            {
                ['B'] = ("bee", "plosive consonant"),
                ['C'] = ("see", "variable consonant (hard/soft)"),
                ['D'] = ("dee", "plosive consonant"),
                ['F'] = ("eff", "fricative consonant"),
                ['G'] = ("jee", "plosive consonant"),
                ['H'] = ("aych", "aspirate consonant"),
                ['J'] = ("jay", "affricate consonant"),
                ['K'] = ("kay", "plosive consonant"),
                ['L'] = ("el", "lateral consonant"),
                ['M'] = ("em", "nasal consonant"),
                ['N'] = ("en", "nasal consonant"),
                ['P'] = ("pee", "plosive consonant"),
                ['Q'] = ("cue", "plosive consonant — always followed by U"),
                ['R'] = ("ar", "rhotic consonant"),
                ['S'] = ("ess", "sibilant consonant"),
                ['T'] = ("tee", "plosive consonant"),
                ['V'] = ("vee", "fricative consonant"),
                ['W'] = ("double-you", "semivowel consonant"),
                ['X'] = ("ex", "fricative — ks or gz sound"),
                ['Y'] = ("why", "semivowel — consonant or vowel depending on position"),
                ['Z'] = ("zee", "sibilant consonant"),
            };
            foreach (var kv in consonants)
                r[kv.Key] = new(kv.Key, "letter", "consonant",
                    kv.Value.phonetic, kv.Value.meaning);

            // ── Digits ────────────────────────────────────────────
            for (int i = 0; i <= 9; i++)
            {
                char c = (char)('0' + i);
                r[c] = new(c, "digit", "number",
                    i.ToString(), $"numeric digit — value {i}");
            }

            // ── Math operators ────────────────────────────────────
            r['+'] = new('+', "operator", "math", "plus", "addition");
            r['-'] = new('-', "operator", "math", "minus", "subtraction");
            r['*'] = new('*', "operator", "math", "times", "multiplication");
            r['/'] = new('/', "operator", "math", "divided", "division");
            r['='] = new('=', "operator", "comparison", "equals", "equality");
            r['<'] = new('<', "operator", "comparison", "less than", "less-than comparison");
            r['>'] = new('>', "operator", "comparison", "greater than", "greater-than comparison");
            r['%'] = new('%', "operator", "math", "percent", "modulo / percentage");
            r['^'] = new('^', "operator", "math", "caret", "exponentiation");

            // ── Grouping symbols ──────────────────────────────────
            r['('] = new('(', "grouping", "open", "open paren", "open parenthesis — starts a group");
            r[')'] = new(')', "grouping", "close", "close paren", "close parenthesis — ends a group");
            r['{'] = new('{', "grouping", "open", "open brace", "open brace — starts a block");
            r['}'] = new('}', "grouping", "close", "close brace", "close brace — ends a block");
            r['['] = new('[', "grouping", "open", "open bracket", "open bracket — starts an array/index");
            r[']'] = new(']', "grouping", "close", "close bracket", "close bracket — ends an array/index");

            // ── Punctuation ───────────────────────────────────────
            r['.'] = new('.', "punctuation", "sentence", "period", "sentence terminator");
            r['!'] = new('!', "punctuation", "sentence", "exclamation", "exclamation — emphasis or end");
            r['?'] = new('?', "punctuation", "sentence", "question mark", "interrogative — marks a question");
            r[','] = new(',', "punctuation", "separator", "comma", "separator — clause or list divider");
            r[';'] = new(';', "punctuation", "separator", "semicolon", "strong separator");
            r[':'] = new(':', "punctuation", "separator", "colon", "introduces a list or explanation");
            r['\''] = new('\'', "punctuation", "connector", "apostrophe", "contraction or possessive marker");
            r['"'] = new('"', "punctuation", "delimiter", "quote", "string / speech delimiter");
            r['-'] = new('-', "punctuation", "connector", "hyphen", "word connector or range indicator");
            r['_'] = new('_', "punctuation", "connector", "underscore", "word separator in code identifiers");
            r['@'] = new('@', "symbol", "address", "at", "address indicator — email / mentions");
            r['#'] = new('#', "symbol", "tag", "hash", "tag, number sign, or preprocessor");
            r['$'] = new('$', "symbol", "currency", "dollar", "currency or variable prefix");
            r['&'] = new('&', "operator", "logic", "ampersand", "logical AND / reference");
            r['|'] = new('|', "operator", "logic", "pipe", "logical OR / stream");
            r['~'] = new('~', "operator", "logic", "tilde", "bitwise NOT / approximate");
            r['\\'] = new('\\', "symbol", "path", "backslash", "escape character or path separator");
            r[' '] = new(' ', "whitespace", "space", "space", "word separator");
            r['\t'] = new('\t', "whitespace", "tab", "tab", "indentation");
            r['\n'] = new('\n', "whitespace", "newline", "newline", "line terminator");

            return r;
        }

        // ==========================================================
        // DOMAIN DICTIONARY
        // Maps abbreviations and game vocabulary to semantic domains.
        // This is Wernicke's semantic memory — the store of word
        // meanings that allows comprehension beyond mere phonology.
        // ==========================================================
        private static Dictionary<string, string> BuildDomainDictionary()
        {
            return new Dictionary<string, string>
            {
                // ── Health ─────────────────────────────────────────
                ["HP"] = "health",
                ["HEALTH"] = "health",
                ["MAXHP"] = "health",
                ["HEAL"] = "health",
                ["MEDKIT"] = "health",
                ["REGEN"] = "health",
                ["REVIVE"] = "health",
                ["RESPAWN"] = "health",
                ["ARMOR"] = "health",
                ["SHIELD"] = "health",

                // ── Combat ─────────────────────────────────────────
                ["DMG"] = "combat",
                ["DAMAGE"] = "combat",
                ["ATK"] = "combat",
                ["ATTACK"] = "combat",
                ["COMBAT"] = "combat",
                ["FIGHT"] = "combat",
                ["KILL"] = "combat",
                ["DEFEAT"] = "combat",
                ["ENEMY"] = "combat",
                ["BOSS"] = "combat",
                ["WEAPON"] = "combat",
                ["BLADE"] = "combat",
                ["GUN"] = "combat",
                ["PLASMA"] = "combat",
                ["CRITICAL"] = "combat",

                // ── Hacking ────────────────────────────────────────
                ["HACK"] = "hacking",
                ["ICE"] = "hacking",
                ["BREACH"] = "hacking",
                ["CODE"] = "hacking",
                ["SYSTEM"] = "hacking",
                ["UPLINK"] = "hacking",
                ["VIRUS"] = "hacking",
                ["CIPHER"] = "hacking",
                ["DECRYPT"] = "hacking",
                ["ENCRYPT"] = "hacking",
                ["BACKDOOR"] = "hacking",
                ["FIREWALL"] = "hacking",
                ["NODE"] = "hacking",
                ["TERMINAL"] = "hacking",
                ["PORT"] = "hacking",

                // ── Stealth ────────────────────────────────────────
                ["STEALTH"] = "stealth",
                ["GHOST"] = "stealth",
                ["SHADOW"] = "stealth",
                ["SNEAK"] = "stealth",
                ["HIDE"] = "stealth",
                ["SILENT"] = "stealth",
                ["CLOAK"] = "stealth",
                ["EVADE"] = "stealth",
                ["COVER"] = "stealth",

                // ── Navigation / Space ─────────────────────────────
                ["SHIP"] = "navigation",
                ["WARP"] = "navigation",
                ["JUMP"] = "navigation",
                ["SECTOR"] = "navigation",
                ["ORBIT"] = "navigation",
                ["PLANET"] = "navigation",
                ["VOID"] = "navigation",
                ["GALAXY"] = "navigation",
                ["STAR"] = "navigation",
                ["SYSTEM"] = "navigation",

                // ── Math ───────────────────────────────────────────
                ["PLUS"] = "math",
                ["MINUS"] = "math",
                ["TIMES"] = "math",
                ["DIVIDED"] = "math",
                ["EQUALS"] = "math",
                ["SUM"] = "math",
                ["TOTAL"] = "math",
                ["COUNT"] = "math",

                // ── Economy ────────────────────────────────────────
                ["XP"] = "economy",
                ["COINS"] = "economy",
                ["GC"] = "economy",
                ["GOLD"] = "economy",
                ["LOOT"] = "economy",
                ["REWARD"] = "economy",
                ["MARKET"] = "economy",
                ["COST"] = "economy",
                ["BUY"] = "economy",
                ["SELL"] = "economy",

                // ── Social ─────────────────────────────────────────
                ["HELLO"] = "social",
                ["HI"] = "social",
                ["HEY"] = "social",
                ["THANKS"] = "social",
                ["PLEASE"] = "social",
                ["SORRY"] = "social",
                ["BYE"] = "social",
                ["NAME"] = "social",

                // ── System / Meta ──────────────────────────────────
                ["HELP"] = "system",
                ["STATS"] = "system",
                ["SKILLS"] = "system",
                ["LEVEL"] = "system",
                ["RESET"] = "system",
                ["SAVE"] = "system",
                ["LOAD"] = "system",
                ["MENU"] = "system",
                ["SPELL"] = "system",
                ["ANALYZE"] = "system",
            };
        }
    }
}