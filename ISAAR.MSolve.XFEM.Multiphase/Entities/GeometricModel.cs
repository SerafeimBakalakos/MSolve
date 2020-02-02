using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Triangulation;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class GeometricModel
    {
        public GeometricModel()
        {
        }

        public Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>> ConformingMesh { get; private set; }

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
    }
}
