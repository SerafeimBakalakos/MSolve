using System;
using System.Collections.Generic;
using ISAAR.MSolve.Analyzers;
using ISAAR.MSolve.Analyzers.Dynamic;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Analyzers.NonLinear;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.LinearSystems;

//TODO: I am not too fond of the provider storing global sized matrices.
namespace ISAAR.MSolve.Problems
{
    public class ProblemThermalSteadyState : IStaticProvider, INonLinearProvider
    {
        private Dictionary<int, IMatrix> conductivityFreeFree;
        private Dictionary<int, IMatrixView> conductivityFreeConstr, conductivityConstrFree, conductivityConstrConstr;
        private readonly IStructuralModel model;
        private readonly ISolver solver;
        private IReadOnlyDictionary<int, ILinearSystem> linearSystems;
        private ElementStructuralStiffnessProvider conductivityProvider = new ElementStructuralStiffnessProvider();

        public ProblemThermalSteadyState(IStructuralModel model, ISolver solver)
        {
            this.model = model;
            this.linearSystems = solver.LinearSystems;
            this.solver = solver;
            this.DirichletLoadsAssembler = new DirichletEquivalentLoadsStructural(conductivityProvider);
        }

        public IDirichletEquivalentLoadsAssembler DirichletLoadsAssembler { get; }

        private IDictionary<int, IMatrix> Conductivity
        {
            get
            {
                if (conductivityFreeFree == null) BuildConductivityFreeFree();
                //else RebuildConductivityMatrices();
                return conductivityFreeFree;
            }
        }

        private void BuildConductivityFreeFree() => conductivityFreeFree = solver.BuildGlobalMatrices(conductivityProvider);

        private void BuildConductivitySubmatrices()
        {
            Dictionary<int, (IMatrix Cff, IMatrixView Cfc, IMatrixView Ccf, IMatrixView Ccc)> matrices =
                solver.BuildGlobalSubmatrices(conductivityProvider);

            conductivityFreeFree = new Dictionary<int, IMatrix>(model.Subdomains.Count);
            conductivityFreeConstr = new Dictionary<int, IMatrixView>(model.Subdomains.Count);
            conductivityConstrFree = new Dictionary<int, IMatrixView>(model.Subdomains.Count);
            conductivityConstrConstr = new Dictionary<int, IMatrixView>(model.Subdomains.Count);
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                int id = subdomain.ID;
                conductivityFreeFree.Add(id, matrices[id].Cff);
                conductivityFreeConstr.Add(id, matrices[id].Cfc);
                conductivityConstrFree.Add(id, matrices[id].Ccf);
                conductivityConstrConstr.Add(id, matrices[id].Ccc);
            }
        }

        private void RebuildConductivityFreeFree()
        {
            //TODO: This will rebuild all the stiffnesses of all subdomains, if even one subdomain has MaterialsModified = true.
            //      Optimize this, by passing a flag foreach subdomain to solver.BuildGlobalSubmatrices().

            bool mustRebuild = false;
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                if (subdomain.StiffnessModified)
                {
                    mustRebuild = true;
                    break;
                }
            }
            if (mustRebuild) conductivityFreeFree = solver.BuildGlobalMatrices(conductivityProvider);
            foreach (ISubdomain subdomain in model.Subdomains) subdomain.ResetMaterialsModifiedProperty();
        }

        #region IAnalyzerProvider Members
        public void ClearMatrices()
        {
            conductivityFreeFree = null;
            conductivityFreeConstr = null;
            conductivityConstrFree = null;
            conductivityConstrConstr = null;
        }

        public void Reset()
        {
            foreach (ISubdomain subdomain in model.Subdomains)
            {
                foreach (IElement element in subdomain.Elements)
                {
                    ((IFiniteElement)element.ElementType).ClearMaterialState();
                }
            }

            conductivityFreeFree = null;
            conductivityConstrFree = null;
            conductivityConstrConstr = null;
        }
        #endregion 

        #region IStaticProvider Members

        public IMatrixView CalculateMatrix(ISubdomain subdomain)
        {
            if (conductivityFreeFree == null) BuildConductivityFreeFree();
            return conductivityFreeFree[subdomain.ID];
        }

        public (IMatrixView matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree,
            IMatrixView matrixConstrConstr) CalculateSubMatrices(ISubdomain subdomain)
        {
            int id = subdomain.ID;
            if ((conductivityFreeFree == null) || (conductivityFreeConstr == null) 
                || (conductivityConstrFree == null) || (conductivityConstrConstr == null))
            {
                BuildConductivitySubmatrices();
            }
            return (conductivityFreeFree[id], conductivityFreeConstr[id], 
                conductivityConstrFree[id], conductivityConstrConstr[id]);
        }
        #endregion

        #region INonLinearProvider Members

        public double CalculateRhsNorm(IVectorView rhs) => rhs.Norm2();

        public void ProcessInternalRhs(ISubdomain subdomain, IVectorView solution, IVector rhs) { }

        #endregion
    }
}
