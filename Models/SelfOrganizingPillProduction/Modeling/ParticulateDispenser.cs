﻿using System;
using SafetySharp.Modeling;

namespace SafetySharp.CaseStudies.SelfOrganizingPillProduction.Modeling
{
    /// <summary>
    /// A production station that adds ingredients to the containers.
    /// </summary>
    public partial class ParticulateDispenser : Station
    {
        public readonly Fault DispenserDefect = new PermanentFault();

        private readonly IngredientTank[] ingredientTanks;

        public override Capability[] AvailableCapabilities
            => Array.ConvertAll(ingredientTanks, tank => tank.Capability);

        // for convenience
        public Fault BlueTankDepleted => ingredientTanks[(int)IngredientType.BlueParticulate].TankDepleted;
        public Fault RedTankDepleted => ingredientTanks[(int)IngredientType.RedParticulate].TankDepleted;
        public Fault YellowTankDepleted => ingredientTanks[(int)IngredientType.YellowParticulate].TankDepleted;

        public ParticulateDispenser()
        {
            ingredientTanks = Array.ConvertAll(
                (IngredientType[])Enum.GetValues(typeof(IngredientType)),
                type => new IngredientTank(name, type)
            );

            CompleteStationFailure.Subsumes(DispenserDefect);
            DispenserDefect.Subsumes(BlueTankDepleted, RedTankDepleted, YellowTankDepleted);
        }

        public void SetStoredAmount(IngredientType ingredientType, uint amount)
        {
            ingredientTanks[(int)ingredientType].Amount = amount;
        }

        protected override void ExecuteRole(Role role)
        {
            foreach (var capability in role.CapabilitiesToApply)
            {
                var ingredient = capability as Ingredient;
                if (ingredient == null)
                    throw new InvalidOperationException($"Invalid capability in ParticulateDispenser: {capability}");

                ingredientTanks[(int)ingredient.Type].Dispense(Container, ingredient);
            }
        }

        [FaultEffect(Fault = nameof(DispenserDefect))]
        public class DispenserDefectEffect : ParticulateDispenser
        {
            public override Capability[] AvailableCapabilities => new Capability[0];
        }

        [FaultEffect(Fault = nameof(CompleteStationFailure))]
        public class CompleteStationFailureEffect : ParticulateDispenser
        {
            public override bool IsAlive => false;

            public override void Update() { }
        }
    }
}
