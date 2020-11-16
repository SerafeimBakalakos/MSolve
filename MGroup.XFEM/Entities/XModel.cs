﻿using System;
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
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Enrichment.Observers;

//Method for enriching nodes and updating observers
//TODO: All the UpdateSomething() methods can be abstracted behind an IModel.Update(). The Analyzer does not need to know each
//      detail.
//TODO: There is a lot of repetition between this FEM.Model and IGA.Model with regards to interconnection data. That code should 
//      be moved to a common class. Same goes for the interconnection methods of XSubdomain.
namespace MGroup.XFEM.Entities
{
    public class XModel<TElement> : IXModel where TElement: IXFiniteElement
    {
        private bool areDataStructuresConnected;

        private List<IEnrichmentObserver> enrichmentObservers = new List<IEnrichmentObserver>();

        public XModel()
        {
            areDataStructuresConnected = false;
        }

        public IDomain2DBoundary Boundary { get; set; }

        public Table<INode, IDofType, double> Constraints { get; private set; } = new Table<INode, IDofType, double>();

        //TODO: Phases (or more accurately phase boundaries) and phase junctions should be stored here, not in a GeometricModel
        public List<IXDiscontinuity> Discontinuities { get; } = new List<IXDiscontinuity>();

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

        public Dictionary<int, EnrichmentItem> Enrichments { get; } = new Dictionary<int, EnrichmentItem>();

        //TODO: Perhaps I need something more involved for storing and interfacing with the enrichments
        //public Dictionary<IEnrichmentFunction, XNode[]> EnrichedNodes { get; set; } = new Dictionary<IEnrichmentFunction, XNode[]>();
        //public Dictionary<IEnrichmentFunction, IDofType[]> EnrichedDofs { get; set; } = new Dictionary<IEnrichmentFunction, IDofType[]>();

        public IGlobalFreeDofOrdering GlobalDofOrdering { get; set; }

        public List<NodalLoad> NodalLoads { get; private set; } = new List<NodalLoad>();

        public List<INodeEnricher> NodeEnrichers { get; set; } = new List<INodeEnricher>();

        IReadOnlyList<INode> IStructuralModel.Nodes => XNodes;
        public List<XNode> XNodes { get; } = new List<XNode>();

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

        public IEnumerable<IXFiniteElement> EnumerateElements() 
        {
            //TODO: There must be a better way than recreating the data structures
            var result = new IXFiniteElement[Elements.Count];
            for (int i = 0; i < Elements.Count; ++i) result[i] = Elements[i];
            return result;
        }

        public void Initialize()
        {
            ConnectDataStructures();

            // Identify enrichments and their dofs
            //TODO: The enrichments may need to change during the analysis (e.g. branching cracks, crack junctions, etc)
            foreach (IXDiscontinuity discontinuity in Discontinuities)
            {
                IList<EnrichmentItem> enrichments = discontinuity.DefineEnrichments(this.Enrichments.Count);
                foreach (EnrichmentItem enrichment in enrichments)
                {
                    this.Enrichments[enrichment.ID] = enrichment;
                }
            }

            UpdateStatePrivate(true, null);
        }

        public void RegisterEnrichmentObserver(IEnrichmentObserver observer)
        {
            var previous = observer.RegisterAfterThese();
            foreach (IEnrichmentObserver other in previous)
            {
                if (!enrichmentObservers.Contains(other))
                {
                    if (other.RegisterAfterThese().Length == 0) enrichmentObservers.Add(other);
                    else
                    {
                        throw new ArgumentException("This observer depends on others that in turn depend on even more."
                            + " The order of registration cannot be safely determined automatically."
                            + " Please register them in the correct order yourself.");
                    }
                }
            }
            enrichmentObservers.Add(observer);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subdomainFreeDisplacements">Total displacements of all dofs of each subdomain.</param>
        public void Update(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            UpdateStatePrivate(false, subdomainFreeDisplacements);
        }

        public void UpdateDofs()
        {
            foreach (IXFiniteElement element in Elements) element.IdentifyDofs();
        }

        //TODO: Replace this with Update() and/or Initialize() 
        public void UpdateMaterials()
        {
            foreach (IXFiniteElement element in Elements) element.IdentifyIntegrationPointsAndMaterials();
        }

        private void AssignConstraints()
        {
            foreach (XNode node in XNodes)
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
            foreach (XNode node in XNodes)
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

        /// <summary>
        /// Common operations for intializing/updating the model's state.
        /// </summary>
        /// <param name="firstAnalysis"></param>
        /// <param name="subdomainFreeDisplacements">if <paramref name="firstAnalysis"/> == true, this can be null.</param>
        private void UpdateStatePrivate(bool firstAnalysis, Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            // Update the discontinuities
            foreach (IXDiscontinuity discontinuity in Discontinuities)
            {
                if (firstAnalysis) discontinuity.InitializeGeometry();
                else discontinuity.UpdateGeometry(subdomainFreeDisplacements);
                discontinuity.InteractWithMesh(); //TODO: Should this be included in UpdateGeometry()?
            }

            // Enrich the required nodes
            foreach (INodeEnricher enricher in NodeEnrichers) enricher.ApplyEnrichments();

            // Identify each element's dofs
            foreach (IXFiniteElement element in Elements) element.IdentifyDofs();

            // Identify each element's integration points and the material properties at those points
            foreach (IXFiniteElement element in Elements) element.IdentifyIntegrationPointsAndMaterials();

            // Let observers read the current state and update themselves
            foreach (IEnrichmentObserver observer in enrichmentObservers) observer.Update(Enrichments.Values);
        }
    }
}
