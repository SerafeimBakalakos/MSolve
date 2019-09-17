using ISAAR.MSolve.XFEM.Materials;
using System;
using System.Collections.Generic;
using System.Text;

//TODO: going through all the if clauses for each material is slow. Use a dictionary instead.
namespace ISAAR.MSolve.XFEM.Transfer.Materials
{
    public class XMaterialSerializer
    {
        public IXMaterialFieldDto Serialize(IXMaterialField2D material)
        {
            if (material is HomogeneousElasticMaterial2D elastic2D) return new HomogeneousElasticMaterial2DDto(elastic2D);
            throw new ArgumentException("Unknown material type.");
        }
    }
}
