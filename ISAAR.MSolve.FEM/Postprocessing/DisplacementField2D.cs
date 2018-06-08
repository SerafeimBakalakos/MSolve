using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.Numerical.LinearAlgebra.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.FEM.Postprocessing
{
    /// <summary>
    /// Doesn't work if there are rotational freedom degrees.
    /// </summary>
    public class DisplacementField2D
    {
        public Dictionary<Node, double[]> FindNodalDisplacements(Model model, IVector solution)
        {
            var field = new Dictionary<Node, double[]>();
            foreach (var idxNodePair in model.NodesDictionary)
            {
                Dictionary<DOFType, int> nodalDofs = model.NodalDOFsDictionary[idxNodePair.Key];
                if (nodalDofs.Count != 2) throw new Exception("There must be exactly 2 dofs per node, X and Y");
                int dofXIdx = nodalDofs[DOFType.X];
                double ux = (dofXIdx != Model.constrainedDofIdx) ? solution[dofXIdx] : 0.0;
                int dofYIdx = nodalDofs[DOFType.Y];
                double uy = (dofYIdx != Model.constrainedDofIdx) ? solution[dofYIdx] : 0.0;
                field.Add(idxNodePair.Value, new double[] { ux, uy });
            }
            return field;
        }

        //public IReadOnlyDictionary<XContinuumElement2D, IReadOnlyList<Vector2>> FindElementWiseDisplacements(
        //    Vector solution)
        //{
        //    Vector constrainedDisplacements = model.CalculateConstrainedDisplacements(dofOrderer);
        //    var allDisplacements = new Dictionary<XContinuumElement2D, IReadOnlyList<Vector2>>();
        //    foreach (var element in model.Elements)
        //    {
        //        Vector displacementsUnrolled = dofOrderer.ExtractDisplacementVectorOfElementFromGlobal(
        //            element, solution, constrainedDisplacements);
        //        var displacementsAsVectors = new Vector2[element.Nodes.Count];
        //        for (int i = 0; i < element.Nodes.Count; ++i) // This only works for continuum elements though.
        //        {
        //            displacementsAsVectors[i] = Vector2.Create(displacementsUnrolled[2 * i], displacementsUnrolled[2 * i + 1]);
        //        }
        //        allDisplacements[element] = displacementsAsVectors;
        //    }
        //    return allDisplacements;
        //}
    }
}
