﻿using System.Runtime.InteropServices;

namespace ISAAR.MSolve.LinearAlgebra.Providers.PInvoke
{
    /// <summary>
    /// Platform invoke methods for Intel MKL's Sparse BLAS. These are not covered by any nuget packages. Also see MKL's C user 
    /// guide. 
    /// Authors: Serafeim Bakalakos
    /// </summary>
    internal class SparseBlas
    {
        //TODO: this has been deprecated. Does this hurt performance much? Should I use the inspector-executor functions instead?
        [DllImport("mkl_rt", CallingConvention = CallingConvention.Cdecl, EntryPoint = "mkl_dcsrcsc")]
        internal static extern int DCsrCsc(int[] job, int n, double[] acsr, int[] ja, int[] ia, 
            double[] acsc, int[] ja1, int[] ia1, ref int info);
    }
}