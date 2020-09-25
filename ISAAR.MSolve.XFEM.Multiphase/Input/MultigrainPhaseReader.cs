using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Entities;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Hexagons;
using ISAAR.MSolve.XFEM_OLD.Multiphase.Geometry.Voronoi;

namespace ISAAR.MSolve.XFEM_OLD.Multiphase.Input
{
    public class MultigrainPhaseReader
    {
        public MultigrainPhaseReader()
        {
        }

        public GeometricModel CreatePhasesFromHexagonalGrid(HexagonalGrid hexGrid)
        {
            var model = new GeometricModel();

            // Phases
            model.Phases.Add(new DefaultPhase());
            for (int c = 0; c < hexGrid.Cells.Count; ++c)
            {
                var phase = new ConvexPhase(c + 1);
                model.Phases.Add(phase);
            }

            // Boundaries
            for (int e = 0; e < hexGrid.Edges.Count; ++e)
            {
                int[] vertices = hexGrid.Edges[e].Vertices;
                var segment = new LineSegment2D(hexGrid.Vertices[vertices[0]], hexGrid.Vertices[vertices[1]]);
                IPhase posPhase = FindPhaseOfCellIndex(hexGrid.Edges[e].CellPositive, model);
                IPhase negPhase = FindPhaseOfCellIndex(hexGrid.Edges[e].CellNegative, model);
                new PhaseBoundary(segment, posPhase, negPhase);
            }

            return model;
        }

        public GeometricModel CreatePhasesFromVoronoi(VoronoiDiagram2D voronoiDiagram)
        {
            var model = new GeometricModel();

            // Phases
            model.Phases.Add(new DefaultPhase());
            for (int c = 0; c < voronoiDiagram.Cells.Count; ++c)
            {
                var phase = new ConvexPhase(c + 1);
                model.Phases.Add(phase);
            }

            // Boundaries
            for (int e = 0; e < voronoiDiagram.Edges.Count; ++e)
            {
                int[] vertices = voronoiDiagram.Edges[e].Vertices;
                var segment = new LineSegment2D(voronoiDiagram.Vertices[vertices[0]], voronoiDiagram.Vertices[vertices[1]]);
                IPhase posPhase = FindPhaseOfCellIndex(voronoiDiagram.Edges[e].CellPositive, model);
                IPhase negPhase = FindPhaseOfCellIndex(voronoiDiagram.Edges[e].CellNegative, model);
                new PhaseBoundary(segment, posPhase, negPhase);
            }

            return model;
        }

        private static IPhase FindPhaseOfCellIndex(int cellIndex, GeometricModel geometricModel)
        {
            if (cellIndex == VoronoiDiagram2D.externalSpaceIndex)
            {
                return geometricModel.Phases[0];
            }
            else
            {
                return geometricModel.Phases[cellIndex + 1];
            }
        }
    }
}
