using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class DefaultPhase : IPhase
    {
        public const int DefaultPhaseID = 0;

        public int ID { get; } = 0;

        public HashSet<XNode> ContainedNodes { get; } = new HashSet<XNode>();

        public HashSet<IXFiniteElement> ContainedElements { get; } = new HashSet<IXFiniteElement>();

        /// <summary>
        /// For best performance, call it after all other phases.
        /// </summary>
        /// <param name="nodes"></param>
        public void FindContainedNodes(IEnumerable<XNode> nodes)
        {
            ContainedNodes.Clear();
            foreach (XNode node in nodes)
            {
                if (node.SurroundingPhase == null) node.SurroundingPhase = this;
            }
        }

    }
}
