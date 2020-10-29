﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.Materials;
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

namespace MGroup.XFEM.Elements
{
    public class XCrackElement2D : IXFiniteElement
    {
        private readonly IElementGeometry elementGeometry;
        private readonly int id;
        private readonly int numStandardDofs;
        private readonly IDofType[][] standardDofTypes;

        private IDofType[][] allDofTypes;

        private Dictionary<PhaseBoundary, IReadOnlyList<GaussPoint>> gaussPointsBoundary;
        private IReadOnlyList<GaussPoint> gaussPointsBulk;

        //TODO: this can be cached once for all standard elements of the same type
        private EvalInterpolation[] evalInterpolationsAtGPsVolume;

        /// <summary>
        /// In the same order as their corresponding <see cref="gaussPointsBulk"/>.
        /// </summary>
        private ElasticMaterial2D[] materialsAtGPsBulk;

        private int numEnrichedDofs;

        public XCrackElement2D(int id, IReadOnlyList<XNode> nodes, double thickness, IElementGeometry elementGeometry,
            IStructuralMaterialField materialField, IIsoparametricInterpolation interpolation, 
            IGaussPointExtrapolation gaussPointExtrapolation, IQuadrature standardQuadrature, 
            IBulkIntegration bulkIntegration)
        {
            this.id = id;
            this.Nodes = nodes;
            this.Thickness = thickness;
            this.elementGeometry = elementGeometry;

            this.Interpolation = interpolation;
            this.GaussPointExtrapolation = gaussPointExtrapolation;
            this.IntegrationStandard = standardQuadrature;
            this.IntegrationBulk = bulkIntegration;
            this.MaterialField = materialField;

            this.numStandardDofs = 2 * nodes.Count;
            this.standardDofTypes = new IDofType[nodes.Count][];
            for (int i = 0; i < nodes.Count; ++i)
            {
                standardDofTypes[i] = new IDofType[] { StructuralDof.TranslationX, StructuralDof.TranslationY };
            }

            (this.Edges, this.Faces) = elementGeometry.FindEdgesFaces(nodes);
        }

        public CellType CellType => Interpolation.CellType;

        public IElementSubcell[] ConformingSubcells { get; set; }

        public IElementDofEnumerator DofEnumerator { get; set; } = new GenericDofEnumerator();

        public IElementType ElementType => this;

        public ElementEdge[] Edges { get; }

        public ElementFace[] Faces { get; }

        public IGaussPointExtrapolation GaussPointExtrapolation { get; }

        public int ID { get => id; set => throw new InvalidOperationException("ID is set at constructor."); }

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

        public List<IElementGeometryIntersection> Intersections { get; } = new List<IElementGeometryIntersection>();

        #region remove 
        //public HashSet<IPhase> Phases { get; } = new HashSet<IPhase>();

        //public Dictionary<PhaseBoundary, IElementGeometryIntersection> PhaseIntersections { get; }
        //    = new Dictionary<PhaseBoundary, IElementGeometryIntersection>();
        #endregion

        ISubdomain IElement.Subdomain => this.Subdomain;
        public XSubdomain Subdomain { get; set; }

        public double Thickness { get; }

        public double CalcBulkSizeCartesian() => elementGeometry.CalcBulkSizeCartesian(Nodes);

        public double CalcBulkSizeNatural() => elementGeometry.CalcBulkSizeNatural();

        public IMatrix DampingMatrix(IElement element) => throw new NotImplementedException();

        public IReadOnlyList<IReadOnlyList<IDofType>> GetElementDofTypes(IElement element) => allDofTypes;

        public XPoint EvaluateFunctionsAt(double[] naturalPoint)
        {
            var result = new XPoint(2);
            result.Coordinates[CoordinateSystem.ElementNatural] = naturalPoint;
            result.Element = this;
            result.ShapeFunctions = Interpolation.EvaluateFunctionsAt(naturalPoint);
            return result;
        }

        public double[] FindCentroidCartesian() => Utilities.FindCentroidCartesian(2, Nodes);

        public (IReadOnlyList<GaussPoint>, IReadOnlyList<ElasticMaterial2D>) GetMaterialsForBulkIntegration()
            => (gaussPointsBulk, materialsAtGPsBulk);

        //TODO: This method should be moved to a base class. Enriched DOFs do not depend on the finite element, but are set globally
        public void IdentifyDofs(Dictionary<IEnrichment, IDofType[]> enrichedDofs)
        {
            this.numEnrichedDofs = 0;
            foreach (XNode node in Nodes) this.numEnrichedDofs += 2 * node.Enrichments.Count;

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
                    var nodalDofs = new IDofType[2 + 2 * node.Enrichments.Count];
                    nodalDofs[0] = StructuralDof.TranslationX;
                    nodalDofs[1] = StructuralDof.TranslationY;
                    int j = 2;
                    foreach (IEnrichment enrichment in node.Enrichments.Keys)
                    {
                        foreach (IDofType dof in enrichedDofs[enrichment])
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
            int numPointsVolume = gaussPointsBulk.Count;

            // Calculate and cache standard interpolation at integration points.
            //TODO: for all standard elements of the same type, this should be cached only once
            this.evalInterpolationsAtGPsVolume = new EvalInterpolation[numPointsVolume];
            for (int i = 0; i < numPointsVolume; ++i)
            {
                evalInterpolationsAtGPsVolume[i] = Interpolation.EvaluateAllAt(Nodes, gaussPointsBulk[i].Coordinates);
            }

            // Create and cache materials at integration points.
            this.materialsAtGPsBulk = new ElasticMaterial2D[numPointsVolume];
            for (int i = 0; i < numPointsVolume; ++i)
            {
                this.materialsAtGPsBulk[i] = MaterialField.FindMaterialAt(null);
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
                (Matrix Kee, Matrix Kse) = BuildConductivityMatricesEnriched();
                Ktotal = JoinStiffnesses(Kss, Kee, Kse);
            }
            return Ktotal;
        }

        private (Matrix Kee, Matrix Kse) BuildConductivityMatricesEnriched()
        {
            var Kse = Matrix.CreateZero(numStandardDofs, numEnrichedDofs);
            var Kee = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);
            for (int i = 0; i < gaussPointsBulk.Count; ++i)
            {
                GaussPoint gaussPoint = gaussPointsBulk[i];
                EvalInterpolation evalInterpolation = evalInterpolationsAtGPsVolume[i];

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
                // Kee = SUM(Benr^T * C * Benr  *  dV*w), Kse = SUM(Bstd^T * C * Benr  *  dV*w)
                Matrix cBe = constitutive.MultiplyRight(Benr); // cache the result
                Matrix BeCBe = Benr.MultiplyRight(cBe, true, false);  // enriched-enriched part
                Kee.AxpyIntoThis(BeCBe, dV * gaussPoint.Weight);
                Matrix BsCBe = Bstd.MultiplyRight(cBe, true, false);  // enriched-standard part
                Kse.AxpyIntoThis(BsCBe, dV * gaussPoint.Weight);
            }
            return (Kee, Kse);
        }

        //TODO: Perhaps the std integration rule should be used for Kss
        private Matrix BuildStiffnessMatrixStandard()
        {
            // If the element is has more than 1 phase, then I cannot use the standard quadrature, since the material is  
            // different on each phase.
            var Kss = Matrix.CreateZero(numStandardDofs, numStandardDofs);
            for (int i = 0; i < gaussPointsBulk.Count; ++i)
            {
                GaussPoint gaussPoint = gaussPointsBulk[i];
                EvalInterpolation evalInterpolation = evalInterpolationsAtGPsVolume[i];
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
            EvalInterpolation evalInterpolation)
        {
            var uniqueEnrichments = new Dictionary<IEnrichment, EvaluatedFunction>();

            var deformationMatrix = Matrix.CreateZero(3, numEnrichedDofs);
            int currentColumn = 0;
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double N = evalInterpolation.ShapeFunctions[nodeIdx];
                double dNdx = evalInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evalInterpolation.ShapeGradientsCartesian[nodeIdx, 1];

                foreach (var enrichmentValuePair in Nodes[nodeIdx].Enrichments)
                {
                    IEnrichment enrichment = enrichmentValuePair.Key;
                    double nodalPsi = enrichmentValuePair.Value;

                    // The enrichment function probably has been evaluated when processing a previous node. Avoid reevaluation.
                    bool exists = uniqueEnrichments.TryGetValue(enrichment, out EvaluatedFunction evalEnrichment);
                    if (!(exists))
                    {
                        evalEnrichment = enrichment.EvaluateAllAt(gaussPoint);
                        uniqueEnrichments[enrichment] = evalEnrichment;
                    }
                    
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
        /// Calculates the deformation matrix B. Dimensions = 3x8.
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
                stdDofIndices[2 * n] = totalDofCounter++;
                stdDofIndices[2 * n + 1] = totalDofCounter++;

                // Enr dofs
                for (int e = 0; e < Nodes[n].Enrichments.Count; ++e)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        enrDofIndices[enrDofCounter++] = totalDofCounter++;
                    }
                }
            }
            return (stdDofIndices, enrDofIndices);
        }
    }
}