﻿using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.XFEM_OLD.FreedomDegrees;

namespace ISAAR.MSolve.XFEM_OLD.Entities
{
    public class NodalLoad
    {
        public Node Node { get; }
        public StructuralDof DofType { get; }
        public double Value { get; }

        public NodalLoad(Node node, StructuralDof dofType, double value)
        {
            this.Node = node;
            this.DofType = dofType;
            this.Value = value;
        }
    }
}
