using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.LinearAlgebra.Matrices
{
    public class CnstValues
    {
        public string exampleOutputPathGen = @"C:\Users\acivi\Documents\notes_elegxoi\REFERENCE_kanonikh_gewmetria_fe2_post_dg\examples\example1\input_matlab";
        public string solverPath { get { return exampleOutputPathGen + @"\model_overwrite\subdomain_data_solver"; } }

        public string debugString = @"C:\Users\acivi\Documents\notes_elegxoi_2\develp_3D";

        public string exampleDiscrInputPathGen { get { return exampleOutputPathGen + @"\input_matlab\Msolve_input"; } }

        public CnstValues()
        {

        }
    }
}
