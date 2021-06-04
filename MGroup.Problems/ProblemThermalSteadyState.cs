using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Providers;
using ISAAR.MSolve.FEM.Entities;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.Analyzers;
using MGroup.Analyzers.Interfaces;
using MGroup.Environments;
using MGroup.Solvers;
using MGroup.Solvers.LinearSystems;

//TODO: I am not too fond of the provider storing global sized matrices.
namespace MGroup.Problems
{
    public class ProblemThermalSteadyState : IStaticProvider, INonLinearProvider
    {
        private Dictionary<int, IMatrix> conductivityFreeFree;
        private Dictionary<int, IMatrixView> conductivityFreeConstr, conductivityConstrFree, conductivityConstrConstr;
        private readonly IComputeEnvironment environment;
        private readonly IStructuralModel model;
        private readonly ISolver solver;
        private IReadOnlyDictionary<int, ILinearSystem> linearSystems;
        private ElementStructuralStiffnessProvider conductivityProvider = new ElementStructuralStiffnessProvider();

        public ProblemThermalSteadyState(IComputeEnvironment environment, IStructuralModel model, ISolver solver)
        {
            this.environment = environment;
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

        private void BuildConductivityFreeFree() 
            => conductivityFreeFree = solver.BuildGlobalMatrices(conductivityProvider, subdomainID => true);

       private void RebuildConductivityFreeFree()
        {
            conductivityFreeFree = solver.BuildGlobalMatrices(conductivityProvider,
                    subdomainID => model.GetSubdomain(subdomainID).StiffnessModified);

            environment.DoPerNode(subdomainID =>
            {
                ISubdomain subdomain = model.GetSubdomain(subdomainID);
                if (subdomain.StiffnessModified)
                {
                    subdomain.ResetMaterialsModifiedProperty();
                }
            });
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
            environment.DoPerNode(subdomainID =>
            {
                foreach (IElement element in model.GetSubdomain(subdomainID).Elements)
                {
                    ((IFiniteElement)element.ElementType).ClearMaterialState();
                }
            });

            conductivityFreeFree = null;
            conductivityConstrFree = null;
            conductivityConstrConstr = null;
        }
        #endregion 

        #region IStaticProvider Members
        public void BuildMatrices()
        {
            if (conductivityFreeFree == null) BuildConductivityFreeFree();
            environment.DoPerNode(s => linearSystems[s].Matrix = conductivityFreeFree[s]);
        }

        public void BuildFreeConstrainedSubMatrices()
        {
            Dictionary<int, (IMatrix Cff, IMatrixView Cfc, IMatrixView Ccf, IMatrixView Ccc)> matrices =
                solver.BuildGlobalSubmatrices(conductivityProvider);

            conductivityFreeFree = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Cff);
            conductivityFreeConstr = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Cfc);
            conductivityConstrFree = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Ccf);
            conductivityConstrConstr = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Ccc);
        }
        #endregion

        #region INonLinearProvider Members

        public double CalculateRhsNorm(IVectorView rhs) => rhs.Norm2();

        public void ProcessInternalRhs(ISubdomain subdomain, IVectorView solution, IVector rhs) { }

        #endregion
    }
}
