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
            #region delete
            //double jumpCoefficient = phaseBoundary.StepEnrichment.PhaseJumpCoefficient;
            #endregion

            if ((phaseBoundary.PositivePhase.ID == matrixPhaseID) || (phaseBoundary.NegativePhase.ID == matrixPhaseID))
            {
                return new ThermalInterfaceMaterial(matrixInclusionInterfaceConductivity);
            }
            else return new ThermalInterfaceMaterial(inclusionInclusionInterfaceConductivity);
        }

        public ThermalMaterial FindMaterialAt(IPhase phase)
        {
            if (phase.ID == matrixPhaseID) return matrixMaterial.Clone();
            else return inclusionMaterial.Clone();
        }
    }
}
