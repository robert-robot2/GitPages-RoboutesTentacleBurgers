// ============================================================
//  AntibodySequence.cs
//  AntigenHunter — Core Antibody Sequence Model
//
//  Represents a parsed, validated VH antibody sequence with:
//  ┌─────────────────────────────────────────────────────┐
//  │  • Full sequence storage + per-residue metadata     │
//  │  • Automatic CDR region detection (Kabat rules)     │
//  │  • Sequence validation against AA database          │
//  │  • Physicochemical property calculations            │
//  │  • Mutation tracking and history                    │
//  │  • Clone/mutate operations for simulation engine    │
//  └─────────────────────────────────────────────────────┘
//
//  CDR Detection uses simplified Kabat numbering:
//    CDR-H1: ~residues 26–35
//    CDR-H2: ~residues 50–65
//    CDR-H3: ~residues 95–102 (variable length)
//
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntigenHunterLibrary.Models
{
    // --------------------------------------------------------
    //  RESIDUE METADATA
    // --------------------------------------------------------

    /// <summary>
    /// Metadata for a single residue position in the sequence.
    /// Combines sequence position with AA properties and CDR status.
    /// </summary>
    public class ResidueInfo
    {
        /// <summary>0-based index in the full sequence.</summary>
        public int Index { get; init; }

        /// <summary>1-based position (Kabat-style display).</summary>
        public int Position => Index + 1;

        /// <summary>Single-letter amino acid code.</summary>
        public char Code { get; init; }

        /// <summary>Full amino acid descriptor from database.</summary>
        public AminoAcid? AminoAcid { get; init; }

        /// <summary>Which CDR this residue belongs to, or null if framework.</summary>
        public CdrRegion? CdrRegion { get; set; }

        /// <summary>True if this residue is inside a CDR loop.</summary>
        public bool IsCdr => CdrRegion != null;

        /// <summary>True if flagged as a known critical binding contact.</summary>
        public bool IsCriticalContact { get; set; }

        /// <summary>
        /// Normalized contribution weight for affinity scoring.
        /// CDR residues weight higher than framework residues.
        /// </summary>
        public double ScoringWeight { get; set; } = 1.0;

        /// <summary>
        /// CSS class for sequence highlighter rendering.
        /// Empty string = plain framework residue.
        /// "ah-seq-cdr1/2/3" = CDR region color.
        /// </summary>
        public string CssClass => CdrRegion == null
            ? "ah-seq-plain"
            : $"ah-seq-{CdrRegion.CssClass}";
    }

    // --------------------------------------------------------
    //  VALIDATION RESULT
    // --------------------------------------------------------

    /// <summary>
    /// Result of sequence validation — passed back to UI for display.
    /// </summary>
    public record SequenceValidationResult
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public List<int> InvalidPositions { get; init; } = new();
        public List<char> InvalidChars { get; init; } = new();
        public int SequenceLength { get; init; }

        public static SequenceValidationResult Success(int length) => new()
        {
            IsValid = true,
            Message = $"Valid sequence — {length} residues detected.",
            SequenceLength = length
        };

        public static SequenceValidationResult Failure(
            string message,
            List<int>? invalidPositions = null,
            List<char>? invalidChars = null) => new()
            {
                IsValid = false,
                Message = message,
                InvalidPositions = invalidPositions ?? new(),
                InvalidChars = invalidChars ?? new()
            };
    }

    // --------------------------------------------------------
    //  MUTATION RECORD
    // --------------------------------------------------------

    /// <summary>
    /// Records a single amino acid substitution applied to a sequence.
    /// Used to track the mutation history of an evolving antibody.
    /// </summary>
    public class MutationRecord
    {
        /// <summary>0-based position where mutation occurred.</summary>
        public int Position { get; init; }

        /// <summary>Original amino acid code.</summary>
        public char FromCode { get; init; }

        /// <summary>New amino acid code.</summary>
        public char ToCode { get; init; }

        /// <summary>Generation number when this mutation was applied.</summary>
        public int Generation { get; init; }

        /// <summary>CDR region affected, or "Framework" if not in CDR.</summary>
        public string RegionName { get; init; } = string.Empty;

        /// <summary>Affinity score before this mutation.</summary>
        public double AffinityBefore { get; init; }

        /// <summary>Affinity score after this mutation.</summary>
        public double AffinityAfter { get; init; }

        /// <summary>Change in affinity score (positive = improvement).</summary>
        public double AffinityDelta => AffinityAfter - AffinityBefore;

        /// <summary>True if this mutation improved binding affinity.</summary>
        public bool IsImprovement => AffinityDelta > 0;

        /// <summary>Human-readable mutation notation, e.g. "T28A".</summary>
        public string Notation => $"{FromCode}{Position + 1}{ToCode}";

        /// <summary>
        /// CSS badge class based on impact magnitude.
        /// Used in MutationLog.razor.
        /// </summary>
        public string ImpactCssClass => AffinityDelta switch
        {
            > 0.5 => "ah-badge--gold",
            > 0.2 => "ah-badge--green",
            > 0.0 => "ah-badge--cyan",
            > -0.2 => "ah-badge--muted",
            _ => "ah-badge--orange"
        };
    }

    // --------------------------------------------------------
    //  ANTIBODY SEQUENCE — CORE MODEL
    // --------------------------------------------------------

    /// <summary>
    /// Core model representing a parsed VH antibody sequence.
    /// Immutable sequence string with rich metadata overlay.
    /// Use Clone() + ApplyMutation() for simulation operations.
    /// </summary>
    public class AntibodySequence
    {
        // ── Private Fields ────────────────────────────────────
        private readonly string _sequence;
        private readonly List<ResidueInfo> _residues;
        private readonly List<CdrRegion> _cdrs;
        private readonly List<MutationRecord> _mutationHistory;

        // ── Public Properties ─────────────────────────────────

        /// <summary>Raw sequence string (uppercase, validated).</summary>
        public string Sequence => _sequence;

        /// <summary>Sequence length in residues.</summary>
        public int Length => _sequence.Length;

        /// <summary>Per-residue metadata list (index-aligned with Sequence).</summary>
        public IReadOnlyList<ResidueInfo> Residues => _residues.AsReadOnly();

        /// <summary>Detected CDR regions.</summary>
        public IReadOnlyList<CdrRegion> CdrRegions => _cdrs.AsReadOnly();

        /// <summary>Full mutation history applied to this sequence.</summary>
        public IReadOnlyList<MutationRecord> MutationHistory => _mutationHistory.AsReadOnly();

        /// <summary>Name of the original antibody if loaded from example library.</summary>
        public string? SourceAntibodyName { get; private set; }

        /// <summary>True if CDR regions were successfully detected.</summary>
        public bool HasCdrs => _cdrs.Count > 0;

        /// <summary>Number of mutations applied since original sequence.</summary>
        public int MutationCount => _mutationHistory.Count;

        /// <summary>Returns CDR-H1 or null.</summary>
        public CdrRegion? CdrH1 => _cdrs.Count > 0 ? _cdrs[0] : null;

        /// <summary>Returns CDR-H2 or null.</summary>
        public CdrRegion? CdrH2 => _cdrs.Count > 1 ? _cdrs[1] : null;

        /// <summary>Returns CDR-H3 or null.</summary>
        public CdrRegion? CdrH3 => _cdrs.Count > 2 ? _cdrs[2] : null;

        // ── Constructor ───────────────────────────────────────

        private AntibodySequence(
            string sequence,
            List<CdrRegion> cdrs,
            List<MutationRecord> mutationHistory,
            string? sourceName,
            List<int> criticalIndices)
        {
            _sequence = sequence;
            _cdrs = cdrs;
            _mutationHistory = mutationHistory;
            SourceAntibodyName = sourceName;
            _residues = BuildResidueList(sequence, cdrs, criticalIndices);
        }

        // ── Factory Methods ───────────────────────────────────

        /// <summary>
        /// Creates an AntibodySequence from a raw string.
        /// Validates, normalizes (uppercase, strips whitespace),
        /// and auto-detects CDR regions.
        /// Returns null if validation fails.
        /// </summary>
        public static AntibodySequence? FromString(
            string rawSequence,
            out SequenceValidationResult validation,
            string? sourceName = null,
            List<int>? criticalIndices = null)
        {
            // Normalize
            var cleaned = rawSequence
                .ToUpper()
                .Replace(" ", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Trim();

            // Validate
            validation = Validate(cleaned);
            if (!validation.IsValid)
                return null;

            // Detect CDRs
            var cdrs = DetectCdrs(cleaned);

            return new AntibodySequence(
                sequence: cleaned,
                cdrs: cdrs,
                mutationHistory: new List<MutationRecord>(),
                sourceName: sourceName,
                criticalIndices: criticalIndices ?? new List<int>());
        }

        /// <summary>
        /// Creates an AntibodySequence directly from an ExampleSequenceEntry.
        /// Reuses pre-computed CDR annotations from the library.
        /// </summary>
        public static AntibodySequence FromExample(ExampleSequenceEntry example)
        {
            return new AntibodySequence(
                sequence: example.VHSequence.ToUpper(),
                cdrs: new List<CdrRegion>(example.CdrRegions),
                mutationHistory: new List<MutationRecord>(),
                sourceName: example.AntibodyName,
                criticalIndices: example.CriticalResidueIndices);
        }

        // ── Mutation Operations ───────────────────────────────

        /// <summary>
        /// Returns a new AntibodySequence with one amino acid substituted.
        /// Records the mutation in the new sequence's history.
        /// Original sequence is unchanged (immutable design).
        /// </summary>
        public AntibodySequence ApplyMutation(
            int position,
            char newCode,
            int generation,
            double affinityBefore,
            double affinityAfter)
        {
            if (position < 0 || position >= _sequence.Length)
                throw new ArgumentOutOfRangeException(nameof(position),
                    $"Position {position} out of range for sequence length {_sequence.Length}");

            newCode = char.ToUpper(newCode);
            if (!AminoAcidDatabase.IsValid(newCode))
                throw new ArgumentException($"'{newCode}' is not a valid amino acid code.");

            // Build mutated sequence string
            var sb = new StringBuilder(_sequence);
            sb[position] = newCode;
            var newSequence = sb.ToString();

            // Determine region name for the record
            var region = _residues[position].CdrRegion?.Name ?? "Framework";

            // Build new mutation history
            var newHistory = new List<MutationRecord>(_mutationHistory)
            {
                new MutationRecord
                {
                    Position      = position,
                    FromCode      = _sequence[position],
                    ToCode        = newCode,
                    Generation    = generation,
                    RegionName    = region,
                    AffinityBefore = affinityBefore,
                    AffinityAfter  = affinityAfter
                }
            };

            // Carry critical indices forward
            var criticalIndices = _residues
                .Where(r => r.IsCriticalContact)
                .Select(r => r.Index)
                .ToList();

            return new AntibodySequence(
                sequence: newSequence,
                cdrs: new List<CdrRegion>(_cdrs),
                mutationHistory: newHistory,
                sourceName: SourceAntibodyName,
                criticalIndices: criticalIndices);
        }

        /// <summary>
        /// Returns an exact clone of this sequence.
        /// Used to branch simulation paths.
        /// </summary>
        public AntibodySequence Clone()
        {
            var criticalIndices = _residues
                .Where(r => r.IsCriticalContact)
                .Select(r => r.Index)
                .ToList();

            return new AntibodySequence(
                sequence: _sequence,
                cdrs: new List<CdrRegion>(_cdrs),
                mutationHistory: new List<MutationRecord>(_mutationHistory),
                sourceName: SourceAntibodyName,
                criticalIndices: criticalIndices);
        }

        // ── Physicochemical Summaries ─────────────────────────

        /// <summary>Net charge of the full sequence at pH 7.4.</summary>
        public int NetCharge =>
            _residues.Sum(r => r.AminoAcid?.Charge ?? 0);

        /// <summary>Average Kyte-Doolittle hydrophobicity of the sequence.</summary>
        public double AverageHydrophobicity
        {
            get
            {
                var valid = _residues.Where(r => r.AminoAcid != null).ToList();
                return valid.Count > 0
                    ? valid.Average(r => r.AminoAcid!.Hydrophobicity)
                    : 0.0;
            }
        }

        /// <summary>
        /// Aromatic residue fraction (F, Y, W, H) — proxy for
        /// π-stacking capacity at binding interface.
        /// </summary>
        public double AromaticFraction
        {
            get
            {
                if (Length == 0) return 0;
                return _residues.Count(r => r.AminoAcid?.IsAromatic == true)
                    / (double)Length;
            }
        }

        /// <summary>
        /// CDR aromatic fraction — aromatic residues specifically
        /// within CDR regions. Key predictor of binding strength.
        /// </summary>
        public double CdrAromaticFraction
        {
            get
            {
                var cdrResidues = _residues.Where(r => r.IsCdr).ToList();
                if (cdrResidues.Count == 0) return 0;
                return cdrResidues.Count(r => r.AminoAcid?.IsAromatic == true)
                    / (double)cdrResidues.Count;
            }
        }

        /// <summary>
        /// Number of SHM hotspot residues (S, T) in CDR regions.
        /// Higher count = more potential mutation sites.
        /// </summary>
        public int CdrShmHotspotCount =>
            _residues.Count(r => r.IsCdr && r.AminoAcid?.IsSHMHotspot == true);

        /// <summary>
        /// Returns the subsequence for a CDR region.
        /// </summary>
        public string GetCdrSubsequence(CdrRegion cdr)
        {
            if (cdr.Start < 0 || cdr.End >= Length) return string.Empty;
            return _sequence.Substring(cdr.Start, cdr.End - cdr.Start + 1);
        }

        /// <summary>
        /// Returns a formatted display string of the sequence
        /// with spaces every 10 residues.
        /// </summary>
        public string FormattedSequence =>
            SequenceHelpers.FormatSequence(_sequence);

        // ── Diff Helpers ──────────────────────────────────────

        /// <summary>
        /// Returns positions that differ between this sequence
        /// and another sequence of the same length.
        /// Used for result comparison display.
        /// </summary>
        public List<int> GetDiffPositions(AntibodySequence other)
        {
            var diffs = new List<int>();
            int len = Math.Min(Length, other.Length);
            for (int i = 0; i < len; i++)
                if (_sequence[i] != other._sequence[i])
                    diffs.Add(i);
            return diffs;
        }

        /// <summary>
        /// Returns percent identity to another sequence.
        /// </summary>
        public double GetPercentIdentity(AntibodySequence other)
        {
            if (Length == 0 || other.Length == 0) return 0;
            int len = Math.Min(Length, other.Length);
            int matches = 0;
            for (int i = 0; i < len; i++)
                if (_sequence[i] == other._sequence[i]) matches++;
            return (double)matches / len * 100.0;
        }

        // ── Static Helpers ────────────────────────────────────

        /// <summary>
        /// Validates a cleaned (uppercase, no whitespace) sequence.
        /// </summary>
        public static SequenceValidationResult Validate(string sequence)
        {
            if (string.IsNullOrWhiteSpace(sequence))
                return SequenceValidationResult.Failure(
                    "Sequence is empty. Please enter or paste a VH amino acid sequence.");

            if (sequence.Length < 50)
                return SequenceValidationResult.Failure(
                    $"Sequence too short ({sequence.Length} residues). " +
                    "A valid VH domain is typically 110–130 residues.");

            if (sequence.Length > 300)
                return SequenceValidationResult.Failure(
                    $"Sequence too long ({sequence.Length} residues). " +
                    "This simulator accepts VH domains up to 300 residues.");

            var invalidPositions = new List<int>();
            var invalidChars = new List<char>();

            for (int i = 0; i < sequence.Length; i++)
            {
                if (!AminoAcidDatabase.IsValid(sequence[i]))
                {
                    invalidPositions.Add(i);
                    if (!invalidChars.Contains(sequence[i]))
                        invalidChars.Add(sequence[i]);
                }
            }

            if (invalidPositions.Count > 0)
                return SequenceValidationResult.Failure(
                    $"Invalid characters found at {invalidPositions.Count} position(s): " +
                    $"{string.Join(", ", invalidChars.Select(c => $"'{c}'"))}. " +
                    "Only standard amino acid single-letter codes are accepted (A-Z excluding B, J, O, U, X, Z).",
                    invalidPositions,
                    invalidChars);

            // Basic VH signature check — should start with E, Q, or D
            // (Glu/Gln/Asp are typical VH N-terminal residues)
            char first = sequence[0];
            if (first != 'E' && first != 'Q' && first != 'D' && first != 'V')
            {
                // Warn but don't reject — user may have custom sequence
                return SequenceValidationResult.Success(sequence.Length) with
                {
                    Message = $"Sequence accepted ({sequence.Length} residues). " +
                              $"Note: VH sequences typically begin with E, Q, D, or V — " +
                              $"yours begins with '{first}'. Proceeding with your input."
                };
            }

            return SequenceValidationResult.Success(sequence.Length);
        }

        /// <summary>
        /// Detects CDR regions using simplified Kabat positional rules.
        /// Heuristic — works well for standard VH sequences 110–130 aa.
        ///
        /// Kabat CDR positions (1-based):
        ///   CDR-H1: 26–35   (10 residues)
        ///   CDR-H2: 50–65   (16 residues, variable)
        ///   CDR-H3: 95–102+ (variable, detected by conserved flanking C/W)
        /// </summary>
        public static List<CdrRegion> DetectCdrs(string sequence)
        {
            var cdrs = new List<CdrRegion>();
            int len = sequence.Length;

            if (len < 50) return cdrs;

            // ── CDR-H1 ────────────────────────────────────────
            // Kabat H1: positions 26–35 (0-based: 25–34)
            // Flanked by conserved Cys (pos 22) and Trp (pos 36)
            int h1Start = Math.Min(25, len - 1);
            int h1End = Math.Min(34, len - 1);

            // Try to find the conserved Cys anchor around pos 22-24
            for (int i = Math.Max(0, 20); i < Math.Min(26, len); i++)
            {
                if (sequence[i] == 'C') { h1Start = i + 3; break; }
            }
            h1End = Math.Min(h1Start + 9, len - 1);

            if (h1Start < len && h1End < len && h1End > h1Start)
            {
                cdrs.Add(new CdrRegion
                {
                    Name = "CDR-H1",
                    Start = h1Start,
                    End = h1End,
                    Sequence = sequence.Substring(h1Start, h1End - h1Start + 1),
                    CssClass = "cdr1",
                    FunctionalNote = "Forms initial contacts with antigen. " +
                                     "Ser and Tyr residues here are primary SHM hotspots."
                });
            }

            // ── CDR-H2 ────────────────────────────────────────
            // Kabat H2: positions 50–65 (0-based: 49–64)
            // Flanked by conserved Trp (pos 47) and Lys/Arg (pos 66)
            int h2Start = Math.Min(49, len - 1);
            int h2End = Math.Min(58, len - 1);

            // Try to find Trp anchor around pos 45-50
            for (int i = Math.Max(40, 0); i < Math.Min(52, len); i++)
            {
                if (sequence[i] == 'W') { h2Start = i + 3; break; }
            }
            h2End = Math.Min(h2Start + 8, len - 1);

            if (h2Start < len && h2End < len && h2End > h2Start && h2Start > h1End)
            {
                cdrs.Add(new CdrRegion
                {
                    Name = "CDR-H2",
                    Start = h2Start,
                    End = h2End,
                    Sequence = sequence.Substring(h2Start, h2End - h2Start + 1),
                    CssClass = "cdr2",
                    FunctionalNote = "Second contact loop. Trp and Tyr here drive " +
                                     "hydrophobic and aromatic contacts with antigen."
                });
            }

            // ── CDR-H3 ────────────────────────────────────────
            // Most variable CDR — detected by conserved flanking:
            //   N-terminal: C-A-R or C-A at positions ~92-94
            //   C-terminal: W-G-Q-G at positions ~103+
            int h3Start = -1;
            int h3End = -1;

            // Search for conserved Cys at ~position 92 (0-based 91)
            int searchFrom = Math.Max(h2End + 10, 80);
            int searchTo = Math.Min(searchFrom + 20, len - 10);

            for (int i = searchFrom; i < searchTo; i++)
            {
                if (sequence[i] == 'C')
                {
                    h3Start = i + 3;
                    break;
                }
            }

            // Search for conserved Trp-Gly-Gln-Gly or just W-G-X-G after H3
            if (h3Start > 0)
            {
                for (int i = h3Start + 1; i < Math.Min(h3Start + 25, len - 3); i++)
                {
                    if (sequence[i] == 'W' &&
                        i + 1 < len && sequence[i + 1] == 'G')
                    {
                        h3End = i - 1;
                        break;
                    }
                }

                // Fallback: use positional heuristic if motif not found
                if (h3End < 0)
                    h3End = Math.Min(h3Start + 9, len - 1);
            }

            if (h3Start > 0 && h3End > h3Start && h3Start < len && h3End < len)
            {
                cdrs.Add(new CdrRegion
                {
                    Name = "CDR-H3",
                    Start = h3Start,
                    End = h3End,
                    Sequence = sequence.Substring(h3Start, h3End - h3Start + 1),
                    CssClass = "cdr3",
                    FunctionalNote = "Primary specificity determinant. Most variable CDR. " +
                                     "Aromatic residues here drive the core binding energy."
                });
            }

            return cdrs;
        }

        // ── Private Helpers ───────────────────────────────────

        private static List<ResidueInfo> BuildResidueList(
            string sequence,
            List<CdrRegion> cdrs,
            List<int> criticalIndices)
        {
            var residues = new List<ResidueInfo>(sequence.Length);

            // Build CDR lookup: position → CdrRegion
            var cdrLookup = new Dictionary<int, CdrRegion>();
            foreach (var cdr in cdrs)
                for (int i = cdr.Start; i <= cdr.End && i < sequence.Length; i++)
                    cdrLookup[i] = cdr;

            var criticalSet = new HashSet<int>(criticalIndices);

            for (int i = 0; i < sequence.Length; i++)
            {
                char code = sequence[i];
                var aa = AminoAcidDatabase.Get(code);
                bool isCdr = cdrLookup.TryGetValue(i, out var cdrRegion);
                bool isCrit = criticalSet.Contains(i);

                // Scoring weights:
                // Critical contact → 2.0
                // CDR-H3           → 1.8
                // CDR-H1/H2        → 1.4
                // Framework        → 0.6
                double weight = 0.6;
                if (isCrit) weight = 2.0;
                else if (isCdr)
                {
                    weight = cdrRegion!.Name == "CDR-H3" ? 1.8 : 1.4;
                }

                residues.Add(new ResidueInfo
                {
                    Index = i,
                    Code = code,
                    AminoAcid = aa,
                    CdrRegion = isCdr ? cdrRegion : null,
                    IsCriticalContact = isCrit,
                    ScoringWeight = weight
                });
            }

            return residues;
        }

        // ── Object Overrides ──────────────────────────────────

        public override string ToString() =>
            $"AntibodySequence[{Length} aa, {_cdrs.Count} CDRs, {MutationCount} mutations]";

        public override bool Equals(object? obj) =>
            obj is AntibodySequence other && _sequence == other._sequence;

        public override int GetHashCode() => _sequence.GetHashCode();
    }
}