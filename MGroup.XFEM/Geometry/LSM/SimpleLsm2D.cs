using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public class SimpleLsm2D : IImplicitGeometry
    {
        public SimpleLsm2D(XModel physicalModel, ICurve2D closedCurve)
        {
            for (int n = 0; n < physicalModel.Nodes.Count; ++n)
            {
                double[] node = physicalModel.Nodes[n].Coordinates;
                NodalValues[n] = closedCurve.SignedDistanceOf(node);
            }
        }

        public double[] NodalValues { get; }

        public double SignedDistanceOf(XNode node) => NodalValues[node.ID];

        public double SignedDistanceOf(XPoint point)
        {
            int[] nodes = point.Element.Nodes.Select(n => n.ID).ToArray();
            double[] shapeFunctions = point.ShapeFunctions;
            double result = 0;
            for (int n = 0; n < nodes.Length; ++n)
            {
                result += shapeFunctions[n] * NodalValues[n];
            }
            return result;
        }
    }
}
