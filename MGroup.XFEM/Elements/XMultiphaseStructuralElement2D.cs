﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.Materials.Interfaces;
using MGroup.XFEM.ElementGeometry;
using MGroup.XFEM.Enrichment;
using MGroup.XFEM.Entities;
using MGroup.XFEM.Geometry;
using MGroup.XFEM.Geometry.ConformingMesh;
using MGroup.XFEM.Geometry.Primitives;
using MGroup.XFEM.Integration;
using MGroup.XFEM.Integration.Quadratures;
using MGroup.XFEM.Interpolation;
using MGroup.XFEM.Interpolation.GaussPointExtrapolation;
using MGroup.XFEM.Materials;
using MGroup.XFEM.Phases;

//TODO: A lot of duplication between this and thermal elements.
namespace MGroup.XFEM.Elements
{
    public class XMultiphaseStructuralElement2D : IXStructuralMultiphaseElement
    {
        private const int dim = 2;

        private readonly int boundaryIntegrationOrder;
        private readonly bool cohesiveInterfaces;
        private readonly IElementGeometry elementGeometry;
        private readonly int id;
        private readonly int numStandardDofs;
        private readonly IDofType[][] standardDofTypes;

        private IDofType[][] allDofTypes;

        private Dictionary<IPhaseBoundary, IReadOnlyList<GaussPoint>> gaussPointsBoundary;
        private Dictionary<IPhaseBoundary, IReadOnlyList<double[]>> gaussPointsBoundaryNormals;
        private IReadOnlyList<GaussPoint> gaussPointsBulk;

        //TODO: this can be cached once for all standard elements of the same type
        private EvalInterpolation[] evalInterpolationsAtGPsBulk;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsBoundary"/>.
        /// </summary>
        private Dictionary<IPhaseBoundary, CohesiveInterfaceMaterial[]> materialsAtGPsBoundary;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsBulk"/>.
        /// </summary>
        private IContinuumMaterial[] materialsAtGPsBulk;

        private int numEnrichedDofs;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsBulk"/>.
        /// </summary>
        private IPhase[] phasesAtGPsVolume;

        public XMultiphaseStructuralElement2D(int id, IReadOnlyList<XNode> nodes, double thickness, IElementGeometry elementGeometry,
            IStructuralMaterialField materialField, IIsoparametricInterpolation interpolation,
            IGaussPointExtrapolation gaussPointExtrapolation, IQuadrature standardQuadrature,
            IBulkIntegration bulkIntegration, int boundaryIntegrationOrder, bool cohesiveInterfaces)
        {
            this.id = id;
            this.Nodes = nodes;
            this.Thickness = thickness;
            this.elementGeometry = elementGeometry;

            this.Interpolation = interpolation;
            this.GaussPointExtrapolation = gaussPointExtrapolation;
            this.IntegrationStandard = standardQuadrature;
            this.IntegrationBulk = bulkIntegration;
            this.boundaryIntegrationOrder = boundaryIntegrationOrder;
            this.cohesiveInterfaces = cohesiveInterfaces;
            this.MaterialField = materialField;

            this.numStandardDofs = dim * nodes.Count;
            this.standardDofTypes = new IDofType[nodes.Count][];
            for (int i = 0; i < nodes.Count; ++i)
            {
                this.standardDofTypes[i] = new IDofType[] { StructuralDof.TranslationX, StructuralDof.TranslationY };
            }

            int[] nodeIDs = nodes.Select(n => n.ID).ToArray();
            (this.Edges, this.Faces) = elementGeometry.FindEdgesFaces(nodeIDs);
        }

        public IReadOnlyList<GaussPoint> BulkIntegrationPoints => gaussPointsBulk;

        public IReadOnlyList<GaussPoint> BoundaryIntegrationPoints
        {
            get
            {
                var allBoundaryPoints = new List<GaussPoint>();
                foreach (var points in gaussPointsBoundary.Values)
                {
                    allBoundaryPoints.AddRange(points);
                }
                return allBoundaryPoints;
            }
        }

        public IReadOnlyList<double[]> BoundaryIntegrationPointNormals
        {
            get
            {
                var allNormals = new List<double[]>();
                foreach (var normals in gaussPointsBoundaryNormals.Values)
                {
                    allNormals.AddRange(normals);
                }
                return allNormals;
            }
        }

        public CellType CellType => Interpolation.CellType;

        public IElementSubcell[] ConformingSubcells { get; set; }

        public IElementDofEnumerator DofEnumerator { get; set; } = new GenericDofEnumerator();

        public IElementType ElementType => this;

        public ElementEdge[] Edges { get; }

        public ElementFace[] Faces { get; }

        public IGaussPointExtrapolation GaussPointExtrapolation { get; }

        public int ID { get => id; set => throw new InvalidOperationException("ID is set at constructor."); }

        //TODO: This should not always be used for Kss. E.g. it doesn't work for bimaterial interface.
        public IQuadrature IntegrationStandard { get; }

        public IBulkIntegration IntegrationBulk { get; set; }

        /// <summary>
        /// Common interpolation for standard and enriched nodes.
        /// </summary>
        public IIsoparametricInterpolation Interpolation { get; }

        public IStructuralMaterialField MaterialField { get; }

        IReadOnlyList<INode> IElement.Nodes => Nodes;
        /// <summary>
        /// All nodes are enriched for now.
        /// </summary>
        public IReadOnlyList<XNode> Nodes { get; }

        public Dictionary<int, IElementDiscontinuityInteraction> InteractingDiscontinuities { get; }
            = new Dictionary<int, IElementDiscontinuityInteraction>();

        public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        public Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction> PhaseIntersections { get; }
            = new Dictionary<IPhaseBoundary, IElementDiscontinuityInteraction>();

        ISubdomain IElement.Subdomain => this.Subdomain;
        public XSubdomain Subdomain { get; set; }

        public double Thickness { get; }

        public double CalcBulkSizeCartesian() => elementGeometry.CalcBulkSizeCartesian(Nodes);

        public double CalcBulkSizeNatural() => elementGeometry.CalcBulkSizeNatural();

        public IMatrix DampingMatrix(IElement element) => throw new NotImplementedException();

        public IReadOnlyList<IReadOnlyList<IDofType>> GetElementDofTypes(IElement element) => allDofTypes;

        public XPoint EvaluateFunctionsAt(double[] naturalPoint)
        {
            var result = new XPoint(dim);
            result.Coordinates[CoordinateSystem.ElementNatural] = naturalPoint;
            result.Element = this;
            result.ShapeFunctions = Interpolation.EvaluateFunctionsAt(naturalPoint);
            return result;
        }

        public double[] FindCentroidCartesian() => Utilities.FindCentroidCartesian(dim, Nodes);


        public Dictionary<IPhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<CohesiveInterfaceMaterial>)>
            GetMaterialsForBoundaryIntegration()
        {
            var result = new Dictionary<IPhaseBoundary, (IReadOnlyList<GaussPoint>, IReadOnlyList<CohesiveInterfaceMaterial>)>();
            foreach (ClosedPhaseBoundary boundary in gaussPointsBoundary.Keys)
            {
                result[boundary] = (gaussPointsBoundary[boundary], materialsAtGPsBoundary[boundary]);
            }
            return result;
        }

        public (IReadOnlyList<GaussPoint>, IReadOnlyList<IContinuumMaterial>) GetMaterialsForBulkIntegration()
            => (gaussPointsBulk, materialsAtGPsBulk);

        public void IdentifyDofs()
        {
            this.numEnrichedDofs = 0;
            foreach (XNode node in Nodes) this.numEnrichedDofs += dim * node.EnrichmentFuncs.Count;

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
                    var nodalDofs = new IDofType[dim + dim * node.EnrichmentFuncs.Count];
                    nodalDofs[0] = StructuralDof.TranslationX;
                    nodalDofs[1] = StructuralDof.TranslationY;
                    int j = dim;
                    foreach (EnrichmentItem enrichment in node.Enrichments)
                    {
                        foreach (IDofType dof in enrichment.EnrichedDofs)
                        {
                            nodalDofs[j++] = dof;
                        }
                    }
                    this.allDofTypes[i] = nodalDofs;
                }
            }
        }

        public void IdentifyIntegrationPointsAndMaterials()
        {
            // Bulk integration
            this.gaussPointsBulk = IntegrationBulk.GenerateIntegrationPoints(this);
            int numPointsBulk = gaussPointsBulk.Count;

            // Calculate and cache standard interpolation at bulk integration points.
            //TODO: for all standard elements of the same type, this should be cached only once
            this.evalInterpolationsAtGPsBulk = new EvalInterpolation[numPointsBulk];
            for (int i = 0; i < numPointsBulk; ++i)
            {
                evalInterpolationsAtGPsBulk[i] = Interpolation.EvaluateAllAt(Nodes, gaussPointsBulk[i].Coordinates);
            }

            // Find and cache the phase at bulk integration points.
            this.phasesAtGPsVolume = new IPhase[numPointsBulk];
            Debug.Assert(Phases.Count != 0);
            if (Phases.Count == 1)
            {
                IPhase commonPhase = Phases.First();
                for (int i = 0; i < numPointsBulk; ++i) this.phasesAtGPsVolume[i] = commonPhase;
            }
            else
            {
                for (int i = 0; i < numPointsBulk; ++i)
                {
                    XPoint point = new XPoint(dim);
                    point.Element = this;
                    point.Coordinates[CoordinateSystem.ElementNatural] = gaussPointsBulk[i].Coordinates;
                    point.ShapeFunctions = evalInterpolationsAtGPsBulk[i].ShapeFunctions;
                    IPhase phase = this.FindPhaseAt(point);
                    point.PhaseID = phase.ID;
                    this.phasesAtGPsVolume[i] = phase;
                }
            }

            // Create and cache materials at bulk integration points.
            this.materialsAtGPsBulk = new ElasticMaterial2D[numPointsBulk];
            for (int i = 0; i < numPointsBulk; ++i)
            {
                this.materialsAtGPsBulk[i] = MaterialField.FindMaterialAt(this.phasesAtGPsVolume[i]);
            }

            // Create and cache materials at boundary integration points.
            if (!cohesiveInterfaces) return;
            this.gaussPointsBoundary = new Dictionary<IPhaseBoundary, IReadOnlyList<GaussPoint>>();
            this.gaussPointsBoundaryNormals = new Dictionary<IPhaseBoundary, IReadOnlyList<double[]>>();
            this.materialsAtGPsBoundary = new Dictionary<IPhaseBoundary, CohesiveInterfaceMaterial[]>();
            foreach (var boundaryIntersectionPair in PhaseIntersections)
            {
                IPhaseBoundary boundary = boundaryIntersectionPair.Key;
                IElementDiscontinuityInteraction intersection = boundaryIntersectionPair.Value;

                IReadOnlyList<GaussPoint> gaussPoints = intersection.GetBoundaryIntegrationPoints(boundaryIntegrationOrder);
                IReadOnlyList<double[]> gaussPointsNormals = 
                    intersection.GetNormalsAtBoundaryIntegrationPoints(boundaryIntegrationOrder);
                int numGaussPoints = gaussPoints.Count;
                var materials = new CohesiveInterfaceMaterial[numGaussPoints];

                //TODO: perhaps I should have one for each Gauss point
                CohesiveInterfaceMaterial material = MaterialField.FindInterfaceMaterialAt(boundary);
                for (int i = 0; i < numGaussPoints; ++i) materials[i] = material;

                gaussPointsBoundary[boundary] = gaussPoints;
                gaussPointsBoundaryNormals[boundary] = gaussPointsNormals;
                materialsAtGPsBoundary[boundary] = materials;
            }
        }

        public IMatrix MassMatrix(IElement element) => throw new NotImplementedException();

        public IMatrix StiffnessMatrix(IElement element)
        {
            Matrix Kss = BuildStiffnessMatrixStandard();
            IMatrix Ktotal;
            if (numEnrichedDofs == 0) Ktotal = Kss;
            else
            {
                (Matrix Kee, Matrix Kse) = BuildStiffnessMatricesEnriched();
                if (cohesiveInterfaces && (PhaseIntersections.Count > 0))
                {
                    Matrix Kii = BuildStiffnessMatrixBoundary();
                    Kee.AddIntoThis(Kii);
                }
                Ktotal = JoinStiffnesses(Kss, Kee, Kse);
            }
            return Ktotal;
        }

        private (Matrix Kee, Matrix Kse) BuildStiffnessMatricesEnriched()
        {
            var Kse = Matrix.CreateZero(numStandardDofs, numEnrichedDofs);
            var Kee = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);
            for (int i = 0; i < gaussPointsBulk.Count; ++i)
            {
                GaussPoint gaussPoint = gaussPointsBulk[i];
                EvalInterpolation evalInterpolation = evalInterpolationsAtGPsBulk[i];

                var gaussPointAlt = new XPoint(2);
                gaussPointAlt.Element = this;
                gaussPointAlt.ShapeFunctions = evalInterpolation.ShapeFunctions;

                double dV = evalInterpolation.Jacobian.DirectDeterminant * Thickness;

                // Material properties
                IMatrixView constitutive = materialsAtGPsBulk[i].ConstitutiveMatrix;

                // Deformation matrices: Bs = grad(Ns), Be = grad(Ne)
                Matrix Bstd = CalcDeformationMatrixStandard(evalInterpolation);
                Matrix Benr = CalculateDeformationMatrixEnriched(numEnrichedDofs, gaussPointAlt, evalInterpolation);

                // Contribution of this gauss point to the element stiffness matrices: 
                // Kee = SUM(Benr^T * c * Benr  *  dV*w), Kse = SUM(Bstd^T * c * Benr  *  dV*w)
                Matrix cBe = constitutive.MultiplyRight(Benr); // cache the result
                Matrix BeCBe = Benr.MultiplyRight(cBe, true, false);  // enriched-enriched part
                Kee.AxpyIntoThis(BeCBe, dV * gaussPoint.Weight);
                Matrix BsCBe = Bstd.MultiplyRight(cBe, true, false);  // enriched-standard part
                Kse.AxpyIntoThis(BsCBe, dV * gaussPoint.Weight);
            }
            return (Kee, Kse);
        }

        private Matrix BuildStiffnessMatrixBoundary()
        {
            var Kii = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);
            foreach (var boundaryGaussPointsPair in gaussPointsBoundary)
            {
                IPhaseBoundary boundary = boundaryGaussPointsPair.Key;
                IReadOnlyList<GaussPoint> gaussPoints = boundaryGaussPointsPair.Value;
                IReadOnlyList<double[]> normalVectorsAtGPs = gaussPointsBoundaryNormals[boundary];
                CohesiveInterfaceMaterial[] materials = materialsAtGPsBoundary[boundary];

                // Kii = sum(N^T * T * N * weight * thickness)
                for (int i = 0; i < gaussPoints.Count; ++i)
                {
                    GaussPoint gaussPoint = gaussPoints[i];
                    double scale = Thickness * gaussPoint.Weight;

                    IMatrix localT = materials[i].ConstitutiveMatrix;
                    double[] normalVector = normalVectorsAtGPs[i];
                    Matrix T = RotateInterfaceCohesiveTensor(localT, normalVector);
                    Matrix N = CalculateEnrichedShapeFunctionMatrix(gaussPoint.Coordinates, boundary);
                    Matrix partialKii = N.ThisTransposeTimesOtherTimesThis(T);
                    Kii.AxpyIntoThis(partialKii, scale);
                }
            }
            return Kii;
        }

        private Matrix BuildStiffnessMatrixStandard()
        {
            // If the element has more than 1 phase, then I cannot use the standard quadrature, since the material is  
            // different on each phase.
            var Kss = Matrix.CreateZero(numStandardDofs, numStandardDofs);
            for (int i = 0; i < gaussPointsBulk.Count; ++i)
            {
                GaussPoint gaussPoint = gaussPointsBulk[i];
                EvalInterpolation evalInterpolation = evalInterpolationsAtGPsBulk[i];
                double dV = evalInterpolation.Jacobian.DirectDeterminant * Thickness;

                // Material properties
                IMatrixView constitutive = materialsAtGPsBulk[i].ConstitutiveMatrix;

                // Deformation matrix:  Bs = grad(Ns)
                Matrix deformation = CalcDeformationMatrixStandard(evalInterpolation);

                // Contribution of this gauss point to the element stiffness matrix: Kss = sum(Bs^T * c * Bs  *  dV*w)
                Matrix partial = deformation.ThisTransposeTimesOtherTimesThis(constitutive);
                Kss.AxpyIntoThis(partial, dV * gaussPoint.Weight);
            }
            return Kss;
        }

        private Matrix CalculateDeformationMatrixEnriched(int numEnrichedDofs, XPoint gaussPoint,
            EvalInterpolation evaluatedInterpolation)
        {
            Dictionary<IEnrichmentFunction, EvaluatedFunction> enrichmentValues = EvaluateEnrichments(gaussPoint);

            var deformationMatrix = Matrix.CreateZero(3, numEnrichedDofs);
            int currentColumn = 0;
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double N = evaluatedInterpolation.ShapeFunctions[nodeIdx];
                double dNdx = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 1];

                foreach (var enrichmentValuePair in Nodes[nodeIdx].EnrichmentFuncs)
                {
                    IEnrichmentFunction enrichment = enrichmentValuePair.Key;
                    double nodalPsi = enrichmentValuePair.Value;
                    EvaluatedFunction evalEnrichment = enrichmentValues[enrichment];

                    // Bx = enrN,x = N,x(x, y) * [psi(x, y) - psi(node)] + N(x,y) * psi,x(x,y)
                    // By = enrN,y = N,y(x, y) * [psi(x, y) - psi(node)] + N(x,y) * psi,y(x,y)
                    double dPsi = evalEnrichment.Value - nodalPsi;
                    double Bx = dNdx * dPsi + N * evalEnrichment.CartesianDerivatives[0];
                    double By = dNdy * dPsi + N * evalEnrichment.CartesianDerivatives[1];

                    // This depends on the convention: node major or enrichment major. 
                    // The following is node major, since this convention is used throughout MSolve.
                    int col1 = currentColumn++;
                    int col2 = currentColumn++;
                    deformationMatrix[0, col1] = Bx;
                    deformationMatrix[1, col2] = By;
                    deformationMatrix[2, col1] = By;
                    deformationMatrix[2, col2] = Bx;
                }
            }
            Debug.Assert(currentColumn == numEnrichedDofs);
            return deformationMatrix;
        }

        /// <summary>
        /// Calculates the deformation matrix B. Dimensions = 3 x (2*numNodes).
        /// B is a linear transformation FROM the nodal values of the displacement field TO the the derivatives of
        /// the displacement field in respect to the cartesian axes (i.e. the stresses): {dU/dX} = [B] * {d} => 
        /// {u,x v,y u,y, v,x} = [... Bk ...] * {u1 v1 u2 v2 u3 v3 u4 v4}, where k = 1, ... nodesCount is a node and
        /// Bk = [dNk/dx 0; 0 dNk/dY; dNk/dy dNk/dx] (3x2)
        /// </summary>
        /// <param name="evaluatedInterpolation">The shape function derivatives calculated at a specific 
        ///     integration point</param>
        private Matrix CalcDeformationMatrixStandard(EvalInterpolation evalInterpolation)
        {
            var deformation = Matrix.CreateZero(3, numStandardDofs);
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                int col0 = 2 * nodeIdx;
                int col1 = 2 * nodeIdx + 1;

                double dNdx = evalInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evalInterpolation.ShapeGradientsCartesian[nodeIdx, 1];
                deformation[0, col0] = dNdx;
                deformation[1, col1] = dNdy;
                deformation[2, col0] = dNdy;
                deformation[2, col1] = dNdx;
            }
            return deformation;
        }


        /// <summary>
        /// The contour integral along a phase boundary is calculated for the enriched dofs that were applied due to that 
        /// boundary. For example, if there are 2 boundaries and all 3 nodes of the element are enriched due to them, then the 12
        /// enriched dofs are 
        /// [ node1Boundary1x, node1Boundary1y, node1Boundary2x, node1Boundary2y, node2Boundary1x, node2Boundary1y, ...
        /// ... node2Boundary2x, node2Boundary2y, node3Boundary1x, node3Boundary1y, node3Boundary2x, node3Boundary2y ].
        /// 
        /// When integrating along boundary 1 we will compute N^T*T*N, where 
        /// N(12x2) = [ N1 0 0 0  N2 0 0 0  N3 0 0 0; 0 N1 0 0  0 N2 0 0  0 N3 0 0 ]. 
        /// If we integrate along boundary 2, then 
        /// N(12x2) = [ 0 0 N1 0  0 0 N2 0  0 0 N3 0; 0 0 0 N1  0 0 0 N2  0 0 0 N3 ]. 
        /// 
        /// Therefore when integrating along a specific boundary, then for every enriched dof of each node i, we need to find 
        /// if the enrichment was applied due to that boundary. If yes, the corresponding index of the total shape function 
        /// array gets the value Ni. Otherwise it remains 0. 
        /// 
        /// The whole thing also takes care of a) blending enrichments due to boundaries in other elements, 
        /// b) rare cases where one or more nodes were not enriched like the rest, because their nodal support was almost 
        /// entirely in one of the two regions.
        /// </summary>
        private Matrix CalculateEnrichedShapeFunctionMatrix(double[] gaussPoint, IPhaseBoundary boundary)
        {
            double[] N = Interpolation.EvaluateFunctionsAt(gaussPoint);
            var point = new XPoint(2);
            point.Element = this;
            point.Coordinates[CoordinateSystem.ElementNatural] = gaussPoint;
            point.ShapeFunctions = N;

            var result = Matrix.CreateZero(2, numEnrichedDofs);
            int col = 0;
            for (int n = 0; n < Nodes.Count; ++n)
            {
                XNode node = Nodes[n];
                //TODO: VERY FRAGILE CODE. This order of enrichments was used to determine the order of enriched dofs in 
                //      another method. It works as of the time of writing, but this dependency must be removed. Perhaps use a 
                //      DofTable.
                foreach (IEnrichmentFunction enrichment in node.EnrichmentFuncs.Keys)
                {
                    // For enrichments that are not affected by this boundary, the next will be 0
                    double phaseJump = enrichment.EvaluateJumpAcross(boundary, point); 
                    result[0, col] = phaseJump * N[n]; // x dof
                    result[1, col + 1] = phaseJump * N[n]; // y dof
                    col += 2; // always move to the column corresponding to the next enrichment
                }
            }
            return result;
        }

        //TODO: This can be used in all XFEM elements
        private Dictionary<IEnrichmentFunction, EvaluatedFunction> EvaluateEnrichments(XPoint gaussPoint)
        {
            var cachedEvalEnrichments = new Dictionary<IEnrichmentFunction, EvaluatedFunction>();
            foreach (XNode node in Nodes)
            {
                foreach (IEnrichmentFunction enrichment in node.EnrichmentFuncs.Keys)
                {
                    // The enrichment function probably has been evaluated when processing a previous node. Avoid reevaluation.
                    if (!cachedEvalEnrichments.TryGetValue(enrichment, out EvaluatedFunction evaluatedEnrichments))
                    {
                        evaluatedEnrichments = enrichment.EvaluateAllAt(gaussPoint);
                        cachedEvalEnrichments[enrichment] = evaluatedEnrichments;
                    }
                }
            }
            return cachedEvalEnrichments;
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
                stdDofIndices[dim * n] = totalDofCounter++;
                stdDofIndices[dim * n + 1] = totalDofCounter++;

                // Enr dofs
                for (int e = 0; e < Nodes[n].EnrichmentFuncs.Count; ++e)
                {
                    for (int i = 0; i < dim; ++i)
                    {
                        enrDofIndices[enrDofCounter++] = totalDofCounter++;
                    }
                }
            }
            return (stdDofIndices, enrDofIndices);
        }

        private Matrix RotateInterfaceCohesiveTensor(IMatrix localTensor, double[] normalVector)
        {
            //TODO: Instead of doing in code the derivation of the rotation matrix, write the equations (in comments and in a 
            //      reference guide) and just implement their final versions in code. Also decide what the local system is (n, s)
            //      or (s, n)

            // Let theta be the angle from global to local system. Then a = -theta is the angle from local to global system.

            //TODO: debug that the vector has length = 1
            double cosTheta = normalVector[0];
            double sinTheta = normalVector[1];

            double cosa = cosTheta;
            double sina = -sinTheta;
            var rotation = Matrix.CreateZero(2, 2);
            rotation[0, 0] = cosa;
            rotation[0, 1] = -sina;
            rotation[1, 0] = sina;
            rotation[1, 1] = cosa;

            Matrix globalTensor = rotation.ThisTransposeTimesOtherTimesThis(localTensor);
            return globalTensor;
        }
    }
}
