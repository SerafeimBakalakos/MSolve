using System;
using System.Collections.Generic;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

namespace MGroup.XFEM.Geometry.Cracks
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
