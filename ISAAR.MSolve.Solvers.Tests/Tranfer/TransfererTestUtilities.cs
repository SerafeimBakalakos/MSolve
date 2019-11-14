using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Transfer;
using MPI;

namespace ISAAR.MSolve.Solvers.Tests.Tranfer
{
    public static class TransfererTestUtilities
    {
        private const int numProcesses = 4;

        public enum SubdomainDistribution
        {
            OnePerProcess, Uniform, Variable
        }

        public enum TransfererChoice
        {
            PerSubdomain, AltogetherFlattened
        }

        public static ActiveSubdomains DetermineActiveSubdomains(ProcessDistribution procs)
        {
            // Every third subdomain is active
            return new ActiveSubdomains(procs, s => (s + 1) % 3 == 0);
        }

        public static ProcessDistribution DetermineProcesses(SubdomainDistribution subdomainDistribution)
        {
            int master = 0;
            var processesToSubdomains = new int[numProcesses][];
            int numSubdomains = 0;
            for (int p = 0; p < numProcesses; ++p)
            {
                int numSubdomainsOfThisProcess;
                if (subdomainDistribution == SubdomainDistribution.OnePerProcess) numSubdomainsOfThisProcess = 1;
                else if (subdomainDistribution == SubdomainDistribution.Uniform) numSubdomainsOfThisProcess = 5;
                else if (subdomainDistribution == SubdomainDistribution.Variable) numSubdomainsOfThisProcess = p + 1;
                else throw new NotImplementedException();
                processesToSubdomains[p] = new int[numSubdomainsOfThisProcess];
                {
                    for (int i = 0; i < numSubdomainsOfThisProcess; ++i)
                    {
                        processesToSubdomains[p][i] = numSubdomains++;
                    }
                }
            }
            return new ProcessDistribution(Communicator.world, master, processesToSubdomains);
        }

        public static ISubdomainDataTransferer DetermineTransferer(TransfererChoice transfererChoice, ProcessDistribution procs)
        {
            if (transfererChoice == TransfererChoice.PerSubdomain) return new TransfererPerSubdomain(procs);
            else if (transfererChoice == TransfererChoice.AltogetherFlattened) return new TransfererAltogetherFlattened(procs);
            else throw new NotImplementedException();
        }
    }
}
