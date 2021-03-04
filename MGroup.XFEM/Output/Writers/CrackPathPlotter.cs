using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Cracks;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;
using MGroup.XFEM.Cracks.Geometry;

namespace MGroup.XFEM.Output.Writers
{
    public class CrackPathPlotter : ICrackObserver
    {
        private readonly LsmCrack2DExterior crack;
        private readonly string outputDirectory;
        private int iteration;

        public CrackPathPlotter(LsmCrack2DExterior crack,string outputDirectory)
        {
            this.crack = crack;
            this.outputDirectory = outputDirectory;
            iteration = 0;
        }

        public void Update()
        {
            // Log the crack path
            if (crack.CrackPath.Count > 0)
            using (var crackWriter = new VtkPolylineWriter($"{outputDirectory}\\crack_path_{iteration}.vtk"))
            {
                crackWriter.WritePolyline(crack.CrackPath);
            }
            ++iteration;
        }
    }
}
