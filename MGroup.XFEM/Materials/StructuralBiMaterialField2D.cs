using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Materials
{
    public class StructuralBiMaterialField2D : IStructuralMaterialField
    {
        private readonly ElasticMaterial2D material0, material1;
        private readonly CohesiveInterfaceMaterial interfaceMaterial;

        public StructuralBiMaterialField2D(ElasticMaterial2D material0, ElasticMaterial2D material1, 
            CohesiveInterfaceMaterial interfaceMaterial)
        {
            this.material0 = material0;
            this.material1 = material1;
            this.interfaceMaterial = interfaceMaterial;
        }

        public HashSet<int> PhasesWithMaterial0 { get; } = new HashSet<int>();
        public HashSet<int> PhasesWithMaterial1 { get; } = new HashSet<int>();

        public CohesiveInterfaceMaterial FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary)
        {
            return interfaceMaterial;
        }

        public IContinuumMaterial FindMaterialAt(IPhase phase)
        {
            if (PhasesWithMaterial0.Contains(phase.ID)) return material0.Clone();
            else
            {
                Debug.Assert(PhasesWithMaterial1.Contains(phase.ID));
                return material1.Clone();
            }
        }
    }
}
