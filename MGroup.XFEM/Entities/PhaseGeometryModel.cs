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

//TODO: The order of operations is problematic when merging level sets, which is typically a geometric operation and thus
//      happens before geometry-mesh interaction. However merging level sets needs the interaction between phases and nodes.
//      Improve the current unsafe implementation.
namespace MGroup.XFEM.Entities
{
    public class PhaseGeometryModel : IGeometryModel
    {
        private readonly XModel<IXMultiphaseElement> physicalModel;
        private bool calcPhasesNodesInteractions = true;

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
            calcPhasesNodesInteractions = true;

            foreach (IPhaseBoundary boundary in PhaseBoundaries.Values)
            {
                boundary.InitializeGeometry();
            }

            if (MergeOverlappingPhases) MergePhases();


            foreach (IPhaseObserver observer in Observers) observer.LogGeometry();
        }

        public void InteractWithMesh()
        {
            // Phases - nodes
            InteractWithNodes();

            // Phases - elements
            IPhase defaultPhase = Phases.Values.Where(p => p is DefaultPhase).FirstOrDefault();
            foreach (IPhase phase in Phases.Values)
            {
                if (phase != defaultPhase) phase.InteractWithElements(physicalModel.Elements);
            }
            if (defaultPhase != null) defaultPhase.InteractWithElements(physicalModel.Elements);

            // Phase boundaries - elements 
            //MODIFICATION NEEDED: I think this is done during Phases - elements. If this is the desired behavior, consider removing InteractWithMesh() from IXDiscontinuity and adding it to ICrack
            foreach (IPhaseBoundary boundary in PhaseBoundaries.Values)
            {
                boundary.InteractWithMesh();
            }

            foreach (IPhaseObserver observer in Observers) observer.LogMeshInteractions();
        }

        public void UpdateGeometry(Dictionary<int, Vector> subdomainFreeDisplacements)
        {
            calcPhasesNodesInteractions = true;

            foreach (IPhaseBoundary boundary in PhaseBoundaries.Values)
            {
                boundary.UpdateGeometry(subdomainFreeDisplacements);
            }

            if (MergeOverlappingPhases) MergePhases();

            foreach (IPhaseObserver observer in Observers) observer.LogGeometry();
        }

        private void InteractWithNodes()
        {
            if (calcPhasesNodesInteractions)
            {
                IPhase defaultPhase = Phases.Values.Where(p => p is DefaultPhase).FirstOrDefault();
                foreach (IPhase phase in Phases.Values)
                {
                    if (phase != defaultPhase) phase.InteractWithNodes(physicalModel.XNodes);
                }
                if (defaultPhase != null) defaultPhase.InteractWithNodes(physicalModel.XNodes);
            }
            calcPhasesNodesInteractions = false;
        }

        private void MergePhases()
        {
            InteractWithNodes(); // Do it beforehand since it will assist with geometric unions

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

            PhaseBoundaries.Clear();
            foreach (IPhase phase in Phases.Values)
            {
                foreach (IPhaseBoundary boundary in phase.AllBoundaries)
                {
                    PhaseBoundaries[boundary.ID] = boundary;
                }
            }
        }
    }
}
