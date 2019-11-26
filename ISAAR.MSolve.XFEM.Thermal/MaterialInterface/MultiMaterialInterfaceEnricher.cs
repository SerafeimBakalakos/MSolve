using System.Collections.Generic;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface.SingularityResolving;

namespace ISAAR.MSolve.XFEM.Thermal.MaterialInterface
{
    /// <summary>
    /// This works if the material interfaces do not intersect each other.
    /// </summary>
    public class MultiMaterialInterfaceEnricher
    {
        private readonly IEnumerable<XThermalElement2D> modelElements;
        private readonly GeometricModel2D geometricModel;
        private readonly SingleMaterialInterfaceEnricher[] singleMaterialInterfaces;

        public MultiMaterialInterfaceEnricher(GeometricModel2D geometricModel, IEnumerable<XThermalElement2D> modelElements,
            double[] interfaceResistances)
        {
            this.geometricModel = geometricModel;
            this.modelElements = modelElements;

            int numCurves = geometricModel.SingleCurves.Count;
            singleMaterialInterfaces = new SingleMaterialInterfaceEnricher[numCurves];
            var resolver = new RelativeAreaResolver(geometricModel);
            for (int c = 0; c < numCurves; ++c)
            {
                singleMaterialInterfaces[c] = new SingleMaterialInterfaceEnricher(
                    geometricModel.SingleCurves[c], modelElements, interfaceResistances[c], resolver);
            }
        }

        public void ApplyEnrichments()
        {
            foreach (var materialInterface in singleMaterialInterfaces) materialInterface.ApplyEnrichments();
        }
    }
}
