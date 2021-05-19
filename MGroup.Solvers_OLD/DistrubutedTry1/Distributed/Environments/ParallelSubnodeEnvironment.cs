using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments
{
    /// <summary>
    /// Operations of the <see cref="ComputeSubnode"/>s of a given <see cref="ComputeNode"/> are run in parallel using TPL. 
    /// The data for each <see cref="ComputeNode"/> and its <see cref="ComputeSubnode"/>s are assumed to exist in the same 
    /// shared memory address space.
    /// </summary>
    public class ParallelSubnodeEnvironment : ISubnodeEnvironment
    {
        public bool IsMemoryAddressSpaceShared => true;

        public IEnumerable<ComputeSubnode> Subnodes { get; set; }

        public Dictionary<int, T> CreateDictionaryPerSubnode<T>(Func<ComputeSubnode, T> createDataPerSubnode)
        {
            var result = new Dictionary<int, T>();
            Parallel.ForEach(Subnodes, subnode =>
            {
                T data = createDataPerSubnode(subnode);
                lock (result) result[subnode.ID] = data;
            });
            return result;
        }

        public void DoPerSubnode(Action<ComputeSubnode> actionPerSubnode)
        {
            Parallel.ForEach(Subnodes, subnode =>
            {
                actionPerSubnode(subnode);
            });
        }
    }
}
