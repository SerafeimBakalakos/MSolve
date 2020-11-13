using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: I dislike storing state in here and it increases memory requirements needlessly. In this case, the state stored here is 
//      only used to avoid recalculating coordinate system data for each of the 4 tip functions. Wouldn't it be better to store 
//      that data in XPoint? But then, what about XNode? Another approach would be to have all tip function objects (of the same 
//      crack tip) store and share these calculated values. They will always be calculated together and in order for the same 
//      point. I could have them store the values for each point in Dictionaries and when all 4 have used them, then remove that 
//      entry from the Dictionary.
namespace MGroup.XFEM.Cracks.Geometry
{
    public abstract class TipCoordinateSystemBase
    {
        protected readonly Dictionary<int, double[]> nodePolarCoords;
        protected readonly Dictionary<XPoint, TipJacobians> pointJacobians;
        protected readonly Dictionary<XPoint, double[]> pointPolarCoords;

        public TipCoordinateSystemBase()
        {
            nodePolarCoords = new Dictionary<int, double[]>();
            pointPolarCoords = new Dictionary<XPoint, double[]>();
            pointJacobians = new Dictionary<XPoint, TipJacobians>();
        }

        public Matrix RotationMatrixGlobalToLocal { get; protected set; }


        public TipJacobians CalcJacobiansAt(XPoint point)
        {
            bool alreadyProcessed = pointJacobians.TryGetValue(point, out TipJacobians jacobians);
            if (!alreadyProcessed)
            {
                double[] polarCoords = MapPointGlobalCartesianToLocalPolar(point);
                jacobians = new TipJacobians(polarCoords, RotationMatrixGlobalToLocal);
                pointJacobians[point] = jacobians;
            }
            return jacobians;
        }

        public double[] MapPointGlobalCartesianToLocalPolar(XNode node)
        {
            bool alreadyProcessed = nodePolarCoords.TryGetValue(node.ID, out double[] polarCoords);
            if (!alreadyProcessed)
            {
                polarCoords = TransformCoordsGlobalCartesianToLocalPolar(node.Coordinates);
                nodePolarCoords[node.ID] = polarCoords;
            }
            return polarCoords;
        }

        public double[] MapPointGlobalCartesianToLocalPolar(XPoint point)
        {
            bool alreadyProcessed = pointPolarCoords.TryGetValue(point, out double[] polarCoords);
            if (!alreadyProcessed)
            {
                double[] cartesianCoords = GetGlobalCartesianCoords(point);
                polarCoords = TransformCoordsGlobalCartesianToLocalPolar(cartesianCoords);
                pointPolarCoords[point] = polarCoords;
            }
            return polarCoords;
        }

        protected abstract double[] TransformCoordsGlobalCartesianToLocalPolar(double[] cartesianGlobalPoint);

        private double[] GetGlobalCartesianCoords(XPoint point)
        {
            bool hasCartesian = point.Coordinates.TryGetValue(CoordinateSystem.GlobalCartesian, out double[] cartesianCoords);
            if (!hasCartesian)
            {
                cartesianCoords = point.MapCoordinates(point.ShapeFunctions, point.Element.Nodes);
                point.Coordinates[CoordinateSystem.GlobalCartesian] = cartesianCoords;
            }
            return cartesianCoords;
        }
    }
}
