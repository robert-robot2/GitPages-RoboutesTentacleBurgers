using System.Collections.Generic;

namespace ChemCalc.Helpers
{
    /// <summary>
    /// Manages calculator color themes
    /// </summary>
    public class ThemeManager
    {
        public string CurrentTheme { get; private set; } = "diablo";

        // Predefined themes
        public static Dictionary<string, Theme> Themes = new()
        {
            {
                "diablo", new Theme
                {
                    Name = "Diablo Red",
                    PrimaryColor = "#ff0000",
                    SecondaryColor = "#ff4444",
                    GlowColor = "#ff0000",
                    DisplayTextColor = "#00ff00",
                    ButtonTextColor = "#ff4444",
                    BorderColor = "#ff0000"
                }
            },
            {
                "matrix", new Theme
                {
                    Name = "Matrix Green",
                    PrimaryColor = "#00ff00",
                    SecondaryColor = "#00cc00",
                    GlowColor = "#00ff00",
                    DisplayTextColor = "#00ff00",
                    ButtonTextColor = "#00cc00",
                    BorderColor = "#00ff00"
                }
            },
            {
                "ocean", new Theme
                {
                    Name = "Ocean Blue",
                    PrimaryColor = "#00ccff",
                    SecondaryColor = "#0099ff",
                    GlowColor = "#00ccff",
                    DisplayTextColor = "#00ffff",
                    ButtonTextColor = "#00ccff",
                    BorderColor = "#00ccff"
                }
            },
            {
                "sunset", new Theme
                {
                    Name = "Sunset Orange",
                    PrimaryColor = "#ff6600",
                    SecondaryColor = "#ff9933",
                    GlowColor = "#ff6600",
                    DisplayTextColor = "#ffcc00",
                    ButtonTextColor = "#ff9933",
                    BorderColor = "#ff6600"
                }
            },
            {
                "royal", new Theme
                {
                    Name = "Royal Purple",
                    PrimaryColor = "#9933ff",
                    SecondaryColor = "#bb66ff",
                    GlowColor = "#9933ff",
                    DisplayTextColor = "#cc99ff",
                    ButtonTextColor = "#bb66ff",
                    BorderColor = "#9933ff"
                }
            },
            {
                "cyber", new Theme
                {
                    Name = "Cyber Pink",
                    PrimaryColor = "#ff00ff",
                    SecondaryColor = "#ff66ff",
                    GlowColor = "#ff00ff",
                    DisplayTextColor = "#ff00ff",
                    ButtonTextColor = "#ff66ff",
                    BorderColor = "#ff00ff"
                }
            },
            {
                "gold", new Theme
                {
                    Name = "Gold",
                    PrimaryColor = "#ffd700",
                    SecondaryColor = "#ffed4e",
                    GlowColor = "#ffd700",
                    DisplayTextColor = "#ffed4e",
                    ButtonTextColor = "#ffd700",
                    BorderColor = "#ffd700"
                }
            }
        };

        public void SetTheme(string themeName)
        {
            if (Themes.ContainsKey(themeName))
            {
                CurrentTheme = themeName;
            }
        }

        public Theme GetCurrentTheme()
        {
            return Themes[CurrentTheme];
        }

        /// <summary>
        /// Generate CSS variables for current theme
        /// </summary>
        public string GenerateThemeCSS()
        {
            var theme = GetCurrentTheme();
            return $@"
                :root {{
                    --calc-primary-color: {theme.PrimaryColor};
                    --calc-secondary-color: {theme.SecondaryColor};
                    --calc-glow-color: {theme.GlowColor};
                    --calc-display-text: {theme.DisplayTextColor};
                    --calc-button-text: {theme.ButtonTextColor};
                    --calc-border-color: {theme.BorderColor};
                }}
            ";
        }

        /// <summary>
        /// Create custom theme from hex colors
        /// </summary>
        public void SetCustomTheme(string primaryColor, string secondaryColor, string glowColor,
            string displayText, string buttonText, string borderColor)
        {
            Themes["custom"] = new Theme
            {
                Name = "Custom",
                PrimaryColor = primaryColor,
                SecondaryColor = secondaryColor,
                GlowColor = glowColor,
                DisplayTextColor = displayText,
                ButtonTextColor = buttonText,
                BorderColor = borderColor
            };
            CurrentTheme = "custom";
        }

        public List<string> GetThemeNames()
        {
            return new List<string>(Themes.Keys);
        }
    }

    /// <summary>
    /// Theme color definition
    /// </summary>
    public class Theme
    {
        public string Name { get; set; } = "";
        public string PrimaryColor { get; set; } = "#ff0000";
        public string SecondaryColor { get; set; } = "#ff4444";
        public string GlowColor { get; set; } = "#ff0000";
        public string DisplayTextColor { get; set; } = "#00ff00";
        public string ButtonTextColor { get; set; } = "#ff4444";
        public string BorderColor { get; set; } = "#ff0000";
    }
}