using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;

namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public class MeshInteractionStrategy2DStolarska: ILsmMeshInteractionStrategy
    {
        public MeshInteractionStrategy2DStolarska()
        {
        }

        public ElementCharacterization FindRelativePositionOf(IXFiniteElement element, 
            Dictionary<XNode, double> levelSetsBody, Dictionary<XNode, double> levelSetsTip)
        {
            double minBodyLevelSet = double.MaxValue;
            double maxBodyLevelSet = double.MinValue;
            double minTipLevelSet = double.MaxValue;
            double maxTipLevelSet = double.MinValue;

            foreach (XNode node in element.Nodes)
            {
                double bodyLevelSet = levelSetsBody[node];
                double tipLevelSet = levelSetsTip[node];
                if (bodyLevelSet < minBodyLevelSet) minBodyLevelSet = bodyLevelSet;
                if (bodyLevelSet > maxBodyLevelSet) maxBodyLevelSet = bodyLevelSet;
                if (tipLevelSet < minTipLevelSet) minTipLevelSet = tipLevelSet;
                if (tipLevelSet > maxTipLevelSet) maxTipLevelSet = tipLevelSet;
            }

            // Warning: This criterion might give false positives for tip elements (see Serafeim's thesis for details)
            if (minBodyLevelSet * maxBodyLevelSet <= 0.0)
            {
                if (minTipLevelSet * maxTipLevelSet <= 0) return ElementCharacterization.Tip;
                else if (maxTipLevelSet < 0) return ElementCharacterization.Intersected;
            }
            return ElementCharacterization.Standard;
        }
    }
}
