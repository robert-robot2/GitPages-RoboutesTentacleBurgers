// ==========================================================
// NovaBrain.cs — Nova Adeptus Intelligence Layer
// Sensory processing, Naive Bayes classification,
// emotional state machine, working memory, reasoning engine.
// Called by NovaCortex.cs — never by razor directly.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NovaAdeptusLibrary
{
    // ==========================================================
    // SENSORY SIGNAL
    // Structured output of raw input processing
    // ==========================================================
    public class SensorySignal
    {
        public string Raw { get; set; } = "";
        public string Cleaned { get; set; } = "";
        public string InputType { get; set; } = "statement";
        public string Complexity { get; set; } = "simple";
        public int WordCount { get; set; } = 0;
        public bool HasGreeting { get; set; } = false;
        public bool HasNameIntro { get; set; } = false;
        public bool HasQuestion { get; set; } = false;
        public bool HasCommand { get; set; } = false;
        public bool HasEmotional { get; set; } = false;
        public bool IsNonsense { get; set; } = false;
        public List<string> Tokens { get; set; } = new();
    }

    // ==========================================================
    // CONTEXT WINDOW
    // Working memory of recent conversation
    // ==========================================================
    public class ContextWindow
    {
        public int SessionTurn { get; set; } = 0;
        public int RepeatCount { get; set; } = 0;
        public string LastInput { get; set; } = "";
        public string PlayerPattern { get; set; } = "exploring";
        public bool NeedsNudge { get; set; } = false;
        public int QuestionsInARow { get; set; } = 0;
        public int CommandsInARow { get; set; } = 0;
        public int NonsenseInARow { get; set; } = 0;
        public int EmotionalInARow { get; set; } = 0;
        public string DominantTopic { get; set; } = "none";
        public List<string> Last3Nova { get; set; } = new();
        public List<string> TopicHistory { get; set; } = new();
    }

    // ==========================================================
    // EMOTIONAL STATE
    // ==========================================================
    public enum NovaEmotion
    {
        Calm, Amused, Irritated, Intrigued, Impressed
    }

    public class EmotionalStateObject
    {
        public NovaEmotion Current { get; set; } = NovaEmotion.Calm;
        public NovaEmotion Previous { get; set; } = NovaEmotion.Calm;
        public double Intensity { get; set; } = 0.5;
        public int TurnsInState { get; set; } = 0;
        public int ConsecutiveIrritations { get; set; } = 0;
        public int ConsecutivePositives { get; set; } = 0;
        public int TotalShifts { get; set; } = 0;
        public string ShiftReason { get; set; } = "initialized";

        public string Color => Current switch
        {
            NovaEmotion.Calm => "#4A90D9",
            NovaEmotion.Amused => "#48C774",
            NovaEmotion.Irritated => "#E53935",
            NovaEmotion.Intrigued => "#9B59B6",
            NovaEmotion.Impressed => "#F4C542",
            _ => "#4A90D9",
        };

        public string Emoji => Current switch
        {
            NovaEmotion.Calm => "🔵",
            NovaEmotion.Amused => "🟢",
            NovaEmotion.Irritated => "🔴",
            NovaEmotion.Intrigued => "🟣",
            NovaEmotion.Impressed => "🟡",
            _ => "🔵",
        };

        public string Name => Current.ToString().ToLower();
    }

    // ==========================================================
    // REASONING RESULT
    // What type of response Nova should give
    // ==========================================================
    public class ReasoningResult
    {
        public string ResponseType { get; set; } = "deflect";
        public double Confidence { get; set; } = 0.0;
        public bool AllowQuestionBack { get; set; } = false;
        public string Urgency { get; set; } = "normal";
        public string? ContentHint { get; set; } = null;
        public Dictionary<string, double> Scores { get; set; } = new();
    }

    // ==========================================================
    // NAIVE BAYES CLASSIFIER
    // Ported directly from NovaMLIntelligence.py
    // ==========================================================
    public class NaiveBayesClassifier
    {
        private readonly Dictionary<string, Dictionary<string, int>> _classWordCounts = new();
        private readonly Dictionary<string, int> _classTotals = new();
        private readonly Dictionary<string, int> _classCounts = new();
        private readonly HashSet<string> _vocab = new();
        private int _totalDocs = 0;
        public bool Trained { get; private set; } = false;

        private static List<string> Tokenize(string text)
        {
            text = Regex.Replace(text.ToLower().Trim(), @"[^a-z0-9\s]", "");
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Where(w => w.Length > 1)
                       .ToList();
        }

        public void Train(Dictionary<string, List<string>> trainingData)
        {
            foreach (var (label, examples) in trainingData)
            {
                if (!_classWordCounts.ContainsKey(label))
                    _classWordCounts[label] = new();

                foreach (var example in examples)
                {
                    var tokens = Tokenize(example);
                    _classCounts[label] = _classCounts.GetValueOrDefault(label) + 1;
                    _totalDocs++;
                    foreach (var token in tokens)
                    {
                        _classWordCounts[label][token] =
                            _classWordCounts[label].GetValueOrDefault(token) + 1;
                        _classTotals[label] =
                            _classTotals.GetValueOrDefault(label) + 1;
                        _vocab.Add(token);
                    }
                }
            }
            Trained = true;
        }

        public (string Label, double Confidence) Classify(string text)
        {
            if (!Trained) return ("unknown", 0.0);
            var tokens = Tokenize(text);
            if (!tokens.Any()) return ("unknown", 0.0);

            var scores = new Dictionary<string, double>();
            int vocabSize = _vocab.Count;

            foreach (var label in _classCounts.Keys)
            {
                double score = Math.Log((double)_classCounts[label] / _totalDocs);
                int total = _classTotals.GetValueOrDefault(label);
                foreach (var token in tokens)
                {
                    int count = _classWordCounts[label]
                        .GetValueOrDefault(token);
                    score += Math.Log((count + 1.0) /
                                      (total + vocabSize + 1));
                }
                scores[label] = score;
            }

            var bestLabel = scores.OrderByDescending(kv => kv.Value)
                                  .First().Key;
            double maxScore = scores.Values.Max();
            var expScores = scores.ToDictionary(
                kv => kv.Key,
                kv => Math.Exp(kv.Value - maxScore));
            double totalExp = expScores.Values.Sum();
            double confidence = expScores[bestLabel] / totalExp;

            return (bestLabel, Math.Round(confidence, 4));
        }
    }

    // ==========================================================
    // NOVA BRAIN — MAIN INTELLIGENCE ENGINE
    // ==========================================================
    public class NovaBrain
    {
        // ── Systems ────────────────────────────────────────────
        private readonly NaiveBayesClassifier _classifier;
        private readonly EmotionalStateObject _emotion;
        private readonly ContextWindow _context;

        // ── Config ─────────────────────────────────────────────
        private const double ConfidenceThreshold = 0.15;
        private const double FallbackThreshold = 0.08;

        // ── Social response bank ───────────────────────────────
        // Pulled from NovaIntelligenceController.SocialResponses
        private static readonly Dictionary<string, List<string>> SocialBank =
            NovaPrefrontalCortex.SocialResponses;

        private static readonly Random _rng = new();
        public string? ExtractName(string input) => TryExtractName(input);
        // ==========================================================
        // CONSTRUCTOR — trains classifier on init
        // ==========================================================
        public NovaBrain()
        {
            _classifier = new NaiveBayesClassifier();
            _emotion = new EmotionalStateObject();
            _context = new ContextWindow();

            _classifier.Train(NovaCerebellum.Examples);
        }

        // ==========================================================
        // MAIN PROCESS — called by NovaCortex
        // Returns a response string or null to fall through
        // ==========================================================
        public string? Process(string input, NovaSession session)
        {
            var signal = Sense(input);
            UpdateWorkingMemory(signal);
            UpdateEmotionalState(signal, session);

            // Auto-save name if detected
            if (signal.HasNameIntro && session.UserName == null)
            {
                var extracted = TryExtractName(input);
                if (extracted != null)
                    session.UserName = extracted;
            }

            // Classify intent
            if (!_classifier.Trained) return null;
            var (intent, confidence) = _classifier.Classify(input);

            // Below fallback threshold — brain has nothing
            if (confidence < FallbackThreshold) return null;

            // Social bank response
            var social = GetSocial(intent);
            if (social != null && confidence >= ConfidenceThreshold)
                return social;

            // Mid-confidence — still return social if available
            if (social != null)
                return social;

            return null;
        }

        // ==========================================================
        // SENSORY PROCESSING
        // ==========================================================
        private static readonly HashSet<string> Greetings = new()
        {
            "hi","hello","hey","sup","yo","greetings","howdy",
            "good morning","good evening","good day",
        };

        private static readonly HashSet<string> NameTriggers = new()
        {
            "my name is","call me","i am","i'm",
            "name's","they call me",
        };

        private static readonly HashSet<string> QuestionWords = new()
        {
            "who","what","where","when","why","how","which",
            "can you","could you","do you","are you","tell me",
        };

        private static readonly HashSet<string> EmotionalWords = new()
        {
            "hate","love","angry","happy","sad","frustrated","excited",
            "bored","amazing","terrible","awesome","stupid","great",
            "scared","worried","confused","thanks","sorry","please",
        };

        private static readonly HashSet<string> KnownCommands = new()
        {
            "accept","complete","reset","stats","help","trivia","mini",
            "hack","fight","stealth","loot","boss","mission","mood",
            "skills","history","list","reward","bonus","advance",
            "story","companion","upgrade","market","endgame","cosmic",
            "event","ship","dismiss","date","time","advice","fact","joke",
            "spell", "analyze", "analyse", "define","playa", "bet", "sheesh", "periodt", "slay", "bussin", "goated",

        };

        public SensorySignal Sense(string raw)
        {
            var sig = new SensorySignal();
            sig.Raw = raw;
            sig.Cleaned = CleanInput(raw);
            sig.Tokens = Tokenize(sig.Cleaned);
            sig.WordCount = sig.Tokens.Count;

            sig.HasGreeting = Greetings.Any(g =>
                sig.Cleaned == g ||
                sig.Cleaned.StartsWith(g + " ") ||
                sig.Cleaned.Contains(" " + g + " "));

            sig.HasNameIntro = NameTriggers.Any(t =>
                sig.Cleaned.Contains(t));

            sig.HasQuestion = sig.Cleaned.EndsWith("?") ||
                QuestionWords.Any(q => sig.Cleaned.StartsWith(q) ||
                                       sig.Cleaned.Contains(" " + q + " "));

            sig.HasCommand = sig.Tokens.Any(t =>
                KnownCommands.Contains(t));

            sig.HasEmotional = sig.Tokens.Any(t =>
                EmotionalWords.Contains(t));

            sig.IsNonsense = DetectNonsense(sig.Tokens);

            sig.Complexity = sig.WordCount <= 3 ? "simple"
                             : sig.WordCount <= 10 ? "moderate"
                             : "complex";

            sig.InputType = ClassifyInputType(sig);

            return sig;
        }

        private static string CleanInput(string text) =>
            Regex.Replace(
                Regex.Replace(text.ToLower().Trim(),
                    @"[^\w\s\?\!\.\,\'\-]", ""),
                @"\s+", " ");

        private static List<string> Tokenize(string text) =>
            Regex.Replace(text, @"[^\w\s]", "")
                 .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                 .ToList();

        private static bool DetectNonsense(List<string> tokens)
        {
            if (!tokens.Any()) return true;
            if (tokens.Count == 1 && tokens[0].Length > 5)
            {
                int consonants = tokens[0].Count(c =>
                    "bcdfghjklmnpqrstvwxyz".Contains(c));
                if ((double)consonants / tokens[0].Length > 0.8)
                    return true;
            }
            return false;
        }

        private static string ClassifyInputType(SensorySignal sig)
        {
            if (sig.HasCommand && !sig.HasQuestion) return "command";
            if (sig.HasQuestion) return "question";
            if (sig.HasEmotional && !sig.HasCommand) return "emotional";
            if (sig.IsNonsense) return "nonsense";
            if (sig.HasGreeting && sig.WordCount <= 3) return "greeting";
            return "statement";
        }

        // ==========================================================
        // WORKING MEMORY UPDATE
        // ==========================================================
        private void UpdateWorkingMemory(SensorySignal signal)
        {
            _context.SessionTurn++;

            _context.RepeatCount = signal.Cleaned == _context.LastInput
                ? _context.RepeatCount + 1 : 0;
            _context.LastInput = signal.Cleaned;

            _context.QuestionsInARow = signal.InputType == "question"
                ? _context.QuestionsInARow + 1 : 0;
            _context.CommandsInARow = signal.InputType == "command"
                ? _context.CommandsInARow + 1 : 0;
            _context.NonsenseInARow = signal.InputType == "nonsense"
                ? _context.NonsenseInARow + 1 : 0;
            _context.EmotionalInARow = signal.InputType == "emotional"
                ? _context.EmotionalInARow + 1 : 0;

            var topic = DetectTopic(signal.Cleaned);
            if (topic != null)
            {
                if (!_context.TopicHistory.Any() ||
                    _context.TopicHistory.Last() != topic)
                    _context.TopicHistory.Add(topic);
                if (_context.TopicHistory.Count > 10)
                    _context.TopicHistory.RemoveAt(0);
                _context.DominantTopic = topic;
            }

            _context.PlayerPattern = ClassifyPattern();
            _context.NeedsNudge = DetectNudgeNeeded();
        }

        private static readonly Dictionary<string, string[]> TopicMap = new()
        {
            ["combat"] = new[] { "fight", "combat", "battle", "enemy", "kill", "weapon" },
            ["hacking"] = new[] { "hack", "cyber", "breach", "code", "crack", "system" },
            ["stealth"] = new[] { "stealth", "sneak", "ghost", "shadow", "silent" },
            ["missions"] = new[] { "mission", "task", "accept", "complete", "objective" },
            ["identity"] = new[] { "who are you", "what are you", "nova", "your name" },
            ["lore"] = new[] { "void", "galaxy", "space", "cosmos", "star", "planet" },
            ["social"] = new[] { "how are you", "feeling", "doing", "okay", "fine" },
            ["humor"] = new[] { "joke", "funny", "laugh", "humor", "lol" },
            ["trivia"] = new[] { "trivia", "quiz", "question", "challenge" },
            ["stats"] = new[] { "stats", "level", "xp", "skills", "progress" },
            ["language"] = new[] { "spell", "spelling", "analyze", "analyse", "define",
    "letters", "alphabet", "phonetic", "character", "word",},

            ["aave"] = new[] {
    "who be", "what be", "where you is", "how be", "jive turkey",
    "playa", "no cap", "periodt", "sheesh", "on god", "bussin",
    "goated", "slay", "its giving", "hit different", "you feel me",
    "real talk", "say word", "word up", "aight", "you trippin",
    "you wilding", "i see you", "built different", "you valid",
},

        };

        private static string? DetectTopic(string cleaned)
        {
            foreach (var (topic, keywords) in TopicMap)
                if (keywords.Any(kw => cleaned.Contains(kw)))
                    return topic;
            return null;
        }

        private string ClassifyPattern()
        {
            if (_context.NonsenseInARow >= 3) return "testing";
            if (_context.QuestionsInARow >= 3) return "exploring";
            if (_context.CommandsInARow >= 3) return "engaged";
            if (_context.EmotionalInARow >= 2) return "emotional";
            if (_context.RepeatCount >= 2) return "stuck";
            if (_context.SessionTurn <= 3) return "new";
            return "exploring";
        }

        private bool DetectNudgeNeeded() =>
            _context.PlayerPattern == "stuck" ||
            _context.NonsenseInARow >= 3 ||
            (_context.SessionTurn > 5 && _context.CommandsInARow == 0);

        // ==========================================================
        // EMOTIONAL STATE MACHINE
        // ==========================================================
        private void UpdateEmotionalState(SensorySignal signal,
                                          NovaSession session)
        {
            _emotion.TurnsInState++;

            if (TriggersIrritation(signal))
            {
                _emotion.ConsecutiveIrritations++;
                _emotion.ConsecutivePositives = 0;
                if (_emotion.ConsecutiveIrritations >= 2)
                    ShiftEmotion(NovaEmotion.Irritated,
                        "consecutive irritation",
                        Math.Min(0.4 + _emotion.ConsecutiveIrritations * 0.15, 1.0));
            }
            else if (TriggersPositive(signal))
            {
                _emotion.ConsecutivePositives++;
                _emotion.ConsecutiveIrritations = 0;
                var target = PickPositiveTarget(signal, session);
                ShiftEmotion(target, "positive interaction",
                    Math.Min(0.3 + _emotion.ConsecutivePositives * 0.1, 0.9));
            }
            else if (TriggersIntrigued(signal))
            {
                _emotion.ConsecutiveIrritations = 0;
                ShiftEmotion(NovaEmotion.Intrigued, "complex input", 0.6);
            }
            else
            {
                DecayEmotion();
            }
        }

        private static bool TriggersIrritation(SensorySignal signal)
        {
            var hostile = new[]
            {
                "hate","stupid","idiot","dumb","useless",
                "suck","broken","terrible","worst","shut up",
            };
            if (signal.HasEmotional &&
                hostile.Any(h => signal.Cleaned.Contains(h)))
                return true;
            return false;
        }

        private static bool TriggersPositive(SensorySignal signal)
        {
            var positive = new[]
            {
                "thank","love","amazing","great","awesome",
                "cool","nice","appreciate","wonderful","correct",
                "won","defeated","completed","success","solved",
            };
            return signal.HasEmotional &&
                   positive.Any(p => signal.Cleaned.Contains(p));
        }

        private static bool TriggersIntrigued(SensorySignal signal) =>
            signal.Complexity == "complex" && signal.HasQuestion;

        private static NovaEmotion PickPositiveTarget(
            SensorySignal signal, NovaSession session)
        {
            var gameWords = new[]
            {
                "correct","won","defeated","completed",
                "success","solved","hacked",
            };
            if (gameWords.Any(g => signal.Cleaned.Contains(g)))
                return NovaEmotion.Impressed;
            var humorWords = new[] { "lol", "haha", "funny", "joke" };
            if (humorWords.Any(h => signal.Cleaned.Contains(h)))
                return NovaEmotion.Amused;
            if (session.Relationship is "trusted" or "respected")
                return NovaEmotion.Impressed;
            return NovaEmotion.Amused;
        }

        private void DecayEmotion()
        {
            var decayMap = new Dictionary<NovaEmotion, int>
            {
                [NovaEmotion.Irritated] = 4,
                [NovaEmotion.Impressed] = 3,
                [NovaEmotion.Amused] = 3,
                [NovaEmotion.Intrigued] = 5,
            };
            if (decayMap.TryGetValue(_emotion.Current, out int threshold) &&
                _emotion.TurnsInState >= threshold)
                ShiftEmotion(NovaEmotion.Calm, "decay", 0.5);
            else
                _emotion.Intensity = Math.Max(0.3, _emotion.Intensity - 0.05);
        }

        private void ShiftEmotion(NovaEmotion next,
                                   string reason, double intensity)
        {
            if (_emotion.Current == next) return;
            _emotion.Previous = _emotion.Current;
            _emotion.Current = next;
            _emotion.TurnsInState = 0;
            _emotion.TotalShifts++;
            _emotion.ShiftReason = reason;
            _emotion.Intensity = intensity;
        }

        // Game event hooks — called by NovaCortex
        public void OnMissionComplete() =>
            ShiftEmotion(NovaEmotion.Impressed, "mission completed", 0.8);
        public void OnBossDefeated() =>
            ShiftEmotion(NovaEmotion.Impressed, "boss defeated", 0.9);
        public void OnTriviaCorrect() =>
            ShiftEmotion(NovaEmotion.Amused, "trivia correct", 0.7);
        public void OnHostileInput()
        {
            _emotion.ConsecutiveIrritations++;
            if (_emotion.ConsecutiveIrritations >= 2)
                ShiftEmotion(NovaEmotion.Irritated, "hostile input", 0.8);
        }

        // ==========================================================
        // REASONING ENGINE
        // Scores response types and returns best fit
        // ==========================================================
        public ReasoningResult Reason(SensorySignal signal,
                                      NovaSession session,
                                      List<string> intents)
        {
            var result = new ReasoningResult();
            var scores = new Dictionary<string, double>
            {
                ["humor"] = 0.0,
                ["cold"] = 0.0,
                ["warm"] = 0.0,
                ["engage"] = 0.0,
                ["deflect"] = 0.0,
                ["inform"] = 0.0,
                ["question"] = 0.0,
                ["taunt"] = 0.0,
                ["dismiss"] = 0.0,
            };

            // Layer 1 — emotional state
            scores = ApplyEmotionScores(scores);

            // Layer 2 — relationship
            scores = ApplyRelationshipScores(scores, session.Relationship);

            // Layer 3 — signal type
            scores = ApplySignalScores(scores, signal);

            // Layer 4 — context
            scores = ApplyContextScores(scores);

            // Layer 5 — first turn override
            if (_context.SessionTurn <= 1)
            {
                scores["cold"] = Math.Max(0, scores["cold"] - 0.5);
                scores["dismiss"] = Math.Max(0, scores["dismiss"] - 0.5);
                scores["warm"] += 0.3;
            }

            // Max irritation override
            if (_emotion.Current == NovaEmotion.Irritated &&
                _emotion.ConsecutiveIrritations >= 3)
            {
                foreach (var key in scores.Keys.ToList()) scores[key] = 0.0;
                scores["cold"] = 1.0;
                scores["dismiss"] = 0.8;
            }

            var best = scores.OrderByDescending(kv => kv.Value).First();
            result.ResponseType = best.Key;
            result.Confidence = best.Value;
            result.Scores = scores;
            result.AllowQuestionBack = ShouldAskBack(best.Key, session);
            result.Urgency = GetUrgency();
            result.ContentHint = GetContentHint(intents);

            return result;
        }

        private Dictionary<string, double> ApplyEmotionScores(
            Dictionary<string, double> s)
        {
            var e = _emotion.Current;
            var i = _emotion.Intensity;
            switch (e)
            {
                case NovaEmotion.Irritated:
                    s["cold"] += 0.5 + i * 0.3;
                    s["dismiss"] += 0.4 + i * 0.2;
                    s["taunt"] += 0.3;
                    s["warm"] -= 0.3;
                    s["humor"] -= 0.2;
                    break;
                case NovaEmotion.Amused:
                    s["humor"] += 0.5 + i * 0.2;
                    s["warm"] += 0.3;
                    s["taunt"] += 0.2;
                    s["cold"] -= 0.3;
                    break;
                case NovaEmotion.Intrigued:
                    s["engage"] += 0.5 + i * 0.2;
                    s["question"] += 0.4;
                    s["inform"] += 0.3;
                    s["dismiss"] -= 0.3;
                    break;
                case NovaEmotion.Impressed:
                    s["warm"] += 0.4 + i * 0.2;
                    s["humor"] += 0.3;
                    s["engage"] += 0.3;
                    s["cold"] -= 0.4;
                    break;
                default: // calm
                    s["deflect"] += 0.3;
                    s["inform"] += 0.2;
                    s["taunt"] += 0.1;
                    break;
            }
            return s;
        }

        private static Dictionary<string, double> ApplyRelationshipScores(
            Dictionary<string, double> s, string rel)
        {
            switch (rel)
            {
                case "rival":
                    s["cold"] += 0.3;
                    s["taunt"] += 0.2;
                    s["warm"] -= 0.4;
                    break;
                case "trusted":
                    s["warm"] += 0.3;
                    s["humor"] += 0.2;
                    s["cold"] -= 0.2;
                    break;
                case "respected":
                    s["warm"] += 0.4;
                    s["humor"] += 0.3;
                    s["engage"] += 0.2;
                    s["cold"] -= 0.3;
                    s["dismiss"] -= 0.3;
                    break;
            }
            return s;
        }

        private static Dictionary<string, double> ApplySignalScores(
            Dictionary<string, double> s, SensorySignal sig)
        {
            switch (sig.InputType)
            {
                case "question":
                    s["inform"] += 0.4;
                    s["engage"] += 0.3;
                    s["question"] += 0.2;
                    s["dismiss"] -= 0.2;
                    break;
                case "command":
                    s["inform"] += 0.5;
                    s["deflect"] -= 0.2;
                    break;
                case "emotional":
                    s["warm"] += 0.2;
                    s["taunt"] += 0.2;
                    break;
                case "nonsense":
                    s["dismiss"] += 0.5;
                    s["humor"] += 0.2;
                    s["taunt"] += 0.2;
                    s["inform"] -= 0.3;
                    break;
                case "greeting":
                    s["warm"] += 0.3;
                    s["cold"] -= 0.1;
                    break;
            }
            if (sig.Complexity == "complex")
            {
                s["engage"] += 0.2;
                s["question"] += 0.1;
            }
            return s;
        }

        private Dictionary<string, double> ApplyContextScores(
            Dictionary<string, double> s)
        {
            switch (_context.PlayerPattern)
            {
                case "stuck":
                    s["inform"] += 0.4;
                    s["warm"] += 0.2;
                    s["dismiss"] -= 0.3;
                    break;
                case "testing":
                    s["taunt"] += 0.3;
                    s["humor"] += 0.2;
                    s["dismiss"] += 0.2;
                    break;
                case "engaged":
                    s["warm"] += 0.2;
                    s["humor"] += 0.2;
                    s["engage"] += 0.2;
                    break;
            }
            if (_context.RepeatCount >= 2)
            {
                s["taunt"] += 0.3;
                s["dismiss"] += 0.2;
            }
            if (_context.NeedsNudge)
                s["inform"] += 0.3;
            return s;
        }

        private bool ShouldAskBack(string responseType, NovaSession session)
        {
            if (_emotion.Current == NovaEmotion.Irritated) return false;
            if (responseType is "engage" or "question")
                return session.Relationship is "trusted" or "respected" or "warming"
                    || _emotion.Current == NovaEmotion.Intrigued;
            return false;
        }

        private string GetUrgency()
        {
            if (_emotion.Current == NovaEmotion.Irritated) return "high";
            if (_context.NeedsNudge) return "high";
            if (_emotion.Current is NovaEmotion.Impressed
                               or NovaEmotion.Amused) return "low";
            return "normal";
        }

        private static string? GetContentHint(List<string> intents)
        {
            if (intents.Contains("trivia_request")) return "trivia";
            if (intents.Contains("joke_request")) return "joke";
            if (intents.Contains("fact_request")) return "fact";
            if (intents.Contains("mission_request")) return "mission";
            if (intents.Contains("stats_request")) return "stats";
            if (intents.Contains("help_request")) return "help";
            return null;
        }

        // ==========================================================
        // SOCIAL BANK LOOKUP
        // ==========================================================
        private static string? GetSocial(string intent)
        {
            if (SocialBank.TryGetValue(intent, out var responses))
                return responses[_rng.Next(responses.Count)];
            return null;
        }

        // ==========================================================
        // NAME EXTRACTION
        // ==========================================================
        private static string? TryExtractName(string input)
        {
            var cleaned = input.ToLower().Trim();
            var patterns = new[]
            {
                @"my name is ([a-zA-Z]{2,20})",
                @"name's ([a-zA-Z]{2,20})",
                @"call me ([a-zA-Z]{2,20})",
                @"i am ([a-zA-Z]{2,20})",
                @"i'm ([a-zA-Z]{2,20})",
            };
            var notNames = new HashSet<string>
            {
                "a","an","the","here","back","ready","not","just",
                "going","trying","sorry","good","bad","ok","okay",
                "playing","new","old","in","on","at","sure","still",
            };
            foreach (var pattern in patterns)
            {
                var match = Regex.Match(cleaned, pattern);
                if (match.Success)
                {
                    var candidate = match.Groups[1].Value.Trim();
                    if (!notNames.Contains(candidate.ToLower()) &&
                        candidate.Length > 1)
                        return char.ToUpper(candidate[0]) + candidate[1..];
                }
            }
            return null;
        }

        // ==========================================================
        // PUBLIC ACCESSORS — for NovaCortex UI sync
        // ==========================================================
        public string GetEmotionalColor() => _emotion.Color;
        public string GetEmotionalEmoji() => _emotion.Emoji;
        public string GetEmotionalState() => _emotion.Name;
        public EmotionalStateObject GetEmotionObject() => _emotion;
        public ContextWindow GetContextWindow() => _context;
    }
}