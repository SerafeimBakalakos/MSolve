using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Materials
{
    public interface IStructuralMaterialField
    {
        CohesiveInterfaceMaterial FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary);

        IContinuumMaterial FindMaterialAt(IPhase phase);
    }
}
