//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace MGroup.XFEM.Geometry.LSM
//{
//    /// <summary>
//    /// Based on "Non-planar 3D crack growth by the extended finite element and level sets—Part II: Level set update, 2002, 
//    /// Gravouli et al.:.
//    /// </summary>
//    public class OpenLsm3D : ISingleTipLsmGeometry
//    {
//        public OpenLsm3D(int id)
//        {
//            this.ID = id;
//        }

//        public int ID { get; }

//        Dictionary<int, double> ILsmGeometry.LevelSets => LevelSetsBody;
//        public Dictionary<int, double> LevelSetsBody { get; } = new Dictionary<int, double>();
//        public Dictionary<int, double> LevelSetsTip { get; } = new Dictionary<int, double>();

//        public void InitializeLevelSetBody()
//        {

//        }

//        public void InitializeLevelSetTip()
//        {

//        }
//    }
//}
