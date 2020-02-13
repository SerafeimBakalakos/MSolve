using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ISAAR.MSolve.LinearAlgebra.Matrices
{
    public class CnstValues
    {
        public string exampleOutputPathGen = @"C:\Users\acivi\Documents\notes_elegxoi\REFERENCE_kanonikh_gewmetria_fe2_post_dg\examples\example1\input_matlab";
        public string solverPath { get { return exampleOutputPathGen + @"\model_overwrite\subdomain_data_solver"; } }

        public string debugString = @"C:\Users\acivi\Documents\notes_elegxoi_2\develp_3D";

        public string exampleDiscrInputPathGen { get { return exampleOutputPathGen + @"\Msolve_input"; } }

        public string interfaceSolverStatsPath { get { return exampleOutputPathGen + @"\Msolve_solution"; } }

        public CnstValues()
        {

        }

        public bool printInterfaceSolutionStats = true;

        public bool printGlobalSolutionStats = true;

        public void WriteToFileStringArray(string[] array, string path)
        {
            var writer = new StreamWriter(path);
            for (int i = 0; i < array.GetLength(0); ++i)
            {

                writer.Write(array[i]);
                //writer.Write(' ');

                writer.WriteLine();
            }
            writer.Flush();

            writer.Dispose();
        }


    }
}
