using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class GeometricModel
    {
        private readonly XModel physicalModel;

        public GeometricModel(XModel physicalModel)
        {
            this.physicalModel = physicalModel;
        }

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public void AssossiatePhasesNodes()
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].FindContainedNodes(physicalModel.Nodes);
            }
            defaultPhase.FindContainedNodes(physicalModel.Nodes);
        }
    }
}
