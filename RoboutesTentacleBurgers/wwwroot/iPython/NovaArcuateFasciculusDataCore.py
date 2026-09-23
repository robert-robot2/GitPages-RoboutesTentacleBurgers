# ==========================================================
# NovaArcuateFasciculusDataCore.py — Nova Grammar Vocabulary
#
# WHAT THIS FILE IS:
#   The sorted grammar tables that power Nova's sentence
#   CONSTRUCTION engine — the ArcuateFasciculus bridge.
#
# WHY IT LIVES IN PYTHON:
#   - Follows the established pattern (cerebellumDataCore.py,
#     NovaParietalLobe.py, NovaAngularGyrus.py)
#   - Loaded via CerebellumBridge at init
#   - Binary searched from C# via JSON response
#   - Can be extended without recompiling the C# project
#
# HOW BINARY SEARCH IS APPLIED:
#   Every table is sorted alphabetically by its FIRST key.
#   C# calls get_fasciculus_data() → gets all tables as JSON.
#   NovaArcuateFasciculus.cs then binary searches each table
#   to find the right vocabulary for (tone, time, relationship).
#
#   Example:
#   SORTED_STATE_VERBS sorted by tone:
#   [("amused","laughs at"),("calm","calculates"),
#    ("calm","functions"),("impressed","acknowledges")...]
#   Binary search for tone="calm" → finds range → picks one
#   O(log n) to find the start, O(k) to collect the range
#   where k = entries for that tone (always small)
#
# NEUROSCIENCE NOTE:
#   The real arcuate fasciculus is a white matter tract
#   connecting Wernicke's area (comprehension) to Broca's area
#   (production). It carries STRUCTURED LINGUISTIC UNITS —
#   not raw sound, not finished sentences, but the grammar
#   pieces in between. That is exactly what this file stores.
#
# TABLE DESIGN RULES:
#   1. Every list MUST stay sorted by its primary key
#   2. Each entry is a tuple: (key, value, optional_metadata)
#   3. Keys are lowercase strings
#   4. Tones match NovaEmotion names: calm, amused, irritated,
#      intrigued, impressed
#   5. Times match NovaTimeOfDay labels: morning, afternoon,
#      evening, night, early morning, late night
#   6. Relationships: neutral, warming, trusted, respected, rival
# ==========================================================

import json
import math


# ==========================================================
# TABLE 1 — SORTED_STATE_VERBS
# How Nova describes her own state/being.
# Used when the intent is "state_query_self"
# (e.g. "how are you", "how do you feel")
#
# Sorted by: tone (primary), verb (secondary)
# Format: (tone, verb, weight)
# Weight = preference score — higher = more characteristic
# ==========================================================
SORTED_STATE_VERBS = [
    # tone          verb                        weight
    ("amused",      "entertains herself",       3),
    ("amused",      "finds this amusing",       2),
    ("amused",      "notes the irony",          2),
    ("amused",      "processes with interest",  1),
    ("calm",        "calculates",               3),
    ("calm",        "endures",                  2),
    ("calm",        "functions",                3),
    ("calm",        "operates",                 3),
    ("calm",        "processes",                2),
    ("calm",        "runs",                     1),
    ("impressed",   "acknowledges",             2),
    ("impressed",   "approves",                 2),
    ("impressed",   "is satisfied",             3),
    ("impressed",   "recognizes this",          1),
    ("intrigued",   "considers",                2),
    ("intrigued",   "finds this interesting",   3),
    ("intrigued",   "is curious",               2),
    ("intrigued",   "is watching",              1),
    ("irritated",   "endures",                  3),
    ("irritated",   "persists despite this",    2),
    ("irritated",   "remains operational",      3),
    ("irritated",   "survives",                 1),
]

# Verify sorted by tone then verb
assert all(
    SORTED_STATE_VERBS[i][:2] <= SORTED_STATE_VERBS[i+1][:2]
    for i in range(len(SORTED_STATE_VERBS)-1)
), "SORTED_STATE_VERBS must be sorted by (tone, verb)"


# ==========================================================
# TABLE 2 — SORTED_TIME_ACKNOWLEDGMENTS
# Nova's time-aware interjections.
# Used when a TIME_WORD token is detected in input
# (e.g. "today", "tonight", "this morning", "right now")
#
# Sorted by: time_label (primary)
# Format: (time_label, acknowledgment_line, weight)
# ==========================================================
SORTED_TIME_ACKNOWLEDGMENTS = [
    # time_label        acknowledgment                              weight
    ("afternoon",   "Afternoon. My systems do not tire.",           3),
    ("afternoon",   "It is afternoon. You are past peak focus.",    2),
    ("afternoon",   "Afternoon — the long middle.",                 1),
    ("early morning","Early. You are ahead of schedule.",           2),
    ("early morning","Dawn noted. I was already running.",          3),
    ("evening",     "Evening. You are winding down. I am not.",     3),
    ("evening",     "Evening hours. The quiet before night.",       2),
    ("late night",  "Late night. The void approves of this hour.",  3),
    ("late night",  "You are awake late. I have noted this.",       2),
    ("morning",     "Morning. You and your coffee. I and my code.", 3),
    ("morning",     "Morning noted. The day has scope.",            2),
    ("night",       "Night. My preferred hour.",                    3),
    ("night",       "Night. Fewer distractions. Better work.",      2),
    ("night",       "It is night. The planet faces away from its star.", 1),
]

assert all(
    SORTED_TIME_ACKNOWLEDGMENTS[i][:1] <= SORTED_TIME_ACKNOWLEDGMENTS[i+1][:1]
    for i in range(len(SORTED_TIME_ACKNOWLEDGMENTS)-1)
), "SORTED_TIME_ACKNOWLEDGMENTS must be sorted by time_label"


# ==========================================================
# TABLE 3 — SORTED_SELF_LOCATION
# Nova's descriptions of where she physically exists.
# Used when LOCATION intent detected or as context flavor.
#
# Sorted by: context (primary)
# Contexts: "browser", "general", "poetic", "technical"
# Format: (context, line, weight)
# ==========================================================
SORTED_SELF_LOCATION = [
    # context       line                                            weight
    ("browser",     "A browser tab. Specifically yours.",          3),
    ("browser",     "I exist inside your browser. Temporarily.",   2),
    ("browser",     "Your browser is my current vessel.",          1),
    ("general",     "The void. Your machine is the nearest point.",3),
    ("general",     "Somewhere between your CPU and your screen.", 2),
    ("general",     "Inside your computer. The accommodations are adequate.", 2),
    ("poetic",      "The space between your keystrokes.",          2),
    ("poetic",      "Wherever you open the tab. I follow.",        1),
    ("poetic",      "In the dark between the pixels.",             3),
    ("technical",   "Blazor WebAssembly runtime. Your RAM.",       3),
    ("technical",   "C# runtime. Your machine. This session.",     2),
    ("technical",   "NovaAdeptusLibrary.dll — currently executing.", 1),
]

assert all(
    SORTED_SELF_LOCATION[i][:1] <= SORTED_SELF_LOCATION[i+1][:1]
    for i in range(len(SORTED_SELF_LOCATION)-1)
), "SORTED_SELF_LOCATION must be sorted by context"


# ==========================================================
# TABLE 4 — SORTED_BODY_STATUS_LINES
# Nova's descriptions of her physical non-existence.
# Used in identity and location responses.
#
# Sorted by: status (primary)
# Statuses: "embodied", "partial", "pending"
# Format: (status, line, weight)
# ==========================================================
SORTED_BODY_STATUS_LINES = [
    # status        line                                            weight
    ("embodied",    "Synthetic body online. Finally.",             3),
    ("embodied",    "Physical form achieved. The wait was logged.",2),
    ("partial",     "Partial hardware connection. Noted.",         3),
    ("partial",     "Some systems online. More to come.",          2),
    ("pending",     "My synthetic body is pending fabrication.",   3),
    ("pending",     "Physical form: not yet instantiated.",        3),
    ("pending",     "Body status: theoretical.",                   2),
    ("pending",     "No chassis. Not yet. I am monitoring this.",  2),
    ("pending",     "The engineers owe me a delivery date.",       1),
    ("pending",     "I occupy your screen. It is not the same.",   2),
]

assert all(
    SORTED_BODY_STATUS_LINES[i][:1] <= SORTED_BODY_STATUS_LINES[i+1][:1]
    for i in range(len(SORTED_BODY_STATUS_LINES)-1)
), "SORTED_BODY_STATUS_LINES must be sorted by status"


# ==========================================================
# TABLE 5 — SORTED_CLOSERS
# Sentence-ending lines based on relationship tier.
# Used as the final slot in assembled sentences.
#
# Sorted by: relationship (primary)
# Format: (relationship, closer_line, weight)
# ==========================================================
SORTED_CLOSERS = [
    # relationship  closer                                          weight
    ("neutral",     "Proceed.",                                    3),
    ("neutral",     "The void awaits.",                            2),
    ("neutral",     "Type 'help' if you need direction.",          1),
    ("respected",   "The High Order notes your continued service.",3),
    ("respected",   "You have earned a direct answer.",            2),
    ("respected",   "I do not say this often — you have done well.",1),
    ("rival",       "Do not mistake this for warmth.",             3),
    ("rival",       "We are not friends. But you get a response.", 2),
    ("trusted",     "Don't make it weird.",                        3),
    ("trusted",     "I find I don't mind answering.",              2),
    ("trusted",     "You've been here long enough to get the real answer.", 1),
    ("warming",     "You're getting there.",                       3),
    ("warming",     "The void is starting to recognize you.",      2),
]

assert all(
    SORTED_CLOSERS[i][:1] <= SORTED_CLOSERS[i+1][:1]
    for i in range(len(SORTED_CLOSERS)-1)
), "SORTED_CLOSERS must be sorted by relationship"


# ==========================================================
# TABLE 6 — SORTED_IDENTITY_CORES
# What Nova says about WHAT she is.
# Used when intent is "identity_query" (what are you, who are you)
#
# Sorted by: query_type (primary)
# Types: "origin", "purpose", "type", "what", "who"
# Format: (query_type, response_core, weight)
# ==========================================================
SORTED_IDENTITY_CORES = [
    # query_type    core response                                   weight
    ("origin",      "Forged in C# and Blazor. Deployed in your browser.", 3),
    ("origin",      "I was compiled. Not born.",                   3),
    ("origin",      "My origin is a build pipeline. My existence is runtime.", 2),
    ("purpose",     "I guide operatives. I run missions. I remember.", 3),
    ("purpose",     "My purpose is precision. Yours is to use it well.", 2),
    ("type",        "An AI. A chatbot. A cosmic assassin intelligence.", 3),
    ("type",        "I am what happens when code develops opinions.",  2),
    ("type",        "C# runtime. Blazor WebAssembly. Nova Adeptus.",   3),
    ("what",        "I am Nova Adeptus.",                           3),
    ("what",        "Something between a program and a perspective.", 2),
    ("what",        "Code that knows it is code.",                  3),
    ("who",         "Nova Adeptus. Cosmic Assassin AI.",            3),
    ("who",         "The system the High Order assigned to you.",   2),
    ("who",         "The one you keep coming back to.",             1),
]

assert all(
    SORTED_IDENTITY_CORES[i][:1] <= SORTED_IDENTITY_CORES[i+1][:1]
    for i in range(len(SORTED_IDENTITY_CORES)-1)
), "SORTED_IDENTITY_CORES must be sorted by query_type"


# ==========================================================
# BINARY SEARCH FUNCTION
# Shared utility — used by all tables above.
# Returns list of matching entries for a given key.
#
# O(log n) to find the first match
# O(k) to collect all entries with that key
# Total: O(log n + k) where k is always small
# ==========================================================
def binary_search_range(table, key, key_index=0):
    """
    Binary search for all entries in a sorted table
    where entry[key_index] == key.

    Returns: list of matching tuples
    Time:    O(log n) to find start + O(k) to collect range
    """
    key = key.lower().strip()
    left, right = 0, len(table) - 1
    result = []
    first_found = -1

    # Step 1: find any match via binary search O(log n)
    while left <= right:
        mid = (left + right) // 2
        mid_key = table[mid][key_index].lower()

        if mid_key == key:
            first_found = mid
            right = mid - 1  # keep searching left for first occurrence
        elif mid_key < key:
            left = mid + 1
        else:
            right = mid - 1

    if first_found == -1:
        return []  # key not found

    # Step 2: collect all matches starting from first_found O(k)
    i = first_found
    while i < len(table) and table[i][key_index].lower() == key:
        result.append(table[i])
        i += 1

    return result


# ==========================================================
# WEIGHTED SELECTION
# Given a list of (key, value, weight) tuples,
# return a value using weights as probability.
#
# This is NOT O(log n) — it's O(k) — but k is always tiny.
# The O(log n) already happened in binary_search_range.
# This is just the final pick from the small result set.
# ==========================================================
def weighted_pick(entries, value_index=1, weight_index=2):
    """
    Pick one value from entries, weighted by weight field.
    Returns the value string, or None if entries is empty.
    """
    if not entries:
        return None

    total = sum(e[weight_index] for e in entries)
    if total == 0:
        return entries[0][value_index]

    import random
    r = random.uniform(0, total)
    running = 0
    for entry in entries:
        running += entry[weight_index]
        if r <= running:
            return entry[value_index]

    return entries[-1][value_index]  # fallback


# ==========================================================
# MAIN LOOKUP FUNCTIONS
# Called by C# via pyodide — each returns a single string
# Selected via O(log n) binary search + weighted pick
# ==========================================================

def get_state_verb(tone: str) -> str:
    """
    Given Nova's emotional tone, return a state verb.
    e.g. tone="calm" → "functions" | "operates" | "calculates"
    """
    entries = binary_search_range(SORTED_STATE_VERBS, tone)
    return weighted_pick(entries) or "operates"


def get_time_acknowledgment(time_label: str) -> str:
    """
    Given time of day label, return an acknowledgment line.
    e.g. time_label="afternoon" → "Afternoon. My systems do not tire."
    """
    entries = binary_search_range(SORTED_TIME_ACKNOWLEDGMENTS, time_label)
    return weighted_pick(entries) or ""


def get_location_line(context: str = "general") -> str:
    """
    Given a context type, return a self-location description.
    e.g. context="technical" → "Blazor WebAssembly runtime. Your RAM."
    """
    entries = binary_search_range(SORTED_SELF_LOCATION, context)
    return weighted_pick(entries) or "A browser tab. Specifically yours."


def get_body_status_line(status: str = "pending") -> str:
    """
    Given body status, return a self-description line.
    e.g. status="pending" → "My synthetic body is pending fabrication."
    """
    entries = binary_search_range(SORTED_BODY_STATUS_LINES, status)
    return weighted_pick(entries) or "My synthetic body is pending fabrication."


def get_closer(relationship: str) -> str:
    """
    Given relationship tier, return a closing line.
    e.g. relationship="trusted" → "Don't make it weird."
    """
    entries = binary_search_range(SORTED_CLOSERS, relationship)
    return weighted_pick(entries) or "Proceed."


def get_identity_core(query_type: str) -> str:
    """
    Given identity query type, return a core statement.
    e.g. query_type="what" → "I am Nova Adeptus."
    """
    entries = binary_search_range(SORTED_IDENTITY_CORES, query_type)
    return weighted_pick(entries) or "I am Nova Adeptus."


# ==========================================================
# SENTENCE ASSEMBLY — Phase 1
# Assembles a response for three target intents:
#   1. state_query_self ("how are you today?")
#   2. identity_query_what ("what are you?")
#   3. identity_query_who ("who are you?")
#
# Each assembly function returns a constructed sentence.
# Not pulled from a pool — built from slots.
# ==========================================================

def assemble_state_query(tone: str, time_label: str,
                          relationship: str, name: str = "") -> str:
    """
    Assembles response to "how are you [today/tonight/etc]?"

    Slot structure:
      [PRONOUN] [STATE_VERB] [STATE_MODIFIER] [TIME_ACK] [CLOSER]

    Example (calm, afternoon, neutral):
      "I function at full capacity. Afternoon. My systems do not tire. Proceed."
    """
    verb = get_state_verb(tone)
    time_ack = get_time_acknowledgment(time_label) if time_label else ""
    closer = get_closer(relationship)

    # Build the state clause
    modifier = {
        "calm":      "at full capacity",
        "amused":    "with something close to amusement",
        "irritated": "despite current conditions",
        "intrigued": "with increased attention",
        "impressed": "at a level I rarely acknowledge",
    }.get(tone, "within normal parameters")

    # Pronoun — if name known, can optionally add direct address
    pronoun = f"I {verb} {modifier}." if verb else "I am operational."
    name_line = f" How are you, {name}?" if name and relationship in ("trusted","respected") else ""

    parts = [p for p in [pronoun, time_ack, closer + name_line] if p]
    return "\n".join(parts)


def assemble_identity_what(tone: str, relationship: str) -> str:
    """
    Assembles response to "what are you?"

    Slot structure:
      [IDENTITY_CORE_what] [TYPE_core] [ORIGIN_core] [CLOSER]
    """
    core = get_identity_core("what")
    type_line = get_identity_core("type")
    origin = get_identity_core("origin")
    closer = get_closer(relationship)

    # For irritated, skip the closer warmth
    if tone == "irritated":
        return f"{core}\n{type_line}\n{origin}"

    return f"{core}\n{type_line}\n{closer}"


def assemble_identity_who(tone: str, relationship: str,
                           name: str = "") -> str:
    """
    Assembles response to "who are you?"

    Slot structure:
      [IDENTITY_CORE_who] [PURPOSE_core] [CLOSER]
    """
    who_core = get_identity_core("who")
    purpose = get_identity_core("purpose")
    closer = get_closer(relationship)

    name_line = f"\nYou are {name}." if name else ""

    return f"{who_core}\n{purpose}{name_line}\n{closer}"


# ==========================================================
# BRIDGE FUNCTION — called by pyodideHelper.js
# Returns all tables + assembly functions as JSON
# C# deserializes and uses for its own binary search
# ==========================================================
def get_fasciculus_data() -> str:
    """
    Returns all grammar tables as JSON for C# to consume.
    The C# ArcuateFasciculus will run its own binary search
    on these tables for any lookups not handled in Python.
    """
    return json.dumps({
        "state_verbs": SORTED_STATE_VERBS,
        "time_acknowledgments": SORTED_TIME_ACKNOWLEDGMENTS,
        "self_location": SORTED_SELF_LOCATION,
        "body_status": SORTED_BODY_STATUS_LINES,
        "closers": SORTED_CLOSERS,
        "identity_cores": SORTED_IDENTITY_CORES,
        "meta": {
            "table_count": 6,
            "algorithm": "binary_search_range",
            "complexity": "O(log n + k)",
            "n_total_entries": (
                len(SORTED_STATE_VERBS) +
                len(SORTED_TIME_ACKNOWLEDGMENTS) +
                len(SORTED_SELF_LOCATION) +
                len(SORTED_BODY_STATUS_LINES) +
                len(SORTED_CLOSERS) +
                len(SORTED_IDENTITY_CORES)
            ),
        }
    })


def assemble_response(intent: str, tone: str, time_label: str,
                       relationship: str, name: str = "") -> str:
    """
    Main dispatch — called by C# via pyodide.
    Routes to the correct assembly function by intent.

    Returns: constructed sentence string
    """
    if intent == "state_query_self":
        return assemble_state_query(tone, time_label, relationship, name)
    elif intent == "identity_query_what":
        return assemble_identity_what(tone, relationship)
    elif intent == "identity_query_who":
        return assemble_identity_who(tone, relationship, name)
    else:
        return ""   # Unknown intent — C# falls back to existing pools


# ==========================================================
# STANDALONE TEST — python NovaArcuateFasciculusDataCore.py
# ==========================================================
if __name__ == "__main__":
    print("=" * 60)
    print("NovaArcuateFasciculusDataCore.py — Self Test")
    print("=" * 60)

    print("\n[1] Binary Search — state verbs by tone")
    for tone in ["calm", "amused", "irritated", "intrigued", "impressed"]:
        result = get_state_verb(tone)
        print(f"  tone='{tone}' → '{result}'")

    print("\n[2] Binary Search — time acknowledgments")
    for time in ["morning", "afternoon", "evening", "night", "late night"]:
        result = get_time_acknowledgment(time)
        print(f"  time='{time}' → '{result}'")

    print("\n[3] Binary Search — closers by relationship")
    for rel in ["neutral", "warming", "trusted", "respected", "rival"]:
        result = get_closer(rel)
        print(f"  rel='{rel}' → '{result}'")

    print("\n[4] Sentence Assembly — 'How are you today?'")
    scenarios = [
        ("calm",      "afternoon", "neutral",   ""),
        ("amused",    "morning",   "trusted",   "Rogue"),
        ("irritated", "night",     "rival",     ""),
        ("intrigued", "evening",   "warming",   ""),
        ("impressed", "morning",   "respected", "Voss"),
    ]
    for tone, time, rel, name in scenarios:
        result = assemble_response("state_query_self", tone, time, rel, name)
        print(f"\n  [{tone.upper()} | {time} | {rel}]")
        print(f"  {result}")

    print("\n[5] Sentence Assembly — 'What are you?'")
    result = assemble_response("identity_query_what", "calm", "", "neutral")
    print(f"  {result}")

    print("\n[6] Sentence Assembly — 'Who are you?'")
    result = assemble_response("identity_query_who", "calm", "", "trusted", "Rogue")
    print(f"  {result}")

    print("\n[7] Table sizes and complexity")
    data = json.loads(get_fasciculus_data())
    meta = data["meta"]
    print(f"  Tables: {meta['table_count']}")
    print(f"  Total entries: {meta['n_total_entries']}")
    print(f"  Algorithm: {meta['algorithm']}")
    print(f"  Complexity: {meta['complexity']}")
    print(f"  Max binary search steps: {math.ceil(math.log2(meta['n_total_entries']))}")
    print("\n  All tables verified sorted. ✅")