using System;
using System.Collections.Generic;
using System.Linq;

namespace ChemCalc.ChemEngine
{
    /// <summary>
    /// Represents a complete chemical equation with reactants and products
    /// </summary>
    public class ChemicalEquation
    {
        // ==================== PROPERTIES ====================

        /// <summary>
        /// Reactant molecules (left side of equation)
        /// </summary>
        public List<MoleculeBuilder> Reactants { get; set; }

        /// <summary>
        /// Product molecules (right side of equation)
        /// </summary>
        public List<MoleculeBuilder> Products { get; set; }

        /// <summary>
        /// Equation type (Synthesis, Decomposition, etc.)
        /// </summary>
        public ReactionType Type { get; set; }

        /// <summary>
        /// Is the equation balanced?
        /// </summary>
        public bool IsBalanced { get; set; }

        // ==================== CONSTRUCTOR ====================

        public ChemicalEquation()
        {
            Reactants = new List<MoleculeBuilder>();
            Products = new List<MoleculeBuilder>();
            Type = ReactionType.Unknown;
            IsBalanced = false;
        }

        // ==================== ADD MOLECULES ====================

        /// <summary>
        /// Add a reactant to the equation
        /// </summary>
        public void AddReactant(MoleculeBuilder molecule)
        {
            if (molecule != null && !molecule.IsEmpty())
            {
                Reactants.Add(molecule.Clone());
            }
        }

        /// <summary>
        /// Add a product to the equation
        /// </summary>
        public void AddProduct(MoleculeBuilder molecule)
        {
            if (molecule != null && !molecule.IsEmpty())
            {
                Products.Add(molecule.Clone());
            }
        }

        /// <summary>
        /// Clear all reactants and products
        /// </summary>
        public void Clear()
        {
            Reactants.Clear();
            Products.Clear();
            IsBalanced = false;
        }

        // ==================== EQUATION STRING GENERATION ====================

        /// <summary>
        /// Get full equation string (e.g., "NaCl (s) + H₂O (l) → Na⁺ (aq) + Cl⁻ (aq)")
        /// </summary>
        public string GetEquationString()
        {
            if (Reactants.Count == 0 && Products.Count == 0)
                return "";

            string reactantsStr = GetMoleculesString(Reactants);
            string productsStr = GetMoleculesString(Products);

            if (string.IsNullOrEmpty(reactantsStr) && string.IsNullOrEmpty(productsStr))
                return "";

            if (string.IsNullOrEmpty(productsStr))
                return reactantsStr;

            if (string.IsNullOrEmpty(reactantsStr))
                return productsStr;

            return $"{reactantsStr} → {productsStr}";
        }

        /// <summary>
        /// Get formatted equation with coefficients
        /// </summary>
        public string GetBalancedEquationString()
        {
            // For v1, just return the basic equation
            // v2 will add auto-balancing with coefficients
            return GetEquationString();
        }

        /// <summary>
        /// Helper: Convert list of molecules to string
        /// </summary>
        private string GetMoleculesString(List<MoleculeBuilder> molecules)
        {
            if (molecules.Count == 0)
                return "";

            var formulaStrings = molecules.Select(m => m.GetFormulaWithState()).ToList();
            return string.Join(" + ", formulaStrings);
        }

        // ==================== EQUATION ANALYSIS ====================

        /// <summary>
        /// Get total molar mass of reactants
        /// </summary>
        public double GetReactantsMolarMass()
        {
            return Reactants.Sum(r => r.GetMolarMass());
        }

        /// <summary>
        /// Get total molar mass of products
        /// </summary>
        public double GetProductsMolarMass()
        {
            return Products.Sum(p => p.GetMolarMass());
        }

        /// <summary>
        /// Check if equation is valid (has both reactants and products)
        /// </summary>
        public bool IsValid()
        {
            return Reactants.Count > 0 && Products.Count > 0;
        }

        /// <summary>
        /// Get human-readable reaction type
        /// </summary>
        public string GetReactionTypeString()
        {
            switch (Type)
            {
                case ReactionType.Synthesis:
                    return "Synthesis";
                case ReactionType.Decomposition:
                    return "Decomposition";
                case ReactionType.SingleReplacement:
                    return "Single Replacement";
                case ReactionType.DoubleReplacement:
                    return "Double Replacement";
                case ReactionType.Combustion:
                    return "Combustion";
                case ReactionType.AcidBase:
                    return "Acid-Base";
                case ReactionType.Redox:
                    return "Redox";
                case ReactionType.Dissociation:
                    return "Dissociation";
                default:
                    return "Unknown";
            }
        }

        // ==================== DISPLAY ====================

        public override string ToString()
        {
            return GetEquationString();
        }

        /// <summary>
        /// Get detailed equation information
        /// </summary>
        public string GetDetailedString()
        {
            string equation = GetEquationString();
            string type = GetReactionTypeString();
            string balanced = IsBalanced ? "Balanced" : "Not Balanced";

            return $"{equation}\nType: {type}\nStatus: {balanced}";
        }
    }

    /// <summary>
    /// Types of chemical reactions
    /// </summary>
    public enum ReactionType
    {
        Unknown,
        Synthesis,           // A + B → AB
        Decomposition,       // AB → A + B
        SingleReplacement,   // A + BC → AC + B
        DoubleReplacement,   // AB + CD → AD + CB
        Combustion,          // CxHy + O₂ → CO₂ + H₂O
        AcidBase,           // Acid + Base → Salt + Water
        Redox,              // Oxidation-Reduction
        Dissociation        // NaCl → Na⁺ + Cl⁻
    }
}