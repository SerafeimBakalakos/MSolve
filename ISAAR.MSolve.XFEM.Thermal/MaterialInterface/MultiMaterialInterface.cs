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
        private readonly GeometricModel2D geometricModel;
        private readonly SingleMaterialInterface[] singleMaterialInterfaces;

        public MultiMaterialInterface(GeometricModel2D geometricModel, IEnumerable<XThermalElement2D> modelElements,
            double[] interfaceResistances)
        {
            this.geometricModel = geometricModel;
            this.modelElements = modelElements;

            int numCurves = geometricModel.SingleCurves.Count;
            singleMaterialInterfaces = new SingleMaterialInterface[numCurves];
            for (int c = 0; c < numCurves; ++c)
            {
                singleMaterialInterfaces[c] = new SingleMaterialInterface(geometricModel, modelElements, interfaceResistances[c]);
            }
        }

        public void ApplyEnrichments()
        {
            foreach (var materialInterface in singleMaterialInterfaces) materialInterface.ApplyEnrichments();
        }
    }
}
