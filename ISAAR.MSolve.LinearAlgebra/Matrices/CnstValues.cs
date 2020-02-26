using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ISAAR.MSolve.LinearAlgebra.Matrices
{
    public class CnstValues
    {
        bool runCluster = false; 

        //public string exampleOutputPathGen = @"C:\Users\acivi\Documents\notes_elegxoi\REFERENCE_kanonikh_gewmetria_fe2_post_dg\examples\example1\input_matlab";

        public string exampleOutputPathGen { get { if (runCluster) { return @"C:\Users\cluster\Documents\Large_rves\examples\example3\input_matlab"; }
                else { return @"C:\Users\acivi\Documents\notes_elegxoi\REFERENCE_kanonikh_gewmetria_fe2_post_dg\examples\example2\input_matlab"; } } }
        public string solverPath { get { return exampleOutputPathGen + @"\model_overwrite\subdomain_data_solver"; } }

        public string debugString = @"C:\Users\acivi\Documents\notes_elegxoi_2\develp_3D";

        public string exampleDiscrInputPathGen { get { return exampleOutputPathGen + @"\Msolve_input"; } }

        public string interfaceSolverStatsPath { get { return exampleOutputPathGen + @"\Msolve_solution"; } }

        public string rand_data_vec_path { get { if (runCluster) { return @"C:\Users\cluster\Documents\Large_rves\files_from_old_paths\rand_data.txt"; }
                else { return @"C:\Users\acivi\Documents\notes_elegxoi_2\develop_random_geometry_Msolve\REF2_50_000_renu_new_multiple_algorithms_check_develop_copy_for_progr_random_direct_in_C\rand_data.txt";  }
            } }
        public CnstValues()
        {

        }

        public bool printInterfaceSolutionStats = false;

        public bool printGlobalSolutionStats = true;

        public bool printPcgMatRhsEtc_AndInterfaceProblemStats = false; // 

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
