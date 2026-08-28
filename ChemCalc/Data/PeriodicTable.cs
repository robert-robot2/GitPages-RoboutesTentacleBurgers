using System.Collections.Generic;
using System.Linq;

namespace ChemCalc.Data
{
    /// <summary>
    /// Complete periodic table database with all 118 elements
    /// </summary>
    public static class PeriodicTable
    {
        private static Dictionary<int, Element> _elements;
        private static Dictionary<string, Element> _elementsBySymbol;

        /// <summary>
        /// Initialize periodic table on first access
        /// </summary>
        static PeriodicTable()
        {
            InitializeElements();
        }

        /// <summary>
        /// Get element by atomic number
        /// </summary>
        public static Element GetElement(int atomicNumber)
        {
            if (_elements.ContainsKey(atomicNumber))
                return _elements[atomicNumber];
            return null;
        }

        /// <summary>
        /// Get element by symbol (case insensitive)
        /// </summary>
        public static Element GetElement(string symbol)
        {
            string upperSymbol = symbol.ToUpper();
            if (_elementsBySymbol.ContainsKey(upperSymbol))
                return _elementsBySymbol[upperSymbol];
            return null;
        }

        /// <summary>
        /// Get all elements of a specific type
        /// </summary>
        public static List<Element> GetElementsByType(ElementType type)
        {
            return _elements.Values.Where(e => e.ElementType == type).ToList();
        }

        /// <summary>
        /// Get all elements in a specific period (row)
        /// </summary>
        public static List<Element> GetElementsByPeriod(int period)
        {
            return _elements.Values.Where(e => e.Period == period).ToList();
        }

        /// <summary>
        /// Get all elements in a specific group (column)
        /// </summary>
        public static List<Element> GetElementsByGroup(int group)
        {
            return _elements.Values.Where(e => e.Group == group).ToList();
        }

        /// <summary>
        /// Get all elements
        /// </summary>
        public static List<Element> GetAllElements()
        {
            return _elements.Values.OrderBy(e => e.AtomicNumber).ToList();
        }

        /// <summary>
        /// Initialize all 118 elements
        /// </summary>
        private static void InitializeElements()
        {
            _elements = new Dictionary<int, Element>();
            _elementsBySymbol = new Dictionary<string, Element>();

            // Period 1
            AddElement(1, "H", "Hydrogen", 1.008, ElementType.Nonmetal, PhysicalState.Gas, 1, 1);
            AddElement(2, "He", "Helium", 4.003, ElementType.NobleGas, PhysicalState.Gas, 18, 1);

            // Period 2
            AddElement(3, "Li", "Lithium", 6.941, ElementType.AlkaliMetal, PhysicalState.Solid, 1, 2);
            AddElement(4, "Be", "Beryllium", 9.012, ElementType.AlkalineEarth, PhysicalState.Solid, 2, 2);
            AddElement(5, "B", "Boron", 10.811, ElementType.Semimetal, PhysicalState.Solid, 13, 2);
            AddElement(6, "C", "Carbon", 12.011, ElementType.Nonmetal, PhysicalState.Solid, 14, 2);
            AddElement(7, "N", "Nitrogen", 14.007, ElementType.Nonmetal, PhysicalState.Gas, 15, 2);
            AddElement(8, "O", "Oxygen", 15.999, ElementType.Nonmetal, PhysicalState.Gas, 16, 2);
            AddElement(9, "F", "Fluorine", 18.998, ElementType.Halogen, PhysicalState.Gas, 17, 2);
            AddElement(10, "Ne", "Neon", 20.180, ElementType.NobleGas, PhysicalState.Gas, 18, 2);

            // Period 3
            AddElement(11, "Na", "Sodium", 22.990, ElementType.AlkaliMetal, PhysicalState.Solid, 1, 3);
            AddElement(12, "Mg", "Magnesium", 24.305, ElementType.AlkalineEarth, PhysicalState.Solid, 2, 3);
            AddElement(13, "Al", "Aluminum", 26.982, ElementType.BasicMetal, PhysicalState.Solid, 13, 3);
            AddElement(14, "Si", "Silicon", 28.086, ElementType.Semimetal, PhysicalState.Solid, 14, 3);
            AddElement(15, "P", "Phosphorus", 30.974, ElementType.Nonmetal, PhysicalState.Solid, 15, 3);
            AddElement(16, "S", "Sulfur", 32.065, ElementType.Nonmetal, PhysicalState.Solid, 16, 3);
            AddElement(17, "Cl", "Chlorine", 35.453, ElementType.Halogen, PhysicalState.Gas, 17, 3);
            AddElement(18, "Ar", "Argon", 39.948, ElementType.NobleGas, PhysicalState.Gas, 18, 3);

            // Period 4
            AddElement(19, "K", "Potassium", 39.098, ElementType.AlkaliMetal, PhysicalState.Solid, 1, 4);
            AddElement(20, "Ca", "Calcium", 40.078, ElementType.AlkalineEarth, PhysicalState.Solid, 2, 4);
            AddElement(21, "Sc", "Scandium", 44.956, ElementType.TransitionMetal, PhysicalState.Solid, 3, 4);
            AddElement(22, "Ti", "Titanium", 47.867, ElementType.TransitionMetal, PhysicalState.Solid, 4, 4);
            AddElement(23, "V", "Vanadium", 50.942, ElementType.TransitionMetal, PhysicalState.Solid, 5, 4);
            AddElement(24, "Cr", "Chromium", 51.996, ElementType.TransitionMetal, PhysicalState.Solid, 6, 4);
            AddElement(25, "Mn", "Manganese", 54.938, ElementType.TransitionMetal, PhysicalState.Solid, 7, 4);
            AddElement(26, "Fe", "Iron", 55.845, ElementType.TransitionMetal, PhysicalState.Solid, 8, 4);
            AddElement(27, "Co", "Cobalt", 58.933, ElementType.TransitionMetal, PhysicalState.Solid, 9, 4);
            AddElement(28, "Ni", "Nickel", 58.693, ElementType.TransitionMetal, PhysicalState.Solid, 10, 4);
            AddElement(29, "Cu", "Copper", 63.546, ElementType.TransitionMetal, PhysicalState.Solid, 11, 4);
            AddElement(30, "Zn", "Zinc", 65.380, ElementType.TransitionMetal, PhysicalState.Solid, 12, 4);
            AddElement(31, "Ga", "Gallium", 69.723, ElementType.BasicMetal, PhysicalState.Solid, 13, 4);
            AddElement(32, "Ge", "Germanium", 72.640, ElementType.Semimetal, PhysicalState.Solid, 14, 4);
            AddElement(33, "As", "Arsenic", 74.922, ElementType.Semimetal, PhysicalState.Solid, 15, 4);
            AddElement(34, "Se", "Selenium", 78.960, ElementType.Nonmetal, PhysicalState.Solid, 16, 4);
            AddElement(35, "Br", "Bromine", 79.904, ElementType.Halogen, PhysicalState.Liquid, 17, 4);
            AddElement(36, "Kr", "Krypton", 83.798, ElementType.NobleGas, PhysicalState.Gas, 18, 4);

            // Period 5
            AddElement(37, "Rb", "Rubidium", 85.468, ElementType.AlkaliMetal, PhysicalState.Solid, 1, 5);
            AddElement(38, "Sr", "Strontium", 87.620, ElementType.AlkalineEarth, PhysicalState.Solid, 2, 5);
            AddElement(39, "Y", "Yttrium", 88.906, ElementType.TransitionMetal, PhysicalState.Solid, 3, 5);
            AddElement(40, "Zr", "Zirconium", 91.224, ElementType.TransitionMetal, PhysicalState.Solid, 4, 5);
            AddElement(41, "Nb", "Niobium", 92.906, ElementType.TransitionMetal, PhysicalState.Solid, 5, 5);
            AddElement(42, "Mo", "Molybdenum", 95.960, ElementType.TransitionMetal, PhysicalState.Solid, 6, 5);
            AddElement(43, "Tc", "Technetium", 98.907, ElementType.TransitionMetal, PhysicalState.Solid, 7, 5);
            AddElement(44, "Ru", "Ruthenium", 101.070, ElementType.TransitionMetal, PhysicalState.Solid, 8, 5);
            AddElement(45, "Rh", "Rhodium", 102.906, ElementType.TransitionMetal, PhysicalState.Solid, 9, 5);
            AddElement(46, "Pd", "Palladium", 106.420, ElementType.TransitionMetal, PhysicalState.Solid, 10, 5);
            AddElement(47, "Ag", "Silver", 107.868, ElementType.TransitionMetal, PhysicalState.Solid, 11, 5);
            AddElement(48, "Cd", "Cadmium", 112.411, ElementType.TransitionMetal, PhysicalState.Solid, 12, 5);
            AddElement(49, "In", "Indium", 114.818, ElementType.BasicMetal, PhysicalState.Solid, 13, 5);
            AddElement(50, "Sn", "Tin", 118.710, ElementType.BasicMetal, PhysicalState.Solid, 14, 5);
            AddElement(51, "Sb", "Antimony", 121.760, ElementType.Semimetal, PhysicalState.Solid, 15, 5);
            AddElement(52, "Te", "Tellurium", 127.600, ElementType.Semimetal, PhysicalState.Solid, 16, 5);
            AddElement(53, "I", "Iodine", 126.904, ElementType.Halogen, PhysicalState.Solid, 17, 5);
            AddElement(54, "Xe", "Xenon", 131.293, ElementType.NobleGas, PhysicalState.Gas, 18, 5);

            // Period 6
            AddElement(55, "Cs", "Cesium", 132.905, ElementType.AlkaliMetal, PhysicalState.Solid, 1, 6);
            AddElement(56, "Ba", "Barium", 137.328, ElementType.AlkalineEarth, PhysicalState.Solid, 2, 6);

            // Lanthanides (Period 6, f-block)
            AddElement(57, "La", "Lanthanum", 138.905, ElementType.Lanthanide, PhysicalState.Solid, 3, 8); // Row 8 for display
            AddElement(58, "Ce", "Cerium", 140.116, ElementType.Lanthanide, PhysicalState.Solid, 4, 8);
            AddElement(59, "Pr", "Praseodymium", 140.908, ElementType.Lanthanide, PhysicalState.Solid, 5, 8);
            AddElement(60, "Nd", "Neodymium", 144.242, ElementType.Lanthanide, PhysicalState.Solid, 6, 8);
            AddElement(61, "Pm", "Promethium", 144.913, ElementType.Lanthanide, PhysicalState.Solid, 7, 8);
            AddElement(62, "Sm", "Samarium", 150.360, ElementType.Lanthanide, PhysicalState.Solid, 8, 8);
            AddElement(63, "Eu", "Europium", 151.964, ElementType.Lanthanide, PhysicalState.Solid, 9, 8);
            AddElement(64, "Gd", "Gadolinium", 157.250, ElementType.Lanthanide, PhysicalState.Solid, 10, 8);
            AddElement(65, "Tb", "Terbium", 158.925, ElementType.Lanthanide, PhysicalState.Solid, 11, 8);
            AddElement(66, "Dy", "Dysprosium", 162.500, ElementType.Lanthanide, PhysicalState.Solid, 12, 8);
            AddElement(67, "Ho", "Holmium", 164.930, ElementType.Lanthanide, PhysicalState.Solid, 13, 8);
            AddElement(68, "Er", "Erbium", 167.259, ElementType.Lanthanide, PhysicalState.Solid, 14, 8);
            AddElement(69, "Tm", "Thulium", 168.934, ElementType.Lanthanide, PhysicalState.Solid, 15, 8);
            AddElement(70, "Yb", "Ytterbium", 173.054, ElementType.Lanthanide, PhysicalState.Solid, 16, 8);
            AddElement(71, "Lu", "Lutetium", 174.967, ElementType.Lanthanide, PhysicalState.Solid, 17, 8);

            // Period 6 continued
            AddElement(72, "Hf", "Hafnium", 178.490, ElementType.TransitionMetal, PhysicalState.Solid, 4, 6);
            AddElement(73, "Ta", "Tantalum", 180.948, ElementType.TransitionMetal, PhysicalState.Solid, 5, 6);
            AddElement(74, "W", "Tungsten", 183.840, ElementType.TransitionMetal, PhysicalState.Solid, 6, 6);
            AddElement(75, "Re", "Rhenium", 186.207, ElementType.TransitionMetal, PhysicalState.Solid, 7, 6);
            AddElement(76, "Os", "Osmium", 190.230, ElementType.TransitionMetal, PhysicalState.Solid, 8, 6);
            AddElement(77, "Ir", "Iridium", 192.217, ElementType.TransitionMetal, PhysicalState.Solid, 9, 6);
            AddElement(78, "Pt", "Platinum", 195.085, ElementType.TransitionMetal, PhysicalState.Solid, 10, 6);
            AddElement(79, "Au", "Gold", 196.967, ElementType.TransitionMetal, PhysicalState.Solid, 11, 6);
            AddElement(80, "Hg", "Mercury", 200.592, ElementType.TransitionMetal, PhysicalState.Liquid, 12, 6);
            AddElement(81, "Tl", "Thallium", 204.383, ElementType.BasicMetal, PhysicalState.Solid, 13, 6);
            AddElement(82, "Pb", "Lead", 207.200, ElementType.BasicMetal, PhysicalState.Solid, 14, 6);
            AddElement(83, "Bi", "Bismuth", 208.980, ElementType.BasicMetal, PhysicalState.Solid, 15, 6);
            AddElement(84, "Po", "Polonium", 208.982, ElementType.BasicMetal, PhysicalState.Solid, 16, 6);
            AddElement(85, "At", "Astatine", 209.987, ElementType.Halogen, PhysicalState.Solid, 17, 6);
            AddElement(86, "Rn", "Radon", 222.018, ElementType.NobleGas, PhysicalState.Gas, 18, 6);

            // Period 7
            AddElement(87, "Fr", "Francium", 223.020, ElementType.AlkaliMetal, PhysicalState.Solid, 1, 7);
            AddElement(88, "Ra", "Radium", 226.025, ElementType.AlkalineEarth, PhysicalState.Solid, 2, 7);

            // Actinides (Period 7, f-block)
            AddElement(89, "Ac", "Actinium", 227.028, ElementType.Actinide, PhysicalState.Solid, 3, 9); // Row 9 for display
            AddElement(90, "Th", "Thorium", 232.038, ElementType.Actinide, PhysicalState.Solid, 4, 9);
            AddElement(91, "Pa", "Protactinium", 231.036, ElementType.Actinide, PhysicalState.Solid, 5, 9);
            AddElement(92, "U", "Uranium", 238.029, ElementType.Actinide, PhysicalState.Solid, 6, 9);
            AddElement(93, "Np", "Neptunium", 237.048, ElementType.Actinide, PhysicalState.Solid, 7, 9);
            AddElement(94, "Pu", "Plutonium", 244.064, ElementType.Actinide, PhysicalState.Solid, 8, 9);
            AddElement(95, "Am", "Americium", 243.061, ElementType.Actinide, PhysicalState.Solid, 9, 9);
            AddElement(96, "Cm", "Curium", 247.070, ElementType.Actinide, PhysicalState.Solid, 10, 9);
            AddElement(97, "Bk", "Berkelium", 247.070, ElementType.Actinide, PhysicalState.Solid, 11, 9);
            AddElement(98, "Cf", "Californium", 251.080, ElementType.Actinide, PhysicalState.Solid, 12, 9);
            AddElement(99, "Es", "Einsteinium", 252.083, ElementType.Actinide, PhysicalState.Solid, 13, 9);
            AddElement(100, "Fm", "Fermium", 257.095, ElementType.Actinide, PhysicalState.Solid, 14, 9);
            AddElement(101, "Md", "Mendelevium", 258.100, ElementType.Actinide, PhysicalState.Solid, 15, 9);
            AddElement(102, "No", "Nobelium", 259.101, ElementType.Actinide, PhysicalState.Solid, 16, 9);
            AddElement(103, "Lr", "Lawrencium", 262.110, ElementType.Actinide, PhysicalState.Solid, 17, 9);

            // Period 7 continued
            AddElement(104, "Rf", "Rutherfordium", 267.122, ElementType.TransitionMetal, PhysicalState.Unknown, 4, 7);
            AddElement(105, "Db", "Dubnium", 268.126, ElementType.TransitionMetal, PhysicalState.Unknown, 5, 7);
            AddElement(106, "Sg", "Seaborgium", 271.134, ElementType.TransitionMetal, PhysicalState.Unknown, 6, 7);
            AddElement(107, "Bh", "Bohrium", 272.138, ElementType.TransitionMetal, PhysicalState.Unknown, 7, 7);
            AddElement(108, "Hs", "Hassium", 270.134, ElementType.TransitionMetal, PhysicalState.Unknown, 8, 7);
            AddElement(109, "Mt", "Meitnerium", 276.151, ElementType.TransitionMetal, PhysicalState.Unknown, 9, 7);
            AddElement(110, "Ds", "Darmstadtium", 281.165, ElementType.TransitionMetal, PhysicalState.Unknown, 10, 7);
            AddElement(111, "Rg", "Roentgenium", 280.165, ElementType.TransitionMetal, PhysicalState.Unknown, 11, 7);
            AddElement(112, "Cn", "Copernicium", 285.177, ElementType.TransitionMetal, PhysicalState.Unknown, 12, 7);
            AddElement(113, "Nh", "Nihonium", 284.178, ElementType.BasicMetal, PhysicalState.Unknown, 13, 7);
            AddElement(114, "Fl", "Flerovium", 289.190, ElementType.BasicMetal, PhysicalState.Unknown, 14, 7);
            AddElement(115, "Mc", "Moscovium", 288.192, ElementType.BasicMetal, PhysicalState.Unknown, 15, 7);
            AddElement(116, "Lv", "Livermorium", 293.205, ElementType.BasicMetal, PhysicalState.Unknown, 16, 7);
            AddElement(117, "Ts", "Tennessine", 294.211, ElementType.Halogen, PhysicalState.Unknown, 17, 7);
            AddElement(118, "Og", "Oganesson", 294.214, ElementType.NobleGas, PhysicalState.Unknown, 18, 7);
        }

        /// <summary>
        /// Helper method to add an element
        /// </summary>
        private static void AddElement(int atomicNumber, string symbol, string name, double atomicMass,
                                      ElementType elementType, PhysicalState standardState, int group, int period)
        {
            string color = ElementTypeHelper.GetStandardColor(elementType);

            var element = new Element(atomicNumber, symbol, name, atomicMass, elementType, standardState, group, period, color);

            _elements.Add(atomicNumber, element);
            _elementsBySymbol.Add(symbol.ToUpper(), element);
        }
    }
}