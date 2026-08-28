// ==========================================================
// NovaBroca.cs — Nova Adeptus Frontal Lobe
//
// Inspired by Broca's area of the human brain:
//   - Located in the inferior frontal gyrus (Brodmann areas 44/45)
//   - Primary role: LANGUAGE PRODUCTION
//   - Forms words, constructs sentences, controls articulation
//   - Processes syntax — the rules of arrangement
//   - Damage → Broca's aphasia: telegraphic speech, effortful
//     output, omission of grammatical words ("I... go... store")
//
// In Nova's architecture:
//   - Receives a LinguisticSignal from NovaWernicke.cs
//   - Runs the spelling engine (primary feature)
//   - Produces Nova-voiced responses from linguistic data
//   - Assembles character-by-character breakdowns
//   - Formats numbers, operators, and symbol sequences
//   - Falls back to telegraphic output when confidence is low
//     (mirroring real Broca's aphasia behavior)
//
// Called by NovaCortex.cs via: NovaBroca.Produce(signal, session)
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NovaAdeptusLibrary
{
    // ==========================================================
    // SPELLING RESULT
    // The structured output of a spelling operation.
    // Broca's area doesn't just say the word — it produces a
    // fully articulated breakdown of its components.
    // ==========================================================
    public class SpellingResult
    {
        public string Word { get; set; } = "";
        public int LetterCount { get; set; } = 0;
        public int VowelCount { get; set; } = 0;
        public int ConsonantCount { get; set; } = 0;
        public int DigitCount { get; set; } = 0;
        public int SymbolCount { get; set; } = 0;
        public List<string> LetterLines { get; set; } = new();   // per-character annotations
        public string SpelledOut { get; set; } = "";             // "P · H · O · T · O · N"
        public string PhoneticLine { get; set; } = "";           // "pee — aych — oh — tee — oh — en"
        public string Summary { get; set; } = "";                // summary sentence
        public string Domain { get; set; } = "general";         // semantic domain from Wernicke
        public string NovaVoice { get; set; } = "";              // final Nova-styled response
    }

    // ==========================================================
    // NOVA BROCA — MAIN PRODUCTION ENGINE
    // ==========================================================
    public class NovaBroca
    {
        private readonly NovaWernicke _wernicke;
        private static readonly Random _rng = new();

        // ── Broca's personality pool for spelling responses ────
        // Broca controls the VOICE — Nova's personality informs
        // how she articulates. These are production templates.
        private static readonly List<string> SpellIntros = new()
        {
            "Spelling that for you. Pay attention.",
            "Fine. Here it is, character by character.",
            "You need this spelled out? Let me oblige.",
            "Decoding for you. Don't waste the output.",
            "Running your word through the language core.",
            "Linguistic analysis initiated. Letter by letter:",
            "Broca online. Spelling engaged.",
        };

        private static readonly List<string> SpellOutros = new()
        {
            "That's it. You're welcome.",
            "Filed under things Nova spelled for you.",
            "Memory banks updated. Try to retain it.",
            "That's all {count} character(s) accounted for.",
            "End of word. Proceed with this knowledge.",
        };

        private static readonly List<string> UnknownWordQuips = new()
        {
            "Unknown domain. Spelling it anyway.",
            "No semantic record found. Raw spelling follows.",
            "Novel pattern. Wernicke filed it. Here's the output.",
            "This is a new one. Broca handles it with what we have.",
        };

        private static readonly List<string> LowConfidenceResponses = new()
        {
            "Input. Unclear. Trying anyway.",
            "Signal weak. Partial processing only.",
            "Processing... low coherence detected.",
        };

        // ==========================================================
        // CONSTRUCTOR
        // ==========================================================
        public NovaBroca(NovaWernicke wernicke)
        {
            _wernicke = wernicke;
        }

        // ==========================================================
        // MAIN ENTRY — Produce a response from a LinguisticSignal
        //
        // This is the Broca-Wernicke pipeline in action:
        //   Wernicke understood → signal → Broca produces output
        //
        // If the signal has low confidence, Broca falls back to
        // telegraphic mode — short, stripped responses — exactly
        // as in real Broca's aphasia.
        // ==========================================================
        public string Produce(LinguisticSignal signal, NovaSession session)
        {
            // Empty input — nothing to produce
            if (signal.Intent == "empty")
                return "...";

            // Low confidence — telegraphic fallback
            if (signal.Confidence < 0.15 && signal.Intent == "chat")
                return TelegraphicResponse(signal);

            return signal.Intent switch
            {
                "spell" => ProduceSpelling(signal, session),
                "analyze" => ProduceAnalysis(signal, session),
                "chat" => null!, // Broca passes back to NovaCortex for chat
                _ => null!,
            };
        }

        // ==========================================================
        // SPELLING ENGINE
        //
        // The core feature. Takes a LinguisticSignal with spell
        // intent and produces a full character-by-character breakdown.
        //
        // Neuroscience parallel: Broca's area coordinates the
        // articulatory motor program for producing each phoneme in
        // sequence. Here, Broca sequences each character with its
        // full metadata before assembling the output.
        // ==========================================================
        public string ProduceSpelling(LinguisticSignal signal,
            NovaSession session)
        {
            var word = signal.TargetWord.ToUpper().Trim();
            if (string.IsNullOrEmpty(word))
                return "Nothing to spell. Try: spell PHOTON";

            var result = BuildSpellingResult(word, signal.DominantDomain);
            return FormatSpellingOutput(result, signal, session);
        }

        // ==========================================================
        // ANALYSIS ENGINE
        // Produces a character-analysis breakdown — not just the
        // spelling but the metadata about what kind of word it is.
        // ==========================================================
        public string ProduceAnalysis(LinguisticSignal signal,
            NovaSession session)
        {
            var word = signal.TargetWord.ToUpper().Trim();
            if (string.IsNullOrEmpty(word))
                return "Nothing to analyze. Try: analyze PHOTON";

            var result = BuildSpellingResult(word, signal.DominantDomain);
            return FormatAnalysisOutput(result, signal, session);
        }

        // ==========================================================
        // BUILD SPELLING RESULT
        // Core character-by-character decomposition.
        // ==========================================================
        private SpellingResult BuildSpellingResult(string word, string domain)
        {
            var result = new SpellingResult
            {
                Word = word,
                Domain = domain,
            };

            var spelledParts = new List<string>();
            var phoneticParts = new List<string>();

            foreach (char c in word)
            {
                var ch = _wernicke.LookupCharacter(c);

                if (ch == null)
                {
                    // Unknown character — Broca still articulates it
                    result.LetterLines.Add(
                        $"  '{c}' → type: unknown | unrecognized symbol");
                    spelledParts.Add(c.ToString());
                    phoneticParts.Add(c.ToString());
                    result.SymbolCount++;
                    continue;
                }

                // Build the annotation line
                string annotation = BuildAnnotationLine(ch);
                result.LetterLines.Add(annotation);
                spelledParts.Add(ch.Symbol.ToString());
                phoneticParts.Add(ch.PhoneticHint);

                // Tally
                switch (ch.Type)
                {
                    case "letter" when ch.Category == "vowel":
                        result.LetterCount++;
                        result.VowelCount++;
                        break;
                    case "letter" when ch.Category == "consonant":
                        result.LetterCount++;
                        result.ConsonantCount++;
                        break;
                    case "digit":
                        result.DigitCount++;
                        break;
                    default:
                        result.SymbolCount++;
                        break;
                }
            }

            result.SpelledOut = string.Join(" · ", spelledParts);
            result.PhoneticLine = string.Join(" — ", phoneticParts);

            // Summary sentence
            result.Summary = BuildSummary(result);

            return result;
        }

        // ==========================================================
        // ANNOTATION LINE
        // Formats a single character's full classification.
        // Broca is the articulator — it produces the detail.
        // ==========================================================
        private static string BuildAnnotationLine(NovaCharacter ch)
        {
            return ch.Type switch
            {
                "letter" =>
                    $"  [{ch.Symbol}] — {ch.Category} | \"{ch.PhoneticHint}\" | {ch.Meaning}",
                "digit" =>
                    $"  [{ch.Symbol}] — digit | value: {ch.PhoneticHint} | {ch.Meaning}",
                "operator" =>
                    $"  [{ch.Symbol}] — {ch.Category} operator | \"{ch.PhoneticHint}\" | {ch.Meaning}",
                "grouping" =>
                    $"  [{ch.Symbol}] — {ch.Category} grouping | \"{ch.PhoneticHint}\" | {ch.Meaning}",
                "punctuation" =>
                    $"  [{ch.Symbol}] — punctuation/{ch.Category} | \"{ch.PhoneticHint}\" | {ch.Meaning}",
                "whitespace" =>
                    $"  [{ch.PhoneticHint}] — whitespace/{ch.Category} | {ch.Meaning}",
                _ =>
                    $"  [{ch.Symbol}] — {ch.Type}/{ch.Category} | {ch.Meaning}",
            };
        }

        // ==========================================================
        // SUMMARY SENTENCE
        // Broca assembles a summary statement — the synthesis stage.
        // ==========================================================
        private static string BuildSummary(SpellingResult r)
        {
            var parts = new List<string>();
            int totalChars = r.LetterCount + r.DigitCount + r.SymbolCount;

            parts.Add($"{totalChars} character(s)");

            if (r.LetterCount > 0)
            {
                parts.Add($"{r.LetterCount} letter(s)");
                if (r.VowelCount > 0)
                    parts.Add($"{r.VowelCount} vowel(s)");
                if (r.ConsonantCount > 0)
                    parts.Add($"{r.ConsonantCount} consonant(s)");
            }
            if (r.DigitCount > 0)
                parts.Add($"{r.DigitCount} digit(s)");
            if (r.SymbolCount > 0)
                parts.Add($"{r.SymbolCount} symbol(s)");

            return string.Join(" | ", parts);
        }

        // ==========================================================
        // FORMAT SPELLING OUTPUT — Nova voice layer
        // Broca's area doesn't just produce neutral output —
        // it routes through Nova's personality before delivery.
        // ==========================================================
        private string FormatSpellingOutput(SpellingResult result,
            LinguisticSignal signal, NovaSession session)
        {
            var sb = new StringBuilder();

            // Intro — personality layer
            var intro = SpellIntros[_rng.Next(SpellIntros.Count)];
            sb.AppendLine(intro);
            sb.AppendLine();

            // Domain context
            if (result.Domain != "general")
                sb.AppendLine($"Domain: {result.Domain.ToUpper()} " +
                    $"[{GetDomainEmoji(result.Domain)}]");
            else if (signal.HasNewPattern)
                sb.AppendLine(UnknownWordQuips[_rng.Next(UnknownWordQuips.Count)]);

            // The word
            sb.AppendLine($"Word: {result.Word}");
            sb.AppendLine($"Spelled: {result.SpelledOut}");
            sb.AppendLine();

            // Character breakdown
            sb.AppendLine("Character breakdown:");
            foreach (var line in result.LetterLines)
                sb.AppendLine(line);

            sb.AppendLine();

            // Phonetic line
            sb.AppendLine($"Phonetic: {result.PhoneticLine}");
            sb.AppendLine();

            // Summary
            sb.AppendLine($"Summary: {result.Summary}");

            // Outro — personality
            var outro = SpellOutros[_rng.Next(SpellOutros.Count)]
                .Replace("{count}", result.LetterCount.ToString());
            sb.AppendLine();
            sb.Append(outro);

            // Adaptive note
            if (signal.HasNewPattern && !string.IsNullOrEmpty(signal.NewPatternLog))
                sb.AppendLine($"\n🔬 Wernicke log: {signal.NewPatternLog}");

            return sb.ToString().TrimEnd();
        }

        // ==========================================================
        // FORMAT ANALYSIS OUTPUT
        // Deeper breakdown — includes all token metadata.
        // ==========================================================
        private string FormatAnalysisOutput(SpellingResult result,
            LinguisticSignal signal, NovaSession session)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"🔬 LINGUISTIC ANALYSIS — {result.Word}");
            sb.AppendLine();

            // Domain
            sb.AppendLine($"Semantic domain: {result.Domain.ToUpper()} " +
                $"{GetDomainEmoji(result.Domain)}");

            // Token type
            string tokenType = result.Word.All(char.IsLetter)
                ? (result.Word.All(char.IsUpper) && result.Word.Length <= 4
                    ? "abbreviation"
                    : "word")
                : result.Word.All(char.IsDigit)
                    ? "number"
                    : "mixed expression";
            sb.AppendLine($"Token type:      {tokenType}");
            sb.AppendLine();

            // Character details
            sb.AppendLine("Character manifest:");
            foreach (var line in result.LetterLines)
                sb.AppendLine(line);
            sb.AppendLine();

            // Stats
            sb.AppendLine($"Composition: {result.Summary}");

            // Phonetic
            if (result.LetterCount > 0)
                sb.AppendLine($"Phonetic:    {result.PhoneticLine}");

            // Known / new
            sb.AppendLine($"Pattern status: " +
                (signal.HasNewPattern
                    ? "NEW — Wernicke has logged this pattern 🔬"
                    : "KNOWN — domain record exists ✅"));

            if (signal.HasNewPattern && !string.IsNullOrEmpty(signal.NewPatternLog))
                sb.AppendLine($"Log: {signal.NewPatternLog}");

            return sb.ToString().TrimEnd();
        }

        // ==========================================================
        // TELEGRAPHIC RESPONSE
        // Low-confidence input → Broca produces minimal output.
        // Mirrors Broca's aphasia: content words only, no filler,
        // effortful and stripped down.
        // ==========================================================
        private string TelegraphicResponse(LinguisticSignal signal)
        {
            var response = LowConfidenceResponses[
                _rng.Next(LowConfidenceResponses.Count)];
            return $"{response}\n" +
                   $"Domain detected: {signal.DominantDomain}. " +
                   $"Confidence: {signal.Confidence:P0}.\n" +
                   "Type 'help' — or try again with clearer input.";
        }

        // ==========================================================
        // DOMAIN EMOJI MAP
        // Small production detail — Broca adds emoji to domain labels.
        // ==========================================================
        private static string GetDomainEmoji(string domain) => domain switch
        {
            "health" => "❤️",
            "combat" => "⚔️",
            "hacking" => "💻",
            "stealth" => "👤",
            "navigation" => "🚀",
            "math" => "🔢",
            "economy" => "💰",
            "social" => "💬",
            "system" => "⚙️",
            _ => "🌌",
        };

        // ==========================================================
        // STATIC HELPERS — for NovaCortex to use directly
        // ==========================================================

        /// <summary>
        /// Quick-spell: bypasses the full signal pipeline.
        /// Used when NovaCortex wants a fast spelling without
        /// a full Wernicke parse. Returns a concise output.
        /// </summary>
        // REPLACE the QuickSpell method in NovaBroca.cs
        public string QuickSpell(string word, NovaSession session)
        {
            // Use the already-injected _wernicke, not a new instance
            var signal = _wernicke.Comprehend($"spell {word}");
            return ProduceSpelling(signal, session);
        }

        /// <summary>
        /// Check if an input is a spelling request.
        /// Returns the target word or null.
        /// Called by NovaCortex before the main brain pipeline.
        /// </summary>
        public static string? DetectSpellRequest(string input)
        {
            var cleaned = input.Trim().ToLower();
            var patterns = new[]
            {
                @"^spell\s+(\w+)",
                @"^how do you spell\s+(\w+)",
                @"^how do i spell\s+(\w+)",
                @"^can you spell\s+(\w+)",
                @"^spell out\s+(\w+)",
                @"^letters in\s+(\w+)",
                @"^break down\s+(\w+)",
            };
            foreach (var p in patterns)
            {
                var m = System.Text.RegularExpressions.Regex.Match(cleaned, p);
                if (m.Success) return m.Groups[1].Value.ToUpper();
            }
            return null;
        }

        /// <summary>
        /// Check if an input is an analysis request.
        /// Returns the target word or null.
        /// </summary>
        public static string? DetectAnalysisRequest(string input)
        {
            var cleaned = input.Trim().ToLower();
            var patterns = new[]
            {
                @"^analyze\s+(\w+)",
                @"^analyse\s+(\w+)",
                @"^define\s+(\w+)",
            };
            foreach (var p in patterns)
            {
                var m = System.Text.RegularExpressions.Regex.Match(cleaned, p);
                if (m.Success) return m.Groups[1].Value.ToUpper();
            }
            return null;
        }
    }
}