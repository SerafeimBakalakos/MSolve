using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.FEM.Entities;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;

//TODO: Perhaps I should save the conforming mesh here as well, rather than in each element
namespace MGroup.XFEM.Entities
{
    public class GeometricModel2D
    {
        private readonly XModel physicalModel;
        private readonly Dictionary<int, Dictionary<PhaseBoundary2D, IElementCurveIntersection2D>> phaseBoundariesOfElements;
        private readonly Dictionary<int, HashSet<IPhase>> phasesOfElements;
        private readonly Dictionary<int, IPhase> phasesOfNodes;

        public GeometricModel2D(XModel physicalModel)
        {
            this.physicalModel = physicalModel;
            phasesOfNodes = new Dictionary<int, IPhase>();

            phasesOfElements = new Dictionary<int, HashSet<IPhase>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                phasesOfElements[element.ID] = new HashSet<IPhase>();
            }

            phaseBoundariesOfElements = new Dictionary<int, Dictionary<PhaseBoundary2D, IElementCurveIntersection2D>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                phaseBoundariesOfElements[element.ID] = new Dictionary<PhaseBoundary2D, IElementCurveIntersection2D>();
            }
        }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public void AddPhaseBoundaryToElement(IXFiniteElement element, PhaseBoundary2D boundary, 
            IElementCurveIntersection2D intersection) 
            => phaseBoundariesOfElements[element.ID].Add(boundary, intersection);

        public void AddPhaseToElement(IXFiniteElement element, IPhase phase) => phasesOfElements[element.ID].Add(phase);
        
        public void AddPhaseToNode(XNode node, IPhase phase) => phasesOfNodes[node.ID] = phase;

        public void InteractWithMesh()
        {
            // Nodes
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.Nodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.Nodes);

            // Elements
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements);

            FindConformingMesh();
        }

        public IPhase FindPhaseAt(XPoint point, IXFiniteElement element)
        {
            IPhase defaultPhase = null;
            foreach (IPhase phase in phasesOfElements[element.ID])
            {
                // Avoid searching for the point in the default phase, since its shape is higly irregular.
                if (phase is DefaultPhase)
                {
                    defaultPhase = phase;
                    continue;
                }
                else if (phase.Contains(point)) return phase;
            }

            // If the point is not contained in any other phases, it must be in the default phase 
            Debug.Assert(defaultPhase != null, "The point does not belong to any phases");
            return defaultPhase;
        }

        public Dictionary<PhaseBoundary2D, IElementCurveIntersection2D> GetPhaseBoundariesOfElement(IXFiniteElement element) 
            => phaseBoundariesOfElements[element.ID];
        public HashSet<IPhase> GetPhasesOfElement(IXFiniteElement element) => phasesOfElements[element.ID];
        public IPhase GetPhaseOfNode(XNode node)
        {
            bool exists = phasesOfNodes.TryGetValue(node.ID, out IPhase phase);
            if (exists) return phase;
            else return null;
        }

        //TODO: Perhaps I need a dedicated class for this
        private void FindConformingMesh()
        {
            var triangulator = new ConformingTriangulator2D();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                Dictionary<PhaseBoundary2D, IElementCurveIntersection2D> boundaries = phaseBoundariesOfElements[element.ID];
                if (boundaries.Count == 0) continue;
                element.ConformingSubtriangles2D = triangulator.FindConformingMesh(element, boundaries.Values, MeshTolerance);
            }
        }
    }
}
