using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public interface IPhase
    {
        List<PhaseBoundary> Boundaries { get; }
        HashSet<XNode> ContainedNodes { get; }
        HashSet<IXFiniteElement> ContainedElements { get; }
        int ID { get; }
        HashSet<IPhase> Neighbors { get; }

        void InteractWithElements(IEnumerable<IXFiniteElement> elements, IMeshTolerance meshTolerance);
        void InteractWithNodes(IEnumerable<XNode> nodes);

    }
}
