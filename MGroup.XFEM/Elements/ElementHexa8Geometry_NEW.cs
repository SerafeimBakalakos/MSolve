﻿using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Interpolation.Jacobians;

namespace MGroup.XFEM.Elements
{
    public class ElementHexa8Geometry_NEW //: IElementGeometry // MODIFICATION NEEDED: change the interface as well
    {
        // MODIFICATION NEEDED: Probably this should take coordinates as parameters. Or move the area/volume computations elsewhere
        public double CalcBulkSizeCartesian(IReadOnlyList<XNode> nodes)
        {
            //TODO: Split it into tetrahedra and use the closed formula for their volume

            double volume = 0.0;
            GaussLegendre3D quadrature = GaussLegendre3D.GetQuadratureWithOrder(2, 2, 2);
            IReadOnlyList<Matrix> shapeGradientsNatural =
                InterpolationHexa8.UniqueInstance.EvaluateNaturalGradientsAtGaussPoints(quadrature);
            for (int gp = 0; gp < quadrature.IntegrationPoints.Count; ++gp)
            {
                var jacobian = new IsoparametricJacobian(3, nodes, shapeGradientsNatural[gp]);
                volume += jacobian.DirectDeterminant * quadrature.IntegrationPoints[gp].Weight;
            }
            return volume;
        }

        public double CalcBulkSizeNatural() => 8.0;

        public (ElementEdge_NEW[], ElementFace_NEW[]) FindEdgesFaces(IReadOnlyList<int> nodeIDs)
        {
            IReadOnlyList<double[]> nodesNatural = InterpolationHexa8.UniqueInstance.NodalNaturalCoordinates;
            var edges = new ElementEdge_NEW[12];
            edges[0] = new ElementEdge_NEW(0, nodeIDs, nodesNatural, 0, 1);
            edges[1] = new ElementEdge_NEW(1, nodeIDs, nodesNatural, 1, 2);
            edges[2] = new ElementEdge_NEW(2, nodeIDs, nodesNatural, 2, 3);
            edges[3] = new ElementEdge_NEW(3, nodeIDs, nodesNatural, 3, 0);
            edges[4] = new ElementEdge_NEW(4, nodeIDs, nodesNatural, 4, 5);
            edges[5] = new ElementEdge_NEW(5, nodeIDs, nodesNatural, 5, 6);
            edges[6] = new ElementEdge_NEW(6, nodeIDs, nodesNatural, 6, 7);
            edges[7] = new ElementEdge_NEW(7, nodeIDs, nodesNatural, 7, 4);
            edges[8] = new ElementEdge_NEW(8, nodeIDs, nodesNatural, 0, 4);
            edges[9] = new ElementEdge_NEW(9, nodeIDs, nodesNatural, 1, 5);
            edges[10] = new ElementEdge_NEW(10, nodeIDs, nodesNatural, 2, 6);
            edges[11] = new ElementEdge_NEW(11, nodeIDs, nodesNatural, 3, 7);

            var faces = new ElementFace_NEW[6];
            faces[0] = new ElementFace_NEW();
            faces[0].ID = 0;
            faces[0].NodeIDs = new int[]
            {
                nodeIDs[0], nodeIDs[1], nodeIDs[2], nodeIDs[3]
            };
            faces[0].NodesNatural = new double[][]
            {
                nodesNatural[0], nodesNatural[1], nodesNatural[2], nodesNatural[3]
            };
            faces[0].Edges = new ElementEdge_NEW[] { edges[0], edges[1], edges[2], edges[3] };

            faces[1] = new ElementFace_NEW();
            faces[1].ID = 1;
            faces[1].NodeIDs = new int[]
            {
                nodeIDs[7], nodeIDs[6], nodeIDs[5], nodeIDs[4]
            };
            faces[1].NodesNatural = new double[][]
            {
                nodesNatural[7], nodesNatural[6], nodesNatural[5], nodesNatural[4]
            };
            faces[1].Edges = new ElementEdge_NEW[] { edges[4], edges[5], edges[6], edges[7] };

            faces[2] = new ElementFace_NEW();
            faces[2].ID = 2;
            faces[2].NodeIDs = new int[]
            {
                nodeIDs[1], nodeIDs[0], nodeIDs[4], nodeIDs[5]
            };
            faces[2].NodesNatural = new double[][]
            {
                nodesNatural[1], nodesNatural[0], nodesNatural[4], nodesNatural[5]
            };
            faces[2].Edges = new ElementEdge_NEW[] { edges[0], edges[8], edges[4], edges[9] };

            faces[3] = new ElementFace_NEW();
            faces[3].ID = 3;
            faces[3].NodeIDs = new int[]
            {
                nodeIDs[3], nodeIDs[2], nodeIDs[6], nodeIDs[7]
            };
            faces[3].NodesNatural = new double[][]
            {
                nodesNatural[3], nodesNatural[2], nodesNatural[6], nodesNatural[7]
            };
            faces[3].Edges = new ElementEdge_NEW[] { edges[2], edges[10], edges[6], edges[11] };


            faces[4] = new ElementFace_NEW();
            faces[4].ID = 4;
            faces[4].NodeIDs = new int[]
            {
                nodeIDs[0], nodeIDs[3], nodeIDs[7], nodeIDs[4]
            };
            faces[4].NodesNatural = new double[][]
            {
                nodesNatural[0], nodesNatural[3], nodesNatural[7], nodesNatural[4]
            };
            faces[4].Edges = new ElementEdge_NEW[] { edges[3], edges[11], edges[7], edges[8] };

            faces[5] = new ElementFace_NEW();
            faces[5].ID = 5;
            faces[5].NodeIDs = new int[]
            {
                nodeIDs[2], nodeIDs[1], nodeIDs[5], nodeIDs[6]
            };
            faces[5].NodesNatural = new double[][]
            {
                nodesNatural[2], nodesNatural[1], nodesNatural[5], nodesNatural[6]
            };
            faces[5].Edges = new ElementEdge_NEW[] { edges[1], edges[9], edges[5], edges[10] };

            return (edges, faces);
        }
    }
}
