using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.IGA.Entities
{
    public class CollocationPoint3D:NaturalPoint, INode
    {
        public CollocationPoint3D(int id,double xi, double eta, double zeta, bool isBoundary=false) : base(xi, eta, zeta)
        {
            _isBoundary = isBoundary;
            _id = id;
            Surfaces= new List<Surface>();
        }

        public CollocationPoint3D(int id,double[] coordinates, bool isBoundary = false) : base(coordinates)
        {
            _isBoundary = isBoundary;
            _id = id;
            Surfaces = new List<Surface>();
        }

        public bool IsBoundary
        {
            get => _isBoundary;
            set => _isBoundary = value;
        }

        public List<Surface> Surfaces { get; set; }

        private int _id;
        private bool _isBoundary;

        public int ID  { get => _id; set => _id = value; }

        int INode.ID => _id;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public List<Constraint> Constraints { get; }
        public int Multiplicity => SubdomainsDictionary.Count;
        public Dictionary<int, ISubdomain> SubdomainsDictionary { get; }

        public int CompareTo(INode other) => this.ID - other.ID;

        public double[] tU { get; set; }
        public double[] tX { get; set; }

        public double[] oX { get; set; }

        double[] oVn { get; set; }
        double[] tVn { get; set; }
        double[] tV1 { get; set; }

        double[] tV2 { get; set; }
        double[] INode.oVn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        double[] INode.tVn { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        double[] INode.tV1 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        double[] INode.tV2 { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    public enum Surface
    {
        Front, Back, Top, Bottom,
        Right, Left
    }
}
