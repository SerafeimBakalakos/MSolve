using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Utilities
{
    public class CombinationTree
    {
        public CombinationTree(int numEntries)
        {
            if (numEntries < 1) throw new ArgumentException();
            Root = new Node(numEntries);
        }

        public Node Root { get; }

        public override string ToString()
        {
            var builder = new StringBuilder();
            Root.RecurseDownwards(node => builder.AppendLine(node.ToString()));
            return builder.ToString();
        }

        public class Node
        {
            public Node(Node parent, int index, int numEntries)
            {
                this.Parent = parent;
                this.Level = parent.Level + 1;

                this.Combination = new int[this.Level];
                Array.Copy(parent.Combination, this.Combination, parent.Level);
                this.Combination[this.Level - 1] = index;

                this.Children = new Node[numEntries - index - 1];
                for (int i = 0; i < this.Children.Length; ++i)
                {
                    this.Children[i] = new Node(this, index + 1 + i, numEntries);
                }
            }

            /// <summary>
            /// Create root node
            /// </summary>
            /// <param name="numEntries"></param>
            public Node(int numEntries)
            {
                Parent = null;
                Level = 0;
                Combination = new int[0];
                this.Children = new Node[numEntries];
                for (int i = 0; i < this.Children.Length; ++i)
                {
                    this.Children[i] = new Node(this, i, numEntries);
                }
            }

            public int Level { get; }

            public Node[] Children { get; }

            public int[] Combination { get; }

            public Node Parent { get; }

            public void RecurseDownwards(Action<Node> callback)
            {
                callback(this);
                foreach (Node node in Children) node.RecurseDownwards(callback);
            }

            public void RecurseDownwardsUntil(Func<Node, bool> callback)
            {
                bool success = callback(this);
                if (!success)
                {
                    foreach (Node node in Children) node.RecurseDownwardsUntil(callback);
                }
            }

            public void RecurseUpwards(Action<Node> callback)
            {
                foreach (Node node in Children) node.RecurseDownwards(callback);
                callback(this);
            }

            public override string ToString()
            {
                if (Combination.Length == 0) return "empty";
                var builder = new StringBuilder();
                builder.Append(Combination[0]);
                for (int i = 1; i < Combination.Length; ++i)
                {
                    builder.Append("-" + Combination[i]);
                }
                return builder.ToString();
            }
        }
    }
}
