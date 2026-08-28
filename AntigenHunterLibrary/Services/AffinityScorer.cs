// ============================================================
//  AffinityScorer.cs
//  AntigenHunter — Hybrid Affinity Scoring Engine
//
//  Scores antibody-antigen binding affinity using a
//  hybrid physicochemical + substitution matrix approach:
//  ┌─────────────────────────────────────────────────────┐
//  │  1. CDR residue property scoring vs epitope profile │
//  │  2. Aromatic contact bonus (π-stacking)             │
//  │  3. Electrostatic complementarity                   │
//  │  4. Critical residue penalty/reward                 │
//  │  5. CDR-H3 depth bonus                              │
//  │  6. BLOSUM62-inspired substitution bias             │
//  │  7. Target-specific weight multipliers              │
//  └─────────────────────────────────────────────────────┘
//
//  Output: internal score 0.0–1.0 + converted nM affinity
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using AntigenHunterLibrary.Models;

namespace AntigenHunterLibrary.Services
{
    public class AffinityScorer
    {
        private readonly Random _rng;

        public AffinityScorer(int? seed = null)
        {
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Scores an antibody sequence against a target protein.
        /// Returns internal score (0–1) and converted nM affinity.
        /// </summary>
        public ScoringResult Score(
            AntibodySequence antibody,
            TargetProtein target)
        {
            double score = 0.0;
            var breakdown = new List<ScoringComponent>();

            var profile = target.ScoringProfile;
            var residues = antibody.Residues;
            int len = residues.Count;

            if (len == 0)
                return ScoringResult.Zero(target);

            // ── Component 1: CDR Residue Property Scoring ────
            double cdrScore = ScoreCdrResidues(residues, target, breakdown);
            score += cdrScore * 0.40;

            // ── Component 2: Aromatic Contact Bonus ──────────
            double aromaticScore = ScoreAromaticContacts(residues, profile, breakdown);
            score += aromaticScore * 0.20;

            // ── Component 3: Electrostatic Complementarity ───
            double esScore = ScoreElectrostatics(residues, target, breakdown);
            score += esScore * 0.15;

            // ── Component 4: Critical Residue Reward/Penalty ─
            double critScore = ScoreCriticalResidues(residues, target, breakdown);
            score += critScore * 0.15;

            // ── Component 5: CDR-H3 Depth Bonus ──────────────
            double h3Score = ScoreCdrH3Depth(antibody, profile, breakdown);
            score += h3Score * 0.10;

            // Clamp to 0–1
            score = Math.Clamp(score, 0.0, 1.0);

            // Convert to nM
            double nM = ScoreToNm(score, profile);

            return new ScoringResult
            {
                InternalScore = score,
                AffinityNm = nM,
                Components = breakdown,
                TargetName = target.Name
            };
        }

        /// <summary>
        /// Fast score for a single mutation — computes delta only.
        /// Used during inner simulation loop for performance.
        /// </summary>
        public double FastScoreDelta(
            AntibodySequence before,
            AntibodySequence after,
            TargetProtein target,
            int mutatedPosition)
        {
            // Only rescore the affected position and its neighbors
            double scoreBefore = Score(before, target).InternalScore;
            double scoreAfter = Score(after, target).InternalScore;
            return scoreAfter - scoreBefore;
        }

        // ── Scoring Components ────────────────────────────────

        private double ScoreCdrResidues(
            IReadOnlyList<ResidueInfo> residues,
            TargetProtein target,
            List<ScoringComponent> breakdown)
        {
            double total = 0.0;
            int count = 0;
            var profile = target.ScoringProfile;

            foreach (var res in residues.Where(r => r.IsCdr))
            {
                if (res.AminoAcid == null) continue;

                double baseScore = res.AminoAcid.BaseAffinityScore;

                // Apply target-specific property weights
                double weight = res.AminoAcid.Group switch
                {
                    AminoAcidGroup.AromaticHydrophobic =>
                        baseScore * profile.AromaticWeight,
                    AminoAcidGroup.PositiveCharged =>
                        baseScore * profile.PositiveChargeWeight,
                    AminoAcidGroup.NegativeCharged =>
                        baseScore * profile.NegativeChargeWeight,
                    AminoAcidGroup.NonpolarAliphatic =>
                        baseScore * profile.HydrophobicWeight,
                    AminoAcidGroup.PolarUncharged =>
                        baseScore * profile.PolarWeight,
                    _ => baseScore
                };

                // CDR position scoring weight
                weight *= res.ScoringWeight;

                // Epitope complementarity bonus
                double epitopeBonus = ScoreEpitopeComplementarity(
                    res.AminoAcid, target);
                weight += epitopeBonus * 0.3;

                total += weight;
                count++;
            }

            double normalized = count > 0
                ? Math.Clamp(total / (count * 1.5), 0, 1)
                : 0;

            breakdown.Add(new ScoringComponent
            {
                Name = "CDR Residue Properties",
                Score = normalized,
                Note = $"{count} CDR residues evaluated"
            });

            return normalized;
        }

        private double ScoreAromaticContacts(
            IReadOnlyList<ResidueInfo> residues,
            TargetScoringProfile profile,
            List<ScoringComponent> breakdown)
        {
            var cdrResidues = residues.Where(r => r.IsCdr).ToList();
            if (!cdrResidues.Any()) return 0;

            int aromatic = cdrResidues.Count(r =>
                r.AminoAcid?.IsAromatic == true);

            // Reward aromatic density in CDRs — weighted by target profile
            double density = (double)aromatic / cdrResidues.Count;
            double score = Math.Clamp(
                density * profile.AromaticWeight * 1.2, 0, 1);

            // Extra reward for Tyr (aromatic + H-bond donor)
            int tyrCount = cdrResidues.Count(r => r.Code == 'Y');
            score += tyrCount * 0.04;
            score = Math.Clamp(score, 0, 1);

            breakdown.Add(new ScoringComponent
            {
                Name = "Aromatic Contacts",
                Score = score,
                Note = $"{aromatic} aromatic CDR residues ({tyrCount} Tyr)"
            });

            return score;
        }

        private double ScoreElectrostatics(
            IReadOnlyList<ResidueInfo> residues,
            TargetProtein target,
            List<ScoringComponent> breakdown)
        {
            // Sum charge of CDR residues
            var cdrResidues = residues.Where(r => r.IsCdr).ToList();
            if (!cdrResidues.Any()) return 0;

            int posCharge = cdrResidues.Count(r =>
                r.AminoAcid?.Charge == 1);
            int negCharge = cdrResidues.Count(r =>
                r.AminoAcid?.Charge == -1);

            // Score based on how well charges complement the target epitope
            double posScore = Math.Min(posCharge * 0.15, 0.6)
                * target.ScoringProfile.PositiveChargeWeight;
            double negScore = Math.Min(negCharge * 0.15, 0.6)
                * target.ScoringProfile.NegativeChargeWeight;

            // Check specific epitope residue complementarity for charge
            double epitopeChargeScore = 0;
            foreach (var epi in target.KeyEpitopeResidues.Where(e =>
                e.PreferredInteraction == BindingRole.ElectrostaticContact))
            {
                bool hasComplement = cdrResidues.Any(r =>
                    epi.ComplementaryAaCodes.Contains(r.Code));
                if (hasComplement)
                    epitopeChargeScore += epi.ImportanceWeight * 0.2;
            }

            double total = Math.Clamp(posScore + negScore + epitopeChargeScore, 0, 1);

            breakdown.Add(new ScoringComponent
            {
                Name = "Electrostatic Complementarity",
                Score = total,
                Note = $"+{posCharge} positive, -{negCharge} negative CDR charges"
            });

            return total;
        }

        private double ScoreCriticalResidues(
            IReadOnlyList<ResidueInfo> residues,
            TargetProtein target,
            List<ScoringComponent> breakdown)
        {
            var critical = residues.Where(r => r.IsCriticalContact).ToList();
            if (!critical.Any()) return 0.5; // neutral if no critical marked

            double total = 0;
            int scored = 0;

            foreach (var res in critical)
            {
                if (res.AminoAcid == null) continue;

                // Reward high-value AAs at critical positions
                double val = res.AminoAcid.BaseAffinityScore;

                // Bonus if this residue complements known epitope contacts
                foreach (var epi in target.KeyEpitopeResidues)
                {
                    if (epi.ComplementaryAaCodes.Contains(res.Code))
                        val += epi.ImportanceWeight * 0.25;
                }

                total += Math.Clamp(val, 0, 1);
                scored++;
            }

            double normalized = scored > 0
                ? Math.Clamp(total / scored, 0, 1)
                : 0.5;

            breakdown.Add(new ScoringComponent
            {
                Name = "Critical Contact Residues",
                Score = normalized,
                Note = $"{critical.Count} critical positions evaluated"
            });

            return normalized;
        }

        private double ScoreCdrH3Depth(
            AntibodySequence antibody,
            TargetScoringProfile profile,
            List<ScoringComponent> breakdown)
        {
            var h3 = antibody.CdrH3;
            if (h3 == null)
            {
                breakdown.Add(new ScoringComponent
                {
                    Name = "CDR-H3 Depth",
                    Score = 0,
                    Note = "CDR-H3 not detected"
                });
                return 0;
            }

            var h3Residues = antibody.Residues
                .Where(r => r.CdrRegion?.Name == "CDR-H3")
                .ToList();

            int aromatic = h3Residues.Count(r => r.AminoAcid?.IsAromatic == true);
            double bonus = aromatic * profile.CdrH3AromaticBonus;

            // Check for Trp specifically — deepest binder
            int trpCount = h3Residues.Count(r => r.Code == 'W');
            bonus += trpCount * 0.08;

            double score = Math.Clamp(bonus, 0, 1);

            breakdown.Add(new ScoringComponent
            {
                Name = "CDR-H3 Depth Bonus",
                Score = score,
                Note = $"{aromatic} aromatic residues in CDR-H3 ({trpCount} Trp)"
            });

            return score;
        }

        private double ScoreEpitopeComplementarity(
            AminoAcid residue,
            TargetProtein target)
        {
            double bonus = 0;
            foreach (var epi in target.KeyEpitopeResidues)
            {
                if (epi.ComplementaryAaCodes.Contains(residue.Code))
                    bonus += epi.ImportanceWeight * 0.15;
            }
            return Math.Clamp(bonus, 0, 0.5);
        }

        // ── Score ↔ nM Conversion ─────────────────────────────

        /// <summary>
        /// Converts internal score (0–1) to nM affinity value.
        /// Uses logarithmic mapping:
        ///   score=0.0 → BaselineAffinityNm (e.g. 500 nM)
        ///   score=1.0 → TheoreticalBestNm  (e.g. 0.05 nM)
        /// </summary>
        public static double ScoreToNm(double score, TargetScoringProfile profile)
        {
            double logBase = Math.Log(profile.BaselineAffinityNm);
            double logBest = Math.Log(profile.TheoreticalBestNm);
            double logAffinity = logBase + (logBest - logBase) * score;
            return Math.Exp(logAffinity);
        }

        /// <summary>
        /// Converts nM value back to internal score.
        /// </summary>
        public static double NmToScore(double nM, TargetScoringProfile profile)
        {
            if (nM <= 0) return 1.0;
            double logBase = Math.Log(profile.BaselineAffinityNm);
            double logBest = Math.Log(profile.TheoreticalBestNm);
            double logNm = Math.Log(Math.Clamp(nM,
                profile.TheoreticalBestNm, profile.BaselineAffinityNm));
            return Math.Clamp((logNm - logBase) / (logBest - logBase), 0, 1);
        }
    }

    // ── Result Classes ────────────────────────────────────────

    public class ScoringResult
    {
        public double InternalScore { get; init; }
        public double AffinityNm { get; init; }
        public string TargetName { get; init; } = string.Empty;
        public List<ScoringComponent> Components { get; init; } = new();

        public string AffinityDisplay =>
            GenerationResult.FormatAffinityNm(AffinityNm);

        public static ScoringResult Zero(TargetProtein target) => new()
        {
            InternalScore = 0,
            AffinityNm = target.ScoringProfile.BaselineAffinityNm,
            TargetName = target.Name
        };
    }

    public class ScoringComponent
    {
        public string Name { get; init; } = string.Empty;
        public double Score { get; init; }
        public string Note { get; init; } = string.Empty;
        public string ScoreDisplay => $"{Score:P0}";
    }
}