namespace ChemCalc.Data
{
    /// <summary>
    /// Categories of chemical elements for periodic table classification
    /// </summary>
    public enum ElementType
    {
        /// <summary>
        /// Alkali Metals: Li, Na, K, Rb, Cs, Fr (Group 1, except H)
        /// Highly reactive, soft metals
        /// Color: Red
        /// </summary>
        AlkaliMetal,

        /// <summary>
        /// Alkaline Earth Metals: Be, Mg, Ca, Sr, Ba, Ra (Group 2)
        /// Reactive metals, less reactive than alkali metals
        /// Color: Orange
        /// </summary>
        AlkalineEarth,

        /// <summary>
        /// Transition Metals: Sc-Zn, Y-Cd, La-Hg, Ac-Cn (Groups 3-12)
        /// Hard metals with high melting points
        /// Color: Yellow
        /// </summary>
        TransitionMetal,

        /// <summary>
        /// Basic Metals (Post-Transition): Al, Ga, In, Sn, Tl, Pb, Bi, Po (Groups 13-16)
        /// Softer than transition metals
        /// Color: Green
        /// </summary>
        BasicMetal,

        /// <summary>
        /// Semimetals (Metalloids): B, Si, Ge, As, Sb, Te, At
        /// Properties between metals and nonmetals
        /// Color: Teal/Cyan
        /// </summary>
        Semimetal,

        /// <summary>
        /// Nonmetals: H, C, N, O, P, S, Se
        /// Poor conductors, brittle in solid form
        /// Color: Blue
        /// </summary>
        Nonmetal,

        /// <summary>
        /// Halogens: F, Cl, Br, I, At (Group 17)
        /// Highly reactive nonmetals
        /// Color: Purple/Violet
        /// </summary>
        Halogen,

        /// <summary>
        /// Noble Gases: He, Ne, Ar, Kr, Xe, Rn, Og (Group 18)
        /// Inert gases, very low reactivity
        /// Color: Pink/Magenta
        /// </summary>
        NobleGas,

        /// <summary>
        /// Lanthanides: La-Lu (Row 6, f-block)
        /// Rare earth elements
        /// Color: Light Pink
        /// </summary>
        Lanthanide,

        /// <summary>
        /// Actinides: Ac-Lr (Row 7, f-block)
        /// Radioactive elements
        /// Color: Dark Pink/Red
        /// </summary>
        Actinide,

        /// <summary>
        /// Unknown or not yet classified
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Helper class for ElementType utilities
    /// </summary>
    public static class ElementTypeHelper
    {
        /// <summary>
        /// Get display name for element type
        /// </summary>
        public static string GetDisplayName(ElementType type)
        {
            switch (type)
            {
                case ElementType.AlkaliMetal:
                    return "Alkali Metal";
                case ElementType.AlkalineEarth:
                    return "Alkaline Earth";
                case ElementType.TransitionMetal:
                    return "Transition Metal";
                case ElementType.BasicMetal:
                    return "Basic Metal";
                case ElementType.Semimetal:
                    return "Semimetal";
                case ElementType.Nonmetal:
                    return "Nonmetal";
                case ElementType.Halogen:
                    return "Halogen";
                case ElementType.NobleGas:
                    return "Noble Gas";
                case ElementType.Lanthanide:
                    return "Lanthanide";
                case ElementType.Actinide:
                    return "Actinide";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Get standard color for element type (hex)
        /// </summary>
        public static string GetStandardColor(ElementType type)
        {
            switch (type)
            {
                case ElementType.AlkaliMetal:
                    return "#FF6B6B"; // Red
                case ElementType.AlkalineEarth:
                    return "#FFB366"; // Orange
                case ElementType.TransitionMetal:
                    return "#FFE66D"; // Yellow
                case ElementType.BasicMetal:
                    return "#95E1D3"; // Green/Teal
                case ElementType.Semimetal:
                    return "#4ECDC4"; // Cyan
                case ElementType.Nonmetal:
                    return "#6C9BD1"; // Blue
                case ElementType.Halogen:
                    return "#AA96DA"; // Purple
                case ElementType.NobleGas:
                    return "#FFACC7"; // Pink
                case ElementType.Lanthanide:
                    return "#FFC9E0"; // Light Pink
                case ElementType.Actinide:
                    return "#FF8FA3"; // Dark Pink
                default:
                    return "#CCCCCC"; // Gray
            }
        }

        /// <summary>
        /// Get legend label for UI
        /// </summary>
        public static string GetLegendLabel(ElementType type)
        {
            switch (type)
            {
                case ElementType.AlkaliMetal:
                    return "Alkali";
                case ElementType.AlkalineEarth:
                    return "Alkaline Earth";
                case ElementType.TransitionMetal:
                    return "Transition";
                case ElementType.BasicMetal:
                    return "Basic Metal";
                case ElementType.Semimetal:
                    return "Semimetal";
                case ElementType.Nonmetal:
                    return "Nonmetal";
                case ElementType.Halogen:
                    return "Halogen";
                case ElementType.NobleGas:
                    return "Noble Gas";
                case ElementType.Lanthanide:
                    return "Lanthanide";
                case ElementType.Actinide:
                    return "Actinide";
                default:
                    return "";
            }
        }
    }
}