using System;
using System.Collections.Generic;
using System.Text;
using MGroup.Solvers_OLD.DistributedTry1.Distributed.Topologies;

namespace MGroup.Solvers_OLD.DistributedTry1.Distributed.Environments
{
    /// <summary>
    /// Operations of the <see cref="ComputeSubnode"/>s of a given <see cref="ComputeNode"/> are run sequentially. 
    /// The data for a given <see cref="ComputeNode"/> and its <see cref="ComputeSubnode"/>s are assumed to exist in the same 
    /// shared memory address space.
    /// </summary>
    public class SequentialSubnodeEnvironment : ISubnodeEnvironment
    {
        public bool IsMemoryAddressSpaceShared => true;

        public IEnumerable<ComputeSubnode> Subnodes { get; set; }

        public Dictionary<int, T> CreateDictionaryPerSubnode<T>(Func<ComputeSubnode, T> createDataPerSubnode)
        {
            var result = new Dictionary<int, T>();
            foreach (ComputeSubnode subnode in Subnodes)
            {
                result[subnode.ID] = createDataPerSubnode(subnode);
            }
            return result;
        }

        public void DoPerSubnode(Action<ComputeSubnode> actionPerSubnode)
        {
            foreach (ComputeSubnode subnode in Subnodes)
            {
                actionPerSubnode(subnode);
            }
        }
    }
}
