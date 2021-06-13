using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Mesh;
using System.Diagnostics;

namespace MGroup.XFEM.Geometry.LSM
{
    public class LsmTri3Interaction
    {
        public (RelativePositionCurveElement, List<double[]> commonPoints) FindIntersection(
            List<double[]> nodeCoords, List<double> nodalLevelSets)
        {
            int numZeroNodes = 0;
            int numPosNodes = 0;
            int numNegNodes = 0;
            for (int i = 0; i < 3; ++i)
            {
                if (nodalLevelSets[i] < 0) ++numNegNodes;
                else if (nodalLevelSets[i] > 0) ++numPosNodes;
                else ++numZeroNodes;
            }

            var commonPoints = new List<double[]>();
            if (numZeroNodes == 3) // 3 conforming edges
            {
                //TODO: The client should decide whether to log this msg or throw an exception
                Debug.WriteLine(
                    $"Found element that has all its edges conforming to level set curve with ID {int.MinValue}");
                commonPoints.AddRange(nodeCoords);
                return (RelativePositionCurveElement.Conforming, commonPoints);
            }
            else if (numZeroNodes == 2) // 1 conforming edge
            {
                commonPoints.Add(nodeCoords[nodalLevelSets.FindIndex(phi => phi == 0)]);
                commonPoints.Add(nodeCoords[nodalLevelSets.FindLastIndex(phi => phi == 0)]);
                return (RelativePositionCurveElement.Conforming, commonPoints);
            }
            else if (numZeroNodes == 1)
            {
                int node0 = nodalLevelSets.FindIndex(phi => phi == 0);
                if ((numPosNodes == 0) || (numNegNodes == 0)) // Tangent (only 1 common point)
                {
                    commonPoints.Add(nodeCoords[node0]);
                    return (RelativePositionCurveElement.Tangent, commonPoints);
                }
                else // 1 intersection point and 1 node
                {
                    int nodeNeg = nodalLevelSets.FindIndex(phi => phi < 0);
                    int nodePos = nodalLevelSets.FindIndex(phi => phi > 0);
                    double[] intersection = Interpolate(nodalLevelSets[nodeNeg], nodeCoords[nodeNeg], 
                        nodalLevelSets[nodePos], nodeCoords[nodePos]);
                    commonPoints.Add(nodeCoords[node0]);
                    commonPoints.Add(intersection);
                    return (RelativePositionCurveElement.Intersecting, commonPoints);
                }
            }
            else
            {
                if ((numPosNodes == 0) || (numNegNodes == 0)) // Disjoint
                {
                    return (RelativePositionCurveElement.Disjoint, commonPoints);
                }
                else // 2 intersection points
                {
                    for (int i = 0; i < 3; ++i)
                    {
                        int j = (i + 1) % 3;
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
                            commonPoints.Add(intersection);
                        }
                    }
                    Debug.Assert(commonPoints.Count == 2);
                    return (RelativePositionCurveElement.Intersecting, commonPoints);
                }
            }
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
