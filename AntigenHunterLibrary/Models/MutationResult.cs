// ============================================================
//  MutationResult.cs
//  AntigenHunter — Simulation Generation Data Model
//
//  Captures everything that happens in one generation of
//  the affinity maturation simulation:
//  ┌─────────────────────────────────────────────────────┐
//  │  • Per-candidate mutation attempts and outcomes     │
//  │  • Generation-level affinity statistics             │
//  │  • Best sequence tracking across all generations   │
//  │  • Full simulation run summary for results display  │
//  │  • Chart data points for AffinityChart.razor        │
//  │  • Lineage tree data for mutation history           │
//  └─────────────────────────────────────────────────────┘
//
//  Data flows:
//    SimulationService → GenerationResult (per gen)
//                     → SimulationRunResult (full run)
//                     → AffinityChart (chart points)
//                     → MutationLog (log rows)
//
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace AntigenHunterLibrary.Models
{
    // --------------------------------------------------------
    //  MUTATION CANDIDATE
    // --------------------------------------------------------

    /// <summary>
    /// Represents a single mutation attempt evaluated during
    /// one generation of simulated annealing.
    /// The simulation evaluates many candidates per generation
    /// and selects the best one (or accepts a worse one with
    /// probability based on temperature).
    /// </summary>
    public class MutationCandidate
    {
        /// <summary>Position in sequence where mutation was attempted (0-based).</summary>
        public int Position { get; init; }

        /// <summary>Original amino acid at this position.</summary>
        public char FromCode { get; init; }

        /// <summary>Proposed new amino acid.</summary>
        public char ToCode { get; init; }

        /// <summary>Human-readable mutation notation e.g. "T28Y".</summary>
        public string Notation => $"{FromCode}{Position + 1}{ToCode}";

        /// <summary>Affinity score of the sequence before this mutation.</summary>
        public double AffinityBefore { get; init; }

        /// <summary>Affinity score of the sequence after this mutation.</summary>
        public double AffinityAfter { get; init; }

        /// <summary>Raw score delta (positive = improvement).</summary>
        public double ScoreDelta => AffinityAfter - AffinityBefore;

        /// <summary>Estimated ΔΔG contribution from substitution type.</summary>
        public double DeltaDeltaG { get; init; }

        /// <summary>CDR region affected, or "Framework".</summary>
        public string RegionName { get; init; } = string.Empty;

        /// <summary>Whether this candidate was accepted into the next generation.</summary>
        public bool WasAccepted { get; init; }

        /// <summary>
        /// True if accepted despite being a worse mutation
        /// (simulated annealing stochastic acceptance).
        /// </summary>
        public bool WasStochasticAccept { get; init; }

        /// <summary>Acceptance probability calculated during SA.</summary>
        public double AcceptanceProbability { get; init; }

        /// <summary>
        /// Type of substitution — Conservative, SemiConservative, or Radical.
        /// </summary>
        public SubstitutionType SubstitutionType { get; init; }

        /// <summary>
        /// True if this mutation touched a critical contact residue.
        /// </summary>
        public bool TouchedCriticalResidue { get; init; }

        /// <summary>
        /// CSS badge class based on outcome.
        /// </summary>
        public string OutcomeCssClass => WasAccepted
            ? (ScoreDelta > 0.3 ? "ah-badge--gold"
             : ScoreDelta > 0 ? "ah-badge--green"
             : WasStochasticAccept ? "ah-badge--orange"
             : "ah-badge--cyan")
            : "ah-badge--muted";

        /// <summary>Icon for this mutation's outcome in the log.</summary>
        public string OutcomeIcon => WasAccepted
            ? (ScoreDelta > 0.5 ? "⭐"
             : ScoreDelta > 0 ? "▲"
             : "~")
            : "✗";
    }

    // --------------------------------------------------------
    //  GENERATION RESULT
    // --------------------------------------------------------

    /// <summary>
    /// Complete data snapshot for one generation of simulation.
    /// Contains all evaluated candidates, the accepted mutation,
    /// and aggregate statistics for this generation.
    /// </summary>
    public class GenerationResult
    {
        // ── Identity ──────────────────────────────────────────

        /// <summary>Generation number (1-based for display).</summary>
        public int GenerationNumber { get; init; }

        /// <summary>Temperature parameter at this generation (SA schedule).</summary>
        public double Temperature { get; init; }

        // ── Sequence State ────────────────────────────────────

        /// <summary>The sequence entering this generation.</summary>
        public AntibodySequence SequenceIn { get; init; } = null!;

        /// <summary>The sequence exiting this generation (after accepted mutation).</summary>
        public AntibodySequence SequenceOut { get; init; } = null!;

        // ── Candidates Evaluated ──────────────────────────────

        /// <summary>All mutation candidates evaluated this generation.</summary>
        public List<MutationCandidate> Candidates { get; init; } = new();

        /// <summary>The mutation candidate that was accepted, or null if none.</summary>
        public MutationCandidate? AcceptedMutation { get; init; }

        /// <summary>True if any mutation was accepted this generation.</summary>
        public bool HadAcceptedMutation => AcceptedMutation != null;

        // ── Affinity Scores ───────────────────────────────────

        /// <summary>Raw affinity score at start of this generation (0.0–1.0 internal).</summary>
        public double ScoreIn { get; init; }

        /// <summary>Raw affinity score at end of this generation.</summary>
        public double ScoreOut { get; init; }

        /// <summary>Affinity in nM at start of this generation.</summary>
        public double AffinityNmIn { get; init; }

        /// <summary>Affinity in nM at end of this generation.</summary>
        public double AffinityNmOut { get; init; }

        /// <summary>Score improvement this generation (can be negative).</summary>
        public double ScoreDelta => ScoreOut - ScoreIn;

        /// <summary>True if this generation improved affinity.</summary>
        public bool IsImprovement => ScoreDelta > 0;

        // ── Population Statistics ─────────────────────────────

        /// <summary>Best score seen among all candidates this generation.</summary>
        public double BestCandidateScore { get; init; }

        /// <summary>Average score across all candidates this generation.</summary>
        public double AverageCandidateScore { get; init; }

        /// <summary>Number of candidates evaluated this generation.</summary>
        public int CandidateCount => Candidates.Count;

        /// <summary>Number of candidates that improved affinity.</summary>
        public int ImprovingCandidateCount =>
            Candidates.Count(c => c.ScoreDelta > 0);

        // ── SHM Hotspot Tracking ──────────────────────────────

        /// <summary>
        /// True if the accepted mutation occurred at a known SHM hotspot (S or T).
        /// </summary>
        public bool WasShmHotspotMutation =>
            AcceptedMutation != null &&
            (AcceptedMutation.FromCode == 'S' || AcceptedMutation.FromCode == 'T');

        // ── Display Helpers ───────────────────────────────────

        /// <summary>
        /// Short generation label for mutation log display.
        /// e.g. "Gen 04"
        /// </summary>
        public string GenerationLabel =>
            $"Gen {GenerationNumber:D2}";

        /// <summary>
        /// Formatted affinity string for display.
        /// Shows nM value with appropriate precision.
        /// </summary>
        public string AffinityDisplayOut =>
            FormatAffinityNm(AffinityNmOut);

        /// <summary>
        /// CSS class for the score delta indicator.
        /// </summary>
        public string DeltaCssClass => ScoreDelta switch
        {
            > 0.4 => "ah-text-gold",
            > 0.1 => "ah-text-green",
            > 0.0 => "ah-text-cyan",
            > -0.1 => "ah-text-muted",
            _ => "ah-text-orange"
        };

        /// <summary>Formats a nM value for display.</summary>
        public static string FormatAffinityNm(double nM) => nM switch
        {
            < 0.01 => $"{nM * 1000:F2} pM",
            < 1.0 => $"{nM:F3} nM",
            < 10.0 => $"{nM:F2} nM",
            < 100.0 => $"{nM:F1} nM",
            _ => $"{nM:F0} nM"
        };
    }

    // --------------------------------------------------------
    //  CHART DATA POINT
    // --------------------------------------------------------

    /// <summary>
    /// Lightweight data point for the SVG affinity chart.
    /// Extracted from GenerationResult for efficient rendering.
    /// </summary>
    public class AffinityChartPoint
    {
        /// <summary>Generation number (X axis).</summary>
        public int Generation { get; init; }

        /// <summary>Affinity in nM (Y axis — lower is better).</summary>
        public double AffinityNm { get; init; }

        /// <summary>Internal score (0–1, higher is better).</summary>
        public double Score { get; init; }

        /// <summary>True if this is the best point seen so far.</summary>
        public bool IsBestSoFar { get; init; }

        /// <summary>True if this generation had an accepted improvement.</summary>
        public bool HadImprovement { get; init; }

        /// <summary>Mutation notation for tooltip, e.g. "T28Y".</summary>
        public string MutationLabel { get; init; } = string.Empty;
    }

    // --------------------------------------------------------
    //  SIMULATION RUN RESULT
    // --------------------------------------------------------

    /// <summary>
    /// Complete result of a full affinity maturation simulation run.
    /// Aggregates all generation results and provides summary
    /// statistics for the results display panel.
    /// </summary>
    public class SimulationRunResult
    {
        // ── Run Identity ──────────────────────────────────────

        /// <summary>Unique run identifier.</summary>
        public Guid RunId { get; init; } = Guid.NewGuid();

        /// <summary>UTC timestamp when simulation started.</summary>
        public DateTime StartedAt { get; init; } = DateTime.UtcNow;

        /// <summary>UTC timestamp when simulation completed.</summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>Wall-clock duration of the simulation.</summary>
        public TimeSpan Duration => CompletedAt - StartedAt;

        // ── Configuration Summary ─────────────────────────────

        /// <summary>Target protein this run was against.</summary>
        public CancerTargetType TargetType { get; init; }

        /// <summary>Target protein display name.</summary>
        public string TargetName { get; init; } = string.Empty;

        /// <summary>Number of rounds of evolution configured.</summary>
        public int TotalGenerations { get; init; }

        /// <summary>Number of antibody variants per round configured.</summary>
        public int PopulationSize { get; init; }

        /// <summary>Mutation boldness setting (0.0–1.0).</summary>
        public double MutationBoldness { get; init; }

        // ── Sequences ─────────────────────────────────────────

        /// <summary>The original input antibody sequence.</summary>
        public AntibodySequence OriginalSequence { get; init; } = null!;

        /// <summary>The best sequence found during the simulation.</summary>
        public AntibodySequence BestSequence { get; set; } = null!;

        /// <summary>The final sequence at end of last generation.</summary>
        public AntibodySequence FinalSequence { get; set; } = null!;

        // ── Affinity Scores ───────────────────────────────────

        /// <summary>Starting affinity in nM (original sequence).</summary>
        public double StartingAffinityNm { get; set; }

        /// <summary>Best affinity achieved during the run in nM.</summary>
        public double BestAffinityNm { get; set; }

        /// <summary>Final affinity at end of simulation in nM.</summary>
        public double FinalAffinityNm { get; set; }

        /// <summary>
        /// Fold improvement: StartingAffinity / BestAffinity.
        /// e.g. 8.0 means 8x tighter binding.
        /// </summary>
        public double FoldImprovement =>
            BestAffinityNm > 0 ? StartingAffinityNm / BestAffinityNm : 1.0;

        /// <summary>
        /// True if meaningful improvement was achieved (>= 1.5x fold).
        /// </summary>
        public bool AchievedImprovement => FoldImprovement >= 1.5;

        /// <summary>
        /// Generation number where best affinity was first achieved.
        /// </summary>
        public int BestAffinityGeneration { get; set; }

        // ── Generation Data ───────────────────────────────────

        /// <summary>Results for every generation.</summary>
        public List<GenerationResult> Generations { get; } = new();

        /// <summary>Number of generations actually completed.</summary>
        public int GenerationsCompleted => Generations.Count;

        /// <summary>
        /// Only the generations where an improvement was accepted.
        /// Used for the mutation log display.
        /// </summary>
        public IEnumerable<GenerationResult> ImprovingGenerations =>
            Generations.Where(g => g.IsImprovement && g.HadAcceptedMutation);

        // ── Chart Data ────────────────────────────────────────

        /// <summary>
        /// Pre-computed chart points for AffinityChart.razor.
        /// Includes start point (generation 0) and one per generation.
        /// </summary>
        public List<AffinityChartPoint> ChartPoints { get; } = new();

        // ── Mutation Summary ──────────────────────────────────

        /// <summary>Total accepted mutations across all generations.</summary>
        public int TotalAcceptedMutations =>
            Generations.Count(g => g.HadAcceptedMutation);

        /// <summary>Total improving mutations accepted.</summary>
        public int TotalImprovingMutations =>
            Generations.Count(g => g.IsImprovement && g.HadAcceptedMutation);

        /// <summary>Total stochastic (worse) mutations accepted.</summary>
        public int TotalStochasticAccepts =>
            Generations.Count(g =>
                g.AcceptedMutation?.WasStochasticAccept == true);

        /// <summary>
        /// Most impactful single mutation in the run.
        /// </summary>
        public MutationCandidate? BestSingleMutation =>
            Generations
                .Where(g => g.AcceptedMutation != null && g.IsImprovement)
                .OrderByDescending(g => g.AcceptedMutation!.ScoreDelta)
                .FirstOrDefault()
                ?.AcceptedMutation;

        /// <summary>
        /// CDR region that contributed the most accepted mutations.
        /// </summary>
        public string MostMutatedRegion
        {
            get
            {
                var accepted = Generations
                    .Where(g => g.HadAcceptedMutation)
                    .Select(g => g.AcceptedMutation!.RegionName)
                    .ToList();

                if (!accepted.Any()) return "—";

                return accepted
                    .GroupBy(r => r)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
            }
        }

        // ── Display Helpers ───────────────────────────────────

        /// <summary>Formatted starting affinity string.</summary>
        public string StartingAffinityDisplay =>
            GenerationResult.FormatAffinityNm(StartingAffinityNm);

        /// <summary>Formatted best affinity string.</summary>
        public string BestAffinityDisplay =>
            GenerationResult.FormatAffinityNm(BestAffinityNm);

        /// <summary>Fold improvement formatted for display.</summary>
        public string FoldImprovementDisplay =>
            FoldImprovement >= 10
                ? $"{FoldImprovement:F0}x"
                : $"{FoldImprovement:F1}x";

        /// <summary>
        /// CSS class for the fold improvement badge.
        /// </summary>
        public string ImprovementCssClass => FoldImprovement switch
        {
            >= 10.0 => "ah-badge--gold",
            >= 4.0 => "ah-badge--green",
            >= 2.0 => "ah-badge--cyan",
            >= 1.5 => "ah-badge--cyan",
            _ => "ah-badge--muted"
        };

        /// <summary>
        /// Summary sentence for results header.
        /// </summary>
        public string SummaryText =>
            AchievedImprovement
                ? $"Achieved {FoldImprovementDisplay} improvement over {GenerationsCompleted} rounds — " +
                  $"best affinity {BestAffinityDisplay} at generation {BestAffinityGeneration}."
                : $"No significant improvement found after {GenerationsCompleted} rounds. " +
                  $"Try increasing Rounds of Evolution or Mutation Boldness.";

        // ── Chart Builder ─────────────────────────────────────

        /// <summary>
        /// Rebuilds chart points from Generations list.
        /// Call after all generations are added.
        /// </summary>
        public void BuildChartPoints()
        {
            ChartPoints.Clear();

            // Generation 0 = starting point
            ChartPoints.Add(new AffinityChartPoint
            {
                Generation = 0,
                AffinityNm = StartingAffinityNm,
                Score = 0.0,
                IsBestSoFar = true,
                HadImprovement = false,
                MutationLabel = "Start"
            });

            double bestNm = StartingAffinityNm;

            foreach (var gen in Generations)
            {
                bool newBest = gen.AffinityNmOut < bestNm;
                if (newBest) bestNm = gen.AffinityNmOut;

                ChartPoints.Add(new AffinityChartPoint
                {
                    Generation = gen.GenerationNumber,
                    AffinityNm = gen.AffinityNmOut,
                    Score = gen.ScoreOut,
                    IsBestSoFar = newBest,
                    HadImprovement = gen.IsImprovement,
                    MutationLabel = gen.AcceptedMutation?.Notation ?? string.Empty
                });
            }
        }

        // ── Sequence Diff Summary ─────────────────────────────

        /// <summary>
        /// Returns a list of all positions that changed between
        /// the original and best sequence, with before/after codes.
        /// Used for the optimized sequence display.
        /// </summary>
        public List<(int Position, char From, char To, string Region)> GetSequenceDiff()
        {
            var result = new List<(int, char, char, string)>();
            if (OriginalSequence == null || BestSequence == null) return result;

            var orig = OriginalSequence.Sequence;
            var best = BestSequence.Sequence;
            int len = Math.Min(orig.Length, best.Length);

            for (int i = 0; i < len; i++)
            {
                if (orig[i] != best[i])
                {
                    var region = BestSequence.Residues[i].CdrRegion?.Name ?? "Framework";
                    result.Add((i, orig[i], best[i], region));
                }
            }

            return result;
        }

        // ── Status ────────────────────────────────────────────

        /// <summary>True if the simulation completed without errors.</summary>
        public bool IsComplete { get; set; }

        /// <summary>Error message if simulation failed, empty otherwise.</summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>True if simulation encountered an error.</summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }

    // --------------------------------------------------------
    //  SIMULATION PROGRESS (for live UI updates)
    // --------------------------------------------------------

    /// <summary>
    /// Lightweight progress snapshot emitted during simulation
    /// for real-time UI updates in AntigenHunter.razor.
    /// </summary>
    public class SimulationProgress
    {
        /// <summary>Current generation number (1-based).</summary>
        public int CurrentGeneration { get; init; }

        /// <summary>Total generations configured.</summary>
        public int TotalGenerations { get; init; }

        /// <summary>Progress fraction 0.0–1.0.</summary>
        public double Fraction =>
            TotalGenerations > 0
                ? Math.Clamp((double)CurrentGeneration / TotalGenerations, 0, 1)
                : 0;

        /// <summary>Progress percentage 0–100.</summary>
        public int ProgressPercent => (int)(Fraction * 100);

        /// <summary>Current best affinity in nM.</summary>
        public double CurrentBestNm { get; init; }

        /// <summary>Starting affinity in nM.</summary>
        public double StartingNm { get; init; }

        /// <summary>Current fold improvement.</summary>
        public double CurrentFold =>
            CurrentBestNm > 0 ? StartingNm / CurrentBestNm : 1.0;

        /// <summary>Last accepted mutation notation e.g. "T28Y".</summary>
        public string LastMutation { get; init; } = string.Empty;

        /// <summary>Current SA temperature.</summary>
        public double Temperature { get; init; }

        /// <summary>Status message for display.</summary>
        public string StatusMessage =>
            $"Round {CurrentGeneration} / {TotalGenerations} — " +
            $"Best: {GenerationResult.FormatAffinityNm(CurrentBestNm)} " +
            $"({CurrentFold:F1}x improvement)";
    }
}