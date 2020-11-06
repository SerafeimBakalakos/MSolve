﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Elements
{
    public interface IElementFactory<TElement> where TElement: class, IXFiniteElement
    {
        TElement CreateElement(int id, CellType cellType, IReadOnlyList<XNode> nodes);
    }
}
