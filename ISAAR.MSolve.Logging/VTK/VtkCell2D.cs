using ISAAR.MSolve.FEM.Elements;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISAAR.MSolve.Logging.VTK
{
    public class VtkCell2D
    {
        public static readonly IReadOnlyDictionary<Type, int> cellTypeCodes = new Dictionary<Type, int>
        {
            { typeof(Quad4), 9 }
        };

        public VtkCell2D(int code, IReadOnlyList<VtkPoint2D> vertices)
        {
            this.Code = code;
            this.Vertices = vertices;
        }

        public int Code { get; }
        public IReadOnlyList<VtkPoint2D> Vertices { get; }
    }
}
