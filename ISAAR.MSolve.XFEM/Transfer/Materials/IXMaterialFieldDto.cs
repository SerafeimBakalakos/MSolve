using ISAAR.MSolve.XFEM.Materials;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Transfer.Materials
{
    public interface IXMaterialFieldDto
    {
        int ID { get; }
        IXMaterialField2D Deserialize();
    }
}
