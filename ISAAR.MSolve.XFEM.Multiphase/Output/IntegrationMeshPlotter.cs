using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Geometry.Coordinates;
//using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Output.Mesh;

namespace ISAAR.MSolve.XFEM.Multiphase.Output
{
    public class IntegrationMeshPlotter
    {
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;

        public IntegrationMeshPlotter(XModel physicalModel, GeometricModel geometricModel)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
        }

        public void PlotIntegrationMesh(string path)
        {
            var integrationMesh = new IntegrationMesh2D(physicalModel, geometricModel);
            using (var writer = new Writers.VtkFileWriter(path))
            {
                writer.WriteMesh(integrationMesh);
            }
        }

        public void PlotIntegrationPoints(string path)
        {
            double value = 0.0;
            var integrationPoints = new Dictionary<CartesianPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                IReadOnlyList<GaussPoint> elementGPs = element.IntegrationStrategy.GenerateIntegrationPoints(element);
                foreach (GaussPoint gp in element.IntegrationStrategy.GenerateIntegrationPoints(element))
                {
                    CartesianPoint point = element.StandardInterpolation.TransformNaturalToCartesian(element.Nodes, gp);
                    integrationPoints[point] = value;
                }
            }
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                writer.WriteScalarField("integration_points", integrationPoints);
            }
        }
    }
}
