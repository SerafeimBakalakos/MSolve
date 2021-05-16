using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers.Distributed.Topologies;

namespace MGroup.Solvers.Distributed.Environments
{
    /// <summary>
    /// Specifies the execution of operations per <see cref="ComputeSubnode"/> of a given <see cref="ComputeNode"/>.
    /// In addition implementations make certain assumptions about the memory address spaces where the data of the
    /// <see cref="ComputeNode"/> and each of its <see cref="ComputeSubnode"/>s exist.
    /// </summary>
    /// <remarks>
    /// To be used as part of some <see cref="IComputeEnvironment"/> implementations, implementing the Strategy Method pattern
    /// </remarks>
    public interface ISubnodeEnvironment
    {
        bool IsMemoryAddressSpaceShared { get; }

        IEnumerable<ComputeSubnode> Subnodes { get; set; }

        /// <summary>
        /// Keys are the ids of the <see cref="ComputeSubnode"/> objects (<see cref="ComputeSubnode.ID"/>) managed by this 
        /// environment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="createPerNode"></param>
        Dictionary<int, T> CreateDictionaryPerSubnode<T>(Func<ComputeSubnode, T> createDataPerSubnode);

        void DoPerSubnode(Action<ComputeSubnode> actionPerSubnode);
    }
}
