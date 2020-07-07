using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Plotting.Writers
{
    public class PhasePlotter
    {
        public const string vtkReaderVersion = "4.1";

        private readonly double colorForDefaultPhase;
        private readonly int defaultPhaseID;
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;

        public PhasePlotter(XModel physicalModel, GeometricModel geometricModel, int defaultPhaseID, 
            double colorForDefaultPhase = 0.0)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
            this.defaultPhaseID = defaultPhaseID;
            this.colorForDefaultPhase = colorForDefaultPhase;
        }

        public void PlotElements(string path, ConformingOutputMesh conformingMesh)
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
            using (var writer = new VtkPointWriter(path))
            {
                var nodalPhases = new Dictionary<INode, double>();

                foreach (XNode node in physicalModel.Nodes)
                {
                    double phaseID = node.Phase.ID;
                    if (node.Phase.ID == defaultPhaseID) phaseID = colorForDefaultPhase;
                    nodalPhases[node] = phaseID;
                }

                writer.WriteScalarField("nodal_phases", nodalPhases);
            }
        }

        private Dictionary<VtkPoint, double> FindPhasesOfElements(ConformingOutputMesh conformingMesh)
        {
            var field = new Dictionary<VtkPoint, double>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var elementPhases = element.Phases;
                if (elementPhases.Count == 1)
                {
                    double phaseID = elementPhases.First().ID;
                    if (elementPhases.First() is DefaultPhase) phaseID = colorForDefaultPhase;
                    VtkCell outCell = conformingMesh.GetOutCellsForOriginal(element).First();
                    for (int n = 0; n < element.Nodes.Count; ++n) field[outCell.Vertices[n]] = phaseID;
                }
                else
                {
                    IEnumerable<ConformingOutputMesh.Subcell> subcells = conformingMesh.GetSubcellsForOriginal(element);
                    foreach (ConformingOutputMesh.Subcell subcell in subcells)
                    {
                        Debug.Assert(subcell.OutVertices.Count == 3 || subcell.OutVertices.Count == 4); //TODO: Not sure what happens for 2nd order elements

                        // TODO: Perhaps I should do the next operations in the natural system of the element.
                        // Find the centroid
                        NaturalPoint centroidNatural = subcell.OriginalSubcell.FindCentroidNatural();
                        var centroid = new XPoint();
                        centroid.Element = subcell.ParentElement;
                        centroid.Coordinates[CoordinateSystem.ElementNatural] = 
                            new double[] { centroidNatural.Xi, centroidNatural.Eta, centroidNatural.Zeta };
                        centroid.ShapeFunctions = centroid.Element.Interpolation.EvaluateFunctionsAt(centroidNatural.Coordinates);

                        // Find the phase of the centroid
                        double phaseID = colorForDefaultPhase;
                        foreach (IPhase phase in elementPhases)
                        {
                            if (phase.ID == defaultPhaseID) continue;
                            if (phase.Contains(centroid))
                            {
                                phaseID = phase.ID;
                                break;
                            }
                        }

                        // All vertices of the subceel will be assigned the same phase as the centroid
                        for (int v = 0; v < subcell.OutVertices.Count; ++v)
                        {
                            VtkPoint vertexOut = subcell.OutVertices[v];
                            field[vertexOut] = phaseID;
                        }
                    }
                }
            }
            return field;
        }
    }
}
