# ==========================================================
# NovaCortex.py — Nova Adeptus Master Pipeline Coordinator
# The brain's integration layer. Coordinates all 7 brain
# systems into a single intelligent response pipeline.
# Replaces smart_dispatch as the main entry point.
# smart_dispatch becomes a one-line wrapper for cortex_dispatch.
# ==========================================================

import random

# ----------------------------------------------------------
# PIPELINE RESULT — full record of what happened
# ----------------------------------------------------------
class PipelineResult:
    def __init__(self):
        self.input_raw        = ""
        self.signal           = None
        self.context          = None
        self.emotion          = None
        self.reasoning        = None
        self.behavior         = None
        self.proposed         = ""
        self.final            = ""
        self.pipeline_errors  = []
        self.fallback_used    = False
        self.turn_number      = 0

    def to_dict(self):
        return {
            "input":          self.input_raw[:50],
            "final":          self.final[:80],
            "fallback_used":  self.fallback_used,
            "pipeline_errors":self.pipeline_errors,
            "turn_number":    self.turn_number,
        }

# ----------------------------------------------------------
# CORTEX ENGINE
# ----------------------------------------------------------
class CortexEngine:
    def __init__(self):
        self._turn_count     = 0
        self._initialized    = False
        self._debug_enabled  = False
        self._last_result    = None

    def initialize(self):
        """
        Called once after all brain files are loaded.
        Verifies all systems are available.
        """
        errors = []
        systems = [
            ("NovaSensory",         "sense"),
            ("NovaWorkingMemory",   "working_memory"),
            ("NovaEmotionalState",  "emotional_state"),
            ("NovaReasoningEngine", "reasoning_engine"),
            ("NovaContextTracker",  "context_tracker"),
            ("NovaLanguageGenerator","language_generator"),
            ("NovaResponseEvaluator","response_evaluator"),
        ]
        for system_name, global_name in systems:
            try:
                g = globals().get(global_name)
                if g is None:
                    g = _get_global(global_name)
                if g is None:
                    errors.append(f"{system_name} not found")
            except Exception as e:
                errors.append(f"{system_name} error: {e}")

        self._initialized = len(errors) == 0
        if errors:
            return f"Cortex init warnings: {errors}"
        return "NovaCortex online — all systems nominal ✅"

    def dispatch(self, msg, player_context=None):
        """
        Main entry point. Full 8-stage pipeline.
        Returns final response string.
        """
        self._turn_count += 1
        result             = PipelineResult()
        result.input_raw   = msg
        result.turn_number = self._turn_count

        # ── STAGE 1: SENSORY ─────────────────────────────
        try:
            result.signal = _call("sense", msg)
        except Exception as e:
            result.pipeline_errors.append(f"sensory: {e}")
            result.signal = _dummy_signal(msg)

        # ── STAGE 2: WORKING MEMORY ───────────────────────
        try:
            result.context = _call(
                "working_memory.update",
                result.signal,
                ""
            )
        except Exception as e:
            result.pipeline_errors.append(f"memory: {e}")
            result.context = _dummy_context()

        # ── STAGE 3: EMOTIONAL STATE ──────────────────────
        try:
            rel = _get_relationship()
            result.emotion = _call(
                "emotional_state.update",
                result.signal,
                result.context,
                rel
            )
        except Exception as e:
            result.pipeline_errors.append(f"emotion: {e}")
            result.emotion = _dummy_emotion()

        # ── STAGE 4: NLP INTENTS ──────────────────────────
        intents = []
        try:
            nlp_result = _call("extract", msg)
            if nlp_result:
                intents = nlp_result.get("intents", [])
                # auto save name if detected
                if ("name_intro" in intents and
                        "name" in nlp_result.get("entities", {})):
                    _set_user_name(
                        nlp_result["entities"]["name"])
        except Exception as e:
            result.pipeline_errors.append(f"nlp: {e}")

        # ── STAGE 5: REASONING ────────────────────────────
        try:
            rel = _get_relationship()
            result.reasoning = _call(
                "reasoning_engine.score",
                result.signal,
                result.context,
                result.emotion,
                rel,
                intents
            )
        except Exception as e:
            result.pipeline_errors.append(f"reasoning: {e}")
            result.reasoning = _dummy_reasoning()

        # ── STAGE 6: CONTEXT TRACKING ─────────────────────
        try:
            result.behavior = _call(
                "context_tracker.update",
                result.context,
                result.signal
            )
        except Exception as e:
            result.pipeline_errors.append(f"context: {e}")
            result.behavior = None

        # ── STAGE 7: CONTENT RESOLUTION ───────────────────
        content = None
        try:
            content = self._resolve_content(
                result.reasoning,
                result.signal,
                intents,
                result.context
            )
        except Exception as e:
            result.pipeline_errors.append(f"content: {e}")

        # ── STAGE 8: LANGUAGE GENERATION ──────────────────
        try:
            rel  = _get_relationship()
            name = _get_user_name()
            result.proposed = _call(
                "language_generator.build",
                result.reasoning,
                result.emotion,
                result.signal,
                rel,
                name,
                content,
                result.context
            )
        except Exception as e:
            result.pipeline_errors.append(f"language: {e}")
            result.proposed = _get_fallback()

        # ── STAGE 9: EVALUATION ───────────────────────────
        try:
            result.final = _call(
                "response_evaluator.get_final",
                result.proposed,
                result.context,
                result.emotion
            )
        except Exception as e:
            result.pipeline_errors.append(f"evaluator: {e}")
            result.final = result.proposed or _get_fallback()

        # ── STAGE 10: MEMORY COMMIT ───────────────────────
        try:
            _commit_to_memory(msg, result.final)
            # update working memory with Nova response
            _call(
                "working_memory.update",
                result.signal,
                result.final
            )
        except Exception as e:
            result.pipeline_errors.append(f"commit: {e}")

        # ── STAGE 11: GAME EVENT TRIGGERS ─────────────────
        try:
            self._handle_game_events(msg, result)
        except Exception as e:
            result.pipeline_errors.append(f"events: {e}")

        # store last result
        self._last_result = result

        # debug log
        if self._debug_enabled:
            self._log_result(result)

        return result.final

    # ----------------------------------------------------------
    # CONTENT RESOLUTION
    # Routes content_hint from reasoning to actual game content
    # ----------------------------------------------------------
    def _resolve_content(self, reasoning, signal,
                         intents, context_window):
        hint = reasoning.content_hint if reasoning else None
        if not hint:
            return self._resolve_from_intents(
                intents, signal, context_window)
        return self._resolve_hint(hint, signal, context_window)

    def _resolve_hint(self, hint, signal, context_window):
        try:
            if hint == "trivia":
                return start_trivia_ext()
            if hint == "joke":
                if _api_jokes:
                    return _api_jokes.pop(0)
                return random.choice(JOKES) if JOKES else None
            if hint == "fact":
                if _api_facts:
                    return _api_facts.pop(0)
                return random.choice(FACTS) if FACTS else None
            if hint == "mission":
                return accept_mission()
            if hint == "stats":
                return show_stats()
            if hint == "help":
                return HELP_TEXT
            if hint == "nudge":
                return None
        except Exception:
            pass
        return None

    def _resolve_from_intents(self, intents, signal,
                               context_window):
        try:
            # direct command resolution
            cleaned = signal.cleaned if signal else ""

            # commands dict lookup
            commands = {
                "accept":   lambda: accept_mission(),
                "complete": lambda: complete_mission(),
                "reset":    lambda: reset_missions(),
                "stats":    lambda: show_stats(),
                "help":     lambda: HELP_TEXT,
                "list":     lambda: list_missions(),
                "skills":   lambda: list_skills(),
                "history":  lambda: view_history(),
                "trivia":   lambda: start_trivia_ext(),
                "mini":     lambda: start_mini(),
                "mood":     lambda: _get_mood_response(),
                "reward":   lambda: random_reward(),
                "bonus":    lambda: random_bonus(),
                "dismiss":  lambda: dismiss_companion(),
                "advance":  lambda: advance_story_arc(),
                "loot":     lambda: loot_drop(),
                "hack":     lambda: start_hack(),
                "boss":     lambda: boss_battle(),
                "companion":lambda: summon_companion(),
                "story":    lambda: start_story_arc(),
                "market":   lambda: trade_market(),
                "endgame":  lambda: endgame_mission(),
                "upgrade":  lambda: ship_upgrade(),
                "event":    lambda: random_cosmic_event(),
                "cosmic":   lambda: cosmic_event_final(),
                "ship":     lambda: ship_ai_interaction(),
                "enemy":    lambda: enemy_encounter(),
                "joke":     lambda: _get_joke(),
                "fact":     lambda: _get_fact(),
                "advice":   lambda: _get_advice(),
            }
            if cleaned in commands:
                return commands[cleaned]()

            # keyword content
            if "fight" in cleaned or "combat" in cleaned:
                return advanced_combat()
            if "stealth" in cleaned:
                return stealth_mission()
            if "puzzle" in cleaned:
                return start_puzzle()
            if "analyze" in cleaned:
                return start_analysis()
            if "mission" in cleaned:
                return random.choice(MISSIONS) if MISSIONS else None
            if "start trivia" in cleaned or "space quiz" in cleaned:
                return start_trivia_ext()

        except Exception:
            pass
        return None

    # ----------------------------------------------------------
    # GAME EVENT TRIGGERS
    # Hooks into emotional state on game outcomes
    # ----------------------------------------------------------
    def _handle_game_events(self, msg, result):
        cleaned = msg.lower().strip()
        final   = result.final.lower() if result.final else ""

        # mission complete
        if "complete" in cleaned and "completed" in final:
            try:
                nova_on_mission_complete()
            except Exception:
                pass

        # boss defeated
        if "boss" in cleaned and "defeated" in final:
            try:
                nova_on_boss_defeated()
            except Exception:
                pass

        # trivia correct
        if "correct" in final and "xp" in final:
            try:
                nova_on_trivia_correct()
            except Exception:
                pass

        # hostile input
        hostile = ["hate","stupid","useless","suck",
                   "broken","terrible","worst","shut up"]
        if any(h in cleaned for h in hostile):
            try:
                nova_on_hostile_input()
            except Exception:
                pass

    def _log_result(self, result):
        try:
            lines = [f"\n[Cortex Turn {result.turn_number}]"]
            lines.append(f"  input:    {result.input_raw[:40]}")
            state = (result.emotion.current
                     if result.emotion else "unknown")
            lines.append(f"  emotion:  {state}")
            rtype = (result.reasoning.response_type
                     if result.reasoning else "unknown")
            lines.append(f"  rtype:    {rtype}")
            lines.append(f"  final:    {result.final[:60]}")
            if result.pipeline_errors:
                lines.append(
                    f"  errors:   {result.pipeline_errors}")
            print("\n".join(lines))
        except Exception:
            pass

# ----------------------------------------------------------
# CALL HELPERS — safe method dispatch
# ----------------------------------------------------------

def _call(func_path, *args):
    import sys
    main = vars(sys.modules['__main__'])
    parts = func_path.split(".")
    if len(parts) == 1:
        fn = main.get(func_path)
        if callable(fn):
            return fn(*args)
    elif len(parts) == 2:
        obj = main.get(parts[0])
        if obj and hasattr(obj, parts[1]):
            method = getattr(obj, parts[1])
            if callable(method):
                return method(*args)
    raise ValueError(f"Cannot resolve: {func_path}")

def _get_global(name):
    return globals().get(name)

# ----------------------------------------------------------
# RELATIONSHIP / USER HELPERS
# ----------------------------------------------------------
def _get_relationship():
    try:
        return memory.relationship
    except Exception:
        return "neutral"

def _get_user_name():
    try:
        return memory.user_name
    except Exception:
        return None

def _build_player_context():
    name = _get_user_name() or "operative"
    rel = _get_relationship()
    emotion = get_emotional_state()
    recent = working_memory.get_recent_user_inputs(3)
    player_type = context_tracker.get_player_type()
    turns = working_memory.window.session_turn
    
    return {
        "name": name,
        "relationship": rel,
        "emotion": emotion,
        "recent": recent,
        "player_type": player_type,
        "turns": turns,
        "has_name": memory.user_name is not None,
    }

def _set_user_name(name):
    try:
        memory.user_name = name
    except Exception:
        pass

def _commit_to_memory(user_msg, nova_response):
    try:
        memory.remember('user', user_msg)
        memory.remember('nova', nova_response)
    except Exception:
        pass

# ----------------------------------------------------------
# DUMMY OBJECTS — fallbacks if a brain stage fails
# ----------------------------------------------------------
def _dummy_signal(msg):
    class DS:
        raw        = msg
        cleaned    = msg.lower().strip()
        input_type = "statement"
        complexity = "simple"
        word_count = len(msg.split())
        has_greeting   = False
        has_name_intro = False
        has_question   = False
        has_command    = False
        has_emotional  = False
        is_nonsense    = False
        language_confidence = 1.0
        tokens = msg.lower().split()
    return DS()

def _dummy_context():
    class DC:
        player_pattern     = "exploring"
        needs_nudge        = False
        last_3_nova        = []
        repeat_count       = 0
        session_turn       = 1
        questions_in_a_row = 0
        commands_in_a_row  = 0
        nonsense_in_a_row  = 0
        dominant_topic     = "none"
    return DC()

def _dummy_emotion():
    class DE:
        current                  = "calm"
        intensity                = 0.5
        consecutive_irritations  = 0
        consecutive_positives    = 0
    return DE()

def _dummy_reasoning():
    class DR:
        response_type       = "deflect"
        confidence          = 0.3
        allow_question_back = False
        content_hint        = None
        urgency             = "normal"
        scores              = {}
    return DR()

# ----------------------------------------------------------
# CONTENT HELPERS
# ----------------------------------------------------------
def _get_fallback():
    try:
        return random.choice(DIALOGUES)
    except Exception:
        return "The void has nothing right now. Try again."

def _get_joke():
    try:
        if _api_jokes:
            return _api_jokes.pop(0)
        return random.choice(JOKES)
    except Exception:
        return "My humor circuits are offline."

def _get_fact():
    try:
        if _api_facts:
            return _api_facts.pop(0)
        return random.choice(FACTS)
    except Exception:
        return "My fact database is unavailable."

def _get_advice():
    try:
        if _api_advice:
            return _api_advice.pop(0)
        return "The void advises: type 'accept' and stop hesitating."
    except Exception:
        return "No advice available."

def _get_mood_response():
    try:
        state = emotional_state.get_current()
        return (f"Nova's current state: {state} "
                f"{emotional_state.get_emoji()} — "
                f"player cannot change this.")
    except Exception:
        return "Nova's mood is her own business."

# ----------------------------------------------------------
# GLOBAL CORTEX INSTANCE
# ----------------------------------------------------------
cortex = CortexEngine()

# ----------------------------------------------------------
# MAIN ENTRY POINT
# Called from ChatBotAdeptus.py smart_dispatch
# ----------------------------------------------------------
def cortex_dispatch(msg):
    """
    Full 8-stage intelligent pipeline.
    This is Nova's brain firing.
    """
    # FSM check first — mid-game states bypass pipeline
    try:
        if fsm.state in _FSM_HANDLERS:
            return _FSM_HANDLERS[fsm.state](msg)
    except Exception:
        pass

    # conv_fsm check — trivia and name await states
    try:
        if conv_fsm.state == ConversationFSM.IN_TRIVIA:
            return check_trivia_answer(msg)
        if conv_fsm.state == ConversationFSM.AWAIT_NAME:
            conv_fsm.reset()
            return _answer_name(msg)
    except Exception:
        pass

    # ML dispatch — fast path for high confidence intents
    try:
        ml_result = ml_dispatch(msg)
        if ml_result:
            # still run emotional state update
            try:
                sig = sense(msg)
                ctx = working_memory.update(sig, ml_result)
                emotional_state.update(sig, ctx, _get_relationship())
                context_tracker.update(ctx, sig)
            except Exception:
                pass
            return ml_result
    except Exception:
        pass
    # greeting chain
    try:
        nlp_result = extract(msg)
        intents = nlp_result.get("intents", []) if nlp_result else []
        if "greeting" in intents:
            name = _get_user_name()
            emotion = get_emotional_state()
            rel = _get_relationship()

            opener = get_opener("greeting", name)

            feeling_map = {
                "calm":      "Systems nominal.",
                "amused":    "Something amuses me today.",
                "irritated": "My patience is already depleted.",
                "intrigued": "Something has my attention.",
                "impressed": "I am in a rare good mood.",
            }
            feeling = feeling_map.get(emotion, "Operational.")

            hook_map = {
                "neutral":   "Type 'accept' for a mission or 'help' to begin.",
                "warming":   "You keep coming back. Type 'accept' or 'mini'.",
                "trusted":   f"Good to see you{', ' + name if name else ''}. What do you need?",
                "respected": f"The void kept your seat warm{', ' + name if name else ''}.",
                "rival":     "Don't push it today. Type 'accept' if you want something.",
            }
            hook = hook_map.get(rel, "Type 'help' if you need direction.")

            return personality.apply(f"{opener} {feeling} {hook}")
    except Exception:
        pass



    ctx = _build_player_context()
    return cortex.dispatch(msg, ctx)

# ----------------------------------------------------------
# CORTEX INIT — call after all files loaded
# ----------------------------------------------------------
def cortex_init():
    return cortex.initialize()

# ----------------------------------------------------------
# DEBUG COMMANDS
# ----------------------------------------------------------
def nova_mood():
    """Debug command — shows full emotional state."""
    try:
        lines = ["Nova internal state:"]
        lines.append(f"  emotion:      {emotion_debug()}")
        lines.append(f"  reasoning:    {reasoning_debug()}")
        lines.append(f"  memory:       {memory_debug()}")
        lines.append(f"  tracker:      {tracker_debug()}")
        return "\n".join(lines)
    except Exception as e:
        return f"nova_mood debug error: {e}"

def cortex_debug(msg="hello"):
    """Full pipeline debug for a test message."""
    try:
        old_debug = cortex._debug_enabled
        cortex._debug_enabled = True
        result = cortex.dispatch(msg)
        cortex._debug_enabled = old_debug
        lines = [f"Cortex debug: '{msg}'"]
        if cortex._last_result:
            for k, v in cortex._last_result.to_dict().items():
                lines.append(f"  {k}: {v}")
        lines.append(f"  output: {result}")
        return "\n".join(lines)
    except Exception as e:
        return f"cortex_debug error: {e}"