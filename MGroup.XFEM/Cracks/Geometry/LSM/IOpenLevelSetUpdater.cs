using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;

//TODO: Move this to a namespace for general 2D open LSM curves. This means no references to crack tips
//TODO: should it just pull properties out of the LSM rather than all these parameters? It would be much more abstracted.
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public interface IOpenLevelSetUpdater
    {
        /// <summary>
        /// Returns nodes that were previously enriched with Heaviside and now their body level set changes.
        /// </summary>
       void Update(double[] oldTip, double[] newTip, IEnumerable<XNode> nodes, 
           Dictionary<int, double> levelSetsBody, Dictionary<int, double> levelSetsTip);
    }
}
