//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Text;
//using ISAAR.MSolve.Geometry.Coordinates;
//using ISAAR.MSolve.Geometry.Shapes;
//using MGroup.XFEM.ElementGeometry;
//using MGroup.XFEM.Elements;
//using MGroup.XFEM.Geometry.LSM;
//using MGroup.XFEM.Geometry.LSM.Utilities;
//using MGroup.XFEM.Geometry.Primitives;
//using MGroup.XFEM.Geometry.Tolerances;
//using MIConvexHull;

////TODO: Allow the option to specify the minimum triangle area.
//namespace MGroup.XFEM.Geometry.ConformingMesh
//{
//    /// <summary>
//    /// Based on "Abaqus implementation of extended finite element method using a level set representation for three-dimensional 
//    /// fatigue crack growth and life predictions, Shi et al., 2010". Does not work if an element is intersected by more than one
//    /// curves.
//    /// </summary>
//    public class ShiTriangulator3D : IConformingTriangulator
//    {
//        IElementSubcell[] IConformingTriangulator.FindConformingMesh(
//            IXFiniteElement element, IEnumerable<IElementDiscontinuityInteraction> intersections, IMeshTolerance meshTolerance)
//            => FindConformingMesh(element, intersections, meshTolerance); 

//        public ElementSubtetrahedron3D[] FindConformingMesh(IXFiniteElement element, 
//            IEnumerable<IElementDiscontinuityInteraction> intersections, IMeshTolerance meshTolerance)
//        {
//            // Store the nodes and all intersection points in a set
//            double tol = meshTolerance.CalcTolerance(element);
//            var comparer = new Point3DComparer(tol);
//            var nodes = new SortedSet<double[]>(comparer);
//            nodes.UnionWith(element.Interpolation.NodalNaturalCoordinates);

//            // Store the nodes and all intersection points in a different set
//            var tetraVertices = new SortedSet<double[]>(comparer);
//            tetraVertices.UnionWith(nodes);

//            // Add intersection points from each curve-element intersection object.
//            foreach (IElementDiscontinuityInteraction intersection in intersections)
//            {
//                // If the curve does not intersect this element (e.g. it conforms to the element edge), 
//                // there is no need to take into account for triangulation
//                if (intersection.RelativePosition != RelativePositionCurveElement.Intersecting) continue;

//                ShiLsmElementIntersection3D intersectionCasted = (ShiLsmElementIntersection3D)intersection;
//                IList<IntersectionPoint> intersectionPoints = intersectionCasted.IntersectionPoints;

//                if (!intersectionCasted.TipInteractsWithElement)
//                {
//                    //5: Use the intersection points of each face to define the above/below crack subarea in each element face
//                    foreach (ElementFace face in intersection.Element.Faces)
//                    {
//                        // Find the intersection points of this face
//                        var intersectionsOfFace = new List<IntersectionPoint>();
//                        var edgesOfFace = new HashSet<ElementEdge>(face.Edges);
//                        foreach (IntersectionPoint intersectionPoint in intersectionPoints)
//                        {
//                            if (edgesOfFace.Contains(intersectionPoint.Edge))
//                            {
//                                intersectionsOfFace.Add(intersectionPoint);
//                            }
//                        }

//                        //HERE: find centroid, find positive, negative nodes, ...
//                        throw new NotImplementedException();
                        

//                        // Case 0: the face is not intersected by the crack



//                        // Case 1: the face is intersected
//                    }

//                    //6: Find the centroid of each subarea and connect it with the appropriate nodes and intersection points, creating conforming triangles

//                    //7: Define the above/below crack subvolume of the whole element

//                    //8: Find the centroid each subvolume and connect it to the vertices of the triangles on each face to create conforming subtetrahedra
//                }
//                else
//                {

//                }
                



//                //9: Delete the comments with this style and the next code
//                IList<double[]> newVertices = intersection.GetVerticesForTriangulation();
//                int countBeforeInsertion = tetraVertices.Count;
//                tetraVertices.UnionWith(newVertices);

//                if (tetraVertices.Count == countBeforeInsertion)
//                {
//                    // Corner case: the curve intersects the element at 4 opposite nodes. In this case also add their centroid 
//                    // to force the Delauny algorithm to conform to the segment.
//                    //TODO: I should use constrained Delauny in all cases and conform to the intersection segment.
//                    bool areNodes = true;
//                    var centroid = new double[3];
//                    foreach (double[] vertex in newVertices)
//                    {
//                        if (!nodes.Contains(vertex)) areNodes = false;
//                        for (int i = 0; i < 3; ++i)
//                        {
//                            centroid[i] += vertex[i];
//                        }
//                    }
//                    for (int i = 0; i < 3; ++i)
//                    {
//                        centroid[i] /= newVertices.Count;
//                    }

//                    if (areNodes)
//                    {
//                        tetraVertices.Add(centroid);
//                    }
//                }
//            }

//            var triangulator = new MIConvexHullTriangulator3D();
//            triangulator.MinTetrahedronVolume = tol * element.CalcBulkSizeNatural();
//            IList<Tetrahedron3D> delaunyTetrahedra = triangulator.CreateMesh(tetraVertices);
//            var subtetrahedra = new ElementSubtetrahedron3D[delaunyTetrahedra.Count];
//            for (int t = 0; t < delaunyTetrahedra.Count; ++t)
//            {
//                subtetrahedra[t] = new ElementSubtetrahedron3D(delaunyTetrahedra[t]);
//            }
//            return subtetrahedra;
//        }
//    }
//}
