using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Logging.Interfaces;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.LinearSystems;

namespace ISAAR.MSolve.Analyzers.Multiscale
{
    /// <summary>
    /// Only for linear problems with RVE subject to linear displacements. See Section 1, Box1 of
    /// "Computational micro-to-macro transitions of discretized microstructures undergoing small strains, Miehe & Koch, 2002",
    /// </summary>
    public class HomogenizationAnalyzer
    {
        private readonly IReadOnlyDictionary<int, ILinearSystem> linearSystems;
        private readonly IStructuralModel model;
        private readonly IStaticProvider provider;
        private readonly IReferenceVolumeElement rve;
        private readonly ISolver solver;

        public HomogenizationAnalyzer(IStructuralModel model, ISolver solver, IStaticProvider provider, 
            IReferenceVolumeElement rve)
        {
            if (model.Subdomains.Count != 1) throw new NotImplementedException();
            this.model = model;
            this.linearSystems = solver.LinearSystems;
            this.solver = solver;
            this.provider = provider;
            this.rve = rve;
        }

        public IMatrix MacroscopicModulus { get; private set; }
        
        public double[] MacroscopicStrains { get; set; }

        public double[] MacroscopicStresses { get; private set; }

        public void Initialize()
        {
            // The order in which the next initializations happen is very important.
            if (MacroscopicStrains != null) rve.ApplyBoundaryConditionsLinear(MacroscopicStrains);
            else rve.ApplyBoundaryConditionsZero();
            model.ConnectDataStructures();
            solver.OrderDofs(true);
            foreach (ILinearSystem linearSystem in linearSystems.Values)
            {
                linearSystem.Reset(); // Necessary to define the linear system's size 
            }
        }

        public void Solve()
        {
            var Kab = new Dictionary<int, IMatrixView>(); // rows = free/internal dofs, columns = constrained/boundary dofs
            var Kba = new Dictionary<int, IMatrixView>(); // rows = constrained dofs, columns = free dofs
            var Kbb = new Dictionary<int, IMatrixView>(); // rows = constrained dofs, columns = constrained dofs

            // Build all matrices for free and constrained dofs.
            foreach (ILinearSystem linearSystem in linearSystems.Values)
            {
                int s = linearSystem.Subdomain.ID;
                (IMatrixView kff, IMatrixView kfc, IMatrixView kcf, IMatrixView kcc) = 
                    provider.CalculateSubMatrices(linearSystem.Subdomain);
                
                linearSystem.Matrix = kff;
                Kab[s] = kfc;
                Kba[s] = kcf;
                Kbb[s] = kcc;
            }

            // Static condensation: Kcond = Kbb - Kba * inv(Kaa) * Kab
            Dictionary<int, Matrix> invKaaTimesKab = solver.InverseSystemMatrixTimesOtherMatrix(Kab);
            ILinearSystem ls = linearSystems.First().Value;
            ISubdomain subdomain = ls.Subdomain;
            int id = subdomain.ID;
            IMatrix Kcondensed = Kbb[id].Subtract(Kba[id].MultiplyRight(invKaaTimesKab[id]));

            // Calculate kinematic relations matrix
            IMatrixView D = rve.CalculateKinematicRelationsMatrix(subdomain);

            // Calculate effective elasticity/conductivity/whatever tensor: C = 1 / V * (D * Kcond * D^T)
            Matrix effectiveTensor = D.ThisTimesOtherTimesThisTranspose(Kcondensed);
            double rveVolume = rve.CalculateRveVolume();
            effectiveTensor.ScaleIntoThis(1.0 / rveVolume);
            MacroscopicModulus = effectiveTensor;

            // Calculate macroscopic stresses
            if (MacroscopicStrains != null)
            {
                IVector s = MacroscopicModulus.Multiply(Vector.CreateFromArray(MacroscopicStrains));
                MacroscopicStresses = s.CopyToArray();

                #region redundant: all these are equivalent to stress = Constitutive * strain, 
                //// Create RHS
                //AddEquivalentNodalLoadsToRHS();

                //// Solve another linear system
                //solver.Solve();

                //// Displacements at free dofs
                //IVectorView ua = ls.Solution;

                //// Prescribed displacements at boundary dofs
                //IVector ub = D.Multiply(Vector.CreateFromArray(MacroscopicStrains), true);

                //// External forces at boundary dofs
                //IVector fbExt = Kba[id].Multiply(ua);
                //fbExt.AddIntoThis(Kbb[id].Multiply(ub));
                #endregion
            }
        }

        private void AddEquivalentNodalLoadsToRHS()
        {
            foreach (ILinearSystem linearSystem in linearSystems.Values)
            {
                linearSystem.Subdomain.Forces = Vector.CreateZero(linearSystem.Size);
                linearSystem.RhsVector = linearSystem.Subdomain.Forces;
                IVector initialFreeSolution = linearSystem.CreateZeroVector();
                IVector equivalentNodalLoads = provider.DirichletLoadsAssembler.GetEquivalentNodalLoads(
                        linearSystem.Subdomain, initialFreeSolution, 1.0);
                linearSystem.RhsVector.SubtractIntoThis(equivalentNodalLoads);
            }
        }
    }
}
