// ============================================================
//  SequenceParser.cs
//  AntigenHunter — Sequence Input Validation & Parsing Service
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AntigenHunterLibrary.Models;

namespace AntigenHunterLibrary.Services
{
    /// <summary>
    /// Validates, cleans, and parses user-provided antibody
    /// sequences. Handles plain sequences and FASTA format.
    /// </summary>
    public class SequenceParser
    {
        // Invalid AA codes — not standard amino acids
        private static readonly HashSet<char> _invalidCodes =
            new() { 'B', 'J', 'O', 'U', 'X', 'Z' };

        /// <summary>
        /// Main entry point. Accepts raw user input (plain or FASTA),
        /// cleans it, validates it, and returns a parsed AntibodySequence.
        /// </summary>
        public ParseResult Parse(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
                return ParseResult.Fail("Input is empty. Please paste or type an antibody sequence.");

            // Step 1 — detect and strip FASTA header
            string cleaned = StripFasta(rawInput.Trim());

            // Step 2 — normalize
            cleaned = Normalize(cleaned);

            // Step 3 — validate
            var validation = AntibodySequence.Validate(cleaned);
            if (!validation.IsValid)
                return ParseResult.Fail(validation.Message, validation.InvalidPositions);

            // Step 4 — build sequence object
            var sequence = AntibodySequence.FromString(
                cleaned, out _, null, null);

            if (sequence == null)
                return ParseResult.Fail("Failed to construct sequence. Please check your input.");

            return ParseResult.Ok(sequence, validation.Message);
        }

        /// <summary>
        /// Parses directly from an example library entry.
        /// </summary>
        public ParseResult ParseFromExample(ExampleSequenceEntry example)
        {
            if (example == null)
                return ParseResult.Fail("Example not found.");

            var sequence = AntibodySequence.FromExample(example);
            return ParseResult.Ok(sequence,
                $"Loaded {example.AntibodyName} ({example.VHSequence.Length} residues). " +
                $"CDR regions pre-annotated from PDB {example.PdbId}.");
        }

        /// <summary>
        /// Strips FASTA header lines (lines starting with '>').
        /// Joins remaining lines into a single sequence string.
        /// </summary>
        private static string StripFasta(string input)
        {
            var lines = input.Split(new[] { '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith(">")) continue; // FASTA header
                if (trimmed.StartsWith(";")) continue; // comment
                sb.Append(trimmed);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Normalizes a sequence string:
        /// - Uppercase
        /// - Remove whitespace, digits, dashes
        /// - Remove common FASTA noise characters
        /// </summary>
        private static string Normalize(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (char c in input.ToUpper())
            {
                if (char.IsLetter(c)) sb.Append(c);
                // skip digits, spaces, dashes, asterisks, slashes
            }
            return sb.ToString();
        }

        /// <summary>
        /// Quick check — is this string a valid AA sequence?
        /// Used for real-time input feedback without full parsing.
        /// </summary>
        public static bool IsValidSequenceChar(char c)
        {
            c = char.ToUpper(c);
            return char.IsLetter(c) && !_invalidCodes.Contains(c);
        }

        /// <summary>
        /// Returns a list of invalid character positions for
        /// real-time highlighting in the textarea.
        /// </summary>
        public static List<int> FindInvalidPositions(string sequence)
        {
            var invalid = new List<int>();
            string upper = sequence.ToUpper();
            for (int i = 0; i < upper.Length; i++)
                if (char.IsLetter(upper[i]) && !AminoAcidDatabase.IsValid(upper[i]))
                    invalid.Add(i);
            return invalid;
        }

        /// <summary>
        /// Computes a live composition summary for display
        /// as the user types — no full parse needed.
        /// </summary>
        public static SequenceCompositionSummary GetLiveComposition(string rawInput)
        {
            string cleaned = Normalize(rawInput.ToUpper());
            int total = cleaned.Length;
            if (total == 0) return new SequenceCompositionSummary();

            int aromatic = cleaned.Count(c => c == 'F' || c == 'Y' || c == 'W' || c == 'H');
            int positive = cleaned.Count(c => c == 'R' || c == 'K');
            int negative = cleaned.Count(c => c == 'D' || c == 'E');
            int nonpolar = cleaned.Count(c => "AVILMGP".Contains(c));
            int polar = cleaned.Count(c => "STNQC".Contains(c));

            return new SequenceCompositionSummary
            {
                TotalResidues = total,
                AromaticCount = aromatic,
                PositiveCount = positive,
                NegativeCount = negative,
                NonpolarCount = nonpolar,
                PolarCount = polar,
                AromaticPct = (double)aromatic / total * 100,
                NetCharge = positive - negative
            };
        }
    }

    // --------------------------------------------------------
    //  PARSE RESULT
    // --------------------------------------------------------

    /// <summary>Result wrapper returned by SequenceParser.Parse().</summary>
    public class ParseResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public AntibodySequence? Sequence { get; init; }
        public List<int> InvalidPositions { get; init; } = new();

        public static ParseResult Ok(AntibodySequence seq, string msg) =>
            new() { Success = true, Sequence = seq, Message = msg };

        public static ParseResult Fail(string msg, List<int>? positions = null) =>
            new()
            {
                Success = false,
                Message = msg,
                InvalidPositions = positions ?? new()
            };
    }

    // --------------------------------------------------------
    //  COMPOSITION SUMMARY
    // --------------------------------------------------------

    /// <summary>Live composition data for real-time UI feedback.</summary>
    public class SequenceCompositionSummary
    {
        public int TotalResidues { get; init; }
        public int AromaticCount { get; init; }
        public int PositiveCount { get; init; }
        public int NegativeCount { get; init; }
        public int NonpolarCount { get; init; }
        public int PolarCount { get; init; }
        public double AromaticPct { get; init; }
        public int NetCharge { get; init; }

        public string AromaticPctDisplay =>
            $"{AromaticPct:F1}%";
        public string NetChargeDisplay =>
            NetCharge >= 0 ? $"+{NetCharge}" : $"{NetCharge}";
    }
}