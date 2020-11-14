using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.Primitives;

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

        public CrackStepEnrichment CrackBodyEnrichment { get; }

        public IReadOnlyList<double[]> CrackPath => crackPath;

        public IXGeometryDescription CrackGeometry => lsmGeometry;

        public IReadOnlyList<ICrackTipEnrichment> CrackTipEnrichments { get; }

        public HashSet<IXCrackElement> IntersectedElements { get; }

        public int ID { get; }

        public ISingleTipLsmGeometry LsmGeometry => lsmGeometry;

        public HashSet<IXCrackElement> TipElements { get; } = new HashSet<IXCrackElement>();

        public override int GetHashCode() => ID.GetHashCode();

        #region marked for removal. However I think I will use them in the new implementation. Or expose them in a ISingleTipGeometry : IXGeometryDescription
        public double[] TipCoordinates => lsmGeometry.Tip;

        public TipCoordinateSystem TipSystem => lsmGeometry.TipSystem;
        #endregion

        #region state logging. Only the absolutely necessary for the current configuration should stay
        public List<ICrackObserver> Observers { get; } = new List<ICrackObserver>();

        // TODO: Analyzer uses IPropagator to check collapse, e.g reading the SIFs. But: calculation of SIFs, of next geometry direction&Length
        // and geometric update are different things currently hidden behind a single Propagate() method. Perhaps I should decouple them.
        // then SIFs should be stored somewhere (e.g. in ICrack and accessed from there).
        public IReadOnlyList<IPropagator> CrackTipPropagators => new IPropagator[] { this.propagator }; 
        #endregion

        #region geometry : trim these down by delegating functionality elsewhere and then just call the delegated methods in a general Initialize/Update() method
        //TODO: Since initialization is different for each crack representation, it would be better if I could refactor the 
        //      Initialize(), Update/Propagate(), InteractWithMesh() methods to make Initialize() more abstract or replace it
        //      with Update(). The parameters that are unique for each representation can be injected in the constructor.
        public void InitializeGeometry(IEnumerable<XNode> nodes, PolyLine2D initialCrack)
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

            // Call observers to pull any state they want
            foreach (ICrackObserver observer in Observers) observer.Update();
        }
        #endregion
    }
}
