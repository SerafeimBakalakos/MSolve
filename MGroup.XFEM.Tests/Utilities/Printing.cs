using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Tests.EpoxyAg;

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

            double totalVolume = 0;
            foreach (string phase in volumes.Keys)
            {
                totalVolume += volumes[phase];
            }
            builder.AppendLine($"Total volume: {totalVolume}");

            var preprocessor = new GeometryPreprocessor3DRandom();
            double volFracAg = volumes[preprocessor.SilverPhaseName] / totalVolume;
            builder.AppendLine($"Volume fraction Ag: {volFracAg}");
            double volFracInclusions =
                (volumes[preprocessor.SilverPhaseName] + volumes[preprocessor.EpoxyPhaseName]) / totalVolume;
            builder.AppendLine($"Volume fraction inclusions: {volFracInclusions}");

            return builder.ToString();
        }
    }
}
