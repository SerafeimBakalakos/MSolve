﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Numerical.Exceptions;
using ISAAR.MSolve.Numerical.LinearAlgebra.Matrices;

namespace ISAAR.MSolve.Numerical.LinearAlgebra
{
    static class Preconditions
    {
        public static void CheckVectorDimensions(IVectorView vector1, IVectorView vector2)
        {
            if (vector1.Length != vector2.Length)
            {
                string message = string.Format("Vector1 has length of {0}, while vector2 has length of {1}", 
                    vector1.Length, vector2.Length);
                throw new NonMatchingDimensionsException(message);
            }
               
        }

        public static void CheckMatrixDimensionsSame(IMatrixView matrix1, IMatrixView matrix2)
        {
            if ((matrix1.Rows != matrix2.Rows) != (matrix1.Columns != matrix2.Columns))
            {
                string message = string.Format(
                    "Matrix1 has dimensions ({0}x{1}), while matrix2 has dimensions ({2}x{3})",
                    matrix1.Rows, matrix1.Columns, matrix2.Rows, matrix2.Columns);
                throw new NonMatchingDimensionsException(message);
            }
        }

        public static void CheckMultiplicationDimensions(IMatrixView leftMatrix, IMatrixView rightMatrix)
        {
            if (leftMatrix.Columns != rightMatrix.Rows)
            {
                string message = string.Format(
                    "Left matrix has dimensions ({0}x{1}), while right matrix has dimensions ({2}x{3})",
                    leftMatrix.Rows, leftMatrix.Columns, rightMatrix.Rows, rightMatrix.Columns);
                throw new NonMatchingDimensionsException(message);
            }
        }

        //Temporary, till I fix inheritence
        public static void CheckMultiplicationDimensions(MatrixMKL leftMatrix, MatrixMKL rightMatrix)
        {
            if (leftMatrix.NumColumns != rightMatrix.NumRows)
            {
                string message = string.Format(
                    "Left matrix has dimensions ({0}x{1}), while right matrix has dimensions ({2}x{3})",
                    leftMatrix.NumRows, leftMatrix.NumColumns, rightMatrix.NumRows, rightMatrix.NumColumns);
                throw new NonMatchingDimensionsException(message);
            }
        }

        public static void CheckMultiplicationDimensions(IMatrixView leftMatrix, IVectorView rightVector)
        {
            if (leftMatrix.Columns != rightVector.Length)
            {
                string message = string.Format(
                    "Left matrix has dimensions ({0}x{1}), while right vector has dimensions ({2}x1)",
                    leftMatrix.Rows, leftMatrix.Columns, rightVector.Length);
                throw new NonMatchingDimensionsException(message);
            }
        }

        //Temporary, till I fix inheritence
        public static void CheckMultiplicationDimensions(IMatrixViewMKL leftMatrix, DenseVector rightVector)
        {
            if (leftMatrix.NumColumns != rightVector.Length)
            {
                string message = string.Format(
                    "Left matrix has dimensions ({0}x{1}), while right vector has dimensions ({2}x1)",
                    leftMatrix.NumRows, leftMatrix.NumColumns, rightVector.Length);
                throw new NonMatchingDimensionsException(message);
            }
        }

        public static void CheckMultiplicationDimensions(IVectorView leftVector, IMatrixView rightMatrix)
        {
            if (leftVector.Length != rightMatrix.Rows)
            {
                string message = string.Format(
                    "Left matrix has dimensions (1x{0}), while right matrix has dimensions ({1}x{2})",
                    leftVector.Length, rightMatrix.Rows, rightMatrix.Columns);
                throw new NonMatchingDimensionsException(message);
            }
        }

        public static void CheckSystemSolutionDimensions(IMatrixViewMKL matrix, DenseVector rhsVector)
        {
            if (matrix.NumRows != rhsVector.Length)
            {
                string message = string.Format(
                    "Matrix has dimensions ({0}x{1}), while the right hand side vector has dimensions ({2}x1)",
                    matrix.NumRows, matrix.NumColumns, rhsVector.Length);
                throw new NonMatchingDimensionsException(message);
            }
        }
    }
}
