using System;
using System.Collections.Generic;
using System.Text;

namespace MGroup.XFEM.Tests.Utilities
{
    public static class Printing
    {
        public static string PrintVolumes(Dictionary<int, double> volumes)
        {
            var builder = new StringBuilder();
            builder.Append("Total areas of each material: ");
            foreach (int phase in volumes.Keys)
            {
                builder.Append($"{phase} phase : {volumes[phase]}, ");
            }
            return builder.ToString();
        }

        public static string PrintVolumes(Dictionary<string, double> volumes)
        {
            var builder = new StringBuilder();
            builder.Append("Total areas of each material: ");
            foreach (string phase in volumes.Keys)
            {
                builder.Append($"{phase} phase : {volumes[phase]}, ");
            }
            return builder.ToString();
        }
    }
}
