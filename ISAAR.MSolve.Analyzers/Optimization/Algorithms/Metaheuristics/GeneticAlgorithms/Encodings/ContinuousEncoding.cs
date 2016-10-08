using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings
{
    class ContinuousEncoding : IEncoding<double>
    {
        public double[] ComputePhenotype(double[] genotype)
        {
            return genotype;
        }

        public double[] CreateRandomGenotype()
        {
            throw new NotImplementedException();
        }
    }
}
