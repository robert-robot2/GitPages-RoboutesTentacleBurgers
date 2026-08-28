namespace SpectralXGLX.Services
{
    public static class GamepadBindsConfig
    {
        // Available gamepad buttons for UI dropdown
        public static readonly string[] AvailableButtons = new[]
        {
            "A", "B", "X", "Y",
            "LeftBumper", "RightBumper",
            "LeftTrigger", "RightTrigger",
            "Start", "Back",
            "DpadUp", "DpadDown", "DpadLeft", "DpadRight",
            "LeftStick", "RightStick"
        };

        // Action → button bindings
        private static readonly Dictionary<string, string> bindings = new()
        {
            // ── Combat ───────────────────────────────────────────────────────
            { "Attack",    "X" },
            { "SPAttack",  "Y" },

            // ── UI ───────────────────────────────────────────────────────────
            { "Menu",      "Start" },
            { "Inventory", "Back" },
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
                    Console.WriteLine($"[GamepadBindsConfig] WARNING: button '{kv.Value}' " +
                        $"bound to both '{dict[kv.Value]}' and '{kv.Key}'");
                }
                dict[kv.Value] = kv.Key;
            }
            return dict;
        }

        public static string? GetAction(string button)
        {
            return reverse.TryGetValue(button, out var action) ? action : null;
        }

        public static string GetBinding(string action)
        {
            return bindings.TryGetValue(action, out var button) ? button : string.Empty;
        }

        public static void SetBinding(string action, string button)
        {
            if (!bindings.ContainsKey(action)) return;

            // Swap if another action already owns this button
            var existingAction = reverse.TryGetValue(button, out var owner) ? owner : null;
            if (existingAction != null && existingAction != action)
            {
                bindings[existingAction] = string.Empty;
            }

            bindings[action] = button;
            reverse = BuildReverse();
        }

        public static IReadOnlyDictionary<string, string> Bindings => bindings;

        // Display-friendly button labels for the UI
        public static string GetButtonLabel(string button) => button switch
        {
            "A" => "A",
            "B" => "B",
            "X" => "X",
            "Y" => "Y",
            "LeftBumper" => "LB",
            "RightBumper" => "RB",
            "LeftTrigger" => "LT",
            "RightTrigger" => "RT",
            "Start" => "Start",
            "Back" => "Back",
            "DpadUp" => "D↑",
            "DpadDown" => "D↓",
            "DpadLeft" => "D←",
            "DpadRight" => "D→",
            "LeftStick" => "L3",
            "RightStick" => "R3",
            _ => button
        };
    }
}