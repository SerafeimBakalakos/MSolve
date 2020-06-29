using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        public GeometricModel(int dimension, XModel physicalModel)
        {
            this.physicalModel = physicalModel;

            if (dimension != 2 && dimension != 3)
            {
                throw new ArgumentException();
            }
            this.Dimension = dimension;
        }

        public int Dimension { get; set; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        //TODO: Perhaps I need a dedicated class for this
        public void FindConformingMesh()
        {
            if (Dimension == 2)
            {
                var triangulator = new ConformingTriangulator2D();
                foreach (IXFiniteElement element in physicalModel.Elements)
                {
                    var element2D = (IXFiniteElement2D)element;
                    var boundaries = element.PhaseIntersections.Values.Cast<IElementCurveIntersection2D>();
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
                    var boundaries = element.PhaseIntersections.Values.Cast<IElementSurfaceIntersection3D>();
                    if (boundaries.Count() == 0) continue;
                    element3D.ConformingSubtetrahedra = triangulator.FindConformingMesh(element, boundaries, MeshTolerance);
                }
            }
        }

        public void InteractWithNodes()
        {
            // Nodes
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.Nodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.Nodes);
        }

        public void InteractWithElements()
        {
            // Elements
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements);

        }

        public void UnifyOverlappingPhases(bool ignoreDefaultPhase)
        {
            var unifiedPhases = new List<IPhase>();
            IPhase defaultPhase = null;
            foreach (IPhase phase in Phases)
            {
                if (phase is DefaultPhase && ignoreDefaultPhase)
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
            if (defaultPhase != null) Phases.Add(defaultPhase);
            Phases.AddRange(unifiedPhases);
        }
    }
}
