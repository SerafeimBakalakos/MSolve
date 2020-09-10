using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public interface IPhase : IComparable<IPhase>
    {
        List<PhaseBoundary> Boundaries { get; }
        HashSet<XNode> ContainedNodes { get; }
        HashSet<IXFiniteElement> ContainedElements { get; }
        int ID { get; }
        HashSet<IPhase> Neighbors { get; }

        bool Contains(CartesianPoint point);
        void InteractWithElements(IEnumerable<IXFiniteElement> elements, IMeshTolerance meshTolerance);
        void InteractWithNodes(IEnumerable<XNode> nodes);

    }
}
