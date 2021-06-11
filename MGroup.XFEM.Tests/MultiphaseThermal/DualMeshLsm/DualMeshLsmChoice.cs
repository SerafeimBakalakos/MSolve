using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Tests.MultiphaseThermal.DualMeshLsm
{
    public enum DualMeshLsmChoice
    {
        Global, Local, Fixed
    }

    internal static class DualMeshLsmChoiceExtensions
    {
        internal static DualMeshLsm2DBase Create(this DualMeshLsmChoice choice, 
            int id, DualMesh2D dualMesh, ICurve2D closedCurve)
        {
            if (choice == DualMeshLsmChoice.Global)
            {
                return new GlobalDualMeshLsm2D(id, dualMesh, closedCurve);
            }
            else if (choice == DualMeshLsmChoice.Local)
            {
                return new LocalDualMeshLsm2D(id, dualMesh, closedCurve);
            }
            else if (choice == DualMeshLsmChoice.Fixed)
            {
                return new FixedDualMeshLsm2D(id, dualMesh, closedCurve);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
