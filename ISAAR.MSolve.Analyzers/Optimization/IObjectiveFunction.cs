using ISAAR.MSolve.Matrices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ISAAR.MSolve.Analyzers.Optimization
{
    public interface IObjectiveFunction
    {

        /// <summary>
        ///     Calculates the fitness value of the function.
        /// </summary>
        ///     <param name="x">The continuous decision variable vector.
        /// </param>
        /// <returns>
        ///     The fitness value.
        /// </returns>
        double Fitness(double[] x);

        /// <summary>
        ///     Gets the dimension of the decision variable.
        /// </summary>
        /// <returns>
        ///     The dimension of the decision variable.
        /// </returns>
        int Dimension();
    }
}
