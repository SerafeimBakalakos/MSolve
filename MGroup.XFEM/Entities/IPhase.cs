using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
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

        //void InteractWithElements(IEnumerable<IXFiniteElement> elements, IMeshTolerance meshTolerance);
        void InteractWithNodes(IEnumerable<XNode> nodes);

    }
}
