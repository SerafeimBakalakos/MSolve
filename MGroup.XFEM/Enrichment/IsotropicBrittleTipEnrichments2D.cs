using System;
using System.Collections.Generic;
using System.Text;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using MGroup.XFEM.Cracks.Geometry;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry.Primitives;

//TODO: I could make an IXOpenGeometry:IXGeometryDescription that also exposes tip data and use that here instead of the delegate.
//      However, how would these classes choose which tip to use, if there are multiple ones? With the delegate this is very easy.
namespace MGroup.XFEM.Enrichment
{
    public static class IsotropicBrittleTipEnrichments2D
    {
        public class Func0 : ICrackTipEnrichment
        {
            private readonly Func<TipCoordinateSystem> getTipSystem;

            public Func0(int id, Func<TipCoordinateSystem> getTipSystem)
            {
                this.ID = id;
                this.getTipSystem = getTipSystem;
            }

            public int ID { get; }

            public IReadOnlyList<IPhase> Phases => throw new NotImplementedException();

            public EvaluatedFunction EvaluateAllAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                TipJacobians jacobians = tipSystem.CalcJacobiansAt(point);

                double sqrtR = Math.Sqrt(polarCoords[0]);
                double cosThetaHalf = Math.Cos(polarCoords[1] / 2.0);
                double sinThetaHalf = Math.Sin(polarCoords[1] / 2.0);

                double value = sqrtR * sinThetaHalf;
                var gradientPolar = Vector.CreateFromArray(new double[]
                {
                    0.5 / sqrtR * sinThetaHalf, 0.5 * sqrtR * cosThetaHalf
                });

                Vector gradientGlobal = jacobians.TransformScalarFieldDerivativesLocalPolarToGlobalCartesian(gradientPolar);
                return new EvaluatedFunction(value, gradientGlobal.RawData);
            }

            public double EvaluateAt(XNode node)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(node);
                return Math.Sqrt(polarCoords[0]) * Math.Sin(polarCoords[1] / 2.0);
            }

            public double EvaluateAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                return Math.Sqrt(polarCoords[0]) * Math.Sin(polarCoords[1] / 2.0);
            }

            public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }

            public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }
        }

        public class Func1 : ICrackTipEnrichment
        {
            private readonly Func<TipCoordinateSystem> getTipSystem;

            public Func1(int id, Func<TipCoordinateSystem> getTipSystem)
            {
                this.ID = id;
                this.getTipSystem = getTipSystem;
            }

            public int ID { get; }

            public IReadOnlyList<IPhase> Phases => throw new NotImplementedException();

            public EvaluatedFunction EvaluateAllAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                TipJacobians jacobians = tipSystem.CalcJacobiansAt(point);

                double sqrtR = Math.Sqrt(polarCoords[0]);
                double cosThetaHalf = Math.Cos(polarCoords[1] / 2.0);
                double sinThetaHalf = Math.Sin(polarCoords[1] / 2.0);

                double value = sqrtR * cosThetaHalf;
                var gradientPolar = Vector.CreateFromArray(new double[]
                {
                    0.5 / sqrtR * cosThetaHalf, -0.5 * sqrtR * sinThetaHalf
                });

                Vector gradientGlobal = jacobians.TransformScalarFieldDerivativesLocalPolarToGlobalCartesian(gradientPolar);
                return new EvaluatedFunction(value, gradientGlobal.RawData);
            }

            public double EvaluateAt(XNode node)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(node);
                return Math.Sqrt(polarCoords[0]) * Math.Cos(polarCoords[1] / 2.0);
            }

            public double EvaluateAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                return Math.Sqrt(polarCoords[0]) * Math.Cos(polarCoords[1] / 2.0);
            }

            public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }

            public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }
        }

        public class Func2 : ICrackTipEnrichment
        {
            private readonly Func<TipCoordinateSystem> getTipSystem;

            public Func2(int id, Func<TipCoordinateSystem> getTipSystem)
            {
                this.ID = id;
                this.getTipSystem = getTipSystem;
            }

            public int ID { get; }

            public IReadOnlyList<IPhase> Phases => throw new NotImplementedException();

            public EvaluatedFunction EvaluateAllAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                TipJacobians jacobians = tipSystem.CalcJacobiansAt(point);

                double sqrtR = Math.Sqrt(polarCoords[0]);
                double cosTheta = Math.Cos(polarCoords[1]);
                double sinTheta = Math.Sin(polarCoords[1]);
                double cosThetaHalf = Math.Cos(polarCoords[1] / 2.0);
                double sinThetaHalf = Math.Sin(polarCoords[1] / 2.0);

                double value = sqrtR * sinThetaHalf * sinTheta;
                var gradientPolar = Vector.CreateFromArray(new double[]
                {
                    0.5 / sqrtR * sinThetaHalf * sinTheta, sqrtR * (0.5 * cosThetaHalf * sinTheta + sinThetaHalf * cosTheta)
                });

                Vector gradientGlobal = jacobians.TransformScalarFieldDerivativesLocalPolarToGlobalCartesian(gradientPolar);
                return new EvaluatedFunction(value, gradientGlobal.RawData);
            }

            public double EvaluateAt(XNode node)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(node);
                return Math.Sqrt(polarCoords[0]) * Math.Sin(polarCoords[1] / 2.0) * Math.Sin(polarCoords[1]);
            }

            public double EvaluateAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                return Math.Sqrt(polarCoords[0]) * Math.Sin(polarCoords[1] / 2.0) * Math.Sin(polarCoords[1]);
            }

            public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }

            public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }
        }

        public class Func3 : ICrackTipEnrichment
        {
            private readonly Func<TipCoordinateSystem> getTipSystem;

            public Func3(int id, Func<TipCoordinateSystem> getTipSystem)
            {
                this.ID = id;
                this.getTipSystem = getTipSystem;
            }

            public int ID { get; }

            public IReadOnlyList<IPhase> Phases => throw new NotImplementedException();

            public EvaluatedFunction EvaluateAllAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                TipJacobians jacobians = tipSystem.CalcJacobiansAt(point);

                double sqrtR = Math.Sqrt(polarCoords[0]);
                double cosTheta = Math.Cos(polarCoords[1]);
                double sinTheta = Math.Sin(polarCoords[1]);
                double cosThetaHalf = Math.Cos(polarCoords[1] / 2.0);
                double sinThetaHalf = Math.Sin(polarCoords[1] / 2.0);

                double value = sqrtR * cosThetaHalf * sinTheta;
                var gradientPolar = Vector.CreateFromArray(new double[]
                {
                    0.5 / sqrtR * cosThetaHalf * sinTheta, sqrtR * (-0.5 * sinThetaHalf * sinTheta + cosThetaHalf * cosTheta)
                });

                Vector gradientGlobal = jacobians.TransformScalarFieldDerivativesLocalPolarToGlobalCartesian(gradientPolar);
                return new EvaluatedFunction(value, gradientGlobal.RawData);
            }

            public double EvaluateAt(XNode node)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(node);
                return Math.Sqrt(polarCoords[0]) * Math.Cos(polarCoords[1] / 2.0) * Math.Sin(polarCoords[1]);
            }

            public double EvaluateAt(XPoint point)
            {
                TipCoordinateSystem tipSystem = getTipSystem();
                double[] polarCoords = tipSystem.MapPointGlobalCartesianToLocalPolar(point);
                return Math.Sqrt(polarCoords[0]) * Math.Cos(polarCoords[1] / 2.0) * Math.Sin(polarCoords[1]);
            }

            public double GetJumpCoefficientBetween(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }

            public bool IsAppliedDueTo(PhaseBoundary phaseBoundary)
            {
                throw new NotImplementedException();
            }
        }
    }
}
