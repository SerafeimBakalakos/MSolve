using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Triangulation;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Elements;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Entities
{
    public class GeometricModel
    {
        public GeometricModel()
        {
        }

        private Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>> ConformingMesh { get; set; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public void AssociatePhasesElements(XModel physicalModel)
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements, MeshTolerance);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements, MeshTolerance);
        }

        public void AssossiatePhasesNodes(XModel physicalModel)
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.Nodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.Nodes);
        }

        public void FindConformingMesh(XModel physicalModel) //TODO: Perhaps I need a dedicated class for this
        {
            ConformingMesh = new Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>>();
            var triangulator = new ConformingTriangulator();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                if (element.PhaseIntersections.Count == 0) continue;
                ConformingMesh[element] = 
                    triangulator.FindConformingMesh(element, element.PhaseIntersections.Values, MeshTolerance);
            }
        }

        public static IPhase FindPhaseAt(CartesianPoint point, IXFiniteElement element)
        {
            IPhase defaultPhase = null;
            foreach (IPhase phase in element.Phases)
            {
                // Avoid searching for the point in the default phase, since its shape is hihly irregular.
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

        public IReadOnlyList<ElementSubtriangle> GetConformingTriangulationOf(IXFiniteElement element)
        {
            Debug.Assert(ConformingMesh != null);
            return ConformingMesh[element];
        }
    }
}
