using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public interface IPhase
    {
        List<PhaseBoundary> Boundaries { get; }
        HashSet<XNode> ContainedNodes { get; }
        HashSet<IXFiniteElement> ContainedElements { get; }
        int ID { get; }
        HashSet<IPhase> Neighbors { get; }

        bool Contains(XNode node);
        bool Contains(XPoint point);

        void InteractWithElements(IEnumerable<IXFiniteElement> elements);
        void InteractWithNodes(IEnumerable<XNode> nodes);

        /// <summary>
        /// If union is successful true will be returned, this phase will be the result of the union and
        /// <paramref name="otherPhase"/> should be removed. If it is not successful, false will be returned.
        /// </summary>
        /// <param name="otherPhase"></param>
        bool UnionWith(IPhase otherPhase);
    }

    public static class PhaseExtensions
    {
        public static bool Overlaps(this IPhase thisPhase, IPhase otherPhase)
        {
            foreach (XNode node in otherPhase.ContainedNodes)
            {
                if (thisPhase.ContainedNodes.Contains(node)) return true;
            }
            return false;
        }
    }
}
