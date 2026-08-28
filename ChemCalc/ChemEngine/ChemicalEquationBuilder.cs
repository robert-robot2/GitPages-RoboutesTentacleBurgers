using System;
using System.Collections.Generic;
using System.Linq;
using ChemCalc.Data;

namespace ChemCalc.ChemEngine
{
    /// <summary>
    /// Manages chemical equation building in dual-mode calculator
    /// Handles string-based equation construction and reaction processing
    /// </summary>
    public class ChemicalEquationBuilder
    {
        // ==================== PROPERTIES ====================

        /// <summary>
        /// Raw equation string being built (e.g., "H₂O (l) + NaCl (s)")
        /// </summary>
        public string EquationString { get; private set; }

        /// <summary>
        /// List of reactant molecules (left side of arrow)
        /// </summary>
        private List<MoleculeBuilder> _reactants;

        /// <summary>
        /// Completed chemical equation (after processing with =)
        /// </summary>
        public ChemicalEquation CompletedEquation { get; private set; }

        /// <summary>
        /// Detected reaction type
        /// </summary>
        public ReactionType ReactionType { get; private set; }

        /// <summary>
        /// Has the equation been processed (= pressed)?
        /// </summary>
        public bool IsProcessed { get; private set; }

        /// <summary>
        /// Reaction engine for processing
        /// </summary>
        private ReactionEngine _reactionEngine;

        // ==================== CONSTRUCTOR ====================

        public ChemicalEquationBuilder()
        {
            EquationString = "";
            _reactants = new List<MoleculeBuilder>();
            CompletedEquation = new ChemicalEquation();
            ReactionType = ReactionType.Unknown;
            IsProcessed = false;
            _reactionEngine = new ReactionEngine();
        }

        // ==================== MOLECULE OPERATIONS ====================

        /// <summary>
        /// Add a molecule to the equation string
        /// </summary>
        public void AddMolecule(MoleculeBuilder molecule)
        {
            if (molecule == null || molecule.IsEmpty())
                return;

            // If equation is already processed, clear it first
            if (IsProcessed)
            {
                ClearEquation();
            }

            // Get molecule formula with state
            string moleculeString = molecule.GetFormulaWithState();

            // Add to equation string
            if (string.IsNullOrEmpty(EquationString))
            {
                EquationString = moleculeString;
            }
            else
            {
                // If last character is not a space or operator, this shouldn't happen
                // but we'll handle it gracefully
                EquationString += " " + moleculeString;
            }

            // Store molecule in reactants list for later processing
            _reactants.Add(molecule.Clone());
        }

        /// <summary>
        /// Add an operator to the equation string (e.g., "+")
        /// </summary>
        public void AddOperator(string operatorSymbol)
        {
            if (string.IsNullOrEmpty(EquationString))
                return;

            // If equation is already processed, don't allow adding operators
            if (IsProcessed)
                return;

            // Prevent consecutive operators (check if last char is already an operator)
            string trimmed = EquationString.TrimEnd();
            if (trimmed.EndsWith("+") || trimmed.EndsWith("-"))
                return;

            // Add operator with spaces
            EquationString += $" {operatorSymbol}";
        }

        // ==================== CLEAR OPERATIONS ====================

        /// <summary>
        /// Clear the entire equation
        /// </summary>
        public void ClearEquation()
        {
            EquationString = "";
            _reactants.Clear();
            CompletedEquation = new ChemicalEquation();
            ReactionType = ReactionType.Unknown;
            IsProcessed = false;
        }

        // ==================== REACTION PROCESSING ====================

        /// <summary>
        /// Process the equation (triggered by = in CHEM mode)
        /// Auto-detects reaction type and generates products
        /// </summary>
        public void ProcessReaction()
        {
            if (_reactants.Count == 0)
                return;

            // Already processed
            if (IsProcessed)
                return;

            // Determine reaction type and process
            if (_reactants.Count == 1)
            {
                // Single reactant - likely decomposition or dissociation
                var molecule = _reactants[0];

                // Check if it's a salt (contains metal + nonmetal)
                if (_reactionEngine.IsSalt(molecule))
                {
                    // Process as dissociation
                    CompletedEquation = _reactionEngine.ProcessDissociation(molecule);
                    ReactionType = ReactionType.Dissociation;
                }
                else
                {
                    // Process as decomposition
                    CompletedEquation = _reactionEngine.ProcessDecomposition(molecule);
                    ReactionType = ReactionType.Decomposition;
                }
            }
            else if (_reactants.Count == 2)
            {
                // Two reactants - check for combustion, dissociation, or synthesis
                var r1 = _reactants[0];
                var r2 = _reactants[1];

                // Check for water + salt dissociation (H₂O + NaCl)
                bool hasWater = IsWater(r1) || IsWater(r2);
                bool hasSalt = _reactionEngine.IsSalt(r1) || _reactionEngine.IsSalt(r2);
                if (hasWater && hasSalt)
                {
                    // Dissociation: salt dissolves in water
                    var salt = _reactionEngine.IsSalt(r1) ? r1 : r2;
                    var water = IsWater(r1) ? r1 : r2;

                    // Create equation with water as reactant
                    CompletedEquation = _reactionEngine.ProcessDissociation(salt);

                    // Manually add water to reactants in the completed equation
                    CompletedEquation.Reactants.Insert(0, water.Clone());

                    ReactionType = ReactionType.Dissociation;
                }
                else
                {
                    // Check for combustion (hydrocarbon + O₂)
                    var oxygen = PeriodicTable.GetElement("O");
                    bool hasO2 = (r2.GetComposition().ContainsKey(oxygen) &&
                                 r2.GetComposition()[oxygen] == 2 &&
                                 r2.GetComposition().Count == 1) ||
                                (r1.GetComposition().ContainsKey(oxygen) &&
                                 r1.GetComposition()[oxygen] == 2 &&
                                 r1.GetComposition().Count == 1);

                    if (hasO2)
                    {
                        // Determine which is the hydrocarbon
                        var hydrocarbon = hasO2 && r2.GetComposition().ContainsKey(oxygen) ? r1 : r2;
                        CompletedEquation = _reactionEngine.ProcessCombustion(hydrocarbon);
                        ReactionType = ReactionType.Combustion;
                    }
                    else
                    {
                        // Default to synthesis
                        CompletedEquation = _reactionEngine.ProcessSynthesis(r1, r2);
                        ReactionType = ReactionType.Synthesis;
                    }
                }
            }
            else
            {
                // More than 2 reactants - not supported in v1
                ReactionType = ReactionType.Unknown;
                return;
            }

            // Update equation string with arrow and products
            if (CompletedEquation.IsValid())
            {
                EquationString = CompletedEquation.GetEquationString();
                IsProcessed = true;
            }
        }

        // ==================== DISPLAY HELPERS ====================
      
        /// <summary>
        /// Check if molecule is water (H₂O)
        /// </summary>
        private bool IsWater(MoleculeBuilder molecule)
        {
            var composition = molecule.GetComposition();

            // Must have exactly 2 elements: H and O
            if (composition.Count != 2)
                return false;

            var hydrogen = PeriodicTable.GetElement("H");
            var oxygen = PeriodicTable.GetElement("O");

            // Check for H₂O
            return composition.ContainsKey(hydrogen) && composition[hydrogen] == 2 &&
                   composition.ContainsKey(oxygen) && composition[oxygen] == 1;
        }
        /// <summary>
        /// Get reaction type as display string
        /// </summary>
        public string GetReactionTypeString()
        {
            if (!IsProcessed || ReactionType == ReactionType.Unknown)
                return "None";

            return CompletedEquation.GetReactionTypeString();
        }

        /// <summary>
        /// Check if equation has content
        /// </summary>
        public bool HasContent()
        {
            return !string.IsNullOrEmpty(EquationString);
        }

        /// <summary>
        /// Get display string for current state
        /// </summary>
        public override string ToString()
        {
            return string.IsNullOrEmpty(EquationString) ? "Empty" : EquationString;
        }
    }
}