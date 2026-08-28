# ==========================================================
# NovaReasoningEngine.py — Nova Adeptus Reasoning Layer
# The judgment layer. Takes signal + context + emotional
# state + relationship and scores possible response types
# before selecting the best one. This is where Nova thinks
# before she speaks.
# ==========================================================

import random

# ----------------------------------------------------------
# RESPONSE TYPES
# ----------------------------------------------------------
RESPONSE_HUMOR    = "humor"
RESPONSE_COLD     = "cold"
RESPONSE_WARM     = "warm"
RESPONSE_ENGAGE   = "engage"
RESPONSE_DEFLECT  = "deflect"
RESPONSE_INFORM   = "inform"
RESPONSE_QUESTION = "question"
RESPONSE_TAUNT    = "taunt"
RESPONSE_DISMISS  = "dismiss"

ALL_RESPONSE_TYPES = [
    RESPONSE_HUMOR,
    RESPONSE_COLD,
    RESPONSE_WARM,
    RESPONSE_ENGAGE,
    RESPONSE_DEFLECT,
    RESPONSE_INFORM,
    RESPONSE_QUESTION,
    RESPONSE_TAUNT,
    RESPONSE_DISMISS,
]

# ----------------------------------------------------------
# REASONING RESULT
# ----------------------------------------------------------
class ReasoningResult:
    def __init__(self):
        self.response_type        = RESPONSE_DEFLECT
        self.confidence           = 0.0
        self.scores               = {}
        self.reasoning_log        = []
        self.allow_question_back  = False
        self.override_personality = False
        self.content_hint         = None
        self.urgency              = "normal"

    def to_dict(self):
        return {
            "response_type":       self.response_type,
            "confidence":          round(self.confidence, 3),
            "allow_question_back": self.allow_question_back,
            "urgency":             self.urgency,
            "content_hint":        self.content_hint,
            "top_scores":          sorted(
                self.scores.items(),
                key=lambda x: -x[1]
            )[:3],
        }

# ----------------------------------------------------------
# REASONING ENGINE
# ----------------------------------------------------------
class ReasoningEngine:
    def __init__(self):
        self._log_enabled = False

    def score(self, signal, context_window,
              emotional_state_obj, relationship, intents=None):
        """
        Main reasoning call. Takes all brain inputs,
        scores every response type, returns ReasoningResult.
        """
        result = ReasoningResult()
        intents = intents or []

        # initialise scores
        scores = {rt: 0.0 for rt in ALL_RESPONSE_TYPES}

        # -- LAYER 1: EMOTIONAL STATE BASE SCORES --
        scores = self._apply_emotional_scores(
            scores, emotional_state_obj)

        # -- LAYER 2: RELATIONSHIP MODIFIERS --
        scores = self._apply_relationship_scores(
            scores, relationship)

        # -- LAYER 3: SIGNAL TYPE MODIFIERS --
        scores = self._apply_signal_scores(
            scores, signal)

        # -- LAYER 4: CONTEXT PATTERN MODIFIERS --
        scores = self._apply_context_scores(
            scores, context_window)

        # -- LAYER 5: INTENT MODIFIERS --
        scores = self._apply_intent_scores(
            scores, intents)

        # -- LAYER 6: SPECIAL OVERRIDES --
        scores = self._apply_overrides(
            scores, signal, context_window,
            emotional_state_obj, relationship)

        # select winner
        best_type  = max(scores, key=scores.get)
        best_score = scores[best_type]

        result.response_type = best_type
        result.confidence    = best_score
        result.scores        = scores

        # decide if Nova asks a question back
        result.allow_question_back = self._should_ask_back(
            best_type, emotional_state_obj, relationship,
            context_window)

        # urgency
        result.urgency = self._get_urgency(
            emotional_state_obj, context_window)

        # content hint
        result.content_hint = self._get_content_hint(
            signal, intents, context_window)

        # reasoning log
        if self._log_enabled:
            result.reasoning_log = self._build_log(
                scores, signal, emotional_state_obj,
                relationship)

        return result

    # ----------------------------------------------------------
    # LAYER 1 — EMOTIONAL STATE BASE SCORES
    # ----------------------------------------------------------
    def _apply_emotional_scores(self, scores, es):
        state = es.current if hasattr(es, 'current') else es
        intensity = es.intensity if hasattr(es, 'intensity') else 0.5

        if state == "irritated":
            scores[RESPONSE_COLD]    += 0.5 + intensity * 0.3
            scores[RESPONSE_DISMISS] += 0.4 + intensity * 0.2
            scores[RESPONSE_TAUNT]   += 0.3
            scores[RESPONSE_WARM]    -= 0.3
            scores[RESPONSE_HUMOR]   -= 0.2

        elif state == "amused":
            scores[RESPONSE_HUMOR]   += 0.5 + intensity * 0.2
            scores[RESPONSE_WARM]    += 0.3
            scores[RESPONSE_TAUNT]   += 0.2
            scores[RESPONSE_COLD]    -= 0.3

        elif state == "intrigued":
            scores[RESPONSE_ENGAGE]   += 0.5 + intensity * 0.2
            scores[RESPONSE_QUESTION] += 0.4
            scores[RESPONSE_INFORM]   += 0.3
            scores[RESPONSE_DISMISS]  -= 0.3

        elif state == "impressed":
            scores[RESPONSE_WARM]    += 0.4 + intensity * 0.2
            scores[RESPONSE_HUMOR]   += 0.3
            scores[RESPONSE_ENGAGE]  += 0.3
            scores[RESPONSE_COLD]    -= 0.4

        else:  # calm
            scores[RESPONSE_DEFLECT] += 0.3
            scores[RESPONSE_INFORM]  += 0.2
            scores[RESPONSE_TAUNT]   += 0.1

        return scores

    # ----------------------------------------------------------
    # LAYER 2 — RELATIONSHIP MODIFIERS
    # ----------------------------------------------------------
    def _apply_relationship_scores(self, scores, relationship):
        if relationship == "rival":
            scores[RESPONSE_COLD]    += 0.3
            scores[RESPONSE_TAUNT]   += 0.2
            scores[RESPONSE_WARM]    -= 0.4
            scores[RESPONSE_HUMOR]   -= 0.1

        elif relationship == "warming":
            scores[RESPONSE_DEFLECT] += 0.1
            scores[RESPONSE_INFORM]  += 0.1

        elif relationship == "trusted":
            scores[RESPONSE_WARM]    += 0.3
            scores[RESPONSE_HUMOR]   += 0.2
            scores[RESPONSE_COLD]    -= 0.2

        elif relationship == "respected":
            scores[RESPONSE_WARM]    += 0.4
            scores[RESPONSE_HUMOR]   += 0.3
            scores[RESPONSE_ENGAGE]  += 0.2
            scores[RESPONSE_COLD]    -= 0.3
            scores[RESPONSE_DISMISS] -= 0.3

        return scores

    # ----------------------------------------------------------
    # LAYER 3 — SIGNAL TYPE MODIFIERS
    # ----------------------------------------------------------
    def _apply_signal_scores(self, scores, signal):
        itype = signal.input_type

        if itype == "question":
            scores[RESPONSE_INFORM]   += 0.4
            scores[RESPONSE_ENGAGE]   += 0.3
            scores[RESPONSE_QUESTION] += 0.2
            scores[RESPONSE_DISMISS]  -= 0.2

        elif itype == "command":
            scores[RESPONSE_INFORM]  += 0.5
            scores[RESPONSE_DEFLECT] -= 0.2

        elif itype == "emotional":
            scores[RESPONSE_WARM]    += 0.2
            scores[RESPONSE_TAUNT]   += 0.2
            scores[RESPONSE_COLD]    += 0.1

        elif itype == "nonsense":
            scores[RESPONSE_DISMISS] += 0.5
            scores[RESPONSE_HUMOR]   += 0.2
            scores[RESPONSE_TAUNT]   += 0.2
            scores[RESPONSE_INFORM]  -= 0.3

        elif itype == "greeting":
            scores[RESPONSE_WARM]    += 0.3
            scores[RESPONSE_HUMOR]   += 0.1
            scores[RESPONSE_COLD]    -= 0.1

        # complexity modifier
        if signal.complexity == "complex":
            scores[RESPONSE_ENGAGE]  += 0.2
            scores[RESPONSE_QUESTION]+= 0.1
        elif signal.complexity == "simple":
            scores[RESPONSE_DEFLECT] += 0.1
            scores[RESPONSE_TAUNT]   += 0.1

        return scores

    # ----------------------------------------------------------
    # LAYER 4 — CONTEXT PATTERN MODIFIERS
    # ----------------------------------------------------------
    def _apply_context_scores(self, scores, context_window):
        pattern = context_window.player_pattern

        if pattern == "stuck":
            scores[RESPONSE_INFORM]  += 0.4
            scores[RESPONSE_WARM]    += 0.2
            scores[RESPONSE_DISMISS] -= 0.3

        elif pattern == "testing":
            scores[RESPONSE_TAUNT]   += 0.3
            scores[RESPONSE_HUMOR]   += 0.2
            scores[RESPONSE_DISMISS] += 0.2

        elif pattern == "engaged":
            scores[RESPONSE_WARM]    += 0.2
            scores[RESPONSE_HUMOR]   += 0.2
            scores[RESPONSE_ENGAGE]  += 0.2

        elif pattern == "exploring":
            scores[RESPONSE_INFORM]  += 0.2
            scores[RESPONSE_ENGAGE]  += 0.1

        elif pattern == "emotional":
            scores[RESPONSE_WARM]    += 0.3
            scores[RESPONSE_COLD]    += 0.1

        # repeat detection
        if context_window.repeat_count >= 2:
            scores[RESPONSE_TAUNT]   += 0.3
            scores[RESPONSE_DISMISS] += 0.2

        # nudge needed
        if context_window.needs_nudge:
            scores[RESPONSE_INFORM]  += 0.3

        return scores

    # ----------------------------------------------------------
    # LAYER 5 — INTENT MODIFIERS
    # ----------------------------------------------------------
    def _apply_intent_scores(self, scores, intents):
        intent_map = {
            "compliment":         [(RESPONSE_WARM, 0.3),
                                   (RESPONSE_HUMOR, 0.2)],
            "insult":             [(RESPONSE_COLD, 0.4),
                                   (RESPONSE_TAUNT, 0.3)],
            "farewell":           [(RESPONSE_COLD, 0.2),
                                   (RESPONSE_WARM, 0.1)],
            "thanks":             [(RESPONSE_WARM, 0.2),
                                   (RESPONSE_DEFLECT, 0.1)],
            "identity_question":  [(RESPONSE_INFORM, 0.4),
                                   (RESPONSE_TAUNT, 0.1)],
            "capability_question":[(RESPONSE_INFORM, 0.5)],
            "help_request":       [(RESPONSE_INFORM, 0.5),
                                   (RESPONSE_WARM, 0.1)],
            "lore_question":      [(RESPONSE_ENGAGE, 0.4),
                                   (RESPONSE_INFORM, 0.3)],
            "social_question":    [(RESPONSE_HUMOR, 0.3),
                                   (RESPONSE_DEFLECT, 0.2)],
            "frustration":        [(RESPONSE_COLD, 0.2),
                                   (RESPONSE_INFORM, 0.3)],
            "vague_engagement":   [(RESPONSE_TAUNT, 0.3),
                                   (RESPONSE_INFORM, 0.2)],
            "trivia_request":     [(RESPONSE_INFORM, 0.5)],
            "mission_request":    [(RESPONSE_INFORM, 0.4),
                                   (RESPONSE_TAUNT, 0.1)],
            "joke_request":       [(RESPONSE_HUMOR, 0.6)],
            "fact_request":       [(RESPONSE_INFORM, 0.5)],
        }

        for intent in intents:
            if intent in intent_map:
                for response_type, bonus in intent_map[intent]:
                    scores[response_type] += bonus

        return scores

    # ----------------------------------------------------------
    # LAYER 6 — SPECIAL OVERRIDES
    # ----------------------------------------------------------
    def _apply_overrides(self, scores, signal, context_window,
                         emotional_state_obj, relationship):
        state = (emotional_state_obj.current
                 if hasattr(emotional_state_obj, 'current')
                 else emotional_state_obj)

        # maximum irritation — Nova goes cold no matter what
        cons_irr = (emotional_state_obj.consecutive_irritations
                    if hasattr(emotional_state_obj,
                               'consecutive_irritations') else 0)
        if state == "irritated" and cons_irr >= 3:
            for rt in ALL_RESPONSE_TYPES:
                scores[rt] = 0.0
            scores[RESPONSE_COLD]    = 1.0
            scores[RESPONSE_DISMISS] = 0.8

        # first turn — always warm/inform regardless of everything
        if context_window.session_turn <= 1:
            scores[RESPONSE_COLD]    = max(0.0,
                scores[RESPONSE_COLD] - 0.5)
            scores[RESPONSE_DISMISS] = max(0.0,
                scores[RESPONSE_DISMISS] - 0.5)
            scores[RESPONSE_WARM]   += 0.3

        # clamp all scores to 0-2 range
        for rt in scores:
            scores[rt] = max(0.0, min(2.0, scores[rt]))

        return scores

    # ----------------------------------------------------------
    # HELPERS
    # ----------------------------------------------------------
    def _should_ask_back(self, response_type, emotional_state_obj,
                         relationship, context_window):
        state = (emotional_state_obj.current
                 if hasattr(emotional_state_obj, 'current')
                 else emotional_state_obj)
        if state in ("irritated",):
            return False
        if response_type in (RESPONSE_ENGAGE, RESPONSE_QUESTION):
            if relationship in ("trusted","respected","warming"):
                return True
            if state == "intrigued":
                return True
        return False

    def _get_urgency(self, emotional_state_obj, context_window):
        state = (emotional_state_obj.current
                 if hasattr(emotional_state_obj, 'current')
                 else emotional_state_obj)
        if state == "irritated":
            return "high"
        if context_window.needs_nudge:
            return "high"
        if state in ("impressed","amused"):
            return "low"
        return "normal"

    def _get_content_hint(self, signal, intents, context_window):
        if "trivia_request" in intents:
            return "trivia"
        if "joke_request" in intents:
            return "joke"
        if "fact_request" in intents:
            return "fact"
        if "mission_request" in intents:
            return "mission"
        if "stats_request" in intents:
            return "stats"
        if "help_request" in intents:
            return "help"
        if context_window.needs_nudge:
            return "nudge"
        return None

    def _build_log(self, scores, signal, emotional_state_obj,
                   relationship):
        log = []
        log.append(f"signal_type: {signal.input_type}")
        log.append(f"complexity: {signal.complexity}")
        state = (emotional_state_obj.current
                 if hasattr(emotional_state_obj, 'current')
                 else emotional_state_obj)
        log.append(f"emotion: {state}")
        log.append(f"relationship: {relationship}")
        sorted_scores = sorted(
            scores.items(), key=lambda x: -x[1])[:5]
        for rt, sc in sorted_scores:
            log.append(f"  {rt}: {sc:.3f}")
        return log

# ----------------------------------------------------------
# GLOBAL INSTANCE
# ----------------------------------------------------------
reasoning_engine = ReasoningEngine()

# ----------------------------------------------------------
# CONVENIENCE
# ----------------------------------------------------------
def enable_reasoning_log():
    reasoning_engine._log_enabled = True

def disable_reasoning_log():
    reasoning_engine._log_enabled = False

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def reasoning_debug(msg="hello"):
    try:
        sig = sense(msg)
        ctx = working_memory.window
        es  = emotional_state.state
        rel = memory.relationship
        result = reasoning_engine.score(sig, ctx, es, rel)
        lines  = ["ReasoningEngine debug:"]
        for k, v in result.to_dict().items():
            lines.append(f"  {k}: {v}")
        return "\n".join(lines)
    except Exception as e:
        return f"reasoning_debug error: {e}"