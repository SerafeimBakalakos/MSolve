using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Phases
{
    public interface IPhaseObserver
    {
        void LogGeometry();

        //MODIFICATION NEEDED: this should probably be in a different interface, that tracks element-geometry interactions.
        //  So far the interactions (with elements) that get plotted depend on a conforming mesh, which is created after this is called and in a different class.
        void LogMeshInteractions(); 
    }
}
