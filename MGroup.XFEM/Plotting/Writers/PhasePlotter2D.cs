using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Plotting.Writers
{
    public class PhasePlotter2D
    {
        public const string vtkReaderVersion = "4.1";

        private readonly double colorForDefaultPhase;
        private readonly int defaultPhaseID;
        private readonly GeometricModel2D geometricModel;
        private readonly XModel physicalModel;

        public PhasePlotter2D(XModel physicalModel, GeometricModel2D geometricModel, int defaultPhaseID, 
            double colorForDefaultPhase = 0.0)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
            this.defaultPhaseID = defaultPhaseID;
            this.colorForDefaultPhase = colorForDefaultPhase;
        }

        public void PlotElements(string path, ConformingOutputMesh2D conformingMesh)
        {
            Dictionary<VtkPoint, double> phases = FindPhasesOfElements(conformingMesh);
            using (var writer = new Writers.VtkFileWriter(path))
            {
                writer.WriteMesh(conformingMesh);
                writer.WriteScalarField("phase", conformingMesh, v => phases[v]);
            }
        }

        public void PlotNodes(string path)
        {
            using (var writer = new ISAAR.MSolve.Logging.VTK.VtkPointWriter(path))
            {
                var nodalPhases = new Dictionary<INode, double>();

                foreach (XNode node in physicalModel.Nodes)
                {
                    double phaseID = node.PhaseID;
                    if (node.PhaseID == defaultPhaseID) phaseID = colorForDefaultPhase;
                    nodalPhases[node] = phaseID;
                }

                writer.WriteScalarField("nodal_phases", nodalPhases);
            }
        }

        private Dictionary<VtkPoint, double> FindPhasesOfElements(ConformingOutputMesh2D conformingMesh)
        {
            var field = new Dictionary<VtkPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element2D = (IXFiniteElement2D)element;
                var elementPhases = element.PhaseIDs;
                if (elementPhases.Count == 1)
                {
                    double phaseID = elementPhases.First();
                    if (elementPhases.First() is DefaultPhase2D) phaseID = colorForDefaultPhase;
                    VtkCell outCell = conformingMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n) field[outCell.Vertices[n]] = phaseID;
                }
                else
                {
                    IEnumerable<ConformingOutputMesh2D.Subtriangle> subtriangles =
                        conformingMesh.GetSubtrianglesForOriginal(element);
                    foreach (ConformingOutputMesh2D.Subtriangle subtriangle in subtriangles)
                    {
                        Debug.Assert(subtriangle.OutVertices.Count == 3); //TODO: Not sure what happens for 2nd order elements

                        // TODO: Perhaps I should do the next operations in the natural system of the element.
                        // Find the centroid
                        NaturalPoint centroidNatural = subtriangle.OriginalTriangle.FindCentroidNatural();
                        var centroid = new XPoint();
                        centroid.Element = subtriangle.ParentElement;
                        centroid.Coordinates[CoordinateSystem.ElementLocal] = 
                            new double[] { centroidNatural.Xi, centroidNatural.Eta };
                        centroid.ShapeFunctions = ((IXFiniteElement2D)centroid.Element)
                            .Interpolation.EvaluateFunctionsAt(centroidNatural);

                        // Find the phase of the centroid
                        double phaseID = colorForDefaultPhase;
                        foreach (int id in elementPhases)
                        {
                            if (id == defaultPhaseID) continue;
                            IPhase2D phase = geometricModel.Phases[id];
                            var convexPhase = (ConvexPhase2D)phase;
                            if (convexPhase.Contains(centroid))
                            {
                                phaseID = convexPhase.ID;
                                break;
                            }
                        }

                        // All vertices of the subtriangle will be assigned the same phase as the centroid
                        for (int v = 0; v < 3; ++v)
                        {
                            VtkPoint vertexOut = subtriangle.OutVertices[v];
                            field[vertexOut] = phaseID;
                        }
                    }
                }
            }
            return field;
        }
    }
}
