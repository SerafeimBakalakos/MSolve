using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Phases;

namespace MGroup.XFEM.Elements
{
    public interface IXThermalElement : IXMultiphaseElement
    {
        //MODIFICATION NEEDED: I need to split this method into 2. One will return the Gauss points and one the materials. 
        // The Gauss points method will be defined in IXMultiphaseElement
        Dictionary<IPhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)>
            GetMaterialsForBoundaryIntegration();

        //MODIFICATION NEEDED: Same for this method.
        (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForBulkIntegration();
    }
}
