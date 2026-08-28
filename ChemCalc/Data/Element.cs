using System;

namespace ChemCalc.Data
{
    /// <summary>
    /// Represents a chemical element with all its properties
    /// </summary>
    public class Element
    {
        // ==================== BASIC PROPERTIES ====================

        /// <summary>
        /// Atomic number (number of protons)
        /// </summary>
        public int AtomicNumber { get; set; }

        /// <summary>
        /// Chemical symbol (e.g., "H", "He", "Li")
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Full element name (e.g., "Hydrogen", "Helium")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Atomic mass in atomic mass units (amu)
        /// </summary>
        public double AtomicMass { get; set; }

        // ==================== CLASSIFICATION ====================

        /// <summary>
        /// Element category (Alkali Metal, Halogen, etc.)
        /// </summary>
        public ElementType ElementType { get; set; }

        /// <summary>
        /// Standard state at room temperature
        /// </summary>
        public PhysicalState StandardState { get; set; }

        // ==================== PERIODIC TABLE POSITION ====================

        /// <summary>
        /// Group number (column) in periodic table (1-18)
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// Period number (row) in periodic table (1-7)
        /// </summary>
        public int Period { get; set; }

        // ==================== VISUAL PROPERTIES ====================

        /// <summary>
        /// Background color for UI display (hex color)
        /// </summary>
        public string Color { get; set; }

        // ==================== ADDITIONAL PROPERTIES ====================

        /// <summary>
        /// Electron configuration (e.g., "1s²")
        /// </summary>
        public string ElectronConfiguration { get; set; }

        /// <summary>
        /// Electronegativity (Pauling scale)
        /// </summary>
        public double Electronegativity { get; set; }

        // ==================== CONSTRUCTOR ====================

        public Element()
        {
            Symbol = "";
            Name = "";
            Color = "#CCCCCC";
            ElectronConfiguration = "";
        }

        public Element(int atomicNumber, string symbol, string name, double atomicMass,
                      ElementType elementType, PhysicalState standardState,
                      int group, int period, string color)
        {
            AtomicNumber = atomicNumber;
            Symbol = symbol;
            Name = name;
            AtomicMass = atomicMass;
            ElementType = elementType;
            StandardState = standardState;
            Group = group;
            Period = period;
            Color = color;
            ElectronConfiguration = "";
            Electronegativity = 0;
        }

        // ==================== METHODS ====================

        /// <summary>
        /// Get element display string for UI
        /// </summary>
        public string GetDisplayString()
        {
            return $"{Symbol} - {Name}";
        }

        /// <summary>
        /// Get formatted atomic mass (rounded to 3 decimal places)
        /// </summary>
        public string GetFormattedMass()
        {
            return AtomicMass.ToString("F3");
        }

        /// <summary>
        /// Check if element is a metal
        /// </summary>
        public bool IsMetal()
        {
            return ElementType == ElementType.AlkaliMetal ||
                   ElementType == ElementType.AlkalineEarth ||
                   ElementType == ElementType.TransitionMetal ||
                   ElementType == ElementType.BasicMetal ||
                   ElementType == ElementType.Lanthanide ||
                   ElementType == ElementType.Actinide;
        }

        /// <summary>
        /// Check if element is a nonmetal
        /// </summary>
        public bool IsNonmetal()
        {
            return ElementType == ElementType.Nonmetal ||
                   ElementType == ElementType.Halogen ||
                   ElementType == ElementType.NobleGas;
        }

        /// <summary>
        /// Get state symbol for display
        /// </summary>
        public string GetStateSymbol()
        {
            switch (StandardState)
            {
                case PhysicalState.Solid:
                    return "(s)";
                case PhysicalState.Liquid:
                    return "(l)";
                case PhysicalState.Gas:
                    return "(g)";
                case PhysicalState.Unknown:
                default:
                    return "";
            }
        }

        public override string ToString()
        {
            return $"{AtomicNumber} {Symbol} - {Name} ({AtomicMass})";
        }
    }

    /// <summary>
    /// Physical state at standard temperature and pressure
    /// </summary>
    public enum PhysicalState
    {
        Solid,
        Liquid,
        Gas,
        Unknown
    }
}