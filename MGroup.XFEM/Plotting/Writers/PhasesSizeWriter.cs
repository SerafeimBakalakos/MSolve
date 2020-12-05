using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MGroup.XFEM.Entities;

namespace MGroup.XFEM.Plotting.Writers
{
    public class PhasesSizeWriter : IModelObserver
    {
        private readonly PhaseGeometryModel geometryModel;
        private readonly string outputDirectory;

        private int iteration;

        public PhasesSizeWriter(string outputDirectory, PhaseGeometryModel geometryModel)
        {
            this.outputDirectory = outputDirectory;
            this.geometryModel = geometryModel;

            iteration = 0;
        }

        public void Update()
        {
            var volumes = geometryModel.CalcBulkSizeOfEachPhase();
            string path = Path.Combine(outputDirectory, $"phase_sizes_t{iteration}.txt");
            using (var writer = new StreamWriter(path))
            {
                var builder = new StringBuilder();
                writer.WriteLine("Total areas/volumes of each material phase:");
                writer.WriteLine("Phase Size");
                foreach (int phase in volumes.Keys)
                {
                    writer.WriteLine($"{phase} {volumes[phase]}");
                }
                writer.Flush();
            }

            ++iteration;
        }
    }
}
