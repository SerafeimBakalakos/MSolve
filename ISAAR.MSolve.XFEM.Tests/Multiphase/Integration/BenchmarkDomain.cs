using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.FEM.Interpolation.Jacobians;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;

namespace ISAAR.MSolve.XFEM.Tests.Multiphase.Integration
{
    public class BenchmarkDomain
    {
        public enum GeometryType { Natural, Rectangle, Quad }

        private readonly GeometryType geometryType;

        public BenchmarkDomain(GeometryType geometryType)
        {
            this.geometryType = geometryType;
            if (geometryType == GeometryType.Natural)
            {
                var nodes = new XNode[4];
                nodes[0] = new XNode(0, -1, -1);
                nodes[1] = new XNode(1, +1, -1);
                nodes[2] = new XNode(2, +1, +1);
                nodes[3] = new XNode(3, -1, +1);
                Element = new MockQuad4(0, nodes);
            }
            else if (geometryType == GeometryType.Rectangle)
            {
                // 3 ----------
                //   |        |
                //   |        |
                //   |        |
                //   ----------
                //  0         4   

                var nodes = new XNode[4];
                nodes[0] = new XNode(0, 0.0, 0.0);
                nodes[1] = new XNode(1, 4.0, 0.0);
                nodes[2] = new XNode(2, 4.0, 3.0);
                nodes[3] = new XNode(3, 0.0, 3.0);
                Element = new MockQuad4(0, nodes);
            }
            else if (geometryType == GeometryType.Quad)
            {
                //                 (4,5)
                //               /\
                //             /   \
                //           /      \
                //         /         \
                //(0,2)  /            \
                //      |              \ 
                //      |               \
                //      -----------------
                //   (0,0)          (6,0)  
                var nodes = new XNode[4];
                nodes[0] = new XNode(0, 0, 0);
                nodes[1] = new XNode(1, 6, 0);
                nodes[2] = new XNode(2, 4, 5);
                nodes[3] = new XNode(3, 0, 2);
                Element = new MockQuad4(0, nodes);
            }
            else throw new NotImplementedException();
        }

        public IXFiniteElement Element { get; }

        public double CalcJacobianDeterminant(GaussPoint point)
        {
            if (geometryType == GeometryType.Natural) return 1.0;
            else if (geometryType == GeometryType.Rectangle) return 3.0; // dx/dxi=2 , dy/deta=3/2
            else if (geometryType == GeometryType.Quad)
            {
                //TODO: Use an analytic formula
                Matrix shapeDerivatives = Element.InterpolationStandard.EvaluateNaturalGradientsAt(point);
                var jacobian = new IsoparametricJacobian2D(Element.Nodes, shapeDerivatives);
                return jacobian.DirectDeterminant;
            }
            else throw new NotImplementedException();
        }
    }
}
