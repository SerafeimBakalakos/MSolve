using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Commons;
using ISAAR.MSolve.XFEM.CrackGeometry.Implicit;
using ISAAR.MSolve.XFEM.Entities;
using Xunit;

namespace ISAAR.MSolve.XFEM.Tests.Transfer
{
    internal static class EnrichmentComparions
    {
        internal static void CheckSameLevelSets(SingleCrackLsm expectedLsm, XSubdomain expectedSubdomain,
            SingleCrackLsm actualLsm, XSubdomain actualSubdomain, int precision = 8)
        {
            foreach (XNode expectedNode in expectedSubdomain.Nodes.Values)
            {
                double expectedBodyLevelSet = expectedLsm.LevelSetsBody[expectedNode];
                double expectedTipLevelSet = expectedLsm.LevelSetsBody[expectedNode];

                XNode actualNode = actualSubdomain.Nodes[expectedNode.ID];
                double actualBodyLevelSet = actualLsm.LevelSetsBody[actualNode];
                double actualTipLevelSet = actualLsm.LevelSetsBody[actualNode];

                Assert.Equal(expectedBodyLevelSet, actualBodyLevelSet, precision);
                Assert.Equal(expectedTipLevelSet, actualTipLevelSet, precision);
            }
        }
    }
}
