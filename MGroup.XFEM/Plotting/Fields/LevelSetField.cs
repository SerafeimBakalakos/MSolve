using System.Collections.Generic;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Geometry
{
    public class LevelSetField
    {
        private readonly XModel model;
        private readonly IImplicitGeometry levelSet;
        private readonly ContinuousOutputMesh outMesh;

        public LevelSetField(XModel model, IImplicitGeometry levelSet)
        {
            this.model = model;
            this.levelSet = levelSet;
            this.outMesh = new ContinuousOutputMesh(model.Nodes, model.Elements);
        }

        public LevelSetField(XModel model, IImplicitGeometry levelSet, ContinuousOutputMesh outputMesh)
        {
            this.model = model;
            this.levelSet = levelSet;
            this.outMesh = outputMesh;
        }

        public IOutputMesh Mesh => outMesh;

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
