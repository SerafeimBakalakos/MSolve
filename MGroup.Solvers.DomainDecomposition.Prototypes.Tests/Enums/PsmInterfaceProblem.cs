using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.DomainDecomposition.Prototypes.PSM;

namespace MGroup.Solvers.DomainDecomposition.Prototypes.Tests.Enums
{
    public enum PsmInterfaceProblem
    {
        Global, Distributed
    }

    public static class PsmInterfaceProblemExtensions
    {
        public static IPsmInterfaceProblem Create(this PsmInterfaceProblem choice)
        {
            switch (choice)
            {
                case PsmInterfaceProblem.Global: return new PsmInterfaceProblemGlobal();
                case PsmInterfaceProblem.Distributed: return new PsmInterfaceProblemDistributed();
                default: throw new NotImplementedException();
            }
        }
    }
}
