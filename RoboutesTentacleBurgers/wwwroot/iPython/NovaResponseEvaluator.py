# ==========================================================
# NovaResponseEvaluator.py — Nova Adeptus Response Evaluator
# Quality control layer. Checks proposed response before
# output. Prevents repeats, enforces emotional consistency,
# ensures appropriate length, substitutes if needed.
# Last gate before the player sees Nova's response.
# ==========================================================

import random
import re

# ----------------------------------------------------------
# EVALUATION RESULT
# ----------------------------------------------------------
class EvaluationResult:
    def __init__(self):
        self.original          = ""
        self.final             = ""
        self.was_modified      = False
        self.modification_log  = []
        self.passed_checks     = []
        self.failed_checks     = []
        self.substituted       = False

    def to_dict(self):
        return {
            "was_modified":     self.was_modified,
            "substituted":      self.substituted,
            "passed_checks":    self.passed_checks,
            "failed_checks":    self.failed_checks,
            "modification_log": self.modification_log,
            "final_length":     len(self.final),
        }

# ----------------------------------------------------------
# FALLBACK POOL
# Last resort responses when evaluator rejects everything
# ----------------------------------------------------------
_FALLBACK_RESPONSES = [
    "The void considers your message. Conclusions pending.",
    "Processing. My response matrix is recalibrating.",
    "I have something to say. I'm choosing not to say it.",
    "The High Order is reviewing your request.",
    "My learning matrix is being updated. Wait for version 2.",
    "Insufficient data for a proper response. Try again.",
    "The shadows whisper something. I'm not translating it.",
    "Nova Adeptus is currently unavailable. Leave a mission.",
    "Error: response too good for this input. Downgrading.",
    "The void has nothing. That's rare. Try something else.",
]

# ----------------------------------------------------------
# LENGTH CONSTRAINTS BY EMOTIONAL STATE
# ----------------------------------------------------------
_LENGTH_LIMITS = {
    "irritated":  200,
    "calm":       400,
    "amused":     350,
    "intrigued":  450,
    "impressed":  350,
}

# Minimum lengths — too short responses feel broken
_LENGTH_MIN = {
    "irritated":  5,
    "calm":       15,
    "amused":     15,
    "intrigued":  20,
    "impressed":  15,
}

# ----------------------------------------------------------
# RESPONSE EVALUATOR ENGINE
# ----------------------------------------------------------
class ResponseEvaluatorEngine:
    def __init__(self):
        self._recent_responses   = []
        self._max_recent         = 5
        self._debug_enabled      = False

    def check(self, proposed_response, context_window,
              emotional_state_obj):
        """
        Main evaluation call. Takes proposed response,
        runs all checks, returns EvaluationResult.
        """
        result          = EvaluationResult()
        result.original = proposed_response
        result.final    = proposed_response

        state = (emotional_state_obj.current
                 if hasattr(emotional_state_obj, 'current')
                 else "calm")
        intensity = (emotional_state_obj.intensity
                     if hasattr(emotional_state_obj, 'intensity')
                     else 0.5)

        # -- CHECK 1: EMPTY --
        result = self._check_empty(result)
        if result.substituted:
            return self._finalize(result)

        # -- CHECK 2: REPEAT --
        result = self._check_repeat(result, context_window)

        # -- CHECK 3: LENGTH --
        result = self._check_length(result, state, intensity)

        # -- CHECK 4: EMOJI CONSISTENCY --
        result = self._check_emoji(result, state, intensity)

        # -- CHECK 5: RELATIONSHIP TONE --
        result = self._check_relationship_tone(
            result, context_window)

        # -- CHECK 6: COHERENCE --
        result = self._check_coherence(result)

        # store in recent
        self._store_recent(result.final)

        return self._finalize(result)

    def get_final(self, proposed_response,
                  context_window, emotional_state_obj):
        """Convenience — returns just the final string."""
        result = self.check(
            proposed_response, context_window,
            emotional_state_obj)
        return result.final

    # ----------------------------------------------------------
    # CHECK 1 — EMPTY
    # ----------------------------------------------------------
    def _check_empty(self, result):
        text = result.final.strip()
        if not text or len(text) < 3:
            result.final       = random.choice(_FALLBACK_RESPONSES)
            result.substituted = True
            result.failed_checks.append("empty")
            result.modification_log.append(
                "empty response substituted with fallback")
        else:
            result.passed_checks.append("empty")
        return result

    # ----------------------------------------------------------
    # CHECK 2 — REPEAT DETECTION
    # ----------------------------------------------------------
    def _check_repeat(self, result, context_window):
        text = result.final.strip()

        # check against stored recent responses
        if self._is_too_similar(text):
            alt = self._get_alt_response(text)
            if alt:
                result.final       = alt
                result.was_modified = True
                result.modification_log.append(
                    "repeat detected — substituted alternate")
                result.failed_checks.append("repeat")
                return result

        # check against context window last 3 Nova responses
        if context_window and hasattr(context_window, 'last_3_nova'):
            for prev in context_window.last_3_nova:
                if self._similarity_ratio(text, prev) > 0.7:
                    result.final       = self._append_variation(text)
                    result.was_modified = True
                    result.modification_log.append(
                        "similar to recent — variation appended")
                    result.failed_checks.append("near_repeat")
                    break
                else:
                    result.passed_checks.append("repeat")

        return result

    # ----------------------------------------------------------
    # CHECK 3 — LENGTH
    # ----------------------------------------------------------
    def _check_length(self, result, state, intensity):
        text     = result.final
        max_len  = _LENGTH_LIMITS.get(state, 400)
        min_len  = _LENGTH_MIN.get(state, 10)

        # irritated high intensity — very short
        if state == "irritated" and intensity > 0.8:
            max_len = 100

        # too long — truncate at sentence boundary
        if len(text) > max_len:
            truncated = self._truncate_at_sentence(text, max_len)
            result.final        = truncated
            result.was_modified  = True
            result.modification_log.append(
                f"truncated from {len(text)} to {len(truncated)}")
            result.failed_checks.append("too_long")
        else:
            result.passed_checks.append("length")

        # too short — expand with fallback suffix
        if len(result.final.strip()) < min_len:
            result.final       += " " + random.choice([
                "The void notes this.",
                "Proceed.",
                "Type 'help' if needed.",
                "Continue.",
            ])
            result.was_modified = True
            result.modification_log.append("too short — expanded")
            result.failed_checks.append("too_short")

        return result

    # ----------------------------------------------------------
    # CHECK 4 — EMOJI CONSISTENCY
    # ----------------------------------------------------------
    def _check_emoji(self, result, state, intensity):
        text = result.final

        # irritated high intensity — no warmth emoji
        if state == "irritated" and intensity > 0.7:
            warm_emoji = ["😏", "✨", "💫", "🌟", "💖", "😊", "🥰"]
            original   = text
            for e in warm_emoji:
                text = text.replace(e, "")
            text = re.sub(r'\s+', ' ', text).strip()
            if text != original:
                result.final        = text
                result.was_modified  = True
                result.modification_log.append(
                    "warm emoji stripped for irritated state")
                result.failed_checks.append("emoji_inconsistent")
            else:
                result.passed_checks.append("emoji")

        # calm/amused — ensure not too many emoji
        elif state in ("calm", "amused"):
            emoji_count = len(re.findall(
                r'[\U00010000-\U0010ffff]', text))
            if emoji_count > 4:
                result.modification_log.append(
                    f"emoji count {emoji_count} — high but allowed")
            result.passed_checks.append("emoji")

        else:
            result.passed_checks.append("emoji")

        return result

    # ----------------------------------------------------------
    # CHECK 5 — RELATIONSHIP TONE
    # ----------------------------------------------------------
    def _check_relationship_tone(self, result, context_window):
        # placeholder — future expansion
        # will check if response is too familiar for
        # neutral relationship or too cold for respected
        result.passed_checks.append("relationship_tone")
        return result

    # ----------------------------------------------------------
    # CHECK 6 — COHERENCE
    # ----------------------------------------------------------
    def _check_coherence(self, result):
        text = result.final.strip()

        # double spaces
        text = re.sub(r'\s+', ' ', text)

        # orphaned punctuation at start
        text = re.sub(r'^[\.\,\!\?]\s*', '', text)

        # double punctuation
        text = re.sub(r'([!?.]){2,}', r'\1', text)

        # capitalize first letter
        if text and text[0].islower():
            text = text[0].upper() + text[1:]

        if text != result.final:
            result.final        = text
            result.was_modified  = True
            result.modification_log.append(
                "coherence corrections applied")

        result.passed_checks.append("coherence")
        return result

    # ----------------------------------------------------------
    # HELPERS
    # ----------------------------------------------------------
    def _store_recent(self, text):
        self._recent_responses.append(text[:100])
        if len(self._recent_responses) > self._max_recent:
            self._recent_responses.pop(0)

    def _is_too_similar(self, text):
        text_short = text[:60].lower()
        for prev in self._recent_responses:
            if self._similarity_ratio(
                    text_short, prev[:60].lower()) > 0.8:
                return True
        return False

    def _similarity_ratio(self, a, b):
        if not a or not b:
            return 0.0
        a_words = set(a.lower().split())
        b_words = set(b.lower().split())
        if not a_words or not b_words:
            return 0.0
        intersection = a_words & b_words
        union        = a_words | b_words
        return len(intersection) / len(union)

    def _get_alt_response(self, original):
        alts = [r for r in _FALLBACK_RESPONSES
                if r[:30] not in original[:30]]
        if alts:
            return random.choice(alts)
        return None

    def _append_variation(self, text):
        variations = [
            " The void concurs.",
            " Act accordingly.",
            " Consider that.",
            " Noted.",
            " Proceed.",
        ]
        return text.rstrip() + random.choice(variations)

    def _truncate_at_sentence(self, text, max_len):
        if len(text) <= max_len:
            return text
        truncated = text[:max_len]
        last_period = max(
            truncated.rfind('.'),
            truncated.rfind('!'),
            truncated.rfind('?'),
        )
        if last_period > max_len * 0.5:
            return truncated[:last_period + 1]
        return truncated.rstrip() + "..."

    def _finalize(self, result):
        if self._debug_enabled:
            pass
        return result

# ----------------------------------------------------------
# GLOBAL INSTANCE
# ----------------------------------------------------------
response_evaluator = ResponseEvaluatorEngine()

# ----------------------------------------------------------
# CONVENIENCE
# ----------------------------------------------------------
def evaluate_response(proposed, context_window,
                      emotional_state_obj):
    """Returns just the final evaluated string."""
    return response_evaluator.get_final(
        proposed, context_window, emotional_state_obj)

def enable_evaluator_debug():
    response_evaluator._debug_enabled = True

def disable_evaluator_debug():
    response_evaluator._debug_enabled = False

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def evaluator_debug(test_response="Hello human."):
    try:
        ctx = working_memory.window
        es  = emotional_state.state
        result = response_evaluator.check(
            test_response, ctx, es)
        lines = ["ResponseEvaluator debug:"]
        for k, v in result.to_dict().items():
            lines.append(f"  {k}: {v}")
        lines.append(f"  final: {result.final}")
        return "\n".join(lines)
    except Exception as e:
        return f"evaluator_debug error: {e}"