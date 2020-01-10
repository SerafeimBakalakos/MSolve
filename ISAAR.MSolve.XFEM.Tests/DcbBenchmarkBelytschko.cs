using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.Discretization.Mesh.Generation;
using ISAAR.MSolve.Discretization.Mesh.Generation.GMSH;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.Geometry.Shapes;
using ISAAR.MSolve.Logging.DomainDecomposition;
using ISAAR.MSolve.Problems;
using ISAAR.MSolve.Solvers;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.Feti1;
using ISAAR.MSolve.Solvers.DomainDecomposition.Dual.FetiDP;
using ISAAR.MSolve.Solvers.Tests.DomainDecomposition;
using ISAAR.MSolve.XFEM.Analyzers;
using ISAAR.MSolve.XFEM.CrackGeometry.CrackTip;
using ISAAR.MSolve.XFEM.CrackGeometry.HeavisideSingularityResolving;
using ISAAR.MSolve.XFEM.CrackGeometry.Implicit;
using ISAAR.MSolve.XFEM.CrackGeometry.Implicit.Logging;
using ISAAR.MSolve.XFEM.CrackPropagation;
using ISAAR.MSolve.XFEM.CrackPropagation.Direction;
using ISAAR.MSolve.XFEM.CrackPropagation.Jintegral;
using ISAAR.MSolve.XFEM.CrackPropagation.Length;
using ISAAR.MSolve.XFEM.Elements;
using ISAAR.MSolve.XFEM.Enrichments.Functions;
using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.Integration;
using ISAAR.MSolve.XFEM.Materials;
using ISAAR.MSolve.XFEM.Solvers;

namespace ISAAR.MSolve.XFEM.Tests
{
    public class DcbBenchmarkBelytschko //: IBenchmark
    {
        public enum PropagatorType
        {
            Standard, // Calculates the propagation data using J-integral
            FixedConstantLength, // Uses fixed propagation data obtained by an earlier analysis. Growth length = 0.3.
            FixedParisLaw // Uses fixed propagation data obtained by an earlier analysis. Growth length was determined by Paris law.
        }

        #region constants
        ///// <summary>
        ///// The material used for the J-integral computation. It msut be stored separately from individual element materials.
        ///// </summary>
        //private static readonly HomogeneousElasticMaterial2D globalHomogeneousMaterial =
        //    HomogeneousElasticMaterial2D.CreateMaterialForPlaneStrain(E, v);

        /// <summary>
        /// The maximum value that the effective SIF can reach before collapse occurs.
        /// </summary>
        private const double fractureToughness = double.MaxValue;

        public static readonly double h = 3.94, L = 3 * h;// in
        private static readonly double v = 0.3, E = 3e7; // psi=lbs/in^2
        private static readonly double load = 197; // lbs
        private static readonly double a = 3.95, da = 0.5; // in 
        private static readonly double dTheta = 5.71 * Math.PI / 180; // initial crack angle
        #endregion

        private readonly double heavisideTol;

        /// <summary>
        /// The length by which the crack grows in each iteration.
        /// </summary>
        private readonly double growthLength;

        /// <summary>
        /// Controls how large will the radius of the J-integral contour be. WARNING: errors are introduced if the J-integral 
        /// radius is larger than the length of the crack segments.
        /// </summary>
        private readonly double jIntegralRadiusOverElementSize;

        private readonly string lsmPlotDirectory;

        private readonly int numElementsY;
        private readonly int numSubdomainsX;
        private readonly int numSubdomainsY;

        /// <summary>
        /// 0 for the actual propagator, 1 for fixed propagator with constant growth length, 2 for fixed propagator with 
        /// Paris law growth length.
        /// </summary>
        private readonly PropagatorType propagatorType;
        private readonly double tipEnrichmentRadius = 0.0;

        private TrackingExteriorCrackLsm crack;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="growthLength">The length by which the crack grows in each iteration.</param>
        public DcbBenchmarkBelytschko(int numElementsY, int numSubdomainsX, int numSubdomainsY, double growthLength, 
            double tipEnrichmentRadius, double jIntegralRadiusOverElementSize, int maxIterations, double heavisideTol,
            string lsmPlotDirectory, PropagatorType propagatorType)
        {
            this.numElementsY = numElementsY;
            this.numSubdomainsX = numSubdomainsX;
            this.numSubdomainsY = numSubdomainsY;
            this.growthLength = growthLength;
            this.tipEnrichmentRadius = tipEnrichmentRadius;
            this.jIntegralRadiusOverElementSize = jIntegralRadiusOverElementSize;
            this.lsmPlotDirectory = lsmPlotDirectory;
            this.MaxIterations = maxIterations;
            this.heavisideTol = heavisideTol;
            this.propagatorType = propagatorType;
        }

        /// <summary>
        /// The crack geometry description. Before accessing it, make sure <see cref="InitializeModel"/> has been called.
        /// </summary>
        public TrackingExteriorCrackLsm Crack { get { return crack; } }

        public CartesianPoint CrackMouth { get; private set; }

        public double FractureToughness => fractureToughness;

        //public IReadOnlyList<double> GrowthAngles { get; private set; }
        /// <summary>
        /// The maximum number of crack propagation steps. The analysis may stop earlier if the crack has reached the domain 
        /// boundary or if the fracture toughness is exceeded.
        /// </summary>
        public int MaxIterations { get; }

        /// <summary>
        /// Before accessing it, make sure <see cref="InitializeModel"/> has been called.
        /// </summary>
        public XModel Model { get; private set; }

        public string Name { get { return "Dcb benchmark"; } }

        //public string PlotDirectory { get { return lsmPlotDirectory; } }

        public TipAdaptivePartitioner Partitioner { get; set; } // Refactor its injection
        
        public void CreateModel()
        {
            var builder = new Uniform2DXModelBuilder();
            builder.DomainLengthX = L;
            builder.DomainLengthY = h;
            builder.NumSubdomainsX = numSubdomainsX;
            builder.NumSubdomainsY = numSubdomainsY;
            builder.NumTotalElementsX = 3 * numElementsY;
            builder.NumTotalElementsY = numElementsY;
            builder.PlaneStress = false;
            builder.YoungModulus = E;
            builder.PoissonRatio = v;
            builder.PrescribeDisplacement(Uniform2DXModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationX, 0.0);
            builder.PrescribeDisplacement(Uniform2DXModelBuilder.BoundaryRegion.RightSide, StructuralDof.TranslationY, 0.0);
            builder.DistributeLoadAtNodes(Uniform2DXModelBuilder.BoundaryRegion.UpperLeftCorner, StructuralDof.TranslationY, load);
            builder.DistributeLoadAtNodes(Uniform2DXModelBuilder.BoundaryRegion.LowerLeftCorner, StructuralDof.TranslationY, -load);

            Model = builder.BuildModel();
        }

        public void InitializeCrack()
        {
            var globalHomogeneousMaterial = HomogeneousElasticMaterial2D.CreateMaterialForPlaneStrain(0, E, v);
            IPropagator propagator;
            if (propagatorType == PropagatorType.Standard)
            {
                propagator = new Propagator(Model.Mesh, jIntegralRadiusOverElementSize,
                    new HomogeneousMaterialAuxiliaryStates(globalHomogeneousMaterial),
                    new HomogeneousSIFCalculator(globalHomogeneousMaterial),
                    new MaximumCircumferentialTensileStressCriterion(), new ConstantIncrement2D(growthLength));
            }
            else if (propagatorType == PropagatorType.FixedConstantLength)
            {
                Debug.Assert(growthLength == 0.3);
                double[] growthAngles = 
                {
                    -0.0311190263005246,
                    -0.073585235948899,
                    -0.128214305959422,
                    -0.197123263064651,
                    -0.258889574558178,
                    -0.264957488864897,
                    -0.198769553144488,
                    -0.129884071347221
                };
                double[] growthLengths = 
                { 
                    0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3
                };
                double[] sifsMode1 = 
                {
                    1433.12359899268,
                    1511.69783315612,
                    1617.55828744903,
                    1781.52464076419,
                    2093.18047596673,
                    2729.70644290519,
                    3989.86723051068,
                    6392.64356131652
                };
                double[] sifsMode2 =
                {
                    22.3113099637035,
                    55.7956288493048,
                    104.702305206624,
                    179.674365148896,
                    282.032913414359,
                    377.153649617101,
                    405.914609568657,
                    419.282490062095
                };
                propagator = new FixedPropagator(growthAngles, growthLengths, sifsMode1, sifsMode2);
            }
            else if (propagatorType == PropagatorType.FixedParisLaw)
            {
                throw new NotImplementedException();
            }
            else throw new ArgumentException();


            CrackMouth = new CartesianPoint(0.0, h/2);
            var crackKink = new CartesianPoint(a, h / 2);
            var initialCrack = new PolyLine2D(CrackMouth, crackKink);
            initialCrack.UpdateGeometry(-dTheta, da);
            //var crackTip = new CartesianPoint(a + da * Math.Cos(dTheta), h/2 - da * Math.Sin(dTheta));

            var lsmCrack = new TrackingExteriorCrackLsm(propagator, tipEnrichmentRadius, new RelativeAreaResolver(heavisideTol), 
                new SignFunction2D());
            lsmCrack.Mesh = Model.Mesh;

            // Logging         
            if (lsmPlotDirectory != null)
            {
                lsmCrack.EnrichmentLogger = new EnrichmentLogger(Model, lsmCrack, lsmPlotDirectory);
                lsmCrack.LevelSetLogger = new LevelSetLogger(Model, lsmCrack, lsmPlotDirectory);
                lsmCrack.LevelSetComparer = new PreviousLevelSetComparer(lsmCrack, lsmPlotDirectory);
            }

            // Mesh geometry interaction
            lsmCrack.InitializeGeometry(initialCrack);
            //lsmCrack.UpdateGeometry(-dTheta, da);
            this.crack = lsmCrack;
        }

        public void InitializeModel()
        {
            Model = new XModel();
            CreateModel();
            InitializeCrack();
        }

        public bool NodeIsOnBoundary(INode node)
        {
            double dx = L / numElementsY;
            double dy = h / numElementsY;
            double meshTolerance = 1E-10 * Math.Min(dx, dy);
            if (Math.Abs(node.X) <= meshTolerance) return true;
            if (Math.Abs(node.X - L) <= meshTolerance) return true;
            if (Math.Abs(node.Y) <= meshTolerance) return true;
            if (Math.Abs(node.Y - h) <= meshTolerance) return true;
            return false;
        }

        public void PrintPropagationLogger()
        {
            PropagationLogger logger = Crack.CrackTipPropagators.Values.First().Logger;
            for (int i = 0; i < logger.GrowthAngles.Count; ++i)
            {
                logger.PrintAnalysisStep(i);
            }
        }

        public class Builder //: IBenchmarkBuilder
        {
            private readonly int numElementsY;
            private readonly int numSubdomainsX;
            private readonly int numSubdomainsY;

            public Builder(int numElementsY, int numSubdomainsX, int numSubdomainsY)
            {
                this.numElementsY = numElementsY;
                this.numSubdomainsX = numSubdomainsX;
                this.numSubdomainsY = numSubdomainsY;
            }

            /// <summary>
            /// A node that lies in the positive halfplane defined by the body level set of a crack, will be enriched with 
            /// heaviside enrichment if Apos/(Apos+Aneg) &gt; <see cref="HeavisideEnrichmentTolerance"/> where Apos, Aneg 
            /// are the subareas of its nodal support  in the positive and negative halfplanes respectively. Similarly a
            /// node in the negative halfplane will be enriched if Aneg/(Apos+Aneg) &gt; 
            /// <see cref="HeavisideEnrichmentTolerance"/>.
            /// </summary>
            public double HeavisideEnrichmentTolerance { get; set; } = 0.0001;

            /// <summary>
            /// Must be sufficiently larger than the element size.
            /// </summary>
            public double GrowthLength { get; set; } = 0.3; //in

            /// <summary>
            /// Controls how large will the radius of the J-integral contour be. WARNING: errors are introduced if the J-integral 
            /// radius is larger than the length of the crack segments.
            /// </summary>
            public double JintegralRadiusOverElementSize { get; set; } = 2.0;

            /// <summary>
            /// The absolute path of the directory where output vtk files with the crack path and the level set functions at 
            /// each iteration will be written. Leave it null to avoid the performance cost it will introduce.
            /// </summary>
            public string LsmPlotDirectory { get; set; } = null;


            /// <summary>
            /// The maximum number of crack propagation steps. The analysis may stop earlier if the crack has reached the domain 
            /// boundary or if the fracture toughness is exceeded.
            /// </summary>
            public int MaxIterations { get; set; } = 8; //TODO: After that I noticed very weird behaviour

            public double TipEnrichmentRadius { get; set; } = 0.0;

            /// <summary>
            /// 0 for the actual propagator, 1 for fixed propagator with constant growth length, 2 for fixed propagator with 
            /// Paris law growth length.
            /// </summary>
            public PropagatorType PropagatorType { get; set; } = PropagatorType.Standard;

            public DcbBenchmarkBelytschko BuildBenchmark()
            {
                return new DcbBenchmarkBelytschko(numElementsY, numSubdomainsX, numSubdomainsY, GrowthLength, TipEnrichmentRadius, 
                    JintegralRadiusOverElementSize, MaxIterations, HeavisideEnrichmentTolerance, LsmPlotDirectory, 
                    PropagatorType);
            }
        }
    }
}
