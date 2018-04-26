﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Utilities;

namespace ISAAR.MSolve.XFEM.Entities.FreedomDegrees
{
    interface IDOFEnumerator
    {
        int ConstrainedDofsCount { get; }
        int EnrichedDofsCount { get ; }
        int FreeDofsCount { get; }

        IEnumerable<int> GetFreeDofsOf(XNode2D node);

        int GetFreeDofOf(XNode2D node, DisplacementDOF dofType);

        IEnumerable<int> GetConstrainedDofsOf(XNode2D node);

        int GetConstrainedDofOf(XNode2D node, DisplacementDOF dofType);

        IEnumerable<int> GetEnrichedDofsOf(XNode2D node);

        int GetEnrichedDofOf(XNode2D node, EnrichedDOF dofType);

        void MatchElementToGlobalStandardDofsOf(XContinuumElement2D element,
            out IReadOnlyDictionary<int, int> elementToGlobalFreeDofs,
            out IReadOnlyDictionary<int, int> elementToGlobalConstrainedDofs);

        IReadOnlyDictionary<int, int> MatchElementToGlobalEnrichedDofsOf(XContinuumElement2D element);

        Vector ExtractDisplacementVectorOfElementFromGlobal(XContinuumElement2D element,
            Vector globalFreeVector, Vector globalConstrainedVector);

        Vector ExtractEnrichedDisplacementsOfElementFromGlobal(XContinuumElement2D element, Vector globalFreeVector);

        double[,] GatherNodalDisplacements(Model2D model, Vector solution);

        ITable<XNode2D, EnrichedDOF, double> GatherEnrichedNodalDisplacements(Model2D model, Vector solution);
    }
}