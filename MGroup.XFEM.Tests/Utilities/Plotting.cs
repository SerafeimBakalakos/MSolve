using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.LSM;
using MGroup.XFEM.Geometry.Tolerances;
using MGroup.XFEM.Plotting.Writers;

namespace MGroup.XFEM.Tests.Utilities
{
    public class Plotting
    {
        public static Dictionary<IXFiniteElement, List<IElementDiscontinuityInteraction>> CalcIntersections(
            XModel<IXMultiphaseElement> model, IEnumerable<IClosedGeometry> geometries)
        {
            var intersections = new Dictionary<IXFiniteElement, List<IElementDiscontinuityInteraction>>();
            foreach (IXFiniteElement element in model.Elements)
            {
                var elementIntersections = new List<IElementDiscontinuityInteraction>();
                foreach (IClosedGeometry curve in geometries)
                {
                    IElementDiscontinuityInteraction intersection = curve.Intersect(element);
                    if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
                        element.InteractingDiscontinuities.Add(curve.ID, intersection);
                        elementIntersections.Add(intersection);
                    }
                }
                if (elementIntersections.Count > 0) intersections.Add(element, elementIntersections);
            }
            return intersections;
        }

        public static Dictionary<IXFiniteElement, List<IElementDiscontinuityInteraction>> CalcIntersections(
            XModel<IXMultiphaseElement> model, PhaseGeometryModel geometricModel)
        {
            Dictionary<int, IClosedGeometry> geometries = FindCurvesOf(geometricModel);
            var intersections = new Dictionary<IXFiniteElement, List<IElementDiscontinuityInteraction>>();
            foreach (IXFiniteElement element in model.Elements)
            {
                var elementIntersections = new List<IElementDiscontinuityInteraction>();
                foreach (IClosedGeometry geometry in geometries.Values)
                {
                    IElementDiscontinuityInteraction intersection = geometry.Intersect(element);
                    if (intersection.RelativePosition != RelativePositionCurveElement.Disjoint)
                    {
                        element.InteractingDiscontinuities.Add(geometry.ID, intersection);
                        elementIntersections.Add(intersection);
                    }
                }
                if (elementIntersections.Count > 0) intersections.Add(element, elementIntersections);
            }
            return intersections;
        }

        public static Dictionary<IXFiniteElement, IElementSubcell[]> CreateConformingMesh(int dimension,
            Dictionary<IXFiniteElement, List<IElementDiscontinuityInteraction>> intersections)
        {

            IConformingTriangulator triangulator;
            if (dimension == 2) triangulator = new ConformingTriangulator2D();
            else if (dimension == 3) triangulator = new ConformingTriangulator3D();
            else throw new NotImplementedException();

            var tolerance = new ArbitrarySideMeshTolerance();
            var conformingMesh = new Dictionary<IXFiniteElement, IElementSubcell[]>();
            foreach (IXFiniteElement element in intersections.Keys)
            {
                List<IElementDiscontinuityInteraction> elementIntersections = intersections[element];
                IElementSubcell[] subtriangles = triangulator.FindConformingMesh(element, elementIntersections, tolerance);
                conformingMesh[element] = subtriangles;
                element.ConformingSubcells = subtriangles;
            }
            return conformingMesh;
        }

        //public static IImplicitGeometry[] FindCurvesOf(GeometricModel geometricModel)
        //{
        //    var lsmCurves = new HashSet<IImplicitGeometry>();
        //    foreach (IPhase phase in geometricModel.Phases)
        //    {
        //        if (phase is DefaultPhase) continue;
        //        foreach (PhaseBoundary boundary in phase.ExternalBoundaries)
        //        {
        //            lsmCurves.Add(boundary.Geometry);
        //        }
        //    }
        //    return lsmCurves.ToArray();
        //}

        public static Dictionary<int, IClosedGeometry> FindCurvesOf(PhaseGeometryModel geometricModel)
        {
            var lsmCurves = new Dictionary<int, IClosedGeometry>();
            foreach (IPhase phase in geometricModel.Phases)
            {
                if (phase is DefaultPhase) continue;
                Debug.Assert(phase.ExternalBoundaries.Count == 1);
                lsmCurves[phase.ID] = phase.ExternalBoundaries[0].Geometry;
            }
            return lsmCurves;
        }

        public static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
            XModel<IXMultiphaseElement> model, PhaseGeometryModel geometricModel)
        {
            Dictionary<int, IClosedGeometry> lsmCurves = FindCurvesOf(geometricModel);
            foreach (var pair in lsmCurves)
            {
                int phaseId = pair.Key;
                IClosedGeometry geometry = pair.Value;
                directoryPath = directoryPath.Trim('\\');
                string suffix = (lsmCurves.Count == 1) ? "" : String.Format("{0:000}", phaseId);
                string file = $"{directoryPath}\\{vtkFilenamePrefix}{suffix}.vtk";
                using (var writer = new VtkFileWriter(file))
                {
                    var levelSetField = new LevelSetField(model, geometry);
                    writer.WriteMesh(levelSetField.Mesh);
                    writer.WriteScalarField($"inclusion{suffix}_level_set",
                        levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
                }
            }
        }

        //private static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix,
        //    XModel model, GeometricModel geometricModel)
        //{
        //    IImplicitGeometry[] lsmCurves = Utilities.Plotting.FindCurvesOf(geometricModel);
        //    for (int c = 0; c < lsmCurves.Length; ++c)
        //    {
        //        directoryPath = directoryPath.Trim('\\');
        //        string suffix = (lsmCurves.Length == 1) ? "" : $"{c}";
        //        string file = $"{directoryPath}\\{vtkFilenamePrefix}{suffix}.vtk";
        //        using (var writer = new VtkFileWriter(file))
        //        {
        //            var levelSetField = new LevelSetField(model, lsmCurves[c]);
        //            writer.WriteMesh(levelSetField.Mesh);
        //            writer.WriteScalarField($"inclusion{suffix}_level_set",
        //                levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
        //        }
        //    }
        //}
    }
}
