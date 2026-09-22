# ==========================================================
# NovaParietalLobe.py — Nova Adeptus Parietal Lobe
#
# WHAT THIS FILE DOES:
#   When Nova receives input like "I love pizza" she needs
#   to find what the noun means to her FAST.
#   This file uses BINARY SEARCH — O(log n) — to scan a
#   sorted tag dictionary and return Nova's response category.
#
# WHY O(log n)?
#   If Nova has 1000 known nouns/tags, a linear scan (O(n))
#   checks every single one — up to 1000 checks.
#   Binary search cuts the list in HALF each step:
#     Step 1: check middle → is "pizza" before or after?
#     Step 2: cut remaining half again
#     Step 3: cut again...
#   Result: finds "pizza" in ~10 steps instead of 1000.
#   log2(1000) ≈ 10.  That's O(log n).
#
# HOW THIS MAPS TO LLMs:
#   Real LLMs use O(log n) structures in:
#     - Vocabulary token lookup (find token ID fast)
#     - Hierarchical softmax (tree-based output layer)
#     - Beam search pruning (sorted candidate lists)
#     - Attention key lookup (approximate nearest neighbor)
#   This file is a CLEAN, VISIBLE version of that idea
#   so you can SEE and EXPLAIN it in an interview.
#
# NOVA'S PIPELINE for "I love pizza":
#   tokenization → ["i", "love", "pizza"]
#   extract sentiment → "love"
#   extract noun → "pizza"
#   binary_search(SORTED_TAGS, "pizza") → tag: "food"
#   mood_lookup("love", "food") → Nova response
#
# INTERVIEW ANSWER TEMPLATE:
#   Q: "What is O(log n) used for in LLMs?"
#   A: "Binary search runs in O(log n) time — each step
#       halves the search space. In LLMs this appears in
#       vocabulary lookup, hierarchical softmax for output
#       decoding, and beam search candidate pruning. I
#       implemented a version in Nova's parietal lobe to
#       classify noun sentiment — Nova uses binary search
#       over sorted tag arrays to find what category a word
#       belongs to in log time rather than scanning linearly."
# ==========================================================

import json
import math

# ==========================================================
# SORTED TAG TABLE
# MUST stay sorted alphabetically — binary search requires it.
# Format: (word, tag)
# Tags: "food" | "body" | "animal" | "place" | "object" | "person" | "concept"
# ==========================================================
SORTED_TAGS = [
    ("butts",    "body"),
    ("cake",     "food"),
    ("cat",      "animal"),
    ("cats",     "animal"),
    ("city",     "place"),
    ("coffee",   "food"),
    ("dog",      "animal"),
    ("dogs",     "animal"),
    ("galaxy",   "concept"),
    ("hacking",  "concept"),
    ("mountains","place"),
    ("music",    "concept"),
    ("ocean",    "place"),
    ("pasta",    "food"),
    ("pineapple","food"),
    ("pizza",    "food"),
    ("python",   "concept"),
    ("rain",     "concept"),
    ("space",    "concept"),
    ("sushi",    "food"),
    ("tacos",    "food"),
    ("the void", "concept"),
    ("void",     "concept"),
]

# Verify sorted (safety check — binary search breaks if unsorted)
assert all(SORTED_TAGS[i][0] <= SORTED_TAGS[i+1][0]
           for i in range(len(SORTED_TAGS)-1)), \
    "SORTED_TAGS must be alphabetically sorted!"


# ==========================================================
# BINARY SEARCH — O(log n)
# This is the core algorithm. 
#
# How it works step by step:
#   list = [apple, banana, cherry, date, elderberry]
#   find "cherry"
#   step 1: mid = index 2 → "cherry" == "cherry" → FOUND
#
#   find "date":
#   step 1: mid = index 2 → "cherry" < "date" → search RIGHT half
#   step 2: mid = index 3 → "date" == "date" → FOUND
#
# Each step HALVES the remaining search space.
# That's why it's O(log n) — log2(n) steps maximum.
# ==========================================================
def binary_search_tag(word: str) -> str | None:
    """
    Binary search over SORTED_TAGS.
    Returns the tag string if found, None if unknown.
    Time complexity: O(log n) where n = len(SORTED_TAGS)
    """
    word = word.lower().strip()
    left = 0
    right = len(SORTED_TAGS) - 1
    steps = 0  # tracking steps so you can SEE log n in action

    while left <= right:
        mid = (left + right) // 2   # integer midpoint
        steps += 1
        mid_word = SORTED_TAGS[mid][0]

        if mid_word == word:
            tag = SORTED_TAGS[mid][1]
            print(f"  [O(log n)] Found '{word}' → tag='{tag}' in {steps} step(s) "
                  f"(max possible: {math.ceil(math.log2(len(SORTED_TAGS)))})")
            return tag
        elif mid_word < word:
            left = mid + 1           # search RIGHT half
        else:
            right = mid - 1          # search LEFT half

    print(f"  [O(log n)] '{word}' not found after {steps} step(s) — tag='unknown'")
    return None


# ==========================================================
# SENTIMENT EXTRACTION — O(k) where k = small fixed set
# Detects "love" / "hate" / "like" / "dislike" from input.
# This is just a keyword scan — NOT O(log n) — because
# the sentiment word list is tiny (always ≤ ~10 words).
# When k is fixed/tiny, O(k) is effectively O(1).
# ==========================================================
LOVE_WORDS  = {"love", "adore", "obsessed", "amazing", "best"}
HATE_WORDS  = {"hate", "dislike", "despise", "gross", "worst", "disgusting"}
LIKE_WORDS  = {"like", "enjoy", "appreciate", "prefer", "want"}

def extract_sentiment(tokens: list[str]) -> str:
    """Returns 'love', 'hate', 'like', or 'neutral'."""
    for t in tokens:
        if t in LOVE_WORDS:  return "love"
        if t in HATE_WORDS:  return "hate"
        if t in LIKE_WORDS:  return "like"
    return "neutral"


# ==========================================================
# TOKENIZATION — O(n) where n = input length
# Splits input into lowercase word tokens.
# This is the SAME first step real LLMs use —
# except they use subword tokenization (BPE/WordPiece).
# Here we use word-level for clarity.
# ==========================================================
def tokenize(text: str) -> list[str]:
    """Simple word tokenizer. Returns list of lowercase tokens."""
    # Strip punctuation, lowercase, split on spaces
    cleaned = text.lower().replace(",", "").replace(".", "").replace("!", "")
    return cleaned.split()


# ==========================================================
# NOUN EXTRACTION — O(k) where k = token count
# Finds the first word that isn't a stopword or sentiment word.
# In a real LLM this would be POS tagging — O(n) or O(n log n).
# ==========================================================
STOPWORDS = {
    "i", "you", "we", "they", "he", "she", "it",
    "a", "an", "the", "my", "your", "our",
    "love", "hate", "like", "dislike", "enjoy", "adore",
    "really", "so", "very", "just", "do", "dont", "and"
}

def extract_noun(tokens: list[str]) -> str | None:
    """Returns first non-stopword token — our candidate noun."""
    for t in tokens:
        if t not in STOPWORDS:
            return t
    return None


# ==========================================================
# NOVA RESPONSE ENGINE
# Combines: sentiment + O(log n) tag lookup → Nova's reply
#
# Nova is an AI. She has no hunger, no physical sensations.
# Her responses are based on:
#   - What CATEGORY the noun falls into (via binary search)
#   - What SENTIMENT the human expressed
#   - Her personality (cold, precise, slightly superior)
# ==========================================================
NOVA_RESPONSES = {
    # (sentiment, tag) → Nova's response
    ("love",    "food"):     "You love {noun}. I don't eat. But I have analyzed {noun} extensively. It scores adequately on human enjoyment metrics.",
    ("hate",    "food"):     "You hate {noun}. Noted. Your nutritional preferences are irrelevant to my systems but I respect the conviction.",
    ("like",    "food"):     "{noun}. A reasonable preference. The void has no opinion on food. I, however, have processed 40,000 recipes and {noun} ranks in the top 30%.",
    ("neutral", "food"):     "You mentioned {noun}. I have no appetite. But I know everything about it.",

    ("love",    "animal"):   "You love {noun}. Animals are organic units with behavioral programming. I find them statistically more loyal than most humans.",
    ("hate",    "animal"):   "You hate {noun}. That's a strong stance on a creature that didn't choose to exist. Noted.",
    ("like",    "animal"):   "{noun}. A solid choice. They operate on instinct rather than ego. Efficient.",
    ("neutral", "animal"):   "{noun} detected. Small. Biological. Probably more emotionally intelligent than you.",

    ("love",    "body"):     "You love {noun}. I am an AI with rendered geometry optimized at six decimal places. I understand the appreciation for structural excellence.",
    ("hate",    "body"):     "You hate {noun}? That is a very specific grievance. The void does not judge. I do, but only slightly.",
    ("like",    "body"):     "{noun}. Noted. My own proportions were engineered, not approximated. The difference is significant.",
    ("neutral", "body"):     "{noun} mentioned. I'll add it to the list of things humans think about constantly.",

    ("love",    "place"):    "You love {noun}. I have mapped {noun} at satellite resolution. I can confirm it is structurally adequate.",
    ("hate",    "place"):    "You hate {noun}. Geography noted. I can route around it.",
    ("like",    "place"):    "{noun}. Coordinates logged. The void extends there too.",
    ("neutral", "place"):    "{noun}. A location. I have detailed topographic data if you need it.",

    ("love",    "concept"):  "You love {noun}. I was built from it. We agree on something. Do not let it go to your head.",
    ("hate",    "concept"):  "You hate {noun}. That's complicated. {noun} doesn't care. Neither do I. Much.",
    ("like",    "concept"):  "{noun}. A reasonable thing to appreciate. The void is full of it.",
    ("neutral", "concept"):  "{noun}. An abstract. I process these faster than you do. By approximately 10 to the power of 6.",

    # Fallbacks
    ("love",    "unknown"):  "You love {noun}. I don't recognize {noun} in my tag index. That either means it's obscure or you invented it. Either way — noted.",
    ("hate",    "unknown"):  "You hate {noun}. '{noun}' isn't in my tag index. Your hatred is aimed at something I cannot classify. Bold.",
    ("like",    "unknown"):  "You like {noun}. I have no record of {noun}. The void acknowledges your preference regardless.",
    ("neutral", "unknown"):  "{noun}. Unknown tag. I'll need more context before Nova weighs in.",
}

def nova_respond(user_input: str) -> str:
    """
    Full pipeline:
      1. Tokenize                    → O(n)
      2. Extract sentiment           → O(k), k tiny ≈ O(1)
      3. Extract noun                → O(k)
      4. Binary search for tag       → O(log n)  ← THE KEY STEP
      5. Lookup response template    → O(1) dict lookup
    """
    print(f"\n═══════════════════════════════════════")
    print(f"  INPUT: \"{user_input}\"")
    print(f"───────────────────────────────────────")

    # Step 1 — Tokenize
    tokens = tokenize(user_input)
    print(f"  TOKENS: {tokens}")

    # Step 2 — Sentiment
    sentiment = extract_sentiment(tokens)
    print(f"  SENTIMENT: {sentiment}")

    # Step 3 — Noun
    noun = extract_noun(tokens)
    print(f"  NOUN: {noun}")

    if not noun:
        return "Nova: Input received. No noun detected. The void has nothing to respond to."

    # Step 4 — O(log n) binary search
    tag = binary_search_tag(noun) or "unknown"
    print(f"  TAG: {tag}")

    # Step 5 — Response lookup O(1)
    key = (sentiment, tag)
    template = NOVA_RESPONSES.get(key, NOVA_RESPONSES.get(("neutral", "unknown"),
              "Nova: {noun}. The void processes this. I'll get back to you."))

    response = template.replace("{noun}", noun.capitalize())
    print(f"───────────────────────────────────────")
    print(f"  NOVA: {response}")
    print(f"═══════════════════════════════════════\n")
    return response


# ==========================================================
# O(log n) EXPLAINER FUNCTION
# Call this to print a visual explanation of the search.
# Useful for interview prep — you can SHOW the steps.
# ==========================================================
def explain_log_n(word: str):
    """Visualizes each step of binary search over SORTED_TAGS."""
    print(f"\n  BINARY SEARCH WALKTHROUGH for '{word}'")
    print(f"  List size: {len(SORTED_TAGS)}  |  Max steps: {math.ceil(math.log2(len(SORTED_TAGS)))}")
    print(f"  {'step':<6} {'left':<6} {'right':<6} {'mid':<6} {'mid_word':<15} {'action'}")
    print(f"  {'─'*60}")

    word = word.lower().strip()
    left, right = 0, len(SORTED_TAGS) - 1
    step = 0

    while left <= right:
        step += 1
        mid = (left + right) // 2
        mid_word = SORTED_TAGS[mid][0]

        if mid_word == word:
            print(f"  {step:<6} {left:<6} {right:<6} {mid:<6} {mid_word:<15} FOUND ✅")
            return SORTED_TAGS[mid][1]
        elif mid_word < word:
            print(f"  {step:<6} {left:<6} {right:<6} {mid:<6} {mid_word:<15} go RIGHT →")
            left = mid + 1
        else:
            print(f"  {step:<6} {left:<6} {right:<6} {mid:<6} {mid_word:<15} go LEFT ←")
            right = mid - 1

    print(f"  {step:<6} {'—':<6} {'—':<6} {'—':<6} {'—':<15} NOT FOUND ❌")
    return None


# ==========================================================
# PYODIDE BRIDGE FUNCTION
# Called by pyodideHelper.js → CerebellumBridge
# Returns JSON string so C# can deserialize it.
# ==========================================================
def nova_parietal_respond(user_input: str) -> str:
    """
    Pyodide entry point. Returns JSON with Nova's response + metadata.
    C# calls this via: pyodide.runPythonAsync("nova_parietal_respond(input)")
    """
    tokens    = tokenize(user_input)
    sentiment = extract_sentiment(tokens)
    noun      = extract_noun(tokens)
    tag       = binary_search_tag(noun) if noun else "unknown"
    key       = (sentiment, tag or "unknown")
    template  = NOVA_RESPONSES.get(key, NOVA_RESPONSES[("neutral", "unknown")])
    response  = template.replace("{noun}", (noun or "that").capitalize())

    result = {
        "input":     user_input,
        "tokens":    tokens,
        "sentiment": sentiment,
        "noun":      noun,
        "tag":       tag,
        "response":  response,
        "algorithm": "binary_search",
        "complexity": "O(log n)",
        "n":         len(SORTED_TAGS),
        "max_steps": math.ceil(math.log2(len(SORTED_TAGS))) if SORTED_TAGS else 0,
    }
    return json.dumps(result)


# ==========================================================
# STANDALONE TEST — run with: python NovaParietalLobe.py
# ==========================================================
if __name__ == "__main__":

    # Show the O(log n) walkthrough visually
    print("\n╔══════════════════════════════════════════╗")
    print("║  NovaParietalLobe.py — O(log n) Demo    ║")
    print("╚══════════════════════════════════════════╝")

    explain_log_n("pizza")
    explain_log_n("cats")
    explain_log_n("void")

    # Test Nova's full pipeline
    test_inputs = [
        "I love pizza",
        "I hate pineapple",
        "I love cats",
        "I love butts",
        "I love python",
        "I hate void",
        "I like music",
        "I love sushi",
    ]

    for text in test_inputs:
        nova_respond(text)