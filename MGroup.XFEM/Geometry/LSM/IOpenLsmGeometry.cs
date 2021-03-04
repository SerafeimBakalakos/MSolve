using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Geometry.LSM;

//TODO: Perhaps abstract the number/type/dimension of tips
namespace MGroup.XFEM.Geometry.LSM
{
    public interface IOpenLsmGeometry : ILsmGeometry
    {
        Dictionary<int, double> LevelSetsTip { get; } //TODO: rename to crack front so that it has meaning for 3D as well.
    }
}
