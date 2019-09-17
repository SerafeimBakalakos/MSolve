using ISAAR.MSolve.FEM.Transfer.Elements;
using ISAAR.MSolve.XFEM.Elements;
using System;
using System.Collections.Generic;
using System.Text;

//TODO: going through all the if clauses for each element is slow. Use a dictionary instead.
namespace ISAAR.MSolve.XFEM.Transfer.Elements
{
    public class XElementSerializer
    {
        public IXElementDto Serialize(IXFiniteElement element)
        {
            if (element is XContinuumElement2D continuum) return new XContinuumElement2DDto(continuum);
            throw new ArgumentException("Unknown element type.");
        }
    }
}
