using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Entities
{
    public class XNode : INode
    {
        public XNode(int id, double[] coordinates)
        {
            this.ID = id;
            this.Coordinates = coordinates;
        }

        public List<Constraint> Constraints { get; } = new List<Constraint>();


        public double[] Coordinates { get; }

        public new Dictionary<int, IXFiniteElement> ElementsDictionary { get; } = new Dictionary<int, IXFiniteElement>();

        public Dictionary<IEnrichment, double> Enrichments { get; } = new Dictionary<IEnrichment, double>();

        public int ID { get; }

        public int NumEnrichedDofs => Enrichments.Count;

        //public IReadOnlyList<EnrichedDof> EnrichedDofs
        //{
        //    get
        //    {
        //        var dofs = new List<EnrichedDof>();
        //        foreach (IEnrichment enrichment in Enrichments.Keys) dofs.Add(enrichment.Dof);
        //        return dofs;
        //    }
        //}

        public bool IsEnriched => Enrichments.Count > 0;


        public new Dictionary<int, XSubdomain> SubdomainsDictionary { get; } = new Dictionary<int, XSubdomain>();

        public IPhase Phase { get; set; }

        public double X => Coordinates[0];

        public double Y => Coordinates[1];

        public double Z => Coordinates[2];

        Dictionary<int, ISubdomain> INode.SubdomainsDictionary => throw new System.NotImplementedException();

        public double CalculateDistanceFrom(XNode other)
        {
            if (Coordinates.Length == 2)
            {
                return this.Coordinates.Distance2D(other.Coordinates);
            }
            else if (Coordinates.Length == 3)
            {
                return this.Coordinates.Distance3D(other.Coordinates);
            }
            else throw new NotImplementedException();
        }
        public int CompareTo(INode other) => this.ID - other.ID;
    }
}