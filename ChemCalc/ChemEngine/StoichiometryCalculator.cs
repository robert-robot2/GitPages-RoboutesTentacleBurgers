using System;
using System.Collections.Generic;
using System.Linq;
using ChemCalc.Data;

namespace ChemCalc.ChemEngine
{
    /// <summary>
    /// Performs stoichiometric calculations for molecules and equations
    /// </summary>
    public static class StoichiometryCalculator
    {
        // ==================== MOLAR MASS CALCULATIONS ====================

        /// <summary>
        /// Calculate molar mass of a molecule
        /// </summary>
        public static double CalculateMolarMass(MoleculeBuilder molecule)
        {
            if (molecule == null || molecule.IsEmpty())
                return 0;

            return molecule.GetMolarMass();
        }

        /// <summary>
        /// Calculate molar mass from element composition
        /// </summary>
        public static double CalculateMolarMass(Dictionary<Element, int> composition)
        {
            if (composition == null || composition.Count == 0)
                return 0;

            double mass = 0;
            foreach (var kvp in composition)
            {
                mass += kvp.Key.AtomicMass * kvp.Value;
            }

            return mass;
        }

        // ==================== MOLE CONVERSIONS ====================

        /// <summary>
        /// Convert mass (grams) to moles
        /// </summary>
        public static double MassToMoles(double massGrams, double molarMass)
        {
            if (molarMass <= 0)
                throw new ArgumentException("Molar mass must be positive");

            return massGrams / molarMass;
        }

        /// <summary>
        /// Convert moles to mass (grams)
        /// </summary>
        public static double MolesToMass(double moles, double molarMass)
        {
            if (molarMass <= 0)
                throw new ArgumentException("Molar mass must be positive");

            return moles * molarMass;
        }

        /// <summary>
        /// Convert moles to number of particles (using Avogadro's number)
        /// </summary>
        public static double MolesToParticles(double moles)
        {
            const double AvogadroNumber = 6.02214076e23;
            return moles * AvogadroNumber;
        }

        /// <summary>
        /// Convert number of particles to moles
        /// </summary>
        public static double ParticlesToMoles(double particles)
        {
            const double AvogadroNumber = 6.02214076e23;
            return particles / AvogadroNumber;
        }

        // ==================== GAS LAW CALCULATIONS ====================

        /// <summary>
        /// Calculate moles using Ideal Gas Law (PV = nRT)
        /// P in atm, V in L, T in K
        /// </summary>
        public static double IdealGasLaw_Moles(double pressureAtm, double volumeL, double temperatureK)
        {
            const double R = 0.08206; // L·atm/(mol·K)

            if (temperatureK <= 0)
                throw new ArgumentException("Temperature must be positive");

            return (pressureAtm * volumeL) / (R * temperatureK);
        }

        /// <summary>
        /// Calculate volume using Ideal Gas Law
        /// </summary>
        public static double IdealGasLaw_Volume(double moles, double pressureAtm, double temperatureK)
        {
            const double R = 0.08206; // L·atm/(mol·K)

            if (pressureAtm <= 0)
                throw new ArgumentException("Pressure must be positive");
            if (temperatureK <= 0)
                throw new ArgumentException("Temperature must be positive");

            return (moles * R * temperatureK) / pressureAtm;
        }

        /// <summary>
        /// Calculate pressure using Ideal Gas Law
        /// </summary>
        public static double IdealGasLaw_Pressure(double moles, double volumeL, double temperatureK)
        {
            const double R = 0.08206; // L·atm/(mol·K)

            if (volumeL <= 0)
                throw new ArgumentException("Volume must be positive");
            if (temperatureK <= 0)
                throw new ArgumentException("Temperature must be positive");

            return (moles * R * temperatureK) / volumeL;
        }

        /// <summary>
        /// Calculate temperature using Ideal Gas Law
        /// </summary>
        public static double IdealGasLaw_Temperature(double moles, double pressureAtm, double volumeL)
        {
            const double R = 0.08206; // L·atm/(mol·K)

            if (moles <= 0)
                throw new ArgumentException("Moles must be positive");
            if (pressureAtm <= 0)
                throw new ArgumentException("Pressure must be positive");
            if (volumeL <= 0)
                throw new ArgumentException("Volume must be positive");

            return (pressureAtm * volumeL) / (moles * R);
        }

        // ==================== CONCENTRATION CALCULATIONS ====================

        /// <summary>
        /// Calculate molarity (M = mol/L)
        /// </summary>
        public static double CalculateMolarity(double moles, double volumeL)
        {
            if (volumeL <= 0)
                throw new ArgumentException("Volume must be positive");

            return moles / volumeL;
        }

        /// <summary>
        /// Calculate moles from molarity and volume
        /// </summary>
        public static double MolarityToMoles(double molarity, double volumeL)
        {
            return molarity * volumeL;
        }

        /// <summary>
        /// Dilution calculation: M1V1 = M2V2
        /// Calculate final molarity
        /// </summary>
        public static double Dilution_FinalMolarity(double initialM, double initialVolumeL, double finalVolumeL)
        {
            if (finalVolumeL <= 0)
                throw new ArgumentException("Final volume must be positive");

            return (initialM * initialVolumeL) / finalVolumeL;
        }

        /// <summary>
        /// Dilution calculation: M1V1 = M2V2
        /// Calculate final volume
        /// </summary>
        public static double Dilution_FinalVolume(double initialM, double initialVolumeL, double finalM)
        {
            if (finalM <= 0)
                throw new ArgumentException("Final molarity must be positive");

            return (initialM * initialVolumeL) / finalM;
        }

        // ==================== PERCENT COMPOSITION ====================

        /// <summary>
        /// Calculate mass percent of an element in a molecule
        /// </summary>
        public static double CalculateMassPercent(Element element, int count, double totalMolarMass)
        {
            if (totalMolarMass <= 0)
                return 0;

            double elementMass = element.AtomicMass * count;
            return (elementMass / totalMolarMass) * 100;
        }

        /// <summary>
        /// Calculate percent composition for all elements in a molecule
        /// </summary>
        public static Dictionary<Element, double> CalculatePercentComposition(MoleculeBuilder molecule)
        {
            var result = new Dictionary<Element, double>();

            if (molecule == null || molecule.IsEmpty())
                return result;

            double totalMass = molecule.GetMolarMass();
            var composition = molecule.GetComposition();

            foreach (var kvp in composition)
            {
                double percent = CalculateMassPercent(kvp.Key, kvp.Value, totalMass);
                result.Add(kvp.Key, percent);
            }

            return result;
        }

        // ==================== EMPIRICAL FORMULA ====================

        /// <summary>
        /// Calculate empirical formula from percent composition
        /// Returns element ratios (simplified to smallest whole numbers)
        /// </summary>
        public static Dictionary<Element, int> CalculateEmpiricalFormula(Dictionary<Element, double> percentComposition)
        {
            var result = new Dictionary<Element, int>();

            if (percentComposition == null || percentComposition.Count == 0)
                return result;

            // Convert percent to moles (assume 100g sample)
            var moles = new Dictionary<Element, double>();
            foreach (var kvp in percentComposition)
            {
                double molesOfElement = kvp.Value / kvp.Key.AtomicMass;
                moles.Add(kvp.Key, molesOfElement);
            }

            // Find smallest mole value
            double smallestMole = moles.Values.Min();

            // Divide all by smallest to get ratios
            var ratios = new Dictionary<Element, double>();
            foreach (var kvp in moles)
            {
                ratios.Add(kvp.Key, kvp.Value / smallestMole);
            }

            // Round to nearest integer (with tolerance for rounding errors)
            foreach (var kvp in ratios)
            {
                int roundedRatio = (int)Math.Round(kvp.Value);
                result.Add(kvp.Key, roundedRatio);
            }

            return result;
        }

        // ==================== LIMITING REAGENT (v2 feature) ====================

        /// <summary>
        /// Identify limiting reagent in a reaction (placeholder for v2)
        /// </summary>
        public static MoleculeBuilder IdentifyLimitingReagent(ChemicalEquation equation, Dictionary<MoleculeBuilder, double> availableMoles)
        {
            // TODO: Implement in v2
            throw new NotImplementedException("Limiting reagent calculation will be added in v2");
        }

        // ==================== HELPER METHODS ====================

        /// <summary>
        /// Format molar mass for display
        /// </summary>
        public static string FormatMolarMass(double molarMass)
        {
            return string.Format("{0:F3} g/mol", molarMass);
        }

        /// <summary>
        /// Format molarity for display
        /// </summary>
        public static string FormatMolarity(double molarity)
        {
            return string.Format("{0:F4} M", molarity);
        }

        /// <summary>
        /// Format percent for display
        /// </summary>
        public static string FormatPercent(double percent)
        {
            return string.Format("{0:F2}%", percent);
        }
    }
}