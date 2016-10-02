namespace ISAAR.MSolve.Analyzers.Optimization
{
    public class Individual
    {
        public double[] Position
        {
            get;  set;
        }

        public double ObjectiveValue
        {
            get;  set;
        }

        public Individual(double[] position, double objectiveValue)
        {
            this.Position = position;
            this.ObjectiveValue = objectiveValue;
        }

    }
}
