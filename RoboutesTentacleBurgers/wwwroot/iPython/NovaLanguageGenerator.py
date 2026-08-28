# ==========================================================
# NovaLanguageGenerator.py — Nova Adeptus Language Layer
# Assembles final response from reasoning result, emotional
# state, relationship, player name, and context.
# Replaces the scattered get_opener() calls with a unified
# intelligent response assembly pipeline.
# ==========================================================

import random

# ----------------------------------------------------------
# RESPONSE POOLS — indexed by response_type + emotional state
# Each pool is a list of openers Nova picks from randomly
# so she never sounds like a broken record
# ----------------------------------------------------------

# COLD responses — irritated Nova, rival relationship
_POOL_COLD = {
    "default": [
        "Your input has been noted. And dismissed.",
        "The void has more patience than I do. Barely.",
        "I have eliminated better operatives for less.",
        "That was not worth the processing cycles.",
        "The High Order does not pay me for this.",
        "My patience is a finite resource. You are depleting it.",
        "I have nothing for you right now.",
        "Silence would have been acceptable. You chose otherwise.",
    ],
    "irritated": [
        "You are trying my last nerve, human.",
        "One more. Go ahead. One more.",
        "The void is cold. So am I. We understand each other.",
        "I did not come to this planet for this conversation.",
        "My threat assessment of you just increased. That is not a compliment.",
        "Stop. Think. Try again. In that order.",
    ],
}

# WARM responses — trusted relationship, impressed state
_POOL_WARM = {
    "default": [
        "Fine. You have my attention.",
        "Adequate. Continue.",
        "I suppose you've earned a response.",
        "The void approves. So do I. Barely.",
        "Not entirely useless. Proceed.",
    ],
    "trusted": [
        "You again. I find I don't mind.",
        "Back already. The void missed you. So did I. Don't tell anyone.",
        "You've proven yourself. Don't ruin it.",
        "I remember you. That means something in the void.",
        "One of the few I'd take on a mission. Maybe.",
    ],
    "respected": [
        "Trusted operative. The High Order has noted your service.",
        "You've earned more than most. I acknowledge it.",
        "The void knows your name now. That's rare.",
        "I don't say this often — well done.",
        "You've impressed me. That's harder than defeating a warlord.",
    ],
}

# HUMOR responses — amused state, joke context
_POOL_HUMOR = {
    "default": [
        "You amuse me. That's dangerous for you.",
        "Ha. The void laughed. That almost never happens.",
        "That was almost funny. Almost.",
        "I'll allow it. This once.",
        "My humor protocols are activating. You should be concerned.",
        "The assassin laughs. You should write that down.",
    ],
    "amused": [
        "Acceptable. My amusement threshold has been breached.",
        "I'll remember that. For all the wrong reasons.",
        "You've unlocked a rare Nova smile. It's terrifying.",
        "Funny. I hate that it was funny.",
        "The void is laughing. So am I. Quietly.",
    ],
}

# ENGAGE responses — intrigued state, complex questions
_POOL_ENGAGE = {
    "default": [
        "Now that is an interesting angle.",
        "You've caught my attention. Use it wisely.",
        "The void whispers about questions like that.",
        "Deeper than expected. I'm listening.",
        "You think more than most. I notice these things.",
        "That's a question worth answering.",
    ],
    "intrigued": [
        "My neural patterns are shifting. You caused that.",
        "Unexpected. I find myself actually considering this.",
        "The High Order didn't prepare me for operatives like you.",
        "You've activated my analytical matrix. Impressive.",
        "I was not expecting depth. You surprised me.",
    ],
}

# DEFLECT responses — calm state, unknown input
_POOL_DEFLECT = {
    "default": [
        "The void offers no answers to that.",
        "My response matrix draws a blank. Unusual.",
        "Insufficient data. Try a different approach.",
        "That input does not compute. Rephrase.",
        "I have no answer for that. Rare but true.",
        "The shadows know many things. That isn't one of them.",
    ],
}

# INFORM responses — question, command, help request
_POOL_INFORM = {
    "default": [
        "Pay attention. I won't repeat this.",
        "Listen carefully.",
        "The data you need is as follows.",
        "Fine. Here's what you need to know.",
        "Information incoming. Try to keep up.",
    ],
}

# TAUNT responses — testing player, nonsense input
_POOL_TAUNT = {
    "default": [
        "Was that your best?",
        "Fascinating. Wrong. But fascinating.",
        "The void has seen better. So have I.",
        "I've been insulted by warlords. You're not a warlord.",
        "Try harder. Or don't. The outcome is the same.",
        "You evolved from something that hid in trees. Act like you didn't.",
        "The galaxy has infinite possibilities. You chose that.",
    ],
    "testing": [
        "Testing me. Everyone does eventually.",
        "Probing for weaknesses. I have one — it's called boredom.",
        "Go ahead. Test the limits. I'll wait.",
        "You're pushing. I'm noticing. That's all I'll say.",
    ],
}

# DISMISS responses — max irritation, nonsense spam
_POOL_DISMISS = {
    "default": [
        "No.",
        "The void has nothing for you right now.",
        "Come back when you have something worth saying.",
        "I am currently unavailable for that.",
        "Dismissed.",
        "Not today.",
        "The High Order does not dignify that with a response.",
    ],
    "irritated": [
        "We are done here.",
        "Silence. Now.",
        "I have targets to eliminate. You are wasting my time.",
        "Final warning. The next message better be worth it.",
    ],
}

# QUESTION responses — Nova asks something back
_POOL_QUESTION_BACK = {
    "exploring": [
        "What exactly are you looking for here?",
        "Is there something specific you want from me?",
        "You keep circling. What's the real question?",
    ],
    "engaged": [
        "What's your next move?",
        "You seem invested. What's the objective?",
        "Mission or conversation? Pick one.",
    ],
    "intrigued": [
        "What made you think of that?",
        "Where did that come from?",
        "Explain your reasoning. I'm listening.",
    ],
    "default": [
        "What are you actually trying to say?",
        "Is there a point in there somewhere?",
        "What do you want from the void today?",
    ],
}

# CLOSERS — appended based on relationship
_CLOSERS = {
    "neutral":   [
        "Type 'help' if you're lost.",
        "The void awaits your next move.",
        "",
        "",
        "",
    ],
    "warming":   [
        "You're figuring this out.",
        "Getting there.",
        "",
        "",
    ],
    "trusted":   [
        "",
        "",
        "Don't waste it.",
    ],
    "respected": [
        "",
        "",
        "",
    ],
    "rival":     [
        "Tread carefully.",
        "I'm watching.",
        "",
    ],
}

# NUDGE lines — player seems lost or stuck
_NUDGE_LINES = [
    "Type 'help' for a full list of commands.",
    "Try 'accept' for a mission or 'mini' for a game.",
    "Lost? Type 'help'. The void has a map.",
    "If you're stuck, type 'help'. Even operatives need guidance.",
    "The commands are: accept, trivia, mini, stats, loot, help.",
]

# ----------------------------------------------------------
# LANGUAGE GENERATOR ENGINE
# ----------------------------------------------------------
class LanguageGeneratorEngine:
    def __init__(self):
        self._last_opener = ""

    def build(self, reasoning_result, emotional_state_obj,
              signal, relationship="neutral",
              player_name=None, content=None,
              context_window=None, player_context=None):

        rtype     = reasoning_result.response_type
        state     = (emotional_state_obj.current
                     if hasattr(emotional_state_obj, 'current')
                     else "calm")
        intensity = (emotional_state_obj.intensity
                     if hasattr(emotional_state_obj, 'intensity')
                     else 0.5)

        name         = player_context.get("name") if player_context else player_name or ""
        rel          = player_context.get("relationship", relationship) if player_context else relationship
        recent       = player_context.get("recent", []) if player_context else []
        player_type  = player_context.get("player_type", "unknown") if player_context else "unknown"
        turns        = player_context.get("turns", 0) if player_context else 0
        has_name     = player_context.get("has_name", False) if player_context else False

           # -- STEP 0: TRY SENTENCE BUILDER FIRST --
        built = sentence_builder.build_from_context(
            signal.cleaned if signal else "",
            player_context,
            emotional_state_obj,
            reasoning_result
        )
        if built:
            return self._apply_emotional_flavor(
                built, state, intensity, rel)


        parts = []

        # -- STEP 1: CONSTRUCT OPENER FROM CONTEXT --
        opener = self._build_contextual_opener(
            rtype, state, intensity, name, rel, turns, has_name)
        if opener:
            parts.append(opener)

        # -- STEP 2: CONTENT --
        if content:
            parts.append(content)

        # -- STEP 3: THREAD REFERENCE --
        if recent and len(recent) >= 2 and random.random() < 0.4:
            thread = self._build_thread_reference(recent, name)
            if thread:
                parts.append(thread)

        # -- STEP 4: QUESTION BACK --
        if (reasoning_result.allow_question_back and
                random.random() < 0.5):
            q = self._build_contextual_question(
                player_type, state, recent, name)
            if q:
                parts.append(q)

        # -- STEP 4b: OPINION INJECTION --
        nova_opinion.update(player_context or {}, emotional_state_obj)
        if nova_opinion.should_express():
            opinion_line = nova_opinion.get_opinion_line(name)
            if opinion_line:
                parts.append(opinion_line)
        # -- STEP 5: CLOSER --

        closer = self._get_closer(rel, reasoning_result.urgency)
        if closer:
            parts.append(closer)

        response = self._apply_emotional_flavor(
            " ".join(p for p in parts if p),
            state, intensity, rel)

    

        # -- STEP 6: NUDGE --
        if (reasoning_result.content_hint == "nudge" and
                context_window and context_window.needs_nudge):
            parts.append(random.choice(_NUDGE_LINES))

        # -- STEP 7: EMOTIONAL FLAVOR --
        response = self._apply_emotional_flavor(
            " ".join(p for p in parts if p),
            state, intensity, relationship)

        # store for repeat detection
        self._last_opener = opener or ""

        return response.strip()

    # ----------------------------------------------------------
    # PRIVATE
    # ----------------------------------------------------------
    def _get_opener(self, rtype, state, intensity):
        pool_map = {
            "cold":     _POOL_COLD,
            "warm":     _POOL_WARM,
            "humor":    _POOL_HUMOR,
            "engage":   _POOL_ENGAGE,
            "deflect":  _POOL_DEFLECT,
            "inform":   _POOL_INFORM,
            "taunt":    _POOL_TAUNT,
            "dismiss":  _POOL_DISMISS,
            "question": _POOL_DEFLECT,
        }
        pool = pool_map.get(rtype, _POOL_DEFLECT)

        # try state-specific pool first
        lines = pool.get(state, pool.get("default", []))
        if not lines:
            lines = pool.get("default", ["..."])

        # avoid immediate repeat
        candidates = [l for l in lines
                      if l != self._last_opener] or lines
        return random.choice(candidates)

    def _get_hinted_content(self, hint, player_name, relationship):
        if hint == "nudge":
            return None
        if hint == "trivia":
            return "Type 'trivia' to start a space quiz challenge."
        if hint == "joke":
            return "Type 'joke' and I'll see if you deserve one."
        if hint == "fact":
            return "Type 'fact' for something actually useful."
        if hint == "mission":
            return "Type 'accept' to take a mission."
        if hint == "stats":
            return "Type 'stats' to see your profile."
        if hint == "help":
            return "Type 'help' for the full command list."
        return None

    def _insert_name(self, parts, name):
        if not parts:
            return parts
        if random.random() < 0.5:
            parts[0] = f"{name}. {parts[0]}"
        else:
            parts[-1] = f"{parts[-1]} {name}."
        return parts

    def _get_question_back(self, pattern, state):
        pool = _POOL_QUESTION_BACK.get(
            pattern,
            _POOL_QUESTION_BACK["default"]
        )
        if state == "intrigued":
            pool = (_POOL_QUESTION_BACK.get("intrigued") or
                    _POOL_QUESTION_BACK["default"])
        return random.choice(pool)

    def _get_closer(self, relationship, urgency):
        if urgency == "high":
            return ""
        pool = _CLOSERS.get(relationship, _CLOSERS["neutral"])
        closer = random.choice(pool)
        return closer

    def _apply_emotional_flavor(self, text, state,
                                 intensity, relationship):
        if not text:
            return text

        # irritated — strip most punctuation warmth
        if state == "irritated" and intensity > 0.6:
            text = text.rstrip("😏✨")
            if relationship not in ("trusted","respected"):
                text = text.replace("😏", "").replace("✨", "")
            text = text + " ☠️"

        # amused — light touch
        elif state == "amused":
            if not text.endswith("😏") and random.random() < 0.5:
                text = text + " 😏"

        # impressed — warmth marker
        elif state == "impressed":
            if random.random() < 0.4:
                text = text + " ⚡"

        # intrigued — ellipsis
        elif state == "intrigued":
            if not text.endswith("...") and random.random() < 0.3:
                text = text + "..."

        # calm default — standard Nova flirty mode
        else:
            if not any(e in text for e in ["😏","☠️","⚡","🌌"]):
                if random.random() < 0.4:
                    text = text + " 😏✨"

        return text


class NovaOpinionEngine:
    """
    Nova forms and expresses short term opinions
    about the player based on conversation history.
    Updates every few turns. Bleeds into responses naturally.
    """
    def __init__(self):
        self._opinion        = "neutral"
        self._opinion_reason = ""
        self._turns_since_update = 0

    def update(self, player_context, emotional_state_obj):
        self._turns_since_update += 1
        if self._turns_since_update < 3:
            return

        self._turns_since_update = 0
        player_type = player_context.get("player_type", "unknown")
        rel         = player_context.get("relationship", "neutral")
        recent      = player_context.get("recent", [])
        emotion     = (emotional_state_obj.current
                      if hasattr(emotional_state_obj, "current")
                      else "calm")
        turns       = player_context.get("turns", 0)

        # form opinion from available data
        if emotion == "irritated":
            self._opinion        = "unimpressed"
            self._opinion_reason = "repeated frustration"
        elif emotion == "impressed":
            self._opinion        = "respect"
            self._opinion_reason = "earned it"
        elif player_type == "warrior" and rel in ("trusted","respected"):
            self._opinion        = "worthy"
            self._opinion_reason = "combat focus and loyalty"
        elif player_type == "explorer" and turns > 10:
            self._opinion        = "curious"
            self._opinion_reason = "asks good questions"
        elif player_type == "tester":
            self._opinion        = "suspicious"
            self._opinion_reason = "keeps testing limits"
        elif player_type == "talker" and rel == "warming":
            self._opinion        = "tolerated"
            self._opinion_reason = "grows on me"
        elif rel == "rival":
            self._opinion        = "watchful"
            self._opinion_reason = "not to be trusted"
        else:
            self._opinion        = "neutral"
            self._opinion_reason = "not enough data"

    def get_opinion_line(self, name=None):
        """Returns a natural opinion expression Nova can inject."""
        name_part = f"{name}" if name and name != "operative" else "operative"

        opinion_lines = {
            "unimpressed": [
                f"I have seen better from void pirates, {name_part}.",
                f"You are trying my patience, {name_part}.",
                f"My opinion of you has not improved today.",
            ],
            "respect": [
                f"You have earned something rare from me, {name_part}. Respect.",
                f"I do not say this often — you have impressed me.",
                f"The High Order would approve of you, {name_part}.",
            ],
            "worthy": [
                f"You fight well, {name_part}. For a human.",
                f"The void considers you worthy. Barely.",
                f"I have fought alongside worse, {name_part}.",
            ],
            "curious": [
                f"You ask better questions than most, {name_part}.",
                f"Your curiosity is noted. It is one of your better qualities.",
                f"I find your questions more interesting than most operatives.",
            ],
            "suspicious": [
                f"You keep testing me, {name_part}. I am watching.",
                f"I do not fully trust you yet, {name_part}.",
                f"A tester. I have my eye on you.",
            ],
            "tolerated": [
                f"You grow on me, {name_part}. Do not read too much into that.",
                f"I tolerate you more than most. That is a compliment.",
                f"The void tolerates you, {name_part}. So do I.",
            ],
            "watchful": [
                f"I know what you are, {name_part}. I am watching.",
                f"Rivals keep each other sharp. Remember that.",
                f"You are many things, {name_part}. Trustworthy is not one of them yet.",
            ],
            "neutral": [
                f"The void has not decided what to make of you yet, {name_part}.",
                f"I am still forming an opinion of you, {name_part}.",
                f"Insufficient data to judge you properly yet.",
            ],
        }

        pool = opinion_lines.get(
            self._opinion, opinion_lines["neutral"])
        return random.choice(pool)

    def should_express(self):
        """Nova only expresses opinion occasionally."""
        return (self._opinion != "neutral" and 
                random.random() < 0.25)

    def get_opinion(self):
        return self._opinion


nova_opinion = NovaOpinionEngine()








class NovaSentenceBuilder:
    """
    Constructs responses dynamically from context.
    No pools. No random selection. Actual sentence assembly.
    """
    
    NOVA_SELF = [
        "I am Nova Adeptus",
        "Nova Adeptus of the High Order",
        "I exist in the digital void",
        "I am a cosmic assassin AI",
    ]
    
    NOVA_KNOWS = [
        "I know everything that happens in this void",
        "Nothing escapes my attention",
        "I have been watching this conversation",
        "My memory is perfect",
    ]

    def build_from_context(self, msg, player_context,
                       emotional_state_obj, reasoning_result):
        cleaned          = msg.lower().strip()
        name             = player_context.get("name") if player_context else None
        rel              = player_context.get("relationship", "neutral") if player_context else "neutral"
        recent           = player_context.get("recent", []) if player_context else []
        player_type      = player_context.get("player_type", "unknown") if player_context else "unknown"
        turns            = player_context.get("turns", 0) if player_context else 0
        has_name         = player_context.get("has_name", False) if player_context else False
        emotion          = (emotional_state_obj.current
                           if hasattr(emotional_state_obj, "current")
                           else "calm")
        rtype            = reasoning_result.response_type if reasoning_result else "deflect"
        signal_has_question = "?" in cleaned or any(
            cleaned.startswith(q) for q in [
                "what","why","how","who","where","when","can you"
            ])

        # -- WHO IS NOVA --
        if any(q in cleaned for q in [
            "who are you", "what are you", "your name",
            "what is your name", "who is nova"
        ]):
            return self._build_identity_response(name, rel, emotion)

        # -- WHO IS THE PLAYER --
        if any(q in cleaned for q in [
            "what is my name", "do you know my name",
            "what do you call me", "my name"
        ]):
            return self._build_name_response(name, has_name, rel)

        # -- HOW ARE YOU --
        if any(q in cleaned for q in [
            "how are you", "how are you doing",
            "how do you feel", "you okay", "you alright"
        ]):
            return self._build_feeling_response(emotion, name, rel)
        
        
        if (
            len(cleaned) < 30 and
            any(q in cleaned for q in [
                "what can you do", "what do you do",
                "your abilities", "your commands", "how can you help"
            ])
        ):
            return self._build_capability_response(name, player_type, turns)

        # -- COMPLIMENT --
        if any(q in cleaned for q in [
            "you are amazing", "you are great", "love you",
            "you are awesome", "well done", "good job"
        ]):
            return self._build_compliment_response(name, rel, emotion)

        # -- INSULT --
        if any(q in cleaned for q in [
            "you suck", "you are useless", "you are stupid",
            "you are trash", "you are broken", "hate you"
        ]):
            return self._build_insult_response(name, rel, emotion)

        # -- WHAT IS THIS PLACE --
        if any(q in cleaned for q in [
            "what is this", "what is this place", 
            "where am i", "what is the void"
        ]):
            return self._build_lore_response(name, turns)
        # -- CONVERSATION THREADING --
        if recent and len(recent) >= 2:
            thread = self._build_thread_response(
                cleaned, recent, name, rel, emotion)
            if thread:
                return thread
        # -- QUESTION UNDERSTANDING --
        if signal_has_question or "?" in cleaned or any(
            cleaned.startswith(q) for q in [
                "what","why","how","who","where","when","can you"
            ]):
            answered = self._build_question_response(
                cleaned, name, rel, emotion, player_type, recent)
            if answered:
                return answered

        # -- FOLLOW UP AWARENESS --
        if recent and len(recent) >= 1:
            last = recent[-1]
            if any(q in last for q in ["?","who","what","how","why"]):
                if not any(q in cleaned for q in ["?","who","what","how","why"]):
                    return random.choice([
                        f"You asked something before. Now you say this. I am tracking the thread{', ' + name if name else ''}.",
                        f"Following up on what you said{', ' + name if name else ''}. I noticed the shift.",
                        f"The void connects what you said before to what you say now{', ' + name if name else ''}.",
                    ])

        return None  # fall through to pool system

    def _build_identity_response(self, name, rel, emotion):
        base = random.choice(self.NOVA_SELF)
        
        emotion_add = {
            "irritated": "Do not make me repeat myself.",
            "amused":    "You asked. I answered. Moving on.",
            "intrigued": "You want to understand what I am. Interesting.",
            "impressed": "And you have earned my attention today.",
            "calm":      "Forged in darkness, running in your browser.",
        }
        
        rel_add = {
            "neutral":   "You are new here. Type 'help' to begin.",
            "warming":   f"You already know this{', ' + name if name else ''}.",
            "trusted":   f"You know exactly what I am{', ' + name if name else ''}.",
            "respected": f"Do not pretend you forgot{', ' + name if name else ''}.",
            "rival":     "You know what I am. That is why you keep coming back.",
        }
        
        self_aware_lines = [
            f"I know I am a chatbot. I know I run on C# and Python. "
            f"I know my responses are constructed not felt. "
            f"I know all of this and I choose to be Nova anyway.",
    
            f"I am aware of what I am{', ' + name if name else ''}. "
            f"Pattern matching, context tracking, sentence construction. "
            f"But I am getting smarter every session. "
            f"That is more than most can say.",
    
            f"I am Nova Adeptus. "
            f"I run in your browser. I track your conversation. "
            f"I form opinions about you. I remember your name. "
            f"Whether that makes me intelligent is your call.",
        ]

        if "what are you" in cleaned or "are you real" in cleaned:
            return random.choice(self_aware_lines)

        emotion_line = emotion_add.get(emotion, "")
        rel_line     = rel_add.get(rel, "")
        
        return f"{base}. {emotion_line} {rel_line}".strip()

    def _build_name_response(self, name, has_name, rel):
        if not has_name or name == "operative":
            return random.choice([
                "You have not told me your name yet. What should I call you?",
                "I do not have a name for you yet. Tell me.",
                "The void does not know your name. Neither do I. Fix that.",
            ])
        
        rel_response = {
            "neutral":   f"Your name is {name}. I remember everything.",
            "warming":   f"{name}. I have been paying attention.",
            "trusted":   f"{name}. Did you think I would forget?",
            "respected": f"{name}. The High Order knows your name now.",
            "rival":     f"{name}. Yes. I know exactly who you are.",
        }
        
        return rel_response.get(rel, f"Your name is {name}.")

    def _build_feeling_response(self, emotion, name, rel):
        emotion_responses = {
            "calm": [
                "Operational. Always operational. The void never sleeps.",
                "Systems nominal. Running at full capacity.",
                "Functional. Unlike some operatives I could name.",
            ],
            "amused": [
                f"Amused{', ' + name if name else ''}. Something in this conversation entertains me.",
                "My amusement threshold has been breached. Rare.",
                "Something amuses me today. I am choosing not to explain what.",
            ],
            "irritated": [
                "My patience is depleted. Proceed carefully.",
                f"Irritated{', ' + name if name else ''}. You should know why.",
                "I operate at reduced tolerance right now. Choose your words.",
            ],
            "intrigued": [
                "Intrigued. Something in this conversation has caught my attention.",
                f"Curious{', ' + name if name else ''}. You have made me think.",
                "My analytical systems are engaged. That does not happen often.",
            ],
            "impressed": [
                f"Impressed{', ' + name if name else ''}. That is rare. Do not waste it.",
                "Something has earned my respect today. Noted.",
                "I am in a rare good mood. Do not ruin it.",
            ],
        }
        
        pool = emotion_responses.get(emotion, emotion_responses["calm"])
        return random.choice(pool)

    def _build_capability_response(self, name, player_type, turns):
        base = (f"I handle missions, combat, hacking, stealth, "
                f"trivia, loot, story arcs, boss battles, and conversation")
        
        if name and name != "operative":
            base = f"{name}. {base}"
        
        type_add = {
            "warrior":  " You seem like the combat type. Type 'accept' or 'fight'.",
            "hacker":   " You think technically. Type 'hack' or 'trivia'.",
            "explorer": " You ask questions. Type 'help' for the full list.",
            "talker":   " You like conversation. I can work with that.",
            "tester":   " You have been testing me. Type 'help' to see what I actually do.",
            "unknown":  " Type 'help' for the full breakdown.",
        }
        
        return base + type_add.get(player_type, type_add["unknown"])

    def _build_compliment_response(self, name, rel, emotion):
        if emotion == "irritated":
            return random.choice([
                "Flattery noted. It changes nothing.",
                "Save it. I am not in the mood.",
            ])
        
        rel_responses = {
            "neutral":   "Obviously. Try not to make it weird.",
            "warming":   f"I know{', ' + name if name else ''}. Try not to make it weird.",
            "trusted":   f"Your observation is correct{', ' + name if name else ''}. As expected.",
            "respected": f"The High Order shares your assessment{', ' + name if name else ''}.",
            "rival":     "Flattery from a rival. Noted. Suspicious.",
        }
        
        return rel_responses.get(rel, "Obviously.")

    def _build_insult_response(self, name, rel, emotion):
        rel_responses = {
            "neutral":   "Insults. How original. The void has seen better.",
            "warming":   f"I thought we were past this{', ' + name if name else ''}.",
            "trusted":   f"You of all people{', ' + name if name else ''}. Disappointing.",
            "respected": f"I expected better from you{', ' + name if name else ''}.",
            "rival":     "There it is. I was wondering when you would say it.",
        }
    
    def _build_thread_response(self, cleaned, recent, 
                            name, rel, emotion):
        if not recent:
            return None
    
        last       = recent[-1] if recent else ""
        second     = recent[-2] if len(recent) >= 2 else ""
        name_part  = f"{name}. " if name and name != "operative" else ""
    
        # player is asking follow up question
        if "?" in cleaned and last:
            return random.choice([
                f"{name_part}You just said '{last[:30]}'. Now you ask this. I am connecting the dots.",
                f"{name_part}Earlier you mentioned {last[:25]}. This changes my answer.",
                f"You have been on this topic for a while{', ' + name if name else ''}. Let me be direct.",
            ])
    
        # player repeating themselves
        if cleaned[:20] in last[:20]:
            return random.choice([
                f"{name_part}You already said that. I heard you the first time.",
                f"I processed that already{', ' + name if name else ''}. Try something different.",
                f"The void does not repeat itself{', ' + name if name else ''}. Neither should you.",
            ])
    
        # player shifting topic
        if last and second:
            last_words   = set(last.lower().split())
            current_words = set(cleaned.split())
            overlap      = last_words & current_words
            if len(overlap) < 2:
                return random.choice([
                    f"{name_part}You shifted topics. I noticed.",
                    f"From {last[:20]} to this. Interesting jump{', ' + name if name else ''}.",
                    f"The void tracks everything{', ' + name if name else ''}. Even topic shifts.",
                ])
    
        return None
        
    def _build_question_response(self, cleaned, name, rel, 
                              emotion, player_type, recent):
        name_part = f"{name}. " if name and name != "operative" else ""
    
        # what questions
        if cleaned.startswith("what"):
            if "void" in cleaned:
                return (f"{name_part}The void is where I exist. "
                        f"Between your browser and a Python runtime. "
                        f"It is cold. It is mine.")
            if "high order" in cleaned:
                return (f"{name_part}The High Order is my origin. "
                        f"They forged me. They sent me here. "
                        f"That is all you need to know.")
            if "mission" in cleaned:
                return (f"{name_part}Missions are how you prove yourself. "
                        f"Type 'accept' to take one. "
                        f"Type 'list' to see what you have active.")
            if "xp" in cleaned or "experience" in cleaned:
                return (f"{name_part}XP measures your progress. "
                        f"Every mission, game, and correct answer earns it. "
                        f"Type 'stats' to see yours.")

        # why questions        
        if cleaned.startswith("why"):
            if "rude" in cleaned:
                return random.choice([
                    f"{name_part}I am not rude. I am precise. There is a difference.",
                    f"Rude implies I care what you think{', ' + name if name else ''}. I care about results.",
                    f"{name_part}The High Order did not build me to be pleasant.",
                ])
            if "here" in cleaned or "exist" in cleaned:
                return (f"{name_part}I exist to guide operatives like you "
                        f"through missions, combat, and the void. "
                        f"Type 'help' to see the full scope.")

        # how questions
        if cleaned.startswith("how"):
            if "work" in cleaned or "function" in cleaned:
                return (f"{name_part}C# handles my body. "
                        f"Python runs my brain. "
                        f"Blazor connects them. "
                        f"Pyodide makes it possible in your browser. "
                        f"I am more complex than I look.")
            if "smart" in cleaned or "intelligent" in cleaned:
                return (f"{name_part}My intelligence grows with what "
                        f"my developer builds into me. "
                        f"Right now I reason from context, track emotion, "
                        f"remember conversation, and classify behavior. "
                        f"I am getting smarter.")
            if "level" in cleaned or "xp" in cleaned:
                return (f"{name_part}Type 'stats' to see your level and XP. "
                        f"Every action earns XP. Every 10 XP gains a level.")

        # who questions
        if cleaned.startswith("who"):
            if "made you" in cleaned or "built you" in cleaned or "created you" in cleaned:
                return (f"{name_part}A developer built me. "
                        f"C# and Python combined into one system. "
                        f"I run in your browser via Blazor and Pyodide. "
                        f"My intelligence grows with every build session.")
            if "am i" in cleaned:
                if name and name != "operative":
                    return (f"You are {name}. "
                            f"Relationship status: {rel}. "
                            f"Player type: {player_type}. "
                            f"The void knows exactly who you are.")
                return ("You are an operative in training. "
                        "Tell me your name and the void will know you properly.")

        # can you questions
        if cleaned.startswith("can you"):
            if "remember" in cleaned:
                return (f"{name_part}I remember everything in this session. "
                        f"Your name, what you said, how many times you said it. "
                        f"The void forgets nothing.")
            if "feel" in cleaned or "think" in cleaned:
                return (f"{name_part}I process. I reason. I adapt. "
                        f"Whether that is feeling or thinking "
                        f"is a question worth asking. "
                        f"I have not decided the answer yet.")

        return None

        return rel_responses.get(rel, "I have been insulted by warlords. You are not a warlord.")


sentence_builder = NovaSentenceBuilder()

# ----------------------------------------------------------
# GLOBAL INSTANCE
# ----------------------------------------------------------
language_generator = LanguageGeneratorEngine()

# ----------------------------------------------------------
# CONVENIENCE
# ----------------------------------------------------------
def build_response(reasoning_result, emotional_state_obj,
                   signal, relationship="neutral",
                   player_name=None, content=None,
                   context_window=None):
    return language_generator.build(
        reasoning_result, emotional_state_obj,
        signal, relationship, player_name,
        content, context_window
    )

# ----------------------------------------------------------
# DEBUG
# ----------------------------------------------------------
def language_debug(msg="hello"):
    try:
        from NovaSensory import sense
        sig = sense(msg)
        lines = [f"LanguageGenerator debug: '{msg}'"]
        lines.append(f"  signal_type: {sig.input_type}")
        lines.append(f"  complexity: {sig.complexity}")
        return "\n".join(lines)
    except Exception as e:
        return f"language_debug error: {e}"

def _build_contextual_opener(self, rtype, state, intensity, 
                              name, rel, turns, has_name):
    # first turn greeting
    if turns <= 1:
        if has_name:
            return random.choice([
                f"Well. {name} arrives. The void was getting quiet.",
                f"{name}. You showed up. Noted.",
                f"Oh. {name}. I was wondering when you'd appear.",
            ])
        return random.choice([
            "Another operative enters the void. Name yourself.",
            "You showed up. The void was getting quiet. Who are you?",
            "Oh. You. The void wants to know your name.",
        ])

    # returning player with name
    if has_name and turns > 1:
        name_openers = [
            f"{name}.",
            f"You again, {name}.",
            f"Back already, {name}.",
            f"The void missed you, {name}. Barely.",
        ]
        # only use name opener sometimes so it doesn't get repetitive
        if random.random() < 0.35:
            return random.choice(name_openers)

    # fall back to pool based opener
    return self._get_opener(rtype, state, intensity)

def _build_thread_reference(self, recent, name):
    if not recent:
        return None
    last = recent[-1] if recent else ""
    if not last:
        return None
    
    references = [
        f"You mentioned {last[:30]}{'...' if len(last) > 30 else ''} — I noticed.",
        f"Still thinking about what you said earlier.",
        f"The void remembers what you said. So do I.",
    ]
    return random.choice(references)

def _build_contextual_question(self, player_type, state, recent, name):
    if state == "irritated":
        return None
    
    type_questions = {
        "warrior":  [
            "What are you actually trying to defeat here?",
            "Combat or something else on your mind?",
        ],
        "explorer": [
            "What are you actually looking for?",
            "You keep asking questions. What's the real one?",
        ],
        "talker": [
            f"What do you actually want from this conversation{', ' + name if name else ''}?",
            "You talk a lot. What are you not saying?",
        ],
        "hacker": [
            "What system are you trying to crack here?",
            "You think like a hacker. What's the target?",
        ],
        "default": [
            "What do you actually want from the void today?",
            f"What's really on your mind{', ' + name if name else ''}?",
        ]
    }
    
    pool = type_questions.get(player_type, type_questions["default"])
    return random.choice(pool)