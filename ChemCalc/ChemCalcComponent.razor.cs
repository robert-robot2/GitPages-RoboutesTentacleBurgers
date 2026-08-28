using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using ChemCalc.Data;
using ChemCalc.ChemEngine;

namespace ChemCalc
{
    public partial class ChemCalcComponent : ComponentBase
    {
        // ==================== STATE ====================

        private Element selectedElement = null;
        private MoleculeBuilder currentMolecule = new MoleculeBuilder();
        private ChemicalEquation currentEquation = new ChemicalEquation();
        private MoleculeState currentState = MoleculeState.Unspecified;
        private ElementType? highlightedCategory = null;
        private ReactionEngine reactionEngine = new ReactionEngine();
        private ChemicalEquationBuilder equationBuilder = new ChemicalEquationBuilder();
        private bool isChemMode = false;
        // ==================== ELEMENT SELECTION ====================

        /// <summary>
        /// Handle element click from periodic table
        /// </summary>
        private void HandleElementSelected(Element element)
        {
            if (element == null)
                return;

            // Update selected element for display
            selectedElement = element;

            // Add element to current molecule
            currentMolecule.AddElement(element, 1);

            // Update state
            StateHasChanged();
        }

        // ==================== CATEGORY HIGHLIGHTING ====================

        /// <summary>
        /// Handle category selection for highlighting
        /// </summary>
        private void HandleCategorySelected(ElementType? category)
        {
            highlightedCategory = category;
            StateHasChanged();
        }

        // ==================== STATE SELECTION ====================

        /// <summary>
        /// Handle state selection (s, l, g, aq)
        /// </summary>
        private void HandleStateSelected(MoleculeState state)
        {
            currentState = state;

            // Apply state to current molecule
            if (!currentMolecule.IsEmpty())
            {
                currentMolecule.State = state;
            }

            StateHasChanged();
        }

        // ==================== CHEM BUTTON TAB HANDLERS ====================

        /// <summary>
        /// Handle "Add Molecule" button click
        /// Transfers current molecule to equation builder
        /// </summary>
        private void HandleAddMolecule()
        {
            if (currentMolecule.IsEmpty())
                return;

            // Apply current state to molecule before adding
            currentMolecule.State = currentState;

            // Add molecule to equation builder
            equationBuilder.AddMolecule(currentMolecule);

            // Clear the molecule builder for next molecule
            currentMolecule.Clear();
            currentState = MoleculeState.Unspecified;

            StateHasChanged();
        }

        /// <summary>
        /// Handle "Clear Molecule" button click
        /// Clears only the current molecule builder
        /// </summary>
        private void HandleClearMolecule()
        {
            currentMolecule.Clear();
            currentState = MoleculeState.Unspecified;
            StateHasChanged();
        }

        /// <summary>
        /// Handle "Clear Equation" button click
        /// Clears the entire chemical equation
        /// </summary>
        private void HandleClearEquation()
        {
            equationBuilder.ClearEquation();
            StateHasChanged();
        }

        /// <summary>
        /// Handle "Mode Toggle" button click
        /// Switches between CALC and CHEM modes
        /// </summary>
        private void HandleToggleMode()
        {
            isChemMode = !isChemMode;
            StateHasChanged();
        }

        /// <summary>
        /// Get current mode display string
        /// </summary>
        private string GetModeDisplay()
        {
            return isChemMode ? "CHEM" : "CALC";
        }

        /// <summary>
        /// Handle chemical operator from calculator (e.g., + in CHEM mode)
        /// </summary>
        /// <summary>
        /// Handle chemical operator from calculator (e.g., + in CHEM mode)
        /// </summary>
        private void HandleChemOperator(string operatorSymbol)
        {
            if (operatorSymbol == "=")
            {
                // Process the reaction
                equationBuilder.ProcessReaction();
            }
            else
            {
                // Add operator to equation
                equationBuilder.AddOperator(operatorSymbol);
            }
            StateHasChanged();
        }
        // ==================== MOLECULE OPERATIONS ====================

        /// <summary>
        /// Clear current molecule
        /// </summary>
        private void ClearMolecule()
        {
            currentMolecule.Clear();
            currentState = MoleculeState.Unspecified;
            StateHasChanged();
        }

        /// <summary>
        /// Add current molecule to equation as reactant
        /// </summary>
        private void AddAsReactant()
        {
            if (!currentMolecule.IsEmpty())
            {
                currentEquation.AddReactant(currentMolecule);
                ClearMolecule();
            }
        }

        /// <summary>
        /// Add current molecule to equation as product
        /// </summary>
        private void AddAsProduct()
        {
            if (!currentMolecule.IsEmpty())
            {
                currentEquation.AddProduct(currentMolecule);
                ClearMolecule();
            }
        }

        // ==================== EQUATION OPERATIONS ====================

        /// <summary>
        /// Clear entire equation
        /// </summary>
        private void ClearEquation()
        {
            currentEquation.Clear();
            ClearMolecule();
            StateHasChanged();
        }

        /// <summary>
        /// Process simple dissociation reaction
        /// Example: NaCl → Na⁺ + Cl⁻
        /// </summary>
        private void ProcessDissociation()
        {
            if (currentMolecule.IsEmpty())
                return;

            var equation = reactionEngine.ProcessDissociation(currentMolecule);
            currentEquation = equation;
            ClearMolecule();
            StateHasChanged();
        }

        /// <summary>
        /// Process combustion reaction
        /// Example: CH₄ + O₂ → CO₂ + H₂O
        /// </summary>
        private void ProcessCombustion()
        {
            if (currentMolecule.IsEmpty())
                return;

            var equation = reactionEngine.ProcessCombustion(currentMolecule);
            currentEquation = equation;
            ClearMolecule();
            StateHasChanged();
        }

        // ==================== INITIALIZATION ====================

        protected override void OnInitialized()
        {
            // Initialize with empty state
            currentMolecule = new MoleculeBuilder();
            currentEquation = new ChemicalEquation();
            reactionEngine = new ReactionEngine();
        }
    }
}