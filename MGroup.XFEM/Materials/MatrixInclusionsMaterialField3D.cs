//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.FEM.Interpolation;
//using ISAAR.MSolve.Geometry.Coordinates;
//using MGroup.XFEM.Entities;

//namespace MGroup.XFEM.Materials
//{
//    public class MatrixInclusionsMaterialField3D : IThermalMaterialField3D
//    {
//        private readonly ThermalMaterial matrixMaterial, inclusionMaterial;
//        private readonly double matrixInclusionInterfaceConductivity, inclusionInclusionInterfaceConductivity;
//        private readonly int matrixPhaseID;

//        public MatrixInclusionsMaterialField3D(ThermalMaterial matrixMaterial, ThermalMaterial inclusionMaterial, 
//            double matrixInclusionInterfaceConductivity, double inclusionInclusionInterfaceConductivity, int matrixPhaseID)
//        {
//            this.matrixMaterial = matrixMaterial;
//            this.inclusionMaterial = inclusionMaterial;
//            this.matrixInclusionInterfaceConductivity = matrixInclusionInterfaceConductivity;
//            this.inclusionInclusionInterfaceConductivity = inclusionInclusionInterfaceConductivity;
//            this.matrixPhaseID = matrixPhaseID;
//        }

//        public ThermalInterfaceMaterial FindInterfaceMaterialAt(PhaseBoundary3D phaseBoundary)
//        {
//            if ((phaseBoundary.PositivePhase.ID == matrixPhaseID) || (phaseBoundary.NegativePhase.ID == matrixPhaseID))
//            {
//                return new ThermalInterfaceMaterial(matrixInclusionInterfaceConductivity);
//            }
//            else return new ThermalInterfaceMaterial(inclusionInclusionInterfaceConductivity);
//        }

//        public ThermalMaterial FindMaterialAt(IPhase3D phase)
//        {
//            if (phase.ID == matrixPhaseID) return matrixMaterial.Clone();
//            else return inclusionMaterial.Clone();
//        }
//    }
//}
