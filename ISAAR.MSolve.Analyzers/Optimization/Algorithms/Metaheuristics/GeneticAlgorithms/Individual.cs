﻿using ISAAR.MSolve.Analyzers.Optimization.Algorithms.Metaheuristics.GeneticAlgorithms.Encodings;
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

        int IComparable<Individual>.CompareTo(Individual other)
        {
            return Math.Sign(this.Fitness - other.Fitness);
        }
    }
}
