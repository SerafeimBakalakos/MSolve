using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Output.Mesh;
using ISAAR.MSolve.XFEM.Multiphase.Output.Writers;

namespace ISAAR.MSolve.XFEM.Multiphase.Output
{
    public class PhasePlotter
    {
        public const string vtkReaderVersion = "4.1";

        private readonly bool distinctColorForDefaultPhase;
        private readonly GeometricModel geometricModel;
        private readonly XModel physicalModel;

        public PhasePlotter(XModel physicalModel, GeometricModel geometricModel, bool distinctColorForDefaultPhase = false)
        {
            this.physicalModel = physicalModel;
            this.geometricModel = geometricModel;
            this.distinctColorForDefaultPhase = distinctColorForDefaultPhase;
        }

        public void PlotNodes(string path)
        {
            using (var writer = new Logging.VTK.VtkPointWriter(path))
            {
                var nodalPhases = new Dictionary<INode, double>();

                if (distinctColorForDefaultPhase)
                {
                    int defaultPhaseID = -555;
                    foreach (XNode node in physicalModel.Nodes)
                    {
                        int phase = node.SurroundingPhase.ID;
                        if (phase == DefaultPhase.DefaultPhaseID) phase = defaultPhaseID;
                        nodalPhases[node] = phase;
                    }
                }
                else
                {
                    foreach (XNode node in physicalModel.Nodes) nodalPhases[node] = node.SurroundingPhase.ID;
                }

                writer.WriteScalarField("nodal_phases", nodalPhases);
            }
        }

        public void PlotPhases(string path)
        {
            using (var writer = new VtkFileWriter(path))
            {
                var phaseMesh = new PhaseMesh<XNode>(geometricModel);
                writer.WriteMesh(phaseMesh);
            }
        }
    }
}
