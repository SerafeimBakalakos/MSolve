using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    public interface IEncoding
    {
        bool[] CreateRandomGenotype();
        double[] Phenotype(bool[] genotype);
        int[] IntegerPhenotype(bool[] genotype);
    }
}
