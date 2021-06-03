using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO:  delete the original one (IEquivalentLoadsAssembler) in FEM.Interfaces
namespace MGroup.Analyzers
{
    public interface IDirichletEquivalentLoadsAssembler
    {
        IVector GetEquivalentNodalLoads(ISubdomain subdomain, IVectorView solution, double constraintScalingFactor);
    }
}
