using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Materials
{
    public class MatrixInclusionsMaterialField : IThermalMaterialField
    {
        private readonly ThermalMaterial matrixMaterial, inclusionMaterial;
        private readonly double matrixInclusionInterfaceConductivity, inclusionInclusionInterfaceConductivity;
        private readonly int matrixPhaseID;

        public MatrixInclusionsMaterialField(ThermalMaterial matrixMaterial, ThermalMaterial inclusionMaterial, 
            double matrixInclusionInterfaceConductivity, double inclusionInclusionInterfaceConductivity, int matrixPhaseID)
        {
            this.matrixMaterial = matrixMaterial;
            this.inclusionMaterial = inclusionMaterial;
            this.matrixInclusionInterfaceConductivity = matrixInclusionInterfaceConductivity;
            this.inclusionInclusionInterfaceConductivity = inclusionInclusionInterfaceConductivity;
            this.matrixPhaseID = matrixPhaseID;
        }

        public ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary phaseBoundary)
        {
            // There is no guarantee that this jump will be due to step enrichment or junction enrichment. However the 
            // general assumption is that both enrichments have the same jump coefficient for the same boundary.
            double jumpCoefficient = phaseBoundary.Enrichment.PhaseJumpCoefficient;
            
            if ((phaseBoundary.PositivePhase.ID == matrixPhaseID) || (phaseBoundary.NegativePhase.ID == matrixPhaseID))
            {
                return new ThermalInterfaceMaterial(matrixInclusionInterfaceConductivity, jumpCoefficient);
            }
            else return new ThermalInterfaceMaterial(inclusionInclusionInterfaceConductivity, jumpCoefficient);
        }

        public ThermalMaterial FindMaterialAt(IXFiniteElement element, EvalInterpolation2D interpolationAtGaussPoint)
        {
            if (element.Phases.Count == 1)
            {
                if (element.Phases.First().ID == matrixPhaseID) return matrixMaterial.Clone();
                else return inclusionMaterial.Clone();
            }
            else
            {
                CartesianPoint point = interpolationAtGaussPoint.TransformPointNaturalToGlobalCartesian();
                bool hasMatrixPhase = false;
                foreach (IPhase phase in element.Phases)
                {
                    // Avoid searching for the point in the default phase, since its shape is hihly irregular.
                    if (phase.ID == matrixPhaseID)
                    {
                        hasMatrixPhase = true;
                        continue;
                    }
                    else if (phase.Contains(point)) return inclusionMaterial.Clone();
                }

                // Instead choose it if the point is not contained in any other phases
                Debug.Assert(hasMatrixPhase, "The point does not belong to any phases");
                return matrixMaterial.Clone();
            }
        }
    }
}
