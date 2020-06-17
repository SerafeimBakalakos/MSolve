using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class HollowPhase : IPhase
    {
        public List<PhaseBoundary> Boundaries => throw new NotImplementedException();

        public HashSet<XNode> ContainedNodes => throw new NotImplementedException();

        public HashSet<IXFiniteElement> ContainedElements => throw new NotImplementedException();

        public int ID => throw new NotImplementedException();

        public HashSet<IPhase> Neighbors => throw new NotImplementedException();

        public bool Contains(XNode node)
        {
            throw new NotImplementedException();
        }

        public bool Contains(XPoint point)
        {
            throw new NotImplementedException();
        }

        public void InteractWithElements(IEnumerable<IXFiniteElement> elements)
        {
            throw new NotImplementedException();
        }

        public void InteractWithNodes(IEnumerable<XNode> nodes)
        {
            throw new NotImplementedException();
        }
    }
}
