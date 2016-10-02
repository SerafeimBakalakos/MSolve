namespace ISAAR.MSolve.Analyzers.Optimization
{
    public class ObjectiveProblem
    {
        public double[] lowerBound
        {
            get; set;
        }

        public double[] upperBound
        {
            get; set;
        }
        public IObjectiveFunction objective
        {
            get; set;
        }

        public ObjectiveProblem(double[] lowerBound, double[] upperBound, IObjectiveFunction objective)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
            this.objective = objective;
        }
    }
}