using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public interface IPhase
    {
        int ID { get; }
        HashSet<XNode> ContainedNodes { get; }
        HashSet<IXFiniteElement> ContainedElements { get; }

        void FindContainedNodes(IEnumerable<XNode> nodes);
    }
}
