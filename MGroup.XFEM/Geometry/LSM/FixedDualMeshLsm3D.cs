using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Shouldn't this extend DualMeshLsm3DBase directly and avoid any level set data?
namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// Only stores level set data in nodes that are inside the curve or belong to elements that are intersected by it.
    /// For signed distances of points away from the curve, the original geometry will be used, which may not be fast and able 
    /// to move.
    /// </summary>
    public class FixedDualMeshLsm3D : LocalDualMeshLsm3D
    {
        private readonly List<ISurface3D> closedSurfaces;

        public FixedDualMeshLsm3D(int id, DualMesh3D dualMesh, ISurface3D closedSurface) : base(id, dualMesh, closedSurface)
        {
            this.closedSurfaces = new List<ISurface3D>();
            this.closedSurfaces.Add(closedSurface);
        }

        public override void UnionWith(IClosedGeometry otherGeometry)
        {
            if (otherGeometry is FixedDualMeshLsm3D otherLsm)
            {
                this.closedSurfaces.AddRange(otherLsm.closedSurfaces);
            }
            else throw new ArgumentException("Incompatible Level Set geometry");
            base.UnionWith(otherGeometry);
        }

        protected override double GetLevelSet(int fineNodeID)
        {
            bool isNodeNear = this.nodalLevelSets.TryGetValue(fineNodeID, out double levelSet);
            if (isNodeNear) return levelSet;
            else
            {
                double minDistance = double.MaxValue;
                foreach (ISurface3D surface in closedSurfaces)
                {
                    double[] coords = dualMesh.FineMesh.GetNodeCoordinates(fineNodeID);
                    double distance = surface.SignedDistanceOf(coords);
                    if (distance < minDistance) minDistance = distance;
                }
                return minDistance;
            }
        }

    }
}
