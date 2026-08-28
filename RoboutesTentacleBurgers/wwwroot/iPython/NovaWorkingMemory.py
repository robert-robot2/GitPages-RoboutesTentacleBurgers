# ==========================================================
# NovaWorkingMemory.py — Nova Adeptus Working Memory Layer
# Maintains a structured context window of recent exchanges.
# Tracks topics, patterns, and player behavior over time.
# Feeds context weight to all downstream reasoning systems.
# ==========================================================

import re
from collections import defaultdict

# ----------------------------------------------------------
# CONTEXT WINDOW — structured memory of recent conversation
# ----------------------------------------------------------
class ContextWindow:
    def __init__(self):
        self.exchanges          = []      # list of (role, text, signal_type)
        self.topic_counts       = defaultdict(int)
        self.dominant_topic     = "none"
        self.repeat_count       = 0
        self.last_input         = ""
        self.questions_in_a_row = 0
        self.commands_in_a_row  = 0
        self.emotional_in_a_row = 0
        self.nonsense_in_a_row  = 0
        self.player_pattern     = "exploring"
        self.session_turn       = 0
        self.needs_nudge        = False
        self.last_nova_response = ""
        self.last_3_nova        = []
        self.topic_history      = []

    def to_dict(self):
        return {
            "dominant_topic":      self.dominant_topic,
            "repeat_count":        self.repeat_count,
            "questions_in_a_row":  self.questions_in_a_row,
            "commands_in_a_row":   self.commands_in_a_row,
            "emotional_in_a_row":  self.emotional_in_a_row,
            "nonsense_in_a_row":   self.nonsense_in_a_row,
            "player_pattern":      self.player_pattern,
            "session_turn":        self.session_turn,
            "needs_nudge":         self.needs_nudge,
            "last_3_nova":         self.last_3_nova,
            "topic_history":       self.topic_history[-5:],
        }

# ----------------------------------------------------------
# WORKING MEMORY ENGINE
# ----------------------------------------------------------
class WorkingMemoryEngine:
    def __init__(self, max_window=10):
        self.max_window  = max_window
        self.window      = ContextWindow()
        self._topic_map  = _build_topic_map()

    def update(self, signal, nova_response=""):
        """
        Call this every turn with the new SensorySignal
        and Nova's response. Returns updated ContextWindow.
        """
        w = self.window
        w.session_turn += 1

        # store exchange
        w.exchanges.append(("user", signal.cleaned, signal.input_type))
        if len(w.exchanges) > self.max_window:
            w.exchanges.pop(0)

        # track Nova's last responses for repeat detection
        if nova_response:
            w.last_nova_response = nova_response
            w.last_3_nova.append(nova_response[:80])
            if len(w.last_3_nova) > 3:
                w.last_3_nova.pop(0)

        # detect repeat input
        if signal.cleaned == w.last_input:
            w.repeat_count += 1
        else:
            w.repeat_count = 0
        w.last_input = signal.cleaned

        # track consecutive input types
        w.questions_in_a_row  = (w.questions_in_a_row  + 1
                                  if signal.input_type == "question"
                                  else 0)
        w.commands_in_a_row   = (w.commands_in_a_row   + 1
                                  if signal.input_type == "command"
                                  else 0)
        w.emotional_in_a_row  = (w.emotional_in_a_row  + 1
                                  if signal.input_type == "emotional"
                                  else 0)
        w.nonsense_in_a_row   = (w.nonsense_in_a_row   + 1
                                  if signal.input_type == "nonsense"
                                  else 0)

        # detect topics
        topic = self._detect_topic(signal.cleaned)
        if topic:
            w.topic_counts[topic] += 1
            if not w.topic_history or w.topic_history[-1] != topic:
                w.topic_history.append(topic)
            if len(w.topic_history) > 10:
                w.topic_history.pop(0)

        # dominant topic
        if w.topic_counts:
            w.dominant_topic = max(
                w.topic_counts, key=w.topic_counts.get
            )

        # player pattern classification
        w.player_pattern = self._classify_pattern(w)

        # nudge detection
        w.needs_nudge = self._needs_nudge(w)

        return w

    def get_recent_user_inputs(self, n=5):
        user_exchanges = [
            e[1] for e in self.window.exchanges
            if e[0] == "user"
        ]
        return user_exchanges[-n:]

    def nova_said_recently(self, phrase):
        """Check if Nova said something similar recently."""
        phrase_lower = phrase.lower()[:60]
        return any(
            phrase_lower[:40] in r.lower()
            for r in self.window.last_3_nova
        )

    def reset_session(self):
        self.window = ContextWindow()

    # ----------------------------------------------------------
    # PRIVATE
    # ----------------------------------------------------------
    def _detect_topic(self, cleaned):
        for topic, keywords in self._topic_map.items():
            if any(kw in cleaned for kw in keywords):
                return topic
        return None

    def _classify_pattern(self, w):
        if w.nonsense_in_a_row >= 3:
            return "testing"
        if w.questions_in_a_row >= 3:
            return "exploring"
        if w.commands_in_a_row >= 3:
            return "engaged"
        if w.emotional_in_a_row >= 2:
            return "emotional"
        if w.repeat_count >= 2:
            return "stuck"
        if w.session_turn <= 3:
            return "new"
        return "exploring"

    def _needs_nudge(self, w):
        if w.player_pattern == "stuck":
            return True
        if w.nonsense_in_a_row >= 3:
            return True
        if w.session_turn > 5 and w.commands_in_a_row == 0:
            return True
        return False

# ----------------------------------------------------------
# TOPIC MAP
# ----------------------------------------------------------
def _build_topic_map():
    return {
        "combat":    ["fight","combat","battle","enemy","attack",
                      "kill","defeat","weapon","war","blade"],
        "hacking":   ["hack","cyber","breach","code","infiltrate",
                      "crack","system","network","encrypt"],
        "stealth":   ["stealth","sneak","ghost","shadow","silent",
                      "invisible","hide","undetected"],
        "missions":  ["mission","task","accept","complete","objective",
                      "assignment","contract"],
        "identity":  ["who are you","what are you","nova","high order",
                      "your name","who made you"],
        "lore":      ["void","galaxy","space","cosmos","star","planet",
                      "universe","dark matter","wormhole"],
        "social":    ["how are you","feeling","doing","okay","alright",
                      "good","bad","fine"],
        "humor":     ["joke","funny","laugh","humor","amusing","lol",
                      "haha","lmao"],
        "trivia":    ["trivia","quiz","question","challenge","test",
                      "space quiz"],
        "stats":     ["stats","level","xp","skills","progress",
                      "rank","profile","reputation"],
        "story":     ["story","arc","chapter","lore","narrative",
                      "plot","quest","adventure"],
        "companion": ["companion","ally","partner","zyra","korrin",
                      "lyra","squad","team"],
    }

# ----------------------------------------------------------
# GLOBAL INSTANCE
# ----------------------------------------------------------
working_memory = WorkingMemoryEngine()

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def memory_debug():
    w = working_memory.window
    lines = ["WorkingMemory snapshot:"]
    for k, v in w.to_dict().items():
        lines.append(f"  {k}: {v}")
    return "\n".join(lines)