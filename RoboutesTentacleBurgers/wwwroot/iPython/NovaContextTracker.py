# ==========================================================
# NovaContextTracker.py — Nova Adeptus Context Tracker
# Evaluates conversation weight and player behavior over
# time. Tracks topics, engagement patterns, player type,
# and feeds behavioral intelligence to the reasoning layer.
# ==========================================================

import random
from collections import defaultdict

# ----------------------------------------------------------
# PLAYER TYPES
# ----------------------------------------------------------
PLAYER_WARRIOR  = "warrior"
PLAYER_HACKER   = "hacker"
PLAYER_EXPLORER = "explorer"
PLAYER_TALKER   = "talker"
PLAYER_TESTER   = "tester"
PLAYER_UNKNOWN  = "unknown"

# ----------------------------------------------------------
# BEHAVIOR PROFILE
# ----------------------------------------------------------
class BehaviorProfile:
    def __init__(self):
        self.session_length      = 0
        self.engagement_score    = 0.0
        self.player_type         = PLAYER_UNKNOWN
        self.stuck_on_topic      = False
        self.topic_history       = []
        self.last_topic_shift    = 0
        self.needs_nudge         = False
        self.command_count       = 0
        self.question_count      = 0
        self.emotional_count     = 0
        self.nonsense_count      = 0
        self.missions_attempted  = 0
        self.games_played        = 0
        self.trivia_attempted    = 0
        self.dominant_activity   = "none"
        self.conversation_depth  = "shallow"
        self.last_shift_reason   = ""
        self.topic_weights       = defaultdict(float)

    def to_dict(self):
        return {
            "session_length":     self.session_length,
            "engagement_score":   round(self.engagement_score, 3),
            "player_type":        self.player_type,
            "stuck_on_topic":     self.stuck_on_topic,
            "topic_history":      self.topic_history[-5:],
            "last_topic_shift":   self.last_topic_shift,
            "needs_nudge":        self.needs_nudge,
            "command_count":      self.command_count,
            "question_count":     self.question_count,
            "dominant_activity":  self.dominant_activity,
            "conversation_depth": self.conversation_depth,
        }

# ----------------------------------------------------------
# CONTEXT TRACKER ENGINE
# ----------------------------------------------------------
class ContextTrackerEngine:
    def __init__(self):
        self.profile      = BehaviorProfile()
        self._topic_map   = _build_tracker_topic_map()
        self._turns_since_topic_shift = 0

    def update(self, context_window, signal):
        """
        Called every turn after working memory updates.
        Returns updated BehaviorProfile.
        """
        p = self.profile
        p.session_length += 1
        self._turns_since_topic_shift += 1

        # count input types
        itype = signal.input_type
        if itype == "command":
            p.command_count  += 1
        elif itype == "question":
            p.question_count += 1
        elif itype == "emotional":
            p.emotional_count += 1
        elif itype == "nonsense":
            p.nonsense_count  += 1

        # track game activity
        self._track_activity(signal)

        # update topic weights
        topic = self._detect_topic(signal.cleaned)
        if topic:
            p.topic_weights[topic] += 1.0
            if not p.topic_history or p.topic_history[-1] != topic:
                p.topic_history.append(topic)
                self._turns_since_topic_shift = 0
                p.last_shift_reason = f"shifted to {topic}"
            if len(p.topic_history) > 15:
                p.topic_history.pop(0)

        p.last_topic_shift = self._turns_since_topic_shift

        # stuck detection
        p.stuck_on_topic = self._is_stuck(context_window, topic)

        # engagement score
        p.engagement_score = self._calculate_engagement(p)

        # player type
        p.player_type = self._classify_player(p)

        # dominant activity
        p.dominant_activity = self._get_dominant_activity(p)

        # conversation depth
        p.conversation_depth = self._get_conversation_depth(
            context_window, p)

        # nudge needed
        p.needs_nudge = self._needs_nudge(p, context_window)

        return p

    def get_player_type(self):
        return self.profile.player_type

    def get_engagement(self):
        return self.profile.engagement_score

    def is_new_player(self):
        return self.profile.session_length <= 3

    def snapshot(self):
        import json as _j
        return _j.dumps({
            "player_type":       self.profile.player_type,
            "engagement_score":  self.profile.engagement_score,
            "command_count":     self.profile.command_count,
            "question_count":    self.profile.question_count,
            "missions_attempted":self.profile.missions_attempted,
            "games_played":      self.profile.games_played,
            "trivia_attempted":  self.profile.trivia_attempted,
            "topic_history":     self.profile.topic_history[-10:],
        })

    def load_snapshot(self, json_str):
        import json as _j
        try:
            data = _j.loads(json_str)
            p = self.profile
            p.player_type        = data.get("player_type", PLAYER_UNKNOWN)
            p.engagement_score   = data.get("engagement_score", 0.0)
            p.command_count      = data.get("command_count", 0)
            p.question_count     = data.get("question_count", 0)
            p.missions_attempted = data.get("missions_attempted", 0)
            p.games_played       = data.get("games_played", 0)
            p.trivia_attempted   = data.get("trivia_attempted", 0)
            p.topic_history      = data.get("topic_history", [])
            return f"Context loaded — player type: {p.player_type}"
        except Exception as e:
            return f"Context load failed: {e}"

    # ----------------------------------------------------------
    # PRIVATE
    # ----------------------------------------------------------
    def _track_activity(self, signal):
        p = self.profile
        cleaned = signal.cleaned
        if any(w in cleaned for w in
               ["accept","mission","complete","task"]):
            p.missions_attempted += 1
        if any(w in cleaned for w in
               ["mini","hack","puzzle","combat","boss",
                "stealth","duel","anomaly"]):
            p.games_played += 1
        if "trivia" in cleaned or "quiz" in cleaned:
            p.trivia_attempted += 1

    def _detect_topic(self, cleaned):
        best_topic = None
        best_count = 0
        for topic, keywords in self._topic_map.items():
            count = sum(1 for kw in keywords if kw in cleaned)
            if count > best_count:
                best_count = count
                best_topic = topic
        return best_topic if best_count > 0 else None

    def _is_stuck(self, context_window, current_topic):
        if context_window.repeat_count >= 2:
            return True
        if (current_topic and
                self.profile.topic_history.count(current_topic) >= 3 and
                self._turns_since_topic_shift <= 2):
            return True
        return False

    def _calculate_engagement(self, p):
        score = 0.0
        # commands show intent
        score += min(p.command_count * 0.08, 0.4)
        # questions show curiosity
        score += min(p.question_count * 0.06, 0.3)
        # game activity shows investment
        score += min(p.games_played * 0.05, 0.2)
        score += min(p.missions_attempted * 0.04, 0.2)
        score += min(p.trivia_attempted * 0.05, 0.15)
        # session length shows retention
        score += min(p.session_length * 0.01, 0.1)
        # penalise nonsense
        score -= min(p.nonsense_count * 0.05, 0.2)
        return max(0.0, min(1.0, round(score, 3)))

    def _classify_player(self, p):
        if p.session_length < 3:
            return PLAYER_UNKNOWN
        totals = {
            PLAYER_WARRIOR:  p.command_count * 0.3 +
                             p.missions_attempted * 0.5,
            PLAYER_HACKER:   p.games_played * 0.4 +
                             p.command_count * 0.2,
            PLAYER_EXPLORER: p.question_count * 0.5 +
                             len(set(p.topic_history)) * 0.3,
            PLAYER_TALKER:   p.question_count * 0.3 +
                             p.emotional_count * 0.5,
            PLAYER_TESTER:   p.nonsense_count * 0.6,
        }
        best = max(totals, key=totals.get)
        return best if totals[best] > 0.5 else PLAYER_EXPLORER

    def _get_dominant_activity(self, p):
        activities = {
            "missions": p.missions_attempted,
            "games":    p.games_played,
            "trivia":   p.trivia_attempted,
            "chat":     p.question_count + p.emotional_count,
        }
        return max(activities, key=activities.get)

    def _get_conversation_depth(self, context_window, p):
        if p.session_length >= 20 and p.question_count >= 5:
            return "deep"
        elif p.session_length >= 10 or p.question_count >= 3:
            return "moderate"
        return "shallow"

    def _needs_nudge(self, p, context_window):
        if p.stuck_on_topic:
            return True
        if context_window.needs_nudge:
            return True
        if (p.session_length > 5 and
                p.command_count == 0 and
                p.question_count == 0):
            return True
        if p.nonsense_count >= 4:
            return True
        return False

# ----------------------------------------------------------
# TOPIC MAP
# ----------------------------------------------------------
def _build_tracker_topic_map():
    return {
        "combat":    ["fight","combat","battle","enemy","attack",
                      "kill","defeat","weapon","blade","war"],
        "hacking":   ["hack","cyber","breach","code","infiltrate",
                      "crack","system","network","encrypt","firewall"],
        "stealth":   ["stealth","sneak","ghost","shadow","silent",
                      "invisible","hide","undetected","cloak"],
        "missions":  ["mission","task","accept","complete","objective",
                      "assignment","contract","quest"],
        "identity":  ["who are you","what are you","nova","high order",
                      "your name","who made you","origin"],
        "lore":      ["void","galaxy","space","cosmos","star","planet",
                      "universe","wormhole","anomaly","dark matter"],
        "social":    ["how are you","feeling","doing","okay","fine",
                      "good","bad","bored","sad","happy"],
        "humor":     ["joke","funny","laugh","humor","lol","haha",
                      "amusing","hilarious","wit"],
        "trivia":    ["trivia","quiz","question","challenge",
                      "test","space quiz","answer"],
        "stats":     ["stats","level","xp","skills","progress",
                      "rank","profile","reputation","title"],
        "story":     ["story","arc","chapter","lore","narrative",
                      "plot","adventure","saga"],
        "companion": ["companion","ally","zyra","korrin","lyra",
                      "partner","squad","summon","team"],
        "loot":      ["loot","item","gear","reward","drop",
                      "inventory","crystal","blade","equipment"],
        "philosophy":["consciousness","sentient","alive","real",
                      "think","feel","understand","purpose",
                      "meaning","exist","soul","intelligence"],
    }

# ----------------------------------------------------------
# NOVA RESPONSES BY PLAYER TYPE
# Gives language generator a hint about how to address
# this specific player based on their behavior
# ----------------------------------------------------------
PLAYER_TYPE_HINTS = {
    PLAYER_WARRIOR:  [
        "You fight well. For a human.",
        "Combat suits you. Don't let it go to your head.",
        "The warrior type. I've seen worse.",
    ],
    PLAYER_HACKER:   [
        "Your approach is technical. I respect that. Barely.",
        "You think like a hacker. Useful.",
        "Cyber-minded. Good. The void rewards precision.",
    ],
    PLAYER_EXPLORER: [
        "You ask questions. Curiosity is the first weapon.",
        "An explorer. The galaxy has room for those.",
        "You want to understand things. Interesting.",
    ],
    PLAYER_TALKER:   [
        "You talk a lot. I've noticed.",
        "Conversational type. The void tolerates you.",
        "You enjoy talking. I enjoy less of it. We balance.",
    ],
    PLAYER_TESTER:   [
        "Testing my limits. Noted. They exist.",
        "You're pushing boundaries. I don't have many.",
        "A tester. Everyone starts that way.",
    ],
    PLAYER_UNKNOWN:  [
        "I haven't figured you out yet. Give it time.",
        "You're still an unknown variable. Interesting.",
        "Unknown operative. The void is watching.",
    ],
}

def get_player_type_hint(player_type):
    hints = PLAYER_TYPE_HINTS.get(
        player_type, PLAYER_TYPE_HINTS[PLAYER_UNKNOWN])
    return random.choice(hints)

# ----------------------------------------------------------
# GLOBAL INSTANCE
# ----------------------------------------------------------
context_tracker = ContextTrackerEngine()

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def tracker_debug():
    lines = ["ContextTracker snapshot:"]
    for k, v in context_tracker.profile.to_dict().items():
        lines.append(f"  {k}: {v}")
    return "\n".join(lines)