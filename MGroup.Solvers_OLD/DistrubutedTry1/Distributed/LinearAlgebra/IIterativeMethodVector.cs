using System;
using System.Collections.Generic;
using System.Text;

//TODOMPI: Perhaps I should split it into subinterfaces and iterative methods should use generic types that are bounded by all 
//      these subinterfaces (e.g. IIndexable, ILinearlyCombinable, IDotMultipliable, etc.).
namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.LinearAlgebra
{
    public interface IIterativeMethodVector
    {
        void AddIntoThis(IIterativeMethodVector otherVector)
            => AxpyIntoThis(otherVector, +1);

        void AxpyIntoThis(IIterativeMethodVector otherVector, double otherCoefficient)
            => LinearCombinationIntoThis(1.0, otherVector, otherCoefficient);

        IIterativeMethodVector Copy()
        {
            IIterativeMethodVector clone = CreateZeroVectorWithSameFormat();
            clone.CopyFrom(this);
            return clone;
        }

        void CopyFrom(IIterativeMethodVector other);

        IIterativeMethodVector CreateZeroVectorWithSameFormat();

        double DotProduct(IIterativeMethodVector otherVector);

        void LinearCombinationIntoThis(double thisCoefficient, IIterativeMethodVector otherVector, double otherCoefficient);

        void SubtractIntoThis(IIterativeMethodVector otherVector)
            => AxpyIntoThis(otherVector, -1);
    }
}
