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

    public record WordDefinition(
    string Word,
    string LexicalCategory,     // "Noun" | "Verb" | "Adjective" etc
    string Definition,          // primary definition string
    string Domain,              // "Computing" | "Zoology" | "General" etc
    string DomainId,            // lowercase domain id
    string Example,             // usage example or ""
    string Pronunciation,       // IPA string or ""
    string RawJson,             // full Oxford response — passed to Python
    bool Found                // false if API had no entry
)
    {
        public static WordDefinition NotFound(string word) => new(
            word, "unknown", "", "general", "general", "", "", "", false);
    }


    public class NovaAPIService
    {
        private readonly HttpClient _http;

        // ── Content queues ─────────────────────────────────────
        public Queue<string> Jokes { get; } = new();
        public Queue<string> Facts { get; } = new();
        public Queue<string> Advice { get; } = new();
        public Queue<TriviaQuestion> Trivia { get; } = new();

        // ── Oxford Dictionary ──────────────────────────────────
        // Replace YOUR_APP_ID and YOUR_APP_KEY with your credentials
        // from developer.oxforddictionaries.com
        /*
        private const string OxfordAppId = "";
        private const string OxfordAppKey = "";
        private const string OxfordBase =
            "https://od-api.oxforddictionaries.com/api/v2";
        private const string OxfordLang = "en-gb";
        */

        // Session cache — one fetch per unique word per session
        // Key: "word:domainhint" (lowercase)
        private readonly Dictionary<string, WordDefinition>
            _definitionCache = new();

        public bool OxfordOnline { get; private set; } = true;

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


        /*
        // ==========================================================
        // OXFORD DICTIONARY — FetchDefinitionAsync
        // Fetches word data from Oxford API v2.
        // Returns WordDefinition — always safe, never throws.
        //
        // domainHint: lowercase Oxford domain id
        //   e.g. "computing", "zoology", "mathematics"
        //   Empty = use first/best sense returned.
        // ==========================================================
        public async Task<WordDefinition> FetchDefinitionAsync(
            string word, string domainHint = "")
        {
            if (string.IsNullOrWhiteSpace(word))
                return WordDefinition.NotFound(word);

            // ── Cache check ──────────────────────────────────────
            var cacheKey = $"{word.ToLower().Trim()}:" +
                           $"{domainHint.ToLower().Trim()}";
            if (_definitionCache.TryGetValue(
                    cacheKey, out var cached))
                return cached;

            // ── API fetch ────────────────────────────────────────
            try
            {
                var url = $"{OxfordBase}/entries/{OxfordLang}/" +
                          $"{Uri.EscapeDataString(word.ToLower())}";

                // Add domain filter if hint provided
                if (!string.IsNullOrEmpty(domainHint))
                    url += $"?fields=definitions,examples," +
                           $"pronunciations,domains";
                else
                    url += "?fields=definitions,examples," +
                           "pronunciations,domains";

                using var request = new HttpRequestMessage(
                    HttpMethod.Get, url);
                request.Headers.Add("app_id", OxfordAppId);
                request.Headers.Add("app_key", OxfordAppKey);

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    // 404 = word not found (valid, not an error)
                    if (response.StatusCode ==
                        System.Net.HttpStatusCode.NotFound)
                    {
                        var notFound = WordDefinition.NotFound(word);
                        _definitionCache[cacheKey] = notFound;
                        return notFound;
                    }
                    OxfordOnline = false;
                    return WordDefinition.NotFound(word);
                }

                var rawJson = await response.Content
                    .ReadAsStringAsync();

                // ── Parse raw JSON ───────────────────────────────
                var definition = ParseOxfordResponse(
                    word, rawJson, domainHint);

                // Cache and return
                _definitionCache[cacheKey] = definition;
                OxfordOnline = true;
                return definition;
            }
            catch
            {
                OxfordOnline = false;
                return WordDefinition.NotFound(word);
            }
        }

        // ==========================================================
        // OXFORD RESPONSE PARSER
        // Parses raw Oxford JSON → WordDefinition record.
        // Mirrors parse_oxford_response() in NovaWernickeDataCore.py
        // but returns a C# record instead of a Python dict.
        // Python still does the enrichment + assembly —
        // C# just needs the key fields for routing decisions.
        // ==========================================================
        private static WordDefinition ParseOxfordResponse(
            string word, string rawJson, string domainHint)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument
                    .Parse(rawJson);
                var root = doc.RootElement;
                var results = root.GetProperty("results");

                string lexCategory = "";
                string definition = "";
                string domain = "General";
                string domainId = "general";
                string example = "";
                string pronunciation = "";

                // Best match tracking for domain hint
                bool domainMatched = false;

                foreach (var result in results.EnumerateArray())
                {
                    foreach (var lex in result
                        .GetProperty("lexicalEntries")
                        .EnumerateArray())
                    {
                        var cat = lex
                            .GetProperty("lexicalCategory")
                            .GetProperty("text")
                            .GetString() ?? "";

                        // Pronunciation
                        if (lex.TryGetProperty(
                            "pronunciations", out var prons))
                        {
                            foreach (var p in
                                prons.EnumerateArray())
                            {
                                if (p.TryGetProperty(
                                    "phoneticSpelling", out var ps))
                                {
                                    pronunciation = ps
                                        .GetString() ?? "";
                                    break;
                                }
                            }
                        }

                        foreach (var entry in lex
                            .GetProperty("entries")
                            .EnumerateArray())
                        {
                            foreach (var sense in entry
                                .GetProperty("senses")
                                .EnumerateArray())
                            {
                                // Get definition
                                if (!sense.TryGetProperty(
                                    "definitions", out var defs))
                                    continue;
                                var defArr = defs
                                    .EnumerateArray().ToList();
                                if (!defArr.Any()) continue;
                                var def = defArr[0]
                                    .GetString() ?? "";

                                // Get domain
                                string senseDomain = "General";
                                string senseDomainId = "general";
                                if (sense.TryGetProperty(
                                    "domains", out var domains))
                                {
                                    var domArr = domains
                                        .EnumerateArray().ToList();
                                    if (domArr.Any())
                                    {
                                        senseDomain = domArr[0]
                                            .GetProperty("text")
                                            .GetString()
                                            ?? "General";
                                        senseDomainId = domArr[0]
                                            .GetProperty("id")
                                            .GetString()
                                            ?? "general";
                                    }
                                }

                                // Get example
                                string senseExample = "";
                                if (sense.TryGetProperty(
                                    "examples", out var exs))
                                {
                                    var exArr = exs
                                        .EnumerateArray().ToList();
                                    if (exArr.Any())
                                        senseExample = exArr[0]
                                            .GetProperty("text")
                                            .GetString() ?? "";
                                }

                                // Domain hint match — prefer this
                                bool matchesDomain =
                                    !string.IsNullOrEmpty(domainHint)
                                    && (senseDomainId.Contains(
                                            domainHint.ToLower())
                                        || senseDomain.ToLower()
                                            .Contains(
                                            domainHint.ToLower()));

                                if (!domainMatched || matchesDomain)
                                {
                                    lexCategory = cat;
                                    definition = def;
                                    domain = senseDomain;
                                    domainId = senseDomainId;
                                    example = senseExample;
                                    domainMatched = matchesDomain;
                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(definition))
                    return WordDefinition.NotFound(word);

                return new WordDefinition(
                    word, lexCategory, definition,
                    domain, domainId, example,
                    pronunciation, rawJson, true);
            }
            catch
            {
                return WordDefinition.NotFound(word);
            }
        }

        // ── Safe dequeue helper (matches existing pattern) ────
        public WordDefinition? PopDefinition(string word)
        {
            var key = word.ToLower().Trim() + ":";
            return _definitionCache.TryGetValue(key, out var d)
                ? d : null;
        }

        public string OxfordOfflineMessage =>
            "Oxford lexicon offline. " +
            "The void keeps its definitions today. " +
            "Try again later. 🌌";
        */
        /*
        // ==========================================================
        // FREE DICTIONARY API — FetchDefinitionAsync
        // No key, no CORS, works directly from Blazor WASM.
        // https://api.dictionaryapi.dev/api/v2/entries/en/{word}
        // ==========================================================

        public async Task<WordDefinition> FetchDefinitionAsync(
            string word, string domainHint = "")
        {
            if (string.IsNullOrWhiteSpace(word))
                return WordDefinition.NotFound(word);

            var cacheKey = $"{word.ToLower().Trim()}:" +
                           $"{domainHint.ToLower().Trim()}";
            if (_definitionCache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                var url = $"https://api.dictionaryapi.dev/api/v2/entries/en/" +
                          $"{Uri.EscapeDataString(word.ToLower())}";

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var notFound = WordDefinition.NotFound(word);
                    _definitionCache[cacheKey] = notFound;
                    return notFound;
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                var definition = ParseFreeDictionaryResponse(word, rawJson);

                _definitionCache[cacheKey] = definition;
                OxfordOnline = true;
                return definition;
            }
            catch
            {
                OxfordOnline = false;
                return WordDefinition.NotFound(word);
            }
        }

        // ==========================================================
        // FREE DICTIONARY API RESPONSE PARSER
        // JSON structure:
        // [                          ← array of entries
        //   {
        //     "word": "cat",
        //     "phonetic": "kæt",
        //     "meanings": [
        //       {
        //         "partOfSpeech": "noun",
        //         "definitions": [
        //           {
        //             "definition": "a small domesticated carnivorous mammal",
        //             "example": "the cat sat on the mat"
        //           }
        //         ]
        //       }
        //     ]
        //   }
        // ]
        // ==========================================================
        private static WordDefinition ParseFreeDictionaryResponse(
            string word, string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Response is an array
                if (root.ValueKind != JsonValueKind.Array)
                    return WordDefinition.NotFound(word);

                var entries = root.EnumerateArray().ToList();
                if (!entries.Any())
                    return WordDefinition.NotFound(word);

                var entry = entries[0];

                // Phonetic / pronunciation
                string pronunciation = "";
                if (entry.TryGetProperty("phonetic", out var phonetic))
                    pronunciation = phonetic.GetString() ?? "";

                // Meanings array
                if (!entry.TryGetProperty("meanings", out var meanings))
                    return WordDefinition.NotFound(word);

                string lexCategory = "";
                string definition = "";
                string example = "";

                foreach (var meaning in meanings.EnumerateArray())
                {
                    var pos = meaning.TryGetProperty(
                        "partOfSpeech", out var p)
                        ? p.GetString() ?? "" : "";

                    if (!meaning.TryGetProperty(
                        "definitions", out var defs))
                        continue;

                    var defList = defs.EnumerateArray().ToList();
                    if (!defList.Any()) continue;

                    var firstDef = defList[0];
                    var defText = firstDef.TryGetProperty(
                        "definition", out var d)
                        ? d.GetString() ?? "" : "";

                    if (string.IsNullOrEmpty(defText)) continue;

                    // First valid meaning wins
                    lexCategory = pos;
                    definition = defText;

                    if (firstDef.TryGetProperty(
                        "example", out var ex))
                        example = ex.GetString() ?? "";

                    break; // first meaning only
                }

                if (string.IsNullOrEmpty(definition))
                    return WordDefinition.NotFound(word);

                // Free Dictionary has no domain — default to general
                return new WordDefinition(
                    word,
                    lexCategory,
                    definition,
                    "General",   // domain
                    "general",   // domainId
                    example,
                    pronunciation,
                    rawJson,
                    true);
            }
            catch
            {
                return WordDefinition.NotFound(word);
            }
        }
        */

        public async Task<WordDefinition> FetchDefinitionAsync(
    string word, string domainHint = "")
        {
            if (string.IsNullOrWhiteSpace(word))
                return WordDefinition.NotFound(word);

            var cacheKey = $"{word.ToLower().Trim()}:" +
                           $"{domainHint.ToLower().Trim()}";
            if (_definitionCache.TryGetValue(cacheKey, out var cached))
                return cached;

            try
            {
                // Datamuse — no key, no CORS, built for browsers
                var url = $"https://api.datamuse.com/words" +
                          $"?sp={Uri.EscapeDataString(word.ToLower())}" +
                          $"&md=d&max=1";

                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    var notFound = WordDefinition.NotFound(word);
                    _definitionCache[cacheKey] = notFound;
                    return notFound;
                }

                var rawJson = await response.Content.ReadAsStringAsync();
                var definition = ParseDatamuseResponse(word, rawJson);

                _definitionCache[cacheKey] = definition;
                OxfordOnline = true;
                return definition;
            }
            catch
            {
                OxfordOnline = false;
                return WordDefinition.NotFound(word);
            }
        }

        private static WordDefinition ParseDatamuseResponse(
            string word, string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Datamuse returns an array
                // [{ "word": "cat", "defs": ["n\ta small domesticated mammal"] }]
                if (root.ValueKind != JsonValueKind.Array)
                    return WordDefinition.NotFound(word);

                var entries = root.EnumerateArray().ToList();
                if (!entries.Any())
                    return WordDefinition.NotFound(word);

                var entry = entries[0];

                if (!entry.TryGetProperty("defs", out var defs))
                    return WordDefinition.NotFound(word);

                var defList = defs.EnumerateArray().ToList();
                if (!defList.Any())
                    return WordDefinition.NotFound(word);

                // Format: "n\tdefinition text here"
                // Letter before \t = part of speech
                var raw = defList[0].GetString() ?? "";
                var parts = raw.Split('\t');

                string lexCategory = parts.Length >= 1
                    ? parts[0] switch
                    {
                        "n" => "Noun",
                        "v" => "Verb",
                        "adj" => "Adjective",
                        "adv" => "Adverb",
                        _ => "Word"
                    } : "Word";

                string definition = parts.Length >= 2
                    ? parts[1] : "";

                if (string.IsNullOrEmpty(definition))
                    return WordDefinition.NotFound(word);

                return new WordDefinition(
                    word,
                    lexCategory,
                    definition,
                    "General",
                    "general",
                    "",          // no examples in Datamuse
                    "",          // no pronunciation in Datamuse
                    rawJson,
                    true);
            }
            catch
            {
                return WordDefinition.NotFound(word);
            }
        }

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