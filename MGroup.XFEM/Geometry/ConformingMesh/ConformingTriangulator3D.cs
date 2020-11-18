using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Geometry.Tolerances;
using MIConvexHull;

//TODO: Allow the option to specify the minimum triangle area.
namespace MGroup.XFEM.Geometry.ConformingMesh
{
    /// <summary>
    /// Does not work correctly if an element is intersected by more than one curves, which also intersect each other.
    /// </summary>
    public class ConformingTriangulator3D : IConformingTriangulator
    {
        IElementSubcell[] IConformingTriangulator.FindConformingMesh(
            IXFiniteElement element, IEnumerable<IElementDiscontinuityInteraction> intersections, IMeshTolerance meshTolerance)
            => FindConformingMesh(element, intersections, meshTolerance); 

        public ElementSubtetrahedron3D[] FindConformingMesh(IXFiniteElement element, 
            IEnumerable<IElementDiscontinuityInteraction> intersections, IMeshTolerance meshTolerance)
        {
            // Store the nodes and all intersection points in a set
            double tol = meshTolerance.CalcTolerance(element);
            var comparer = new Point3DComparer(tol);
            var nodes = new SortedSet<double[]>(comparer);
            nodes.UnionWith(element.Interpolation.NodalNaturalCoordinates);

            // Store the nodes and all intersection points in a different set
            var tetraVertices = new SortedSet<double[]>(comparer);
            tetraVertices.UnionWith(nodes);

            // Add intersection points from each curve-element intersection object.
            foreach (IElementDiscontinuityInteraction intersection in intersections)
            {
                // If the curve does not intersect this element (e.g. it conforms to the element edge), 
                // there is no need to take into account for triangulation
                if (intersection.RelativePosition != RelativePositionCurveElement.Intersecting) continue;

                IList<double[]> newVertices = intersection.GetVerticesForTriangulation();
                int countBeforeInsertion = tetraVertices.Count;
                tetraVertices.UnionWith(newVertices);

                if (tetraVertices.Count == countBeforeInsertion)
                {
                    // Corner case: the curve intersects the element at 4 opposite nodes. In this case also add their centroid 
                    // to force the Delauny algorithm to conform to the segment.
                    //TODO: I should use constrained Delauny in all cases and conform to the intersection segment.
                    bool areNodes = true;
                    var centroid = new double[3];
                    foreach (double[] vertex in newVertices)
                    {
                        if (!nodes.Contains(vertex)) areNodes = false;
                        for (int i = 0; i < 3; ++i)
                        {
                            centroid[i] += vertex[i];
                        }
                    }
                    for (int i = 0; i < 3; ++i)
                    {
                        centroid[i] /= newVertices.Count;
                    }

                    if (areNodes)
                    {
                        tetraVertices.Add(centroid);
                    }
                }
            }

            var triangulator = new MIConvexHullTriangulator3D();
            triangulator.MinTetrahedronVolume = tol * element.CalcBulkSizeNatural();
            IList<Tetrahedron3D> delaunyTetrahedra = triangulator.CreateMesh(tetraVertices);
            var subtetrahedra = new ElementSubtetrahedron3D[delaunyTetrahedra.Count];
            for (int t = 0; t < delaunyTetrahedra.Count; ++t)
            {
                subtetrahedra[t] = new ElementSubtetrahedron3D(delaunyTetrahedra[t]);
            }
            return subtetrahedra;
        }
    }
}
