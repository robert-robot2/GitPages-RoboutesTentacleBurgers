
/*
// Pyodide Helper.js

window.PyodideHelper = {

    pyodide: null,

    async initialize(identityJson, missionsJson, greetingsJson, profileJson, mlPayload) {
        this.pyodide = await loadPyodide();

        // Note these files have been merged into chatbot.py to find gltiches and bugs with naming issue
        
        // 1 — Vocabulary first — just data, no dependencies
        const vocabCode = await (await fetch('/iPython/NovaVocabulary.py')).text();
        await this.pyodide.runPythonAsync(vocabCode);

        // 2 — NLP second — just data + regex, no dependencies
        const nlpCode = await (await fetch('/iPython/NovaNLP.py')).text();
        await this.pyodide.runPythonAsync(nlpCode);

        // 3 — Extensions third — uses vocabulary + nlp
        const extCode = await (await fetch('/iPython/NovaAIExtensions.py')).text();
        await this.pyodide.runPythonAsync(extCode);

        const mlCode = await (await fetch('/iPython/NovaMLIntelligence.py')).text();
        await this.pyodide.runPythonAsync(mlCode);
        
        const files = [
            '/iPython/NovaSensory.py',
            '/iPython/NovaWorkingMemory.py',
            '/iPython/NovaEmotionalState.py',
            '/iPython/NovaReasoningEngine.py',
            '/iPython/NovaContextTracker.py',
            '/iPython/NovaLanguageGenerator.py',
            '/iPython/NovaResponseEvaluator.py',
          
            '/iPython/ChatBotAdeptus.py',  // loads last, ties everything together
            '/iPython/NovaCortex.py',
            '/iPython/PythonTests.py', 
        ];
        for (const file of files) {
            const code = await (await fetch(file)).text();
            await this.pyodide.runPythonAsync(code);
        }
        // 4 — Main file last — overwrites dummy globals, uses everything above
      //  const code = await (await fetch('/iPython/ChatBotAdeptus.py')).text();
     //   await this.pyodide.runPythonAsync(code);

        if (identityJson && missionsJson && greetingsJson) {
            const safe = s => s.replace(/\\/g, '\\\\').replace(/'/g, "\\'");
            await this.pyodide.runPythonAsync(`
import json
CSHARP_IDENTITY  = json.loads('${safe(identityJson)}')
CSHARP_MISSIONS  = json.loads('${safe(missionsJson)}')
CSHARP_GREETINGS = json.loads('${safe(greetingsJson)}')
MISSIONS  = MISSIONS  + CSHARP_MISSIONS
GREETINGS = GREETINGS + CSHARP_GREETINGS
for k, v in CSHARP_IDENTITY.items():
    NOVA_IDENTITY[(k,)] = v
            `);
        }

        // ADD before fetchTrivia():
      // const mlPayload = NovaIntelligenceController_SerializeForPython;
        if (mlPayload) {
            const mlResult = await this.pyodide.runPythonAsync(
                `ml_init(${JSON.stringify(mlPayload)})`
            );
            console.log('[Nova ML]', mlResult);
        }

        await this.fetchTrivia();
        await this.loadMemory();
    },




    async fetchTrivia(amount = 10) {
        const categories = [17, 19, 14, 13];
        const cat = categories[Math.floor(Math.random() * categories.length)];
        const url = `https://opentdb.com/api.php?amount=${amount}&category=${cat}&type=multiple&encode=url3986`;
        try {
            const res = await fetch(url);
            const text = await res.text();
            const decoded = JSON.parse(text);
            decoded.results = decoded.results.map(q => ({
                ...q,
                question: decodeURIComponent(q.question),
                correct_answer: decodeURIComponent(q.correct_answer),
                incorrect_answers: q.incorrect_answers.map(decodeURIComponent),
                category: decodeURIComponent(q.category),
            }));
            const escaped = JSON.stringify(decoded).replace(/\\/g, '\\\\').replace(/'/g, "\\'");
            await this.pyodide.runPythonAsync(`inject_trivia_result('${escaped}')`);
            return true;
        } catch (err) {
            console.warn('[Nova] Trivia fetch failed:', err);
            return false;
        }
    },

    async fetchJoke() {
        try {
            const res = await fetch('https://v2.jokeapi.dev/joke/Any?blacklistFlags=nsfw,racist&type=single');
            const data = await res.json();
            if (data.joke) {
                const escaped = JSON.stringify(data.joke);
                await this.pyodide.runPythonAsync(`inject_api_joke(${escaped})`);
            }
        } catch (err) {
            console.warn('[Nova] JokeAPI fetch failed:', err);
        }
    },

    async fetchFact() {
        try {
            const res = await fetch('https://uselessfacts.jsph.pl/api/v2/facts/random?language=en');
            const data = await res.json();
            if (data.text) {
                const escaped = JSON.stringify(data.text);
                await this.pyodide.runPythonAsync(`inject_api_fact(${escaped})`);
            }
        } catch (err) {
            console.warn('[Nova] UselessFacts fetch failed:', err);
        }
    },

    async fetchAdvice() {
        try {
            const res = await fetch('https://api.adviceslip.com/advice');
            const data = await res.json();
            if (data.slip?.advice) {
                const escaped = JSON.stringify(data.slip.advice);
                await this.pyodide.runPythonAsync(`inject_api_advice(${escaped})`);
            }
        } catch (err) {
            console.warn('[Nova] AdviceSlip fetch failed:', err);
        }
    },

    async sendMessage(userMessage) {
        const escaped = JSON.stringify(userMessage);
        const reply = await this.pyodide.runPythonAsync(`
try:
    result = smart_dispatch(${escaped})
except Exception as e:
    result = f"[Nova Error]: {str(e)}"
result
`);
        const cacheSize = await this.pyodide.runPythonAsync('len(_trivia_cache)');
        if (cacheSize < 3) {
            this.fetchTrivia().catch(() => { });
            this.fetchJoke().catch(() => { });
            this.fetchFact().catch(() => { });
            this.fetchAdvice().catch(() => { });
        }
        await this.saveMemory();
        return reply;
       
    },

    // Syncprofile?
    async syncContext(contextJson) {
        if (!this.pyodide) return;
        try {
            const safe = contextJson.replace(/\\/g, '\\\\').replace(/'/g, "\\'");
            await this.pyodide.runPythonAsync(`
import json as _j
_ctx = _j.loads('${safe}')
memory.relationship   = _ctx.get('relationship', 'neutral')
memory.player_title   = _ctx.get('player_title', 'human')
memory.dominant_style = _ctx.get('dominant_style', 'neutral')
        `);
        } catch (err) {
            console.warn('[Nova] Context sync failed:', err);
        }
    },

    async awardXP(amount, reason = '') {
        const escaped = JSON.stringify(reason);
        return await this.pyodide.runPythonAsync(`
memory.xp += ${amount}
personality.apply(f"XP +${amount}" + (" (" + ${escaped} + ")" if ${escaped} else ""))
        `);
    },

    async getMemorySnapshot() {
        const raw = await this.pyodide.runPythonAsync(`
import json as _j
_j.dumps({
    'xp':                 memory.xp,
    'level':              memory.level,
    'skills':             memory.skills,
    'missions_active':    len(memory.active_missions),
    'missions_completed': len(memory.completed_missions),
    'mood':               personality.mode,
    'fsm_state':          fsm.state,
})
        `);
        return JSON.parse(raw);
    },

    async saveMemory() {
        try {
            const snapshot = await this.pyodide.runPythonAsync('save_memory_snapshot()');
            localStorage.setItem('nova_memory', snapshot);
        } catch (err) {
            console.warn('[Nova] Memory save failed:', err);
        }
    },

    async loadMemory() {
        try {
            const saved = localStorage.getItem('nova_memory');
            if (saved) {
                const escaped = JSON.stringify(saved);
                const result = await this.pyodide.runPythonAsync(
                    `load_memory_snapshot(${escaped})`
                );
                console.log('[Nova Memory]', result);
                return result;
            }
        } catch (err) {
            console.warn('[Nova] Memory load failed:', err);
        }
        return null;
    },

    async runPythonTest(functionName, testInput) {
        if (!this.pyodide) {
            return "Pyodide not ready yet — wait a moment and try again";
        }
        const safeName = JSON.stringify(functionName);
        const safeInput = JSON.stringify(testInput);
        const result = await this.pyodide.runPythonAsync(`
try:
    result = run_test(${safeName}, ${safeInput})
except Exception as e:
    result = f"Python error: {str(e)}"
result
`);
        return String(result);
    }

};

*/





    
    window.PyodideHelper = {

    pyodide: null,
    _emotionColor: "#4A90D9",

    async initialize(identityJson, missionsJson,
        greetingsJson, profileJson, mlPayload) {
        this.pyodide = await loadPyodide();

        // ── LOAD BRAIN FILES IN ORDER ──────────────────────
        const brainFiles = [
            '/iPython/NovaSensory.py',
            '/iPython/NovaWorkingMemory.py',
            '/iPython/NovaEmotionalState.py',
            '/iPython/NovaReasoningEngine.py',
            '/iPython/NovaContextTracker.py',
            '/iPython/NovaLanguageGenerator.py',
            '/iPython/NovaResponseEvaluator.py',
        
            '/iPython/ChatBotAdeptus.py',
            '/iPython/NovaCortex.py',
            '/iPython/PythonTests.py',
        ];

        for (const file of brainFiles) {
            try {
                const code = await (await fetch(file)).text();
                await this.pyodide.runPythonAsync(code);
                console.log(`[Nova Brain] Loaded: ${file}`);
            } catch (err) {
                console.warn(`[Nova Brain] Failed to load ${file}:`, err);
            }
        }

        // ── INJECT C# DATA ─────────────────────────────────
        if (identityJson && missionsJson && greetingsJson) {
            const safe = s => s
                .replace(/\\/g, '\\\\')
                .replace(/'/g, "\\'");
            await this.pyodide.runPythonAsync(`
import json
CSHARP_IDENTITY  = json.loads('${safe(identityJson)}')
CSHARP_MISSIONS  = json.loads('${safe(missionsJson)}')
CSHARP_GREETINGS = json.loads('${safe(greetingsJson)}')
MISSIONS  = MISSIONS  + CSHARP_MISSIONS
GREETINGS = GREETINGS + CSHARP_GREETINGS
for k, v in CSHARP_IDENTITY.items():
    NOVA_IDENTITY[(k,)] = v
            `);
        }

        // ── ML INIT ────────────────────────────────────────
        if (mlPayload) {
            const mlResult = await this.pyodide.runPythonAsync(
                `ml_init(${JSON.stringify(mlPayload)})`
            );
            console.log('[Nova ML]', mlResult);
        }

        // ── CORTEX INIT ────────────────────────────────────
        try {
            const cortexResult = await this.pyodide
                .runPythonAsync(`cortex_init()`);
            console.log('[Nova Cortex]', cortexResult);
        } catch (err) {
            console.warn('[Nova Cortex] Init failed:', err);
        }

        // ── LOAD SAVED MEMORY ──────────────────────────────
        await this.loadMemory();
        await this.loadEmotionalState();

        // ── FETCH INITIAL CONTENT ──────────────────────────
        await this.fetchTrivia();
        this.fetchJoke().catch(() => { });
        this.fetchFact().catch(() => { });
        this.fetchAdvice().catch(() => { });
    },

    // ── TRIVIA ─────────────────────────────────────────────
    async fetchTrivia(amount = 10) {
        const categories = [17, 19, 14, 13];
        const cat = categories[
            Math.floor(Math.random() * categories.length)];
        const url = `https://opentdb.com/api.php?amount=${amount}`
            + `&category=${cat}&type=multiple&encode=url3986`;
        try {
            const res = await fetch(url);
            const text = await res.text();
            const decoded = JSON.parse(text);
            decoded.results = decoded.results.map(q => ({
                ...q,
                question: decodeURIComponent(q.question),
                correct_answer: decodeURIComponent(
                    q.correct_answer),
                incorrect_answers: q.incorrect_answers.map(
                    decodeURIComponent),
                category: decodeURIComponent(q.category),
            }));
            const escaped = JSON.stringify(decoded)
                .replace(/\\/g, '\\\\')
                .replace(/'/g, "\\'");
            await this.pyodide.runPythonAsync(
                `inject_trivia_result('${escaped}')`);
            return true;
        } catch (err) {
            console.warn('[Nova] Trivia fetch failed:', err);
            return false;
        }
    },

    // ── JOKE API ───────────────────────────────────────────
    async fetchJoke() {
        try {
            const res = await fetch(
                'https://v2.jokeapi.dev/joke/Any'
                + '?blacklistFlags=nsfw,racist&type=single');
            const data = await res.json();
            if (data.joke) {
                const escaped = JSON.stringify(data.joke);
                await this.pyodide.runPythonAsync(
                    `inject_api_joke(${escaped})`);
            }
        } catch (err) {
            console.warn('[Nova] JokeAPI fetch failed:', err);
        }
    },

    // ── FACTS API ──────────────────────────────────────────
    async fetchFact() {
        try {
            const res = await fetch(
                'https://uselessfacts.jsph.pl/api/v2'
                + '/facts/random?language=en');
            const data = await res.json();
            if (data.text) {
                const escaped = JSON.stringify(data.text);
                await this.pyodide.runPythonAsync(
                    `inject_api_fact(${escaped})`);
            }
        } catch (err) {
            console.warn('[Nova] UselessFacts failed:', err);
        }
    },

    // ── ADVICE API ─────────────────────────────────────────
    async fetchAdvice() {
        try {
            const res = await fetch(
                'https://api.adviceslip.com/advice');
            const data = await res.json();
            if (data.slip?.advice) {
                const escaped = JSON.stringify(
                    data.slip.advice);
                await this.pyodide.runPythonAsync(
                    `inject_api_advice(${escaped})`);
            }
        } catch (err) {
            console.warn('[Nova] AdviceSlip failed:', err);
        }
    },

    // ── SEND MESSAGE ───────────────────────────────────────
    async sendMessage(userMessage) {
        const escaped = JSON.stringify(userMessage);
        const reply = await this.pyodide.runPythonAsync(`
try:
    result = cortex_dispatch(${escaped})
except Exception as e:
    result = f"[Nova Error]: {str(e)}"
result
`);
        // refill content pools
        const cacheSize = await this.pyodide
            .runPythonAsync('len(_trivia_cache)');
        if (cacheSize < 3) {
            this.fetchTrivia().catch(() => { });
        }
        this.fetchJoke().catch(() => { });
        this.fetchFact().catch(() => { });
        this.fetchAdvice().catch(() => { });

        // sync emotional state color to UI
        await this.syncEmotionalState();
        await this.saveMemory();           // direct call — replaces JS.InvokeVoidAsync
        await this.saveEmotionalState();   // direct call — replaces JS.InvokeVoidAsync
        return reply;
    },

    // ── EMOTIONAL STATE SYNC ───────────────────────────────
    async syncEmotionalState() {
        try {
            const color = await this.pyodide
                .runPythonAsync('get_emotional_color()');
            const emoji = await this.pyodide
                .runPythonAsync('get_emotional_emoji()');
            const state = await this.pyodide
                .runPythonAsync('get_emotional_state()');
            this._emotionColor = color;

            // call Blazor to update the dot
            if (window.DotNet) {
                await window.DotNet.invokeMethodAsync(
                    'RoboutesTentacleBurgers',
                    'UpdateEmotionalState',
                    color, emoji, state
                );
            }
        } catch (err) {
            console.warn('[Nova] Emotion sync failed:', err);
        }
    },

    // ── CONTEXT SYNC ───────────────────────────────────────
    async syncContext(contextJson) {
        if (!this.pyodide) return;
        try {
            const safe = contextJson
                .replace(/\\/g, '\\\\')
                .replace(/'/g, "\\'");
            await this.pyodide.runPythonAsync(`
import json as _j
_ctx = _j.loads('${safe}')
memory.relationship   = _ctx.get('relationship', 'neutral')
memory.player_title   = _ctx.get('player_title', 'human')
memory.dominant_style = _ctx.get('dominant_style', 'neutral')
            `);
        } catch (err) {
            console.warn('[Nova] Context sync failed:', err);
        }
    },

    // ── XP AWARD ───────────────────────────────────────────
    async awardXP(amount, reason = '') {
        const escaped = JSON.stringify(reason);
        return await this.pyodide.runPythonAsync(`
memory.xp += ${amount}
f"XP +${amount}"
        `);
    },

    // ── MEMORY SAVE / LOAD ─────────────────────────────────
    async saveMemory() {
        try {
            const snapshot = await this.pyodide
                .runPythonAsync('save_memory_snapshot()');
            localStorage.setItem('nova_memory', snapshot);

            // save context tracker
            const ctxSnap = await this.pyodide
                .runPythonAsync('context_tracker.snapshot()');
            localStorage.setItem(
                'nova_context_tracker', ctxSnap);
        } catch (err) {
            console.warn('[Nova] Memory save failed:', err);
        }
    },

    async loadMemory() {
        try {
            const saved = localStorage.getItem('nova_memory');
            if (saved) {
                const escaped = JSON.stringify(saved);
                const result = await this.pyodide
                    .runPythonAsync(
                        `load_memory_snapshot(${escaped})`);
                console.log('[Nova Memory]', result);
            }
            // load context tracker
            const ctxSaved = localStorage.getItem(
                'nova_context_tracker');
            if (ctxSaved) {
                const escaped = JSON.stringify(ctxSaved);
                await this.pyodide.runPythonAsync(
                    `context_tracker.load_snapshot(${escaped})`
                );
            }
        } catch (err) {
            console.warn('[Nova] Memory load failed:', err);
        }
    },

    // ── EMOTIONAL STATE SAVE / LOAD ────────────────────────
    async saveEmotionalState() {
        try {
            const snap = await this.pyodide
                .runPythonAsync('emotional_snapshot()');
            localStorage.setItem('nova_emotion', snap);
        } catch (err) {
            console.warn('[Nova] Emotion save failed:', err);
        }
    },

    async loadEmotionalState() {
        try {
            const saved = localStorage.getItem('nova_emotion');
            if (saved) {
                const escaped = JSON.stringify(saved);
                const result = await this.pyodide
                    .runPythonAsync(
                        `load_emotional_snapshot(${escaped})`
                    );
                console.log('[Nova Emotion]', result);
            }
        } catch (err) {
            console.warn(
                '[Nova] Emotion load failed:', err);
        }
    },

    // ── FULL STATE SNAPSHOT ────────────────────────────────
    async getMemorySnapshot() {
        const raw = await this.pyodide.runPythonAsync(`
import json as _j
_j.dumps({
    'xp':                 memory.xp,
    'level':              memory.level,
    'skills':             memory.skills,
    'missions_active':    len(memory.active_missions),
    'missions_completed': len(memory.completed_missions),
    'emotion':            get_emotional_state(),
    'emotion_color':      get_emotional_color(),
    'fsm_state':          fsm.state,
    'relationship':       memory.relationship,
    'player_title':       memory.player_title,
})
        `);
        return JSON.parse(raw);
    },

    // ── DEBUG ──────────────────────────────────────────────
    async cortexDebug(msg = "hello") {
        return await this.pyodide.runPythonAsync(
            `cortex_debug("${msg}")`);
    },

    async emotionDebug() {
        return await this.pyodide.runPythonAsync(
            'emotion_debug()');
    },

    async novaMood() {
        return await this.pyodide.runPythonAsync(
            'nova_mood()');
    },


    async runPythonTest(functionName, testInput) {
        if (!this.pyodide) {
            return "Pyodide not ready yet — wait a moment and try again";
        }
        const safeName = JSON.stringify(functionName);
        const safeInput = JSON.stringify(testInput);
        const result = await this.pyodide.runPythonAsync(`
try:
    result = run_test(${safeName}, ${safeInput})
except Exception as e:
    result = f"Python error: {str(e)}"
result
`);
        return String(result);
    }

};
    
    
    