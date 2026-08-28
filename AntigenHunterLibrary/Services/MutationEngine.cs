// ============================================================
//  MutationEngine.cs
//  AntigenHunter — Somatic Hypermutation Engine
//
//  Generates biologically-grounded amino acid mutations:
//  ┌─────────────────────────────────────────────────────┐
//  │  • SHM hotspot bias (S,T positions mutate first)    │
//  │  • CDR-biased position selection (85% CDR)          │
//  │  • Conservative / semi / radical substitution tiers │
//  │  • Boldness-scaled radical mutation probability     │
//  │  • Simulated annealing acceptance function          │
//  │  • Population-based candidate generation            │
//  └─────────────────────────────────────────────────────┘
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using AntigenHunterLibrary.Models;

namespace AntigenHunterLibrary.Services
{
    public class MutationEngine
    {
        private readonly Random _rng;
        private readonly AffinityScorer _scorer;

        public MutationEngine(AffinityScorer scorer, int? seed = null)
        {
            _scorer = scorer;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ── Public API ────────────────────────────────────────

        /// <summary>
        /// Generates a population of mutation candidates for one generation.
        /// Each candidate is a single amino acid substitution evaluated
        /// against the target, returned with its score.
        /// </summary>
        public List<MutationCandidate> GenerateCandidates(
            AntibodySequence current,
            TargetProtein target,
            SimulationConfig config,
            double currentScore,
            int generation)
        {
            var candidates = new List<MutationCandidate>();
            double temperature = GetTemperature(config, generation);

            for (int i = 0; i < config.VariantsPerRound; i++)
            {
                // Pick a position to mutate
                int position = SelectMutationPosition(current, config);
                if (position < 0) continue;

                // Pick a substitution
                char newCode = SelectSubstitution(
                    current, position, config, out var subType, out var ddg);

                if (newCode == current.Sequence[position]) continue;

                // Apply and score
                bool isCritical = current.Residues[position].IsCriticalContact;
                var mutated = current.ApplyMutation(
                    position, newCode, generation,
                    currentScore, 0); // temp affinity

                var scored = _scorer.Score(mutated, target);
                double newScore = scored.InternalScore;

                // Acceptance probability (SA)
                double delta = newScore - currentScore;
                double prob = AcceptanceProbability(delta, temperature);
                bool accepted = _rng.NextDouble() < prob;
                bool stochastic = accepted && delta <= 0;

                string region = current.Residues[position].CdrRegion?.Name
                    ?? "Framework";

                candidates.Add(new MutationCandidate
                {
                    Position = position,
                    FromCode = current.Sequence[position],
                    ToCode = newCode,
                    AffinityBefore = currentScore,
                    AffinityAfter = newScore,
                    DeltaDeltaG = ddg,
                    RegionName = region,
                    WasAccepted = accepted,
                    WasStochasticAccept = stochastic,
                    AcceptanceProbability = prob,
                    SubstitutionType = subType,
                    TouchedCriticalResidue = isCritical
                });
            }

            return candidates;
        }

        /// <summary>
        /// Selects the best candidate to accept for this generation.
        /// Prefers highest-scoring accepted candidate.
        /// Falls back to first accepted if no improving candidates.
        /// </summary>
        public MutationCandidate? SelectAccepted(
            List<MutationCandidate> candidates)
        {
            var accepted = candidates.Where(c => c.WasAccepted).ToList();
            if (!accepted.Any()) return null;

            // Prefer improving mutations first, then stochastic
            return accepted
                .OrderByDescending(c => c.ScoreDelta)
                .First();
        }

        // ── Position Selection ────────────────────────────────

        private int SelectMutationPosition(
            AntibodySequence sequence,
            SimulationConfig config)
        {
            bool targetCdr = _rng.NextDouble() < config.CdrMutationBias;
            var residues = sequence.Residues;

            List<int> pool;

            if (targetCdr)
            {
                // Prefer SHM hotspot positions (S, T) within CDRs
                var hotspots = residues
                    .Where(r => r.IsCdr && r.AminoAcid?.IsSHMHotspot == true)
                    .Select(r => r.Index)
                    .ToList();

                // Also include all CDR positions
                var cdrPositions = residues
                    .Where(r => r.IsCdr)
                    .Select(r => r.Index)
                    .ToList();

                // 40% chance to pick a hotspot if available
                if (hotspots.Any() && _rng.NextDouble() < 0.40)
                    pool = hotspots;
                else
                    pool = cdrPositions;
            }
            else
            {
                // Framework mutation
                pool = residues
                    .Where(r => !r.IsCdr && r.Code != 'C') // never mutate framework Cys
                    .Select(r => r.Index)
                    .ToList();
            }

            if (!pool.Any()) return -1;
            return pool[_rng.Next(pool.Count)];
        }

        // ── Substitution Selection ────────────────────────────

        private char SelectSubstitution(
            AntibodySequence sequence,
            int position,
            SimulationConfig config,
            out SubstitutionType subType,
            out double ddg)
        {
            char current = sequence.Sequence[position];
            var aa = AminoAcidDatabase.Get(current);
            subType = SubstitutionType.Conservative;
            ddg = 0.0;

            if (aa == null || !aa.ValidSubstitutions.Any())
            {
                // Fallback: random valid AA
                var all = AminoAcidDatabase.All
                    .Where(a => a.Code != current && a.Code != 'C')
                    .ToList();
                if (!all.Any()) return current;
                var picked = all[_rng.Next(all.Count)];
                subType = SubstitutionType.Radical;
                ddg = 1.0;
                return picked.Code;
            }

            // Filter substitutions by boldness
            var available = aa.ValidSubstitutions.ToList();

            double radicalThreshold = config.RadicalMutationProbability;
            double semiThreshold = 0.3 + config.MutationBoldness * 0.3;

            // Build weighted pool
            var pool = new List<(AminoAcidSubstitution Sub, double Weight)>();
            foreach (var sub in available)
            {
                double w = sub.Type switch
                {
                    SubstitutionType.Conservative => 1.0,
                    SubstitutionType.SemiConservative => semiThreshold,
                    SubstitutionType.Radical => radicalThreshold,
                    _ => 0.5
                };
                if (w > 0) pool.Add((sub, w));
            }

            if (!pool.Any()) return current;

            // Weighted random selection
            double totalWeight = pool.Sum(p => p.Weight);
            double roll = _rng.NextDouble() * totalWeight;
            double cumulative = 0;

            foreach (var (sub, weight) in pool)
            {
                cumulative += weight;
                if (roll <= cumulative)
                {
                    subType = sub.Type;
                    ddg = sub.DeltaDeltaG;
                    return sub.TargetCode;
                }
            }

            // Fallback
            var last = pool.Last();
            subType = last.Sub.Type;
            ddg = last.Sub.DeltaDeltaG;
            return last.Sub.TargetCode;
        }

        // ── Simulated Annealing ───────────────────────────────

        /// <summary>
        /// SA acceptance probability function.
        /// delta > 0: always accept (improvement).
        /// delta ≤ 0: accept with probability exp(delta / T).
        /// </summary>
        private static double AcceptanceProbability(
            double delta, double temperature)
        {
            if (delta > 0) return 1.0;
            if (temperature <= 0) return 0.0;
            return Math.Exp(delta / temperature);
        }

        /// <summary>
        /// Returns SA temperature for a given generation.
        /// Exponential cooling schedule.
        /// </summary>
        private static double GetTemperature(
            SimulationConfig config, int generation)
        {
            double t = config.StartTemperature *
                       Math.Pow(config.CoolingRate, generation - 1);
            return Math.Max(t, 0.001);
        }
    }
}