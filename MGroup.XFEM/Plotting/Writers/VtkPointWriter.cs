using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Geometry.Coordinates;
using MGroup.XFEM.Plotting.Mesh;

namespace MGroup.XFEM.Plotting.Writers
{
    public class VtkPointWriter : IDisposable
    {
        public const string vtkReaderVersion = "4.1";
        private readonly StreamWriter writer;

        public VtkPointWriter(string filePath)
        {
            this.writer = new StreamWriter(filePath);
            writer.Write("# vtk DataFile Version ");
            writer.WriteLine(vtkReaderVersion);
            writer.WriteLine(filePath);
            writer.Write("ASCII\n\n");
        }

        public void Dispose()
        {
            if (writer != null) writer.Dispose();
        }

        public void WritePoints(IEnumerable<VtkPoint> points)
        {
            writer.WriteLine("DATASET UNSTRUCTURED_GRID");
            writer.WriteLine($"POINTS {points.Count()} double");
            foreach (VtkPoint point in points)
            {
                double[] coords = point.Get3DCoordinates();
                writer.WriteLine($"{coords[0]} {coords[1]} {coords[2]}");
            }
        }

        public void WriteScalarField(string fieldName, IReadOnlyDictionary<double[], double> pointValues)
        {
            // Points
            writer.WriteLine("DATASET UNSTRUCTURED_GRID");
            writer.WriteLine($"POINTS {pointValues.Count} double");
            foreach (double[] point in pointValues.Keys)
            {
                if (point.Length == 2) writer.WriteLine($"{point[0]} {point[1]} 0.0");
                else if (point.Length == 3) writer.WriteLine($"{point[0]} {point[1]} {point[2]}");
                else throw new NotImplementedException();
            }
            writer.Flush();

            // Values
            writer.Write("\n\n");
            writer.WriteLine($"POINT_DATA {pointValues.Count}");
            writer.WriteLine($"SCALARS {fieldName} double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (double value in pointValues.Values)
            {
                writer.WriteLine(value);
            }
            writer.WriteLine();
            writer.Flush();
        }

        //TODO: Avoid the duplication.
        public void WriteScalarField(string fieldName, IReadOnlyDictionary<INode, double> nodalValues)
        {
            // Points
            writer.WriteLine("DATASET UNSTRUCTURED_GRID");
            writer.WriteLine($"POINTS {nodalValues.Count} double");
            foreach (var point in nodalValues.Keys)
            {
                writer.WriteLine($"{point.X} {point.Y} {point.Z}");
            }

            // Values
            writer.Write("\n\n");
            writer.WriteLine($"POINT_DATA {nodalValues.Count}");
            writer.WriteLine($"SCALARS {fieldName} double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (var value in nodalValues.Values)
            {
                writer.WriteLine(value);
            }
            writer.WriteLine();
        }

        public void WriteVectorField(string fieldName, IReadOnlyDictionary<double[], double[]> pointVectors)
        {
            // Points
            writer.WriteLine("DATASET UNSTRUCTURED_GRID");
            writer.WriteLine($"POINTS {pointVectors.Count} double");
            foreach (double[] point in pointVectors.Keys)
            {
                if (point.Length == 2) writer.WriteLine($"{point[0]} {point[1]} 0.0");
                else if (point.Length == 3) writer.WriteLine($"{point[0]} {point[1]} {point[2]}");
                else throw new NotImplementedException();
            }

            // Values
            writer.Write("\n\n");
            writer.WriteLine($"POINT_DATA {pointVectors.Count}");
            writer.WriteLine($"VECTORS {fieldName} double");
            foreach (double[] vector in pointVectors.Values)
            {
                if (vector.Length == 2) writer.WriteLine($"{vector[0]} {vector[1]} 0.0");
                else if (vector.Length == 3) writer.WriteLine($"{vector[0]} {vector[1]} {vector[2]}");
                else throw new NotImplementedException();
            }
            writer.WriteLine();
        }
    }
}
