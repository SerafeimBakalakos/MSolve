﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;

//MODIFICATION NEEDED: Perhaps crack observers should be called by this class
namespace MGroup.XFEM.Entities
{
    public class CrackGeometryModel : IGeometryModel
    {
        private readonly XModel<IXCrackElement> physicalModel;

        public CrackGeometryModel(XModel<IXCrackElement> physicalModel)
        {
            this.physicalModel = physicalModel;
        }

        public Dictionary<int, ICrack> Cracks { get; } = new Dictionary<int, ICrack>();

        public INodeEnricher Enricher { get; set; }

        public IEnumerable<IXDiscontinuity> EnumerateDiscontinuities() => Cracks.Values;

        public IXDiscontinuity GetDiscontinuity(int discontinuityID) => Cracks[discontinuityID];

        public void InitializeGeometry()
        {
            foreach (ICrack crack in Cracks.Values) crack.InitializeGeometry();
        }

        public void InteractWithMesh()
        {
            foreach (ICrack crack in Cracks.Values) crack.InteractWithMesh();
        }

        public void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            foreach (ICrack crack in Cracks.Values) crack.UpdateGeometry(subdomainFreeDisplacements);
        }
    }
}