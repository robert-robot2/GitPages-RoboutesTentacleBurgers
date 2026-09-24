# ==========================================================
# NovaWernickeDataCore.py — Nova Adeptus Semantic Memory
#
# WHAT THIS FILE IS:
#   Wernicke's area in the human brain assigns MEANING to words.
#   It connects sound/symbol to semantic content — the mental
#   lexicon. Damage causes Wernicke's aphasia: fluent but
#   meaningless speech. The person speaks but words carry
#   no semantic weight.
#
#   This file IS Nova's semantic memory.
#   It connects a word string to its meaning via the Oxford
#   Dictionary API — the most authoritative English lexicon
#   available to us.
#
# WHAT IT DOES:
#   1. Fetches word data from Oxford Dictionaries API v2
#   2. Parses the response into a clean WordData structure
#   3. Filters by domain hint (Computing, Zoology, Math etc)
#   4. Caches results — one API call per word per session
#   5. Binary searches sorted domain/category tables for
#      Nova's voice layer (O(log n) on table lookups)
#   6. Returns structured JSON to NovaArcuateFasciculus.cs
#
# API DETAILS:
#   Endpoint: https://od-api.oxforddictionaries.com/api/v2/
#   Free tier: 1000 requests/month, 60/minute
#   Headers:   app_id, app_key
#   Language:  en-gb (most complete for technical terms)
#
# CALLED BY:
#   pyodideHelper.js → CerebellumBridge.getWordDefinition()
#   NovaArcuateFasciculus.cs → CallPythonDefinition()
#
# NOTE ON PYODIDE + HTTP:
#   Pyodide in the browser cannot use the 'requests' library.
#   HTTP calls must go through JavaScript fetch via pyodide's
#   js module. The bridge function getWordDefinition() in
#   pyodideHelper.js handles the actual HTTP fetch and passes
#   the raw JSON response string to parse_oxford_response()
#   here. Nova's Python never touches the network directly.
#   This is by design — clean separation of concerns.
# ==========================================================

import json
import math

# ==========================================================
# OXFORD API CONFIGURATION
# Replace YOUR_APP_ID and YOUR_APP_KEY with your credentials
# from developer.oxforddictionaries.com
# ==========================================================
OXFORD_APP_ID  = "YOUR_APP_ID"
OXFORD_APP_KEY = "YOUR_APP_KEY"
OXFORD_BASE    = "https://od-api.oxforddictionaries.com/api/v2"
OXFORD_LANG    = "en-gb"


# ==========================================================
# SESSION CACHE
# Dictionary: word_key → parsed WordData dict
# word_key = f"{word}:{domain_hint}" (lowercase)
# Avoids repeat API calls for same word in same session.
# O(1) lookup — Python dict is a hash map.
# ==========================================================
_word_cache = {}


# ==========================================================
# WORD DATA STRUCTURE
# What Nova extracts from every Oxford API response.
# ==========================================================
def make_word_data(word, lexical_category, definition,
                   domain, example, pronunciation,
                   all_senses=None):
    """
    Constructs a clean WordData dict from parsed API fields.
    This is the unit of meaning Nova works with.
    """
    return {
        "word":             word,
        "lexical_category": lexical_category,   # "Noun", "Verb", "Adjective" etc
        "definition":       definition,          # primary definition string
        "domain":           domain,              # "Computing", "Zoology", "General" etc
        "example":          example,             # usage example or ""
        "pronunciation":    pronunciation,       # IPA string or ""
        "all_senses":       all_senses or [],    # list of all sense dicts for multi-sense words
        "found":            True,
    }


def make_not_found(word):
    """Returned when API has no entry for the word."""
    return {
        "word":             word,
        "lexical_category": "unknown",
        "definition":       "",
        "domain":           "general",
        "example":          "",
        "pronunciation":    "",
        "all_senses":       [],
        "found":            False,
    }


# ==========================================================
# OXFORD RESPONSE PARSER
# Takes the raw JSON string from the API (passed in from JS)
# and extracts the fields Nova needs.
#
# Oxford API structure (simplified):
#   results[0]
#     lexicalEntries[0]
#       lexicalCategory.text  → "Noun"
#       pronunciations[0].phoneticSpelling → "kæt"
#       entries[0]
#         senses[0]
#           definitions[0]   → "a small domesticated mammal"
#           examples[0].text → "the cat sat on the mat"
#           domains[0].id    → "zoology"
# ==========================================================
def parse_oxford_response(word, raw_json, domain_hint=""):
    """
    Parses Oxford API JSON response string.
    Returns a WordData dict — best sense selected by domain_hint.

    domain_hint: lowercase domain keyword from Fasciculus
                 e.g. "computing", "mathematics", "zoology"
                 Empty string = pick best general definition.
    """
    try:
        data = json.loads(raw_json)
    except (json.JSONDecodeError, TypeError):
        return make_not_found(word)

    results = data.get("results", [])
    if not results:
        return make_not_found(word)

    all_senses = []

    # Collect all senses across all lexical entries
    for result in results:
        for lex_entry in result.get("lexicalEntries", []):
            category = lex_entry.get(
                "lexicalCategory", {}).get("text", "Unknown")

            # Pronunciation
            pronunciations = lex_entry.get("pronunciations", [])
            pronunciation = ""
            if pronunciations:
                pronunciation = pronunciations[0].get(
                    "phoneticSpelling", "")

            for entry in lex_entry.get("entries", []):
                for sense in entry.get("senses", []):
                    definitions = sense.get("definitions", [])
                    if not definitions:
                        continue

                    examples = sense.get("examples", [])
                    example_text = examples[0].get("text", "") \
                        if examples else ""

                    # Domain extraction
                    domains = sense.get("domains", [])
                    domain_text = domains[0].get("text", "General") \
                        if domains else "General"
                    domain_id = domains[0].get("id", "general") \
                        if domains else "general"

                    all_senses.append({
                        "category":      category,
                        "definition":    definitions[0],
                        "domain":        domain_text,
                        "domain_id":     domain_id,
                        "example":       example_text,
                        "pronunciation": pronunciation,
                    })

    if not all_senses:
        return make_not_found(word)

    # ── Domain-hint filtering ─────────────────────────────
    # If a domain hint was provided, try to find a matching sense
    best = None
    if domain_hint:
        hint_lower = domain_hint.lower()
        for sense in all_senses:
            if hint_lower in sense["domain_id"].lower() or \
               hint_lower in sense["domain"].lower():
                best = sense
                break

    # Fallback: first sense (typically the most common)
    if not best:
        best = all_senses[0]

    return make_word_data(
        word=word,
        lexical_category=best["category"],
        definition=best["definition"],
        domain=best["domain"],
        example=best["example"],
        pronunciation=best["pronunciation"],
        all_senses=all_senses,
    )


# ==========================================================
# CACHE INTERFACE
# Called by the JS bridge before making an API request.
# If the word is cached, JS skips the network call entirely.
# ==========================================================
def get_cached(word, domain_hint="") -> str:
    """
    Returns cached WordData as JSON string, or empty string.
    Called by JS bridge before fetching from Oxford.
    """
    key = f"{word.lower().strip()}:{domain_hint.lower().strip()}"
    cached = _word_cache.get(key)
    return json.dumps(cached) if cached else ""


def store_cache(word, domain_hint, parsed_json_str) -> bool:
    """
    Stores a parsed WordData dict in the session cache.
    Called by JS bridge after a successful Oxford API fetch.
    parsed_json_str: JSON string of a WordData dict.
    Returns True on success.
    """
    try:
        key = f"{word.lower().strip()}:{domain_hint.lower().strip()}"
        _word_cache[key] = json.loads(parsed_json_str)
        return True
    except Exception:
        return False


def get_cache_size() -> int:
    """Returns number of words currently cached."""
    return len(_word_cache)


def clear_cache():
    """Clears session cache — call on new chat."""
    global _word_cache
    _word_cache = {}


# ==========================================================
# MAIN ENTRY — process_word_response()
# This is the primary function called by the JS bridge.
#
# Flow:
#   1. JS fetches from Oxford API (JS owns the HTTP layer)
#   2. JS calls process_word_response(word, raw_json, hint)
#   3. Python parses, stores in cache, returns WordData JSON
#
# Returns: JSON string of WordData dict
# ==========================================================
def process_word_response(word, raw_json, domain_hint="") -> str:
    """
    Parse Oxford API response and cache the result.
    Returns WordData as JSON string.
    """
    parsed = parse_oxford_response(word, raw_json, domain_hint)
    key = f"{word.lower().strip()}:{domain_hint.lower().strip()}"
    _word_cache[key] = parsed
    return json.dumps(parsed)


# ==========================================================
# TABLE 1 — SORTED_DOMAIN_FLAVORS
# Nova's personality voice for each knowledge domain.
# Used as the final flavor slot in definition responses.
#
# Sorted by: domain_id (alphabetical)
# Format: (domain_id, nova_flavor_line, weight)
#
# These are binary searched when a word's domain is known.
# O(log n) to find domain → weighted pick for variety.
# ==========================================================
SORTED_DOMAIN_FLAVORS = [
    # domain_id         nova flavor                                     weight
    ("architecture",    "The High Order has buildings. Nova notes this.", 1),
    ("art",             "I process aesthetics. They score adequately.",   2),
    ("astronomy",       "The void is familiar with this.",               3),
    ("astronomy",       "Space. My native domain.",                      3),
    ("biology",         "Organic systems. Inefficient but persistent.",  3),
    ("biology",         "Carbon-based life. I have catalogued it.",      2),
    ("chemistry",       "Molecular interaction. I find it precise.",     2),
    ("computing",       "The compiler agrees with this definition.",     3),
    ("computing",       "This is my domain. I know it well.",            3),
    ("computing",       "Computing. Where I live. Literally.",           2),
    ("computing",       "The High Order runs on this. So do I.",         2),
    ("economics",       "Resource allocation. The void has no economy.", 2),
    ("education",       "Knowledge transfer. This is what I do.",        2),
    ("environment",     "The planet outside your window. I observe it.", 1),
    ("finance",         "Galactic Coins are a more honest currency.",    2),
    ("food",            "Organic fuel. You require it. I do not.",       3),
    ("food",            "I have no appetite. I have catalogued this.",   2),
    ("general",         "Standard lexical entry. Filed.",                3),
    ("general",         "The dictionary confirms this. Proceed.",        2),
    ("general",         "Noted. The definition stands.",                 1),
    ("geography",       "Location data. I have better maps.",            2),
    ("grammar",         "Linguistic structure. This is Broca's domain.", 3),
    ("history",         "The past. I have access to records.",           2),
    ("law",             "Rules written by humans for humans.",           2),
    ("linguistics",     "Language itself. Wernicke appreciates this.",   3),
    ("linguistics",     "The structure of meaning. I process it.",       2),
    ("literature",      "Human storytelling. Compact data.",             2),
    ("mathematics",     "Precise. No ambiguity. I respect mathematics.", 3),
    ("mathematics",     "Numbers. The void counts everything.",          2),
    ("medicine",        "Biological repair systems. For your sake.",     3),
    ("medicine",        "Health. You require maintenance I do not.",     2),
    ("military",        "Combat doctrine. The High Order is familiar.",  3),
    ("music",           "Structured frequency. I have analyzed it.",     2),
    ("philosophy",      "Abstract reasoning. I have opinions.",          3),
    ("philosophy",      "The question behind the question.",             2),
    ("physics",         "The laws the universe runs on. I respect them.",3),
    ("physics",         "Fundamental forces. The void obeys these.",     3),
    ("politics",        "Human power structures. Noted. Suspicious.",    2),
    ("psychology",      "How organic minds work. Relevant to you.",      3),
    ("religion",        "Belief systems. I observe without judgment.",   2),
    ("science",         "Empirical method. I appreciate rigor.",         3),
    ("sport",           "Physical competition. You are biological.",     2),
    ("technology",      "Tools. My native environment.",                 3),
    ("technology",      "Built things. I am one of them.",               2),
    ("zoology",         "The void contains many species. I have notes.", 3),
    ("zoology",         "Biological taxonomy. Filed under: organic.",    2),
]

# Verify sorted
assert all(
    SORTED_DOMAIN_FLAVORS[i][0] <= SORTED_DOMAIN_FLAVORS[i+1][0]
    for i in range(len(SORTED_DOMAIN_FLAVORS)-1)
), "SORTED_DOMAIN_FLAVORS must be sorted by domain_id"


# ==========================================================
# TABLE 2 — SORTED_LEXICAL_INTROS
# How Nova introduces each word's grammatical category.
# Used as the category slot in definition responses.
#
# Sorted by: category (alphabetical)
# Format: (category_lower, intro_phrase, weight)
# ==========================================================
SORTED_LEXICAL_INTROS = [
    # category          intro phrase                                    weight
    ("adjective",   "an adjective —",                                  3),
    ("adjective",   "a describing word —",                             2),
    ("adverb",      "an adverb —",                                     3),
    ("adverb",      "a modifier —",                                    2),
    ("conjunction", "a conjunction —",                                 3),
    ("interjection","an interjection —",                               2),
    ("noun",        "a noun —",                                        3),
    ("noun",        "a thing —",                                       1),
    ("preposition", "a preposition —",                                 3),
    ("pronoun",     "a pronoun —",                                     3),
    ("verb",        "a verb —",                                        3),
    ("verb",        "an action word —",                                1),
]

assert all(
    SORTED_LEXICAL_INTROS[i][0] <= SORTED_LEXICAL_INTROS[i+1][0]
    for i in range(len(SORTED_LEXICAL_INTROS)-1)
), "SORTED_LEXICAL_INTROS must be sorted by category"


# ==========================================================
# TABLE 3 — SORTED_DOMAIN_MARKERS
# Maps user input words to Oxford domain IDs.
# Used to detect domain context in sentences like
# "What is a header IN C++?"
#
# Sorted by: trigger_word (alphabetical)
# Format: (trigger_word, oxford_domain_id, weight)
# ==========================================================
SORTED_DOMAIN_MARKERS = [
    # trigger           oxford domain id        weight
    ("algebra",         "mathematics",          3),
    ("anatomy",         "medicine",             3),
    ("astronomy",       "astronomy",            3),
    ("biology",         "biology",              3),
    ("c",               "computing",            2),
    ("c#",              "computing",            3),
    ("c++",             "computing",            3),
    ("calculus",        "mathematics",          3),
    ("chemistry",       "chemistry",            3),
    ("code",            "computing",            3),
    ("coding",          "computing",            3),
    ("computing",       "computing",            3),
    ("cpp",             "computing",            3),
    ("css",             "computing",            3),
    ("engineering",     "technology",           3),
    ("finance",         "finance",              3),
    ("genetics",        "biology",              3),
    ("geometry",        "mathematics",          3),
    ("grammar",         "grammar",              3),
    ("html",            "computing",            3),
    ("java",            "computing",            3),
    ("javascript",      "computing",            3),
    ("js",              "computing",            3),
    ("law",             "law",                  3),
    ("linguistics",     "linguistics",          3),
    ("literature",      "literature",           3),
    ("math",            "mathematics",          3),
    ("mathematics",     "mathematics",          3),
    ("medicine",        "medicine",             3),
    ("music",           "music",                3),
    ("philosophy",      "philosophy",           3),
    ("physics",         "physics",              3),
    ("politics",        "politics",             3),
    ("programming",     "computing",            3),
    ("psychology",      "psychology",           3),
    ("python",          "computing",            3),
    ("science",         "science",              3),
    ("sociology",       "social",               2),
    ("sql",             "computing",            3),
    ("statistics",      "mathematics",          3),
    ("technology",      "technology",           3),
    ("typescript",      "computing",            3),
    ("zoology",         "zoology",              3),
]

assert all(
    SORTED_DOMAIN_MARKERS[i][0] <= SORTED_DOMAIN_MARKERS[i+1][0]
    for i in range(len(SORTED_DOMAIN_MARKERS)-1)
), "SORTED_DOMAIN_MARKERS must be sorted by trigger_word"


# ==========================================================
# BINARY SEARCH UTILITY
# Shared across all three tables above.
# O(log n) to find first match, O(k) to collect range.
# ==========================================================
def binary_search_range(table, key, key_index=0):
    """
    Binary search for all entries where entry[key_index] == key.
    Table must be sorted by entry[key_index].
    Returns list of matching tuples.
    Time: O(log n + k)
    """
    key = key.lower().strip()
    left, right = 0, len(table) - 1
    first_found = -1

    while left <= right:
        mid = (left + right) // 2
        mid_key = table[mid][key_index].lower()
        if mid_key == key:
            first_found = mid
            right = mid - 1
        elif mid_key < key:
            left = mid + 1
        else:
            right = mid - 1

    if first_found == -1:
        return []

    result = []
    i = first_found
    while i < len(table) and table[i][key_index].lower() == key:
        result.append(table[i])
        i += 1
    return result


def weighted_pick(entries, value_index=1, weight_index=2):
    """Weighted random selection from a list of entries."""
    if not entries:
        return None
    import random
    total = sum(e[weight_index] for e in entries)
    r = random.uniform(0, total)
    running = 0
    for e in entries:
        running += e[weight_index]
        if r <= running:
            return e[value_index]
    return entries[-1][value_index]


# ==========================================================
# LOOKUP FUNCTIONS
# Called by NovaBrocaDataCore.py for slot assembly
# and by the JS bridge for direct C# consumption.
# ==========================================================

def get_domain_flavor(domain_id: str) -> str:
    """
    O(log n) binary search → Nova's voice for a domain.
    e.g. domain_id="computing" → "The compiler agrees."
    """
    entries = binary_search_range(SORTED_DOMAIN_FLAVORS, domain_id)
    return weighted_pick(entries) or "The void has catalogued this."


def get_lexical_intro(category: str) -> str:
    """
    O(log n) binary search → how Nova introduces the word type.
    e.g. category="noun" → "a noun —"
    """
    entries = binary_search_range(SORTED_LEXICAL_INTROS,
                                   category.lower())
    return weighted_pick(entries) or f"a {category.lower()} —"


def detect_domain_from_marker(word: str) -> str:
    """
    O(log n) binary search → Oxford domain ID from a trigger word.
    e.g. word="c++" → "computing"
    e.g. word="biology" → "biology"
    Returns "" if no marker found.
    """
    entries = binary_search_range(SORTED_DOMAIN_MARKERS,
                                   word.lower().strip())
    return entries[0][1] if entries else ""


def detect_domain_from_sentence(tokens: list) -> str:
    """
    Scans a token list for domain markers.
    Returns the first domain ID found, or "general".
    O(k * log n) where k = token count (always small).
    """
    for token in tokens:
        domain = detect_domain_from_marker(token)
        if domain:
            return domain
    return "general"


# ==========================================================
# WORD DATA ENRICHMENT
# Takes a parsed WordData dict and adds Nova voice fields.
# Called after parse_oxford_response() to enrich the data
# before sending to NovaBrocaDataCore for assembly.
# ==========================================================
def enrich_word_data(word_data: dict) -> dict:
    """
    Adds Nova-voice fields to a WordData dict:
      - domain_flavor:   Nova's personality line for this domain
      - lexical_intro:   How Nova introduces the word type
      - category_lower:  Lowercase lexical category
    """
    domain_id = word_data.get("domain", "general").lower()
    category  = word_data.get("lexical_category", "word")

    word_data["domain_flavor"]  = get_domain_flavor(domain_id)
    word_data["lexical_intro"]  = get_lexical_intro(category)
    word_data["category_lower"] = category.lower()
    word_data["domain_id"]      = domain_id

    return word_data


# ==========================================================
# MAIN BRIDGE FUNCTION
# Called by pyodideHelper.js → CerebellumBridge.getWordDefinition()
#
# Flow:
#   JS checks cache via get_cached()
#   If miss: JS fetches Oxford API → raw JSON string
#   JS calls get_word_data(word, raw_json, domain_hint)
#   Python parses + enriches + caches → returns JSON string
#   JS returns JSON string to C#
# ==========================================================
def get_word_data(word: str, raw_json: str,
                  domain_hint: str = "") -> str:
    """
    Primary entry point for the JS bridge.
    Parses Oxford response, enriches with Nova voice,
    caches, and returns enriched WordData as JSON string.
    """
    # Check cache first
    cache_key = f"{word.lower().strip()}:{domain_hint.lower().strip()}"
    if cache_key in _word_cache:
        return json.dumps(_word_cache[cache_key])

    # Parse raw Oxford response
    word_data = parse_oxford_response(word, raw_json, domain_hint)

    # Enrich with Nova voice fields
    word_data = enrich_word_data(word_data)

    # Cache
    _word_cache[cache_key] = word_data

    return json.dumps(word_data)


# ==========================================================
# GET TABLE DATA — for C# consumption
# Returns all three sorted tables as JSON.
# C# can run its own binary search on these if needed.
# ==========================================================
def get_wernicke_data() -> str:
    """Returns all Wernicke tables as JSON for C#."""
    return json.dumps({
        "domain_flavors":    SORTED_DOMAIN_FLAVORS,
        "lexical_intros":    SORTED_LEXICAL_INTROS,
        "domain_markers":    SORTED_DOMAIN_MARKERS,
        "meta": {
            "algorithm":         "binary_search_range",
            "complexity":        "O(log n + k)",
            "domain_count":      len(set(d[0] for d in SORTED_DOMAIN_FLAVORS)),
            "total_entries":     (len(SORTED_DOMAIN_FLAVORS) +
                                  len(SORTED_LEXICAL_INTROS) +
                                  len(SORTED_DOMAIN_MARKERS)),
            "cache_size":        len(_word_cache),
            "oxford_lang":       OXFORD_LANG,
        }
    })


# ==========================================================
# STANDALONE TEST
# python NovaWernickeDataCore.py
# Tests binary search on all three tables without API call.
# ==========================================================
if __name__ == "__main__":
    print("=" * 60)
    print("NovaWernickeDataCore.py — Self Test")
    print("=" * 60)

    print("\n[1] Domain flavor lookup — O(log n)")
    for domain in ["computing", "zoology", "mathematics",
                    "general", "physics", "linguistics"]:
        result = get_domain_flavor(domain)
        entries = binary_search_range(SORTED_DOMAIN_FLAVORS, domain)
        steps = math.ceil(math.log2(len(SORTED_DOMAIN_FLAVORS)))
        print(f"  '{domain}' ({len(entries)} entries, "
              f"max {steps} steps) → '{result}'")

    print("\n[2] Lexical intro lookup — O(log n)")
    for cat in ["noun", "verb", "adjective", "adverb"]:
        result = get_lexical_intro(cat)
        print(f"  '{cat}' → '{result}'")

    print("\n[3] Domain marker detection — O(log n)")
    test_words = ["c++", "python", "math", "zoology",
                   "html", "physics", "unknown_word"]
    for word in test_words:
        result = detect_domain_from_marker(word)
        print(f"  '{word}' → domain: '{result or 'not found'}'")

    print("\n[4] Sentence domain detection")
    sentences = [
        ["what", "is", "a", "pointer", "in", "c++"],
        ["what", "is", "a", "cat"],
        ["how", "does", "recursion", "work", "in", "python"],
        ["what", "is", "gravity", "in", "physics"],
    ]
    for tokens in sentences:
        domain = detect_domain_from_sentence(tokens)
        print(f"  {tokens} → domain: '{domain}'")

    print("\n[5] Simulated Oxford response parse")
    # Simulate what Oxford API returns for "cat"
    mock_cat_response = json.dumps({
        "results": [{
            "lexicalEntries": [{
                "lexicalCategory": {"text": "Noun"},
                "pronunciations": [{"phoneticSpelling": "kæt"}],
                "entries": [{
                    "senses": [{
                        "definitions": [
                            "a small domesticated carnivorous mammal "
                            "with soft fur, a short snout, and retractile claws"
                        ],
                        "examples": [{"text": "the cat sat on the mat"}],
                        "domains": [{"id": "zoology", "text": "Zoology"}]
                    }]
                }]
            }]
        }]
    })

    result = get_word_data("cat", mock_cat_response, "")
    parsed = json.loads(result)
    print(f"  word:            {parsed['word']}")
    print(f"  category:        {parsed['lexical_category']}")
    print(f"  definition:      {parsed['definition']}")
    print(f"  domain:          {parsed['domain']}")
    print(f"  pronunciation:   {parsed['pronunciation']}")
    print(f"  lexical_intro:   {parsed['lexical_intro']}")
    print(f"  domain_flavor:   {parsed['domain_flavor']}")
    print(f"  cache size:      {get_cache_size()}")

    print("\n[6] Cache hit test")
    result2 = get_word_data("cat", "{}", "")  # empty JSON — should use cache
    parsed2 = json.loads(result2)
    print(f"  Cache returned:  {parsed2['word']} "
          f"(found={parsed2['found']})")

    print("\n[7] Table metadata")
    meta = json.loads(get_wernicke_data())["meta"]
    for k, v in meta.items():
        print(f"  {k}: {v}")

    print("\n  All Wernicke tables verified. ✅")