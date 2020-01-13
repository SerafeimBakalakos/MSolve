using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Entities;

namespace ISAAR.MSolve.XFEM.CrackGeometry.HeavisideSingularityResolving
{
    public class ExactSingularityResolver : IHeavisideSingularityResolver
    {
        public ISet<XNode> FindHeavisideNodesToRemove(ISingleCrack crack, IMesh2D<XNode, XContinuumElement2D> mesh, 
            ISet<XNode> heavisideNodes)
        {
            // If the integration points in the support of a node are all on the same side of the crack, then that node should 
            // not be enriched
            var pointsOfElements = new Dictionary<XContinuumElement2D, (int numPosPoints, int numNegPoints)>();
            var nodesToRemove = new HashSet<XNode>();
            foreach (XNode node in heavisideNodes)
            {
                int numPosPointsNode = 0;
                int numNegPointsNode = 0;

                foreach (var element in mesh.FindElementsWithNode(node))
                {
                    bool alreadyProcessed = pointsOfElements.TryGetValue(element, out (int pos, int neg) numPointsElement);
                    if (!alreadyProcessed)
                    {
                        numPointsElement = FindIntegrationPointsOfElement(element, crack);
                        pointsOfElements[element] = numPointsElement;
                    }
                    numPosPointsNode += numPointsElement.pos;
                    numNegPointsNode += numPointsElement.neg;
                }

                if ((numPosPointsNode == 0) || (numNegPointsNode == 0)) nodesToRemove.Add(node);
            }

            return nodesToRemove;

            //throw new NotImplementedException();

        }

        public ISet<XNode> FindHeavisideNodesToRemove(ISingleCrack crack, IReadOnlyList<XNode> heavisideNodes, 
            IReadOnlyList<ISet<XContinuumElement2D>> nodalSupports)
        {
            throw new NotImplementedException();
        }

        private (int numPosPoints, int numNegPoints) FindIntegrationPointsOfElement(XContinuumElement2D element, 
            ISingleCrack crack)
        {
            int numPosPoints = 0;
            int numNegPoints = 0;
            foreach (GaussPoint point in element.IntegrationStrategy.GenerateIntegrationPoints(element))
            {
                EvalInterpolation2D evaluatedInterpolation = element.Interpolation.EvaluateAllAt(element.Nodes, point);
                double levelSet = crack.SignedDistanceOf(point, element, evaluatedInterpolation);
                int sign = Math.Sign(levelSet);
                if (sign > 0) ++numPosPoints;
                else if (sign < 0) ++numNegPoints;
                else throw new Exception("Gauss points on the crack");
            }
            return (numPosPoints, numNegPoints);
        }
    }
}
