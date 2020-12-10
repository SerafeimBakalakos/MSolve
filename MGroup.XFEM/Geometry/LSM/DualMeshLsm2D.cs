using System;
using System.Collections.Generic;
using System.Text;
using MGroup.XFEM.Elements;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Mesh;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.LSM
{
    public class DualMeshLsm2D : IImplicitGeometry
    {
        private readonly DualMesh2D dualMesh;

        public DualMeshLsm2D(int id, DualMesh2D dualMesh, ICurve2D closedCurve)
        {
            this.dualMesh = dualMesh;
            IStructuredMesh lsmMesh = dualMesh.LsmMesh;
            this.ID = id;
            NodalLevelSets = new double[lsmMesh.NumNodesTotal];
            for (int n = 0; n < NodalLevelSets.Length; ++n)
            {
                double[] node = lsmMesh.GetNodeCoordinates(lsmMesh.GetNodeIdx(n));
                NodalLevelSets[n] = closedCurve.SignedDistanceOf(node);
            }
        }

        public double[] NodalLevelSets { get; }

        public int ID { get; }

        public IElementGeometryIntersection Intersect(IXFiniteElement element)
        {
            throw new NotImplementedException();
        }

        public double SignedDistanceOf(XNode node)
        {
            return NodalLevelSets[dualMesh.MapNodeFemToLsm(node.ID)];
        }

        public double SignedDistanceOf(XPoint point)
        {
            int femElementID = point.Element.ID;
            double[] femNaturalCoords = point.Coordinates[CoordinateSystem.ElementNatural];
            DualMeshPoint dualMeshPoint = dualMesh.CalcShapeFunctions(femElementID, femNaturalCoords);
            double[] shapeFunctions = dualMeshPoint.LsmShapeFunctions;
            int[] lsmNodes = dualMesh.LsmMesh.GetElementConnectivity(dualMeshPoint.LsmElementIdx);

            double result = 0;
            for (int n = 0; n < lsmNodes.Length; ++n)
            {
                result += shapeFunctions[n] * NodalLevelSets[lsmNodes[n]];
            }
            return result;
        }

        public void UnionWith(IImplicitGeometry otherGeometry)
        {
            if (otherGeometry is DualMeshLsm2D otherLsm)
            {
                if (this.NodalLevelSets.Length != otherLsm.NodalLevelSets.Length)
                {
                    throw new ArgumentException("Incompatible Level Set geometry");
                }
                for (int i = 0; i < this.NodalLevelSets.Length; ++i)
                {
                    this.NodalLevelSets[i] = Math.Min(this.NodalLevelSets[i], otherLsm.NodalLevelSets[i]);
                }
            }
            else throw new ArgumentException("Incompatible Level Set geometry");
        }
    }
}
