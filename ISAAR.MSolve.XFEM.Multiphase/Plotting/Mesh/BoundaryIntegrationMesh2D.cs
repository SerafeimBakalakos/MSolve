using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Multiphase.Elements;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;

namespace ISAAR.MSolve.XFEM.Multiphase.Plotting.Mesh
{
    public class BoundaryIntegrationMesh2D : IOutputMesh<XNode>
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;

        public BoundaryIntegrationMesh2D(XModel physicalModel)
        {
            this.outVertices = new List<VtkPoint>();
            this.outCells = new List<VtkCell>();
            int outVertexID = 0;
            foreach (IXFiniteElement element in physicalModel.Elements)
            {
                foreach (CurveElementIntersection intersection in element.PhaseIntersections.Values)
                {
                    Debug.Assert(intersection.RelativePosition != RelativePositionCurveElement.Disjoint);
                    Debug.Assert(intersection.IntersectionPoints.Length == 2);
                    if (intersection.RelativePosition == RelativePositionCurveElement.Tangent)
                    {
                        throw new NotImplementedException();
                    }

                    var vertices = new VtkPoint[2];
                    for (int i = 0; i < 2; ++i)
                    {
                        NaturalPoint natural = intersection.IntersectionPoints[i];
                        CartesianPoint point = 
                            element.InterpolationStandard.TransformNaturalToCartesian(element.Nodes, natural);
                        VtkPoint outVertex = new VtkPoint(outVertexID++, point.X, point.Y, point.Z);
                        vertices[i] = outVertex;
                        outVertices.Add(outVertex);
                    }

                    outCells.Add(new VtkCell(CellType.Line, vertices));
                }
            }
        }

        public int NumOutCells => outCells.Count;
        public int NumOutVertices => outVertices.Count;
        public IEnumerable<ICell<XNode>> OriginalCells => throw new NotImplementedException();
        public IEnumerable<XNode> OriginalVertices => throw new NotImplementedException();
        public IEnumerable<VtkCell> OutCells => outCells;
        public IEnumerable<VtkPoint> OutVertices => outVertices;
    }
}
