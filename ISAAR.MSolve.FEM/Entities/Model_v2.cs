﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Numerical.Commons;
using IEmbeddedElement = ISAAR.MSolve.FEM.Interfaces.IEmbeddedElement;

namespace ISAAR.MSolve.FEM.Entities
{
    public class Model_v2 : IStructuralModel_v2
    {
        //TODO: remove these and let the solver's dof orderer do the job.
        public delegate IGlobalFreeDofOrdering OrderDofs(IStructuralModel_v2 model);
        public OrderDofs dofOrderer;

        //public const int constrainedDofIdx = -1;
        private int totalDOFs = 0;
        private readonly Dictionary<int, Node> nodesDictionary = new Dictionary<int, Node>();
        //private readonly IList<EmbeddedNode> embeddedNodes = new List<EmbeddedNode>();
        private readonly Dictionary<int, Element> elementsDictionary = new Dictionary<int, Element>();
        private readonly Dictionary<int, Subdomain_v2> subdomainsDictionary = new Dictionary<int, Subdomain_v2>();
        private readonly Dictionary<int, Cluster> clustersDictionary = new Dictionary<int, Cluster>();
        //private readonly Dictionary<int, Dictionary<DOFType, int>> nodalDOFsDictionary = new Dictionary<int, Dictionary<DOFType, int>>();
        //private readonly Dictionary<int, Dictionary<DOFType, double>> constraintsDictionary = new Dictionary<int, Dictionary<DOFType, double>>();//TODOMaria: maybe it's useless in model class
        private readonly IList<Load> loads = new List<Load>();
        private readonly IList<ElementMassAccelerationLoad> elementMassAccelerationLoads = new List<ElementMassAccelerationLoad>();
        private readonly IList<MassAccelerationLoad> massAccelerationLoads = new List<MassAccelerationLoad>();
        private readonly IList<IMassAccelerationHistoryLoad> massAccelerationHistoryLoads = new List<IMassAccelerationHistoryLoad>();
        private readonly IList<ElementMassAccelerationHistoryLoad> elementMassAccelerationHistoryLoads = new List<ElementMassAccelerationHistoryLoad>();

        #region Properties
        //public IList<EmbeddedNode> EmbeddedNodes
        //{
        //    get { return embeddedNodes; }
        //}

        public Dictionary<int, Node> NodesDictionary
        {
            get { return nodesDictionary; }
        }

        public Dictionary<int, Element> ElementsDictionary
        {
            get { return elementsDictionary; }
        }

        public Dictionary<int, Subdomain_v2> SubdomainsDictionary
        {
            get { return subdomainsDictionary; }
        }

        public Dictionary<int, Cluster> ClustersDictionary
        {
            get { return clustersDictionary; }
        }

        IReadOnlyList<INode> IStructuralModel_v2.Nodes => nodesDictionary.Values.ToList();
        public IList<Node> Nodes
        {
            get { return nodesDictionary.Values.ToList(); }
        }

        IReadOnlyList<IElement> IStructuralModel_v2.Elements => elementsDictionary.Values.ToList();
        public IList<Element> Elements
        {
            get { return elementsDictionary.Values.ToList(); }
        }

        IReadOnlyList<ISubdomain_v2> IStructuralModel_v2.Subdomains => subdomainsDictionary.Values.ToList();
        public IList<Subdomain_v2> Subdomains
        {
            get { return subdomainsDictionary.Values.ToList(); }
        }

        public IList<Cluster> Clusters
        {
            get { return clustersDictionary.Values.ToList(); }
        }

        public Table<INode, DOFType, double> Constraints { get; private set; } = new Table<INode, DOFType, double>();//TODOMaria: maybe it's useless in model class

        //public Dictionary<int, Dictionary<DOFType, double>> Constraints
        //{
        //    get { return this.constraintsDictionary; }
        //}

        public IList<Load> Loads
        {
            get { return loads; }
        }

        public IList<ElementMassAccelerationLoad> ElementMassAccelerationLoads
        {
            get { return elementMassAccelerationLoads; }
        }

        public IList<MassAccelerationLoad> MassAccelerationLoads
        {
            get { return massAccelerationLoads; }
        }

        public IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads
        {
            get { return massAccelerationHistoryLoads; }
        }

        public IList<ElementMassAccelerationHistoryLoad> ElementMassAccelerationHistoryLoads
        {
            get { return elementMassAccelerationHistoryLoads; }
        }

        public IGlobalFreeDofOrdering GlobalDofOrdering { get; private set; }

        //public Dictionary<int, Dictionary<DOFType, int>> NodalDOFsDictionary
        //{
        //    get { return nodalDOFsDictionary; }
        //}

        public int TotalDOFs
        {
            get { return totalDOFs; }
        }

        #endregion

        #region Data interconnection routines
        private void BuildElementDictionaryOfEachNode()
        {
            foreach (Element element in elementsDictionary.Values)
                foreach (Node node in element.Nodes)
                    node.ElementsDictionary.Add(element.ID, element);
        }

        private void BuildSubdomainOfEachElement()
        {
            foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
                foreach (Element element in subdomain.Elements)
                    element.Subdomain_v2 = subdomain;
        }

        private void BuildInterconnectionData()//TODOMaria: maybe I have to generate the constraints dictionary for each subdomain here
        {
            BuildSubdomainOfEachElement();
            DuplicateInterSubdomainEmbeddedElements();
            BuildElementDictionaryOfEachNode();
            foreach (Node node in nodesDictionary.Values)
                node.BuildSubdomainDictionary_v2();

            //BuildNonConformingNodes();

            foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
                subdomain.BuildNodesList();
        }

        private void DuplicateInterSubdomainEmbeddedElements()
        {
            foreach (var e in elementsDictionary.Values.Where(x => x.ElementType is IEmbeddedElement))
            {
                var subs = ((IEmbeddedElement)e.ElementType).EmbeddedNodes.Select(x => x.EmbeddedInElement.Subdomain_v2).Distinct();
                foreach (var s in subs.Where(x => x.ID != e.Subdomain_v2.ID))
                    s.Elements.Add(e);
            }
        }

        private void BuildNonConformingNodes()
        {
            List<int> subIDs = new List<int>();
            foreach (Element element in elementsDictionary.Values)
            {
                subIDs.Clear();

                foreach (Node node in element.Nodes)
                    foreach (int subID in node.SubdomainsDictionary.Keys)
                        if (!subIDs.Contains(subID)) subIDs.Add(subID);

                foreach (Node node in element.Nodes)
                    foreach (int subID in subIDs)
                        if (!node.SubdomainsDictionary.ContainsKey(subID)) node.NonMatchingSubdomainsDictionary_v2.Add(subID, subdomainsDictionary[subID]);
            }
        }

        //private void EnumerateGlobalDOFs()
        //{
        //    totalDOFs = 0;
        //    Dictionary<int, List<DOFType>> nodalDOFTypesDictionary = new Dictionary<int, List<DOFType>>();
        //    foreach (Element element in elementsDictionary.Values)
        //    {
        //        for (int i = 0; i < element.Nodes.Count; i++)
        //        {
        //            if (!nodalDOFTypesDictionary.ContainsKey(element.Nodes[i].ID))
        //                nodalDOFTypesDictionary.Add(element.Nodes[i].ID, new List<DOFType>());
        //            nodalDOFTypesDictionary[element.Nodes[i].ID].AddRange(element.ElementType.DOFEnumerator.GetDOFTypesForDOFEnumeration(element)[i]);
        //        }
        //    }

        //    foreach (Node node in nodesDictionary.Values)
        //    {
        //        Dictionary<DOFType, int> dofsDictionary = new Dictionary<DOFType, int>();
        //        foreach (DOFType dofType in nodalDOFTypesDictionary[node.ID].Distinct())
        //        {
        //            int dofID = 0;
        //            #region removeMaria
        //            //foreach (DOFType constraint in node.Constraints)
        //            //{
        //            //    if (constraint == dofType)
        //            //    {
        //            //        dofID = -1;
        //            //        break;
        //            //    }
        //            //}
        //            #endregion

        //            foreach (var constraint in node.Constraints)
        //            {
        //                if (constraint.DOF == dofType)
        //                {
        //                    dofID = -1;
        //                    break;
        //                }
        //            }

        //            //// TODO: this is not applicable! Embedded nodes have to do with the embedded element and not with the host
        //            //// User should define which DOFs would be dependent on the host element. For our case
        //            //// we should select between translational and translational + rotational
        //            //var embeddedNode = embeddedNodes.Where(x => x.Node == node).FirstOrDefault();
        //            ////if (node.EmbeddedInElement != null && node.EmbeddedInElement.ElementType.GetDOFTypes(null)
        //            ////    .SelectMany(d => d).Count(d => d == dofType) > 0)
        //            ////    dofID = -1;
        //            //if (embeddedNode != null && embeddedNode.EmbeddedInElement.ElementType.DOFEnumerator.GetDOFTypes(null)
        //            //    .SelectMany(d => d).Count(d => d == dofType) > 0)
        //            //    dofID = -1;

        //            if (dofID == 0)
        //            {
        //                dofID = totalDOFs;
        //                totalDOFs++;
        //            }
        //            dofsDictionary.Add(dofType, dofID);
        //        }

        //        nodalDOFsDictionary.Add(node.ID, dofsDictionary);
        //    }
        //}

        private void EnumerateDOFs()
        {
            GlobalDofOrdering = dofOrderer(this);
            foreach (Subdomain_v2 subdomain in Subdomains)
            {
                subdomain.DofOrdering = GlobalDofOrdering.SubdomainDofOrderings[subdomain];
                subdomain.Forces = new double[subdomain.DofOrdering.NumFreeDofs];
            }

            //EnumerateGlobalDOFs();
            //foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
            //{
            //    subdomain.EnumerateDOFs();
            //}
        }

        private void BuildConstraintDisplacementDictionary()
        {
            //foreach (Node node in nodesDictionary.Values)
            //{
            //    if (node.Constraints == null) continue;
            //    constraintsDictionary[node.ID] = new Dictionary<DOFType, double>();
            //    foreach (Constraint constraint in node.Constraints)
            //    {
            //        constraintsDictionary[node.ID][constraint.DOF] = constraint.Amount;
            //    }
            //}

            foreach (Node node in nodesDictionary.Values)
            {
                if (node.Constraints == null) continue;
                foreach (Constraint constraint in node.Constraints) Constraints[node, constraint.DOF] = constraint.Amount;
            }

            foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
                subdomain.BuildConstraintDisplacementDictionary();
        }

        private void AssignNodalLoads()
        {
            foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
            {
                subdomain.NodalLoads = new Table<Node, DOFType, double>();
            }

            foreach (Load load in loads)
            {
                double amountPerSubdomain = load.Amount / load.Node.SubdomainsDictionary_v2.Count;
                foreach (Subdomain_v2 subdomain in load.Node.SubdomainsDictionary_v2.Values)
                {
                    bool wasNotContained = subdomain.NodalLoads.TryAdd(load.Node, load.DOF, amountPerSubdomain);
                    Debug.Assert(wasNotContained, $"Duplicate load at node {load.Node.ID}, dof {load.DOF}");
                }
            }

            //TODO: this should be done by the subdomain when the analyzer decides.
            foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
            {
                Array.Clear(subdomain.Forces, 0, subdomain.Forces.Length);
                foreach ((Node node, DOFType dofType, double amount) in subdomain.NodalLoads)
                {
                    int subdomainDofIdx = subdomain.DofOrdering.FreeDofs[node, dofType];
                    subdomain.Forces[subdomainDofIdx] = amount;
                }
            }
        }

        private void AssignElementMassLoads()
        {
            foreach (ElementMassAccelerationLoad load in elementMassAccelerationLoads)
                load.Element.Subdomain.AddLocalVectorToGlobal(load.Element,
                    load.Element.ElementType.CalculateAccelerationForces(load.Element, massAccelerationLoads),
                    load.Element.Subdomain_v2.Forces);
        }

        private void AssignMassAccelerationLoads()
        {
            if (massAccelerationLoads.Count < 1) return;

            foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
            {
                foreach (Element element in subdomain.Elements)
                {
                    // subdomain.AddLocalVectorToGlobal(element,
                    //     element.ElementType.CalculateAccelerationForces(element, massAccelerationLoads),
                    //     subdomain.Forces);
                    subdomain.DofOrdering.AddVectorElementToSubdomain(element,
                        Vector.CreateFromArray(element.ElementType.CalculateAccelerationForces(element, massAccelerationLoads)),
                        Vector.CreateFromArray(subdomain.Forces));
                }
            }
                
        }

        public void AssignLoads()
        {
            AssignNodalLoads();
            AssignElementMassLoads();
            AssignMassAccelerationLoads();
        }

        public void AssignMassAccelerationHistoryLoads(int timeStep)
        {
            if (massAccelerationHistoryLoads.Count > 0)
            {
                List<MassAccelerationLoad> m = new List<MassAccelerationLoad>(massAccelerationHistoryLoads.Count);
                foreach (IMassAccelerationHistoryLoad l in massAccelerationHistoryLoads)
                {
                    m.Add(new MassAccelerationLoad() { Amount = l[timeStep], DOF = l.DOF });
                }

                foreach (Subdomain_v2 subdomain in subdomainsDictionary.Values)
                {
                    foreach (Element element in subdomain.Elements)
                    {
                        //subdomain.AddLocalVectorToGlobal(element,
                        //    element.ElementType.CalculateAccelerationForces(element, m), subdomain.Forces);
                        subdomain.DofOrdering.AddVectorElementToSubdomain(element,
                            Vector.CreateFromArray(element.ElementType.CalculateAccelerationForces(element, m)),
                            Vector.CreateFromArray(subdomain.Forces));
                    }
                }
            }

            foreach (ElementMassAccelerationHistoryLoad load in elementMassAccelerationHistoryLoads)
            {
                MassAccelerationLoad hl = new MassAccelerationLoad() { Amount = load.HistoryLoad[timeStep] * 564000000, DOF = load.HistoryLoad.DOF };
                load.Element.Subdomain.AddLocalVectorToGlobal(load.Element,
                    load.Element.ElementType.CalculateAccelerationForces(load.Element, (new MassAccelerationLoad[] { hl }).ToList()),
                    load.Element.Subdomain_v2.Forces);
            }
        }

        public void ConnectDataStructures()
        {
            BuildInterconnectionData();
            BuildConstraintDisplacementDictionary();
            EnumerateDOFs();
            //EnumerateSubdomainLagranges();
            //EnumerateDOFMultiplicity();

            //TODOMaria: Here is where the element loads are assembled
            //TODOSerafeim: This should be called by the analyzer, which defines when the dofs are ordered and when the global vectors/matrices are built.
            AssignLoads();
        }
        #endregion

        //What is the purpose of this method? If someone wanted to clear the Model, they could just create a new one.
        public void Clear()
        {
            loads.Clear();
            clustersDictionary.Clear();
            subdomainsDictionary.Clear();
            elementsDictionary.Clear();
            nodesDictionary.Clear();
            //nodalDOFsDictionary.Clear();
            GlobalDofOrdering = null;
            //constraintsDictionary.Clear();
            Constraints.Clear();
            elementMassAccelerationHistoryLoads.Clear();
            elementMassAccelerationLoads.Clear();
            massAccelerationHistoryLoads.Clear();
            massAccelerationLoads.Clear();
        }
    }
}