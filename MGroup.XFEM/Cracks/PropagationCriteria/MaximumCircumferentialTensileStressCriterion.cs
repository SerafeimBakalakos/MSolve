using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MGroup.XFEM.Cracks.PropagationCriteria
{
    // TODO: Research whether this work for heterogenegous materials as well.
    public class MaximumCircumferentialTensileStressCriterion: ICrackGrowthDirectionLaw2D
    {
        public MaximumCircumferentialTensileStressCriterion()
        {
        }

        public double ComputeGrowthAngle(double sif1, double sif2)
        {
            if (sif1 > 0)
            {
                double ratio = sif2 / sif1;
                return 2 * Math.Atan((-2 * ratio) / (1 + Math.Sqrt(1 + 8 * ratio * ratio)));
            }
            else throw new NotImplementedException("SIF 1 = " + sif1 + " <= 0. What happens in this case?");
        }
    }
}
