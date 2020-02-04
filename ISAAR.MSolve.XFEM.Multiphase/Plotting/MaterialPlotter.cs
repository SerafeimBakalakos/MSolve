using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Materials;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting
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
                var castedElement = (MockQuad4)element;

                foreach (var pointMaterialPair in castedElement.MaterialsForBoundaryIntegration)
                {
                    GaussPoint gp = pointMaterialPair.Key;
                    ThermalInterfaceMaterial material = pointMaterialPair.Value;

                    CartesianPoint point = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, gp);
                    materialPoints[point] = material.InterfaceConductivity;
                }
            }
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                writer.WriteScalarField("interface_conductivities", materialPoints);
            }
        }

        public void PlotBoundaryPhaseJumpCoefficients(string path)
        {
            var materialPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var castedElement = (MockQuad4)element;

                foreach (var pointMaterialPair in castedElement.MaterialsForBoundaryIntegration)
                {
                    GaussPoint gp = pointMaterialPair.Key;
                    ThermalInterfaceMaterial material = pointMaterialPair.Value;

                    CartesianPoint point = element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, gp);
                    materialPoints[point] = material.PhaseJumpCoefficient ;
                }
            }
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                writer.WriteScalarField("phase_jump_coeffs", materialPoints);
            }
        }

        public void PlotVolumeMaterials(string path)
        {
            var materialPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var castedElement = (MockQuad4)element;

                foreach (var pointMaterialPair in castedElement.MaterialsForVolumeIntegration)
                {
                    GaussPoint gp = pointMaterialPair.Key;
                    ThermalMaterial material = pointMaterialPair.Value;

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
