﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Geometry.Tolerances;

namespace MGroup.XFEM.Entities
{
    public interface IGeometryModel
    {
        INodeEnricher Enricher { get; }

        IXDiscontinuity GetDiscontinuity(int discontinuityID);

        IEnumerable<IXDiscontinuity> EnumerateDiscontinuities();

        void InitializeGeometry();

        void InteractWithMesh();

        void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements);

    }
}