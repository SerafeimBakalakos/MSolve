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
    /// This works only for cubic (3D) RVEs, centered at (0,0), with 1 thermal dof per node and linear boundary conditions.
    /// </summary>
    public class ThermalCubicRve : IReferenceVolumeElement
    {
        private const int numDimensions = 3;

        private readonly IStructuralModel model;
        private readonly double xMin, yMin, zMin, xMax, yMax, zMax;
        private readonly HashSet<INode> minXnodes, maxXnodes, minYnodes, maxYnodes, minZnodes, maxZnodes;

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="minCornerCoords"></param>
        /// <param name="maxCornerCoords"></param>
        /// <param name="meshTolerance">The default is 1E-10 * min(|xMax-xMin|, |yMax-yMin|)</param>
        public ThermalCubicRve(IStructuralModel model, double[] minCornerCoords, double[] maxCornerCoords, double meshTolerance)
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

        public ThermalCubicRve(IStructuralModel model, double[] minCornerCoords, double[] maxCornerCoords) : 
            this(model, minCornerCoords, maxCornerCoords, 
                1E-10 * Vector.CreateFromArray(maxCornerCoords).Subtract(Vector.CreateFromArray(minCornerCoords)).MinAbsolute())
        {
        }

        public void ApplyBoundaryConditions()
        {
            foreach (var node in minXnodes) node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.0 });
            foreach (var node in minZnodes) node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.0 });
            foreach (var node in minYnodes) node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.0 });
            foreach (var node in maxXnodes) node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.0 });
            foreach (var node in maxZnodes) node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.0 });
            foreach (var node in maxYnodes) node.Constraints.Add(new Constraint() { DOF = ThermalDof.Temperature, Amount = 0.0 });
        }

        public double CalculateRveVolume() => (xMax - xMin) * (yMax - yMin) * (zMax - zMin);

        public IMatrixView CalculateKinematicRelationsMatrix(ISubdomain subdomain)
        {
            ISubdomainConstrainedDofOrdering constrainedDofOrdering = subdomain.ConstrainedDofOrdering;
            var kinematicRelations = Matrix.CreateZero(numDimensions, constrainedDofOrdering.NumConstrainedDofs);
            CalculateKinematicsOfFace(constrainedDofOrdering, minZnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, minXnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, minYnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, maxYnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, maxXnodes, kinematicRelations);
            CalculateKinematicsOfFace(constrainedDofOrdering, maxZnodes, kinematicRelations);
            return kinematicRelations;
        }

        private void CalculateKinematicsOfFace(ISubdomainConstrainedDofOrdering constrainedDofOrdering,
            IEnumerable<INode> edgeNodes, Matrix kinematicRelations)
        {
            foreach (INode node in edgeNodes)
            {
                int dofIdx = constrainedDofOrdering.ConstrainedDofs[node, ThermalDof.Temperature];
                kinematicRelations[0, dofIdx] = node.X;
                kinematicRelations[1, dofIdx] = node.Y;
                kinematicRelations[2, dofIdx] = node.Z;
            }
        }
    }
}
