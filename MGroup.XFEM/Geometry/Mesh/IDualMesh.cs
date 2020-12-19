﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MGroup.XFEM.Geometry.Mesh
{
    public interface IDualMesh
    {
        IStructuredMesh FemMesh { get; }

        IStructuredMesh LsmMesh { get; }

        /// <summary>
        /// If the node in the LSM mesh does not correspond to a node in the FEM mesh, -1 will be returned
        /// </summary>
        /// <param name="lsmNodeID"></param>
        int MapNodeLsmToFem(int lsmNodeID);

        int MapNodeIDFemToLsm(int femNodeID);

        int MapElementLsmToFem(int lsmElementID);

        int[] MapElementFemToLsm(int femElementID);

        DualMeshPoint CalcShapeFunctions(int femElementID, double[] femNaturalCoords);
    }
}