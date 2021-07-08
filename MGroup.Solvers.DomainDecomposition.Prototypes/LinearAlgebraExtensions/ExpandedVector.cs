using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Reduction;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.LinearAlgebraExtensions
{
    public class ExpandedVector : IVector
    {
        public SortedDictionary<int, Vector> SubdomainVectors = new SortedDictionary<int, Vector>();

        public double this[int index] => throw new NotImplementedException();

        public int Length
        {
            get
            {
                int totalLength = 0;
                foreach (int s in SubdomainVectors.Keys)
                {
                    totalLength += SubdomainVectors[s].Length;
                }
                return totalLength;
            }
        }

        public void AddIntoThisNonContiguouslyFrom(int[] thisIndices, IVectorView otherVector, int[] otherIndices)
        {
            throw new NotImplementedException();
        }

        public void AddIntoThisNonContiguouslyFrom(int[] thisIndices, IVectorView otherVector)
        {
            throw new NotImplementedException();
        }

        public IVector Axpy(IVectorView otherVector, double otherCoefficient)
        {
            IVector result = this.Copy();
            result.AxpyIntoThis(otherVector, otherCoefficient);
            return result;
        }

        public void AxpyIntoThis(IVectorView otherVector, double otherCoefficient)
        {
            var otherCasted = (ExpandedVector)otherVector;
            foreach (int s in this.SubdomainVectors.Keys)
            {
                this.SubdomainVectors[s].AxpyIntoThis(otherCasted.SubdomainVectors[s], otherCoefficient);
            }
        }

        public void AxpySubvectorIntoThis(int destinationIndex, IVectorView sourceVector, double sourceCoefficient, int sourceIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            foreach (int s in this.SubdomainVectors.Keys)
            {
                SubdomainVectors[s].Clear();
            }
        }

        public IVector Copy(bool copyIndexingData = false)
        {
            var result = new ExpandedVector();
            foreach (int s in this.SubdomainVectors.Keys)
            {
                result.SubdomainVectors[s] = this.SubdomainVectors[s].Copy();
            }
            return result;
        }

        public void CopyFrom(IVectorView sourceVector)
        {
            var otherCasted = (ExpandedVector)sourceVector;
            foreach (int s in this.SubdomainVectors.Keys)
            {
                this.SubdomainVectors[s].CopyFrom(otherCasted.SubdomainVectors[s]);
            }
        }

        public void CopyNonContiguouslyFrom(int[] thisIndices, IVectorView otherVector, int[] otherIndices)
        {
            throw new NotImplementedException();
        }

        public void CopyNonContiguouslyFrom(IVectorView otherVector, int[] otherIndices)
        {
            throw new NotImplementedException();
        }

        public void CopySubvectorFrom(int destinationIndex, IVectorView sourceVector, int sourceIndex, int length)
        {
            throw new NotImplementedException();
        }

        public double[] CopyToArray() => CopyToFullVector().RawData;

        public Vector CopyToFullVector()
        {
            int totalLength = 0;
            foreach (int s in SubdomainVectors.Keys)
            {
                totalLength += SubdomainVectors[s].Length;
            }

            var result = Vector.CreateZero(this.Length);
            int start = 0;
            foreach (int s in SubdomainVectors.Keys)
            {
                result.CopySubvectorFrom(start, SubdomainVectors[s], 0, SubdomainVectors[s].Length);
                start += SubdomainVectors[s].Length;
            }

            return result;
        }

        public IVector CreateZeroVectorWithSameFormat()
        {
            throw new NotImplementedException();
        }

        public IVector DoEntrywise(IVectorView vector, Func<double, double, double> binaryOperation)
        {
            throw new NotImplementedException();
        }

        public void DoEntrywiseIntoThis(IVectorView otherVector, Func<double, double, double> binaryOperation)
        {
            throw new NotImplementedException();
        }

        public IVector DoToAllEntries(Func<double, double> unaryOperation)
        {
            throw new NotImplementedException();
        }

        public void DoToAllEntriesIntoThis(Func<double, double> unaryOperation)
        {
            throw new NotImplementedException();
        }

        public double DotProduct(IVectorView vector)
        {
            throw new NotImplementedException();
        }

        public bool Equals(IIndexable1D other, double tolerance = 1E-13)
        {
            throw new NotImplementedException();
        }

        public IVector LinearCombination(double thisCoefficient, IVectorView otherVector, double otherCoefficient)
        {
            IVector result = this.Copy();
            result.LinearCombinationIntoThis(thisCoefficient, otherVector, otherCoefficient);
            return result;
        }

        public void LinearCombinationIntoThis(double thisCoefficient, IVectorView otherVector, double otherCoefficient)
        {
            var otherCasted = (ExpandedVector)otherVector;
            foreach (int s in this.SubdomainVectors.Keys)
            {
                this.SubdomainVectors[s].LinearCombinationIntoThis(thisCoefficient, otherCasted.SubdomainVectors[s], otherCoefficient);
            }
        }

        public double Norm2()
        {
            throw new NotImplementedException();
        }

        public double Reduce(double identityValue, ProcessEntry processEntry, ProcessZeros processZeros, Finalize finalize)
        {
            throw new NotImplementedException();
        }

        public IVector Scale(double scalar)
        {
            IVector result = this.Copy();
            result.ScaleIntoThis(scalar);
            return result;
        }

        public void ScaleIntoThis(double scalar)
        {
            foreach (int s in this.SubdomainVectors.Keys)
            {
                this.SubdomainVectors[s].ScaleIntoThis(scalar);
            }
        }

        public void Set(int index, double value)
        {
            throw new NotImplementedException();
        }

        
    }
}
