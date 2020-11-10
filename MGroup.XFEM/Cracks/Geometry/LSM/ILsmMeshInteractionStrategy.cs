using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

//TODO: Delete this altogether. I kept my approach which is the correct one. Stolarska's approach does not need to be in the code.
//TODO: Move this to a namespace for general 2D open LSM curves. This means no references to crack tips
//TODO: Rename this to represent that it is a geometric strategy from LSM. Right now its 
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public interface ILsmMeshInteractionStrategy
    {
        ElementCharacterization FindRelativePositionOf(IXFiniteElement element, 
            Dictionary<XNode, double> levelSetsBody, Dictionary<XNode, double> levelSetsTip);
    }

    public enum ElementCharacterization { Standard, Intersected, Tip }
}
