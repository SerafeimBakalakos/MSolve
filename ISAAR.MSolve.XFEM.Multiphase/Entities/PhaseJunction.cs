using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class PhaseJunction
    {
        public int ID { get; set; }

        /// <summary>
        /// The order is import and represents the chain of phases each having a common boundary with 2 others 
        /// (previous - current- next)
        /// </summary>
        public List<IPhase> Phases { get; set; }

        public IXFiniteElement Element { get; set; }

        public bool HasSamePhasesAs(PhaseJunction other)
        {
            var thisPhases = new HashSet<IPhase>(this.Phases);
            if (thisPhases.SetEquals(other.Phases)) return true;
            else return false;
        }
    }
}
