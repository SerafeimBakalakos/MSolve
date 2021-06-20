using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra.Commons;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Interpolation;

//TODO: The global/local/fixed and 2D/3D hierarchies seem like a good candidate for Bridge pattern.
namespace MGroup.XFEM.Geometry.LSM.DualMesh
{
    public abstract class DualMeshLsm2DBase : IClosedGeometry
    {
        private const int dim = 2;

        protected readonly IDualMesh dualMesh;
        private readonly ValueComparer comparer;
        private readonly bool isFineMeshSimplicial;
        private readonly IIsoparametricInterpolation fineMeshInterpolation;
        private readonly LsmTri3Interaction intersectionStrategy;

        protected DualMeshLsm2DBase(int id, IDualMesh dualMesh)
        {
            this.dualMesh = dualMesh;
            this.ID = id;
            this.comparer = new ValueComparer(1E-6);

            if (dualMesh.FineMesh.CellType == CellType.Tri3)
            {
                this.isFineMeshSimplicial = true;
                this.intersectionStrategy = new LsmTri3Interaction();
                this.fineMeshInterpolation = InterpolationTri3.UniqueInstance;
            }
            else if (dualMesh.FineMesh.CellType == CellType.Quad4)
            {
                this.isFineMeshSimplicial = false;
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public int ID { get; }

        public IElementDiscontinuityInteraction Intersect(IXFiniteElement element)
        {
            // WARNING: This optimization must be avoided. Coarse elements may be flagged as disjoint incorrectly .
            //if (IsCoarseElementDisjoint(element)) return new NullElementDiscontinuityInteraction(this.ID, element);

            //HERE: merge the individual meshes from each fine element. The normals of the segments will always point towards
            //      the positive halfspace, thus the 2nd vertex of one segment will be the 1st segment of another.

            bool isIntersected = false;
            var totalIntersectionMesh = new IntersectionMesh2D();
            int[] fineElementIDs = dualMesh.MapElementCoarseToFine(element.ID);
            foreach (int fineElementID in fineElementIDs)
            {
                int[] fineElementIdx = dualMesh.FineMesh.GetElementIdx(fineElementID);
                int[] fineElementNodes = dualMesh.FineMesh.GetElementConnectivity(fineElementIdx);

                var nodeCoords = new List<double[]>();
                var nodeLevelSets = new List<double>();
                for (int n = 0; n < fineElementNodes.Length; ++n)
                {
                    nodeCoords.Add(fineMeshInterpolation.NodalNaturalCoordinates[n]);
                    nodeLevelSets.Add(GetLevelSet(fineElementNodes[n]));
                }

                (RelativePositionCurveElement relativePosition, IntersectionMesh2D intersectionMesh) =
                    intersectionStrategy.FindIntersection(fineElementNodes, nodeCoords, nodeLevelSets);

                if ((relativePosition == RelativePositionCurveElement.Disjoint) 
                    || (relativePosition == RelativePositionCurveElement.Tangent))
                {
                    continue;
                }
                if ((relativePosition == RelativePositionCurveElement.Intersecting) 
                    || (relativePosition == RelativePositionCurveElement.Conforming))
                {
                    //TODO: Also take care of the case that the coarse element is conforming. Especially important for comparisons with FEM.
                    //      ow can I check and what to do if the intersection mesh or part of it conforms to the element edges?
                    isIntersected = true;

                    // Convert the coordinates of the intersection points from the natural system of the fine element to the 
                    // natural system of the coarse element.
                    for (int p = 0; p < intersectionMesh.Vertices.Count; ++p)
                    {
                        intersectionMesh.Vertices[p] = dualMesh.MapPointFineNaturalToCoarseNatural(
                            fineElementIdx, intersectionMesh.Vertices[p]);
                    }

                    // Combine the line segments into a mesh
                    totalIntersectionMesh.MergeWith(intersectionMesh);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            if (isIntersected)
            {
                return new LsmElementIntersection2D(this.ID, RelativePositionCurveElement.Intersecting, element, totalIntersectionMesh);
            }
            else 
            {
                return new NullElementDiscontinuityInteraction(this.ID, element);
            }
        }

        public double SignedDistanceOf(XNode node) => GetLevelSet(dualMesh.MapNodeIDCoarseToFine(node.ID));

        public double SignedDistanceOf(XPoint point)
        {
            int coarseElementID = point.Element.ID;
            double[] coarseNaturalCoords = point.Coordinates[CoordinateSystem.ElementNatural];
            DualMeshPoint dualMeshPoint = dualMesh.CalcShapeFunctions(coarseElementID, coarseNaturalCoords);
            double[] shapeFunctions = dualMeshPoint.FineShapeFunctions;
            int[] fineNodes = dualMesh.FineMesh.GetElementConnectivity(dualMeshPoint.FineElementIdx);

            double result = 0;
            for (int n = 0; n < fineNodes.Length; ++n)
            {
                result += shapeFunctions[n] * GetLevelSet(fineNodes[n]);
            }
            return result;
        }

        public abstract void UnionWith(IClosedGeometry otherGeometry);

        protected abstract double GetLevelSet(int fineNodeID);
    }
}
