using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Triangulation;
using ISAAR.MSolve.XFEM.Multiphase.Curves.MeshInteraction;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Entities
{
    public class GeometricModel
    {
        private readonly XModel physicalModel;

        public GeometricModel(XModel physicalModel)
        {
            this.physicalModel = physicalModel;
        }

        public Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>> ConformingMesh { get; private set; }

        public IMeshTolerance MeshTolerance { get; set; } = new ArbitrarySideMeshTolerance();

        public List<IPhase> Phases { get; } = new List<IPhase>();

        public void AssociatePhasesElements()
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithElements(physicalModel.Elements, MeshTolerance);
            }
            defaultPhase.InteractWithElements(physicalModel.Elements, MeshTolerance);
        }

        public void AssossiatePhasesNodes()
        {
            IPhase defaultPhase = Phases[0];
            for (int i = 1; i < Phases.Count; ++i)
            {
                Phases[i].InteractWithNodes(physicalModel.Nodes);
            }
            defaultPhase.InteractWithNodes(physicalModel.Nodes);
        }

        public void FindConformingMesh() //TODO: Perhaps I need a dedicated class for this
        {
            ConformingMesh = new Dictionary<IXFiniteElement, IReadOnlyList<ElementSubtriangle>>();
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                if (element.PhaseIntersections.Count == 0) continue;

                double tol = MeshTolerance.CalcTolerance(element);
                var comparer = new Point2DComparerXMajor<NaturalPoint>(tol); //TODO: This should be avoided.
                var triangleVertices = new SortedSet<NaturalPoint>(comparer); //TODO: Better use a HashSet, which needs a hash function for points.
                triangleVertices.UnionWith(element.StandardInterpolation.NodalNaturalCoordinates);
                var nodes = new SortedSet<NaturalPoint>(comparer);
                nodes.UnionWith(element.StandardInterpolation.NodalNaturalCoordinates);

                foreach (CurveElementIntersection intersection in element.PhaseIntersections.Values)
                {
                    // If the boundary goes through an element side, there is no need to triangulate
                    if (intersection.RelativePosition != RelativePositionCurveElement.Intersection) continue;

                    // Otherwise keep the intersection points
                    triangleVertices.UnionWith(intersection.IntersectionPoints);

                    // Corner case: the curve intersects the element at 2 opposite nodes. In this case also add the middle of their 
                    // segment to force the Delauny algorithm to conform to the segment.
                    //TODO: I should use constrained Delauny in all cases and conform to the intersection segment.
                    NaturalPoint p0 = intersection.IntersectionPoints[0];
                    NaturalPoint p1 = intersection.IntersectionPoints[1];
                    if (nodes.Contains(p0) && nodes.Contains(p1))
                    {
                        triangleVertices.Add(new NaturalPoint(05 * (p0.Xi + p1.Xi), 0.5 * (p0.Eta + p1.Eta)));
                    }
                }

                // Create Delauny triangulation
                var triangulator = new Triangulator2D<NaturalPoint>((x1, x2) => new NaturalPoint(x1, x2));
                List<Triangle2D<NaturalPoint>> triangles = triangulator.CreateMesh(triangleVertices);
                ElementSubtriangle[] subtriangles = triangles.Select(t => new ElementSubtriangle(t.Vertices)).ToArray();
                ConformingMesh[element] = subtriangles;
            }
        }
    }
}
