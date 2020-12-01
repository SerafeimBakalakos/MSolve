using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Enrichment.Enrichers;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;

//MODIFICATION NEEDED: Perhaps plotting element-node-phase interactions should be done using observers and called in this class
namespace MGroup.XFEM.Entities
{
    public class PhaseGeometryModel : IGeometryModel
    {
        private readonly XModel<IXMultiphaseElement> physicalModel;

        public PhaseGeometryModel(XModel<IXMultiphaseElement> physicalModel)
        {
            this.physicalModel = physicalModel;
        }

        public bool EnableOptimizations { get; set; }

        public INodeEnricher Enricher { get; set; }

        public bool MergeOverlappingPhases { get; set; } = true;

        public List<IPhaseObserver> Observers { get; } = new List<IPhaseObserver>();

        public Dictionary<int, IPhase> Phases { get; } = new Dictionary<int, IPhase>();
        
        public Dictionary<int, IPhaseBoundary> PhaseBoundaries { get; } = new Dictionary<int, IPhaseBoundary>();

        public Dictionary<int, double> CalcBulkSizeOfEachPhase() //MODIFICATION NEEDED: this is a pre/post-processing feature. No need to be in the core classes. Use an observer instead
        {
            var bulkSizes = new Dictionary<int, double>();
            foreach (IPhase phase in Phases.Values) bulkSizes[phase.ID] = 0.0;

            foreach (IXMultiphaseElement element in physicalModel.Elements)
            {
                if ((element.ConformingSubcells == null) || (element.ConformingSubcells.Length == 0))
                {
                    Debug.Assert(element.Phases.Count == 1);
                    IPhase phase = element.Phases.First();
                    double elementBulkSize = element.CalcBulkSizeCartesian();
                    bulkSizes[phase.ID] += elementBulkSize;
                }
                else
                {
                    foreach (IElementSubcell subcell in element.ConformingSubcells)
                    {
                        double[] centroidNatural = subcell.FindCentroidNatural();
                        var centroid = new XPoint(centroidNatural.Length);
                        centroid.Coordinates[CoordinateSystem.ElementNatural] = centroidNatural;
                        centroid.Element = element;
                        centroid.ShapeFunctions =
                            element.Interpolation.EvaluateFunctionsAt(centroid.Coordinates[CoordinateSystem.ElementNatural]);
                        element.FindPhaseAt(centroid);
                        IPhase phase = centroid.Phase;

                        (_, double subcellBulk) = subcell.FindCentroidAndBulkSizeCartesian(element);

                        bulkSizes[phase.ID] += subcellBulk;
                    }
                }
            }

            return bulkSizes;
        }

        public IEnumerable<IXDiscontinuity> EnumerateDiscontinuities() => PhaseBoundaries.Values;

        public IXDiscontinuity GetDiscontinuity(int discontinuityID) => PhaseBoundaries[discontinuityID];

        public void InitializeGeometry()
        {
            foreach (ClosedLsmPhaseBoundary boundary in PhaseBoundaries.Values)
            {
                boundary.InitializeGeometry();
            }

            foreach (IPhaseObserver observer in Observers) observer.LogGeometry();
        }

        public void InteractWithMesh()
        {
            // Phases - nodes
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.XNodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.XNodes);

            // Phases - elements
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements);

            // Phase boundaries - elements 
            //MODIFICATION NEEDED: I think this is done during Phases - elements. If this is the desired behavior, consider removing InteractWithMesh() from IXDiscontinuity and adding it to ICrack
            foreach (ClosedLsmPhaseBoundary boundary in PhaseBoundaries.Values)
            {
                boundary.InteractWithMesh();
            }

            foreach (IPhaseObserver observer in Observers) observer.LogMeshInteractions();
        }

        public void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            foreach (ClosedLsmPhaseBoundary boundary in PhaseBoundaries.Values)
            {
                boundary.UpdateGeometry(subdomainFreeDisplacements);
            }

            if (MergeOverlappingPhases) MergePhases();

            foreach (IPhaseObserver observer in Observers) observer.LogGeometry();
        }

        private void MergePhases()
        {
            var unifiedPhases = new List<IPhase>();
            IPhase defaultPhase = null;
            foreach (IPhase phase in Phases.Values)
            {
                if (phase is DefaultPhase)
                {
                    defaultPhase = phase;
                    continue;
                }
                unifiedPhases.Add(phase);
            }

            int i = 0;
            while (i < unifiedPhases.Count - 1)
            {
                bool unionFound = false;
                for (int j = i + 1; j < unifiedPhases.Count; ++j)
                {
                    bool areJoined = unifiedPhases[i].UnionWith(unifiedPhases[j]);
                    if (areJoined)
                    {
                        unifiedPhases.RemoveAt(j);
                        unionFound = true;
                        break;
                    }
                }
                if (!unionFound)
                {
                    ++i;
                }
            }

            Phases.Clear();
            if (defaultPhase != null) Phases[defaultPhase.ID] = defaultPhase;
            foreach (IPhase phase in unifiedPhases) Phases[phase.ID] = phase;
        }
    }
}
