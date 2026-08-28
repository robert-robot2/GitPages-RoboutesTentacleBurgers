// ============================================================
//  SimulationConfig.cs
//  AntigenHunter — Human-Readable Simulation Parameters
//  ⚔️ Forged by Claude AI
// ============================================================

namespace AntigenHunterLibrary.Models
{
    /// <summary>
    /// User-configurable simulation parameters with human-readable
    /// labels. Maps to the Step 3 UI controls in AntigenHunter.razor.
    /// </summary>
    public class SimulationConfig
    {
        // ── Rounds of Evolution (Generations) ─────────────────
        /// <summary>
        /// How many rounds of mutation + selection to run.
        /// UI Label: "Rounds of Evolution"
        /// Range: 10–200. Default: 50.
        /// </summary>
        public int RoundsOfEvolution { get; set; } = 50;

        public const int MinRounds = 10;
        public const int MaxRounds = 200;

        // ── Antibody Variants per Round (Population) ──────────
        /// <summary>
        /// How many mutation candidates to evaluate each round.
        /// UI Label: "Antibody Variants per Round"
        /// Range: 5–50. Default: 20.
        /// </summary>
        public int VariantsPerRound { get; set; } = 20;

        public const int MinVariants = 5;
        public const int MaxVariants = 50;

        // ── Mutation Boldness (Temperature) ───────────────────
        /// <summary>
        /// Controls how aggressively the SA algorithm explores.
        /// UI Label: "Mutation Boldness"
        /// 0 = Conservative (only safe swaps, slow but reliable)
        /// 1 = Bold        (radical swaps allowed, fast but risky)
        /// Default: 0.5 (Medium)
        /// </summary>
        public double MutationBoldness { get; set; } = 0.5;

        public const double MinBoldness = 0.0;
        public const double MaxBoldness = 1.0;

        // ── Target ────────────────────────────────────────────
        /// <summary>Selected cancer target for this run.</summary>
        public CancerTargetType SelectedTarget { get; set; } = CancerTargetType.HER2;

        // ── Derived SA Parameters ─────────────────────────────
        /// <summary>
        /// Starting SA temperature — derived from MutationBoldness.
        /// Higher boldness = higher starting temperature = more exploration.
        /// </summary>
        public double StartTemperature =>
            0.2 + (MutationBoldness * 1.3);

        /// <summary>
        /// SA cooling rate per generation.
        /// Derived so temperature reaches ~0.01 by final generation.
        /// </summary>
        public double CoolingRate =>
            RoundsOfEvolution > 1
                ? Math.Pow(0.01 / StartTemperature, 1.0 / (RoundsOfEvolution - 1))
                : 0.95;

        /// <summary>
        /// Probability of attempting a radical substitution.
        /// Higher boldness increases this.
        /// </summary>
        public double RadicalMutationProbability =>
            0.05 + (MutationBoldness * 0.35);

        /// <summary>
        /// Probability of attempting a CDR mutation vs framework.
        /// Always biased heavily toward CDRs.
        /// </summary>
        public double CdrMutationBias => 0.85;

        // ── Boldness Label ────────────────────────────────────
        /// <summary>Human-readable boldness label for UI display.</summary>
        public string BoldnessLabel => MutationBoldness switch
        {
            <= 0.25 => "Conservative",
            <= 0.50 => "Balanced",
            <= 0.75 => "Aggressive",
            _ => "Maximum"
        };

        /// <summary>CSS class for boldness indicator.</summary>
        public string BoldnessCssClass => MutationBoldness switch
        {
            <= 0.25 => "ah-text-cyan",
            <= 0.50 => "ah-text-green",
            <= 0.75 => "ah-text-gold",
            _ => "ah-text-orange"
        };

        /// <summary>Estimated simulation time label.</summary>
        public string EstimatedTimeLabel
        {
            get
            {
                int ops = RoundsOfEvolution * VariantsPerRound;
                return ops switch
                {
                    < 500 => "~1 sec",
                    < 2000 => "~2 sec",
                    < 5000 => "~3-5 sec",
                    _ => "~5-10 sec"
                };
            }
        }

        /// <summary>Returns a default config for a given target.</summary>
        public static SimulationConfig Default(CancerTargetType target) => new()
        {
            SelectedTarget = target,
            RoundsOfEvolution = 50,
            VariantsPerRound = 20,
            MutationBoldness = 0.5
        };
    }
}