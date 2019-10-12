using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Entities;

namespace ISAAR.MSolve.XFEM.Thermal.Entities
{
    public class NodalLoad
    {
        public XNode Node { get; }
        public StructuralDof DofType { get; }
        public double Value { get; }

        public NodalLoad(XNode node, StructuralDof dofType, double value)
        {
            this.Node = node;
            this.DofType = dofType;
            this.Value = value;
        }
    }
}
