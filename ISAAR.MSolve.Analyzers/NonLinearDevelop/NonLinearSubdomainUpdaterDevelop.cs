using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Analyzers.NonLinear
{
    public class NonLinearSubdomainUpdaterDevelop : INonLinearSubdomainUpdater
    {
        private readonly ISubdomainDevelop subdomain;

        public NonLinearSubdomainUpdaterDevelop(ISubdomainDevelop subdomain)
        {
            this.subdomain = subdomain;
        }

        public void ScaleConstraints(double scalingFactor)
        {
            this.subdomain.ScaleConstraints(scalingFactor);
        }

        public IVector GetRhsFromSolution(IVectorView solution, IVectorView dSolution)
        {
            return subdomain.GetRhsFromSolution(solution, dSolution);
        }

        public void CalculateStressesOnly(IVectorView solution, IVectorView dSolution)
        {
            subdomain.CalculateStressesOnly(solution, dSolution);
        }

        public IVector CalculateRHSonly(IVectorView solution, IVectorView dSolution)
        {
            return subdomain.CalculateRHSonly(solution, dSolution);
        }

        public void ResetState()
        {
            this.subdomain.ClearMaterialStresses();
        }

        public void UpdateState()
        {
            this.subdomain.SaveMaterialState();
        }
    }
}

