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
    public class GeometricModel
    {
        private readonly XModel physicalModel;
        private readonly Dictionary<int, Dictionary<PhaseBoundary, IElementGeometryIntersection>> phaseBoundariesOfElements;

        public GeometricModel(int dimension, XModel physicalModel)
        {
            this.physicalModel = physicalModel;

            if (dimension != 2 && dimension != 3)
            {
                throw new ArgumentException();
            }
            this.Dimension = dimension;

            phaseBoundariesOfElements = new Dictionary<int, Dictionary<PhaseBoundary, IElementGeometryIntersection>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                phaseBoundariesOfElements[element.ID] = new Dictionary<PhaseBoundary, IElementGeometryIntersection>();
            }
        }

        public int Dimension { get; set; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public void AddPhaseBoundaryToElement(IXFiniteElement element, PhaseBoundary boundary, 
            IElementGeometryIntersection intersection) 
            => phaseBoundariesOfElements[element.ID].Add(boundary, intersection);

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
            foreach (int phaseID in element.PhaseIDs)
            {
                IPhase phase = Phases[phaseID];
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

        public Dictionary<PhaseBoundary, IElementGeometryIntersection> GetPhaseBoundariesOfElement(IXFiniteElement element) 
            => phaseBoundariesOfElements[element.ID];


        //TODO: Perhaps I need a dedicated class for this
        private void FindConformingMesh()
        {
            if (Dimension == 2)
            {
                var triangulator = new ConformingTriangulator2D();
                foreach (IXFiniteElement element in physicalModel.Elements)
                {
                    var element2D = (IXFiniteElement2D)element;
                    var boundaries = phaseBoundariesOfElements[element.ID].Values.Cast<IElementCurveIntersection2D>();
                    if (boundaries.Count() == 0) continue;
                    element2D.ConformingSubtriangles = triangulator.FindConformingMesh(element, boundaries, MeshTolerance);
                }
            }
            else if (Dimension == 3)
            {
                var triangulator = new ConformingTriangulator3D();
                foreach (IXFiniteElement element in physicalModel.Elements)
                {
                    var element3D = (IXFiniteElement3D)element;
                    var boundaries = phaseBoundariesOfElements[element.ID].Values.Cast<IElementSurfaceIntersection3D>();
                    if (boundaries.Count() == 0) continue;
                    element3D.ConformingSubtetrahedra = triangulator.FindConformingMesh(element, boundaries, MeshTolerance);
                }
            }
        }
    }
}
