using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class PhaseJunction
    {
        public int ID { get; set; }

        public SortedSet<IPhase> Phases { get; } = new SortedSet<IPhase>();

        public IXFiniteElement Element { get; set; }
    }
}
