using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

//TODO: Using a default phase messes up pretty much everything (avoiding it in collections, casts). Its geometry is too 
//      different to treat it as other phases. It is imply it, than using an explit phase.
namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class DefaultPhase : IPhase
    {
        public const int DefaultPhaseID = 0;

        public int ID => DefaultPhaseID;

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        public List<PhaseBoundary> Boundaries { get; } = new List<PhaseBoundary>();

        public HashSet<IPhase> Neighbors { get; } = new HashSet<IPhase>();

        /// <summary>
        /// For best performance, call it after all other phases.
        /// </summary>
        /// <param name="nodes"></param>
        public void InteractWithNodes(IEnumerable<XNode> nodes)
        {
            foreach (XNode node in nodes)
            {
                if (node.SurroundingPhase == null)
                {
                    ContainedNodes.Add(node);
                    node.SurroundingPhase = this;
                }
            }
        }

        /// <summary>
        /// This must be called after all other phases have finished.
        /// </summary>
        /// <param name="elements"></param>
        public void InteractWithElements(IEnumerable<IXFiniteElement> elements, IMeshTolerance meshTolerance)
        {
            foreach (IXFiniteElement element in elements)
            {
                if (element.Phases.Count == 0)
                {
                    ContainedElements.Add(element);
                    element.Phases.Add(this);
                }
            }
        }

        public bool Contains(CartesianPoint point)
        {
            throw new InvalidOperationException(
                "Call this method in every other valid phase. If none contains the point, then this phase does");
        }

        public int CompareTo(IPhase other) => other.ID - this.ID;
    }
}
