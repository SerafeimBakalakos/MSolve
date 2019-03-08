﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.FEM.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Numerical.Commons;

//TODO: find what is going on with the dynamic loads and refactor them. That 564000000 in AssignMassAccelerationHistoryLoads()
//      cannot be correct.
//TODO: ConnectDataStructures() should not be called twice. There should be a flag that determines if it has been called. If it
//      has the method should just return without doing anything.
namespace ISAAR.MSolve.FEM.Entities
{
    public class Model_v2 : IStructuralModel_v2
    {
        //public IList<EmbeddedNode> EmbeddedNodes { get; } = new List<EmbeddedNode>();

        public IList<Cluster> Clusters => ClustersDictionary.Values.ToList();
        public Dictionary<int, Cluster> ClustersDictionary { get; } = new Dictionary<int, Cluster>();

        IReadOnlyList<IElement_v2> IStructuralModel_v2.Elements => ElementsDictionary.Values.ToList();
        public IList<Element_v2> Elements => ElementsDictionary.Values.ToList();
        public Dictionary<int, Element_v2> ElementsDictionary { get; } = new Dictionary<int, Element_v2>();

        public IList<ElementMassAccelerationHistoryLoad_v2> ElementMassAccelerationHistoryLoads { get; } 
            = new List<ElementMassAccelerationHistoryLoad_v2>();
        public IList<ElementMassAccelerationLoad_v2> ElementMassAccelerationLoads { get; } 
            = new List<ElementMassAccelerationLoad_v2>();
        public IList<Load_v2> Loads { get; } = new List<Load_v2>();
        public IList<MassAccelerationLoad> MassAccelerationLoads { get; } = new List<MassAccelerationLoad>();
        public IList<IMassAccelerationHistoryLoad> MassAccelerationHistoryLoads { get; } = new List<IMassAccelerationHistoryLoad>();

        public IList<Node_v2> Nodes => NodesDictionary.Values.ToList();
        IReadOnlyList<INode> IStructuralModel_v2.Nodes => NodesDictionary.Values.ToList();
        public Dictionary<int, Node_v2> NodesDictionary { get; } = new Dictionary<int, Node_v2>();

        IReadOnlyList<ISubdomain_v2> IStructuralModel_v2.Subdomains => SubdomainsDictionary.Values.ToList();
        public IReadOnlyList<Subdomain_v2> Subdomains => SubdomainsDictionary.Values.ToList();
        public Dictionary<int, Subdomain_v2> SubdomainsDictionary { get; } = new Dictionary<int, Subdomain_v2>();

        public IList<ITimeDependentNodalLoad> TimeDependentNodalLoads { get; } = new List<ITimeDependentNodalLoad>();

        public Table<INode, DOFType, double> Constraints { get; private set; } = new Table<INode, DOFType, double>();//TODOMaria: maybe it's useless in model class

        public IGlobalFreeDofOrdering GlobalDofOrdering { get; set; }

        public void AssignLoads()
        {
            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values) subdomain.Forces.Clear();
            AssignNodalLoads();
            AssignElementMassLoads();
            AssignMassAccelerationLoads();
        }

        public void AssignMassAccelerationHistoryLoads(int timeStep)
        {
            if (MassAccelerationHistoryLoads.Count > 0)
            {
                List<MassAccelerationLoad> m = new List<MassAccelerationLoad>(MassAccelerationHistoryLoads.Count);
                foreach (IMassAccelerationHistoryLoad l in MassAccelerationHistoryLoads)
                {
                    m.Add(new MassAccelerationLoad() { Amount = l[timeStep], DOF = l.DOF });
                }

                foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values)
                {
                    foreach (Element_v2 element in subdomain.Elements)
                    {
                        double[] accelerationForces = element.ElementType.CalculateAccelerationForces(element, m);
                        subdomain.FreeDofOrdering.AddVectorElementToSubdomain(element, accelerationForces, subdomain.Forces);
                    }
                }
            }

            foreach (ElementMassAccelerationHistoryLoad_v2 load in ElementMassAccelerationHistoryLoads)
            {
                MassAccelerationLoad hl = new MassAccelerationLoad()
                {
                    Amount = load.HistoryLoad[timeStep] * 564000000, DOF = load.HistoryLoad.DOF
                };
                Element_v2 element = load.Element;
                ISubdomain_v2 subdomain = element.Subdomain;
                var accelerationForces = element.ElementType.CalculateAccelerationForces(
                    load.Element, (new MassAccelerationLoad[] { hl }).ToList());
                GlobalDofOrdering.SubdomainDofOrderings[subdomain].AddVectorElementToSubdomain(element, accelerationForces,
                    subdomain.Forces);
            }
        }

        public void AssignNodalLoads()
        {
            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values)
            {
                subdomain.NodalLoads = new Table<Node_v2, DOFType, double>();
            }

            foreach (Load_v2 load in Loads)
            {
                //WARNING: Computing the forces vector norm from the subdomains requires that the total load is distributed evenly
                double amountPerSubdomain = load.Amount / load.Node.SubdomainsDictionary.Count;
                foreach (Subdomain_v2 subdomain in load.Node.SubdomainsDictionary.Values)
                {
                    bool wasNotContained = subdomain.NodalLoads.TryAdd(load.Node, load.DOF, amountPerSubdomain);
                    Debug.Assert(wasNotContained, $"Duplicate load at node {load.Node.ID}, dof {load.DOF}");
                }
            }

            //TODO: this should be done by the subdomain when the analyzer decides.
            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values)
            {
                foreach ((Node_v2 node, DOFType dofType, double amount) in subdomain.NodalLoads)
                {
                    int subdomainDofIdx = subdomain.FreeDofOrdering.FreeDofs[node, dofType];
                    subdomain.Forces[subdomainDofIdx] += amount;
                }
            }
        }

        public void AssignTimeDependentNodalLoads(int timeStep)
        {
            //WARNING: Computing the forces vector norm from the subdomains requires that the total load is distributed evenly
            foreach (ITimeDependentNodalLoad load in TimeDependentNodalLoads)
            {
                double amountPerSubdomain = load.GetLoadAmount(timeStep) / load.Node.SubdomainsDictionary.Count;

                foreach (ISubdomain_v2 subdomain in load.Node.SubdomainsDictionary.Values)
                {
                    int subdomainDofIdx = subdomain.FreeDofOrdering.FreeDofs[load.Node, load.DOF];
                    subdomain.Forces[subdomainDofIdx] += amountPerSubdomain;
                }
            }
        }

        //What is the purpose of this method? If someone wanted to clear the Model, they could just create a new one.
        public void Clear()
        {
            Loads.Clear();
            ClustersDictionary.Clear();
            SubdomainsDictionary.Clear();
            ElementsDictionary.Clear();
            NodesDictionary.Clear();
            GlobalDofOrdering = null;
            Constraints.Clear();
            ElementMassAccelerationHistoryLoads.Clear();
            ElementMassAccelerationLoads.Clear();
            MassAccelerationHistoryLoads.Clear();
            MassAccelerationLoads.Clear();
        }

        // Warning: This is called by the analyzer, so that the user does not have to call it explicitly. However, it is must be 
        // called explicitly before the AutomaticDomainDecompositioner is used.
        public void ConnectDataStructures()
        {         
            BuildInterconnectionData();
            AssignConstraints();
            //EnumerateDOFs();

            //TODOSerafeim: This should be called by the analyzer, which defines when the dofs are ordered and when the global vectors/matrices are built.
            //AssignLoads();
        }

        //TODO: constraints should not be saved inside the nodes. As it is right now (22/11/2018) the same constraint 
        //      is saved in the node, the model constraints table and the subdomain constraints table. Furthermore,
        //      displacement control analyzer updates the subdomain constraints table only (another bad design decision).  
        //      It is too easy to access the wrong instance of the constraint. 
        private void AssignConstraints()
        {
            foreach (Node_v2 node in NodesDictionary.Values)
            {
                if (node.Constraints == null) continue;
                foreach (Constraint constraint in node.Constraints) Constraints[node, constraint.DOF] = constraint.Amount;
            }

            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values) subdomain.ExtractConstraintsFromGlobal(Constraints);
        }

        private void AssignElementMassLoads()
        {
            foreach (ElementMassAccelerationLoad_v2 load in ElementMassAccelerationLoads)
            {
                ISubdomain_v2 subdomain = load.Element.Subdomain;
                var accelerationForces = load.Element.ElementType.CalculateAccelerationForces(
                    load.Element, MassAccelerationLoads);
                GlobalDofOrdering.SubdomainDofOrderings[subdomain].AddVectorElementToSubdomain(load.Element,
                    accelerationForces, subdomain.Forces);
            }
        }

        private void AssignMassAccelerationLoads()
        {
            if (MassAccelerationLoads.Count < 1) return;

            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values)
            {
                foreach (Element_v2 element in subdomain.Elements)
                {
                    subdomain.FreeDofOrdering.AddVectorElementToSubdomain(element,
                        element.ElementType.CalculateAccelerationForces(element, MassAccelerationLoads),
                        subdomain.Forces);
                }
            }
        }

        private void BuildElementDictionaryOfEachNode()
        {
            foreach (Element_v2 element in ElementsDictionary.Values)
            {
                foreach (Node_v2 node in element.Nodes) node.ElementsDictionary.Add(element.ID, element);
            }
        }

        private void BuildInterconnectionData()//TODOMaria: maybe I have to generate the constraints dictionary for each subdomain here
        {
            BuildSubdomainOfEachElement();
            DuplicateInterSubdomainEmbeddedElements();
            BuildElementDictionaryOfEachNode();
            foreach (Node_v2 node in NodesDictionary.Values) node.BuildSubdomainDictionary();

            //BuildNonConformingNodes();

            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values) subdomain.DefineNodesFromElements();
        }

        private void BuildSubdomainOfEachElement()
        {
            foreach (Subdomain_v2 subdomain in SubdomainsDictionary.Values)
            {
                foreach (Element_v2 element in subdomain.Elements) element.Subdomain = subdomain;
            }
        }

        private void BuildNonConformingNodes()
        {
            List<int> subIDs = new List<int>();
            foreach (Element_v2 element in ElementsDictionary.Values)
            {
                subIDs.Clear();

                foreach (Node_v2 node in element.Nodes)
                {
                    foreach (int subID in node.SubdomainsDictionary.Keys)
                    {
                        if (!subIDs.Contains(subID)) subIDs.Add(subID);

                    }
                }

                foreach (Node_v2 node in element.Nodes)
                {
                    foreach (int subID in subIDs)
                    {
                        if (!node.SubdomainsDictionary.ContainsKey(subID))
                        {
                            node.NonMatchingSubdomainsDictionary.Add(subID, SubdomainsDictionary[subID]);
                        }
                    }
                }
                
            }
        }

        private void DuplicateInterSubdomainEmbeddedElements()
        {
            foreach (var e in ElementsDictionary.Values.Where(x => x.ElementType is IEmbeddedElement_v2))
            {
                var subs = ((IEmbeddedElement_v2)e.ElementType).EmbeddedNodes.Select(x => x.EmbeddedInElement.Subdomain).Distinct();
                foreach (var s in subs.Where(x => x.ID != e.Subdomain.ID))
                    s.Elements.Add(e);
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
    }
}
