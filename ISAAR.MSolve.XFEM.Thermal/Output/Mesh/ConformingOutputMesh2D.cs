using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Thermal.Elements;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod;
using ISAAR.MSolve.XFEM.Thermal.LevelSetMethod.MeshInteraction;

namespace ISAAR.MSolve.XFEM.Thermal.Output.Mesh
{
    public class ConformingOutputMesh2D : IOutputMesh<XNode>
    {
        private readonly List<VtkCell> outCells;
        private readonly List<VtkPoint> outVertices;
        //private readonly Dictionary<IXFiniteElement, VtkCell> original2OutCells;
        //private readonly Dictionary<XNode, HashSet<VtkPoint>> original2OutVertices;

        public ConformingOutputMesh2D(IReadOnlyList<XNode> originalVertices, IReadOnlyList<IXFiniteElement> originalCells, 
            ILsmCurve2D discontinuity)
        {
            this.outVertices = new List<VtkPoint>(originalVertices.Count);
            this.outCells = new List<VtkCell>(originalCells.Count);

            //this.original2OutVertices = new Dictionary<XNode, HashSet<VtkPoint>>();
            //foreach (XNode vertex in originalVertices) original2OutVertices[vertex] = new HashSet<VtkPoint>();
            //this.original2OutCells = new Dictionary<IXFiniteElement, VtkCell>();

            int outVertexID = 0;
            for (int c = 0; c < originalCells.Count; ++c)
            {
                IXFiniteElement originalCell = originalCells[c];

                CurveElementIntersection intersection = discontinuity.IntersectElement(originalCell);
                if (intersection.RelativePosition == RelativePositionCurveElement.Intersection)
                {
                    bool success = discontinuity.TryConformingTriangulation(originalCell, intersection,
                        out IReadOnlyList<ElementSubtriangle> subtriangles);
                    Debug.Assert(success);
                    foreach (ElementSubtriangle triangle in subtriangles)
                    {
                        VtkPoint[] subvertices = triangle.GetVerticesCartesian(originalCell).
                            Select(v => new VtkPoint(outVertexID++, v.X, v.Y, v.Z)).
                            ToArray();
                        outVertices.AddRange(subvertices);

                        //TODO: The resulting triangle is Tri3 only for 1st order elements. Extend this.
                        Debug.Assert(originalCell.StandardInterpolation == FEM.Interpolation.InterpolationQuad4.UniqueInstance
                            || originalCell.StandardInterpolation == FEM.Interpolation.InterpolationTri3.UniqueInstance);
                        outCells.Add(new VtkCell(CellType.Tri3, subvertices));
                    }
                }
                else
                {
                    var verticesOfCell = new VtkPoint[originalCell.Nodes.Count];
                    for (int i = 0; i < originalCell.Nodes.Count; ++i)
                    {
                        XNode originalVertex = originalCell.Nodes[i];
                        var outVertex = new VtkPoint(outVertexID++, originalVertex.X, originalVertex.Y, originalVertex.Z);
                        outVertices.Add(outVertex);
                        //original2OutVertices[originalVertex].Add(outVertex);
                        verticesOfCell[i] = outVertex;
                    }
                    var outCell = new VtkCell(((IElementType)originalCell).CellType, verticesOfCell);
                    outCells.Add(outCell);
                    //original2OutCells[originalCell] = outCell;
                }
            }

            this.NumOutVertices = outVertices.Count;
            this.NumOutCells = outCells.Count;
        }

        public int NumOutCells { get; }

        public int NumOutVertices { get; }

        public IEnumerable<VtkCell> OutCells => outCells;

        public IEnumerable<VtkPoint> OutVertices => outVertices;

        //public IEnumerable<VtkCell> GetOutCellsForOriginal(IXFiniteElement originalCell)
        //    => new VtkCell[] { original2OutCells[originalCell] };

        //public IEnumerable<VtkPoint> GetOutVerticesForOriginal(XNode originalVertex)
        //    => original2OutVertices[originalVertex];
    }
}
