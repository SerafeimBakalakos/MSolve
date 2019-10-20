using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.MaterialInterface.Geometry;
using ISAAR.MSolve.XFEM.Thermal.Output.Mesh;

namespace ISAAR.MSolve.XFEM.Thermal.Output.Fields
{
    public class LevelSetField
    {
        private readonly XModel model;
        private readonly IMaterialInterfaceGeometry levelSet;
        private readonly ContinuousOutputMesh<XNode> outMesh;

        public LevelSetField(XModel model, IMaterialInterfaceGeometry levelSet)
        {
            this.model = model;
            this.levelSet = levelSet;
            this.outMesh = new ContinuousOutputMesh<XNode>(model.Nodes, model.Elements);
        }

        public LevelSetField(XModel model, IMaterialInterfaceGeometry levelSet, ContinuousOutputMesh<XNode> outputMesh)
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
