using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    public interface IEncoding<T>
    {
        T[] ComputeGenotype(double[] phenotype);
        T[] CreateRandomGenotype(); // This should be assigned to an Initializer class instead
        double[] ComputePhenotype(T[] genotype);
        //int[] IntegerPhenotype(T[] genotype);
    }
}
