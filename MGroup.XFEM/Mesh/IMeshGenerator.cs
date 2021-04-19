using System.Collections.Generic;

namespace MGroup.XFEM.Mesh
{
    /// <summary>
    /// Creates 2D and 3D meshes for use in FEM or similar methods.
    /// </summary>
    public interface IMeshGenerator
    {
        PreprocessingMesh CreateMesh();
    }
}
