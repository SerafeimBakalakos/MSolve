using System.Collections.Generic;
using ISAAR.MSolve.Logging.VTK;

//TODO: Perhaps I should use IDs for vertices, cells and avoid any generics.
namespace ISAAR.MSolve.XFEM_OLD.Thermal.Output.Mesh
{
    /// <summary>
    /// Stores vertices and cells used for output (so far visualization). Also defines associations between the vertices
    /// and cells of the output mesh and the corresponding ones of the original mesh used for the analysis.
    /// </summary>
    public interface IOutputMesh<TNode>
    {
        int NumOutCells { get; }
        int NumOutVertices { get; }

        /// <summary>
        /// The order will be always the same.
        /// </summary>
        IEnumerable<VtkCell> OutCells { get; }

        /// <summary>
        /// The order will be always the same.
        /// </summary>
        IEnumerable<VtkPoint> OutVertices { get; }

        ///// <summary>
        ///// The order will be always the same.
        ///// </summary>
        //IEnumerable<VtkCell> GetOutCellsForOriginal(ICell<TNode> originalCell);

        ///// <summary>
        ///// The order will be always the same.
        ///// </summary>
        //IEnumerable<VtkPoint> GetOutVerticesForOriginal(TNode originalVertex);
    }
}
