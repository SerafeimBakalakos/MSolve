using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Entities
{
    public interface IXModel : IStructuralModel
    {
        List<XNode> XNodes { get; }

        IEnumerable<IXFiniteElement> EnumerateElements();

        void Initialize();

        void Update(Dictionary<int, Vector> subdomainFreeDisplacements);
    }
}
