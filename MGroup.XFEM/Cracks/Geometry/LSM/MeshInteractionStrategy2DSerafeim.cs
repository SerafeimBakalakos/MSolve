using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    /// <summary>
    /// Correctly identifies the elements around the crack tip that Stolarska's criterion always marks as tip elements. Uses the
    /// intersections of the suspected tip element with the 1st order zero crack body level set. As this is done for only a 
    /// handful elements (or none), the performance hit is negligible. It may also be possible to extend this criterion to 
    /// higher order LSM by using an appropriate crack-element intersection formula.
    /// </summary>
    public class MeshInteractionStrategy2DSerafeim : ILsmMeshInteractionStrategy
    {
        public MeshInteractionStrategy2DSerafeim()
        {
        }

        public ElementCharacterization FindRelativePositionOf(IXFiniteElement element,
            Dictionary<XNode, double> levelSetsBody, Dictionary<XNode, double> levelSetsTip)
        {
            double minBodyLevelSet = double.MaxValue;
            double maxBodyLevelSet = double.MinValue;
            double minTipLevelSet = double.MaxValue;
            double maxTipLevelSet = double.MinValue;

            foreach (XNode node in element.Nodes)
            {
                double bodyLevelSet = levelSetsBody[node];
                double tipLevelSet = levelSetsTip[node];
                if (bodyLevelSet < minBodyLevelSet) minBodyLevelSet = bodyLevelSet;
                if (bodyLevelSet > maxBodyLevelSet) maxBodyLevelSet = bodyLevelSet;
                if (tipLevelSet < minTipLevelSet) minTipLevelSet = tipLevelSet;
                if (tipLevelSet > maxTipLevelSet) maxTipLevelSet = tipLevelSet;
            }

            if (minBodyLevelSet * maxBodyLevelSet > 0.0) return ElementCharacterization.Standard;
            else // The element is intersected by the zero body level set.
            {
                if (minTipLevelSet > 0) return ElementCharacterization.Standard; // intersected by the crack's extension
                else if (maxTipLevelSet < 0) return ElementCharacterization.Intersected;
                else // Stolarska's criterion marks all the next as tip elements
                {
                    Dictionary<double[], double> intersections = 
                        FindIntersectionsAndTipLevelSets(element, levelSetsBody, levelSetsTip);
                    Debug.Assert((intersections.Count == 2) || (intersections.Count == 1)); // 1 is veeeeery improbable
                    double tipLevelSetInter1 = intersections.First().Value;
                    double tipLevelSetInter2 = intersections.Last().Value;
                    if ((tipLevelSetInter1 > 0) && (tipLevelSetInter2 > 0))
                    {
                        return ElementCharacterization.Standard;
                    }
                    else if ((tipLevelSetInter1 < 0) && (tipLevelSetInter2 < 0))
                    {
                        return ElementCharacterization.Intersected;
                    }
                    else return ElementCharacterization.Tip;
                }
            }
        }

        private Dictionary<double[], double> FindIntersectionsAndTipLevelSets(IXFiniteElement element,
            Dictionary<XNode, double> levelSetsBody, Dictionary<XNode, double> levelSetsTip)
        {
            // Find intersections of element with the zero body level set. 
            //TODO: abstract this procedure and reuse it here and in LSM
            var intersections = new Dictionary<double[], double>();
            var nodes = element.Nodes;
            for (int i = 0; i < nodes.Count; ++i)
            {
                XNode node1 = nodes[i];
                XNode node2 = nodes[(i + 1) % nodes.Count];
                double bodyLevelSet1 = levelSetsBody[node1];
                double bodyLevelSet2 = levelSetsBody[node2];

                if (bodyLevelSet1 * bodyLevelSet2 < 0.0)
                {
                    // The intersection point between these nodes can be found using the linear interpolation, see 
                    // Sukumar 2001
                    double k = -bodyLevelSet1 / (bodyLevelSet2 - bodyLevelSet1);
                    double x = node1.X + k * (node2.X - node1.X);
                    double y = node1.Y + k * (node2.Y - node1.Y);

                    double tipLevelSet1 = levelSetsTip[node1];
                    double tipLevelSet2 = levelSetsTip[node2];
                    double tipLevelSet = tipLevelSet1 + k * (tipLevelSet2 - tipLevelSet1);

                    intersections.Add(new double[] { x, y }, tipLevelSet);
                }
                else if (bodyLevelSet1 == 0.0) intersections[node1.Coordinates] = levelSetsTip[node1]; // TODO: perhaps some tolerance is needed.
                else if (bodyLevelSet2 == 0.0) intersections[node2.Coordinates] = levelSetsTip[node2];
            }

            return intersections;
        }
    }
}
