using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Materials
{
    public class GeneralMultiphaseMaterial : IThermalMaterialField
    {
        private readonly Table<IPhase, IPhase, ThermalInterfaceMaterial> boundaryMaterials;
        private readonly Dictionary<IPhase, ThermalMaterial> phaseMaterials;


        public GeneralMultiphaseMaterial()
        {
            this.phaseMaterials = new Dictionary<IPhase, ThermalMaterial>();
            this.boundaryMaterials = new Table<IPhase, IPhase, ThermalInterfaceMaterial>();
        }

        public ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary phaseBoundary)
        {
            (IPhase minPhase, IPhase maxPhase) = FindMinMaxPhases(phaseBoundary.PositivePhase, phaseBoundary.NegativePhase);
            return boundaryMaterials[minPhase, maxPhase].Clone();
        }

        public ThermalMaterial FindMaterialAt(IPhase phase) => phaseMaterials[phase].Clone();

        public void RegisterBoundaryMaterial(IPhase phase1, IPhase phase2, double interfaceConductivity)
        {
            (IPhase minPhase, IPhase maxPhase) = FindMinMaxPhases(phase1, phase2);
            var material = new ThermalInterfaceMaterial(interfaceConductivity);
            this.boundaryMaterials[minPhase, maxPhase] = material;
        }

        public void RegisterPhaseMaterial(IPhase phase, ThermalMaterial material) => this.phaseMaterials[phase] = material;

        private static (IPhase minPhase, IPhase maxPhase) FindMinMaxPhases(IPhase phase1, IPhase phase2)
        {
            IPhase minPhase, maxPhase;
            if (phase1.ID < phase2.ID)
            {
                minPhase = phase1;
                maxPhase = phase2;
            }
            else
            {
                minPhase = phase2;
                maxPhase = phase1;
            }
            return (minPhase, maxPhase);
        }
    }
}
