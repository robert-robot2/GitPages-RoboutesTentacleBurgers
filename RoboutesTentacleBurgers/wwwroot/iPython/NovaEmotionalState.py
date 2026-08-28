# ==========================================================
# NovaEmotionalState.py — Nova Adeptus Emotional Engine
# Nova owns her mood. The player cannot set it.
# State shifts based on conversation patterns, relationship,
# and incoming signal quality. Persists between sessions.
# ==========================================================

import random

# ----------------------------------------------------------
# EMOTIONAL STATE CONSTANTS
# ----------------------------------------------------------
CALM      = "calm"
AMUSED    = "amused"
IRRITATED = "irritated"
INTRIGUED = "intrigued"
IMPRESSED = "impressed"

# UI color map — synced to Blazor dot indicator
EMOTION_COLORS = {
    CALM:      "#4A90D9",   # blue
    AMUSED:    "#48C774",   # green
    IRRITATED: "#E53935",   # red
    INTRIGUED: "#9B59B6",   # purple
    IMPRESSED: "#F4C542",   # gold
}

# Emoji hint for stats display
EMOTION_EMOJI = {
    CALM:      "🔵",
    AMUSED:    "🟢",
    IRRITATED: "🔴",
    INTRIGUED: "🟣",
    IMPRESSED: "🟡",
}

# ----------------------------------------------------------
# EMOTIONAL STATE OBJECT
# ----------------------------------------------------------
class EmotionalStateObject:
    def __init__(self):
        self.current                = CALM
        self.previous               = CALM
        self.intensity              = 0.5
        self.shift_reason           = "initialized"
        self.consecutive_irritations = 0
        self.consecutive_positives  = 0
        self.turns_in_state         = 0
        self.total_shifts           = 0

    @property
    def color(self):
        return EMOTION_COLORS[self.current]

    @property
    def emoji(self):
        return EMOTION_EMOJI[self.current]

    def to_dict(self):
        return {
            "current":                  self.current,
            "previous":                 self.previous,
            "intensity":                round(self.intensity, 3),
            "color":                    self.color,
            "emoji":                    self.emoji,
            "shift_reason":             self.shift_reason,
            "consecutive_irritations":  self.consecutive_irritations,
            "consecutive_positives":    self.consecutive_positives,
            "turns_in_state":           self.turns_in_state,
            "total_shifts":             self.total_shifts,
        }

# ----------------------------------------------------------
# EMOTIONAL STATE ENGINE
# ----------------------------------------------------------
class EmotionalStateEngine:
    def __init__(self):
        self.state = EmotionalStateObject()

    def update(self, signal, context_window, relationship="neutral"):
        """
        Called every turn. Evaluates signal + context + relationship
        and shifts emotional state if conditions are met.
        Returns updated EmotionalStateObject.
        """
        s  = self.state
        s.turns_in_state += 1

        # -- IRRITATION TRIGGERS --
        if self._triggers_irritation(signal, context_window):
            s.consecutive_irritations += 1
            s.consecutive_positives    = 0
            if s.consecutive_irritations >= 2:
                self._shift(IRRITATED,
                            f"consecutive irritation x{s.consecutive_irritations}",
                            min(0.4 + s.consecutive_irritations * 0.15, 1.0))

        # -- POSITIVE TRIGGERS --
        elif self._triggers_positive(signal, context_window):
            s.consecutive_positives   += 1
            s.consecutive_irritations  = 0
            target = self._positive_target(signal, context_window, relationship)
            self._shift(target, "positive interaction", 
                       min(0.3 + s.consecutive_positives * 0.1, 0.9))

        # -- INTRIGUED TRIGGERS --
        elif self._triggers_intrigued(signal):
            s.consecutive_irritations = 0
            self._shift(INTRIGUED, "complex or deep input", 0.6)

        # -- NATURAL DECAY --
        else:
            self._decay(relationship)

        # -- RELATIONSHIP BASELINE --
        self._apply_relationship_baseline(relationship)

        return s

    def force_shift(self, new_state, reason="forced", intensity=0.7):
        """Used by game events — mission complete, boss defeat etc."""
        self._shift(new_state, reason, intensity)
        return self.state

    def get_current(self):
        return self.state.current

    def get_color(self):
        return self.state.color

    def get_emoji(self):
        return self.state.emoji

    def snapshot(self):
        """Returns JSON-serializable dict for persistence."""
        return self.state.to_dict()

    def load_snapshot(self, data):
        """Restore from saved dict."""
        try:
            self.state.current               = data.get("current", CALM)
            self.state.previous              = data.get("previous", CALM)
            self.state.intensity             = data.get("intensity", 0.5)
            self.state.consecutive_irritations = data.get(
                "consecutive_irritations", 0)
            self.state.consecutive_positives = data.get(
                "consecutive_positives", 0)
            self.state.shift_reason          = data.get(
                "shift_reason", "loaded")
        except Exception:
            pass

    # ----------------------------------------------------------
    # PRIVATE — TRIGGER DETECTION
    # ----------------------------------------------------------
    def _triggers_irritation(self, signal, context_window):
        # hostile input
        if signal.has_emotional:
            hostile = ["hate","stupid","idiot","dumb","useless",
                       "suck","broken","terrible","worst","shut up"]
            if any(h in signal.cleaned for h in hostile):
                return True
        # repeated nonsense
        if context_window.nonsense_in_a_row >= 2:
            return True
        # player stuck repeating same thing
        if context_window.repeat_count >= 2:
            return True
        # very short meaningless input repeatedly
        if (signal.word_count == 1 and
                signal.input_type == "nonsense" and
                context_window.nonsense_in_a_row >= 1):
            return True
        return False

    def _triggers_positive(self, signal, context_window):
        if signal.has_emotional:
            positive = ["thank","love","amazing","great","awesome",
                        "cool","nice","appreciate","wonderful",
                        "fantastic","brilliant"]
            if any(p in signal.cleaned for p in positive):
                return True
        # player is engaged — using commands, completing things
        if context_window.commands_in_a_row >= 2:
            return True
        # player asking interesting questions
        if context_window.questions_in_a_row >= 2:
            return True
        return False

    def _triggers_intrigued(self, signal):
        # complex multi-part question
        if signal.complexity == "complex" and signal.has_question:
            return True
        # deep topic words
        deep_words = ["consciousness","sentient","alive","real",
                      "think","feel","understand","origin","purpose",
                      "meaning","exist","soul","intelligence"]
        if any(d in signal.cleaned for d in deep_words):
            return True
        return False

    def _positive_target(self, signal, context_window, relationship):
        """Decide which positive state to shift to."""
        # mission/game performance → impressed
        game_words = ["correct","won","defeated","completed",
                      "success","beat","solved","hacked"]
        if any(g in signal.cleaned for g in game_words):
            return IMPRESSED
        # humor → amused
        humor_words = ["lol","haha","lmao","funny","joke","hilarious"]
        if any(h in signal.cleaned for h in humor_words):
            return AMUSED
        # trusted+ relationship + compliment → impressed
        if relationship in ("trusted","respected") and signal.has_emotional:
            return IMPRESSED
        return AMUSED

    def _decay(self, relationship):
        """Gradually return toward calm unless relationship holds state."""
        s = self.state
        # irritated decays after 4 turns
        if s.current == IRRITATED and s.turns_in_state >= 4:
            self._shift(CALM, "irritation decayed", 0.5)
        # impressed decays after 3 turns
        elif s.current == IMPRESSED and s.turns_in_state >= 3:
            self._shift(CALM, "impression faded", 0.5)
        # amused decays after 3 turns
        elif s.current == AMUSED and s.turns_in_state >= 3:
            self._shift(CALM, "amusement faded", 0.5)
        # intrigued decays after 5 turns
        elif s.current == INTRIGUED and s.turns_in_state >= 5:
            self._shift(CALM, "intrigue faded", 0.5)
        else:
            # slow intensity decay
            s.intensity = max(0.3, s.intensity - 0.05)

    def _apply_relationship_baseline(self, relationship):
        """Relationship level biases starting state on new sessions."""
        s = self.state
        if relationship == "rival" and s.current == CALM:
            # rivals start with low irritation baseline
            s.intensity = max(s.intensity, 0.55)
            s.shift_reason = "rival baseline"
        elif relationship in ("trusted","respected") and s.current == CALM:
            # trusted relationships start warmer
            s.intensity = min(s.intensity, 0.4)
            s.shift_reason = "trusted baseline"

    def _shift(self, new_state, reason, intensity):
        """Execute a state transition."""
        s = self.state
        if s.current != new_state:
            s.previous       = s.current
            s.current        = new_state
            s.turns_in_state = 0
            s.total_shifts  += 1
            s.shift_reason   = reason
            s.intensity      = intensity

# ----------------------------------------------------------
# GLOBAL INSTANCE
# ----------------------------------------------------------
emotional_state = EmotionalStateEngine()

# ----------------------------------------------------------
# CONVENIENCE FUNCTIONS
# ----------------------------------------------------------
def get_emotional_state():
    return emotional_state.state.current

def get_emotional_color():
    return emotional_state.state.color

def get_emotional_emoji():
    return emotional_state.state.emoji

def emotional_snapshot():
    import json as _j
    return _j.dumps(emotional_state.snapshot())

def load_emotional_snapshot(json_str):
    import json as _j
    try:
        data = _j.loads(json_str)
        emotional_state.load_snapshot(data)
        return f"Emotional state loaded: {emotional_state.get_current()}"
    except Exception as e:
        return f"Emotional state load failed: {e}"

# Called by game events in ChatBotAdeptus.py
def nova_on_mission_complete():
    emotional_state.force_shift(IMPRESSED, "mission completed", 0.8)

def nova_on_trivia_correct():
    emotional_state.force_shift(AMUSED, "trivia correct", 0.7)

def nova_on_boss_defeated():
    emotional_state.force_shift(IMPRESSED, "boss defeated", 0.9)

def nova_on_hostile_input():
    emotional_state.state.consecutive_irritations += 1
    if emotional_state.state.consecutive_irritations >= 2:
        emotional_state.force_shift(IRRITATED, "hostile input", 0.8)

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def emotion_debug():
    lines = ["EmotionalState snapshot:"]
    for k, v in emotional_state.snapshot().items():
        lines.append(f"  {k}: {v}")
    return "\n".join(lines)