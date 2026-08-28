// ==========================================================
// NovaAPIService.cs — Nova Adeptus API Service
// Pure C# HttpClient calls for all four content APIs.
// No JS fetch needed. Called by NovaCortex on init
// and refilled automatically when queues run low.
// ==========================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;

namespace NovaAdeptusLibrary
{
    public class NovaAPIService
    {
        private readonly HttpClient _http;

        // ── Content queues ─────────────────────────────────────
        public Queue<string> Jokes { get; } = new();
        public Queue<string> Facts { get; } = new();
        public Queue<string> Advice { get; } = new();
        public Queue<TriviaQuestion> Trivia { get; } = new();

        // ── Status flags ───────────────────────────────────────
        public bool TriviaOnline { get; private set; } = true;
        public bool JokesOnline { get; private set; } = true;
        public bool FactsOnline { get; private set; } = true;
        public bool AdviceOnline { get; private set; } = true;

        private static readonly Random _rng = new();

        // ── Trivia categories: Science, Math, General, Science:Computers
        private static readonly int[] TriviaCategories = { 17, 19, 9, 18 };

        public NovaAPIService(HttpClient http)
        {
            _http = http;
        }

        // ==========================================================
        // LOAD ALL — called once on init
        // ==========================================================
        public async Task LoadAllAsync()
        {
            await Task.WhenAll(
                FetchTriviaAsync(),
                FetchJokeAsync(),
                FetchFactAsync(),
                FetchAdviceAsync()
            );
        }

        // ==========================================================
        // REFILL CHECK — called after each message
        // Silently refills any queue that's running low
        // ==========================================================
        public async Task RefillIfNeededAsync()
        {
            var tasks = new List<Task>();

            if (Trivia.Count < 3) tasks.Add(FetchTriviaAsync());
            if (Jokes.Count < 2) tasks.Add(FetchJokeAsync());
            if (Facts.Count < 2) tasks.Add(FetchFactAsync());
            if (Advice.Count < 2) tasks.Add(FetchAdviceAsync());

            if (tasks.Any())
                await Task.WhenAll(tasks);
        }

        // ==========================================================
        // TRIVIA — opentdb.com
        // ==========================================================
        public async Task FetchTriviaAsync(int amount = 10)
        {
            try
            {
                int cat = TriviaCategories[_rng.Next(TriviaCategories.Length)];
                var url = $"https://opentdb.com/api.php"
                        + $"?amount={amount}"
                        + $"&category={cat}"
                        + $"&type=multiple"
                        + $"&encode=url3986";

                var response = await _http.GetFromJsonAsync<OpenTDBResponse>(url);

                if (response?.ResponseCode == 0 && response.Results != null)
                {
                    foreach (var q in response.Results)
                    {
                        Trivia.Enqueue(new TriviaQuestion
                        {
                            Question = Uri.UnescapeDataString(q.Question),
                            CorrectAnswer = Uri.UnescapeDataString(q.CorrectAnswer),
                            IncorrectAnswers = q.IncorrectAnswers
                                .Select(a => Uri.UnescapeDataString(a))
                                .ToArray(),
                            Category = Uri.UnescapeDataString(q.Category),
                        });
                    }
                    TriviaOnline = true;
                }
            }
            catch
            {
                TriviaOnline = false;
            }
        }

        // ==========================================================
        // JOKES — v2.jokeapi.dev
        // ==========================================================
        public async Task FetchJokeAsync()
        {
            try
            {
                var url = "https://v2.jokeapi.dev/joke/Any"
                             + "?blacklistFlags=nsfw,racist&type=single";
                var response = await _http.GetFromJsonAsync<JokeAPIResponse>(url);

                if (response?.Joke != null)
                {
                    Jokes.Enqueue(response.Joke);
                    JokesOnline = true;
                }
            }
            catch
            {
                JokesOnline = false;
            }
        }

        // ==========================================================
        // FACTS — uselessfacts.jsph.pl
        // ==========================================================
        public async Task FetchFactAsync()
        {
            try
            {
                var url = "https://uselessfacts.jsph.pl/api/v2"
                             + "/facts/random?language=en";
                var response = await _http.GetFromJsonAsync<UselessFactResponse>(url);

                if (response?.Text != null)
                {
                    Facts.Enqueue(response.Text);
                    FactsOnline = true;
                }
            }
            catch
            {
                FactsOnline = false;
            }
        }

        // ==========================================================
        // ADVICE — api.adviceslip.com
        // ==========================================================
        public async Task FetchAdviceAsync()
        {
            try
            {
                var url = "https://api.adviceslip.com/advice";
                var response = await _http.GetFromJsonAsync<AdviceSlipResponse>(url);

                if (response?.Slip?.Advice != null)
                {
                    Advice.Enqueue(response.Slip.Advice);
                    AdviceOnline = true;
                }
            }
            catch
            {
                AdviceOnline = false;
            }
        }

        // ==========================================================
        // DEQUEUE HELPERS — safe pop with fallback
        // ==========================================================
        public string? PopJoke() =>
            Jokes.TryDequeue(out var j) ? j : null;

        public string? PopFact() =>
            Facts.TryDequeue(out var f) ? f : null;

        public string? PopAdvice() =>
            Advice.TryDequeue(out var a) ? a : null;

        public TriviaQuestion? PopTrivia() =>
            Trivia.TryDequeue(out var t) ? t : null;

        // ==========================================================
        // STATUS MESSAGE — Nova voice offline messages
        // ==========================================================
        public string TriviaOfflineMessage =>
            "Trivia uplink offline — the void is interference-heavy right now. Try again in a moment 🌌";
        public string JokesOfflineMessage =>
            "My humor feed is down. The void is humorless today ☠️";
        public string FactsOfflineMessage =>
            "Fact database offline. The cosmos keeps its secrets today 🌌";
        public string AdviceOfflineMessage =>
            "Advice channel down. The void offers only silence 🌌";
    }

    // ==========================================================
    // JSON RESPONSE MODELS
    // ==========================================================

    // ── OpenTDB ────────────────────────────────────────────────
    public class OpenTDBResponse
    {
        [JsonPropertyName("response_code")]
        public int ResponseCode { get; set; }

        [JsonPropertyName("results")]
        public List<OpenTDBQuestion>? Results { get; set; }
    }

    public class OpenTDBQuestion
    {
        [JsonPropertyName("question")]
        public string Question { get; set; } = "";

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer { get; set; } = "";

        [JsonPropertyName("incorrect_answers")]
        public List<string> IncorrectAnswers { get; set; } = new();

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";
    }

    // ── JokeAPI ────────────────────────────────────────────────
    public class JokeAPIResponse
    {
        [JsonPropertyName("joke")]
        public string? Joke { get; set; }
    }

    // ── UselessFacts ───────────────────────────────────────────
    public class UselessFactResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    // ── AdviceSlip ─────────────────────────────────────────────
    public class AdviceSlipResponse
    {
        [JsonPropertyName("slip")]
        public AdviceSlip? Slip { get; set; }
    }

    public class AdviceSlip
    {
        [JsonPropertyName("advice")]
        public string? Advice { get; set; }
    }
}