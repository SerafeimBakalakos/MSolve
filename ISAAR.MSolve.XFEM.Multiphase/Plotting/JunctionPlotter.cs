using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Plotting
{
    public class JunctionPlotter
    {
        private readonly double elementSize;
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;

        public JunctionPlotter(XModel physicalModel, GeometricModel geometricModel, double elementSize)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
            this.elementSize = elementSize;
        }

        public void PlotJunctionElements(string path)
        {
            var junctionElements = new Dictionary<IXFiniteElement, HashSet<PhaseJunction>>();
            foreach (PhaseJunction junction in geometricModel.Junctions)
            {
                bool exists = junctionElements.TryGetValue(junction.Element, out HashSet<PhaseJunction> junctions);
                if (!exists)
                {
                    junctions = new HashSet<PhaseJunction>();
                    junctionElements[junction.Element] = junctions;
                }
                junctions.Add(junction);
            }

            var pointsToPlot = new Dictionary<CartesianPoint, double>();
            foreach (var pair in junctionElements)
            {
                IXFiniteElement element = pair.Key;
                CartesianPoint centroid = FindCentroid(element);
                if (pair.Value.Count == 1)
                {
                    pointsToPlot[centroid] = pair.Value.First().ID;
                }
                else
                {
                    CartesianPoint[] centroidInstances = DuplicatePointForBetterViewing(centroid, pair.Value.Count);
                    int i = 0;
                    foreach (PhaseJunction junction in pair.Value)
                    {
                        CartesianPoint point = centroidInstances[i++];
                        pointsToPlot[point] = junction.ID;
                    }
                }
            }

            using (var writer = new VtkPointWriter(path))
            {
                writer.WriteScalarField("phase_junctions", pointsToPlot);
            }
        }

        private CartesianPoint[] DuplicatePointForBetterViewing(CartesianPoint point, int numInstances)
        {
            //TODO: Add more.
            var possibilites = new CartesianPoint[4]; // The further ones apart go to top
            double offset = 0.05 * elementSize;
            possibilites[0] = new CartesianPoint(point.X - offset, point.Y - offset);
            possibilites[1] = new CartesianPoint(point.X + offset, point.Y + offset);
            possibilites[2] = new CartesianPoint(point.X + offset, point.Y - offset);
            possibilites[3] = new CartesianPoint(point.X - offset, point.Y + offset);

            var instances = new CartesianPoint[numInstances];
            for (int i = 0; i < numInstances; ++i) instances[i] = possibilites[i];
            return instances;
        }

        private CartesianPoint FindCentroid(IXFiniteElement element)
        {
            double x = 0, y = 0;
            foreach (XNode node in element.Nodes)
            {
                x += node.X;
                y += node.Y;
            }
            return new CartesianPoint(x / element.Nodes.Count, y / element.Nodes.Count);
        }
    }
}
