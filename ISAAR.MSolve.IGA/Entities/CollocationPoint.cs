﻿using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;

namespace ISAAR.MSolve.IGA.Entities
{
	public class CollocationPoint2D: NaturalPoint, INode
    {
        private int _id;
        private bool _isBoundary;
        public int ID { get => _id; set => ID = value; }
    int INode.ID { get => _id; }

        public bool IsBoundary
        {
            get => _isBoundary;
            set => _isBoundary = value;
        }

        public double X { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Y { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double Z { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public List<Constraint> Constraints => new List<Constraint>();

        public int Multiplicity => SubdomainsDictionary.Count;

        public Dictionary<int, ISubdomain> SubdomainsDictionary => throw new NotImplementedException();

        public CollocationPoint2D(int id, double xi, double eta, bool isBoundary=false) : base(xi, eta)
		{
            _id = id;
            _isBoundary = isBoundary;
        }

		public CollocationPoint2D(int id, double[] coordinates, bool isBoundary=false) : base(coordinates)
		{
            _id = id;
            _isBoundary = isBoundary;
        }

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
}
