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
        private readonly Dictionary<int, HashSet<IPhase2D>> phasesOfElements;

        public GeometricModel2D(XModel physicalModel)
        {
            this.physicalModel = physicalModel;

            phasesOfElements = new Dictionary<int, HashSet<IPhase2D>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                phasesOfElements[element.ID] = new HashSet<IPhase2D>();
            }

            phaseBoundariesOfElements = new Dictionary<int, Dictionary<PhaseBoundary2D, IElementCurveIntersection2D>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                phaseBoundariesOfElements[element.ID] = new Dictionary<PhaseBoundary2D, IElementCurveIntersection2D>();
            }
        }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase2D> Phases { get; } = new List<IPhase2D>();

        public void AddPhaseBoundaryToElement(IXFiniteElement element, PhaseBoundary2D boundary, 
            IElementCurveIntersection2D intersection) 
            => phaseBoundariesOfElements[element.ID].Add(boundary, intersection);

        public void AddPhaseToElement(IXFiniteElement element, IPhase2D phase) => phasesOfElements[element.ID].Add(phase);
        
        public void InteractWithMesh()
        {
            // Nodes
            IPhase2D defaultPhase = Phases[0];
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

        public IPhase2D FindPhaseAt(XPoint point, IXFiniteElement element)
        {
            IPhase2D defaultPhase = null;
            foreach (IPhase2D phase in phasesOfElements[element.ID])
            {
                // Avoid searching for the point in the default phase, since its shape is higly irregular.
                if (phase is DefaultPhase2D)
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
        public HashSet<IPhase2D> GetPhasesOfElement(IXFiniteElement element) => phasesOfElements[element.ID];


        //TODO: Perhaps I need a dedicated class for this
        private void FindConformingMesh()
        {
            var triangulator = new ConformingTriangulator2D();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element2D = (IXFiniteElement2D)element;
                Dictionary<PhaseBoundary2D, IElementCurveIntersection2D> boundaries = phaseBoundariesOfElements[element.ID];
                if (boundaries.Count == 0) continue;
                element2D.ConformingSubtriangles = triangulator.FindConformingMesh(element, boundaries.Values, MeshTolerance);
            }
        }
    }
}
