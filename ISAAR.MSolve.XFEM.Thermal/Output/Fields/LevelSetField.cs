using System.Collections.Generic;
using ISAAR.MSolve.XFEM.ThermalOLD.Entities;
using ISAAR.MSolve.XFEM.ThermalOLD.Curves;
using ISAAR.MSolve.XFEM.ThermalOLD.Output.Mesh;

namespace ISAAR.MSolve.XFEM.ThermalOLD.Output.Fields
{
    public class LevelSetField
    {
        private readonly XModel model;
        private readonly ICurve2D levelSet;
        private readonly ContinuousOutputMesh<XNode> outMesh;

        public LevelSetField(XModel model, ICurve2D levelSet)
        {
            this.model = model;
            this.levelSet = levelSet;
            this.outMesh = new ContinuousOutputMesh<XNode>(model.Nodes, model.Elements);
        }

        public LevelSetField(XModel model, ICurve2D levelSet, ContinuousOutputMesh<XNode> outputMesh)
        {
            this.model = model;
            this.levelSet = levelSet;
            this.outMesh = outputMesh;
        }

        public IOutputMesh<XNode> Mesh => outMesh;

        public IEnumerable<double> CalcValuesAtVertices()
        {
            var vals = new double[outMesh.NumOutVertices];
            int idx = 0;
            foreach (XNode node in outMesh.OriginalVertices) // same order as mesh.OutVertices
            {
                vals[idx++] = levelSet.SignedDistanceOf(node);
            }
            return vals;
        }
    }
}
