using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.Custom;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Plotting;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Tests.Plotting
{
    public static class LsmBalls3DExamples
    {
        private const string outputDirectory = @"C:\Users\Serafeim\Desktop\HEAT\2020\Spheres3D\";
        private const string pathMesh = outputDirectory + "conforming_mesh.vtk";
        private const string pathIntersections = outputDirectory + "intersections.vtk";

        private const double xMin = -1.0, xMax = 1.0, yMin = -1, yMax = 1.0, zMin = -1.0, zMax = +1.0;

        // There are 2 or more inclusions in the same element
        private const int numElementsX = 15, numElementsY = 15, numElementsZ = 15;
        private const int numBallsX = 2, numBallsY = 1, numBallsZ = 1;
        private const double ballRadius = 0.3;

        private const double zeroLevelSetTolerance = 1E-6;
        private const int subdomainID = 0;


        public static void Run()
        {
            // Create model and LSM
            XModel model = CreateModelHexa(numElementsX, numElementsY, numElementsZ);
            List<SimpleLsm3D> lsmSurfaces = InitializeLSM(model);

            // Plot original mesh and level sets
            PlotInclusionLevelSets(outputDirectory, "level_set", model, lsmSurfaces);

            // Plot intersections between level set curves and elements
            var intersectionPlotter = new Lsm3DElementIntersectionsPlotter(model, lsmSurfaces);
            intersectionPlotter.PlotIntersections(pathIntersections);
        }

        //public static void PlotConformingMesh()
        //{
        //    // Create model and LSM
        //    (XModel model, GeometricModel2D geometricModel) = CreateModel(numElementsX, numElementsY);
        //    InitializeLSM(model, geometricModel);

        //    // Plot conforming mesh and level sets
        //    using (var writer = new VtkFileWriter(pathMesh))
        //    {
        //        var mesh = new ConformingOutputMesh2D(geometricModel, model.Nodes, model.Elements);
        //        writer.WriteMesh(mesh);
        //    }

        //    // Plot original mesh and level sets
        //    Utilities.PlotInclusionLevelSets(outputDirectory, "level_set", model, geometricModel);
        //}

        private static XModel CreateModelHexa(int numElementsX, int numElementsY, int numElementsZ)
        {
            var model = new XModel();
            model.Subdomains[subdomainID] = new XSubdomain(subdomainID);

            // Mesh generation
            var meshGen = new UniformMeshGenerator3D<XNode>(xMin, yMin, zMin, xMax, yMax, zMax, 
                numElementsX, numElementsY, numElementsZ);
            (IReadOnlyList<XNode> nodes, IReadOnlyList<CellConnectivity<XNode>> cells) =
                meshGen.CreateMesh((id, x, y, z) => new XNode(id, x, y, z));

            // Nodes
            foreach (XNode node in nodes) model.Nodes.Add(node);

            // Elements
            int numGaussPointsInterface = 2;
            for (int e = 0; e < cells.Count; ++e)
            {
                var element = new MockElement(e, CellType.Hexa8, cells[e].Vertices);
                model.Elements.Add(element);
                model.Subdomains[subdomainID].Elements.Add(element);
            }

            model.ConnectDataStructures();
            return model;
        }

        
        private static List<SimpleLsm3D> InitializeLSM(XModel model)
        {
            var surfaces = new List<SimpleLsm3D>(numBallsX * numBallsY * numBallsZ);
            double dx = (xMax - xMin) / (numBallsX + 1);
            double dy = (yMax - yMin) / (numBallsY + 1);
            double dz = (zMax - zMin) / (numBallsZ + 1);
            for (int i = 0; i < numBallsX; ++i)
            {
                double centerX = xMin + (i + 1) * dx;
                for (int j = 0; j < numBallsY; ++j)
                {
                    double centerY = yMin + (j + 1) * dy;
                    for (int k = 0; k < numBallsZ; ++k)
                    {
                        double centerZ = zMin + (k + 1) * dz;
                        var sphere = new Sphere(centerX, centerY, centerZ, ballRadius);
                        var lsm = new SimpleLsm3D(model, sphere);
                        surfaces.Add(lsm);
                    }
                }
            }

            return surfaces;
        }

        internal static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
            XModel model, IList<SimpleLsm3D> lsmCurves)
        {
            for (int c = 0; c < lsmCurves.Count; ++c)
            {
                directoryPath = directoryPath.Trim('\\');
                string suffix = (lsmCurves.Count == 1) ? "" : $"{c}";
                string file = $"{directoryPath}\\{vtkFilenamePrefix}{suffix}.vtk";
                using (var writer = new VtkFileWriter(file))
                {
                    var levelSetField = new LevelSetField(model, lsmCurves[c]);
                    writer.WriteMesh(levelSetField.Mesh);
                    writer.WriteScalarField($"inclusion{suffix}_level_set",
                        levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
                }
            }
        }
    }
}
