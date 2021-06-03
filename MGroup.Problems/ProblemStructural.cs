using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.FreedomDegrees;
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

//TODO: Usually the LinearSystem is passed in, but for GetRHSFromHistoryLoad() it is stored as a field. Decide on one method.
//TODO: I am not too fond of the provider storing global sized matrices. However it is necessary to abstract from the analyzers 
//      the various matrices in coupled problems (e.g. stiffness, porous, coupling).
//TODO: Right now this class decides when to build or rebuild the matrices. The analyzer should decide that.
namespace MGroup.Problems
{
    public class ProblemStructural : IImplicitIntegrationProvider, IStaticProvider, INonLinearProvider
    {
        private Dictionary<int, IMatrix> mass, damping, stiffnessFreeFree;
        private Dictionary<int, IMatrixView> stiffnessFreeConstr, stiffnessConstrFree, stiffnessConstrConstr;
        private readonly IComputeEnvironment environment;
        private readonly IStructuralModel model;
        private readonly ISolver solver;
        private IReadOnlyDictionary<int, ILinearSystem> linearSystems;
        private ElementStructuralStiffnessProvider stiffnessProvider = new ElementStructuralStiffnessProvider();
        private ElementStructuralMassProvider massProvider = new ElementStructuralMassProvider();
        private ElementStructuralDampingProvider dampingProvider = new ElementStructuralDampingProvider();

        public ProblemStructural(IComputeEnvironment environment, IStructuralModel model, ISolver solver)
        {
            this.environment = environment;
            this.model = model;
            this.linearSystems = solver.LinearSystems;
            this.solver = solver;
            this.DirichletLoadsAssembler = new DirichletEquivalentLoadsStructural(stiffnessProvider);
        }

        //public double AboserberE { get; set; }
        //public double Aboseberv { get; set; }

        public IDirichletEquivalentLoadsAssembler DirichletLoadsAssembler { get; } 

        private IDictionary<int, IMatrix> Mass
        {
            get
            {
                if (mass == null) BuildMass();
                return mass;
            }
        }

        private IDictionary<int, IMatrix> Damping
        {
            get
            {
                if (damping == null) BuildDamping();
                return damping;
            }
        }

        private IDictionary<int, IMatrix> StiffnessFreeFree
        {
            get
            {
                if (stiffnessFreeFree == null)
                {
                    BuildStiffnessFreeFree();
                }
                else
                {
                    //TODO I am not too fond of side effects, especially in getters
                    RebuildBuildStiffnessFreeFree(); // This is the same but also resets the material modified properties. 
                }
                return stiffnessFreeFree;
            }
        }

        private void BuildStiffnessFreeFree() 
            => stiffnessFreeFree = solver.BuildGlobalMatrices(stiffnessProvider, subdomainID => true);

        private void BuildStiffnessSubmatrices()
        {
            Dictionary<int, (IMatrix Kff, IMatrixView Kfc, IMatrixView Kcf, IMatrixView Kcc)> matrices =
                solver.BuildGlobalSubmatrices(stiffnessProvider);

            stiffnessFreeFree = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Kff);
            stiffnessFreeConstr = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Kfc);
            stiffnessConstrFree = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Kcf);
            stiffnessConstrConstr = environment.CreateDictionaryPerNode(subdomainID => matrices[subdomainID].Kcc);
        }

        private void RebuildBuildStiffnessFreeFree()
        {
            stiffnessFreeFree = solver.BuildGlobalMatrices(stiffnessProvider,
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

        private void BuildMass() => mass = solver.BuildGlobalMatrices(massProvider, subdomainID => true);

        //TODO: With Rayleigh damping, C is more efficiently built using linear combinations of global K, M, 
        //      instead of building and assembling element k, m matrices.
        private void BuildDamping() => damping = solver.BuildGlobalMatrices(dampingProvider, subdomainID => true);

        #region IAnalyzerProvider Members
        public void ClearMatrices()
        {
            damping = null;
            stiffnessFreeFree = null;
            stiffnessConstrFree = null;
            stiffnessConstrConstr = null;
            mass = null;
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

            damping = null;
            stiffnessFreeFree = null;
            stiffnessConstrFree = null;
            stiffnessConstrConstr = null;
            mass = null;
        }
        #endregion 

        #region IImplicitIntegrationProvider Members

        public IMatrixView LinearCombinationOfMatricesIntoStiffness(ImplicitIntegrationCoefficients coefficients, 
            ISubdomain subdomain)
        {
            //TODO: 1) Why do we want Ks to be built only if it has not been factorized? 
            //      2) When calling Ks[id], the matrix will be built anyway, due to the annoying side effects of the property.
            //         Therefore, if the matrix was indeed factorized it would be built twice!
            //      3) The provider should be decoupled from solver logic, such as knowing if the matrix is factorized. Knowledge
            //         that the matrix has been altered by the solver could be implemented by observers, if necessary.
            //      4) The analyzer should decide when global matrices need to be rebuilt, not the provider.
            //      5) The need to rebuild the system matrix if the solver has modified it might be avoidable if the analyzer 
            //         uses the appropriate order of operations. However, that may not always be possible. Such a feature 
            //         (rebuild or store) is nice to have. Whow would be responsible, the solver, provider or assembler?
            //      6) If the analyzer needs the system matrix, then it can call solver.PreventFromOverwritingMatrix(). E.g.
            //          explicit dynamic analyzers would need to do that.
            //if (linearSystem.IsMatrixOverwrittenBySolver) BuildKs();

            int id = subdomain.ID;
            IMatrix matrix = this.StiffnessFreeFree[id];
            matrix.LinearCombinationIntoThis(coefficients.Stiffness, Mass[id], coefficients.Mass);
            matrix.AxpyIntoThis(Damping[id], coefficients.Damping);
            return matrix;
        }

        public void ProcessRhs(ImplicitIntegrationCoefficients coefficients, ISubdomain subdomain, IVector rhs)
        {
            // Method intentionally left empty.
        }

        public IDictionary<int, IVector> GetAccelerationsOfTimeStep(int timeStep)
        {
            Dictionary<int, IVector> d = environment.CreateDictionaryPerNode(
                subdomainID => linearSystems[subdomainID].CreateZeroVector());
            
            if (model.MassAccelerationHistoryLoads.Count > 0)
            {
                List<MassAccelerationLoad> m = new List<MassAccelerationLoad>(model.MassAccelerationHistoryLoads.Count);
                foreach (IMassAccelerationHistoryLoad l in model.MassAccelerationHistoryLoads)
                    m.Add(new MassAccelerationLoad() { Amount = l[timeStep], DOF = l.DOF });

                Action<int> calcSubdomainAccelerations = subdomainID =>
                {
                    ISubdomain subdomain = model.GetSubdomain(subdomainID);
                    int[] subdomainToGlobalDofs = model.GlobalDofOrdering.MapFreeDofsSubdomainToGlobal(subdomain);
                    foreach ((INode node, IDofType dofType, int subdomainDofIdx) in subdomain.FreeDofOrdering.FreeDofs)
                    {
                        int globalDofIdx = subdomainToGlobalDofs[subdomainDofIdx];
                        foreach (var l in m)
                        {
                            if (dofType == l.DOF) d[subdomain.ID].Set(globalDofIdx, l.Amount);
                        }
                    }

                    //foreach (var nodeInfo in subdomain.GlobalNodalDOFsDictionary)
                    //{
                    //    foreach (var dofPair in nodeInfo.Value)
                    //    {
                    //        foreach (var l in m)
                    //        {
                    //            if (dofPair.Key == l.DOF && dofPair.Value != -1)
                    //            {
                    //                d[subdomain.ID].Set(dofPair.Value, l.Amount);
                    //            }
                    //        }
                    //    }
                    //}
                };
                environment.DoPerNode(calcSubdomainAccelerations);
            }

            //foreach (ElementMassAccelerationHistoryLoad load in model.ElementMassAccelerationHistoryLoads)
            //{
            //    MassAccelerationLoad hl = new MassAccelerationLoad() { Amount = load.HistoryLoad[timeStep] * 564000000, DOF = load.HistoryLoad.DOF };
            //    load.Element.Subdomain.AddLocalVectorToGlobal(load.Element,
            //        load.Element.ElementType.CalculateAccelerationForces(load.Element, (new MassAccelerationLoad[] { hl }).ToList()),
            //        load.Element.Subdomain.Forces);
            //}

            return d;
        }

        public IDictionary<int, IVector> GetVelocitiesOfTimeStep(int timeStep)
        {
            return environment.CreateDictionaryPerNode(subdomainID => linearSystems[subdomainID].CreateZeroVector());
        }

        public IDictionary<int, IVector> GetRhsFromHistoryLoad(int timeStep)
        {
            //TODO: this is also done by model.AssignLoads()
            environment.DoPerNode(subdomainID => model.GetSubdomain(subdomainID).Forces.Clear());

            model.AssignLoads(solver.DistributeNodalLoads);
            model.AssignMassAccelerationHistoryLoads(timeStep);

            Dictionary<int, IVector> rhsVectors = environment.CreateDictionaryPerNode<IVector>(
                subdomainID => model.GetSubdomain(subdomainID).Forces);
            return rhsVectors;
        }

        public IVector MassMatrixVectorProduct(ISubdomain subdomain, IVectorView vector)
            => this.Mass[subdomain.ID].Multiply(vector);

        public IVector DampingMatrixVectorProduct(ISubdomain subdomain, IVectorView vector)
            => this.Damping[subdomain.ID].Multiply(vector);

        #endregion

        #region IStaticProvider Members

        public IMatrixView CalculateMatrix(ISubdomain subdomain)
        {
            if (stiffnessFreeFree == null) BuildStiffnessFreeFree();
            return stiffnessFreeFree[subdomain.ID];
        }


        public (IMatrixView matrixFreeFree, IMatrixView matrixFreeConstr, IMatrixView matrixConstrFree, 
            IMatrixView matrixConstrConstr) CalculateSubMatrices(ISubdomain subdomain)
        {
            int id = subdomain.ID;
            if ((stiffnessFreeFree == null) || (stiffnessFreeConstr == null) 
                || (stiffnessConstrFree == null) || (stiffnessConstrConstr == null))
            {
                BuildStiffnessSubmatrices();
            }
            return (stiffnessFreeFree[id], stiffnessFreeConstr[id], stiffnessConstrFree[id], stiffnessConstrConstr[id]);
        }

        #endregion

        #region INonLinearProvider Members

        public double CalculateRhsNorm(IVectorView rhs) => rhs.Norm2();

        public void ProcessInternalRhs(ISubdomain subdomain, IVectorView solution, IVector rhs) {}

        #endregion
    }
}
