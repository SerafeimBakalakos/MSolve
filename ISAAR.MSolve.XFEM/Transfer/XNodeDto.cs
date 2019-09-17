﻿using ISAAR.MSolve.XFEM.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.XFEM.Transfer
{
    [Serializable]
    public class XNodeDto //TODO: Enrichments
    {
        public int id;

        /// <summary>
        /// Transfering all the subdomain IDs is both time consuming and will cause problems since not all subdomains are 
        /// available to each process.
        /// </summary>
        public int multiplicity; //TODO: Storing and working with a list of boundary nodes of each subdomain would be cleaner.

        public double x, y, z;

        public XNodeDto(int id, double x, double y, double z, int multiplicity)
        {
            this.id = id;
            this.multiplicity = multiplicity;
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public XNodeDto(XNode node)
        {
            this.id = node.ID;
            this.multiplicity = node.Multiplicity;
            this.x = node.X;
            this.y = node.Y;
            this.z = node.Z;
        }

        public XNode Deserialize() => new XNode(id, x, y, z, multiplicity);
    }
}