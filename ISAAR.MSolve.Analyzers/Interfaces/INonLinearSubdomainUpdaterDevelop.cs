using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Analyzers.Interfaces
{
    public interface INonLinearSubdomainUpdaterDevelop
    {
        void ScaleConstraints(double scalingFactor);
        IVector GetRhsFromSolution(IVectorView solution, IVectorView dSolution);
        void UpdateState();
        void ResetState();
    }
}
