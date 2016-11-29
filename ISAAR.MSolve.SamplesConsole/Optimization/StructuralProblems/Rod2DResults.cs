using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISAAR.MSolve.Matrices.Interfaces;
using ISAAR.MSolve.PreProcessor;
using ISAAR.MSolve.PreProcessor.Elements;
using ISAAR.MSolve.Solvers.Interfaces;

namespace ISAAR.MSolve.SamplesConsole.Optimization.StructuralProblems
{
    public class Rod2DResults
    {
        private readonly Subdomain subdomain;
        private readonly ISolverSubdomain solverSubdomain;
        private double[] solution;

        public Rod2DResults(Subdomain subdomain, ISolverSubdomain solverSubdomain)
        {
            this.subdomain = subdomain;
            this.solverSubdomain = solverSubdomain;
        }

        public void QueryResults()
        {
            IVector<double> solutionVector = solverSubdomain.Solution;
            this.solution = new double[solutionVector.Length];
            solutionVector.CopyTo(solution, 0);
        }

        public double AxialRod2DStress(Element element)
        {
            double[] localDisplacements = subdomain.GetLocalVectorFromGlobal(element, solution);
            Rod2D rod = (Rod2D)element.ElementType;
            return  rod.CalculateAxialStress(element, localDisplacements, null);
        }
    }
}
