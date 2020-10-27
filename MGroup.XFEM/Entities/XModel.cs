using System;
using System.Collections.Generic;
using System.Linq;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;

//TODO: There is a lot of repetition between this FEM.Model and IGA.Model with regards to interconnection data. That code should 
//      be moved to a common class. Same goes for the interconnection methods of XSubdomain.
namespace MGroup.XFEM.Entities
{
    public class XModel<TElement> : IXModel where TElement: IXFiniteElement
    {
        private bool areDataStructuresConnected;

        public XModel()
        {
            areDataStructuresConnected = false;
        }

        public IDomain2DBoundary Boundary { get; set; }

        public Table<INode, IDofType, double> Constraints { get; private set; } = new Table<INode, IDofType, double>();

        IReadOnlyList<IElement> IStructuralModel.Elements
        {
            get
            {
                var result = new IElement[Elements.Count];
                for (int i = 0; i < Elements.Count; ++i) result[i] = Elements[i];
                return result;
            }
        }

        public List<TElement> Elements { get; } = new List<TElement>();

        public Dictionary<IEnrichment, IDofType[]> EnrichedDofs { get; set; } = new Dictionary<IEnrichment, IDofType[]>();

        public IGlobalFreeDofOrdering GlobalDofOrdering { get; set; }

        public List<NodalLoad> NodalLoads { get; private set; } = new List<NodalLoad>();

        IReadOnlyList<INode> IStructuralModel.Nodes => Nodes;
        public List<XNode> Nodes { get; } = new List<XNode>();

        IReadOnlyList<ISubdomain> IStructuralModel.Subdomains => Subdomains.Values.ToList();
        public Dictionary<int, XSubdomain> Subdomains { get; } = new Dictionary<int, XSubdomain>();

        public IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads => throw new NotImplementedException();

        IList<IMassAccelerationHistoryLoad> IStructuralModel.MassAccelerationHistoryLoads => throw new NotImplementedException();

        public void AssignLoads(NodalLoadsToSubdomainsDistributor distributeNodalLoads)
        {
            foreach (XSubdomain subdomain in Subdomains.Values) subdomain.Forces.Clear();
            AssignNodalLoads(distributeNodalLoads);
        }

        public void AssignNodalLoads(NodalLoadsToSubdomainsDistributor distributeNodalLoads)
        {
            var globalNodalLoads = new Table<INode, IDofType, double>();
            foreach (NodalLoad load in NodalLoads) globalNodalLoads.TryAdd(load.Node, load.DofType, load.Value);

            Dictionary<int, SparseVector> subdomainNodalLoads = distributeNodalLoads(globalNodalLoads);
            foreach (var idSubdomainLoads in subdomainNodalLoads)
            {
                Subdomains[idSubdomainLoads.Key].Forces.AddIntoThis(idSubdomainLoads.Value);
            }
        }

        public void AssignMassAccelerationHistoryLoads(int timeStep) => throw new NotImplementedException();

        public void ConnectDataStructures()
        {
            if (!areDataStructuresConnected)
            {
                BuildInterconnectionData();
                AssignConstraints();
                RemoveInactiveNodalLoads();
                areDataStructuresConnected = true;
            }
        }


        public IEnumerable<IXFiniteElement> EnumerateElements() //TODO: There must be a better way
        {
            var result = new IXFiniteElement[Elements.Count];
            for (int i = 0; i < Elements.Count; ++i) result[i] = Elements[i];
            return result;

        }

        public void UpdateDofs()
        {
            foreach (IXFiniteElement element in Elements) element.IdentifyDofs(EnrichedDofs);
        }

        public void UpdateMaterials()
        {
            foreach (IXFiniteElement element in Elements) element.IdentifyIntegrationPointsAndMaterials();
        }

        private void AssignConstraints()
        {
            foreach (XNode node in Nodes)
            {
                if (node.Constraints == null) continue;
                foreach (Constraint constraint in node.Constraints) Constraints[node, constraint.DOF] = constraint.Amount;
            }

            foreach (XSubdomain subdomain in Subdomains.Values) subdomain.ExtractConstraintsFromGlobal(Constraints);
        }

        private void BuildInterconnectionData()
        {
            // Associate each element with its subdomains
            foreach (XSubdomain subdomain in Subdomains.Values)
            {
                foreach (IXFiniteElement element in subdomain.Elements) element.Subdomain = subdomain;
            }

            // Associate each node with its elements
            foreach (IXFiniteElement element in Elements)
            {
                foreach (XNode node in element.Nodes) node.ElementsDictionary[element.ID] = element;
            }

            // Associate each node with its subdomains
            foreach (XNode node in Nodes)
            {
                foreach (IXFiniteElement element in node.ElementsDictionary.Values)
                {
                    node.SubdomainsDictionary[element.Subdomain.ID] = element.Subdomain;
                }
            }

            // Associate each subdomain with its nodes
            foreach (XSubdomain subdomain in Subdomains.Values) subdomain.DefineNodesFromElements();
        }   

        private void RemoveInactiveNodalLoads()
        {
            // Static loads
            var activeLoadsStatic = new List<NodalLoad>(NodalLoads.Count);
            foreach (NodalLoad load in NodalLoads)
            {
                bool isConstrained = Constraints.Contains(load.Node, load.DofType);
                if (!isConstrained) activeLoadsStatic.Add(load);
            }
            NodalLoads = activeLoadsStatic;
        }
    }
}
