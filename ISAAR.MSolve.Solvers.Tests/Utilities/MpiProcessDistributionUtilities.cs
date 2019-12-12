using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Distributed;
using ISAAR.MSolve.LinearAlgebra.Distributed.Exceptions;
using MPI;

namespace ISAAR.MSolve.Solvers.Tests.Utilities
{
    public static class MpiProcessDistributionUtilities
    {
        /// <summary>
        /// Example: 3 processes & 8 subdomains:
        /// process 0: s0, s1, s2
        /// process 1: s3, s4, s5
        /// process 2: s6, s7
        /// </summary>
        /// <param name="numProcesses"></param>
        /// <param name="numSubdomains"></param>
        /// <returns></returns>
        public static ProcessDistribution DefineProcesses(int numProcesses, int numSubdomains)
        {
            if ((numProcesses < 2) || (numProcesses > numSubdomains))
            {
                throw new MpiProcessesException("Number of MPI processes must belong to [2, 4]");
            }

            // Gather a set of subdomains that are multiple of the number of processes and distribute them evenly. 
            // Then each of the remainder subdomains goes to one of the first processes. 
            int div = numSubdomains / numProcesses;
            int mod = numSubdomains % numProcesses;
            var numSubdomainsPerProcess = new int[numProcesses];
            for (int p = 0; p < numProcesses; ++p) numSubdomainsPerProcess[p] = div;
            for (int m = 0; m < mod; ++m) ++numSubdomainsPerProcess[m];


            int subdomainID = 0; //TODO: Risky
            int[][] processesToSubdomains = new int[numProcesses][];
            for (int p = 0; p < numProcesses; ++p)
            {
                processesToSubdomains[p] = new int[numSubdomainsPerProcess[p]];
                for (int i = 0; i < numSubdomainsPerProcess[p]; ++i) processesToSubdomains[p][i] = subdomainID++;
            }

            int master = 0;
            var procs = new ProcessDistribution(Communicator.world, master, processesToSubdomains);
            //Console.WriteLine($"(process {procs.OwnRank}) Hello World!"); // Run this to check if MPI works correctly.
            //PrintProcessDistribution(procs);
            return procs;
        }
    }
}