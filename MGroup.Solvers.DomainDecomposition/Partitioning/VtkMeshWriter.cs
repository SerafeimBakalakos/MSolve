using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MGroup.Solvers.DomainDecomposition.Partitioning
{
    public class VtkMeshWriter
    {
        public const string vtkReaderVersion = "4.1";

        public void WriteMesh2D(string path, IStructuredMesh mesh)
        {
            using (var writer = new StreamWriter(path))
            {
                WriteHeader(writer);
                WriteVertices2D(writer, mesh);
                WriteCells(writer, mesh);
            }
        }

        public void WriteMesh3D(string path, IStructuredMesh mesh)
        {
            using (var writer = new StreamWriter(path))
            {
                WriteHeader(writer);
                WriteVertices3D(writer, mesh);
                WriteCells(writer, mesh);
            }
        }

        private void WriteHeader(StreamWriter writer)
        {
            writer.WriteLine($"# vtk DataFile Version {vtkReaderVersion}");
            writer.WriteLine("Header:");
            writer.WriteLine("ASCII");
            writer.WriteLine();
            writer.WriteLine("DATASET UNSTRUCTURED_GRID");
        }

        private void WriteCells(StreamWriter writer, IStructuredMesh mesh)
        {
            // Cell connectivity
            int cellDataCount = mesh.NumElementsTotal * (1 + mesh.NumNodesPerElement);
            writer.WriteLine($"CELLS {mesh.NumElementsTotal} {cellDataCount}");
            for (int e = 0; e < mesh.NumElementsTotal; ++e)
            {
                int[] nodeIDs = mesh.GetElementConnectivity(e);
                writer.Write(nodeIDs.Length);
                foreach (int nodeID in nodeIDs)
                {
                    writer.Write($" {nodeID}");
                }
                writer.WriteLine();
            }
            writer.WriteLine();

            // Cell types
            int vtkCellCode = VtkCellTypes.GetVtkCellCode(mesh.CellType);
            writer.WriteLine("CELL_TYPES " + mesh.NumElementsTotal);
            for (int e = 0; e < mesh.NumElementsTotal; ++e)
            {
                writer.WriteLine(vtkCellCode);
            }
        }

        private void WriteVertices2D(StreamWriter writer, IStructuredMesh mesh)
        {
            writer.WriteLine($"POINTS {mesh.NumNodesTotal} double");
            for (int n = 0; n < mesh.NumNodesTotal; ++n)
            {
                double[] coords = mesh.GetNodeCoordinates(n);
                writer.WriteLine($"{coords[0]} {coords[1]} 0");
            }
            writer.WriteLine();
        }

        private void WriteVertices3D(StreamWriter writer, IStructuredMesh mesh)
        {
            writer.WriteLine($"POINTS {mesh.NumNodesTotal} double");
            for (int n = 0; n < mesh.NumNodesTotal; ++n)
            {
                double[] coords = mesh.GetNodeCoordinates(n);
                writer.WriteLine($"{coords[0]} {coords[1]} {coords[2]}");
            }
            writer.WriteLine();
        }
    }
}
