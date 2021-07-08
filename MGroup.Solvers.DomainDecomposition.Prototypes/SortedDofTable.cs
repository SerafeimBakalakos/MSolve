﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.FreedomDegrees;

namespace MGroup.Solvers.DomainDecomposition.Prototypes
{
    public class SortedDofTable : IEnumerable<(int row, int dof, int idx)>
    {
        private readonly SortedDictionary<int, SortedDictionary<int, int>> data = 
            new SortedDictionary<int, SortedDictionary<int, int>>();

        public int this[int node, int dof]
        {
            get => data[node][dof];
            set
            {
                bool nodeExists = data.TryGetValue(node, out SortedDictionary<int, int> nodeData);
                if (!nodeExists)
                {
                    nodeData = new SortedDictionary<int, int>();
                    data[node] = nodeData;
                }
                nodeData[dof] = value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<(int row, int dof, int idx)> GetEnumerator()
        {
            foreach (var wholeRow in data)
            {
                foreach (var colValPair in wholeRow.Value)
                {
                    yield return (wholeRow.Key, colValPair.Key, colValPair.Value);
                }
            }
        }

        public bool TryAdd(int node, int dof, int index)
        {
            bool nodeExists = data.TryGetValue(node, out SortedDictionary<int, int> nodeData);
            if (!nodeExists)
            {
                nodeData = new SortedDictionary<int, int>();
                data[node] = nodeData;
            }
            bool dofExists = nodeData.TryAdd(dof, index);
            return dofExists;
        }

        
    }
}
