using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MPI;

//TODO: What about the case where more than one subdomains are delegated to the same process?
//TODO: If there are point2point communications between two processes, should this be transfered to all of them?
namespace ISAAR.MSolve.Discretization.Transfer
{
    /// <summary>
    /// This meant for the master process that handles communication. It should not be transfered to other processes.
    /// </summary>
    public class ModelDistribution
    {
        public ModelDistribution(ISubdomain[] processesToSubdomains)
        {
            this.ProcesesToSubdomains = processesToSubdomains;
            this.SubdomainsToProcesses = new Dictionary<ISubdomain, int>();
            for (int p = 0; p < processesToSubdomains.Length; ++p)
            {
                this.SubdomainsToProcesses[processesToSubdomains[p]] = p;
            }
        }

        //TODO: These should be readonly, unless some other need arises.
        public ISubdomain[] ProcesesToSubdomains { get; }
        public Dictionary<ISubdomain, int> SubdomainsToProcesses { get; }
    }
}
