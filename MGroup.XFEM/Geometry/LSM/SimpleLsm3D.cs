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
    public class SimpleLsm3D : IImplicitSurface3D
    {
        public SimpleLsm3D(XModel physicalModel, ISurface3D closedSurface)
        {
            NodalValues = new double[physicalModel.Nodes.Count];
            for (int n = 0; n < physicalModel.Nodes.Count; ++n)
            {
                double[] node = physicalModel.Nodes[n].Coordinates;
                NodalValues[n] = closedSurface.SignedDistanceOf(node);
            }
        }

        public double[] NodalValues { get; }

        public IElementSurfaceIntersection3D Intersect(IXFiniteElement element)
        {
            throw new NotImplementedException();
        }

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
