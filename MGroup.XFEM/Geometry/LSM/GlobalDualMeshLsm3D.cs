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
    /// Stores level set data in all nodes of the mesh.
    /// </summary>
    public class GlobalDualMeshLsm3D : DualMeshLsm3DBase
    {
        private readonly double[] nodalLevelSets;

        public GlobalDualMeshLsm3D(int id, DualMesh3D dualMesh, ISurface3D closedSurface) : base(id, dualMesh)
        {
            IStructuredMesh fineMesh = dualMesh.FineMesh;
            nodalLevelSets = new double[fineMesh.NumNodesTotal];
            for (int n = 0; n < nodalLevelSets.Length; ++n)
            {
                double[] node = fineMesh.GetNodeCoordinates(fineMesh.GetNodeIdx(n));
                nodalLevelSets[n] = closedSurface.SignedDistanceOf(node);
            }
        }

        public override void UnionWith(IClosedGeometry otherGeometry)
        {
            if (otherGeometry is GlobalDualMeshLsm3D otherLsm)
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
