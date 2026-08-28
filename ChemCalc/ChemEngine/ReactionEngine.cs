using System;
using System.Collections.Generic;
using System.Linq;
using ChemCalc.Data;

namespace ChemCalc.ChemEngine
{
    /// <summary>
    /// Processes chemical reactions and generates products
    /// v1: Simple dissociation and basic reactions
    /// v2: Complex balancing and reaction prediction
    /// </summary>
    public class ReactionEngine
    {
        // ==================== DISSOCIATION REACTIONS ====================

        /// <summary>
        /// Process ionic dissociation in water
        /// Example: NaCl (s) + H₂O (l) → Na⁺ (aq) + Cl⁻ (aq)
        /// </summary>
        public ChemicalEquation ProcessDissociation(MoleculeBuilder salt)
        {
            var equation = new ChemicalEquation();
            equation.Type = ReactionType.Dissociation;

            if (salt == null || salt.IsEmpty())
                return equation;

            // Add salt as reactant
            equation.AddReactant(salt);

            // Simple dissociation logic for common salts
            var composition = salt.GetComposition();

            if (composition.Count == 2)
            {
                // Binary salt (e.g., NaCl, MgCl₂)
                var elements = composition.Keys.ToList();

                // Create cation (metal, positive)
                var cation = new MoleculeBuilder();
                cation.AddElement(elements[0], composition[elements[0]]);
                cation.Charge = composition[elements[0]]; // Simplified charge assignment
                cation.State = MoleculeState.Aqueous;

                // Create anion (nonmetal, negative)
                var anion = new MoleculeBuilder();
                anion.AddElement(elements[1], composition[elements[1]]);
                anion.Charge = -composition[elements[1]]; // Simplified charge assignment
                anion.State = MoleculeState.Aqueous;

                equation.AddProduct(cation);
                equation.AddProduct(anion);
            }

            return equation;
        }

        /// <summary>
        /// Process acid dissociation
        /// Example: HCl (aq) → H⁺ (aq) + Cl⁻ (aq)
        /// </summary>
        public ChemicalEquation ProcessAcidDissociation(MoleculeBuilder acid)
        {
            var equation = new ChemicalEquation();
            equation.Type = ReactionType.AcidBase;

            if (acid == null || acid.IsEmpty())
                return equation;

            equation.AddReactant(acid);

            // Check if it contains Hydrogen
            var composition = acid.GetComposition();
            var hydrogen = PeriodicTable.GetElement("H");

            if (composition.ContainsKey(hydrogen))
            {
                // Create H⁺ ion
                var hIon = new MoleculeBuilder();
                hIon.AddElement(hydrogen, 1);
                hIon.Charge = 1;
                hIon.State = MoleculeState.Aqueous;
                equation.AddProduct(hIon);

                // Create anion (remainder)
                var anion = new MoleculeBuilder();
                foreach (var kvp in composition)
                {
                    if (kvp.Key != hydrogen)
                    {
                        anion.AddElement(kvp.Key, kvp.Value);
                    }
                    else if (kvp.Value > 1)
                    {
                        anion.AddElement(kvp.Key, kvp.Value - 1);
                    }
                }
                anion.Charge = -1;
                anion.State = MoleculeState.Aqueous;
                equation.AddProduct(anion);
            }

            return equation;
        }

        // ==================== SYNTHESIS REACTIONS ====================

        /// <summary>
        /// Combine two molecules into one (synthesis)
        /// Example: 2H₂ + O₂ → 2H₂O
        /// </summary>
        public ChemicalEquation ProcessSynthesis(MoleculeBuilder reactant1, MoleculeBuilder reactant2)
        {
            var equation = new ChemicalEquation();
            equation.Type = ReactionType.Synthesis;

            if (reactant1 == null || reactant1.IsEmpty() || reactant2 == null || reactant2.IsEmpty())
                return equation;

            equation.AddReactant(reactant1);
            equation.AddReactant(reactant2);

            // Combine all elements into product
            var product = new MoleculeBuilder();

            foreach (var kvp in reactant1.GetComposition())
            {
                product.AddElement(kvp.Key, kvp.Value);
            }

            foreach (var kvp in reactant2.GetComposition())
            {
                product.AddElement(kvp.Key, kvp.Value);
            }

            equation.AddProduct(product);

            return equation;
        }

        // ==================== COMBUSTION REACTIONS ====================

        /// <summary>
        /// Combust a hydrocarbon with oxygen
        /// Example: CH₄ + 2O₂ → CO₂ + 2H₂O
        /// </summary>
        public ChemicalEquation ProcessCombustion(MoleculeBuilder hydrocarbon)
        {
            var equation = new ChemicalEquation();
            equation.Type = ReactionType.Combustion;

            if (hydrocarbon == null || hydrocarbon.IsEmpty())
                return equation;

            var composition = hydrocarbon.GetComposition();

            // Check for C and H
            var carbon = PeriodicTable.GetElement("C");
            var hydrogen = PeriodicTable.GetElement("H");
            var oxygen = PeriodicTable.GetElement("O");

            if (!composition.ContainsKey(carbon) || !composition.ContainsKey(hydrogen))
                return equation; // Not a hydrocarbon

            equation.AddReactant(hydrocarbon);

            // Add O₂ as reactant
            var o2 = new MoleculeBuilder();
            o2.AddElement(oxygen, 2);
            o2.State = MoleculeState.Gas;
            equation.AddReactant(o2);

            // Create CO₂ product
            var co2 = new MoleculeBuilder();
            co2.AddElement(carbon, 1);
            co2.AddElement(oxygen, 2);
            co2.State = MoleculeState.Gas;
            equation.AddProduct(co2);

            // Create H₂O product
            var h2o = new MoleculeBuilder();
            h2o.AddElement(hydrogen, 2);
            h2o.AddElement(oxygen, 1);
            h2o.State = MoleculeState.Liquid;
            equation.AddProduct(h2o);

            return equation;
        }

        // ==================== DECOMPOSITION REACTIONS ====================

        /// <summary>
        /// Decompose a compound into elements (simplified)
        /// Example: 2H₂O → 2H₂ + O₂
        /// </summary>
        public ChemicalEquation ProcessDecomposition(MoleculeBuilder compound)
        {
            var equation = new ChemicalEquation();
            equation.Type = ReactionType.Decomposition;

            if (compound == null || compound.IsEmpty())
                return equation;

            equation.AddReactant(compound);

            // Split into elemental molecules
            var composition = compound.GetComposition();

            foreach (var kvp in composition)
            {
                var element = kvp.Key;
                var product = new MoleculeBuilder();

                // Most elements exist as diatomic molecules (H₂, O₂, N₂, etc.)
                if (IsDiatomic(element))
                {
                    product.AddElement(element, 2);
                    product.State = ConvertPhysicalStateToMoleculeState(element.StandardState);
                }
                else
                {
                    product.AddElement(element, 1);
                    product.State = ConvertPhysicalStateToMoleculeState(element.StandardState);
                }

                equation.AddProduct(product);
            }

            return equation;
        }

        /// <summary>
        /// Check if element is naturally diatomic
        /// </summary>
        private bool IsDiatomic(Element element)
        {
            // H₂, N₂, O₂, F₂, Cl₂, Br₂, I₂
            string[] diatomic = { "H", "N", "O", "F", "Cl", "Br", "I" };
            return diatomic.Contains(element.Symbol);
        }

        /// <summary>
        /// Convert PhysicalState to MoleculeState
        /// </summary>
        private MoleculeState ConvertPhysicalStateToMoleculeState(PhysicalState state)
        {
            switch (state)
            {
                case PhysicalState.Solid:
                    return MoleculeState.Solid;
                case PhysicalState.Liquid:
                    return MoleculeState.Liquid;
                case PhysicalState.Gas:
                    return MoleculeState.Gas;
                default:
                    return MoleculeState.Unspecified;
            }
        }

        // ==================== REACTION PREDICTION (v2) ====================

        /// <summary>
        /// Predict products based on reactants (placeholder for v2)
        /// </summary>
        public ChemicalEquation PredictReaction(List<MoleculeBuilder> reactants)
        {
            // TODO: Implement in v2 with advanced logic
            // - Detect reaction type
            // - Apply reaction rules
            // - Balance equation
            throw new NotImplementedException("Advanced reaction prediction will be added in v2");
        }

        // ==================== EQUATION BALANCING (v2) ====================

        /// <summary>
        /// Balance a chemical equation (placeholder for v2)
        /// </summary>
        public ChemicalEquation BalanceEquation(ChemicalEquation equation)
        {
            // TODO: Implement in v2
            // - Use matrix method or algebraic balancing
            // - Find coefficients
            throw new NotImplementedException("Equation balancing will be added in v2");
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Check if a molecule is an acid (simplified)
        /// </summary>
        public bool IsAcid(MoleculeBuilder molecule)
        {
            if (molecule == null || molecule.IsEmpty())
                return false;

            var composition = molecule.GetComposition();
            var hydrogen = PeriodicTable.GetElement("H");

            // Simple check: contains hydrogen
            return composition.ContainsKey(hydrogen);
        }

        /// <summary>
        /// Check if a molecule is a salt (simplified)
        /// </summary>
        public bool IsSalt(MoleculeBuilder molecule)
        {
            if (molecule == null || molecule.IsEmpty())
                return false;

            var composition = molecule.GetComposition();

            // Simple check: contains a metal and a nonmetal
            bool hasMetal = composition.Keys.Any(e => e.IsMetal());
            bool hasNonmetal = composition.Keys.Any(e => e.IsNonmetal());

            return hasMetal && hasNonmetal;
        }

        /// <summary>
        /// Determine likely reaction type from reactants (simplified)
        /// </summary>
        public ReactionType DetermineReactionType(List<MoleculeBuilder> reactants)
        {
            if (reactants == null || reactants.Count == 0)
                return ReactionType.Unknown;

            if (reactants.Count == 1)
                return ReactionType.Decomposition;

            if (reactants.Count == 2)
            {
                var r1 = reactants[0];
                var r2 = reactants[1];

                // Check for combustion (hydrocarbon + O₂)
                var oxygen = PeriodicTable.GetElement("O");
                bool hasO2 = r2.GetComposition().ContainsKey(oxygen) &&
                            r2.GetComposition()[oxygen] == 2 &&
                            r2.GetComposition().Count == 1;

                if (hasO2)
                    return ReactionType.Combustion;

                // Simple synthesis
                return ReactionType.Synthesis;
            }

            return ReactionType.Unknown;
        }
    }
}