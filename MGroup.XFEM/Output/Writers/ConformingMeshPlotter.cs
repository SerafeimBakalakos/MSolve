using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Output.Mesh;
using MGroup.XFEM.Output.Vtk;

namespace MGroup.XFEM.Output.Writers
{
    public class ConformingMeshPlotter : IModelObserver
    {
        private readonly IXModel model;
        private readonly bool plotOnlyIntersectedElements;
        private readonly string outputDirectory;
        private int iteration;

        public ConformingMeshPlotter(string outputDirectory, IXModel model, bool plotOnlyIntersectedElements = false)
        {
            this.outputDirectory = outputDirectory;
            this.model = model;
            this.plotOnlyIntersectedElements = plotOnlyIntersectedElements;
            iteration = 0;
        }

        public void Update()
        {
            string filename;
            if (plotOnlyIntersectedElements) filename = "conforming_mesh_of_intersected_elements";
            else filename = "conforming_mesh";
            string path = Path.Combine(outputDirectory, $"{filename}_t{iteration}.vtk");
            using (var writer = new VtkFileWriter(path))
            { 
                var conformingMesh = new ConformingOutputMesh(model, plotOnlyIntersectedElements);
                writer.WriteMesh(conformingMesh);
            }

            ++iteration;
        }
    }
}
