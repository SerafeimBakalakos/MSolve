using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using MGroup.XFEM.Elements;

namespace MGroup.XFEM.Entities
{
    public interface IXModel : IStructuralModel
    {
        List<XNode> XNodes { get; }

        IEnumerable<IXFiniteElement> EnumerateElements();
    }
}
