using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Geometry.HybridFries
{
    public class Vertex3D : IComparable<Vertex3D>
    {
        public Vertex3D(int id)
        {
            this.ID = id;
        }

        public int ID { get; }

        public double[] CoordsGlobal { get; } = new double[3];

        public List<TriangleCell3D> Cells { get; } = new List<TriangleCell3D>();

        public int CompareTo(Vertex3D other) => other.ID - this.ID;

        public override int GetHashCode() => ID.GetHashCode();
    }
}
