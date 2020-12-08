using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Plotting.Fields;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Plotting.Writers
{
    public class PhaseLevelSetPlotter : IPhaseObserver
    {
        private readonly string directoryPath;
        private readonly PhaseGeometryModel geometryModel;
        private readonly IXModel physicalModel;
        private int iteration;

        public PhaseLevelSetPlotter(string directoryPath, IXModel physicalModel, PhaseGeometryModel geometryModel)
        {
            this.directoryPath = directoryPath.Trim('\\');
            this.physicalModel = physicalModel;
            this.geometryModel = geometryModel;
            iteration = 0;
        }

        public void LogGeometry()
        {
            var outMesh = new ContinuousOutputMesh(physicalModel.XNodes, physicalModel.EnumerateElements());
            foreach (IPhaseBoundary boundary in geometryModel.PhaseBoundaries.Values)
            {
                #region debug
                //if (boundary.ID != 1) continue;
                #endregion
                string file = $"{directoryPath}\\level_set{boundary.ID}_t{iteration}.vtk";
                using (var writer = new VtkFileWriter(file))
                {
                    var levelSetField = new LevelSetField(physicalModel, boundary.Geometry, outMesh);
                    writer.WriteMesh(levelSetField.Mesh);
                    writer.WriteScalarField($"inclusion_level_set",
                        levelSetField.Mesh, levelSetField.CalcValuesAtVertices());
                }
            }
        }

        public void LogMeshInteractions()
        {
            ++iteration;
        }
    }
}
