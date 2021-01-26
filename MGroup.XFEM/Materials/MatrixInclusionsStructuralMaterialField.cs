using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Materials;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Materials
{
    public class MatrixInclusionsStructuralMaterialField : IStructuralMaterialField
    {
        private readonly ElasticMaterial2D matrixMaterial, inclusionMaterial;
        private readonly CohesiveInterfaceMaterial2D interfaceMaterial;
        private readonly int matrixPhaseID;

        public MatrixInclusionsStructuralMaterialField(ElasticMaterial2D matrixMaterial, ElasticMaterial2D inclusionMaterial,
            CohesiveInterfaceMaterial2D interfaceMaterial, int matrixPhaseID)
        {
            this.matrixMaterial = matrixMaterial;
            this.inclusionMaterial = inclusionMaterial;
            this.interfaceMaterial = interfaceMaterial;
            this.matrixPhaseID = matrixPhaseID;
        }

        public CohesiveInterfaceMaterial2D FindInterfaceMaterialAt(IPhaseBoundary phaseBoundary)
        {
            return interfaceMaterial.Clone();
        }

        public ElasticMaterial2D FindMaterialAt(IPhase phase)
        {
            if (phase.ID == matrixPhaseID) return matrixMaterial.Clone();
            else return inclusionMaterial.Clone();
        }
    }
}
