﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.Commons;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Integration;
using ISAAR.MSolve.Discretization.Integration.Quadratures;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.Discretization.Mesh;
using ISAAR.MSolve.FEM.Interpolation;
using ISAAR.MSolve.FEM.Interpolation.GaussPointExtrapolation;
using ISAAR.MSolve.Geometry.Coordinates;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Vectors;
using ISAAR.MSolve.Materials;
using ISAAR.MSolve.XFEM.Enrichments.Items;
using ISAAR.MSolve.XFEM.Entities;
using ISAAR.MSolve.XFEM.FreedomDegrees;
using ISAAR.MSolve.XFEM.Integration;
using ISAAR.MSolve.XFEM.Materials;
using ISAAR.MSolve.XFEM.Utilities;

namespace ISAAR.MSolve.XFEM.Elements
{
    public class XThermalElement2D : IXFiniteElement 
    {
        private readonly int id;
        private readonly IDofType[][] standardDofTypes;

        public XThermalElement2D(int id, IReadOnlyList<XNode> nodes, ThermalMaterial material, double thickness, 
            IIsoparametricInterpolation2D interpolation, IGaussPointExtrapolation2D gaussPointExtrapolation, 
            IQuadrature2D standardQuadrature, IIntegrationStrategy2D<XThermalElement2D> integrationStrategy, 
            int numGaussPointsInterface)
        {
            this.id = id;
            this.Thickness = thickness;
            this.Nodes = nodes;
            this.Interpolation = interpolation;
            this.GaussPointExtrapolation = gaussPointExtrapolation;
            this.StandardQuadrature = standardQuadrature;
            this.IntegrationStrategy = integrationStrategy;
            this.NumGaussPointsInterface = numGaussPointsInterface;
            this.Material = material;

            this.NumStandardDofs = nodes.Count;
            standardDofTypes = new IDofType[nodes.Count][];
            for (int i = 0; i < nodes.Count; ++i) standardDofTypes[i] = new IDofType[] { ThermalDof.Temperature };
        }

        public CellType CellType => Interpolation.CellType;
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
                        NaturalPoint node1 = Interpolation.NodalNaturalCoordinates[i];
                        NaturalPoint node2 = Interpolation.NodalNaturalCoordinates[(i + 1) % Nodes.Count];
                        edges[i] = (node1, node2);
                    }
                    return edges;
                }
            }
        }

        public List<ThermalInterfaceEnrichment> EnrichmentItems { get; } = new List<ThermalInterfaceEnrichment>();

        public IGaussPointExtrapolation2D GaussPointExtrapolation { get; }

        public int ID { get => id; set => throw new NotImplementedException(); }

        internal IIntegrationStrategy2D<XThermalElement2D> IntegrationStrategy { get; }

        /// <summary>
        /// Common interpolation for standard and enriched nodes.
        /// </summary>
        public IIsoparametricInterpolation2D Interpolation { get; }

        //TODO: This must be refactored together with EnrichmentItems properties
        private bool IsStandardElement
        {
            get
            {
                foreach (XNode node in Nodes)
                {
                    if (node.EnrichmentItems.Count != 0) return false;
                }
                return true;
            }
        }

        internal ThermalMaterial Material { get; }

        IReadOnlyList<INode> IElement.Nodes => Nodes;
        /// <summary>
        /// All nodes are enriched for now.
        /// </summary>
        public IReadOnlyList<XNode> Nodes { get; }

        public int NumGaussPointsInterface { get; }

        public int NumStandardDofs { get; }
        internal IQuadrature2D StandardQuadrature { get; } //TODO: This should not always be used for Kss. E.g. it doesn't work for bimaterial interface.

        ISubdomain IElement.Subdomain => this.Subdomain;
        public XSubdomain Subdomain { get; set; }

        public double Thickness { get; }

        /// <summary>
        /// This only works for points that do not lie on the crack interface. As such it is safe to pass GPs only
        /// </summary>
        /// <param name="gaussPoint"></param>
        /// <param name="evaluatedInterpolation"></param>
        /// <param name="standardNodalDisplacements"></param>
        /// <param name="enrichedNodalDisplacements"></param>
        /// <returns></returns>
        public Vector2 CalculateDisplacementField(NaturalPoint gaussPoint, EvalInterpolation2D evaluatedInterpolation,
            Vector standardNodalDisplacements, Vector enrichedNodalDisplacements)
        {
            #region debug
            double tol = 1e-6;
            if ((Math.Abs(gaussPoint.Xi) <= tol) || (Math.Abs(gaussPoint.Eta) <= tol))
            {
                Console.WriteLine("Found an intersection point that isn't a node.");
            }
            #endregion
            var displacements = new double[2];

            // Standard contributions
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double shapeFunction = evaluatedInterpolation.ShapeFunctions[nodeIdx];
                displacements[0] += shapeFunction * standardNodalDisplacements[2 * nodeIdx];
                displacements[1] += shapeFunction * standardNodalDisplacements[2 * nodeIdx + 1];
            }

            // Enriched contributions
            //TODO: this should be taken as input, so that it is only computed once if it is needed for displacements, strains, 
            //      stresses, just like the evaluated interpolation.
            IReadOnlyDictionary<IEnrichmentItem2D, EvaluatedFunction2D[]> evalEnrichments =
                EvaluateEnrichments(gaussPoint, evaluatedInterpolation);
            int dof = 0;
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double shapeFunction = evaluatedInterpolation.ShapeFunctions[nodeIdx];
                foreach (var nodalEnrichment in Nodes[nodeIdx].EnrichmentItems)
                {
                    EvaluatedFunction2D[] currentEvalEnrichments = evalEnrichments[nodalEnrichment.Key];
                    for (int e = 0; e < currentEvalEnrichments.Length; ++e)
                    {
                        double basisFunction = shapeFunction * (currentEvalEnrichments[e].Value - nodalEnrichment.Value[e]);
                        //double basisFunction = shapeFunction * currentEvalEnrichments[e].Value; // for debugging
                        displacements[0] += basisFunction * enrichedNodalDisplacements[dof++];
                        displacements[1] += basisFunction * enrichedNodalDisplacements[dof++];
                    }
                }
            }

            return Vector2.CreateFromArray(displacements);
        }

        /// <summary>
        /// The displacement field derivatives are a 2x2 matrix: gradientU[i,j] = dui/dj where i is the vector component 
        /// and j is the coordinate, w.r.t which the differentiation is done. The differentation coordinates and the
        /// vector components refer to the global cartesian system. 
        /// </summary>
        /// <param name="evaluatedInterpolation"></param>
        /// <param name="nodalDisplacementsX"></param>
        /// <param name="nodalDisplacementsY"></param>
        /// <returns></returns>
        public Matrix2by2 CalculateDisplacementFieldGradient(NaturalPoint gaussPoint,
            EvalInterpolation2D evaluatedInterpolation, Vector standardNodalDisplacements,
            Vector enrichedNodalDisplacements) //TODO: this must only allow evaluations at Gauss points. It doesn't work for points on the crack interface
        {
            var displacementGradient = Matrix2by2.CreateZero();

            // Standard contributions
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double displacementX = standardNodalDisplacements[2 * nodeIdx];
                double displacementY = standardNodalDisplacements[2 * nodeIdx + 1];

                double dNdx = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 1];
                displacementGradient[0, 0] += dNdx * displacementX;
                displacementGradient[0, 1] += dNdy * displacementX;
                displacementGradient[1, 0] += dNdx * displacementY;
                displacementGradient[1, 1] += dNdy * displacementY;
            }

            // Enriched contributions. TODO: Extract the common steps with building B into a separate method 
            IReadOnlyDictionary<IEnrichmentItem2D, EvaluatedFunction2D[]> evalEnrichments =
                EvaluateEnrichments(gaussPoint, evaluatedInterpolation);
            int dof = 0;
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double N = evaluatedInterpolation.ShapeFunctions[nodeIdx];
                double dNdx = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 1];

                foreach (var nodalEnrichment in Nodes[nodeIdx].EnrichmentItems)
                {
                    EvaluatedFunction2D[] currentEvalEnrichments = evalEnrichments[nodalEnrichment.Key];
                    for (int e = 0; e < currentEvalEnrichments.Length; ++e)
                    {
                        double psi = currentEvalEnrichments[e].Value;
                        Vector2 gradPsi = currentEvalEnrichments[e].CartesianDerivatives;
                        double deltaPsi = psi - nodalEnrichment.Value[e];

                        double Bx = dNdx * deltaPsi + N * gradPsi[0];
                        double By = dNdy * deltaPsi + N * gradPsi[1];

                        double enrDisplacementX = enrichedNodalDisplacements[dof++];
                        double enrDisplacementY = enrichedNodalDisplacements[dof++];

                        displacementGradient[0, 0] += Bx * enrDisplacementX;
                        displacementGradient[0, 1] += By * enrDisplacementX;
                        displacementGradient[1, 0] += Bx * enrDisplacementY;
                        displacementGradient[1, 1] += By * enrDisplacementY;
                    }
                }
            }

            return displacementGradient;
        }

        // In a non linear problem I would also have to pass the new displacements or I would have to update the
        // material state elsewhere.
        public Tensor2D CalculateStressTensor(Matrix2by2 displacementFieldGradient, Matrix constitutive)
        {
            double strainXX = displacementFieldGradient[0, 0];
            double strainYY = displacementFieldGradient[1, 1];
            double strainXYtimes2 = displacementFieldGradient[0, 1] + displacementFieldGradient[1, 0];

            // Should constitutive also be a tensor? Or  should I use matrices and vectors instead of tensors?
            double stressXX = constitutive[0, 0] * strainXX + constitutive[0, 1] * strainYY;
            double stressYY = constitutive[1, 0] * strainXX + constitutive[1, 1] * strainYY;
            double stressXY = constitutive[2, 2] * strainXYtimes2;

            return new Tensor2D(stressXX, stressYY, stressXY);
        }

        public IMatrix DampingMatrix(IElement element) => throw new NotImplementedException();

        public IReadOnlyList<IReadOnlyList<IDofType>> GetElementDofTypes(IElement element) => OrderDofsNodeMajor();

        public IMatrix MassMatrix(IElement element) => throw new NotImplementedException();

        public (double[] standardElementTemperatures, double[] enrichedElementTemperatures)
            SeparateStdEnrVector(double[] elementTemperatures)
            => SeparateStdEnrVectorNodeMajor(elementTemperatures);

        public IMatrix StiffnessMatrix(IElement element)
        {
            return JoinStiffnessesNodeMajor(BuildStandardConductivityMatrix, () =>
            {
                (Matrix Kee, Matrix Kse) = BuildEnrichedConductivityMatricesUpper();
                Debug.Assert(EnrichmentItems.Count <= 1);
                Matrix Kii = BuildStiffnessForInterfacialResistance(EnrichmentItems[0]);
                Kee.AddIntoThis(Kii);
                return (Kee, Kse);
            });
        }

        //TODO: In some cases this could use a the Gauss points of standard quadrature to save time.
        internal Matrix BuildStandardConductivityMatrix(IReadOnlyList<GaussPoint> gaussPoints)
        {
            var Kss = Matrix.CreateZero(NumStandardDofs, NumStandardDofs);

            //TODO: Use the standard quadrature for Kss.
            foreach (GaussPoint gaussPoint in gaussPoints)
            {
                EvalInterpolation2D evaluatedInterpolation = Interpolation.EvaluateAllAt(Nodes, gaussPoint);
                double dV = evaluatedInterpolation.Jacobian.DirectDeterminant * Thickness;
                //TODO: The thickness is constant per element in FEM, but what about XFEM? Different materials within the same element are possible.
                
                // Material properties
                double conductivity = Material.ThermalConductivity;

                // Bs = grad(Ns)
                Matrix deformation = CalculateStandardDeformationMatrix(evaluatedInterpolation.ShapeGradientsCartesian);

                // Contribution of this gauss point to the element stiffness matrix: Kss = sum(Bs^T * c * Bs  *  dV*w)
                Matrix partial = deformation.MultiplyRight(conductivity * deformation, true);
                Kss.AxpyIntoThis(partial, dV * gaussPoint.Weight);
            }
            return Kss;
        }

        internal (Matrix Kee, Matrix Kse) BuildEnrichedConductivityMatricesUpper()
        {
            int numStandardDofs = NumStandardDofs;
            int numEnrichedDofs = CountEnrichedDofs();
            var Kse = Matrix.CreateZero(numStandardDofs, numEnrichedDofs);
            var Kee = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);

            foreach (GaussPoint gaussPoint in IntegrationStrategy.GenerateIntegrationPoints(this))
            {
                EvalInterpolation2D evaluatedInterpolation = Interpolation.EvaluateAllAt(Nodes, gaussPoint);
                double dV = evaluatedInterpolation.Jacobian.DirectDeterminant * Thickness;
                //TODO: The thickness is constant per element in FEM, but what about XFEM? Different materials within the same element are possible.

                // Material properties
                double conductivity = Material.ThermalConductivity;

                // Bs = grad(Ns), Be = grad(Ne)
                Matrix Bstd = CalculateStandardDeformationMatrix(evaluatedInterpolation.ShapeGradientsCartesian);
                Matrix Benr = CalculateEnrichedDeformationMatrix(numEnrichedDofs, gaussPoint, evaluatedInterpolation);

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

        internal Matrix BuildStiffnessForInterfacialResistance(ThermalInterfaceEnrichment enrichment)
        {
            int numEnrichedDofs = CountEnrichedDofs();
            var Kii = Matrix.CreateZero(numEnrichedDofs, numEnrichedDofs);
            GaussPoint[] gaussPoints = enrichment.Discontinuity.IntegrationPointsAlongInterface(this, NumGaussPointsInterface);

            if (gaussPoints.Length == 0) return Kii; // The element is not intersected by the discontinuity

            foreach (GaussPoint gaussPoint in gaussPoints)
            {
                // Kee = sum(1/a * N^T * N)
                Vector N = Vector.CreateFromArray(Interpolation.EvaluateFunctionsAt(gaussPoint));
                Matrix integratedFunction = N.TensorProduct(N.Scale(1.0 / enrichment.InterfaceResistance));
                Kii.AxpyIntoThis(integratedFunction, enrichment.Discontinuity.Thickness * gaussPoint.Weight);
            }
            return Kii;
        }

        //TODO: This should be delegated to element specific std and enr DofOrderers
        //TODO: This should be cached and, along with other dof data, updated when the element's enrichments change, which must 
        //      happen with a single call. 
        internal int CountEnrichedDofs()
        {
            int count = 0;
            foreach (XNode node in Nodes) count += node.EnrichedDofsCount; // in all nodes or in enriched interpolation nodes?
            return count;
        }

        //TODO: This should be delegated to element specific std and enr DofOrderers
        internal FreedomDegrees.Ordering.DofTable<EnrichedDof> GetEnrichedDofs()
        {
            var elementDofs = new FreedomDegrees.Ordering.DofTable<EnrichedDof>();
            int dofCounter = 0;
            foreach (XNode node in Nodes)
            {
                foreach (var enrichment in node.EnrichmentItems.Keys)
                {
                    foreach (var enrichedDof in enrichment.Dofs) // there are different dofs for x and y axes
                    {
                        elementDofs[node, enrichedDof] = dofCounter++;
                    }
                }
            }
            return elementDofs;
        }

        // TODO: Perhaps this should be saved as a DofOrderer object (the dofs themselves would be created on  
        // demand though). XElement will have a mutable one, while others will get a view. I could still use a  
        // DofOrderer even if I do not save it. Transfering most of the code to the Enumerator class, also reduces  
        // code duplication with the standard ContinuumElement2D
        //TODO: This should be delegated to element specific std and enr DofOrderers
        internal FreedomDegrees.Ordering.DofTable<StructuralDof> GetStandardDofs()
        {
            var elementDofs = new FreedomDegrees.Ordering.DofTable<StructuralDof>();
            int dofCounter = 0;
            foreach (XNode node in Nodes)
            {
                elementDofs[node, StructuralDof.TranslationX] = dofCounter++;
                elementDofs[node, StructuralDof.TranslationY] = dofCounter++;
            }
            return elementDofs;
        }

        private IMatrix JoinStiffnessesNodeMajor(Func<IReadOnlyList<GaussPoint>, Matrix> buildKff, 
            Func<(Matrix Kee, Matrix Kes)> buildKeeKes)
        {
            //TODO: Perhaps it is more efficient to do this by just appending Kse and Kee to Kss.
            if (IsStandardElement) return buildKff(StandardQuadrature.IntegrationPoints);
            else
            {
                // The dof order in increasing frequency of change is: node, enrichment item, enrichment function, axis.
                // WARNING: The order here must match the order in OrderDofsNodeMajor() and BuildEnrichedStiffnessMatricesUpper()

                // Find the mapping from Kss, Kse, Kee to a total matrix for the element. TODO: This could be a different method.
                int numEnrichedDofs = CountEnrichedDofs();
                var stdDofIndices = new int[NumStandardDofs];
                var enrDofIndices = new int[numEnrichedDofs];
                int enrDofCounter = 0, totDofCounter = 0;
                for (int n = 0; n < Nodes.Count; ++n)
                {
                    // Std dofs
                    stdDofIndices[2 * n] = totDofCounter;           // std X
                    stdDofIndices[2 * n + 1] = totDofCounter + 1;   // std Y
                    totDofCounter += 2;

                    // Enr dofs
                    for (int e = 0; e < Nodes[n].EnrichedDofsCount; ++e)
                    {
                        enrDofIndices[enrDofCounter++] = totDofCounter++;
                    }
                }

                // Copy the entries of Kss, Kse, Kee to the upper triangle of a total matrix for the element.
                Matrix Kss = buildKff(IntegrationStrategy.GenerateIntegrationPoints(this));
                (Matrix Kee, Matrix Kse) = BuildEnrichedConductivityMatricesUpper();
                var Ktotal = SymmetricMatrix.CreateZero(NumStandardDofs + numEnrichedDofs);

                // Upper triangle of Kss
                for (int stdCol = 0; stdCol < NumStandardDofs; ++stdCol)
                {
                    int totColIdx = stdDofIndices[stdCol];
                    for (int stdRow = 0; stdRow <= stdCol; ++stdRow)
                    {
                        Ktotal[stdDofIndices[stdRow], totColIdx] = Kss[stdRow, stdCol];
                    }
                }

                for (int enrCol = 0; enrCol < numEnrichedDofs; ++enrCol)
                {
                    int totColIdx = enrDofIndices[enrCol];

                    // Whole Kse
                    for (int stdRow = 0; stdRow < NumStandardDofs; ++stdRow)
                    {
                        Ktotal[stdDofIndices[stdRow], totColIdx] = Kse[stdRow, enrCol];
                    }

                    // Upper triangle of Kee
                    for (int enrRow = 0; enrRow <= enrCol; ++enrRow)
                    {
                        Ktotal[enrDofIndices[enrRow], totColIdx] = Kee[enrRow, enrCol];
                    }
                }

                return Ktotal;
            }
        }

        internal IReadOnlyList<IReadOnlyList<IDofType>> OrderDofsNodeMajor()
        {
            //TODO: should they enriched dofs also be cached per element?
            if (IsStandardElement) return standardDofTypes;
            else
            {
                // The dof order in increasing frequency of change is: node, enrichment item, enrichment function, axis.
                // A similar convention should also hold for each enrichment item: enrichment function major, axis minor.
                // WARNING: The order here must match the order in JoinStiffnessesNodeMajor().
                var dofTypes = new List<IDofType>[Nodes.Count];
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    dofTypes[i] = new List<IDofType>(4); // At least 2 * num std dofs
                    dofTypes[i].AddRange(standardDofTypes[i]);
                    foreach (IEnrichmentItem2D enrichment in Nodes[i].EnrichmentItems.Keys)
                    {
                        dofTypes[i].AddRange(enrichment.Dofs);
                    }
                }
                return dofTypes;
            }
        }

        /// <summary>
        /// BUG: This does not work. MSolve assumes all dofs of the same node have consecutive indices in the stiffness matrix.
        /// </summary>
        internal IReadOnlyList<IReadOnlyList<IDofType>> OrderDofsStandardFirst()
        {
            //TODO: should they enriched dofs also be cached per element?
            if (IsStandardElement) return standardDofTypes;
            else
            {
                // The dof order in increasing frequency of change is: node, enrichment item, enrichment function, axis.
                // A similar convention should also hold for each enrichment item: enrichment function major, axis minor.
                // WARNING: The order here must match the order in JoinStiffnessesStandardFirst().
                var dofTypes = new List<IDofType>[Nodes.Count];

                // Standard dofs first
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    dofTypes[i] = new List<IDofType>(4); // At least 2 * num std dofs
                    dofTypes[i].AddRange(standardDofTypes[i]);
                }

                // Then enriched dofs
                for (int i = 0; i < Nodes.Count; ++i)
                {
                    foreach (IEnrichmentItem2D enrichment in Nodes[i].EnrichmentItems.Keys)
                    {
                        dofTypes[i].AddRange(enrichment.Dofs);
                    }
                }
                return dofTypes;
            }
        }

        internal (double[] standardElementDisplacements, double[] enrichedElementDisplacements)
            SeparateStdEnrVectorNodeMajor(double[] elementDisplacements)
        {
            int numEnrichedDofs = CountEnrichedDofs();
            var standardElementDisplacements = new double[NumStandardDofs];
            var enrichedElementDisplacements = new double[numEnrichedDofs];

            int totalIdx = 0;
            int enrichedIdx = 0;
            for (int n = 0; n < Nodes.Count; ++n)
            {
                standardElementDisplacements[2 * n] = elementDisplacements[totalIdx++];
                standardElementDisplacements[2 * n + 1] = elementDisplacements[totalIdx++];

                for (int e = 0; e < Nodes[n].EnrichedDofsCount; ++e)
                {
                    enrichedElementDisplacements[enrichedIdx++] = elementDisplacements[totalIdx++];
                }
            }
            return (standardElementDisplacements, enrichedElementDisplacements);
        }

        private Matrix CalculateEnrichedDeformationMatrix(int artificialDofsCount,
            NaturalPoint gaussPoint, EvalInterpolation2D evaluatedInterpolation)
        {
            //CartesianPoint cartesianPoint = evaluatedInterpolation.TransformPointNaturalToGlobalCartesian(gaussPoint);
            var uniqueEnrichments = new Dictionary<IEnrichmentItem2D, EvaluatedFunction2D[]>();

            var deformationMatrix = Matrix.CreateZero(2, artificialDofsCount);
            int currentColumn = 0;
            for (int nodeIdx = 0; nodeIdx < Nodes.Count; ++nodeIdx)
            {
                double N = evaluatedInterpolation.ShapeFunctions[nodeIdx];
                double dNdx = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 0];
                double dNdy = evaluatedInterpolation.ShapeGradientsCartesian[nodeIdx, 1];

                foreach (var enrichment in Nodes[nodeIdx].EnrichmentItems)
                {
                    IEnrichmentItem2D enrichmentItem = enrichment.Key;
                    double[] nodalEnrichmentValues = enrichment.Value;

                    // The enrichment function probably has been evaluated when processing a previous node. Avoid reevaluation.
                    EvaluatedFunction2D[] evaluatedEnrichments;
                    if (!(uniqueEnrichments.TryGetValue(enrichmentItem, out evaluatedEnrichments)))
                    {
                        evaluatedEnrichments = enrichmentItem.EvaluateAllAt(gaussPoint, this, evaluatedInterpolation);
                        uniqueEnrichments[enrichmentItem] = evaluatedEnrichments;
                    }

                    for (int i = 0; i < evaluatedEnrichments.Length; ++i)
                    {
                        // For each node and with all derivatives w.r.t. cartesian coordinates, the enrichment derivatives 
                        // are: Bx = enrN,x = N,x(x,y) * [psi(x,y) - psi(node)] + N(x,y) * psi,x(x,y), where psi is the  
                        // enrichment function
                        double Bx = dNdx * (evaluatedEnrichments[i].Value - nodalEnrichmentValues[i])
                            + N * evaluatedEnrichments[i].CartesianDerivatives[0];
                        double By = dNdy * (evaluatedEnrichments[i].Value - nodalEnrichmentValues[i])
                            + N * evaluatedEnrichments[i].CartesianDerivatives[1];

                        // This depends on the convention: node major or enrichment major. The following is node major.
                        int col = currentColumn++;

                        deformationMatrix[0, col] = Bx;
                        deformationMatrix[1, col] = By;
                    }
                }
            }
            Debug.Assert(currentColumn == artificialDofsCount);
            return deformationMatrix;
        }

        /// <summary>
        /// Calculates the deformation matrix B. Dimensions = 3x8.
        /// B is a linear transformation FROM the nodal values of the displacement field TO the the derivatives of
        /// the displacement field in respect to the cartesian axes (i.e. the stresses): {dU/dX} = [B] * {d} => 
        /// {T,x T,y} = [... Bk ...] * {T1 T2 T3 T4}, where k = 1, ... nodesCount is a node and
        /// Bk = [dNk/dx; dNk/dy] (2x1)
        /// </summary>
        /// <param name="evaluatedInterpolation">The shape function derivatives calculated at a specific 
        ///     integration point</param>
        /// <returns></returns>
        private Matrix CalculateStandardDeformationMatrix(Matrix shapeGradientsCartesian)
            => shapeGradientsCartesian.Transpose();

        private IReadOnlyDictionary<IEnrichmentItem2D, EvaluatedFunction2D[]> EvaluateEnrichments(
            NaturalPoint gaussPoint, EvalInterpolation2D evaluatedInterpolation)
        {
            var cachedEvalEnrichments = new Dictionary<IEnrichmentItem2D, EvaluatedFunction2D[]>();
            foreach (XNode node in Nodes)
            {
                foreach (var enrichment in node.EnrichmentItems)
                {
                    IEnrichmentItem2D enrichmentItem = enrichment.Key;
                    double[] nodalEnrichmentValues = enrichment.Value;

                    // The enrichment function probably has been evaluated when processing a previous node. Avoid reevaluation.
                    if (!(cachedEvalEnrichments.TryGetValue(enrichmentItem, out EvaluatedFunction2D[] evaluatedEnrichments)))
                    {
                        evaluatedEnrichments = enrichmentItem.EvaluateAllAt(gaussPoint, this, evaluatedInterpolation);
                        cachedEvalEnrichments[enrichmentItem] = evaluatedEnrichments;
                    }
                }
            }
            return cachedEvalEnrichments;
        }
    }
}