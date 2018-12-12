﻿using System;
using System.Collections.Generic;
using System.Text;

//TODO: Perhaps I should have providers for LAPACKE, rather than LAPACK, or both. LAPACKE seems to be a wrapper, but this whole 
//      project does just that. LAPACKE functions may also explicitly transpose matrices, which should not be hidden
//      from the matrix/vector classes. MKL provides both LAPACK and LAPACKE, but other major packages 
//      (e.g. dotNumerics, cuSolver) only provide the LAPACK interface.
//TODO: Even better would be to write my own LAPACK wrapper, similarly to LAPACKE, which would wrap all LAPACK providers and 
//      be simple enough for regular use. It would handle the work arrays, the output codes and use enums as input. These enums 
//      will be the same for BLAS and LAPACK. It will not support row major layout, unless that method does not require 
//      explicit transposition. 
namespace ISAAR.MSolve.LinearAlgebra.Providers
{
    public interface ILapackProvider
    {
        /// <summary>
        /// QR factorization. The matrix is general, stored in full column major format. See
        /// http://www.netlib.org/lapack/explore-html/dd/d9a/group__double_g_ecomputational_ga436228e38ef5c55e3229502afa2c4220.html#ga436228e38ef5c55e3229502afa2c4220
        /// </summary>
        void Dgelqf(int m, int n, double[] a, int offsetA, int ldA, double[] tau, int offsetTau,
            double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// QR factorization. The matrix is general, stored in full column major format. See
        /// http://www.netlib.org/lapack/explore-html/dd/d9a/group__double_g_ecomputational_ga3766ea903391b5cf9008132f7440ec7b.html#ga3766ea903391b5cf9008132f7440ec7b
        /// </summary>
        void Dgeqrf(int m, int n, double[] a, int offsetA, int ldA, double[] tau, int offsetTau,
            double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// LU factorization. The matrix is general, stored in full column major format. See
        /// http://www.netlib.org/lapack/explore-html/dd/d9a/group__double_g_ecomputational_ga0019443faea08275ca60a734d0593e60.html#ga0019443faea08275ca60a734d0593e60
        /// </summary>
        void Dgetrf(int m, int n, double[] a, int offsetA, int ldA, int[] ipiv, int offsetIpiv, ref int info);

        /// <summary>
        /// Matrix inversion using the factorized data of 
        /// <see cref="Dgetrf(int, int, double[], int, int, int[], int, ref int)"/>. The matrix is general, stored in full column 
        /// major format. See
        /// http://www.netlib.org/lapack/explore-html/dd/d9a/group__double_g_ecomputational_ga56d9c860ce4ce42ded7f914fdb0683ff.html#ga56d9c860ce4ce42ded7f914fdb0683ff
        /// </summary>
        void Dgetri(int n, double[] a, int offsetA, int ldA, int[] ipiv, int offsetIpiv, 
            double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// Linear system solution using the factorized data of 
        /// <see cref="Dgetrf(int, int, double[], int, int, int[], int, ref int)"/>. 
        /// The matrix is general, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/dd/d9a/group__double_g_ecomputational_ga58e332cb1b8ab770270843221a48296d.html#ga58e332cb1b8ab770270843221a48296d
        /// </summary>
        void Dgetrs(string transA, int n, int nRhs, double[] a, int offsetA, int ldA, int[] ipiv, int offsetIpiv,
            double[] b, int offsetB, int ldB, ref int info);

        /// <summary>
        /// Generates an m-by-n matrix holding the LQ factor Q, which has orthonormal columns, using the factorized data of 
        /// <see cref="Dgelqf(int, int, double[], int, int, double[], int, double[], int, int, ref int)"/>.
        /// The matrix is general, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_ga97adc24a3547a789a3ab145688e3a3ca.html#ga97adc24a3547a789a3ab145688e3a3ca
        /// </summary>
        void Dorglq(int m, int n, int k, double[] a, int offsetA, int ldA, double[] tau, int offsetTau,
            double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// Generates an m-by-n matrix holding the QR factor Q, which has orthonormal columns, using the factorized data of 
        /// <see cref="Dgeqrf(int, int, double[], int, int, double[], int, double[], int, int, ref int)"/>.
        /// The matrix is general, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_ga14b45f7374dc8654073aa06879c1c459.html#ga14b45f7374dc8654073aa06879c1c459
        /// </summary>
        void Dorgqr(int m, int n, int k, double[] a, int offsetA, int ldA, double[] tau, int offsetTau,
            double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// Multiplies the QR factor Q with another matrix C, using the factorized data of 
        /// <see cref="Dgelqf(int, int, double[], int, int, double[], int, double[], int, int, ref int)"/>.
        /// The matrix is general, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_ga99147464f79c5447c08eead5a06a90ce.html#ga99147464f79c5447c08eead5a06a90ce
        /// </summary>
        void Dormlq(string side, string transQ, int m, int n, int k, double[] a, int offsetA, int ldA, double[] tau,
            int offsetTau, double[] c, int offsetC, int ldC, double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// Multiplies the QR factor Q with another matrix C, using the factorized data of 
        /// <see cref="Dgeqrf(int, int, double[], int, int, double[], int, double[], int, int, ref int)"/>.
        /// The matrix is general, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_ga17b0765a8a0e6547bcf933979b38f0b0.html#ga17b0765a8a0e6547bcf933979b38f0b0
        /// </summary>
        void Dormqr(string side, string transQ, int m, int n, int k, double[] a, int offsetA, int ldA, double[] tau,
            int offsetTau, double[] c, int offsetC, int ldC, double[] work, int offsetWork, int lWork, ref int info);

        /// <summary>
        /// Cholesky factorization. The matrix is symmetric positive definite, stored in full column major format. See
        /// http://www.netlib.org/lapack/explore-html/d1/d7a/group__double_p_ocomputational_ga2f55f604a6003d03b5cd4a0adcfb74d6.html#ga2f55f604a6003d03b5cd4a0adcfb74d6
        /// </summary>
        void Dpotrf(string uplo, int n, double[] a, int offsetA, int ldA, ref int info);

        /// <summary>
        /// Matrix inversion using the factorized data of 
        /// <see cref="Dpotrf(string, int, double[], int, int, ref int)"/>. 
        /// The matrix is symmetric positive definite, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/d1/d7a/group__double_p_ocomputational_ga9dfc04beae56a3b1c1f75eebc838c14c.html#ga9dfc04beae56a3b1c1f75eebc838c14c
        /// </summary>
        void Dpotri(string uplo, int n, double[] a, int offsetA, int ldA, ref int info);

        /// <summary>
        /// Linear system solution using the factorized data of 
        /// <see cref="Dpotrf(string, int, double[], int, int, ref int)"/>. 
        /// The matrix is symmetric positive definite, stored in full column major format. See 
        /// http://www.netlib.org/lapack/explore-html/d1/d7a/group__double_p_ocomputational_ga167aa0166c4ce726385f65e4ab05e7c1.html#ga167aa0166c4ce726385f65e4ab05e7c1
        /// </summary>
        void Dpotrs(string uplo, int n, int nRhs, double[] a, int offsetA, int ldA,
            double[] b, int offsetB, int ldB, ref int info);

        /// <summary>
        /// Cholesky factorization. The matrix is symmetric positive definite, stored in packed column major format. See
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_ga1fa71e503eabce2514406ba9d872ba63.html#ga1fa71e503eabce2514406ba9d872ba63
        /// </summary>
        void Dpptrf(string uplo, int n, double[] a, int offsetA, ref int info);

        /// <summary>
        /// Matrix inversion using the factorized data of 
        /// <see cref="Dpotrf(string, int, double[], int, int, ref int)"/>. 
        /// The matrix is symmetric positive definite, stored in packed column major format. See 
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_gae50ca6e928ba3eb2917521a6c886a41b.html#gae50ca6e928ba3eb2917521a6c886a41b
        /// </summary>
        void Dpptri(string uplo, int n, double[] a, int offsetA, ref int info);

        /// <summary>
        /// Linear system solution using the factorized data of 
        /// <see cref="Dpotrf(string, int, double[], int, int, ref int)"/>. 
        /// The matrix is symmetric positive definite, stored in packed column major format. See 
        /// http://www.netlib.org/lapack/explore-html/da/dba/group__double_o_t_h_e_rcomputational_gaa0b8f7830a459c434c84ce5e7a939850.html#gaa0b8f7830a459c434c84ce5e7a939850
        /// </summary>
        void Dpptrs(string uplo, int n, int nRhs, double[] a, int offsetA, double[] b, int offsetB, int ldB, ref int info);
    }
}