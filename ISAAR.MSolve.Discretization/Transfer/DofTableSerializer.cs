using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;

namespace ISAAR.MSolve.Discretization.Transfer
{
    public class DofTableSerializer
    {
        private readonly IDofSerializer dofSerializer;

        public DofTableSerializer(IDofSerializer dofSerializer)
        {
            this.dofSerializer = dofSerializer;
        }

        public DofTable Deserialize(int[] flatTable, Dictionary<int, INode> nodes)
        {
            Debug.Assert(flatTable.Length % 3 == 0,
                "The provided flattened table does not correspond to a dof table. It must have a length divisible by 3, where"
                + " the first element of each triad is the node, the second is the dof type and the thrd is the dof index");
            int numEntires = flatTable.Length / 3;
            var table = new DofTable();
            for (int i = 0; i < numEntires; ++i)
            {
                INode node = nodes[3 * i];
                IDofType dofType = dofSerializer.Deserialize(3 * i + 1);
                int dofIdx = 3 * i + 2;
                table[node, dofType] = dofIdx; //TODO: perhaps this can be optimized to avoid checking if there is already such an entry.
            }
            return table;
        }

        public int[] Serialize(DofTable table)
        {
            int entryCount = table.EntryCount;
            var flatTable = new int[entryCount];
            int counter = -1;
            foreach ((INode row, IDofType col, int val) in table)
            {
                flatTable[++counter] = row.ID;                          // Node
                flatTable[++counter] = dofSerializer.Serialize(col);    // Dof type
                flatTable[++counter] = val;                             // Dof index in vectors/matrices
            }
            return flatTable;
        }
    }
}
