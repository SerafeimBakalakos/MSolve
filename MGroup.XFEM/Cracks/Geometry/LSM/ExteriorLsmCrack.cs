using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Geometry.Commons;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;


//TODO: Extract the purely geometric stuff to a dedicated geometry class. Enrichments, J-integrals, etc will either be kept here
//      or moved to other classes as well. Tracking items (nodes, elements, crack tip positions) from previous configurations
//      (and also solver specific data tracked) must be moved to dedicated logger classes (pull observers). 
START HERE: once I clean this up, I must test just the LSM geometry as it propagates (e.g a parabol, S-curve);


//TODO: perhaps the bookkeeping of nodes and elements can be done by a dedicated class. Narrow banding would then be implemented
//      there. In general, this is a god class and should be broken down to smaller ones.
//TODO: Crack tips should be handled differently than using enums. Interior and exterior cracks should compose their common 
//      dedicated strategy classes with their common functionality and expose appropriate properties for the crack tip data.
//TODO: Perhaps all loggers can be grouped and called together
//TODO: a lot of the tracking is just wasted memory and cpu time for most cases. It would be better to use observers to do it.
//      However, syncing the observers with the LSM is error prone and needs to be done at well defined points, without changing
//      the LSM itself and without too much memory duplication.
//TODO: A lot of functionality should be delegated to strategy classes. This can be done by having the strategy classes share
//      the fields of LSM and then mutate them when called upon. Each strategy classes gets injected with the fields it needs  
//      during construction. Alternatively I could have a readonly LSM interface that only exposes readonly properties, and a 
//      mutable one for the various strategy classes to mutate LSM data that they pull.
//TODO: If I do delegate a lot of functionality to strategy classes, how can the observers be updated correctly and efficiently,
//      namely without a lot of memory copying?
//TODO: Use a builder. It deserves one.
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public class ExteriorLsmCrack : IExteriorCrack
    {
        private static readonly bool reports = false;
        private static readonly IComparer<CartesianPoint> pointComparer = new Point2DComparerXMajor();
        private readonly OpenLsmSingleTip2D lsmGeometry;
        private readonly double tipEnrichmentAreaRadius;
        private readonly List<IXCrackElement> tipElements; // Ideally there is only 1, but what if the tip falls on the edge bewteen elements?
        private readonly ILsmMeshInteractionStrategy meshInteraction;
        private readonly IPropagator propagator;

        private readonly List<double[]> crackPath; //TODO: should this also be stored in the pure geometry class
        private ISet<XNode> crackBodyNodesAll; // TODO: a TreeSet might be better if set intersections are applied
        private ISet<XNode> crackBodyNodesModified;
        private ISet<XNode> crackBodyNodesNearModified;
        private ISet<XNode> crackBodyNodesNew;
        private ISet<XNode> crackBodyNodesRejected;
        private ISet<XNode> crackTipNodesNew;
        private ISet<XNode> crackTipNodesOld;

        public ExteriorLsmCrack(int id, IPropagator propagator, double tipEnrichmentAreaRadius,
            IHeavisideSingularityResolver singularityResolver)
        {
            Id = id;
            this.propagator = propagator;
            this.tipEnrichmentAreaRadius = tipEnrichmentAreaRadius;

            this.crackPath = new List<CartesianPoint>();
            this.lsmGeometry = new OpenLsmSingleTip2D();
            this.levelSetsTip = new Dictionary<XNode, double>();
            this.tipElements = new List<IXCrackElement>();

            this.crackBodyNodesAll = new HashSet<XNode>();
            this.crackBodyNodesModified = new HashSet<XNode>();
            this.crackBodyNodesNearModified = new HashSet<XNode>();
            this.crackBodyNodesNew = new HashSet<XNode>();
            this.crackBodyNodesRejected = new HashSet<XNode>();
            this.crackTipNodesNew = new HashSet<XNode>();
            this.crackTipNodesOld = new HashSet<XNode>();
            this.ElementsModified = new HashSet<IXCrackElement>();

            //this.meshInteraction = new StolarskaMeshInteraction(this);
            //this.meshInteraction = new HybridMeshInteraction(this);
            this.meshInteraction = new MeshInteractionStrategy2DSerafeim(this);
            this.SingularityResolver = singularityResolver;
        }



        public ExteriorLsmCrack(int id, IPropagator propagator, double tipEnrichmentAreaRadius = 0.0) :
            this(id, propagator, tipEnrichmentAreaRadius, new RelativeAreaResolver(1E-4))
        {
        }

        public int Id { get; }


        #region marked for removal. However I think I will use them in the new implementation
        public double[] CrackTip => crackTip;

        public TipCoordinateSystem TipSystem => tipSystem;
        #endregion

        #region data tracking. Only the absolutely necessary for the current configuration should stay
        public IReadOnlyList<CartesianPoint> CrackPath { get { return crackPath; } }

        public IReadOnlyDictionary<CrackBodyEnrichment2D, ISet<XNode>> CrackBodyNodesAll
        {
            get
            {
                return new Dictionary<CrackBodyEnrichment2D, ISet<XNode>> { [CrackBodyEnrichment] = crackBodyNodesAll };
            }
        }

        public IReadOnlyDictionary<CrackBodyEnrichment2D, ISet<XNode>> CrackBodyNodesModified
        {
            get
            {
                return new Dictionary<CrackBodyEnrichment2D, ISet<XNode>> { [CrackBodyEnrichment] = crackBodyNodesModified };
            }
        }

        public IReadOnlyDictionary<CrackBodyEnrichment2D, ISet<XNode>> CrackBodyNodesNearModified
        {
            get
            {
                return new Dictionary<CrackBodyEnrichment2D, ISet<XNode>> { [CrackBodyEnrichment] = crackBodyNodesNearModified };
            }
        }

        public IReadOnlyDictionary<CrackBodyEnrichment2D, ISet<XNode>> CrackBodyNodesNew
        {
            get
            {
                return new Dictionary<CrackBodyEnrichment2D, ISet<XNode>> { [CrackBodyEnrichment] = crackBodyNodesNew };
            }
        }

        public IReadOnlyDictionary<CrackBodyEnrichment2D, ISet<XNode>> CrackBodyNodesRejected
        {
            get
            {
                return new Dictionary<CrackBodyEnrichment2D, ISet<XNode>> { [CrackBodyEnrichment] = crackBodyNodesRejected };
            }
        }

        public IReadOnlyList<CartesianPoint> CrackTips { get { return new CartesianPoint[] { crackTip }; } }

        public IReadOnlyDictionary<CartesianPoint, IReadOnlyList<IXCrackElement>> CrackTipElements
        {
            get
            {
                var crackTipElements = new Dictionary<CartesianPoint, IReadOnlyList<IXCrackElement>>();
                crackTipElements.Add(this.crackTip, this.tipElements);
                return crackTipElements;
            }
        }

        public IReadOnlyDictionary<CartesianPoint, IPropagator> CrackTipPropagators
        {
            get
            {
                var tipPropagators = new Dictionary<CartesianPoint, IPropagator>();
                tipPropagators.Add(this.crackTip, this.propagator);
                return tipPropagators;
            }
        }

        public ISet<IXCrackElement> ElementsModified { get; private set; }
        
        public IReadOnlyDictionary<CrackTipEnrichments2D, ISet<XNode>> CrackTipNodesOld
        {
            get
            {
                return new Dictionary<CrackTipEnrichments2D, ISet<XNode>> { [CrackTipEnrichments] = crackTipNodesOld };
            }
        }

        public IReadOnlyDictionary<CrackTipEnrichments2D, ISet<XNode>> CrackTipNodesNew
        {
            get
            {
                return new Dictionary<CrackTipEnrichments2D, ISet<XNode>> { [CrackTipEnrichments] = crackTipNodesNew };
            }
        }

        public EnrichmentLogger EnrichmentLogger { get; set; }
        public LevelSetLogger LevelSetLogger { get; set; }
        public PreviousLevelSetComparer LevelSetComparer { get; set; }
        #endregion

        #region geometry : trim these down by delegating functionality elsewhere and then just call the delegated methods in a general Initialize/Update() method
        private void InitializeGeometry(IEnumerable<XNode> nodes, PolyLine2D initialCrack)
        {
            //TODO: This should work for any IOpenCurve2D. Same for all ICrackGeometryDescriptions.
            lsmGeometry.Initialize(nodes, initialCrack);

            foreach (var vertex in initialCrack.Vertices) crackPath.Add(vertex);

            CrackTipEnrichments.TipSystem = tipSystem;
            if (LevelSetLogger != null) LevelSetLogger.InitialLog(); //TODO: handle this with a NullLogger.
        }

        //TODO: make this private
        private void UpdateGeometry(double localGrowthAngle, double growthLength)
        {
            crackPath.Add(newTip);
            CrackTipEnrichments.TipSystem = tipSystem;

            //TODO: it is inconsistent that the modified body nodes are updated here, while the other in UpdateEnrichments(); 
            crackBodyNodesModified = levelSetUpdater.Update(oldTip, localGrowthAngle, growthLength, dx, dy, Mesh.Nodes,
                crackBodyNodesAll, levelSetsBody, levelSetsTip);
            if (LevelSetLogger != null) LevelSetLogger.Log(); //TODO: handle this with a NullLogger.
        }
        #endregion

        #region propagation
        public void Propagate(Dictionary<int, Vector> totalFreeDisplacements)
        {
            (double growthAngle, double growthLength) = propagator.Propagate(totalFreeDisplacements,
                crackTip, tipSystem, tipElements);
            UpdateGeometry(growthAngle, growthLength);
        }
        #endregion

        #region enrichments
        // TODO: Not too fond of the setters, but at least the enrichments are immutable. Perhaps I can pass their
        // parameters to a CrackDescription builder and construct them there, without involving the user.
        public CrackBodyEnrichment2D CrackBodyEnrichment { get; set; }
        public CrackTipEnrichments2D CrackTipEnrichments { get; set; }

        public IReadOnlyList<IEnrichmentItem2D> Enrichments
        {
            get { return new IEnrichmentItem2D[] { CrackBodyEnrichment, CrackTipEnrichments }; }
        }

        public IHeavisideSingularityResolver SingularityResolver { get; }

        // The tip enrichments are cleared and reapplied at each call. In constrast, nodes previously enriched with Heavise will 
        // continue to do so, with the addition of newly Heaviside enriched nodes. If the caller needs the nodes to be cleared of 
        // Heaviside enrichments he must do so himself.
        public void UpdateEnrichments()
        {
            // The order these are called is important, as they mess with state stored in the fields
            ClearPreviousEnrichments();
            FindBodyAndTipNodesAndElements();
            ApplyFixedEnrichmentArea(tipElements[0]);
            ResolveHeavisideEnrichmentDependencies();
            ApplyEnrichmentFunctions();

            // Modified elements
            ElementsModified.Clear();
            var modifiedNodes = new HashSet<XNode>(crackBodyNodesNew);
            modifiedNodes.UnionWith(crackTipNodesNew);
            modifiedNodes.UnionWith(crackTipNodesOld);
            modifiedNodes.UnionWith(crackBodyNodesModified);
            foreach (var node in modifiedNodes)
            {
                foreach (var element in Mesh.FindElementsWithNode(node)) ElementsModified.Add(element);
            }

            // Unmodified body nodes of modified elements
            crackBodyNodesNearModified.Clear();
            foreach (var element in ElementsModified)
            {
                foreach (var node in element.Nodes)
                {
                    // Only Heaviside enriched nodes of these elements are of concern.
                    // TODO: what about tip enriched nodes?
                    bool isEnrichedHeaviside = node.EnrichmentItems.ContainsKey(CrackBodyEnrichment);
                    if (!modifiedNodes.Contains(node) && isEnrichedHeaviside) crackBodyNodesNearModified.Add(node);

                    //WARNING: The next would also mark std nodes which is incorrect. Code left over as a reminder.
                    //if (!modifiedNodes.Contains(node)) CrackBodyNodesNearModified.Add(node);
                }
            }

            if (EnrichmentLogger != null) EnrichmentLogger.Log(); //TODO: handle this with a NullLogger.
            if (LevelSetComparer != null) LevelSetComparer.Log();
        }

        private void ApplyEnrichmentFunctions()
        {
            foreach (var node in crackTipNodesNew)
            {
                double[] enrichmentValues = CrackTipEnrichments.EvaluateFunctionsAt(node);
                node.EnrichmentItems[CrackTipEnrichments] = enrichmentValues;
            }

            // There is no need to process each mesh node. Once a node is enriched with Heaviside it will stay that way until the 
            // end. Even if the crack curves towards itself and a crack tip comes near the node, the original discontinuity must 
            // be represented by the original Heaviside (this case creates a lot of problems and cannot be modeled with LSM 
            // accurately anyway). 
            // I am not sure if the value of the Heaviside enrichment doesn't change for elements around the crack tip, once the 
            // crack propagates. In first order LSM this is improbable, since there cannot be kinks inside the element, but what 
            // about explicit cracks and higher order LSM?
            //TODO: It could be sped up by only updating the Heaviside enrichments of nodes that have updated body  
            //      level sets, which requires tracking them.
            //      - Done. Tracking newly enriched nodes is useful for many reasons.
            //TODO: should I also clear and reapply all Heaviside enrichments? It is safer and might be useful for e.g. 
            //      reanalysis. Certainly I must not clear all node enrichments, as they may include material interfaces etc.
            //      - Ans: nope in reanalysis old Heaviside enrichments are assumed to stay the same (not sure if that is correct 
            //          though)
            foreach (var node in crackBodyNodesNew)
            {
                double[] enrichmentValues = CrackBodyEnrichment.EvaluateFunctionsAt(node);
                node.EnrichmentItems[CrackBodyEnrichment] = enrichmentValues;
            }
        }

        /// <summary>
        /// If a fixed enrichment area is applied, all nodes inside a circle around the tip are enriched with tip 
        /// functions. They can still be enriched with Heaviside functions, if they do not belong to the tip 
        /// element(s).
        /// </summary>
        /// <param name="tipElement"></param>
        private void ApplyFixedEnrichmentArea(IXCrackElement tipElement)
        {
            if (tipEnrichmentAreaRadius > 0)
            {
                var enrichmentArea = new Circle2D(crackTip, tipEnrichmentAreaRadius);
                foreach (var element in Mesh.FindElementsInsideCircle(enrichmentArea, tipElement))
                {
                    bool completelyInside = true;
                    foreach (var node in element.Nodes)
                    {
                        CirclePointPosition position = enrichmentArea.FindRelativePositionOfPoint(node);
                        if ((position == CirclePointPosition.Inside) || (position == CirclePointPosition.On))
                        {
                            crackTipNodesNew.Add(node);
                        }
                        else completelyInside = false;
                    }

                    //TODO: I tried an alternative approach, ie elements access their enrichments from their nodes. 
                    //      My original thought that this approach (storing enrichments in elements, unless they are standard /
                    //      blending) wouldn't work for blending elements, was incorrect, as elements with 0 enrichments
                    //      were then examined and separated into standard / blending.
                    if (completelyInside) element.EnrichmentItems.Add(CrackTipEnrichments);
                }

                #region alternatively
                /* // If there wasn't a need to enrich the elements, this is more performant
                foreach (var node in mesh.FindNodesInsideCircle(enrichmentArea, true, tipElement))
                {
                    tipNodes.Add(node); // Nodes of tip element(s) will not be included twice
                } */
                #endregion
            }
        }

        private void ClearPreviousEnrichments()
        {
            //TODO: this method should not exist. Clearing these sets should be done in the methods where they are updated.
            //WARNING: Do not clear CrackBodyNodesModified here, since they are updated during UpdateGeometry() which is called first. 
            crackBodyNodesNew = new HashSet<XNode>();
            crackTipNodesOld = crackTipNodesNew;
            crackTipNodesNew = new HashSet<XNode>();
            foreach (var node in crackTipNodesOld) node.EnrichmentItems.Remove(CrackTipEnrichments);
            tipElements.Clear();
        }

        private void FindBodyAndTipNodesAndElements() //TODO: perhaps the singularity resolution should be done inside this method
        {
            foreach (var element in Mesh.Elements)
            {
                //TODO: I tried an alternative approach, ie elements access their enrichments from their nodes. 
                //      My original thought that this approach (storing enrichments in elements, unless they are standard /
                //      blending) wouldn't work for blending elements, was incorrect, as elements with 0 enrichments
                //      were then examined and separated into standard / blending.
                element.EnrichmentItems.Clear(); //TODO: not too fond of saving enrichment state in elements. It should be confined in nodes

                CrackElementPosition relativePosition = meshInteraction.FindRelativePositionOf(element);
                if (relativePosition == CrackElementPosition.ContainsTip)
                {
                    tipElements.Add(element);
                    foreach (var node in element.Nodes) crackTipNodesNew.Add(node);

                    element.EnrichmentItems.Add(CrackTipEnrichments);
                }
                else if (relativePosition == CrackElementPosition.Intersected)
                {
                    foreach (var node in element.Nodes)
                    {
                        bool isNew = crackBodyNodesAll.Add(node);
                        if (isNew) crackBodyNodesNew.Add(node);
                    }

                    // Cut elements next to tip elements will be enriched with both Heaviside and tip functions. If all 
                    // nodes were enriched with tip functions, the element would not be enriched with Heaviside, but then it 
                    // would be a tip element and not fall under this case.
                    element.EnrichmentItems.Add(CrackBodyEnrichment);
                }
            }
            foreach (var node in crackTipNodesNew) // tip element's nodes are not enriched with Heaviside
            {
                crackBodyNodesAll.Remove(node);
                crackBodyNodesNew.Remove(node);
            }

            Debug.Assert(tipElements.Count >= 1);
        }

        private void ResolveHeavisideEnrichmentDependencies()
        {
            //TODO: Is it safe to only search the newly enriched nodes? Update the rejected set appropriately
            crackBodyNodesRejected.Clear();
            crackBodyNodesRejected = SingularityResolver.FindHeavisideNodesToRemove(this, Mesh, crackBodyNodesAll);
            foreach (var node in crackBodyNodesRejected) // using set operations might be better and faster
            {
                //Console.WriteLine("Removing Heaviside enrichment from node: " + node);
                crackBodyNodesAll.Remove(node);
                crackBodyNodesNew.Remove(node);
            }
        }
        #endregion
    }
}
