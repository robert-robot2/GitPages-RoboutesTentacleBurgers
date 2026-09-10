// ==========================================================
// pyodideHelper.js — Nova Adeptus Cerebellum Bridge
// Minimal Pyodide helper — sole purpose is to load
// cerebellumDataCore.py and return its training examples
// to C# as a JSON string.
// ==========================================================

window.CerebellumBridge = {

    pyodide: null,

    // ── INITIALIZE ─────────────────────────────────────────
    // Loads Pyodide and cerebellumDataCore.py.
    // Call once from NovaCortex.LoadAPIContent().
    async initialize() {
        try {
            this.pyodide = await loadPyodide();
            console.log('[CerebellumBridge] Pyodide loaded.');

            const code = await (await fetch('/iPython/cerebellumDataCore.py')).text();
            await this.pyodide.runPythonAsync(code);
            console.log('[CerebellumBridge] cerebellumDataCore.py loaded.');

            return true;
        } catch (err) {
            console.error('[CerebellumBridge] Init failed:', err);
            return false;
        }
    },

    // ── GET EXAMPLES ───────────────────────────────────────
    // Calls get_examples() in Python.
    // Returns a JSON string of the training data dict,
    // or null on failure.
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
};