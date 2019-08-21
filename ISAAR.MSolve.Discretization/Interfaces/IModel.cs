using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;

namespace ISAAR.MSolve.Discretization.Interfaces
{
    public delegate Dictionary<int, SparseVector> NodalLoadsToSubdomainsDistributor(
        Table<INode, IDofType, double> globalNodalLoads);

    public interface IModel
    {
        Table<INode, IDofType, double> Constraints { get; }
        IGlobalFreeDofOrdering GlobalDofOrdering { get; set; } //TODO: this should not be managed by the model. Update after 6 months: yeap, see the mess in collocation
        IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads { get; }

        int NumElements { get; }
        int NumNodes { get; }
        int NumSubdomains { get; }

        void AssignLoads(NodalLoadsToSubdomainsDistributor distributeNodalLoads); //TODOMaria: Here is where the element loads are assembled
        void AssignMassAccelerationHistoryLoads(int timeStep);
        void ConnectDataStructures();

        IEnumerable<IElement> EnumerateElements(); //TODO: At some point I must do the same for concrete classes
        IEnumerable<INode> EnumerateNodes();
        IEnumerable<ISubdomain> EnumerateSubdomains();

        IElement GetElement(int elementID);
        INode GetNode(int nodeID);
        ISubdomain GetSubdomain(int subdomainID);

        ////TODO: This circumvents the covariance issue between Dictionary<int, Node> and Dictionary<int, INode>. Is there a more elegant solution?
        ////TODO: Similarly there should be methods and properties NumNodes, EnumerateNodes(), AddNode(), RemoveNode(). 
        /////This is much better than exposing lists or dictionaries. Ditto for elements and subdomains.
        //INode GetNode(int nodeID); 
    }
}
