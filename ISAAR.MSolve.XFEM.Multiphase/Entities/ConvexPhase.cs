using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class ConvexPhase : IPhase
    {

        public ConvexPhase(int id)
        {
            if (id == DefaultPhase.DefaultPhaseID) throw new ArgumentException("Phase ID must be > 0");
            this.ID = id;
        }

        public int ID { get; }

        public List<PhaseBoundary> Boundaries { get; } = new List<PhaseBoundary>(4);

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        public void FindContainedNodes(IEnumerable<XNode> nodes)
        {
            ContainedNodes.Clear();
            foreach (XNode node in nodes)
            {
                if (Contains(node))
                {
                    ContainedNodes.Add(node);
                    node.SurroundingPhase = this;
                }
            }
        }

        private bool Contains(XNode node)
        {
            foreach (PhaseBoundary boundary in Boundaries)
            {
                double distance = boundary.Segment.SignedDistanceOf(node);
                bool sameSide = (distance > 0) && (boundary.PositivePhase == this);
                sameSide |= (distance < 0) && (boundary.NegativePhase == this);
                if (!sameSide) return false;
            }
            return true;
        }
    }
}
