using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh;
using ISAAR.MSolve.XFEM.Multiphase.Plotting.Writers;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting
{
    public class PhasePlotter
    {
        public const string vtkReaderVersion = "4.1";

        private readonly double colorForDefaultPhase;
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;

        public PhasePlotter(XModel physicalModel, GeometricModel geometricModel, double colorForDefaultPhase = 0.0)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
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
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                var nodalPhases = new Dictionary<INode, double>();

                foreach (XNode node in physicalModel.Nodes)
                {
                    double phase = node.SurroundingPhase.ID;
                    if (phase == DefaultPhase.DefaultPhaseID) phase = colorForDefaultPhase;
                    nodalPhases[node] = phase;
                }

                writer.WriteScalarField("nodal_phases", nodalPhases);
            }
        }

        public void PlotPhases(string path)
        {
            using (var writer = new Writers.VtkFileWriter(path))
            {
                var phaseMesh = new PhaseMesh<XNode>(geometricModel);
                writer.WriteMesh(phaseMesh);
            }
        }

        private Dictionary<VtkPoint, double> FindPhasesOfElements(ConformingOutputMesh2D conformingMesh)
        {
            var field = new Dictionary<VtkPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                if (element.Phases.Count == 1)
                {
                    double phaseID = element.Phases.First().ID;
                    if (phaseID == DefaultPhase.DefaultPhaseID) phaseID = colorForDefaultPhase;
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
                        double xm = 0.0, ym = 0.0;
                        for (int v = 0; v < 3; ++v)
                        {
                            xm += subtriangle.OutVertices[v].X;
                            ym += subtriangle.OutVertices[v].Y;
                        }
                        var centroid = new CartesianPoint(xm / 3.0, ym / 3.0);

                        // Find the phase of the centroid
                        double phaseID = colorForDefaultPhase;
                        foreach (IPhase phase in element.Phases)
                        {
                            if (phase.ID == DefaultPhase.DefaultPhaseID) continue;
                            var convexPhase = (ConvexPhase)phase;
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
