using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ISAAR.MSolve.Logging.VTK;
using ISAAR.MSolve.XFEM.Thermal.Output.Fields;
using ISAAR.MSolve.XFEM.Thermal.Output.Mesh;

namespace ISAAR.MSolve.XFEM.Thermal.Output.Writers
{
    public class VtkFileWriter : IDisposable
    {
        public const string vtkReaderVersion = "4.1";
        private readonly StreamWriter writer;
        private bool writeFieldsNext;
        //private int numVertices = -1;

        public VtkFileWriter(string filePath)
        {
            this.writer = new StreamWriter(filePath);
            writer.Write("# vtk DataFile Version ");
            writer.WriteLine(vtkReaderVersion);
            writer.WriteLine(filePath);
            writer.Write("ASCII\n\n");
            writeFieldsNext = false;
        }

        public void Dispose()
        {
            if (writer != null) writer.Dispose();
        }

        //TODO: Perhaps the mesh should be injected into the contructor
        public void WriteMesh<TNode>(IOutputMesh<TNode> mesh)
        {
            if (writeFieldsNext) throw new InvalidOperationException("A mesh has already been written.");

            // Vertices 
            //this.numVertices = mesh.NumOutVertices;
            writer.WriteLine("DATASET UNSTRUCTURED_GRID");
            writer.WriteLine($"POINTS {mesh.NumOutVertices} double");
            foreach (VtkPoint point in mesh.OutVertices) writer.WriteLine($"{point.X} {point.Y} 0.0");

            // Cell connectivity
            int cellDataCount = 0;
            foreach (VtkCell cell in mesh.OutCells) cellDataCount += 1 + cell.Vertices.Count;
            writer.WriteLine($"\nCELLS {mesh.NumOutCells} {cellDataCount}");
            foreach (VtkCell cell in mesh.OutCells)
            {
                writer.Write(cell.Vertices.Count);
                foreach (VtkPoint point in cell.Vertices)
                {
                    writer.Write(' ');
                    writer.Write(point.ID);
                }
                writer.WriteLine();
            }

            // Cell types
            writer.WriteLine("\nCELL_TYPES " + mesh.NumOutCells);
            foreach (VtkCell cell in mesh.OutCells) writer.WriteLine(cell.Code);
        }

        public void WriteScalarField<TNode>(string fieldName, IOutputMesh<TNode> mesh, Func<VtkPoint, double> getScalarValue)
        {
            WriteFieldsHeader(mesh.NumOutVertices);
            writer.WriteLine($"SCALARS {fieldName} double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (VtkPoint vertex in mesh.OutVertices) writer.WriteLine(getScalarValue(vertex));
            writer.WriteLine();
        }

        public void WriteScalarField<TNode>(string fieldName, IOutputMesh<TNode> mesh, IEnumerable<double> scalarsAtVertices)
        {
            WriteFieldsHeader(mesh.NumOutVertices);
            writer.WriteLine($"SCALARS {fieldName} double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (double val in scalarsAtVertices) writer.WriteLine(val);
            writer.WriteLine();
        }

        public void WriteTensor2DField<TNode>(string fieldName, IOutputMesh<TNode> mesh, Func<VtkPoint, double[]> getTensorValue)
        {
            WriteFieldsHeader(mesh.NumOutVertices);

            // Component 11
            writer.WriteLine($"SCALARS {fieldName}_11 double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (VtkPoint vertex in mesh.OutVertices) writer.WriteLine(getTensorValue(vertex)[0]);
            writer.WriteLine();

            // Component 22
            writer.WriteLine($"SCALARS {fieldName}_22 double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (VtkPoint vertex in mesh.OutVertices) writer.WriteLine(getTensorValue(vertex)[1]);
            writer.WriteLine();

            // Component 12
            writer.WriteLine($"SCALARS {fieldName}_12 double 1");
            writer.WriteLine("LOOKUP_TABLE default");
            foreach (VtkPoint vertex in mesh.OutVertices) writer.WriteLine(getTensorValue(vertex)[2]);
            writer.WriteLine();
        }

        public void WriteVector2DField<TNode>(string fieldName, IOutputMesh<TNode> mesh, IEnumerable<double[]> vectorsAtVertices)
        {
            WriteFieldsHeader(mesh.NumOutVertices);
            writer.WriteLine($"VECTORS {fieldName} double");
            foreach (double[] vector in vectorsAtVertices) writer.WriteLine($"{vector[0]} {vector[1]} 0.0");
            writer.WriteLine();
        }

        public void WriteVector2DField<TNode>(string fieldName, IOutputMesh<TNode> mesh, Func<VtkPoint, double[]> getVectorValue)
        {
            WriteFieldsHeader(mesh.NumOutVertices);
            writer.WriteLine($"VECTORS {fieldName} double");
            foreach (VtkPoint vertex in mesh.OutVertices)
            {
                double[] vector = getVectorValue(vertex);
                writer.WriteLine($"{vector[0]} {vector[1]} 0.0");
            }
            writer.WriteLine();
        }

        public void WriteVector3DField<TNode>(string fieldName, IOutputMesh<TNode> mesh, Func<VtkPoint, double[]> getVectorValue)
        {
            WriteFieldsHeader(mesh.NumOutVertices);
            writer.WriteLine($"VECTORS {fieldName} double");
            foreach (VtkPoint vertex in mesh.OutVertices)
            {
                double[] vector = getVectorValue(vertex);
                writer.WriteLine($"{vector[0]} {vector[1]} {vector[2]}");
            }
            writer.WriteLine();
        }

        /// <summary>
        /// If the user only wants the mesh, this should not be called. Therefore only call it if one or more field output is 
        /// written.
        /// </summary>
        private void WriteFieldsHeader(int numVertices)
        {
            if (!writeFieldsNext) // Fields header
            {
                writer.Write("\n\n");
                writer.WriteLine("POINT_DATA " + numVertices);
                writeFieldsNext = true;
            }
        }
    }
}
