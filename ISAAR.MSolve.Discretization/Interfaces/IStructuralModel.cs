using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Discretization.Interfaces
{
    public delegate Dictionary<int, SparseVector> NodalLoadsToSubdomainsDistributor(
        Table<INode, IDofType, double> globalNodalLoads);

    public interface IStructuralModel
    {
        Table<INode, IDofType, double> Constraints { get; }
        IReadOnlyList<IElement> Elements { get; }
        IGlobalFreeDofOrdering GlobalDofOrdering { get; set; } //TODO: this should not be managed by the model. Update after 6 months: yeap, see the mess in collocation
        IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads { get; }
        IReadOnlyList<INode> Nodes { get; }
        IReadOnlyList<ISubdomain> Subdomains { get; }

        void AssignLoads(NodalLoadsToSubdomainsDistributor distributeNodalLoads); //TODOMaria: Here is where the element loads are assembled
        void AssignMassAccelerationHistoryLoads(int timeStep);
        void ConnectDataStructures();

        ////TODO: This circumvents the covariance issue between Dictionary<int, Node> and Dictionary<int, INode>. Is there a more elegant solution?
        ////TODO: Similarly there should be methods and properties NumNodes, EnumerateNodes(), AddNode(), RemoveNode(). 
        /////This is much better than exposing lists or dictionaries. Ditto for elements and subdomains.
        //INode GetNode(int nodeID); 
    }
}
