using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Enrichments.Items;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface
{
    public class MultiMaterialInterface
    {
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly MultiLsmClosedCurve2D geometry;
        private readonly SingleMaterialInterface[] singleMaterialInterfaces;

        public MultiMaterialInterface(MultiLsmClosedCurve2D geometry, IEnumerable<XThermalElement2D> modelElements,
            double[] interfaceResistances)
        {
            this.geometry = geometry;
            this.modelElements = modelElements;

            int numCurves = geometry.SingleCurves.Count;
            singleMaterialInterfaces = new SingleMaterialInterface[numCurves];
            for (int c = 0; c < numCurves; ++c)
            {
                singleMaterialInterfaces[c] = new SingleMaterialInterface(
                    geometry.SingleCurves[c], modelElements, interfaceResistances[c]);
            }
        }

        public void ApplyEnrichments()
        {
            foreach (var materialInterface in singleMaterialInterfaces) materialInterface.ApplyEnrichments();
        }
    }
}
