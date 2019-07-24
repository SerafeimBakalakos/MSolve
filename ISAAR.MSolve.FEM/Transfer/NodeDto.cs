using ISAAR.MSolve.FEM.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.FEM.Transfer
{
    public struct NodeDto
    {
        public int id;
        public double x, y, z;

        public NodeDto(int id, double x, double y, double z)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public NodeDto(Node node)
        {
            this.id = node.ID;
            this.x = node.X;
            this.y = node.Y;
            this.z = node.Z;
        }

        //public static NodeDto Serialize(Node node)
        //{
        //    var trans = new NodeDto();
        //    trans.id = node.ID;
        //    trans.x = node.X;
        //    trans.y = node.Y;
        //    trans.z = node.Z;
        //    return trans;
        //}

        public Node Deserialize() => new Node(id, x, y, z);
    }
}
