// ==========================================================
// pyodideHelper.js — Nova Adeptus Cerebellum Bridge
// ==========================================================

window.CerebellumBridge = {

    pyodide: null,
    async initialize() {
        try {
            this.pyodide = await loadPyodide();
            console.log('[CerebellumBridge] Pyodide loaded.');

            const code = await (await fetch('/iPython/cerebellumDataCore.py')).text();
            await this.pyodide.runPythonAsync(code);

            // Load Parietal Lobe at init too
            const angular = await (await fetch('/iPython/NovaAngularGyrus.py')).text();
            await this.pyodide.runPythonAsync(angular);
            this._gyrusLoaded = true;

            // Load Parietal Lobe at init too
            const parietal = await (await fetch('/iPython/NovaParietalLobe.py')).text();
            await this.pyodide.runPythonAsync(parietal);
            this._parietalLoaded = true;

            // Load Arcuate Fasciculus grammar tables
            const fasciculus = await (
                await fetch('/iPython/NovaArcuateFasciculusDataCore.py')
            ).text();
            await this.pyodide.runPythonAsync(fasciculus);
            this._fasciculusLoaded = true;
            console.log('[CerebellumBridge] Fasciculus grammar tables loaded.');
            // Load Wernicke semantic memory (domain tables + cache)
            const wernickeData = await (
                await fetch('/iPython/NovaWernickeDataCore.py')
            ).text();
            await this.pyodide.runPythonAsync(wernickeData);
            this._wernickeDataLoaded = true;
            console.log('[CerebellumBridge] Wernicke data tables loaded.');

            // Load Broca production templates
            const brocaData = await (
                await fetch('/iPython/NovaBrocaDataCore.py')
            ).text();
            await this.pyodide.runPythonAsync(brocaData);
            this._brocaDataLoaded = true;
            console.log('[CerebellumBridge] Broca assembly templates loaded.');

            console.log('[CerebellumBridge] All Python modules loaded.');
            return true;
        } catch (err) {
            console.error('[CerebellumBridge] Init failed:', err);
            return false;
        }
    },
    async getAngularGyrusResponse(userInput) {
        if (!this.pyodide) {
            console.error('[CerebellumBridge] Pyodide not initialized');
            return null;
        }
        try {
            if (!this._gyrusLoaded) {
                const code = await (await fetch('/iPython/NovaAngularGyrus.py')).text();
                await this.pyodide.runPythonAsync(code);
                this._gyrusLoaded = true;
            }

            console.log('[CerebellumBridge] Calling nova_angular_gyrus with input:', userInput);
            const result = await this.pyodide.runPythonAsync(
                `nova_angular_gyrus(${JSON.stringify(userInput)})`
            );

            // ← FIX: Parse the JSON string that Python returns
            const parsed = typeof result === 'string' ? JSON.parse(result) : result;
            console.log('[CerebellumBridge] Angular Gyrus returned:', parsed);
            return parsed;
        } catch (err) {
            console.error('[CerebellumBridge] Angular Gyrus error:', err);
            return null;
        }
    },
    async getParietalResponse(userInput) {
        if (!this.pyodide) return null;
        try {
            if (!this._parietalLoaded) {
                const code = await (await fetch('/iPython/NovaParietalLobe.py')).text();
                await this.pyodide.runPythonAsync(code);
                this._parietalLoaded = true;
            }
            // Return raw string — C# will parse it
            const result = await this.pyodide.runPythonAsync(
                `nova_parietal_respond(${JSON.stringify(userInput)})`
            );
            return result;  // ← no JSON.parse here
        } catch (err) {
            console.error('[CerebellumBridge] ParietalLobe failed:', err);
            return null;
        }
    },
    // ADD this to CerebellumBridge instead of runPython()
    // Mirrors getAngularGyrusResponse exactly

    async getFasciculusResponse(intent, tone, timeLabel, relationship, name) {
        if (!this.pyodide) return null;
        try {
            // Load file if not already loaded
            if (!this._fasciculusLoaded) {
                const code = await (
                    await fetch('/iPython/NovaArcuateFasciculusDataCore.py')
                ).text();
                await this.pyodide.runPythonAsync(code);
                this._fasciculusLoaded = true;
            }

            // Call the specific function by name — same pattern as Angular Gyrus
            const result = await this.pyodide.runPythonAsync(
                `assemble_response(
                ${JSON.stringify(intent)},
                ${JSON.stringify(tone)},
                ${JSON.stringify(timeLabel)},
                ${JSON.stringify(relationship)},
                ${JSON.stringify(name)}
            )`
            );

            return typeof result === 'string' ? result : null;

        } catch (err) {
            console.error('[CerebellumBridge] getFasciculusResponse error:', err);
            return null;
        }
    },
    async getBrocaAssembly(intent, tone, relationship,
        wordDataJson, subjectDataJson,
        domainHint) {
        if (!this.pyodide) {
            console.error('[CerebellumBridge] getBrocaAssembly: not ready');
            return null;
        }
        try {
            // Ensure Wernicke data is loaded (dependency)
            if (!this._wernickeDataLoaded) {
                const code = await (
                    await fetch('/iPython/NovaWernickeDataCore.py')
                ).text();
                await this.pyodide.runPythonAsync(code);
                this._wernickeDataLoaded = true;
            }

            // Ensure Broca data is loaded
            if (!this._brocaDataLoaded) {
                const code = await (
                    await fetch('/iPython/NovaBrocaDataCore.py')
                ).text();
                await this.pyodide.runPythonAsync(code);
                this._brocaDataLoaded = true;
            }

            // Parse Oxford raw JSON via Wernicke first
            // This enriches wordData with domain_flavor + lexical_intro
            const enrichedJson = await this.pyodide.runPythonAsync(
                `get_word_data(
                    ${JSON.stringify(intent === 'process_query'
                    ? JSON.parse(wordDataJson || '{}').word || ''
                    : JSON.parse(wordDataJson || '{}').word || '')},
                    ${JSON.stringify(wordDataJson)},
                    ${JSON.stringify(domainHint)}
                )`
            );

            // Enrich subject too for process queries
            let enrichedSubjectJson = '';
            if (intent === 'process_query' && subjectDataJson) {
                const subjectWord =
                    JSON.parse(subjectDataJson || '{}').word || '';
                enrichedSubjectJson =
                    await this.pyodide.runPythonAsync(
                        `get_word_data(
                            ${JSON.stringify(subjectWord)},
                            ${JSON.stringify(subjectDataJson)},
                            ''
                        )`
                    );
            }

            // Call Broca assembly with enriched data
            const result = await this.pyodide.runPythonAsync(
                `assemble_broca_response(
                    ${JSON.stringify(intent)},
                    ${JSON.stringify(tone)},
                    ${JSON.stringify(relationship)},
                    ${JSON.stringify(enrichedJson || wordDataJson)},
                    ${JSON.stringify(enrichedSubjectJson || subjectDataJson)},
                    ${JSON.stringify(domainHint)}
                )`
            );

            return typeof result === 'string' ? result : null;

        } catch (err) {
            console.error('[CerebellumBridge] getBrocaAssembly error:', err);
            return null;
        }
    },

    // ── GET EXAMPLES ───────────────────────────────────────
    async getExamples() {
        if (!this.pyodide) {
            console.error('[CerebellumBridge] Not initialized.');
            return null;
        }
        try {
            const result = await this.pyodide.runPythonAsync('get_examples()');
            console.log('[CerebellumBridge] Examples received.');
            return result;
        } catch (err) {
            console.error('[CerebellumBridge] getExamples failed:', err);
            return null;
        }
    }

    // ← No comma on the last method — correct
};