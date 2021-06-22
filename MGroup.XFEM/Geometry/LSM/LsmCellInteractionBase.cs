using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MGroup.XFEM.Geometry.LSM
{
    public abstract class LsmCellInteractionBase
    {
        private readonly int dimension;
        private readonly int numNodes;
        protected readonly IList<int> nodeIDs;
        protected readonly List<double[]> nodeCoords;
        protected readonly double tolerance;

        protected bool areLevelSetsAdjusted = false;
        protected List<double> nodeLevelSets;

        protected LsmCellInteractionBase(int dimension, int numNodes,
            IList<int> nodeIDs, List<double[]> nodeCoords, List<double> nodeLevelSets, double tolerance)
        {
            Debug.Assert(nodeIDs.Count == 3);
            Debug.Assert(nodeCoords.Count == 3);
            Debug.Assert(nodeLevelSets.Count == 3);

            this.dimension = dimension;
            this.numNodes = numNodes;
            this.nodeIDs = nodeIDs;
            this.nodeCoords = nodeCoords;
            this.nodeLevelSets = nodeLevelSets;
            this.tolerance = tolerance;

            Mesh = new IntersectionMesh(dimension);
        }

        public IntersectionMesh Mesh { get; }

        public RelativePositionCurveElement Position { get; protected set; } = RelativePositionCurveElement.Disjoint;

        protected abstract List<(int nodeIdx0, int nodeIdx1)> Edges { get; }

        /// <summary>
        /// This will ensure machine precision will return the same result if this method is called with the same 
        /// arguments more than once.
        /// </summary>
        /// <param name="levelSetNeg"></param>
        /// <param name="levelSetPos"></param>
        [Conditional("DEBUG")]
        protected static void CheckLevelSets(double levelSetNeg, double levelSetPos)
        {
            if (levelSetNeg >= levelSetPos)
            {
                throw new ArgumentException("Always provide the negative level set and corresponding values first");
            }
        }

        protected static double[] Interpolate(double levelSetNeg, double[] valuesNeg, double levelSetPos, double[] valuesPos)
        {
            Debug.Assert(valuesNeg.Length == valuesPos.Length);
            CheckLevelSets(levelSetNeg, levelSetPos);

            // The intersection point between these nodes can be found using the linear interpolation, see Sukumar 2001
            // The same interpolation can be used for interpolating coordinates or any other values.
            double k = -levelSetNeg / (levelSetPos - levelSetNeg);
            var result = new double[valuesNeg.Length];
            for (int i = 0; i < valuesNeg.Length; ++i)
            {
                result[i] = valuesNeg[i] + k * (valuesPos[i] - valuesNeg[i]);
            }
            return result;
        }

        protected void AdjustLevelSetsToAvoidDegenerateIntersections()
        {
            // Deep copy the injected list, to avoid corrupting outside data. 
            var copy = new List<double>(nodeLevelSets.Count);
            copy.AddRange(nodeLevelSets);
            nodeLevelSets = copy;

            // Find intersected edges
            foreach ((int nodeIdx0, int nodeIdx1) in Edges)
            {
                if (nodeLevelSets[nodeIdx0] * nodeLevelSets[nodeIdx1] < 0)
                {
                    int nodeNeg, nodePos;
                    if (nodeLevelSets[nodeIdx0] < nodeLevelSets[nodeIdx1])
                    {
                        nodeNeg = nodeIdx0;
                        nodePos = nodeIdx1;
                    }
                    else
                    {
                        nodeNeg = nodeIdx1;
                        nodePos = nodeIdx0;
                    }

                    // The intersection point between these nodes can be found using the linear interpolation, see Sukumar 2001
                    // k belongs in (0, 1), where 0 is the negative node and 1 is the positive.
                    double levelSetNeg = nodeLevelSets[nodeNeg];
                    double levelSetPos = nodeLevelSets[nodePos];
                    double k = -levelSetNeg / (levelSetPos - levelSetNeg);


                    // If the intersection point is too close to either node, set the level set of the corresponding node to 0.
                    if (k < tolerance) // also catches k being slightly lower than 0
                    {
                        nodeLevelSets[nodeNeg] = 0.0;
                        areLevelSetsAdjusted = true;
                    }
                    else if (1 - k < tolerance) // also catches k being slightly greater than 1
                    {
                        nodeLevelSets[nodePos] = 0.0;
                        areLevelSetsAdjusted = true;
                    }
                }
            }
        }

        protected void AddConformingNodeToMesh(int nodeIdx)
        {
            Mesh.Vertices.Add(nodeCoords[nodeIdx]);
            Mesh.IntersectedEdges.Add(new int[] { nodeIDs[nodeIdx] });
        }

        protected (int numZeroNodes, int numPosNodes, int numNegNodes) CountNodes()
        {
            int numZeroNodes = 0;
            int numPosNodes = 0;
            int numNegNodes = 0;
            for (int i = 0; i < numNodes; ++i)
            {
                if (nodeLevelSets[i] < 0) ++numNegNodes;
                else if (nodeLevelSets[i] > 0) ++numPosNodes;
                else ++numZeroNodes;
            }
            return (numZeroNodes, numPosNodes, numNegNodes);
        }

        protected int[] DefineIntersectedEdge(int nodeIdx0, int nodeIdx1)
        {
            if (nodeIDs[nodeIdx0] < nodeIDs[nodeIdx1])
            {
                return new int[] { nodeIDs[nodeIdx0], nodeIDs[nodeIdx1] };
            }
            else
            {
                return new int[] { nodeIDs[nodeIdx1], nodeIDs[nodeIdx0] };
            }
        }

        /// <summary>
        /// Fixes the orientation of each cell in the intersection mesh, so that its normal points towards the positive 
        /// region defined by the level set surface.
        /// </summary>
        /// <param name="data"></param>
        protected void FixCellsOrientation()
        {
            for (int c = 0; c < Mesh.Cells.Count; ++c)
            {
                int[] connectivity = Mesh.Cells[c].connectivity;
                double[] pA = Mesh.Vertices[connectivity[0]]; // The first vertex of the cell

                // Find a normal (non-unit) of the segment
                double[] normal = CalcNormalOfCell(connectivity);

                // Find the node with the max distance from the segment, by projecting onto the normal.
                // This assumes that there is at least 1 node with non-zero level set.
                // We need the max to avoid degenerate cases.
                double max = 0;
                int farthestNodeIdx = -1;
                double farthestNodeDot = double.NaN;
                for (int n = 0; n < nodeCoords.Count; ++n)
                {
                    if (nodeLevelSets[n] == 0) // These always lie on the intersection segment
                    {
                        continue;
                    }

                    var q = nodeCoords[n];
                    double[] vAQ = { q[0] - pA[0], q[1] - pA[1] };
                    double signedDistance = vAQ[0] * normal[0] + vAQ[1] * normal[1];
                    double distance = Math.Abs(signedDistance);
                    if (distance > max)
                    {
                        max = distance;
                        farthestNodeIdx = n;
                        farthestNodeDot = signedDistance;
                    }
                }
                Debug.Assert(farthestNodeIdx != -1);

                // Decide wether the normal points towards the positive region. 
                bool normalPointsTowardsPositive;
                if (nodeLevelSets[farthestNodeIdx] > 0)
                {
                    // For the normal to point towards the positive region, it must point towards a positive node.
                    normalPointsTowardsPositive = farthestNodeDot > 0;
                }
                else
                {
                    // For the normal to point towards the positive region, it must point opposite to a negative node.
                    normalPointsTowardsPositive = farthestNodeDot < 0;
                }

                if (!normalPointsTowardsPositive) // Swap the first 2 vertices to flip the normal towards the positive region.
                {
                    int swap = connectivity[0];
                    connectivity[0] = connectivity[1];
                    connectivity[1] = swap;
                }
            }
        }

        protected abstract double[] CalcNormalOfCell(int[] connectivity);
    }
}
