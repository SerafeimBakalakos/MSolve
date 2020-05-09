using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Materials;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting
{
    public class MaterialPlotter
    {
        private readonly XModel physicalModel;

        public MaterialPlotter(XModel physicalModel)
        {
            this.physicalModel = physicalModel;
        }

        public void PlotBoundaryMaterials(string path)
        {
            var materialPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> allData =
                    element.GetMaterialsForBoundaryIntegration();
                foreach (var entry in element.GetMaterialsForBoundaryIntegration())
                {
                    PhaseBoundary boundary = entry.Key;
                    (IReadOnlyList<GaussPoint> points, IReadOnlyList<ThermalInterfaceMaterial> materials) = entry.Value;
                    for (int i = 0; i < points.Count; ++i)
                    {
                        GaussPoint gp = points[i];
                        CartesianPoint point = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, gp);
                        materialPoints[point] = materials[i].InterfaceConductivity;
                    }
                }
            }
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                writer.WriteScalarField("interface_conductivities", materialPoints);
            }
        }

        #region obsolete
        //public void PlotBoundaryPhaseJumpCoefficients(string path)
        //{
        //    var jumpCoeffs = new Dictionary<CartesianPoint, double>();
        //    foreach (IXFiniteElement element in physicalModel.Elements)
        //    {
        //        Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> allData =
        //            element.GetMaterialsForBoundaryIntegration();
        //        foreach (var entry in element.GetMaterialsForBoundaryIntegration())
        //        {
        //            PhaseBoundary boundary = entry.Key;
        //            (IReadOnlyList<GaussPoint> points, IReadOnlyList<ThermalInterfaceMaterial> materials) = entry.Value;
        //            for (int i = 0; i < points.Count; ++i)
        //            {
        //                GaussPoint gp = points[i];
        //                CartesianPoint point = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, gp);
        //                jumpCoeffs[point] = boundary.StepEnrichment.PhaseJumpCoefficient;
        //            }
        //        }
        //    }
        //    using (var writer = new Logging.VTK.VtkPointWriter(path))
        //    {
        //        writer.WriteScalarField("phase_jump_coeffs", jumpCoeffs);
        //    }
        //}
        #endregion

        public void PlotVolumeMaterials(string path)
        {
            var materialPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                (IReadOnlyList<GaussPoint> points, IReadOnlyList<ThermalMaterial> materials) = 
                    element.GetMaterialsForVolumeIntegration();

                for (int i = 0; i < points.Count; ++i)
                {
                    GaussPoint gp = points[i];
                    ThermalMaterial material = materials[i];

                    CartesianPoint point = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, gp);
                    materialPoints[point] = material.ThermalConductivity;
                }
            }
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                writer.WriteScalarField("conductivities", materialPoints);
            }
        }
    }
}
