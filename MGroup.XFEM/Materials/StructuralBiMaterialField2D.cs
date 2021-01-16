using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Materials
{
    public class StructuralBiMaterialField2D : IStructuralMaterialField
    {
        private readonly ElasticMaterial2D material0, material1;

        public StructuralBiMaterialField2D(ElasticMaterial2D material0, ElasticMaterial2D material1)
        {
            this.material0 = material0;
            this.material1 = material1;
        }

        public HashSet<int> PhasesWithMaterial0 { get; } = new HashSet<int>();
        public HashSet<int> PhasesWithMaterial1 { get; } = new HashSet<int>();

        public StructuralInterfaceMaterial FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary)
        {
            return new StructuralInterfaceMaterial(double.NaN);
        }

        public ElasticMaterial2D FindMaterialAt(IPhase phase)
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
