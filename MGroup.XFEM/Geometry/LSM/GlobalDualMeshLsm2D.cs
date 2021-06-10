using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// Only stores level set data in nodes that are inside the curve or belong to elements that are intersected by it.
    /// </summary>
    public class GlobalDualMeshLsm2D : DualMeshLsm2DBase
    {
        private readonly double[] nodalLevelSets;

        public GlobalDualMeshLsm2D(int id, DualMesh2D dualMesh, ICurve2D closedCurve) : base(id, dualMesh, closedCurve)
        {
            IStructuredMesh fineMesh = dualMesh.FineMesh;
            nodalLevelSets = new double[fineMesh.NumNodesTotal];
            for (int n = 0; n < nodalLevelSets.Length; ++n)
            {
                double[] node = fineMesh.GetNodeCoordinates(fineMesh.GetNodeIdx(n));
                nodalLevelSets[n] = closedCurve.SignedDistanceOf(node);
            }
        }

        public override void UnionWith(IClosedGeometry otherGeometry)
        {
            if (otherGeometry is GlobalDualMeshLsm2D otherLsm)
            {
                if (this.nodalLevelSets.Length != otherLsm.nodalLevelSets.Length)
                {
                    throw new ArgumentException("Incompatible Level Set geometry");
                }
                for (int i = 0; i < this.nodalLevelSets.Length; ++i)
                {
                    this.nodalLevelSets[i] = Math.Min(this.nodalLevelSets[i], otherLsm.nodalLevelSets[i]);
                }
            }
            else throw new ArgumentException("Incompatible Level Set geometry");
        }

        protected override double GetLevelSet(int fineNodeID) => nodalLevelSets[fineNodeID];
    }
}
