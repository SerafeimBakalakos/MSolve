using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Materials
{
    public class MatrixInclusionsLayersMaterialField : IThermalMaterialField
    {
        private readonly ThermalMaterial matrixMaterial, inclusionMaterial, layerMaterial;
        private readonly double matrixLayerInterfaceConductivity, layerLayerInterfaceConductivity, inclusionLayerInterfaceConductivity;
        private readonly int matrixPhaseID;

        public MatrixInclusionsLayersMaterialField(ThermalMaterial matrixMaterial, ThermalMaterial inclusionMaterial, 
            ThermalMaterial layerMaterial, double matrixLayerInterfaceConductivity, 
            double layerLayerInterfaceConductivity, double inclusionLayerInterfaceConductivity, int matrixPhaseID)
        {
            this.matrixMaterial = matrixMaterial;
            this.inclusionMaterial = inclusionMaterial;
            this.layerMaterial = layerMaterial;
            this.matrixLayerInterfaceConductivity = matrixLayerInterfaceConductivity;
            this.layerLayerInterfaceConductivity = layerLayerInterfaceConductivity;
            this.inclusionLayerInterfaceConductivity = inclusionLayerInterfaceConductivity;
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
                return new ThermalInterfaceMaterial(matrixLayerInterfaceConductivity);
            }
            else
            {
                if (phaseBoundary.PositivePhase is HollowConvexPhase && phaseBoundary.NegativePhase is HollowConvexPhase)
                {
                    return new ThermalInterfaceMaterial(layerLayerInterfaceConductivity);
                }
                else if (phaseBoundary.PositivePhase is ConvexPhase || phaseBoundary.NegativePhase is ConvexPhase)
                {
                    return new ThermalInterfaceMaterial(inclusionLayerInterfaceConductivity);
                }
                else throw new NotImplementedException();
            }
        }

        public ThermalMaterial FindMaterialAt(IPhase phase)
        {
            if (phase.ID == matrixPhaseID) return matrixMaterial.Clone();
            else
            {
                if (phase is HollowConvexPhase) return layerMaterial.Clone();
                else if (phase is ConvexPhase) return inclusionMaterial.Clone();
                else throw new NotImplementedException();
            }
        }
    }
}
