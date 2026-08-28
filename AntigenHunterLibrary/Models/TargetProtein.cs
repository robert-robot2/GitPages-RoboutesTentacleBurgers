// ============================================================
//  TargetProtein.cs
//  AntigenHunter — Cancer Target Protein Definitions
//
//  Defines all cancer-associated protein targets including:
//  ┌─────────────────────────────────────────────────────┐
//  │  ACTIVE   →  HER2, EGFR, PD-L1                     │
//  │  PHASE 2  →  CD20, VEGFR2, BCMA, CD19              │
//  │  PHASE 3  →  CTLA4, CEA, MUC1, Custom              │
//  └─────────────────────────────────────────────────────┘
//
//  Each target carries:
//  • Epitope residue definitions for affinity scoring
//  • Binding site physicochemical profile
//  • Scoring weight matrix for AffinityScorer
//  • Clinical context for UI display
//  • Per-target mutation preference hints
//
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace AntigenHunterLibrary.Models
{
    // --------------------------------------------------------
    //  ENUMS
    // --------------------------------------------------------

    /// <summary>
    /// All cancer target identifiers.
    /// Active targets have full scoring data.
    /// Placeholder targets are defined but locked in UI.
    /// </summary>
    public enum CancerTargetType
    {
        // ✅ ACTIVE — fully implemented
        HER2 = 0,
        EGFR = 1,
        PDL1 = 2,

        // 🔲 PHASE 2 — placeholder, data skeleton ready
        CD20 = 10,
        VEGFR2 = 11,
        BCMA = 12,
        CD19 = 13,

        // 🔲 PHASE 3 — placeholder, future expansion
        CTLA4 = 20,
        CEA = 21,
        MUC1 = 22,
        Custom = 99
    }

    /// <summary>
    /// Surface character of the binding epitope.
    /// Drives which amino acid properties are rewarded in scoring.
    /// </summary>
    public enum EpitopeSurfaceType
    {
        Hydrophobic,    // Nonpolar patch — rewards F, Y, W, L, I
        Charged,        // Electrostatic — rewards R, K, D, E
        Mixed,          // Both hydrophobic and polar contacts
        Aromatic,       // π-stacking dominated — rewards F, Y, W, H
        Flat            // Flat/featureless — hardest to target
    }

    /// <summary>
    /// Therapeutic mechanism class of antibody targeting this protein.
    /// </summary>
    public enum TherapeuticMechanism
    {
        ReceptorBlockade,       // Block ligand binding (HER2, EGFR)
        ImmuneCheckpointBlock,  // Restore T cell function (PD-L1, CTLA4)
        ADCC,                   // Antibody-dependent cellular cytotoxicity (CD20)
        AngiogenesisBlock,      // Block tumor blood supply (VEGFR2)
        DirectCytotoxicity,     // Direct tumor cell killing (CD19, BCMA)
        TumorAssociatedAntigen  // Target overexpressed surface marker (CEA, MUC1)
    }

    // --------------------------------------------------------
    //  EPITOPE RESIDUE
    // --------------------------------------------------------

    /// <summary>
    /// Defines a single key residue on the target protein's
    /// epitope surface that the antibody must complement.
    /// </summary>
    public class EpitopeResidue
    {
        /// <summary>Residue name on target protein, e.g. "Tyr56".</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>Single-letter code of this target residue.</summary>
        public char Code { get; init; }

        /// <summary>
        /// Interaction type this residue prefers from the antibody paratope.
        /// </summary>
        public BindingRole PreferredInteraction { get; init; }

        /// <summary>
        /// Which antibody amino acid properties score best against this residue.
        /// e.g. a target Asp(-) rewards antibody Arg(+) and Lys(+).
        /// </summary>
        public List<char> ComplementaryAaCodes { get; init; } = new();

        /// <summary>
        /// Relative importance of this epitope residue (0.0–1.0).
        /// Drives how much the affinity scorer rewards/penalizes
        /// complementary vs non-complementary paratope residues.
        /// </summary>
        public double ImportanceWeight { get; init; } = 0.5;

        /// <summary>Brief note about this contact shown in UI.</summary>
        public string Note { get; init; } = string.Empty;
    }

    // --------------------------------------------------------
    //  SCORING PROFILE
    // --------------------------------------------------------

    /// <summary>
    /// Target-specific scoring weights used by AffinityScorer.
    /// Each target rewards different amino acid properties.
    /// </summary>
    public class TargetScoringProfile
    {
        /// <summary>Multiplier for aromatic residues (F,Y,W,H) at contact positions.</summary>
        public double AromaticWeight { get; init; } = 1.0;

        /// <summary>Multiplier for positively charged residues (R,K) at contact positions.</summary>
        public double PositiveChargeWeight { get; init; } = 1.0;

        /// <summary>Multiplier for negatively charged residues (D,E) at contact positions.</summary>
        public double NegativeChargeWeight { get; init; } = 1.0;

        /// <summary>Multiplier for hydrophobic residues (L,I,V,M) at contact positions.</summary>
        public double HydrophobicWeight { get; init; } = 1.0;

        /// <summary>Multiplier for polar residues (S,T,N,Q) at contact positions.</summary>
        public double PolarWeight { get; init; } = 1.0;

        /// <summary>
        /// Penalty multiplier when a critical residue is mutated.
        /// Values > 1.0 = harsher penalty.
        /// </summary>
        public double CriticalMutationPenalty { get; init; } = 2.5;

        /// <summary>
        /// Bonus affinity score awarded per CDR-H3 aromatic residue.
        /// Reflects CDR-H3 dominance in most antibody-antigen interfaces.
        /// </summary>
        public double CdrH3AromaticBonus { get; init; } = 0.15;

        /// <summary>
        /// Base affinity (nM) for a random unoptimized sequence against this target.
        /// Simulation starts near this value.
        /// </summary>
        public double BaselineAffinityNm { get; init; } = 500.0;

        /// <summary>
        /// Theoretical best affinity achievable against this target (nM).
        /// Used to normalize the score display.
        /// </summary>
        public double TheoreticalBestNm { get; init; } = 0.05;

        /// <summary>
        /// Difficulty coefficient — scales how quickly affinity improves.
        /// 1.0 = normal, 2.0 = twice as hard to improve.
        /// </summary>
        public double DifficultyCoefficient { get; init; } = 1.0;
    }

    // --------------------------------------------------------
    //  TARGET PROTEIN — CORE MODEL
    // --------------------------------------------------------

    /// <summary>
    /// Complete definition of a cancer-associated protein target.
    /// Contains all data needed for scoring, UI display,
    /// and simulation configuration.
    /// </summary>
    public class TargetProtein
    {
        // ── Identity ──────────────────────────────────────────
        public CancerTargetType TargetType { get; init; }
        public string Name { get; init; } = string.Empty;
        public string FullProteinName { get; init; } = string.Empty;
        public string GeneSymbol { get; init; } = string.Empty;

        // ── Clinical Context ──────────────────────────────────
        public List<string> CancerTypes { get; init; } = new();
        public string EpitopeDescription { get; init; } = string.Empty;
        public string EpitopeRegion { get; init; } = string.Empty;
        public EpitopeSurfaceType SurfaceType { get; init; }
        public TherapeuticMechanism Mechanism { get; init; }
        public string MechanismDescription { get; init; } = string.Empty;

        // ── Reference Data ────────────────────────────────────
        public string ReferenceAntibody { get; init; } = string.Empty;
        public string ReferenceAntibodyBrand { get; init; } = string.Empty;
        public string PdbId { get; init; } = string.Empty;
        public double ReferenceAffinityNm { get; init; }

        // ── Epitope Definition ────────────────────────────────
        public List<EpitopeResidue> KeyEpitopeResidues { get; init; } = new();

        // ── Scoring ───────────────────────────────────────────
        public TargetScoringProfile ScoringProfile { get; init; } = new();

        // ── UI Metadata ───────────────────────────────────────
        public string DifficultyLabel { get; init; } = string.Empty;
        public string DifficultyClass { get; init; } = string.Empty;  // easy/medium/hard
        public string DifficultyStars { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string Phase { get; init; } = string.Empty;  // "Active","Phase2","Phase3"
        public string SimulationHint { get; init; } = string.Empty;
        public string CssAccentClass { get; init; } = string.Empty;

        // ── Computed Helpers ──────────────────────────────────

        /// <summary>Comma-separated cancer type string for display.</summary>
        public string CancerTypeDisplay =>
            string.Join(", ", CancerTypes);

        /// <summary>Returns the most important epitope residues (top 3 by weight).</summary>
        public IEnumerable<EpitopeResidue> TopEpitopeResidues =>
            KeyEpitopeResidues
                .OrderByDescending(r => r.ImportanceWeight)
                .Take(3);

        /// <summary>True if this target has a PDB structure reference.</summary>
        public bool HasPdbReference => !string.IsNullOrWhiteSpace(PdbId);
    }

    // --------------------------------------------------------
    //  TARGET PROTEIN DATABASE
    // --------------------------------------------------------

    /// <summary>
    /// Static registry of all cancer targets.
    /// Access via TargetProteinDatabase.Get(CancerTargetType.HER2)
    /// or TargetProteinDatabase.ActiveTargets.
    /// </summary>
    public static class TargetProteinDatabase
    {
        private static readonly Dictionary<CancerTargetType, TargetProtein> _lookup;

        /// <summary>All registered targets including placeholders.</summary>
        public static IReadOnlyList<TargetProtein> All { get; }

        /// <summary>Only active (fully implemented) targets.</summary>
        public static IReadOnlyList<TargetProtein> ActiveTargets { get; }

        /// <summary>Phase 2 placeholder targets.</summary>
        public static IReadOnlyList<TargetProtein> Phase2Targets { get; }

        /// <summary>Phase 3 placeholder targets.</summary>
        public static IReadOnlyList<TargetProtein> Phase3Targets { get; }

        static TargetProteinDatabase()
        {
            var all = BuildDatabase();
            All = all.AsReadOnly();
            ActiveTargets = all.Where(t => t.Phase == "Active").ToList().AsReadOnly();
            Phase2Targets = all.Where(t => t.Phase == "Phase2").ToList().AsReadOnly();
            Phase3Targets = all.Where(t => t.Phase == "Phase3").ToList().AsReadOnly();
            _lookup = all.ToDictionary(t => t.TargetType);
        }

        /// <summary>Returns the TargetProtein for a given type, or null.</summary>
        public static TargetProtein? Get(CancerTargetType type) =>
            _lookup.TryGetValue(type, out var t) ? t : null;

        // ── Database Construction ─────────────────────────────
        private static List<TargetProtein> BuildDatabase() => new()
        {
            // ================================================
            //  HER2 — ErbB-2 Receptor
            //  Breast Cancer, Gastric Cancer
            //  Reference: Trastuzumab (Herceptin) Kd ~0.1 nM
            // ================================================
            new TargetProtein
            {
                TargetType       = CancerTargetType.HER2,
                Name             = "HER2",
                FullProteinName  = "Human Epidermal Growth Factor Receptor 2",
                GeneSymbol       = "ERBB2",
                CancerTypes      = new() { "Breast Cancer", "Gastric Cancer", "Gastroesophageal Cancer" },
                EpitopeDescription =
                    "Domain IV of the HER2 extracellular region. " +
                    "A concave, partially hydrophobic pocket flanked by charged residues. " +
                    "Antibody binding blocks receptor dimerization.",
                EpitopeRegion    = "Extracellular Domain IV",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.ReceptorBlockade,
                MechanismDescription =
                    "Blocks HER2 homodimerization and heterodimerization with HER3. " +
                    "Prevents downstream PI3K/Akt and RAS/MAPK pro-survival signaling. " +
                    "Also activates NK cell-mediated ADCC against HER2+ tumor cells.",
                ReferenceAntibody      = "Trastuzumab",
                ReferenceAntibodyBrand = "Herceptin",
                PdbId                  = "1N8Z",
                ReferenceAffinityNm    = 0.1,
                DifficultyLabel        = "Hard",
                DifficultyClass        = "hard",
                DifficultyStars        = "⭐⭐⭐",
                IsActive               = true,
                Phase                  = "Active",
                CssAccentClass         = "ah-text-cyan",

                KeyEpitopeResidues = new()
                {
                    new EpitopeResidue
                    {
                        Name = "Glu558", Code = 'E',
                        PreferredInteraction = BindingRole.ElectrostaticContact,
                        ComplementaryAaCodes = new() { 'R', 'K', 'N', 'Q' },
                        ImportanceWeight = 0.9,
                        Note = "Primary salt bridge anchor on HER2 Domain IV"
                    },
                    new EpitopeResidue
                    {
                        Name = "Ser598", Code = 'S',
                        PreferredInteraction = BindingRole.HydrogenBond,
                        ComplementaryAaCodes = new() { 'Y', 'N', 'T', 'S', 'D' },
                        ImportanceWeight = 0.85,
                        Note = "H-bond acceptor — Tyr31/Asp32 of Trastuzumab make key contacts here"
                    },
                    new EpitopeResidue
                    {
                        Name = "Thr551", Code = 'T',
                        PreferredInteraction = BindingRole.HydrogenBond,
                        ComplementaryAaCodes = new() { 'Y', 'N', 'S', 'T' },
                        ImportanceWeight = 0.75,
                        Note = "CDR-H2 contact — Asn55 H-bonds here"
                    },
                    new EpitopeResidue
                    {
                        Name = "Pro562", Code = 'P',
                        PreferredInteraction = BindingRole.HydrophobicContact,
                        ComplementaryAaCodes = new() { 'W', 'F', 'Y', 'L', 'I' },
                        ImportanceWeight = 0.70,
                        Note = "Hydrophobic pocket — CDR-H3 Trp98 buries here"
                    },
                    new EpitopeResidue
                    {
                        Name = "Glu599", Code = 'E',
                        PreferredInteraction = BindingRole.ElectrostaticContact,
                        ComplementaryAaCodes = new() { 'R', 'K', 'H' },
                        ImportanceWeight = 0.65,
                        Note = "Secondary electrostatic anchor near Domain IV tip"
                    }
                },

                ScoringProfile = new TargetScoringProfile
                {
                    AromaticWeight         = 1.6,   // Domain IV pocket rewards aromatics
                    PositiveChargeWeight   = 1.4,   // Negative epitope patch
                    NegativeChargeWeight   = 0.7,
                    HydrophobicWeight      = 1.3,
                    PolarWeight            = 1.2,
                    CriticalMutationPenalty = 3.0,
                    CdrH3AromaticBonus     = 0.20,
                    BaselineAffinityNm     = 480.0,
                    TheoreticalBestNm      = 0.05,
                    DifficultyCoefficient  = 1.8
                },

                SimulationHint =
                    "HER2 Domain IV is already well-optimized by Trastuzumab. " +
                    "The simulation may uncover W→Y swaps for better H-bond geometry, " +
                    "or T→R upgrades to strengthen the Glu558 salt bridge. " +
                    "Mutations to Trp98 (CDR-H3) will sharply reduce affinity — watch the log!"
            },

            // ================================================
            //  EGFR — Epidermal Growth Factor Receptor
            //  Colorectal, Lung, Head & Neck Cancer
            //  Reference: Cetuximab (Erbitux) Kd ~0.2 nM
            // ================================================
            new TargetProtein
            {
                TargetType       = CancerTargetType.EGFR,
                Name             = "EGFR",
                FullProteinName  = "Epidermal Growth Factor Receptor",
                GeneSymbol       = "EGFR",
                CancerTypes      = new() { "Colorectal Cancer", "Non-Small Cell Lung Cancer", "Head and Neck Cancer" },
                EpitopeDescription =
                    "Domain III of the EGFR extracellular region. " +
                    "A defined groove that overlaps the EGF ligand binding site. " +
                    "Mixture of aromatic contacts and polar interactions.",
                EpitopeRegion    = "Extracellular Domain III",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.ReceptorBlockade,
                MechanismDescription =
                    "Competitively blocks EGF and TGF-α from binding EGFR Domain III. " +
                    "Prevents receptor activation and dimerization. " +
                    "Inhibits RAS/RAF/MEK/ERK and PI3K/Akt tumor growth pathways. " +
                    "Triggers ADCC and complement-dependent cytotoxicity.",
                ReferenceAntibody      = "Cetuximab",
                ReferenceAntibodyBrand = "Erbitux",
                PdbId                  = "1YY9",
                ReferenceAffinityNm    = 0.2,
                DifficultyLabel        = "Medium",
                DifficultyClass        = "medium",
                DifficultyStars        = "⭐⭐",
                IsActive               = true,
                Phase                  = "Active",
                CssAccentClass         = "ah-text-gold",

                KeyEpitopeResidues = new()
                {
                    new EpitopeResidue
                    {
                        Name = "Arg353", Code = 'R',
                        PreferredInteraction = BindingRole.ElectrostaticContact,
                        ComplementaryAaCodes = new() { 'D', 'E', 'N', 'Y' },
                        ImportanceWeight = 0.95,
                        Note = "Critical positive anchor — most important EGFR contact residue"
                    },
                    new EpitopeResidue
                    {
                        Name = "Asp355", Code = 'D',
                        PreferredInteraction = BindingRole.HydrogenBond,
                        ComplementaryAaCodes = new() { 'N', 'R', 'K', 'S', 'Y' },
                        ImportanceWeight = 0.85,
                        Note = "CDR-H1 Asn31 H-bonds here — essential for specificity"
                    },
                    new EpitopeResidue
                    {
                        Name = "Gln408", Code = 'Q',
                        PreferredInteraction = BindingRole.HydrogenBond,
                        ComplementaryAaCodes = new() { 'N', 'Y', 'W', 'S' },
                        ImportanceWeight = 0.80,
                        Note = "CDR-H2 Tyr59 contacts — polar groove anchor"
                    },
                    new EpitopeResidue
                    {
                        Name = "Ser418", Code = 'S',
                        PreferredInteraction = BindingRole.HydrogenBond,
                        ComplementaryAaCodes = new() { 'N', 'Y', 'T', 'H' },
                        ImportanceWeight = 0.75,
                        Note = "CDR-H1 Tyr32 / Asn31 contact site"
                    },
                    new EpitopeResidue
                    {
                        Name = "His409", Code = 'H',
                        PreferredInteraction = BindingRole.AromaticStacking,
                        ComplementaryAaCodes = new() { 'W', 'Y', 'F', 'H' },
                        ImportanceWeight = 0.70,
                        Note = "π-stacking site — Cetuximab Trp52 drives deep aromatic contact"
                    }
                },

                ScoringProfile = new TargetScoringProfile
                {
                    AromaticWeight         = 1.8,   // Deep aromatic groove
                    PositiveChargeWeight   = 0.8,
                    NegativeChargeWeight   = 1.5,   // Rewards D/E to complement Arg353
                    HydrophobicWeight      = 1.2,
                    PolarWeight            = 1.3,
                    CriticalMutationPenalty = 2.5,
                    CdrH3AromaticBonus     = 0.18,
                    BaselineAffinityNm     = 420.0,
                    TheoreticalBestNm      = 0.08,
                    DifficultyCoefficient  = 1.3    // Medium difficulty
                },

                SimulationHint =
                    "EGFR Domain III has a well-defined aromatic groove. " +
                    "The simulation often finds N→Y and S→W upgrades in CDR-H2, " +
                    "and H→R swaps in CDR-H3 to better complement Arg353. " +
                    "This is a Medium difficulty target — expect 3-6x affinity gains."
            },

            // ================================================
            //  PD-L1 — Programmed Death Ligand 1
            //  Pan-cancer immunotherapy target
            //  Reference: Atezolizumab (Tecentriq) Kd ~0.4 nM
            // ================================================
            new TargetProtein
            {
                TargetType       = CancerTargetType.PDL1,
                Name             = "PD-L1",
                FullProteinName  = "Programmed Death Ligand 1",
                GeneSymbol       = "CD274",
                CancerTypes      = new()
                {
                    "Bladder Cancer", "Non-Small Cell Lung Cancer",
                    "Triple-Negative Breast Cancer", "Hepatocellular Carcinoma",
                    "Melanoma", "Cervical Cancer"
                },
                EpitopeDescription =
                    "IgV domain of PD-L1 — the PD-1 binding face. " +
                    "A relatively flat hydrophobic surface with sparse polar contacts. " +
                    "Among the most challenging antibody epitopes due to flat topology.",
                EpitopeRegion    = "IgV Domain — PD-1 Binding Face",
                SurfaceType      = EpitopeSurfaceType.Flat,
                Mechanism        = TherapeuticMechanism.ImmuneCheckpointBlock,
                MechanismDescription =
                    "Blocks PD-L1 from engaging PD-1 and CD80 on T cells. " +
                    "Prevents T cell exhaustion in the tumor microenvironment. " +
                    "Restores cytotoxic T cell activity against tumor cells. " +
                    "Does not block PD-L2, preserving peripheral immune homeostasis.",
                ReferenceAntibody      = "Atezolizumab",
                ReferenceAntibodyBrand = "Tecentriq",
                PdbId                  = "5XXY",
                ReferenceAffinityNm    = 0.4,
                DifficultyLabel        = "Hard",
                DifficultyClass        = "hard",
                DifficultyStars        = "⭐⭐⭐",
                IsActive               = true,
                Phase                  = "Active",
                CssAccentClass         = "ah-text-green",

                KeyEpitopeResidues = new()
                {
                    new EpitopeResidue
                    {
                        Name = "Tyr56", Code = 'Y',
                        PreferredInteraction = BindingRole.AromaticStacking,
                        ComplementaryAaCodes = new() { 'W', 'Y', 'F', 'H', 'D' },
                        ImportanceWeight = 0.95,
                        Note = "Central aromatic anchor — most critical PD-L1 contact"
                    },
                    new EpitopeResidue
                    {
                        Name = "Glu58", Code = 'E',
                        PreferredInteraction = BindingRole.ElectrostaticContact,
                        ComplementaryAaCodes = new() { 'R', 'K', 'N', 'S' },
                        ImportanceWeight = 0.85,
                        Note = "CDR-H1 Asp31 and Ser33 contact — electrostatic anchor"
                    },
                    new EpitopeResidue
                    {
                        Name = "Arg113", Code = 'R',
                        PreferredInteraction = BindingRole.ElectrostaticContact,
                        ComplementaryAaCodes = new() { 'D', 'E', 'Y', 'N' },
                        ImportanceWeight = 0.80,
                        Note = "CDR-H2 Tyr56 H-bonds here — key specificity contact"
                    },
                    new EpitopeResidue
                    {
                        Name = "Asp61", Code = 'D',
                        PreferredInteraction = BindingRole.ElectrostaticContact,
                        ComplementaryAaCodes = new() { 'R', 'K', 'H' },
                        ImportanceWeight = 0.75,
                        Note = "CDR-H3 Arg95 salt bridge — primary CDR-H3 anchor"
                    },
                    new EpitopeResidue
                    {
                        Name = "Met115", Code = 'M',
                        PreferredInteraction = BindingRole.HydrophobicContact,
                        ComplementaryAaCodes = new() { 'W', 'F', 'Y', 'L', 'I' },
                        ImportanceWeight = 0.70,
                        Note = "Hydrophobic pocket — CDR-H3 Trp106 buries here"
                    }
                },

                ScoringProfile = new TargetScoringProfile
                {
                    AromaticWeight         = 2.0,   // Flat surface REQUIRES aromatics
                    PositiveChargeWeight   = 1.5,   // Complement negative epitope patch
                    NegativeChargeWeight   = 1.3,   // Complement Arg113
                    HydrophobicWeight      = 1.4,
                    PolarWeight            = 1.0,
                    CriticalMutationPenalty = 3.5,  // Flat epitope punishes mistakes hard
                    CdrH3AromaticBonus     = 0.25,  // CDR-H3 is critical here
                    BaselineAffinityNm     = 600.0, // Hardest baseline
                    TheoreticalBestNm      = 0.05,
                    DifficultyCoefficient  = 2.0    // Hardest target
                },

                SimulationHint =
                    "PD-L1 is a flat epitope — the hardest class of antibody target. " +
                    "The simulation will strongly select for Trp and Tyr residues throughout CDRs. " +
                    "Watch for S→W and N→Y upgrades. CDR-H3 Trp106 is essential — " +
                    "mutations here cause catastrophic affinity loss. " +
                    "Expect 2-4x improvements — even small gains are significant here."
            },

            // ================================================
            //  PHASE 2 PLACEHOLDERS
            //  Skeleton data — UI shows as locked
            // ================================================

            new TargetProtein
            {
                TargetType       = CancerTargetType.CD20,
                Name             = "CD20",
                FullProteinName  = "B-Lymphocyte Antigen CD20",
                GeneSymbol       = "MS4A1",
                CancerTypes      = new() { "B-Cell Non-Hodgkin Lymphoma", "Chronic Lymphocytic Leukemia" },
                EpitopeDescription = "Loop regions of the CD20 tetraspanin extracellular domain.",
                EpitopeRegion    = "Extracellular Loop 2",
                SurfaceType      = EpitopeSurfaceType.Hydrophobic,
                Mechanism        = TherapeuticMechanism.ADCC,
                ReferenceAntibody = "Rituximab",
                ReferenceAntibodyBrand = "Rituxan",
                PdbId            = "",
                ReferenceAffinityNm = 8.0,
                DifficultyLabel  = "Medium",
                DifficultyClass  = "medium",
                DifficultyStars  = "⭐⭐",
                IsActive         = false,
                Phase            = "Phase2",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile { BaselineAffinityNm = 400 },
                SimulationHint   = "Coming in Phase 2."
            },

            new TargetProtein
            {
                TargetType       = CancerTargetType.VEGFR2,
                Name             = "VEGFR2",
                FullProteinName  = "Vascular Endothelial Growth Factor Receptor 2",
                GeneSymbol       = "KDR",
                CancerTypes      = new() { "Solid Tumors", "Gastric Cancer", "Non-Small Cell Lung Cancer" },
                EpitopeDescription = "Immunoglobulin-like domains 2-3 of VEGFR2 extracellular region.",
                EpitopeRegion    = "IgG-like Domains 2-3",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.AngiogenesisBlock,
                ReferenceAntibody = "Ramucirumab",
                ReferenceAntibodyBrand = "Cyramza",
                PdbId            = "",
                ReferenceAffinityNm = 1.0,
                DifficultyLabel  = "Medium",
                DifficultyClass  = "medium",
                DifficultyStars  = "⭐⭐",
                IsActive         = false,
                Phase            = "Phase2",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile { BaselineAffinityNm = 450 },
                SimulationHint   = "Coming in Phase 2."
            },

            new TargetProtein
            {
                TargetType       = CancerTargetType.BCMA,
                Name             = "BCMA",
                FullProteinName  = "B-Cell Maturation Antigen",
                GeneSymbol       = "TNFRSF17",
                CancerTypes      = new() { "Multiple Myeloma" },
                EpitopeDescription = "Cysteine-rich domain of BCMA extracellular region.",
                EpitopeRegion    = "Cysteine-Rich Domain",
                SurfaceType      = EpitopeSurfaceType.Charged,
                Mechanism        = TherapeuticMechanism.DirectCytotoxicity,
                ReferenceAntibody = "Belantamab mafodotin",
                ReferenceAntibodyBrand = "Blenrep",
                PdbId            = "",
                ReferenceAffinityNm = 0.6,
                DifficultyLabel  = "Hard",
                DifficultyClass  = "hard",
                DifficultyStars  = "⭐⭐⭐",
                IsActive         = false,
                Phase            = "Phase2",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile { BaselineAffinityNm = 500 },
                SimulationHint   = "Coming in Phase 2."
            },

            new TargetProtein
            {
                TargetType       = CancerTargetType.CD19,
                Name             = "CD19",
                FullProteinName  = "B-Lymphocyte Antigen CD19",
                GeneSymbol       = "CD19",
                CancerTypes      = new() { "Acute Lymphoblastic Leukemia", "B-Cell Lymphoma" },
                EpitopeDescription = "Extracellular immunoglobulin-like domain of CD19.",
                EpitopeRegion    = "Ig-like Domain",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.DirectCytotoxicity,
                ReferenceAntibody = "Tafasitamab",
                ReferenceAntibodyBrand = "Monjuvi",
                PdbId            = "",
                ReferenceAffinityNm = 2.0,
                DifficultyLabel  = "Easy",
                DifficultyClass  = "easy",
                DifficultyStars  = "⭐",
                IsActive         = false,
                Phase            = "Phase2",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile { BaselineAffinityNm = 350 },
                SimulationHint   = "Coming in Phase 2."
            },

            // ================================================
            //  PHASE 3 PLACEHOLDERS
            // ================================================

            new TargetProtein
            {
                TargetType       = CancerTargetType.CTLA4,
                Name             = "CTLA-4",
                FullProteinName  = "Cytotoxic T-Lymphocyte Associated Protein 4",
                GeneSymbol       = "CTLA4",
                CancerTypes      = new() { "Melanoma", "Non-Small Cell Lung Cancer", "Renal Cell Carcinoma" },
                EpitopeDescription = "IgV domain ligand-binding face — blocks CD80/CD86 binding.",
                EpitopeRegion    = "IgV Domain",
                SurfaceType      = EpitopeSurfaceType.Flat,
                Mechanism        = TherapeuticMechanism.ImmuneCheckpointBlock,
                ReferenceAntibody = "Ipilimumab",
                ReferenceAntibodyBrand = "Yervoy",
                IsActive         = false,
                Phase            = "Phase3",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile(),
                SimulationHint   = "Coming in Phase 3."
            },

            new TargetProtein
            {
                TargetType       = CancerTargetType.CEA,
                Name             = "CEA",
                FullProteinName  = "Carcinoembryonic Antigen",
                GeneSymbol       = "CEACAM5",
                CancerTypes      = new() { "Colorectal Cancer", "Lung Cancer", "Pancreatic Cancer" },
                EpitopeDescription = "N-terminal IgV domain of CEA — overexpressed on tumor surfaces.",
                EpitopeRegion    = "N-terminal IgV Domain",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.TumorAssociatedAntigen,
                ReferenceAntibody = "Labetuzumab",
                ReferenceAntibodyBrand = "CEA-Scan",
                IsActive         = false,
                Phase            = "Phase3",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile(),
                SimulationHint   = "Coming in Phase 3."
            },

            new TargetProtein
            {
                TargetType       = CancerTargetType.MUC1,
                Name             = "MUC1",
                FullProteinName  = "Mucin-1 / Polymorphic Epithelial Mucin",
                GeneSymbol       = "MUC1",
                CancerTypes      = new() { "Breast Cancer", "Ovarian Cancer", "Pancreatic Cancer" },
                EpitopeDescription = "VNTR repeat domain of the MUC1 extracellular mucin core.",
                EpitopeRegion    = "VNTR Repeat Domain",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.TumorAssociatedAntigen,
                ReferenceAntibody = "Cantuzumab",
                ReferenceAntibodyBrand = "—",
                IsActive         = false,
                Phase            = "Phase3",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile(),
                SimulationHint   = "Coming in Phase 3."
            },

            new TargetProtein
            {
                TargetType       = CancerTargetType.Custom,
                Name             = "Custom Target",
                FullProteinName  = "User-Defined Cancer Protein",
                GeneSymbol       = "—",
                CancerTypes      = new() { "User-Defined" },
                EpitopeDescription = "Paste your own epitope sequence to simulate against.",
                EpitopeRegion    = "User-Defined",
                SurfaceType      = EpitopeSurfaceType.Mixed,
                Mechanism        = TherapeuticMechanism.ReceptorBlockade,
                ReferenceAntibody = "—",
                IsActive         = false,
                Phase            = "Phase3",
                CssAccentClass   = "ah-text-muted",
                ScoringProfile   = new TargetScoringProfile(),
                SimulationHint   = "Coming in Phase 3."
            }
        };
    }
}