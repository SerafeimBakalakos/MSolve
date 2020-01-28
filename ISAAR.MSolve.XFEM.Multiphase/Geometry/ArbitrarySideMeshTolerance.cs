using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Multiphase.Geometry
{
    public class ArbitrarySideMeshTolerance : IMeshTolerance
    {
        private readonly double coeff;

        public ArbitrarySideMeshTolerance(double coeff = 1E-8)
        {
            this.coeff = coeff;
        }

        public double CalcTolerance(IXFiniteElement element)
        {
            XNode node1 = element.Nodes[0];
            XNode node2 = element.Nodes[1];
            double edgeLength = node2.CalculateDistanceFrom(node1);
            return coeff * edgeLength;
        }
    }
}
