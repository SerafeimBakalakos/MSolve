using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.FEM.Interpolation.GaussPointExtrapolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.XFEM.Multiphase.Enrichment;
using ISAAR.MSolve.XFEM.Multiphase.Entities;
using ISAAR.MSolve.XFEM.Multiphase.Geometry;
using ISAAR.MSolve.XFEM.Multiphase.Integration;
using ISAAR.MSolve.XFEM.Multiphase.Materials;

//TODO: Bstd or Benr assume different order of the shape function gradient. Which is the correct one?
namespace ISAAR.MSolve.XFEM.Multiphase.Elements
{
    public class XThermalElement2D : IXFiniteElement
    {
        private readonly int id;
        private readonly int numStandardDofs;
        private readonly IDofType[][] standardDofTypes;

        private IDofType[][] allDofTypes;

        private Dictionary<PhaseBoundary, IReadOnlyList<GaussPoint>> gaussPointsBoundary;
        private IReadOnlyList<GaussPoint> gaussPointsVolume;

        //TODO: this can be cached once for all standard elements of the same type
        private EvalInterpolation2D[] evalInterpolationsAtGPsVolume;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsBoundary"/>.
        /// </summary>
        private Dictionary<PhaseBoundary, ThermalInterfaceMaterial[]> materialsAtGPsBoundary;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsVolume"/>.
        /// </summary>
        private ThermalMaterial[] materialsAtGPsVolume;

        private int numEnrichedDofs;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsVolume"/>.
        /// </summary>
        private IPhase[] phasesAtGPsVolume;

        public XThermalElement2D(int id, IReadOnlyList<XNode> nodes, double thickness, IThermalMaterialField materialField,
            IIsoparametricInterpolation2D interpolation, IGaussPointExtrapolation2D gaussPointExtrapolation,
            IQuadrature2D standardQuadrature, IIntegrationStrategy volumeIntegration, IBoundaryIntegration boundaryIntegration)
        {
            this.id = id;
            this.Thickness = thickness;
            this.Nodes = nodes;
            this.InterpolationStandard = interpolation;
            this.GaussPointExtrapolation = gaussPointExtrapolation;
            this.IntegrationStandard = standardQuadrature;
            this.IntegrationVolume = volumeIntegration;
            this.IntegrationBoundary = boundaryIntegration;
            this.MaterialField = materialField;

            this.numStandardDofs = nodes.Count;
            this.standardDofTypes = new IDofType[nodes.Count][];
            for (int i = 0; i < nodes.Count; ++i) this.standardDofTypes[i] = new IDofType[] { ThermalDof.Temperature };
        }

        public CellType CellType => InterpolationStandard.CellType;
        public IElementDofEnumerator DofEnumerator { get; set; } = new GenericDofEnumerator();

        public IElementType ElementType => this;

        public IReadOnlyList<(XNode node1, XNode node2)> EdgeNodes
        {
            get
            {
                if (Nodes.Count > 4) throw new NotImplementedException();
                else
                {
                    var edges = new (XNode node1, XNode node2)[Nodes.Count];
                    for (int i = 0; i < Nodes.Count; ++i)
                    {
                        XNode node1 = Nodes[i];
                        XNode node2 = Nodes[(i + 1) % Nodes.Count];
                        edges[i] = (node1, node2);
                    }
                    return edges;
                }
            }
        }

        public IReadOnlyList<(NaturalPoint node1, NaturalPoint node2)> EdgesNodesNatural
        {
            get
            {
                if (Nodes.Count > 4) throw new NotImplementedException();
                else
                {
                    var edges = new (NaturalPoint node1, NaturalPoint node2)[Nodes.Count];
                    for (int i = 0; i < Nodes.Count; ++i)
                    {
                        NaturalPoint node1 = InterpolationStandard.NodalNaturalCoordinates[i];
                        NaturalPoint node2 = InterpolationStandard.NodalNaturalCoordinates[(i + 1) % Nodes.Count];
                        edges[i] = (node1, node2);
                    }
                    return edges;
                }
            }
        }

        public IGaussPointExtrapolation2D GaussPointExtrapolation { get; }

        public int ID { get => id; set => throw new InvalidOperationException("ID is set at constructor."); }

        public IBoundaryIntegration IntegrationBoundary { get; set; }

        //TODO: This should not always be used for Kss. E.g. it doesn't work for bimaterial interface.
        public IQuadrature2D IntegrationStandard { get; }

        public IIntegrationStrategy IntegrationVolume { get; set; }

        /// <summary>
        /// Common interpolation for standard and enriched nodes.
        /// </summary>
        public IIsoparametricInterpolation2D InterpolationStandard { get; }

        public IThermalMaterialField MaterialField { get; }

        IReadOnlyList<INode> IElement.Nodes => Nodes;
        /// <summary>
        /// All nodes are enriched for now.
        /// </summary>
        public IReadOnlyList<XNode> Nodes { get; }

        public Dictionary<PhaseBoundary, CurveElementIntersection> PhaseIntersections { get; }
            = new Dictionary<PhaseBoundary, CurveElementIntersection>();

        public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        ISubdomain IElement.Subdomain => this.Subdomain;
        public XSubdomain Subdomain { get; set; }

        public double Thickness { get; }

        //TODO: delete
        //private bool IsStandardElement
        //{
        //    get
        //    {
        //        return NumEnrichedDofs == 0;

        //        //Debug.Assert(Phases.Count >= 1);
        //        //return Phases.Count == 1.0;

        //        //foreach (XNode node in Nodes)
        //        //{
        //        //    if (node.Enrichments.Count != 0) return false;
        //        //}
        //        //return true;
        //    }
        //}

        public IMatrix DampingMatrix(IElement element) => throw new NotImplementedException();

        public IReadOnlyList<IReadOnlyList<IDofType>> GetElementDofTypes(IElement element) => allDofTypes;


        public Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)> 
            GetMaterialsForBoundaryIntegration()
        {
            var result = new Dictionary<PhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalInterfaceMaterial>)>();
            foreach (PhaseBoundary boundary in gaussPointsBoundary.Keys)
            {
                result[boundary] = (gaussPointsBoundary[boundary], materialsAtGPsBoundary[boundary]);
            }
            return result;
        }

        public (IReadOnlyList<GaussPoint>, IReadOnlyList<ThermalMaterial>) GetMaterialsForVolumeIntegration()
            => (gaussPointsVolume, materialsAtGPsVolume);

        public void IdentifyDofs()
        {
            this.numEnrichedDofs = 0;
            foreach (XNode node in Nodes) this.numEnrichedDofs += node.EnrichedDofsCount;

            if (this.numEnrichedDofs == 0) allDofTypes = standardDofTypes;
            else
            {
                // The dof order in increasing frequency of change is: node, enrichment item, enrichment function, axis.
                // A similar convention should also hold for each enrichment item: enrichment function major, axis minor.
                // WARNING: The order here must match the order in JoinStiffnessesNodeMajor().
                this.allDofTypes = new IDofType[Nodes.Count][];
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    XNode node = Nodes[i];
                    var nodalDofs = new IDofType[1 + node.EnrichedDofsCount];
                    nodalDofs[0] = ThermalDof.Temperature;
                    int j = 1;
                    foreach (IEnrichment enrichment in node.Enrichments.Keys)
                    {
                        nodalDofs[j++] = enrichment.Dof;
                    }
                    this.allDofTypes[i] = nodalDofs;
                }
            }
        }

        public void IdentifyIntegrationPointsAndMaterials()
        {
            // Volume integration
            this.gaussPointsVolume = IntegrationVolume.GenerateIntegrationPoints(this);
            int numPointsVolume = gaussPointsVolume.Count;
            
            // Calculate and cache standard interpolation at integration points.
            //TODO: for all standard elements of the same type, this should be cached only once
            this.evalInterpolationsAtGPsVolume = new EvalInterpolation2D[numPointsVolume];
            for (int i = 0; i < numPointsVolume; ++i)
            {
                evalInterpolationsAtGPsVolume[i] = InterpolationStandard.EvaluateAllAt(Nodes, gaussPointsVolume[i]);
            }

            // Find and cache the phase at integration points.
            this.phasesAtGPsVolume = new IPhase[numPointsVolume];
            Debug.Assert(Phases.Count != 0);
            if (Phases.Count == 1)
            {
                IPhase commonPhase = Phases.First();
                for (int i = 0; i < numPointsVolume; ++i) this.phasesAtGPsVolume[i] = commonPhase;
            }
            else
            {
                for (int i = 0; i < numPointsVolume; ++i)
                {
                    CartesianPoint point = evalInterpolationsAtGPsVolume[i].TransformPointNaturalToGlobalCartesian();
                    IPhase phase = GeometricModel.FindPhaseAt(point, this);
                    this.phasesAtGPsVolume[i] = phase;
                }
            }

            // Create and cache materials at integration points.
            this.materialsAtGPsVolume = new ThermalMaterial[numPointsVolume];
            for (int i = 0; i < numPointsVolume; ++i)
            {
                this.materialsAtGPsVolume[i] = MaterialField.FindMaterialAt(this.phasesAtGPsVolume[i]);
            }

            // Create and cache materials at boundary integration points.
            this.gaussPointsBoundary = new Dictionary<PhaseBoundary, IReadOnlyList<GaussPoint>>();
            this.materialsAtGPsBoundary = new Dictionary<PhaseBoundary, ThermalInterfaceMaterial[]>();
            foreach (var boundaryIntersectionPair in PhaseIntersections)
            {
                PhaseBoundary boundary = boundaryIntersectionPair.Key;
                CurveElementIntersection intersection = boundaryIntersectionPair.Value;

                IReadOnlyList<GaussPoint> gaussPoints = IntegrationBoundary.GenerateIntegrationPoints(this, intersection);
                int numGaussPoints = gaussPoints.Count;
                var materials = new ThermalInterfaceMaterial[numGaussPoints];

                //TODO: perhaps I should have one for each Gauss point
                ThermalInterfaceMaterial material = MaterialField.FindInterfaceMaterialAt(boundary);
                for (int i = 0; i < numGaussPoints; ++i) materials[i] = material;

                gaussPointsBoundary[boundary] = gaussPoints;
                materialsAtGPsBoundary[boundary] = materials;
            }
        }

        public IMatrix MassMatrix(IElement element) => throw new NotImplementedException();

        public IMatrix StiffnessMatrix(IElement element)
        {
            Matrix Kss = BuildConductivityMatrixStandard();
            IMatrix Ktotal;
            if (numEnrichedDofs == 0) Ktotal = Kss;
            else
            {
                (Matrix Kee, Matrix Kse) = BuildConductivityMatricesEnriched();
                #region debug
                //if (PhaseIntersections.Count > 0)
                //{
                //    Matrix Kii = BuildConductivityMatrixBoundary();
                //    Kee.AddIntoThis(Kii);
                //}
                #endregion
                Ktotal = JoinStiffnesses(Kss, Kee, Kse);
            }
            #region debug
            //string path = @"C:\Users\Serafeim\Desktop\HEAT\debug\" + $"K{element.ID}.txt";
            //var writer = new LinearAlgebra.Output.FullMatrixWriter();
            //writer.WriteToFile(Ktotal, path);
            #endregion
            return Ktotal;
        }

        private (Matrix Kee, Matrix Kse) BuildConductivityMatricesEnriched()
        {
            var Kse = Matrix.CreateZero(numStandardDofs, numEnrichedDofs);
            var Kee = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);
            for (int i = 0; i < gaussPointsVolume.Count; ++i)
            {
                GaussPoint gaussPoint = gaussPointsVolume[i];
                EvalInterpolation2D evalInterpolation = evalInterpolationsAtGPsVolume[i];
                double dV = evalInterpolation.Jacobian.DirectDeterminant * Thickness;

                // Material properties
                double conductivity = materialsAtGPsVolume[i].ThermalConductivity;

                // Deformation matrices: Bs = grad(Ns), Be = grad(Ne)
                Matrix Bstd = CalcDeformationMatrixStandard(evalInterpolation);
                IPhase phase = phasesAtGPsVolume[i];
                Matrix Benr = CalculateDeformationMatrixEnriched(numEnrichedDofs, phase, evalInterpolation);

                // Contribution of this gauss point to the element stiffness matrices: 
                // Kee = SUM(Benr^T * c * Benr  *  dV*w), Kse = SUM(Bstd^T * c * Benr  *  dV*w)
                Matrix cBe = conductivity * Benr; // cache the result
                Matrix BeCBe = Benr.MultiplyRight(cBe, true, false);  // enriched-enriched part
                Kee.AxpyIntoThis(BeCBe, dV * gaussPoint.Weight);
                Matrix BsCBe = Bstd.MultiplyRight(cBe, true, false);  // enriched-standard part
                Kse.AxpyIntoThis(BsCBe, dV * gaussPoint.Weight);
            }
            return (Kee, Kse);
        }

        private Matrix BuildConductivityMatrixBoundary()
        {
            var Kii = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);
            foreach (var boundaryGaussPointsPair in gaussPointsBoundary)
            {
                PhaseBoundary boundary = boundaryGaussPointsPair.Key;
                IReadOnlyList<GaussPoint> gaussPoints = boundaryGaussPointsPair.Value;
                ThermalInterfaceMaterial[] materials = materialsAtGPsBoundary[boundary];

                // Kii = sum(conductivity * jumpCoeff^2 * N^T * N * weight * thickness)
                double phaseJumpCoeff = boundary.Enrichment.PhaseJumpCoefficient;
                double commonCoeff = phaseJumpCoeff * phaseJumpCoeff * Thickness;
                for (int i = 0; i < gaussPoints.Count; ++i)
                {
                    GaussPoint gaussPoint = gaussPoints[i];
                    double interfaceConductivity = materials[i].InterfaceConductivity;
                    double scale = commonCoeff * interfaceConductivity * gaussPoint.Weight;

                    Vector N = CalculateEnrichedShapeFunctionVector(gaussPoint, boundary);
                    Matrix NtN = N.TensorProduct(N);
                    Kii.AxpyIntoThis(NtN, scale);
                }
            }
            return Kii;
        }

        private Matrix BuildConductivityMatrixStandard()
        {
            // If the element is has more than 1 phase, then I cannot use the standard quadrature, since the material is  
            // different on each phase.
            var Kss = Matrix.CreateZero(numStandardDofs, numStandardDofs);
            for (int i = 0; i < gaussPointsVolume.Count; ++i)
            {
                GaussPoint gaussPoint = gaussPointsVolume[i];
                EvalInterpolation2D evalInterpolation = evalInterpolationsAtGPsVolume[i];
                double dV = evalInterpolation.Jacobian.DirectDeterminant * Thickness;
                //TODO: The thickness is constant per element in FEM, but what about XFEM? Different materials within the same 
                //      element are possible. Yeah but the thickness is a geometric porperty, rather than a material one.

                // Material properties
                double conductivity = materialsAtGPsVolume[i].ThermalConductivity;

                // Deformation matrix:  Bs = grad(Ns)
                Matrix deformation = CalcDeformationMatrixStandard(evalInterpolation);

                // Contribution of this gauss point to the element stiffness matrix: Kss = sum(Bs^T * c * Bs  *  dV*w)
                Matrix partial = deformation.MultiplyRight(deformation, true);
                Kss.AxpyIntoThis(partial, conductivity * dV * gaussPoint.Weight);
            }
            return Kss;
        }

        //TODO: delete
        //private IMatrix JoinStiffnessesNodeMajor(Func<IReadOnlyList<GaussPoint>, Matrix> buildKss,
        //    Func<(Matrix Kee, Matrix Kse)> buildKeeKse)
        //{
        //    //TODO: Perhaps it is more efficient to do this by just appending Kse and Kee to Kss.
        //    if (numEnrichedDofs == 0) return buildKss(IntegrationStandard.IntegrationPoints);
        //    else
        //    {
        //        // The dof order in increasing frequency of change is: node, enrichment item, enrichment function, axis.
        //        // WARNING: The order here must match the order in OrderDofsNodeMajor() and BuildEnrichedStiffnessMatricesUpper()

        //        // Find the mapping from Kss, Kse, Kee to a total matrix for the element. TODO: This could be a different method.
        //        var stdDofIndices = new int[numStandardDofs];
        //        var enrDofIndices = new int[numEnrichedDofs];
        //        int enrDofCounter = 0, totDofCounter = 0;
        //        for (int n = 0; n < Nodes.Count; ++n)
        //        {
        //            // Std dofs
        //            stdDofIndices[n] = totDofCounter;           // std X
        //            totDofCounter += 1;

        //            // Enr dofs
        //            for (int e = 0; e < Nodes[n].EnrichedDofsCount; ++e)
        //            {
        //                enrDofIndices[enrDofCounter++] = totDofCounter++;
        //            }
        //        }

        //        // Copy the entries of Kss, Kse, Kee to the upper triangle of a total matrix for the element.
        //        Matrix Kss = buildKss(IntegrationStrategy.GenerateIntegrationPoints(this));
        //        (Matrix Kee, Matrix Kse) = buildKeeKse();
        //        var Ktotal = SymmetricMatrix.CreateZero(numStandardDofs + numEnrichedDofs);

        //        // Upper triangle of Kss
        //        for (int stdCol = 0; stdCol < numStandardDofs; ++stdCol)
        //        {
        //            int totColIdx = stdDofIndices[stdCol];
        //            for (int stdRow = 0; stdRow <= stdCol; ++stdRow)
        //            {
        //                Ktotal[stdDofIndices[stdRow], totColIdx] = Kss[stdRow, stdCol];
        //            }
        //        }

        //        for (int enrCol = 0; enrCol < numEnrichedDofs; ++enrCol)
        //        {
        //            int totColIdx = enrDofIndices[enrCol];

        //            // Whole Kse
        //            for (int stdRow = 0; stdRow < numStandardDofs; ++stdRow)
        //            {
        //                Ktotal[stdDofIndices[stdRow], totColIdx] = Kse[stdRow, enrCol];
        //            }

        //            // Upper triangle of Kee
        //            for (int enrRow = 0; enrRow <= enrCol; ++enrRow)
        //            {
        //                Ktotal[enrDofIndices[enrRow], totColIdx] = Kee[enrRow, enrCol];
        //            }
        //        }

        //        return Ktotal;
        //    }
        //}

        private Matrix CalculateDeformationMatrixEnriched(int numEnrichedDofs, IPhase phaseAtGaussPoint, 
            EvalInterpolation2D evaluatedInterpolation)
        {
            // For each node and with all derivatives w.r.t. cartesian coordinates, the enrichment derivatives 
            // are: Bx = enrN,x = N,x(x,y) * [psi(x,y) - psi(node)] + N(x,y) * psi,x(x,y), where psi is the  
            // enrichment function. However in this formulation of multiphase XFEM, only piecewise constant enrichments
            // are used. Therefore always psi,x = 0.


            //CartesianPoint cartesianPoint = evaluatedInterpolation.TransformPointNaturalToGlobalCartesian(gaussPoint);
            var uniqueEnrichments = new Dictionary<IEnrichment, double>();

            var deformationMatrix = Matrix.CreateZero(2, numEnrichedDofs);
            int currentColumn = 0;
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double dNdx = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 1];

                foreach (var enrichmentValuePair in Nodes[nodeIdx].Enrichments)
                {
                    IEnrichment enrichment = enrichmentValuePair.Key;
                    #region debug
                    //Debug.Assert((enrichment is StepEnrichment) || (enrichment is JunctionEnrichment), 
                    //    "Otherwise the derivative calculation is wrong");
                    #endregion

                    double nodalPsi = enrichmentValuePair.Value;

                    // The enrichment function probably has been evaluated when processing a previous node. Avoid reevaluation.
                    double psi;
                    if (!(uniqueEnrichments.TryGetValue(enrichment, out psi)))
                    {
                        psi = enrichment.EvaluateAt(phaseAtGaussPoint);
                        uniqueEnrichments[enrichment] = psi;
                    }

                    // Bx = enrN,x = N,x(x, y) * [psi(x, y) - psi(node)]
                    // By = enrN,y = N,y(x, y) * [psi(x, y) - psi(node)]
                    double dPsi = psi - nodalPsi;
                    double Bx = dNdx * dPsi;
                    double By = dNdy * dPsi;

                    // This depends on the convention: node major or enrichment major. 
                    // The following is node major, since this convention is used throughout MSolve.
                    int col = currentColumn++;
                    deformationMatrix[0, col] = Bx;
                    deformationMatrix[1, col] = By;
                }
            }
            Debug.Assert(currentColumn == numEnrichedDofs);
            return deformationMatrix;
        }

        private Matrix CalcDeformationMatrixStandard(EvalInterpolation2D evalInterpolation)
        {
            // gradT = [ T,x ] = [ sum(Ni,x) * Ti ] = [ ... Ni,x ... ] * [ ... ]
            //         [ T,y ]   [ sum(Ni,y) * Ti ]   [ ... Ni,y ... ]   [  Ti ]
            //                                                           [ ... ]

            // The ones stored are [ N1,x N2,x N3,x ... ]. Therefore they need transposing
            //                     [ N1,y N2,y N3,y ... ]
            return evalInterpolation.ShapeGradientsCartesian.Transpose();
        }

        /// <summary>
        /// The contour integral along a phase boundary is calculated for the enriched dofs that were applied due to that 
        /// boundary. For example, if there are 2 boundaries and all 3 nodes of the element are enriched due to them, then the 6
        /// enriched dofs are [node1Boundary1, node1Boundary2, node2Boundary1, node2Boundary2, node3Boundary1, node3Boundary2].
        /// When integrating along boundary 1 we will compute N^T*N, where N(6x1) = [N1 0 N2 0 N3 0]. If we integrate along
        /// boundary 2, then N(6x1) = [0 N1 0 N2 0 N3]. 
        /// 
        /// Therefore when integrating along a specific boundary, then for every enriched dof of each node i, we need to find 
        /// if the enrichment was applied due to that boundary. If yes, the corresponding index of the total shape function 
        /// array gets the value Ni. Otherwise it remains 0. 
        /// 
        /// The whole thing also takes care of a) blending enrichments due to boundaries in other elements, 
        /// b) rare cases where one or more nodes were not enriched like the rest, because their nodal support was almost 
        /// entirely in one of the two regions.
        /// </summary>
        private Vector CalculateEnrichedShapeFunctionVector(NaturalPoint gaussPoint, PhaseBoundary boundary)
        {
            //TODO: Optimize this: The mapping should be done once per enrichment ane reused for all Gauss points.
            //      See an attempt at MapEnrichedDofIndicesToNodeIndices().

            Vector totalShapeFunctions = Vector.CreateZero(numEnrichedDofs);
            double[] N = InterpolationStandard.EvaluateFunctionsAt(gaussPoint);
            int idx = 0;
            for (int n = 0; n < Nodes.Count; ++n)
            {
                XNode node = Nodes[n];
                //TODO: VERY FRAGILE CODE. This order of enrichments was used to determine the order of enriched dofs in 
                //      another method. It works as of the time of writing, but this dependency must be removed. Perhaps use a 
                //      DofTable.
                foreach (IEnrichment enrichment in node.Enrichments.Keys) 
                {
                    if (enrichment.IsAppliedDueTo(boundary)) totalShapeFunctions[idx] = N[n];
                    ++idx; // always move to the next index in the total shape function array
                }
            }
            return totalShapeFunctions;
        }

        /// <summary>
        /// Copy the entries of Kss, Kse, Kee to the upper triangle of a total matrix for the element.
        /// </summary>
        private IMatrix JoinStiffnesses(Matrix Kss, Matrix Kee, Matrix Kse)
        {
            // Find the mapping from Kss, Kse, Kee to a total matrix for the element.
            (int[] stdDofIndices, int[] enrDofIndices) = MapDofsFromStdEnrToNodeMajor();
            var Ktotal = SymmetricMatrix.CreateZero(numStandardDofs + numEnrichedDofs);

            // Upper triangle of Kss
            for (int stdCol = 0; stdCol < numStandardDofs; ++stdCol)
            {
                int totalCol = stdDofIndices[stdCol];
                for (int stdRow = 0; stdRow <= stdCol; ++stdRow)
                {
                    Ktotal[stdDofIndices[stdRow], totalCol] = Kss[stdRow, stdCol];
                }
            }

            for (int enrCol = 0; enrCol < numEnrichedDofs; ++enrCol)
            {
                int totalCol = enrDofIndices[enrCol];

                // Whole Kse
                for (int stdRow = 0; stdRow < numStandardDofs; ++stdRow)
                {
                    Ktotal[stdDofIndices[stdRow], totalCol] = Kse[stdRow, enrCol];
                }

                // Upper triangle of Kee
                for (int enrRow = 0; enrRow <= enrCol; ++enrRow)
                {
                    Ktotal[enrDofIndices[enrRow], totalCol] = Kee[enrRow, enrCol];
                }
            }

            return Ktotal;
        }

        private (int[] stdDofIndices, int[] enrDofIndices) MapDofsFromStdEnrToNodeMajor()
        {
            // WARNING: The order here must match the order assumed in other methods of this class
            
            // The dof order in increasing frequency of change is: node, enrichment item, enrichment function, axis.
            var stdDofIndices = new int[numStandardDofs];
            var enrDofIndices = new int[numEnrichedDofs];
            int enrDofCounter = 0, totalDofCounter = 0;
            for (int n = 0; n < Nodes.Count; ++n)
            {
                // Std dofs
                stdDofIndices[n] = totalDofCounter;
                totalDofCounter += 1;

                // Enr dofs
                for (int e = 0; e < Nodes[n].EnrichedDofsCount; ++e)
                {
                    enrDofIndices[enrDofCounter++] = totalDofCounter++;
                }
            }
            return (stdDofIndices, enrDofIndices);
        }

        private int[] MapEnrichedDofIndicesToNodeIndices(PhaseBoundary boundary)
        {
            var enrichedDofIndicesToNodeIndices = new int[numEnrichedDofs];
            int idx = 0;
            for (int n = 0; n < Nodes.Count; ++n)
            {
                XNode node = Nodes[n];
                //TODO: VERY FRAGILE CODE. This order of enrichments was used to determine the order of enriched dofs in 
                //      another method. It works as of the time of writing, but this dependency must be removed. Perhaps use a 
                //      DofTable.
                foreach (IEnrichment enrichment in node.Enrichments.Keys)
                {
                    if (enrichment.IsAppliedDueTo(boundary)) enrichedDofIndicesToNodeIndices[idx] = n;
                    else enrichedDofIndicesToNodeIndices[idx] = -1;
                    ++idx; // always move to the next index in the total shape function array
                }
            }
            return enrichedDofIndicesToNodeIndices;
        }

    }
}