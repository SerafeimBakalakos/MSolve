using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public static class MeshUtilities
    {
        public static HashSet<TElement> FindElementsIntersectedByCircle<TElement>(Circle2D circle,
            TElement startElement) where TElement : class, IXFiniteElement
        {
            //TODO: It should also be tested (it is easy)
            var internalElements = new HashSet<TElement>();
            var intersectedElements = new HashSet<TElement>();

            int pos = RelativePositionOfCircleElement(circle, startElement);
            if (pos < 0) internalElements.Add(startElement);
            else if (pos == 0) intersectedElements.Add(startElement);
            else throw new ArgumentException("The provided starting element must not lie outside the circle");

            HashSet<TElement> neighbors = FindNeighbors(startElement);
            var processedElements = new HashSet<TElement>();
            processedElements.Add(startElement);
            bool allElementsExternal = false;
            while (!allElementsExternal)
            {
                allElementsExternal = true;
                var nextNeighbors = new HashSet<TElement>();
                foreach (TElement element in neighbors)
                {
                    if (!processedElements.Contains(element))
                    {
                        processedElements.Add(element);
                        pos = RelativePositionOfCircleElement(circle, element);
                        if (pos < 0)
                        {
                            internalElements.Add(element);
                            allElementsExternal = false;
                        }
                        else if (pos == 0)
                        {
                            intersectedElements.Add(element);
                            allElementsExternal = false;
                        }
                        nextNeighbors.UnionWith(FindNeighbors(element));
                    }
                }
                neighbors = nextNeighbors;
            }

            return intersectedElements;
        }

        public static HashSet<TElement> FindNeighbors<TElement>(TElement element) where TElement : class, IXFiniteElement
        {
            var neighbors = new HashSet<TElement>();
            foreach (XNode node in element.Nodes)
            {
                foreach (TElement other in node.ElementsDictionary.Values)
                {
                    if (other != element) neighbors.Add(other);
                }
            }
            return neighbors;
        }

        /// <summary>
        /// Positive = outside, Negative = inside, Zero = intersected
        /// </summary>
        public static int RelativePositionOfCircleElement(Circle2D circle, IElement element)
        {
            var polygon = new ConvexPolygon2D(element.Nodes.Select(n => n.Coordinates()).ToArray());
            CirclePolygonPosition pos = polygon.FindRelativePositionOfCircle(circle);
            if (pos == CirclePolygonPosition.Disjoint) return +1;
            else if (pos == CirclePolygonPosition.PolygonInsideCircle) return -1;
            else if ((pos == CirclePolygonPosition.CircleInsidePolygon) || (pos == CirclePolygonPosition.Intersecting)) return 0;
            else throw new NotImplementedException();
        }
    }
}
