using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Entities
{
    public interface IXModel : IStructuralModel
    {
        List<XNode> Nodes { get; }

        IEnumerable<IXFiniteElement> EnumerateElements();
    }
}
