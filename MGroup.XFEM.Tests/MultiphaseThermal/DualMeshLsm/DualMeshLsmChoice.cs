﻿using System;
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

        internal static DualMeshLsm3DBase Create(this DualMeshLsmChoice choice,
            int id, DualMesh3D dualMesh, ISurface3D closedSurface)
        {
            if (choice == DualMeshLsmChoice.Global)
            {
                return new GlobalDualMeshLsm3D(id, dualMesh, closedSurface);
            }
            else if (choice == DualMeshLsmChoice.Local)
            {
                return new LocalDualMeshLsm3D(id, dualMesh, closedSurface);
            }
            else if (choice == DualMeshLsmChoice.Fixed)
            {
                return new FixedDualMeshLsm3D(id, dualMesh, closedSurface);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
