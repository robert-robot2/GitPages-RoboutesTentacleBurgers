using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Web;

namespace SpectralXGLX.Services
{
    public static class KeybindConfig
    {
        // Bindings are stored in canonical combo format, e.g. "W", "UP", "SHIFT+W".
        // This is the ONLY key format used anywhere (input handling + rebind UI).
        private static readonly Dictionary<string, string> bindings = new()
        {
            // ── Movement ─────────────────────────────────────────────────────
            { "MoveUp",    "W" },
            { "MoveDown",  "S" },
            { "MoveLeft",  "A" },
            { "MoveRight", "D" },

            // ── Combat ───────────────────────────────────────────────────────
            { "Attack",   "F" },
            { "SPAttack", "G" },

            // ── UI ───────────────────────────────────────────────────────────
            { "Menu",      "5" },
            { "Inventory", "I" },
            { "UIToggle",  "4" },
            { "HUDToggle", "6" },
            { "Debug",     "3" },
            { "LandscapeToggle", "2" },

                       // ── Camera Mode Switches ──────────────────────────────────────────
            { "CamModeFreeCam",      "SHIFT+F" },
            { "CamModeOrbit",        "SHIFT+O" },
            { "CamModeOrtho",        "SHIFT+P" },
            { "CamModeWebpage",      "SHIFT+W" },

            // ── Orbit Camera — Preset Views ───────────────────────────────────
            { "CamFront",            "SHIFT+1" },
            { "CamBack",             "SHIFT+ALT+1" },
            { "CamRight",            "SHIFT+3" },
            { "CamLeft",             "SHIFT+ALT+3" },
            { "CamTop",              "SHIFT+7" },
            { "CamBottom",           "SHIFT+ALT+7" },
            { "CamToggleOrtho",      "SHIFT+5" },
            { "CamOppositeView",     "SHIFT+9" },
            { "CamLookThrough",      "SHIFT+SLASH" },
            { "CamAlignToViewport",  "SHIFT+ALT+0" },

            // ── Orbit Camera — Numpad Step Orbit ─────────────────────────────
            { "CamOrbitDown",        "SHIFT+2" },
            { "CamOrbitLeft",        "SHIFT+4" },
            { "CamOrbitRight",       "SHIFT+6" },
            { "CamOrbitUp",          "SHIFT+8" },

            // ── Ortho Camera — Player Lock Toggle ────────────────────────────
            { "CamOrthoLockPlayer",  "SHIFT+L" },

            // ── Zoom (shared across Orbit and Ortho) ──────────────────────────
            { "CamZoomIn",           "SHIFT+EQUALS" },
            { "CamZoomOut",          "SHIFT+MINUS" },
        };

        private static Dictionary<string, string> reverse = BuildReverse();

        private static Dictionary<string, string> BuildReverse()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in bindings)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                if (dict.ContainsKey(kv.Value))
                {
                    Console.WriteLine($"[KeybindConfig] WARNING: key '{kv.Value}' " +
                        $"bound to both '{dict[kv.Value]}' and '{kv.Key}'");
                }
                dict[kv.Value] = kv.Key;
            }
            return dict;
        }

        public static string? GetAction(string canonicalKey)
        {
            return reverse.TryGetValue(canonicalKey, out var action) ? action : null;
        }

        public static bool IsMovementKey(string canonicalKey)
        {
            var movementActions = new[] { "MoveUp", "MoveDown", "MoveLeft", "MoveRight" };
            return movementActions.Any(action =>
                string.Equals(bindings[action], canonicalKey,
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Returns true if the action is a camera control action.
        /// Used by SpectralXInput to gate camera binds in correct modes.
        /// </summary>
        public static bool IsCameraAction(string action)
        {
            return action.StartsWith("Cam", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if the action is an orbit-specific action.
        /// Orbit actions only fire when CameraMode == Orbit.
        /// </summary>
        public static bool IsOrbitAction(string action)
        {
            return action is
                "CamFront" or "CamBack" or
                "CamRight" or "CamLeft" or
                "CamTop" or "CamBottom" or
                "CamToggleOrtho" or "CamOppositeView" or
                "CamLookThrough" or "CamAlignToViewport" or
                "CamOrbitDown" or "CamOrbitLeft" or
                "CamOrbitRight" or "CamOrbitUp";
        }

        /// <summary>
        /// Returns true if the action is valid in Orthographic camera mode.
        /// </summary>
        public static bool IsOrthoAction(string action)
        {
            return action is
                "CamOrthoLockPlayer" or
                "CamZoomIn" or "CamZoomOut";
        }

        public static void SetBinding(string action, string canonicalKey)
        {
            if (!bindings.ContainsKey(action)) return;

            // If another action already owns this key unbind it first
            // so we never have two actions silently colliding
            var existingAction = reverse.TryGetValue(canonicalKey, out var owner)
                ? owner : null;
            if (existingAction != null && existingAction != action)
            {
                bindings[existingAction] = string.Empty;
            }

            bindings[action] = canonicalKey;
            reverse = BuildReverse();
        }

        public static string GetBinding(string action)
        {
            return bindings.TryGetValue(action, out var key) ? key : string.Empty;
        }

        public static IReadOnlyDictionary<string, string> Bindings => bindings;

        /// <summary>
        /// Single source of truth for building a canonical combo key from a
        /// KeyboardEventArgs. Used by both SpectralXInput (live input) and the
        /// rebind UI (CaptureKey), so they can never drift out of sync again.
        /// </summary>
        public static string BuildCanonicalKey(KeyboardEventArgs e)
        {
            string key = e.Key switch
            {
                // ── Existing keys ─────────────────────────────────────────────
                "Escape" => "ESCAPE",
                "ArrowUp" => "UP",
                "ArrowDown" => "DOWN",
                "ArrowLeft" => "LEFT",
                "ArrowRight" => "RIGHT",
                "a" => "A",
                "b" => "B",
                "c" => "C",
                "d" => "D",
                "e" => "E",
                "f" => "F",
                "g" => "G",
                "h" => "H",
                "i" => "I",
                "j" => "J",
                "k" => "K",
                "l" => "L",
                "m" => "M",
                "n" => "N",
                "o" => "O",
                "p" => "P",
                "q" => "Q",
                "r" => "R",
                "s" => "S",
                "t" => "T",
                "u" => "U",
                "v" => "V",
                "w" => "W",
                "x" => "X",
                "y" => "Y",
                "z" => "Z",

                // ── Number row — camera preset views ─────────────────────────
                "1" => "1",
                "2" => "2",
                "3" => "3",
                "4" => "4",
                "5" => "5",
                "6" => "6",
                "7" => "7",
                "8" => "8",
                "9" => "9",
                "0" => "0",

                // ── Shifted number row — browser sends symbol instead of digit ────
                "!" => "1",
                "@" => "2",
                "#" => "3",
                "$" => "4",
                "%" => "5",
                "^" => "6",
                "&" => "7",
                "*" => "8",
                "(" => "9",
                ")" => "0",

                // ── Numpad — map to same canonical strings as number row ───────
                // Browser sends "Numpad1" etc with NumLock on
                // We map them to the same "1"-"9" so bindings work
                // regardless of whether user uses numpad or number row
                "Numpad0" => "0",
                "Numpad1" => "1",
                "Numpad2" => "2",
                "Numpad3" => "3",
                "Numpad4" => "4",
                "Numpad5" => "5",
                "Numpad6" => "6",
                "Numpad7" => "7",
                "Numpad8" => "8",
                "Numpad9" => "9",

                // ── Special characters ────────────────────────────────────────
                "/" => "SLASH",
                "NumpadDivide" => "SLASH",
                "=" => "EQUALS",
                "NumpadAdd" => "EQUALS",
                "-" => "MINUS",
                "NumpadSubtract" => "MINUS",

                _ => e.Key.ToUpper()
            };

            var parts = new List<string>();
            if (e.CtrlKey) parts.Add("CTRL");
            if (e.ShiftKey) parts.Add("SHIFT");
            if (e.AltKey) parts.Add("ALT");
            parts.Add(key);

            return string.Join("+", parts);
        }
    }
}