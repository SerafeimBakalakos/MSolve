using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.XFEM.Thermal.Entities;
using ISAAR.MSolve.XFEM.Thermal.Curves;
using ISAAR.MSolve.XFEM.Thermal.Output.Fields;
using ISAAR.MSolve.XFEM.Thermal.Output.Writers;

namespace ISAAR.MSolve.XFEM.Tests.HEAT.Plotting
{
    internal static class Utilities
    {
        internal static void PlotInclusionLevelSets(string directoryPath, string vtkFilenamePrefix, 
            XModel model, GeometricModel2D geometry)
        {
            for (int c = 0; c < geometry.SingleCurves.Count; ++c)
            {
                directoryPath = directoryPath.Trim('\\');
                string suffix = (geometry.SingleCurves.Count == 1) ? "" : $"{c}";
                string file = $"{directoryPath}\\{vtkFilenamePrefix}{suffix}.vtk";
                using (var writer = new VtkFileWriter(file))
                {
                    var levelSetField = new LevelSetField(model, geometry.SingleCurves[c]);
                    writer.WriteMesh(levelSetField.Mesh);
                    writer.WriteScalarField($"inclusion{suffix}_level_set", 
                        levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
                }
            }
        }
    }
}
