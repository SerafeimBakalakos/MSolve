using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Materials
{
    public class MatrixInclusionsStructuralMaterialField : IStructuralMaterialField
    {
        private readonly ElasticMaterial2D matrixMaterial, inclusionMaterial;
        private readonly CohesiveInterfaceMaterial interfaceMaterial;
        private readonly int matrixPhaseID;

        public MatrixInclusionsStructuralMaterialField(ElasticMaterial2D matrixMaterial, ElasticMaterial2D inclusionMaterial,
            CohesiveInterfaceMaterial interfaceMaterial, int matrixPhaseID)
        {
            this.matrixMaterial = matrixMaterial;
            this.inclusionMaterial = inclusionMaterial;
            this.interfaceMaterial = interfaceMaterial;
            this.matrixPhaseID = matrixPhaseID;
        }

        public CohesiveInterfaceMaterial FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary)
        {
            return interfaceMaterial.Clone();
        }

        public IContinuumMaterial FindMaterialAt(IPhase phase)
        {
            if (phase.ID == matrixPhaseID) return matrixMaterial.Clone();
            else return inclusionMaterial.Clone();
        }
    }
}
