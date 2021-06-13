using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using System.Diagnostics;

//TODO: Make sure all triangles have the same orientation. This orientation must be the same with triangles from other elements!
//      This could be done by pointing always towards a positive node. Also apply this to 2D.
//TODO: Make these intersections as smooth as the contours in ParaView
//TODO: Optimizations are possible, but may mess up readability. E.g. depending on the case, we can target specific edges that 
//      are intersected, instead of checking all of them
namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmTet4Interaction
    {
        public (RelativePositionCurveElement pos, IntersectionMesh3D intersectionMesh) FindIntersection(
            List<double[]> nodeCoords, List<double> nodeLevelSets)
        {
            Debug.Assert(nodeCoords.Count == 4);
            Debug.Assert(nodeLevelSets.Count == 4);

            int numZeroNodes = 0;
            int numPosNodes = 0;
            int numNegNodes = 0;
            for (int i = 0; i < 3; ++i)
            {
                if (nodeLevelSets[i] < 0) ++numNegNodes;
                else if (nodeLevelSets[i] > 0) ++numPosNodes;
                else ++numZeroNodes;
            }

            var intersectionMesh = new IntersectionMesh3D();
            if (numZeroNodes == 0)
            {
                if ((numPosNodes == 0) || (numNegNodes == 0)) 
                {
                    // Disjoint
                    return (RelativePositionCurveElement.Disjoint, intersectionMesh);
                }
                else if((numPosNodes == 1) || (numNegNodes == 1))
                {
                    // Intersection. 3 intersection points on edges of the single positive/negative node.
                    // The intersection mesh consists of a single triangle.
                    List<double[]> intersections = FindEdgeIntersections(nodeCoords, nodeLevelSets);
                    Debug.Assert(intersections.Count == 3);
                    foreach (double[] point in intersections)
                    {
                        intersectionMesh.Vertices.Add(point);
                    }
                    intersectionMesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
                    return (RelativePositionCurveElement.Intersecting, intersectionMesh);
                }
                else
                {
                    // Intersection. 4 intersection points on edges of the 2 positive nodes that connect them with the 2 
                    // negative nodes. The intersection mesh consists of 2 triangles.
                    Debug.Assert((numPosNodes == 2) && (numNegNodes == 2));
                    List<double[]> intersections = FindEdgeIntersections(nodeCoords, nodeLevelSets);
                    Debug.Assert(intersections.Count == 4);
                    foreach (double[] point in intersections)
                    {
                        intersectionMesh.Vertices.Add(point);
                    }

                    List<int[]> triangles = Utilities.Delauny4Points3D(intersections);
                    Debug.Assert(triangles.Count == 2);
                    intersectionMesh.Cells.Add((CellType.Tri3, triangles[0]));
                    intersectionMesh.Cells.Add((CellType.Tri3, triangles[1]));
                    return (RelativePositionCurveElement.Intersecting, intersectionMesh);
                }
            }
            else if (numZeroNodes == 1)
            {
                int nodeZero = nodeLevelSets.FindIndex(phi => phi == 0);
                intersectionMesh.Vertices.Add(nodeCoords[nodeZero]);

                if ((numPosNodes == 0) || (numNegNodes == 0))
                {
                    // Tangent. The zero node is the only common point.
                    return (RelativePositionCurveElement.Tangent, intersectionMesh);
                }
                else
                {
                    // Intersection. A single positive or negative node. 2 intersection points on its edges and the zero node.
                    // The intersection mesh consists of a single triangle.
                    Debug.Assert(((numPosNodes == 1) && (numPosNodes == 2)) || ((numPosNodes == 2) && (numPosNodes == 1)));
                    List<double[]> intersections = FindEdgeIntersections(nodeCoords, nodeLevelSets);
                    Debug.Assert(intersections.Count == 2);
                    intersectionMesh.Vertices.Add(intersections[0]);
                    intersectionMesh.Vertices.Add(intersections[1]);
                    intersectionMesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
                    return (RelativePositionCurveElement.Intersecting, intersectionMesh);
                }
            }
            else if (numZeroNodes == 2)
            {
                int nodeZero0 = nodeLevelSets.FindIndex(phi => phi == 0);
                int nodeZero1 = nodeLevelSets.FindLastIndex(phi => phi == 0);
                intersectionMesh.Vertices.Add(nodeCoords[nodeZero0]);
                intersectionMesh.Vertices.Add(nodeCoords[nodeZero1]);

                if ((numPosNodes == 0) || (numNegNodes == 0))
                {
                    // Tangent. The 2 zero nodes define a single common line segment, but no cell.
                    //intersectionMesh.Cells.Add((CellType.Line, new int[] { 0, 1 })); // Nope, edges are different than cells.
                    return (RelativePositionCurveElement.Tangent, intersectionMesh);
                }
                else
                {
                    // Intersection. 2 zero nodes and 1 intersection point on the edge connecting the positive and negative edge.
                    // The intersection mesh consists of a single triangle.
                    Debug.Assert((numPosNodes == 1) && (numPosNodes == 1));
                    List<double[]> intersections = FindEdgeIntersections(nodeCoords, nodeLevelSets);
                    Debug.Assert(intersections.Count == 1);
                    intersectionMesh.Vertices.Add(intersections[0]);
                    intersectionMesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
                    return (RelativePositionCurveElement.Intersecting, intersectionMesh);
                }
            }
            else if (numZeroNodes == 3)
            {
                // Conforming. The intersection mesh consists of the face connecting the 3 zero nodes.
                for (int i = 0; i < 4; ++i)
                {
                    if (nodeLevelSets[i] == 0)
                    {
                        intersectionMesh.Vertices.Add(nodeCoords[i]);
                    }
                }
                intersectionMesh.Cells.Add((CellType.Tri3, new int[] { 0, 1, 2 }));
                return (RelativePositionCurveElement.Conforming, intersectionMesh);
            }
            else
            {
                // Conforming. All faces are conforming, which means that the lsm surface hugs the Tet4 and nothing else.
                Debug.Assert(numZeroNodes == 4);
                //TODO: The client should decide whether to log this msg or throw an exception
                Debug.WriteLine(
                    $"Found element that has all its faces conforming to level set surface with ID {int.MinValue}");
                for (int i = 0; i < 4; ++i)
                {
                    intersectionMesh.Vertices.Add(nodeCoords[i]);
                    intersectionMesh.Cells.Add((CellType.Line, new int[] { i, (i + 1) % 3, (i + 2) % 3 }));
                }
                return (RelativePositionCurveElement.Conforming, intersectionMesh);
            }
        }

        private static List<double[]> FindEdgeIntersections(List<double[]> nodeCoords, List<double> nodalLevelSets)
        {
            var intersections = new List<double[]>();
            for (int i = 0; i < 4; ++i)
            {
                for (int j = i + 1; j < 4; ++j)
                {
                    if (nodalLevelSets[i] * nodalLevelSets[j] < 0)
                    {
                        int nodeNeg, nodePos;
                        if (nodalLevelSets[i] < nodalLevelSets[j])
                        {
                            nodeNeg = i;
                            nodePos = j;
                        }
                        else
                        {
                            nodeNeg = j;
                            nodePos = i;
                        }
                        double[] intersection = Interpolate(nodalLevelSets[nodeNeg], nodeCoords[nodeNeg],
                            nodalLevelSets[nodePos], nodeCoords[nodePos]);
                        intersections.Add(intersection);
                    }
                }
            }
            return intersections;
        }

        private static double[] Interpolate(double levelSetNeg, double[] valuesNeg, double levelSetPos, double[] valuesPos)
        {
            Debug.Assert(valuesNeg.Length == valuesPos.Length);
            if (levelSetNeg >= levelSetPos)
            {
                // This will ensure machine precision will return the same result if this method is called with the same 
                // arguments more than once.
                throw new ArgumentException("Always provide the negative level set and corresponding values first");
            }

            // The intersection point between these nodes can be found using the linear interpolation, see Sukumar 2001
            // The same interpolation can be used for coordinates or any other values.
            //
            double k = -levelSetNeg / (levelSetPos - levelSetNeg);
            var result = new double[valuesNeg.Length];
            for (int i = 0; i < valuesNeg.Length; ++i)
            {
                result[i] = valuesNeg[i] + k * (valuesPos[i] - valuesNeg[i]);
            }
            return result;
        }
    }
}
