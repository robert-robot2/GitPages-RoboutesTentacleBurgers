# ==========================================================
# NovaBrocaDataCore.py — Nova Adeptus Language Production
#
# WHAT THIS FILE IS:
#   Broca's area in the human brain handles LANGUAGE PRODUCTION.
#   It takes meaning (from Wernicke) and constructs utterances —
#   chooses words, arranges grammar, controls articulation.
#   Damage causes Broca's aphasia: telegraphic, effortful speech.
#   The person knows what they mean but struggles to say it.
#
#   This file IS Nova's production layer for dictionary responses.
#   It receives enriched WordData from NovaWernickeDataCore.py
#   and assembles a complete, Nova-voiced sentence.
#
# WHAT IT DOES:
#   1. Stores sorted sentence template tables (O(log n) lookup)
#   2. Assembles responses for three intent types:
#        definition_query          "What is a cat?"
#        definition_query_domain   "What is a header in C++?"
#        process_query             "How does a cat jump?"
#   3. Applies Nova personality via domain flavor + relationship
#   4. Handles chain queries — two words, subject + action
#   5. Returns assembled sentence string to Fasciculus
#
# DEPENDS ON:
#   NovaWernickeDataCore.py — WordData dicts + domain flavor
#   Called after Wernicke has fetched and parsed the word.
#
# CALLED BY:
#   pyodideHelper.js → CerebellumBridge.getBrocaAssembly()
#   NovaArcuateFasciculus.cs → CallPythonBrocaAssembly()
# ==========================================================

import json
import math
import random

# Import Wernicke lookups — both files loaded into same Pyodide session
# These functions are available because NovaWernickeDataCore.py
# is loaded first in pyodideHelper.js initialize()
try:
    from NovaWernickeDataCore import (
        get_domain_flavor,
        get_lexical_intro,
        binary_search_range,
        weighted_pick,
    )
except ImportError:
    # Pyodide flat namespace — functions available globally
    # This branch handles the case where we're running standalone
    def get_domain_flavor(d):  return "The void has catalogued this."
    def get_lexical_intro(c):  return f"a {c} —"
    def binary_search_range(t, k, ki=0): return []
    def weighted_pick(e, vi=1, wi=2):    return None


# ==========================================================
# TABLE 1 — SORTED_DEFINITION_TEMPLATES
# Sentence templates for "What is X?" responses.
# Slots: {word}, {intro}, {definition}, {example}, {flavor}
#
# Sorted by: tone (primary)
# Format: (tone, template_string, weight)
# ==========================================================
SORTED_DEFINITION_TEMPLATES = [
    # tone          template                                            weight
    ("amused",
     "{word} is {intro} {definition}. {example_line}{flavor}",         2),
    ("amused",
     "Oh, {word}? {intro} {definition}. {flavor}",                     1),

    ("calm",
     "{word}: {intro} {definition}. {example_line}{flavor}",           3),
    ("calm",
     "{word} is {intro} {definition}. {flavor}",                       3),
    ("calm",
     "By definition — {word}: {intro} {definition}. {flavor}",         2),

    ("impressed",
     "{word}. {intro} {definition}. {example_line}Noted. {flavor}",    2),
    ("impressed",
     "Good question. {word} is {intro} {definition}. {flavor}",        1),

    ("intrigued",
     "{word} — interesting choice. {intro} {definition}. {flavor}",    2),
    ("intrigued",
     "Examining {word}: {intro} {definition}. {example_line}{flavor}", 2),

    ("irritated",
     "{word}. {intro} {definition}. {flavor}",                         3),
    ("irritated",
     "Fine. {word}: {intro} {definition}. Move on.",                   2),
]

assert all(
    SORTED_DEFINITION_TEMPLATES[i][0] <=
    SORTED_DEFINITION_TEMPLATES[i+1][0]
    for i in range(len(SORTED_DEFINITION_TEMPLATES)-1)
), "SORTED_DEFINITION_TEMPLATES must be sorted by tone"


# ==========================================================
# TABLE 2 — SORTED_DOMAIN_TEMPLATES
# Templates for "What is X in Y?" — domain-specific queries.
# Slots: {domain_intro}, {word}, {intro}, {definition}, {flavor}
#
# Sorted by: tone (primary)
# Format: (tone, template_string, weight)
# ==========================================================
SORTED_DOMAIN_TEMPLATES = [
    # tone          template                                            weight
    ("amused",
     "In {domain}, {word} is {intro} {definition}. {flavor}",          2),

    ("calm",
     "In {domain}: {word} is {intro} {definition}. {example_line}{flavor}", 3),
    ("calm",
     "{domain_cap} context — {word}: {intro} {definition}. {flavor}",  2),

    ("impressed",
     "{domain_cap}. {word} is {intro} {definition}. Precise. {flavor}",2),

    ("intrigued",
     "In the context of {domain} — {word}: {intro} {definition}. {flavor}", 2),

    ("irritated",
     "{word}. {domain_cap} definition: {intro} {definition}. {flavor}",3),
    ("irritated",
     "In {domain}: {intro} {definition}. That is all.",                 2),
]

assert all(
    SORTED_DOMAIN_TEMPLATES[i][0] <= SORTED_DOMAIN_TEMPLATES[i+1][0]
    for i in range(len(SORTED_DOMAIN_TEMPLATES)-1)
), "SORTED_DOMAIN_TEMPLATES must be sorted by tone"


# ==========================================================
# TABLE 3 — SORTED_PROCESS_TEMPLATES
# Templates for "How does X do Y?" chain queries.
# Slots: {subject}, {subject_def}, {action}, {action_def}, {flavor}
#
# Sorted by: tone (primary)
# Format: (tone, template_string, weight)
# ==========================================================
SORTED_PROCESS_TEMPLATES = [
    # tone          template                                            weight
    ("amused",
     "A {subject} — {subject_def} — {action}s by {action_method}. "
     "{flavor}",                                                         2),

    ("calm",
     "{subject_cap}: {subject_def_short}. "
     "It {action}s by {action_method}. {flavor}",                       3),
    ("calm",
     "The process: a {subject} ({subject_def_short}) "
     "{action}s via {action_method}. {flavor}",                         2),

    ("impressed",
     "Interesting. A {subject} — {subject_def_short} — "
     "{action}s by {action_method}. {flavor}",                          2),

    ("intrigued",
     "Examining the process: {subject} ({subject_def_short}) "
     "{action}s through {action_method}. {flavor}",                     3),
    ("intrigued",
     "How does a {subject} {action}? "
     "{subject_def_short}. It does so by {action_method}. {flavor}",    2),

    ("irritated",
     "A {subject} {action}s by {action_method}. "
     "That is the answer. {flavor}",                                     3),
    ("irritated",
     "{subject_cap}. {action_cap}. {action_method}. "
     "Next question.",                                                    1),
]

assert all(
    SORTED_PROCESS_TEMPLATES[i][0] <= SORTED_PROCESS_TEMPLATES[i+1][0]
    for i in range(len(SORTED_PROCESS_TEMPLATES)-1)
), "SORTED_PROCESS_TEMPLATES must be sorted by tone"


# ==========================================================
# TABLE 4 — SORTED_FALLBACK_RESPONSES
# When Oxford API returns nothing useful.
# Nova doesn't pretend to know. She says so — in character.
#
# Sorted by: tone (primary)
# Format: (tone, response, weight)
# ==========================================================
SORTED_FALLBACK_RESPONSES = [
    # tone          response                                            weight
    ("amused",
     "'{word}' — the dictionary had nothing. "
     "Even Oxford has limits apparently.",                               2),

    ("calm",
     "'{word}' returned no definition from the lexicon. "
     "The word may be too specialized, too new, or misspelled. "
     "Try again.",                                                       3),
    ("calm",
     "No entry found for '{word}'. "
     "The void acknowledges the gap.",                                   2),

    ("impressed",
     "'{word}' — not in Oxford's database. "
     "Rare. Or invented. Either way, I cannot define what isn't filed.", 2),

    ("intrigued",
     "'{word}' — Oxford has no entry. "
     "That is either a gap in the dictionary or a gap in your spelling. "
     "I will not speculate which.",                                      3),

    ("irritated",
     "No definition found for '{word}'. "
     "Check the spelling. Then try again.",                              3),
    ("irritated",
     "'{word}' — nothing. "
     "The dictionary is unimpressed. So am I.",                          2),
]

assert all(
    SORTED_FALLBACK_RESPONSES[i][0] <= SORTED_FALLBACK_RESPONSES[i+1][0]
    for i in range(len(SORTED_FALLBACK_RESPONSES)-1)
), "SORTED_FALLBACK_RESPONSES must be sorted by tone"


# ==========================================================
# TEMPLATE LOOKUP
# Binary searches sorted template tables by tone.
# Returns a randomly weighted template string.
# O(log n) to find tone range, O(k) to collect, O(1) to pick.
# ==========================================================
def get_definition_template(tone: str) -> str:
    entries = binary_search_range(
        SORTED_DEFINITION_TEMPLATES, tone.lower())
    return weighted_pick(entries) or \
        "{word} is {intro} {definition}. {flavor}"


def get_domain_template(tone: str) -> str:
    entries = binary_search_range(
        SORTED_DOMAIN_TEMPLATES, tone.lower())
    return weighted_pick(entries) or \
        "In {domain}: {word} is {intro} {definition}. {flavor}"


def get_process_template(tone: str) -> str:
    entries = binary_search_range(
        SORTED_PROCESS_TEMPLATES, tone.lower())
    return weighted_pick(entries) or \
        "{subject_cap} {action}s by {action_method}. {flavor}"


def get_fallback(tone: str, word: str) -> str:
    entries = binary_search_range(
        SORTED_FALLBACK_RESPONSES, tone.lower())
    template = weighted_pick(entries) or \
        "No definition found for '{word}'."
    return template.replace("{word}", word)


# ==========================================================
# SLOT PREPARATION HELPERS
# Clean up raw WordData fields before slot insertion.
# ==========================================================

def _short_def(definition: str, max_words: int = 12) -> str:
    """Truncates definition to max_words for compact slots."""
    words = definition.split()
    if len(words) <= max_words:
        return definition
    return " ".join(words[:max_words]) + "..."


def _example_line(example: str) -> str:
    """Formats example for inline use, or returns empty string."""
    if not example:
        return ""
    example = example.strip().rstrip(".")
    return f'For example: "{example}." '


def _action_method(verb_definition: str) -> str:
    """
    Extracts the 'how' from a verb definition.
    Oxford verb definitions often start with the action itself.
    e.g. "push oneself off the ground" → "pushing oneself off the ground"
    We convert to gerund for natural sentence flow.
    """
    if not verb_definition:
        return "an unknown mechanism"
    # Strip leading "to " if present
    d = verb_definition.lstrip("to ").strip()
    # Capitalize first word for sentence flow
    if d and not d[0].isupper():
        d = d[0].lower() + d[1:]
    return d


def _cap(s: str) -> str:
    """Capitalize first letter."""
    return s[0].upper() + s[1:] if s else s


# ==========================================================
# MAIN ASSEMBLY FUNCTIONS
# Each corresponds to one intent type.
# Receives enriched WordData dict(s) from Wernicke.
# Returns a fully assembled Nova-voiced sentence string.
# ==========================================================

def assemble_definition(word_data: dict, tone: str,
                         relationship: str) -> str:
    """
    Assembles response to "What is X?" or "Define X"

    Required word_data fields:
      word, lexical_intro, definition, domain_flavor,
      example, found

    Slot structure:
      {word} is {intro} {definition}. {example_line}{flavor}
    """
    word = word_data.get("word", "that")

    # Not found — use fallback
    if not word_data.get("found", False):
        return get_fallback(tone, word)

    # Prepare slots
    intro       = word_data.get("lexical_intro", "a word —")
    definition  = word_data.get("definition", "")
    flavor      = word_data.get("domain_flavor",
                                "The void has noted this.")
    example     = _example_line(word_data.get("example", ""))

    if not definition:
        return get_fallback(tone, word)

    # Get template via O(log n) search
    template = get_definition_template(tone)

    # Fill slots
    result = (template
              .replace("{word}",         word.capitalize())
              .replace("{intro}",        intro)
              .replace("{definition}",   definition)
              .replace("{example_line}", example)
              .replace("{flavor}",       flavor))

    return result.strip()


def assemble_definition_domain(word_data: dict, tone: str,
                                relationship: str,
                                domain_hint: str) -> str:
    """
    Assembles response to "What is X in Y?" (domain-specific)

    Additional slot: {domain}, {domain_cap}, {domain_intro}
    """
    word = word_data.get("word", "that")

    if not word_data.get("found", False):
        return get_fallback(tone, word)

    intro       = word_data.get("lexical_intro", "a term —")
    definition  = word_data.get("definition", "")
    flavor      = word_data.get("domain_flavor",
                                "The void has catalogued this.")
    example     = _example_line(word_data.get("example", ""))
    domain      = word_data.get("domain", domain_hint or "general")
    domain_id   = word_data.get("domain_id", domain_hint or "general")

    if not definition:
        return get_fallback(tone, word)

    template = get_domain_template(tone)

    result = (template
              .replace("{word}",         word.capitalize())
              .replace("{intro}",        intro)
              .replace("{definition}",   definition)
              .replace("{example_line}", example)
              .replace("{flavor}",       flavor)
              .replace("{domain_cap}",   _cap(domain))
              .replace("{domain}",       domain.lower()))

    return result.strip()


def assemble_process_query(subject_data: dict,
                            action_data: dict,
                            tone: str,
                            relationship: str) -> str:
    """
    Assembles response to "How does X do Y?" chain queries.

    subject_data: WordData for the subject noun (e.g. "cat")
    action_data:  WordData for the action verb (e.g. "jump")

    Slot structure:
      {subject_cap}: {subject_def_short}.
      It {action}s by {action_method}. {flavor}
    """
    subject = subject_data.get("word", "it")
    action  = action_data.get("word", "do")

    # If either lookup failed, graceful degradation
    if not subject_data.get("found") and not action_data.get("found"):
        return (f"I could not find definitions for "
                f"'{subject}' or '{action}' in Oxford's lexicon.")

    subject_def = subject_data.get("definition",
                                    f"a {subject}") \
                  if subject_data.get("found") else f"a {subject}"

    action_def  = action_data.get("definition",
                                   f"to {action}") \
                  if action_data.get("found") else f"to {action}"

    # Derive domain flavor — prefer action's domain
    flavor = action_data.get("domain_flavor") \
          or subject_data.get("domain_flavor") \
          or "The void observes the process."

    # Prepare slots
    subject_def_short = _short_def(subject_def, 8)
    action_method     = _action_method(action_def)

    template = get_process_template(tone)

    result = (template
              .replace("{subject_cap}",       _cap(subject))
              .replace("{subject}",           subject.lower())
              .replace("{subject_def}",       subject_def)
              .replace("{subject_def_short}", subject_def_short)
              .replace("{action_cap}",        _cap(action))
              .replace("{action}",            action.lower())
              .replace("{action_def}",        action_def)
              .replace("{action_method}",     action_method)
              .replace("{flavor}",            flavor))

    return result.strip()


# ==========================================================
# MAIN DISPATCH — assemble_broca_response()
# Called by CerebellumBridge.getBrocaAssembly()
# Single entry point for all assembly types.
#
# Parameters (all strings, passed from C# via JS):
#   intent:        "definition_query" | "definition_query_domain"
#                  | "process_query"
#   tone:          emotional tone from NovaBrain
#   relationship:  from NovaSession
#   word_data_json:     JSON string of primary WordData
#   subject_data_json:  JSON string of subject WordData (process only)
#   domain_hint:   domain context string
# ==========================================================
def assemble_broca_response(intent: str,
                             tone: str,
                             relationship: str,
                             word_data_json: str,
                             subject_data_json: str = "",
                             domain_hint: str = "") -> str:
    """
    Main dispatch function. Routes to correct assembler by intent.
    Returns assembled sentence string.
    """
    try:
        word_data = json.loads(word_data_json) \
            if word_data_json else {}
    except Exception:
        word_data = {}

    try:
        subject_data = json.loads(subject_data_json) \
            if subject_data_json else {}
    except Exception:
        subject_data = {}

    if intent == "definition_query":
        return assemble_definition(word_data, tone, relationship)

    elif intent == "definition_query_domain":
        return assemble_definition_domain(
            word_data, tone, relationship, domain_hint)

    elif intent == "process_query":
        # word_data = action, subject_data = subject
        return assemble_process_query(
            subject_data, word_data, tone, relationship)

    else:
        # Unknown intent — return empty for C# fallback
        return ""


# ==========================================================
# GET TABLE DATA — for C# consumption
# ==========================================================
def get_broca_data() -> str:
    """Returns all Broca tables as JSON for C#."""
    return json.dumps({
        "definition_templates": SORTED_DEFINITION_TEMPLATES,
        "domain_templates":     SORTED_DOMAIN_TEMPLATES,
        "process_templates":    SORTED_PROCESS_TEMPLATES,
        "fallback_responses":   SORTED_FALLBACK_RESPONSES,
        "meta": {
            "algorithm":     "binary_search_range",
            "complexity":    "O(log n + k)",
            "intent_types":  [
                "definition_query",
                "definition_query_domain",
                "process_query",
            ],
            "total_templates": (
                len(SORTED_DEFINITION_TEMPLATES) +
                len(SORTED_DOMAIN_TEMPLATES) +
                len(SORTED_PROCESS_TEMPLATES) +
                len(SORTED_FALLBACK_RESPONSES)
            ),
        }
    })


# ==========================================================
# STANDALONE TEST
# python NovaBrocaDataCore.py
# ==========================================================
if __name__ == "__main__":
    print("=" * 60)
    print("NovaBrocaDataCore.py — Self Test")
    print("=" * 60)

    # Simulate enriched WordData from Wernicke
    mock_cat = {
        "word": "cat", "found": True,
        "lexical_category": "Noun",
        "lexical_intro": "a noun —",
        "definition": "a small domesticated carnivorous mammal "
                      "with soft fur and retractile claws",
        "domain": "Zoology", "domain_id": "zoology",
        "domain_flavor": "The void contains many species. I have notes.",
        "example": "the cat sat on the mat",
        "pronunciation": "kæt",
    }

    mock_header_cpp = {
        "word": "header", "found": True,
        "lexical_category": "Noun",
        "lexical_intro": "a noun —",
        "definition": "a file containing declarations "
                      "imported by other source files",
        "domain": "Computing", "domain_id": "computing",
        "domain_flavor": "The compiler agrees with this definition.",
        "example": "include the header at the top of your file",
        "pronunciation": "hɛdə",
    }

    mock_jump = {
        "word": "jump", "found": True,
        "lexical_category": "Verb",
        "lexical_intro": "a verb —",
        "definition": "push oneself off the ground "
                      "using one's legs and feet",
        "domain": "General", "domain_id": "general",
        "domain_flavor": "The dictionary confirms this. Proceed.",
        "example": "she jumped over the puddle",
        "pronunciation": "dʒʌmp",
    }

    tones = ["calm", "irritated", "amused", "intrigued", "impressed"]

    print("\n[1] Definition query — 'What is a cat?'")
    for tone in tones:
        result = assemble_definition(mock_cat, tone, "neutral")
        print(f"\n  [{tone.upper()}]")
        print(f"  {result}")

    print("\n[2] Domain query — 'What is a header in C++?'")
    for tone in ["calm", "irritated"]:
        result = assemble_definition_domain(
            mock_header_cpp, tone, "neutral", "computing")
        print(f"\n  [{tone.upper()}]")
        print(f"  {result}")

    print("\n[3] Process query — 'How does a cat jump?'")
    for tone in ["calm", "intrigued", "amused"]:
        result = assemble_process_query(
            mock_cat, mock_jump, tone, "neutral")
        print(f"\n  [{tone.upper()}]")
        print(f"  {result}")

    print("\n[4] Not found fallback")
    mock_not_found = {"word": "xyzqq", "found": False}
    for tone in ["calm", "irritated"]:
        result = assemble_definition(mock_not_found, tone, "neutral")
        print(f"\n  [{tone.upper()}] {result}")

    print("\n[5] Dispatch function test")
    result = assemble_broca_response(
        "definition_query", "calm", "neutral",
        json.dumps(mock_cat))
    print(f"\n  definition_query: {result}")

    result = assemble_broca_response(
        "definition_query_domain", "calm", "neutral",
        json.dumps(mock_header_cpp), "", "computing")
    print(f"\n  definition_query_domain: {result}")

    result = assemble_broca_response(
        "process_query", "intrigued", "trusted",
        json.dumps(mock_jump), json.dumps(mock_cat))
    print(f"\n  process_query: {result}")

    print("\n[6] Table metadata")
    meta = json.loads(get_broca_data())["meta"]
    for k, v in meta.items():
        print(f"  {k}: {v}")

    print("\n  All Broca assembly tests complete. ✅")