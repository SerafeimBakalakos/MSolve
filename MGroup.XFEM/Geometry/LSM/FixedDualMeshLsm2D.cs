﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;

//TODO: Shouldn't this extend DualMeshLsm2DBase directly and avoid any level set data?
namespace MGroup.XFEM.Geometry.LSM
{
    /// <summary>
    /// Only stores level set data in nodes that are inside the curve or belong to elements that are intersected by it.
    /// For signed distances of points away from the curve, the original geometry will be used, which may not be fast and able 
    /// to move.
    /// </summary>
    public class FixedDualMeshLsm2D : LocalDualMeshLsm2D
    {
        private readonly List<ICurve2D> closedCurves;

        public FixedDualMeshLsm2D(int id, DualMesh2D dualMesh, ICurve2D closedCurve) : base(id, dualMesh, closedCurve)
        {
            this.closedCurves = new List<ICurve2D>();
            this.closedCurves.Add(closedCurve);
        }

        public override void UnionWith(IClosedGeometry otherGeometry)
        {
            if (otherGeometry is FixedDualMeshLsm2D otherLsm)
            {
                this.closedCurves.AddRange(otherLsm.closedCurves);
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
                foreach (ICurve2D curve in closedCurves)
                {
                    double[] coords = dualMesh.FineMesh.GetNodeCoordinates(fineNodeID);
                    double distance = curve.SignedDistanceOf(coords);
                    if (distance < minDistance) minDistance = distance;
                }
                return minDistance;
            }
        }

    }
}
