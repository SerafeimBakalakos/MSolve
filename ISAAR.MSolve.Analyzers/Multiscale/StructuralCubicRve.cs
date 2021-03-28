using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Analyzers.Interfaces;
using ISAAR.MSolve.Discretization;
using ISAAR.MSolve.Discretization.FreedomDegrees;
using ISAAR.MSolve.Discretization.Interfaces;
using ISAAR.MSolve.LinearAlgebra.Matrices;
using ISAAR.MSolve.LinearAlgebra.Reduction;
using ISAAR.MSolve.LinearAlgebra.Vectors;

//TODO: How can we ensure that the model has the correct shape and discretization?
namespace ISAAR.MSolve.Analyzers.Multiscale
{
    /// <summary>
    /// This works only for cubic (3D) RVEs, centered at (0,0,0), with translation dofs along x, y and z per node and 
    /// linear boundary conditions.
    /// </summary>
    public class StructuralCubicRve : IReferenceVolumeElement
    {
        private readonly IStructuralModel model;
        private readonly double xMin, yMin, zMin, xMax, yMax, zMax;
        private readonly HashSet<INode> minXnodes, maxXnodes, minYnodes, maxYnodes, minZnodes, maxZnodes;

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="minCornerCoords"></param>
        /// <param name="maxCornerCoords"></param>
        /// <param name="meshTolerance">The default is 1E-10 * min(|xMax-xMin|, |yMax-yMin|)</param>
        public StructuralCubicRve(IStructuralModel model, double[] minCornerCoords, double[] maxCornerCoords, double meshTolerance)
        {
            this.model = model;
            this.xMin = minCornerCoords[0]; 
            this.yMin = minCornerCoords[1];
            this.zMin = minCornerCoords[2];
            this.xMax = maxCornerCoords[0];
            this.yMax = maxCornerCoords[1];
            this.zMax = maxCornerCoords[2];

            // Find the nodes of each edge
            minXnodes = new HashSet<INode>();
            maxXnodes = new HashSet<INode>();
            minYnodes = new HashSet<INode>();
            maxYnodes = new HashSet<INode>();
            minZnodes = new HashSet<INode>();
            maxZnodes = new HashSet<INode>();
            foreach (INode node in model.Nodes)
            {
                // Top and right edges are prioritized for corner nodes. //TODO: should the corner nodes be handled differently?
                if (Math.Abs(node.Z - zMax) <= meshTolerance) maxZnodes.Add(node);
                else if (Math.Abs(node.X - xMax) <= meshTolerance) maxXnodes.Add(node);
                else if (Math.Abs(node.Y - yMax) <= meshTolerance) maxYnodes.Add(node);
                else if (Math.Abs(node.Z - zMin) <= meshTolerance) minZnodes.Add(node);
                else if (Math.Abs(node.X - xMin) <= meshTolerance) minXnodes.Add(node);
                else if (Math.Abs(node.Y - yMin) <= meshTolerance) minYnodes.Add(node);
            }
        }

        public StructuralCubicRve(IStructuralModel model, double[] minCornerCoords, double[] maxCornerCoords) : 
            this(model, minCornerCoords, maxCornerCoords, 
                1E-10 * Vector.CreateFromArray(maxCornerCoords).Subtract(Vector.CreateFromArray(minCornerCoords)).MinAbsolute())
        {
        }

        public void ApplyBoundaryConditionsLinear(double[] macroscopicStrains)
        {
            var e = Vector.CreateFromArray(macroscopicStrains);
            IEnumerable<INode> boundaryNodes =
                minXnodes.Concat(minZnodes).Concat(minYnodes).Concat(maxXnodes).Concat(maxZnodes).Concat(maxYnodes);
            foreach (var node in boundaryNodes)
            {
                // Kinematic relations submatrix for linear displacements
                var transposeD = Matrix.CreateZero(3, 6);
                transposeD[0, 0] = node.X;
                transposeD[1, 1] = node.Y;
                transposeD[2, 2] = node.Z;

                transposeD[0, 3] = 0.5 * node.Y;
                transposeD[1, 3] = 0.5 * node.X;

                transposeD[1, 4] = 0.5 * node.Z;
                transposeD[2, 4] = 0.5 * node.Y;

                transposeD[2, 5] = 0.5 * node.X;
                transposeD[0, 5] = 0.5 * node.Z;

                // Prescribed displacements at boundary node
                Vector ub = transposeD * e;

                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = ub[0] });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = ub[1] });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationZ, Amount = ub[2] });
            }

        }

        public void ApplyBoundaryConditionsZero()
        {
            IEnumerable<INode> boundaryNodes =
                minXnodes.Concat(minZnodes).Concat(minYnodes).Concat(maxXnodes).Concat(maxZnodes).Concat(maxYnodes);
            foreach (var node in boundaryNodes)
            {
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationZ, Amount = 0.0 });
            }
        }

        public double CalculateRveVolume() => (xMax - xMin) * (yMax - yMin) * (zMax - zMin);

        public IMatrixView CalculateKinematicRelationsMatrix(ISubdomain subdomain)
        {
            ISubdomainConstrainedDofOrdering constrainedDofOrdering = subdomain.ConstrainedDofOrdering;
            var kinematicRelations = Matrix.CreateZero(6, constrainedDofOrdering.NumConstrainedDofs);
            CalculateKinematicsOfFace(constrainedDofOrdering, minZnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, minXnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, minYnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, maxYnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, maxXnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, maxZnodes, kinematicRelations);
            return kinematicRelations;
        }

        private void CalculateKinematicsOfFace(ISubdomainConstrainedDofOrdering constrainedDofOrdering,
            IEnumerable<INode> faceNodes, Matrix kinematicRelations)
        {
            foreach (INode node in faceNodes)
            {
                int dofX = constrainedDofOrdering.ConstrainedDofs[node, StructuralDof.TranslationX];
                int dofY = constrainedDofOrdering.ConstrainedDofs[node, StructuralDof.TranslationY];
                int dofZ = constrainedDofOrdering.ConstrainedDofs[node, StructuralDof.TranslationZ];

                kinematicRelations[0, dofX] = node.X;
                kinematicRelations[1, dofY] = node.Y;
                kinematicRelations[2, dofZ] = node.Z;

                kinematicRelations[3, dofX] = 0.5 * node.Y;
                kinematicRelations[3, dofY] = 0.5 * node.X;

                kinematicRelations[4, dofY] = 0.5 * node.Z;
                kinematicRelations[4, dofZ] = 0.5 * node.Y;

                kinematicRelations[5, dofZ] = 0.5 * node.X;
                kinematicRelations[5, dofX] = 0.5 * node.Z;
            }
        }
    }
}
