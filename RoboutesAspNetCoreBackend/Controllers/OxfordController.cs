// ==========================================================
// OxfordController.cs — Oxford Dictionary Proxy
// Receives requests from NovaAPIService (Blazor WASM)
// and forwards them to Oxford server-side.
// No CORS issues — browser never touches Oxford directly.
// ==========================================================

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace RoboutesAspNetCoreBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("BlazorClient")]
    public class OxfordController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // ── Oxford credentials ─────────────────────────────
        private const string OxfordBase =
       "https://od-api-sandbox.oxforddictionaries.com/api/v2";
        private const string OxfordLang = "en-gb";

        public OxfordController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        // ==========================================================
        // GET api/oxford/define/{word}?domain={domainHint}
        // ==========================================================
        [HttpGet("define/{word}")]
        public async Task<IActionResult> Define(
            string word,
            [FromQuery] string domain = "")
        {
            if (string.IsNullOrWhiteSpace(word))
                return BadRequest("Word is required.");

            // ── Pull credentials from config ───────────────
            var appId = _configuration["Oxford:AppId"];
            var appKey = _configuration["Oxford:AppKey"];

            if (string.IsNullOrEmpty(appId) ||
                string.IsNullOrEmpty(appKey))
                return StatusCode(500,
                    "Oxford credentials not configured.");

            // ── Build Oxford URL ───────────────────────────
            var url = $"{OxfordBase}/entries/{OxfordLang}/" +
                      $"{Uri.EscapeDataString(word.ToLower())}" +
                      $"?fields=definitions,examples," +
                       "pronunciations,domains";

            // ── Call Oxford server-side ────────────────────
            try
            {
                var client = _httpClientFactory.CreateClient();

                using var request = new HttpRequestMessage(
                    HttpMethod.Get, url);
                request.Headers.Add("app_id", appId);
                request.Headers.Add("app_key", appKey);

                var response = await client.SendAsync(request);

                // Pass status + raw JSON straight back to Nova
                var rawJson = await response.Content
                    .ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(
                        (int)response.StatusCode, rawJson);

                return Content(rawJson, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }
        [HttpGet("freedict/{word}")]
        public async Task<IActionResult> FreeDictionary(string word)
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"https://api.dictionaryapi.dev/api/v2/entries/en/" +
                      $"{Uri.EscapeDataString(word.ToLower())}";

            var response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url));
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, body);

            return Content(body, "application/json");
        }
        [HttpGet("merriam/{word}")]
        public async Task<IActionResult> Merriam(string word)
        {
            var apiKey = _configuration["Merriam:ApiKey"];
            var client = _httpClientFactory.CreateClient();

            var url = $"https://www.dictionaryapi.com/api/v3/references/collegiate/json/" +
                      $"{Uri.EscapeDataString(word.ToLower())}?key={apiKey}";

            var response = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Get, url));
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, body);

            return Content(body, "application/json");
        }

        [HttpGet("test")]
        public async Task<IActionResult> Test()
        {
            var appId = _configuration["Oxford:AppId"];
            var appKey = _configuration["Oxford:AppKey"];

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "https://od-api-sandbox.oxforddictionaries.com/api/v2/entries/en-gb/cat?fields=definitions");

            request.Headers.Add("app_id", appId);
            request.Headers.Add("app_key", appKey);

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            return Content($"Status: {response.StatusCode}\n\nBody: {body}", "text/plain");
        }

    }




}
