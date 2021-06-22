using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using System.Diagnostics;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmTri3Interaction_NEW 
    {
        private readonly IList<int> nodeIDs;
        private readonly List<double[]> nodeCoords;
        private readonly double tolerance = 1E-6;

        private bool areLevelSetsAdjusted;
        private List<double> nodeLevelSets;

        public LsmTri3Interaction_NEW(IList<int> nodeIDs, List<double[]> nodeCoords, List<double> nodeLevelSets)
        {
            Debug.Assert(nodeIDs.Count == 3);
            Debug.Assert(nodeCoords.Count == 3);
            Debug.Assert(nodeLevelSets.Count == 3);
            this.nodeIDs = nodeIDs;
            this.nodeCoords = nodeCoords;
            this.nodeLevelSets = nodeLevelSets;
        }

        public IntersectionMesh Mesh { get; } = new IntersectionMesh(2);

        public RelativePositionCurveElement Position { get; private set; } = RelativePositionCurveElement.Disjoint;

        public void Resolve()
        {
            int numZeroNodes = 0;
            int numPosNodes = 0;
            int numNegNodes = 0;
            for (int i = 0; i < 3; ++i)
            {
                if (nodeLevelSets[i] < 0) ++numNegNodes;
                else if (nodeLevelSets[i] > 0) ++numPosNodes;
                else ++numZeroNodes;
            }

            if (numZeroNodes == 0)
            {
                if ((numPosNodes == 0) || (numNegNodes == 0)) // Disjoint
                {
                    ProcessCase0Zeros3SameSigns();
                }
                else // 2 intersection points
                {
                    if (!areLevelSetsAdjusted)
                    {
                        AdjustLevelSetsToAvoidDegenerateIntersections();

                        if (areLevelSetsAdjusted)
                        {
                            // Level sets needed adjusting. Find and process the new interaction case, based on the new level sets.
                            //TODO: Now only some cases are possible, so I could optimize that determination.
                            Resolve(); // recurse but only 1 level
                            return; // do not do anything else after finishing the recursive level.
                        }
                        else
                        {
                            // Level sets were ok after all. Proceed to find intersections normally.
                            ProcessCase0Zeros2SameSigns1Different();
                        }
                    }
                    else
                    {
                        throw new Exception("This should not have happened. Reaching this means that level sets were modified," +
                            " thus there is at least 1 zero node.");
                    }
                }
            }
            else if (numZeroNodes == 1)
            {
                if ((numPosNodes == 0) || (numNegNodes == 0)) // Tangent (only 1 common point)
                {
                    ProcessCase1Zero2SameSigns();
                }
                else // 1 intersection point and 1 node
                {
                    if (!areLevelSetsAdjusted)
                    {
                        AdjustLevelSetsToAvoidDegenerateIntersections();

                        if (areLevelSetsAdjusted)
                        {
                            // Level sets needed adjusting. Find and process the new interaction case, based on the new level sets.
                            //TODO: Now only some cases are possible, so I could optimize that determination.
                            Resolve(); // recurse but only 1 level
                            return; // do not do anything else after finishing the recursive level.
                        }
                        else
                        {
                            // Level sets were ok after all. Proceed to find intersections normally.
                            ProcessCase1Zero1Pos1Neg();
                        }
                    }
                    else
                    {
                        // It is possible to reach this point by adjusting the level sets in another case.
                        ProcessCase1Zero1Pos1Neg();
                    }
                }
            }
            else if (numZeroNodes == 2) // 1 conforming edge
            {
                ProcessCase2Zeros1Nonzero();
            }
            else // 3 conforming edges
            {
                Debug.Assert(numZeroNodes == 3);
                ProcessCase3Zeros();
            }
        }

        private void ProcessCase0Zeros3SameSigns()
        {
            Position = RelativePositionCurveElement.Disjoint;
        }

        private void ProcessCase0Zeros2SameSigns1Different()
        {
            

            // There are 2 ways to reach this point:
            // i) The level sets have just been checked and no adjustment was necessary. We can continue with the intersections.
            // ii) The level sets have been checked by another method and adjusted. Resolve was called again and we ended up 
            //      here, which is not very probable.
            Position = RelativePositionCurveElement.Intersecting;

            for (int i = 0; i < 3; ++i)
            {
                int j = (i + 1) % 3;
                if (nodeLevelSets[i] * nodeLevelSets[j] < 0)
                {
                    int nodeNeg, nodePos;
                    if (nodeLevelSets[i] < nodeLevelSets[j])
                    {
                        nodeNeg = i;
                        nodePos = j;
                    }
                    else
                    {
                        nodeNeg = j;
                        nodePos = i;
                    }
                    double[] intersection = Interpolate(nodeLevelSets[nodeNeg], nodeCoords[nodeNeg],
                        nodeLevelSets[nodePos], nodeCoords[nodePos]);
                    Mesh.Vertices.Add(intersection);
                    Mesh.IntersectedEdges.Add(DefineIntersectedEdge(nodeIDs, i, j));
                }
            }
            Debug.Assert(Mesh.Vertices.Count == 2);

            Mesh.Cells.Add((CellType.Line, new int[] { 0, 1 }));
            FixCellsOrientation();
        }

        private void ProcessCase1Zero2SameSigns()
        {
            Position = RelativePositionCurveElement.Tangent;
        }

        private void ProcessCase1Zero1Pos1Neg()
        {
            if (!areLevelSetsAdjusted)
            {
                AdjustLevelSetsToAvoidDegenerateIntersections();

                if (areLevelSetsAdjusted)
                {
                    // Level sets needed adjusting. Find and process the new interaction case, based on the new level sets.
                    //TODO: Now only some cases are possible, so I could optimize that determination.
                    Resolve();
                }
            }

            Position = RelativePositionCurveElement.Intersecting;

            int nodeZero = nodeLevelSets.FindIndex(phi => phi == 0);
            AddConformingNodeToMesh(nodeZero);

            int nodeNeg = nodeLevelSets.FindIndex(phi => phi < 0);
            int nodePos = nodeLevelSets.FindIndex(phi => phi > 0);
            double[] intersection = Interpolate(nodeLevelSets[nodeNeg], nodeCoords[nodeNeg],
                nodeLevelSets[nodePos], nodeCoords[nodePos]);
            Mesh.Vertices.Add(intersection);
            Mesh.IntersectedEdges.Add(DefineIntersectedEdge(nodeIDs, nodeNeg, nodePos));

            Mesh.Cells.Add((CellType.Line, new int[] { 0, 1 }));
            FixCellsOrientation();
        }

        private void ProcessCase2Zeros1Nonzero()
        {
            Position = RelativePositionCurveElement.Conforming;
            int nodeZero0 = nodeLevelSets.FindIndex(phi => phi == 0);
            int nodeZero1 = nodeLevelSets.FindLastIndex(phi => phi == 0);
            AddConformingNodeToMesh(nodeZero0);
            AddConformingNodeToMesh(nodeZero1);
            Mesh.Cells.Add((CellType.Line, new int[] { 0, 1 }));
            FixCellsOrientation();
        }

        private void ProcessCase3Zeros()
        {
            //TODO: The client should decide whether to log this msg or throw an exception
            Debug.WriteLine(
                $"Found element that has all its edges conforming to level set curve with ID {int.MinValue}." +
                $" This usually indicates an error. It may also cause problems if the triangle nodes are not given in" +
                $" counter-clockwise order.");
            Position = RelativePositionCurveElement.Conforming;
            for (int i = 0; i < 3; ++i)
            {
                AddConformingNodeToMesh(i);

                // We assume that i) the level set encircles this element and intersects no other, ii) the interior is 
                // negative and the exterior positive, iii) the triangle's nodes are in counter-clockwise order. 
                // Thus if we traverse each edge is i+1 -> i, then the normal will point outside, meaning towards positive.
                int j = (i + 1) % 3;
                Mesh.Cells.Add((CellType.Line, new int[] { j, i }));
            }
        }

        /// <summary>
        /// Fixes the oriantation of each segment in the intersection mesh, so that its normal points towards the positive 
        /// halfplane defined by the level set surface.
        /// </summary>
        /// <param name="data"></param>
        private void FixCellsOrientation()
        {
            for (int c = 0; c < Mesh.Cells.Count; ++c)
            {
                // Find a normal (non-unit) of the segment
                int[] connectivity = Mesh.Cells[c].connectivity;
                double[] pA = Mesh.Vertices[connectivity[0]];
                double[] pB = Mesh.Vertices[connectivity[1]];
                double[] normal = { -(pB[1] - pA[1]), pB[0] - pA[0] }; // normal to AB, pi/2 counter-clockwise
                //normal.ScaleIntoThis(1.0 / normal.Norm2()); // Not needed here

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

                // Decide wether the normal points towards the positive halfplane. 
                bool normalPointsTowardsPositive;
                if (nodeLevelSets[farthestNodeIdx] > 0)
                {
                    // For the normal to point towards the positive halfplane, it must point towards a positive node.
                    normalPointsTowardsPositive = farthestNodeDot > 0;
                }
                else
                {
                    // For the normal to point towards the positive halfplane, it must point opposite to a negative node.
                    normalPointsTowardsPositive = farthestNodeDot < 0;
                }

                if (!normalPointsTowardsPositive) // Swap 2 vertices to flip the normal towards the positive halfplane.
                {
                    int swap = connectivity[0];
                    connectivity[0] = connectivity[1];
                    connectivity[1] = swap;
                }
            }
        }

        private void AddConformingNodeToMesh(int nodeIdx)
        {
            Mesh.Vertices.Add(nodeCoords[nodeIdx]);
            Mesh.IntersectedEdges.Add(new int[] { nodeIDs[nodeIdx] });
        }

        private static int[] DefineIntersectedEdge(IList<int> nodeIDs, int nodeIdx0, int nodeIdx1)
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

        private void AdjustLevelSetsToAvoidDegenerateIntersections()
        {
            // Deep copy the injected list, to avoid corrupting outside data. 
            var copy = new List<double>(nodeLevelSets.Count);
            copy.AddRange(nodeLevelSets);
            nodeLevelSets = copy;

            // Find intersected edges
            for (int i = 0; i < 3; ++i)
            {
                int j = (i + 1) % 3;
                if (nodeLevelSets[i] * nodeLevelSets[j] < 0)
                {
                    int nodeNeg, nodePos;
                    if (nodeLevelSets[i] < nodeLevelSets[j])
                    {
                        nodeNeg = i;
                        nodePos = j;
                    }
                    else
                    {
                        nodeNeg = j;
                        nodePos = i;
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

        private static double[] Interpolate(double levelSetNeg, double[] valuesNeg, double levelSetPos, double[] valuesPos)
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

        /// <summary>
        /// This will ensure machine precision will return the same result if this method is called with the same 
        /// arguments more than once.
        /// </summary>
        /// <param name="levelSetNeg"></param>
        /// <param name="levelSetPos"></param>
        [Conditional("DEBUG")]
        private static void CheckLevelSets(double levelSetNeg, double levelSetPos)
        {
            if (levelSetNeg >= levelSetPos)
            {
                throw new ArgumentException("Always provide the negative level set and corresponding values first");
            }
        }
    }
}
