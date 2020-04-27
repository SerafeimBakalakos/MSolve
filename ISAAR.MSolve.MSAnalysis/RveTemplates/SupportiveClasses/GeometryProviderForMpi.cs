using ISAAR.MSolve.LinearAlgebra.Matrices;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.MSAnalysis.RveTemplates.SupportiveClasses
{
    public static class GeometryProviderForMpi
    {
        internal static (double[][] o_xsunol_vectors, int[] sunol_nodes_numbering) GetGeometryAndNumbering()
        {
            if(CnstValues.exampleNo==42)
            { return GetGeometryAndNumbering42(); }
            if (CnstValues.exampleNo == 58)
            { throw new NotImplementedException(); }
            else { throw new NotImplementedException(); }
                

        }

        public static (int[] discrData, double[] modelScaleFactor) GetDiscrDataAndModelScaleFactor()
        {
            if (CnstValues.exampleNo == 42)
            { return GetDiscrDataAndModelScaleFactor42(); }
            if (CnstValues.exampleNo == 58)
            { throw new NotImplementedException(); }
            else { throw new NotImplementedException(); }
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

        public static (int[] discrData, double[] modelScaleFactor) GetDiscrDataAndModelScaleFactor42()
        {
            var discrData = new int[6]
            {10,5,7,8,9,20};
            double[] modelScaleFactor = new double[]
            {1};
            return (discrData, modelScaleFactor);
        }

        internal static (double[][] o_xsunol_vectors, int[] sunol_nodes_numbering) GetGeometryAndNumbering58()
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
        internal static (int[] discrData, double[] modelScaleFactor) GetDiscrDataAndModelScaleFactor58()
        {
            var modelScaleFactor = new double[1]
            {1.00000};
            var discrData = new int[5]
            {4,3,6,1,2};
            return (discrData, modelScaleFactor);
        }

    }
}
