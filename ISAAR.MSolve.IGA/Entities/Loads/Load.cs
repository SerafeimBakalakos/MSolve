using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.IGA.Entities
{
    public class Load : INodalLoad
    {
        INode INodalLoad.Node => ControlPoint;
        public ControlPoint ControlPoint { get; set; }

        public IDofType DOF { get; set; }

        public double Amount { get; set; }
    }
}
