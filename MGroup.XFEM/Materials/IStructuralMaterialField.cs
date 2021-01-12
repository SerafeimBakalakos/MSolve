using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Materials
{
    public interface IStructuralMaterialField
    {
        StructuralInterfaceMaterial FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary);

        ElasticMaterial2D FindMaterialAt(IPhase phase);
    }
}
