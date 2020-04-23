using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.MSAnalysis.RveTemplates.SupportiveClasses
{
    public static class GeometryProviderForMpi
    {
        internal static (double[][] o_xsunol_vectors, int[] sunol_nodes_numbering) GetGeometryAndNumbering()
        {
            #region to change every time for each example
            //double[][] o_xsunol_vectors, int[] sunol_nodes_numbering
            throw new NotImplementedException();
            #endregion

            double[][] o_xsunol_vectors = new double[][] {

                new double[] {
                    0,0,0,0,0,
                    0,0,0,0,0,0,0,
                    0,0,0,0,0},
                new double[]{0,0,0,
                0,0,0,0 } };


            // return (o_xsunol_vectors, sunol_nodes_numbering)
        }

        internal static (double[][] o_xsunol_vectors, int[] sunol_nodes_numbering) GetGeometryAndNumbering42()
        {
            var sunol_nodes_numbering = new int[6]
            {10,5,7,8,9,20};
            double[][] o_xsunol_vectors = new double[][] {
new double[]
{0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,
0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,0.00000000,
0.00000000,0.00000000},
new double[]
{1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,
1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,
1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,
1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,
1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,1.00000000,
1.00000000,1.00000000,1.00000000}};
            return (o_xsunol_vectors, sunol_nodes_numbering);
        }
    }
}
