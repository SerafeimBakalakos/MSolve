using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.PropagationCriteria;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Cracks.Geometry
{
    public class Propagator : IPropagator
    {
        public Propagator(ICrackGrowthDirectionCriterion growthDirectionCriterion)
        {
        }

        public PropagationLogger Logger => throw new NotImplementedException();

        public (double growthAngle, double growthLength) Propagate(Dictionary<int, Vector> subdomainFreeDisplacements, 
            double[] globalCrackTip, TipCoordinateSystem tipSystem, IEnumerable<IXCrackElement> tipElements)
        {
            throw new NotImplementedException();
        }
    }
}
