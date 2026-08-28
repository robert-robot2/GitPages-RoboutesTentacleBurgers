// ============================================================
//  SimulationService.cs
//  AntigenHunter — Simulation Orchestrator
//  ⚔️ Forged by Claude AI
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AntigenHunterLibrary.Models;

namespace AntigenHunterLibrary.Services
{
    public class SimulationService
    {
        private readonly AffinityScorer _scorer;
        private readonly MutationEngine _engine;
        private readonly SequenceParser _parser;

        public SimulationService()
        {
            _scorer = new AffinityScorer();
            _engine = new MutationEngine(_scorer);
            _parser = new SequenceParser();
        }

        /// <summary>
        /// Runs the full affinity maturation simulation asynchronously.
        /// Reports progress via the onProgress callback each generation.
        /// </summary>
        public async Task<SimulationRunResult> RunAsync(
            AntibodySequence startSequence,
            TargetProtein target,
            SimulationConfig config,
            Action<SimulationProgress>? onProgress = null,
            CancellationToken cancellationToken = default)
        {
            var result = new SimulationRunResult
            {
                TargetType = target.TargetType,
                TargetName = target.Name,
                TotalGenerations = config.RoundsOfEvolution,
                PopulationSize = config.VariantsPerRound,
                MutationBoldness = config.MutationBoldness,
                OriginalSequence = startSequence,
                StartedAt = DateTime.UtcNow
            };

            try
            {
                // ── Initial Score ─────────────────────────────
                var initialScore = _scorer.Score(startSequence, target);
                result.StartingAffinityNm = initialScore.AffinityNm;
                result.BestAffinityNm = initialScore.AffinityNm;
                result.BestSequence = startSequence;
                result.FinalSequence = startSequence;

                var current = startSequence;
                double curScore = initialScore.InternalScore;
                double bestScore = curScore;
                int bestGen = 0;

                // ── Generation Loop ───────────────────────────
                for (int gen = 1; gen <= config.RoundsOfEvolution; gen++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    double temperature = config.StartTemperature *
                        Math.Pow(config.CoolingRate, gen - 1);

                    // Generate candidates
                    var candidates = _engine.GenerateCandidates(
                        current, target, config, curScore, gen);

                    // Select best accepted
                    var accepted = _engine.SelectAccepted(candidates);

                    AntibodySequence nextSequence;
                    double nextScore;
                    double nextNm;

                    if (accepted != null)
                    {
                        nextSequence = current.ApplyMutation(
                            accepted.Position,
                            accepted.ToCode,
                            gen,
                            curScore,
                            accepted.AffinityAfter);

                        nextScore = accepted.AffinityAfter;
                        var scored = _scorer.Score(nextSequence, target);
                        nextScore = scored.InternalScore;
                        nextNm = scored.AffinityNm;
                    }
                    else
                    {
                        nextSequence = current;
                        nextScore = curScore;
                        var scored = _scorer.Score(current, target);
                        nextNm = scored.AffinityNm;
                    }

                    // Track best
                    if (nextScore > bestScore)
                    {
                        bestScore = nextScore;
                        result.BestSequence = nextSequence;
                        result.BestAffinityNm = nextNm;
                        result.BestAffinityGeneration = gen;
                        bestGen = gen;
                    }

                    // Build generation result
                    var genResult = new GenerationResult
                    {
                        GenerationNumber = gen,
                        Temperature = temperature,
                        SequenceIn = current,
                        SequenceOut = nextSequence,
                        Candidates = candidates,
                        AcceptedMutation = accepted,
                        ScoreIn = curScore,
                        ScoreOut = nextScore,
                        AffinityNmIn = AffinityScorer.ScoreToNm(
                            curScore, target.ScoringProfile),
                        AffinityNmOut = nextNm,
                        BestCandidateScore = GetBestCandidateScore(candidates),
                        AverageCandidateScore = GetAvgCandidateScore(candidates)
                    };

                    result.Generations.Add(genResult);

                    // Progress callback
                    onProgress?.Invoke(new SimulationProgress
                    {
                        CurrentGeneration = gen,
                        TotalGenerations = config.RoundsOfEvolution,
                        CurrentBestNm = result.BestAffinityNm,
                        StartingNm = result.StartingAffinityNm,
                        LastMutation = accepted?.Notation ?? string.Empty,
                        Temperature = temperature
                    });

                    current = nextSequence;
                    curScore = nextScore;

                    // Yield to UI thread every 5 generations
                    if (gen % 5 == 0)
                        await Task.Delay(1, cancellationToken);
                }

                // ── Finalize ──────────────────────────────────
                result.FinalSequence = current;
                result.FinalAffinityNm = AffinityScorer.ScoreToNm(
                    curScore, target.ScoringProfile);
                result.CompletedAt = DateTime.UtcNow;
                result.IsComplete = true;
                result.BuildChartPoints();
            }
            catch (OperationCanceledException)
            {
                result.ErrorMessage = "Simulation was cancelled.";
                result.CompletedAt = DateTime.UtcNow;
                result.BuildChartPoints();
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Simulation error: {ex.Message}";
                result.CompletedAt = DateTime.UtcNow;
            }

            return result;
        }

        // ── Helpers ───────────────────────────────────────────

        private static double GetBestCandidateScore(
            List<MutationCandidate> candidates)
        {
            if (!candidates.Any()) return 0;
            double best = double.MinValue;
            foreach (var c in candidates)
                if (c.AffinityAfter > best) best = c.AffinityAfter;
            return best;
        }

        private static double GetAvgCandidateScore(
            List<MutationCandidate> candidates)
        {
            if (!candidates.Any()) return 0;
            double sum = 0;
            foreach (var c in candidates) sum += c.AffinityAfter;
            return sum / candidates.Count;
        }
    }
}