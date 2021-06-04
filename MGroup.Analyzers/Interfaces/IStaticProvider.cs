using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;

namespace MGroup.Analyzers.Interfaces
{
    public interface IStaticProvider : IAnalyzerProvider
    {
        /// <summary>
        /// Builds the matrix that corresponds to the free freedom degrees of each subdomain. If A is the matrix corresponding 
        /// to all dofs, f denotes free dofs and c denotes constrained dofs then A = [ Aff Acf^T; Acf Acc ]. This method
        /// builds only Aff.
        /// </summary>
        void BuildMatrices();

        /// <summary>
        /// Builds the submatrices that corresponds to the free and constrained freedom degrees of each subdomain. If A is the 
        /// matrix corresponding to all dofs, f denotes free dofs and c denotes constrained dofs then A = [ Aff Acf^T; Acf Acc ]. 
        /// This method builds (Aff, Afc, Acf, Acc). If the linear system is symmetric, then Afc = Acf^T.
        /// In this case, these entries are only stored once and shared between the returned Afc, Acf.
        /// </summary>
        void BuildFreeConstrainedSubMatrices();
    }
}
