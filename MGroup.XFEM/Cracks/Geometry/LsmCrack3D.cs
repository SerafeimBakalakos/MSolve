﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.PropagationTermination;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Enrichment.Functions;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Cracks.Geometry
{
    public class LsmCrack3D : ICrack
    {
        //private readonly List<double[]> crackPath; //TODO: should this also be stored in the pure geometry class?
        private readonly OpenLsm3D lsmGeometry; //TODO: Abstract this
        private readonly XModel<IXCrackElement> model;
        private readonly IPropagator propagator;

        public LsmCrack3D(int id, OpenLsm3D lsmGeometry, XModel<IXCrackElement> model, IPropagator propagator)
        {
            this.ID = id;
            this.model = model;

            // Geometry
            this.lsmGeometry = lsmGeometry;
            //this.crackPath = new List<double[]>();
            this.propagator = propagator;
        }

        public HashSet<IXCrackElement> ConformingElements { get; } = new HashSet<IXCrackElement>();

        public EnrichmentItem CrackBodyEnrichment { get; private set; }

        public IReadOnlyList<double[]> CrackPath => null/*new double[0][]*/; //HERE

        public IXGeometryDescription CrackGeometry => lsmGeometry;

        public EnrichmentItem CrackTipEnrichments { get; private set; }

        public int Dimension => 3;

        public HashSet<IXCrackElement> IntersectedElements { get; } = new HashSet<IXCrackElement>();

        public int ID { get; }

        public IOpenLsmGeometry LsmGeometry => lsmGeometry;

        public List<ICrackObserver> Observers { get; } = new List<ICrackObserver>();

        public double[] TipCoordinates => null; //HERE

        public HashSet<IXCrackElement> TipElements { get; } = new HashSet<IXCrackElement>();

        public TipCoordinateSystem TipSystem => null; //HERE

        public void CheckPropagation(IPropagationTermination termination)
        {
            //HERE
            double[] sifs = { propagator.Logger.SIFsMode1.Last(), propagator.Logger.SIFsMode2.Last() }; //TODO: These should be accessible without the logger.
            termination.Update(sifs, TipCoordinates);
        }

        public IList<EnrichmentItem> DefineEnrichments(int numCurrentEnrichments)
        {
            int enrichmentID = numCurrentEnrichments;

            // Crack body enrichment
            var stepEnrichmentFunc = new CrackStepEnrichment(this);
            IDofType[] stepEnrichedDofs =
            {
                new EnrichedDof(stepEnrichmentFunc, StructuralDof.TranslationX),
                new EnrichedDof(stepEnrichmentFunc, StructuralDof.TranslationY)
            };
            this.CrackBodyEnrichment = new EnrichmentItem(
                enrichmentID++, new IEnrichmentFunction[] { stepEnrichmentFunc }, stepEnrichedDofs);

            // Crack tip enrichments
            //TODO: For problems other than LEFM, use Abstract Factory pattern for tip enrichments, materials, propagators, etc.
            var tipEnrichment = new IsotropicBrittleTipEnrichments2D(() => null/*lsmGeometry.TipSystem*/); //HERE
            ICrackTipEnrichment[] tipEnrichmentFuncs = tipEnrichment.Functions;
            var tipEnrichedDofs = new List<IDofType>(12);
            for (int i = 0; i < tipEnrichmentFuncs.Length; ++i)
            {
                tipEnrichedDofs.Add(new EnrichedDof(tipEnrichmentFuncs[i], StructuralDof.TranslationX));
                tipEnrichedDofs.Add(new EnrichedDof(tipEnrichmentFuncs[i], StructuralDof.TranslationY));
                tipEnrichedDofs.Add(new EnrichedDof(tipEnrichmentFuncs[i], StructuralDof.TranslationZ));
            }
            this.CrackTipEnrichments = new EnrichmentItem(
                enrichmentID++, tipEnrichmentFuncs, tipEnrichedDofs.ToArray());

            return new EnrichmentItem[] { this.CrackBodyEnrichment, this.CrackTipEnrichments };
        }

        public override int GetHashCode() => ID.GetHashCode();

        public void InitializeGeometry()
        {
            //HERE
            //lsmGeometry.Initialize(model.XNodes, initialCrack);
            //foreach (var vertex in initialCrack.Vertices) crackPath.Add(vertex);
        }

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
                IElementOpenGeometryInteraction interaction = lsmGeometry.Intersect(element);
                if (interaction.TipInteractsWithElement)
                {
                    TipElements.Add(element);
                    element.RegisterInteractionWithCrack(this, interaction);
                }
                else if (interaction.RelativePosition == RelativePositionCurveElement.Intersecting)
                {
                    IntersectedElements.Add(element);
                    element.RegisterInteractionWithCrack(this, interaction);
                }
                else if (interaction.RelativePosition == RelativePositionCurveElement.Conforming)
                {
                    ConformingElements.Add(element);
                    element.RegisterInteractionWithCrack(this, interaction);
                }
            }

            // Call observers to pull any state they want
            foreach (ICrackObserver observer in Observers) observer.Update();
        }

        public void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            //HERE
            //(double growthAngle, double growthLength) = propagator.Propagate(
            //    subdomainFreeDisplacements, lsmGeometry.Tip, lsmGeometry.TipSystem, TipElements);
            //lsmGeometry.Update(model.XNodes, growthAngle, growthLength);
            //crackPath.Add(lsmGeometry.Tip);
        }
    }
}
