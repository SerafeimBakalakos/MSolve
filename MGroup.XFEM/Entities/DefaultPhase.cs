using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

//TODO: Using a default phase messes up pretty much everything (avoiding it in collections, casts). Its geometry is too 
//      different to treat it as other phases. It is imply it, than using an explit phase.
namespace MGroup.XFEM.Entities
{
    public class DefaultPhase : IPhase
    {
        public DefaultPhase(int id)
        {
            this.ID = id;
        }

        public int ID { get; }

        public HashSet<IXFiniteElement> BoundaryElements => throw new NotImplementedException();

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        public List<PhaseBoundary> ExternalBoundaries { get; } = new List<PhaseBoundary>();

        public int MergeLevel => -1;

        public HashSet<IPhase> Neighbors { get; } = new HashSet<IPhase>();


        /// <summary>
        /// For best performance, call it after all other phases.
        /// </summary>
        /// <param name="nodes"></param>
        public void InteractWithNodes(IEnumerable<XNode> nodes)
        {
            foreach (XNode node in nodes)
            {
                if (node.Phase == null)
                {
                    ContainedNodes.Add(node);
                    node.Phase = this;
                }
            }
        }

        /// <summary>
        /// This must be called after all other phases have finished.
        /// </summary>
        /// <param name="elements"></param>
        public void InteractWithElements(IEnumerable<IXFiniteElement> elements)
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

        public bool Contains(XNode node)
        {
            throw new InvalidOperationException(
                "Call this method in every other valid phase. If none contains the point, then this phase does");
        }

        public bool Contains(XPoint point)
        {
            throw new InvalidOperationException(
                "Call this method in every other valid phase. If none contains the point, then this phase does");
        }

        public bool UnionWith(IPhase otherPhase)
        {
            throw new InvalidOperationException();
        }
    }
}
