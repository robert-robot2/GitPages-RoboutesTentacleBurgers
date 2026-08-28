using ChemCalc.Data;
using ChemCalc.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChemCalc.ChemEngine
{
    /// <summary>
    /// Builds molecules from selected elements
    /// </summary>
    public class MoleculeBuilder
    {
        // ==================== PROPERTIES ====================

        /// <summary>
        /// Dictionary of elements and their counts in the molecule
        /// Key: Element, Value: Count
        /// </summary>
        private Dictionary<Element, int> _composition;

        /// <summary>
        /// Physical state of the molecule
        /// </summary>
        public MoleculeState State { get; set; }

        /// <summary>
        /// Ionic charge (0 for neutral, +1, -1, etc.)
        /// </summary>
        public int Charge { get; set; }

        // ==================== CONSTRUCTOR ====================

        public MoleculeBuilder()
        {
            _composition = new Dictionary<Element, int>();
            State = MoleculeState.Unspecified;
            Charge = 0;
        }

        // ==================== ADD/REMOVE ELEMENTS ====================

        /// <summary>
        /// Add an element to the molecule
        /// </summary>
        public void AddElement(Element element, int count = 1)
        {
            if (element == null)
                return;

            if (_composition.ContainsKey(element))
            {
                _composition[element] += count;
            }
            else
            {
                _composition.Add(element, count);
            }
        }

        /// <summary>
        /// Remove one instance of an element
        /// </summary>
        public void RemoveElement(Element element)
        {
            if (element == null || !_composition.ContainsKey(element))
                return;

            _composition[element]--;

            if (_composition[element] <= 0)
            {
                _composition.Remove(element);
            }
        }

        /// <summary>
        /// Clear all elements
        /// </summary>
        public void Clear()
        {
            _composition.Clear();
            State = MoleculeState.Unspecified;
            Charge = 0;
        }

        /// <summary>
        /// Check if molecule has any elements
        /// </summary>
        public bool IsEmpty()
        {
            return _composition.Count == 0;
        }

        // ==================== FORMULA GENERATION ====================

        /// <summary>
        /// Get chemical formula string (e.g., "H₂O", "NaCl")
        /// </summary>
        public string GetFormula()
        {
            if (IsEmpty())
                return "";

            // Sort elements by: Carbon first, then Hydrogen, then alphabetically
            var sortedElements = _composition.Keys
                .OrderBy(e => GetElementSortOrder(e))
                .ThenBy(e => e.Symbol)  // Secondary sort by symbol alphabetically
                .ToList();

            string formula = "";
            foreach (var element in sortedElements)
            {
                formula += element.Symbol;

                int count = _composition[element];
                if (count > 1)
                {
                    formula += ChemicalFormatter.ToSubscript(count.ToString());
                }
            }

            // Add charge if present
            if (Charge != 0)
            {
                formula += ChemicalFormatter.ToSuperscript(Charge);
            }

            return formula;
        }

        /// <summary>
        /// Get formula with state notation (e.g., "H₂O (l)")
        /// </summary>
        public string GetFormulaWithState()
        {
            string formula = GetFormula();

            if (State != MoleculeState.Unspecified)
            {
                formula += " " + GetStateNotation();
            }

            return formula;
        }

        /// <summary>
        /// Get state notation string
        /// </summary>
        private string GetStateNotation()
        {
            switch (State)
            {
                case MoleculeState.Solid:
                    return "(s)";
                case MoleculeState.Liquid:
                    return "(l)";
                case MoleculeState.Gas:
                    return "(g)";
                case MoleculeState.Aqueous:
                    return "(aq)";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Determine sort order for elements (Hill system + ionic)
        /// Priority: 1) Carbon, 2) Hydrogen, 3) Metals (cations), 4) Nonmetals (anions)
        /// </summary>
        private int GetElementSortOrder(Element element)
        {
            // Special cases: Carbon and Hydrogen (Hill system)
            if (element.Symbol == "C")
                return 0;
            if (element.Symbol == "H")
                return 1;

            // Ionic compounds: Metals before nonmetals
            if (element.IsMetal())
                return 2; // Metals (cations) first
            else
                return 3; // Nonmetals (anions) second
        }

        // ==================== CALCULATIONS ====================

        /// <summary>
        /// Calculate molar mass of the molecule
        /// </summary>
        public double GetMolarMass()
        {
            double mass = 0;

            foreach (var kvp in _composition)
            {
                mass += kvp.Key.AtomicMass * kvp.Value;
            }

            return mass;
        }

        /// <summary>
        /// Get formatted molar mass string
        /// </summary>
        public string GetFormattedMolarMass()
        {
            return $"{GetMolarMass():F3} g/mol";
        }

        /// <summary>
        /// Get number of atoms in molecule
        /// </summary>
        public int GetAtomCount()
        {
            return _composition.Values.Sum();
        }

        /// <summary>
        /// Get element composition breakdown
        /// </summary>
        public Dictionary<Element, int> GetComposition()
        {
            return new Dictionary<Element, int>(_composition);
        }

        // ==================== CLONE ====================

        /// <summary>
        /// Create a copy of this molecule
        /// </summary>
        public MoleculeBuilder Clone()
        {
            var clone = new MoleculeBuilder();
            clone._composition = new Dictionary<Element, int>(_composition);
            clone.State = this.State;
            clone.Charge = this.Charge;
            return clone;
        }

        // ==================== DISPLAY ====================

        public override string ToString()
        {
            return GetFormulaWithState();
        }
    }

    /// <summary>
    /// Physical state of a molecule
    /// </summary>
    public enum MoleculeState
    {
        Unspecified,
        Solid,      // (s)
        Liquid,     // (l)
        Gas,        // (g)
        Aqueous     // (aq) - dissolved in water
    }
}