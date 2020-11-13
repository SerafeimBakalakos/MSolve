using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Geometry.Commons;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;
using TriangleNet;


//TODO: Tracking items (nodes, elements, crack tip positions) from previous configurations
//      (and also solver specific data tracked) must be moved to dedicated observer classes (pull observers). 
//BEFORE FINISHING THIS CLASS: once I clean this up, I must test just the LSM geometry as it propagates (e.g a parabol, S-curve);
//After finishing here, go tidy up TipCoordinateSystemBase


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
namespace MGroup.XFEM.Cracks.Geometry.LSM
{
    public class ExteriorLsmCrack : ICrack
    {
        private readonly List<double[]> crackPath; //TODO: should this also be stored in the pure geometry class?
        private readonly OpenLsmSingleTip2D lsmGeometry; //TODO: Abstract this
        private readonly XModel<IXCrackElement> model;
        private readonly IPropagator propagator;

        public ExteriorLsmCrack(int id, XModel<IXCrackElement> model, IPropagator propagator)
        {
            this.ID = id;
            this.model = model;

            // Geometry
            this.lsmGeometry = new OpenLsmSingleTip2D(id);
            this.crackPath = new List<double[]>();
            this.propagator = propagator;

            // Enrichments
            //TODO: Perhaps a global component (e.g. INodeEnricher) should number these ids. Or store enrichments in XModel, read 
            //      and update them here. Another approach is to not have ICrack expose its enrichments. Instead it will just 
            //      generate new ones (like a factory class) and they will be stored in INodeEnricher.
            int enrichmentIdStart = 10 * id;
            this.CrackBodyEnrichment = new CrackStepEnrichment(enrichmentIdStart, lsmGeometry);

            //TODO: For problems other than LEFM, use Abstract Factory pattern for tip enrichments, materials, propagators, etc.
            var tipEnrichments = new ICrackTipEnrichment[4];
            tipEnrichments[0] = new IsotropicBrittleTipEnrichments2D.Func0(enrichmentIdStart + 1, () => lsmGeometry.TipSystem);
            tipEnrichments[1] = new IsotropicBrittleTipEnrichments2D.Func1(enrichmentIdStart + 2, () => lsmGeometry.TipSystem);
            tipEnrichments[2] = new IsotropicBrittleTipEnrichments2D.Func2(enrichmentIdStart + 3, () => lsmGeometry.TipSystem);
            tipEnrichments[3] = new IsotropicBrittleTipEnrichments2D.Func3(enrichmentIdStart + 4, () => lsmGeometry.TipSystem);
            this.CrackTipEnrichments = tipEnrichments;
        }

        public HashSet<IXCrackElement> ConformingElements { get; }

        public IReadOnlyList<double[]> CrackPath => crackPath;

        public IXGeometryDescription CrackGeometry => lsmGeometry;

        public HashSet<IXCrackElement> IntersectedElements { get; }

        public int ID { get; }

        public HashSet<IXCrackElement> TipElements { get; } = new HashSet<IXCrackElement>();

        public override int GetHashCode() => ID.GetHashCode();

        #region marked for removal. However I think I will use them in the new implementation
        public double[] TipCoordinates => lsmGeometry.Tip;

        public TipCoordinateSystem TipSystem => lsmGeometry.TipSystem;
        #endregion

        #region state logging. Only the absolutely necessary for the current configuration should stay
        public ISet<XNode> CrackBodyNodesAll { get; }
        public ISet<XNode> CrackBodyNodesModified { get; }
        public ISet<XNode> CrackBodyNodesNearModified { get; }
        public ISet<XNode> CrackBodyNodesNew { get; }
        public ISet<XNode> CrackBodyNodesRejected { get; } // Rejected due to singular stiffnesses

        public IReadOnlyList<double[]> CrackTips => new double[][] { TipCoordinates };

        public IReadOnlyList<ICollection<IXCrackElement>> CrackTipElements => new HashSet<IXCrackElement>[] { TipElements };

        public IReadOnlyList<IPropagator> CrackTipPropagators => new IPropagator[] { this.propagator };

        public ISet<IXCrackElement> ElementsModified { get; private set; }

        public ISet<XNode> CrackTipNodesOld {get;}
        public ISet<XNode> CrackTipNodesNew { get; }

        //public EnrichmentLogger EnrichmentLogger { get; set; }
        //public LevelSetLogger LevelSetLogger { get; set; }
        //public PreviousLevelSetComparer LevelSetComparer { get; set; }
        #endregion

        #region geometry : trim these down by delegating functionality elsewhere and then just call the delegated methods in a general Initialize/Update() method
        private void InitializeGeometry(IEnumerable<XNode> nodes, PolyLine2D initialCrack)
        {
            lsmGeometry.Initialize(nodes, initialCrack);
            foreach (var vertex in initialCrack.Vertices) crackPath.Add(vertex);
        }

        public void Propagate(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            (double growthAngle, double growthLength) = propagator.Propagate(
                subdomainFreeDisplacements, lsmGeometry.Tip, lsmGeometry.TipSystem, TipElements);
            lsmGeometry.Update(model.XNodes, growthAngle, growthLength);
            crackPath.Add(lsmGeometry.Tip);
        }
        #endregion


        #region mesh interaction. These are probably the same for cracks represented with different ways
        public void InteractWithMesh()
        {
            //TODO: Optimization: Do not go over the intersecting elements that are already stored here. Even better,
            //      implement some sort of narrow band (here or in the geometry class), to avoid checking all the elements in 
            //      the model. This will blur the difference between this class and the geometry one even further. Perhaps this
            //      method belongs to the geometry class, but that would mean that the geometry class, also stores interacted 
            //      elements, which is this class's responsibility.

            // I do not clear the interaction of all elements with this crack. As the crack propagates, the interaction may 
            // change from intersected/conforming to tip, but it will never be removed.
            TipElements.Clear(); 
            foreach (IXCrackElement element in model.Elements)
            {
                IElementCrackInteraction interaction = lsmGeometry.Intersect(element);
                if (interaction.TipInteractsWithElement)
                {
                    TipElements.Add(element);
                    element.InteractingCracks[this] = interaction;
                }
                else if (interaction.RelativePosition == RelativePositionCurveElement.Intersecting)
                {
                    IntersectedElements.Add(element);
                    element.InteractingCracks[this] = interaction;
                }
                else if (interaction.RelativePosition == RelativePositionCurveElement.Conforming)
                {
                    ConformingElements.Add(element);
                    element.InteractingCracks[this] = interaction;
                }
            }
        }
        #endregion

        #region enrichments. These should probably be delegated to a ICrackEnricher class
        public CrackStepEnrichment CrackBodyEnrichment { get; }
        public IReadOnlyList<ICrackTipEnrichment> CrackTipEnrichments { get; }
        #endregion
    }
}
