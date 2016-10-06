using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Troschuetz.Random.Generators;

namespace ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms
{
    public class Individual : IComparable<Individual>
    {
        // Static variables is not the best solution. Multiple GA subpopulations may exist and they might (?) use different encodings.
        // A Factory perhaps?
        private static IEncoding encoding;
        private static bool isEncodingSet;

        // Can only be set once. Still this hack is not thread safe.
        public static IEncoding Encoding
        {
            get
            {
                if (!isEncodingSet) throw new InvalidOperationException("No encoding has been provided");
                return encoding;
            }
            set
            {
                if (isEncodingSet) throw new InvalidOperationException("Fitness has already been evaluated");
                encoding = value;
                isEncodingSet = true;
            }
        }

        public static Individual CreateRandom(int continuousVariablesCount, int intVariablesCount)
        {
            return new Individual(encoding.CreateRandomGenotype());
        }

        private double fitness = double.MaxValue;

        public Individual(bool[] chromosome, double fitness = double.MaxValue)
        {
            this.Chromosome = chromosome;
            this.fitness = fitness;
        }

        public bool[] Chromosome { get; private set; }
        public bool IsEvaluated { get; private set; }

        public double Fitness {
            get
            {
                if (!IsEvaluated) throw new InvalidOperationException("Fitness has not been evaluated yet");
                else return fitness;
            }
            set
            {
                if (IsEvaluated) throw new InvalidOperationException("Fitness has already been evaluated");
                else
                {
                    fitness = value;
                    IsEvaluated = true;
                }
            }
        }

        public double[] Phenotype()
        {
            return encoding.ComputePhenotype(Chromosome);
        }

        int IComparable<Individual>.CompareTo(Individual other)
        {
            return Math.Sign(this.Fitness - other.Fitness);
        }
    }
}
