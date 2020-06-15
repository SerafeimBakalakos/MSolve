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
    public class GeometricModel3D
    {
        private readonly XModel physicalModel;
        private readonly Dictionary<int, Dictionary<PhaseBoundary3D, IElementSurfaceIntersection3D>> phaseBoundariesOfElements;

        public GeometricModel3D(XModel physicalModel)
        {
            this.physicalModel = physicalModel;

            phaseBoundariesOfElements = new Dictionary<int, Dictionary<PhaseBoundary3D, IElementSurfaceIntersection3D>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                phaseBoundariesOfElements[element.ID] = new Dictionary<PhaseBoundary3D, IElementSurfaceIntersection3D>();
            }
        }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase3D> Phases { get; } = new List<IPhase3D>();

        public void AddPhaseBoundaryToElement(IXFiniteElement element, PhaseBoundary3D boundary, 
            IElementSurfaceIntersection3D intersection) 
            => phaseBoundariesOfElements[element.ID].Add(boundary, intersection);

        public void InteractWithMesh()
        {
            // Nodes
            IPhase3D defaultPhase = Phases[0];
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

        public IPhase3D FindPhaseAt(XPoint point, IXFiniteElement element)
        {
            IPhase3D defaultPhase = null;
            foreach (int phaseID in element.PhaseIDs)
            {
                IPhase3D phase = Phases[phaseID];
                // Avoid searching for the point in the default phase, since its shape is higly irregular.
                if (phase is DefaultPhase3D)
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

        public Dictionary<PhaseBoundary3D, IElementSurfaceIntersection3D> GetPhaseBoundariesOfElement(IXFiniteElement element) 
            => phaseBoundariesOfElements[element.ID];

        //TODO: Perhaps I need a dedicated class for this
        private void FindConformingMesh()
        {
            var triangulator = new ConformingTriangulator3D();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                var element3D = (IXFiniteElement3D)element;
                Dictionary<PhaseBoundary3D, IElementSurfaceIntersection3D> boundaries = phaseBoundariesOfElements[element.ID];
                if (boundaries.Count == 0) continue;
                element3D.ConformingSubtetrahedra = triangulator.FindConformingMesh(element, boundaries.Values, MeshTolerance);
            }
        }
    }
}
