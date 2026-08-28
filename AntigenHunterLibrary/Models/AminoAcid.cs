// ============================================================
//  AminoAcid.cs
//  AntigenHunter — Complete Amino Acid Database
//  All 20 standard amino acids with physicochemical
//  properties, substitution groups, and antibody binding roles.
//  ⚔️ Forged by Claude AI
// ============================================================

using System.Collections.Generic;

namespace AntigenHunterLibrary.Models
{
    // --------------------------------------------------------
    //  ENUMS
    // --------------------------------------------------------

    /// <summary>
    /// Physicochemical property group of an amino acid.
    /// Used for coloring, mutation logic, and scoring.
    /// </summary>
    public enum AminoAcidGroup
    {
        NonpolarAliphatic,   // A, G, V, L, I, M, P
        AromaticHydrophobic, // F, Y, W
        PolarUncharged,      // S, T, N, Q, C
        PositiveCharged,     // R, K, H
        NegativeCharged      // D, E
    }

    /// <summary>
    /// How radical a substitution is — affects ΔΔG scoring.
    /// </summary>
    public enum SubstitutionType
    {
        Conservative,   // Similar properties, small energy penalty
        SemiConservative, // Moderate change
        Radical         // Large property change, high energy cost/gain
    }

    /// <summary>
    /// Role this amino acid typically plays in antibody-antigen binding.
    /// </summary>
    public enum BindingRole
    {
        Framework,          // Structural, rarely mutated in CDRs
        HydrogenBond,       // Donates or accepts H-bonds with antigen
        HydrophobicContact, // Nonpolar packing against antigen surface
        ElectrostaticContact, // Charge-based interaction
        AromaticStacking,   // π-π or cation-π stacking
        DisulfideBond,      // Cysteine-specific structural role
        LoopFlexibility,    // Glycine/Proline — shape CDR loops
        SHMHotspot          // Serine/Threonine — frequent SHM targets
    }

    // --------------------------------------------------------
    //  SUBSTITUTION RECORD
    // --------------------------------------------------------

    /// <summary>
    /// Describes a valid amino acid substitution with its type and
    /// estimated affinity impact direction.
    /// </summary>
    public class AminoAcidSubstitution
    {
        /// <summary>Single-letter code of the target amino acid.</summary>
        public char TargetCode { get; init; }

        /// <summary>How conservative or radical this swap is.</summary>
        public SubstitutionType Type { get; init; }

        /// <summary>
        /// Rough ΔΔG bias in kcal/mol.
        /// Negative = likely stabilizing (better affinity).
        /// Positive = likely destabilizing.
        /// Zero = neutral / context-dependent.
        /// </summary>
        public double DeltaDeltaG { get; init; }

        /// <summary>Human-readable note on why this swap matters.</summary>
        public string Note { get; init; } = string.Empty;

        public AminoAcidSubstitution(
            char targetCode,
            SubstitutionType type,
            double deltaDeltaG,
            string note)
        {
            TargetCode = targetCode;
            Type = type;
            DeltaDeltaG = deltaDeltaG;
            Note = note;
        }
    }

    // --------------------------------------------------------
    //  AMINO ACID MODEL
    // --------------------------------------------------------

    /// <summary>
    /// Full descriptor for one of the 20 standard amino acids.
    /// </summary>
    public class AminoAcid
    {
        // ── Identity ──────────────────────────────────────────
        /// <summary>Single-letter IUPAC code (e.g. 'A').</summary>
        public char Code { get; init; }

        /// <summary>Three-letter abbreviation (e.g. "Ala").</summary>
        public string ThreeLetter { get; init; } = string.Empty;

        /// <summary>Full chemical name.</summary>
        public string FullName { get; init; } = string.Empty;

        // ── Classification ────────────────────────────────────
        /// <summary>Property group — drives coloring and mutation grouping.</summary>
        public AminoAcidGroup Group { get; init; }

        /// <summary>Primary binding role in antibody paratope.</summary>
        public BindingRole PrimaryRole { get; init; }

        // ── Physicochemical Properties ────────────────────────
        /// <summary>
        /// Kyte-Doolittle hydrophobicity scale.
        /// Higher = more hydrophobic. Range: -4.5 (Arg) to +4.5 (Ile).
        /// </summary>
        public double Hydrophobicity { get; init; }

        /// <summary>
        /// Molecular weight in Daltons.
        /// </summary>
        public double MolecularWeight { get; init; }

        /// <summary>
        /// Side chain charge at physiological pH (7.4).
        /// +1, 0, or -1.
        /// </summary>
        public int Charge { get; init; }

        /// <summary>
        /// Polarity score (0 = nonpolar, 1 = polar, 2 = charged).
        /// </summary>
        public int Polarity { get; init; }

        /// <summary>
        /// Volume of side chain in ų. Affects steric fit.
        /// </summary>
        public double SideChainVolume { get; init; }

        /// <summary>
        /// True if aromatic ring present — enables π-stacking.
        /// </summary>
        public bool IsAromatic { get; init; }

        /// <summary>
        /// True if this residue commonly appears in SHM hotspot motifs.
        /// </summary>
        public bool IsSHMHotspot { get; init; }

        /// <summary>
        /// True if this residue is frequently found at CDR-antigen interfaces.
        /// Based on structural database analysis.
        /// </summary>
        public bool IsCommonContactResidue { get; init; }

        // ── Binding Affinity Contribution ─────────────────────
        /// <summary>
        /// Base affinity contribution score when found at a contact position.
        /// Used by AffinityScorer. Range: 0.0 – 1.0.
        /// Higher = stronger typical contribution to binding.
        /// </summary>
        public double BaseAffinityScore { get; init; }

        // ── Substitution Map ──────────────────────────────────
        /// <summary>
        /// Valid substitutions with energy and type annotations.
        /// </summary>
        public List<AminoAcidSubstitution> ValidSubstitutions { get; init; } = new();

        // ── Description ───────────────────────────────────────
        /// <summary>Human-readable role description shown in the UI panel.</summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>CSS class suffix for theming (maps to AntigenHunter.css groups).</summary>
        public string CssGroup { get; init; } = string.Empty;
    }

    // --------------------------------------------------------
    //  AMINO ACID DATABASE (Static Registry)
    // --------------------------------------------------------

    /// <summary>
    /// Static lookup for all 20 standard amino acids.
    /// Access via AminoAcidDatabase.Get('A') or .All.
    /// </summary>
    public static class AminoAcidDatabase
    {
        private static readonly Dictionary<char, AminoAcid> _lookup;

        /// <summary>All 20 amino acids in canonical order.</summary>
        public static IReadOnlyList<AminoAcid> All { get; }

        static AminoAcidDatabase()
        {
            var entries = BuildDatabase();
            All = entries.AsReadOnly();
            _lookup = new Dictionary<char, AminoAcid>(20);
            foreach (var aa in entries)
                _lookup[aa.Code] = aa;
        }

        /// <summary>
        /// Returns the AminoAcid for a given single-letter code.
        /// Returns null if code is not a valid standard amino acid.
        /// </summary>
        public static AminoAcid? Get(char code) =>
            _lookup.TryGetValue(char.ToUpper(code), out var aa) ? aa : null;

        /// <summary>Returns true if the character is a valid amino acid code.</summary>
        public static bool IsValid(char code) =>
            _lookup.ContainsKey(char.ToUpper(code));

        /// <summary>
        /// Returns all amino acids in a given property group.
        /// </summary>
        public static IEnumerable<AminoAcid> GetGroup(AminoAcidGroup group)
        {
            foreach (var aa in All)
                if (aa.Group == group) yield return aa;
        }

        // ── Database Construction ─────────────────────────────
        private static List<AminoAcid> BuildDatabase() => new()
        {
            // ================================================
            //  NONPOLAR ALIPHATIC
            // ================================================

            new AminoAcid
            {
                Code = 'A', ThreeLetter = "Ala", FullName = "Alanine",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.Framework,
                Hydrophobicity = 1.8, MolecularWeight = 89.09,
                Charge = 0, Polarity = 0, SideChainVolume = 67.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = false,
                BaseAffinityScore = 0.25,
                CssGroup = "nonpolar",
                Description = "Smallest hydrophobic AA. Stabilizes framework helices. Common substitution target in SHM — replaces Gly to rigidify CDR loops.",
                ValidSubstitutions = new()
                {
                    new('G', SubstitutionType.Conservative,    +0.5, "Removes methyl, adds flexibility — loosens CDR loop"),
                    new('V', SubstitutionType.Conservative,    +0.3, "Adds bulk, conservative hydrophobic swap"),
                    new('S', SubstitutionType.SemiConservative, 0.0, "Adds polar hydroxyl — can gain new H-bond with antigen"),
                    new('T', SubstitutionType.SemiConservative,-0.2, "SHM hotspot swap — often improves affinity"),
                    new('L', SubstitutionType.SemiConservative,+0.6, "Larger hydrophobic — may clash in tight packing"),
                }
            },

            new AminoAcid
            {
                Code = 'G', ThreeLetter = "Gly", FullName = "Glycine",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.LoopFlexibility,
                Hydrophobicity = -0.4, MolecularWeight = 75.03,
                Charge = 0, Polarity = 0, SideChainVolume = 48.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = false,
                BaseAffinityScore = 0.15,
                CssGroup = "nonpolar",
                Description = "No side chain. Unique conformational flexibility. Critical in CDR loop hinges — especially CDR-H3 base. Mutation is usually destabilizing.",
                ValidSubstitutions = new()
                {
                    new('A', SubstitutionType.Conservative,    +0.4, "Adds methyl — rigidifies loop slightly"),
                    new('S', SubstitutionType.SemiConservative,+0.8, "Adds polar group — often disrupts loop geometry"),
                    new('V', SubstitutionType.Radical,         +1.2, "Branched chain may cause steric clash in tight loops"),
                }
            },

            new AminoAcid
            {
                Code = 'V', ThreeLetter = "Val", FullName = "Valine",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.HydrophobicContact,
                Hydrophobicity = 4.2, MolecularWeight = 117.15,
                Charge = 0, Polarity = 0, SideChainVolume = 105.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = false,
                BaseAffinityScore = 0.35,
                CssGroup = "nonpolar",
                Description = "Branched hydrophobic. Good for hydrophobic core packing. Moderate size makes it versatile in CDR contact positions.",
                ValidSubstitutions = new()
                {
                    new('L', SubstitutionType.Conservative,    -0.2, "Larger chain — may deepen hydrophobic contact"),
                    new('I', SubstitutionType.Conservative,    -0.1, "Similar bulk, good conservative swap"),
                    new('A', SubstitutionType.Conservative,    +0.4, "Smaller — may lose hydrophobic contact area"),
                    new('T', SubstitutionType.SemiConservative, 0.0, "Adds hydroxyl — can gain H-bond"),
                    new('M', SubstitutionType.SemiConservative,-0.3, "Sulfur chain — flexible, good contact residue"),
                }
            },

            new AminoAcid
            {
                Code = 'L', ThreeLetter = "Leu", FullName = "Leucine",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.HydrophobicContact,
                Hydrophobicity = 3.8, MolecularWeight = 131.17,
                Charge = 0, Polarity = 0, SideChainVolume = 124.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.45,
                CssGroup = "nonpolar",
                Description = "Large hydrophobic. One of the most common CDR contact residues. Provides strong nonpolar interaction patches with antigen hydrophobic pockets.",
                ValidSubstitutions = new()
                {
                    new('I', SubstitutionType.Conservative,    0.0,  "Near-identical — branching differs slightly"),
                    new('V', SubstitutionType.Conservative,    +0.3, "Smaller — may weaken hydrophobic contact"),
                    new('M', SubstitutionType.Conservative,    -0.2, "Sulfur adds flexibility, often improves fit"),
                    new('F', SubstitutionType.SemiConservative,-0.5, "Aromatic upgrade — can gain π contacts"),
                    new('A', SubstitutionType.Radical,         +1.0, "Drastic size reduction — likely affinity loss"),
                }
            },

            new AminoAcid
            {
                Code = 'I', ThreeLetter = "Ile", FullName = "Isoleucine",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.HydrophobicContact,
                Hydrophobicity = 4.5, MolecularWeight = 131.17,
                Charge = 0, Polarity = 0, SideChainVolume = 124.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.45,
                CssGroup = "nonpolar",
                Description = "Most hydrophobic standard AA. Excellent for deep hydrophobic pockets. Branch at β-carbon gives rigid packing geometry.",
                ValidSubstitutions = new()
                {
                    new('L', SubstitutionType.Conservative,    0.0,  "Isomers — very similar hydrophobic contribution"),
                    new('V', SubstitutionType.Conservative,    +0.3, "Slightly smaller — minor affinity reduction"),
                    new('M', SubstitutionType.Conservative,    -0.1, "Flexible sulfur chain can improve fit"),
                    new('F', SubstitutionType.SemiConservative,-0.4, "π contacts can compensate for lost van der Waals"),
                }
            },

            new AminoAcid
            {
                Code = 'M', ThreeLetter = "Met", FullName = "Methionine",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.HydrophobicContact,
                Hydrophobicity = 1.9, MolecularWeight = 149.21,
                Charge = 0, Polarity = 0, SideChainVolume = 124.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = false,
                BaseAffinityScore = 0.35,
                CssGroup = "nonpolar",
                Description = "Long flexible chain with terminal sulfur. Surprisingly versatile CDR residue — sulfur can accept weak H-bonds and provides flexible packing.",
                ValidSubstitutions = new()
                {
                    new('L', SubstitutionType.Conservative,    +0.2, "Loses sulfur flexibility but retains bulk"),
                    new('I', SubstitutionType.Conservative,    +0.2, "Loses flexibility, rigid branched chain"),
                    new('V', SubstitutionType.Conservative,    +0.5, "Significantly smaller"),
                    new('Q', SubstitutionType.SemiConservative, 0.0, "Polar swap — gains H-bond capability"),
                }
            },

            new AminoAcid
            {
                Code = 'P', ThreeLetter = "Pro", FullName = "Proline",
                Group = AminoAcidGroup.NonpolarAliphatic,
                PrimaryRole = BindingRole.LoopFlexibility,
                Hydrophobicity = -1.6, MolecularWeight = 115.13,
                Charge = 0, Polarity = 0, SideChainVolume = 90.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = false,
                BaseAffinityScore = 0.1,
                CssGroup = "special",
                Description = "Cyclic — creates rigid kinks in loops. Cannot donate backbone N-H bonds. Defines CDR loop constraints. Mutation almost always disrupts loop geometry.",
                ValidSubstitutions = new()
                {
                    new('A', SubstitutionType.Radical,         +1.5, "Removes rigidity — often catastrophic for CDR shape"),
                    new('G', SubstitutionType.Radical,         +1.0, "Hyperflex replacement — alters loop trajectory"),
                    new('V', SubstitutionType.Radical,         +1.2, "Removes cyclic constraint, adds bulk"),
                }
            },

            // ================================================
            //  AROMATIC / HYDROPHOBIC
            // ================================================

            new AminoAcid
            {
                Code = 'F', ThreeLetter = "Phe", FullName = "Phenylalanine",
                Group = AminoAcidGroup.AromaticHydrophobic,
                PrimaryRole = BindingRole.AromaticStacking,
                Hydrophobicity = 2.8, MolecularWeight = 165.19,
                Charge = 0, Polarity = 0, SideChainVolume = 135.0,
                IsAromatic = true, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.65,
                CssGroup = "nonpolar",
                Description = "Large aromatic ring. Excellent for π-stacking and cation-π interactions. Commonly found at antibody-antigen interfaces, especially CDR-H3.",
                ValidSubstitutions = new()
                {
                    new('Y', SubstitutionType.Conservative,    -0.4, "Adds hydroxyl — can gain H-bond, very common SHM gain"),
                    new('W', SubstitutionType.Conservative,    -0.3, "Larger aromatic — stronger π contacts"),
                    new('L', SubstitutionType.SemiConservative,+0.4, "Loses aromaticity — reduces π stacking capability"),
                    new('H', SubstitutionType.SemiConservative,-0.1, "Smaller aromatic, adds charge capability"),
                    new('A', SubstitutionType.Radical,         +1.5, "Drastic — loses all aromatic character"),
                }
            },

            new AminoAcid
            {
                Code = 'Y', ThreeLetter = "Tyr", FullName = "Tyrosine",
                Group = AminoAcidGroup.AromaticHydrophobic,
                PrimaryRole = BindingRole.AromaticStacking,
                Hydrophobicity = -1.3, MolecularWeight = 181.19,
                Charge = 0, Polarity = 1, SideChainVolume = 141.0,
                IsAromatic = true, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.85,
                CssGroup = "nonpolar",
                Description = "THE most important CDR contact residue. Aromatic ring + hydroxyl = dual capability: π-stacking AND H-bonding. Overrepresented in CDR-H3 by 4x statistical expectation.",
                ValidSubstitutions = new()
                {
                    new('F', SubstitutionType.Conservative,    +0.3, "Loses H-bond donor — usually reduces affinity"),
                    new('W', SubstitutionType.Conservative,    -0.2, "Larger aromatic — better π packing, loses H-bond"),
                    new('H', SubstitutionType.SemiConservative,-0.3, "Smaller, adds pH-sensitive charge — sometimes useful"),
                    new('S', SubstitutionType.Radical,         +1.2, "Retains hydroxyl but loses aromatic — big affinity loss"),
                    new('N', SubstitutionType.Radical,         +0.8, "Loses aromaticity, retains some polar character"),
                }
            },

            new AminoAcid
            {
                Code = 'W', ThreeLetter = "Trp", FullName = "Tryptophan",
                Group = AminoAcidGroup.AromaticHydrophobic,
                PrimaryRole = BindingRole.AromaticStacking,
                Hydrophobicity = -0.9, MolecularWeight = 204.23,
                Charge = 0, Polarity = 1, SideChainVolume = 163.0,
                IsAromatic = true, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.80,
                CssGroup = "nonpolar",
                Description = "Largest standard AA. Bicyclic indole ring provides exceptional π-stacking surface. NH donor capable of H-bonds. Found at ~20% of antibody-antigen interfaces despite low natural frequency.",
                ValidSubstitutions = new()
                {
                    new('Y', SubstitutionType.Conservative,    +0.3, "Smaller aromatic — loses some stacking surface"),
                    new('F', SubstitutionType.Conservative,    +0.5, "Loses NH donor and ring size"),
                    new('H', SubstitutionType.SemiConservative,+0.6, "Much smaller aromatic"),
                    new('L', SubstitutionType.Radical,         +1.4, "Loses all aromatic character — large affinity impact"),
                }
            },

            // ================================================
            //  POLAR UNCHARGED
            // ================================================

            new AminoAcid
            {
                Code = 'S', ThreeLetter = "Ser", FullName = "Serine",
                Group = AminoAcidGroup.PolarUncharged,
                PrimaryRole = BindingRole.SHMHotspot,
                Hydrophobicity = -0.8, MolecularWeight = 105.09,
                Charge = 0, Polarity = 1, SideChainVolume = 73.0,
                IsAromatic = false, IsSHMHotspot = true, IsCommonContactResidue = true,
                BaseAffinityScore = 0.55,
                CssGroup = "polar",
                Description = "Primary SHM hotspot residue. Small hydroxyl group enables H-bonds. Commonly mutated to Tyr or Asn during affinity maturation — often the single most impactful substitution.",
                ValidSubstitutions = new()
                {
                    new('T', SubstitutionType.Conservative,    -0.1, "Methyl addition — conservative, common SHM"),
                    new('N', SubstitutionType.Conservative,    -0.2, "Larger polar — often improves H-bond network"),
                    new('Y', SubstitutionType.SemiConservative,-0.8, "Classic SHM upgrade — aromatic + H-bond"),
                    new('A', SubstitutionType.SemiConservative,+0.5, "Loses hydroxyl — reduces polar contacts"),
                    new('G', SubstitutionType.SemiConservative,+0.7, "Gains flexibility but loses H-bond"),
                    new('D', SubstitutionType.Radical,         -0.3, "Gains negative charge — context-dependent"),
                }
            },

            new AminoAcid
            {
                Code = 'T', ThreeLetter = "Thr", FullName = "Threonine",
                Group = AminoAcidGroup.PolarUncharged,
                PrimaryRole = BindingRole.SHMHotspot,
                Hydrophobicity = -0.7, MolecularWeight = 119.12,
                Charge = 0, Polarity = 1, SideChainVolume = 93.0,
                IsAromatic = false, IsSHMHotspot = true, IsCommonContactResidue = true,
                BaseAffinityScore = 0.50,
                CssGroup = "polar",
                Description = "Second most common SHM hotspot. Branched polar chain. β-hydroxyl donates H-bonds. Thr→Ser is the most statistically common SHM mutation observed in vivo.",
                ValidSubstitutions = new()
                {
                    new('S', SubstitutionType.Conservative,    0.0,  "Loses methyl — most common SHM swap"),
                    new('A', SubstitutionType.SemiConservative,+0.6, "Loses hydroxyl — polar contact loss"),
                    new('N', SubstitutionType.SemiConservative,-0.1, "Larger amide — can improve H-bond geometry"),
                    new('Y', SubstitutionType.Radical,         -0.6, "Major aromatic gain — can dramatically improve affinity"),
                    new('V', SubstitutionType.SemiConservative,+0.4, "Loses polarity — gains hydrophobicity"),
                }
            },

            new AminoAcid
            {
                Code = 'N', ThreeLetter = "Asn", FullName = "Asparagine",
                Group = AminoAcidGroup.PolarUncharged,
                PrimaryRole = BindingRole.HydrogenBond,
                Hydrophobicity = -3.5, MolecularWeight = 132.12,
                Charge = 0, Polarity = 1, SideChainVolume = 96.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.60,
                CssGroup = "polar",
                Description = "Amide group is both H-bond donor and acceptor. Highly versatile in CDR loops. Common at CDR-H3 tip positions. Can form bidentate H-bonds with antigen backbone.",
                ValidSubstitutions = new()
                {
                    new('D', SubstitutionType.Conservative,    -0.2, "Adds negative charge — can strengthen polar contacts"),
                    new('S', SubstitutionType.Conservative,    +0.2, "Smaller polar — retains H-bond, slight volume loss"),
                    new('Q', SubstitutionType.Conservative,    -0.1, "Larger amide — may reach further into epitope"),
                    new('H', SubstitutionType.SemiConservative,-0.3, "Aromatic polar — adds π capability"),
                    new('Y', SubstitutionType.Radical,         -0.5, "Major aromatic upgrade for contact positions"),
                }
            },

            new AminoAcid
            {
                Code = 'Q', ThreeLetter = "Gln", FullName = "Glutamine",
                Group = AminoAcidGroup.PolarUncharged,
                PrimaryRole = BindingRole.HydrogenBond,
                Hydrophobicity = -3.5, MolecularWeight = 146.15,
                Charge = 0, Polarity = 1, SideChainVolume = 109.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.55,
                CssGroup = "polar",
                Description = "Longer amide than Asn. Excellent CDR contact residue — can reach deep into antigen grooves. Both donor and acceptor. Common in CDR-H1 and CDR-H2.",
                ValidSubstitutions = new()
                {
                    new('N', SubstitutionType.Conservative,    +0.1, "Shorter chain — may lose deep contacts"),
                    new('E', SubstitutionType.Conservative,    -0.2, "Gains negative charge — electrostatic upgrade"),
                    new('K', SubstitutionType.SemiConservative,-0.1, "Polarity switch to positive"),
                    new('H', SubstitutionType.SemiConservative,-0.2, "Aromatic polar — can gain π contacts"),
                    new('R', SubstitutionType.Radical,         -0.3, "Strong positive charge — context dependent"),
                }
            },

            new AminoAcid
            {
                Code = 'C', ThreeLetter = "Cys", FullName = "Cysteine",
                Group = AminoAcidGroup.PolarUncharged,
                PrimaryRole = BindingRole.DisulfideBond,
                Hydrophobicity = 2.5, MolecularWeight = 121.16,
                Charge = 0, Polarity = 1, SideChainVolume = 86.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = false,
                BaseAffinityScore = 0.20,
                CssGroup = "special",
                Description = "Forms disulfide bonds — critical for VH/VL framework stability. Rarely mutated in SHM due to structural consequences. Mutation can destabilize entire domain fold.",
                ValidSubstitutions = new()
                {
                    new('S', SubstitutionType.SemiConservative,+1.5, "Loses sulfur — destroys disulfide if paired"),
                    new('A', SubstitutionType.Radical,         +2.0, "Completely removes side chain — usually catastrophic"),
                    new('V', SubstitutionType.Radical,         +1.8, "Hydrophobic replacement — loses thiol"),
                }
            },

            // ================================================
            //  POSITIVELY CHARGED
            // ================================================

            new AminoAcid
            {
                Code = 'R', ThreeLetter = "Arg", FullName = "Arginine",
                Group = AminoAcidGroup.PositiveCharged,
                PrimaryRole = BindingRole.ElectrostaticContact,
                Hydrophobicity = -4.5, MolecularWeight = 174.20,
                Charge = +1, Polarity = 2, SideChainVolume = 148.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.70,
                CssGroup = "positive",
                Description = "Strongly positive guanidinium group. Forms salt bridges and multiple H-bonds simultaneously. Highly favorable for targeting negatively charged epitope patches. Common in CDR-H3.",
                ValidSubstitutions = new()
                {
                    new('K', SubstitutionType.Conservative,    +0.2, "Shorter positive chain — loses some H-bond geometry"),
                    new('H', SubstitutionType.Conservative,    +0.5, "Weaker, pH-sensitive positive charge"),
                    new('Q', SubstitutionType.SemiConservative,+0.8, "Loses charge — significant reduction in electrostatics"),
                    new('N', SubstitutionType.SemiConservative,+0.6, "Loses positive charge, retains some polarity"),
                    new('A', SubstitutionType.Radical,         +2.0, "Destroys positive contact — major loss"),
                }
            },

            new AminoAcid
            {
                Code = 'K', ThreeLetter = "Lys", FullName = "Lysine",
                Group = AminoAcidGroup.PositiveCharged,
                PrimaryRole = BindingRole.ElectrostaticContact,
                Hydrophobicity = -3.9, MolecularWeight = 146.19,
                Charge = +1, Polarity = 2, SideChainVolume = 135.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.60,
                CssGroup = "positive",
                Description = "Long flexible positive chain. H-bond donor at ε-amino. Very common at antibody-antigen interfaces. Pairs well with Asp/Glu on antigen surface.",
                ValidSubstitutions = new()
                {
                    new('R', SubstitutionType.Conservative,    -0.2, "Stronger positive — can improve salt bridges"),
                    new('H', SubstitutionType.Conservative,    +0.4, "Weaker charge, aromatic addition"),
                    new('Q', SubstitutionType.SemiConservative,+0.7, "Loses charge, retains length and polarity"),
                    new('E', SubstitutionType.Radical,         +1.5, "Charge flip — usually strongly destabilizing"),
                    new('M', SubstitutionType.SemiConservative,+0.5, "Loses charge, gains sulfur flexibility"),
                }
            },

            new AminoAcid
            {
                Code = 'H', ThreeLetter = "His", FullName = "Histidine",
                Group = AminoAcidGroup.PositiveCharged,
                PrimaryRole = BindingRole.ElectrostaticContact,
                Hydrophobicity = -3.2, MolecularWeight = 155.16,
                Charge = 0, Polarity = 2, SideChainVolume = 118.0,  // neutral at pH 7.4
                IsAromatic = true, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.65,
                CssGroup = "positive",
                Description = "pH-sensitive aromatic. Unique dual identity: aromatic π-stacking at all pH + positive charge at pH < 6. Used in pH-dependent binding antibodies. Imidazole is both donor and acceptor.",
                ValidSubstitutions = new()
                {
                    new('N', SubstitutionType.Conservative,    +0.2, "Loses aromaticity, retains polar character"),
                    new('Q', SubstitutionType.Conservative,    +0.2, "Longer non-aromatic polar"),
                    new('Y', SubstitutionType.Conservative,    -0.3, "Larger aromatic + H-bond — often upgrade"),
                    new('R', SubstitutionType.SemiConservative,-0.2, "Stronger unconditional positive charge"),
                    new('F', SubstitutionType.SemiConservative,+0.4, "Keeps aromatic, loses charge capability"),
                }
            },

            // ================================================
            //  NEGATIVELY CHARGED
            // ================================================

            new AminoAcid
            {
                Code = 'D', ThreeLetter = "Asp", FullName = "Aspartate",
                Group = AminoAcidGroup.NegativeCharged,
                PrimaryRole = BindingRole.ElectrostaticContact,
                Hydrophobicity = -3.5, MolecularWeight = 133.10,
                Charge = -1, Polarity = 2, SideChainVolume = 91.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.60,
                CssGroup = "negative",
                Description = "Short negative carboxylate. Salt bridges with Arg/Lys on antigen. Both H-bond donor and acceptor. Very common in CDR-H2 contact positions targeting basic epitope patches.",
                ValidSubstitutions = new()
                {
                    new('E', SubstitutionType.Conservative,    -0.1, "Longer chain — may reach further into epitope"),
                    new('N', SubstitutionType.Conservative,    +0.5, "Loses charge — amide retains some polarity"),
                    new('S', SubstitutionType.SemiConservative,+0.8, "Loses charge and bulk"),
                    new('A', SubstitutionType.Radical,         +1.5, "Complete polar loss"),
                    new('H', SubstitutionType.Radical,         -0.4, "Charge flip — can improve targeting basic epitopes"),
                }
            },

            new AminoAcid
            {
                Code = 'E', ThreeLetter = "Glu", FullName = "Glutamate",
                Group = AminoAcidGroup.NegativeCharged,
                PrimaryRole = BindingRole.ElectrostaticContact,
                Hydrophobicity = -3.5, MolecularWeight = 147.13,
                Charge = -1, Polarity = 2, SideChainVolume = 109.0,
                IsAromatic = false, IsSHMHotspot = false, IsCommonContactResidue = true,
                BaseAffinityScore = 0.60,
                CssGroup = "negative",
                Description = "Longer negative carboxylate than Asp. More reach into antigen pockets. Frequently pairs with Arg in salt bridges. Can coordinate metal ions in some epitopes.",
                ValidSubstitutions = new()
                {
                    new('D', SubstitutionType.Conservative,    +0.1, "Shorter — may lose deep contact"),
                    new('Q', SubstitutionType.Conservative,    +0.6, "Loses charge, retains polarity"),
                    new('K', SubstitutionType.Radical,         +1.5, "Charge flip — context-dependent"),
                    new('A', SubstitutionType.Radical,         +1.5, "Complete polar loss"),
                    new('N', SubstitutionType.SemiConservative,+0.5, "Shorter, loses charge"),
                }
            },
        };
    }
}