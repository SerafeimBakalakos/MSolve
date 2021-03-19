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
    /// This works only for square (2D) RVEs, centered at (0,0), with translation dofs along x and y per node and 
    /// linear boundary conditions.
    /// </summary>
    public class StructuralSquareRve : IReferenceVolumeElement
    {
        private const int numDimensions = 2;

        private readonly IStructuralModel model;
        private readonly double xMin, yMin, xMax, yMax;
        private readonly double thickness;
        private readonly HashSet<INode> leftNodes, rightNodes, bottomNodes, topNodes;

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="bottomLeftCoords"></param>
        /// <param name="topRightCoords"></param>
        /// <param name="thickness"></param>
        /// <param name="meshTolerance">The default is 1E-10 * min(|xMax-xMin|, |yMax-yMin|)</param>
        public StructuralSquareRve(IStructuralModel model, Vector2 bottomLeftCoords, Vector2 topRightCoords, double thickness, 
            double meshTolerance)
        {
            this.model = model;
            this.xMin = bottomLeftCoords[0];
            this.yMin = bottomLeftCoords[1];
            this.xMax = topRightCoords[0];
            this.yMax = topRightCoords[1];
            this.thickness = thickness;

            // Find the nodes of each edge
            leftNodes = new HashSet<INode>();
            rightNodes = new HashSet<INode>();
            bottomNodes = new HashSet<INode>();
            topNodes = new HashSet<INode>();
            foreach (INode node in model.Nodes)
            {
                // Top and right edges are prioritized for corner nodes. //TODO: should the corner nodes be handled differently?
                if (Math.Abs(node.Y - yMax) <= meshTolerance) topNodes.Add(node);
                else if (Math.Abs(node.X - xMax) <= meshTolerance) rightNodes.Add(node);
                else if (Math.Abs(node.Y - yMin) <= meshTolerance) bottomNodes.Add(node);
                else if (Math.Abs(node.X - xMin) <= meshTolerance) leftNodes.Add(node);
            }
        }

        public StructuralSquareRve(IStructuralModel model, Vector2 bottomLeftCoords, Vector2 topRightCoords, double thickness) : 
            this(model, bottomLeftCoords, topRightCoords, thickness, 
                1E-10 * topRightCoords.Subtract(bottomLeftCoords).MinAbsolute())
        {
        }

        public void ApplyBoundaryConditions()
        {
            foreach (var node in leftNodes.Concat(bottomNodes).Concat(rightNodes).Concat(topNodes))
            {
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationX, Amount = 0.0 });
                node.Constraints.Add(new Constraint() { DOF = StructuralDof.TranslationY, Amount = 0.0 });
            }
        }

        public double CalculateRveVolume() => (xMax - xMin) * (yMax - yMin) * thickness;

        public IMatrixView CalculateKinematicRelationsMatrix(ISubdomain subdomain)
        {
            ISubdomainConstrainedDofOrdering constrainedDofOrdering = subdomain.ConstrainedDofOrdering;
            var kinematicRelations = Matrix.CreateZero(3, constrainedDofOrdering.NumConstrainedDofs);
            CalculateKinematicsOfEdge(constrainedDofOrdering, bottomNodes, kinematicRelations);
            CalculateKinematicsOfEdge(constrainedDofOrdering, leftNodes, kinematicRelations);
            CalculateKinematicsOfEdge(constrainedDofOrdering, rightNodes, kinematicRelations);
            CalculateKinematicsOfEdge(constrainedDofOrdering, topNodes, kinematicRelations);
            return kinematicRelations;
        }

        private void CalculateKinematicsOfEdge(ISubdomainConstrainedDofOrdering constrainedDofOrdering,
            IEnumerable<INode> edgeNodes, Matrix kinematicRelations)
        {
            foreach (INode node in edgeNodes)
            {
                int dofX = constrainedDofOrdering.ConstrainedDofs[node, StructuralDof.TranslationX];
                int dofY = constrainedDofOrdering.ConstrainedDofs[node, StructuralDof.TranslationY];
                kinematicRelations[0, dofX] = node.X;
                kinematicRelations[1, dofY] = node.Y;
                kinematicRelations[2, dofX] = 0.5 * node.Y;
                kinematicRelations[2, dofY] = 0.5 * node.X;
            }
        }
    }
}
