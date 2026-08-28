# ==========================================================
# NovaSensory.py — Nova Adeptus Sensory Processing Layer
# Receives raw input, cleans and classifies it into a
# structured SensorySignal for all downstream brain systems.
# ==========================================================

import re

# ----------------------------------------------------------
# SENSORY SIGNAL — structured output of this layer
# Think of this as the nerve signal reaching the brain
# ----------------------------------------------------------
class SensorySignal:
    def __init__(self):
        self.raw            = ""
        self.cleaned        = ""
        self.input_type     = "statement"
        self.complexity     = "simple"
        self.word_count     = 0
        self.has_greeting   = False
        self.has_name_intro = False
        self.has_question   = False
        self.has_command    = False
        self.has_emotional  = False
        self.is_nonsense    = False
        self.language_confidence = 1.0
        self.tokens         = []

    def to_dict(self):
        return {
            "raw":                  self.raw,
            "cleaned":              self.cleaned,
            "input_type":           self.input_type,
            "complexity":           self.complexity,
            "word_count":           self.word_count,
            "has_greeting":         self.has_greeting,
            "has_name_intro":       self.has_name_intro,
            "has_question":         self.has_question,
            "has_command":          self.has_command,
            "has_emotional":        self.has_emotional,
            "is_nonsense":          self.is_nonsense,
            "language_confidence":  self.language_confidence,
            "tokens":               self.tokens,
        }

# ----------------------------------------------------------
# CONSTANTS
# ----------------------------------------------------------
_GREETINGS = {
    "hi","hello","hey","sup","yo","greetings","howdy",
    "good morning","good evening","good day","hiya","heya",
}

_NAME_TRIGGERS = {
    "my name is","call me","i am","i'm","name's","they call me",
}

_COMMANDS = {
    "accept","complete","reset","stats","help","trivia","mini",
    "hack","fight","stealth","loot","boss","mission","mood",
    "skills","history","list","reward","bonus","advance",
    "story","companion","upgrade","market","endgame","cosmic",
    "event","ship","dismiss","date","time","advice","fact","joke",
}

_QUESTION_WORDS = {
    "who","what","where","when","why","how","which","whose","whom",
    "can you","could you","would you","do you","are you","is there",
    "tell me","explain","describe",
}

_EMOTIONAL_WORDS = {
    "hate","love","angry","happy","sad","frustrated","excited",
    "bored","amazing","terrible","awesome","stupid","great",
    "scared","worried","confused","thanks","sorry","please",
}

_NONSENSE_THRESHOLD = 0.35

# ----------------------------------------------------------
# MAIN SENSE FUNCTION
# ----------------------------------------------------------
def sense(raw_input):
    """
    Takes raw user input string.
    Returns a populated SensorySignal object.
    """
    signal          = SensorySignal()
    signal.raw      = raw_input
    signal.cleaned  = _clean(raw_input)
    signal.cleaned = _fuzzy_match_command(signal.cleaned)
    signal.tokens   = _tokenize(signal.cleaned)
    signal.word_count = len(signal.tokens)

    # detect features
    signal.has_greeting   = _has_greeting(signal.cleaned)
    signal.has_name_intro = _has_name_intro(signal.cleaned)
    signal.has_question   = _has_question(signal.cleaned)
    signal.has_command    = _has_command(signal.tokens)
    signal.has_emotional  = _has_emotional(signal.tokens)
    signal.is_nonsense    = _is_nonsense(signal.tokens)

    # classify complexity
    signal.complexity = _classify_complexity(signal.word_count, signal.cleaned)

    # classify primary input type
    signal.input_type = _classify_type(signal)

    # language confidence
    signal.language_confidence = _language_confidence(signal.tokens)

    return signal

# ----------------------------------------------------------
# PRIVATE HELPERS
# ----------------------------------------------------------
def _clean(text):
    text = text.lower().strip()
    text = re.sub(r"[^\w\s\?\!\.\,\'\-]", "", text)
    text = re.sub(r"\s+", " ", text)
    return text

def _tokenize(text):
    return [w for w in re.sub(r"[^\w\s]", "", text).split() if len(w) > 0]

def _has_greeting(cleaned):
    if any(cleaned == g or cleaned.startswith(g + " ") or
           (" " + g + " ") in cleaned for g in _GREETINGS):
        return True
    return False

def _has_name_intro(cleaned):
    return any(trigger in cleaned for trigger in _NAME_TRIGGERS)

def _has_question(cleaned):
    if cleaned.endswith("?"):
        return True
    if any(cleaned.startswith(qw) or (" " + qw) in cleaned
           for qw in _QUESTION_WORDS):
        return True
    return False

def _has_command(tokens):
    return any(t in _COMMANDS for t in tokens)

def _has_emotional(tokens):
    return any(t in _EMOTIONAL_WORDS for t in tokens)

def _is_nonsense(tokens):
    if not tokens:
        return True
    # ratio of very short or unrecognizable tokens
    weird = sum(1 for t in tokens
                if len(t) > 8 and not _is_real_word(t))
    if len(tokens) > 0 and weird / len(tokens) > _NONSENSE_THRESHOLD:
        return True
    # single token that looks like keyboard mash
    if len(tokens) == 1 and len(tokens[0]) > 5:
        consonants = sum(1 for c in tokens[0]
                        if c in "bcdfghjklmnpqrstvwxyz")
        if len(tokens[0]) > 0 and consonants / len(tokens[0]) > 0.8:
            return True
    return False

def _is_real_word(word):
    # simple heuristic — alternating vowel/consonant pattern
    vowels = set("aeiou")
    vowel_count = sum(1 for c in word if c in vowels)
    if len(word) == 0:
        return False
    return 0.2 < vowel_count / len(word) < 0.8

def _fuzzy_match_command(cleaned):
    """
    Matches typo'd input to known commands.
    Returns corrected string or original if no match.
    """
    known = [
        "accept","complete","reset","stats","help","trivia",
        "mini","hack","fight","stealth","loot","boss","mission",
        "skills","history","list","reward","bonus","advance",
        "story","companion","upgrade","market","endgame","cosmic",
        "event","ship","dismiss","fact","joke","advice","name",
        "who are you","what are you","how are you","what can you do",
    ]
    
    if not cleaned:
        return cleaned
    
    # exact match — no work needed    
    if cleaned in known:
        return cleaned
    
    # single word — check edit distance
    words = cleaned.split()
    # only fuzzy match if input is a single word under 10 chars
    if len(words) == 1 and len(words[0]) <= 10:
        word = words[0]
        best_match = None
        best_score = 0
        
        for candidate in known:
            if " " in candidate:
                continue
            score = _similarity(word, candidate)
            if score > best_score and score >= 0.75:
                best_score = score
                best_match = candidate
        
        if best_match:
            return best_match
    
    return cleaned


def _similarity(a, b):
    """
    Simple character similarity ratio.
    Good enough for single word typo correction.
    """
    if not a or not b:
        return 0.0
    
    # length difference penalty
    len_diff = abs(len(a) - len(b))
    if len_diff > 3:
        return 0.0
    
    # count matching characters in sequence
    matches = 0
    a_chars = list(a)
    b_chars = list(b)
    
    for ch in a_chars:
        if ch in b_chars:
            matches += 1
            b_chars.remove(ch)
    
    return matches / max(len(a), len(b))


def _classify_complexity(word_count, cleaned):
    if word_count <= 3:
        return "simple"
    elif word_count <= 10:
        return "moderate"
    return "complex"

def _classify_type(signal):
    if signal.has_command and not signal.has_question:
        return "command"
    if signal.has_question:
        return "question"
    if signal.has_emotional and not signal.has_command:
        return "emotional"
    if signal.is_nonsense:
        return "nonsense"
    if signal.has_greeting and signal.word_count <= 3:
        return "greeting"
    return "statement"

def _language_confidence(tokens):
    if not tokens:
        return 0.0
    real = sum(1 for t in tokens if _is_real_word(t))
    return round(real / len(tokens), 3)

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def sense_debug(raw_input):
    s = sense(raw_input)
    lines = [f"SensorySignal: '{raw_input}'"]
    for k, v in s.to_dict().items():
        lines.append(f"  {k}: {v}")
    return "\n".join(lines)