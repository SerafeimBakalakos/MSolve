using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.FEM.Transfer
{
    public struct NodalDisplacementDto
    {
        public double amount;
        public int node;
        public DofTypeDto dof;

        public NodalDisplacementDto(Node node, Constraint constraint)
        {
            this.node = node.ID;
            this.amount = constraint.Amount;
            this.dof = DofTypeTransfer.Serialization[constraint.DOF];
        }

        public void Deserialize(Dictionary<int, Node> allNodes)
        {
            Node targetNode = allNodes[this.node];
            var constraint = new Constraint() { Amount = this.amount, DOF = DofTypeTransfer.Deserialization[this.dof] };
            targetNode.Constraints.Add(constraint);
        }
    }
}
